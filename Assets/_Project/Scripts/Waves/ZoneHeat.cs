#nullable enable
using UnityEngine;
using ZombieGame.Core;
using ZombieGame.Core.Config;
using ZombieGame.Core.Events;
using ZombieGame.Core.Physics;

namespace ZombieGame.Waves
{
    /// <summary>
    /// Runs one zone's heat meter: listens for noise, cools off in the quiet, and says so.
    /// </summary>
    /// <remarks>
    /// The bridge between the arithmetic in <see cref="HeatMeter"/> and the rest of the scene.
    /// Nothing publishes heat at a listener and nothing asks a character how loud it was being —
    /// noise goes on the bus, this adds it up, and whoever cares subscribes.
    /// One of these per zone. The base has its own, much slower meter later; they share the maths
    /// and nothing else.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class ZoneHeat : MonoBehaviour
    {
        [Tooltip("Where the heat numbers come from.")]
        [SerializeField]
        private BalanceConfig? _balance;

        [Tooltip("Who the zone is looking for. Leave empty and nowhere counts as hidden.")]
        [SerializeField]
        private Transform? _squad;

        [Tooltip("How hot the zone already is when the squad arrives, 0 to 1.")]
        [SerializeField]
        [Range(0f, 1f)]
        private float _startingHeat;

        [Tooltip("What this zone multiplies every noise by. Above 1 for a place already on edge.")]
        [SerializeField]
        [Min(0f)]
        private float _noiseFactor = 1f;

        private EventBus? _bus;
        private ICoverMap _cover = OpenSight.Instance;
        private HeatMeter _meter = null!;
        private float _lastAnnounced = float.NaN;
        private HeatStage _announcedStage = HeatStage.Calm;

        /// <summary>How hot the zone is, 0 to 1.</summary>
        public float Value => _meter.Value;

        /// <summary>What that heat means.</summary>
        public HeatStage Stage => _meter.Stage;

        private void Awake()
        {
            if (_balance is null)
            {
                Debug.LogWarning($"[Heat] {name} has no BalanceConfig; using the defaults.");
                _balance = BalanceConfig.CreateDefault();
            }

            _meter = new HeatMeter(new HeatSettings(
                _balance.HeatPerNoise,
                _balance.HeatDecayPerSecond,
                _balance.HeatStirredAt,
                _balance.HeatHuntingAt,
                _balance.HeatStageDrop));

            // A zone the squad walks into already stirred. The mission says how bad it was
            // before anyone arrived, which is what makes two runs through the same streets
            // different runs rather than the same one twice.
            if (_startingHeat > 0f)
            {
                _meter.Hear(_startingHeat / Mathf.Max(0.0001f, _balance.HeatPerNoise));
            }
        }

        /// <summary>
        /// Sets this zone's heat profile. Called by the mission before the first tick.
        /// </summary>
        public void Profile(float startingHeat, float noiseFactor)
        {
            _startingHeat = Mathf.Clamp01(startingHeat);
            _noiseFactor = Mathf.Max(0f, noiseFactor);
        }

        private void Start()
        {
            // Start, not Awake: the bootstrap registers the bus in its own Awake.
            GameServices.TryGet(out _bus);
            if (_bus is null)
            {
                Debug.LogWarning("[Heat] no event bus; the zone will never hear anything.");
                return;
            }

            _bus.Subscribe<NoiseEvent>(OnNoise);

            // The opening value is deliberately not published here. Start order between objects is
            // undefined, so a HUD that subscribes in its own Start would miss it — and since a
            // calm zone never moves, the bar would then sit at whatever width the scene was built
            // with until the first noise. The first Update does it instead, by which time every
            // Start has run.
        }

        private void OnDestroy() => _bus?.Unsubscribe<NoiseEvent>(OnNoise);

        private void Update()
        {
            _meter.Tick(Time.deltaTime, Hidden ? _balance!.HeatCoverDecayFactor : 1f);
            Announce();
        }

        /// <summary>
        /// Whether the squad has gone to ground. Looked up rather than stored, because the cover
        /// grid is registered by the zone's own <c>Awake</c> and this component's may run first.
        /// </summary>
        private bool Hidden
        {
            get
            {
                if (_squad is null)
                {
                    return false;
                }

                if (ReferenceEquals(_cover, OpenSight.Instance)
                    && GameServices.TryGet(out ICoverMap? registered))
                {
                    _cover = registered!;
                }

                Vector3 at = _squad.position;
                return _cover.Hides(new Vector2(at.x, at.y));
            }
        }

        /// <summary>Feeds the meter directly. For a zone starting hot, and for tests.</summary>
        public void Hear(float loudness)
        {
            _meter.Hear(loudness);
            Announce();
        }

        private void OnNoise(NoiseEvent noise) => Hear(noise.Loudness * _noiseFactor);

        /// <summary>
        /// Publishes the meter, but only when it has actually moved.
        /// </summary>
        /// <remarks>
        /// A zone at rest would otherwise publish the same zero sixty times a second, for every
        /// listener to ignore. The stage is checked as well as the value, so the one frame the
        /// meter crosses a threshold is never the frame that gets skipped.
        /// </remarks>
        private void Announce()
        {
            HeatStage stage = _meter.Stage;
            bool moved = !Mathf.Approximately(_meter.Value, _lastAnnounced);
            if (!moved && stage == _announcedStage)
            {
                return;
            }

            HeatStage previous = _announcedStage;
            _lastAnnounced = _meter.Value;
            _announcedStage = stage;
            _bus?.Publish(new HeatChangedEvent(_meter.Value, stage, previous));
        }
    }
}
