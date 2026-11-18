#nullable enable
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ZombieGame.Editor.Art
{
    /// <summary>
    /// Writes the project's placeholder sprites to disk. Run from the menu, or from a build script
    /// with <c>-executeMethod ZombieGame.Editor.Art.PlaceholderSpriteGenerator.GenerateAll</c>.
    /// </summary>
    /// <remarks>
    /// Existing files are never overwritten unless asked: once a real sprite replaces a
    /// placeholder, re-running this must not throw the art away.
    /// </remarks>
    public static class PlaceholderSpriteGenerator
    {
        private const string SpriteRoot = "Assets/_Project/Art/Sprites";

        // Muted, desaturated and plainly unfinished, in the spirit of the art direction without
        // pretending to be it.
        private static readonly Color32 SurvivorBody = new(118, 140, 160, 255);
        private static readonly Color32 SurvivorAccent = new(206, 214, 222, 255);
        private static readonly Color32 PlayerBody = new(140, 170, 128, 255);
        private static readonly Color32 PlayerAccent = new(226, 232, 214, 255);
        private static readonly Color32 ZombieBody = new(104, 122, 94, 255);
        private static readonly Color32 ZombieAccent = new(150, 68, 62, 255);
        private static readonly Color32 FloorBody = new(74, 72, 68, 255);
        private static readonly Color32 FloorAccent = new(92, 90, 84, 255);
        private static readonly Color32 WallBody = new(96, 92, 86, 255);
        private static readonly Color32 WallAccent = new(66, 63, 59, 255);
        // Pale and slightly warm, so a swing reads against both the floor and a zombie without
        // looking like a light source.
        private static readonly Color32 SwingBody = new(214, 208, 190, 150);
        private static readonly Color32 SwingAccent = new(246, 243, 232, 220);
        // Hot and bright: a round has to read against a dark floor at a glance.
        private static readonly Color32 BulletBody = new(232, 176, 72, 255);
        private static readonly Color32 BulletAccent = new(255, 240, 198, 255);
        // Washed-out green, the colour every game paints a way out in.
        private static readonly Color32 ExitBody = new(74, 104, 72, 170);
        private static readonly Color32 ExitAccent = new(132, 200, 128, 230);
        private static readonly Color32 CrateBody = new(126, 104, 74, 255);
        private static readonly Color32 CrateAccent = new(80, 64, 44, 255);

        /// <summary>
        /// Every placeholder the project ships, with the folder each one belongs in.
        /// </summary>
        public static IReadOnlyList<(string Folder, PlaceholderSpriteSpec Spec)> Catalogue { get; } =
            new List<(string, PlaceholderSpriteSpec)>
            {
                ("Characters", new PlaceholderSpriteSpec(
                    "placeholder_mc", 48, 64, PlaceholderShape.Humanoid, PlayerBody, PlayerAccent)),
                ("Characters", new PlaceholderSpriteSpec(
                    "placeholder_survivor", 48, 64, PlaceholderShape.Humanoid,
                    SurvivorBody, SurvivorAccent)),
                ("Zombies", new PlaceholderSpriteSpec(
                    "placeholder_walker", 48, 64, PlaceholderShape.Humanoid, ZombieBody, ZombieAccent)),
                ("Tiles", new PlaceholderSpriteSpec(
                    "placeholder_floor", 64, 64, PlaceholderShape.Tile, FloorBody, FloorAccent)),
                ("Tiles", new PlaceholderSpriteSpec(
                    "placeholder_wall", 64, 64, PlaceholderShape.Tile, WallBody, WallAccent)),
                ("FX", new PlaceholderSpriteSpec(
                    "swing_arc", 128, 128, PlaceholderShape.SwingArc, SwingBody, SwingAccent)),
                ("FX", new PlaceholderSpriteSpec(
                    "bullet", 24, 8, PlaceholderShape.Bullet, BulletBody, BulletAccent)),
                ("FX", new PlaceholderSpriteSpec(
                    "extraction_pad", 192, 192, PlaceholderShape.ExtractionPad, ExitBody, ExitAccent)),
                ("Items", new PlaceholderSpriteSpec(
                    "placeholder_crate", 64, 64, PlaceholderShape.Box, CrateBody, CrateAccent)),
            };

        /// <summary>Writes any placeholder that is not on disk yet.</summary>
        [MenuItem("ZombieGame/Art/Generate Missing Placeholder Sprites")]
        public static void GenerateAll() => Generate(overwrite: false);

        /// <summary>Rewrites every placeholder, replacing whatever is there.</summary>
        [MenuItem("ZombieGame/Art/Regenerate All Placeholder Sprites")]
        public static void RegenerateAll() => Generate(overwrite: true);

        /// <summary>
        /// Writes the catalogue to disk and returns the asset paths that were actually written.
        /// </summary>
        public static IReadOnlyList<string> Generate(bool overwrite)
        {
            var written = new List<string>();

            try
            {
                AssetDatabase.StartAssetEditing();

                foreach ((string folder, PlaceholderSpriteSpec spec) in Catalogue)
                {
                    string directory = Path.Combine(SpriteRoot, folder).Replace('\\', '/');
                    string assetPath = $"{directory}/{spec.Name}.png";

                    if (!overwrite && File.Exists(assetPath))
                    {
                        continue;
                    }

                    Directory.CreateDirectory(directory);
                    File.WriteAllBytes(assetPath, Encode(spec));
                    written.Add(assetPath);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            foreach (string path in written)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }

            AssetDatabase.Refresh();
            Debug.Log($"[Placeholders] wrote {written.Count} of {Catalogue.Count} sprite(s).");
            return written;
        }

        private static byte[] Encode(PlaceholderSpriteSpec spec)
        {
            var texture = new Texture2D(spec.Width, spec.Height, TextureFormat.RGBA32, mipChain: false);
            try
            {
                texture.SetPixels32(PlaceholderSpriteFactory.CreatePixels(spec));
                texture.Apply();
                return texture.EncodeToPNG();
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }
    }
}
