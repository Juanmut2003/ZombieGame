#nullable enable
using System;

namespace ZombieGame.AI
{
    /// <summary>
    /// What one kind of zombie can notice: how far it sees, how wide, and how far it hears.
    /// </summary>
    /// <remarks>
    /// PROJECT.md 9 puts stealth level with fighting rather than beneath it, which only works if
    /// what a zombie can notice is a number a designer can turn. A zombie that sees everything
    /// makes sneaking pointless; one that sees nothing makes it free.
    /// Hearing is deliberately a circle while sight is a wedge: ears have no facing, and that
    /// asymmetry is what makes walking behind one safe and running behind one not.
    /// </remarks>
    public readonly struct ZombieSenses
    {
        /// <param name="viewDistance">How far it can see, in world units.</param>
        /// <param name="viewHalfAngleDegrees">
        /// Half the width of the cone it sees in. 90 is everything in front of it.
        /// </param>
        /// <param name="hearingRadius">
        /// How far an ordinary noise carries to it. A louder noise reaches further.
        /// </param>
        /// <param name="listenSeconds">How long it keeps looking at whatever it heard.</param>
        public ZombieSenses(
            float viewDistance = 7f,
            float viewHalfAngleDegrees = 55f,
            float hearingRadius = 6f,
            float listenSeconds = 2f)
        {
            if (viewDistance < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(viewDistance), viewDistance, "Cannot be negative.");
            }

            if (viewHalfAngleDegrees < 0f || viewHalfAngleDegrees > 180f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(viewHalfAngleDegrees),
                    viewHalfAngleDegrees,
                    "A half-angle runs from nothing to all the way around.");
            }

            if (hearingRadius < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(hearingRadius), hearingRadius, "Cannot be negative.");
            }

            if (listenSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(listenSeconds), listenSeconds, "Cannot be negative.");
            }

            ViewDistance = viewDistance;
            ViewHalfAngleDegrees = viewHalfAngleDegrees;
            HearingRadius = hearingRadius;
            ListenSeconds = listenSeconds;
        }

        /// <summary>How far it sees.</summary>
        public float ViewDistance { get; }

        /// <summary>Half the width of its view cone, in degrees.</summary>
        public float ViewHalfAngleDegrees { get; }

        /// <summary>How far an ordinary noise carries to it.</summary>
        public float HearingRadius { get; }

        /// <summary>How long it keeps looking towards a noise it heard.</summary>
        public float ListenSeconds { get; }

        /// <summary>Notices nothing at all. For tests that want perception out of the way.</summary>
        public static ZombieSenses Deaf => new(0f, 0f, 0f, 0f);
    }
}
