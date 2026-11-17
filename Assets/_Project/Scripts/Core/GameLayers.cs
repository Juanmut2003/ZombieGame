#nullable enable
using UnityEngine;

namespace ZombieGame.Core
{
    /// <summary>
    /// The physics layers the game names, looked up once instead of spelled out everywhere.
    /// </summary>
    /// <remarks>
    /// A layer is a number on a GameObject, and a layer mask is the set of those numbers a query
    /// is willing to touch. Naming them here keeps the strings in one place: a misspelled
    /// <c>"Wals"</c> resolves to −1 and quietly turns a mask into "nothing", which looks exactly
    /// like a physics bug rather than a typo.
    /// </remarks>
    public static class GameLayers
    {
        /// <summary>Everything that blocks sight and bodies: walls, closed doors, solid scenery.</summary>
        public const string Walls = "Walls";

        /// <summary>The <see cref="Walls"/> layer index, or −1 when the project has no such layer.</summary>
        public static int WallsIndex => LayerMask.NameToLayer(Walls);

        /// <summary>A mask holding only <see cref="Walls"/>, or zero when the layer is missing.</summary>
        public static int WallsMask
        {
            get
            {
                int index = WallsIndex;
                return index < 0 ? 0 : 1 << index;
            }
        }
    }
}
