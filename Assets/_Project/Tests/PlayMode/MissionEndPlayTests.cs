#nullable enable
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using ZombieGame.Characters;
using ZombieGame.Combat;
using ZombieGame.UI;

namespace ZombieGame.Tests.PlayMode
{
    /// <summary>
    /// What happens on the frame a run ends.
    /// </summary>
    /// <remarks>
    /// A lost run used to say one line at the top of the HUD and leave the player walking about
    /// in a zone that had stopped counting, apparently still playing, until the menu loaded a few
    /// seconds later. Dying has to look like dying at the moment it happens, and the controls
    /// have to stop with it.
    /// </remarks>
    [TestFixture]
    public sealed class MissionEndPlayTests
    {
        private Transform _player = null!;

        [UnitySetUp]
        public IEnumerator LoadTown()
        {
            yield return SceneManager.LoadSceneAsync("Mission_Town", LoadSceneMode.Single);
            yield return new WaitForFixedUpdate();

            GameObject? playerObject = GameObject.Find("MC");
            Assert.That(playerObject, Is.Not.Null);
            _player = playerObject!.transform;

            yield return null;
        }

        private static MissionResultPanel Panel()
        {
            GameObject? presenter = GameObject.Find("Mission Result Presenter");
            Assert.That(presenter, Is.Not.Null, "the zone should carry a result screen");
            return presenter!.GetComponent<MissionResultPanel>();
        }

        [UnityTest]
        public IEnumerator NothingIsShownWhileTheRunIsOn()
        {
            Assert.That(Panel().IsShowing, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DyingPutsTheResultUpAtOnce()
        {
            MissionResultPanel panel = Panel();

            _player.GetComponent<CharacterVitals>()!.TakeDamage(new DamageInfo(9999f));
            yield return null;

            Assert.That(
                panel.IsShowing,
                Is.True,
                "the result has to be up on the frame the squad goes down");
        }

        [UnityTest]
        public IEnumerator DyingTakesTheControlsAway()
        {
            var standDown = _player.GetComponent<SquadStandDown>();
            Assert.That(standDown, Is.Not.Null, "the squad should stop taking orders when it dies");

            _player.GetComponent<CharacterVitals>()!.TakeDamage(new DamageInfo(9999f));
            yield return null;

            Assert.That(standDown!.HasStoodDown, Is.True);

            // And the character is actually standing still, not merely told to.
            var input = _player.GetComponent<PlayerInputReader>();
            yield return null;

            Assert.That(input.MoveInput, Is.EqualTo(Vector2.zero));
        }
    }
}
