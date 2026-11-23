#nullable enable
using NUnit.Framework;
using UnityEngine;
using ZombieGame.Base;

namespace ZombieGame.Tests.EditMode
{
    /// <summary>
    /// A building as a room: where its walls stand, where its door is, and where its workers do.
    /// </summary>
    /// <remarks>
    /// PROJECT.md 4.2.2. The faults worth catching here are the ones that look like nothing in a
    /// screenshot: a doorway that closes when a building is improved, a wall that leaves a gap at
    /// the corner, or a post half a cell inside the wall, where a survivor sent to work would
    /// stand pressed against the furniture for ever.
    /// </remarks>
    [TestFixture]
    public sealed class BuildingRoomTests
    {
        private static readonly BuildingFootprint Barracks = new BuildingFootprint(4, 5);

        [Test]
        public void EveryBuildingHasExactlyOneDoor()
        {
            foreach (int width in new[] { 1, 2, 3, 4, 5, 6 })
            {
                var footprint = new BuildingFootprint(width, 4);
                Rect door = BuildingWalls.Doorway(footprint);

                Assert.That(door.width, Is.EqualTo(1f), $"{width} wide has no way in");
                Assert.That(
                    door.xMin,
                    Is.GreaterThanOrEqualTo(-width * 0.5f),
                    "the door is outside the building");
                Assert.That(door.xMax, Is.LessThanOrEqualTo(width * 0.5f));
            }
        }

        [Test]
        public void TheDoorIsInTheFrontWallAndInTheMiddleOfIt()
        {
            Rect door = BuildingWalls.Doorway(Barracks);

            Assert.That(door.yMin, Is.EqualTo(-2.5f), "the door is not in the front wall");

            // Four wide: the cell left of the middle, which is where the steps are drawn.
            Assert.That(door.xMin, Is.EqualTo(-1f));
            Assert.That(door.xMax, Is.EqualTo(0f));
        }

        [Test]
        public void TheFrontWallIsBuiltEitherSideOfTheDoorAndNowhereElse()
        {
            WallSegment left = BuildingWalls.Segment(Barracks, 3);
            WallSegment right = BuildingWalls.Segment(Barracks, 4);
            Rect door = BuildingWalls.Doorway(Barracks);

            // Left stretch runs from the corner to the near side of the door.
            Assert.That(left.Offset.x - (left.Size.x * 0.5f), Is.EqualTo(-2f).Within(0.001f));
            Assert.That(left.Offset.x + (left.Size.x * 0.5f), Is.EqualTo(door.xMin).Within(0.001f));

            // Right stretch runs from the far side of the door to the other corner.
            Assert.That(
                right.Offset.x - (right.Size.x * 0.5f), Is.EqualTo(door.xMax).Within(0.001f));
            Assert.That(right.Offset.x + (right.Size.x * 0.5f), Is.EqualTo(2f).Within(0.001f));
        }

        [Test]
        public void AOneWideShedIsAllDoorway()
        {
            var shed = new BuildingFootprint(1, 2);

            // Nothing either side of a door as wide as the building. Both stretches have to say
            // so rather than collapse to a box of size zero sitting in the doorway.
            Assert.That(BuildingWalls.Segment(shed, 3).Exists, Is.False);
            Assert.That(BuildingWalls.Segment(shed, 4).Exists, Is.False);
            Assert.That(BuildingWalls.Segment(shed, 0).Exists, Is.True, "it has no back");
        }

        [Test]
        public void TheWallsReachTheCornersOnEverySide()
        {
            WallSegment back = BuildingWalls.Segment(Barracks, 0);
            WallSegment left = BuildingWalls.Segment(Barracks, 1);
            WallSegment right = BuildingWalls.Segment(Barracks, 2);

            Assert.That(back.Size.x, Is.EqualTo(4f), "the back wall stops short of the corners");
            Assert.That(left.Size.y, Is.EqualTo(5f), "the left wall stops short of the corners");
            Assert.That(right.Size.y, Is.EqualTo(5f));

            // Inside faces, which is what actually keeps somebody in the room.
            Assert.That(
                back.Offset.y - (back.Size.y * 0.5f),
                Is.EqualTo(2.5f - BuildingWalls.BackThickness).Within(0.001f));
            Assert.That(
                left.Offset.x + (left.Size.x * 0.5f),
                Is.EqualTo(-2f + BuildingWalls.Thickness).Within(0.001f));
        }

        [Test]
        public void TheRoomIsTheFloorInsideTheWalls()
        {
            Rect room = BuildingWalls.Room(Barracks);

            Assert.That(room.xMin, Is.EqualTo(-2f + BuildingWalls.Thickness).Within(0.001f));
            Assert.That(room.xMax, Is.EqualTo(2f - BuildingWalls.Thickness).Within(0.001f));
            Assert.That(room.yMin, Is.EqualTo(-2.5f + BuildingWalls.Thickness).Within(0.001f));
            Assert.That(
                room.yMax, Is.EqualTo(2.5f - BuildingWalls.BackThickness).Within(0.001f));
        }

        [Test]
        public void EveryPostIsInsideTheRoom()
        {
            foreach (int posts in new[] { 1, 2, 3, 4 })
            {
                for (int slot = 0; slot < posts; slot++)
                {
                    Vector2 post = BuildingPosts.At(Barracks, slot, posts);
                    Rect room = BuildingWalls.Room(Barracks);

                    Assert.That(
                        post.x,
                        Is.GreaterThan(room.xMin).And.LessThan(room.xMax),
                        $"post {slot} of {posts} is in a wall");
                    Assert.That(post.y, Is.GreaterThanOrEqualTo(room.yMin));
                    Assert.That(post.y, Is.LessThanOrEqualTo(room.yMax));
                }
            }
        }

        [Test]
        public void TwoWorkersStandAtTwoDifferentPosts()
        {
            Vector2 first = BuildingPosts.At(Barracks, 0, 2);
            Vector2 second = BuildingPosts.At(Barracks, 1, 2);

            Assert.That(first, Is.Not.EqualTo(second), "they stand inside each other");
            Assert.That(first.x, Is.LessThan(second.x), "the posts are not in order");
            Assert.That(first.y, Is.EqualTo(second.y), "the posts are not along the same wall");
        }

        [Test]
        public void APostAskedForOutOfRangeIsStillAPost()
        {
            // A building improved down to fewer posts, or a worker numbered past the end of them:
            // better the last bench than a figure standing outside the wall.
            Vector2 post = BuildingPosts.At(Barracks, 9, 2);

            Assert.That(post, Is.EqualTo(BuildingPosts.At(Barracks, 1, 2)));
        }

        [Test]
        public void ABiggerBuildingMovesItsRoomAndItsPostsWithIt()
        {
            // What improving a barracks does. A post that stayed where the old wall was is a
            // worker standing in masonry.
            var improved = new BuildingFootprint(5, 6);

            Assert.That(
                BuildingWalls.Room(improved).width,
                Is.GreaterThan(BuildingWalls.Room(Barracks).width));
            Assert.That(
                BuildingPosts.At(improved, 0, 2).y,
                Is.GreaterThan(BuildingPosts.At(Barracks, 0, 2).y),
                "the posts did not move back with the wall");
        }
    }
}
