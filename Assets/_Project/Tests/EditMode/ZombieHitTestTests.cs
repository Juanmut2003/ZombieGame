#nullable enable
using NUnit.Framework;
using UnityEngine;
using ZombieGame.AI;
using ZombieGame.Combat;

namespace ZombieGame.Tests.EditMode
{
    /// <summary>
    /// Rounds against the horde: who is on the line, and who is nearest.
    /// </summary>
    /// <remarks>
    /// The sweep is the only place where combat and the horde meet, so it is the place where a
    /// mistake is quietly expensive: a bullet that picks whichever zombie sits earliest in the
    /// array would shoot past the one in front of you and kill something behind it.
    /// </remarks>
    [TestFixture]
    public sealed class ZombieHitTestTests
    {
        private ZombieManager _horde = null!;
        private ZombieDefinition _definition = null!;
        private ZombieHitTest _shots = null!;

        [SetUp]
        public void CreateHorde()
        {
            _definition = ScriptableObject.CreateInstance<ZombieDefinition>();
            _horde = new ZombieManager(capacity: 8, crowd: CrowdSettings.None);
            _shots = new ZombieHitTest(_horde);
        }

        [TearDown]
        public void DestroyDefinition() => Object.DestroyImmediate(_definition);

        private static readonly DamageInfo Round = new(15f, DamageType.Ballistic, sourceId: 1);

        [Test]
        public void ARoundHitsAZombieOnItsLine()
        {
            _horde.Spawn(_definition, new Vector2(2f, 0f));

            Assert.That(
                _shots.TryHit(Vector2.zero, new Vector2(4f, 0f), 0.08f, Round, out Vector2 point),
                Is.True);
            Assert.That(point.x, Is.LessThan(2.1f));
            Assert.That(_horde.Get(0).Health, Is.LessThan(_definition.MaxHealth));
        }

        [Test]
        public void ARoundMissesWhatIsNotOnItsLine()
        {
            _horde.Spawn(_definition, new Vector2(2f, 5f));

            Assert.That(
                _shots.TryHit(Vector2.zero, new Vector2(4f, 0f), 0.08f, Round, out _),
                Is.False);
            Assert.That(_horde.Get(0).Health, Is.EqualTo(_definition.MaxHealth));
        }

        [Test]
        public void TheNearestOneIsTheOneThatIsHit()
        {
            int near = _horde.Spawn(_definition, new Vector2(1f, 0f));
            int far = _horde.Spawn(_definition, new Vector2(3f, 0f));

            _shots.TryHit(Vector2.zero, new Vector2(4f, 0f), 0.08f, Round, out _);

            Assert.That(
                _horde.Get(_horde.IndexOf(near)).Health,
                Is.LessThan(_definition.MaxHealth),
                "the round went past the one in front");
            Assert.That(
                _horde.Get(_horde.IndexOf(far)).Health, Is.EqualTo(_definition.MaxHealth));
        }

        [Test]
        public void OneRoundHurtsOnlyOneZombie()
        {
            _horde.Spawn(_definition, new Vector2(1f, 0f));
            _horde.Spawn(_definition, new Vector2(2f, 0f));

            _shots.TryHit(Vector2.zero, new Vector2(4f, 0f), 0.08f, Round, out _);

            int hurt = 0;
            foreach (ZombieState zombie in _horde.States)
            {
                if (zombie.Health < _definition.MaxHealth)
                {
                    hurt++;
                }
            }

            Assert.That(hurt, Is.EqualTo(1), "a pistol is not a shotgun");
        }

        [Test]
        public void AZombieBehindTheMuzzleIsSafe()
        {
            _horde.Spawn(_definition, new Vector2(-2f, 0f));

            Assert.That(
                _shots.TryHit(Vector2.zero, new Vector2(4f, 0f), 0.08f, Round, out _),
                Is.False,
                "shooting forwards must not hurt what is behind you");
        }

        [Test]
        public void AKillingRoundTakesTheZombieOutOfTheHorde()
        {
            _horde.Spawn(_definition, new Vector2(1f, 0f));
            var killing = new DamageInfo(10000f, DamageType.Ballistic, sourceId: 1);

            Assert.That(
                _shots.TryHit(Vector2.zero, new Vector2(4f, 0f), 0.08f, killing, out _), Is.True);
            Assert.That(_horde.Count, Is.Zero);
        }

        [Test]
        public void AnEmptyZoneSwallowsNoRounds() =>
            Assert.That(
                _shots.TryHit(Vector2.zero, new Vector2(4f, 0f), 0.08f, Round, out _), Is.False);
    }
}
