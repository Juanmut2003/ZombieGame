#nullable enable
using UnityEngine;
using ZombieGame.Core;
using ZombieGame.Core.Config;
using ZombieGame.Core.Events;
using ZombieGame.Waves;

namespace ZombieGame.Characters
{
    /// <summary>
    /// Turns walking into sound: a step every so often, louder when sprinting.
    /// </summary>
    /// <remarks>
    /// This is what makes sprinting a decision rather than a free upgrade. Running is faster and
    /// costs stamina, which the player already feels; what it also does is drop a loud noise every
    /// third of a second along the whole route, and a route like that stays hostile for a while
    /// afterwards. PROJECT.md §1: noise is the enemy.
    /// Steps rather than a continuous hum, because a step is a thing a listener can be at the
    /// wrong distance from — and because a meter that climbs smoothly while you hold a key reads
    /// as a cost, where a sound reads as a mistake.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterMovement))]
    public sealed class FootstepNoise : MonoBehaviour
    {
        [Tooltip("Where the step loudness and timing come from.")]
        [SerializeField]
        private BalanceConfig? _balance;

        [Tooltip("Who is steering, for telling a sprint from a walk.")]
        [SerializeField]
        private PlayerInputReader? _input;

        [Tooltip("Whether the character is keeping low.")]
        [SerializeField]
        private CharacterStance? _stance;

        private CharacterMovement _movement = null!;
        private EventBus? _bus;
        private int _actorId;
        private float _untilNextStep;

        private void Awake()
        {
            _movement = GetComponent<CharacterMovement>();
            _actorId = ActorIds.Next();

            if (_balance is null)
            {
                Debug.LogWarning($"[Noise] {name} has no BalanceConfig; using the defaults.");
                _balance = BalanceConfig.CreateDefault();
            }
        }

        private void Start() => GameServices.TryGet(out _bus);

        private void Update()
        {
            // Actually moving, not merely holding a direction into a wall: a character pinned
            // against a corner is standing still, however hard the player pushes.
            bool moving = _movement.Velocity.sqrMagnitude > 0.05f;
            if (!moving)
            {
                // The next step comes immediately on setting off, so the first footfall of a
                // sprint is heard rather than swallowed by whatever was left of the timer.
                _untilNextStep = 0f;
                return;
            }

            _untilNextStep -= Time.deltaTime;
            if (_untilNextStep > 0f)
            {
                return;
            }

            BalanceConfig balance = _balance!;
            bool sprinting = _input is not null
                             && _input.SprintHeld
                             && _movement.Velocity.magnitude > balance.WalkSpeed * 1.05f;

            // Crouching beats sprinting here as well as in the movement: you cannot be both, and
            // the quiet one is the one the player chose deliberately.
            bool crouching = _stance != null && _stance.IsCrouching;

            _untilNextStep = crouching
                ? balance.CrouchStepSeconds
                : sprinting ? balance.SprintStepSeconds : balance.WalkStepSeconds;

            _bus?.Publish(new NoiseEvent(
                transform.position,
                crouching
                    ? balance.CrouchNoise
                    : sprinting ? balance.SprintNoise : balance.WalkNoise,
                _actorId));
        }
    }
}
