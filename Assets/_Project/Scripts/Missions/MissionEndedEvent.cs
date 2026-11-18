#nullable enable
using UnityEngine;
using ZombieGame.Core.Events;

namespace ZombieGame.Missions
{
    /// <summary>The run is over, one way or the other.</summary>
    public readonly struct MissionEndedEvent : IEvent
    {
        public MissionEndedEvent(string missionName, bool extracted, int banked)
        {
            MissionName = missionName;
            Extracted = extracted;
            Banked = banked;
        }

        /// <summary>Which run it was.</summary>
        public string MissionName { get; }

        /// <summary>True when the squad got out.</summary>
        public bool Extracted { get; }

        /// <summary>What came home. Nothing, when they did not.</summary>
        public int Banked { get; }
    }
}
