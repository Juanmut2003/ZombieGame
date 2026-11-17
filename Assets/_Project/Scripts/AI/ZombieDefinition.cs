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

        [Header("Senses")]
        [Tooltip("How far it can see, in world units.")]
        [SerializeField]
        [Min(0f)]
        private float _viewDistance = 7f;

        [Tooltip("Half the width of its view cone, in degrees. 90 is everything in front of it.")]
        [SerializeField]
        [Range(0f, 180f)]
        private float _viewHalfAngleDegrees = 55f;

        [Tooltip("How far an ordinary noise carries to it. A louder noise reaches further.")]
        [SerializeField]
        [Min(0f)]
        private float _hearingRadius = 6f;

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

        [Header("Look — alerted")]
        [Tooltip("Facing the camera, arms up. Shown while it has noticed someone.")]
        [SerializeField]
        private Sprite? _frontAlert;

        [Tooltip("Lunging to the left.")]
        [SerializeField]
        private Sprite? _leftAlert;

        [Tooltip("Lunging to the right.")]
        [SerializeField]
        private Sprite? _rightAlert;

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

        /// <summary>What this kind can see and hear.</summary>
        public ZombieSenses Senses =>
            new(_viewDistance, _viewHalfAngleDegrees, _hearingRadius);

        /// <summary>The sprite for a facing, falling back to the front view.</summary>
        public Sprite? SpriteFor(ZombieFacing facing) => facing switch
        {
            ZombieFacing.Left => _left != null ? _left : _front,
            ZombieFacing.Right => _right != null ? _right : _front,
            _ => _front,
        };

        /// <summary>
        /// The sprite for a facing, in the pose that matches whether it has noticed anyone.
        /// </summary>
        /// <remarks>
        /// An alerted zombie showing a different pose is the only way a player can tell they have
        /// been seen. Without it, perception is invisible: the first sign of being noticed would
        /// be a zombie arriving, which is far too late to do anything about.
        /// Falls back to the calm pose, so a kind without alert art simply never changes.
        /// </remarks>
        public Sprite? SpriteFor(ZombieFacing facing, bool alerted)
        {
            if (!alerted)
            {
                return SpriteFor(facing);
            }

            Sprite? alert = facing switch
            {
                ZombieFacing.Left => _leftAlert != null ? _leftAlert : _frontAlert,
                ZombieFacing.Right => _rightAlert != null ? _rightAlert : _frontAlert,
                _ => _frontAlert,
            };

            return alert != null ? alert : SpriteFor(facing);
        }
    }
}
