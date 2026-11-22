#nullable enable
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using ZombieGame.Characters;

namespace ZombieGame.Tests.PlayMode
{
    /// <summary>
    /// That the MC walks rather than slides, and that walking does not move the collider.
    /// </summary>
    /// <remarks>
    /// Two halves of the same change. The sprite was moved onto a child so that it can be
    /// lifted a couple of pixels on every footfall; if that lift ever found its way back onto
    /// the object the capsule is on, the character would hop in and out of the walls they are
    /// standing against, which is a physics bug that looks like a rendering one.
    /// </remarks>
    [TestFixture]
    public sealed class GaitPlayTests
    {
        private GameObject _player = null!;
        private Transform _sprite = null!;
        private PlayerInputReader _input = null!;

        [UnitySetUp]
        public IEnumerator LoadTown()
        {
            yield return SceneManager.LoadSceneAsync("Mission_Town", LoadSceneMode.Single);
            yield return new WaitForFixedUpdate();

            GameObject? playerObject = GameObject.Find("MC");
            Assert.That(playerObject, Is.Not.Null);
            _player = playerObject!;

            Transform? body = _player.transform.Find("Body");
            Assert.That(body, Is.Not.Null, "the MC's sprite should hang off the MC, not sit on it");
            _sprite = body!;

            _input = _player.GetComponent<PlayerInputReader>();
            yield return null;
        }

        [TearDown]
        public void StopWalking() => _input.ClearInputOverride();

        [UnityTest]
        public IEnumerator ThePictureIsNotOnTheThingThatCollides()
        {
            Assert.That(
                _player.GetComponent<SpriteRenderer>(),
                Is.Null,
                "a renderer on the MC itself cannot be moved without moving the capsule");

            Assert.That(_sprite.GetComponent<SpriteRenderer>(), Is.Not.Null);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheMcIsTheSoldier()
        {
            var renderer = _sprite.GetComponent<SpriteRenderer>();
            Assert.That(renderer.sprite, Is.Not.Null, "the MC is invisible");
            Assert.That(
                renderer.sprite.name,
                Does.StartWith("soldier"),
                "the MC is wearing the fantasy pack again");

            var outfit = _player.GetComponent<CharacterOutfit>();
            Assert.That(outfit, Is.Not.Null, "the zone ignores who the player picked");
            Assert.That(outfit.Wearing, Is.Not.Null);

            yield return null;
        }

        [UnityTest]
        public IEnumerator WalkingSidewaysPlaysTheWalk()
        {
            var renderer = _sprite.GetComponent<SpriteRenderer>();
            var seen = new System.Collections.Generic.HashSet<string>();

            foreach (Vector2 way in new[] { Vector2.right, Vector2.left })
            {
                _input.SetInputOverride(way, sprint: false);
                for (int frame = 0; frame < 45; frame++)
                {
                    yield return null;
                    if (renderer.sprite != null)
                    {
                        seen.Add(renderer.sprite.name);
                    }
                }

                if (seen.Count > 1)
                {
                    break;
                }
            }

            // More than one drawing means the legs are actually moving rather than the whole
            // figure being slid along, which is what every character did before the soldier.
            Assert.That(
                seen.Count,
                Is.GreaterThan(1),
                $"the MC crossed the zone on one frame: {string.Join(", ", seen)}");
        }

        [UnityTest]
        public IEnumerator WalkingAwayShowsTheBack()
        {
            var renderer = _sprite.GetComponent<SpriteRenderer>();
            var facing = _player.GetComponent<CharacterFacing>();

            // Turned by hand rather than walked: which way is open depends on where in the town
            // the MC happens to start, and this is about the view, not the route.
            facing.Face(Vector2.up);
            yield return null;

            Assert.That(renderer.sprite, Is.Not.Null);
            Assert.That(
                renderer.sprite.name,
                Does.Contain("back"),
                "walking away from the camera still shows a face");
        }

        [UnityTest]
        public IEnumerator WalkingLiftsTheSprite()
        {
            float rest = _sprite.localPosition.y;
            float highest = rest;

            // Whichever way is actually open. A zone is full of walls, and a character leaning
            // on one is standing still however hard the test pushes.
            foreach (Vector2 way in new[] { Vector2.right, Vector2.left, Vector2.up, Vector2.down })
            {
                _input.SetInputOverride(way, sprint: false);

                for (int frame = 0; frame < 45; frame++)
                {
                    yield return null;
                    highest = Mathf.Max(highest, _sprite.localPosition.y);
                }

                if (highest > rest)
                {
                    break;
                }
            }

            Assert.That(
                highest,
                Is.GreaterThan(rest),
                "the MC crossed the zone without once picking a foot up");
        }

        [UnityTest]
        public IEnumerator WalkingLeavesTheColliderWhereItIs()
        {
            // Walking sideways, so the character's own y is not supposed to change at all and
            // any wobble in it is the gait leaking onto the body.
            float before = _player.transform.position.y;
            _input.SetInputOverride(Vector2.right, sprint: false);

            for (int frame = 0; frame < 45; frame++)
            {
                yield return null;
                Assert.That(
                    _player.transform.position.y,
                    Is.EqualTo(before).Within(0.002f),
                    "the MC is bobbing, capsule and all");
            }
        }

        [UnityTest]
        public IEnumerator StandingStillPutsTheFootBackDown()
        {
            float rest = _sprite.localPosition.y;

            _input.SetInputOverride(Vector2.right, sprint: false);
            for (int frame = 0; frame < 30; frame++)
            {
                yield return null;
            }

            _input.SetInputOverride(Vector2.zero, sprint: false);

            // Given time to come to a halt rather than one frame: the character slows into a
            // stop, so for a moment after the key is let go they are still walking, and still
            // ought to be.
            for (int frame = 0; frame < 60; frame++)
            {
                yield return null;
                if (Mathf.Approximately(_sprite.localPosition.y, rest))
                {
                    break;
                }
            }

            Assert.That(
                _sprite.localPosition.y,
                Is.EqualTo(rest).Within(0.0001f),
                "the MC stopped mid-stride and stayed up on one leg");
        }
    }
}
