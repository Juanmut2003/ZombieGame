#nullable enable
using System;
using NUnit.Framework;
using UnityEngine;
using ZombieGame.Base;

namespace ZombieGame.Tests.EditMode
{
    /// <summary>
    /// Free placement: what fits where, and what happens to the ground underneath.
    /// </summary>
    /// <remarks>
    /// All of this is invisible. A building that covers four cells but only claims one looks
    /// perfectly right until the second building is dropped half inside it, and a site that
    /// forgets to give ground back is a base that slowly fills up with nothing. Both are the
    /// sort of fault that takes an hour of play to notice and a second to assert.
    /// </remarks>
    [TestFixture]
    public sealed class BuildSiteTests
    {
        private static BuildSite Site(int width = 10, int height = 8) =>
            new(Vector2.zero, width, height);

        private static readonly BuildingFootprint OneByOne = new(1, 1);
        private static readonly BuildingFootprint TwoByThree = new(2, 3);

        [Test]
        public void ASiteNeedsGroundToBeASite()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BuildSite(Vector2.zero, 0, 4));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BuildSite(Vector2.zero, 4, -1));
        }

        [Test]
        public void EmptyGroundTakesAnything()
        {
            BuildSite site = Site();

            Assert.That(site.Check(TwoByThree, new Vector2Int(3, 2)), Is.EqualTo(PlacementVerdict.Ok));
            Assert.That(site.Buildings, Is.Empty);
        }

        [Test]
        public void NothingMayHangOffThePlot()
        {
            BuildSite site = Site(10, 8);

            // Each edge in turn, and the far corners, because an off-by-one usually only shows
            // up on one of the four sides.
            Assert.That(
                site.Check(OneByOne, new Vector2Int(-1, 4)), Is.EqualTo(PlacementVerdict.OutsideTheSite));
            Assert.That(
                site.Check(OneByOne, new Vector2Int(4, -1)), Is.EqualTo(PlacementVerdict.OutsideTheSite));
            Assert.That(
                site.Check(OneByOne, new Vector2Int(10, 4)), Is.EqualTo(PlacementVerdict.OutsideTheSite));
            Assert.That(
                site.Check(OneByOne, new Vector2Int(4, 8)), Is.EqualTo(PlacementVerdict.OutsideTheSite));

            // The last cell that is still on the plot.
            Assert.That(site.Check(OneByOne, new Vector2Int(9, 7)), Is.EqualTo(PlacementVerdict.Ok));
        }

        [Test]
        public void ABuildingIsCheckedWholeAndNotByItsCorner()
        {
            BuildSite site = Site(10, 8);

            // The corner is comfortably on the plot; the far side is not.
            Assert.That(
                site.Check(TwoByThree, new Vector2Int(9, 6)),
                Is.EqualTo(PlacementVerdict.OutsideTheSite),
                "a building was allowed to hang off the plot by most of itself");
        }

        [Test]
        public void BarredGroundSaysSo()
        {
            BuildSite site = Site();
            site.Block(4, 3);

            Assert.That(site.IsBlocked(4, 3), Is.True);
            Assert.That(
                site.Check(TwoByThree, new Vector2Int(3, 2)),
                Is.EqualTo(PlacementVerdict.GroundIsBlocked));

            // And clear of it, the same building is fine.
            Assert.That(site.Check(TwoByThree, new Vector2Int(0, 0)), Is.EqualTo(PlacementVerdict.Ok));
        }

        [Test]
        public void GroundOffThePlotCountsAsBarred()
        {
            BuildSite site = Site();

            // Asked about a cell that does not exist, the honest answer is "you cannot build
            // there", not an exception and not a cheerful yes.
            Assert.That(site.IsBlocked(-1, 0), Is.True);
            Assert.That(site.IsBlocked(0, 99), Is.True);
            Assert.That(site.OccupantAt(-1, 0), Is.Zero);
        }

        [Test]
        public void PuttingSomethingUpClaimsEveryCellOfIt()
        {
            BuildSite site = Site();
            Assert.That(site.TryPlace(TwoByThree, new Vector2Int(2, 2), out int id), Is.True);
            Assert.That(id, Is.EqualTo(1), "the first building on a site is number one");

            for (int column = 2; column < 4; column++)
            {
                for (int row = 2; row < 5; row++)
                {
                    Assert.That(
                        site.OccupantAt(column, row),
                        Is.EqualTo(id),
                        $"cell {column},{row} is under the building and does not know it");
                }
            }

            // And nothing outside it was claimed.
            Assert.That(site.OccupantAt(1, 2), Is.Zero);
            Assert.That(site.OccupantAt(4, 2), Is.Zero);
            Assert.That(site.OccupantAt(2, 5), Is.Zero);
        }

        [Test]
        public void TwoBuildingsCannotShareGround()
        {
            BuildSite site = Site();
            Assert.That(site.TryPlace(TwoByThree, new Vector2Int(2, 2), out int _), Is.True);

            Assert.That(
                site.Check(OneByOne, new Vector2Int(3, 4)),
                Is.EqualTo(PlacementVerdict.SomethingIsThere));

            Assert.That(site.TryPlace(OneByOne, new Vector2Int(3, 4), out int refused), Is.False);
            Assert.That(refused, Is.Zero, "a refused building still took a number");
            Assert.That(site.Buildings.Count, Is.EqualTo(1));
        }

        [Test]
        public void BuildingsMayStandShoulderToShoulder()
        {
            BuildSite site = Site();
            Assert.That(site.TryPlace(TwoByThree, new Vector2Int(2, 2), out int _), Is.True);

            // Hard against its right-hand side. Free placement means free, not politely spaced.
            Assert.That(site.TryPlace(TwoByThree, new Vector2Int(4, 2), out int second), Is.True);
            Assert.That(second, Is.EqualTo(2));
        }

        [Test]
        public void TakingSomethingDownGivesTheGroundBack()
        {
            BuildSite site = Site();
            site.TryPlace(TwoByThree, new Vector2Int(2, 2), out int id);

            Assert.That(site.Remove(id), Is.True);
            Assert.That(site.Buildings, Is.Empty);
            Assert.That(site.OccupantAt(3, 4), Is.Zero, "the ground is still spoken for");
            Assert.That(site.Check(TwoByThree, new Vector2Int(2, 2)), Is.EqualTo(PlacementVerdict.Ok));
        }

        [Test]
        public void TakingDownWhatIsNotThereChangesNothing()
        {
            BuildSite site = Site();
            site.TryPlace(OneByOne, new Vector2Int(1, 1), out int id);

            Assert.That(site.Remove(0), Is.False, "zero means nothing, not the first building");
            Assert.That(site.Remove(id + 99), Is.False);
            Assert.That(site.Remove(id), Is.True);
            Assert.That(site.Remove(id), Is.False, "it came down twice");
        }

        [Test]
        public void ANumberIsNeverHandedOutTwice()
        {
            BuildSite site = Site();
            site.TryPlace(OneByOne, new Vector2Int(0, 0), out int first);
            site.Remove(first);
            site.TryPlace(OneByOne, new Vector2Int(0, 0), out int second);

            // Reusing the number of something demolished is how a later system ends up feeding
            // the wrong building's workers.
            Assert.That(second, Is.Not.EqualTo(first));
        }

        [Test]
        public void TurningABuildingSwapsItsSides()
        {
            var wide = new BuildingFootprint(4, 2);
            BuildingFootprint tall = wide.Rotated();

            Assert.That(tall.Width, Is.EqualTo(2));
            Assert.That(tall.Height, Is.EqualTo(4));
            Assert.That(tall.Area, Is.EqualTo(wide.Area));
            Assert.That(tall.Rotated(), Is.EqualTo(wide), "turned twice is back where it started");
            Assert.That(wide.IsSquare, Is.False);
            Assert.That(new BuildingFootprint(3, 3).IsSquare, Is.True);
        }

        [Test]
        public void TurningABuildingCanMakeItFit()
        {
            // A corridor three cells wide and plenty long.
            BuildSite site = Site(3, 9);
            var wide = new BuildingFootprint(4, 2);

            Assert.That(
                site.Check(wide, new Vector2Int(0, 0)), Is.EqualTo(PlacementVerdict.OutsideTheSite));
            Assert.That(site.Check(wide.Rotated(), new Vector2Int(0, 0)), Is.EqualTo(PlacementVerdict.Ok));
        }

        [Test]
        public void ABuildingCannotBeSmallerThanACell()
        {
            // Nothing stands on no ground, and a zero-sized footprint would pass every check
            // ever made of it.
            var nothing = new BuildingFootprint(0, -3);
            Assert.That(nothing.Width, Is.EqualTo(1));
            Assert.That(nothing.Height, Is.EqualTo(1));
        }

        [Test]
        public void ABuildingGrowsIntoTheGroundBesideIt()
        {
            BuildSite site = Site(10, 8);
            site.TryPlace(new BuildingFootprint(3, 5), new Vector2Int(1, 1), out int id);

            // Its own cells must not count against it, or no building could ever grow by one.
            Assert.That(
                site.CheckUpgrade(id, new BuildingFootprint(4, 5)),
                Is.EqualTo(PlacementVerdict.Ok));

            Assert.That(site.TryUpgrade(id, new BuildingFootprint(4, 5)), Is.True);
            Assert.That(site.OccupantAt(4, 1), Is.EqualTo(id), "the new ground was never claimed");
            Assert.That(site.Find(id)!.Value.Level, Is.EqualTo(2));
            Assert.That(site.Find(id)!.Value.Footprint, Is.EqualTo(new BuildingFootprint(4, 5)));
        }

        [Test]
        public void TheCornerStaysWhereItWasPutDown()
        {
            BuildSite site = Site(10, 8);
            site.TryPlace(new BuildingFootprint(3, 5), new Vector2Int(2, 1), out int id);
            site.TryUpgrade(id, new BuildingFootprint(4, 6));

            // Growth is up and to the right. A corner that moved would let a building creep
            // across the plot one level at a time.
            Assert.That(site.Find(id)!.Value.Cell, Is.EqualTo(new Vector2Int(2, 1)));
            Assert.That(site.OccupantAt(1, 1), Is.Zero, "it grew the wrong way");
        }

        [Test]
        public void ABuildingHemmedInCannotGrow()
        {
            BuildSite site = Site(10, 8);
            site.TryPlace(new BuildingFootprint(3, 5), new Vector2Int(0, 0), out int first);
            site.TryPlace(new BuildingFootprint(3, 5), new Vector2Int(3, 0), out int _);

            Assert.That(
                site.CheckUpgrade(first, new BuildingFootprint(4, 5)),
                Is.EqualTo(PlacementVerdict.SomethingIsThere));

            Assert.That(site.TryUpgrade(first, new BuildingFootprint(4, 5)), Is.False);
            Assert.That(site.Find(first)!.Value.Level, Is.EqualTo(1), "it grew anyway");
        }

        [Test]
        public void ABuildingCannotGrowOffThePlot()
        {
            BuildSite site = Site(10, 8);
            site.TryPlace(new BuildingFootprint(3, 5), new Vector2Int(7, 1), out int id);

            Assert.That(
                site.CheckUpgrade(id, new BuildingFootprint(4, 5)),
                Is.EqualTo(PlacementVerdict.OutsideTheSite));
        }

        [Test]
        public void GrowingGivesUpTheGroundItNoLongerStandsOn()
        {
            BuildSite site = Site(10, 8);
            site.TryPlace(new BuildingFootprint(4, 5), new Vector2Int(0, 0), out int id);

            // A shape that is wider in one direction and shorter in the other. Without releasing
            // the old footprint first, the lost row would stay claimed forever.
            Assert.That(site.TryUpgrade(id, new BuildingFootprint(5, 4)), Is.True);
            Assert.That(site.OccupantAt(0, 4), Is.Zero, "it kept ground it no longer stands on");
            Assert.That(site.OccupantAt(4, 0), Is.EqualTo(id));
        }

        [Test]
        public void NothingThatIsNotThereCanGrow()
        {
            BuildSite site = Site();
            Assert.That(
                site.CheckUpgrade(99, new BuildingFootprint(4, 5)),
                Is.EqualTo(PlacementVerdict.OutsideTheSite));
            Assert.That(site.TryUpgrade(99, new BuildingFootprint(4, 5)), Is.False);
        }

        [Test]
        public void CellsAndCornersAgreeWithEachOther()
        {
            var site = new BuildSite(new Vector2(-5f, 2f), 10, 8);

            Assert.That(site.CellAt(new Vector2(-5f, 2f)), Is.EqualTo(new Vector2Int(0, 0)));
            Assert.That(site.CellAt(new Vector2(-4.4f, 2.9f)), Is.EqualTo(new Vector2Int(0, 0)));
            Assert.That(site.CellAt(new Vector2(-3.5f, 4.5f)), Is.EqualTo(new Vector2Int(1, 2)));

            Assert.That(site.CornerOf(new Vector2Int(1, 2)), Is.EqualTo(new Vector2(-4f, 4f)));
        }

        [Test]
        public void TheMiddleOfAnEvenBuildingSitsOnTheLineBetweenCells()
        {
            BuildSite site = Site();

            // A two-wide shed centred on a cell would be half a cell into its neighbour.
            Assert.That(
                site.CentreOf(new Vector2Int(2, 2), new BuildingFootprint(2, 2)),
                Is.EqualTo(new Vector2(3f, 3f)));

            // An odd one centres on its middle cell.
            Assert.That(
                site.CentreOf(new Vector2Int(2, 2), new BuildingFootprint(3, 3)),
                Is.EqualTo(new Vector2(3.5f, 3.5f)));
        }

        [Test]
        public void TheGhostIsHeldByItsMiddleAndNotItsCorner()
        {
            BuildSite site = Site();
            var shed = new BuildingFootprint(3, 3);

            // Pointer in the middle of cell 4,4: a three-by-three held by its middle has its
            // corner one cell down and left.
            Vector2Int corner = site.CornerUnder(new Vector2(4.5f, 4.5f), shed);
            Assert.That(corner, Is.EqualTo(new Vector2Int(3, 3)));

            // And the building really does end up under the pointer.
            Assert.That(site.CentreOf(corner, shed), Is.EqualTo(new Vector2(4.5f, 4.5f)));
        }
    }
}
