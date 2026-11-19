#nullable enable
using UnityEngine;
using ZombieGame.Combat;
using ZombieGame.Core;
using ZombieGame.Core.Config;
using ZombieGame.Core.Events;
using ZombieGame.Missions;
using ZombieGame.Waves;

namespace ZombieGame.Characters
{
    /// <summary>
    /// Kills an unaware zombie from behind, quietly.
    /// </summary>
    /// <remarks>
    /// The other half of what PROJECT.md §9 means by stealth being first class: crouching lets
    /// you get close, and this is what being close is <i>for</i>. Without it, sneaking only ever
    /// postpones a fight.
    /// Deliberately not free. It costs stamina, it makes a noise — far less than a swing, but not
    /// nothing — and it refuses anything that has already noticed you, which is what stops it
    /// being the answer to being surrounded.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class PlayerTakedown : MonoBehaviour
    {
        [Tooltip("Where the reach, the angle and the costs come from.")]
        [SerializeField]
        private BalanceConfig? _balance;

        [Tooltip("Who is steering.")]
        [SerializeField]
        private PlayerInputReader? _input;

        [Tooltip("What it is paid for with.")]
        [SerializeField]
        private CharacterStamina? _stamina;

        private EventBus? _bus;
        private int _actorId;
        private bool _offered;

        /// <summary>How many have been taken down this run. For tests and, later, a debrief.</summary>
        public int Count { get; private set; }

        private void Awake()
        {
            if (_balance is null)
            {
                Debug.LogWarning($"[Takedown] {name} has no BalanceConfig; using the defaults.");
                _balance = BalanceConfig.CreateDefault();
            }

            _actorId = _stamina is not null ? _stamina.OwnerId : ActorIds.Next();
        }

        private void Start() => GameServices.TryGet(out _bus);

        private void Update()
        {
            Offer();

            if (_input is null || !_input.TakedownPressed)
            {
                return;
            }

            TryTakedown();
        }

        /// <summary>
        /// Tells the HUD when the move is on, and stops telling it when it is not.
        /// </summary>
        /// <remarks>
        /// Three conditions have to hold at once and none of them is visible on screen. Without a
        /// prompt the player presses the key, nothing happens, and there is no way to tell
        /// whether they were too far, not far enough round the back, or had already been seen —
        /// which reads as a broken key rather than as a rule.
        /// Published only on a change, so it does not put sixty identical lines a second on the
        /// bus, and it leaves the looting prompt alone by saying nothing when there is nothing
        /// to say.
        /// </remarks>
        private void Offer()
        {
            bool available = _bus is not null
                             && GameServices.TryGet(out ISilentKill? horde)
                             && horde is not null
                             && CanAfford()
                             && horde.CanTakedown(
                                 _balance!.TakedownReach,
                                 _balance.TakedownBehindDegrees,
                                 At());

            if (available == _offered)
            {
                return;
            }

            _offered = available;
            if (available)
            {
                _bus?.Publish(new InteractionPromptEvent("F  TAKEDOWN", 0f));
            }
            else
            {
                _bus?.Publish(new InteractionPromptEvent(string.Empty, 0f));
            }
        }

        private bool CanAfford() =>
            _stamina is null || _stamina.CanSpend(_balance!.TakedownStamina);

        private Vector2 At()
        {
            Vector3 at = transform.position;
            return new Vector2(at.x, at.y);
        }

        /// <summary>Attempts a takedown from where the character is standing.</summary>
        /// <returns>False when there was nobody to creep up on, or nothing left to pay with.</returns>
        public bool TryTakedown()
        {
            if (!GameServices.TryGet(out ISilentKill? horde) || horde is null)
            {
                return false;
            }

            BalanceConfig balance = _balance!;

            // Checked before the kill, not after: finding out you could not afford it once the
            // zombie is already down would be the worst possible order.
            if (_stamina is not null && !_stamina.CanSpend(balance.TakedownStamina))
            {
                return false;
            }

            if (!horde.TryTakedown(
                    balance.TakedownReach,
                    balance.TakedownBehindDegrees,
                    At(),
                    out Vector2 where))
            {
                return false;
            }

            _stamina?.TrySpend(balance.TakedownStamina);
            Count++;

            _bus?.Publish(new NoiseEvent(where, balance.TakedownNoise, _actorId));
            return true;
        }
    }
}
