#nullable enable
using UnityEngine;
using ZombieGame.Core.Events;

namespace ZombieGame.Base
{
    /// <summary>Something went up on the site.</summary>
    /// <remarks>
    /// Announced rather than called, because the list of systems that care about a new building
    /// only grows: the power grid wants its draw, the jobs want its posts, the wave scheduler
    /// wants to know the plot got louder. None of them should have to be known to the thing
    /// that put it down.
    /// </remarks>
    public readonly struct BuildingPlacedEvent : IEvent
    {
        public BuildingPlacedEvent(int id, Vector2Int cell, BuildingDefinition? what)
        {
            Id = id;
            Cell = cell;
            What = what;
        }

        /// <summary>Its number on the site.</summary>
        public int Id { get; }

        /// <summary>The cell its bottom-left corner stands on.</summary>
        public Vector2Int Cell { get; }

        /// <summary>What was put up.</summary>
        public BuildingDefinition? What { get; }
    }
}
