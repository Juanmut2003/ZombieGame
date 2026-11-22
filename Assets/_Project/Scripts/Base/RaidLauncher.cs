#nullable enable
using UnityEngine;
using ZombieGame.Core;
using ZombieGame.Core.SceneFlow;
using ZombieGame.Economy;

namespace ZombieGame.Base
{
    /// <summary>
    /// The way out of the base: one trip to a zone a day.
    /// </summary>
    /// <remarks>
    /// PROJECT.md 4.6. Three things have to happen in this order and nowhere else: the day's
    /// trip is spent, the colony is written down, and only then does the scene change. Capturing
    /// after the load has started is capturing a plot that may already be half unloaded, and
    /// spending the trip after the capture is how a double click sends two squads out on one day.
    /// Coming home is the other half of this and lives in <see cref="RaidReturn"/>, because the
    /// object that reads the report must be one that exists on the way in rather than one that
    /// went out with the scene.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class RaidLauncher : MonoBehaviour
    {
        [Tooltip("The plot, so that what is standing on it comes back.")]
        [SerializeField]
        private BuildSiteView? _siteView;

        [Tooltip("The people and the day count.")]
        [SerializeField]
        private ColonyRuntime? _colony;

        [Tooltip("The zone the squad goes to.")]
        [SerializeField]
        private string _zoneScene = GameScenes.MissionTown;

        /// <summary>The run in progress, or null before one has been started.</summary>
        public ColonyRun? Run => ColonyRuns.Current;

        /// <summary>True while the squad can still go out today.</summary>
        public bool CanGo
        {
            get
            {
                ColonyRun? run = Run;
                return run is null || run.CanRaid;
            }
        }

        /// <summary>Why the squad cannot go, in the words the button shows. Empty when they can.</summary>
        public string Why => CanGo ? string.Empty : "BACK TOMORROW";

        /// <summary>
        /// Sends the squad out.
        /// </summary>
        /// <returns>False when they have already been out today, or there is nowhere to go.</returns>
        public bool Go()
        {
            if (_siteView == null)
            {
                Debug.LogWarning("[Raid] no plot to come back to; the squad stays in.");
                return false;
            }

            if (!GameServices.TryGet(out SceneLoader? loader) || loader is null)
            {
                Debug.LogWarning("[Raid] no scene loader — start from the Boot scene.");
                return false;
            }

            // The stores the scene is already using, so that a base opened on its own starts a
            // run around the ledger the readout is showing rather than around a second one.
            GameServices.TryGet(out Stockpile? stores);
            ColonyRun run = ColonyRuns.GetOrBegin(stores ?? new Stockpile());

            if (!run.TrySpendTheDaysRaid())
            {
                return false;
            }

            run.Capture(
                _siteView.Site,
                _colony == null ? null : _colony.Colony,
                _colony == null ? run.DaysLived : _colony.DaysLived);

            if (!GameServices.TryGet(out RaidTrip? trip) || trip is null)
            {
                trip = new RaidTrip();
                GameServices.Replace(trip);
            }

            trip.Leave(loader.ActiveScene);
            return loader.Load(_zoneScene);
        }
    }
}
