#nullable enable
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using ZombieGame.AI;
using ZombieGame.Characters;
using ZombieGame.Core;
using ZombieGame.Core.Physics;
using ZombieGame.Missions;

namespace ZombieGame.Tests.PlayMode
{
    /// <summary>
    /// The depot as a mission: shelves that can be gone through, and a gate that ends the run.
    /// </summary>
    /// <remarks>
    /// The rules are settled in edit mode. What only the built zone shows is that the pieces are
    /// actually in it and wired to each other — a map with no crates in it, or an extraction
    /// point watching nobody, looks exactly like a working mission until it is played.
    /// </remarks>
    [TestFixture]
    public sealed class MissionPlayTests
    {
        private MissionRunner _mission = null!;
        private ExtractionPoint _extraction = null!;
        private PlayerInputReader _input = null!;
        private Transform _player = null!;

        [UnitySetUp]
        public IEnumerator LoadDepot()
        {
            yield return SceneManager.LoadSceneAsync("Mission_Depot", LoadSceneMode.Single);
            yield return new WaitForFixedUpdate();

            GameObject? playerObject = GameObject.Find("MC");
            Assert.That(playerObject, Is.Not.Null, "the depot should have an MC in it");
            _player = playerObject!.transform;
            _input = playerObject.GetComponent<PlayerInputReader>();

            GameObject? missionObject = GameObject.Find("Mission");
            Assert.That(missionObject, Is.Not.Null, "and a mission to run");
            _mission = missionObject!.GetComponent<MissionRunner>();

            GameObject? extractionObject = GameObject.Find("Extraction");
            Assert.That(extractionObject, Is.Not.Null, "and a way out");
            _extraction = extractionObject!.GetComponent<ExtractionPoint>();

            yield return null;
        }

        [UnityTest]
        public IEnumerator TheZoneIsFullOfThingsToGoThrough()
        {
            LootContainer[] crates =
                Object.FindObjectsByType<LootContainer>(FindObjectsInactive.Exclude);

            Assert.That(crates, Has.Length.GreaterThanOrEqualTo(5));
            yield break;
        }

        [UnityTest]
        public IEnumerator SearchingACrateTakesTimeAndThenPaysOut()
        {
            LootContainer crate =
                Object.FindObjectsByType<LootContainer>(FindObjectsInactive.Exclude)[0];

            // Standing next to it and holding the button, which is what a player does.
            _player.position = crate.transform.position + new Vector3(0.4f, 0f, 0f);
            _input.SetInputOverride(Vector2.zero, sprint: false, interact: true);

            yield return null;
            Assert.That(crate.IsEmpty, Is.False, "it should not empty in a single frame");
            Assert.That(_mission.Progress.Carried, Is.Zero);

            yield return new WaitForSeconds(2.5f);

            Assert.That(crate.IsEmpty, Is.True, "holding the button should have emptied it");
            Assert.That(_mission.Progress.Carried, Is.GreaterThan(0), "and the run should count it");
        }

        [UnityTest]
        public IEnumerator WalkingAwayLosesTheSearch()
        {
            LootContainer crate =
                Object.FindObjectsByType<LootContainer>(FindObjectsInactive.Exclude)[0];

            _player.position = crate.transform.position + new Vector3(0.4f, 0f, 0f);
            _input.SetInputOverride(Vector2.zero, sprint: false, interact: true);
            yield return new WaitForSeconds(0.8f);

            Assert.That(crate.SearchFraction, Is.GreaterThan(0f));

            _input.SetInputOverride(Vector2.zero, sprint: false, interact: false);
            yield return null;

            Assert.That(crate.SearchFraction, Is.Zero, "a search in instalments is not a search");
            Assert.That(crate.IsEmpty, Is.False);
        }

        [UnityTest]
        public IEnumerator StandingOnTheGateEndsTheRun()
        {
            _player.position = _extraction.transform.position;
            yield return null;

            Assert.That(_extraction.SquadInside, Is.True);

            // Long enough for the hold the mission asset asks for.
            yield return new WaitForSeconds(5f);

            Assert.That(_mission.Progress.State, Is.EqualTo(MissionState.Extracted));
        }

