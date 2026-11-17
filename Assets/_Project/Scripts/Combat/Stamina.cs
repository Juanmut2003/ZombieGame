#nullable enable
using System;

namespace ZombieGame.Combat
{
    /// <summary>The numbers behind one character's stamina.</summary>
    public readonly struct StaminaSettings
    {
        /// <param name="maximum">Full stamina.</param>
        /// <param name="regenPerSecond">How fast it comes back once it is allowed to.</param>
        /// <param name="regenDelaySeconds">
        /// How long after spending before it starts coming back. Without a pause, sprinting in
        /// taps would be strictly better than sprinting, which is a mechanic nobody intended.
        /// </param>
        public StaminaSettings(
            float maximum = 100f, float regenPerSecond = 18f, float regenDelaySeconds = 0.8f)
        {
            if (maximum <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "Must be positive.");
            }

            if (regenPerSecond < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(regenPerSecond), regenPerSecond, "Cannot be negative.");
            }

            if (regenDelaySeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(regenDelaySeconds), regenDelaySeconds, "Cannot be negative.");
            }

            Maximum = maximum;
            RegenPerSecond = regenPerSecond;
            RegenDelaySeconds = regenDelaySeconds;
        }

        public float Maximum { get; }

        public float RegenPerSecond { get; }

        public float RegenDelaySeconds { get; }
    }

    /// <summary>
    /// What sprinting, swinging and dodging all draw on.
    /// </summary>
    /// <remarks>
    /// Stamina is what stops combat being a question of holding a button: PROJECT.md 9 puts sprint,
    /// melee swings and dodges on the same pool on purpose, so running from a fight costs what
    /// fighting would have cost.
    /// Plain C#, so the feel of it is settled in tests rather than in the inspector.
    /// </remarks>
    public sealed class Stamina
    {
        private readonly StaminaSettings _settings;
        private float _secondsUntilRegen;

        public Stamina(StaminaSettings settings = default)
        {
            // A default-constructed settings has a maximum of zero, which is nobody having said
            // anything rather than someone asking for a character who cannot move.
            _settings = settings.Maximum > 0f ? settings : new StaminaSettings();
            Current = _settings.Maximum;
        }

        /// <summary>Stamina left.</summary>
        public float Current { get; private set; }

        /// <summary>Full stamina.</summary>
        public float Maximum => _settings.Maximum;

        /// <summary>How full it is, for the bar on the HUD.</summary>
        public float Fraction => Current / _settings.Maximum;

        /// <summary>True while it is still coming back.</summary>
        public bool IsRecovering => _secondsUntilRegen <= 0f && Current < _settings.Maximum;

        /// <summary>True when there is enough left to pay <paramref name="cost"/>.</summary>
        public bool CanSpend(float cost) => cost <= 0f || Current >= cost;

        /// <summary>
        /// Spends stamina if there is enough, and restarts the pause before it regenerates.
        /// </summary>
        /// <returns>False when there was not enough, in which case nothing was spent.</returns>
        public bool TrySpend(float cost)
        {
            if (cost < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(cost), cost, "Cannot be negative.");
            }

            if (!CanSpend(cost))
            {
                return false;
            }

            Current -= cost;
            _secondsUntilRegen = _settings.RegenDelaySeconds;
            return true;
        }

        /// <summary>
        /// Spends as much as is left, even when that is less than asked for.
        /// </summary>
        /// <remarks>
        /// This is what a continuous drain like sprinting wants: running out mid-stride should
        /// stop the sprint, not refuse to charge for the half second already spent running.
        /// </remarks>
        /// <returns>How much was actually spent.</returns>
        public float Drain(float amount)
        {
            if (amount < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Cannot be negative.");
            }

            float spent = Math.Min(amount, Current);
            if (spent <= 0f)
            {
                return 0f;
            }

            Current -= spent;
            _secondsUntilRegen = _settings.RegenDelaySeconds;
            return spent;
        }

        /// <summary>Lets time pass: counts down the pause, then regenerates.</summary>
        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds <= 0f)
            {
                return;
            }

            if (_secondsUntilRegen > 0f)
            {
                _secondsUntilRegen -= deltaSeconds;
                return;
            }

            if (Current >= _settings.Maximum)
            {
                return;
            }

            Current = Math.Min(_settings.Maximum, Current + (_settings.RegenPerSecond * deltaSeconds));
        }

        /// <summary>Back to full, with no pause. For a fresh run or a rested night.</summary>
        public void ResetToFull()
        {
            Current = _settings.Maximum;
            _secondsUntilRegen = 0f;
        }
    }
}
