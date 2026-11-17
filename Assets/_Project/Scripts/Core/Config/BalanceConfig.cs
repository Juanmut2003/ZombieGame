#nullable enable
using UnityEngine;
using ZombieGame.Core.Clock;

namespace ZombieGame.Core.Config
{
    /// <summary>
    /// The tunable numbers that do not belong to any one piece of content. Designers edit the
    /// asset; code only ever reads it.
    /// </summary>
    /// <remarks>
    /// This is the home for values that would otherwise end up as constants in C#, which
    /// CLAUDE.md rules out. It starts with the clock and grows as systems arrive — wave formulas,
    /// noise weights and unlock thresholds all land here or in their own definition assets.
    /// </remarks>
    [CreateAssetMenu(menuName = "ZombieGame/Balance Config", fileName = "BalanceConfig")]
    public sealed class BalanceConfig : ScriptableObject
    {
        [Header("Time")]
        [Tooltip("Real-time seconds for a full in-game day while the squad is in a zone.")]
        [SerializeField]
        [Min(1f)]
        private float _secondsPerDay = 1440f;

        [Tooltip("Where a fresh day starts, 0 to 1. 0.25 is six in the morning.")]
        [SerializeField]
        [Range(0f, 0.999f)]
        private float _dayStartNormalized = 0.25f;

        [Header("Movement")]
        [Tooltip("How fast a character walks, in world units per second.")]
        [SerializeField]
        [Min(0.1f)]
        private float _walkSpeed = 3.5f;

        [Tooltip("What sprinting multiplies the walking speed by.")]
        [SerializeField]
        [Min(1f)]
        private float _sprintMultiplier = 1.8f;

        [Tooltip("How quickly a character reaches full speed, in units per second squared.")]
        [SerializeField]
        [Min(1f)]
        private float _acceleration = 40f;

        [Header("Crowds")]
        [Tooltip("How hard zombies standing in each other push apart, relative to walking speed.")]
        [SerializeField]
        [Min(0f)]
        private float _zombieSeparationStrength = 1.6f;

        /// <summary>Real-time seconds for a full in-game day in a zone.</summary>
        public float SecondsPerDay => _secondsPerDay;

        /// <summary>Walking speed in world units per second.</summary>
        public float WalkSpeed => _walkSpeed;

        /// <summary>What sprinting multiplies walking speed by.</summary>
        public float SprintMultiplier => _sprintMultiplier;

        /// <summary>How quickly a character reaches full speed.</summary>
        public float Acceleration => _acceleration;

        /// <summary>How hard crowded zombies push each other apart.</summary>
        public float ZombieSeparationStrength => _zombieSeparationStrength;

        /// <summary>The point in the day a confirmed day jumps to.</summary>
        public float DayStartNormalized => _dayStartNormalized;

        /// <summary>Builds the settings the <see cref="GameClock"/> runs on.</summary>
        public ClockSettings CreateClockSettings() => new(_secondsPerDay, _dayStartNormalized);
    }
}
