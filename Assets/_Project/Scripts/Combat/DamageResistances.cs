#nullable enable
using System;

namespace ZombieGame.Combat
{
    /// <summary>
    /// How much of each damage type something actually feels, as a multiplier per type.
    /// </summary>
    /// <remarks>
    /// One multiplied, not a chain of special cases: 1 takes it in full, 0.25 is heavy armour,
    /// 0 is immunity and 2 is a weak point. Defining armour this way means a new zombie or a new
    /// piece of gear is a row of numbers in an asset, never a branch in the damage code.
    /// </remarks>
    public readonly struct DamageResistances
    {
        private const int TypeCount = 5;

        private readonly float[]? _multipliers;

        /// <param name="multipliers">
        /// One multiplier per <see cref="DamageType"/>, in enum order. Each must be zero or more.
        /// </param>
        public DamageResistances(params float[] multipliers)
        {
            if (multipliers is null)
            {
                throw new ArgumentNullException(nameof(multipliers));
            }

            if (multipliers.Length != TypeCount)
            {
                throw new ArgumentException(
                    $"Expected {TypeCount} multipliers, one per damage type, got {multipliers.Length}.",
                    nameof(multipliers));
            }

            for (int i = 0; i < multipliers.Length; i++)
            {
                if (multipliers[i] < 0f)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(multipliers),
                        multipliers[i],
                        $"The multiplier for {(DamageType)i} is negative, which would heal on hit.");
                }
            }

            _multipliers = (float[])multipliers.Clone();
        }

        /// <summary>Takes every damage type at face value.</summary>
        public static DamageResistances None => default;

        /// <summary>The multiplier for one damage type.</summary>
        public float Multiplier(DamageType type)
        {
            // A default-constructed value has no array, and "unarmoured" is the right reading of
            // that rather than a crash — most things in the world have no resistances at all.
            if (_multipliers is null)
            {
                return 1f;
            }

            int index = (int)type;
            return index >= 0 && index < _multipliers.Length ? _multipliers[index] : 1f;
        }

        /// <summary>What <paramref name="damage"/> actually does after resistances.</summary>
        public float Apply(DamageInfo damage) => damage.Amount * Multiplier(damage.Type);
    }
}
