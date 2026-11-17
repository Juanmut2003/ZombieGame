#nullable enable
using System;
using NUnit.Framework;
using UnityEngine;
using ZombieGame.AI;

namespace ZombieGame.Tests.EditMode
{
    /// <summary>
    /// What a zombie can see and hear, settled as geometry.
    /// </summary>
    /// <remarks>
    /// Stealth is only a mechanic if its edges are exact. Creeping around in play mode to find
    /// out whether a cone is a degree too wide is how a stealth game ends up feeling unfair.
    /// </remarks>
    [TestFixture]
    public sealed class PerceptionMathTests
    {
        private static readonly ZombieSenses Eyes = new(viewDistance: 10f, viewHalfAngleDegrees: 45f);

        private static bool Sees(Vector2 target, float radius = 0f) =>
            PerceptionMath.InViewCone(Vector2.zero, Vector2.right, target, radius, Eyes);

        [Test]
        public void StraightAheadIsSeen() => Assert.That(Sees(new Vector2(5f, 0f)), Is.True);

        [Test]
        public void DirectlyBehindIsNotSeen() => Assert.That(Sees(new Vector2(-5f, 0f)), Is.False);

        [Test]
        public void TooFarAwayIsNotSeen() => Assert.That(Sees(new Vector2(11f, 0f)), Is.False);

        [Test]
        public void JustInsideTheConeIsSeen()
        {
            // 44 degrees off a 45-degree half-angle.
            Vector2 target = Quaternion.Euler(0f, 0f, 44f) * new Vector2(5f, 0f);

            Assert.That(Sees(target), Is.True);
        }

        [Test]
        public void JustOutsideTheConeIsNotSeen()
        {
            Vector2 target = Quaternion.Euler(0f, 0f, 46f) * new Vector2(5f, 0f);

            Assert.That(Sees(target), Is.False);
        }

        [Test]
        public void TheConeIsSymmetrical()
        {
            Vector2 left = Quaternion.Euler(0f, 0f, 30f) * new Vector2(5f, 0f);
            Vector2 right = Quaternion.Euler(0f, 0f, -30f) * new Vector2(5f, 0f);

            Assert.That(Sees(left), Is.EqualTo(Sees(right)));
            Assert.That(Sees(left), Is.True);
        }

        [Test]
        public void AShoulderInTheConeCounts()
        {
            // Centre just outside the wedge, but the body reaches into it.
            Vector2 target = Quaternion.Euler(0f, 0f, 48f) * new Vector2(5f, 0f);

            Assert.That(Sees(target), Is.False, "the centre alone is outside");
            Assert.That(Sees(target, radius: 0.5f), Is.True, "but part of the body is inside");
        }

        [Test]
        public void ABodyReachingIntoRangeCounts()
        {
            Assert.That(Sees(new Vector2(10.4f, 0f)), Is.False);
            Assert.That(Sees(new Vector2(10.4f, 0f), radius: 0.5f), Is.True);
        }

        [Test]
        public void SomethingStandingOnTopOfItIsNoticed() =>
            Assert.That(Sees(Vector2.zero, radius: 0.3f), Is.True);

        [Test]
        public void BlindSensesSeeNothing() =>
            Assert.That(
                PerceptionMath.InViewCone(
                    Vector2.zero, Vector2.right, new Vector2(1f, 0f), 0f, ZombieSenses.Deaf),
                Is.False);

        [Test]
        public void AWatcherLookingNowhereSeesNothing() =>
            Assert.That(
                PerceptionMath.InViewCone(
                    Vector2.zero, Vector2.zero, new Vector2(5f, 0f), 0f, Eyes),
                Is.False);

        [Test]
        public void AFullCircleOfViewSeesBehindIt()
        {
            var allRound = new ZombieSenses(10f, 180f);

            Assert.That(
                PerceptionMath.InViewCone(
                    Vector2.zero, Vector2.right, new Vector2(-5f, 0f), 0f, allRound),
                Is.True);
        }

        [Test]
        public void ANoiseIsLoudestAtItsSource() =>
            Assert.That(
                PerceptionMath.Audibility(Vector2.zero, Vector2.zero, 1f, 6f),
                Is.EqualTo(1f).Within(0.001f));

        [Test]
        public void ANoiseFadesWithDistance()
        {
            float near = PerceptionMath.Audibility(new Vector2(1f, 0f), Vector2.zero, 1f, 6f);
            float far = PerceptionMath.Audibility(new Vector2(4f, 0f), Vector2.zero, 1f, 6f);

            Assert.That(near, Is.GreaterThan(far));
            Assert.That(far, Is.GreaterThan(0f));
        }

        [Test]
        public void OutOfEarshotIsSilent() =>
            Assert.That(
                PerceptionMath.CanHear(new Vector2(6f, 0f), Vector2.zero, 1f, 6f), Is.False);

        [Test]
        public void TwiceAsLoudCarriesTwiceAsFar()
        {
            Assert.That(
                PerceptionMath.CanHear(new Vector2(11f, 0f), Vector2.zero, 1f, 6f),
                Is.False);
            Assert.That(
                PerceptionMath.CanHear(new Vector2(11f, 0f), Vector2.zero, 2f, 6f),
                Is.True,
                "a shot has to reach further than a footstep");
        }

        [Test]
        public void ASilentNoiseIsNotHeard() =>
            Assert.That(PerceptionMath.CanHear(Vector2.zero, Vector2.zero, 0f, 6f), Is.False);

        [Test]
        public void SensesRefuseNonsense()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ZombieSenses(-1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ZombieSenses(5f, 200f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ZombieSenses(5f, 45f, -1f));
        }
    }
}
