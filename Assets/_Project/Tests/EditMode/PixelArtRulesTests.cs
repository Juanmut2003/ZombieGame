#nullable enable
using NUnit.Framework;
using UnityEngine;
using ZombieGame.Editor.Importing;

namespace ZombieGame.Tests.EditMode
{
    [TestFixture]
    public sealed class PixelArtRulesTests
    {
        [TestCase("Assets/_Project/Art/Sprites/Characters/mc.png")]
        [TestCase("Assets/_Project/Art/Sprites/Tiles/floor.PNG")]
        [TestCase("Assets/_Project/Art/Sprites/Zombies/walker.aseprite")]
        [TestCase("Assets/_Project/Art/Sprites/UI/icon.psd")]
        public void SpritesInTheProjectFolderAreGoverned(string path)
        {
            Assert.That(PixelArtRules.AppliesTo(path), Is.True);
        }

        [TestCase("Assets/Plugins/SomePackage/art.png", "third-party art keeps its own settings")]
        [TestCase("Assets/_Project/Art/Palettes/palette.png", "only the Sprites folder is governed")]
        [TestCase("Assets/_Project/Art/Sprites/readme.txt", "not an image")]
        [TestCase("Assets/_Project/Scripts/Core/GameServices.cs", "not art at all")]
        [TestCase("", "no path")]
        [TestCase(null, "null path")]
        public void EverythingElseIsLeftAlone(string? path, string reason)
        {
            Assert.That(PixelArtRules.AppliesTo(path), Is.False, reason);
        }

        [Test]
        public void BackslashPathsAreUnderstood()
        {
            Assert.That(
                PixelArtRules.AppliesTo(@"Assets\_Project\Art\Sprites\Characters\mc.png"),
                Is.True,
                "Windows hands paths over with backslashes");
        }

        [TestCase("Assets/_Project/Art/Sprites/Characters/mc.png")]
        [TestCase("Assets/_Project/Art/Sprites/Zombies/walker.png")]
        [TestCase("Assets/_Project/Art/Sprites/Buildings/barracks.png")]
        public void ThingsThatStandOnTheFloorPivotAtTheirFeet(string path)
        {
            Assert.That(PixelArtRules.PivotFor(path), Is.EqualTo(SpriteAlignment.BottomCenter));
        }

        [TestCase("Assets/_Project/Art/Sprites/Tiles/floor.png")]
        [TestCase("Assets/_Project/Art/Sprites/Items/crate.png")]
        [TestCase("Assets/_Project/Art/Sprites/UI/icon.png")]
        [TestCase("Assets/_Project/Art/Sprites/FX/blood.png")]
        public void EverythingElsePivotsInTheCentre(string path)
        {
            Assert.That(PixelArtRules.PivotFor(path), Is.EqualTo(SpriteAlignment.Center));
        }

        [Test]
        public void PivotFallsBackToCentreForUnknownPaths()
        {
            Assert.That(PixelArtRules.PivotFor(null), Is.EqualTo(SpriteAlignment.Center));
            Assert.That(
                PixelArtRules.PivotFor("Assets/Plugins/whatever.png"),
                Is.EqualTo(SpriteAlignment.Center));
        }

        [Test]
        public void ASpriteDirectlyInTheRootStillGetsAPivot()
        {
            Assert.That(
                PixelArtRules.PivotFor("Assets/_Project/Art/Sprites/loose.png"),
                Is.EqualTo(SpriteAlignment.Center));
        }

        [Test]
        public void ThePixelsPerUnitMatchesTheArtDirection()
        {
            Assert.That(PixelArtRules.PixelsPerUnit, Is.EqualTo(32));
        }
    }
}
