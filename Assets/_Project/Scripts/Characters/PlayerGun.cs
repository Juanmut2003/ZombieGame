#nullable enable
using UnityEngine;
using ZombieGame.Combat;
using ZombieGame.Core;
using ZombieGame.Core.Events;
using ZombieGame.Waves;

namespace ZombieGame.Characters
{
    /// <summary>
    /// Fires the character's gun, counts the rounds, and puts a fresh magazine in.
    /// </summary>
    /// <remarks>
    /// It announces the shot and stops there, exactly like <see cref="PlayerMelee"/>: what the
    /// round does on its way is the projectile runner's business, and who it hurts belongs to
    /// whoever owns the targets.
    /// A gun is the loudest thing in the game by a wide margin, and that is the point. Every shot
    /// is a trade — a zombie down now against a zone that knows where you are.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterFacing))]
    public sealed class PlayerGun : MonoBehaviour
    {
        [Tooltip("What is being fired. Must be a firearm.")]
        [SerializeField]
        private WeaponDefinition? _weapon;

        [Tooltip("Who is steering.")]
        [SerializeField]
        private PlayerInputReader? _input;

        [Tooltip("How many rounds are in the pockets at the start of a zone.")]
        [SerializeField]
        [Min(0)]
        private int _startingReserve = 48;

        [Tooltip("How far in front of the character a round starts, so it clears their own body.")]
        [SerializeField]
        [Min(0f)]
        private float _muzzleOffset = 0.45f;

        private CharacterFacing _facing = null!;
        private EventBus? _bus;
        private Magazine _magazine = null!;
        private System.Random _spread = null!;
        private int _actorId;
        private float _untilNextShot;
        private float _reloadRemaining;

        /// <summary>What is in the gun and in the pockets.</summary>
        public Magazine Ammo => _magazine;

        /// <summary>True while a magazine is going in. The gun does not fire during this.</summary>
        public bool IsReloading => _reloadRemaining > 0f;

        private void Awake()
        {
            _facing = GetComponent<CharacterFacing>();
            _actorId = ActorIds.Next();
            _spread = new System.Random(_actorId * 7919);

            int capacity = _weapon is not null ? _weapon.MagazineSize : 1;
            _magazine = new Magazine(capacity, capacity, _startingReserve);
        }

        private void Start()
        {
            GameServices.TryGet(out _bus);
            Announce();
        }

        private void Update()
        {
            if (_untilNextShot > 0f)
            {
                _untilNextShot -= Time.deltaTime;
            }

            if (_reloadRemaining > 0f)
            {
                _reloadRemaining -= Time.deltaTime;
                if (_reloadRemaining <= 0f)
                {
                    _magazine.Reload();
                    Announce();
                }

                return;
            }

            if (_input is null)
            {
                return;
            }

            if (_input.ReloadPressed)
            {
                TryReload();
                return;
            }

            // Held, not pressed: a pistol still fires once per cooldown, but holding the button is
            // what an automatic weapon will want and it costs nothing to support now.
            if (_input.FireHeld)
            {
                TryFire();
            }
        }

        /// <summary>
        /// Fires one round if the gun is ready and loaded.
        /// </summary>
        /// <returns>False when it was on cooldown, unarmed, reloading or empty.</returns>
        public bool TryFire()
        {
            if (_weapon is null || _weapon.Kind != WeaponKind.Firearm || IsReloading
                || _untilNextShot > 0f)
            {
                return false;
            }

            if (!_magazine.TryFire())
            {
                // Empty. Reloading starts on its own, because the alternative is a player holding
                // a dead trigger and wondering what broke.
                TryReload();
                return false;
            }

            _untilNextShot = _weapon.CooldownSeconds;

            Vector2 aim = Spread(_facing.Direction, _weapon.SpreadDegrees);
            Vector2 muzzle = (Vector2)transform.position + (aim * _muzzleOffset);

            _bus?.Publish(new ShotFiredEvent(
                _actorId,
                muzzle,
                aim,
                _weapon.MuzzleSpeed,
                _weapon.ProjectileLifeSeconds,
                _weapon.ProjectileRadius,
                _weapon.CreateDamage(_actorId)));

            // The loudest thing the player can do, and the whole reason stealth exists.
            _bus?.Publish(new NoiseEvent(transform.position, _weapon.Noise, _actorId));

            Announce();
            return true;
        }

        /// <summary>Starts putting a fresh magazine in.</summary>
        /// <returns>False when the gun is full, the pockets are empty, or one is already going in.</returns>
        public bool TryReload()
        {
            if (_weapon is null || IsReloading || !_magazine.CanReload)
            {
                return false;
            }

            _reloadRemaining = _weapon.ReloadSeconds;
            Announce();
            return true;
        }

        /// <summary>Scatters a shot within the weapon's spread.</summary>
        private Vector2 Spread(Vector2 aim, float degrees)
        {
            Vector2 straight = aim.sqrMagnitude > 0.000001f ? aim.normalized : Vector2.right;
            if (degrees <= 0f)
            {
                return straight;
            }

            float offset = (float)((_spread.NextDouble() * 2d) - 1d) * degrees;
            return (Vector2)(Quaternion.Euler(0f, 0f, offset) * straight);
        }

        private void Announce() =>
            _bus?.Publish(new AmmoChangedEvent(
                _magazine.Loaded, _magazine.Reserve, IsReloading, isPlayer: true));
    }
}
