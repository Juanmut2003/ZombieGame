#nullable enable
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ZombieGame.Base;

namespace ZombieGame.Tests.EditMode
{
    /// <summary>
    /// Finding a way across the plot that goes round the walls.
    /// </summary>
    /// <remarks>
    /// Written after a survivor sent to the farm from the far side of it walked into the back
    /// wall and stood there pushing. The faults to catch are a route that crosses a wall, a
    /// route that refuses a way that is plainly there, and a route into a room that does not go
    /// through the door.
    /// </remarks>
    [TestFixture]
    public sealed class PlotRouteTests
    {
        private static readonly Rect Plot = new Rect(-0.5f, -0.5f, 14f, 8f);

        private readonly List<Vector2> _route = new();

        private static List<Rect> WallsOf(BuildingFootprint footprint, Vector2 middle)
        {
            var walls = new List<Rect>();
            BuildingWalls.AppendTo(walls, footprint, middle);
            return walls;
        }

        /// <summary>Whether any leg of a route crosses a wall. The whole point of the thing.</summary>
        private bool Clear(Vector2 from, IReadOnlyList<Rect> walls)
        {
            Vector2 at = from;
            foreach (Vector2 leg in _route)
            {
                if (!PlotRoutes.Open(at, leg, walls))
                {
                    return false;
                }

                at = leg;
            }

            return true;
        }

        [Test]
        public void AClearPlotIsCrossedWithoutDetours()
        {
            var nothing = new List<Rect>();

            Assert.That(
                PlotRoutes.TryFind(new Vector2(1f, 1f), new Vector2(9f, 1f), Plot, nothing, _route),
                Is.True);

            // A straight run is one leg and the spot at the end of it, not eight steps.
            Assert.That(_route, Has.Count.LessThanOrEqualTo(2));
            Assert.That(_route[_route.Count - 1], Is.EqualTo(new Vector2(9f, 1f)));
        }

        [Test]
        public void StandingOnTheSpotIsNoJourney()
        {
            var nothing = new List<Rect>();
            var spot = new Vector2(4.2f, 3.1f);

            Assert.That(PlotRoutes.TryFind(spot, spot, Plot, nothing, _route), Is.True);
            Assert.That(_route, Has.Count.EqualTo(1));
            Assert.That(_route[0], Is.EqualTo(spot));
        }

        [Test]
        public void AWallIsWalkedRoundAndNeverThrough()
        {
            // A fence straight across the middle of the plot with a gap at the right-hand end.
            var walls = new List<Rect> { new Rect(-0.5f, 3f, 11f, 0.25f) };
            var from = new Vector2(1f, 1f);
            var to = new Vector2(1f, 6f);

            Assert.That(PlotRoutes.TryFind(from, to, Plot, walls, _route), Is.True);
            Assert.That(Clear(from, walls), Is.True, "a leg of the route goes through the fence");

            // It had to go the long way round, so it cannot be two legs.
            Assert.That(_route, Has.Count.GreaterThan(2), "it walked straight through");
            Assert.That(_route[_route.Count - 1], Is.EqualTo(to));
        }

        [Test]
        public void TheWayIntoARoomGoesThroughItsDoor()
        {
            var footprint = new BuildingFootprint(4, 5);
            var middle = new Vector2(5.5f, 4.5f);
            List<Rect> walls = WallsOf(footprint, middle);

            Vector2 post = middle + BuildingPosts.At(footprint, 0, 2);
            var from = new Vector2(11f, 6f);

            Assert.That(PlotRoutes.TryFind(from, post, Plot, walls, _route), Is.True);
            Assert.That(Clear(from, walls), Is.True, "they walked in through a wall");

            // Through the gap in the front wall, which means the route dips below the building
            // and comes up into it rather than going over the top.
            Rect door = BuildingWalls.Doorway(footprint);
            float doorX = middle.x + door.center.x;
            bool throughTheDoor = false;
            foreach (Vector2 leg in _route)
            {
                throughTheDoor |= Mathf.Abs(leg.x - doorX) < 0.51f
                                  && leg.y < middle.y - (footprint.Height * 0.5f) + 1.01f;
            }

            Assert.That(throughTheDoor, Is.True, $"nothing in {Printed()} is near the door");
        }

        [Test]
        public void AWayOutOfTheRoomIsTheSameWayBack()
        {
            var footprint = new BuildingFootprint(4, 5);
            var middle = new Vector2(5.5f, 4.5f);
            List<Rect> walls = WallsOf(footprint, middle);

            Vector2 post = middle + BuildingPosts.At(footprint, 0, 1);

            Assert.That(
                PlotRoutes.TryFind(post, new Vector2(11f, 1f), Plot, walls, _route), Is.True);
            Assert.That(Clear(post, walls), Is.True, "they left through a wall");
        }

        [Test]
        public void SomewhereWalledInIsSaidToBeUnreachable()
        {
            // A box with no door at all, with the goal inside it. Better a plain no than a
            // figure walking at a wall for the rest of the day.
            var walls = new List<Rect>
            {
                new Rect(3f, 3f, 4f, 0.25f),
                new Rect(3f, 6f, 4f, 0.25f),
                new Rect(3f, 3f, 0.25f, 3.25f),
                new Rect(6.75f, 3f, 0.25f, 3.25f),
            };

            Assert.That(
                PlotRoutes.TryFind(new Vector2(1f, 1f), new Vector2(5f, 5f), Plot, walls, _route),
                Is.False);
            Assert.That(_route, Is.Empty);
        }

        [Test]
        public void WalkingAlongAWallIsNotWalkingThroughIt()
        {
            // Touching an edge is not crossing it: walls sit on cell boundaries, and a figure
            // walking down the outside of one would otherwise be told there is no way past.
            var wall = new List<Rect> { new Rect(1f, 0f, 0.25f, 6f) };

            Assert.That(
                PlotRoutes.Open(new Vector2(0.5f, 1f), new Vector2(0.5f, 2f), wall),
                Is.True,
                "walking beside a wall counts as walking through it");
            Assert.That(
                PlotRoutes.Open(new Vector2(0.5f, 1f), new Vector2(1.5f, 1f), wall),
                Is.False,
                "walking across a wall counts as clear");
        }

        private string Printed() => string.Join(" ", _route);
    }
}
