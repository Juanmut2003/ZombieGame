#nullable enable
using UnityEngine;

namespace ZombieGame.Base
{
    /// <summary>One building standing on the site.</summary>
    public readonly struct PlacedBuilding
    {
        public PlacedBuilding(
            int id,
            Vector2Int cell,
            BuildingFootprint footprint,
            BuildingDefinition? definition,
            int level = 1)
        {
            Id = id;
            Cell = cell;
            Footprint = footprint;
            Definition = definition;
            Level = level < 1 ? 1 : level;
        }

        /// <summary>
        /// Its number on this site. Counts from one, so that zero means "nothing here".
        /// </summary>
        public int Id { get; }

        /// <summary>The cell its bottom-left corner stands on.</summary>
        public Vector2Int Cell { get; }

        /// <summary>
        /// How much ground it covers, as it stands — already turned if it was turned.
        /// </summary>
        public BuildingFootprint Footprint { get; }

        /// <summary>What it is, when it was placed as something rather than as a shape.</summary>
        public BuildingDefinition? Definition { get; }

        /// <summary>Which rung of its ladder it is standing on. Counts from one.</summary>
        public int Level { get; }

        /// <summary>What it is now, at the level it has reached.</summary>
        public BuildingLevel? Rung => Definition == null ? null : Definition.Level(Level);
    }
}
