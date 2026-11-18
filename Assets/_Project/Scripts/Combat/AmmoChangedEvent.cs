#nullable enable
using ZombieGame.Core.Events;

namespace ZombieGame.Combat
{
    /// <summary>The ammunition count moved, or a reload started or finished.</summary>
    /// <remarks>
    /// The HUD reads this and nothing else. Ammunition is the one number a player has to see
    /// before they need it — finding out the gun is empty by pulling the trigger is finding out
    /// too late.
    /// </remarks>
    public readonly struct AmmoChangedEvent : IEvent
    {
        public AmmoChangedEvent(int loaded, int reserve, bool reloading, bool isPlayer)
        {
            Loaded = loaded;
            Reserve = reserve;
            IsReloading = reloading;
            IsPlayer = isPlayer;
        }

        /// <summary>Rounds in the gun.</summary>
        public int Loaded { get; }

        /// <summary>Rounds in the pockets.</summary>
        public int Reserve { get; }

        /// <summary>True while a magazine is going in.</summary>
        public bool IsReloading { get; }

        /// <summary>True when this is the character the player steers.</summary>
        public bool IsPlayer { get; }
    }
}
