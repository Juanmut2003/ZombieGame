#nullable enable
using NUnit.Framework;
using ZombieGame.Editor.Scenes;

namespace ZombieGame.Tests.EditMode
{
    /// <summary>
    /// What the map says a cell is standing on.
    /// </summary>
    /// <remarks>
    /// Written after a play-test: every zombie nest and every crate came out painted as indoor
    /// floor, so the town had lone grey squares lying in the middle of its roads and gardens. The
    /// map was right and the reading of it was wrong, which is exactly the kind of fault a picture
    /// shows and a scene file hides.
    /// </remarks>
    [TestFixture]
    public sealed class ZoneGroundTests
    {
        [Test]
        public void GroundIsWhatTheMapSaysItIs()
        {
            string[] map =
            {
                "grp",
                "grp",
            };

            Assert.That(ZoneGround.Under(map, 0, 0), Is.EqualTo(ZoneGround.Grass));
            Assert.That(ZoneGround.Under(map, 1, 0), Is.EqualTo(ZoneGround.Road));
            Assert.That(ZoneGround.Under(map, 2, 0), Is.EqualTo(ZoneGround.Pavement));
        }

        [Test]
        public void ANestInTheRoadIsStandingOnRoad()
        {
            string[] map =
            {
                "rrrrr",
                "rrZrr",
                "rrrrr",
            };

            Assert.That(ZoneGround.Under(map, 2, 1), Is.EqualTo(ZoneGround.Road));
        }

        [Test]
        public void ACrateInAGardenIsStandingOnGrass()
        {
            string[] map =
            {
                "ggggg",
                "ggLgg",
                "ggggg",
            };

            Assert.That(ZoneGround.Under(map, 2, 1), Is.EqualTo(ZoneGround.Grass));
        }

        [Test]
        public void ACrateOnThePavementDoesNotStandInTheRoad()
        {
            // The awkward case: a pavement cell has a road on one side, so picking the first
            // ground found would put the crate in the carriageway.
            string[] map =
            {
                "rrppg",
                "rrLpg",
                "rrppg",
            };

            Assert.That(ZoneGround.Under(map, 2, 1), Is.EqualTo(ZoneGround.Pavement));
        }

        [Test]
        public void AStartOnANarrowPavementStaysOnThePavement()
        {
            // The town's own layout: one cell of pavement between the road and a terrace. Count
            // all eight neighbours evenly and the diagonals put the squad in the road.
            string[] map =
            {
                "rrrrr",
                "ppPpp",
                "##.##",
            };

            Assert.That(ZoneGround.Under(map, 2, 1), Is.EqualTo(ZoneGround.Pavement));
        }

        [Test]
        public void ACrateInARoomStaysIndoors()
        {
            string[] map =
            {
                "#####",
                "#...#",
                "#.L.#",
                "#...#",
                "#####",
            };

            Assert.That(ZoneGround.Under(map, 2, 2), Is.EqualTo(ZoneGround.Indoors));
        }

        [Test]
        public void AGapInAWallIsADoorway()
        {
            string[] map =
            {
                "ppppp",
                "##.##",
                "#...#",
            };

            Assert.That(ZoneGround.Under(map, 2, 1), Is.EqualTo(ZoneGround.Doorway));
            Assert.That(ZoneGround.Under(map, 2, 2), Is.EqualTo(ZoneGround.Indoors));
        }

        [Test]
        public void OnlyTheSideYouCanSeeGetsAHouseFront()
        {
            string[] map =
            {
                "ppppp",
                "#####",
                "#...#",
                "#####",
                "ppppp",
            };

            // The bottom wall faces the street: that is the frontage.
            Assert.That(ZoneGround.IsFrontWall(map, 2, 3), Is.True);

            // The top wall has the pavement on the wrong side of it.
            Assert.That(ZoneGround.IsFrontWall(map, 2, 1), Is.False);
        }

        [Test]
        public void AGardenWallIsNobodysFrontage()
        {
            // Open on both sides, so there is no building behind it. A house front here would
            // stand in a field with nothing to belong to.
            string[] map =
            {
                "ggggg",
                "#####",
                "ggggg",
            };

            Assert.That(ZoneGround.IsFrontWall(map, 2, 1), Is.False);
        }

        [Test]
        public void TheMiddleOfAStreetCarriesTheLine()
        {
            string[] map =
            {
                "ggrrrgg",
                "ggrrrgg",
                "ggrrrgg",
                "ggrrrgg",
                "ggrrrgg",
            };

            Assert.That(ZoneGround.IsStreetCentre(map, 3, 2, out bool vertical), Is.True);
            Assert.That(vertical, Is.True, "a street running down the map");
            Assert.That(ZoneGround.IsStreetCentre(map, 2, 2, out _), Is.False, "the kerb side");
        }

        [Test]
        public void ACrossingCarriesNoLineAtAll()
        {
            string[] map =
            {
                "ggrrrgg",
                "rrrrrrr",
                "rrrrrrr",
                "rrrrrrr",
                "ggrrrgg",
            };

            // Both readings hold at once in a junction, and a line painted through one reads as
            // two streets drawn over each other.
            Assert.That(ZoneGround.IsStreetCentre(map, 3, 2, out _), Is.False);
        }
    }
}
