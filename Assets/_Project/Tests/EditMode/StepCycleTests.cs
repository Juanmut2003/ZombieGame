#nullable enable
using NUnit.Framework;
using UnityEngine;
using ZombieGame.Core.Rendering;

namespace ZombieGame.Tests.EditMode
{
    /// <summary>
    /// The stride: where the footfalls land, and what the body does between them.
    /// </summary>
    /// <remarks>
    /// Worth pinning down because none of it can be checked by eye. A gait that is half a beat
    /// out from the footstep sound still looks like walking, and a lift that never returns to
    /// zero just reads as a figure hovering — both are invisible until somebody wonders why the
    /// game feels slightly wrong.
    /// </remarks>
    [TestFixture]
    public sealed class StepCycleTests
    {
        [Test]
        public void AWholeCycleComesBackToTheStart()
        {
            Assert.That(StepCycle.Wrap(0f), Is.EqualTo(0f));
            Assert.That(StepCycle.Wrap(1f), Is.EqualTo(0f));
            Assert.That(StepCycle.Wrap(3.25f), Is.EqualTo(0.25f).Within(0.0001f));

            // Walking backwards is still walking.
            Assert.That(StepCycle.Wrap(-0.25f), Is.EqualTo(0.75f).Within(0.0001f));
        }

        [Test]
        public void TwoFootfallsMakeOneCycle()
        {
            // A stride is one footfall, so covering exactly one stride should land on the second
            // footfall of the cycle rather than finishing it.
            Assert.That(StepCycle.FromDistance(1.1f, 1.1f), Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(StepCycle.FromDistance(2.2f, 1.1f), Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void TheBodyIsLowestWhenAFootLands()
        {
            const float height = 0.03f;

            Assert.That(StepCycle.Bob(0f, height), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(StepCycle.Bob(0.5f, height), Is.EqualTo(0f).Within(0.0001f));

            // And highest halfway between, riding over the straight leg.
            Assert.That(StepCycle.Bob(0.25f, height), Is.EqualTo(height).Within(0.0001f));
            Assert.That(StepCycle.Bob(0.75f, height), Is.EqualTo(height).Within(0.0001f));
        }

        [Test]
        public void TheBodyNeverSinksIntoTheGround()
        {
            for (int i = 0; i <= 64; i++)
            {
                float phase = i / 64f;
                Assert.That(
                    StepCycle.Bob(phase, 0.03f),
                    Is.GreaterThanOrEqualTo(0f),
                    $"phase {phase} put the figure below its own feet");
            }
        }

        [Test]
        public void TheLeanAlternatesBetweenTheFeet()
        {
            const float degrees = 2.5f;

            float first = StepCycle.Lean(0.25f, degrees);
            float second = StepCycle.Lean(0.75f, degrees);

            Assert.That(first, Is.GreaterThan(0f));
            Assert.That(second, Is.LessThan(0f));
            Assert.That(first, Is.EqualTo(-second).Within(0.0001f), "the rock is lopsided");

            // Upright at the moment of the footfall itself, like the lift.
            Assert.That(StepCycle.Lean(0f, degrees), Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void TheGaitKeepsTimeWithTheFootstepSound()
        {
            // The squad's steps are on a timer the noise system owns; the gait reads the same
            // number. Every footfall of that timer has to be a moment the body is down, or the
            // boot lands while the figure is at the top of its bounce.
            const float stepSeconds = 0.55f;

            for (int step = 0; step < 8; step++)
            {
                float at = step * stepSeconds;
                Assert.That(
                    StepCycle.Bob(StepCycle.FromTime(at, stepSeconds), 0.03f),
                    Is.EqualTo(0f).Within(0.0005f),
                    $"step {step} was heard mid-bounce");
            }
        }

        [Test]
        public void TheFramesAreSpreadOverTheWholeStride()
        {
            // Six drawings, so each one covers a sixth of the cycle and the last one gives way
            // to the first exactly as the stride comes round.
            Assert.That(StepCycle.Frame(0f, 6), Is.Zero);
            Assert.That(StepCycle.Frame(0.17f, 6), Is.EqualTo(1));
            Assert.That(StepCycle.Frame(0.99f, 6), Is.EqualTo(5));
            Assert.That(StepCycle.Frame(1f, 6), Is.Zero);
        }

        [Test]
        public void ArtWithOnePoseIsStillDrawn()
        {
            // Most of the cast has exactly one drawing per direction. Asking for a frame of a
            // one-frame walk has to answer zero rather than run off the end of the array.
            for (int i = 0; i <= 8; i++)
            {
                Assert.That(StepCycle.Frame(i / 8f, 1), Is.Zero);
            }

            Assert.That(StepCycle.Frame(0.5f, 0), Is.Zero);
        }

        [Test]
        public void AFrameIsNeverPastTheEndOfTheWalk()
        {
            foreach (int count in new[] { 1, 2, 3, 6, 12 })
            {
                for (int i = 0; i <= 256; i++)
                {
                    int frame = StepCycle.Frame(i / 256f, count);
                    Assert.That(frame, Is.InRange(0, count - 1), $"{count} frames at {i}/256");
                }
            }
        }

        [Test]
        public void StandingStillIsNotAnError()
        {
            // A zone can be built before its balance asset is, and a stride of zero would
            // otherwise divide by it.
            Assert.That(StepCycle.FromDistance(5f, 0f), Is.EqualTo(0f));
            Assert.That(StepCycle.FromTime(5f, 0f), Is.EqualTo(0f));
            Assert.That(StepCycle.Bob(0.25f, 0f), Is.EqualTo(0f));
            Assert.That(StepCycle.Lean(0.25f, 0f), Is.EqualTo(0f));
        }

        [Test]
        public void SlowerWalkingMeansSlowerBobbing()
        {
            // The horde's gait comes off the ground it covers, so a bloater at half the speed of
            // a walker is half as far through its stride after the same second.
            float fast = StepCycle.FromDistance(1.0f, 1.1f);
            float slow = StepCycle.FromDistance(0.5f, 1.1f);

            Assert.That(slow, Is.LessThan(fast));
            Assert.That(slow * 2f, Is.EqualTo(fast).Within(0.0001f));
        }
    }
}
