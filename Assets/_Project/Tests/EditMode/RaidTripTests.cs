#nullable enable
using System.Collections.Generic;
using NUnit.Framework;
using ZombieGame.Economy;

namespace ZombieGame.Tests.EditMode
{
    /// <summary>
    /// The handshake between a base and a zone.
    /// </summary>
    /// <remarks>
    /// A handful of fields, and three ways to get them wrong that all cost the player something
    /// real: a zone played from the menu paying a colony that never sent it, a haul read twice
    /// and paid twice, and a squad that comes home to the wrong scene.
    /// </remarks>
    [TestFixture]
    public sealed class RaidTripTests
    {
        private static ResourceAmount[] Haul(int materials, int food = 0, int water = 0) =>
            new[]
            {
                new ResourceAmount(ResourceKind.Materials, materials),
                new ResourceAmount(ResourceKind.Food, food),
                new ResourceAmount(ResourceKind.Water, water),
            };

        [Test]
        public void NobodyIsOutToBeginWith()
        {
            var trip = new RaidTrip();

            Assert.That(trip.IsOut, Is.False);
            Assert.That(trip.HasReport, Is.False);
            Assert.That(trip.HomeScene, Is.Empty);
            Assert.That(trip.Collect(), Is.Empty);
        }

        [Test]
        public void ASquadThatWentOutComesHomeWithWhatTheZoneBanked()
        {
            var trip = new RaidTrip();
            trip.Leave("Base");

            Assert.That(trip.IsOut, Is.True);
            Assert.That(trip.HomeScene, Is.EqualTo("Base"));

            trip.Report("Town Run", Haul(materials: 6, food: 4));

            Assert.That(trip.HasReport, Is.True);
            Assert.That(trip.Hauled, Is.EqualTo(10), "the total is what the objective counts");
            Assert.That(trip.ZoneName, Is.EqualTo("Town Run"));

            IReadOnlyList<ResourceAmount> home = trip.Collect();
            Assert.That(home.Count, Is.EqualTo(2), "an empty store came home as a line");
            Assert.That(ResourceNames.Line(home), Is.EqualTo("6 MAT  4 FOOD"));
        }

        [Test]
        public void AHaulIsPaidOnceAndOnlyOnce()
        {
            var trip = new RaidTrip();
            trip.Leave("Base");
            trip.Report("Town Run", Haul(materials: 7));

            Assert.That(ResourceNames.Total(trip.Collect()), Is.EqualTo(7));
            Assert.That(trip.Collect(), Is.Empty, "the colony was paid twice for one run");
            Assert.That(trip.IsOut, Is.False, "the trip never closed");
        }

        [Test]
        public void AZonePlayedOnItsOwnPaysNobody()
        {
            var trip = new RaidTrip();
            trip.Report("Depot Run", Haul(materials: 6));

            Assert.That(trip.HasReport, Is.False);
            Assert.That(trip.Hauled, Is.Zero);
            Assert.That(trip.Collect(), Is.Empty);
        }

        [Test]
        public void ARunThatWentBadlyStillCloses()
        {
            var trip = new RaidTrip();
            trip.Leave("Base");
            trip.Report("Town Run", System.Array.Empty<ResourceAmount>());

            Assert.That(trip.HasReport, Is.True, "the base will wait for a squad that is gone");
            Assert.That(trip.Collect(), Is.Empty);
            Assert.That(trip.IsOut, Is.False);
        }

        [Test]
        public void LeavingAgainForgetsTheLastTrip()
        {
            var trip = new RaidTrip();
            trip.Leave("Base");
            trip.Report("Town Run", Haul(materials: 9));
            trip.Leave("Base");

            Assert.That(trip.HasReport, Is.False, "yesterday's haul would be paid again");
            Assert.That(trip.Hauled, Is.Zero);
            Assert.That(trip.Haul, Is.Empty);
        }
    }
}
