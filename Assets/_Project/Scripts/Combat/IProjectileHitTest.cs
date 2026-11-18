#nullable enable
using UnityEngine;

namespace ZombieGame.Combat
{
    /// <summary>
    /// Asks whoever owns the targets whether a round passing through hit one of them.
    /// </summary>
    /// <remarks>
    /// Combat cannot see the horde, and should not: the zombie manager owns its array and is the
    /// only thing allowed to touch it. So the projectile says where it went this step and the
    /// answer — including the damage — comes from the side that owns the bodies. Survivors,
    /// raiders and later vehicles implement the same interface without this file changing.
    /// It is an interface rather than an event because a round needs the answer <b>now</b>, in the
    /// middle of its step, to stop at what it hit instead of flying on through it.
    /// </remarks>
    public interface IProjectileHitTest
    {
        /// <summary>
        /// Looks for the first target on the segment a round just travelled, and hurts it.
        /// </summary>
        /// <param name="from">Where the round was.</param>
        /// <param name="to">Where it would be.</param>
        /// <param name="radius">How fat the round is.</param>
        /// <param name="damage">What it does on contact.</param>
        /// <param name="point">Where it struck, when it did.</param>
        /// <returns>True when something was hit, which also ends the round.</returns>
        bool TryHit(Vector2 from, Vector2 to, float radius, in DamageInfo damage, out Vector2 point);
    }

    /// <summary>Nothing to hit. The default, and what tests use when they only want travel.</summary>
    public sealed class EmptyField : IProjectileHitTest
    {
        /// <summary>The one instance; it holds no state.</summary>
        public static readonly EmptyField Instance = new();

        private EmptyField()
        {
        }

        /// <inheritdoc />
        public bool TryHit(
            Vector2 from, Vector2 to, float radius, in DamageInfo damage, out Vector2 point)
        {
            point = to;
            return false;
        }
    }
}
