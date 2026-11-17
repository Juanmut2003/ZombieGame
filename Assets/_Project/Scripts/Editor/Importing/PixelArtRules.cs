#nullable enable
using System;
using UnityEngine;
using ZombieGame.Core.Rendering;

namespace ZombieGame.Editor.Importing
{
    /// <summary>
    /// What pixel art is supposed to look like coming out of the importer, and which pivot each
    /// kind of sprite gets. Kept free of importer plumbing so the rules can be tested directly.
    /// </summary>
    /// <remarks>
    /// These are project conventions from CLAUDE.md, not balance values: changing them changes how
    /// every sprite renders, so they live in code and not in a designer-facing asset.
    /// </remarks>
    public static class PixelArtRules
    {
        /// <summary>
        /// Every sprite in the project is authored and rendered at this scale. Mirrors
        /// <see cref="PixelPerfectSettings.PixelsPerUnit"/>, so the importer and the camera can
        /// never drift apart.
        /// </summary>
        public const int PixelsPerUnit = PixelPerfectSettings.PixelsPerUnit;

        /// <summary>The folder these rules apply to. Anything outside is left alone.</summary>
        public const string SpriteRoot = "Assets/_Project/Art/Sprites/";

        /// <summary>
        /// True when the importer should force the pixel-art settings onto this asset.
        /// </summary>
        /// <remarks>
        /// Deliberately narrow: third-party art under <c>Assets/Plugins</c> and anything outside the
        /// sprite folder keeps whatever settings it shipped with, because silently rewriting an
        /// imported package's textures is a good way to break it.
        /// </remarks>
        public static bool AppliesTo(string? assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            string normalised = assetPath!.Replace('\\', '/');
            return normalised.StartsWith(SpriteRoot, StringComparison.Ordinal)
                   && HasImageExtension(normalised);
        }

        /// <summary>
        /// The pivot for a sprite, decided by which category folder it sits in.
        /// </summary>
        /// <remarks>
        /// Anything that stands on the ground pivots at its feet, so a character sorts and lines up
        /// against floor tiles by its contact point rather than its middle. Everything else pivots
        /// in the centre.
        /// </remarks>
        public static SpriteAlignment PivotFor(string? assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return SpriteAlignment.Center;
            }

            string normalised = assetPath!.Replace('\\', '/');
            if (!normalised.StartsWith(SpriteRoot, StringComparison.Ordinal))
            {
                return SpriteAlignment.Center;
            }

            string remainder = normalised.Substring(SpriteRoot.Length);
            int slash = remainder.IndexOf('/');
            string category = slash < 0 ? remainder : remainder.Substring(0, slash);

            return StandsOnTheGround(category) ? SpriteAlignment.BottomCenter : SpriteAlignment.Center;
        }

        /// <summary>True for categories whose sprites have feet or a footprint on the floor.</summary>
        public static bool StandsOnTheGround(string? category) =>
            string.Equals(category, "Characters", StringComparison.Ordinal)
            || string.Equals(category, "Zombies", StringComparison.Ordinal)
            || string.Equals(category, "Buildings", StringComparison.Ordinal);

        private static bool HasImageExtension(string path) =>
            path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".psd", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".psb", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".aseprite", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".ase", StringComparison.OrdinalIgnoreCase);
    }
}
