#nullable enable
using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ZombieGame.Combat;
using ZombieGame.Core.Physics;

namespace ZombieGame.Tests.EditMode
{
    /// <summary>
    /// Rounds in flight: how far they get, what stops them, and what they do on the way.
    /// </summary>
    /// <remarks>
    /// The one that matters most is the sweep. A round covers half a world unit between physics
    /// steps, so a hit test that only looks where it landed lets bullets pass through bodies —
    /// and that bug shows up as "the gun feels unreliable", which is unfalsifiable by hand.
    /// </remarks>
    [TestFixture]
    public sealed class ProjectileTests
    {
        /// <summary>A target sitting at one spot, which records what hit it.</summary>
        private sealed class Dummy : IProjectileHitTest
        {
            private readonly Vector2 _at;
            private readonly float _radius;

            public Dummy(Vector2 at, float radius = 0.3f)
            {
                _at = at;
                _radius = radius;
            }

            public List<DamageInfo> Taken { get; } = new();

            public bool TryHit(
                Vector2 from, Vector2 to, float radius, in DamageInfo damage, out Vector2 point)
            {
                point = _at;

                Vector2 along = to - from;
                float lengthSquared = along.sqrMagnitude;
                float t = lengthSquared > 0.000001f
                    ? Mathf.Clamp01(Vector2.Dot(_at - from, along) / lengthSquared)
                    : 0f;

                Vector2 closest = from + (along * t);
                if ((_at - closest).sqrMagnitude > (_radius + radius) * (_radius + radius))
                {
                    return false;
                }

                Taken.Add(damage);
                return true;
            }
        }

        /// <summary>A wall across the world.</summary>
        private sealed class BlockEverything : ILineOfSight
        {
            public bool IsClear(Vector2 from, Vector2 to) => false;
        }

        private static readonly DamageInfo Shot = new(22f, DamageType.Ballistic, sourceId: 1);

        private ProjectileManager _rounds = null!;

        [SetUp]
        public void CreateManager() => _rounds = new ProjectileManager(capacity: 8);

        private int FireRight(Vector2 from, float speed = 20f, float life = 0.5f) =>
            _rounds.Fire(from, Vector2.right, speed, life, radius: 0.08f, Shot);

        [Test]
        public void FiringPutsARoundInTheAir()
        {
            Assert.That(FireRight(Vector2.zero), Is.GreaterThan(0));
            Assert.That(_rounds.Count, Is.EqualTo(1));
        }

        [Test]
        public void ARoundTravelsTheWayItWasPointed()
        {
            FireRight(Vector2.zero, speed: 10f);
            _rounds.Tick(0.1f);

            Assert.That(_rounds.Get(0).Position.x, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(_rounds.Get(0).Position.y, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void ARoundGivesUpWhenItsTimeIsOut()
        {
            FireRight(Vector2.zero, speed: 10f, life: 0.2f);

            _rounds.Tick(0.1f);
            Assert.That(_rounds.Count, Is.EqualTo(1));

            _rounds.Tick(0.15f);
            Assert.That(_rounds.Count, Is.Zero, "a round that never expires is a laser");
        }

        [Test]
        public void ARoundHitsWhatIsInItsWay()
        {
            var dummy = new Dummy(new Vector2(1f, 0f));
            FireRight(Vector2.zero, speed: 10f);

            Assert.That(_rounds.Tick(0.2f, dummy), Is.EqualTo(1));
            Assert.That(dummy.Taken, Has.Count.EqualTo(1));
            Assert.That(dummy.Taken[0].Amount, Is.EqualTo(Shot.Amount));
            Assert.That(_rounds.Count, Is.Zero, "a round that hits is spent");
        }

        [Test]
        public void ARoundCannotJumpOverSomethingInOneStep()
        {
            // 40 units a second and a twentieth of a second: two whole units of travel in one
            // step, with the target sitting in the middle of it.
            var dummy = new Dummy(new Vector2(1f, 0f));
            FireRight(Vector2.zero, speed: 40f);

            _rounds.Tick(0.05f, dummy);

            Assert.That(
                dummy.Taken,
                Has.Count.EqualTo(1),
                "the round stepped straight over the target");
        }

        [Test]
        public void ARoundMissesWhatItIsNotPointedAt()
        {
            var dummy = new Dummy(new Vector2(1f, 5f));
            FireRight(Vector2.zero, speed: 10f);

            Assert.That(_rounds.Tick(0.2f, dummy), Is.Zero);
            Assert.That(dummy.Taken, Is.Empty);
        }

        [Test]
        public void AWallStopsARound()
        {
            FireRight(Vector2.zero, speed: 10f);

            _rounds.Tick(0.1f, null, new BlockEverything());

            Assert.That(_rounds.Count, Is.Zero, "shooting through walls would make cover pointless");
        }

        [Test]
        public void TheAirCanFillUp()
        {
            for (int i = 0; i < 20; i++)
            {
                FireRight(new Vector2(0f, i));
            }

            Assert.That(_rounds.Count, Is.EqualTo(_rounds.Capacity));
            Assert.That(FireRight(Vector2.zero), Is.Zero, "a full array has to refuse, not grow");
        }

        [Test]
        public void SeveralRoundsSurviveOneOfThemEnding()
        {
            var dummy = new Dummy(new Vector2(1f, 1f));
            FireRight(new Vector2(0f, 0f), speed: 10f);
            FireRight(new Vector2(0f, 1f), speed: 10f);
            FireRight(new Vector2(0f, 2f), speed: 10f);

            _rounds.Tick(0.2f, dummy);

            Assert.That(_rounds.Count, Is.EqualTo(2));
            foreach (ProjectileState round in _rounds.Rounds)
            {
                Assert.That(round.Id, Is.GreaterThan(0), "a live slot holding a dead round");
            }
        }

        [Test]
        public void ARoundWithNoSpeedIsNotFired()
        {
            Assert.That(
                _rounds.Fire(Vector2.zero, Vector2.right, 0f, 1f, 0.1f, Shot),
                Is.Zero);
        }

        [Test]
        public void AManagerOfNoRoundsIsRefused() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => new ProjectileManager(0));
    }
}
