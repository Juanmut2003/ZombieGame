#nullable enable
using System;
using UnityEngine;

namespace ZombieGame.Core.Rendering
{
    /// <summary>How closely the camera chases what it is watching.</summary>
    public readonly struct CameraFollowSettings
    {
        /// <param name="sharpness">
        /// How fast the gap closes, per second. Higher is tighter; 0 pins the camera in place.
        /// </param>
        /// <param name="deadzoneRadius">
        /// How far the target may drift from the centre before the camera bothers to move, in
        /// world units. Keeps small steps and idle fidgeting from nudging the whole view.
        /// </param>
        public CameraFollowSettings(float sharpness, float deadzoneRadius)
        {
            if (sharpness < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sharpness), sharpness, "Cannot be negative.");
            }

            if (deadzoneRadius < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deadzoneRadius), deadzoneRadius, "Cannot be negative.");
            }

            Sharpness = sharpness;
            DeadzoneRadius = deadzoneRadius;
        }

        public float Sharpness { get; }

        public float DeadzoneRadius { get; }
    }

    /// <summary>
    /// Works out where the camera should be next. Pure maths, no components, so the feel of the
    /// follow can be pinned down in tests rather than argued about in the editor.
    /// </summary>
    public static class CameraFollowMath
    {
        /// <summary>
        /// Moves <paramref name="current"/> towards <paramref name="target"/> for one frame.
        /// </summary>
        /// <remarks>
        /// Exponential smoothing, not a fixed fraction per frame: the gap closes by the same
        /// proportion per second whatever the frame rate, so the camera feels identical at 30 and
        /// at 144 fps. A fixed fraction would make the camera lag on a slow machine, which is
        /// exactly when it is most noticeable.
        /// </remarks>
        public static Vector2 Advance(
            Vector2 current, Vector2 target, CameraFollowSettings settings, float deltaSeconds)
        {
            if (deltaSeconds <= 0f || settings.Sharpness <= 0f)
            {
                return current;
            }

            Vector2 toTarget = target - current;
            float distance = toTarget.magnitude;
            if (distance <= settings.DeadzoneRadius)
            {
                return current;
            }

            // Aim at the edge of the deadzone rather than the target itself, so the camera trails
            // by that much instead of snapping the slack shut the moment the target moves.
            Vector2 direction = toTarget / distance;
            Vector2 aim = target - (direction * settings.DeadzoneRadius);

            float blend = 1f - Mathf.Exp(-settings.Sharpness * deltaSeconds);
            return current + ((aim - current) * blend);
        }
    }
}
