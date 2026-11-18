#nullable enable
using ZombieGame.Core.Events;

namespace ZombieGame.Missions
{
    /// <summary>The run is over, one way or the other.</summary>
    /// <remarks>
    /// Carries what a debrief needs and nothing a debrief could work out for itself. The one
    /// number worth both telling and keeping apart is what was carried against what was banked:
    /// losing a squad with nine supplies on it is a different kind of bad run from losing one
    /// with none, and PROJECT.md §8 wants the player to feel that difference.
    /// </remarks>
    public readonly struct MissionEndedEvent : IEvent
    {
        public MissionEndedEvent(
            string missionName,
            MissionOutcome outcome,
            int banked,
            int lost,
            int wanted,
            float seconds,
            float heatAtEnd)
        {
            MissionName = missionName;
            Outcome = outcome;
            Banked = banked;
            Lost = lost;
            Wanted = wanted;
            Seconds = seconds;
            HeatAtEnd = heatAtEnd;
        }

        /// <summary>Which run it was.</summary>
        public string MissionName { get; }

        /// <summary>How it ended.</summary>
        public MissionOutcome Outcome { get; }

        /// <summary>What came home.</summary>
        public int Banked { get; }

        /// <summary>What was being carried and did not come home.</summary>
        public int Lost { get; }

        /// <summary>What the run was sent for.</summary>
        public int Wanted { get; }

        /// <summary>How long the run took.</summary>
        public float Seconds { get; }

        /// <summary>How hot the zone was when it ended, 0 to 1.</summary>
        public float HeatAtEnd { get; }

        /// <summary>True when the squad got out.</summary>
        public bool Extracted =>
            Outcome is MissionOutcome.Extracted or MissionOutcome.ExtractedShort;
    }
}
