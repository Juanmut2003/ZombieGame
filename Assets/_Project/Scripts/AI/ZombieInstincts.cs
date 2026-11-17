#nullable enable
using System;

namespace ZombieGame.AI
{
    /// <summary>
    /// How one kind of zombie behaves when it is not simply walking at you: how far it strays,
    /// how long it stands about, and how long it keeps looking once you are out of sight.
    /// </summary>
    /// <remarks>
    /// These are the numbers that decide whether a zone can be crossed quietly. Giving up quickly
    /// makes breaking line of sight a reliable escape; giving up slowly makes it a gamble. Both
    /// are legitimate designs for different kinds of zombie, which is exactly why they belong in
    /// the asset and not in the state machine.
    /// </remarks>
    public readonly struct ZombieInstincts
    {
        /// <param name="wanderRadius">How far from where it started it will shuffle.</param>
        /// <param name="wanderPauseSeconds">How long it stands still between short walks.</param>
        /// <param name="giveUpSeconds">
        /// How long it keeps walking towards the last thing it noticed before losing interest.
        /// </param>
        /// <param name="wanderSpeedFactor">
        /// What it multiplies its speed by while it has noticed nothing. An undisturbed zombie
        /// moving at hunting speed would make the whole zone look alerted.
        /// </param>
        /// <param name="arriveDistance">How close counts as having got there.</param>
        public ZombieInstincts(
            float wanderRadius = 3f,
            float wanderPauseSeconds = 2.5f,
            float giveUpSeconds = 5f,
            float wanderSpeedFactor = 0.55f,
            float arriveDistance = 0.35f)
        {
            if (wanderRadius < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(wanderRadius), wanderRadius, "Cannot be negative.");
            }

            if (wanderPauseSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(wanderPauseSeconds), wanderPauseSeconds, "Cannot be negative.");
            }

            if (giveUpSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(giveUpSeconds), giveUpSeconds, "Cannot be negative.");
            }

            if (wanderSpeedFactor < 0f || wanderSpeedFactor > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(wanderSpeedFactor),
                    wanderSpeedFactor,
                    "Wandering is never faster than hunting.");
            }

            if (arriveDistance <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(arriveDistance), arriveDistance, "Nobody ever arrives exactly.");
            }

            WanderRadius = wanderRadius;
            WanderPauseSeconds = wanderPauseSeconds;
            GiveUpSeconds = giveUpSeconds;
            WanderSpeedFactor = wanderSpeedFactor;
            ArriveDistance = arriveDistance;
        }

        /// <summary>How far from home it strays.</summary>
        public float WanderRadius { get; }

        /// <summary>How long it stands still between short walks.</summary>
        public float WanderPauseSeconds { get; }

        /// <summary>How long it keeps after something it has lost.</summary>
        public float GiveUpSeconds { get; }

        /// <summary>What it multiplies its speed by while undisturbed.</summary>
        public float WanderSpeedFactor { get; }

        /// <summary>How close counts as having arrived.</summary>
        public float ArriveDistance { get; }

        /// <summary>Stays exactly where it is put. For tests that want behaviour out of the way.</summary>
        public static ZombieInstincts Still => new(0f, 1f, 0f, 0f);
    }
}
