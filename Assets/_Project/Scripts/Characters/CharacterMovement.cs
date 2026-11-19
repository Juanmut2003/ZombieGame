#nullable enable
using UnityEngine;
using ZombieGame.Core.Config;

namespace ZombieGame.Characters
{
    /// <summary>
    /// Moves a character from the input it is given, through the physics engine.
    /// </summary>
    /// <remarks>
    /// Runs in <c>FixedUpdate</c> and sets a velocity rather than moving the transform: anything
    /// that should collide with walls has to go through physics, or it walks straight through
    /// them. Input is read in <c>Update</c> by <see cref="PlayerInputReader"/> and consumed here,
    /// which is the split CLAUDE.md asks for.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class CharacterMovement : MonoBehaviour
    {
        [Tooltip("Where speeds and acceleration come from.")]
        [SerializeField]
        private BalanceConfig? _balance;

        [Tooltip("Who is steering. Usually on the same object.")]
        [SerializeField]
        private PlayerInputReader? _input;

        [Tooltip("What sprinting is paid for with. Without one, sprinting is free.")]
        [SerializeField]
        private CharacterStamina? _stamina;

        [Tooltip("Whether the character is keeping low.")]
        [SerializeField]
        private CharacterStance? _stance;

        private Rigidbody2D _body = null!;
        private MovementSettings _settings;
        private float _sprintCostPerSecond;

        /// <summary>How fast the character is actually going.</summary>
        public Vector2 Velocity => _body.linearVelocity;

        private void Awake()
        {
            // Cached here, never looked up in a loop: this runs for every character in the scene,
            // and later for hundreds of zombies.
            _body = GetComponent<Rigidbody2D>();

            if (_balance is null)
            {
                Debug.LogWarning($"[Movement] {name} has no BalanceConfig; using the defaults.");
                _balance = BalanceConfig.CreateDefault();
            }

            _settings = new MovementSettings(
                _balance.WalkSpeed, _balance.SprintMultiplier, _balance.Acceleration);
            _sprintCostPerSecond = _balance.SprintStaminaPerSecond;
        }

        private void FixedUpdate()
        {
            Vector2 input = _input is null ? Vector2.zero : _input.MoveInput;
            bool wantsToSprint = _input is not null && _input.SprintHeld && input != Vector2.zero;

            // Sprinting is only sprinting while it is being paid for. Running out mid-stride drops
            // the character back to a walk rather than refusing the step.
            bool sprinting = wantsToSprint;
            if (wantsToSprint && _stamina is not null)
            {
                sprinting = _stamina.Drain(_sprintCostPerSecond, Time.fixedDeltaTime) > 0f;
            }

            // Crouching cannot be sprinted out of: keeping low is slower, and a player who could
            // hold both would simply hold both.
            bool crouching = _stance != null && _stance.IsCrouching;
            if (crouching)
            {
                sprinting = false;
            }

            Vector2 moved = MovementMath.Advance(
                _body.linearVelocity, input, sprinting, _settings, Time.fixedDeltaTime);

            _body.linearVelocity = crouching ? moved * _stance!.SpeedFactor : moved;
        }
    }
}
