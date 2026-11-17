#nullable enable
using System;
using NUnit.Framework;
using UnityEngine;
using ZombieGame.Core.Rendering;

namespace ZombieGame.Tests.EditMode
{
    [TestFixture]
    public sealed class CameraFollowMathTests
    {
        private static CameraFollowSettings Settings(float sharpness = 8f, float deadzone = 0f) =>
            new(sharpness, deadzone);

        [Test]
        public void TheCameraMovesTowardsItsTarget()
        {
            Vector2 next = CameraFollowMath.Advance(
                Vector2.zero, new Vector2(10f, 0f), Settings(), 0.016f);

            Assert.That(next.x, Is.GreaterThan(0f));
            Assert.That(next.x, Is.LessThan(10f), "it eases in, it does not teleport");
            Assert.That(next.y, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void ItKeepsClosingInOverManyFrames()
        {
            Vector2 current = Vector2.zero;
            var target = new Vector2(10f, 5f);

            for (int frame = 0; frame < 300; frame++)
            {
                current = CameraFollowMath.Advance(current, target, Settings(), 0.016f);
            }

            Assert.That(Vector2.Distance(current, target), Is.LessThan(0.01f));
        }

        [Test]
        public void ItNeverOvershoots()
        {
            Vector2 current = Vector2.zero;
            var target = new Vector2(1f, 0f);

            for (int frame = 0; frame < 100; frame++)
            {
                current = CameraFollowMath.Advance(current, target, Settings(), 0.1f);
                Assert.That(current.x, Is.LessThanOrEqualTo(target.x + 0.0001f));
            }
        }

        [Test]
        public void TheFeelDoesNotDependOnTheFrameRate()
        {
            var target = new Vector2(10f, 0f);
            CameraFollowSettings settings = Settings();

            Vector2 oneBigStep = CameraFollowMath.Advance(Vector2.zero, target, settings, 0.1f);

            Vector2 manySmallSteps = Vector2.zero;
            for (int i = 0; i < 10; i++)
            {
                manySmallSteps = CameraFollowMath.Advance(manySmallSteps, target, settings, 0.01f);
            }

            Assert.That(
                manySmallSteps.x,
                Is.EqualTo(oneBigStep.x).Within(0.0001f),
                "ten frames at 100 fps must land where one frame at 10 fps does");
        }

        [Test]
        public void ATargetInsideTheDeadzoneIsIgnored()
        {
            var settings = Settings(deadzone: 1f);

            Vector2 next = CameraFollowMath.Advance(
                Vector2.zero, new Vector2(0.9f, 0f), settings, 0.016f);

            Assert.That(next, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void TheCameraTrailsByTheDeadzoneRatherThanCentring()
        {
            var settings = Settings(sharpness: 50f, deadzone: 2f);
            Vector2 current = Vector2.zero;
            var target = new Vector2(10f, 0f);

            for (int frame = 0; frame < 300; frame++)
            {
                current = CameraFollowMath.Advance(current, target, settings, 0.016f);
            }

            Assert.That(
                current.x,
                Is.EqualTo(8f).Within(0.01f),
                "it settles at the edge of the deadzone, not on the target");
        }

        [Test]
        public void ASharpnessOfZeroPinsTheCamera()
        {
            Vector2 next = CameraFollowMath.Advance(
                Vector2.zero, new Vector2(10f, 10f), Settings(sharpness: 0f), 0.5f);

            Assert.That(next, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void AFrameOfNoTimeChangesNothing()
        {
            var current = new Vector2(3f, 4f);

            Assert.That(
                CameraFollowMath.Advance(current, new Vector2(9f, 9f), Settings(), 0f),
                Is.EqualTo(current));
        }

        [Test]
        public void HigherSharpnessClosesTheGapFaster()
        {
            var target = new Vector2(10f, 0f);

            Vector2 slow = CameraFollowMath.Advance(Vector2.zero, target, Settings(2f), 0.1f);
            Vector2 fast = CameraFollowMath.Advance(Vector2.zero, target, Settings(20f), 0.1f);

            Assert.That(fast.x, Is.GreaterThan(slow.x));
        }

        [Test]
        public void SettingsRejectNegativeValues()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CameraFollowSettings(-1f, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CameraFollowSettings(1f, -0.5f));
        }

        [Test]
        public void TheReferenceResolutionMatchesTheArtDirection()
        {
            Assert.That(PixelPerfectSettings.ReferenceWidth, Is.EqualTo(960));
            Assert.That(PixelPerfectSettings.ReferenceHeight, Is.EqualTo(540));
            Assert.That(PixelPerfectSettings.PixelsPerUnit, Is.EqualTo(64));
        }

        [Test]
        public void TheOrthographicSizeShowsExactlyTheReferenceHeight()
        {
            // 540 pixels tall at 64 pixels per unit is 8.4375 units, and Unity wants half of that.
            // Unchanged by the move from 32 to 64: the camera still shows the same slice of world.
            Assert.That(PixelPerfectSettings.OrthographicSize, Is.EqualTo(4.21875f).Within(0.00001f));
            Assert.That(PixelPerfectSettings.OrthographicSizeFor(540, 32), Is.EqualTo(8.4375f));
        }

        [Test]
        public void TheReferenceHeightUpscalesCleanlyToFullHd()
        {
            Assert.That(
                1080 % PixelPerfectSettings.ReferenceHeight,
                Is.Zero,
                "1080p must be a whole-number upscale, or the pixel grid breaks");
        }
    }
}
