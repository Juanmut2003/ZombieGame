#nullable enable
using NUnit.Framework;
using UnityEngine;
using ZombieGame.AI;
using ZombieGame.Combat;
using ZombieGame.Core.Physics;

namespace ZombieGame.Tests.EditMode
{
    /// <summary>
    /// What a wall does to a swing.
    /// </summary>
    /// <remarks>
    /// The arc said a zombie was in front of the bat and nothing asked what was in between, so
    /// the bat reached into the next room: anyone could stand on the safe side of a wall and
    /// clear a building through it. A swing is stopped by the same walls the horde cannot see
    /// through.
    /// </remarks>
    [TestFixture]
    public sealed class MeleeThroughWallsTests
    {
        /// <summary>A wall across everything.</summary>
        private sealed class BlockEverything : ILineOfSight
        {
            public bool IsClear(Vector2 from, Vector2 to) => false;
        }

        /// <summary>A wall that only stands between the swing and anybody to the right.</summary>
        private sealed class WallToTheRight : ILineOfSight
        {
            public bool IsClear(Vector2 from, Vector2 to) => to.x < 1.5f;
        }

        private ZombieManager _horde = null!;
        private ZombieDefinition _definition = null!;

        [SetUp]
        public void CreateHorde()
        {
            _definition = ScriptableObject.CreateInstance<ZombieDefinition>();
            _horde = new ZombieManager(capacity: 8, crowd: CrowdSettings.None);
        }

        [TearDown]
        public void DestroyDefinition() => Object.DestroyImmediate(_definition);

        private static MeleeSwungEvent Swing() =>
            new MeleeSwungEvent(
                attackerId: 1,
                origin: Vector2.zero,
                facing: Vector2.right,
                range: 4f,
                halfAngleDegrees: 80f,
                maxTargets: 4,
                damage: new DamageInfo(5f));

        [Test]
        public void AZombieInTheOpenIsHit()
        {
            _horde.Spawn(_definition, new Vector2(1f, 0f));

            Assert.That(_horde.ApplySwing(Swing()), Is.EqualTo(1));
        }

        [Test]
        public void AZombieBehindAWallIsNotHit()
        {
            _horde.Spawn(_definition, new Vector2(1f, 0f));
            float before = _horde.Get(0).Health;

            Assert.That(_horde.ApplySwing(Swing(), new BlockEverything()), Is.EqualTo(0));
            Assert.That(
                _horde.Get(0).Health,
                Is.EqualTo(before),
                "the bat went through the wall and took health with it");
        }

        [Test]
        public void TheOneThisSideOfTheWallIsStillHit()
        {
            // Both are inside the arc. Only the near one is actually reachable, and a wall in
            // the room must not make the whole swing miss.
            _horde.Spawn(_definition, new Vector2(1f, 0f));
            _horde.Spawn(_definition, new Vector2(3f, 0f));

            Assert.That(_horde.ApplySwing(Swing(), new WallToTheRight()), Is.EqualTo(1));
        }

        [Test]
        public void WithNothingInTheWayEverybodyInTheArcIsHit()
        {
            _horde.Spawn(_definition, new Vector2(1f, 0f));
            _horde.Spawn(_definition, new Vector2(2f, 1f));

            Assert.That(_horde.ApplySwing(Swing(), null), Is.EqualTo(2));
        }
    }
}
