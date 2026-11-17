#nullable enable
using System;

namespace ZombieGame.Combat
{
    /// <summary>
    /// What kind of harm this is. Armour and weak points are expressed as resistances per type,
    /// which is how PROJECT.md 6 wants an Armored zombie to shrug off bullets while a crowbar or
    /// a molotov still works on it.
    /// </summary>
    public enum DamageType
    {
        /// <summary>Bats, crowbars, fists. The answer to armour.</summary>
        Blunt,

        /// <summary>Axes, machetes, knives. Good on the unarmoured, poor against plating.</summary>
        Sharp,

        /// <summary>Firearms. Loud, and wasted on a riot-geared corpse.</summary>
        Ballistic,

        /// <summary>Molotovs and burning wreckage. Slow, spreads, frightening.</summary>
        Fire,

        /// <summary>Bloater gas and spoiled water. Ignores armour, works on the living.</summary>
        Toxic,
    }

    /// <summary>One instance of harm on its way to something that can be hurt.</summary>
    public readonly struct DamageInfo
    {
        /// <param name="amount">Raw damage before resistances. Must not be negative.</param>
        /// <param name="type">Which resistance applies.</param>
        /// <param name="sourceId">
        /// Who or what caused it, as an id the sender chooses — usually an instance id. Zero means
        /// nobody in particular: fall damage, spoiled food, the world itself.
        /// </param>
        public DamageInfo(float amount, DamageType type = DamageType.Blunt, int sourceId = 0)
        {
            if (amount < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount), amount, "Negative damage is healing; say so explicitly.");
            }

            Amount = amount;
            Type = type;
            SourceId = sourceId;
        }

        /// <summary>Raw damage, before any resistance is applied.</summary>
        public float Amount { get; }

        /// <summary>Which kind of harm this is.</summary>
        public DamageType Type { get; }

        /// <summary>Who caused it, or zero for nobody in particular.</summary>
        public int SourceId { get; }
    }
}
