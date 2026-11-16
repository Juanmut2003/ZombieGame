#nullable enable
using System;
using ZombieGame.Core.Events;

namespace ZombieGame.Core.Clock
{
    /// <summary>
    /// The game's single source of time. Nothing in the project reads <c>Time.timeScale</c> or
    /// pauses on its own — pause, time scale and the day counter all live here.
    /// </summary>
    /// <remarks>
    /// The game runs on two clocks, as laid down in PROJECT.md §12:
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// <b>In a zone</b> (missions, travel, wave defence) time flows in real time: call
    /// <see cref="Tick"/> every frame and <see cref="TimeOfDay"/> advances. A day that runs out
    /// here rolls over on its own.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <b>In the base</b> nothing ticks. Time only moves when the player calls
    /// <see cref="ConfirmEndOfDay"/>, which jumps straight to the next morning.
    /// </description>
    /// </item>
    /// </list>
    /// Either way the rollover funnels into one place, so a system that resolves per day only has
    /// to listen for <see cref="DayAdvanced"/> and does not care which layer moved the clock.
    /// </remarks>
    public sealed class GameClock
    {
        private readonly ClockSettings _settings;
        private readonly EventBus? _bus;

        /// <summary>
        /// Creates a clock. Pass an <paramref name="bus"/> to also broadcast
        /// <see cref="DayAdvancedEvent"/>; without one the clock stays a plain C# object, which is
        /// how tests use it.
        /// </summary>
        public GameClock(ClockSettings settings, EventBus? bus = null)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _bus = bus;
            Day = 1;
            TimeOfDay = settings.DayStartNormalized;
            TimeScale = 1f;
        }

        /// <summary>The current calendar day, 1-based.</summary>
        public int Day { get; private set; }

        /// <summary>Where we are inside the current day, normalised 0–1 (0.5 == midday).</summary>
        public float TimeOfDay { get; private set; }

        /// <summary>True while time is held. A paused clock ignores <see cref="Tick"/>.</summary>
        public bool IsPaused { get; private set; }

        /// <summary>
        /// Multiplier applied to every tick. 1 is normal speed, 0 freezes without counting as a
        /// pause (useful for cutscenes that should not show the pause menu).
        /// </summary>
        public float TimeScale { get; private set; }

        /// <summary>
        /// True while the squad is in a zone and real time flows. False in the base, where the
        /// clock only moves on <see cref="ConfirmEndOfDay"/>.
        /// </summary>
        public bool IsZoneTimeRunning { get; private set; }

        /// <summary>Raised after the calendar day rolled over.</summary>
        public event Action<DayAdvancedEvent>? DayAdvanced;

        /// <summary>Raised whenever <see cref="IsPaused"/> changes.</summary>
        public event Action<bool>? PausedChanged;

        /// <summary>
        /// Advances in-zone time. Call once per frame with unscaled delta seconds; the clock
        /// applies <see cref="TimeScale"/> itself. Does nothing while paused or outside a zone.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Delta is negative.</exception>
        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deltaSeconds), deltaSeconds, "Time does not run backwards.");
            }

            if (!IsZoneTimeRunning || IsPaused || TimeScale <= 0f || deltaSeconds == 0f)
            {
                return;
            }

            TimeOfDay += deltaSeconds * TimeScale / _settings.SecondsPerDay;

            // A long frame or a big time scale can cross more than one midnight, so each day that
            // passed gets its own rollover rather than being silently swallowed.
            while (TimeOfDay >= 1f)
            {
                TimeOfDay -= 1f;
                AdvanceDay(wasConfirmedByPlayer: false);
            }
        }

        /// <summary>
        /// The base layer's heartbeat: ends the current day, jumps to the next morning and raises
        /// <see cref="DayAdvanced"/> so every base system resolves its day.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Called while in a zone. Days out there pass on their own; confirming one would skip
        /// however much of the day the squad still had left.
        /// </exception>
        public void ConfirmEndOfDay()
        {
            if (IsZoneTimeRunning)
            {
                throw new InvalidOperationException(
                    "The end of a day cannot be confirmed while in a zone — call ExitZone first.");
            }

            TimeOfDay = _settings.DayStartNormalized;
            AdvanceDay(wasConfirmedByPlayer: true);
        }

        /// <summary>Starts real-time flow, entering a mission, travel leg or wave defence.</summary>
        public void EnterZone() => IsZoneTimeRunning = true;

        /// <summary>Stops real-time flow, back to the base layer.</summary>
        public void ExitZone() => IsZoneTimeRunning = false;

        /// <summary>Holds time. Idempotent.</summary>
        public void Pause()
        {
            if (IsPaused)
            {
                return;
            }

            IsPaused = true;
            PausedChanged?.Invoke(true);
        }

        /// <summary>Releases a pause. Idempotent.</summary>
        public void Resume()
        {
            if (!IsPaused)
            {
                return;
            }

            IsPaused = false;
            PausedChanged?.Invoke(false);
        }

        /// <summary>Sets the time multiplier.</summary>
        /// <exception cref="ArgumentOutOfRangeException">The scale is negative.</exception>
        public void SetTimeScale(float timeScale)
        {
            if (timeScale < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timeScale), timeScale, "Time scale cannot be negative.");
            }

            TimeScale = timeScale;
        }

        private void AdvanceDay(bool wasConfirmedByPlayer)
        {
            Day++;
            var message = new DayAdvancedEvent(Day, wasConfirmedByPlayer);
            DayAdvanced?.Invoke(message);
            _bus?.Publish(message);
        }
    }
}
