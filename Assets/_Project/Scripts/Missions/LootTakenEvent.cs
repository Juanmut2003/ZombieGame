#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieGame.Core.Events;
using ZombieGame.Economy;

namespace ZombieGame.Missions
{
    /// <summary>Something was taken out of a container.</summary>
    /// <remarks>
    /// Announced rather than handed over, so the run's bookkeeping, the HUD and later the noise of
    /// rummaging through a shelf all hear the same thing without the container knowing any of them.
    /// Carries what was in it by store and not a total of it: the one thing that needs a total
    /// is the objective, and the run that owns the objective can add up its own tally.
    /// </remarks>
    public readonly struct LootTakenEvent : IEvent
    {
        public LootTakenEvent(Vector2 where, IReadOnlyList<ResourceAmount>? contents, int ammo)
        {
            Where = where;
            Contents = contents ?? Array.Empty<ResourceAmount>();
            Ammo = ammo;
        }

        /// <summary>Where the container stood.</summary>
        public Vector2 Where { get; }

        /// <summary>What was in it, by store.</summary>
        public IReadOnlyList<ResourceAmount> Contents { get; }

        /// <summary>How many rounds were in it.</summary>
        public int Ammo { get; }
    }
}
