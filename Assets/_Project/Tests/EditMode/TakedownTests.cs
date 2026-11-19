#nullable enable
using NUnit.Framework;
using UnityEngine;
using ZombieGame.AI;

namespace ZombieGame.Tests.EditMode
{
    /// <summary>
    /// Who can be crept up on, and who cannot.
    /// </summary>
    /// <remarks>
    /// A silent takedown is the payoff for crouching: without it, sneaking only ever postpones a
    /// fight. But it is also the move most likely to turn into a free answer to being surrounded,
    /// so the three conditions that hold it back — close enough, behind it, and it has not
    /// noticed you — are rules the player will learn by feel and therefore worth stating exactly.
    /// </remarks>
    [TestFixture]
    public sealed class TakedownTests
    {
        private const float Reach = 0.95f;
        private const float Behind = 75f;

        private ZombieManager _manager = null!;
        private ZombieDefinition _definition = null!;

        [SetUp]
        public void CreateHorde()
        {
            _definition = ScriptableObject.CreateInstance<ZombieDefinition>();
            _manager = new ZombieManager(capacity: 8, crowd: CrowdSettings.None, seed: 77);
        }

        [TearDown]
        public void DestroyDefinition() => UnityEngine.Object.DestroyImmediate(_definition);

        /// <summary>Spawns one standing at a spot and looking a given way.</summary>
        private int Spawn(Vector2 at, Vector2 look)
        {
            int id = _manager.Spawn(_definition, at);
            int index = _manager.IndexOf(id);

            _manager.SetDestination(index, at + look.normalized);
            _manager.Tick(0.05f);
            return index;
        }

        [Test]
        public void OneFacingAwayCanBeTakenDown()
        {
            // Looking right, attacker stands to its left: squarely behind it.
            Spawn(Vector2.zero, Vector2.right);

            Assert.That(
                _manager.FindTakedown(new Vector2(-0.6f, 0f), Reach, Behind),
                Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void OneLookingAtYouCannotBe()
        {
            Spawn(Vector2.zero, Vector2.left);

            Assert.That(_manager.FindTakedown(new Vector2(-0.6f, 0f), Reach, Behind), Is.EqualTo(-1));
        }

        [Test]
        public void StandingBesideItIsNotBehindIt()
        {
            // The move is a knife in the back, not a tap on the shoulder.
            Spawn(Vector2.zero, Vector2.right);

            Assert.That(_manager.FindTakedown(new Vector2(0f, 0.6f), Reach, Behind), Is.EqualTo(-1));
        }

        [Test]
        public void TooFarAwayIsTooFarAway()
        {
            Spawn(Vector2.zero, Vector2.right);

            Assert.That(_manager.FindTakedown(new Vector2(-4f, 0f), Reach, Behind), Is.EqualTo(-1));
        }

        [Test]
        public void SomethingAlreadyHuntingYouCannotBeCreptUpOn()
        {
            // Seen first, from in front of it, so that it is actually hunting.
            var player = new Vector2(0f, -0.6f);
            Spawn(Vector2.zero, Vector2.down);

            for (int i = 0; i < 10; i++)
            {
                _manager.Perceive(player, 0.25f);
                _manager.Decide(player, 0.05f);
            }

            Assert.That(_manager.Get(0).Behaviour, Is.EqualTo(ZombieBehaviour.Chasing));

            // Now asked for from squarely behind it. The rule that stops a takedown being the
            // answer to a fight: once it is coming for you, getting round the back is not enough.
            Assert.That(
                _manager.FindTakedown(new Vector2(0f, 0.6f), Reach, Behind),
                Is.EqualTo(-1),
                "something already hunting you cannot be crept up on, wherever you stand");
        }

        [Test]
        public void TheNearestOneIsTheOneYouGet()
        {
            Spawn(new Vector2(0.8f, 0f), Vector2.right);
            int near = Spawn(new Vector2(0.4f, 0f), Vector2.right);

            Assert.That(_manager.FindTakedown(Vector2.zero, Reach, Behind), Is.EqualTo(near));
        }

        [Test]
        public void TheHordeGathersRoundYouRatherThanInsideYou()
        {
            // What the player sees when this is missing: a crowd closing in ends as one pile of
            // bodies on a single tile with the character somewhere underneath it.
            var player = new Vector2(5f, 5f);
            for (int i = 0; i < 6; i++)
            {
                _manager.Spawn(_definition, player + new Vector2(0.01f * i, 0f));
            }

            for (int step = 0; step < 60; step++)
            {
                _manager.Perceive(player, 0.25f);
                _manager.Decide(player, 0.05f);
                _manager.Tick(0.05f);
            }

            for (int i = 0; i < 6; i++)
            {
                ZombieState zombie = _manager.Get(i);
                Assert.That(
                    Vector2.Distance(zombie.Position, player),
                    Is.GreaterThan(0.2f),
                    $"zombie {i} is standing in the squad: {zombie.Behaviour}, "
                    + $"at {zombie.Position}, heading for {zombie.Destination}");
            }
        }

        [Test]
        public void AnEmptyZoneOffersNobody()
        {
            Assert.That(_manager.FindTakedown(Vector2.zero, Reach, Behind), Is.EqualTo(-1));
        }

        [Test]
        public void ANarrowerArcIsHarderToStandIn()
        {
            // The angle is a balance number, and it has to actually do something: the same spot
            // that counts as behind at a wide arc must stop counting at a narrow one.
            Spawn(Vector2.zero, Vector2.right);
            var diagonal = new Vector2(-0.45f, 0.45f);

            Assert.That(_manager.FindTakedown(diagonal, Reach, 120f), Is.GreaterThanOrEqualTo(0));
            Assert.That(_manager.FindTakedown(diagonal, Reach, 20f), Is.EqualTo(-1));
        }
    }
}
