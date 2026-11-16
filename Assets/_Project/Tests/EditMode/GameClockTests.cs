#nullable enable
using System;
using System.Collections.Generic;
using NUnit.Framework;
using ZombieGame.Core.Clock;
using ZombieGame.Core.Events;

namespace ZombieGame.Tests.EditMode
{
    [TestFixture]
    public sealed class GameClockTests
    {
        // 100 seconds a day keeps the arithmetic in the tests obvious: 1 second == 1% of a day.
        private static ClockSettings Settings(float secondsPerDay = 100f, float dayStart = 0.25f) =>
            new(secondsPerDay, dayStart);

        private static GameClock InZone(EventBus? bus = null)
        {
            var clock = new GameClock(Settings(), bus);
            clock.EnterZone();
            return clock;
        }

        [Test]
        public void AFreshClockStartsOnDayOneAtTheConfiguredMorning()
        {
            var clock = new GameClock(Settings(dayStart: 0.25f));

            Assert.That(clock.Day, Is.EqualTo(1));
            Assert.That(clock.TimeOfDay, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(clock.TimeScale, Is.EqualTo(1f));
            Assert.That(clock.IsPaused, Is.False);
            Assert.That(clock.IsZoneTimeRunning, Is.False, "the base layer does not tick");
        }

        [Test]
        public void Tick_DoesNothingOutsideAZone()
        {
            var clock = new GameClock(Settings());

            clock.Tick(10f);

            Assert.That(clock.TimeOfDay, Is.EqualTo(0.25f).Within(0.0001f));
        }

        [Test]
        public void Tick_AdvancesTimeOfDayInsideAZone()
        {
            GameClock clock = InZone();

            clock.Tick(25f);

            Assert.That(clock.TimeOfDay, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(clock.Day, Is.EqualTo(1));
        }

        [Test]
        public void Tick_IsIgnoredWhilePaused()
        {
            GameClock clock = InZone();
            clock.Pause();

            clock.Tick(25f);

            Assert.That(clock.TimeOfDay, Is.EqualTo(0.25f).Within(0.0001f));
        }

        [Test]
        public void Tick_ResumesAfterUnpausing()
        {
            GameClock clock = InZone();
            clock.Pause();
            clock.Tick(25f);
            clock.Resume();

            clock.Tick(25f);

            Assert.That(clock.TimeOfDay, Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void TimeScale_ScalesTheTick()
        {
            GameClock clock = InZone();
            clock.SetTimeScale(2f);

            clock.Tick(10f);

            Assert.That(clock.TimeOfDay, Is.EqualTo(0.45f).Within(0.0001f));
        }

        [Test]
        public void TimeScaleOfZeroFreezesWithoutPausing()
        {
            GameClock clock = InZone();
            clock.SetTimeScale(0f);

            clock.Tick(50f);

            Assert.That(clock.TimeOfDay, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(clock.IsPaused, Is.False);
        }

        [Test]
        public void SetTimeScale_RejectsNegativeValues()
        {
            var clock = new GameClock(Settings());

            Assert.Throws<ArgumentOutOfRangeException>(() => clock.SetTimeScale(-1f));
        }

        [Test]
        public void Tick_RejectsNegativeDelta()
        {
            GameClock clock = InZone();

            Assert.Throws<ArgumentOutOfRangeException>(() => clock.Tick(-0.016f));
        }

        [Test]
        public void ADayThatRunsOutInAZoneRollsOverOnItsOwn()
        {
            GameClock clock = InZone();
            var days = new List<DayAdvancedEvent>();
            clock.DayAdvanced += days.Add;

            clock.Tick(80f); // 0.25 -> 1.05, so one midnight passes

            Assert.That(clock.Day, Is.EqualTo(2));
            Assert.That(clock.TimeOfDay, Is.EqualTo(0.05f).Within(0.0001f));
            Assert.That(days, Has.Count.EqualTo(1));
            Assert.That(days[0].WasConfirmedByPlayer, Is.False);
        }

        [Test]
        public void ALongTickCrossesEveryMidnightItCovers()
        {
            GameClock clock = InZone();
            var days = new List<int>();
            clock.DayAdvanced += e => days.Add(e.Day);

            clock.Tick(275f); // 2.75 days

            Assert.That(days, Is.EqualTo(new[] { 2, 3, 4 }));
            Assert.That(clock.Day, Is.EqualTo(4));
        }

        [Test]
        public void ConfirmEndOfDay_AdvancesToTheNextMorning()
        {
            var clock = new GameClock(Settings(dayStart: 0.25f));

            clock.ConfirmEndOfDay();

            Assert.That(clock.Day, Is.EqualTo(2));
            Assert.That(clock.TimeOfDay, Is.EqualTo(0.25f).Within(0.0001f));
        }

        [Test]
        public void ConfirmEndOfDay_ReportsThatThePlayerConfirmedIt()
        {
            var clock = new GameClock(Settings());
            DayAdvancedEvent? received = null;
            clock.DayAdvanced += e => received = e;

            clock.ConfirmEndOfDay();

            Assert.That(received.HasValue, Is.True);
            Assert.That(received!.Value.Day, Is.EqualTo(2));
            Assert.That(received.Value.WasConfirmedByPlayer, Is.True);
        }

        [Test]
        public void ConfirmEndOfDay_ThrowsWhileInAZone()
        {
            GameClock clock = InZone();

            Assert.Throws<InvalidOperationException>(() => clock.ConfirmEndOfDay());
        }

        [Test]
        public void ExitZone_HandsControlBackToTheBaseLayer()
        {
            GameClock clock = InZone();
            clock.Tick(25f);

            clock.ExitZone();
            clock.Tick(25f);

            Assert.That(clock.TimeOfDay, Is.EqualTo(0.5f).Within(0.0001f), "time stops in the base");
            Assert.DoesNotThrow(() => clock.ConfirmEndOfDay());
            Assert.That(clock.Day, Is.EqualTo(2));
        }

        [Test]
        public void DayAdvanced_AlsoGoesOutOnTheEventBus()
        {
            var bus = new EventBus();
            var received = new List<DayAdvancedEvent>();
            bus.Subscribe<DayAdvancedEvent>(received.Add);
            var clock = new GameClock(Settings(), bus);

            clock.ConfirmEndOfDay();

            Assert.That(received, Has.Count.EqualTo(1));
            Assert.That(received[0].Day, Is.EqualTo(2));
            Assert.That(received[0].WasConfirmedByPlayer, Is.True);
        }

        [Test]
        public void PausedChanged_FiresOncePerActualChange()
        {
            var clock = new GameClock(Settings());
            var changes = new List<bool>();
            clock.PausedChanged += changes.Add;

            clock.Pause();
            clock.Pause();
            clock.Resume();
            clock.Resume();

            Assert.That(changes, Is.EqualTo(new[] { true, false }));
        }

        [Test]
        public void ClockSettings_RejectNonsense()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ClockSettings(secondsPerDay: 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ClockSettings(secondsPerDay: -1f));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new ClockSettings(dayStartNormalized: 1f));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new ClockSettings(dayStartNormalized: -0.1f));
        }

        [Test]
        public void Constructor_RejectsMissingSettings()
        {
            Assert.Throws<ArgumentNullException>(() => new GameClock(null!));
        }
    }
}
