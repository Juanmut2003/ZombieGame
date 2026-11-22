#nullable enable
using System.Collections.Generic;

namespace ZombieGame.Base
{
    /// <summary>One thing the colony has been asked to do, and whether it has.</summary>
    public readonly struct ColonyTask
    {
        public ColonyTask(string title, bool done, string detail)
        {
            Title = title;
            Done = done;
            Detail = detail;
        }

        /// <summary>What to do, in as few words as it takes.</summary>
        public string Title { get; }

        /// <summary>True once the colony has it.</summary>
        public bool Done { get; }

        /// <summary>Where it stands, when a count says more than a tick.</summary>
        public string Detail { get; }
    }

    /// <summary>
    /// What a new colony is asked to do, read off the colony itself.
    /// </summary>
    /// <remarks>
    /// PROJECT.md 4.6. Derived rather than scripted: there is no step counter and no "tutorial
    /// state" to get out of step with the base. A barracks that is torn down un-ticks its line,
    /// a worker sent home un-ticks theirs, and a colony loaded from a save is correct without
    /// anything having to replay what it did.
    /// Plain C# over the plot, the people and the run, so the awkward half — what counts as a
    /// farm, what counts as working — is settled in tests instead of by playing three days and
    /// squinting at a panel.
    /// </remarks>
    public static class ColonyTasks
    {
        /// <summary>How many things the colony is asked for.</summary>
        public const int Count = 4;

        /// <summary>
        /// The list, in the order it is read.
        /// </summary>
        /// <param name="site">The plot, for what is standing on it.</param>
        /// <param name="colony">The people, for who is working. Null before the colony exists.</param>
        /// <param name="run">The run, for what has been brought home. Null outside a run.</param>
        /// <param name="haulWanted">How many supplies one raid is asked to bring back.</param>
        public static IReadOnlyList<ColonyTask> Describe(
            BuildSite site, Colony? colony, ColonyRun? run, int haulWanted)
        {
            var tasks = new List<ColonyTask>(Count)
            {
                Standing(site, BuildingCategory.Barracks, "BUILD A BARRACKS"),
                Standing(site, BuildingCategory.Farm, "BUILD A FARM"),
                Working(colony),
                Hauled(run, haulWanted),
            };

            return tasks;
        }

        /// <summary>True once every line is ticked.</summary>
        public static bool AllDone(IReadOnlyList<ColonyTask> tasks)
        {
            foreach (ColonyTask task in tasks)
            {
                if (!task.Done)
                {
                    return false;
                }
            }

            return tasks.Count > 0;
        }

        /// <summary>
        /// Whether something of a kind is up, by category rather than by name.
        /// </summary>
        /// <remarks>
        /// A greenhouse is a farm. Matching on the display name would mean the objective quietly
        /// stops being satisfiable the first time the catalogue gains a second kind of farm.
        /// </remarks>
        private static ColonyTask Standing(BuildSite site, BuildingCategory kind, string title)
        {
            foreach (PlacedBuilding building in site.Buildings)
            {
                if (building.Definition != null && building.Definition.Category == kind)
                {
                    return new ColonyTask(title, true, "UP");
                }
            }

            return new ColonyTask(title, false, string.Empty);
        }

        private static ColonyTask Working(Colony? colony)
        {
            const string title = "PUT SOMEBODY TO WORK";
            if (colony is null)
            {
                return new ColonyTask(title, false, string.Empty);
            }

            int working = 0;
            foreach (Survivor survivor in colony.People)
            {
                if (!survivor.IsIdle)
                {
                    working++;
                }
            }

            return new ColonyTask(
                title, working > 0, working == 0 ? string.Empty : $"{working} AT WORK");
        }

        /// <summary>
        /// The raid, counted over the whole run rather than per trip.
        /// </summary>
        /// <remarks>
        /// Two trips of five count. Asking for ten in one go would make a run that is going
        /// badly unfinishable, and the objective is to go out and come back with something, not
        /// to perform it in a single visit.
        /// </remarks>
        private static ColonyTask Hauled(ColonyRun? run, int wanted)
        {
            int target = wanted < 0 ? 0 : wanted;
            int home = run is null ? 0 : run.Hauled;
            return new ColonyTask(
                $"RAID THE TOWN FOR {target} SUPPLIES",
                home >= target,
                $"{home} / {target}");
        }
    }
}
