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

        /// <summary>The pack's own face, which stands in until the generated one is built.</summary>
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
        /// The face the whole HUD is set in: Neonbyte, a 5x7 monospaced pixel font.
        /// </summary>
        /// <remarks>
        /// Falls back to the pack's font while Neonbyte has not been generated yet — see
        /// <see cref="NeonbyteFont"/> and the menu item that builds it. The fallback is
        /// proportional where Neonbyte is monospaced, so a HUD laid out for one is loose or
        /// crowded in the other; it is there so a fresh checkout has readable text, not so the
        /// two can be swapped freely.
        /// </remarks>
        internal static Font? PixelFont() =>
            AssetDatabase.LoadAssetAtPath<Font>(NeonbyteFont.FontPath)
            ?? AssetDatabase.LoadAssetAtPath<Font>(PackFontPath);

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
