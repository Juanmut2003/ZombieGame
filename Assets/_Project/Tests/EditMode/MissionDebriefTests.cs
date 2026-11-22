#nullable enable
using System.Collections.Generic;
using NUnit.Framework;
using ZombieGame.Missions;
using ZombieGame.Waves;

namespace ZombieGame.Tests.EditMode
{
    /// <summary>
    /// What the debrief says about a finished run.
    /// </summary>
    /// <remarks>
    /// Checked here rather than by playing a mission to the end and reading the screen, which is
    /// the only other way to see this text and takes a few minutes per look. The wording matters:
    /// a results screen is the only place the game ever explains itself.
    /// </remarks>
    [TestFixture]
    public sealed class MissionDebriefTests
    {
        private static MissionEndedEvent Ended(
            MissionOutcome outcome,
            int banked = 0,
            int lost = 0,
            int wanted = 8,
            float seconds = 125f,
            HeatStage stage = HeatStage.Calm) =>
            new("Town", outcome, banked, lost, wanted, seconds, heatAtEnd: 0.5f, stage);

        private static string ValueOf(IReadOnlyList<DebriefRow> rows, string label)
        {
            foreach (DebriefRow row in rows)
            {
                if (row.Label == label)
                {
                    return row.Value;
                }
            }

            return string.Empty;
        }

        private static bool Has(IReadOnlyList<DebriefRow> rows, string label) =>
            ValueOf(rows, label).Length > 0;

        [Test]
        public void AGoodRunSaysSoInOneWord()
        {
            Assert.That(MissionDebrief.Verdict(MissionOutcome.Extracted), Is.EqualTo("EXTRACTED"));
            Assert.That(MissionDebrief.Verdict(MissionOutcome.SquadLost), Is.EqualTo("SQUAD LOST"));
        }

        [Test]
        public void TheSuppliesAreCountedAgainstWhatTheRunWasFor()
        {
            IReadOnlyList<DebriefRow> rows =
                MissionDebrief.Rows(Ended(MissionOutcome.Extracted, banked: 6, wanted: 8));

            Assert.That(ValueOf(rows, "SUPPLIES"), Is.EqualTo("6 / 8"));
        }

        [Test]
        public void ACleanRunIsNotToldWhatItDidNotLose()
        {
            IReadOnlyList<DebriefRow> rows =
                MissionDebrief.Rows(Ended(MissionOutcome.Extracted, banked: 8, lost: 0));

            Assert.That(
                Has(rows, "LEFT BEHIND"),
                Is.False,
                "a debrief that reports a nil is a form, not a report");
        }

        [Test]
        public void ALostSquadIsToldWhatWentDownWithIt()
        {
            IReadOnlyList<DebriefRow> rows =
                MissionDebrief.Rows(Ended(MissionOutcome.SquadLost, banked: 0, lost: 5));

            // The whole point of keeping banked and lost apart: these are different bad runs.
            Assert.That(ValueOf(rows, "SUPPLIES"), Is.EqualTo("0 / 8"));
            Assert.That(ValueOf(rows, "LEFT BEHIND"), Is.EqualTo("5"));
        }

        [Test]
        public void TheTimeReadsAsAClock()
        {
            Assert.That(MissionDebrief.Clock(0f), Is.EqualTo("0:00"));
            Assert.That(MissionDebrief.Clock(9f), Is.EqualTo("0:09"));
            Assert.That(MissionDebrief.Clock(125f), Is.EqualTo("2:05"));
            Assert.That(MissionDebrief.Clock(3600f), Is.EqualTo("60:00"));

            // Not something a running mission can produce, but a broken save can.
            Assert.That(MissionDebrief.Clock(-4f), Is.EqualTo("0:00"));
        }

        [Test]
        public void HowTheZoneWasLeftIsReportedInTheMetersOwnWords()
        {
            Assert.That(MissionDebrief.Zone(HeatStage.Calm), Is.EqualTo("QUIET"));
            Assert.That(MissionDebrief.Zone(HeatStage.Stirred), Is.EqualTo("STIRRED"));
            Assert.That(MissionDebrief.Zone(HeatStage.Hunting), Is.EqualTo("HUNTING"));

            IReadOnlyList<DebriefRow> rows =
                MissionDebrief.Rows(Ended(MissionOutcome.Extracted, stage: HeatStage.Hunting));

            Assert.That(ValueOf(rows, "ZONE"), Is.EqualTo("HUNTING"));
        }

        [Test]
        public void EveryRunGetsASentenceOfItsOwn()
        {
            var seen = new HashSet<string>();
            foreach (MissionOutcome outcome in new[]
                     {
                         MissionOutcome.Extracted,
                         MissionOutcome.ExtractedShort,
                         MissionOutcome.SquadLost,
                     })
            {
                string line = MissionDebrief.Summary(Ended(outcome, lost: 3));
                Assert.That(line, Is.Not.Empty, $"{outcome} has nothing to say for itself");
                Assert.That(seen.Add(line), Is.True, $"{outcome} reads like another outcome");
            }
        }

        [Test]
        public void BothColumnsOfTheDebriefAreTheSameLength()
        {
            // The screen prints the labels and the values as two blocks of text side by side,
            // so a row that is in one and not the other would slide every line below it out of
            // step with its own number.
            foreach (MissionOutcome outcome in new[]
                     {
                         MissionOutcome.Extracted,
                         MissionOutcome.ExtractedShort,
                         MissionOutcome.SquadLost,
                         MissionOutcome.None,
                     })
            {
                foreach (int lost in new[] { 0, 4 })
                {
                    IReadOnlyList<DebriefRow> rows = MissionDebrief.Rows(Ended(outcome, lost: lost));
                    foreach (DebriefRow row in rows)
                    {
                        Assert.That(row.Label, Is.Not.Empty, $"{outcome} had a nameless row");
                        Assert.That(row.Value, Is.Not.Empty, $"{outcome}: '{row.Label}' said nothing");
                    }
                }
            }
        }
    }
}
