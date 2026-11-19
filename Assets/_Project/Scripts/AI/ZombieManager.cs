#nullable enable
using System.Collections.Generic;
using System;
using UnityEngine;
using ZombieGame.Combat;
using ZombieGame.Core.Events;
using ZombieGame.Core.Physics;

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
        private readonly ZombieInstincts[] _instincts;
        private readonly ZombieBite[] _bites;
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
            _instincts = new ZombieInstincts[capacity];
            _bites = new ZombieBite[capacity];
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

            // And its own face, where the kind has more than one. Drawn only then, so a kind with
            // a single look leaves the random sequence — and therefore the whole zone — exactly
            // as it was before skins existed.
            int skin = definition.SkinCount > 1 ? _random.Next(definition.SkinCount) : 0;

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

                // Resting with no time left on the clock, so the first decision it makes is to
                // pick somewhere to shuffle off to rather than to stand through a pause it never
                // started. Full speed until something decides otherwise, so anything driving the
                // horde by hand — tests, a scripted set piece — gets the speed on the asset.
                Behaviour = ZombieBehaviour.Resting,
                BehaviourTimer = 0f,
                Home = position,
                LastKnown = position,
                SpeedFactor = 1f,
                TypeId = typeId,
                SkinId = skin,
                Id = id,
            };
            _resistances[index] = definition.Resistances;
            _senses[index] = definition.Senses;
            _instincts[index] = definition.Instincts;
            _bites[index] = definition.Bite;

            return id;
        }

        /// <summary>
        /// Lets every zombie in reach of <paramref name="target"/> bite it, and returns what they
        /// took off between them.
        /// </summary>
        /// <remarks>
        /// One pass over the array, like everything else here: with three hundred bodies a zone
        /// cannot afford a trigger collider and an attack coroutine each. The cooldown is per
        /// zombie, so a crowd closing in from three sides hurts three times as fast as one — the
        /// arithmetic that makes being surrounded the thing to avoid.
        /// Only a zombie that is actually hunting bites. One that has merely wandered into you
        /// has not noticed you, and a corpse that gnaws at whatever it bumps into would make
        /// sneaking past a standing crowd impossible rather than tense.
        /// </remarks>
        /// <param name="target">Where the squad is.</param>
        /// <param name="targetRadius">How wide the squad is, in world units.</param>
        /// <param name="deltaSeconds">How long since the last call.</param>
        /// <param name="type">Set to the kind of harm done, when anything was.</param>
        /// <returns>Raw damage dealt this step, before the target's own resistances.</returns>
        public float BiteAt(
            Vector2 target, float targetRadius, float deltaSeconds, out DamageType type)
        {
            type = DamageType.Sharp;
            float total = 0f;

            for (int i = 0; i < Count; i++)
            {
                ref ZombieState state = ref _states[i];
                if (state.BiteTimer > 0f)
                {
                    state.BiteTimer = Mathf.Max(0f, state.BiteTimer - deltaSeconds);
                    continue;
                }

                if (state.Behaviour != ZombieBehaviour.Chasing)
                {
                    continue;
                }

                ZombieBite bite = _bites[i];
                float reach = state.Radius + targetRadius + bite.Reach;
                if ((target - state.Position).sqrMagnitude > reach * reach)
                {
                    continue;
                }

                state.BiteTimer = bite.Seconds;
                if (bite.Damage <= 0f)
                {
                    continue;
                }

                total += bite.Damage;
                type = bite.Type;
            }

            return total;
        }

        /// <summary>
        /// Finds a zombie that could be taken down quietly from <paramref name="from"/>.
        /// </summary>
        /// <remarks>
        /// Three conditions, and each one is a rule the player can feel. It has to be within
        /// arm's length. It has to not be hunting — something already coming for you cannot be
        /// crept up on, and that is what stops a takedown being a free answer to a fight. And you
        /// have to be behind it, measured against where it is actually looking rather than where
        /// it is walking, because a zombie that has turned to stare at a noise has its back to
        /// wherever it was going.
        /// The nearest eligible one wins, so walking into a pair does not pick the far one.
        /// </remarks>
        /// <param name="from">Where the attacker is standing.</param>
        /// <param name="reach">How far the attacker can reach, past both bodies.</param>
        /// <param name="behindDegrees">
        /// How far round the back counts as behind, either way from directly astern.
        /// </param>
        /// <returns>The index of the zombie, or −1 when there is nobody to creep up on.</returns>
        public int FindTakedown(Vector2 from, float reach, float behindDegrees)
        {
            int best = -1;
            float nearest = float.MaxValue;
            float behind = Mathf.Cos(Mathf.Clamp(behindDegrees, 0f, 180f) * Mathf.Deg2Rad);

            for (int i = 0; i < Count; i++)
            {
                ref ZombieState zombie = ref _states[i];
                if (zombie.Behaviour == ZombieBehaviour.Chasing)
                {
                    continue;
                }

                Vector2 toAttacker = from - zombie.Position;
                float distance = toAttacker.magnitude;
                if (distance > zombie.Radius + reach || distance <= 0.0001f)
                {
                    continue;
                }

                // Behind means: the attacker lies outside the arc the zombie is facing. Measured
                // against its look rather than its velocity, because the two come apart the
                // moment it hears something.
                Vector2 look = zombie.Look.sqrMagnitude > 0.0001f ? zombie.Look.normalized : Vector2.down;
                if (Vector2.Dot(look, toAttacker / distance) > -behind)
                {
                    continue;
                }

                if (distance >= nearest)
                {
                    continue;
                }

                nearest = distance;
                best = i;
            }

            return best;
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

                // Being hit interrupts the bite. Without it a bat is a trade — the arc is barely
                // longer than the reach, so every swing landed means a bite taken — and a melee
                // weapon nobody can use without losing health is not a weapon. Hitting first now
                // buys exactly one cycle of that zombie's own rhythm, so a Bloater reels longer
                // than a Nurse without a second number being invented for it, and a crowd still
                // overwhelms you because each of them has to be hit separately.
                _states[index].BiteTimer = Math.Max(_states[index].BiteTimer, _bites[index].Seconds);
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

        /// <summary>
        /// Finds the first zombie standing on a line, for something travelling along it.
        /// </summary>
        /// <remarks>
        /// A segment rather than a point because a round covers half a world unit between physics
        /// steps: testing only where it landed lets it pass clean through a body that was standing
        /// in the middle of that jump.
        /// "First" means nearest to <paramref name="from"/>, so a round stops at the zombie in
        /// front rather than at whichever one happens to sit earlier in the array.
        /// </remarks>
        /// <param name="from">Where the segment starts.</param>
        /// <param name="to">Where it ends.</param>
        /// <param name="radius">How fat the thing travelling is.</param>
        /// <param name="index">The slot of the zombie that was struck.</param>
        /// <param name="point">Where along the segment it struck.</param>
        /// <returns>False when the line is clear of zombies.</returns>
        public bool TryHitSegment(
            Vector2 from, Vector2 to, float radius, out int index, out Vector2 point)
        {
            index = -1;
            point = to;

            Vector2 along = to - from;
            float lengthSquared = along.sqrMagnitude;
            float nearest = float.MaxValue;

            for (int i = 0; i < Count; i++)
            {
                ZombieState zombie = _states[i];
                float reach = zombie.Radius + radius;

                // How far along the segment this body sits, clamped to its ends so a zombie
                // behind the muzzle or beyond the step is measured from the nearer end.
                float t = lengthSquared > 0.000001f
                    ? Mathf.Clamp01(Vector2.Dot(zombie.Position - from, along) / lengthSquared)
                    : 0f;

                Vector2 closest = from + (along * t);
                if ((zombie.Position - closest).sqrMagnitude > reach * reach || t >= nearest)
                {
                    continue;
                }

                nearest = t;
                index = i;
                point = closest;
            }

            return index >= 0;
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
        /// <param name="visibility">
        /// How easily the target is seen at all, against a zombie's view distance. One is somebody
        /// walking about; crouching is well under that, which is the whole of what crouching does
        /// to a watcher. Hearing is untouched — ears have no cone and keeping low does not make a
        /// footstep silent, only quieter, and that is the noise system's business.
        /// </param>
        public int Perceive(
            Vector2 target,
            float targetRadius,
            ILineOfSight? lineOfSight = null,
            float visibility = 1f)
        {
            ILineOfSight sight = lineOfSight ?? OpenLineOfSight.Instance;
            int seen = 0;
            float seenAt = Mathf.Clamp(visibility, 0.01f, 1f);

            for (int i = 0; i < Count; i++)
            {
                ref ZombieState zombie = ref _states[i];

                ZombieSenses senses = seenAt >= 1f
                    ? _senses[i]
                    : new ZombieSenses(
                        _senses[i].ViewDistance * seenAt,
                        _senses[i].ViewHalfAngleDegrees,
                        _senses[i].HearingRadius,
                        _senses[i].ListenSeconds);

                bool sees = PerceptionMath.InViewCone(
                                zombie.Position, zombie.Look, target, targetRadius, senses)
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
        /// Lets every zombie decide what to do, and where to walk while doing it.
        /// </summary>
        /// <remarks>
        /// The state machine the whole stealth layer hangs off, and it is deliberately one pass
        /// over one array rather than a component per zombie: three hundred of these run every
        /// physics step.
        /// Sight wins over everything. A zombie that can see the target is chasing it, whatever
        /// it was doing a moment ago, and its memory of where the target is refreshes every step
        /// — so breaking line of sight leaves it walking at a place you have already left, which
        /// is what makes a corner worth running round.
        /// Call it after <see cref="Perceive"/> and before <see cref="Tick"/>: it reads what was
        /// perceived and writes the destinations that the tick then walks towards.
        /// </remarks>
        /// <param name="target">Where the thing they hunt is right now.</param>
        /// <param name="deltaSeconds">How much time has passed.</param>
        /// <returns>How many are chasing.</returns>
        public int Decide(Vector2 target, float deltaSeconds)
        {
            int chasing = 0;

            for (int i = 0; i < Count; i++)
            {
                ref ZombieState zombie = ref _states[i];
                ZombieInstincts instincts = _instincts[i];

                if (zombie.SeesTarget)
                {
                    zombie.Behaviour = ZombieBehaviour.Chasing;
                    zombie.BehaviourTimer = instincts.GiveUpSeconds;
                    zombie.LastKnown = target;
                    zombie.Destination = target;
                    zombie.SpeedFactor = 1f;
                    chasing++;
                    continue;
                }

                zombie.BehaviourTimer -= deltaSeconds;
                bool arrived = (zombie.Destination - zombie.Position).sqrMagnitude
                               <= instincts.ArriveDistance * instincts.ArriveDistance;

                switch (zombie.Behaviour)
                {
                    case ZombieBehaviour.Chasing:
                        // Lost sight of it. Keep going to where it was last seen — giving up the
                        // instant a wall interrupts would make every pillar in the zone a
                        // guaranteed escape.
                        zombie.Destination = zombie.LastKnown;
                        zombie.SpeedFactor = 1f;
                        if (zombie.BehaviourTimer <= 0f || arrived)
                        {
                            GiveUp(ref zombie, instincts);
                        }

                        break;

                    case ZombieBehaviour.Investigating:
                        zombie.Destination = zombie.LastKnown;
                        zombie.SpeedFactor = 1f;
                        if (zombie.BehaviourTimer <= 0f || arrived)
                        {
                            GiveUp(ref zombie, instincts);
                        }

                        break;

                    case ZombieBehaviour.Wandering:
                        zombie.SpeedFactor = instincts.WanderSpeedFactor;
                        if (zombie.BehaviourTimer <= 0f || arrived)
                        {
                            Rest(ref zombie, instincts);
                        }

                        break;

                    default:
                        zombie.Destination = zombie.Position;
                        zombie.SpeedFactor = instincts.WanderSpeedFactor;
                        if (zombie.BehaviourTimer <= 0f)
                        {
                            Wander(ref zombie, instincts);
                        }

                        break;
                }
            }

            return chasing;
        }

        /// <summary>Stops chasing or investigating and goes back to standing about.</summary>
        private static void GiveUp(ref ZombieState zombie, in ZombieInstincts instincts)
        {
            // Resting where it gave up, not where it started: a zombie drawn across the zone by a
            // noise stays drawn across the zone, which is what makes a thrown bottle worth using.
            zombie.Home = zombie.Position;
            Rest(ref zombie, instincts);
        }

        /// <summary>Stands still for a while.</summary>
        private static void Rest(ref ZombieState zombie, in ZombieInstincts instincts)
        {
            zombie.Behaviour = ZombieBehaviour.Resting;
            zombie.BehaviourTimer = instincts.WanderPauseSeconds;
            zombie.Destination = zombie.Position;
            zombie.SpeedFactor = instincts.WanderSpeedFactor;
        }

        /// <summary>Picks somewhere near home and shuffles off to it.</summary>
        private void Wander(ref ZombieState zombie, in ZombieInstincts instincts)
        {
            zombie.Behaviour = ZombieBehaviour.Wandering;
            zombie.SpeedFactor = instincts.WanderSpeedFactor;

            if (instincts.WanderRadius <= 0f)
            {
                // A kind that does not stray stands where it was put, for as long as the zone
                // lasts. Guards and penned zombies will want exactly this.
                zombie.Destination = zombie.Home;
                zombie.BehaviourTimer = instincts.WanderPauseSeconds;
                return;
            }

            double angle = _random.NextDouble() * Math.PI * 2d;
            double distance = _random.NextDouble() * instincts.WanderRadius;
            zombie.Destination = zombie.Home + new Vector2(
                (float)(Math.Cos(angle) * distance),
                (float)(Math.Sin(angle) * distance));

            // A generous budget rather than an estimate: the timer is here so that a zombie stuck
            // against a wall eventually picks somewhere else, not to time the walk.
            float reach = Mathf.Max(0.01f, zombie.Speed * instincts.WanderSpeedFactor);
            zombie.BehaviourTimer = (instincts.WanderRadius * 2f / reach) + instincts.WanderPauseSeconds;
        }

        /// <summary>
        /// Lets a noise reach the horde. Whoever hears it turns to look that way.
        /// </summary>
        /// <remarks>
        /// Turning the head is the smallest thing hearing can do and still matter, and it matters
        /// a lot: a zombie looking at a noise is a zombie whose view cone has moved, so the sound
        /// you made behind it is what gets you seen. Walking over to investigate, and giving up
        /// again, are the next step — this one only redirects attention.
        /// Facing is left alone on purpose. The three-pose art shows which way a body is walking,
        /// and a zombie that spun its sprite round without moving would read as a bug.
        /// </remarks>
        /// <param name="origin">Where the noise came from.</param>
        /// <param name="loudness">How loud it was. 1 is an ordinary noise.</param>
        /// <returns>How many heard it.</returns>
        public int HearNoise(Vector2 origin, float loudness)
        {
            int heard = 0;

            for (int i = 0; i < Count; i++)
            {
                ref ZombieState zombie = ref _states[i];
                if (!PerceptionMath.CanHear(
                        zombie.Position, origin, loudness, _senses[i].HearingRadius))
                {
                    continue;
                }

                heard++;
                zombie.HeardAt = origin;
                zombie.ListenTimer = _senses[i].ListenSeconds;

                // A noise is worth walking over to look at — unless it is already chasing
                // something it can see, in which case the thing it can see is the better lead.
                if (zombie.Behaviour != ZombieBehaviour.Chasing)
                {
                    zombie.Behaviour = ZombieBehaviour.Investigating;
                    zombie.BehaviourTimer = _instincts[i].GiveUpSeconds;
                    zombie.LastKnown = origin;
                }

                Vector2 towards = origin - zombie.Position;
                if (towards.sqrMagnitude > 0.000001f)
                {
                    zombie.Look = towards.normalized;
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
        /// <param name="deltaSeconds">How long since the last tick.</param>
        /// <param name="walls">What is solid. Open ground when nothing is passed.</param>
        /// <param name="route">
        /// Which way is downhill towards whatever the horde is hunting. Used only by the ones
        /// actually chasing: a zombie that has heard a noise is walking to a spot it picked, not
        /// to the player, and one that is shuffling about has nowhere particular to be.
        /// </param>
        public void Tick(float deltaSeconds, IWallMap? walls = null, IFlowField? route = null)
        {
            if (deltaSeconds <= 0f)
            {
                return;
            }

            IWallMap solid = walls ?? OpenGround.Instance;
            IFlowField flow = route ?? NoFlow.Instance;
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

                // Same here: a zombie listening to something stops listening eventually, whether
                // or not it is walking anywhere.
                if (zombie.ListenTimer > 0f)
                {
                    zombie.ListenTimer = Math.Max(0f, zombie.ListenTimer - deltaSeconds);
                }

                Vector2 toTarget = zombie.Destination - zombie.Position;
                float distance = toTarget.magnitude;

                // What this one can do right now: its own top speed, scaled by whatever its
                // behaviour allows. A shuffling zombie and a charging one are the same body.
                float speed = zombie.Speed * zombie.SpeedFactor;
                float step = speed * deltaSeconds;

                Vector2 push = separating ? SeparationFor(i) : Vector2.zero;

                if ((distance <= 0.0001f || step <= 0f) && push == Vector2.zero)
                {
                    zombie.Velocity = Vector2.zero;
                    continue;
                }

                Vector2 direction = distance > 0.0001f ? toTarget / distance : Vector2.zero;

                // A chase follows the route rather than the straight line. Without this a zombie
                // walks at the player and stops dead against whatever is in between, which from
                // the far side of a wall reads as a corpse having a quiet moment rather than as
                // something hunting you.
                // Only while it can see where it is going downhill: where the field says nothing
                // — a zombie inside a doorway, or a frame before the first sweep — the straight
                // line is still better than standing still.
                if (zombie.Behaviour == ZombieBehaviour.Chasing && distance > 0.75f)
                {
                    Vector2 downhill = flow.Direction(zombie.Position);
                    if (downhill.sqrMagnitude > 0.000001f)
                    {
                        direction = downhill;
                    }
                }

                // Arrival only counts when nothing is shoving: a zombie standing on its
                // destination still has to give way to the one walking into it.
                if (push == Vector2.zero && step >= distance)
                {
                    zombie.Position = Slide(zombie.Position, toTarget, zombie.Radius, solid);
                    zombie.Velocity = Vector2.zero;
                }
                else
                {
                    Vector2 desired = (direction * speed) +
                                      (push * speed * _crowd.SeparationStrength);

                    // Clamped to walking speed, so being crowded never makes a zombie faster than
                    // it is supposed to be able to move.
                    if (desired.sqrMagnitude > speed * speed)
                    {
                        desired = desired.normalized * speed;
                    }

                    zombie.Velocity = desired;
                    zombie.Position =
                        Slide(zombie.Position, desired * deltaSeconds, zombie.Radius, solid);
                    direction = desired.sqrMagnitude > 0.000001f ? desired.normalized : direction;
                }

                // A zombie looks where it is going. Kept only while it is actually moving, so one
                // that has stopped keeps staring where it last walked instead of snapping to a
                // zero vector and going blind — and not at all while it is still listening to
                // something, which is what gives a noise its hold on a zombie's attention.
                if (zombie.ListenTimer <= 0f && direction.sqrMagnitude > 0.000001f)
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

        /// <summary>
        /// Moves a body by a step, keeping whichever part of it does not run into a wall.
        /// </summary>
        /// <remarks>
        /// One axis at a time, which is what makes a zombie slide along a wall instead of
        /// sticking to it: walking diagonally into a wall keeps the component that is parallel to
        /// it. A single test of the whole step would stop the body dead at every corner, and
        /// no test at all is what let them walk through the building.
        /// The probe is offset by the body's radius, so a zombie stops with its shoulder against
        /// the wall rather than with its centre inside it.
        /// </remarks>
        private static Vector2 Slide(Vector2 from, Vector2 step, float radius, IWallMap walls)
        {
            Vector2 moved = from;

            if (step.x != 0f)
            {
                float x = moved.x + step.x;
                if (!walls.IsSolid(new Vector2(x + (Math.Sign(step.x) * radius), moved.y)))
                {
                    moved.x = x;
                }
            }

            if (step.y != 0f)
            {
                float y = moved.y + step.y;
                if (!walls.IsSolid(new Vector2(moved.x, y + (Math.Sign(step.y) * radius))))
                {
                    moved.y = y;
                }
            }

            return moved;
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
                _instincts[index] = _instincts[last];
                _bites[index] = _bites[last];
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
