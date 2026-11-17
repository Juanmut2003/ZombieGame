#nullable enable
using System;
using NUnit.Framework;
using UnityEngine;
using ZombieGame.Editor.Art;

namespace ZombieGame.Tests.EditMode
{
    [TestFixture]
    public sealed class PlaceholderSpriteFactoryTests
    {
        private static readonly Color32 Body = new(100, 120, 140, 255);
        private static readonly Color32 Accent = new(200, 210, 220, 255);

        private static PlaceholderSpriteSpec Spec(
            PlaceholderShape shape, int width = 24, int height = 32) =>
            new("test", width, height, shape, Body, Accent);

        private static Color32 PixelAt(Color32[] pixels, PlaceholderSpriteSpec spec, int x, int y) =>
            pixels[(y * spec.Width) + x];

        [TestCase(PlaceholderShape.Humanoid)]
        [TestCase(PlaceholderShape.Tile)]
        [TestCase(PlaceholderShape.Box)]
        public void ThePixelCountMatchesTheRequestedSize(PlaceholderShape shape)
        {
            PlaceholderSpriteSpec spec = Spec(shape);

            Assert.That(PlaceholderSpriteFactory.CreatePixels(spec), Has.Length.EqualTo(24 * 32));
        }

        [TestCase(PlaceholderShape.Humanoid)]
        [TestCase(PlaceholderShape.Tile)]
        [TestCase(PlaceholderShape.Box)]
        public void TheSameSpecAlwaysDrawsTheSamePixels(PlaceholderShape shape)
        {
            PlaceholderSpriteSpec spec = Spec(shape);

            Color32[] first = PlaceholderSpriteFactory.CreatePixels(spec);
            Color32[] second = PlaceholderSpriteFactory.CreatePixels(spec);

            Assert.That(second, Is.EqualTo(first), "generated art must be reproducible");
        }

        [Test]
        public void AHumanoidStandsOnTheBottomEdge()
        {
            PlaceholderSpriteSpec spec = Spec(PlaceholderShape.Humanoid);

            Color32[] pixels = PlaceholderSpriteFactory.CreatePixels(spec);

            Assert.That(
                PixelAt(pixels, spec, spec.Width / 2, 0).a,
                Is.EqualTo(255),
                "the feet have to touch the bottom row, that is where the pivot sits");
        }

        [Test]
        public void AHumanoidLeavesItsCornersEmpty()
        {
            PlaceholderSpriteSpec spec = Spec(PlaceholderShape.Humanoid);

            Color32[] pixels = PlaceholderSpriteFactory.CreatePixels(spec);

            Assert.That(PixelAt(pixels, spec, 0, 0).a, Is.Zero);
            Assert.That(PixelAt(pixels, spec, spec.Width - 1, 0).a, Is.Zero);
            Assert.That(PixelAt(pixels, spec, 0, spec.Height - 1).a, Is.Zero);
            Assert.That(PixelAt(pixels, spec, spec.Width - 1, spec.Height - 1).a, Is.Zero);
        }

        [Test]
        public void AHumanoidCarriesAFacingMarker()
        {
            PlaceholderSpriteSpec spec = Spec(PlaceholderShape.Humanoid);

            Color32[] pixels = PlaceholderSpriteFactory.CreatePixels(spec);

            bool hasAccent = false;
            foreach (Color32 pixel in pixels)
            {
                if (pixel.r == Accent.r && pixel.g == Accent.g && pixel.b == Accent.b && pixel.a == 255)
                {
                    hasAccent = true;
                    break;
                }
            }

            Assert.That(hasAccent, Is.True, "without it you cannot tell which way the figure faces");
        }

        [Test]
        public void ATileCoversEveryPixel()
        {
            PlaceholderSpriteSpec spec = Spec(PlaceholderShape.Tile, 32, 32);

            Color32[] pixels = PlaceholderSpriteFactory.CreatePixels(spec);

            foreach (Color32 pixel in pixels)
            {
                Assert.That(pixel.a, Is.EqualTo(255), "a floor tile with a hole in it is a bug");
            }
        }

        [Test]
        public void ATileIsPatternedSoTheGridIsVisible()
        {
            PlaceholderSpriteSpec spec = Spec(PlaceholderShape.Tile, 32, 32);

            Color32[] pixels = PlaceholderSpriteFactory.CreatePixels(spec);

            Assert.That(PixelAt(pixels, spec, 0, 0).r, Is.EqualTo(Accent.r));
            Assert.That(PixelAt(pixels, spec, 1, 1).r, Is.EqualTo(Body.r));
        }

        [Test]
        public void ABoxIsFramedOnEverySide()
        {
            PlaceholderSpriteSpec spec = Spec(PlaceholderShape.Box, 32, 32);

            Color32[] pixels = PlaceholderSpriteFactory.CreatePixels(spec);

            for (int x = 0; x < spec.Width; x++)
            {
                Assert.That(PixelAt(pixels, spec, x, 0).r, Is.EqualTo(Accent.r), $"bottom at x={x}");
                Assert.That(
                    PixelAt(pixels, spec, x, spec.Height - 1).r, Is.EqualTo(Accent.r), $"top at x={x}");
            }

            for (int y = 0; y < spec.Height; y++)
            {
                Assert.That(PixelAt(pixels, spec, 0, y).r, Is.EqualTo(Accent.r), $"left at y={y}");
                Assert.That(
                    PixelAt(pixels, spec, spec.Width - 1, y).r, Is.EqualTo(Accent.r), $"right at y={y}");
            }

            Assert.That(
                PixelAt(pixels, spec, spec.Width / 2, spec.Height / 2).r,
                Is.EqualTo(Body.r),
                "the middle stays body colour");
        }

        [Test]
        public void TinySizesDoNotFallApart()
        {
            PlaceholderSpriteSpec spec = Spec(PlaceholderShape.Humanoid, 2, 2);

            Assert.DoesNotThrow(() => PlaceholderSpriteFactory.CreatePixels(spec));
        }

        [Test]
        public void ASpecNeedsANameAndAPositiveSize()
        {
            Assert.Throws<ArgumentException>(
                () => new PlaceholderSpriteSpec(" ", 8, 8, PlaceholderShape.Box, Body, Accent));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new PlaceholderSpriteSpec("x", 0, 8, PlaceholderShape.Box, Body, Accent));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new PlaceholderSpriteSpec("x", 8, -1, PlaceholderShape.Box, Body, Accent));
        }

        [Test]
        public void TheCatalogueOnlyNamesFoldersThatExistInTheLayout()
        {
            string[] allowed = { "Characters", "Zombies", "Tiles", "Buildings", "Items", "UI", "FX" };

            foreach ((string folder, PlaceholderSpriteSpec spec) in PlaceholderSpriteGenerator.Catalogue)
            {
                Assert.That(allowed, Contains.Item(folder), $"'{spec.Name}' sits in '{folder}'");
            }
        }

        [Test]
        public void TheCatalogueHasNoDuplicateNames()
        {
            var seen = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);

            foreach ((string folder, PlaceholderSpriteSpec spec) in PlaceholderSpriteGenerator.Catalogue)
            {
                Assert.That(
                    seen.Add($"{folder}/{spec.Name}"),
                    Is.True,
                    $"'{spec.Name}' appears twice in '{folder}'");
            }
        }
    }
}
