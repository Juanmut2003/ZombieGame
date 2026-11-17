#nullable enable
using ZombieGame.Core.Events;

namespace ZombieGame.Waves
{
    /// <summary>The zone's heat moved. Carries the number and what it now means.</summary>
    /// <remarks>
    /// Both are on the event on purpose: the HUD wants the fraction so the bar moves smoothly,
    /// and the spawner wants the stage so it escalates once per step rather than on every frame
    /// the meter creeps upward.
    /// </remarks>
    public readonly struct HeatChangedEvent : IEvent
    {
        public HeatChangedEvent(float value, HeatStage stage, HeatStage previousStage)
        {
            Value = value;
            Stage = stage;
            PreviousStage = previousStage;
        }

        /// <summary>How hot the zone is, 0 to 1.</summary>
        public float Value { get; }

        /// <summary>What that amount of heat means now.</summary>
        public HeatStage Stage { get; }

        /// <summary>What it meant a moment ago.</summary>
        public HeatStage PreviousStage { get; }

        /// <summary>True when the zone just got worse. The cue for anything that escalates.</summary>
        public bool Escalated => Stage > PreviousStage;
    }
}
