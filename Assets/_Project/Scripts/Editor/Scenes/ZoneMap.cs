#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace ZombieGame.Editor.Scenes
{
    /// <summary>
    /// Where the things a map asked for ended up, in world coordinates.
    /// </summary>
    /// <remarks>
    /// Painting the tiles and placing what stands on them are two jobs. The painter reads the
    /// map, works out where every marker landed, and hands back this — so the builder places
    /// loot and a way out without doing the row-and-column arithmetic a second time.
    /// </remarks>
    internal sealed class ZoneMap
    {
        /// <summary>Where the squad starts.</summary>
        public Vector2 PlayerStart { get; set; }

        /// <summary>Where the way out is.</summary>
        public Vector2 Extraction { get; set; }

        /// <summary>Every container the map asked for.</summary>
        public List<Vector2> Loot { get; } = new();

        /// <summary>Every spot the map wants zombies around.</summary>
        public List<Vector2> ZombieNests { get; } = new();
    }
}
