#nullable enable
using UnityEngine;
using ZombieGame.Combat;
using ZombieGame.Core;
using ZombieGame.Core.Config;
using ZombieGame.Core.Events;

namespace ZombieGame.Characters
{
    /// <summary>
    /// Holds one character's stamina and lets it recover.
    /// </summary>
    /// <remarks>
    /// Sprinting and swinging both draw on this, which is what makes running away from a fight
    /// cost the same pool that fighting would have.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class CharacterStamina : MonoBehaviour
    {
        [Tooltip("Where the stamina numbers come from.")]
        [SerializeField]
        private BalanceConfig? _balance;

        private Stamina _stamina = null!;
        private EventBus? _bus;
        private int _ownerId;
        private bool _isPlayer;
        private float _lastAnnounced = float.NaN;

        /// <summary>Stamina left.</summary>
        public float Current => _stamina.Current;

        /// <summary>How full it is, for the bar on the HUD.</summary>
        public float Fraction => _stamina.Fraction;

        /// <summary>Who this stamina belongs to, as the HUD and later systems see it.</summary>
        public int OwnerId => _ownerId;

        private void Awake()
        {
            // Assembled here rather than in BalanceConfig: Core cannot reference Combat, because
            // Combat already references Core, and this is the layer that knows both.
            _stamina = new Stamina(
                _balance is null
                    ? new StaminaSettings()
                    : new StaminaSettings(
                        _balance.StaminaMax,
                        _balance.StaminaRegenPerSecond,
                        _balance.StaminaRegenDelay));

            _ownerId = ActorIds.Next();

            // The character the player steers is the one with an input reader on it. Deriving it
            // beats a "this is the hero" checkbox that can be ticked on two objects at once.
            _isPlayer = GetComponent<PlayerInputReader>() is not null;

            GameServices.TryGet(out _bus);
        }

        private void Update()
        {
            _stamina.Tick(Time.deltaTime);
            Announce();
        }

        /// <summary>
        /// Tells the bus when the number moved, and once at the start so a bar has something to
        /// draw before the player does anything.
        /// </summary>
        /// <remarks>
        /// Only on a change: during a full bar this would otherwise publish sixty identical events
        /// a second for every survivor in the zone.
        /// </remarks>
        private void Announce()
        {
            // Looked up again when it was missing: the bootstrap that registers the bus lives in
            // the same scene, and Awake order between two scene objects is not something to rely
            // on. By the first Update it is certainly up.
            if (_bus is null)
            {
                GameServices.TryGet(out _bus);
            }

            if (_bus is null)
            {
                return;
            }

            // NaN compares false against everything, including itself, so the first call always
            // publishes without needing a separate flag.
            if (Mathf.Approximately(_stamina.Current, _lastAnnounced))
            {
                return;
            }

            _lastAnnounced = _stamina.Current;
            _bus.Publish(
                new StaminaChangedEvent(_ownerId, _stamina.Current, _stamina.Maximum, _isPlayer));
        }

        /// <summary>True when a swing or a dodge could be paid for right now.</summary>
        public bool CanSpend(float cost) => _stamina.CanSpend(cost);

        /// <summary>Pays for a one-off action. Returns false when there is not enough.</summary>
        public bool TrySpend(float cost) => _stamina.TrySpend(cost);

        /// <summary>
        /// Pays for a continuous drain like sprinting, taking whatever is left if that is less
        /// than asked for.
        /// </summary>
        /// <returns>How much was actually spent.</returns>
        public float Drain(float perSecond, float deltaSeconds) =>
            _stamina.Drain(perSecond * deltaSeconds);

        /// <summary>Back to full. For a fresh run or a night's rest.</summary>
        public void ResetToFull() => _stamina.ResetToFull();
    }
}
