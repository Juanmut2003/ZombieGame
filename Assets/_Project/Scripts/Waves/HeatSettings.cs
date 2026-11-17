#nullable enable
using System;

namespace ZombieGame.Waves
{
    /// <summary>What a zone's heat meter runs on. Comes from the balance asset, never from code.</summary>
    public readonly struct HeatSettings
    {
        /// <param name="perLoudness">How much heat one ordinary noise adds, of a full meter.</param>
        /// <param name="decayPerSecond">How much heat falls away each second of quiet.</param>
        /// <param name="stirredAt">Where the zone counts as having noticed.</param>
        /// <param name="huntingAt">Where it counts as hunting.</param>
        /// <param name="stageDrop">
        /// How far back below a threshold the meter has to fall before the stage drops again.
        /// Without it a meter hovering on a threshold flickers between two stages, which on the
        /// HUD looks like a fault and in the spawner would mean a fresh escalation every second.
        /// </param>
        public HeatSettings(
            float perLoudness,
            float decayPerSecond,
            float stirredAt,
            float huntingAt,
            float stageDrop)
        {
            if (perLoudness < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(perLoudness), perLoudness, "A noise cannot cool a zone down.");
            }

            if (decayPerSecond < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(decayPerSecond), decayPerSecond, "Quiet cannot heat a zone up.");
            }

            if (stirredAt <= 0f || huntingAt <= stirredAt || huntingAt > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(huntingAt), huntingAt, "Needs 0 < stirred < hunting <= 1.");
            }

            if (stageDrop < 0f || stageDrop >= stirredAt)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(stageDrop), stageDrop, "The drop has to be smaller than the first step.");
            }

            PerLoudness = perLoudness;
            DecayPerSecond = decayPerSecond;
            StirredAt = stirredAt;
            HuntingAt = huntingAt;
            StageDrop = stageDrop;
        }

        /// <summary>How much of a full meter one ordinary noise adds.</summary>
        public float PerLoudness { get; }

        /// <summary>How much heat a second of quiet takes off.</summary>
        public float DecayPerSecond { get; }

        /// <summary>Where <see cref="HeatStage.Stirred"/> begins.</summary>
        public float StirredAt { get; }

        /// <summary>Where <see cref="HeatStage.Hunting"/> begins.</summary>
        public float HuntingAt { get; }

        /// <summary>How far below a threshold the meter falls before the stage drops.</summary>
        public float StageDrop { get; }
    }
}
