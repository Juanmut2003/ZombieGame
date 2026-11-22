#nullable enable
using System.Collections.Generic;

namespace ZombieGame.Base
{
    /// <summary>
    /// What the base's wiring adds up to, and what gets switched off when it does not.
    /// </summary>
    /// <remarks>
    /// Power is the one thing a colony keeps that is not kept in crates: it is made and used in
    /// the same moment, and a shortfall is not a store running low but lights going out tonight.
    /// So it is worked out fresh each day rather than stored as a resource.
    /// A deficit browns out whole buildings rather than slowing everything down a little.
    /// Half a farm is nothing anybody can picture; a farm that did not run today is.
    /// PROJECT.md 4.3 says the buildings that go dark are picked at random. They are picked
    /// newest-first here instead. Random would mean a colony that cannot be reasoned about —
    /// the player would watch a different building fail each day with no way to plan around it —
    /// and a test that needs a seeded generator to say anything. Newest-first is the same
    /// pressure and can be understood: the thing you just plugged in is the thing that trips.
    /// </remarks>
    public static class PowerGrid
    {
        /// <summary>Works out the day's wiring.</summary>
        /// <param name="site">What is standing.</param>
        /// <param name="running">
        /// Filled with the number of every building that has power. Cleared first.
        /// </param>
        public static PowerReport Resolve(BuildSite site, HashSet<int> running)
        {
            running.Clear();

            int supply = 0;
            int draw = 0;

            // Generators first and always on: a generator that browned itself out would take
            // the whole grid down with it and could never come back.
            foreach (PlacedBuilding building in site.Buildings)
            {
                BuildingLevel? rung = building.Rung;
                if (rung is null)
                {
                    continue;
                }

                if (rung.IsGenerator)
                {
                    supply += rung.PowerSupply;
                    running.Add(building.Id);
                }
            }

            // Then everything that needs nothing. A barracks with no draw is never dark.
            foreach (PlacedBuilding building in site.Buildings)
            {
                BuildingLevel? rung = building.Rung;
                if (rung is null || rung.IsGenerator)
                {
                    continue;
                }

                if (rung.PowerDraw == 0)
                {
                    running.Add(building.Id);
                }
            }

            // Then the rest, oldest first, until the supply runs out. Walking oldest-first is
            // what makes the newest the one that goes dark.
            foreach (PlacedBuilding building in site.Buildings)
            {
                BuildingLevel? rung = building.Rung;
                if (rung is null || rung.IsGenerator || rung.PowerDraw == 0)
                {
                    continue;
                }

                if (draw + rung.PowerDraw <= supply)
                {
                    draw += rung.PowerDraw;
                    running.Add(building.Id);
                }
            }

            int wanted = 0;
            foreach (PlacedBuilding building in site.Buildings)
            {
                BuildingLevel? rung = building.Rung;
                if (rung is not null && !rung.IsGenerator)
                {
                    wanted += rung.PowerDraw;
                }
            }

            return new PowerReport(supply, wanted, draw);
        }
    }
}
