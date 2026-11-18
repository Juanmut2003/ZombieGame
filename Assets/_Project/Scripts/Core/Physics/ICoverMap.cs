#nullable enable
using UnityEngine;

namespace ZombieGame.Core.Physics
{
    /// <summary>Answers whether a spot in the world hides whoever is standing on it.</summary>
    /// <remarks>
    /// Cover is not collision: scrub can be walked through, and that is the point of it. A zone
    /// keeps the two as separate grids so that walking into a hedge changes what the zone knows
    /// about you without changing where you can go.
    /// </remarks>
    public interface ICoverMap
    {
        /// <summary>True when somebody standing here is out of plain sight.</summary>
        bool Hides(Vector2 point);
    }

    /// <summary>Nowhere to hide. The default, and what tests get by not passing one.</summary>
    public sealed class OpenSight : ICoverMap
    {
        /// <summary>The one instance; it holds no state.</summary>
        public static readonly OpenSight Instance = new();

        private OpenSight()
        {
        }

        /// <inheritdoc />
        public bool Hides(Vector2 point) => false;
    }
}
