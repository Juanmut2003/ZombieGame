#nullable enable
using NUnit.Framework;
using UnityEngine;
using ZombieGame.Core.Physics;

namespace ZombieGame.Tests.EditMode
{
    /// <summary>
    /// Whether the horde can find its way round a corner.
    /// </summary>
    /// <remarks>
    /// Until this existed a zombie walked at you in a straight line and stopped dead against
    /// whatever was in between. From the far side of a wall that reads as a corpse having a
    /// quiet moment rather than as something hunting you, and once they could kill you it was
    /// the difference between a game that is hard and one that is broken.
    /// The shapes below are the ones that catch it: a wall across the middle, a dead end, and a
    /// corner tight enough that cutting it would mean walking through the bricks.
    /// </remarks>
    [TestFixture]
    public sealed class FlowFieldTests
    {
        /// <summary>Builds a field over a drawn room. <c>#</c> is wall, everything else is open.</summary>
        /// <remarks>Rows read downwards the way they are written, as in <c>ZoneMaps</c>.</remarks>
        private static GridFlowField Field(string[] rows)
        {
            int height = rows.Length;
            int width = rows[0].Length;
            var walls = new WallGrid(Vector2.zero, width, height);

            for (int row = 0; row < height; row++)
            {
                for (int column = 0; column < width; column++)
                {
                    if (rows[height - 1 - row][column] == '#')
                    {
                        walls.SetSolid(column, row);
                    }
                }
            }

            var field = new GridFlowField(Vector2.zero, width, height);
            field.ReadWalls(walls);
            return field;
        }

        /// <summary>The middle of the cell at a column and a row, counting from the bottom.</summary>
        private static Vector2 Cell(int column, int row) => new(column + 0.5f, row + 0.5f);

        [Test]
        public void InTheOpenItPointsStraightAtTheGoal()
        {
            GridFlowField field = Field(new[]
            {
                ".......",
                ".......",
                ".......",
            });

            Assert.That(field.Rebuild(Cell(6, 1)), Is.True);

            Vector2 step = field.Direction(Cell(0, 1));

            Assert.That(step.x, Is.GreaterThan(0.5f));
            Assert.That(step.y, Is.EqualTo(0f).Within(0.3f));
        }

        [Test]
        public void ItGoesRoundAWallRatherThanIntoIt()
        {
            // A wall across the middle with the only gap at the top. Walking at the goal means
            // walking into bricks; the route is up and over.
            GridFlowField field = Field(new[]
            {
                ".......",
                "...#...",
                "...#...",
                "...#...",
                "...#...",
            });

            Assert.That(field.Rebuild(Cell(6, 0)), Is.True);

            Vector2 step = field.Direction(Cell(0, 0));

            Assert.That(step.y, Is.GreaterThan(0.3f), "the way out is upwards, not through");
        }

        [Test]
        public void EveryOpenCellKnowsTheWayOut()
        {
            GridFlowField field = Field(new[]
            {
                "#########",
                "#...#...#",
                "#...#...#",
                "#...#...#",
                "#.......#",
                "#########",
            });

            field.Rebuild(Cell(7, 1));

            // Two rooms joined along the bottom: every open cell in both of them has a route.
            Assert.That(field.Reaches(Cell(1, 4)), Is.True);
            Assert.That(field.Reaches(Cell(7, 4)), Is.True);
            Assert.That(field.Reaches(Cell(2, 1)), Is.True);
        }

        [Test]
        public void SomewhereWalledOffHasNoRouteAtAll()
        {
            GridFlowField field = Field(new[]
            {
                "#########",
                "#...#..X#",
                "#...#####",
                "#.......#",
                "#########",
            });

            field.Rebuild(Cell(1, 1));

            // The cell marked X is sealed off. Saying so is what lets a zombie give up instead
            // of pressing itself against the wall for ever.
            Assert.That(field.Reaches(Cell(7, 3)), Is.False);
            Assert.That(field.Direction(Cell(7, 3)), Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void ItNeverCutsACorner()
        {
            // The goal sits diagonally next door, and both cells between it and here are wall.
            // Taking the diagonal would be walking through the corner of a building; the way
            // there is the long way round.
            GridFlowField field = Field(new[]
            {
                ".....",
                ".....",
                ".#...",
                "..#..",
                ".....",
            });

            field.Rebuild(Cell(2, 2));
            Vector2 step = field.Direction(Cell(1, 1));

            // There is a route — up the middle — but not through the corner itself.
            Assert.That(step, Is.Not.EqualTo(Vector2.zero));
            Assert.That(
                Vector2.Dot(step, new Vector2(1f, 1f).normalized),
                Is.LessThan(0.99f),
                "a diagonal between two walls is not a step");
        }

        [Test]
        public void AGoalInsideAWallIsNoGoal()
        {
            GridFlowField field = Field(new[]
            {
                "###",
                "#.#",
                "###",
            });

            Assert.That(field.Rebuild(Cell(0, 0)), Is.False);
            Assert.That(field.Reaches(Cell(1, 1)), Is.False);
        }

        [Test]
        public void StandingOnTheGoalIsNowhereToGo()
        {
            GridFlowField field = Field(new[] { "....." });

            field.Rebuild(Cell(2, 0));

            Assert.That(field.Direction(Cell(2, 0)), Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void ARebuildReplacesTheLastOne()
        {
            GridFlowField field = Field(new[]
            {
                ".......",
            });

            field.Rebuild(Cell(0, 0));
            Assert.That(field.Direction(Cell(3, 0)).x, Is.LessThan(0f));

            field.Rebuild(Cell(6, 0));
            Assert.That(
                field.Direction(Cell(3, 0)).x,
                Is.GreaterThan(0f),
                "a field pointed somewhere new must forget where it pointed before");
        }
    }
}
