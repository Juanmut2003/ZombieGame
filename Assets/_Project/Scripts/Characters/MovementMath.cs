#nullable enable
using System;
using UnityEngine;

namespace ZombieGame.Characters
{
    /// <summary>How a character accelerates and how fast it may end up going.</summary>
    public readonly struct MovementSettings
    {
        /// <param name="walkSpeed">Top speed while walking, in units per second.</param>
        /// <param name="sprintMultiplier">What sprinting multiplies that by.</param>
        /// <param name="acceleration">How fast the gap to the wanted velocity closes.</param>
        public MovementSettings(float walkSpeed, float sprintMultiplier, float acceleration)
        {
            if (walkSpeed <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(walkSpeed), walkSpeed, "A character that cannot walk is not a character.");
            }

            if (sprintMultiplier < 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sprintMultiplier), sprintMultiplier, "Sprinting is never slower than walking.");
            }

            if (acceleration <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(acceleration), acceleration, "Must be positive.");
            }

            WalkSpeed = walkSpeed;
            SprintMultiplier = sprintMultiplier;
            Acceleration = acceleration;
        }

        public float WalkSpeed { get; }

        public float SprintMultiplier { get; }

        public float Acceleration { get; }

        /// <summary>Top speed for the current state.</summary>
        public float TopSpeed(bool sprinting) => sprinting ? WalkSpeed * SprintMultiplier : WalkSpeed;
    }

    /// <summary>
    /// Turns an input direction into the velocity a character should have next.
    /// </summary>
    /// <remarks>
    /// Pure maths, no components and no physics engine, so how movement feels is settled by tests
    /// instead of by nudging numbers in the inspector until it seems fine.
    /// </remarks>
    public static class MovementMath
    {
        /// <summary>
        /// Works out the next velocity from the current one and what the player is asking for.
        /// </summary>
        /// <param name="currentVelocity">Where the character is going now.</param>
        /// <param name="input">
        /// Desired direction. Longer than one unit is clamped, so a diagonal on the keyboard is
        /// not faster than a straight line — the classic bug of eight-way movement.
        /// </param>
        /// <param name="sprinting">Whether the sprint input is held.</param>
        /// <param name="settings">Speeds and acceleration.</param>
        /// <param name="deltaSeconds">The fixed step this is being advanced by.</param>
        public static Vector2 Advance(
            Vector2 currentVelocity,
            Vector2 input,
            bool sprinting,
            MovementSettings settings,
            float deltaSeconds)
        {
            if (deltaSeconds <= 0f)
            {
                return currentVelocity;
            }

            Vector2 wanted = ClampInput(input) * settings.TopSpeed(sprinting);

            // Move towards the wanted velocity at a fixed rate rather than snapping to it: this is
            // what gives weight to starting, stopping and changing direction. Releasing the stick
            // decelerates through the same path, so stopping is not instant either.
            return Vector2.MoveTowards(currentVelocity, wanted, settings.Acceleration * deltaSeconds);
        }

        /// <summary>
        /// Normalises an input vector that is longer than one unit, leaving shorter ones alone so
        /// a gamepad stick keeps its analogue range.
        /// </summary>
        public static Vector2 ClampInput(Vector2 input)
        {
            float magnitude = input.magnitude;
            if (magnitude <= 1f || magnitude <= Mathf.Epsilon)
            {
                return input;
            }

            return input / magnitude;
        }
    }
}
