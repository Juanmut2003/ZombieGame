#nullable enable
using UnityEngine;

namespace ZombieGame.Core.Rendering
{
    /// <summary>
    /// Which sorting order everything in a zone draws at.
    /// </summary>
    /// <remarks>
    /// Written down in one place after a zombie standing in a house turned out to be drawn
    /// underneath its floor. Nothing was broken on its own: the tilemaps sorted from zero upwards
    /// and the horde sorted by how far down the screen it stood, and the second ran straight
    /// through the first. A body five metres up the map scored −40 and vanished under the ground
    /// it was walking on — invisible, still biting.
    /// So the orders are bands rather than numbers picked next to the code that uses them. The
    /// ground is below everything, actors sort against each other inside a band that cannot reach
    /// the ground, and what flies is above all of it.
    /// </remarks>
    public static class SortingBands
    {
        /// <summary>The ground itself.</summary>
        public const int Floor = 0;

        /// <summary>Paint on the ground: lane markings, an extraction pad.</summary>
        public const int Markings = 1;

        /// <summary>Walls.</summary>
        public const int Walls = 2;

        /// <summary>Scrub, which stands on the ground and is walked through.</summary>
        public const int Scrub = 3;

        /// <summary>Where the actor band is centred.</summary>
        public const int ActorMiddle = 500;

        /// <summary>The lowest an actor can sort. Comfortably above the ground.</summary>
        public const int ActorLowest = 100;

        /// <summary>The highest an actor can sort.</summary>
        public const int ActorHighest = 900;

        /// <summary>Rounds in flight, above everyone.</summary>
        public const int Projectile = 950;

        /// <summary>
        /// Where something standing at <paramref name="y"/> draws, so that a figure lower down
        /// the screen overlaps one further back.
        /// </summary>
        /// <remarks>
        /// Ten steps per world unit, which separates two bodies a tenth of a unit apart — finer
        /// than anything in a crowd ever is. Clamped, because a zone big enough to run off the
        /// end of the band would otherwise start sorting its far corners into the floor again,
        /// and that is precisely the fault this class exists to make impossible.
        /// </remarks>
        public static int ForGround(float y) =>
            Mathf.Clamp(
                ActorMiddle - Mathf.RoundToInt(y * 10f), ActorLowest, ActorHighest);
    }
}
