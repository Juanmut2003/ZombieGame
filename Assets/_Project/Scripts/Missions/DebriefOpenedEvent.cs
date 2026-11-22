#nullable enable
using ZombieGame.Core.Events;

namespace ZombieGame.Missions
{
    /// <summary>A debrief is on screen, and the player is reading it.</summary>
    /// <remarks>
    /// Published so that nothing else decides when the run is over. Before this the zone left
    /// for the menu on a four-second timer, which is either an age or nowhere near long enough
    /// depending on whether the player is counting their supplies or staring at the spot where
    /// they died. Whoever shows the debrief says so here, and whoever would have moved the
    /// player on stands down until <see cref="DebriefClosedEvent"/>.
    /// A zone with no debrief in it publishes nothing and keeps its timer, so a sandbox scene
    /// built without the screen still finds its way home.
    /// </remarks>
    public readonly struct DebriefOpenedEvent : IEvent
    {
    }
}
