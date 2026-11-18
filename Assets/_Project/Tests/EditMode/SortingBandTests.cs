#nullable enable
using NUnit.Framework;
using ZombieGame.Core.Rendering;

namespace ZombieGame.Tests.EditMode
{
    /// <summary>
    /// Where things draw, relative to each other.
    /// </summary>
    /// <remarks>
    /// Written after a zombie standing in a house turned out to be drawn underneath its floor:
    /// the horde sorted by how far down the screen it stood, the tilemaps sorted from zero up,
    /// and a body five metres up the map scored −40 and vanished into the ground. Invisible and
    /// still biting, which is the worst way for a bug to present itself.
    /// </remarks>
    [TestFixture]
    public sealed class SortingBandTests
    {
        [TestCase(-40f)]
        [TestCase(-5f)]
        [TestCase(0f)]
        [TestCase(5f)]
        [TestCase(40f)]
        [TestCase(4000f)]
        public void NothingStandingOnTheGroundEverDrawsUnderIt(float y)
        {
            Assert.That(SortingBands.ForGround(y), Is.GreaterThan(SortingBands.Scrub));
        }

        [TestCase(-4000f)]
        [TestCase(4000f)]
        public void NothingEverDrawsOverWhatIsFlying(float y)
        {
            Assert.That(SortingBands.ForGround(y), Is.LessThan(SortingBands.Projectile));
        }

        [Test]
        public void WhatIsFurtherDownTheScreenDrawsInFront()
        {
            Assert.That(
                SortingBands.ForGround(-2f), Is.GreaterThan(SortingBands.ForGround(3f)));
        }

        [Test]
        public void ATenthOfAUnitIsEnoughToTellTwoApart()
        {
            // Finer than any crowd ever gets, so two bodies never flicker over each other.
            Assert.That(
                SortingBands.ForGround(0.1f), Is.Not.EqualTo(SortingBands.ForGround(0f)));
        }

        [Test]
        public void TheGroundSortsBeneathEverything()
        {
            Assert.That(SortingBands.Floor, Is.LessThan(SortingBands.Markings));
            Assert.That(SortingBands.Markings, Is.LessThan(SortingBands.Walls));
            Assert.That(SortingBands.Walls, Is.LessThan(SortingBands.Scrub));
            Assert.That(SortingBands.Scrub, Is.LessThan(SortingBands.ActorLowest));
        }
    }
}
