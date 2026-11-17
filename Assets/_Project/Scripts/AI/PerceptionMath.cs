#nullable enable
using UnityEngine;

namespace ZombieGame.AI
{
    /// <summary>
    /// Whether something is seen or heard, as pure geometry.
    /// </summary>
    /// <remarks>
    /// No components, no physics and no scene, so what a zombie notices is settled by tests rather
    /// than by creeping around in play mode hoping to spot the edge case. The one thing geometry
    /// cannot answer — whether a wall is in the way — is left to <see cref="ILineOfSight"/>, which
    /// is the only part that needs the physics engine.
    /// </remarks>
    public static class PerceptionMath
    {
        /// <summary>
        /// Whether a target of a given size falls inside a view cone.
        /// </summary>
        /// <remarks>
        /// The body's size counts: a figure whose shoulder is in the cone has been seen, even
        /// though its centre is just outside. Measuring to the centre alone makes a zombie look
        /// past someone standing right beside it.
        /// </remarks>
        /// <param name="eye">Where the watcher is.</param>
        /// <param name="look">Which way it is facing. Need not be normalised.</param>
        /// <param name="target">Centre of the target.</param>
        /// <param name="targetRadius">How wide the target is.</param>
        /// <param name="senses">How far and how wide the watcher sees.</param>
        public static bool InViewCone(
            Vector2 eye, Vector2 look, Vector2 target, float targetRadius, in ZombieSenses senses)
        {
            if (senses.ViewDistance <= 0f || senses.ViewHalfAngleDegrees <= 0f)
            {
                return false;
            }

            Vector2 toTarget = target - eye;
            float distance = toTarget.magnitude;

            // Measured to the near edge of the body, as reach is in the melee arc: a target is in
            // range when any of it is, not only when its middle is.
            if (distance - targetRadius > senses.ViewDistance)
            {
                return false;
            }

            // Standing on top of the watcher. Nothing to point at, and it is certainly noticed.
            if (distance <= targetRadius || distance <= 0.0001f)
            {
                return true;
            }

            if (look.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            float halfAngle = senses.ViewHalfAngleDegrees
                              + (Mathf.Rad2Deg * Mathf.Asin(Mathf.Clamp01(targetRadius / distance)));

            return halfAngle >= 180f
                   || Vector2.Angle(look, toTarget) <= halfAngle;
        }

        /// <summary>
        /// How clearly a noise is heard, from 1 at the source to 0 out of earshot.
        /// </summary>
        /// <remarks>
        /// Loudness stretches the radius rather than adding to it, so "twice as loud" means "heard
        /// twice as far" — the relationship the noise meter in PROJECT.md 9 is built on. Falls off
        /// with distance rather than cutting out, so later systems can tell a nearby shot from a
        /// distant one instead of only knowing that something happened.
        /// </remarks>
        /// <param name="ear">Where the listener is.</param>
        /// <param name="source">Where the noise came from.</param>
        /// <param name="loudness">How loud it was. 1 is an ordinary noise.</param>
        /// <param name="hearingRadius">How far an ordinary noise carries to this listener.</param>
        public static float Audibility(
            Vector2 ear, Vector2 source, float loudness, float hearingRadius)
        {
            if (loudness <= 0f || hearingRadius <= 0f)
            {
                return 0f;
            }

            float reach = hearingRadius * loudness;
            float distance = Vector2.Distance(ear, source);
            if (distance >= reach)
            {
                return 0f;
            }

            return 1f - (distance / reach);
        }

        /// <summary>Whether a noise is loud enough to be noticed at all.</summary>
        public static bool CanHear(
            Vector2 ear, Vector2 source, float loudness, float hearingRadius) =>
            Audibility(ear, source, loudness, hearingRadius) > 0f;
    }
}
