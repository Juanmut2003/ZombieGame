#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace ZombieGame.Base
{
    /// <summary>One stretch of a building's wall, as a box around the building's middle.</summary>
    public readonly struct WallSegment
    {
        public WallSegment(Vector2 offset, Vector2 size)
        {
            Offset = offset;
            Size = size;
        }

        /// <summary>Where its middle sits, relative to the building's middle.</summary>
        public Vector2 Offset { get; }

        /// <summary>How big it is, in world units. A zero width means there is no wall there.</summary>
        public Vector2 Size { get; }

        /// <summary>True when this stretch is actually there.</summary>
        public bool Exists => Size.x > 0f && Size.y > 0f;
    }

    /// <summary>
    /// Where a building's walls stand, and where its doorway is.
    /// </summary>
    /// <remarks>
    /// The buildings are drawn as rooms seen from above with the roof cut away: beds and a table
    /// inside, a low fence along the front, and steps up to a doorway in the middle of it. So the
    /// solid part of a building is its walls and not its ground — a figure walks in through the
    /// door and stands among the furniture, which is how a survivor at work is seen at all.
    /// Pure maths, in cells, tested without a scene. The walls are set from this every time a
    /// building is drawn, so turning one round and improving it into a bigger one both rebuild
    /// the room rather than leaving last version's walls standing.
    /// </remarks>
    public static class BuildingWalls
    {
        /// <summary>How thick a side or front wall is, in world units. A quarter of a cell.</summary>
        public const float Thickness = 0.25f;

        /// <summary>
        /// How deep the back wall is: the roof, which is drawn as a strip across the top of
        /// every building and is the one part of the room that is not floor.
        /// </summary>
        public const float BackThickness = 0.75f;

        /// <summary>How many stretches of wall a building has, doorway and all.</summary>
        public const int Count = 5;

        /// <summary>How wide the doorway is, in cells.</summary>
        public const int DoorWidth = 1;

        /// <summary>
        /// Which cell of the front wall the doorway is cut from, counting from the left.
        /// </summary>
        /// <remarks>
        /// The middle, which for an even-sided building is the cell to the left of the middle —
        /// where the art puts the steps. One door and not two: a room with a way in at each end
        /// is a corridor, and later a lane for whatever is trying to get in.
        /// </remarks>
        public static int DoorCell(BuildingFootprint footprint) => (footprint.Width - 1) / 2;

        /// <summary>The gap in the front wall, relative to the building's middle.</summary>
        public static Rect Doorway(BuildingFootprint footprint)
        {
            float left = (-footprint.Width * 0.5f) + DoorCell(footprint);
            return new Rect(left, -footprint.Height * 0.5f, DoorWidth, Thickness);
        }

        /// <summary>
        /// One stretch of wall: 0 the back, 1 the left, 2 the right, 3 and 4 the front either
        /// side of the doorway.
        /// </summary>
        public static WallSegment Segment(BuildingFootprint footprint, int index)
        {
            float halfWide = footprint.Width * 0.5f;
            float halfTall = footprint.Height * 0.5f;

            switch (index)
            {
                case 0:
                    return new WallSegment(
                        new Vector2(0f, halfTall - (BackThickness * 0.5f)),
                        new Vector2(footprint.Width, BackThickness));
                case 1:
                    return new WallSegment(
                        new Vector2(-halfWide + (Thickness * 0.5f), 0f),
                        new Vector2(Thickness, footprint.Height));
                case 2:
                    return new WallSegment(
                        new Vector2(halfWide - (Thickness * 0.5f), 0f),
                        new Vector2(Thickness, footprint.Height));
                case 3:
                {
                    // From the left corner to the near side of the doorway. Nothing at all when
                    // the door is in the corner, which is every one-wide building.
                    float door = Doorway(footprint).xMin;
                    float width = door + halfWide;
                    return new WallSegment(
                        new Vector2((-halfWide + door) * 0.5f, -halfTall + (Thickness * 0.5f)),
                        new Vector2(Mathf.Max(0f, width), Thickness));
                }
                case 4:
                {
                    // From the far side of the doorway to the right corner.
                    float door = Doorway(footprint).xMax;
                    float width = halfWide - door;
                    return new WallSegment(
                        new Vector2((door + halfWide) * 0.5f, -halfTall + (Thickness * 0.5f)),
                        new Vector2(Mathf.Max(0f, width), Thickness));
                }
                default:
                    return new WallSegment(Vector2.zero, Vector2.zero);
            }
        }

        /// <summary>
        /// Adds a building's walls to a list, in world units.
        /// </summary>
        /// <param name="walls">The list to add to. Not cleared.</param>
        /// <param name="footprint">How much ground the building stands on.</param>
        /// <param name="middle">Where the middle of that ground is, in world units.</param>
        public static void AppendTo(
            List<Rect> walls, BuildingFootprint footprint, Vector2 middle)
        {
            for (int i = 0; i < Count; i++)
            {
                WallSegment wall = Segment(footprint, i);
                if (!wall.Exists)
                {
                    continue;
                }

                walls.Add(
                    new Rect(
                        middle.x + wall.Offset.x - (wall.Size.x * 0.5f),
                        middle.y + wall.Offset.y - (wall.Size.y * 0.5f),
                        wall.Size.x,
                        wall.Size.y));
            }
        }

        /// <summary>
        /// The floor inside the walls, relative to the building's middle: where a figure in the
        /// room can stand.
        /// </summary>
        public static Rect Room(BuildingFootprint footprint) =>
            new Rect(
                (-footprint.Width * 0.5f) + Thickness,
                (-footprint.Height * 0.5f) + Thickness,
                Mathf.Max(0f, footprint.Width - (Thickness * 2f)),
                Mathf.Max(0f, footprint.Height - Thickness - BackThickness));
    }
}
