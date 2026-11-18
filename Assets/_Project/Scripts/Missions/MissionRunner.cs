#nullable enable
using UnityEngine;
using ZombieGame.Combat;
using ZombieGame.Core;
using ZombieGame.Core.Events;
using ZombieGame.Waves;

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

        [Tooltip("The zone's heat meter, so the debrief can say how bad it got.")]
        [SerializeField]
        private ZoneHeat? _heat;

        [Tooltip("The squad. The run ends when they do.")]
        [SerializeField]
        private Transform? _squadTransform;

        private IHurtable? _squad;
        private EventBus? _bus;
        private MissionProgress _progress = null!;
        private bool _announcedOnce;
        private float _elapsed;

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

            if (_squadTransform is not null)
            {
                _squad = _squadTransform.GetComponent<IHurtable>();
            }

            if (_squad is null)
            {
                Debug.LogWarning(
                    $"[Mission] {name} has nobody to lose; this run cannot be failed.");
            }
        }

        private void OnEnable() => Listen();

        private void OnDisable()
        {
            _bus?.Unsubscribe<LootTakenEvent>(OnLootTaken);
            _bus?.Unsubscribe<DiedEvent>(OnSomebodyDied);
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
            _bus.Subscribe<DiedEvent>(OnSomebodyDied);
        }

        /// <summary>
        /// Ends the run when the squad does. Zombies die on this bus too, so the one that matters
        /// is told apart by who it was, not by what killed them.
        /// </summary>
        private void OnSomebodyDied(DiedEvent died)
        {
            if (_squad is not null && died.TargetId == _squad.OwnerId)
            {
                Fail();
            }
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

            _elapsed += Time.deltaTime;

            bool inside = _extraction is not null && _extraction.SquadInside;
            MissionState before = _progress.State;
            bool won = _progress.TickExtraction(Time.deltaTime, inside);

            if (won)
            {
                Announce();

                // Getting out short of the objective is still getting out, and the supplies
                // still count. PROJECT.md §8 asks for extraction to be required, not for the
                // objective to be — leaving early with four of six is a decision, not a failure.
                End(
                    _progress.Banked >= _progress.SuppliesWanted
                        ? MissionOutcome.Extracted
                        : MissionOutcome.ExtractedShort,
                    banked: _progress.Banked,
                    lost: 0);
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

            // Read before failing: Fail clears what was being carried, and what was carried is
            // precisely the thing worth telling the player about.
            int lost = _progress.Carried;

            _progress.Fail();
            Announce();
            End(MissionOutcome.SquadLost, banked: 0, lost: lost);
        }

        private void End(MissionOutcome outcome, int banked, int lost) =>
            _bus?.Publish(new MissionEndedEvent(
                _mission is not null ? _mission.DisplayName : name,
                outcome,
                banked,
                lost,
                _progress.SuppliesWanted,
                _elapsed,
                _heat is not null ? _heat.Value : 0f));

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
