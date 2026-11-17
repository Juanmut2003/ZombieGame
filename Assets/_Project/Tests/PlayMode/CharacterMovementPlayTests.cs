#nullable enable
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using ZombieGame.Characters;

namespace ZombieGame.Tests.PlayMode
{
    /// <summary>
    /// Drives the character through the real scene, with real physics.
    /// </summary>
    /// <remarks>
    /// The maths is covered by edit-mode tests; what these cover is everything in between — that
    /// the components are actually wired up in the sandbox, that the rigidbody moves, and that the
    /// wall collider stops it. None of that can be asserted without play mode running.
    /// </remarks>
    [TestFixture]
    public sealed class CharacterMovementPlayTests
    {
        private const string SandboxScene = "Sandbox";
        private const string PlayerName = "MC (placeholder)";

        private PlayerInputReader _input = null!;
        private Transform _player = null!;

        [UnitySetUp]
        public IEnumerator LoadSandbox()
        {
            yield return SceneManager.LoadSceneAsync(SandboxScene, LoadSceneMode.Single);

            GameObject? playerObject = GameObject.Find(PlayerName);
            Assert.That(playerObject, Is.Not.Null, $"the sandbox should contain '{PlayerName}'");

            _player = playerObject!.transform;
            _input = playerObject.GetComponent<PlayerInputReader>();
            Assert.That(_input, Is.Not.Null, "the MC should be steerable");
            Assert.That(
                playerObject.GetComponent<CharacterMovement>(),
                Is.Not.Null,
                "the MC should have movement");
        }

        [TearDown]
        public void ReleaseInput() => _input.ClearInputOverride();

        [UnityTest]
        public IEnumerator TheCharacterMovesWhereTheInputPoints()
        {
            Vector3 start = _player.position;

            _input.SetInputOverride(Vector2.right, sprint: false);
            yield return RunFor(0.5f);

            Assert.That(_player.position.x, Is.GreaterThan(start.x + 0.5f), "it should have walked");
            Assert.That(
                _player.position.y, Is.EqualTo(start.y).Within(0.05f), "and not drifted sideways");
        }

        [UnityTest]
        public IEnumerator ReleasingTheInputBringsItToAStop()
        {
            _input.SetInputOverride(Vector2.right, sprint: false);
            yield return RunFor(0.4f);

            _input.SetInputOverride(Vector2.zero, sprint: false);
            yield return RunFor(0.5f);

            Vector3 resting = _player.position;
            yield return RunFor(0.3f);

            Assert.That(
                _player.position.x, Is.EqualTo(resting.x).Within(0.001f), "it should stand still");
        }

        [UnityTest]
        public IEnumerator SprintingCoversMoreGround()
        {
            Vector3 start = _player.position;
            _input.SetInputOverride(Vector2.up, sprint: false);
            yield return RunFor(0.4f);
            float walked = _player.position.y - start.y;

            yield return LoadSandbox();

            start = _player.position;
            _input.SetInputOverride(Vector2.up, sprint: true);
            yield return RunFor(0.4f);
            float sprinted = _player.position.y - start.y;

            Assert.That(sprinted, Is.GreaterThan(walked * 1.2f), "sprinting should be clearly faster");
        }

        [UnityTest]
        public IEnumerator TheWallStopsIt()
        {
            // Long enough to cross the room several times over, so the only thing that can keep it
            // inside is the wall collider.
            _input.SetInputOverride(Vector2.right, sprint: true);
            yield return RunFor(4f);

            float stopped = _player.position.x;
            Assert.That(stopped, Is.LessThan(6f), "it should be held inside the room");

            yield return RunFor(1f);
            Assert.That(
                _player.position.x,
                Is.EqualTo(stopped).Within(0.01f),
                "and stay there rather than grinding through");
        }

        private static IEnumerator RunFor(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;
            }
        }
    }
}
