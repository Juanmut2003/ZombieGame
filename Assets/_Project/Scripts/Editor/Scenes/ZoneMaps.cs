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
        /// The town: a crossroads with houses, a school, a park, a filling station and a market,
        /// drawn after the reference picture.
        /// </summary>
        /// <remarks>
        /// The tutorial zone, and laid out to teach rather than to test. The squad arrives at the
        /// bottom of the map and the way out is at the top, so the whole run is one journey across
        /// town: the buildings on the route hold what it was sent for, and the streets between
        /// them are open enough to see trouble coming and wide enough to walk round it.
        /// The park in the north is the one place the way out can be seen from a distance, which
        /// is what makes the last stretch a decision rather than a search.
        /// </remarks>
        public static string[] Town { get; } =
        {
            "#############################################################",
            "#ggggggggggggggggprrpgggggggggXXgggggggggprrpggggggggggggggg#",
            "#ggggggggggggggggprrpgggggggggppgggggggggprrpggggggggggggggg#",
            "#gg###########gggprrpgggtgggggppgtgggggggprrpgg###########gg#",
            "#gg#.........#gggprrpgggggggggppggggtggggprrpgg#.........#gg#",
            "#gg#.L.......#gggprrpgggggggggppgggggggggprrpgg#.L.......#gg#",
            "#gg#.......L.#gggprrpggggtggggppgggggggggprrpgg#.......L.#gg#",
            "#gg#.........#gggprrpgggggggggppgggggtgggprrpgg#.........#gg#",
            "#gg#####.#####gggprrpggggggtggppggZggggggprrpgg#####.#####gg#",
            "#ggggggggggggggggprrpgggggggggppgggggggggprrpggggggggggggggg#",
            "#ppppppppppppppppprrpppppppppppppppppppppprrpppppppppppppppp#",
            "#rrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrr#",
            "#rrrrrrrrrrrrrrrrrrrrZrrrrrrrrrrrrrrrrrrrrrrrrrZrrrrrrrrrrrr#",
            "#rrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrr#",
            "#ppppppppppppppppprrpppppppppppppppppppppprrpppppppppppppppp#",
            "#ggggggggggggggggprrpggggggggggggggggggggprrpggggggggggggggg#",
            "#ggggggggggggggggprrpgg#################gprrpggggggggggggggg#",
            "#gg#####.#####gggprrpgg#...............#gprrpgg#####.#####gg#",
            "#gg#.........#gggprrpgg#.L....L.....L..#gprrpgg#.........#gg#",
            "#gg#.L.......#gggprrpgg#....Z......Z...#gprrpgg#.L.......#gg#",
            "#gg#.......L.#gggprrpgg#.........L.....#gprrpgg#.......L.#gg#",
            "#gg#.........#gggprrpgg#...............#gprrpgg#.........#gg#",
            "#gg###########gggprrpgg###.####.########gprrpgg###########gg#",
            "#ggggggggggggggggprrpggpppppppppppppppppgprrpggggggggggggggg#",
            "#ppppppppppppppppprrpppppppppppppppppppppprrpppppppppppppppp#",
            "#rrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrr#",
            "#rrrrrrrrrrrrrZrrrrrrrrrrrrrrrrrrrrrrrZrrrrrrrrrrrrrrrrrrrrr#",
            "#rrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrr#",
            "#ppppppppppppppppprrpppppppppppppppppppppprrpppppppppppppppp#",
            "#ggggpppppppppppgprrpggggggggZgggggggggggprrpgppppppppppppgg#",
            "#ggggpppZpppppppgprrpggg####.#####gggggggprrpgppppZpppppppgg#",
            "#gggg#####.#####gprrpggg#........#gggggggprrpg#####.######gg#",
            "#gggg#.........#gprrpggg#.L....L.#gggggggprrpg#..........#gg#",
            "#gggg#.L.....L.#gprrpggg#........#gggggggprrpg#.L...L..L.#gg#",
            "#gggg###########gprrpggg##########gggggggprrpg############gg#",
            "#ggggggggggggggggprrpggggggggggggggggggggprrpggggggggggggggg#",
            "#ppppppppppppppppprrpppppppppppppppppppppprrpppppppppppppppp#",
            "#rrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrr#",
            "#rrrrrrrrrrrrrrrrrrrrrrrZrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrr#",
            "#rrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrr#",
            "#ppppppppppppppppprrpppppppppppppppppppppprrpppppppppppppppp#",
            "#ggggg####.####ggprrpggggg####.#####gggggprrpg####.#####gggg#",
            "#ggggg#.......#ggprrpggggg#...P....#gggggprrpg#........#gggg#",
            "#ggggg#.L.....#ggprrpggggg#.L......#gggggprrpg#.....L..#gggg#",
            "#############################################################",
        };

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
