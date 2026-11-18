#nullable enable
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using ZombieGame.Characters;
using ZombieGame.Missions;

namespace ZombieGame.Tests.PlayMode
{
    /// <summary>
    /// Whether the built town can actually be played: the controls are connected, and standing
    /// still at the start does not end the run.
    /// </summary>
    /// <remarks>
    /// Every other test drives the character with <c>SetInputOverride</c>, which proves the
    /// movement works and proves nothing about the wiring in front of it — the actions asset, the
    /// reference on the component, the map being switched on. Those are what a scene rebuild
    /// breaks, and they break silently: the character simply stands there.
    /// Pressing a real key cannot be tested from here. A batch-mode editor has no window focus,
    /// and the Input System disables its devices when an application loses focus, so a queued key
    /// never arrives however it is sent. What can be checked is everything up to the key, which
    /// is where the breakages actually are.
    /// </remarks>
    [TestFixture]
    public sealed class ControlsPlayTests
    {
        private PlayerInputReader _input = null!;
        private Transform _player = null!;

        [UnitySetUp]
        public IEnumerator LoadTown()
        {
            yield return SceneManager.LoadSceneAsync("Mission_Town", LoadSceneMode.Single);
            yield return new WaitForFixedUpdate();

            GameObject? playerObject = GameObject.Find("MC");
            Assert.That(playerObject, Is.Not.Null, "the town should have an MC in it");
            _player = playerObject!.transform;
            _input = playerObject.GetComponent<PlayerInputReader>();
            Assert.That(_input, Is.Not.Null, "and the MC should be reading input");

            yield return null;
        }

        private InputActionAsset Actions() =>
            (InputActionAsset)typeof(PlayerInputReader)
                .GetField("_actions", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(_input)!;

        [UnityTest]
        public IEnumerator TheMovementKeysAreConnected()
        {
            InputActionAsset actions = Actions();
            Assert.That(actions, Is.Not.Null, "the MC has no actions asset");

            InputActionMap? map = actions.FindActionMap("Gameplay", throwIfNotFound: false);
            Assert.That(map, Is.Not.Null, "there is no Gameplay map in the asset");

            InputAction? move = map!.FindAction("Move", throwIfNotFound: false);
            Assert.That(move, Is.Not.Null, "there is no Move action in the map");

            // Switched on is the one that breaks without a trace: the asset is there, the
            // bindings are there, and nothing reads.
            Assert.That(map.enabled, Is.True, "the Gameplay map was never switched on");

            // Eight controls is WASD and the arrow keys. A keyboard has to be present for them
            // to resolve at all, which in a batch editor it may not be, so this only insists
            // the bindings exist rather than that they found a device.
            Assert.That(
                move!.bindings.Count,
                Is.GreaterThanOrEqualTo(8),
                "Move should be bound to WASD and the arrows");

            yield return null;
        }

        [UnityTest]
        public IEnumerator EveryOtherControlIsConnectedToo()
        {
            InputActionMap map = Actions().FindActionMap("Gameplay", throwIfNotFound: false)!;

            foreach (string named in new[]
                     { "Move", "Sprint", "Attack", "Reload", "Aim", "Interact", "Slot1" })
            {
                InputAction? action = map.FindAction(named, throwIfNotFound: false);
                Assert.That(action, Is.Not.Null, $"no '{named}' action");
                Assert.That(action!.bindings.Count, Is.GreaterThan(0), $"'{named}' binds nothing");
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator StandingStillAtTheStartDoesNotEndTheRun()
        {
            // Since the horde bites and finds its way round walls, the opening seconds are the
            // place a run can now be lost before the player has done anything. A squad that dies
            // on the doorstep is sent back to the menu, where pressing a movement key does
            // nothing at all — which from the far side of the screen looks exactly like broken
            // controls.
            var vitals = _player.GetComponent<CharacterVitals>();
            var mission = GameObject.Find("Mission")!.GetComponent<MissionRunner>();

            float waited = 0f;
            while (waited < 8f)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            Assert.That(vitals!.IsAlive, Is.True, "the squad died standing on its own start");
            Assert.That(mission.Progress.State, Is.EqualTo(MissionState.Running));
            Assert.That(
                SceneManager.GetActiveScene().name,
                Is.EqualTo("Mission_Town"),
                "the zone should still be the one that was loaded");
        }
    }
}
