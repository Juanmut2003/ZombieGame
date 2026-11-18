#nullable enable
using UnityEngine;
using ZombieGame.Core.Events;

namespace ZombieGame.Missions
{
    /// <summary>Something was taken out of a container.</summary>
    /// <remarks>
    /// Announced rather than handed over, so the run's bookkeeping, the HUD and later the noise of
    /// rummaging through a shelf all hear the same thing without the container knowing any of them.
    /// </remarks>
    public readonly struct LootTakenEvent : IEvent
    {
        public LootTakenEvent(Vector2 where, int supplies, int ammo)
        {
            Where = where;
            Supplies = supplies;
            Ammo = ammo;
        }

        /// <summary>Where the container stood.</summary>
        public Vector2 Where { get; }

        /// <summary>How many supplies were in it.</summary>
        public int Supplies { get; }

        /// <summary>How many rounds were in it.</summary>
        public int Ammo { get; }
    }
}
