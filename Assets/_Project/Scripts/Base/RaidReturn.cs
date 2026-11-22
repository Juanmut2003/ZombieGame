#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieGame.Core;
using ZombieGame.Economy;

namespace ZombieGame.Base
{
    /// <summary>
    /// The squad coming home: reads what the zone banked and puts it in the stores.
    /// </summary>
    /// <remarks>
    /// Read once, on the way in, and the trip is closed by the reading — so a base re-entered
    /// twice cannot be paid twice for one run. That is the whole job, and it is a component of
    /// its own rather than a few lines in <see cref="ColonyRuntime"/> because the thing that
    /// has to be remembered is "what the player should be told when they arrive", and the
    /// colony has no business holding that.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class RaidReturn : MonoBehaviour
    {
        /// <summary>What the squad brought back in all, or zero when they did not go out.</summary>
        public int Hauled { get; private set; }

        /// <summary>What they brought back, by store.</summary>
        public IReadOnlyList<ResourceAmount> Haul { get; private set; } =
            Array.Empty<ResourceAmount>();

        /// <summary>The zone they came back from, or empty when they did not go out.</summary>
        public string From { get; private set; } = string.Empty;

        /// <summary>True once a trip has been read in, whatever it came to.</summary>
        public bool CameHome { get; private set; }

        /// <summary>
        /// Started rather than woken, because the stores are built in another component's
        /// <c>Awake</c> and there is no promised order between two of those.
        /// </summary>
        private void Start()
        {
            if (!GameServices.TryGet(out RaidTrip? trip) || trip is null || !trip.HasReport)
            {
                return;
            }

            From = trip.ZoneName;
            Haul = trip.Collect();
            Hauled = ResourceNames.Total(Haul);
            CameHome = true;

            ColonyRun? run = ColonyRuns.Current;
            if (run is null)
            {
                Debug.LogWarning("[Raid] nobody is running this colony; the haul is lost.");
                return;
            }

            run.Bank(Haul);
        }

        /// <summary>What to say about the trip, or an empty string when there is nothing to say.</summary>
        public string Line()
        {
            if (!CameHome)
            {
                return string.Empty;
            }

            string where = string.IsNullOrEmpty(From) ? "THE RUN" : From.ToUpperInvariant();
            string what = ResourceNames.Line(Haul);
            return what.Length > 0
                ? $"BACK FROM {where} WITH {what}"
                : $"BACK FROM {where} WITH NOTHING";
        }
    }
}
