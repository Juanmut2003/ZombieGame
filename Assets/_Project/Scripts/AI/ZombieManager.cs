#nullable enable
using System;
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
        private readonly EventBus? _bus;
        private int _nextId = 1;

        /// <param name="capacity">
        /// The most zombies that can be alive at once. Fixed on purpose: the array is allocated
        /// once, and a zone that wants more has a design problem, not a memory problem.
        /// </param>
        /// <param name="bus">Optional; deaths are published on it.</param>
        public ZombieManager(int capacity, EventBus? bus = null)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(capacity), capacity, "A horde of none is not worth managing.");
            }

            Capacity = capacity;
            _states = new ZombieState[capacity];
            _resistances = new DamageResistances[capacity];
            _bus = bus;
        }

        /// <summary>The most zombies that can be alive at once.</summary>
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
        public int Spawn(ZombieDefinition definition, Vector2 position)
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

            _states[index] = new ZombieState
            {
                Position = position,
                Destination = position,
                Velocity = Vector2.zero,
                Health = definition.MaxHealth,
                Speed = definition.MoveSpeed,
                Facing = ZombieFacing.Front,
                Id = id,
            };
            _resistances[index] = definition.Resistances;

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
                return applied;
            }

            KillAt(index, damage);
            return applied;
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

        /// <summary>
        /// Moves every zombie one step towards its destination.
        /// </summary>
        /// <remarks>
        /// Deliberately dumb for now: straight-line seek, no perception, no avoidance, no states.
        /// Investigating a noise, chasing and losing interest arrive with the senses in a later
        /// step; this is the integration loop they will drive.
        /// TODO: no separation between zombies yet, so a horde stacks on one point.
        /// </remarks>
        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds <= 0f)
            {
                return;
            }

            for (int i = 0; i < Count; i++)
            {
                ref ZombieState zombie = ref _states[i];

                Vector2 toTarget = zombie.Destination - zombie.Position;
                float distance = toTarget.magnitude;
                float step = zombie.Speed * deltaSeconds;

                if (distance <= 0.0001f || step <= 0f)
                {
                    zombie.Velocity = Vector2.zero;
                    continue;
                }

                Vector2 direction = toTarget / distance;
                if (step >= distance)
                {
                    zombie.Position = zombie.Destination;
                    zombie.Velocity = Vector2.zero;
                }
                else
                {
                    zombie.Velocity = direction * zombie.Speed;
                    zombie.Position += zombie.Velocity * deltaSeconds;
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
