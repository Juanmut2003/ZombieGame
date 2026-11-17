#nullable enable
using UnityEngine;

namespace ZombieGame.Waves
{
    /// <summary>
    /// One zone's local heat: how much noise it has taken lately, and what that makes it.
    /// </summary>
    /// <remarks>
    /// PROJECT.md §5 gives missions their own heat meter, apart from the base's: gunfire and
    /// alarms escalate the zone in real time. This is that meter and nothing else — a plain C#
    /// class with no <c>MonoBehaviour</c> in sight, because the arithmetic of "noise in, quiet
    /// out" is exactly the kind of thing CLAUDE.md wants testable without opening the editor.
    /// Heat is normalised: 0 is a zone that has heard nothing, 1 is as bad as it gets. That keeps
    /// the HUD, the stage thresholds and the balance numbers all in the same units.
    /// </remarks>
    public sealed class HeatMeter
    {
        private readonly HeatSettings _settings;

        public HeatMeter(HeatSettings settings)
        {
            _settings = settings;
            Stage = HeatStage.Calm;
        }

        /// <summary>How hot the zone is, 0 to 1.</summary>
        public float Value { get; private set; }

        /// <summary>What that amount of heat means. Changes later than <see cref="Value"/> does.</summary>
        public HeatStage Stage { get; private set; }

        /// <summary>Takes in a noise. Louder noises heat the zone more.</summary>
        /// <param name="loudness">1 is an ordinary noise.</param>
        public void Hear(float loudness)
        {
            if (loudness <= 0f)
            {
                return;
            }

            Value = Mathf.Clamp01(Value + (loudness * _settings.PerLoudness));
            Restage();
        }

        /// <summary>Lets a stretch of quiet cool the zone down.</summary>
        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds <= 0f || Value <= 0f)
            {
                return;
            }

            Value = Mathf.Max(0f, Value - (_settings.DecayPerSecond * deltaSeconds));
            Restage();
        }

        /// <summary>Puts the zone back to silent. For starting a mission, and for tests.</summary>
        public void Reset()
        {
            Value = 0f;
            Stage = HeatStage.Calm;
        }

        /// <summary>
        /// Works out the stage, rising as soon as a threshold is passed and falling only once the
        /// meter is clearly back below it.
        /// </summary>
        /// <remarks>
        /// The asymmetry is the point. Escalation should feel immediate — the shot was heard, the
        /// zone reacts — while calming down should take visibly longer than the moment of quiet
        /// that happened to dip under the line.
        /// </remarks>
        private void Restage()
        {
            if (Value >= _settings.HuntingAt)
            {
                Stage = HeatStage.Hunting;
                return;
            }

            // Hunting holds until the meter is clearly back under the line it was crossed at.
            if (Stage == HeatStage.Hunting && Value >= _settings.HuntingAt - _settings.StageDrop)
            {
                return;
            }

            if (Value >= _settings.StirredAt)
            {
                Stage = HeatStage.Stirred;
                return;
            }

            if (Stage != HeatStage.Calm && Value >= _settings.StirredAt - _settings.StageDrop)
            {
                Stage = HeatStage.Stirred;
                return;
            }

            Stage = HeatStage.Calm;
        }
    }
}
