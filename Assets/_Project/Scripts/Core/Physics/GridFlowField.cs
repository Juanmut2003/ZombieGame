#nullable enable
using System;
using UnityEngine;

namespace ZombieGame.Core.Physics
{
    /// <summary>
    /// A flow field over a zone's cells: how many steps each one is from the goal, and therefore
    /// which way is downhill.
    /// </summary>
    /// <remarks>
    /// Breadth-first from the goal outwards, so every cell ends up holding its true distance
    /// through the walls rather than across them. That is the whole difference between a zombie
    /// that walks round a house and one that presses itself against the back of it: walking
    /// straight at someone is a direction, and this is a route.
    /// Rebuilt rather than updated. A zone is a few thousand cells and the sweep is linear in
    /// them, which is cheaper than any attempt to patch a field as a target moves — and far
    /// cheaper than three hundred separate searches.
    /// The queue and both arrays are allocated once and reused, so a rebuild every few frames
    /// costs no garbage.
    /// </remarks>
    public sealed class GridFlowField : IFlowField
    {
        /// <summary>Stands for "no route from here".</summary>
        private const int Unreached = int.MaxValue;

        private readonly int _width;
        private readonly int _height;
        private readonly Vector2 _origin;
        private readonly bool[] _solid;
        private readonly int[] _distance;
        private readonly int[] _queue;

        private int _head;
        private int _tail;

        /// <param name="origin">The world position of the bottom-left cell's corner.</param>
        /// <param name="width">How many cells across.</param>
        /// <param name="height">How many cells up.</param>
        public GridFlowField(Vector2 origin, int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(width), $"{width}x{height}", "A field needs some ground to cover.");
            }

            _origin = origin;
            _width = width;
            _height = height;
            _solid = new bool[width * height];
            _distance = new int[width * height];
            _queue = new int[width * height];
        }

        /// <summary>Where the field currently leads.</summary>
        public Vector2 Goal { get; private set; }

        /// <summary>True once a goal has been set and anything at all was reached.</summary>
        public bool HasRoute { get; private set; }

        /// <summary>Reads the walls once, before the first rebuild.</summary>
        /// <remarks>
        /// Separate from the sweep because walls do not move and the goal does. Sampling the
        /// centre of each cell, which is the convention the whole grid is laid out on.
        /// </remarks>
        public void ReadWalls(IWallMap walls)
        {
            for (int row = 0; row < _height; row++)
            {
                for (int column = 0; column < _width; column++)
                {
                    var centre = new Vector2(
                        _origin.x + column + 0.5f, _origin.y + row + 0.5f);
                    _solid[(row * _width) + column] = walls.IsSolid(centre);
                }
            }
        }

        /// <summary>Sweeps the field outwards from <paramref name="goal"/>.</summary>
        /// <returns>False when the goal is off the map or inside a wall.</returns>
        public bool Rebuild(Vector2 goal)
        {
            Goal = goal;
            HasRoute = false;

            Array.Fill(_distance, Unreached);
            _head = 0;
            _tail = 0;

            if (!TryCell(goal, out int startColumn, out int startRow))
            {
                return false;
            }

            int start = (startRow * _width) + startColumn;
            if (_solid[start])
            {
                // Standing inside a wall is not a route out of one. Happens for a frame when a
                // zone loads and nothing has been placed yet.
                return false;
            }

            _distance[start] = 0;
            _queue[_tail++] = start;

            while (_head < _tail)
            {
                int at = _queue[_head++];
                int column = at % _width;
                int row = at / _width;
                int next = _distance[at] + 1;

                Step(column - 1, row, next);
                Step(column + 1, row, next);
                Step(column, row - 1, next);
                Step(column, row + 1, next);
            }

            HasRoute = true;
            return true;
        }

        /// <inheritdoc />
        public bool Reaches(Vector2 from) =>
            HasRoute && TryCell(from, out int column, out int row)
                     && _distance[(row * _width) + column] != Unreached;

        /// <inheritdoc />
        /// <remarks>
        /// Downhill, including diagonally — but a diagonal is only offered when both cells beside
        /// it are open, or a zombie would cut the corner of a building and walk through its wall.
        /// </remarks>
        public Vector2 Direction(Vector2 from)
        {
            if (!HasRoute || !TryCell(from, out int column, out int row))
            {
                return Vector2.zero;
            }

            int here = _distance[(row * _width) + column];
            if (here == Unreached || here == 0)
            {
                return Vector2.zero;
            }

            int best = here;
            int bestColumn = column;
            int bestRow = row;

            for (int down = -1; down <= 1; down++)
            {
                for (int across = -1; across <= 1; across++)
                {
                    if (down == 0 && across == 0)
                    {
                        continue;
                    }

                    int nextColumn = column + across;
                    int nextRow = row + down;
                    if (!Inside(nextColumn, nextRow))
                    {
                        continue;
                    }

                    if (down != 0 && across != 0
                        && (IsSolidCell(column + across, row) || IsSolidCell(column, row + down)))
                    {
                        continue;
                    }

                    int candidate = _distance[(nextRow * _width) + nextColumn];
                    if (candidate >= best)
                    {
                        continue;
                    }

                    best = candidate;
                    bestColumn = nextColumn;
                    bestRow = nextRow;
                }
            }

            if (bestColumn == column && bestRow == row)
            {
                return Vector2.zero;
            }

            // Aimed at the middle of the next cell rather than along the raw step, so a body
            // walking a diagonal keeps off the corners it is squeezing past.
            var centre = new Vector2(_origin.x + bestColumn + 0.5f, _origin.y + bestRow + 0.5f);
            Vector2 away = centre - from;
            return away.sqrMagnitude > 0.000001f ? away.normalized : Vector2.zero;
        }

        private void Step(int column, int row, int distance)
        {
            if (!Inside(column, row))
            {
                return;
            }

            int at = (row * _width) + column;
            if (_solid[at] || _distance[at] <= distance)
            {
                return;
            }

            _distance[at] = distance;
            _queue[_tail++] = at;
        }

        private bool IsSolidCell(int column, int row) =>
            !Inside(column, row) || _solid[(row * _width) + column];

        private bool Inside(int column, int row) =>
            (uint)column < (uint)_width && (uint)row < (uint)_height;

        private bool TryCell(Vector2 point, out int column, out int row)
        {
            column = Mathf.FloorToInt(point.x - _origin.x);
            row = Mathf.FloorToInt(point.y - _origin.y);
            return Inside(column, row);
        }
    }
}
