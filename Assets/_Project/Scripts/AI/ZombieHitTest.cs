#nullable enable
using UnityEngine;
using ZombieGame.Combat;

namespace ZombieGame.AI
{
    /// <summary>
    /// Lets rounds hit the horde without the combat code knowing the horde exists.
    /// </summary>
    /// <remarks>
    /// The adapter that joins the two halves: <see cref="IProjectileHitTest"/> is asked by
    /// whatever is flying, and the answer — including the damage — comes from the manager that
    /// owns the array. Exactly the shape of the line-of-sight abstraction, and for the same
    /// reason: the side that owns the data is the only side allowed to change it.
    /// </remarks>
    public sealed class ZombieHitTest : IProjectileHitTest
    {
        private readonly ZombieManager _horde;

        public ZombieHitTest(ZombieManager horde) =>
            _horde = horde ?? throw new System.ArgumentNullException(nameof(horde));

        /// <summary>The horde this answers for, so a zone can tell its own registration apart.</summary>
        public ZombieManager Horde => _horde;

        /// <inheritdoc />
        public bool TryHit(
            Vector2 from, Vector2 to, float radius, in DamageInfo damage, out Vector2 point)
        {
            if (!_horde.TryHitSegment(from, to, radius, out int index, out point))
            {
                return false;
            }

            // Damaging can kill, which swaps another zombie into this slot — so the index is used
            // here and never kept.
            _horde.Damage(index, damage);
            return true;
        }
    }
}
