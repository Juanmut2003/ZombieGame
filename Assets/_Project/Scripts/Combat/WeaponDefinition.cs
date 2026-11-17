#nullable enable
using UnityEngine;

namespace ZombieGame.Combat
{
    /// <summary>
    /// One weapon, as data. The bat, the axe, the crowbar and the machete of PROJECT.md 9 are
    /// assets, never classes.
    /// </summary>
    /// <remarks>
    /// Melee only for now; firearms add their own fields when they arrive, and the fields they
    /// share stay here.
    /// </remarks>
    [CreateAssetMenu(menuName = "ZombieGame/Weapon Definition", fileName = "WeaponDefinition")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField]
        private string _displayName = "Bat";

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

        [Header("Noise")]
        [Tooltip("How loud a swing is. Melee is quiet, which is the whole point of it.")]
        [SerializeField]
        [Min(0f)]
        private float _noise = 1f;

        /// <summary>Shown in the inventory and the loadout.</summary>
        public string DisplayName => _displayName;

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
