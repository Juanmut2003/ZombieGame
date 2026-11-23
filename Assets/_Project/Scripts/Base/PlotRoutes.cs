#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace ZombieGame.Base
{
    /// <summary>
    /// A way across the plot that goes round the walls instead of into them.
    /// </summary>
    /// <remarks>
    /// The smallest thing that deserves to be called pathfinding, and only because the plot is
    /// small: a breadth-first search over the cells, four ways out of each, with a step allowed
    /// only when the line between the two cell centres crosses no wall. Fourteen cells by eight
    /// is a hundred and twelve of them, searched once when somebody is given a job and never
    /// again until the job changes.
    /// Steering straight at a bench instead was the whole bug: a survivor sent to the farm from
    /// the other side of it walked into the back wall and stood there pushing for the rest of
    /// the day. A door is a gap one cell wide, and a figure has to be aimed at it.
    /// Four ways and no diagonals, so that a step can never slip between two corners that touch.
    /// Pure maths with the walls handed in, so the whole of it is tested without a scene.
    /// </remarks>
    public static class PlotRoutes
    {
        /// <summary>How many cells the search will look at before giving up.</summary>
        public const int MaxCells = 4096;

        /// <summary>
        /// Finds a way from one point to another, round the walls.
        /// </summary>
        /// <param name="from">Where they are standing now.</param>
        /// <param name="to">Where they want to be. The last leg of the route.</param>
        /// <param name="area">The ground they may cross, in world units.</param>
        /// <param name="walls">What they may not cross, in world units.</param>
        /// <param name="route">Filled with the way there. Cleared first.</param>
        /// <returns>False when there is no way through at all.</returns>
        public static bool TryFind(
            Vector2 from,
            Vector2 to,
            Rect area,
            IReadOnlyList<Rect> walls,
            List<Vector2> route)
        {
            route.Clear();

            int wide = Mathf.Max(1, Mathf.RoundToInt(area.width));
            int tall = Mathf.Max(1, Mathf.RoundToInt(area.height));
            if (wide * tall > MaxCells)
            {
                return false;
            }

            Vector2Int start = CellOf(from, area, wide, tall);
            Vector2Int goal = CellOf(to, area, wide, tall);

            // Already there, or near enough that a route would only be the step they are about
            // to take anyway.
            if (start == goal)
            {
                route.Add(to);
                return true;
            }

            var cameFrom = new int[wide * tall];
            for (int i = 0; i < cameFrom.Length; i++)
            {
                cameFrom[i] = -2;
            }

            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);
            cameFrom[Index(start, wide)] = -1;

            bool found = false;
            while (queue.Count > 0)
            {
                Vector2Int at = queue.Dequeue();
                if (at == goal)
                {
                    found = true;
                    break;
                }

                for (int side = 0; side < 4; side++)
                {
                    Vector2Int next = at + Sides[side];
                    if ((uint)next.x >= (uint)wide || (uint)next.y >= (uint)tall)
                    {
                        continue;
                    }

                    int index = Index(next, wide);
                    if (cameFrom[index] != -2)
                    {
                        continue;
                    }

                    if (!Open(Middle(at, area), Middle(next, area), walls))
                    {
                        continue;
                    }

                    cameFrom[index] = Index(at, wide);
                    queue.Enqueue(next);
                }
            }

            if (!found)
            {
                return false;
            }

            // Walked back from the goal, then turned round. Corners only: a straight run of cells
            // is one leg, because steering is what follows this and a leg a cell long is a stop
            // and a start for no reason.
            var back = new List<Vector2Int>();
            for (int at = Index(goal, wide); at >= 0; at = cameFrom[at])
            {
                back.Add(new Vector2Int(at % wide, at / wide));
            }

            back.Reverse();

            for (int i = 1; i < back.Count; i++)
            {
                bool corner = i == back.Count - 1
                              || (back[i + 1] - back[i]) != (back[i] - back[i - 1]);

                if (corner)
                {
                    route.Add(Middle(back[i], area));
                }
            }

            // And the spot itself, which is somewhere inside the last cell rather than its
            // middle: a bench stands against the furniture, not in the centre of the floor.
            route.Add(to);
            return true;
        }

        /// <summary>
        /// Whether the straight line between two neighbouring cell centres is clear of walls.
        /// </summary>
        /// <remarks>
        /// The step is always along one axis, so the line is a flat box and crossing a wall is
        /// an overlap of two rectangles. Touching an edge is not crossing it: walls are drawn on
        /// the cell boundary, and a figure walking along the outside of one is not walking
        /// through it.
        /// </remarks>
        public static bool Open(Vector2 from, Vector2 to, IReadOnlyList<Rect> walls)
        {
            float left = Mathf.Min(from.x, to.x);
            float right = Mathf.Max(from.x, to.x);
            float bottom = Mathf.Min(from.y, to.y);
            float top = Mathf.Max(from.y, to.y);

            for (int i = 0; i < walls.Count; i++)
            {
                Rect wall = walls[i];
                if (left < wall.xMax && wall.xMin < right
                                     && bottom < wall.yMax && wall.yMin < top)
                {
                    return false;
                }
            }

            return true;
        }

        private static readonly Vector2Int[] Sides =
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
        };

        private static int Index(Vector2Int cell, int wide) => (cell.y * wide) + cell.x;

        private static Vector2Int CellOf(Vector2 point, Rect area, int wide, int tall) =>
            new Vector2Int(
                Mathf.Clamp(Mathf.FloorToInt(point.x - area.xMin), 0, wide - 1),
                Mathf.Clamp(Mathf.FloorToInt(point.y - area.yMin), 0, tall - 1));

        private static Vector2 Middle(Vector2Int cell, Rect area) =>
            new Vector2(area.xMin + cell.x + 0.5f, area.yMin + cell.y + 0.5f);
    }
}
