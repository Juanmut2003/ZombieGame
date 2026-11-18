#nullable enable
using System;
using NUnit.Framework;
using ZombieGame.Missions;

namespace ZombieGame.Tests.EditMode
{
    /// <summary>
    /// The rules of a scavenge run, with the one that matters most: nothing counts until the
    /// squad is out.
    /// </summary>
    [TestFixture]
    public sealed class MissionProgressTests
    {
        private static MissionProgress Run(int wanted = 6, float hold = 4f) => new(wanted, hold);

        [Test]
        public void ARunStartsEmptyAndRunning()
        {
            MissionProgress run = Run();

            Assert.That(run.Carried, Is.Zero);
            Assert.That(run.Banked, Is.Zero);
            Assert.That(run.State, Is.EqualTo(MissionState.Running));
            Assert.That(run.ObjectiveMet, Is.False);
        }

        [Test]
        public void LootIsCarriedNotBanked()
        {
            MissionProgress run = Run();
            run.Collect(3);

            Assert.That(run.Carried, Is.EqualTo(3));
            Assert.That(run.Banked, Is.Zero, "loot only counts once the squad is out");
        }

        [Test]
        public void EnoughLootMeetsTheObjective()
        {
            MissionProgress run = Run(wanted: 4);
            run.Collect(4);

            Assert.That(run.ObjectiveMet, Is.True);
            Assert.That(run.State, Is.EqualTo(MissionState.Running), "meeting it is not leaving");
        }

        [Test]
        public void StandingInTheZoneExtracts()
        {
            MissionProgress run = Run(hold: 2f);
            run.Collect(5);

            Assert.That(run.TickExtraction(1f, inZone: true), Is.False);
            Assert.That(run.State, Is.EqualTo(MissionState.Extracting));

            Assert.That(run.TickExtraction(1.5f, inZone: true), Is.True);
            Assert.That(run.State, Is.EqualTo(MissionState.Extracted));
            Assert.That(run.Banked, Is.EqualTo(5));
        }

        [Test]
        public void SteppingOutLosesTheHold()
        {
            MissionProgress run = Run(hold: 2f);

            run.TickExtraction(1.9f, inZone: true);
            run.TickExtraction(0.1f, inZone: false);

            Assert.That(run.State, Is.EqualTo(MissionState.Running));
            Assert.That(run.ExtractionFraction, Is.Zero, "a hold in instalments is not a hold");

            run.TickExtraction(1f, inZone: true);
            Assert.That(run.State, Is.EqualTo(MissionState.Extracting), "and it starts again");
        }

        [Test]
        public void TheHoldReportsHowFarAlongItIs()
        {
            MissionProgress run = Run(hold: 4f);
            run.TickExtraction(1f, inZone: true);

            Assert.That(run.ExtractionFraction, Is.EqualTo(0.25f).Within(0.001f));
        }

        [Test]
        public void ExtractingWithNothingIsStillGettingOut()
        {
            MissionProgress run = Run(wanted: 6, hold: 1f);

            Assert.That(run.TickExtraction(1f, inZone: true), Is.True);
            Assert.That(run.State, Is.EqualTo(MissionState.Extracted));
            Assert.That(run.Banked, Is.Zero);
        }

        [Test]
        public void FailingLosesEverythingCarried()
        {
            MissionProgress run = Run();
            run.Collect(9);
            run.Fail();

            Assert.That(run.State, Is.EqualTo(MissionState.Failed));
            Assert.That(run.Banked, Is.Zero, "a squad that does not come back brings nothing back");
            Assert.That(run.IsOver, Is.True);
        }

        [Test]
        public void AFinishedRunIgnoresEverythingAfterwards()
        {
            MissionProgress run = Run(hold: 1f);
            run.Collect(2);
            run.TickExtraction(1f, inZone: true);

            run.Collect(5);
            run.Fail();

            Assert.That(run.State, Is.EqualTo(MissionState.Extracted));
            Assert.That(run.Carried, Is.EqualTo(2));
            Assert.That(run.Banked, Is.EqualTo(2));
        }

        [Test]
        public void AnInstantExtractionIsAllowed()
        {
            MissionProgress run = Run(hold: 0f);

            Assert.That(run.TickExtraction(0.1f, inZone: true), Is.True);
        }

        [Test]
        public void ARunRefusesNonsense()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new MissionProgress(-1, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MissionProgress(1, -1f));
        }
    }
}
