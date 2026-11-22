#nullable enable
using System.Collections.Generic;
using UnityEngine;
using ZombieGame.Combat;
using ZombieGame.Core.Config;
using ZombieGame.Core.Events;
using ZombieGame.Core.Physics;
using ZombieGame.Core.Pooling;
using ZombieGame.Core.Rendering;
using ZombieGame.Core;
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

        [Tooltip(
            "Spots to scatter them around instead of the box. A drawn zone wants them in the "
            + "rooms, not spread evenly through the walls.")]
        [SerializeField]
        private Vector2[] _nests = System.Array.Empty<Vector2>();

        [Tooltip("How far from a nest they stand.")]
        [SerializeField]
        [Min(0f)]
        private float _nestRadius = 1.6f;

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
        private IHurtable? _targetVitals;
        private IConcealable? _targetCover;
        private Vector2? _sweptFor;
        private System.Random _rimRandom = null!;
        private EventBus? _bus;
        private ObjectPool<ZombieView> _views = null!;
        private ILineOfSight _lineOfSight = OpenLineOfSight.Instance;
        private readonly List<ZombieView> _active = new();

        /// <summary>The horde itself, for tests and for whatever shoots at it.</summary>
        public ZombieManager Manager => _manager;

        /// <summary>
        /// Below this much speed squared a body counts as standing still, for the gait only.
        /// Not a balance number: it is a twentieth of a unit a second, which is slower than
        /// anything in the game walks and far above what the solver leaves behind when it
        /// stops something against a wall.
        /// </summary>
        private const float StandingStill = 0.0025f;

        private void Awake()
        {
            if (_balance == null)
            {
                Debug.LogWarning($"[Zombies] {name} has no BalanceConfig; using the defaults.");
                _balance = BalanceConfig.CreateDefault();
            }

            _manager = new ZombieManager(
                _capacity,
                bus: null,
                new CrowdSettings(_balance.ZombieSeparationStrength, _balance.ZombieCrowdCellSize),
                _seed);

            // The manager is built before anything has registered a bus, so deaths are forwarded
            // from here instead of published by it: by the time one happens the bus is found.
            _manager.ZombieDied += OnZombieDied;

            // Its own stream, so an escalation does not shuffle the opening layout: a zone that
            // laid itself out differently depending on how loud the player had been would make
            // every bug in it unreproducible.
            _rimRandom = new System.Random(_seed ^ 0x5EED);

            _views = new ObjectPool<ZombieView>(CreateView, prewarm: _spawnOnStart);

            // Anything that fires asks the locator who can be hit; the horde is the answer while
            // this zone is loaded. Replace rather than Register, so loading a zone twice does not
            // throw over a service the last one left behind.
            GameServices.Replace<IProjectileHitTest>(new ZombieHitTest(_manager));

            // And who can be crept up on. Registered together, because both are the horde
            // answering a question asked by somebody who must not know it exists.
            GameServices.Replace<ISilentKill>(new ZombieTakedown(_manager));

            // Asked once. The horde walks towards a transform and never holds what it is biting,
            // so all that is kept is the one thing that can be hurt.
            if (_target is not null)
            {
                _targetVitals = _target.GetComponent<IHurtable>();
                _targetCover = _target.GetComponent<IConcealable>();
            }

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

        private void OnDestroy()
        {
            // Only if it is still ours. Another zone loading first would have replaced it, and
            // tearing that one down with this one would leave the new zone unshootable.
            if (GameServices.TryGet(out IProjectileHitTest? registered)
                && registered is ZombieHitTest mine
                && ReferenceEquals(mine.Horde, _manager))
            {
                GameServices.Unregister<IProjectileHitTest>();
            }
        }

        private void OnEnable() => Listen();

        private void OnDisable()
        {
            if (_bus is null)
            {
                return;
            }

            _bus.Unsubscribe<MeleeSwungEvent>(OnMeleeSwung);
            _bus.Unsubscribe<NoiseEvent>(OnNoise);
            _bus.Unsubscribe<HeatChangedEvent>(OnHeatChanged);
            _bus = null;
        }

        /// <summary>
        /// Subscribes as soon as there is a bus to subscribe to.
        /// </summary>
        /// <remarks>
        /// Not once in <c>Awake</c>, which is where this used to be: whether the bootstrap has
        /// registered the bus by then depends on the order objects sit in the scene file. In one
        /// zone it worked and in the next the horde heard no swings at all — the bat did nothing
        /// while the pistol, which goes through a different path, was fine.
        /// </remarks>
        private void Listen()
        {
            if (_bus is not null || !GameServices.TryGet(out _bus) || _bus is null)
            {
                return;
            }

            _bus.Subscribe<MeleeSwungEvent>(OnMeleeSwung);
            _bus.Subscribe<NoiseEvent>(OnNoise);
            _bus.Subscribe<HeatChangedEvent>(OnHeatChanged);
        }

        /// <summary>Passes a death on to whoever is listening for one.</summary>
        private void OnZombieDied(DiedEvent died) => _bus?.Publish(died);

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
                // Round-robin across the nests rather than at random, so a drawn zone puts the
                // same few zombies in every room instead of emptying one by chance.
                Vector2 around = _nests.Length > 0
                    ? _nests[i % _nests.Length]
                    : (Vector2)transform.position;

                Vector2 offset = _nests.Length > 0
                    ? new Vector2(
                        (float)((random.NextDouble() * 2d) - 1d) * _nestRadius,
                        (float)((random.NextDouble() * 2d) - 1d) * _nestRadius)
                    : new Vector2(
                        (float)((random.NextDouble() * 2d) - 1d) * _spawnArea.x,
                        (float)((random.NextDouble() * 2d) - 1d) * _spawnArea.y);

                int type = random.Next(_definitions.Length);
                _manager.Spawn(_definitions[type], around + offset, type);
            }

            SyncViews();
        }

        private void FixedUpdate()
        {
            Listen();

            if (_target is not null)
            {
                // Perception first, movement second: looking at where the target was last frame
                // would show a zombie reacting to a position the player has already left.
                _manager.Perceive(
                    _target.position,
                    _targetRadius,
                    _lineOfSight,
                    _targetCover is null ? 1f : _targetCover.Visibility);

                // Perceive answers 'can it see you'; Decide turns that into what it does about
                // it. Both before the tick, which only walks towards whatever was decided.
                _manager.Decide(_target.position, Time.fixedDeltaTime);

                Bite();
            }

            // Looked up each step rather than cached: the zone's walls register themselves when
            // it loads, and a director that asked once might have asked before they did.
            GameServices.TryGet(out IWallMap? walls);
            GameServices.TryGet(out IFlowField? routes);
            _manager.Tick(Time.fixedDeltaTime, walls, RefreshRoutes(routes));
        }

        /// <summary>
        /// Keeps the horde's routes pointed at the target, and sweeps them again when it has
        /// moved far enough for the old ones to be wrong.
        /// </summary>
        /// <remarks>
        /// Not every step. The sweep is linear in the size of the zone and the answer barely
        /// changes while the target is still standing in the same cell, so it is redone when the
        /// target has left the cell the field was built for — which during a chase is a few times
        /// a second and while the player stands still is never.
        /// </remarks>
        private IFlowField? RefreshRoutes(IFlowField? routes)
        {
            if (routes is null || _target is null)
            {
                return routes;
            }

            Vector3 at = _target.position;
            var goal = new Vector2(at.x, at.y);
            if (_sweptFor.HasValue
                && Mathf.FloorToInt(_sweptFor.Value.x) == Mathf.FloorToInt(goal.x)
                && Mathf.FloorToInt(_sweptFor.Value.y) == Mathf.FloorToInt(goal.y))
            {
                return routes;
            }

            _sweptFor = routes.Rebuild(goal) ? goal : null;
            return routes;
        }

        /// <summary>
        /// Lets whoever has reached the target take a bite out of it.
        /// </summary>
        /// <remarks>
        /// The horde never holds the thing it is biting, only the transform it walks towards; what
        /// can be hurt is asked for once, from that transform, and only the interface is kept. A
        /// target with nothing hurtable on it is a legitimate zone — the sandbox is one — so this
        /// quietly does nothing rather than warning every step.
        /// </remarks>
        private void Bite()
        {
            if (_targetVitals is null || !_targetVitals.IsAlive)
            {
                return;
            }

            float damage = _manager.BiteAt(
                _target!.position, _targetRadius, Time.fixedDeltaTime, out DamageType type);
            if (damage <= 0f)
            {
                return;
            }

            _targetVitals.TakeDamage(new DamageInfo(damage, type));
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

            // Read once for the whole horde rather than per body: three property calls times
            // three hundred zombies times sixty frames is a lot of nothing.
            BalanceConfig balance = _balance!;
            float stride = balance.HordeStride;
            float bobHeight = balance.GaitBob;
            float leanDegrees = balance.GaitLean;

            System.ReadOnlySpan<ZombieState> states = _manager.States;
            for (int i = 0; i < live; i++)
            {
                ZombieDefinition type = _definitions[Mathf.Clamp(states[i].TypeId, 0, _definitions.Length - 1)];

                // A zombie standing still stands still. Its tally of walked ground stops where
                // it stopped, and drawing the gait from it regardless would leave the body
                // frozen halfway through a step, lifted off its own feet and leaning.
                bool walking = states[i].Velocity.sqrMagnitude > StandingStill;
                float phase = walking
                    ? StepCycle.FromDistance(states[i].Walked, stride)
                    : 0f;

                _active[i].Show(
                    states[i].Position,
                    type.SpriteFor(
                        states[i].SkinId,
                        states[i].Facing,
                        states[i].Behaviour == ZombieBehaviour.Chasing),
                    states[i].HitFlash > 0f,
                    StepCycle.Bob(phase, bobHeight),
                    StepCycle.Lean(phase, leanDegrees));
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

            // On the layer the map is not allowed to look at. Set here, where bodies are made,
            // rather than in the scene: they come out of a pool and nothing else ever touches
            // them, so this is the only place that could forget.
            int horde = GameLayers.HordeIndex;
            if (horde >= 0)
            {
                body.layer = horde;
            }
            else
            {
                Debug.LogWarning(
                    $"[Zombies] the project has no '{GameLayers.Horde}' layer; "
                    + "the minimap will show every zombie in the zone.");
            }

            return body.GetComponent<ZombieView>();
        }
    }
}
