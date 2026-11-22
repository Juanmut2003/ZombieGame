#nullable enable
using System.IO;
using NUnit.Framework;
using UnityEngine;
using ZombieGame.Editor.Art;

namespace ZombieGame.Tests.EditMode
{
    /// <summary>
    /// The MC's poses: that they are there, and that the fantasy has been taken out of them.
    /// </summary>
    /// <remarks>
    /// The figure is the archer pack's drawing with its colours moved, so the one thing that
    /// can quietly go wrong is the colours not moving — a re-import of the pack, a rebuild
    /// pointed back at the original files, a hue window edited to miss. All three leave a game
    /// that runs perfectly and stars a purple wizard, which no test of behaviour would ever
    /// catch.
    /// </remarks>
    [TestFixture]
    public sealed class SurvivorArtTests
    {
        private const string Folder = "Assets/_Project/Art/Sprites/Characters";

        private static readonly string[] Poses =
        {
            "survivor_front_walk",
            "survivor_side_left_walk",
            "survivor_side_right_walk",
        };

        /// <summary>The band of hues the archer's cloak lived in. Nothing may be left in it.</summary>
        private const float CloakFrom = 0.655f;

        /// <inheritdoc cref="CloakFrom"/>
        private const float CloakTo = 0.815f;

        [TestCaseSource(nameof(Poses))]
        public void ThePoseIsOnDisk(string pose)
        {
            Assert.That(
                File.Exists($"{Folder}/{pose}.png"),
                Is.True,
                $"'{pose}' is missing — run ZombieGame/Art/Recolour The Survivor");
        }

        [TestCaseSource(nameof(Poses))]
        public void NothingInThePoseIsStillPurple(string pose)
        {
            string path = $"{Folder}/{pose}.png";
            if (!File.Exists(path))
            {
                Assert.Ignore($"'{pose}' is not on disk yet");
                return;
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false);
            try
            {
                Assert.That(texture.LoadImage(File.ReadAllBytes(path)), Is.True, path);

                foreach (Color32 pixel in texture.GetPixels32())
                {
                    if (pixel.a == 0)
                    {
                        continue;
                    }

                    Color.RGBToHSV(
                        new Color32(pixel.r, pixel.g, pixel.b, 255),
                        out float h,
                        out float _,
                        out float _);

                    Assert.That(
                        h < CloakFrom || h > CloakTo,
                        Is.True,
                        $"{pose} still has the archer's cloak in it at hue {h:N2}");
                }
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void TheCloakIsMovedAndEverythingElseIsLeftAlone()
        {
            // A cloth pixel from the pack's own palette.
            var cloak = new Color32(0x80, 0x3C, 0xB0, 255);
            Color32 moved = SurvivorArt.Recoloured(cloak);

            Color.RGBToHSV(new Color32(moved.r, moved.g, moved.b, 255), out float hue, out float _, out float _);
            Assert.That(hue, Is.LessThan(CloakFrom).Or.GreaterThan(CloakTo));

            // Leather, skin and the gold trim are the pack artist's and stay exactly as drawn.
            foreach (Color32 kept in new[]
                     {
                         new Color32(0x96, 0x64, 0x4C, 255),
                         new Color32(0xCE, 0xB0, 0x56, 255),
                         new Color32(0x46, 0x2A, 0x16, 255),
                     })
            {
                Color32 after = SurvivorArt.Recoloured(kept);
                Assert.That(after.r, Is.EqualTo(kept.r));
                Assert.That(after.g, Is.EqualTo(kept.g));
                Assert.That(after.b, Is.EqualTo(kept.b));
            }
        }

        [Test]
        public void ThePictureKeepsItsHoles()
        {
            Color32 nothing = SurvivorArt.Recoloured(new Color32(0x80, 0x3C, 0xB0, 0));
            Assert.That(nothing.a, Is.Zero, "a transparent pixel is not cloth");
        }

        [Test]
        public void TheArcherIsStillThereToRebuildFrom()
        {
            // The recolour reads these. Deleting the pack would leave the MC unbuildable on a
            // fresh clone, which is the sort of thing nobody notices until a fresh clone.
            Assert.That(File.Exists($"{Folder}/archer_front_walk.png"), Is.True);
        }
    }
}
