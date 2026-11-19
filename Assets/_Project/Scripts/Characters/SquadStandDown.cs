#nullable enable
using UnityEngine;
using ZombieGame.Core;
using ZombieGame.Core.Events;
using ZombieGame.Missions;

namespace ZombieGame.Characters
{
    /// <summary>
    /// Takes the controls away the moment a run ends.
    /// </summary>
    /// <remarks>
    /// A finished run used to leave the player walking about for the few seconds before the menu
    /// loaded — shooting, looting and swinging at a zone that had already stopped counting. Worse
    /// after a death, where it read as the game not having noticed.
    /// Done by handing the input reader an override of nothing rather than by switching the
    /// component off: the character keeps its physics, keeps being drawn, and simply stops
    /// obeying. Switching it off mid-frame would leave whatever was held stuck on.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class SquadStandDown : MonoBehaviour
    {
        [Tooltip("The controls to take away.")]
        [SerializeField]
        private PlayerInputReader? _input;

        private EventBus? _bus;

        /// <summary>True once the squad has stopped taking orders.</summary>
        public bool HasStoodDown { get; private set; }

        private void Start()
        {
            GameServices.TryGet(out _bus);
            _bus?.Subscribe<MissionEndedEvent>(OnEnded);
        }

        private void OnDestroy() => _bus?.Unsubscribe<MissionEndedEvent>(OnEnded);

        /// <summary>Stops the character responding to anything. Public for tests.</summary>
        public void StandDown()
        {
            HasStoodDown = true;
            _input?.SetInputOverride(Vector2.zero, sprint: false);
        }

        private void OnEnded(MissionEndedEvent ended) => StandDown();
    }
}
