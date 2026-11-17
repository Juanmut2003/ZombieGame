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

        /// <summary>
        /// Which of its kind's looks it wears, drawn at spawn. Zero when the kind has one.
        /// </summary>
        /// <remarks>
        /// Fixed for life: a zombie that re-rolled its face every time a pooled body was
        /// handed around would flicker through the whole wardrobe as the crowd shifted.
        /// </remarks>
        public int SkinId;

        /// <summary>Which definition it was spawned from, as the spawner numbered them.</summary>
        public int TypeId;

        /// <summary>Which way it is facing, as the three poses the art has.</summary>
        public ZombieFacing Facing;

        /// <summary>
        /// Which way it is actually looking, as a direction. Kept apart from
        /// <see cref="Facing"/> because the art has three poses and perception has 360 degrees.
        /// </summary>
        public Vector2 Look;

        /// <summary>What it is doing about the world.</summary>
        public ZombieBehaviour Behaviour;

        /// <summary>
        /// Seconds left of the current behaviour before it is reconsidered. What it means depends
        /// on the behaviour: a pause while resting, a patience while chasing.
        /// </summary>
        public float BehaviourTimer;

        /// <summary>Where it was put down. Wandering happens around this, not around the player.</summary>
        public Vector2 Home;

        /// <summary>The last place it saw or heard something worth walking to.</summary>
        public Vector2 LastKnown;

        /// <summary>
        /// What its top speed is multiplied by right now. Undisturbed zombies shuffle; a chase is
        /// the only time a body moves at the speed written on its asset.
        /// </summary>
        public float SpeedFactor;

        /// <summary>Where the last noise it heard came from.</summary>
        public Vector2 HeardAt;

        /// <summary>
        /// Seconds left of staring at that noise. Zero means it is back to looking where it walks.
        /// </summary>
        public float ListenTimer;

        /// <summary>True while it can see what it is hunting.</summary>
        public bool SeesTarget;

        /// <summary>
        /// Identifies this zombie across its life, even as it moves around in the array. Starts
        /// at one; zero means nobody.
        /// </summary>
        public int Id;

        /// <summary>
        /// Seconds left of the flash that says this one was just hit. Zero means unhurt.
        /// </summary>
        /// <remarks>
        /// Presentation kept in the state rather than in the view, because the view is a pooled
        /// shell that may be handed to a different zombie at any time — the flash belongs to the
        /// zombie, not to the body currently drawing it.
        /// </remarks>
        public float HitFlash;
    }
}
