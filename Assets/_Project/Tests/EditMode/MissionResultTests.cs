#nullable enable
using NUnit.Framework;
using ZombieGame.Missions;

namespace ZombieGame.Tests.EditMode
{
    /// <summary>
    /// What the debrief is allowed to say about a finished run.
    /// </summary>
    /// <remarks>
    /// The three endings are not three shades of the same thing. Getting out with everything,
    /// getting out early, and not getting out are the three outcomes the whole design of a
    /// scavenge run turns on, and a result that blurs them takes the decision out of leaving.
    /// </remarks>
    [TestFixture]
    public sealed class MissionResultTests
    {
        private static MissionEndedEvent Ended(
            MissionOutcome outcome, int banked, int lost, int wanted = 6) =>
            new("Depot Run", outcome, banked, lost, wanted, seconds: 90f, heatAtEnd: 0.5f);

        [Test]
        public void GettingOutWithEverythingCounts()
        {
            MissionEndedEvent result = Ended(MissionOutcome.Extracted, banked: 6, lost: 0);

            Assert.That(result.Extracted, Is.True);
            Assert.That(result.Banked, Is.EqualTo(6));
            Assert.That(result.Lost, Is.Zero);
        }

        [Test]
        public void LeavingEarlyIsStillGettingOut()
        {
            // PROJECT.md §8 makes extraction compulsory, not the objective. Walking out with
            // four of six is a decision the player is allowed to make, and the four come home.
            MissionEndedEvent result = Ended(MissionOutcome.ExtractedShort, banked: 4, lost: 0);

            Assert.That(result.Extracted, Is.True);
            Assert.That(result.Banked, Is.EqualTo(4));
        }

        [Test]
        public void LosingTheSquadLosesWhatItWasCarrying()
        {
            MissionEndedEvent result = Ended(MissionOutcome.SquadLost, banked: 0, lost: 5);

            Assert.That(result.Extracted, Is.False);
            Assert.That(result.Banked, Is.Zero);

            // Said out loud rather than folded into a zero: five supplies carried to the door
            // and lost there is the run worth being sore about.
            Assert.That(result.Lost, Is.EqualTo(5));
        }
    }
}
