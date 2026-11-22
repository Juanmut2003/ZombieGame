#nullable enable
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ZombieGame.Editor.Art
{
    /// <summary>
    /// Turns the archer pack's hooded figure into somebody who belongs in this game.
    /// </summary>
    /// <remarks>
    /// The MC was a fantasy ranger in a purple cloak, which is a genre away from a town full of
    /// the walking dead. What makes it read as fantasy is the colour, not the drawing: strip the
    /// purple out and the same silhouette is a survivor in a hooded coat. So this recolours
    /// rather than redraws — the shading, the fold of the hood and the leather underneath are
    /// the pack artist's and stay exactly as they were.
    /// Blue-grey rather than the olive that a survivor would more obviously wear, and that is a
    /// gameplay decision rather than a taste one: the horde is green-skinned, and a player in
    /// olive drab disappears into a crowd of them at exactly the moment they most need to find
    /// themselves on screen.
    /// Only the three walking poses. The pack's shooting poses have a drawn bow in them, which
    /// no amount of recolouring turns into a pistol, and nothing in the game draws them.
    /// The archer files are left untouched — this writes new ones beside them, so the pack can
    /// be re-imported and the originals compared against at any time.
    /// </remarks>
    public static class SurvivorArt
    {
        private const string Folder = "Assets/_Project/Art/Sprites/Characters";

        /// <summary>The archer poses that are worth keeping, and what each becomes.</summary>
        private static readonly (string From, string To)[] Poses =
        {
            ("archer_front_walk", "survivor_front_walk"),
            ("archer_side_left_walk", "survivor_side_left_walk"),
            ("archer_side_right_walk", "survivor_side_right_walk"),
        };

        /// <summary>
        /// The band of hues the cloak occupies, as a fraction of the colour wheel.
        /// </summary>
        /// <remarks>
        /// Measured off the pack's own palette rather than guessed: every pixel of cloth in the
        /// three poses falls between 248 and 278 degrees, and the next nearest thing in the
        /// sprite — the leather and the skin — sits down at 13 to 46. The window has room on
        /// both sides and still cannot reach either.
        /// </remarks>
        private const float CloakHueFrom = 0.655f;

        /// <inheritdoc cref="CloakHueFrom"/>
        private const float CloakHueTo = 0.815f;

        /// <summary>The hue the cloth becomes: a cold slate blue, well clear of the horde.</summary>
        private const float CoatHue = 0.58f;

        /// <summary>
        /// What the cloth's colourfulness is multiplied by.
        /// </summary>
        /// <remarks>
        /// The cloak is dyed; a coat that has been worn through a winter outdoors is not. Taking
        /// most of the saturation out is what stops the result reading as a costume, and it
        /// leaves the artist's shading ramp intact because only the colourfulness moves — how
        /// light each pixel is never changes.
        /// </remarks>
        private const float CoatSaturation = 0.32f;

        /// <summary>Writes the survivor poses, replacing whatever is already there.</summary>
        [MenuItem("ZombieGame/Art/Recolour The Survivor")]
        public static void RecolourAll()
        {
            IReadOnlyList<string> written = Recolour(overwrite: true);
            Debug.Log($"[Survivor] wrote {written.Count} pose(s).");
        }

        /// <summary>
        /// Writes any pose that is not on disk yet, and says so if it had to.
        /// </summary>
        /// <remarks>
        /// Called by the scene builders, so that a fresh clone builds a zone with the MC in it
        /// rather than one with a warning where the MC should be. Somebody who has tuned the
        /// colours by hand keeps their files.
        /// </remarks>
        public static void EnsureAll()
        {
            IReadOnlyList<string> written = Recolour(overwrite: false);
            if (written.Count > 0)
            {
                Debug.Log($"[Survivor] recoloured {written.Count} missing pose(s).");
            }
        }

        /// <summary>
        /// Recolours the poses and returns the asset paths written.
        /// </summary>
        /// <param name="overwrite">False to keep poses that are already on disk.</param>
        public static IReadOnlyList<string> Recolour(bool overwrite)
        {
            var written = new List<string>();

            try
            {
                AssetDatabase.StartAssetEditing();

                foreach ((string from, string to) in Poses)
                {
                    string source = $"{Folder}/{from}.png";
                    if (!File.Exists(source))
                    {
                        Debug.LogWarning($"[Survivor] no pose at '{source}'; skipping it.");
                        continue;
                    }

                    string destination = $"{Folder}/{to}.png";
                    if (!overwrite && File.Exists(destination))
                    {
                        continue;
                    }

                    File.WriteAllBytes(destination, Recoloured(File.ReadAllBytes(source)));
                    written.Add(destination);
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
            return written;
        }

        /// <summary>
        /// Reads a PNG, moves its cloak colours, and encodes it again.
        /// </summary>
        /// <remarks>
        /// Decoded from the file's own bytes rather than from the imported sprite, so that this
        /// works whether or not the source texture happens to be marked readable — an import
        /// setting nobody should have to remember to tick for the art to rebuild.
        /// </remarks>
        private static byte[] Recoloured(byte[] png)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false);
            try
            {
                texture.LoadImage(png);

                Color32[] pixels = texture.GetPixels32();
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = Recoloured(pixels[i]);
                }

                texture.SetPixels32(pixels);
                texture.Apply();
                return texture.EncodeToPNG();
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        /// <summary>Moves one pixel, if it is cloth. Everything else is left alone.</summary>
        public static Color32 Recoloured(Color32 pixel)
        {
            // Nothing to recolour in a hole, and a transparent pixel still carries a colour that
            // would otherwise be dragged along for no reason.
            if (pixel.a == 0)
            {
                return pixel;
            }

            Color.RGBToHSV(new Color32(pixel.r, pixel.g, pixel.b, 255), out float h, out float s, out float v);
            if (h < CloakHueFrom || h > CloakHueTo)
            {
                return pixel;
            }

            Color moved = Color.HSVToRGB(CoatHue, s * CoatSaturation, v);
            return new Color32(
                (byte)Mathf.RoundToInt(moved.r * 255f),
                (byte)Mathf.RoundToInt(moved.g * 255f),
                (byte)Mathf.RoundToInt(moved.b * 255f),
                pixel.a);
        }
    }
}
