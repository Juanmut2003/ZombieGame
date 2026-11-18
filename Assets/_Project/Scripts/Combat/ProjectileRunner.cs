#nullable enable
using System.Collections.Generic;
using UnityEngine;
using ZombieGame.Core;
using ZombieGame.Core.Events;
using ZombieGame.Core.Physics;
using ZombieGame.Core.Pooling;

namespace ZombieGame.Combat
{
    /// <summary>
    /// Runs every round in the zone: one tick for all of them, one pooled body each.
    /// </summary>
    /// <remarks>
    /// The other half of <see cref="ShotFiredEvent"/>. Whoever fires says so on the bus and this
    /// puts the round in the air, which is what keeps guns out of the character code and the
    /// horde out of the gun.
    /// Who can be hit is looked up as a service rather than wired in the inspector: the horde
    /// registers itself, and a zone without one simply has nothing to shoot at instead of a
    /// missing reference.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class ProjectileRunner : MonoBehaviour
    {
        [Tooltip("What a round looks like.")]
        [SerializeField]
        private Sprite? _bulletSprite;

        [Tooltip("The most rounds that can be in the air at once in this zone.")]
        [SerializeField]
        [Min(1)]
        private int _capacity = 64;

        [Tooltip("The layers that stop a round. Usually the walls.")]
        [SerializeField]
        private LayerMask _barriers;

        private ProjectileManager _rounds = null!;
        private ObjectPool<ProjectileView> _views = null!;
        private readonly List<ProjectileView> _active = new();
        private EventBus? _bus;
        private ILineOfSight _walls = OpenLineOfSight.Instance;

        /// <summary>The rounds in the air, for tests and for whatever wants to count them.</summary>
        public ProjectileManager Rounds => _rounds;

        private void Awake()
        {
            _rounds = new ProjectileManager(_capacity);
            _views = new ObjectPool<ProjectileView>(CreateView, prewarm: 8);

            if (_barriers.value != 0)
            {
                _walls = new Physics2DLineOfSight(_barriers);
            }
        }

        private void OnEnable() => Listen();

        private void OnDisable()
        {
            _bus?.Unsubscribe<ShotFiredEvent>(OnShotFired);
            _bus = null;
        }

        /// <summary>
        /// Subscribes as soon as there is a bus to subscribe to.
        /// </summary>
        /// <remarks>
        /// Not once in <c>OnEnable</c>: whether the bootstrap has registered the bus by then
        /// depends on where this object happens to sit in the scene file, and missing it would
        /// mean a zone where triggers click and nothing ever comes out of the barrel. Trying
        /// again each step costs a dictionary lookup and removes the ordering question entirely.
        /// </remarks>
        private void Listen()
        {
            if (_bus is not null || !GameServices.TryGet(out _bus) || _bus is null)
            {
                return;
            }

            _bus.Subscribe<ShotFiredEvent>(OnShotFired);
        }

        private void FixedUpdate()
        {
            Listen();

            GameServices.TryGet(out IProjectileHitTest? targets);
            _rounds.Tick(Time.fixedDeltaTime, targets, _walls);
        }

        private void LateUpdate() => SyncViews();

        private void OnShotFired(ShotFiredEvent shot) =>
            _rounds.Fire(
                shot.Origin, shot.Direction, shot.Speed, shot.LifeSeconds, shot.Radius, shot.Damage);

        /// <summary>
        /// Hands every live round a body and takes the spare bodies back.
        /// </summary>
        /// <remarks>
        /// Bodies are matched to slots, not to rounds — a round that ends is swapped out of the
        /// middle of the array and the body at that slot simply shows whoever took its place.
        /// Over a tenth of a second nobody can tell, and it is far cheaper than tracking which
        /// streak belongs to which round.
        /// </remarks>
        private void SyncViews()
        {
            int live = _rounds.Count;

            while (_active.Count < live)
            {
                ProjectileView view = _views.Get();
                view.Sprite = _bulletSprite;
                _active.Add(view);
            }

            while (_active.Count > live)
            {
                int last = _active.Count - 1;
                _views.Return(_active[last]);
                _active.RemoveAt(last);
            }

            System.ReadOnlySpan<ProjectileState> rounds = _rounds.Rounds;
            for (int i = 0; i < live; i++)
            {
                _active[i].Show(rounds[i].Position, rounds[i].Velocity);
            }
        }

        private ProjectileView CreateView()
        {
            var body = new GameObject("Round", typeof(SpriteRenderer), typeof(ProjectileView));
            body.transform.SetParent(transform, worldPositionStays: true);
            return body.GetComponent<ProjectileView>();
        }
    }
}
