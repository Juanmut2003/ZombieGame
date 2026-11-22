#nullable enable
using System;
using System.Collections.Generic;

namespace ZombieGame.Economy
{
    /// <summary>
    /// One trip out of the base: where home is, and what the squad came back with.
    /// </summary>
    /// <remarks>
    /// The handshake between a base and a zone, and deliberately the smallest one that works.
    /// A zone must not know what a colony is — the whole point of PROJECT.md §8 is that a mission
    /// is the same mission wherever it was started from — and a colony must not have to reach
    /// into a zone to find out how it went. So the two share a few fields and nothing else: the
    /// base says where to come back to, the zone says what it banked, and the base reads that on
    /// the way in.
    /// Here beside the stores rather than in <c>Core</c>, where it began, because what it carries
    /// is stores. The base layer and the mission layer cannot see each other, and this is the
    /// lowest assembly both of them can see that knows what a resource is.
    /// Lives in the service locator rather than in a scene, because it has to survive the two
    /// scene loads that are the entire reason it exists.
    /// </remarks>
    public sealed class RaidTrip
    {
        private readonly List<ResourceAmount> _haul = new();

        /// <summary>True while the squad is out and a zone owes the base an answer.</summary>
        public bool IsOut { get; private set; }

        /// <summary>The scene to come back to. Empty when nobody is out.</summary>
        public string HomeScene { get; private set; } = string.Empty;

        /// <summary>True once a zone has reported in and the base has not read it yet.</summary>
        public bool HasReport { get; private set; }

        /// <summary>What the squad got out with in all. Zero for a run that went badly.</summary>
        public int Hauled { get; private set; }

        /// <summary>What they got out with, by store.</summary>
        public IReadOnlyList<ResourceAmount> Haul => _haul;

        /// <summary>The zone's name, for the line the base shows on the way in.</summary>
        public string ZoneName { get; private set; } = string.Empty;

        /// <summary>Sends the squad out, and says where they are coming back to.</summary>
        public void Leave(string homeScene)
        {
            IsOut = true;
            HomeScene = homeScene;
            HasReport = false;
            Hauled = 0;
            ZoneName = string.Empty;
            _haul.Clear();
        }

        /// <summary>
        /// The zone reporting what it banked.
        /// </summary>
        /// <remarks>
        /// Ignored when nobody is out: a mission zone played on its own, from the menu or in the
        /// editor, ends the same way and must not post a haul to a colony that never sent it.
        /// </remarks>
        public void Report(string zoneName, IReadOnlyList<ResourceAmount>? haul)
        {
            if (!IsOut)
            {
                return;
            }

            HasReport = true;
            ZoneName = zoneName;

            _haul.Clear();
            if (haul is not null)
            {
                foreach (ResourceAmount amount in haul)
                {
                    if (amount.Amount > 0)
                    {
                        _haul.Add(amount);
                    }
                }
            }

            Hauled = ResourceNames.Total(_haul);
        }

        /// <summary>
        /// The base reading the report on the way in, and closing the trip.
        /// </summary>
        /// <returns>What to add to the stores. Empty when there was nothing to read.</returns>
        public IReadOnlyList<ResourceAmount> Collect()
        {
            if (!HasReport)
            {
                return Array.Empty<ResourceAmount>();
            }

            var haul = _haul.ToArray();
            IsOut = false;
            HasReport = false;
            HomeScene = string.Empty;
            _haul.Clear();
            Hauled = 0;
            return haul;
        }
    }
}
