#nullable enable
using UnityEngine;

namespace ZombieGame.AI
{
    /// <summary>One zombie, as plain data in an array.</summary>
    /// <remarks>
    /// A struct, not a component: hundreds of these are walked over every tick, and they need to
    /// sit next to each other in memory rather than scattered across the heap as objects.
    /// </remarks>
    public struct ZombieState
    {
        /// <summary>Where it is.</summary>
        public Vector2 Position;

        /// <summary>Where it is heading, in units per second.</summary>
        public Vector2 Velocity;

        /// <summary>Where it wants to get to.</summary>
        public Vector2 Destination;

        /// <summary>Health left.</summary>
        public float Health;

        /// <summary>Top speed for this individual, varied from the definition at spawn.</summary>
        public float Speed;

        /// <summary>How much room it takes up, for keeping the crowd apart.</summary>
        public float Radius;

        /// <summary>Which definition it was spawned from, as the spawner numbered them.</summary>
        public int TypeId;

        /// <summary>Which way it is facing.</summary>
        public ZombieFacing Facing;

        /// <summary>
        /// Identifies this zombie across its life, even as it moves around in the array. Starts
        /// at one; zero means nobody.
        /// </summary>
        public int Id;
    }
}
