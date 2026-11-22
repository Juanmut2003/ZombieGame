#nullable enable
using UnityEngine;
using ZombieGame.Core;
using ZombieGame.Core.Events;
using ZombieGame.Core.SceneFlow;

namespace ZombieGame.Missions
{
    /// <summary>
    /// Takes the squad home when the run is over.
    /// </summary>
    /// <remarks>
    /// Without this a finished run leaves the player standing in a zone that no longer wants
    /// anything from them, reading a line that says they are done — which is how it looked the
    /// first time anyone played it to the end.
    /// Who decides when to leave depends on what the zone has in it. A zone with a debrief
    /// screen hands the decision to the player: the screen says it is up, the countdown is
    /// called off, and nothing happens until the player presses the button. A zone without one
    /// — a sandbox, a test scene — keeps the old pause, which is short enough to be a beat and
    /// long enough to read a verdict.
    /// Going back to the menu is the stand-in for the debrief of PROJECT.md §8 landing on the
    /// world map, which does not exist yet; what the player reads before leaving is already the
    /// real thing.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class MissionEndFlow : MonoBehaviour
    {
        [Tooltip("How long the result stays up in a zone that has no debrief screen.")]
        [SerializeField]
        [Min(0f)]
        private float _secondsOnResult = 4f;

        [Tooltip("Where to go afterwards.")]
        [SerializeField]
        private string _nextScene = GameScenes.MainMenu;

        private EventBus? _bus;
        private SceneLoader? _loader;
        private float _remaining = -1f;
        private bool _over;
        private bool _debriefUp;

        /// <summary>True once the run is over and the way home is counting down.</summary>
        public bool IsLeaving => _remaining >= 0f;

        /// <summary>True while the zone is waiting for the player to finish reading.</summary>
        public bool IsWaitingOnTheDebrief => _over && _remaining < 0f;

        private void OnEnable() => Listen();

        private void OnDisable()
        {
            if (_bus is not null)
            {
                _bus.Unsubscribe<MissionEndedEvent>(OnMissionEnded);
                _bus.Unsubscribe<DebriefOpenedEvent>(OnDebriefOpened);
                _bus.Unsubscribe<DebriefClosedEvent>(OnDebriefClosed);
            }

            _bus = null;
        }

        private void Update()
        {
            Listen();

            if (_remaining < 0f)
            {
                return;
            }

            _remaining -= Time.deltaTime;
            if (_remaining > 0f)
            {
                return;
            }

            _remaining = -1f;
            Leave();
        }

        private void Listen()
        {
            if (_bus is not null || !GameServices.TryGet(out _bus) || _bus is null)
            {
                return;
            }

            _bus.Subscribe<MissionEndedEvent>(OnMissionEnded);
            _bus.Subscribe<DebriefOpenedEvent>(OnDebriefOpened);
            _bus.Subscribe<DebriefClosedEvent>(OnDebriefClosed);
        }

        private void OnMissionEnded(MissionEndedEvent ended)
        {
            _over = true;

            // Only when nothing is going to be put in front of the player. The debrief answers
            // the same event this does, and the bus hands it round in whatever order the two
            // subscribed — so this has to cope with the screen already being up as well as with
            // it arriving a moment later.
            _remaining = _debriefUp ? -1f : _secondsOnResult;
        }

        /// <summary>Calls off the countdown: the player has something to read.</summary>
        private void OnDebriefOpened(DebriefOpenedEvent opened)
        {
            _debriefUp = true;
            _remaining = -1f;
        }

        private void OnDebriefClosed(DebriefClosedEvent closed)
        {
            _debriefUp = false;

            // Only after a run has actually ended. A debrief shown from a menu, or closed twice,
            // is not a reason to unload the zone.
            if (!_over)
            {
                return;
            }

            _over = false;
            _remaining = -1f;
            Leave();
        }

        private void Leave()
        {
            if (_loader is null)
            {
                GameServices.TryGet(out _loader);
            }

            if (_loader is null)
            {
                Debug.LogWarning("[Mission] no scene loader; the zone has nowhere to go.");
                return;
            }

            _loader.Load(_nextScene);
        }
    }
}
