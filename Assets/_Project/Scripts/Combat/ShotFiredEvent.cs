#nullable enable
using UnityEngine;
using ZombieGame.Core.Events;

namespace ZombieGame.Combat
{
    /// <summary>
    /// Somebody pulled a trigger. Whoever runs the rounds puts one in the air.
    /// </summary>
    /// <remarks>
    /// Same split as the swing: the shooter announces, and the system that owns the rounds spawns
    /// them. So the character code holds no reference to the projectile pool, and a survivor,
    /// a raider or a turret firing the same shot needs nothing new.
    /// </remarks>
    public readonly struct ShotFiredEvent : IEvent
    {
        public ShotFiredEvent(
            int shooterId,
            Vector2 origin,
            Vector2 direction,
            float speed,
            float lifeSeconds,
            float radius,
            DamageInfo damage)
        {
            ShooterId = shooterId;
            Origin = origin;
            Direction = direction;
            Speed = speed;
            LifeSeconds = lifeSeconds;
            Radius = radius;
            Damage = damage;
        }

        /// <summary>Who fired.</summary>
        public int ShooterId { get; }

        /// <summary>Where the round starts.</summary>
        public Vector2 Origin { get; }

        /// <summary>Where it is headed, spread already applied.</summary>
        public Vector2 Direction { get; }

        /// <summary>Muzzle speed in world units per second.</summary>
        public float Speed { get; }

        /// <summary>Seconds the round stays in the air.</summary>
        public float LifeSeconds { get; }

        /// <summary>How fat the round is.</summary>
        public float Radius { get; }

        /// <summary>What it does to whatever it hits.</summary>
        public DamageInfo Damage { get; }
    }
}
