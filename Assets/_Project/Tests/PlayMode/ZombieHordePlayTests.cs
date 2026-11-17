#nullable enable
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using ZombieGame.AI;
using ZombieGame.Combat;
using ZombieGame.Core;
using ZombieGame.Core.Events;
using ZombieGame.Waves;

namespace ZombieGame.Tests.PlayMode
{
    /// <summary>
    /// Checks the horde in the real scene: that the director put zombies down, gave them bodies,
    /// and that they come after the player once something gives them a reason to.
    /// </summary>
    /// <remarks>
    /// The manager's rules are covered by edit-mode tests. What only play mode can show is that
    /// the thing is actually wired into the scene and that the bodies follow the data — a
    /// screenshot cannot, because nothing spawns until the game runs.
    /// </remarks>
    [TestFixture]
    public sealed class ZombieHordePlayTests
    {
        private ZombieDirector _director = null!;

        [UnitySetUp]
        public IEnumerator LoadSandbox()
        {
            yield return SceneManager.LoadSceneAsync("Sandbox", LoadSceneMode.Single);
            yield return new WaitForFixedUpdate();

            GameObject? directorObject = GameObject.Find("Zombie Director");
            Assert.That(directorObject, Is.Not.Null, "the sandbox should contain a zombie director");
            _director = directorObject!.GetComponent<ZombieDirector>();
            Assert.That(_director, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator TheDirectorPutsAHordeDown()
        {
            yield return null;

            Assert.That(_director.Manager.Count, Is.GreaterThan(0), "the zone should not be empty");
            Assert.That(
                _director.Manager.Capacity,
                Is.GreaterThanOrEqualTo(_director.Manager.Count));
        }

        [UnityTest]
        public IEnumerator EveryLiveZombieHasABody()
        {
            yield return null;
            yield return null;

            ZombieView[] bodies = Object.FindObjectsByType<ZombieView>(FindObjectsInactive.Exclude);

            Assert.That(
                bodies.Length,
                Is.EqualTo(_director.Manager.Count),
                "one visible body per live zombie, no more and no fewer");
        }

        [UnityTest]
        public IEnumerator ANoiseBringsTheHordeCloser()
        {
            yield return null;

            GameObject player = GameObject.Find("MC");
            Assert.That(player, Is.Not.Null);

            var target = (Vector2)player.transform.position;
            float before = AverageDistanceTo(target);

            // They no longer walk at the player because he exists; something has to give them a
            // reason. A loud noise where he stands is the reason.
            Assert.That(GameServices.TryGet(out EventBus? bus), Is.True);
            bus!.Publish(new NoiseEvent(target, loudness: 6f));

            yield return RunFor(1.5f);

            float after = AverageDistanceTo(target);
            Assert.That(after, Is.LessThan(before), "they should have closed the gap");
        }

        [UnityTest]
        public IEnumerator BeingSeenTurnsAnInvestigationIntoAChase()
        {
            yield return null;

            GameObject player = GameObject.Find("MC");
            Assert.That(player, Is.Not.Null);

            Assert.That(GameServices.TryGet(out EventBus? bus), Is.True);
            bus!.Publish(new NoiseEvent((Vector2)player.transform.position, loudness: 6f));

            yield return RunFor(3f);

            int chasing = 0;
            System.ReadOnlySpan<ZombieState> states = _director.Manager.States;
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i].Behaviour == ZombieBehaviour.Chasing)
                {
                    chasing++;
                }
            }

            Assert.That(
                chasing,
                Is.GreaterThan(0),
                "walking towards a noise should put the player in somebody's view cone");
        }

        [UnityTest]
        public IEnumerator KillingOneTakesItsBodyAwayToo()
        {
            yield return null;

            int before = _director.Manager.Count;
            _director.Manager.Damage(0, new DamageInfo(1000f));

            yield return null;
            yield return null;

            Assert.That(_director.Manager.Count, Is.EqualTo(before - 1));
            Assert.That(
                Object.FindObjectsByType<ZombieView>(FindObjectsInactive.Exclude),
                Has.Length.EqualTo(before - 1),
                "the spare body goes back to the pool rather than standing around dead");
        }

        private float AverageDistanceTo(Vector2 target)
        {
            float sum = 0f;
            System.ReadOnlySpan<ZombieState> states = _director.Manager.States;
            for (int i = 0; i < states.Length; i++)
            {
                sum += Vector2.Distance(states[i].Position, target);
            }

            return states.Length == 0 ? 0f : sum / states.Length;
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
