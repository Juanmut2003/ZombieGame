#nullable enable
using UnityEngine;

namespace ZombieGame.Combat
{
    /// <summary>One round in the air, as plain data in an array.</summary>
    /// <remarks>
    /// Same reasoning as the zombies: a magazine emptied into a crowd is dozens of these in a
    /// second, and a GameObject with its own Update for each one would spend more time on engine
    /// overhead than on the game.
    /// </remarks>
    public struct ProjectileState
    {
        /// <summary>Where it is.</summary>
        public Vector2 Position;

        /// <summary>Where it is going, in world units per second.</summary>
        public Vector2 Velocity;

        /// <summary>Seconds left before it gives up. This, times speed, is its reach.</summary>
        public float Remaining;

        /// <summary>How fat it is, for deciding what it clips.</summary>
        public float Radius;

        /// <summary>What it does to whatever it hits.</summary>
        public DamageInfo Damage;

        /// <summary>
        /// Identifies this round across its life, even as it moves around in the array. Starts at
        /// one; zero means nobody.
        /// </summary>
        public int Id;
    }
}
