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

        /// <summary>
        /// The trail of a melee swing: a crescent opening to the right, apex in the middle of the
        /// texture, so it can be rotated around the character who swung it.
        /// </summary>
        SwingArc,

        /// <summary>A round in flight: a bright streak with a hot tip.</summary>
        Bullet,

        /// <summary>The way out: a hatched pad with a bright border.</summary>
        ExtractionPad,

        /// <summary>
        /// One dash of a road's centre line, running up the middle of an otherwise empty tile, so
        /// a row of them reads as a painted line and a tile rotated a quarter turn serves a street
        /// running the other way.
        /// </summary>
        RoadDash,

        /// <summary>
        /// One quarter of the ring the building menu is drawn as: a 90 degree slice of an
        /// annulus, pointing straight up, with a seam left on either side so four of them
        /// rotated into place read as four buttons rather than one disc.
        /// </summary>
        WheelWedge,

        /// <summary>The hole in the middle of that ring, where the building itself shows.</summary>
        WheelHub,
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
                case PlaceholderShape.SwingArc:
                    DrawSwingArc(pixels, spec);
                    break;
                case PlaceholderShape.Bullet:
                    DrawBullet(pixels, spec);
                    break;
                case PlaceholderShape.ExtractionPad:
                    DrawExtractionPad(pixels, spec);
                    break;
                case PlaceholderShape.RoadDash:
                    DrawRoadDash(pixels, spec);
                    break;
                case PlaceholderShape.WheelWedge:
                    DrawWheelWedge(pixels, spec);
                    break;
                case PlaceholderShape.WheelHub:
                    DrawWheelHub(pixels, spec);
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

        /// <summary>
        /// A worn stripe up the middle of a transparent tile, with a gap at either end so that
        /// laying one on every other cell gives a broken line rather than a continuous one.
        /// </summary>
        private static void DrawRoadDash(Color32[] pixels, PlaceholderSpriteSpec spec)
        {
            int thickness = Math.Max(2, spec.Width / 10);
            int left = (spec.Width - thickness) / 2;
            int gap = spec.Height / 8;

            Fill(pixels, spec, left, gap, thickness, spec.Height - (gap * 2), spec.Body);

            // A thinner core in the brighter colour: fresh paint in the middle of a stripe the
            // traffic has worn, which keeps it from reading as a flat white bar.
            int core = Math.Max(1, thickness / 2);
            Fill(
                pixels,
                spec,
                left + ((thickness - core) / 2),
                gap + 1,
                core,
                spec.Height - (gap * 2) - 2,
                spec.Accent);
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

        /// <summary>
        /// Draws the crescent a swing leaves behind: a band near the far end of the reach,
        /// spanning the width of the wedge, with its apex at the centre of the texture.
        /// </summary>
        /// <remarks>
        /// Only the outer part of the wedge is drawn. A filled pie slice reads as a shield or a
        /// torch beam; what a swing looks like is the far edge of it moving. The spec's half-angle
        /// and inner radius are fixed here rather than taken from the weapon, because this is one
        /// sprite rotated and scaled to whatever weapon swung it.
        /// </remarks>
        /// <summary>
        /// The way out: a hatched square with a bright border, drawn to be seen from across a room.
        /// </summary>
        /// <remarks>
        /// A player who cannot find the exit is not making the decision the mission is about. The
        /// border carries the reading at a distance and the hatching keeps it from looking like
        /// a piece of floor once you are standing on it.
        /// </remarks>
        private static void DrawExtractionPad(Color32[] pixels, PlaceholderSpriteSpec spec)
        {
            int border = Math.Max(2, spec.Width / 16);

            for (int y = 0; y < spec.Height; y++)
            {
                for (int x = 0; x < spec.Width; x++)
                {
                    bool edge = x < border || y < border
                                || x >= spec.Width - border || y >= spec.Height - border;

                    // Diagonal hatching, the way a loading bay is painted.
                    bool hatch = ((x + y) / Math.Max(3, spec.Width / 12)) % 2 == 0;

                    if (edge)
                    {
                        pixels[(y * spec.Width) + x] = spec.Accent;
                    }
                    else if (hatch)
                    {
                        pixels[(y * spec.Width) + x] = spec.Body;
                    }
                }
            }
        }

        /// <summary>
        /// A round: a streak along the x axis with a brighter head, drawn pointing right.
        /// </summary>
        /// <remarks>
        /// Pointing right because the view rotates it to the direction of travel, and right is
        /// zero degrees. A dot would be honest but unreadable at this size and speed; the streak
        /// is what lets a player see where a shot went.
        /// </remarks>
        private static void DrawBullet(Color32[] pixels, PlaceholderSpriteSpec spec)
        {
            int middle = spec.Height / 2;
            int thickness = Math.Max(1, spec.Height / 3);
            int head = Math.Max(1, spec.Width / 4);

            for (int y = middle - thickness; y <= middle + thickness; y++)
            {
                if (y < 0 || y >= spec.Height)
                {
                    continue;
                }

                for (int x = 0; x < spec.Width; x++)
                {
                    bool edge = Math.Abs(y - middle) == thickness;
                    pixels[(y * spec.Width) + x] =
                        x >= spec.Width - head || !edge ? spec.Accent : spec.Body;
                }
            }
        }

        private static void DrawSwingArc(Color32[] pixels, PlaceholderSpriteSpec spec)
        {
            const float halfAngleDegrees = 55f;
            const float innerFraction = 0.62f;

            float centreX = (spec.Width - 1) * 0.5f;
            float centreY = (spec.Height - 1) * 0.5f;
            float outerRadius = Math.Min(centreX, centreY);
            float innerRadius = outerRadius * innerFraction;
            float cosLimit = (float)Math.Cos(halfAngleDegrees * Math.PI / 180.0);

            for (int y = 0; y < spec.Height; y++)
            {
                for (int x = 0; x < spec.Width; x++)
                {
                    float dx = x - centreX;
                    float dy = y - centreY;
                    float distance = (float)Math.Sqrt((dx * dx) + (dy * dy));

                    if (distance < innerRadius || distance > outerRadius || distance <= 0.001f)
                    {
                        continue;
                    }

                    // The wedge opens towards +x, so the cosine of the angle to that axis is
                    // simply the normalised x — no trigonometry per pixel needed.
                    if (dx / distance < cosLimit)
                    {
                        continue;
                    }

                    // Brightest at the leading edge and fading inwards, which is what sells it as
                    // a trail rather than a painted shape.
                    float depth = (distance - innerRadius) / (outerRadius - innerRadius);
                    Color32 colour = depth > 0.72f ? spec.Accent : spec.Body;
                    Set(pixels, spec, x, y, colour);
                }
            }
        }

        /// <summary>
        /// A quarter of a ring, opening straight up.
        /// </summary>
        /// <remarks>
        /// Drawn once and rotated four times rather than drawn four times, so the four buttons
        /// of the building menu cannot drift apart by a pixel. The half-angle is a little under
        /// 45 degrees on purpose: the gap it leaves is what makes the ring read as separate
        /// slices you can click rather than a single dial.
        /// </remarks>
        private static void DrawWheelWedge(Color32[] pixels, PlaceholderSpriteSpec spec)
        {
            const float halfAngleDegrees = 42f;
            const float innerFraction = 0.4f;
            const float rimFraction = 0.9f;

            float centreX = (spec.Width - 1) * 0.5f;
            float centreY = (spec.Height - 1) * 0.5f;
            float outerRadius = Math.Min(centreX, centreY);
            float innerRadius = outerRadius * innerFraction;
            float rimRadius = outerRadius * rimFraction;
            double radians = halfAngleDegrees * Math.PI / 180.0;
            float cosLimit = (float)Math.Cos(radians);
            float sinLimit = (float)Math.Sin(radians);
            // Scaled with the texture, so the outline reads the same whatever size the wheel is
            // drawn at, and never thinner than the two pixels a line needs to exist at all.
            float edgeWidth = Math.Max(2f, outerRadius * 0.05f);

            for (int y = 0; y < spec.Height; y++)
            {
                for (int x = 0; x < spec.Width; x++)
                {
                    float dx = x - centreX;
                    float dy = y - centreY;
                    float distance = (float)Math.Sqrt((dx * dx) + (dy * dy));

                    if (distance < innerRadius || distance > outerRadius || distance <= 0.001f)
                    {
                        continue;
                    }

                    // The slice opens towards +y, so the cosine of the angle to that axis is
                    // the normalised y and no trigonometry is needed per pixel.
                    if (dy / distance < cosLimit)
                    {
                        continue;
                    }

                    // An edge all the way round: the two arcs, and a line down each straight
                    // side. The straight ones are measured as the distance to the boundary
                    // itself rather than as an angle, or the line would thin out to nothing
                    // towards the middle, where the two sides are closest together.
                    float toRight = Math.Abs((dx * cosLimit) - (dy * sinLimit));
                    float toLeft = Math.Abs((dx * cosLimit) + (dy * sinLimit));
                    bool edge = distance > rimRadius
                        || distance < innerRadius + edgeWidth
                        || toRight < edgeWidth
                        || toLeft < edgeWidth;
                    Set(pixels, spec, x, y, edge ? spec.Accent : spec.Body);
                }
            }
        }

        /// <summary>The disc in the middle of the wheel, with a ring around it.</summary>
        private static void DrawWheelHub(Color32[] pixels, PlaceholderSpriteSpec spec)
        {
            float centreX = (spec.Width - 1) * 0.5f;
            float centreY = (spec.Height - 1) * 0.5f;
            float outerRadius = Math.Min(centreX, centreY);
            float ringRadius = outerRadius - Math.Max(2f, outerRadius * 0.08f);

            for (int y = 0; y < spec.Height; y++)
            {
                for (int x = 0; x < spec.Width; x++)
                {
                    float dx = x - centreX;
                    float dy = y - centreY;
                    float distance = (float)Math.Sqrt((dx * dx) + (dy * dy));
                    if (distance > outerRadius)
                    {
                        continue;
                    }

                    Set(pixels, spec, x, y, distance > ringRadius ? spec.Accent : spec.Body);
                }
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
