#nullable enable
using ZombieGame.Core.Events;

namespace ZombieGame.Combat
{
    /// <summary>Raised whenever something took damage and survived it.</summary>
    /// <remarks>
    /// Hit reactions, damage numbers, blood and the "someone is shooting" reaction all hang off
    /// this rather than off the thing that dealt the blow.
    /// </remarks>
    public readonly struct DamageTakenEvent : IEvent
    {
        public DamageTakenEvent(int targetId, float applied, float remaining, DamageInfo damage)
        {
            TargetId = targetId;
            Applied = applied;
            Remaining = remaining;
            Damage = damage;
        }

        /// <summary>Who was hit.</summary>
        public int TargetId { get; }

        /// <summary>How much health it actually cost, after resistances.</summary>
        public float Applied { get; }

        /// <summary>How much health is left.</summary>
        public float Remaining { get; }

        /// <summary>The blow as it was dealt, before resistances.</summary>
        public DamageInfo Damage { get; }
    }

    /// <summary>Raised once, when something runs out of health.</summary>
    /// <remarks>
    /// Exactly once per death: loot drops, grief debuffs, score and the zombie manager returning
    /// a body to the pool all listen here, and none of them survives being told twice.
    /// </remarks>
    public readonly struct DiedEvent : IEvent
    {
        public DiedEvent(int targetId, DamageInfo killingBlow)
        {
            TargetId = targetId;
            KillingBlow = killingBlow;
        }

        /// <summary>What died.</summary>
        public int TargetId { get; }

        /// <summary>The blow that finished it.</summary>
        public DamageInfo KillingBlow { get; }
    }
}
