#nullable enable
using NUnit.Framework;
using UnityEngine;
using ZombieGame.AI;
using ZombieGame.Core.Config;

namespace ZombieGame.Tests.EditMode
{
    /// <summary>
    /// What keeping low actually buys, and what it costs.
    /// </summary>
    /// <remarks>
    /// PROJECT.md §9 asks for three things from crouching — quiet, slower, a smaller detection
    /// cone — and each is a number somebody can turn. A crouch that is only quieter is a free
    /// action; one that is only slower is a punishment. The three have to move together, and the
    /// cross-system rule is that crouching must be worth the speed it costs.
    /// </remarks>
    [TestFixture]
    public sealed class CrouchTests
    {
        private static BalanceConfig Balance() => BalanceConfig.CreateDefault();

        [Test]
        public void CrouchingIsSlowerThanWalking()
        {
            BalanceConfig balance = Balance();

            Assert.That(balance.CrouchSpeedFactor, Is.LessThan(1f));
        }

        [Test]
        public void CrouchingIsQuieterThanWalking()
        {
            BalanceConfig balance = Balance();

            Assert.That(balance.CrouchNoise, Is.LessThan(balance.WalkNoise));
            Assert.That(balance.CrouchNoise, Is.LessThan(balance.SprintNoise));
        }

        [Test]
        public void CrouchedStepsAreFurtherApart()
        {
            BalanceConfig balance = Balance();

            Assert.That(balance.CrouchStepSeconds, Is.GreaterThan(balance.WalkStepSeconds));
        }

        [Test]
        public void CrouchingIsWorthTheSpeedItCosts()
        {
            // The rule the three numbers have to keep between them. Noise per step falls further
            // than steps per second rise, so crouching across a room is quieter in total and not
            // merely quieter per footfall — otherwise the slower you went the more noise you made.
            BalanceConfig balance = Balance();

            float walkingPerSecond = balance.WalkNoise / balance.WalkStepSeconds;
            float crouchingPerSecond = balance.CrouchNoise / balance.CrouchStepSeconds;

            Assert.That(crouchingPerSecond, Is.LessThan(walkingPerSecond * 0.5f));
        }

        [Test]
        public void CrouchingShortensHowFarYouCanBeSeen()
        {
            var definition = ScriptableObject.CreateInstance<ZombieDefinition>();
            try
            {
                var manager = new ZombieManager(capacity: 4, crowd: CrowdSettings.None, seed: 11);
                int id = manager.Spawn(definition, Vector2.zero);
                int index = manager.IndexOf(id);

                // Looking straight at somebody near the far end of its sight.
                var player = new Vector2(0f, -5.5f);
                manager.SetDestination(index, new Vector2(0f, -1f));
                manager.Tick(0.05f);

                Assert.That(manager.Perceive(player, 0.25f), Is.EqualTo(1), "standing up, in view");

                Assert.That(
                    manager.Perceive(player, 0.25f, null, Balance().CrouchVisibility),
                    Is.Zero,
                    "crouched at the same spot, out of view");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void CrouchingDoesNotMakeYouInaudible()
        {
            // Ears have no cone and keeping low does not silence a footstep. Walking into
            // somebody while crouched still gets you noticed.
            var definition = ScriptableObject.CreateInstance<ZombieDefinition>();
            try
            {
                var manager = new ZombieManager(capacity: 4, crowd: CrowdSettings.None, seed: 12);
                manager.Spawn(definition, Vector2.zero);

                Assert.That(
                    manager.CountHearing(new Vector2(0f, 1f), loudness: 1f),
                    Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }
    }
}
