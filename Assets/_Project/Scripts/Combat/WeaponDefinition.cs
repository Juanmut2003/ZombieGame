#nullable enable
using UnityEngine;

namespace ZombieGame.Combat
{
    /// <summary>
    /// One weapon, as data. The bat, the axe, the crowbar and the machete of PROJECT.md 9 are
    /// assets, never classes.
    /// </summary>
    /// <remarks>
    /// Melee and firearms share this one asset type. A pistol and a bat have more in common than
    /// not — damage, noise, a rate of fire — and the handful of fields only one of them uses is a
    /// far smaller price than two asset types that drift apart.
    /// </remarks>
    [CreateAssetMenu(menuName = "ZombieGame/Weapon Definition", fileName = "WeaponDefinition")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField]
        private string _displayName = "Bat";

        [Tooltip("Swung or fired. Decides which half of the combat code picks this up.")]
        [SerializeField]
        private WeaponKind _kind = WeaponKind.Melee;

        [Header("Damage")]
        [Tooltip("Damage per hit, before the target's resistances.")]
        [SerializeField]
        [Min(0f)]
        private float _damage = 18f;

        [Tooltip("Which resistance applies. Blunt is the answer to armour.")]
        [SerializeField]
        private DamageType _damageType = DamageType.Blunt;

        [Header("Reach")]
        [Tooltip("How far the swing reaches, in world units.")]
        [SerializeField]
        [Min(0.1f)]
        private float _range = 1.1f;

        [Tooltip("Half the width of the swing, in degrees. 180 would hit all around you.")]
        [SerializeField]
        [Range(5f, 180f)]
        private float _halfAngleDegrees = 60f;

        [Tooltip("How many targets one swing can catch. A bat does not clear a street.")]
        [SerializeField]
        [Min(1)]
        private int _maxTargets = 3;

        [Header("Cost")]
        [Tooltip("Stamina per swing.")]
        [SerializeField]
        [Min(0f)]
        private float _staminaCost = 6f;

        [Tooltip("Seconds before it can swing again.")]
        [SerializeField]
        [Min(0.05f)]
        private float _cooldownSeconds = 0.45f;

        [Header("Firearm")]
        [Tooltip("Rounds in a full magazine. Ignored by melee weapons.")]
        [SerializeField]
        [Min(1)]
        private int _magazineSize = 12;

        [Tooltip("Seconds to put a fresh magazine in.")]
        [SerializeField]
        [Min(0.1f)]
        private float _reloadSeconds = 1.6f;

        [Tooltip("How fast a round leaves the barrel, in world units per second.")]
        [SerializeField]
        [Min(1f)]
        private float _muzzleSpeed = 24f;

        [Tooltip("How far off aim a round can go, in degrees either way.")]
        [SerializeField]
        [Range(0f, 45f)]
        private float _spreadDegrees = 2.5f;

        [Tooltip("Seconds a round stays in the air before it gives up. Speed times this is its reach.")]
        [SerializeField]
        [Min(0.05f)]
        private float _projectileLifeSeconds = 0.7f;

        [Tooltip("How fat a round is, for deciding what it clips.")]
        [SerializeField]
        [Min(0.01f)]
        private float _projectileRadius = 0.08f;

        [Header("Noise")]
        [Tooltip("How loud a swing is. Melee is quiet, which is the whole point of it.")]
        [SerializeField]
        [Min(0f)]
        private float _noise = 1f;

        /// <summary>Shown in the inventory and the loadout.</summary>
        public string DisplayName => _displayName;

        /// <summary>Swung or fired.</summary>
        public WeaponKind Kind => _kind;

        /// <summary>Rounds in a full magazine.</summary>
        public int MagazineSize => _magazineSize;

        /// <summary>Seconds to reload.</summary>
        public float ReloadSeconds => _reloadSeconds;

        /// <summary>Muzzle speed in world units per second.</summary>
        public float MuzzleSpeed => _muzzleSpeed;

        /// <summary>How far off aim a round can go, in degrees either way.</summary>
        public float SpreadDegrees => _spreadDegrees;

        /// <summary>Seconds a round stays in the air.</summary>
        public float ProjectileLifeSeconds => _projectileLifeSeconds;

        /// <summary>How fat a round is.</summary>
        public float ProjectileRadius => _projectileRadius;

        /// <summary>How far a round can reach before it expires.</summary>
        public float ProjectileReach => _muzzleSpeed * _projectileLifeSeconds;

        /// <summary>Damage per hit, before resistances.</summary>
        public float Damage => _damage;

        /// <summary>Which resistance applies.</summary>
        public DamageType DamageType => _damageType;

        /// <summary>How far the swing reaches.</summary>
        public float Range => _range;

        /// <summary>Half the width of the swing, in degrees.</summary>
        public float HalfAngleDegrees => _halfAngleDegrees;

        /// <summary>How many targets one swing can catch.</summary>
        public int MaxTargets => _maxTargets;

        /// <summary>Stamina per swing.</summary>
        public float StaminaCost => _staminaCost;

        /// <summary>Seconds before the next swing.</summary>
        public float CooldownSeconds => _cooldownSeconds;

        /// <summary>How loud one swing is, for the noise system.</summary>
        public float Noise => _noise;

        /// <summary>The blow this weapon deals, as the damage model wants it.</summary>
        public DamageInfo CreateDamage(int attackerId) => new(_damage, _damageType, attackerId);
    }
}
