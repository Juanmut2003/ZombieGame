#nullable enable

namespace ZombieGame.Editor.Scenes
{
    /// <summary>
    /// Reads a drawn map and says what kind of ground each cell is.
    /// </summary>
    /// <remarks>
    /// The legend in <see cref="ZoneMaps"/> mixes two things that look alike in a text file and
    /// nothing alike on screen: four characters say what the ground is, and five more say what is
    /// standing on it. Treating the second group as ground is how a zombie nest in the middle of a
    /// street came out as a grey square in the tarmac — correct by the letter of the map, plainly
    /// wrong to look at.
    /// Kept apart from the scene builder because it is pure map arithmetic, which CLAUDE.md asks
    /// to be testable without opening the editor, and because it is the half that is easy to get
    /// subtly wrong.
    /// </remarks>
    public static class ZoneGround
    {
        /// <summary>Open grass or garden.</summary>
        public const char Grass = 'g';

        /// <summary>Roadway.</summary>
        public const char Road = 'r';

        /// <summary>Pavement along a road.</summary>
        public const char Pavement = 'p';

        /// <summary>A floor indoors.</summary>
        public const char Indoors = '.';

        /// <summary>The threshold of a door: floor, but worth being able to see from outside.</summary>
        public const char Doorway = 'd';

        /// <summary>
        /// What the cell at <paramref name="column"/>, <paramref name="row"/> is standing on,
        /// looking past whatever the map put on top of it.
        /// </summary>
        public static char Under(string[] rows, int column, int row)
        {
            if (row < 0 || row >= rows.Length || column < 0 || column >= rows[row].Length)
            {
                // Off the map is not ground of any kind. Said once, here, so that no caller has
                // to guess what the eight cells around a corner cell were.
                return Indoors;
            }

            char marker = At(rows, column, row);
            if (marker is Grass or Road or Pavement)
            {
                return marker;
            }

            if (IsDoorway(rows, column, row))
            {
                return Doorway;
            }

            // A marker takes the ground of what it actually touches — the four cells sharing an
            // edge with it, and only those. The diagonals are consulted when the four say
            // nothing, and never otherwise: a pavement one cell wide has a road along one edge
            // and road on both of the diagonals past it, so counting all eight puts the squad's
            // own starting cell in the middle of the carriageway.
            char touching = Commonest(rows, column, row, diagonals: false);
            return touching != '\0' ? touching : Commonest(rows, column, row, diagonals: true);
        }

        /// <summary>The ground most of a cell's neighbours are, or nul when none of them is ground.</summary>
        private static char Commonest(string[] rows, int column, int row, bool diagonals)
        {
            int grass = 0, road = 0, pavement = 0, indoors = 0;
            for (int downwards = -1; downwards <= 1; downwards++)
            {
                for (int across = -1; across <= 1; across++)
                {
                    bool diagonal = downwards != 0 && across != 0;
                    if ((downwards == 0 && across == 0) || diagonal != diagonals)
                    {
                        continue;
                    }

                    switch (At(rows, column + across, row + downwards))
                    {
                        case Grass: grass++; break;
                        case Road: road++; break;
                        case Pavement: pavement++; break;
                        case Indoors: indoors++; break;
                    }
                }
            }

            if (grass == 0 && road == 0 && pavement == 0)
            {
                return indoors > 0 || diagonals ? Indoors : '\0';
            }

            // Indoors wins a tie, because a crate in a room is ringed by the room's own walls and
            // sees very little of anything else.
            if (indoors >= grass && indoors >= road && indoors >= pavement)
            {
                return Indoors;
            }

            if (grass >= road && grass >= pavement)
            {
                return Grass;
            }

            return road >= pavement ? Road : Pavement;
        }

        /// <summary>
        /// Whether this cell is a gap through a wall: floor, with wall on both sides of it.
        /// </summary>
        public static bool IsDoorway(string[] rows, int column, int row)
        {
            if (At(rows, column, row) != Indoors)
            {
                return false;
            }

            return (IsWall(rows, column - 1, row) && IsWall(rows, column + 1, row))
                   || (IsWall(rows, column, row - 1) && IsWall(rows, column, row + 1));
        }

        /// <summary>
        /// Whether this wall is the front of a building: the side facing down the screen.
        /// </summary>
        /// <remarks>
        /// True when the cell below is outside and the cell above is not. The second half matters
        /// as much as the first: a single-thickness garden wall is open on both sides and is not
        /// anybody's frontage, and painting a house front on it would leave a facade standing in
        /// a field with nothing behind it.
        /// </remarks>
        public static bool IsFrontWall(string[] rows, int column, int row)
        {
            if (At(rows, column, row) != '#')
            {
                return false;
            }

            // Rows are written downwards, so the cell below on the screen is the next row.
            char below = At(rows, column, row + 1);
            char above = At(rows, column, row - 1);

            bool outsideBelow = below is Grass or Road or Pavement;
            bool insideAbove = above is '#' or Indoors;
            return outsideBelow && insideAbove;
        }

        /// <summary>
        /// Whether this cell is the middle of a street running north to south, or east to west.
        /// </summary>
        /// <remarks>
        /// The centre of a three-wide street has road either side of it and something else beyond
        /// that. At a crossing both hold at once and neither is reported: a junction with lane
        /// markings painted straight through reads as two roads drawn over each other.
        /// </remarks>
        public static bool IsStreetCentre(string[] rows, int column, int row, out bool vertical)
        {
            bool northSouth = IsRoad(rows, column - 1, row) && IsRoad(rows, column + 1, row)
                              && !IsRoad(rows, column - 2, row) && !IsRoad(rows, column + 2, row);
            bool eastWest = IsRoad(rows, column, row - 1) && IsRoad(rows, column, row + 1)
                            && !IsRoad(rows, column, row - 2) && !IsRoad(rows, column, row + 2);

            vertical = northSouth;
            return northSouth != eastWest;
        }

        private static bool IsRoad(string[] rows, int column, int row) =>
            Under(rows, column, row) == Road;

        private static bool IsWall(string[] rows, int column, int row) =>
            At(rows, column, row) == '#';

        /// <summary>The character at a cell, or a wall for anything off the map.</summary>
        private static char At(string[] rows, int column, int row) =>
            row >= 0 && row < rows.Length && column >= 0 && column < rows[row].Length
                ? rows[row][column]
                : '#';
    }
}
