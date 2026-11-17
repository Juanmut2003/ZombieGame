#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using ZombieGame.AI;
using ZombieGame.Combat;
using ZombieGame.Core.Events;

namespace ZombieGame.Tests.EditMode
{
    [TestFixture]
    public sealed class ZombieManagerTests
    {
        private ZombieDefinition _walker = null!;

        [SetUp]
        public void CreateDefinition()
        {
            // Built in memory rather than loaded from an asset: a test should not depend on
            // someone not having edited the walker's numbers.
            _walker = ScriptableObject.CreateInstance<ZombieDefinition>();
            var serialized = new UnityEditor.SerializedObject(_walker);
            serialized.FindProperty("_maxHealth").floatValue = 40f;
            serialized.FindProperty("_moveSpeed").floatValue = 2f;
            serialized.FindProperty("_ballistic").floatValue = 0.5f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void DestroyDefinition() => UnityEngine.Object.DestroyImmediate(_walker);

        [Test]
        public void AFreshHordeIsEmpty()
        {
            var manager = new ZombieManager(10);

            Assert.That(manager.Count, Is.Zero);
            Assert.That(manager.Capacity, Is.EqualTo(10));
            Assert.That(manager.IsFull, Is.False);
            Assert.That(manager.States.Length, Is.Zero);
        }

        [Test]
        public void SpawningAddsAZombieWithTheDefinitionsNumbers()
        {
            var manager = new ZombieManager(10);

            int id = manager.Spawn(_walker, new Vector2(3f, 4f));

            Assert.That(id, Is.GreaterThan(0));
            Assert.That(manager.Count, Is.EqualTo(1));
            ZombieState zombie = manager.Get(0);
            Assert.That(zombie.Position, Is.EqualTo(new Vector2(3f, 4f)));
            Assert.That(zombie.Health, Is.EqualTo(40f));
            Assert.That(zombie.Speed, Is.EqualTo(2f));
        }

        [Test]
        public void EveryZombieGetsItsOwnId()
        {
            var manager = new ZombieManager(10);
            var ids = new HashSet<int>();

            for (int i = 0; i < 5; i++)
            {
                Assert.That(ids.Add(manager.Spawn(_walker, Vector2.zero)), Is.True);
            }
        }

        [Test]
        public void AFullHordeRefusesToGrow()
        {
            var manager = new ZombieManager(2);
            manager.Spawn(_walker, Vector2.zero);
            manager.Spawn(_walker, Vector2.zero);

            Assert.That(manager.IsFull, Is.True);
            Assert.That(manager.Spawn(_walker, Vector2.zero), Is.Zero, "no id means no room");
            Assert.That(manager.Count, Is.EqualTo(2));
        }

        [Test]
        public void TheyWalkTowardsTheirDestination()
        {
            var manager = new ZombieManager(10);
            manager.Spawn(_walker, Vector2.zero);
            manager.SetDestination(0, new Vector2(10f, 0f));

            manager.Tick(0.5f);

            Assert.That(manager.Get(0).Position.x, Is.EqualTo(1f).Within(0.0001f), "2 units/s for 0.5 s");
            Assert.That(manager.Get(0).Position.y, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void TheyStopOnArrivalInsteadOfOvershooting()
        {
            var manager = new ZombieManager(10);
            manager.Spawn(_walker, Vector2.zero);
            manager.SetDestination(0, new Vector2(0.5f, 0f));

            manager.Tick(10f);

            Assert.That(manager.Get(0).Position, Is.EqualTo(new Vector2(0.5f, 0f)));
            Assert.That(manager.Get(0).Velocity, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void FacingFollowsTheDirectionOfTravel()
        {
            var manager = new ZombieManager(10);
            manager.Spawn(_walker, Vector2.zero);

            manager.SetDestination(0, new Vector2(10f, 0f));
            manager.Tick(0.1f);
            Assert.That(manager.Get(0).Facing, Is.EqualTo(ZombieFacing.Right));

            manager.SetDestination(0, new Vector2(-10f, 0f));
            manager.Tick(0.1f);
            Assert.That(manager.Get(0).Facing, Is.EqualTo(ZombieFacing.Left));

            manager.SetDestination(0, new Vector2(manager.Get(0).Position.x, 10f));
            manager.Tick(0.1f);
            Assert.That(manager.Get(0).Facing, Is.EqualTo(ZombieFacing.Front), "straight up stays front");
        }

        [Test]
        public void SetDestinationForAll_TurnsTheWholeHorde()
        {
            var manager = new ZombieManager(10);
            for (int i = 0; i < 5; i++)
            {
                manager.Spawn(_walker, new Vector2(i, 0f));
            }

            manager.SetDestinationForAll(new Vector2(0f, 10f));
            manager.Tick(0.1f);

            for (int i = 0; i < manager.Count; i++)
            {
                Assert.That(manager.Get(i).Position.y, Is.GreaterThan(0f));
            }
        }

        [Test]
        public void DamageUsesTheDefinitionsResistances()
        {
            var manager = new ZombieManager(10);
            manager.Spawn(_walker, Vector2.zero);

            float applied = manager.Damage(0, new DamageInfo(10f, DamageType.Ballistic));

            Assert.That(applied, Is.EqualTo(5f), "ballistic is halved for this one");
            Assert.That(manager.Get(0).Health, Is.EqualTo(35f));
        }

        [Test]
        public void EnoughDamageKillsAndRemovesIt()
        {
            var manager = new ZombieManager(10);
            manager.Spawn(_walker, Vector2.zero);

            manager.Damage(0, new DamageInfo(100f));

            Assert.That(manager.Count, Is.Zero);
        }

        [Test]
        public void DeathIsAnnouncedWithTheZombiesId()
        {
            var manager = new ZombieManager(10);
            int id = manager.Spawn(_walker, Vector2.zero);
            var deaths = new List<DiedEvent>();
            manager.ZombieDied += deaths.Add;

            manager.Damage(0, new DamageInfo(100f, DamageType.Fire));

            Assert.That(deaths, Has.Count.EqualTo(1));
            Assert.That(deaths[0].TargetId, Is.EqualTo(id));
            Assert.That(deaths[0].KillingBlow.Type, Is.EqualTo(DamageType.Fire));
        }

        [Test]
        public void DeathsAlsoGoOutOnTheEventBus()
        {
            var bus = new EventBus();
            var deaths = new List<DiedEvent>();
            bus.Subscribe<DiedEvent>(deaths.Add);
            var manager = new ZombieManager(10, bus);
            manager.Spawn(_walker, Vector2.zero);

            manager.Kill(0);

            Assert.That(deaths, Has.Count.EqualTo(1));
        }

        [Test]
        public void KillingFromTheMiddleKeepsTheRestPacked()
        {
            var manager = new ZombieManager(10);
            int first = manager.Spawn(_walker, new Vector2(1f, 0f));
            int second = manager.Spawn(_walker, new Vector2(2f, 0f));
            int third = manager.Spawn(_walker, new Vector2(3f, 0f));

            manager.Kill(1);

            Assert.That(manager.Count, Is.EqualTo(2));
            Assert.That(manager.IndexOf(second), Is.EqualTo(-1), "the dead one is gone");
            Assert.That(manager.IndexOf(first), Is.EqualTo(0));
            Assert.That(manager.IndexOf(third), Is.EqualTo(1), "the last one moved into the hole");
            Assert.That(manager.Get(1).Position.x, Is.EqualTo(3f));
        }

        [Test]
        public void KillingTheLastOneNeedsNoSwap()
        {
            var manager = new ZombieManager(10);
            int first = manager.Spawn(_walker, new Vector2(1f, 0f));
            manager.Spawn(_walker, new Vector2(2f, 0f));

            manager.Kill(1);

            Assert.That(manager.Count, Is.EqualTo(1));
            Assert.That(manager.IndexOf(first), Is.EqualTo(0));
        }

        [Test]
        public void IndexOf_ReportsTheGoneAndTheNever()
        {
            var manager = new ZombieManager(10);

            Assert.That(manager.IndexOf(0), Is.EqualTo(-1));
            Assert.That(manager.IndexOf(999), Is.EqualTo(-1));
        }

        [Test]
        public void SpawnsAfterADeathReuseTheFreedSlot()
        {
            var manager = new ZombieManager(2);
            manager.Spawn(_walker, Vector2.zero);
            manager.Spawn(_walker, Vector2.zero);
            manager.Kill(0);

            Assert.That(manager.IsFull, Is.False);
            Assert.That(manager.Spawn(_walker, Vector2.zero), Is.GreaterThan(0));
            Assert.That(manager.Count, Is.EqualTo(2));
        }

        [Test]
        public void Clear_EmptiesTheZone()
        {
            var manager = new ZombieManager(10);
            for (int i = 0; i < 6; i++)
            {
                manager.Spawn(_walker, Vector2.zero);
            }

            manager.Clear();

            Assert.That(manager.Count, Is.Zero);
        }

        [Test]
        public void ReachingPastTheLiveRangeThrows()
        {
            var manager = new ZombieManager(10);
            manager.Spawn(_walker, Vector2.zero);

            Assert.Throws<ArgumentOutOfRangeException>(() => manager.Get(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => manager.SetDestination(5, Vector2.zero));
            Assert.That(manager.Kill(7), Is.False, "killing nothing is not an error");
        }

        [Test]
        public void ConstructorRejectsNonsense()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ZombieManager(0));
            Assert.Throws<ArgumentNullException>(
                () => new ZombieManager(1).Spawn(null!, Vector2.zero));
        }

        [Test]
        public void ThreeHundredZombiesTickWellInsideAFrame()
        {
            const int count = 300;
            var manager = new ZombieManager(count);
            for (int i = 0; i < count; i++)
            {
                manager.Spawn(_walker, new Vector2(i % 20, i / 20f));
            }

            manager.SetDestinationForAll(new Vector2(100f, 100f));

            // Warm up, so the first run's JIT does not land in the measurement.
            for (int i = 0; i < 50; i++)
            {
                manager.Tick(0.02f);
            }

            var watch = Stopwatch.StartNew();
            const int ticks = 500;
            for (int i = 0; i < ticks; i++)
            {
                manager.Tick(0.02f);
            }

            watch.Stop();
            double perTickMs = watch.Elapsed.TotalMilliseconds / ticks;
            TestContext.WriteLine($"{count} zombies: {perTickMs:F4} ms per tick");

            // A 60 fps frame is 16.7 ms. A generous ceiling, meant to catch a regression that
            // makes the horde cost real time, not to benchmark the machine.
            Assert.That(
                perTickMs,
                Is.LessThan(2.0),
                $"ticking {count} zombies took {perTickMs:F3} ms, which is no longer background noise");
        }
    }
}
