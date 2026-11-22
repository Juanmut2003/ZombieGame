#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieGame.Economy;

namespace ZombieGame.Base
{
    /// <summary>
    /// A colony between scenes: its stores, its people, its buildings and its days.
    /// </summary>
    /// <remarks>
    /// PROJECT.md 4.6. A raid unloads the base scene, and everything the base scene owns goes
    /// with it — the plot is rebuilt in <c>Awake</c>, the colony in <c>Start</c>, the stores from
    /// a config. Without something that outlives both loads, coming home means arriving at a
    /// brand new colony with the same name.
    /// What is kept here is a snapshot and not the live objects. Holding the live
    /// <see cref="BuildSite"/> would mean holding a grid measured from an origin that belongs to
    /// a scene which no longer exists; a list of what stood where is portable, and it is also
    /// the shape a save file wants, which is where this goes next.
    /// The stores are the exception and are the live ledger: there is nothing scene-shaped about
    /// a count of four numbers, and a snapshot of them would only be a copy to keep in step.
    /// </remarks>
    public sealed class ColonyRun
    {
        private readonly List<BuildingRecord> _buildings = new();
        private readonly List<SurvivorRecord> _people = new();

        private readonly ColonyLedger _ledger;

        /// <param name="ledger">The stores the whole run spends out of.</param>
        public ColonyRun(ColonyLedger ledger) => _ledger = ledger;

        /// <summary>One building, as something a scene can be rebuilt from.</summary>
        public readonly struct BuildingRecord
        {
            public BuildingRecord(
                string what, Vector2Int cell, BuildingFootprint footprint, int level)
            {
                What = what;
                Cell = cell;
                Footprint = footprint;
                Level = level;
            }

            /// <summary>The display name of what it is, which is how it is found again.</summary>
            public string What { get; }

            /// <summary>The cell its bottom-left corner stands on.</summary>
            public Vector2Int Cell { get; }

            /// <summary>How much ground it covers as it stands, already turned if it was.</summary>
            public BuildingFootprint Footprint { get; }

            /// <summary>Which rung of its ladder it has reached.</summary>
            public int Level { get; }
        }

        /// <summary>One person, and where they were working.</summary>
        public readonly struct SurvivorRecord
        {
            public SurvivorRecord(string name, int workplace)
            {
                Name = name;
                Workplace = workplace;
            }

            /// <summary>What everybody calls them.</summary>
            public string Name { get; }

            /// <summary>The building they were working in, or zero.</summary>
            public int Workplace { get; }
        }

        /// <summary>The run's stores. The same object from the menu to the end.</summary>
        public Stockpile Stores => _ledger.Stores;

        /// <summary>How many days the colony has lived through.</summary>
        public int DaysLived { get; private set; }

        /// <summary>How many supplies have been brought home over the whole run.</summary>
        public int Hauled { get; private set; }

        /// <summary>True once there is a colony to come back to.</summary>
        public bool HasSnapshot { get; private set; }

        /// <summary>What was standing on the plot, in the order it went up.</summary>
        public IReadOnlyList<BuildingRecord> Buildings => _buildings;

        /// <summary>Who was living here, in the order they arrived.</summary>
        public IReadOnlyList<SurvivorRecord> People => _people;

        /// <summary>
        /// The day the squad last went out, or zero when they never have.
        /// </summary>
        /// <remarks>
        /// A day number rather than a flag that something has to remember to clear. A flag is
        /// one missed reset away from a colony that can never raid again, and that bug only
        /// shows itself on the second day.
        /// </remarks>
        public int LastRaidDay { get; private set; }

        /// <summary>The calendar day the colony is on. Counts from one.</summary>
        public int Today => DaysLived + 1;

        /// <summary>True while the squad can still go out today. One trip a day.</summary>
        public bool CanRaid => LastRaidDay != Today;

        /// <summary>Counts a day that has been resolved.</summary>
        public void DayPassed() => DaysLived++;

        /// <summary>Spends today's trip out.</summary>
        /// <returns>False when the squad has already been out today.</returns>
        public bool TrySpendTheDaysRaid()
        {
            if (!CanRaid)
            {
                return false;
            }

            LastRaidDay = Today;
            return true;
        }

        /// <summary>
        /// Puts a haul into the stores, store by store, and remembers it came home.
        /// </summary>
        /// <remarks>
        /// <see cref="Hauled"/> counts the whole haul and not the salvage in it, because what
        /// the opening objectives ask for is a quantity brought back (PROJECT.md §4.6) and a
        /// run that came home with nothing but water has done what it was asked.
        /// </remarks>
        public void Bank(IReadOnlyList<ResourceAmount>? haul)
        {
            if (haul is null)
            {
                return;
            }

            foreach (ResourceAmount amount in haul)
            {
                if (amount.Amount <= 0)
                {
                    continue;
                }

                Hauled += amount.Amount;
                Stores.Add(amount.Kind, amount.Amount);
            }
        }

        /// <summary>Puts salvage into the stores, for a zone with no loot table behind it.</summary>
        public void Bank(int supplies) =>
            Bank(new[] { new ResourceAmount(ResourceKind.Materials, supplies) });

        /// <summary>Writes down the colony as it stands, so that it can be put back.</summary>
        public void Capture(BuildSite site, Colony? colony, int daysLived)
        {
            _buildings.Clear();
            _people.Clear();

            foreach (PlacedBuilding building in site.Buildings)
            {
                _buildings.Add(new BuildingRecord(
                    building.Definition == null ? string.Empty : building.Definition.DisplayName,
                    building.Cell,
                    building.Footprint,
                    building.Level));
            }

            if (colony is not null)
            {
                foreach (Survivor survivor in colony.People)
                {
                    _people.Add(new SurvivorRecord(survivor.Name, survivor.Workplace));
                }
            }

            DaysLived = daysLived;
            HasSnapshot = true;
        }

        /// <summary>
        /// Puts the colony back onto a freshly built plot.
        /// </summary>
        /// <remarks>
        /// In the order it was captured, which is what makes the numbers line up again: the site
        /// hands out building numbers in the order things are placed, and a survivor remembers
        /// the number of the building they worked in. Restore them out of order and the farmhand
        /// turns up in the armoury.
        /// A building whose definition is no longer in the catalogue is dropped rather than
        /// guessed at, and anybody who worked in it simply comes back idle.
        /// </remarks>
        /// <returns>The numbers of the buildings that went back up.</returns>
        public List<int> Restore(
            BuildSite site, Colony colony, IReadOnlyList<BuildingDefinition> catalogue)
        {
            var raised = new List<int>();
            foreach (BuildingRecord record in _buildings)
            {
                BuildingDefinition? what = Match(catalogue, record.What);
                if (what == null)
                {
                    Debug.LogWarning(
                        $"[Run] nothing in the catalogue is called {record.What}; it is gone.");
                    continue;
                }

                if (site.TryRestore(record.Footprint, record.Cell, what, record.Level, out int id))
                {
                    raised.Add(id);
                }
            }

            foreach (SurvivorRecord record in _people)
            {
                Survivor survivor = colony.AddFounder(record.Name);
                if (record.Workplace != 0)
                {
                    colony.Assign(survivor.Id, record.Workplace);
                }
            }

            return raised;
        }

        private static BuildingDefinition? Match(
            IReadOnlyList<BuildingDefinition> catalogue, string displayName)
        {
            foreach (BuildingDefinition building in catalogue)
            {
                if (building != null
                    && string.Equals(building.DisplayName, displayName, StringComparison.Ordinal))
                {
                    return building;
                }
            }

            return null;
        }
    }
}
