#nullable enable
using System.Collections.Generic;
using UnityEngine;
using ZombieGame.Core;
using ZombieGame.Combat;
using ZombieGame.Core.Config;
using ZombieGame.Core.Events;
using ZombieGame.Core.Pooling;
using ZombieGame.Waves;

namespace ZombieGame.AI
{
    /// <summary>
    /// The one component that drives the horde: ticks the manager and keeps the visible bodies in
    /// step with it.
    /// </summary>
    /// <remarks>
    /// One <c>FixedUpdate</c> for all zombies, not one per zombie. The split is the whole point —
    /// the manager holds the state and the rules, this holds the bridge to the engine.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class ZombieDirector : MonoBehaviour
    {
        [Tooltip("The kinds of zombie this zone puts out. Spawns are drawn from these at random.")]
        [SerializeField]
        private ZombieDefinition[] _definitions = System.Array.Empty<ZombieDefinition>();

        [Tooltip("Where the crowd numbers come from.")]
        [SerializeField]
        private BalanceConfig? _balance;

        [Tooltip("The most zombies that can be alive at once in this zone.")]
        [SerializeField]
        [Min(1)]
        private int _capacity = 300;

        [Tooltip("How many to put down when the zone starts.")]
        [SerializeField]
        [Min(0)]
        private int _spawnOnStart = 24;

        [Tooltip("Half the width and height of the box they are scattered in, around this object.")]
        [SerializeField]
        private Vector2 _spawnArea = new(5f, 3f);

        [Tooltip("What the horde walks towards. Usually the MC.")]
        [SerializeField]
        private Transform? _target;

        [Tooltip("Seed for the scatter, so a zone lays out the same way every time.")]
        [SerializeField]
        private int _seed = 1337;

        [Tooltip("The layers that block sight. Usually the walls.")]
        [SerializeField]
        private LayerMask _sightBlockers;

        [Tooltip("How wide the thing they are watching is, for the edge-of-body sight test.")]
        [SerializeField]
        [Min(0f)]
        private float _targetRadius = 0.25f;

        // Far enough out that arrivals come from beyond the scattered crowd rather than inside it.
        private const float RimFactor = 1.25f;

        private ZombieManager _manager = null!;
        private System.Random _rimRandom = null!;
        private EventBus? _bus;
        private ObjectPool<ZombieView> _views = null!;
        private ILineOfSight _lineOfSight = OpenLineOfSight.Instance;
        private readonly List<ZombieView> _active = new();

        /// <summary>The horde itself, for tests and for whatever shoots at it.</summary>
        public ZombieManager Manager => _manager;

        private void Awake()
        {
            GameServices.TryGet(out EventBus? bus);
            _bus = bus;

            if (_balance is null)
            {
                Debug.LogWarning($"[Zombies] {name} has no BalanceConfig; using the defaults.");
                _balance = BalanceConfig.CreateDefault();
            }

            _manager = new ZombieManager(
                _capacity,
                bus,
                new CrowdSettings(_balance.ZombieSeparationStrength, _balance.ZombieCrowdCellSize),
                _seed);

            // Its own stream, so an escalation does not shuffle the opening layout: a zone that
            // laid itself out differently depending on how loud the player had been would make
            // every bug in it unreproducible.
            _rimRandom = new System.Random(_seed ^ 0x5EED);

            _views = new ObjectPool<ZombieView>(CreateView, prewarm: _spawnOnStart);

            // With no layers named, nothing blocks sight — and a zombie that sees through walls
            // would be a long afternoon of wondering why. Say so instead.
            if (_sightBlockers.value == 0)
            {
                Debug.LogWarning($"[Zombies] {name} has no sight blockers; they see through walls.");
            }
            else
            {
                _lineOfSight = new Physics2DLineOfSight(_sightBlockers, _target);
            }
        }

        private void OnEnable()
        {
            _bus?.Subscribe<MeleeSwungEvent>(OnMeleeSwung);
            _bus?.Subscribe<NoiseEvent>(OnNoise);
            _bus?.Subscribe<HeatChangedEvent>(OnHeatChanged);
        }

        private void OnDisable()
        {
            _bus?.Unsubscribe<MeleeSwungEvent>(OnMeleeSwung);
            _bus?.Unsubscribe<NoiseEvent>(OnNoise);
            _bus?.Unsubscribe<HeatChangedEvent>(OnHeatChanged);
        }

        /// <summary>Lets the horde hear what the zone heard.</summary>
        private void OnNoise(NoiseEvent noise) => _manager.HearNoise(noise.Origin, noise.Loudness);

        /// <summary>
        /// Walks more zombies in whenever the zone gets worse.
        /// </summary>
        /// <remarks>
        /// This is the part of PROJECT.md §5 that makes noise cost something right now rather than
        /// at the end of the mission: gunfire and alarms escalate zone spawns in real time. They
        /// arrive at the rim rather than next to the player — the zone is drawing them in, not
        /// conjuring them on top of you, and a crowd that appears in your face is a cheat however
        /// loud you were being.
        /// </remarks>
        private void OnHeatChanged(HeatChangedEvent heat)
        {
            if (!heat.Escalated || _definitions.Length == 0 || _balance is null)
            {
                return;
            }

            SpawnAtRim(_balance.HeatSpawnsPerStage);
        }

        /// <summary>Puts fresh zombies down around the edge of the spawn area.</summary>
        public int SpawnAtRim(int count)
        {
            if (_definitions.Length == 0)
            {
                return 0;
            }

            int spawned = 0;
            for (int i = 0; i < count && !_manager.IsFull; i++)
            {
                float angle = (float)(_rimRandom.NextDouble() * Mathf.PI * 2f);
                var rim = new Vector2(
                    Mathf.Cos(angle) * _spawnArea.x * RimFactor,
                    Mathf.Sin(angle) * _spawnArea.y * RimFactor);

                int type = _rimRandom.Next(_definitions.Length);
                if (_manager.Spawn(_definitions[type], (Vector2)transform.position + rim, type) != 0)
                {
                    spawned++;
                }
            }

            return spawned;
        }

        /// <summary>
        /// Resolves a swing against the horde. The attacker never touches the array itself.
        /// </summary>
        private void OnMeleeSwung(MeleeSwungEvent swing) => _manager.ApplySwing(swing);

        private void Start()
        {
            if (_definitions.Length == 0)
            {
                Debug.LogWarning($"[Zombies] {name} has no definitions; the zone stays empty.");
                return;
            }

            var random = new System.Random(_seed);
            for (int i = 0; i < _spawnOnStart; i++)
            {
                var offset = new Vector2(
                    (float)((random.NextDouble() * 2d) - 1d) * _spawnArea.x,
                    (float)((random.NextDouble() * 2d) - 1d) * _spawnArea.y);

                int type = random.Next(_definitions.Length);
                _manager.Spawn(_definitions[type], (Vector2)transform.position + offset, type);
            }

            SyncViews();
        }

        private void FixedUpdate()
        {
            if (_target is not null)
            {
                // Perception first, movement second: looking at where the target was last frame
                // would show a zombie reacting to a position the player has already left.
                _manager.Perceive(_target.position, _targetRadius, _lineOfSight);

                // Perceive answers 'can it see you'; Decide turns that into what it does about
                // it. Both before the tick, which only walks towards whatever was decided.
                _manager.Decide(_target.position, Time.fixedDeltaTime);
            }

            _manager.Tick(Time.fixedDeltaTime);
        }

        private void LateUpdate() => SyncViews();

        /// <summary>
        /// Hands every live zombie a body, and takes the spare bodies back.
        /// </summary>
        /// <remarks>
        /// Views are matched to slots, not to zombies. A zombie that dies is swapped out of the
        /// middle of the array, so the body at that slot simply shows whoever took its place —
        /// cheaper than tracking which body belongs to which zombie, and invisible at this scale.
        /// </remarks>
        private void SyncViews()
        {
            if (_definitions.Length == 0)
            {
                return;
            }

            int live = _manager.Count;

            while (_active.Count < live)
            {
                _active.Add(_views.Get());
            }

            while (_active.Count > live)
            {
                int last = _active.Count - 1;
                _views.Return(_active[last]);
                _active.RemoveAt(last);
            }

            System.ReadOnlySpan<ZombieState> states = _manager.States;
            for (int i = 0; i < live; i++)
            {
                ZombieDefinition type = _definitions[Mathf.Clamp(states[i].TypeId, 0, _definitions.Length - 1)];
                _active[i].Show(
                    states[i].Position,
                    type.SpriteFor(
                        states[i].SkinId,
                        states[i].Facing,
                        states[i].Behaviour == ZombieBehaviour.Chasing),
                    states[i].HitFlash > 0f);
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Draws what the horde can see, so perception can be looked at instead of guessed at.
        /// </summary>
        /// <remarks>
        /// Only while the director is selected and only in the editor: this walks the whole horde
        /// and would be a waste in a build, where nobody can see a gizmo anyway.
        /// </remarks>
        private void OnDrawGizmosSelected()
        {
            if (_manager is null || !Application.isPlaying)
            {
                return;
            }

            System.ReadOnlySpan<ZombieState> states = _manager.States;
            for (int i = 0; i < states.Length; i++)
            {
                ZombieSenses senses = _manager.SensesOf(i);
                if (senses.ViewDistance <= 0f)
                {
                    continue;
                }

                Gizmos.color = states[i].SeesTarget
                    ? new Color(0.85f, 0.3f, 0.25f, 0.9f)
                    : new Color(0.5f, 0.55f, 0.5f, 0.35f);

                var eye = new Vector3(states[i].Position.x, states[i].Position.y, 0f);
                Vector2 look = states[i].Look.sqrMagnitude > 0.0001f
                    ? states[i].Look.normalized
                    : Vector2.down;

                DrawConeEdge(eye, look, senses, +senses.ViewHalfAngleDegrees);
                DrawConeEdge(eye, look, senses, -senses.ViewHalfAngleDegrees);
            }
        }

        private static void DrawConeEdge(
            Vector3 eye, Vector2 look, in ZombieSenses senses, float degrees)
        {
            Vector2 edge = Quaternion.Euler(0f, 0f, degrees) * look;
            Gizmos.DrawLine(eye, eye + (new Vector3(edge.x, edge.y, 0f) * senses.ViewDistance));
        }
#endif

        private ZombieView CreateView()
        {
            var body = new GameObject("Zombie", typeof(SpriteRenderer), typeof(ZombieView));
            body.transform.SetParent(transform, worldPositionStays: true);
            return body.GetComponent<ZombieView>();
        }
    }
}
