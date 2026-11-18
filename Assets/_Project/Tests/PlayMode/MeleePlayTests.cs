#nullable enable
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using ZombieGame.AI;
using ZombieGame.Characters;

namespace ZombieGame.Tests.PlayMode
{
    /// <summary>
    /// Swings the bat in the real scene and checks that the horde feels it.
    /// </summary>
    /// <remarks>
    /// The arc maths and the stamina rules are covered by edit-mode tests. What only play mode
    /// shows is that the chain actually connects: input to weapon to event bus to the manager that
    /// owns the zombies — four pieces that each work alone.
    /// </remarks>
    [TestFixture]
    public sealed class MeleePlayTests
    {
        private ZombieDirector _director = null!;
        private PlayerMelee _melee = null!;
        private CharacterFacing _facing = null!;
        private CharacterStamina _stamina = null!;
        private Transform _player = null!;

        [UnitySetUp]
        public IEnumerator LoadSandbox()
        {
            yield return SceneManager.LoadSceneAsync("Sandbox", LoadSceneMode.Single);
            yield return new WaitForFixedUpdate();

            GameObject? playerObject = GameObject.Find("MC");
            Assert.That(playerObject, Is.Not.Null);
            _player = playerObject!.transform;
            _melee = playerObject.GetComponent<PlayerMelee>();
            _facing = playerObject.GetComponent<CharacterFacing>();
            _stamina = playerObject.GetComponent<CharacterStamina>();
            Assert.That(_melee, Is.Not.Null, "the MC should be armed");
            Assert.That(_stamina, Is.Not.Null, "and should have stamina");

            _director = GameObject.Find("Zombie Director").GetComponent<ZombieDirector>();
        }

        [UnityTest]
        public IEnumerator ASwingHurtsAZombieStandingInFrontOfIt()
        {
            ZombieState target = _director.Manager.Get(0);

            // Put the victim right in front of the player and turn to face it.
            Vector2 spot = (Vector2)_player.position + new Vector2(0.8f, 0f);
            MoveZombieTo(target.Id, spot);
            PointRight();

            float healthBefore = FindHealth(target.Id);
            _melee.TrySwing();
            yield return null;

            Assert.That(
                FindHealth(target.Id),
                Is.LessThan(healthBefore),
                "a zombie an arm's length away should have been hit");
        }

        [UnityTest]
        public IEnumerator ASwingMissesWhatIsBehindTheCharacter()
        {
            ZombieState target = _director.Manager.Get(0);
            MoveZombieTo(target.Id, (Vector2)_player.position + new Vector2(-0.8f, 0f));
            PointRight();

            float healthBefore = FindHealth(target.Id);
            _melee.TrySwing();
            yield return null;

            Assert.That(FindHealth(target.Id), Is.EqualTo(healthBefore));
        }

        [UnityTest]
        public IEnumerator TheWeaponHasToRecoverBetweenSwings()
        {
            PointRight();

            Assert.That(_melee.TrySwing(), Is.True);
            Assert.That(_melee.TrySwing(), Is.False, "no machine-gunning the bat");
            Assert.That(_melee.IsOnCooldown, Is.True);

            yield return new WaitForSeconds(0.6f);

            Assert.That(_melee.IsOnCooldown, Is.False);
            Assert.That(_melee.TrySwing(), Is.True);
        }

        [UnityTest]
        public IEnumerator TheSwingIsVisible()
        {
            GameObject? arcObject = GameObject.Find("Arc");
            Assert.That(arcObject, Is.Not.Null, "the scene should carry a swing arc");

            var arc = arcObject!.GetComponent<SpriteRenderer>();
            Assert.That(arc.sprite, Is.Not.Null, "and it needs a sprite to show");
            Assert.That(arc.enabled, Is.False, "nothing should be drawn before a swing");

            PointRight();
            Assert.That(_melee.TrySwing(), Is.True);
            yield return null;

            Assert.That(arc.enabled, Is.True, "a swing the player cannot see is a swing that failed");

            // Well past the flash, which is a fraction of a second.
            yield return new WaitForSeconds(0.5f);

            Assert.That(arc.enabled, Is.False, "and it should not linger");
        }

        [UnityTest]
        public IEnumerator SwingingCostsStamina()
        {
            float before = _stamina.Current;

            _melee.TrySwing();
            yield return null;

            Assert.That(_stamina.Current, Is.LessThan(before));
        }

        [UnityTest]
        public IEnumerator ExhaustionStopsTheSwinging()
        {
            // Drain it dry, then try to swing on an empty tank.
            while (_stamina.Current > 0.01f)
            {
                _stamina.Drain(1000f, 1f);
            }

            yield return null;

            Assert.That(_melee.TrySwing(), Is.False, "a swing has to be paid for");
        }

        /// <summary>
        /// Puts the pointer to the right of the character, which is how a character is turned
        /// now that aiming follows the mouse rather than the direction of travel.
        /// </summary>
        private void PointRight()
        {
            Camera camera = Camera.main;
            Vector2 spot = (Vector2)_player.position + new Vector2(3f, 0f);
            _player.GetComponent<PlayerInputReader>().SetAimOverride(camera.WorldToScreenPoint(spot));
        }

        private void MoveZombieTo(int id, Vector2 position)
        {
            int index = _director.Manager.IndexOf(id);
            Assert.That(index, Is.GreaterThanOrEqualTo(0));

            // Walking it there would take time and might kill it on the way; this is a test
            // setting up a situation, not a zombie deciding something.
            _director.Manager.Teleport(index, position);
        }

        private float FindHealth(int id)
        {
            int index = _director.Manager.IndexOf(id);
            return index < 0 ? 0f : _director.Manager.Get(index).Health;
        }
    }
}
