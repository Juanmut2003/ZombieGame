#nullable enable
using System.Collections.Generic;
using NUnit.Framework;
using ZombieGame.Editor.Scenes;

namespace ZombieGame.Tests.EditMode
{
    /// <summary>
    /// The drawn maps, checked as drawings.
    /// </summary>
    /// <remarks>
    /// A zone is twenty lines of text, which makes it easy to edit and easy to break: one short
    /// row and the right-hand wall has a hole in it that nobody sees until a zombie walks out of
    /// the world. These are the checks a person cannot do by counting characters.
    /// </remarks>
    [TestFixture]
    public sealed class ZoneMapTests
    {
        /// <summary>Every drawn zone, so a new one is checked from the minute it exists.</summary>
        private static string[][] EveryMap() => new[] { ZoneMaps.Town, ZoneMaps.Depot };

        private static readonly string[] Depot = ZoneMaps.Depot;

        [TestCaseSource(nameof(EveryMap))]
        public void EveryRowIsTheSameLength(string[] map)
        {
            int width = map[0].Length;

            for (int row = 0; row < map.Length; row++)
            {
                Assert.That(
                    map[row], Has.Length.EqualTo(width), $"row {row} is a different length");
            }
        }

        [TestCaseSource(nameof(EveryMap))]
        public void EveryMapIsWalledIn(string[] map)
        {
            for (int row = 0; row < map.Length; row++)
            {
                bool edge = row == 0 || row == map.Length - 1;
                foreach (char cell in edge ? map[row] : $"{map[row][0]}{map[row][^1]}")
                {
                    Assert.That(cell, Is.EqualTo('#'), $"row {row} is open");
                }
            }
        }

        [TestCaseSource(nameof(EveryMap))]
        public void EveryMapHasOneStartAndOneWayOut(string[] map)
        {
            Assert.That(CountIn(map, 'P'), Is.EqualTo(1), "exactly one place to start");
            Assert.That(CountIn(map, 'X'), Is.GreaterThanOrEqualTo(1), "somewhere to get out");
        }

        [TestCaseSource(nameof(EveryMap))]
        public void EveryMapIsWorthWalkingThrough(string[] map)
        {
            Assert.That(CountIn(map, 'L'), Is.GreaterThanOrEqualTo(5));
            Assert.That(CountIn(map, 'Z'), Is.GreaterThanOrEqualTo(3));
        }

        [Test]
        public void TheTownHoldsEnoughForItsObjective()
        {
            // Ten supplies at two to a container, and enough spare that the run is a choice of
            // route rather than a tour of every building in town.
            Assert.That(CountIn(ZoneMaps.Town, 'L'), Is.GreaterThanOrEqualTo(10));
        }

        [TestCaseSource(nameof(EveryMap))]
        public void EverythingWorthWalkingToCanBeWalkedTo(string[] map)
        {
            // The check a person cannot do by eye: a wall drawn one cell too far turns a room
            // into a sealed box, and the only sign of it is a container nobody can ever reach.
            bool[,] reached = Flood(map);

            for (int row = 0; row < map.Length; row++)
            {
                for (int column = 0; column < map[row].Length; column++)
                {
                    char cell = map[row][column];
                    if (cell != 'L' && cell != 'X')
                    {
                        continue;
                    }

                    Assert.That(
                        reached[row, column],
                        Is.True,
                        $"the {(cell == 'L' ? "container" : "way out")} at {column},{row} is walled in");
                }
            }
        }

        /// <summary>Everywhere a character can walk to from the start of a map.</summary>
        private static bool[,] Flood(string[] map)
        {
            int height = map.Length, width = map[0].Length;
            var reached = new bool[height, width];
            var queue = new Queue<(int Row, int Column)>();

            for (int row = 0; row < height; row++)
            {
                int column = map[row].IndexOf('P');
                if (column < 0)
                {
                    continue;
                }

                reached[row, column] = true;
                queue.Enqueue((row, column));
            }

            Assert.That(queue, Is.Not.Empty, "a map with nowhere to start");

            while (queue.Count > 0)
            {
                (int row, int column) = queue.Dequeue();
                foreach ((int dr, int dc) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                {
                    int nextRow = row + dr, nextColumn = column + dc;
                    if (nextRow < 0 || nextRow >= height || nextColumn < 0 || nextColumn >= width)
                    {
                        continue;
                    }

                    // Scrub is cover, not a barrier: it can be pushed through, and a route that
                    // goes through a hedge is still a route. A wall, a wreck and a fence are all
                    // solid, whatever they look like.
                    char cell = map[nextRow][nextColumn];
                    if (reached[nextRow, nextColumn] || cell is '#' or 'v' or 'f')
                    {
                        continue;
                    }

                    reached[nextRow, nextColumn] = true;
                    queue.Enqueue((nextRow, nextColumn));
                }
            }

            return reached;
        }

        [Test]
        public void TheWayOutIsNotNextToTheWayIn()
        {
            (int Row, int Column) start = Find('P');
            (int Row, int Column) exit = Find('X');

            int distance = System.Math.Abs(start.Row - exit.Row)
                           + System.Math.Abs(start.Column - exit.Column);

            // Otherwise the whole mission is two steps long.
            Assert.That(distance, Is.GreaterThan(15));
        }

        [Test]
        public void AnEmptyRoomIsAClosedBox()
        {
            string[] room = ZoneMaps.EmptyRoom(6, 4);

            Assert.That(room, Has.Length.EqualTo(4));
            Assert.That(room[0], Is.EqualTo("######"));
            Assert.That(room[1], Is.EqualTo("#....#"));
            Assert.That(room[^1], Is.EqualTo("######"));
        }

        private static int CountIn(string[] map, char marker)
        {
            int found = 0;
            foreach (string row in map)
            {
                foreach (char cell in row)
                {
                    if (cell == marker)
                    {
                        found++;
                    }
                }
            }

            return found;
        }

        private static (int Row, int Column) Find(char marker)
        {
            for (int row = 0; row < Depot.Length; row++)
            {
                int column = Depot[row].IndexOf(marker);
                if (column >= 0)
                {
                    return (row, column);
                }
            }

            return (-1, -1);
        }
    }
}
