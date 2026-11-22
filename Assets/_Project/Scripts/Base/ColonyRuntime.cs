#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieGame.Core;
using ZombieGame.Core.Clock;
using ZombieGame.Core.Config;
using ZombieGame.Economy;

namespace ZombieGame.Base
{
    /// <summary>
    /// The colony as it exists in a scene: its people, and the day they live through.
    /// </summary>
    /// <remarks>
    /// Days at the base do not run on their own. The clock only moves when the player says the
    /// day is over, which is what makes a base turn-shaped and a zone real-time — see
    /// <see cref="GameClock.ConfirmEndOfDay"/>. Everything that resolves a day hangs off that
    /// one signal, so nothing has to agree with anything else about when a day ended.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class ColonyRuntime : MonoBehaviour
    {
        [Tooltip("The plot the colony lives on.")]
        [SerializeField]
        private BuildSiteView? _siteView;

        [Tooltip("Where the day's numbers come from.")]
        [SerializeField]
        private BalanceConfig? _balance;

        [Tooltip("The placer, which knows the catalogue and how to raise a building.")]
        [SerializeField]
        private BuildPlacer? _placer;

        [Tooltip("The people who took the site. They arrive whether or not there are beds.")]
        [SerializeField]
        private string[] _founders = { "Rae", "Osei", "Marta" };

        private GameClock? _clock;
        private Stockpile? _stores;

        /// <summary>The people. Null until Start has run.</summary>
        public Colony? Colony { get; private set; }

        /// <summary>What the last day came to.</summary>
        public DayReport LastReport { get; private set; }

        /// <summary>How many days have been lived through here.</summary>
        public int DaysLived { get; private set; }

        /// <summary>Raised once a day has been resolved, for whatever is reporting on it.</summary>
        public event Action<DayReport>? DayResolved;

        private void Start()
        {
            // Started rather than woken: the plot is built in BuildSiteView.Awake, and there is
            // no promised order between two components' Awake.
            if (_siteView == null)
            {
                Debug.LogWarning("[Colony] no build site; nobody lives here.");
                return;
            }

            if (_balance == null)
            {
                Debug.LogWarning("[Colony] no BalanceConfig; using the defaults.");
                _balance = BalanceConfig.CreateDefault();
            }

            Colony = new Colony(_siteView.Site);

            ColonyRun? run = ColonyRuns.Current;
            if (run is not null && run.HasSnapshot)
            {
                PutItBack(run);
            }
            else
            {
                foreach (string name in _founders)
                {
                    Colony.AddFounder(name);
                }
            }

            GameServices.Replace(Colony);
            GameServices.TryGet(out _stores);

            if (GameServices.TryGet(out _clock) && _clock is not null)
            {
                _clock.DayAdvanced += OnDayAdvanced;
            }
        }

        private void OnDestroy()
        {
            if (_clock is not null)
            {
                _clock.DayAdvanced -= OnDayAdvanced;
            }

            GameServices.Unregister<Colony>();
        }

        /// <summary>
        /// Calls it a day. The clock answers, and the day resolves on the way back.
        /// </summary>
        /// <remarks>
        /// Goes through the clock rather than resolving here, so that a day ended by anything
        /// else — a mission returning, a scripted event — runs through exactly the same path.
        /// </remarks>
        public void EndDay()
        {
            if (_clock is null)
            {
                // No clock means no boot scene, which is the editor opening the base on its own.
                // The day still has to resolve or the scene is a diorama.
                Resolve();
                return;
            }

            _clock.ConfirmEndOfDay();
        }

        /// <summary>
        /// Puts the colony that went out on the raid back on the plot it left.
        /// </summary>
        /// <remarks>
        /// The buildings first and the people after, because a survivor remembers the number of
        /// the building they worked in and that number only means anything once the buildings
        /// are standing again.
        /// Each one needs its body in the scene as well as its square on the grid, and the
        /// placer is what knows how to make one — the same code that raises a building when it
        /// is first put down, so a restored barracks cannot end up drawn differently from a new
        /// one.
        /// </remarks>
        private void PutItBack(ColonyRun run)
        {
            if (Colony is null || _siteView == null)
            {
                return;
            }

            IReadOnlyList<BuildingDefinition> catalogue = _placer == null
                ? Array.Empty<BuildingDefinition>()
                : _placer.Catalogue;

            if (catalogue.Count == 0 && run.Buildings.Count > 0)
            {
                Debug.LogWarning(
                    "[Colony] no catalogue to look the buildings up in; the plot comes back bare.");
            }

            foreach (int id in run.Restore(_siteView.Site, Colony, catalogue))
            {
                if (_placer != null)
                {
                    _placer.Raise(id);
                }
            }

            DaysLived = run.DaysLived;
        }

        private void OnDayAdvanced(DayAdvancedEvent advanced) => Resolve();

        private void Resolve()
        {
            if (Colony is null || _siteView == null)
            {
                return;
            }

            // Anybody whose workplace was torn down since yesterday is sent home first, or they
            // would work a building that is not there and hold a post in whatever replaced it.
            Colony.SendHomeFromRubble();

            Stockpile stores = _stores ?? new Stockpile();
            LastReport = ColonyDay.Resolve(
                Colony,
                _siteView.Site,
                stores,
                _balance!.FoodPerPersonPerDay,
                _balance.WaterPerPersonPerDay);

            DaysLived++;

            // The run counts days too, and it is the one that survives a raid. Told here rather
            // than left to work it out, so that there is exactly one place a day is counted.
            ColonyRun? run = ColonyRuns.Current;
            if (run is not null)
            {
                run.DayPassed();
            }

            DayResolved?.Invoke(LastReport);
        }
    }
}
