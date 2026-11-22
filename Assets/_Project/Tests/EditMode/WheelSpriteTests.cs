#nullable enable
using NUnit.Framework;
using UnityEngine;
using ZombieGame.Editor.Art;

namespace ZombieGame.Tests.EditMode
{
    /// <summary>
    /// The pieces the building wheel is drawn from.
    /// </summary>
    /// <remarks>
    /// Four quarters rotated into place only line up if the one they are rotated from is
    /// exactly a quarter, pointing exactly up, around exactly the middle of its texture. None
    /// of that is visible in a code review and all of it is obvious once it is wrong.
    /// </remarks>
    [TestFixture]
    public sealed class WheelSpriteTests
    {
        private const int Size = 64;

        private static readonly Color32 Body = new(30, 40, 35, 230);
        private static readonly Color32 Edge = new(120, 140, 120, 255);

        private static Color32[] Wedge() => PlaceholderSpriteFactory.CreatePixels(
            new PlaceholderSpriteSpec(
                "wedge", Size, Size, PlaceholderShape.WheelWedge, Body, Edge));

        private static Color32 At(Color32[] pixels, int x, int y) => pixels[(y * Size) + x];

        private static bool Painted(Color32[] pixels, int x, int y) => At(pixels, x, y).a > 0;

        [Test]
        public void TheWedgePointsUp()
        {
            Color32[] pixels = Wedge();
            const int middle = Size / 2;

            // Three quarters of the way up the middle column is inside the ring.
            Assert.That(Painted(pixels, middle, middle + 24), Is.True, "nothing above the hub");
            Assert.That(Painted(pixels, middle, middle - 24), Is.False, "the wedge faces down");
            Assert.That(Painted(pixels, middle + 24, middle), Is.False, "the wedge faces right");
            Assert.That(Painted(pixels, middle - 24, middle), Is.False, "the wedge faces left");
        }

        [Test]
        public void TheMiddleIsLeftOpenForTheBuilding()
        {
            Color32[] pixels = Wedge();
            const int middle = Size / 2;

            Assert.That(Painted(pixels, middle, middle), Is.False);
            Assert.That(Painted(pixels, middle, middle + 4), Is.False, "the hole is too small");
        }

        [Test]
        public void TheWedgeLeavesASeamForTheOneBesideIt()
        {
            Color32[] pixels = Wedge();
            const int middle = Size / 2;

            // Exactly on the diagonal, where two neighbouring slices would otherwise touch.
            Assert.That(Painted(pixels, middle + 20, middle + 20), Is.False);
        }

        [Test]
        public void TheWedgeHasAnEdgeAllTheWayRound()
        {
            Color32[] pixels = Wedge();
            const int middle = Size / 2;

            // Just inside the outer radius, straight up: the rim, not the fill.
            Assert.That(At(pixels, middle, Size - 2).a, Is.GreaterThan(0));
            Assert.That(At(pixels, middle, Size - 2).r, Is.EqualTo(Edge.r));

            // And down the straight sides, a good way in from the rim, where an angle-based
            // edge would have thinned out to nothing.
            Assert.That(At(pixels, 44, 47).r, Is.EqualTo(Edge.r), "the right side has no line");
            Assert.That(At(pixels, 19, 47).r, Is.EqualTo(Edge.r), "the left side has no line");
        }

        [Test]
        public void TheHubIsARoundPlateWithARing()
        {
            Color32[] pixels = PlaceholderSpriteFactory.CreatePixels(
                new PlaceholderSpriteSpec("hub", Size, Size, PlaceholderShape.WheelHub, Body, Edge));
            const int middle = Size / 2;

            Assert.That(At(pixels, middle, middle).r, Is.EqualTo(Body.r), "the middle is not solid");
            Assert.That(At(pixels, middle, Size - 2).r, Is.EqualTo(Edge.r), "no ring");
            Assert.That(At(pixels, 1, 1).a, Is.Zero, "the corners are not round");
        }
    }
}
