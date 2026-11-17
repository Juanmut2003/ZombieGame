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

        private Rigidbody2D _body = null!;
        private MovementSettings _settings;

        /// <summary>How fast the character is actually going.</summary>
        public Vector2 Velocity => _body.linearVelocity;

        private void Awake()
        {
            // Cached here, never looked up in a loop: this runs for every character in the scene,
            // and later for hundreds of zombies.
            _body = GetComponent<Rigidbody2D>();

            _settings = _balance is null
                ? new MovementSettings(3.5f, 1.8f, 40f)
                : new MovementSettings(
                    _balance.WalkSpeed, _balance.SprintMultiplier, _balance.Acceleration);

            if (_balance is null)
            {
                Debug.LogWarning($"[Movement] {name} has no BalanceConfig; using fallback speeds.");
            }
        }

        private void FixedUpdate()
        {
            Vector2 input = _input is null ? Vector2.zero : _input.MoveInput;
            bool sprinting = _input is not null && _input.SprintHeld;

            _body.linearVelocity = MovementMath.Advance(
                _body.linearVelocity, input, sprinting, _settings, Time.fixedDeltaTime);
        }
    }
}
