#nullable enable
using UnityEngine;
using ZombieGame.Core.Config;
using ZombieGame.Core.Rendering;

namespace ZombieGame.Characters
{
    /// <summary>
    /// Makes a character walk rather than glide: a small rise and rock, on the beat of its steps.
    /// </summary>
    /// <remarks>
    /// The art is one frame per direction, so there is no walk cycle to play. What there is, is
    /// a footstep timer that the noise system and the audio already run off, and a body that can
    /// be moved a couple of pixels. Driving the lift from that same timer puts the dip of the
    /// body on the frame the boot is heard, at a walk, a sprint and a crouch alike — which is
    /// most of what reads as walking.
    /// It moves the <i>sprite</i>, never the character. The collider is a small patch at the
    /// figure's feet, and bobbing the object that owns it would have the character hopping in and
    /// out of walls. So the sprite lives on a child, and this touches nothing else.
    /// The clock is reset whenever the character stops, so setting off always begins on a
    /// footfall — the same reason <see cref="FootstepNoise"/> zeroes its own timer — and a
    /// character pinned against a wall stands still instead of marching on the spot.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterMovement))]
    public sealed class CharacterGait : MonoBehaviour
    {
        [Tooltip("Where the step timing and the size of the movement come from.")]
        [SerializeField]
        private BalanceConfig? _balance;

        [Tooltip("Who is steering, for telling a sprint from a walk.")]
        [SerializeField]
        private PlayerInputReader? _input;

        [Tooltip("Whether the character is keeping low.")]
        [SerializeField]
        private CharacterStance? _stance;

        [Tooltip("The sprite to move. Must not be the object the collider is on.")]
        [SerializeField]
        private Transform? _sprite;

        private CharacterMovement _movement = null!;
        private Vector3 _rest;
        private float _walking;

        /// <summary>Where the character is in its stride, 0 to 1. Zero while standing still.</summary>
        public float Phase { get; private set; }

        /// <summary>How far the sprite is lifted right now, in world units.</summary>
        public float Lift { get; private set; }

        private void Awake()
        {
            _movement = GetComponent<CharacterMovement>();

            if (_balance == null)
            {
                Debug.LogWarning($"[Gait] {name} has no BalanceConfig; using the defaults.");
                _balance = BalanceConfig.CreateDefault();
            }

            if (_sprite == null)
            {
                SpriteRenderer? found = GetComponentInChildren<SpriteRenderer>();
                _sprite = found == null ? null : found.transform;
            }

            // Compared with Unity's own operator, not `is null`: an unassigned serialized
            // reference is a live wrapper around nothing rather than a C# null.
            if (_sprite == null || _sprite == transform)
            {
                // Moving the character's own transform would drag the collider with it, so there
                // is nothing safe to do here. Said out loud rather than silently skipped: a scene
                // built without the sprite child looks fine and simply never animates.
                Debug.LogWarning(
                    $"[Gait] {name} has no sprite child to move; the figure will not walk.");
                _sprite = null;
                enabled = false;
                return;
            }

            _rest = _sprite.localPosition;
        }

        private void Update()
        {
            if (_sprite == null)
            {
                return;
            }

            // Actually moving, not merely leaning on a wall — the same test the footsteps make,
            // and for the same reason.
            if (_movement.Velocity.sqrMagnitude <= 0.05f)
            {
                Settle();
                return;
            }

            _walking += Time.deltaTime;
            Phase = StepCycle.FromTime(_walking, SecondsPerStep());

            BalanceConfig balance = _balance!;
            Lift = StepCycle.Bob(Phase, balance.GaitBob);

            _sprite.localPosition = new Vector3(_rest.x, _rest.y + Lift, _rest.z);
            _sprite.localRotation = Quaternion.Euler(
                0f, 0f, StepCycle.Lean(Phase, balance.GaitLean));
        }

        /// <summary>
        /// Seconds between this character's footfalls, by what it is doing.
        /// </summary>
        /// <remarks>
        /// Deliberately the same ladder <see cref="FootstepNoise"/> walks down, crouching ahead
        /// of sprinting: you cannot be both, and the quiet one is the one the player chose.
        /// </remarks>
        private float SecondsPerStep()
        {
            BalanceConfig balance = _balance!;

            if (_stance != null && _stance.IsCrouching)
            {
                return balance.CrouchStepSeconds;
            }

            bool sprinting = _input is not null
                             && _input.SprintHeld
                             && _movement.Velocity.magnitude > balance.WalkSpeed * 1.05f;

            return sprinting ? balance.SprintStepSeconds : balance.WalkStepSeconds;
        }

        private void Settle()
        {
            _walking = 0f;
            Phase = 0f;
            Lift = 0f;

            if (_sprite!.localPosition != _rest)
            {
                _sprite.localPosition = _rest;
            }

            if (_sprite.localRotation != Quaternion.identity)
            {
                _sprite.localRotation = Quaternion.identity;
            }
        }
    }
}
