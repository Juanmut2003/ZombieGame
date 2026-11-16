#nullable enable
using System;

namespace ZombieGame.Core.Clock
{
    /// <summary>
    /// The tunable numbers behind <see cref="GameClock"/>, passed in rather than hardcoded.
    /// A <c>BalanceConfig</c> ScriptableObject supplies these at runtime; tests construct them
    /// directly.
    /// </summary>
    public sealed class ClockSettings
    {
        /// <summary>Real-time seconds a full in-zone day takes at time scale 1.</summary>
        public float SecondsPerDay { get; }

        /// <summary>
        /// The point in the day a fresh day starts at, normalised 0–1 (0.25 == 06:00).
        /// Confirming the end of a day jumps here.
        /// </summary>
        public float DayStartNormalized { get; }

        /// <param name="secondsPerDay">Must be greater than zero.</param>
        /// <param name="dayStartNormalized">Must be within 0–1 (inclusive of 0, exclusive of 1).</param>
        public ClockSettings(float secondsPerDay = 1440f, float dayStartNormalized = 0.25f)
        {
            if (secondsPerDay <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(secondsPerDay), secondsPerDay, "A day must last longer than zero seconds.");
            }

            if (dayStartNormalized < 0f || dayStartNormalized >= 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(dayStartNormalized), dayStartNormalized, "Must be within [0, 1).");
            }

            SecondsPerDay = secondsPerDay;
            DayStartNormalized = dayStartNormalized;
        }
    }
}
