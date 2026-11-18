#nullable enable
using System;
using ZombieGame.Combat;

namespace ZombieGame.AI
{
    /// <summary>
    /// What one kind of zombie does when it finally reaches you: how hard, how far, how often.
    /// </summary>
    /// <remarks>
    /// Three numbers rather than an attack animation, because what makes a crowd dangerous is
    /// arithmetic: a Walker that bites for eight every second and a half is survivable alone and
    /// lethal in fours, and that is a thing a designer tunes rather than a thing code decides.
    /// The reach is measured between body edges, not centres, so a wide zombie does not also get
    /// a longer arm for free.
    /// </remarks>
    public readonly struct ZombieBite
    {
        /// <param name="damage">What one bite takes off.</param>
        /// <param name="reach">How far past its own body it can reach, in world units.</param>
        /// <param name="seconds">How long it needs between bites.</param>
        /// <param name="type">What kind of harm it does.</param>
        public ZombieBite(
            float damage = 8f,
            float reach = 0.35f,
            float seconds = 1.1f,
            DamageType type = DamageType.Sharp)
        {
            if (damage < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(damage), damage, "A bite cannot heal.");
            }

            if (reach < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(reach), reach, "Cannot be negative.");
            }

            if (seconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(seconds), seconds, "Cannot be negative.");
            }

            Damage = damage;
            Reach = reach;
            Seconds = seconds;
            Type = type;
        }

        /// <summary>What one bite takes off.</summary>
        public float Damage { get; }

        /// <summary>How far past its own body it can reach.</summary>
        public float Reach { get; }

        /// <summary>How long between bites.</summary>
        public float Seconds { get; }

        /// <summary>What kind of harm it does.</summary>
        public DamageType Type { get; }

        /// <summary>Bites nobody. For tests that want the horde harmless.</summary>
        public static ZombieBite Toothless => new(0f, 0f, 1f);
    }
}
