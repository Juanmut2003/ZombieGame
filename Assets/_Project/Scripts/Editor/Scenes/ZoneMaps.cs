#nullable enable
using System.Text;

namespace ZombieGame.Editor.Scenes
{
    /// <summary>
    /// The hand-drawn layouts, as text.
    /// </summary>
    /// <remarks>
    /// A zone is drawn rather than generated. PROJECT.md §7 asks for handcrafted templates that
    /// are then dressed procedurally, and the handcrafted half has to live somewhere a person can
    /// change in ten seconds and a reviewer can read in a diff. A tilemap in a scene file is
    /// neither; twenty lines of text are both.
    /// Legend: <c>#</c> wall, <c>.</c> floor, space nothing, <c>P</c> where the squad starts,
    /// <c>X</c> the way out, <c>L</c> a container worth going through, <c>Z</c> somewhere zombies
    /// are standing about.
    /// </remarks>
    public static class ZoneMaps
    {
        /// <summary>
        /// The depot: a loading bay to arrive in, three storerooms with shelves, and a yard gate
        /// to leave by.
        /// </summary>
        /// <remarks>
        /// Drawn around one question: is the shortest way back to the gate worth the room with
        /// the most shelves in it? The two store rooms on the right hold the loot and are the
        /// furthest from the way out, the corridor between them is a lane zombies will walk, and
        /// the side office is the quiet route that costs time. Walking it is a decision; running
        /// it is louder and therefore a different decision.
        /// </remarks>
        public static string[] Depot { get; } =
        {
            "#########################",
            "#....#........#....L...L#",
            "#.P..#...Z....#....#....#",
            "#....#........#....#..Z.#",
            "#....######.####....#...#",
            "#.......................#",
            "#....######.#############",
            "#....#....#.#...L...L...#",
            "#..Z.#.L..#.#...........#",
            "#....#....#.#......Z....#",
            "#....#....#.#...........#",
            "#.####....#.#....####...#",
            "#.#..#....#.#....#..#..L#",
            "#.#..#......#....#..#...#",
            "#.#..##############..####",
            "#.#.................#...#",
            "#.#...L...Z.........#.X.#",
            "#.#.................#...#",
            "#.###################...#",
            "#.......................#",
            "#########################",
        };

        /// <summary>A plain walled room of the given size, for the sandbox.</summary>
        public static string[] EmptyRoom(int width, int height)
        {
            var rows = new string[height];
            for (int y = 0; y < height; y++)
            {
                var row = new StringBuilder(width);
                for (int x = 0; x < width; x++)
                {
                    bool border = x == 0 || x == width - 1 || y == 0 || y == height - 1;
                    row.Append(border ? '#' : '.');
                }

                rows[y] = row.ToString();
            }

            return rows;
        }
    }
}
