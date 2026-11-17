#nullable enable
using System;
using NUnit.Framework;
using UnityEngine;
using ZombieGame.Characters;

namespace ZombieGame.Tests.EditMode
{
    [TestFixture]
    public sealed class MovementMathTests
    {
        private static MovementSettings Settings(
            float walk = 4f, float sprint = 2f, float acceleration = 40f) =>
            new(walk, sprint, acceleration);

        [Test]
        public void HoldingADirectionAcceleratesTowardsIt()
        {
            Vector2 next = MovementMath.Advance(
                Vector2.zero, Vector2.right, sprinting: false, Settings(), 0.02f);

            Assert.That(next.x, Is.GreaterThan(0f));
            Assert.That(next.x, Is.LessThan(4f), "it accelerates, it does not jump to top speed");
        }

        [Test]
        public void ItSettlesAtWalkingSpeed()
        {
            Vector2 velocity = Vector2.zero;
            for (int step = 0; step < 100; step++)
            {
                velocity = MovementMath.Advance(
                    velocity, Vector2.right, sprinting: false, Settings(), 0.02f);
            }

            Assert.That(velocity.x, Is.EqualTo(4f).Within(0.0001f));
        }

        [Test]
        public void SprintingSettlesAtTheMultipliedSpeed()
        {
            Vector2 velocity = Vector2.zero;
            for (int step = 0; step < 100; step++)
            {
                velocity = MovementMath.Advance(
                    velocity, Vector2.right, sprinting: true, Settings(), 0.02f);
            }

            Assert.That(velocity.x, Is.EqualTo(8f).Within(0.0001f));
        }

        [Test]
        public void ReleasingTheInputBringsTheCharacterToAStop()
        {
            var velocity = new Vector2(4f, 0f);

            for (int step = 0; step < 100; step++)
            {
                velocity = MovementMath.Advance(
                    velocity, Vector2.zero, sprinting: false, Settings(), 0.02f);
            }

            Assert.That(velocity.magnitude, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void StoppingIsNotInstant()
        {
            var velocity = new Vector2(4f, 0f);

            Vector2 next = MovementMath.Advance(
                velocity, Vector2.zero, sprinting: false, Settings(), 0.02f);

            Assert.That(next.x, Is.GreaterThan(0f), "there is weight to it");
            Assert.That(next.x, Is.LessThan(4f));
        }

        [Test]
        public void DiagonalMovementIsNotFasterThanStraight()
        {
            Vector2 straight = Vector2.zero;
            Vector2 diagonal = Vector2.zero;
            var diagonalInput = new Vector2(1f, 1f);

            for (int step = 0; step < 100; step++)
            {
                straight = MovementMath.Advance(
                    straight, Vector2.right, sprinting: false, Settings(), 0.02f);
                diagonal = MovementMath.Advance(
                    diagonal, diagonalInput, sprinting: false, Settings(), 0.02f);
            }

            Assert.That(
                diagonal.magnitude,
                Is.EqualTo(straight.magnitude).Within(0.0001f),
                "the classic eight-way bug: holding two keys must not be faster");
        }

        [Test]
        public void AHalfPressedStickMovesAtHalfSpeed()
        {
            Vector2 velocity = Vector2.zero;
            var halfInput = new Vector2(0.5f, 0f);

            for (int step = 0; step < 100; step++)
            {
                velocity = MovementMath.Advance(
                    velocity, halfInput, sprinting: false, Settings(), 0.02f);
            }

            Assert.That(velocity.x, Is.EqualTo(2f).Within(0.0001f), "analogue input stays analogue");
        }

        [Test]
        public void ClampInput_LeavesShortVectorsAlone()
        {
            var half = new Vector2(0.5f, 0f);

            Assert.That(MovementMath.ClampInput(half), Is.EqualTo(half));
            Assert.That(MovementMath.ClampInput(Vector2.zero), Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void ClampInput_ShortensLongVectorsToOne()
        {
            Vector2 clamped = MovementMath.ClampInput(new Vector2(3f, 4f));

            Assert.That(clamped.magnitude, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(clamped.x / clamped.y, Is.EqualTo(0.75f).Within(0.0001f), "direction is kept");
        }

        [Test]
        public void TurningAroundGoesThroughZero()
        {
            var velocity = new Vector2(4f, 0f);
            bool wasSlow = false;

            for (int step = 0; step < 100; step++)
            {
                velocity = MovementMath.Advance(
                    velocity, Vector2.left, sprinting: false, Settings(), 0.02f);
                if (Mathf.Abs(velocity.x) < 0.5f)
                {
                    wasSlow = true;
                }
            }

            Assert.That(wasSlow, Is.True, "a character has to slow down before running the other way");
            Assert.That(velocity.x, Is.EqualTo(-4f).Within(0.0001f));
        }

        [Test]
        public void AStepOfNoTimeChangesNothing()
        {
            var velocity = new Vector2(1f, 2f);

            Assert.That(
                MovementMath.Advance(velocity, Vector2.right, false, Settings(), 0f),
                Is.EqualTo(velocity));
        }

        [Test]
        public void HigherAccelerationReachesSpeedSooner()
        {
            Vector2 sluggish = MovementMath.Advance(
                Vector2.zero, Vector2.right, false, Settings(acceleration: 10f), 0.02f);
            Vector2 snappy = MovementMath.Advance(
                Vector2.zero, Vector2.right, false, Settings(acceleration: 80f), 0.02f);

            Assert.That(snappy.x, Is.GreaterThan(sluggish.x));
        }

        [Test]
        public void SettingsRejectNonsense()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new MovementSettings(0f, 1f, 10f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MovementSettings(4f, 0.5f, 10f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MovementSettings(4f, 1f, 0f));
        }

        [Test]
        public void TopSpeed_ReflectsSprinting()
        {
            MovementSettings settings = Settings(walk: 3f, sprint: 2f);

            Assert.That(settings.TopSpeed(sprinting: false), Is.EqualTo(3f));
            Assert.That(settings.TopSpeed(sprinting: true), Is.EqualTo(6f));
        }
    }
}
