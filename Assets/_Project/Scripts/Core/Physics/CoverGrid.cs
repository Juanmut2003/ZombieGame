#nullable enable
using UnityEngine;

namespace ZombieGame.Core.Physics
{
    /// <summary>Which cells of a zone hide whoever stands in them.</summary>
    /// <remarks>
    /// The same bitmap as <see cref="WallGrid"/> — one world unit per cell, built once when the
    /// zone loads — asked a different question. It wraps one rather than inheriting from it,
    /// because a cover grid is not a wall grid and must never be handed to anything that moves
    /// bodies around: a hedge that stopped the horde would undo the whole idea of hiding in one.
    /// </remarks>
    public sealed class CoverGrid : ICoverMap
    {
        private readonly WallGrid _cells;

        /// <param name="origin">The world position of the bottom-left cell's corner.</param>
        /// <param name="width">How many cells across.</param>
        /// <param name="height">How many cells up.</param>
        public CoverGrid(Vector2 origin, int width, int height) =>
            _cells = new WallGrid(origin, width, height);

        /// <summary>Marks one cell as cover, counting from the origin.</summary>
        public void SetCover(int column, int row, bool cover = true) =>
            _cells.SetSolid(column, row, cover);

        /// <inheritdoc />
        public bool Hides(Vector2 point) => _cells.IsSolid(point);
    }
}
