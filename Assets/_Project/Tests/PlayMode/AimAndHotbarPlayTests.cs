#nullable enable
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using ZombieGame.Characters;
using ZombieGame.Combat;
using ZombieGame.Core;
using ZombieGame.Core.Events;

namespace ZombieGame.Tests.PlayMode
{
    /// <summary>
    /// Pointing and picking: that an attack goes where the mouse is, and that the belt decides
    /// what the attack button does.
    /// </summary>
    /// <remarks>
    /// Neither can be checked without a running scene. Aiming is a screen position turned into a
    /// world direction by a camera, and the hotbar is two components agreeing about which one of
    /// them is holding the weapon.
    /// </remarks>
    [TestFixture]
    public sealed class AimAndHotbarPlayTests
    {
        private PlayerInputReader _input = null!;
        private PlayerAim _aim = null!;
        private PlayerGun _gun = null!;
        private PlayerMelee _melee = null!;
        private Loadout _loadout = null!;
        private ProjectileRunner _runner = null!;
        private Transform _player = null!;
        private Camera _camera = null!;

        [UnitySetUp]
        public IEnumerator LoadSandbox()
        {
            yield return SceneManager.LoadSceneAsync("Sandbox", LoadSceneMode.Single);
            yield return new WaitForFixedUpdate();

            GameObject? playerObject = GameObject.Find("MC");
            Assert.That(playerObject, Is.Not.Null);
            _player = playerObject!.transform;
            _input = playerObject.GetComponent<PlayerInputReader>();
            _aim = playerObject.GetComponent<PlayerAim>();
            _gun = playerObject.GetComponent<PlayerGun>();
            _melee = playerObject.GetComponent<PlayerMelee>();
            _loadout = playerObject.GetComponent<Loadout>();
            Assert.That(_aim, Is.Not.Null, "the MC should aim where the player points");

            _camera = Camera.main;
            Assert.That(_camera, Is.Not.Null);

            _runner = GameObject.Find("Projectiles").GetComponent<ProjectileRunner>();
        }

        /// <summary>Puts the pointer on a spot in the world and lets the aim catch up.</summary>
        private IEnumerator PointAt(Vector2 worldPoint)
        {
            _input.SetAimOverride(_camera.WorldToScreenPoint(worldPoint));
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheAimFollowsThePointer()
        {
            yield return PointAt((Vector2)_player.position + new Vector2(-4f, 0f));

            Assert.That(_aim.Direction.x, Is.LessThan(-0.8f), "it should be aiming left");

            yield return PointAt((Vector2)_player.position + new Vector2(0f, 4f));

            Assert.That(_aim.Direction.y, Is.GreaterThan(0.8f), "and now up");
        }

        [UnityTest]
        public IEnumerator AShotGoesWhereTheMouseIs()
        {
            _loadout.Select(1);
            yield return PointAt((Vector2)_player.position + new Vector2(0f, 5f));

            Assert.That(_gun.TryFire(), Is.True);

            Assert.That(_runner.Rounds.Count, Is.GreaterThan(0));
            Vector2 velocity = _runner.Rounds.Get(0).Velocity;

            // Upwards, not along the direction of travel, which is the whole point of aiming.
            Assert.That(velocity.y, Is.GreaterThan(0f));
            Assert.That(Mathf.Abs(velocity.x), Is.LessThan(velocity.y));
        }

        [UnityTest]
        public IEnumerator TheSwingGoesWhereTheMouseIsToo()
        {
            _loadout.Select(0);
            yield return PointAt((Vector2)_player.position + new Vector2(0f, -4f));

            Assert.That(GameServices.TryGet(out EventBus? bus), Is.True);

            var swings = new List<MeleeSwungEvent>();
            void Record(MeleeSwungEvent swing) => swings.Add(swing);
            bus!.Subscribe<MeleeSwungEvent>(Record);

            try
            {
                // Walking has not changed, so a bat that followed the body would swing elsewhere.
                Assert.That(_melee.TrySwing(), Is.True);
                yield return null;

                Assert.That(swings, Has.Count.EqualTo(1));
                Assert.That(swings[0].Facing.y, Is.LessThan(-0.8f), "it swung where the body walks");
            }
            finally
            {
                bus.Unsubscribe<MeleeSwungEvent>(Record);
            }
        }

        [UnityTest]
        public IEnumerator TheBeltDecidesWhatTheAttackButtonDoes()
        {
            _loadout.Select(0);
            yield return null;

            Assert.That(_melee.TrySwing(), Is.True, "slot one is the bat");
            Assert.That(_gun.TryFire(), Is.False, "and the gun is away");

            _loadout.Select(1);
            yield return null;

            Assert.That(_gun.TryFire(), Is.True, "slot two is the pistol");
            Assert.That(_melee.TrySwing(), Is.False, "and the bat is away");
        }

        [UnityTest]
        public IEnumerator TheHotbarShowsWhatIsInHand()
        {
            GameObject? slot = GameObject.Find("Hotbar Slot 2");
            Assert.That(slot, Is.Not.Null, "the HUD should carry a belt");

            Text label = slot!.GetComponentInChildren<Text>();
            Assert.That(label.text, Does.Contain("Pistol"));

            _loadout.Select(1);
            yield return null;

            GameObject first = GameObject.Find("Hotbar Slot 1");
            Color selected = slot.GetComponent<Image>().color;
            Color idle = first.GetComponent<Image>().color;

            Assert.That(selected, Is.Not.EqualTo(idle), "the slot in hand has to stand out");
        }
    }
}
