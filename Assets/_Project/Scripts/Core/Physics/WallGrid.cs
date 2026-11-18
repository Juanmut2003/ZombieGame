#nullable enable
using System;
using UnityEngine;

namespace ZombieGame.Core.Physics
{
    /// <summary>
    /// Which cells of a zone are solid, as a bitmap that can be asked a thousand times a frame.
    /// </summary>
    /// <remarks>
    /// Built once when a zone loads and never changed until a wall is knocked down. One world
    /// unit is one cell, which is the convention the tilemaps, the placement grid and the maps in
    /// <c>ZoneMaps</c> all already use.
    /// </remarks>
    public sealed class WallGrid : IWallMap
    {
        private readonly bool[] _solid;

        /// <param name="origin">The world position of the bottom-left cell's corner.</param>
        /// <param name="width">How many cells across.</param>
        /// <param name="height">How many cells up.</param>
        public WallGrid(Vector2 origin, int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(width), $"{width}x{height}", "A zone has to have some ground in it.");
            }

            Origin = origin;
            Width = width;
            Height = height;
            _solid = new bool[width * height];
        }

        /// <summary>The world position of the bottom-left corner.</summary>
        public Vector2 Origin { get; }

        /// <summary>How many cells across.</summary>
        public int Width { get; }

        /// <summary>How many cells up.</summary>
        public int Height { get; }

        /// <summary>Marks one cell solid, counting from the origin.</summary>
        public void SetSolid(int column, int row, bool solid = true)
        {
            if ((uint)column >= (uint)Width || (uint)row >= (uint)Height)
            {
                return;
            }

            _solid[(row * Width) + column] = solid;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Anything outside the grid counts as open rather than solid. A zone's edge is walled in
        /// by its own map; treating the outside as rock would mean a body nudged half a unit past
        /// the border could never move again.
        /// </remarks>
        public bool IsSolid(Vector2 point)
        {
            int column = Mathf.FloorToInt(point.x - Origin.x);
            int row = Mathf.FloorToInt(point.y - Origin.y);

            if ((uint)column >= (uint)Width || (uint)row >= (uint)Height)
            {
                return false;
            }

            return _solid[(row * Width) + column];
        }
    }
}
