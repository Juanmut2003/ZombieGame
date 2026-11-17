#nullable enable
using System;
using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using ZombieGame.AI;

namespace ZombieGame.Tests.EditMode
{
    /// <summary>
    /// Covers the thing that turns a horde back into a crowd: that they do not end up standing
    /// inside one another.
    /// </summary>
    [TestFixture]
    public sealed class ZombieCrowdTests
    {
        private ZombieDefinition _walker = null!;

        [SetUp]
        public void CreateDefinition()
        {
            _walker = ScriptableObject.CreateInstance<ZombieDefinition>();
            var serialized = new UnityEditor.SerializedObject(_walker);
            serialized.FindProperty("_maxHealth").floatValue = 40f;
            serialized.FindProperty("_moveSpeed").floatValue = 2f;
            serialized.FindProperty("_bodyRadius").floatValue = 0.3f;
            serialized.FindProperty("_speedVariance").floatValue = 0f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void DestroyDefinition() => UnityEngine.Object.DestroyImmediate(_walker);

        private static float ClosestPair(ZombieManager manager)
        {
            float closest = float.MaxValue;
            ReadOnlySpan<ZombieState> states = manager.States;

            for (int i = 0; i < states.Length; i++)
            {
                for (int j = i + 1; j < states.Length; j++)
                {
                    closest = Mathf.Min(
                        closest, Vector2.Distance(states[i].Position, states[j].Position));
                }
            }

            return closest;
        }

        [Test]
        public void AHordeWalkingAtOnePointDoesNotMergeIntoOneBody()
        {
            var manager = new ZombieManager(20, crowd: CrowdSettings.Default);
            for (int i = 0; i < 12; i++)
            {
                manager.Spawn(_walker, new Vector2(-4f + (i * 0.7f), (i % 3) - 1f));
            }

            // Everyone walks at the same spot for long enough to arrive and pile up.
            manager.SetDestinationForAll(Vector2.zero);
            for (int step = 0; step < 400; step++)
            {
                manager.Tick(0.02f);
            }

            Assert.That(
                ClosestPair(manager),
                Is.GreaterThan(0.15f),
                "they should be jostling around the spot, not standing in each other");
        }

        [Test]
        public void WithoutSeparationTheyDoMergeIntoOneBody()
        {
            var manager = new ZombieManager(20, crowd: CrowdSettings.None);
            for (int i = 0; i < 12; i++)
            {
                manager.Spawn(_walker, new Vector2(-4f + (i * 0.7f), (i % 3) - 1f));
            }

            manager.SetDestinationForAll(Vector2.zero);
            for (int step = 0; step < 400; step++)
            {
                manager.Tick(0.02f);
            }

            Assert.That(
                ClosestPair(manager),
                Is.LessThan(0.01f),
                "this is the behaviour separation exists to fix, pinned down so it stays fixed");
        }

        [Test]
        public void TwoZombiesOnTheExactSameSpotPushApart()
        {
            var manager = new ZombieManager(4, crowd: CrowdSettings.Default);
            manager.Spawn(_walker, Vector2.zero);
            manager.Spawn(_walker, Vector2.zero);
            manager.SetDestinationForAll(Vector2.zero);

            for (int step = 0; step < 100; step++)
            {
                manager.Tick(0.02f);
            }

            Assert.That(
                Vector2.Distance(manager.Get(0).Position, manager.Get(1).Position),
                Is.GreaterThan(0.1f),
                "a perfect overlap has no natural direction to part in, and still has to resolve");
        }

        [Test]
        public void ZombiesFarApartAreLeftAlone()
        {
            var manager = new ZombieManager(4, crowd: CrowdSettings.Default);
            manager.Spawn(_walker, Vector2.zero);
            manager.Spawn(_walker, new Vector2(20f, 0f));
            manager.SetDestination(0, Vector2.zero);
            manager.SetDestination(1, new Vector2(20f, 0f));

            manager.Tick(0.02f);

            Assert.That(manager.Get(0).Position, Is.EqualTo(Vector2.zero));
            Assert.That(manager.Get(1).Position, Is.EqualTo(new Vector2(20f, 0f)));
        }

        [Test]
        public void BeingCrowdedNeverMakesAZombieFasterThanItsWalk()
        {
            var manager = new ZombieManager(40, crowd: CrowdSettings.Default);
            for (int i = 0; i < 30; i++)
            {
                manager.Spawn(_walker, new Vector2(i * 0.05f, 0f));
            }

            manager.SetDestinationForAll(Vector2.zero);

            for (int step = 0; step < 50; step++)
            {
                manager.Tick(0.02f);

                ReadOnlySpan<ZombieState> states = manager.States;
                for (int i = 0; i < states.Length; i++)
                {
                    Assert.That(
                        states[i].Velocity.magnitude,
                        Is.LessThanOrEqualTo(states[i].Speed + 0.0001f),
                        "a shove is not a sprint");
                }
            }
        }

        [Test]
        public void SpeedVariesBetweenIndividualsWithinTheTypesSpread()
        {
            var varied = ScriptableObject.CreateInstance<ZombieDefinition>();
            try
            {
                var serialized = new UnityEditor.SerializedObject(varied);
                serialized.FindProperty("_moveSpeed").floatValue = 2f;
                serialized.FindProperty("_speedVariance").floatValue = 0.25f;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                var manager = new ZombieManager(50);
                for (int i = 0; i < 50; i++)
                {
                    manager.Spawn(varied, Vector2.zero);
                }

                float min = float.MaxValue;
                float max = float.MinValue;
                ReadOnlySpan<ZombieState> states = manager.States;
                for (int i = 0; i < states.Length; i++)
                {
                    min = Mathf.Min(min, states[i].Speed);
                    max = Mathf.Max(max, states[i].Speed);
                }

                Assert.That(max, Is.GreaterThan(min), "identical speeds keep a horde in lockstep");
                Assert.That(min, Is.GreaterThanOrEqualTo(1.5f - 0.0001f), "2.0 less a quarter");
                Assert.That(max, Is.LessThanOrEqualTo(2.5f + 0.0001f), "2.0 plus a quarter");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(varied);
            }
        }

        [Test]
        public void TheSameSeedLaysOutTheSameHorde()
        {
            float[] First() => SpeedsOf(new ZombieManager(10, seed: 7));
            float[] Second() => SpeedsOf(new ZombieManager(10, seed: 7));

            Assert.That(Second(), Is.EqualTo(First()), "a bug in a horde has to be reproducible");
        }

        private float[] SpeedsOf(ZombieManager manager)
        {
            var varied = ScriptableObject.CreateInstance<ZombieDefinition>();
            try
            {
                var serialized = new UnityEditor.SerializedObject(varied);
                serialized.FindProperty("_speedVariance").floatValue = 0.4f;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                var speeds = new float[10];
                for (int i = 0; i < speeds.Length; i++)
                {
                    manager.Spawn(varied, Vector2.zero);
                    speeds[i] = manager.Get(i).Speed;
                }

                return speeds;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(varied);
            }
        }

        [Test]
        public void SeparatingThreeHundredZombiesStillFitsInAFrame()
        {
            const int count = 300;
            var manager = new ZombieManager(count, crowd: CrowdSettings.Default);

            // Packed tightly on purpose: this is the worst case, the whole horde in one place.
            for (int i = 0; i < count; i++)
            {
                manager.Spawn(_walker, new Vector2((i % 20) * 0.4f, (i / 20) * 0.4f));
            }

            manager.SetDestinationForAll(new Vector2(4f, 3f));

            for (int i = 0; i < 50; i++)
            {
                manager.Tick(0.02f);
            }

            var watch = Stopwatch.StartNew();
            const int ticks = 300;
            for (int i = 0; i < ticks; i++)
            {
                manager.Tick(0.02f);
            }

            watch.Stop();
            double perTickMs = watch.Elapsed.TotalMilliseconds / ticks;
            TestContext.WriteLine($"{count} zombies with separation: {perTickMs:F4} ms per tick");

            // The ceiling is half a frame, not a target. This is the pathological case — the whole
            // horde overlapping inside a few tiles, every one of them shoving eight neighbours —
            // and a horde spread across a zone costs an order of magnitude less. If this ever
            // becomes the frame's most expensive line, the dictionary is what to replace: a flat
            // grid of cells indexed by arithmetic would drop the hashing entirely.
            Assert.That(
                perTickMs,
                Is.LessThan(8.0),
                $"separating {count} packed zombies took {perTickMs:F3} ms of a 16.7 ms frame");
        }

        [Test]
        public void CrowdSettingsRejectNonsense()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CrowdSettings(-1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CrowdSettings(1f, cellSize: 0f));
        }
    }
}
