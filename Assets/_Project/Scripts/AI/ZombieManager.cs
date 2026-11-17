#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieGame.Combat;
using ZombieGame.Core.Events;

namespace ZombieGame.AI
{
    /// <summary>
    /// Owns every zombie in a zone and ticks them as one batch.
    /// </summary>
    /// <remarks>
    /// The reason this exists is in CLAUDE.md: zombie counts reach several hundred, and hundreds of
    /// <c>MonoBehaviour.Update</c> calls would spend more time on engine overhead than on the game.
    /// One manager walks one array instead.
    /// Dead zombies are removed by swapping the last live one into the hole, so the live range
    /// stays packed and the tick never checks whether a slot is empty. That means <b>indices are
    /// not stable</b> — anything holding on to a zombie between frames holds its
    /// <see cref="ZombieState.Id"/> and looks it up.
    /// </remarks>
    public sealed class ZombieManager
    {
        private readonly ZombieState[] _states;
        private readonly DamageResistances[] _resistances;
        private readonly ZombieSenses[] _senses;
        private readonly EventBus? _bus;
        private readonly CrowdSettings _crowd;
        private readonly System.Random _random;

        // Neighbour search, allocated once. The dictionary is cleared rather than replaced each
        // tick, which keeps its buckets and so allocates nothing after the first few frames.
        private readonly Dictionary<long, int> _cellHeads = new();
        private readonly int[] _nextInCell;

        private int _nextId = 1;

