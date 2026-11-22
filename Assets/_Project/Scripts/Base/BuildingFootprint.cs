#nullable enable
using System;
using UnityEngine;

namespace ZombieGame.Base
{
    /// <summary>How much ground a building stands on, in cells.</summary>
    /// <remarks>
    /// Cells rather than world units, because the whole of base building is a question about
    /// cells: what is under this one, is that one taken, does the barn fit between the fence and
    /// the well. One cell is one world unit, the same convention the tilemaps and
    /// <see cref="ZombieGame.Core.Physics.WallGrid"/> already keep.
    /// A rectangle and not an outline. An L-shaped workshop would be a nicer silhouette and a far
    /// worse thing to drag around with a mouse, and nothing in PROJECT.md 4.2 asks for one.
    /// </remarks>
    public readonly struct BuildingFootprint : IEquatable<BuildingFootprint>
    {
        /// <param name="width">How many cells across. At least one.</param>
        /// <param name="height">How many cells up. At least one.</param>
        public BuildingFootprint(int width, int height)
        {
            Width = Mathf.Max(1, width);
            Height = Mathf.Max(1, height);
        }

        /// <summary>How many cells across.</summary>
        public int Width { get; }

        /// <summary>How many cells up.</summary>
        public int Height { get; }

        /// <summary>How many cells of ground it takes up in all.</summary>
        public int Area => Width * Height;

        /// <summary>True when turning it a quarter turn would change anything.</summary>
        public bool IsSquare => Width == Height;

        /// <summary>The same building stood a quarter turn round.</summary>
        public BuildingFootprint Rotated() => new(Height, Width);

        /// <summary>
        /// Where the middle of the building sits, given the cell its bottom-left corner is on.
        /// </summary>
        /// <remarks>
        /// Half a cell out from the corner for every cell of size, which for an even-sided
        /// building lands exactly on the line between two cells. That is correct and worth
        /// saying: a two-wide shed centred on a cell would be half a cell into its neighbour.
        /// </remarks>
        public Vector2 CentreFrom(Vector2 corner) =>
            new(corner.x + (Width * 0.5f), corner.y + (Height * 0.5f));

        /// <inheritdoc />
        public bool Equals(BuildingFootprint other) =>
            Width == other.Width && Height == other.Height;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is BuildingFootprint other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => (Width * 397) ^ Height;

        /// <inheritdoc />
        public override string ToString() => $"{Width}x{Height}";
    }
}
