#nullable enable
using ZombieGame.Core.Events;

namespace ZombieGame.Combat
{
    /// <summary>Which weapon is in hand, and what else is on the belt.</summary>
    /// <remarks>
    /// Carries names rather than the weapon assets themselves. The HUD has no business holding a
    /// reference to a gameplay object, and the hotbar only ever needs to draw what the slots are
    /// called and which one is lit.
    /// </remarks>
    public readonly struct LoadoutChangedEvent : IEvent
    {
        public LoadoutChangedEvent(string[] slotNames, int selected, bool isPlayer)
        {
            SlotNames = slotNames;
            Selected = selected;
            IsPlayer = isPlayer;
        }

        /// <summary>What each slot is called, in order.</summary>
        public string[] SlotNames { get; }

        /// <summary>Which slot is in hand, counting from zero.</summary>
        public int Selected { get; }

        /// <summary>True when this is the character the player steers.</summary>
        public bool IsPlayer { get; }
    }
}
