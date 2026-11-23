#nullable enable
using System.Collections.Generic;
using UnityEngine;
using ZombieGame.Core.Config;

namespace ZombieGame.Characters
{
    /// <summary>
    /// Walks a character about a patch of ground with nobody steering them.
    /// </summary>
    /// <remarks>
    /// What makes the base look lived in rather than staffed by a list of names: somebody is
    /// actually out there, crossing the plot and standing about between errands. They are
    /// steered through <see cref="CharacterMovement.Steer"/>, so the walls stop them and the
    /// gait draws them exactly as they do the MC.
    /// Deliberately not a job system: this is the body, not the work. Who does what is still
    /// the colony's business, and nothing here looks at it.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterMovement))]
    public sealed class CharacterStroll : MonoBehaviour
    {
        [Tooltip("Where the pace, the pauses and the patience come from.")]
        [SerializeField]
        private BalanceConfig? _balance;

        [Tooltip("The legs. Usually on the same object.")]
        [SerializeField]
        private CharacterMovement? _movement;

        [Tooltip("The ground they keep to, in world units. Not a balance number: it is the plot.")]
        [SerializeField]
        private Rect _area = new Rect(-2f, -2f, 4f, 4f);

        private Vector2[] _route = new Vector2[8];
        private int _legs;
        private int _onLeg;
        private float _standingFor;
        private float _walkingFor;
        private Vector2 _spot;
        private float _pauseSeconds;
        private float _patienceSeconds;
        private float _arriveDistance;
        private float _speedFactor;

        /// <summary>The ground they keep to.</summary>
        public Rect Area => _area;

        /// <summary>Where they are heading at the moment.</summary>
        public Vector2 Spot => _spot;

        /// <summary>Whether they are standing about rather than walking.</summary>
        public bool Standing => _standingFor > 0f;

        /// <summary>Whether they are on their way to a post, or already at it.</summary>
        public bool HasAJob => _legs > 0;

        /// <summary>True once they have arrived at the last leg of the way to their post.</summary>
        public bool AtWork { get; private set; }

        /// <summary>
        /// Sends them to work along a route, a leg at a time, ending at their post.
        /// </summary>
        /// <remarks>
        /// A route and not a destination, because steering straight at a bench from the far side
        /// of the building means leaning on the back wall for the rest of the day — which is
        /// exactly what it did. Whoever hands the route in has looked at the walls; this only
        /// walks it. Handing in the same route again changes nothing, so it is safe to say every
        /// frame.
        /// </remarks>
        /// <param name="route">Where to walk, in order. The last point is the post.</param>
        public void Follow(IReadOnlyList<Vector2> route)
        {
            if (route.Count == 0)
            {
                Roam();
                return;
            }

            if (SameAs(route))
            {
                return;
            }

            if (_route.Length < route.Count)
            {
                _route = new Vector2[route.Count];
            }

            for (int i = 0; i < route.Count; i++)
            {
                _route[i] = route[i];
            }

            _legs = route.Count;
            _onLeg = 0;
            AtWork = false;
            _spot = _route[0];
            _walkingFor = 0f;
        }

        private bool SameAs(IReadOnlyList<Vector2> route)
        {
            if (_legs != route.Count)
            {
                return false;
            }

            for (int i = 0; i < _legs; i++)
            {
                if (!Approximately(_route[i], route[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Takes the job away again and hands them back the plot.</summary>
        public void Roam()
        {
            if (_legs == 0)
            {
                return;
            }

            _legs = 0;
            _onLeg = 0;
            AtWork = false;
            PickASpot();
        }

        private static bool Approximately(Vector2 a, Vector2 b) =>
            (a - b).sqrMagnitude < 0.0001f;

        private void Awake()
        {
            if (_movement == null)
            {
                _movement = GetComponent<CharacterMovement>();
            }

            if (_balance is null)
            {
                Debug.LogWarning($"[Stroll] {name} has no BalanceConfig; using the defaults.");
                _balance = BalanceConfig.CreateDefault();
            }

            _pauseSeconds = _balance.StrollPauseSeconds;
            _patienceSeconds = _balance.StrollPatienceSeconds;
            _arriveDistance = _balance.StrollArriveDistance;
            _speedFactor = _balance.StrollSpeedFactor;

            // Part way through a pause rather than at the start of one, so that two survivors
            // added at the same moment do not set off and stop in step like a chorus line.
            _standingFor = Random.Range(0f, _pauseSeconds);
            PickASpot();
        }

        // In FixedUpdate because the thing it drives runs there: steering in Update would hand
        // the legs a direction that is one physics step out of date every other frame.
        private void FixedUpdate()
        {
            if (_movement == null)
            {
                return;
            }

            if (_standingFor > 0f)
            {
                _standingFor -= Time.fixedDeltaTime;
                _movement.Steer(Vector2.zero);
                return;
            }

            // Already at their post: standing at a bench is the whole point of being sent to one,
            // so there is nothing to steer and nothing to pick.
            if (AtWork)
            {
                _movement.Steer(Vector2.zero);
                return;
            }

            _walkingFor += Time.fixedDeltaTime;
            Vector2 here = transform.position;
            Vector2 move = StrollMath.Toward(here, _spot, _arriveDistance, _speedFactor);
            bool arrived = move == Vector2.zero;

            // Arrived, or leaning on something. Giving up matters as much as arriving: a survivor
            // walking into the side of a barracks would otherwise keep walking into it for the
            // rest of the run.
            if (arrived || _walkingFor >= _patienceSeconds)
            {
                _movement.Steer(Vector2.zero);
                _standingFor = _pauseSeconds;

                if (_legs > 0)
                {
                    NextLeg(arrived);
                }
                else
                {
                    PickASpot();
                }

                return;
            }

            _movement.Steer(move);
        }

        /// <summary>
        /// Moves on to the next leg of the way to work, or tries this one again.
        /// </summary>
        /// <param name="arrived">
        /// False when they ran out of patience instead of getting there — somebody standing in
        /// the doorway, or a corner taken too tightly. Then the same leg is aimed at again after
        /// the pause, rather than the job being abandoned: a worker who gives up on the way in
        /// would stand in the yard all game with nothing to say for it.
        /// </param>
        private void NextLeg(bool arrived)
        {
            if (!arrived)
            {
                _walkingFor = 0f;
                return;
            }

            if (_onLeg + 1 < _legs)
            {
                _onLeg++;
                _spot = _route[_onLeg];
                _walkingFor = 0f;
                return;
            }

            AtWork = true;
        }

        private void PickASpot()
        {
            _spot = StrollMath.SpotIn(_area, Random.value, Random.value);
            _walkingFor = 0f;
        }
    }
}
