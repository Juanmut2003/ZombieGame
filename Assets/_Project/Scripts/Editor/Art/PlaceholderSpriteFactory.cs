#nullable enable
using System;
using UnityEngine;

namespace ZombieGame.Editor.Art
{
    /// <summary>The silhouettes the placeholder generator can draw.</summary>
    public enum PlaceholderShape
    {
        /// <summary>A standing figure: feet at the bottom edge, head on top, a facing marker.</summary>
        Humanoid,

        /// <summary>A full-bleed floor or wall tile with a subtle pattern, so the grid is readable.</summary>
        Tile,

        /// <summary>A bordered box for props, crates and buildings.</summary>
        Box,
    }

    /// <summary>What one placeholder sprite should look like.</summary>
    public readonly struct PlaceholderSpriteSpec
    {
        public PlaceholderSpriteSpec(
            string name,
            int width,
            int height,
            PlaceholderShape shape,
            Color32 body,
            Color32 accent)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("A placeholder needs a name.", nameof(name));
            }

            if (width <= 0 || height <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(width), $"{width}x{height}", "A sprite needs a positive size.");
            }

            Name = name;
            Width = width;
            Height = height;
            Shape = shape;
            Body = body;
            Accent = accent;
        }

        public string Name { get; }

        public int Width { get; }

        public int Height { get; }

        public PlaceholderShape Shape { get; }

        /// <summary>The main fill colour.</summary>
        public Color32 Body { get; }

        /// <summary>Used for the facing marker, borders and pattern — whatever reads the shape.</summary>
        public Color32 Accent { get; }
    }

    /// <summary>
    /// Draws stand-in sprites in code so the game has something visible long before there is art.
    /// </summary>
    /// <remarks>
    /// Pure pixel pushing, no files and no Unity asset types, so the output can be asserted in a
    /// test. Placeholders are deliberately ugly and obviously placeholder — nobody should ever
    /// mistake one for finished art, and real art replaces them file by file.
    /// TODO: colours are local constants until the palette in Art/Palettes exists.
    /// </remarks>
    public static class PlaceholderSpriteFactory
    {
        private static readonly Color32 Transparent = new(0, 0, 0, 0);

        /// <summary>
        /// Builds the pixels of a placeholder, bottom row first, exactly as Unity's
        /// <c>SetPixels32</c> expects them.
        /// </summary>
        public static Color32[] CreatePixels(PlaceholderSpriteSpec spec)
        {
            var pixels = new Color32[spec.Width * spec.Height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Transparent;
            }

            switch (spec.Shape)
            {
                case PlaceholderShape.Humanoid:
                    DrawHumanoid(pixels, spec);
                    break;
                case PlaceholderShape.Tile:
                    DrawTile(pixels, spec);
                    break;
                case PlaceholderShape.Box:
                    DrawBox(pixels, spec);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(spec), spec.Shape, "Unknown placeholder shape.");
            }

            return pixels;
        }

        private static void DrawHumanoid(Color32[] pixels, PlaceholderSpriteSpec spec)
        {
            int bodyWidth = Math.Max(2, spec.Width / 2);
            int bodyHeight = Math.Max(1, spec.Height * 55 / 100);
            int bodyLeft = (spec.Width - bodyWidth) / 2;
            Fill(pixels, spec, bodyLeft, 0, bodyWidth, bodyHeight, spec.Body);

            int headSize = Math.Max(2, spec.Width * 45 / 100);
            int headLeft = (spec.Width - headSize) / 2;
            int headBottom = bodyHeight;
            int headHeight = Math.Min(headSize, spec.Height - headBottom);
            if (headHeight > 0)
            {
                Fill(pixels, spec, headLeft, headBottom, headSize, headHeight, spec.Body);

                // A band across the lower half of the head marks the front, so facing is readable
                // at a glance even on a featureless blob.
                int faceHeight = Math.Max(1, headHeight / 3);
                Fill(pixels, spec, headLeft, headBottom, headSize, faceHeight, spec.Accent);
            }
        }

        private static void DrawTile(Color32[] pixels, PlaceholderSpriteSpec spec)
        {
            Fill(pixels, spec, 0, 0, spec.Width, spec.Height, spec.Body);

            // One accent pixel every few steps: enough to see the grid and any tiling seam,
            // not enough to look like a texture.
            const int step = 16;
            for (int y = 0; y < spec.Height; y += step)
            {
                for (int x = 0; x < spec.Width; x += step)
                {
                    Set(pixels, spec, x, y, spec.Accent);
                }
            }
        }

        private static void DrawBox(Color32[] pixels, PlaceholderSpriteSpec spec)
        {
            Fill(pixels, spec, 0, 0, spec.Width, spec.Height, spec.Body);

            for (int x = 0; x < spec.Width; x++)
            {
                Set(pixels, spec, x, 0, spec.Accent);
                Set(pixels, spec, x, spec.Height - 1, spec.Accent);
            }

            for (int y = 0; y < spec.Height; y++)
            {
                Set(pixels, spec, 0, y, spec.Accent);
                Set(pixels, spec, spec.Width - 1, y, spec.Accent);
            }
        }

        private static void Fill(
            Color32[] pixels,
            PlaceholderSpriteSpec spec,
            int left,
            int bottom,
            int width,
            int height,
            Color32 colour)
        {
            for (int y = bottom; y < bottom + height; y++)
            {
                for (int x = left; x < left + width; x++)
                {
                    Set(pixels, spec, x, y, colour);
                }
            }
        }

        private static void Set(
            Color32[] pixels, PlaceholderSpriteSpec spec, int x, int y, Color32 colour)
        {
            if (x < 0 || y < 0 || x >= spec.Width || y >= spec.Height)
            {
                return;
            }

            pixels[(y * spec.Width) + x] = colour;
        }
    }
}
