#nullable enable
using UnityEngine;
using ZombieGame.Core.Events;

namespace ZombieGame.Combat
{
    /// <summary>
    /// Announces a swing. Whoever can be hit by it works out whether they were.
    /// </summary>
    /// <remarks>
    /// The attacker does not reach into the horde to deal damage. It says where it swung, and the
    /// zombie manager — which owns its array and is the only thing allowed to touch it — resolves
    /// the hits. That keeps the character code free of any reference to the AI domain, which is
    /// what CLAUDE.md asks of a signal that crosses domains, and it means survivors, companions
    /// and later human raiders all answer the same swing without the attacker knowing they exist.
    /// </remarks>
    public readonly struct MeleeSwungEvent : IEvent
    {
        public MeleeSwungEvent(
            int attackerId,
            Vector2 origin,
            Vector2 facing,
            float range,
            float halfAngleDegrees,
            int maxTargets,
            DamageInfo damage)
        {
            AttackerId = attackerId;
            Origin = origin;
            Facing = facing;
            Range = range;
            HalfAngleDegrees = halfAngleDegrees;
            MaxTargets = maxTargets;
            Damage = damage;
        }

        /// <summary>Who swung, so nobody hits themselves.</summary>
        public int AttackerId { get; }

        /// <summary>Where the swing started.</summary>
        public Vector2 Origin { get; }

        /// <summary>Which way it went.</summary>
        public Vector2 Facing { get; }

        /// <summary>How far it reached.</summary>
        public float Range { get; }

        /// <summary>Half the width of the wedge, in degrees.</summary>
        public float HalfAngleDegrees { get; }

        /// <summary>How many targets it may catch.</summary>
        public int MaxTargets { get; }

        /// <summary>The blow each target takes.</summary>
        public DamageInfo Damage { get; }
    }
}
