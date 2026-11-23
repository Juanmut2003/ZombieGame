#nullable enable
using NUnit.Framework;
using UnityEngine;
using ZombieGame.Characters;

namespace ZombieGame.Tests.EditMode
{
    /// <summary>
    /// Where somebody with nothing to do walks, and how they get there.
    /// </summary>
    /// <remarks>
    /// The failures worth catching are the ones that look like nothing in the editor: a survivor
    /// who strays off the plot, one who sprints across it, and one who stands at a spot
    /// shuffling because arriving was never quite true.
    /// </remarks>
    [TestFixture]
    public sealed class StrollTests
    {
        private static readonly Rect Plot = new Rect(0f, 0f, 14f, 8f);

        [Test]
        public void EverySpotIsOnTheGroundTheyAreAllowed()
        {
            for (int across = 0; across <= 10; across++)
            {
                for (int up = 0; up <= 10; up++)
                {
                    Vector2 spot = StrollMath.SpotIn(Plot, across / 10f, up / 10f);

                    Assert.That(
                        Plot.Contains(spot) || OnTheEdge(spot),
                        Is.True,
                        $"{spot} is off the plot");
                }
            }
        }

        private static bool OnTheEdge(Vector2 spot) =>
            Mathf.Approximately(spot.x, Plot.xMax) || Mathf.Approximately(spot.y, Plot.yMax);

        [Test]
        public void AFractionOutsideTheRangeStillLandsOnThePlot()
        {
            // The fractions come from a random source, and one day something will hand this a
            // number it did not expect. Straying off the plot is worse than ignoring the figure.
            Assert.That(StrollMath.SpotIn(Plot, -3f, 7f), Is.EqualTo(new Vector2(0f, 8f)));
        }

        [Test]
        public void TheMiddleOfThePlotIsTheMiddleOfThePlot()
        {
            Assert.That(StrollMath.SpotIn(Plot, 0.5f, 0.5f), Is.EqualTo(new Vector2(7f, 4f)));
        }

        [Test]
        public void ArrivingIsCloseEnoughRatherThanExact()
        {
            var spot = new Vector2(5f, 5f);

            Assert.That(StrollMath.Arrived(new Vector2(5.1f, 5f), spot, 0.25f), Is.True);
            Assert.That(StrollMath.Arrived(new Vector2(6f, 5f), spot, 0.25f), Is.False);
        }

        [Test]
        public void HeadingSomewhereIsAStrollAndNotARun()
        {
            Vector2 move = StrollMath.Toward(Vector2.zero, new Vector2(10f, 0f), 0.25f, 0.45f);

            Assert.That(move.x, Is.EqualTo(0.45f).Within(0.0001f));
            Assert.That(move.y, Is.EqualTo(0f).Within(0.0001f));

            // Below one unit is what makes the legs walk slower rather than teleport: the
            // movement maths treats a short vector as a gentler push on the stick.
            Assert.That(move.magnitude, Is.LessThan(1f));
        }

        [Test]
        public void ADiagonalStrollIsNoFasterThanAStraightOne()
        {
            Vector2 straight = StrollMath.Toward(Vector2.zero, new Vector2(10f, 0f), 0.25f, 0.5f);
            Vector2 diagonal = StrollMath.Toward(Vector2.zero, new Vector2(10f, 10f), 0.25f, 0.5f);

            Assert.That(diagonal.magnitude, Is.EqualTo(straight.magnitude).Within(0.0001f));
        }

        [Test]
        public void ArrivedMeansStandingStill()
        {
            Vector2 move = StrollMath.Toward(
                new Vector2(5f, 5f), new Vector2(5.05f, 5f), 0.25f, 0.45f);

            Assert.That(move, Is.EqualTo(Vector2.zero), "they would shuffle on the spot forever");
        }

        [Test]
        public void StrollingNeverRuns()
        {
            // A factor above one would hand the legs more than a full push and the survivor
            // would cross the plot faster than the player can.
            Vector2 move = StrollMath.Toward(Vector2.zero, new Vector2(10f, 0f), 0.25f, 4f);

            Assert.That(move.magnitude, Is.EqualTo(1f).Within(0.0001f));
        }
    }
}
