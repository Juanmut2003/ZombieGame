#nullable enable

namespace ZombieGame.Missions
{
    /// <summary>Why a run ended.</summary>
    /// <remarks>
    /// Kept apart from <see cref="MissionState"/>, which says where a run stands while it is
    /// still going. This says what to tell the player afterwards, and a debrief that cannot
    /// distinguish "you got out with six" from "you got out with none because you ran" is not
    /// a debrief.
    /// </remarks>
    public enum MissionOutcome
    {
        /// <summary>Still out there.</summary>
        None = 0,

        /// <summary>Out, with what the run was sent for.</summary>
        Extracted = 1,

        /// <summary>Out, but short of the objective. The supplies still count.</summary>
        ExtractedShort = 2,

        /// <summary>The squad did not come back. Everything carried is gone.</summary>
        SquadLost = 3,
    }
}