        [UnityTest]
        public IEnumerator LootOnlyCountsOnceTheSquadIsOut()
        {
            LootContainer crate =
                Object.FindObjectsByType<LootContainer>(FindObjectsInactive.Exclude)[0];

            _player.position = crate.transform.position + new Vector3(0.4f, 0f, 0f);
            _input.SetInputOverride(Vector2.zero, sprint: false, interact: true);
            yield return new WaitForSeconds(2.5f);

            int carried = _mission.Progress.Carried;
            Assert.That(carried, Is.GreaterThan(0));
            Assert.That(_mission.Progress.Banked, Is.Zero, "nothing counts out in the zone");

            _input.SetInputOverride(Vector2.zero, sprint: false, interact: false);
            _player.position = _extraction.transform.position;
            yield return new WaitForSeconds(5f);

            Assert.That(_mission.Progress.Banked, Is.EqualTo(carried));
        }

        [UnityTest]
        public IEnumerator GettingOutTakesTheSquadHome()
        {
            GameObject? flowObject = GameObject.Find("Mission End");
            Assert.That(flowObject, Is.Not.Null, "a finished run has to lead somewhere");
            MissionEndFlow flow = flowObject!.GetComponent<MissionEndFlow>();

            Assert.That(flow.IsLeaving, Is.False);

            _player.position = _extraction.transform.position;
            yield return new WaitForSeconds(5f);

            // The result stays up for a moment and then the zone is left; without this the
            // player stood in a finished zone reading a line that said they were done.
            Assert.That(flow.IsLeaving, Is.True);
        }

        [UnityTest]
        public IEnumerator TheZoneKnowsWhereItsWallsAre()
        {
            Assert.That(GameServices.TryGet(out IWallMap? walls), Is.True, "the walls were never baked");

            // The corner of the map, which every drawing walls in.
            GameObject? wallsObject = GameObject.Find("Zone Walls");
            Assert.That(wallsObject, Is.Not.Null);

            // The outer wall, which every drawing has all the way round. Counted rather than
            // sampled once: it is the band being checked, not one particular tile.
            int solid = 0;
            for (float x = -12f; x <= 12f; x += 1f)
            {
                if (walls!.IsSolid(new Vector2(x, 10f)))
                {
                    solid++;
                }
            }

            Assert.That(solid, Is.GreaterThan(20), "the top edge is not solid");

            // And the yard inside it is not.
            Assert.That(walls!.IsSolid(_extraction.transform.position), Is.False);
            yield break;
        }

        [UnityTest]
        public IEnumerator TheBatWorksInThisZoneToo()
        {
            // It did not, once. The horde subscribed to swings in Awake, which runs before the
            // bootstrap has registered the bus in a scene where the objects happen to sit the
            // other way round — so the bat did nothing here while the pistol, which travels a
            // different path, was fine. One zone working is not the same as the game working.
            GameObject? directorObject = GameObject.Find("Zombie Director");
            Assert.That(directorObject, Is.Not.Null);
            ZombieManager horde = directorObject!.GetComponent<ZombieDirector>().Manager;
            Assert.That(horde.Count, Is.GreaterThan(0), "an empty zone proves nothing");

            PlayerMelee melee = _player.GetComponent<PlayerMelee>();
            Loadout loadout = _player.GetComponent<Loadout>();
            Assert.That(loadout.Select(0), Is.True, "slot one is the bat");

            int id = horde.Get(0).Id;
            horde.Teleport(0, (Vector2)_player.position + new Vector2(0.7f, 0f));

            Camera camera = Camera.main;
            _input.SetAimOverride(
                camera.WorldToScreenPoint((Vector2)_player.position + new Vector2(3f, 0f)));
            yield return null;

            float before = horde.Get(horde.IndexOf(id)).Health;
            Assert.That(melee.TrySwing(), Is.True);
            yield return null;

            int index = horde.IndexOf(id);
            float after = index < 0 ? 0f : horde.Get(index).Health;
            Assert.That(after, Is.LessThan(before), "the swing went nowhere");
        }

        [UnityTest]
        public IEnumerator TheHudSaysWhatTheRunIsFor()
        {
            GameObject? objective = GameObject.Find("Objective Label");
            Assert.That(objective, Is.Not.Null, "the HUD should carry the objective");

            yield return null;

            Assert.That(objective!.GetComponent<Text>().text, Is.Not.Empty);
        }
    }
}
