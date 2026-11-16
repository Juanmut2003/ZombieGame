#nullable enable
using ZombieGame.Core.Events;

namespace ZombieGame.Core.Clock
{
    /// <summary>
    /// Raised once whenever the calendar day rolls over — both when the player confirms the end of
    /// a day in the base and when a day passes while the squad is out in a zone.
    /// </summary>
    /// <remarks>
    /// This is the heartbeat the whole base layer resolves on: jobs, production, growth, healing,
    /// construction, spoilage and the wave countdown all do their work in response to it.
    /// </remarks>
    public readonly struct DayAdvancedEvent : IEvent
    {
        /// <summary>The day that just started. Days are 1-based.</summary>
        public int Day { get; }

        /// <summary>
        /// True when the day rolled over because the player confirmed the end of the day in the
        /// base, false when it passed while in a zone. Base systems resolve either way; UI and
        /// audio usually only care about the confirmed case.
        /// </summary>
        public bool WasConfirmedByPlayer { get; }

        public DayAdvancedEvent(int day, bool wasConfirmedByPlayer)
        {
            Day = day;
            WasConfirmedByPlayer = wasConfirmedByPlayer;
        }
    }
}
