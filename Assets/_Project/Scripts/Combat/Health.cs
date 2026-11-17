#nullable enable
using System;
using ZombieGame.Core.Events;

namespace ZombieGame.Combat
{
    /// <summary>
    /// The health of one thing that can be hurt — the MC, a survivor, a zombie, a wall section.
    /// </summary>
    /// <remarks>
    /// Plain C# on purpose. Every rule about dying lives here and is covered by tests, rather than
    /// being spread across whatever happened to call <c>TakeDamage</c>. The one rule everything
    /// else depends on: <b>death happens once</b>. A corpse hit by three more bullets in the same
    /// frame must not drop three sets of loot or be returned to the pool three times.
    /// </remarks>
    public sealed class Health
    {
        private readonly EventBus? _bus;

        /// <param name="maximum">Starting and maximum health. Must be greater than zero.</param>
        /// <param name="ownerId">
        /// Identifies the owner in events, usually an instance id. Zero is allowed for things
        /// nobody needs to address.
        /// </param>
        /// <param name="resistances">Armour and weak points. Defaults to taking everything in full.</param>
        /// <param name="bus">Optional; when given, damage and death are published on it.</param>
        public Health(
            float maximum,
            int ownerId = 0,
            DamageResistances resistances = default,
            EventBus? bus = null)
        {
            if (maximum <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximum), maximum, "Something that starts dead is not worth modelling.");
            }

            Maximum = maximum;
            Current = maximum;
            OwnerId = ownerId;
            Resistances = resistances;
            _bus = bus;
        }

        /// <summary>Full health.</summary>
        public float Maximum { get; }

        /// <summary>Health right now, between zero and <see cref="Maximum"/>.</summary>
        public float Current { get; private set; }

        /// <summary>Who this belongs to, as reported in events.</summary>
        public int OwnerId { get; }

        /// <summary>Armour and weak points.</summary>
        public DamageResistances Resistances { get; }

        /// <summary>True while there is health left.</summary>
        public bool IsAlive => Current > 0f;

        /// <summary>Health as a fraction of maximum, for bars and AI thresholds.</summary>
        public float Fraction => Current / Maximum;

        /// <summary>Raised when damage landed and the owner survived.</summary>
        public event Action<DamageTakenEvent>? DamageTaken;

        /// <summary>Raised exactly once, when the owner dies.</summary>
        public event Action<DiedEvent>? Died;

        /// <summary>
        /// Applies a blow after resistances.
        /// </summary>
        /// <returns>
        /// How much health it actually cost. Zero when the blow was fully resisted, or when the
        /// owner was already dead — hitting a corpse is allowed and simply does nothing.
        /// </returns>
        public float TakeDamage(DamageInfo damage)
        {
            if (!IsAlive)
            {
                return 0f;
            }

            float applied = Math.Min(Resistances.Apply(damage), Current);
            if (applied <= 0f)
            {
                return 0f;
            }

            Current -= applied;

            if (IsAlive)
            {
                var taken = new DamageTakenEvent(OwnerId, applied, Current, damage);
                DamageTaken?.Invoke(taken);
                _bus?.Publish(taken);
                return applied;
            }

            Current = 0f;
            var died = new DiedEvent(OwnerId, damage);
            Died?.Invoke(died);
            _bus?.Publish(died);
            return applied;
        }

        /// <summary>
        /// Restores health, up to the maximum. Does nothing to the dead: coming back is a decision
        /// for whoever owns the body, not a side effect of a bandage.
        /// </summary>
        /// <returns>How much was actually restored.</returns>
        public float Heal(float amount)
        {
            if (amount < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount), amount, "Negative healing is damage; say so explicitly.");
            }

            if (!IsAlive || amount == 0f)
            {
                return 0f;
            }

            float healed = Math.Min(amount, Maximum - Current);
            Current += healed;
            return healed;
        }

        /// <summary>
        /// Kills outright, whatever the health. Used for executions, scripted deaths and the
        /// bite that nobody amputated in time.
        /// </summary>
        /// <returns>False when it was already dead.</returns>
        public bool Kill(DamageInfo killingBlow = default)
        {
            if (!IsAlive)
            {
                return false;
            }

            Current = 0f;
            var died = new DiedEvent(OwnerId, killingBlow);
            Died?.Invoke(died);
            _bus?.Publish(died);
            return true;
        }

        /// <summary>
        /// Puts the health back to full without raising anything. For reusing a pooled body, not
        /// for reviving someone in play.
        /// </summary>
        public void ResetToFull() => Current = Maximum;
    }
}
