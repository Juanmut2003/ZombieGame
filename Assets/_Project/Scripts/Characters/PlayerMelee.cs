#nullable enable
using UnityEngine;
using ZombieGame.Combat;
using ZombieGame.Core;
using ZombieGame.Core.Events;

namespace ZombieGame.Characters
{
    /// <summary>
    /// Swings the character's melee weapon when the attack input comes in.
    /// </summary>
    /// <remarks>
    /// It announces the swing and stops there. Working out who was caught belongs to whoever owns
    /// the targets — the zombie manager owns its array and is the only thing allowed to touch it.
    /// So this class has no idea zombies exist, which is also how survivors, companions and later
    /// human raiders will answer the same swing without it changing.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterFacing))]
    public sealed class PlayerMelee : MonoBehaviour
    {
        [Tooltip("What is being swung.")]
        [SerializeField]
        private WeaponDefinition? _weapon;

        [Tooltip("Who is steering.")]
        [SerializeField]
        private PlayerInputReader? _input;

        [Tooltip("What a swing is paid for with.")]
        [SerializeField]
        private CharacterStamina? _stamina;

        private CharacterFacing _facing = null!;
        private EventBus? _bus;
        private float _cooldownRemaining;
        private int _actorId;

        /// <summary>True while the weapon is still recovering from the last swing.</summary>
        public bool IsOnCooldown => _cooldownRemaining > 0f;

        private void Awake()
        {
            _facing = GetComponent<CharacterFacing>();
            _actorId = ActorIds.Next();
            GameServices.TryGet(out _bus);
        }

        private void Update()
        {
            if (_cooldownRemaining > 0f)
            {
                _cooldownRemaining -= Time.deltaTime;
            }

            if (_input is null || !_input.AttackPressed)
            {
                return;
            }

            TrySwing();
        }

        /// <summary>
        /// Swings if the weapon is ready and there is stamina for it.
        /// </summary>
        /// <returns>False when it was on cooldown, unarmed, or too tired.</returns>
        public bool TrySwing()
        {
            if (_weapon is null || IsOnCooldown)
            {
                return false;
            }

            // Stamina is checked and paid before anything else happens, so a swing that costs
            // nothing because the player was exhausted cannot also deal damage.
            if (_stamina is not null && !_stamina.TrySpend(_weapon.StaminaCost))
            {
                return false;
            }

            _cooldownRemaining = _weapon.CooldownSeconds;

            var swing = new MeleeSwungEvent(
                attackerId: _actorId,
                origin: transform.position,
                facing: _facing.Direction,
                range: _weapon.Range,
                halfAngleDegrees: _weapon.HalfAngleDegrees,
                maxTargets: _weapon.MaxTargets,
                damage: _weapon.CreateDamage(_actorId));

            _bus?.Publish(swing);
            return true;
        }
    }
}
