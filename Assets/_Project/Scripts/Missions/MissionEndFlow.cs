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
    /// The pause before leaving is deliberate: the result has to be readable before the screen
    /// changes, and an instant cut makes a win feel like a crash.
    /// Going back to the menu is the stand-in for the debrief of PROJECT.md §8, which belongs on
    /// the world map that does not exist yet.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class MissionEndFlow : MonoBehaviour
    {
        [Tooltip("How long the result stays on screen before the zone is left.")]
        [SerializeField]
        [Min(0f)]
        private float _secondsOnResult = 4f;

        [Tooltip("Where to go afterwards.")]
        [SerializeField]
        private string _nextScene = GameScenes.MainMenu;

        private EventBus? _bus;
        private SceneLoader? _loader;
        private float _remaining = -1f;

        /// <summary>True once the run is over and the way home is counting down.</summary>
        public bool IsLeaving => _remaining >= 0f;

        private void OnEnable() => Listen();

        private void OnDisable()
        {
            _bus?.Unsubscribe<MissionEndedEvent>(OnMissionEnded);
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
        }

        private void OnMissionEnded(MissionEndedEvent ended) => _remaining = _secondsOnResult;

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
