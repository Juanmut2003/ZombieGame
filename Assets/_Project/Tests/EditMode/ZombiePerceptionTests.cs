#nullable enable
using NUnit.Framework;
using UnityEngine;
using ZombieGame.AI;

namespace ZombieGame.Tests.EditMode
{
    /// <summary>
    /// The horde's perception pass: who can see the target, and what a wall does about it.
    /// </summary>
    [TestFixture]
    public sealed class ZombiePerceptionTests
    {
        /// <summary>A wall across the world, for asking what sight does when something is in it.</summary>
        private sealed class BlockEverything : ILineOfSight
        {
            public bool IsClear(Vector2 from, Vector2 to) => false;
        }

        private ZombieManager _manager = null!;
        private ZombieDefinition _definition = null!;

        [SetUp]
        public void CreateHorde()
        {
            _definition = ScriptableObject.CreateInstance<ZombieDefinition>();
            _manager = new ZombieManager(capacity: 8, crowd: CrowdSettings.None);
        }

        [TearDown]
        public void DestroyDefinition() => Object.DestroyImmediate(_definition);

        /// <summary>Spawns one and points it in a direction, which spawning alone cannot do.</summary>
        private int SpawnLooking(Vector2 position, Vector2 look)
        {
            int id = _manager.Spawn(_definition, position);
            int index = _manager.IndexOf(id);

            // Walking a step is how a zombie ends up looking somewhere; the destination is the
            // only handle on that from outside.
            _manager.SetDestination(index, position + look.normalized);
            _manager.Tick(0.01f);
            return id;
        }

        [Test]
        public void AZombieLookingAtTheTargetSeesIt()
        {
            SpawnLooking(Vector2.zero, Vector2.right);

            Assert.That(_manager.Perceive(new Vector2(3f, 0f), 0.25f), Is.EqualTo(1));
            Assert.That(_manager.Get(0).SeesTarget, Is.True);
        }

        [Test]
        public void AZombieLookingAwaySeesNothing()
        {
            SpawnLooking(Vector2.zero, Vector2.left);

            Assert.That(_manager.Perceive(new Vector2(3f, 0f), 0.25f), Is.Zero);
            Assert.That(_manager.Get(0).SeesTarget, Is.False);
        }

        [Test]
        public void AWallStopsSight()
        {
            SpawnLooking(Vector2.zero, Vector2.right);

            Assert.That(
                _manager.Perceive(new Vector2(3f, 0f), 0.25f, new BlockEverything()),
                Is.Zero,
                "it is looking straight at it, through a wall");
        }

        [Test]
        public void LosingSightOfTheTargetClearsTheFlag()
        {
            SpawnLooking(Vector2.zero, Vector2.right);
            _manager.Perceive(new Vector2(3f, 0f), 0.25f);
            Assert.That(_manager.Get(0).SeesTarget, Is.True);

            // Walked off to somewhere out of range.
            _manager.Perceive(new Vector2(300f, 0f), 0.25f);

            Assert.That(_manager.Get(0).SeesTarget, Is.False);
        }

        [Test]
        public void OnlyTheOnesFacingTheRightWaySeeIt()
        {
            SpawnLooking(Vector2.zero, Vector2.right);
            SpawnLooking(new Vector2(0f, 1f), Vector2.left);
            SpawnLooking(new Vector2(0f, -1f), Vector2.right);

            Assert.That(_manager.Perceive(new Vector2(4f, 0f), 0.25f), Is.EqualTo(2));
        }

        [Test]
        public void ANoiseNearbyIsHeardByEveryone()
        {
            SpawnLooking(Vector2.zero, Vector2.right);
            SpawnLooking(new Vector2(0f, 1f), Vector2.left);

            // Hearing has no facing: the one looking away hears it just as well.
            Assert.That(_manager.CountHearing(new Vector2(1f, 0f), loudness: 1f), Is.EqualTo(2));
        }

        [Test]
        public void ANoiseFarAwayIsHeardByNobody()
        {
            SpawnLooking(Vector2.zero, Vector2.right);

            Assert.That(_manager.CountHearing(new Vector2(200f, 0f), loudness: 1f), Is.Zero);
        }

        [Test]
        public void ALouderNoiseReachesFurther()
        {
            SpawnLooking(Vector2.zero, Vector2.right);
            ZombieSenses senses = _manager.SensesOf(0);
            var justOutOfEarshot = new Vector2(senses.HearingRadius + 1f, 0f);

            Assert.That(_manager.CountHearing(justOutOfEarshot, loudness: 1f), Is.Zero);
            Assert.That(_manager.CountHearing(justOutOfEarshot, loudness: 3f), Is.EqualTo(1));
        }

        [Test]
        public void AZombieTurnsTowardsWhatItHeard()
        {
            SpawnLooking(Vector2.zero, Vector2.right);
            var behind = new Vector2(-3f, 0f);

            Assert.That(_manager.Perceive(behind, 0.25f), Is.Zero, "it is looking the other way");

            _manager.HearNoise(behind, loudness: 1f);

            // The point of hearing: a noise behind a zombie moves its cone, and the thing that
            // made the noise is now in it.
            Assert.That(_manager.Perceive(behind, 0.25f), Is.EqualTo(1));
        }

        [Test]
        public void ANoiseItCannotHearLeavesItLookingWhereItWas()
        {
            SpawnLooking(Vector2.zero, Vector2.right);
            Vector2 look = _manager.Get(0).Look;

            _manager.HearNoise(new Vector2(-200f, 0f), loudness: 1f);

            Assert.That(_manager.Get(0).Look, Is.EqualTo(look));
        }

        [Test]
        public void ItGoesBackToLookingWhereItWalksOnceItStopsListening()
        {
            int id = SpawnLooking(Vector2.zero, Vector2.right);
            int index = _manager.IndexOf(id);
            _manager.HearNoise(new Vector2(0f, 3f), loudness: 1f);

            // Still listening: walking does not pull its eyes back yet.
            _manager.SetDestination(index, new Vector2(10f, 0f));
            _manager.Tick(0.1f);
            Assert.That(_manager.Get(index).Look.y, Is.GreaterThan(0.5f));

            // Long enough for any sensible attention span to run out.
            _manager.Tick(_manager.SensesOf(index).ListenSeconds + 0.1f);
            _manager.Tick(0.1f);

            Assert.That(_manager.Get(index).Look.x, Is.GreaterThan(0.9f));
        }

        [Test]
        public void ADeafZombieHearsNothing()
        {
            var deaf = ScriptableObject.CreateInstance<ZombieDefinition>();
            try
            {
                var serialized = new UnityEditor.SerializedObject(deaf);
                serialized.FindProperty("_hearingRadius").floatValue = 0f;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                _manager.Spawn(deaf, Vector2.zero);

                Assert.That(_manager.HearNoise(new Vector2(0.1f, 0f), loudness: 10f), Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(deaf);
            }
        }

        [Test]
        public void PerceptionSurvivesADeathInTheMiddleOfTheArray()
        {
            SpawnLooking(Vector2.zero, Vector2.right);
            int doomed = SpawnLooking(new Vector2(0f, 1f), Vector2.right);
            SpawnLooking(new Vector2(0f, -1f), Vector2.right);

            // Killing swaps the last zombie into the hole; its senses have to follow it, or the
            // survivors end up perceiving with a dead one's eyes.
            _manager.Kill(_manager.IndexOf(doomed));

            Assert.That(_manager.Count, Is.EqualTo(2));
            Assert.That(_manager.Perceive(new Vector2(4f, 0f), 0.25f), Is.EqualTo(2));
        }
    }
}
