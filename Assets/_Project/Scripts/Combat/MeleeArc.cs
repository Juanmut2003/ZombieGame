#nullable enable
using UnityEngine;

namespace ZombieGame.Combat
{
    /// <summary>
    /// Works out who a swing reaches: everything inside a wedge in front of the attacker.
    /// </summary>
    /// <remarks>
    /// A wedge rather than a circle, because a bat swung forwards should not hit the thing behind
    /// you — and rather than a single ray, because then a crowd pressing in from the side would be
    /// unhittable. Pure geometry, so the reach of a weapon is pinned down by tests instead of
    /// discovered in play.
    /// </remarks>
    public static class MeleeArc
    {
        /// <summary>
        /// True when <paramref name="target"/> is within reach of a swing.
        /// </summary>
        /// <param name="origin">Where the attacker stands.</param>
        /// <param name="facing">Which way the swing goes. Need not be normalised.</param>
        /// <param name="target">The middle of the thing being swung at.</param>
        /// <param name="targetRadius">
        /// How wide that thing is. A body is hit when any part of it is inside the wedge, not only
        /// when its exact centre is.
        /// </param>
        /// <param name="range">How far the weapon reaches, measured to the target's near edge.</param>
        /// <param name="halfAngleDegrees">
        /// Half the width of the wedge. 180 is a full circle around the attacker.
        /// </param>
        public static bool Covers(
            Vector2 origin,
            Vector2 facing,
            Vector2 target,
            float targetRadius,
            float range,
            float halfAngleDegrees)
        {
            if (range <= 0f || halfAngleDegrees <= 0f)
            {
                return false;
            }

            Vector2 toTarget = target - origin;
            float distance = toTarget.magnitude;

            if (distance - targetRadius > range)
            {
                return false;
            }

            // Standing inside the attacker: there is no angle to speak of, and a swing that
            // misses what is already on top of you would be absurd.
            if (distance <= targetRadius || distance <= 0.0001f)
            {
                return true;
            }

            if (halfAngleDegrees >= 180f)
            {
                return true;
            }

            Vector2 forward = facing.sqrMagnitude > 0.000001f ? facing.normalized : Vector2.up;
            float angle = Vector2.Angle(forward, toTarget / distance);

            // A wide body sticking into the wedge counts, so the effective angle is widened by how
            // much of it subtends at this distance.
            float slack = Mathf.Rad2Deg * Mathf.Asin(Mathf.Clamp01(targetRadius / distance));
            return angle <= halfAngleDegrees + slack;
        }
    }
}
