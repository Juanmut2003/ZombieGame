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
        public static int WallsMask => MaskOf(WallsIndex);

        /// <summary>
        /// The bodies of the horde — and anything else the map must not give away.
        /// </summary>
        /// <remarks>
        /// A layer about what may be <i>shown</i> rather than what may be touched, which is
        /// unusual and worth saying out loud. The minimap is a second camera looking at the same
        /// world, so without somewhere to put them it shows every zombie in the zone and turns a
        /// map into a radar. That would undo the whole of the perception work: sight cones, line
        /// of sight and crouching all stop meaning anything once the player can simply read the
        /// positions off the corner of the screen.
        /// The squad is deliberately not on it. Seeing where you are is the one thing a map is
        /// unambiguously for.
        /// </remarks>
        public const string Horde = "Horde";

        /// <summary>The <see cref="Horde"/> layer index, or −1 when the project has no such layer.</summary>
        public static int HordeIndex => LayerMask.NameToLayer(Horde);

        /// <summary>A mask holding only <see cref="Horde"/>, or zero when the layer is missing.</summary>
        public static int HordeMask => MaskOf(HordeIndex);

        /// <summary>Everything except the horde: what a map is allowed to show.</summary>
        public static int EverythingButTheHorde => ~HordeMask;

        private static int MaskOf(int index) => index < 0 ? 0 : 1 << index;
    }
}
