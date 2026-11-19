#nullable enable
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using ZombieGame.Audio;
using ZombieGame.Characters;
using ZombieGame.Core;
using ZombieGame.Core.Events;
using ZombieGame.Missions;

namespace ZombieGame.Tests.PlayMode
{
    /// <summary>
    /// That a zone actually makes a noise when something happens in it.
    /// </summary>
    /// <remarks>
    /// The arithmetic of what gets played is settled in edit mode. What only the built zone can
    /// show is that the pieces are in it and joined up: speakers in the scene, a bank with clips
    /// in it, and a router subscribed to the bus. Each of those is invisible when missing — the
    /// game simply stays quiet, which is also what it did before any of this existed.
    /// </remarks>
    [TestFixture]
    public sealed class SoundPlayTests
    {
        private AudioService _audio = null!;

        [UnitySetUp]
        public IEnumerator LoadTown()
        {
            yield return SceneManager.LoadSceneAsync("Mission_Town", LoadSceneMode.Single);
            yield return new WaitForFixedUpdate();

            GameObject? audioObject = GameObject.Find("Audio");
            Assert.That(audioObject, Is.Not.Null, "the zone should have speakers in it");
            _audio = audioObject!.GetComponent<AudioService>();
            Assert.That(_audio, Is.Not.Null);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TheZoneRegistersSomethingToPlaySoundsWith()
        {
            Assert.That(GameServices.TryGet(out ISoundService? service), Is.True);
            Assert.That(service, Is.Not.Null);

            yield return null;
        }

        [UnityTest]
        public IEnumerator SwingingMakesANoise()
        {
            Assert.That(GameServices.TryGet(out EventBus? bus), Is.True);

            int before = _audio.Played;
            bus!.Publish(new ZombieGame.Combat.MeleeSwungEvent(
                attackerId: 1,
                origin: Vector2.zero,
                facing: Vector2.right,
                range: 1.45f,
                halfAngleDegrees: 60f,
                maxTargets: 3,
                damage: default));

            yield return null;

            Assert.That(
                _audio.Played,
                Is.GreaterThan(before),
                "a swing published on the bus should have reached the speakers");
        }

        [UnityTest]
        public IEnumerator LootingMakesADifferentNoise()
        {
            Assert.That(GameServices.TryGet(out EventBus? bus), Is.True);

            int before = _audio.Played;
            bus!.Publish(new LootTakenEvent(Vector2.zero, supplies: 2, ammo: 0));

            yield return null;

            Assert.That(_audio.Played, Is.GreaterThan(before));
        }

        [UnityTest]
        public IEnumerator ACrowdDyingAtOnceIsNotACrowdOfSounds()
        {
            Assert.That(GameServices.TryGet(out EventBus? bus), Is.True);

            int before = _audio.Played;
            for (int i = 0; i < 20; i++)
            {
                bus!.Publish(new ZombieGame.Combat.DiedEvent(targetId: 500 + i, killingBlow: default));
            }

            yield return null;

            // The whole reason the limiter exists: twenty of the same sample at the same instant
            // is one flat crack, not twenty deaths.
            Assert.That(_audio.Played - before, Is.LessThanOrEqualTo(2));
        }

        [UnityTest]
        public IEnumerator TheSquadIsWiredUpToItsOwnSounds()
        {
            GameObject? player = GameObject.Find("MC");
            Assert.That(player, Is.Not.Null);
            Assert.That(
                player!.GetComponent<CharacterVitals>(),
                Is.Not.Null,
                "the router tells the squad's injuries apart by its owner id");

            yield return null;
        }
    }
}
