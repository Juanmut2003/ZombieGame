#nullable enable
using NUnit.Framework;
using UnityEngine;
using ZombieGame.Editor.Art;

namespace ZombieGame.Tests.EditMode
{
    /// <summary>
    /// Checks the shape of the swing arc, which is the one sprite a player reads as feedback
    /// rather than as scenery.
    /// </summary>
    [TestFixture]
    public sealed class SwingArcSpriteTests
    {
        private const int Size = 64;

        private static readonly PlaceholderSpriteSpec Arc = new(
            "swing_arc",
            Size,
            Size,
            PlaceholderShape.SwingArc,
            new Color32(214, 208, 190, 150),
            new Color32(246, 243, 232, 220));

        private static Color32 PixelAt(Color32[] pixels, int x, int y) => pixels[(y * Size) + x];

        [Test]
        public void TheApexIsEmptySoTheCharacterShowsThrough()
        {
            Color32[] pixels = PlaceholderSpriteFactory.CreatePixels(Arc);

            Assert.That(PixelAt(pixels, Size / 2, Size / 2).a, Is.Zero);
        }

        [Test]
        public void TheArcReachesForward()
        {
            Color32[] pixels = PlaceholderSpriteFactory.CreatePixels(Arc);

            // Just inside the outer radius, straight ahead of the apex.
            Assert.That(PixelAt(pixels, Size - 3, Size / 2).a, Is.GreaterThan(0));
        }

        [Test]
        public void NothingIsDrawnBehindTheSwing()
        {
            Color32[] pixels = PlaceholderSpriteFactory.CreatePixels(Arc);

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size / 2; x++)
                {
                    Assert.That(
                        PixelAt(pixels, x, y).a,
                        Is.Zero,
                        $"({x},{y}) is behind the character and should be empty");
                }
            }
        }
    }
}
