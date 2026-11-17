#nullable enable
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using ZombieGame.AI;
using ZombieGame.Characters;
using ZombieGame.Core;
using ZombieGame.Core.Events;
using ZombieGame.Waves;

namespace ZombieGame.Tests.PlayMode
{
    /// <summary>
    /// The noise chain in a running scene: a sound is made, the zone hears it, the HUD shows it.
    /// </summary>
    /// <remarks>
    /// The arithmetic is covered in edit mode. What only a loaded scene can show is that the
    /// pieces are actually wired to each other — a meter nobody feeds and a bar nobody tells look
    /// exactly like a calm zone.
    /// </remarks>
    [TestFixture]
    public sealed class ZoneHeatPlayTests
    {
        private ZoneHeat _heat = null!;
        private RectTransform _fill = null!;
        private EventBus _bus = null!;

        [UnitySetUp]
        public IEnumerator LoadSandbox()
        {
            yield return SceneManager.LoadSceneAsync("Sandbox", LoadSceneMode.Single);

            GameObject? heatObject = GameObject.Find("Zone Heat");
            Assert.That(heatObject, Is.Not.Null, "the zone should have a heat meter");
            _heat = heatObject!.GetComponent<ZoneHeat>();

            GameObject? fillObject = GameObject.Find("Heat Bar Fill");
            Assert.That(fillObject, Is.Not.Null, "the HUD should carry a heat bar");
            _fill = (RectTransform)fillObject!.transform;

            Assert.That(GameServices.TryGet(out EventBus? bus), Is.True);
            _bus = bus!;

            yield return null;
        }

        [UnityTest]
        public IEnumerator TheZoneStartsQuiet()
        {
            Assert.That(_heat.Stage, Is.EqualTo(HeatStage.Calm));
            Assert.That(_fill.anchorMax.x, Is.LessThan(0.05f));
            yield break;
        }

        [UnityTest]
        public IEnumerator ANoiseMovesTheBar()
        {
            float before = _fill.anchorMax.x;

            _bus.Publish(new NoiseEvent(Vector2.zero, loudness: 6f));
            yield return null;

            Assert.That(_heat.Value, Is.GreaterThan(0f));
            Assert.That(_fill.anchorMax.x, Is.GreaterThan(before));
        }

        [UnityTest]
        public IEnumerator SwingingTheBatIsHeardByTheZone()
        {
            GameObject? playerObject = GameObject.Find("MC");
            Assert.That(playerObject, Is.Not.Null);

            float before = _heat.Value;
            Assert.That(playerObject!.GetComponent<PlayerMelee>().TrySwing(), Is.True);
            yield return null;

            // Not the loudest thing in the game, but not free either.
            Assert.That(_heat.Value, Is.GreaterThan(before));
        }

        [UnityTest]
        public IEnumerator EnoughNoiseBringsMoreZombies()
        {
            GameObject? directorObject = GameObject.Find("Zombie Director");
            Assert.That(directorObject, Is.Not.Null);
            ZombieManager horde = directorObject!.GetComponent<ZombieDirector>().Manager;

            int before = horde.Count;

            // Loud enough to cross a stage in one go, which is what the zone escalates on.
            _bus.Publish(new NoiseEvent(Vector2.zero, loudness: 40f));
            yield return null;

            Assert.That(
                horde.Count,
                Is.GreaterThan(before),
                "crossing a heat stage should walk more zombies into the zone");
        }

        [UnityTest]
        public IEnumerator TheHordeHearsWhatTheZoneHears()
        {
            GameObject? directorObject = GameObject.Find("Zombie Director");
            Assert.That(directorObject, Is.Not.Null);
            ZombieManager horde = directorObject!.GetComponent<ZombieDirector>().Manager;
            Assert.That(horde.Count, Is.GreaterThan(0), "an empty zone proves nothing");

            Vector2 where = horde.Get(0).Position + new Vector2(0f, 1.5f);
            _bus.Publish(new NoiseEvent(where, loudness: 2f));
            yield return null;

            Vector2 look = horde.Get(0).Look;
            Assert.That(look.y, Is.GreaterThan(0.3f), "it should be looking towards the noise");
        }
    }
}
