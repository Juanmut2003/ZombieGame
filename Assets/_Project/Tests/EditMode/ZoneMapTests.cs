#nullable enable
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
        private static readonly string[] Depot = ZoneMaps.Depot;

        [Test]
        public void EveryRowIsTheSameLength()
        {
            int width = Depot[0].Length;

            for (int row = 0; row < Depot.Length; row++)
            {
                Assert.That(
                    Depot[row], Has.Length.EqualTo(width), $"row {row} is a different length");
            }
        }

        [Test]
        public void TheZoneIsWalledIn()
        {
            int last = Depot.Length - 1;

            foreach (char cell in Depot[0])
            {
                Assert.That(cell, Is.EqualTo('#'), "the top edge is open");
            }

            foreach (char cell in Depot[last])
            {
                Assert.That(cell, Is.EqualTo('#'), "the bottom edge is open");
            }

            for (int row = 0; row < Depot.Length; row++)
            {
                Assert.That(Depot[row][0], Is.EqualTo('#'), $"row {row} is open on the left");
                Assert.That(
                    Depot[row][^1], Is.EqualTo('#'), $"row {row} is open on the right");
            }
        }

        [Test]
        public void TheZoneHasAStartAndAWayOut()
        {
            Assert.That(Count('P'), Is.EqualTo(1), "exactly one place to start");
            Assert.That(Count('X'), Is.EqualTo(1), "exactly one way out");
        }

        [Test]
        public void TheZoneIsWorthLooting()
        {
            // Enough containers that the objective can be met twice over: a zone where every
            // crate is mandatory is a zone with no decisions in it.
            Assert.That(Count('L'), Is.GreaterThanOrEqualTo(5));
            Assert.That(Count('Z'), Is.GreaterThanOrEqualTo(3), "and worth being careful in");
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

        private static int Count(char marker)
        {
            int found = 0;
            foreach (string row in Depot)
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
