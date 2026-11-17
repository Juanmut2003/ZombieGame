#nullable enable
using UnityEngine;
using ZombieGame.Combat;
using ZombieGame.Core.Config;

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

        /// <summary>Stamina left.</summary>
        public float Current => _stamina.Current;

        /// <summary>How full it is, for the bar on the HUD.</summary>
        public float Fraction => _stamina.Fraction;

        private void Awake() =>
            // Assembled here rather than in BalanceConfig: Core cannot reference Combat, because
            // Combat already references Core, and this is the layer that knows both.
            _stamina = new Stamina(
                _balance is null
                    ? new StaminaSettings()
                    : new StaminaSettings(
                        _balance.StaminaMax,
                        _balance.StaminaRegenPerSecond,
                        _balance.StaminaRegenDelay));

        private void Update() => _stamina.Tick(Time.deltaTime);

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
