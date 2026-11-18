#nullable enable
using NUnit.Framework;
using UnityEngine;
using ZombieGame.AI;
using ZombieGame.Combat;

namespace ZombieGame.Tests.EditMode
{
    /// <summary>
    /// What the horde does once it has actually reached you.
    /// </summary>
    /// <remarks>
    /// Until this existed a run could not be lost, which made every decision in a mission a
    /// matter of patience rather than risk. These are the rules that put the cost back: who can
    /// bite, how often, and what being surrounded is worth.
    /// </remarks>
    [TestFixture]
    public sealed class ZombieBiteTests
    {
        private ZombieManager _manager = null!;
        private ZombieDefinition _definition = null!;

        [SetUp]
        public void CreateHorde()
        {
            _definition = ScriptableObject.CreateInstance<ZombieDefinition>();
            _manager = new ZombieManager(capacity: 8, crowd: CrowdSettings.None, seed: 909);
        }

        [TearDown]
        public void DestroyDefinition() => UnityEngine.Object.DestroyImmediate(_definition);

        /// <summary>Spawns one already hunting, which is the only state that bites.</summary>
        private int SpawnChasing(Vector2 at)
        {
            int id = _manager.Spawn(_definition, at);
            int index = _manager.IndexOf(id);

            // Decide is what turns "sees you" into "chases you"; perceiving first is what makes
            // it see. Both run against a target standing on top of it, so it cannot miss.
            _manager.Perceive(at, 0.25f);
            _manager.Decide(at, 0.02f);
            return index;
        }

        [Test]
        public void OneWithinReachBites()
        {
            SpawnChasing(Vector2.zero);

            float damage = _manager.BiteAt(Vector2.zero, 0.25f, 0.02f, out DamageType type);

            Assert.That(damage, Is.GreaterThan(0f));
            Assert.That(type, Is.EqualTo(_definition.Bite.Type));
        }

        [Test]
        public void OneOutOfReachDoesNot()
        {
            SpawnChasing(Vector2.zero);

            float damage = _manager.BiteAt(new Vector2(12f, 0f), 0.25f, 0.02f, out _);

            Assert.That(damage, Is.Zero);
        }

        [Test]
        public void ItCannotBiteTwiceInARow()
        {
            SpawnChasing(Vector2.zero);

            float first = _manager.BiteAt(Vector2.zero, 0.25f, 0.02f, out _);
            float second = _manager.BiteAt(Vector2.zero, 0.25f, 0.02f, out _);

            Assert.That(first, Is.GreaterThan(0f));
            Assert.That(second, Is.Zero, "the cooldown is what stops a bite becoming a blender");
        }

        [Test]
        public void ItBitesAgainOnceTheCooldownHasRunOut()
        {
            SpawnChasing(Vector2.zero);
            _manager.BiteAt(Vector2.zero, 0.25f, 0.02f, out _);

            // Waited out in steps, the way a frame does it.
            float waited = 0f;
            float again = 0f;
            while (waited < _definition.Bite.Seconds + 0.2f && again <= 0f)
            {
                again = _manager.BiteAt(Vector2.zero, 0.25f, 0.05f, out _);
                waited += 0.05f;
            }

            Assert.That(again, Is.GreaterThan(0f));
        }

        [Test]
        public void BeingSurroundedCostsMore()
        {
            // The arithmetic that makes a crowd worse than a queue: every one of them is on its
            // own clock, so three of them land three bites in the step one of them lands one.
            SpawnChasing(Vector2.zero);
            float alone = _manager.BiteAt(Vector2.zero, 0.25f, 0.02f, out _);

            _manager.Clear();
            SpawnChasing(new Vector2(0.1f, 0f));
            SpawnChasing(new Vector2(-0.1f, 0f));
            SpawnChasing(new Vector2(0f, 0.1f));
            float surrounded = _manager.BiteAt(Vector2.zero, 0.25f, 0.02f, out _);

            Assert.That(surrounded, Is.EqualTo(alone * 3f).Within(0.001f));
        }

        [Test]
        public void HittingOneInterruptsItsBite()
        {
            // Why this exists: the bat's arc is barely longer than a zombie's reach, so without
            // an interruption every swing landed was a bite taken, and a melee weapon nobody can
            // use without losing health is not a weapon. Hitting first now buys one cycle.
            int index = SpawnChasing(Vector2.zero);
            _manager.Damage(index, new DamageInfo(1f));

            float damage = _manager.BiteAt(Vector2.zero, 0.25f, 0.02f, out _);

            Assert.That(damage, Is.Zero);
        }

        [Test]
        public void HittingOneDoesNotInterruptTheOneNextToIt()
        {
            // The limit of it: a crowd still gets through, because every one of them has to be
            // hit separately and a bat only swings at one at a time.
            int first = SpawnChasing(new Vector2(0.1f, 0f));
            SpawnChasing(new Vector2(-0.1f, 0f));
            _manager.Damage(first, new DamageInfo(1f));

            float damage = _manager.BiteAt(Vector2.zero, 0.25f, 0.02f, out _);

            Assert.That(damage, Is.GreaterThan(0f));
        }

        [Test]
        public void OneThatHasNotNoticedYouDoesNotBite()
        {
            // Spawned and left alone, so it is resting rather than hunting. Walking past a
            // standing crowd has to be possible, or there is no sneaking in the game at all.
            _manager.Spawn(_definition, Vector2.zero);

            float damage = _manager.BiteAt(Vector2.zero, 0.25f, 0.02f, out _);

            Assert.That(damage, Is.Zero);
        }
    }
}
