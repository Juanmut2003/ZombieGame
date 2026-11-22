#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZombieGame.Base
{
    /// <summary>
    /// A claimed plot: which ground can be built on, and what is standing on it.
    /// </summary>
    /// <remarks>
    /// Plain C# with no scene behind it, so the awkward half of base building — does this fit,
    /// what is under it, what happens when it comes down — can be settled in tests rather than
    /// by dragging a barn around with a mouse. PROJECT.md 4.2 asks for free placement on
    /// buildable tiles, which is two questions per cell: may anything stand here, and is
    /// something standing here already.
    /// A rectangle of cells, like every other grid in the game, one world unit to the cell.
    /// Real sites are not rectangles — a petrol station has a forecourt and a shop — but the
    /// parts that are not plot are blocked ground, which is the same answer by a shorter road.
    /// </remarks>
    public sealed class BuildSite
    {
        private readonly bool[] _blocked;
        private readonly int[] _occupant;
        private readonly List<PlacedBuilding> _buildings = new();
        private int _nextId = 1;

        /// <param name="origin">The world position of the bottom-left cell's corner.</param>
        /// <param name="width">How many cells across.</param>
        /// <param name="height">How many cells up.</param>
        public BuildSite(Vector2 origin, int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(width), $"{width}x{height}", "A site has to have some ground in it.");
            }

            Origin = origin;
            Width = width;
            Height = height;
            _blocked = new bool[width * height];
            _occupant = new int[width * height];
        }

        /// <summary>The world position of the bottom-left corner.</summary>
        public Vector2 Origin { get; }

        /// <summary>How many cells across.</summary>
        public int Width { get; }

        /// <summary>How many cells up.</summary>
        public int Height { get; }

        /// <summary>Everything standing on the site, in the order it went up.</summary>
        public IReadOnlyList<PlacedBuilding> Buildings => _buildings;

        /// <summary>Marks ground nothing may stand on: rock, water, a road, a standing wall.</summary>
        public void Block(int column, int row, bool blocked = true)
        {
            if (!Holds(column, row))
            {
                return;
            }

            _blocked[Index(column, row)] = blocked;
        }

        /// <summary>True when the ground here is barred, or there is no ground here at all.</summary>
        public bool IsBlocked(int column, int row) =>
            !Holds(column, row) || _blocked[Index(column, row)];

        /// <summary>Which building stands on this cell, or zero for none.</summary>
        public int OccupantAt(int column, int row) =>
            Holds(column, row) ? _occupant[Index(column, row)] : 0;

        /// <summary>True when the site has a cell at these coordinates at all.</summary>
        public bool Holds(int column, int row) =>
            (uint)column < (uint)Width && (uint)row < (uint)Height;

        /// <summary>
        /// Whether a building of this shape may stand with its corner on this cell.
        /// </summary>
        /// <remarks>
        /// Every cell it would cover is checked, not only the corner. Checking one cell is the
        /// bug that lets a four-by-four warehouse hang off the plot by three quarters.
        /// </remarks>
        public PlacementVerdict Check(BuildingFootprint footprint, Vector2Int cell)
        {
            for (int row = 0; row < footprint.Height; row++)
            {
                for (int column = 0; column < footprint.Width; column++)
                {
                    int x = cell.x + column;
                    int y = cell.y + row;

                    if (!Holds(x, y))
                    {
                        return PlacementVerdict.OutsideTheSite;
                    }

                    if (_blocked[Index(x, y)])
                    {
                        return PlacementVerdict.GroundIsBlocked;
                    }

                    if (_occupant[Index(x, y)] != 0)
                    {
                        return PlacementVerdict.SomethingIsThere;
                    }
                }
            }

            return PlacementVerdict.Ok;
        }

        /// <summary>Puts a shape down, for tests and for anything with no asset behind it.</summary>
        public bool TryPlace(BuildingFootprint footprint, Vector2Int cell, out int id) =>
            TryPlace(footprint, cell, null, out id);

        /// <summary>Puts a building down if it fits.</summary>
        /// <returns>True when it went up; <paramref name="id"/> is zero when it did not.</returns>
        public bool TryPlace(
            BuildingFootprint footprint, Vector2Int cell, BuildingDefinition? what, out int id)
        {
            id = 0;
            if (Check(footprint, cell) != PlacementVerdict.Ok)
            {
                return false;
            }

            id = _nextId++;
            Fill(footprint, cell, id);
            _buildings.Add(new PlacedBuilding(id, cell, footprint, what));
            return true;
        }

        /// <summary>
        /// Whether a building could grow into the shape its next level needs.
        /// </summary>
        /// <remarks>
        /// Its own ground does not count against it — a building growing from three cells wide
        /// to four is standing on three of the four it will need, and checking them as occupied
        /// would make every upgrade impossible.
        /// Growth is up and to the right from the corner it was put down on, so the corner never
        /// moves and a building cannot creep across the plot one level at a time.
        /// </remarks>
        public PlacementVerdict CheckUpgrade(int id, BuildingFootprint grown)
        {
            PlacedBuilding? standing = Find(id);
            if (standing is null)
            {
                return PlacementVerdict.OutsideTheSite;
            }

            Vector2Int cell = standing.Value.Cell;
            for (int row = 0; row < grown.Height; row++)
            {
                for (int column = 0; column < grown.Width; column++)
                {
                    int x = cell.x + column;
                    int y = cell.y + row;

                    if (!Holds(x, y))
                    {
                        return PlacementVerdict.OutsideTheSite;
                    }

                    if (_blocked[Index(x, y)])
                    {
                        return PlacementVerdict.GroundIsBlocked;
                    }

                    int occupant = _occupant[Index(x, y)];
                    if (occupant != 0 && occupant != id)
                    {
                        return PlacementVerdict.SomethingIsThere;
                    }
                }
            }

            return PlacementVerdict.Ok;
        }

        /// <summary>Grows a building onto the next rung of its ladder.</summary>
        public bool TryUpgrade(int id, BuildingFootprint grown)
        {
            if (CheckUpgrade(id, grown) != PlacementVerdict.Ok)
            {
                return false;
            }

            for (int i = 0; i < _buildings.Count; i++)
            {
                if (_buildings[i].Id != id)
                {
                    continue;
                }

                PlacedBuilding was = _buildings[i];

                // The old shape is released first. Without that, a building that grew in one
                // direction and shrank in another would keep claiming ground it no longer
                // stands on, and nothing could ever be built there again.
                Fill(was.Footprint, was.Cell, 0);
                Fill(grown, was.Cell, id);

                _buildings[i] = new PlacedBuilding(
                    id, was.Cell, grown, was.Definition, was.Level + 1);
                return true;
            }

            return false;
        }

        /// <summary>Finds what is standing under a number.</summary>
        public PlacedBuilding? Find(int id)
        {
            foreach (PlacedBuilding building in _buildings)
            {
                if (building.Id == id)
                {
                    return building;
                }
            }

            return null;
        }

        /// <summary>Takes a building down and gives its ground back.</summary>
        public bool Remove(int id)
        {
            if (id == 0)
            {
                return false;
            }

            for (int i = 0; i < _buildings.Count; i++)
            {
                if (_buildings[i].Id != id)
                {
                    continue;
                }

                PlacedBuilding gone = _buildings[i];
                Fill(gone.Footprint, gone.Cell, 0);
                _buildings.RemoveAt(i);
                return true;
            }

            return false;
        }

        /// <summary>Which cell a point in the world falls on. May be off the site.</summary>
        public Vector2Int CellAt(Vector2 world) =>
            new(
                Mathf.FloorToInt(world.x - Origin.x),
                Mathf.FloorToInt(world.y - Origin.y));

        /// <summary>Where in the world the bottom-left corner of a cell is.</summary>
        public Vector2 CornerOf(Vector2Int cell) => new(Origin.x + cell.x, Origin.y + cell.y);

        /// <summary>Where in the world the middle of a building on this cell would be.</summary>
        public Vector2 CentreOf(Vector2Int cell, BuildingFootprint footprint) =>
            footprint.CentreFrom(CornerOf(cell));

        /// <summary>
        /// The cell to put a building's corner on so that its middle lands nearest a point.
        /// </summary>
        /// <remarks>
        /// What the mouse actually wants. Dragging a three-by-three shed around by its corner
        /// leaves it trailing a cell and a half behind the pointer, which reads as lag rather
        /// than as holding something.
        /// </remarks>
        public Vector2Int CornerUnder(Vector2 world, BuildingFootprint footprint) =>
            new(
                Mathf.FloorToInt(world.x - Origin.x - (footprint.Width * 0.5f) + 0.5f),
                Mathf.FloorToInt(world.y - Origin.y - (footprint.Height * 0.5f) + 0.5f));

        private void Fill(BuildingFootprint footprint, Vector2Int cell, int id)
        {
            for (int row = 0; row < footprint.Height; row++)
            {
                for (int column = 0; column < footprint.Width; column++)
                {
                    int x = cell.x + column;
                    int y = cell.y + row;
                    if (Holds(x, y))
                    {
                        _occupant[Index(x, y)] = id;
                    }
                }
            }
        }

        private int Index(int column, int row) => (row * Width) + column;
    }
}
