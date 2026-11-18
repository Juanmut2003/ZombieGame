#nullable enable
using UnityEngine;
using ZombieGame.Core.Events;

namespace ZombieGame.Missions
{
    /// <summary>The state of the run changed: loot, extraction, or the end of it.</summary>
    public readonly struct MissionProgressEvent : IEvent
    {
        public MissionProgressEvent(
            string objective, int carried, int wanted, MissionState state, float extraction)
        {
            Objective = objective;
            Carried = carried;
            Wanted = wanted;
            State = state;
            ExtractionFraction = extraction;
        }

        /// <summary>One line of what to do.</summary>
        public string Objective { get; }

        /// <summary>What the squad is carrying.</summary>
        public int Carried { get; }

        /// <summary>What the run was sent for.</summary>
        public int Wanted { get; }

        /// <summary>Where the run stands.</summary>
        public MissionState State { get; }

        /// <summary>How far through the extraction hold the squad is, 0 to 1.</summary>
        public float ExtractionFraction { get; }
    }
}
