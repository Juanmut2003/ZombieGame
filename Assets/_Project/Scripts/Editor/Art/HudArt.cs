#nullable enable
using UnityEditor;
using UnityEngine;

namespace ZombieGame.Editor.Art
{
    /// <summary>
    /// The HUD pieces from the Undead Survivor pack: panels, bar tracks, fills, item icons and a
    /// pixel font.
    /// </summary>
    /// <remarks>
    /// The sheet's panels and tracks carry nine-slice borders, which is what lets an eighteen
    /// pixel square stretch into a bar two hundred wide without its corners smearing. That only
    /// works if the canvas is told the art's own scale — see <c>UiSceneParts.CreateCanvas</c>.
    /// Everything returns null when the pack is missing, and the HUD falls back to the flat
    /// rectangles it was built from.
    /// </remarks>
    internal static class HudArt
    {
        private const string Sheet = "Assets/Plugins/Undead Survivor/Sprites/UI.png";

        /// <summary>The face the HUD is set in: the pack's own pixel font.</summary>
        private const string PackFontPath = "Assets/Plugins/Undead Survivor/Fonts/neodgm.ttf";

        /// <summary>How many pixels of the sheet make up one unit. Its nine-slice borders are in these.</summary>
        public const float PixelsPerUnit = 18f;

        /// <summary>The frame behind a hotbar slot or a panel.</summary>
        internal static Sprite? Panel() => Named("Panel");

        /// <summary>The empty part of a bar.</summary>
        internal static Sprite? BarTrack() => Named("Back 0");

        /// <summary>The filled part of a bar.</summary>
        internal static Sprite? BarFill() => Named("Front 0");

        /// <summary>One of the pack's framed item icons, numbered as the sheet names them.</summary>
        internal static Sprite? Icon(int index) => Named($"Select {index}");

        /// <summary>
        /// The face the whole HUD is set in.
        /// </summary>
        /// <remarks>
        /// Returns null when the pack is not installed, and every caller falls back to a system
        /// face so that a checkout without the pack still has readable text. It is a fallback
        /// and not an option: a HUD drawn at 64 pixels to the tile and labelled in a smooth
        /// face reads as two games at once.
        /// </remarks>
        internal static Font? PixelFont() =>
            AssetDatabase.LoadAssetAtPath<Font>(PackFontPath);

        private static Sprite? Named(string spriteName)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(Sheet))
            {
                if (asset is Sprite sprite && sprite.name == spriteName)
                {
                    return sprite;
                }
            }

            return null;
        }
    }
}
