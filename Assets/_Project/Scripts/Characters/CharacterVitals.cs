#nullable enable
using UnityEngine;
using ZombieGame.Combat;
using ZombieGame.Core;
using ZombieGame.Core.Config;
using ZombieGame.Core.Events;

namespace ZombieGame.Characters
{
    /// <summary>
    /// Holds one character's health, says when it moves, and says once when it runs out.
    /// </summary>
    /// <remarks>
    /// The twin of <see cref="CharacterStamina"/>, and deliberately a separate component: stamina
    /// is spent and comes back on its own, health is taken and does not. A run that could not be
    /// lost is a tour, so this is the piece that turns the zone into a mission — but only the
    /// bookkeeping half. What ending a run means belongs to the mission layer, which listens.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class CharacterVitals : MonoBehaviour, IHurtable
    {
        [Tooltip("Where the health numbers come from.")]
        [SerializeField]
        private BalanceConfig? _balance;

        private Health _health = null!;
        private EventBus? _bus;
        private bool _isPlayer;
        private float _lastAnnounced = float.NaN;

        /// <summary>Health left.</summary>
        public float Current => _health.Current;

        /// <summary>How full it is, for the bar on the HUD.</summary>
        public float Fraction => _health.Fraction;

        /// <summary>True while there is health left.</summary>
        public bool IsAlive => _health.IsAlive;

        /// <summary>Who this belongs to, as the HUD and the mission see it.</summary>
        public int OwnerId => _health.OwnerId;

        private void Awake()
        {
            if (_balance is null)
            {
                Debug.LogWarning($"[Vitals] {name} has no BalanceConfig; using the defaults.");
                _balance = BalanceConfig.CreateDefault();
            }

            // The character the player steers is the one with an input reader on it, the same way
            // stamina decides it. A "this is the hero" checkbox can be ticked on two objects.
            _isPlayer = GetComponent<PlayerInputReader>() is not null;
            _health = new Health(_balance.SurvivorHealth, ActorIds.Next());
        }

        private void Update()
        {
            // Fetched here rather than in Awake: the bootstrap registering the bus lives in the
            // same scene, and the order of two objects' Awake is not something to rely on.
            if (_bus is null)
            {
                GameServices.TryGet(out _bus);
            }

            Announce();
        }

        /// <summary>Hurts the character, and announces the result.</summary>
        /// <returns>How much health it actually cost.</returns>
        public float TakeDamage(DamageInfo damage)
        {
            bool wasAlive = _health.IsAlive;
            float applied = _health.TakeDamage(damage);

            if (applied > 0f)
            {
                Announce();
            }

            // Death goes out exactly once, from here rather than from Health's own event, so that
            // a listener gets it on the bus with everything else rather than having to find this
            // component and hook a delegate to it.
            if (wasAlive && !_health.IsAlive)
            {
                _bus?.Publish(new DiedEvent(_health.OwnerId, damage));
            }

            return applied;
        }

        /// <summary>Back to full, for a fresh run.</summary>
        public void ResetToFull() => _health.ResetToFull();

        private void Announce()
        {
            if (_bus is null || Mathf.Approximately(_health.Current, _lastAnnounced))
            {
                return;
            }

            _lastAnnounced = _health.Current;
            _bus.Publish(new HealthChangedEvent(
                _health.OwnerId, _health.Current, _health.Maximum, _isPlayer));
        }
    }
}
