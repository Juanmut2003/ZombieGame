#nullable enable
using UnityEngine;
using UnityEngine.InputSystem;

namespace ZombieGame.Characters
{
    /// <summary>
    /// Reads the player's input once per frame and hands it on as plain values.
    /// </summary>
    /// <remarks>
    /// Everything that reacts to input asks this, rather than touching the Input System itself.
    /// That keeps the rest of the game free of input plumbing, makes rebinding a question of the
    /// actions asset alone, and means a test or a replay can drive a character by setting these
    /// values directly.
    /// Legacy <c>UnityEngine.Input</c> is not used anywhere in the project; the actions asset is
    /// the only source.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [Tooltip("The generated ZombieGame.inputactions asset.")]
        [SerializeField]
        private InputActionAsset? _actions;

        [Tooltip("Action map to enable while this reader is active.")]
        [SerializeField]
        private string _mapName = "Gameplay";

        private InputAction? _move;
        private InputAction? _sprint;
        private bool _overridden;

        /// <summary>Where the player wants to go, already in the −1..1 range per axis.</summary>
        public Vector2 MoveInput { get; private set; }

        /// <summary>Whether the sprint input is held.</summary>
        public bool SprintHeld { get; private set; }

        /// <summary>
        /// Takes the controls away from the devices and drives the character from code. Used by
        /// tests, cutscenes and replays.
        /// </summary>
        /// <remarks>
        /// The override holds until <see cref="ClearInputOverride"/>; without that, the next
        /// <c>Update</c> would read the idle devices and wipe the values straight away.
        /// </remarks>
        public void SetInputOverride(Vector2 move, bool sprint)
        {
            _overridden = true;
            MoveInput = move;
            SprintHeld = sprint;
        }

        /// <summary>Hands control back to the player's devices.</summary>
        public void ClearInputOverride() => _overridden = false;

        private void Awake()
        {
            if (_actions is null)
            {
                Debug.LogWarning($"[Input] {name} has no actions asset; it will read as idle.");
                return;
            }

            InputActionMap? map = _actions.FindActionMap(_mapName, throwIfNotFound: false);
            if (map is null)
            {
                Debug.LogWarning($"[Input] no action map '{_mapName}' in the asset.");
                return;
            }

            _move = map.FindAction("Move", throwIfNotFound: false);
            _sprint = map.FindAction("Sprint", throwIfNotFound: false);
        }

        private void OnEnable()
        {
            _move?.actionMap?.Enable();
        }

        private void OnDisable()
        {
            _move?.actionMap?.Disable();
            MoveInput = Vector2.zero;
            SprintHeld = false;
        }

        private void Update()
        {
            if (_overridden)
            {
                return;
            }

            // Polled rather than event-driven: movement needs the current state every frame
            // anyway, and polling keeps the order of input and movement obvious.
            if (_move is not null)
            {
                MoveInput = _move.ReadValue<Vector2>();
            }

            if (_sprint is not null)
            {
                SprintHeld = _sprint.IsPressed();
            }
        }
    }
}
