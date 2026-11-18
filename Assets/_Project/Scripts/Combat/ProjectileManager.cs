#nullable enable
using System;
using UnityEngine;
using ZombieGame.Core.Physics;

namespace ZombieGame.Combat
{
    /// <summary>
    /// Every round in the air in one zone, moved as one batch.
    /// </summary>
    /// <remarks>
    /// A round is a segment per step, not a point: at twenty-four units a second and a fiftieth of
    /// a second per step, a bullet covers half a world unit between frames. Testing the point it
    /// landed on would let it pass clean through a body that was standing in the middle of that
    /// jump — the classic bullet-through-paper bug, and it gets worse the faster the weapon.
    /// Capacity is fixed and a full array drops new rounds. A zone that fires more than this in
    /// the time one round lives has a design problem, not a memory problem.
    /// </remarks>
    public sealed class ProjectileManager
    {
        private readonly ProjectileState[] _rounds;
        private int _nextId = 1;

        public ProjectileManager(int capacity = 64)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(capacity), capacity, "A magazine of none is not worth managing.");
            }

            Capacity = capacity;
            _rounds = new ProjectileState[capacity];
        }

        /// <summary>The most rounds that can be in the air at once.</summary>
        public int Capacity { get; }

        /// <summary>How many are in the air right now.</summary>
        public int Count { get; private set; }

        /// <summary>
        /// The live rounds. Only the first <see cref="Count"/> mean anything, and their order
        /// changes as rounds expire.
        /// </summary>
        public ReadOnlySpan<ProjectileState> Rounds => new(_rounds, 0, Count);

        /// <summary>Reads one round by its slot.</summary>
        public ProjectileState Get(int index)
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index), index, $"Only 0..{Count - 1} are live rounds.");
            }

            return _rounds[index];
        }

        /// <summary>Puts a round in the air.</summary>
        /// <returns>Its id, or zero when the array is full.</returns>
        public int Fire(
            Vector2 origin, Vector2 direction, float speed, float lifeSeconds, float radius,
            in DamageInfo damage)
        {
            if (Count >= Capacity || speed <= 0f || lifeSeconds <= 0f)
            {
                return 0;
            }

            Vector2 heading = direction.sqrMagnitude > 0.000001f
                ? direction.normalized
                : Vector2.right;

            int index = Count++;
            int id = _nextId++;

            _rounds[index] = new ProjectileState
            {
                Position = origin,
                Velocity = heading * speed,
                Remaining = lifeSeconds,
                Radius = Mathf.Max(0.01f, radius),
                Damage = damage,
                Id = id,
            };

            return id;
        }

        /// <summary>
        /// Moves every round one step, resolving what it ran into on the way.
        /// </summary>
        /// <param name="deltaSeconds">How much time passed.</param>
        /// <param name="targets">Who can be hit. Null means an empty field.</param>
        /// <param name="walls">What stops a round. Null means open ground.</param>
        /// <returns>How many rounds struck something.</returns>
        public int Tick(float deltaSeconds, IProjectileHitTest? targets = null, ILineOfSight? walls = null)
        {
            if (deltaSeconds <= 0f)
            {
                return 0;
            }

            IProjectileHitTest field = targets ?? EmptyField.Instance;
            ILineOfSight barrier = walls ?? OpenLineOfSight.Instance;
            int hits = 0;

            // Walked backwards: a round that ends is replaced by the one from the end of the
            // array, so going down means the slot that gets overwritten is already behind us.
            for (int i = Count - 1; i >= 0; i--)
            {
                ref ProjectileState round = ref _rounds[i];

                Vector2 from = round.Position;
                Vector2 to = from + (round.Velocity * deltaSeconds);

                if (field.TryHit(from, to, round.Radius, round.Damage, out Vector2 point))
                {
                    round.Position = point;
                    hits++;
                    RemoveAt(i);
                    continue;
                }

                // Targets before walls: a zombie standing against a wall is in front of it, and
                // the round reaches the body first.
                if (!barrier.IsClear(from, to))
                {
                    RemoveAt(i);
                    continue;
                }

                round.Position = to;
                round.Remaining -= deltaSeconds;
                if (round.Remaining <= 0f)
                {
                    RemoveAt(i);
                }
            }

            return hits;
        }

        /// <summary>Takes every round out of the air. For tearing a zone down.</summary>
        public void Clear() => Count = 0;

        private void RemoveAt(int index)
        {
            int last = --Count;
            if (index != last)
            {
                _rounds[index] = _rounds[last];
            }
        }
    }
}
