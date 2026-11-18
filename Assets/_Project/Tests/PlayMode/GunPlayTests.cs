#nullable enable
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using ZombieGame.AI;
using ZombieGame.Characters;
using ZombieGame.Combat;
using ZombieGame.Waves;

namespace ZombieGame.Tests.PlayMode
{
    /// <summary>
    /// The gun in the real scene: a round leaves it, crosses the room, and arrives.
    /// </summary>
    /// <remarks>
    /// The arithmetic of flight and of counting rounds is settled in edit mode. What only a
    /// loaded scene shows is the chain: trigger, event, pooled round, the horde's hit test, and a
    /// zombie with less health than it had — six pieces that each work alone.
    /// </remarks>
    [TestFixture]
    public sealed class GunPlayTests
    {
        private ZombieDirector _director = null!;
        private PlayerGun _gun = null!;
        private CharacterFacing _facing = null!;
        private Transform _player = null!;
        private ProjectileRunner _runner = null!;

        [UnitySetUp]
        public IEnumerator LoadSandbox()
        {
            yield return SceneManager.LoadSceneAsync("Sandbox", LoadSceneMode.Single);
            yield return new WaitForFixedUpdate();

            GameObject? playerObject = GameObject.Find("MC");
            Assert.That(playerObject, Is.Not.Null);
            _player = playerObject!.transform;
            _gun = playerObject.GetComponent<PlayerGun>();
            _facing = playerObject.GetComponent<CharacterFacing>();
            Assert.That(_gun, Is.Not.Null, "the MC should be carrying a gun");

            GameObject? runnerObject = GameObject.Find("Projectiles");
            Assert.That(runnerObject, Is.Not.Null, "the zone should run the rounds");
            _runner = runnerObject!.GetComponent<ProjectileRunner>();

            _director = GameObject.Find("Zombie Director").GetComponent<ZombieDirector>();
        }

        [UnityTest]
        public IEnumerator FiringPutsARoundInTheAir()
        {
            _facing.Face(Vector2.right);

            Assert.That(_gun.TryFire(), Is.True);

            // Checked before the next step runs: the round is spawned by the shot itself, and a
            // frame later it may already have found something.
            Assert.That(_runner.Rounds.Count, Is.GreaterThan(0));
            yield break;
        }

        [UnityTest]
        public IEnumerator FiringSpendsARound()
        {
            int before = _gun.Ammo.Loaded;

            Assert.That(_gun.TryFire(), Is.True);
            yield return null;

            Assert.That(_gun.Ammo.Loaded, Is.EqualTo(before - 1));
        }

        [UnityTest]
        public IEnumerator AShotHurtsAZombieAcrossTheRoom()
        {
            int targetId = ClearTheRoomAndKeepOne();

            // Four units away — far beyond anything the bat can reach.
            Vector2 spot = (Vector2)_player.position + new Vector2(4f, 0f);
            MoveZombieTo(targetId, spot);
            _facing.Face(Vector2.right);

            float before = FindHealth(targetId);
            Assert.That(_gun.TryFire(), Is.True);

            // Long enough for the round to cross four units at the muzzle speed on the asset.
            yield return new WaitForSeconds(0.5f);

            Assert.That(
                FindHealth(targetId),
                Is.LessThan(before),
                "the round never arrived");
        }

        [UnityTest]
        public IEnumerator AnEmptyGunDoesNotFire()
        {
            while (_gun.Ammo.Loaded > 0)
            {
                _gun.Ammo.TryFire();
            }

            Assert.That(_gun.TryFire(), Is.False);
            yield break;
        }

        [UnityTest]
        public IEnumerator ReloadingTakesTimeAndThenFillsTheGun()
        {
            while (_gun.Ammo.Loaded > 0)
            {
                _gun.Ammo.TryFire();
            }

            Assert.That(_gun.TryReload(), Is.True);
            Assert.That(_gun.IsReloading, Is.True);
            Assert.That(_gun.TryFire(), Is.False, "a gun being reloaded cannot also be fired");

            yield return new WaitForSeconds(2f);

            Assert.That(_gun.IsReloading, Is.False);
            Assert.That(_gun.Ammo.Loaded, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator TheHudShowsTheCount()
        {
            GameObject? label = GameObject.Find("Ammo Label");
            Assert.That(label, Is.Not.Null, "the HUD should carry an ammo count");

            string before = label!.GetComponent<Text>().text;
            _gun.TryFire();
            yield return null;

            Assert.That(label.GetComponent<Text>().text, Is.Not.EqualTo(before));
        }

        [UnityTest]
        public IEnumerator AShotIsLouderThanASwing()
        {
            GameObject? heatObject = GameObject.Find("Zone Heat");
            Assert.That(heatObject, Is.Not.Null);
            ZoneHeat heat = heatObject!.GetComponent<ZoneHeat>();

            float before = heat.Value;
            _player.GetComponent<PlayerMelee>().TrySwing();
            yield return null;
            float afterSwing = heat.Value - before;

            before = heat.Value;
            Assert.That(_gun.TryFire(), Is.True);
            yield return null;
            float afterShot = heat.Value - before;

            // The whole reason to own a bat.
            Assert.That(afterShot, Is.GreaterThan(afterSwing * 2f));
        }

        /// <summary>
        /// Sends the whole horde off to the far corner and returns the one left to shoot at.
        /// </summary>
        /// <remarks>
        /// A zone with two dozen zombies standing around is the wrong place to ask whether a
        /// particular one was hit: any of the others can catch the round on the way, and the test
        /// would fail for a reason that is actually correct behaviour.
        /// </remarks>
        private int ClearTheRoomAndKeepOne()
        {
            int kept = _director.Manager.Get(0).Id;

            for (int i = 1; i < _director.Manager.Count; i++)
            {
                _director.Manager.Teleport(i, new Vector2(120f, 120f + i));
            }

            return kept;
        }

        private void MoveZombieTo(int id, Vector2 position)
        {
            int index = _director.Manager.IndexOf(id);
            Assert.That(index, Is.GreaterThanOrEqualTo(0));
            _director.Manager.Teleport(index, position);
        }

        private float FindHealth(int id)
        {
            int index = _director.Manager.IndexOf(id);
            return index < 0 ? 0f : _director.Manager.Get(index).Health;
        }
    }
}
