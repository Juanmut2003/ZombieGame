#nullable enable
using System;
using NUnit.Framework;
using ZombieGame.Waves;

namespace ZombieGame.Tests.EditMode
{
    /// <summary>
    /// A zone's heat: noise puts it up, quiet brings it down, and the stage follows behind.
    /// </summary>
    /// <remarks>
    /// All of it is arithmetic, which is exactly why it is worth testing here rather than by
    /// sprinting around a zone with a stopwatch. The numbers below are the test's own; the game's
    /// live in the balance asset and are checked separately.
    /// </remarks>
    [TestFixture]
    public sealed class HeatMeterTests
    {
        private static HeatMeter Meter(float perNoise = 0.1f, float decay = 0.05f) =>
            new(new HeatSettings(perNoise, decay, stirredAt: 0.4f, huntingAt: 0.8f, stageDrop: 0.1f));

        [Test]
        public void AQuietZoneStartsCold()
        {
            HeatMeter heat = Meter();

            Assert.That(heat.Value, Is.Zero);
            Assert.That(heat.Stage, Is.EqualTo(HeatStage.Calm));
        }

        [Test]
        public void ANoiseHeatsItUp()
        {
            HeatMeter heat = Meter();
            heat.Hear(1f);

            Assert.That(heat.Value, Is.EqualTo(0.1f).Within(0.0001f));
        }

        [Test]
        public void ALouderNoiseHeatsItMore()
        {
            HeatMeter quiet = Meter();
            HeatMeter loud = Meter();

            quiet.Hear(1f);
            loud.Hear(4f);

            // A gunshot has to cost more than a footstep, or silencers and bats are decoration.
            Assert.That(loud.Value, Is.GreaterThan(quiet.Value));
        }

        [Test]
        public void ItNeverGoesPastFull()
        {
            HeatMeter heat = Meter();
            for (int i = 0; i < 50; i++)
            {
                heat.Hear(5f);
            }

            Assert.That(heat.Value, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void QuietCoolsItDown()
        {
            HeatMeter heat = Meter();
            heat.Hear(5f);
            float hot = heat.Value;

            heat.Tick(2f);

            Assert.That(heat.Value, Is.LessThan(hot));
        }

        [Test]
        public void HidingCoolsItDownFaster()
        {
            HeatMeter inTheOpen = Meter();
            HeatMeter inTheScrub = Meter();
            inTheOpen.Hear(5f);
            inTheScrub.Hear(5f);

            inTheOpen.Tick(2f);
            inTheScrub.Tick(2f, decayFactor: 4f);

            // The whole reason to walk into a hedge rather than round it.
            Assert.That(inTheScrub.Value, Is.LessThan(inTheOpen.Value));
        }

        [Test]
        public void StandingStillInTheOpenIsTheUnchangedRate()
        {
            HeatMeter heat = Meter();
            heat.Hear(5f);
            float hot = heat.Value;

            heat.Tick(2f, decayFactor: 1f);
            float cooled = heat.Value;

            heat.Reset();
            heat.Hear(5f);
            heat.Tick(2f);

            // A factor of one has to mean exactly what no factor at all meant, or every balance
            // number tuned before cover existed quietly changed meaning.
            Assert.That(heat.Value, Is.EqualTo(cooled).Within(0.0001f));
            Assert.That(cooled, Is.LessThan(hot));
        }

        [Test]
        public void ItNeverGoesBelowEmpty()
        {
            HeatMeter heat = Meter();
            heat.Hear(1f);
            heat.Tick(1000f);

            Assert.That(heat.Value, Is.Zero);
            Assert.That(heat.Stage, Is.EqualTo(HeatStage.Calm));
        }

        [Test]
        public void EnoughNoiseStirsTheZone()
        {
            HeatMeter heat = Meter();
            heat.Hear(4f);

            Assert.That(heat.Value, Is.EqualTo(0.4f).Within(0.0001f));
            Assert.That(heat.Stage, Is.EqualTo(HeatStage.Stirred));
        }

        [Test]
        public void ALotOfNoiseStartsAHunt()
        {
            HeatMeter heat = Meter();
            heat.Hear(8f);

            Assert.That(heat.Stage, Is.EqualTo(HeatStage.Hunting));
        }

        [Test]
        public void AStageHoldsThroughAMomentOfQuiet()
        {
            HeatMeter heat = Meter();
            heat.Hear(8f);

            // Just under the threshold it crossed. Escalation is immediate, calming down is not:
            // a meter that flickered on the line would re-escalate the zone every second.
            heat.Tick(0.5f);

            Assert.That(heat.Value, Is.LessThan(0.8f));
            Assert.That(heat.Stage, Is.EqualTo(HeatStage.Hunting));
        }

        [Test]
        public void ItCalmsDownOnceTheNoiseIsWellPast()
        {
            HeatMeter heat = Meter();
            heat.Hear(8f);
            heat.Tick(4f);

            Assert.That(heat.Stage, Is.EqualTo(HeatStage.Stirred));

            heat.Tick(8f);

            Assert.That(heat.Stage, Is.EqualTo(HeatStage.Calm));
        }

        [Test]
        public void SilenceChangesNothing()
        {
            HeatMeter heat = Meter();
            heat.Hear(0f);

            Assert.That(heat.Value, Is.Zero);
        }

        [Test]
        public void ResettingPutsTheZoneBackToSilent()
        {
            HeatMeter heat = Meter();
            heat.Hear(9f);
            heat.Reset();

            Assert.That(heat.Value, Is.Zero);
            Assert.That(heat.Stage, Is.EqualTo(HeatStage.Calm));
        }

        [Test]
        public void SettingsRefuseNonsense()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new HeatSettings(-1f, 0.1f, 0.4f, 0.8f, 0.1f));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new HeatSettings(0.1f, -1f, 0.4f, 0.8f, 0.1f));

            // Hunting below stirred would mean a zone that calms down as it gets louder.
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new HeatSettings(0.1f, 0.1f, 0.8f, 0.4f, 0.1f));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new HeatSettings(0.1f, 0.1f, 0.4f, 0.8f, 0.9f));
        }

        [Test]
        public void ANoiseEventCannotBeNegativelyLoud()
        {
            var noise = new NoiseEvent(UnityEngine.Vector2.zero, -3f);

            Assert.That(noise.Loudness, Is.Zero);
        }

        [Test]
        public void TheChangeSaysWhetherThingsGotWorse()
        {
            var worse = new HeatChangedEvent(0.5f, HeatStage.Stirred, HeatStage.Calm);
            var better = new HeatChangedEvent(0.3f, HeatStage.Calm, HeatStage.Stirred);

            Assert.That(worse.Escalated, Is.True);
            Assert.That(better.Escalated, Is.False);
        }
    }
}
