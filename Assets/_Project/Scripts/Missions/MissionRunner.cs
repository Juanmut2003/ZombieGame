#nullable enable
using UnityEngine;
using ZombieGame.Core;
using ZombieGame.Core.Events;

namespace ZombieGame.Missions
{
    /// <summary>
    /// Runs one mission in a zone: counts the loot, watches the way out, and calls the end of it.
    /// </summary>
    /// <remarks>
    /// All the rules live in <see cref="MissionProgress"/>; this is the bridge between them and
    /// the scene. It never reaches for a container or a zombie — loot arrives on the bus and the
    /// extraction point answers one question about itself.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class MissionRunner : MonoBehaviour
    {
        [Tooltip("Which mission this zone is running.")]
        [SerializeField]
        private MissionDefinition? _mission;

        [Tooltip("The way out.")]
        [SerializeField]
        private ExtractionPoint? _extraction;

        private EventBus? _bus;
        private MissionProgress _progress = null!;
        private bool _announcedOnce;

        /// <summary>How the run is going.</summary>
        public MissionProgress Progress => _progress;

        /// <summary>Which mission is being run.</summary>
        public MissionDefinition? Mission => _mission;

        private void Awake()
        {
            if (_mission is null)
            {
                Debug.LogWarning($"[Mission] {name} has no mission; the zone has no objective.");
            }

            _progress = _mission is not null
                ? _mission.CreateProgress()
                : new MissionProgress(0, 0f);
        }

        private void OnEnable() => Listen();

        private void OnDisable()
        {
            _bus?.Unsubscribe<LootTakenEvent>(OnLootTaken);
            _bus = null;
        }

        /// <summary>
        /// Subscribes as soon as there is a bus to subscribe to.
        /// </summary>
        /// <remarks>
        /// Not once in <c>OnEnable</c>: whether the bootstrap has registered the bus by then
        /// depends on where this object sits in the scene file, and a run that misses it counts
        /// no loot at all while looking perfectly healthy.
        /// </remarks>
        private void Listen()
        {
            if (_bus is not null || !GameServices.TryGet(out _bus) || _bus is null)
            {
                return;
            }

            _bus.Subscribe<LootTakenEvent>(OnLootTaken);
        }

        private void Update()
        {
            Listen();

            // The opening state goes out on the first frame rather than in Start, where a HUD
            // subscribing in its own Start would miss it.
            if (!_announcedOnce)
            {
                _announcedOnce = true;
                Announce();
            }

            if (_progress.IsOver)
            {
                return;
            }

            bool inside = _extraction is not null && _extraction.SquadInside;
            MissionState before = _progress.State;
            bool won = _progress.TickExtraction(Time.deltaTime, inside);

            if (won)
            {
                Announce();
                _bus?.Publish(new MissionEndedEvent(
                    _mission is not null ? _mission.DisplayName : name,
                    extracted: true,
                    banked: _progress.Banked));
                return;
            }

            // While the hold is running the fraction changes every frame and the HUD wants it;
            // the rest of the time, only a change of state is worth saying.
            if (_progress.State == MissionState.Extracting || before != _progress.State)
            {
                Announce();
            }
        }

        /// <summary>Ends the run badly, losing everything carried.</summary>
        public void Fail()
        {
            if (_progress.IsOver)
            {
                return;
            }

            _progress.Fail();
            Announce();
            _bus?.Publish(new MissionEndedEvent(
                _mission is not null ? _mission.DisplayName : name,
                extracted: false,
                banked: 0));
        }

        private void OnLootTaken(LootTakenEvent loot)
        {
            _progress.Collect(loot.Supplies);
            Announce();
        }

        private void Announce() =>
            _bus?.Publish(new MissionProgressEvent(
                _mission is not null ? _mission.ObjectiveText : string.Empty,
                _progress.Carried,
                _progress.SuppliesWanted,
                _progress.State,
                _progress.ExtractionFraction));
    }
}