        /// <param name="capacity">
        /// The most zombies that can be alive at once. Fixed on purpose: the array is allocated
        /// once, and a zone that wants more has a design problem, not a memory problem.
        /// </param>
        /// <param name="bus">Optional; deaths are published on it.</param>
        /// <param name="crowd">How hard they push each other apart.</param>
        /// <param name="seed">
        /// Drives the per-zombie speed variation. Fixed by default so a zone behaves the same way
        /// every run, which is what makes a bug in a horde reproducible.
        /// </param>
        public ZombieManager(
            int capacity,
            EventBus? bus = null,
            CrowdSettings crowd = default,
            int seed = 20261117)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(capacity), capacity, "A horde of none is not worth managing.");
            }

            Capacity = capacity;
            _states = new ZombieState[capacity];
            _resistances = new DamageResistances[capacity];
            _senses = new ZombieSenses[capacity];
            _nextInCell = new int[capacity];
            _bus = bus;

            // A default-constructed CrowdSettings has a cell size of zero, which is nobody asking
            // for "no separation" — it is nobody having said anything.
            _crowd = crowd.CellSize > 0f ? crowd : CrowdSettings.Default;
            _random = new System.Random(seed);
        }

        /// <summary>The most zombies that can be alive at once.</summary>
        /// <summary>How long a zombie flashes after taking a hit.</summary>
        public const float HitFlashSeconds = 0.12f;

        public int Capacity { get; }

        /// <summary>How many are alive right now.</summary>
        public int Count { get; private set; }

        /// <summary>True when another spawn would not fit.</summary>
        public bool IsFull => Count >= Capacity;

        /// <summary>Raised when a zombie dies, before it leaves the array.</summary>
        public event Action<DiedEvent>? ZombieDied;

        /// <summary>
        /// The live zombies. Only the first <see cref="Count"/> entries mean anything, and their
        /// order changes as zombies die.
        /// </summary>
        public ReadOnlySpan<ZombieState> States => new(_states, 0, Count);

        /// <summary>Reads one zombie by its slot.</summary>
        /// <exception cref="ArgumentOutOfRangeException">The index is not a live slot.</exception>
        public ZombieState Get(int index)
        {
            ThrowIfNotLive(index);
            return _states[index];
        }

        /// <summary>
        /// Adds a zombie at <paramref name="position"/>.
        /// </summary>
        /// <returns>Its id, or zero when the horde is already at capacity.</returns>
        public int Spawn(ZombieDefinition definition, Vector2 position, int typeId = 0)
        {
            if (definition is null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (IsFull)
            {
                return 0;
            }

            int index = Count++;
            int id = _nextId++;

            // Every zombie gets its own walking speed within the type's spread. Identical speeds
            // are the other half of why a horde collapses into one body: they stay in lockstep
            // forever, however far they walk.
            float spread = definition.SpeedVariance;
            float factor = 1f + (((float)_random.NextDouble() * 2f) - 1f) * spread;

            _states[index] = new ZombieState
            {
                Position = position,
                Destination = position,
                Velocity = Vector2.zero,
                Health = definition.MaxHealth,
                Speed = Mathf.Max(0f, definition.MoveSpeed * factor),
                Radius = definition.BodyRadius,
                Facing = ZombieFacing.Front,

                // Looking the way the art shows it: at the camera, which in a top-down view is
                // down the screen. Without this a fresh zombie looks at (0,0) and sees nothing.
                Look = Vector2.down,
                TypeId = typeId,
                Id = id,
            };
            _resistances[index] = definition.Resistances;
            _senses[index] = definition.Senses;

            return id;
        }

        /// <summary>Finds the slot a zombie currently sits in, or −1 when it is gone.</summary>
        public int IndexOf(int id)
        {
            if (id <= 0)
            {
                return -1;
            }

            for (int i = 0; i < Count; i++)
            {
                if (_states[i].Id == id)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Puts a zombie somewhere directly, without walking it there.
        /// </summary>
        /// <remarks>
        /// For spawning, for a zone setting up its opening state, and for tests that need a
        /// situation rather than a journey. Not for anything the zombie itself decides.
        /// </remarks>
        public void Teleport(int index, Vector2 position)
        {
            ThrowIfNotLive(index);
            _states[index].Position = position;
        }

        /// <summary>Points a zombie at somewhere to walk.</summary>
        public void SetDestination(int index, Vector2 destination)
        {
            ThrowIfNotLive(index);
            _states[index].Destination = destination;
        }

        /// <summary>Points every zombie at the same spot. The crude horde, until perception lands.</summary>
        public void SetDestinationForAll(Vector2 destination)
        {
            for (int i = 0; i < Count; i++)
            {
                _states[i].Destination = destination;
            }
        }

        /// <summary>
        /// Hurts one zombie, killing and removing it when its health runs out.
        /// </summary>
        /// <returns>How much health it cost.</returns>
        public float Damage(int index, DamageInfo damage)
        {
            ThrowIfNotLive(index);

            float applied = Math.Min(_resistances[index].Apply(damage), _states[index].Health);
            if (applied <= 0f)
            {
                return 0f;
            }

            _states[index].Health -= applied;
            if (_states[index].Health > 0f)
            {
                // Not a balance number: how long a body flashes is how the hit is read on screen,
                // and it has no effect on what the hit does.
                _states[index].HitFlash = HitFlashSeconds;
                return applied;
            }

            KillAt(index, damage);
            return applied;
        }

        /// <summary>
        /// Works out who a swing caught and hurts them.
        /// </summary>
        /// <remarks>
        /// Walked backwards because a killed zombie is replaced by the one from the end of the
        /// array: going down means the slot that gets overwritten has already been considered.
        /// </remarks>
        /// <returns>How many were hit.</returns>
        public int ApplySwing(in MeleeSwungEvent swing)
        {
            int hits = 0;

            for (int i = Count - 1; i >= 0 && hits < swing.MaxTargets; i--)
            {
                if (!MeleeArc.Covers(
                        swing.Origin,
                        swing.Facing,
                        _states[i].Position,
                        _states[i].Radius,
                        swing.Range,
                        swing.HalfAngleDegrees))
                {
                    continue;
                }

                Damage(i, swing.Damage);
                hits++;
            }

            return hits;
        }

        /// <summary>Removes a zombie outright, raising its death.</summary>
        /// <returns>False when that slot held nothing.</returns>
        public bool Kill(int index, DamageInfo killingBlow = default)
        {
            if (index < 0 || index >= Count)
            {
                return false;
            }

            KillAt(index, killingBlow);
            return true;
        }

        /// <summary>What one zombie can see and hear. For tests and for drawing the cones.</summary>
        public ZombieSenses SensesOf(int index)
        {
            ThrowIfNotLive(index);
            return _senses[index];
        }

        /// <summary>
        /// Works out which zombies can see the target right now.
        /// </summary>
        /// <remarks>
        /// Sight is two cheap tests and one expensive one, in that order: the cone is geometry,
        /// the wall check is a physics query. Asking physics only about the zombies that already
        /// have the target in their cone keeps a horde of three hundred from firing three hundred
        /// linecasts a tick for an answer the geometry had already given.
        /// Nothing acts on the result yet — chasing, investigating and giving up are the next
        /// step. This only answers the question.
        /// </remarks>
        /// <param name="target">Where the target is.</param>
        /// <param name="targetRadius">How wide the target is.</param>
        /// <param name="lineOfSight">What blocks sight. Null means nothing does.</param>
        /// <returns>How many zombies can see it.</returns>
        public int Perceive(Vector2 target, float targetRadius, ILineOfSight? lineOfSight = null)
        {
            ILineOfSight sight = lineOfSight ?? OpenLineOfSight.Instance;
            int seen = 0;

            for (int i = 0; i < Count; i++)
            {
                ref ZombieState zombie = ref _states[i];

                bool sees = PerceptionMath.InViewCone(
                                zombie.Position, zombie.Look, target, targetRadius, _senses[i])
                            && sight.IsClear(zombie.Position, target);

                zombie.SeesTarget = sees;
                if (sees)
                {
                    seen++;
                }
            }

            return seen;
        }

        /// <summary>
        /// Which zombies could hear a noise, as a count. Who reacts is the next step's problem.
        /// </summary>
        /// <param name="origin">Where the noise came from.</param>
        /// <param name="loudness">How loud it was. 1 is an ordinary noise.</param>
        public int CountHearing(Vector2 origin, float loudness)
        {
            int heard = 0;
            for (int i = 0; i < Count; i++)
            {
                if (PerceptionMath.CanHear(
                        _states[i].Position, origin, loudness, _senses[i].HearingRadius))
                {
                    heard++;
                }
            }

            return heard;
        }

        /// <summary>
        /// Moves every zombie one step towards its destination.
        /// </summary>
        /// <remarks>
        /// Still dumb about where to go: straight-line seek, no perception, no states.
        /// Investigating a noise, chasing and losing interest arrive with the senses in a later
        /// step; this is the integration loop they will drive.
        /// What it is no longer dumb about is each other — see <see cref="CrowdSettings"/>.
        /// </remarks>
        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds <= 0f)
            {
                return;
            }

            bool separating = _crowd.SeparationStrength > 0f && Count > 1;
            if (separating)
            {
                BuildNeighbourGrid();
            }

            for (int i = 0; i < Count; i++)
            {
                ref ZombieState zombie = ref _states[i];

                // Before any of the early exits below: a zombie standing still still has to stop
                // flashing.
                if (zombie.HitFlash > 0f)
                {
                    zombie.HitFlash = Math.Max(0f, zombie.HitFlash - deltaSeconds);
                }

                Vector2 toTarget = zombie.Destination - zombie.Position;
                float distance = toTarget.magnitude;
                float step = zombie.Speed * deltaSeconds;

                Vector2 push = separating ? SeparationFor(i) : Vector2.zero;

                if ((distance <= 0.0001f || step <= 0f) && push == Vector2.zero)
                {
                    zombie.Velocity = Vector2.zero;
                    continue;
                }

                Vector2 direction = distance > 0.0001f ? toTarget / distance : Vector2.zero;

                // Arrival only counts when nothing is shoving: a zombie standing on its
                // destination still has to give way to the one walking into it.
                if (push == Vector2.zero && step >= distance)
                {
                    zombie.Position = zombie.Destination;
                    zombie.Velocity = Vector2.zero;
                }
                else
                {
                    Vector2 desired = (direction * zombie.Speed) +
                                      (push * zombie.Speed * _crowd.SeparationStrength);

                    // Clamped to walking speed, so being crowded never makes a zombie faster than
                    // it is supposed to be able to move.
                    if (desired.sqrMagnitude > zombie.Speed * zombie.Speed)
                    {
                        desired = desired.normalized * zombie.Speed;
                    }

                    zombie.Velocity = desired;
                    zombie.Position += desired * deltaSeconds;
                    direction = desired.sqrMagnitude > 0.000001f ? desired.normalized : direction;
                }

                // A zombie looks where it is going. Kept only while it is actually moving, so one
                // that has stopped keeps staring where it last walked instead of snapping to a
                // zero vector and going blind.
                if (direction.sqrMagnitude > 0.000001f)
                {
                    zombie.Look = direction;
                }

                // Only sideways movement flips the sprite; walking straight up or down keeps the
                // front view, which is what the three-pose art supports.
                if (direction.x > 0.3f)
                {
                    zombie.Facing = ZombieFacing.Right;
                }
                else if (direction.x < -0.3f)
                {
                    zombie.Facing = ZombieFacing.Left;
                }
                else
                {
                    zombie.Facing = ZombieFacing.Front;
                }
            }
        }

        /// <summary>Empties the horde without raising anything. For tearing a zone down.</summary>
        public void Clear() => Count = 0;

        /// <summary>
        /// Buckets every zombie by the cell it stands in, so the neighbour search only has to look
        /// at the nine cells around one instead of at the whole horde.
        /// </summary>
        /// <remarks>
        /// Comparing everyone with everyone is 90,000 distance checks at 300 zombies, every tick.
        /// The grid turns that into a handful per zombie, which is the difference between
        /// separation being free and separation being the most expensive thing in the frame.
        /// </remarks>
        private void BuildNeighbourGrid()
        {
            _cellHeads.Clear();

            for (int i = 0; i < Count; i++)
            {
                long key = CellKey(_states[i].Position);
                _nextInCell[i] = _cellHeads.TryGetValue(key, out int head) ? head : -1;
                _cellHeads[key] = i;
            }
        }

        /// <summary>
        /// The push away from everyone standing too close, as a direction of length at most one.
        /// </summary>
        private Vector2 SeparationFor(int index)
        {
            ZombieState self = _states[index];
            var push = Vector2.zero;

            int cellX = Mathf.FloorToInt(self.Position.x / _crowd.CellSize);
            int cellY = Mathf.FloorToInt(self.Position.y / _crowd.CellSize);

            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    long key = CellKey(cellX + dx, cellY + dy);
                    if (!_cellHeads.TryGetValue(key, out int other))
                    {
                        continue;
                    }

                    while (other >= 0)
                    {
                        if (other != index)
                        {
                            push += PushAwayFrom(self, _states[other]);
                        }

                        other = _nextInCell[other];
                    }
                }
            }

            return push.sqrMagnitude > 1f ? push.normalized : push;
        }

        /// <summary>
        /// How hard one body shoves another, growing as they overlap and vanishing once they do not.
        /// </summary>
        private static Vector2 PushAwayFrom(ZombieState self, ZombieState other)
        {
            Vector2 away = self.Position - other.Position;
            float gap = self.Radius + other.Radius;
            float distanceSquared = away.sqrMagnitude;

            if (distanceSquared >= gap * gap || gap <= 0f)
            {
                return Vector2.zero;
            }

            // Two bodies on exactly the same spot have no direction to part in. Nudging by the
            // difference in their ids keeps it deterministic and gets them unstuck.
            if (distanceSquared < 0.000001f)
            {
                int sign = self.Id > other.Id ? 1 : -1;
                return new Vector2(0.01f * sign, 0.01f * sign);
            }

            float distance = Mathf.Sqrt(distanceSquared);
            float overlap = (gap - distance) / gap;
            return away / distance * overlap;
        }

        private long CellKey(Vector2 position) =>
            CellKey(
                Mathf.FloorToInt(position.x / _crowd.CellSize),
                Mathf.FloorToInt(position.y / _crowd.CellSize));

        private static long CellKey(int x, int y) => ((long)x << 32) ^ (uint)y;

        private void KillAt(int index, DamageInfo killingBlow)
        {
            var died = new DiedEvent(_states[index].Id, killingBlow);

            // Announced before the swap, so a listener can still read the zombie that died.
            ZombieDied?.Invoke(died);
            _bus?.Publish(died);

            int last = --Count;
            if (index != last)
            {
                _states[index] = _states[last];
                _resistances[index] = _resistances[last];
                _senses[index] = _senses[last];
            }
        }

        private void ThrowIfNotLive(int index)
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index), index, $"Only 0..{Count - 1} are live zombies.");
            }
        }
    }
}
