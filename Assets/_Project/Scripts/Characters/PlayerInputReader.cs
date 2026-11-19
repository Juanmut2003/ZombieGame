#nullable enable
using UnityEngine;
using UnityEngine.EventSystems;
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
        private InputAction? _attack;
        private InputAction? _reload;
        private InputAction? _aim;
        private InputAction? _aimStick;
        private InputAction? _cycle;
        private InputAction? _interact;
        private InputAction? _crouch;
        private InputAction? _takedown;
        private readonly InputAction?[] _slots = new InputAction?[3];
        private bool _overridden;
        private bool _aimOverridden;

        /// <summary>Where the player wants to go, already in the −1..1 range per axis.</summary>
        public Vector2 MoveInput { get; private set; }

        /// <summary>Whether the sprint input is held.</summary>
        public bool SprintHeld { get; private set; }

        /// <summary>True on the frame the attack input was pressed. Swinging is not a hold.</summary>
        public bool AttackPressed { get; private set; }

        /// <summary>
        /// Whether the attack input is held. A swing wants the press, a trigger wants the hold,
        /// and both come off the same button.
        /// </summary>
        public bool AttackHeld { get; private set; }

        /// <summary>Where the mouse is, in screen pixels.</summary>
        public Vector2 AimScreen { get; private set; }

        /// <summary>Where the right stick is pushed. Zero when it is not being used.</summary>
        public Vector2 AimStick { get; private set; }

        /// <summary>
        /// The hotbar slot pressed this frame, counting from one. Zero means none.
        /// </summary>
        public int SlotPressed { get; private set; }

        /// <summary>Which way the wheel turned this frame. Zero means it did not.</summary>
        public float WeaponScroll { get; private set; }

        /// <summary>Whether the interact input is held. Searching is a hold, not a press.</summary>
        public bool InteractHeld { get; private set; }

        /// <summary>Whether the crouch input is held. Keeping low is a hold, never a toggle.</summary>
        public bool CrouchHeld { get; private set; }

        /// <summary>True on the frame a silent takedown was asked for.</summary>
        public bool TakedownPressed { get; private set; }

        /// <summary>True on the frame the reload input was pressed.</summary>
        public bool ReloadPressed { get; private set; }

        /// <summary>
        /// Takes the controls away from the devices and drives the character from code. Used by
        /// tests, cutscenes and replays.
        /// </summary>
        /// <remarks>
        /// The override holds until <see cref="ClearInputOverride"/>; without that, the next
        /// <c>Update</c> would read the idle devices and wipe the values straight away.
        /// </remarks>
        public void SetInputOverride(
            Vector2 move,
            bool sprint,
            bool attack = false,
            bool reload = false,
            bool interact = false)
        {
            _overridden = true;
            MoveInput = move;
            SprintHeld = sprint;
            AttackPressed = attack;
            AttackHeld = attack;
            ReloadPressed = reload;
            InteractHeld = interact;
        }

        /// <summary>
        /// Says where the player is pointing, in screen pixels, without taking over the rest of
        /// the controls.
        /// </summary>
        /// <remarks>
        /// Aiming is the one input a test cannot fake by moving a character about: it is a point
        /// on the screen, and the only honest way to check that a shot goes where the mouse is
        /// pointing is to put the mouse somewhere and look at the round.
        /// </remarks>
        public void SetAimOverride(Vector2 screenPosition)
        {
            _aimOverridden = true;
            AimScreen = screenPosition;
            AimStick = Vector2.zero;
        }

        /// <summary>Hands aiming back to the devices.</summary>
        public void ClearAimOverride() => _aimOverridden = false;

        /// <summary>Hands control back to the player's devices.</summary>
        public void ClearInputOverride() => _overridden = false;

        /// <summary>
        /// Whether the pointer is sitting on something clickable in the HUD.
        /// </summary>
        /// <remarks>
        /// False when there is no event system, which is every scene that has nothing to click:
        /// treating "no UI at all" as "the pointer is over the UI" would silently disable
        /// attacking everywhere.
        /// </remarks>
        private static bool PointerIsOverTheHud() =>
            EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

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
            _attack = map.FindAction("Attack", throwIfNotFound: false);
            _reload = map.FindAction("Reload", throwIfNotFound: false);
            _aim = map.FindAction("Aim", throwIfNotFound: false);
            _aimStick = map.FindAction("AimStick", throwIfNotFound: false);
            _cycle = map.FindAction("CycleWeapon", throwIfNotFound: false);
            _interact = map.FindAction("Interact", throwIfNotFound: false);
            _crouch = map.FindAction("Crouch", throwIfNotFound: false);
            _takedown = map.FindAction("Takedown", throwIfNotFound: false);

            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i] = map.FindAction($"Slot{i + 1}", throwIfNotFound: false);
            }
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
            AttackHeld = false;
            InteractHeld = false;
            CrouchHeld = false;
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

            // A click that lands on the HUD belongs to the HUD. Without this, opening the map
            // also swings the bat — and a swing is a noise, so the button that shows you where
            // the zombies are would be the thing that fetched them.
            bool onHud = PointerIsOverTheHud();

            if (_attack is not null)
            {
                AttackPressed = !onHud && _attack.WasPressedThisFrame();
                AttackHeld = !onHud && _attack.IsPressed();
            }

            if (_aim is not null && !_aimOverridden)
            {
                AimScreen = _aim.ReadValue<Vector2>();
            }

            if (_aimStick is not null && !_aimOverridden)
            {
                AimStick = _aimStick.ReadValue<Vector2>();
            }

            if (_cycle is not null)
            {
                WeaponScroll = _cycle.ReadValue<float>();
            }

            if (_interact is not null)
            {
                InteractHeld = _interact.IsPressed();
            }

            if (_crouch is not null)
            {
                CrouchHeld = _crouch.IsPressed();
            }

            if (_takedown is not null)
            {
                TakedownPressed = !onHud && _takedown.WasPressedThisFrame();
            }

            SlotPressed = 0;
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i]?.WasPressedThisFrame() == true)
                {
                    SlotPressed = i + 1;
                    break;
                }
            }

            if (_reload is not null)
            {
                ReloadPressed = _reload.WasPressedThisFrame();
            }
        }
    }
}
