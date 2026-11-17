#nullable enable
using UnityEngine;
using ZombieGame.Combat;

namespace ZombieGame.AI
{
    /// <summary>
    /// One kind of zombie, as data. The Walker, the Runner, the Armored and the rest of
    /// PROJECT.md 6 are assets built from this, never subclasses.
    /// </summary>
    /// <remarks>
    /// Adding a zombie type must stay a matter of filling in an asset. The manager ticks numbers
    /// out of these and never asks what kind of zombie it is dealing with.
    /// </remarks>
    [CreateAssetMenu(menuName = "ZombieGame/Zombie Definition", fileName = "ZombieDefinition")]
    public sealed class ZombieDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Shown in the bestiary and in debug overlays.")]
        [SerializeField]
        private string _displayName = "Walker";

        [Header("Body")]
        [Tooltip("Health at full.")]
        [SerializeField]
        [Min(1f)]
        private float _maxHealth = 40f;

        [Tooltip("How fast it moves, in world units per second.")]
        [SerializeField]
        [Min(0f)]
        private float _moveSpeed = 1.2f;

        [Tooltip("How much the walking speed varies between individuals, as a fraction. 0.2 is plus or minus a fifth.")]
        [SerializeField]
        [Range(0f, 0.9f)]
        private float _speedVariance = 0.25f;

        [Tooltip("How much room this body takes up, in world units. Drives how tightly a crowd packs.")]
        [SerializeField]
        [Min(0.05f)]
        private float _bodyRadius = 0.3f;

        [Header("Resistances")]
        [Tooltip("Damage multiplier per type: blunt, sharp, ballistic, fire, toxic. 1 is full.")]
        [SerializeField]
        private float _blunt = 1f;

        [SerializeField]
        private float _sharp = 1f;

        [SerializeField]
        private float _ballistic = 1f;

        [SerializeField]
        private float _fire = 1f;

        [SerializeField]
        private float _toxic = 1f;

        [Header("Look")]
        [Tooltip("Facing the camera.")]
        [SerializeField]
        private Sprite? _front;

        [Tooltip("Walking to the left.")]
        [SerializeField]
        private Sprite? _left;

        [Tooltip("Walking to the right.")]
        [SerializeField]
        private Sprite? _right;

        /// <summary>Shown in the bestiary and debug overlays.</summary>
        public string DisplayName => _displayName;

        /// <summary>Health at full.</summary>
        public float MaxHealth => _maxHealth;

        /// <summary>Movement speed in world units per second, before individual variation.</summary>
        public float MoveSpeed => _moveSpeed;

        /// <summary>How much the speed varies between individuals, as a fraction.</summary>
        public float SpeedVariance => _speedVariance;

        /// <summary>How much room one body takes up, in world units.</summary>
        public float BodyRadius => _bodyRadius;

        /// <summary>Armour and weak points, as the combat code wants them.</summary>
        public DamageResistances Resistances =>
            new(_blunt, _sharp, _ballistic, _fire, _toxic);

        /// <summary>The sprite for a facing, falling back to the front view.</summary>
        public Sprite? SpriteFor(ZombieFacing facing) => facing switch
        {
            ZombieFacing.Left => _left != null ? _left : _front,
            ZombieFacing.Right => _right != null ? _right : _front,
            _ => _front,
        };
    }
}
