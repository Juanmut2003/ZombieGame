#nullable enable
using ZombieGame.Core.Events;

namespace ZombieGame.Combat
{
    /// <summary>
    /// Says that someone's health moved, so a bar on the HUD can show it.
    /// </summary>
    /// <remarks>
    /// The same shape as <see cref="StaminaChangedEvent"/> and for the same reason: gameplay may
    /// never know a UI type exists, so what both sides share is a struct on the bus.
    /// Separate from <see cref="DamageTakenEvent"/>, which says a blow landed. A bar wants the
    /// number, not the blow — and it also has to be told about healing, which no blow reports.
    /// </remarks>
    public readonly struct HealthChangedEvent : IEvent
    {
        public HealthChangedEvent(int ownerId, float current, float maximum, bool isPlayer)
        {
            OwnerId = ownerId;
            Current = current;
            Maximum = maximum;
            IsPlayer = isPlayer;
        }

        /// <summary>Whose health this is.</summary>
        public int OwnerId { get; }

        /// <summary>Health left.</summary>
        public float Current { get; }

        /// <summary>Full health.</summary>
        public float Maximum { get; }

        /// <summary>True for the character the player is steering.</summary>
        public bool IsPlayer { get; }

        /// <summary>How full it is, clamped to 0..1 and safe when the maximum is nonsense.</summary>
        public float Fraction => Maximum <= 0f
            ? 0f
            : Current <= 0f ? 0f : Current >= Maximum ? 1f : Current / Maximum;
    }
}
