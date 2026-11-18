#nullable enable
using System;
using NUnit.Framework;
using UnityEngine;
using ZombieGame.AI;
using ZombieGame.Core.Physics;

namespace ZombieGame.Tests.EditMode
{
    /// <summary>
    /// What a zombie does when it is not simply being walked at the player: shuffle, look into
    /// something, chase, and give up.
    /// </summary>
    /// <remarks>
    /// These are the rules a player reads off the screen and plans around, so they are worth
    /// pinning down exactly. "It mostly stops following eventually" is not a stealth mechanic.
    /// </remarks>
    [TestFixture]
    public sealed class ZombieBehaviourTests
    {
        private ZombieManager _manager = null!;
        private ZombieDefinition _definition = null!;

        [SetUp]
        public void CreateHorde()
        {
            _definition = ScriptableObject.CreateInstance<ZombieDefinition>();
            _manager = new ZombieManager(capacity: 8, crowd: CrowdSettings.None, seed: 4242);
        }

        [TearDown]
        public void DestroyDefinition() => UnityEngine.Object.DestroyImmediate(_definition);

        /// <summary>Spawns one and points it in a direction, which spawning alone cannot do.</summary>
        private int SpawnLooking(Vector2 position, Vector2 look)
        {
            int id = _manager.Spawn(_definition, position);
            int index = _manager.IndexOf(id);
            _manager.SetDestination(index, position + look.normalized);
            _manager.Tick(0.01f);
            return index;
        }

        /// <summary>One round of the loop the director runs: perceive, decide, move.</summary>
        private void Step(Vector2 target, float seconds, bool visible = true)
        {
            _manager.Perceive(visible ? target : new Vector2(9999f, 9999f), 0.25f);
            _manager.Decide(target, seconds);
            _manager.Tick(seconds);
        }

        [Test]
        public void AFreshZombieIsNotChasingAnything()
        {
            int index = SpawnLooking(Vector2.zero, Vector2.right);

            Assert.That(_manager.Get(index).Behaviour, Is.Not.EqualTo(ZombieBehaviour.Chasing));
        }

        [Test]
        public void AnUndisturbedZombieWandersNearWhereItStarted()
        {
            var home = new Vector2(5f, 5f);
            int index = SpawnLooking(home, Vector2.right);
            float radius = _definition.Instincts.WanderRadius;

            for (int i = 0; i < 400; i++)
            {
                Step(new Vector2(-500f, -500f), 0.05f, visible: false);

                Assert.That(
                    Vector2.Distance(_manager.Get(index).Position, home),
                    Is.LessThanOrEqualTo(radius + 0.5f),
                    "a zombie nobody disturbed should stay around its own patch");
            }
        }

        [Test]
        public void AnUndisturbedZombieActuallyMoves()
        {
            int index = SpawnLooking(Vector2.zero, Vector2.right);
            Vector2 start = _manager.Get(index).Position;

            for (int i = 0; i < 200; i++)
            {
                Step(new Vector2(-500f, -500f), 0.05f, visible: false);
            }

            // A zone of statues is worse than a zone of nothing: it reads as unfinished.
            Assert.That(Vector2.Distance(_manager.Get(index).Position, start), Is.GreaterThan(0.5f));
        }

        [Test]
        public void SeeingTheTargetStartsAChase()
        {
            int index = SpawnLooking(Vector2.zero, Vector2.right);
            var target = new Vector2(3f, 0f);

            Step(target, 0.1f);

            Assert.That(_manager.Get(index).Behaviour, Is.EqualTo(ZombieBehaviour.Chasing));
            Assert.That(_manager.Get(index).Destination, Is.EqualTo(target));
        }

        [Test]
        public void AChaseIsFasterThanAShuffle()
        {
            int index = SpawnLooking(Vector2.zero, Vector2.right);

            float shuffled = 0f;
            for (int i = 0; i < 40; i++)
            {
                Vector2 before = _manager.Get(index).Position;
                Step(new Vector2(-500f, -500f), 0.05f, visible: false);
                shuffled += Vector2.Distance(_manager.Get(index).Position, before);
            }

            float chased = 0f;
            for (int i = 0; i < 40; i++)
            {
                ZombieState zombie = _manager.Get(index);

                // Kept three units ahead of whatever it is looking at, so it is always visible
                // and never arrives — the distance covered is then the speed of the chase.
                Vector2 target = zombie.Position + (zombie.Look.normalized * 3f);
                Step(target, 0.05f);
                chased += Vector2.Distance(_manager.Get(index).Position, zombie.Position);
            }

            Assert.That(_manager.Get(index).Behaviour, Is.EqualTo(ZombieBehaviour.Chasing));
            Assert.That(chased, Is.GreaterThan(shuffled));
        }

        [Test]
        public void LosingSightSendsItToWhereYouWere()
        {
            int index = SpawnLooking(Vector2.zero, Vector2.right);
            var lastSeen = new Vector2(3f, 0f);

            Step(lastSeen, 0.1f);
            Step(new Vector2(3f, 9f), 0.1f, visible: false);

            // It walks at the place you were, not the place you are. That gap is the whole
            // payoff of breaking line of sight.
            Assert.That(_manager.Get(index).Destination, Is.EqualTo(lastSeen));
            Assert.That(_manager.Get(index).Behaviour, Is.EqualTo(ZombieBehaviour.Chasing));
        }

