#nullable enable
using System.Collections.Generic;
using ZombieGame.Economy;

namespace ZombieGame.Base
{
    /// <summary>
    /// A day at the base: the lights come on, everyone works, then everyone eats and drinks.
    /// </summary>
    /// <remarks>
    /// Plain static maths over the colony, the plot and the stores, so a week can be run in a
    /// test in a millisecond instead of by sitting through it. This is the loop the whole base
    /// half of the game is built on, and the order of its parts is the design:
    /// the grid is settled first, because a building with no power did not work today and
    /// cannot be counted as having done; work comes in before meals go out, so that a farm can
    /// feed the people who worked it; and the generators burn their fuel whether or not anybody
    /// liked what the day produced.
    /// </remarks>
    public static class ColonyDay
    {
        /// <summary>Runs one day and says what it came to.</summary>
        /// <param name="colony">Who is here and where they work.</param>
        /// <param name="site">What is standing.</param>
        /// <param name="stores">What the day is paid into and out of.</param>
        /// <param name="foodPerPerson">What one person eats in a day.</param>
        /// <param name="waterPerPerson">What one person drinks in a day.</param>
        public static DayReport Resolve(
            Colony colony,
            BuildSite site,
            Stockpile stores,
            int foodPerPerson,
            int waterPerPerson = 0)
        {
            var running = new HashSet<int>();
            PowerReport power = PowerGrid.Resolve(site, running);

            int worked = 0;
            int idle = 0;
            int darkened = 0;
            int foodBefore = stores.AmountOf(ResourceKind.Food);

            foreach (PlacedBuilding building in site.Buildings)
            {
                BuildingLevel? rung = building.Rung;
                if (rung is null)
                {
                    continue;
                }

                bool lit = running.Contains(building.Id);
                if (!lit)
                {
                    darkened++;
                }

                // A generator burns its fuel for being switched on, not for being useful. Running
                // one for a base that needs nothing is a decision the player gets to regret.
                if (rung.FuelPerDay > 0 && lit)
                {
                    if (!stores.TrySpend(ResourceKind.Fuel, rung.FuelPerDay))
                    {
                        // Out of fuel: it did not run after all, and neither did whatever it was
                        // keeping alive. Settled next day — a grid that reshuffled itself inside
                        // one day would be a day nobody could follow.
                        darkened++;
                    }
                }

                int hands = colony.WorkersAt(building.Id);
                if (hands == 0)
                {
                    continue;
                }

                if (!lit)
                {
                    // The people are still at their posts; there was simply nothing to run. They
                    // are counted as idle, which is what the day will have looked like to them.
                    idle += hands;
                    continue;
                }

                // Water is drawn by the building, not by the worker: a farm's beds want the same
                // soaking whether one person or three turned up to tend them.
                if (rung.WaterPerDay > 0 && !stores.TrySpend(ResourceKind.Water, rung.WaterPerDay))
                {
                    idle += hands;
                    continue;
                }

                worked += hands;

                foreach (ResourceAmount line in rung.YieldPerWorker)
                {
                    stores.Add(line.Kind, line.Amount * hands);
                }
            }

            foreach (Survivor survivor in colony.People)
            {
                if (survivor.IsIdle)
                {
                    idle++;
                }
            }

            int foodGrown = stores.AmountOf(ResourceKind.Food) - foodBefore;

            // Meals and drinks one person at a time rather than as one bill, because a colony
            // that is two short should feed everybody it can and leave two wanting — not refuse
            // to feed anyone and sit on a full larder.
            int eaten = 0;
            int hungry = 0;
            int thirsty = 0;
            int meal = foodPerPerson < 0 ? 0 : foodPerPerson;
            int drink = waterPerPerson < 0 ? 0 : waterPerPerson;

            foreach (Survivor survivor in colony.People)
            {
                if (meal > 0)
                {
                    if (stores.TrySpend(ResourceKind.Food, meal))
                    {
                        eaten += meal;
                    }
                    else
                    {
                        hungry++;
                    }
                }

                if (drink > 0 && !stores.TrySpend(ResourceKind.Water, drink))
                {
                    thirsty++;
                }
            }

            return new DayReport(worked, idle, foodGrown, eaten, hungry, thirsty, darkened, power);
        }
    }
}
