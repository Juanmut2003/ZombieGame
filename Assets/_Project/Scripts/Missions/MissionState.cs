#nullable enable

namespace ZombieGame.Missions
{
    /// <summary>How far through a scavenge run the squad is.</summary>
    public enum MissionState
    {
        /// <summary>Out there, looting.</summary>
        Running = 0,

        /// <summary>Standing in the extraction zone, waiting out the clock.</summary>
        Extracting = 1,

        /// <summary>Out, with whatever was carried.</summary>
        Extracted = 2,

        /// <summary>Over, with nothing.</summary>
        Failed = 3,
    }
}
