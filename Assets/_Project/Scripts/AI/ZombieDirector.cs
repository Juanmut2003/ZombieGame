#nullable enable
using System.Collections.Generic;
using UnityEngine;
using ZombieGame.Core;
using ZombieGame.Core.Events;
using ZombieGame.Core.Pooling;

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
        [Tooltip("Which zombie to spawn. One definition for now; a zone will mix several.")]
        [SerializeField]
        private ZombieDefinition? _definition;

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

        private ZombieManager _manager = null!;
        private ObjectPool<ZombieView> _views = null!;
        private readonly List<ZombieView> _active = new();

        /// <summary>The horde itself, for tests and for whatever shoots at it.</summary>
        public ZombieManager Manager => _manager;

        private void Awake()
        {
            GameServices.TryGet(out EventBus bus);
            _manager = new ZombieManager(_capacity, bus);

            _views = new ObjectPool<ZombieView>(CreateView, prewarm: _spawnOnStart);
        }

        private void Start()
        {
            if (_definition is null)
            {
                Debug.LogWarning($"[Zombies] {name} has no definition; the zone stays empty.");
                return;
            }

            var random = new System.Random(_seed);
            for (int i = 0; i < _spawnOnStart; i++)
            {
                var offset = new Vector2(
                    (float)((random.NextDouble() * 2d) - 1d) * _spawnArea.x,
                    (float)((random.NextDouble() * 2d) - 1d) * _spawnArea.y);

                _manager.Spawn(_definition, (Vector2)transform.position + offset);
            }

            SyncViews();
        }

        private void FixedUpdate()
        {
            if (_target is not null)
            {
                _manager.SetDestinationForAll(_target.position);
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
            if (_definition is null)
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
                _active[i].Show(states[i].Position, _definition.SpriteFor(states[i].Facing));
            }
        }

        private ZombieView CreateView()
        {
            var body = new GameObject("Zombie", typeof(SpriteRenderer), typeof(ZombieView));
            body.transform.SetParent(transform, worldPositionStays: true);
            return body.GetComponent<ZombieView>();
        }
    }
}
