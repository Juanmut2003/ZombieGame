#nullable enable
using UnityEngine;
using ZombieGame.Combat;
using ZombieGame.Core;
using ZombieGame.Core.Events;
using ZombieGame.Waves;

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
        [Tooltip("What is on the belt. The slot in hand decides whether this swings at all.")]
        [SerializeField]
        private Loadout? _loadout;

        [Tooltip("What is being swung, when there is no belt to ask.")]
        [SerializeField]
        private WeaponDefinition? _weapon;

        [Tooltip("Who is steering.")]
        [SerializeField]
        private PlayerInputReader? _input;

        [Tooltip("Where the player is pointing. Without one, the swing goes where the body walks.")]
        [SerializeField]
        private PlayerAim? _aim;

        [Tooltip("What a swing is paid for with.")]
        [SerializeField]
        private CharacterStamina? _stamina;

        private CharacterFacing _facing = null!;
        private EventBus? _bus;
        private float _cooldownRemaining;
        private int _actorId;

        /// <summary>
        /// The melee weapon in hand, or null when the slot in hand is something else.
        /// </summary>
        /// <remarks>
        /// Both weapon components listen to the same attack button and each one checks whether
        /// the slot in hand is theirs. One button, whatever is selected — which is what keeps the
        /// arsenal from needing a key each.
        /// </remarks>
        public WeaponDefinition? Weapon
        {
            get
            {
                if (_loadout is null)
                {
                    return _weapon;
                }

                WeaponDefinition? held = _loadout.Current;
                return held != null && held.Kind == WeaponKind.Melee ? held : null;
            }
        }

        /// <summary>True while the weapon is still recovering from the last swing.</summary>
        public bool IsOnCooldown => _cooldownRemaining > 0f;

        private void Awake()
        {
            _facing = GetComponent<CharacterFacing>();
            _actorId = ActorIds.Next();
        }

        // Start, not Awake: the bootstrap registers the bus in its own Awake, and which of the
        // two runs first depends on where each object sits in the scene file. Fetched too early,
        // the swing is published to nobody — the bat stops working and nothing says why.
        private void Start() => GameServices.TryGet(out _bus);

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
            WeaponDefinition? weapon = Weapon;
            if (weapon is null || IsOnCooldown)
            {
                return false;
            }

            // Stamina is checked and paid before anything else happens, so a swing that costs
            // nothing because the player was exhausted cannot also deal damage.
            if (_stamina is not null && !_stamina.TrySpend(weapon.StaminaCost))
            {
                return false;
            }

            _cooldownRemaining = weapon.CooldownSeconds;

            var swing = new MeleeSwungEvent(
                attackerId: _actorId,
                origin: transform.position,
                facing: _aim is not null ? _aim.Direction : _facing.Direction,
                range: weapon.Range,
                halfAngleDegrees: weapon.HalfAngleDegrees,
                maxTargets: weapon.MaxTargets,
                damage: weapon.CreateDamage(_actorId));

            _bus?.Publish(swing);

            // A swing is heard whether or not it connects. Missing is louder than standing still,
            // which is what makes a wild swing in the dark a decision rather than a free try.
            _bus?.Publish(new NoiseEvent(transform.position, weapon.Noise, _actorId));
            return true;
        }
    }
}
