#nullable enable
using ZombieGame.Core.Events;

namespace ZombieGame.Combat
{
    /// <summary>
    /// Says that someone's stamina moved, so a bar on the HUD can show it.
    /// </summary>
    /// <remarks>
    /// This travels on the bus rather than the HUD holding a reference to a character, because
    /// CLAUDE.md puts UI and gameplay on opposite sides of a wall: gameplay may never know a UI
    /// type exists, and the UI may not reach into gameplay for its numbers. A struct on the bus is
    /// what both sides can share.
    /// Carrying the owner means the same event serves a survivor's bar, a companion portrait or a
    /// floating bar over a wounded raider later, without a second event type.
    /// </remarks>
    public readonly struct StaminaChangedEvent : IEvent
    {
        public StaminaChangedEvent(int ownerId, float current, float maximum, bool isPlayer)
        {
            OwnerId = ownerId;
            Current = current;
            Maximum = maximum;
            IsPlayer = isPlayer;
        }

        /// <summary>Whose stamina this is.</summary>
        public int OwnerId { get; }

        /// <summary>Stamina left.</summary>
        public float Current { get; }

        /// <summary>Full stamina.</summary>
        public float Maximum { get; }

        /// <summary>
        /// True for the character the player is steering, so the main HUD can ignore everyone else.
        /// </summary>
        public bool IsPlayer { get; }

        /// <summary>How full it is, clamped to 0..1 and safe when the maximum is nonsense.</summary>
        public float Fraction => Maximum <= 0f
            ? 0f
            : Current <= 0f ? 0f : Current >= Maximum ? 1f : Current / Maximum;
    }
}
