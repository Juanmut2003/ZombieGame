#nullable enable
using ZombieGame.Core.Events;

namespace ZombieGame.Missions
{
    /// <summary>The player has read the debrief and asked to move on.</summary>
    /// <remarks>
    /// The other half of <see cref="DebriefOpenedEvent"/>. It travels through the bus rather
    /// than as a call from the button, because the screen is UI and leaving the zone is not:
    /// the two are not allowed to know about each other.
    /// </remarks>
    public readonly struct DebriefClosedEvent : IEvent
    {
    }
}