        [Test]
        public void ItGivesUpEventually()
        {
            int index = SpawnLooking(Vector2.zero, Vector2.right);
            Step(new Vector2(3f, 0f), 0.1f);
            Assert.That(_manager.Get(index).Behaviour, Is.EqualTo(ZombieBehaviour.Chasing));

            float patience = _definition.Instincts.GiveUpSeconds;
            for (float t = 0f; t < patience + 1f; t += 0.1f)
            {
                Step(new Vector2(500f, 500f), 0.1f, visible: false);
            }

            Assert.That(
                _manager.Get(index).Behaviour,
                Is.Not.EqualTo(ZombieBehaviour.Chasing),
                "a zombie that never gives up makes hiding pointless");
        }

        [Test]
        public void ANoiseIsWorthLookingInto()
        {
            int index = SpawnLooking(Vector2.zero, Vector2.right);
            var noise = new Vector2(0f, 3f);

            _manager.HearNoise(noise, loudness: 1f);
            _manager.Decide(new Vector2(-500f, -500f), 0.1f);

            Assert.That(_manager.Get(index).Behaviour, Is.EqualTo(ZombieBehaviour.Investigating));
            Assert.That(_manager.Get(index).Destination, Is.EqualTo(noise));
        }

        [Test]
        public void ItWalksAllTheWayToTheNoise()
        {
            int index = SpawnLooking(Vector2.zero, Vector2.right);
            var noise = new Vector2(0f, 2f);
            _manager.HearNoise(noise, loudness: 1f);

            for (int i = 0; i < 40; i++)
            {
                Step(new Vector2(-500f, -500f), 0.05f, visible: false);
            }

            Assert.That(Vector2.Distance(_manager.Get(index).Position, noise), Is.LessThan(1f));
        }

        [Test]
        public void SomethingItCanSeeBeatsSomethingItHeard()
        {
            int index = SpawnLooking(Vector2.zero, Vector2.right);
            var target = new Vector2(3f, 0f);

            Step(target, 0.1f);
            _manager.HearNoise(new Vector2(0f, -3f), loudness: 1f);

            // A noise behind it is no reason to stop chasing the thing in front of it.
            Assert.That(_manager.Get(index).Behaviour, Is.EqualTo(ZombieBehaviour.Chasing));
        }

        [Test]
        public void GivingUpLeavesItWhereItEndedUp()
        {
            int index = SpawnLooking(Vector2.zero, Vector2.right);
            var noise = new Vector2(0f, 6f);
            _manager.HearNoise(noise, loudness: 4f);

            for (int i = 0; i < 400; i++)
            {
                Step(new Vector2(-500f, -500f), 0.05f, visible: false);
            }

            // Drawn across the zone and left there: the patch it shuffles around afterwards is
            // the one it walked to, not the one it was spawned in. That is what makes a thrown
            // bottle worth carrying.
            Assert.That(_manager.Get(index).Home.y, Is.GreaterThan(3f));
        }

        [Test]
        public void InstinctsRefuseNonsense()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ZombieInstincts(-1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ZombieInstincts(3f, -1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ZombieInstincts(3f, 1f, -1f));

            // Faster while bored than while hunting would be a zone that calms down into a sprint.
            Assert.Throws<ArgumentOutOfRangeException>(() => new ZombieInstincts(3f, 1f, 1f, 1.5f));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new ZombieInstincts(3f, 1f, 1f, 0.5f, 0f));
        }

        [Test]
        public void AChaseGoesRoundAWallInsteadOfIntoIt()
        {
            // A wall between the zombie and the player, open at the top. Walking at the player
            // means walking into bricks, and that is exactly what used to happen.
            var walls = new WallGrid(Vector2.zero, 12, 8);
            for (int row = 0; row < 6; row++)
            {
                walls.SetSolid(6, row);
            }

            var routes = new GridFlowField(Vector2.zero, 12, 8);
            routes.ReadWalls(walls);

            var player = new Vector2(9.5f, 1.5f);
            routes.Rebuild(player);

            // Looking at the player to begin with, so this is a test about walking rather than
            // about whether it noticed.
            int index = SpawnLooking(new Vector2(3.5f, 1.5f), Vector2.right);

            for (int i = 0; i < 600; i++)
            {
                _manager.Perceive(player, 0.25f);
                _manager.Decide(player, 0.05f);
                _manager.Tick(0.05f, walls, routes);
            }

            // Not "it arrived" — a chase is also a giving-up timer, and the point here is only
            // that it got past the wall rather than standing against it for twenty seconds.
            Assert.That(
                _manager.Get(index).Position.x,
                Is.GreaterThan(6.5f),
                "it should have come round the wall, not stopped at it");
        }

        [Test]
        public void AKindThatDoesNotStrayStaysPut()
        {
            var still = ScriptableObject.CreateInstance<ZombieDefinition>();
            try
            {
                var serialized = new UnityEditor.SerializedObject(still);
                serialized.FindProperty("_wanderRadius").floatValue = 0f;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                int id = _manager.Spawn(still, new Vector2(2f, 2f));
                int index = _manager.IndexOf(id);

                for (int i = 0; i < 200; i++)
                {
                    Step(new Vector2(-500f, -500f), 0.05f, visible: false);
                }

                Assert.That(
                    Vector2.Distance(_manager.Get(index).Position, new Vector2(2f, 2f)),
                    Is.LessThan(0.1f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(still);
            }
        }
    }
}
