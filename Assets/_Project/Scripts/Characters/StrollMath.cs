#nullable enable
using UnityEngine;

namespace ZombieGame.Characters
{
    /// <summary>
    /// Where somebody with nothing to do walks next, and which way that is from here.
    /// </summary>
    /// <remarks>
    /// Pure maths, no components: whether a colonist stays on their patch of ground is decided
    /// by tests rather than by watching them for a minute and hoping. The randomness is handed
    /// in as two fractions, so the same pair always picks the same spot.
    /// </remarks>
    public static class StrollMath
    {
        /// <summary>
        /// A spot inside an area, picked by two fractions of its width and height.
        /// </summary>
        /// <param name="area">The ground they are allowed on, in world units.</param>
        /// <param name="across">Where between the left and right edge, 0 to 1.</param>
        /// <param name="up">Where between the bottom and top edge, 0 to 1.</param>
        public static Vector2 SpotIn(Rect area, float across, float up) =>
            new Vector2(
                area.xMin + (Mathf.Clamp01(across) * Mathf.Max(0f, area.width)),
                area.yMin + (Mathf.Clamp01(up) * Mathf.Max(0f, area.height)));

        /// <summary>Whether they are close enough to the spot to count as having got there.</summary>
        public static bool Arrived(Vector2 from, Vector2 spot, float arriveDistance) =>
            (spot - from).sqrMagnitude <= arriveDistance * arriveDistance;

        /// <summary>
        /// The move vector that walks from here towards the spot, or zero once they have
        /// arrived.
        /// </summary>
        /// <param name="speedFactor">
        /// How much of a walk this is. Below one it is a stroll, which is the whole point: a
        /// survivor crossing the plot at the player's running speed reads as a bug.
        /// </param>
        public static Vector2 Toward(
            Vector2 from, Vector2 spot, float arriveDistance, float speedFactor)
        {
            if (Arrived(from, spot, arriveDistance))
            {
                return Vector2.zero;
            }

            return (spot - from).normalized * Mathf.Clamp01(speedFactor);
        }
    }
}
