#nullable enable
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using ZombieGame.Characters;
using ZombieGame.Combat;
using ZombieGame.Missions;
using ZombieGame.UI;

namespace ZombieGame.Tests.PlayMode
{
    /// <summary>
    /// The debrief that comes up when a run ends, and who decides when it goes away.
    /// </summary>
    /// <remarks>
    /// The zone used to leave for the menu on a four-second timer, which is either an age or
    /// nowhere near enough depending on whether the player is counting supplies or staring at
    /// the spot where they died. Now the screen says it is up and the zone waits. The part
    /// worth pinning down is the handshake: if the countdown is not actually called off, the
    /// debrief still appears and is still pulled away mid-sentence, and the only way to notice
    /// is to sit and read one.
    /// </remarks>
    [TestFixture]
    public sealed class DebriefPlayTests
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
            Assert.That(presenter, Is.Not.Null, "the zone should carry a debrief");
            return presenter!.GetComponent<MissionResultPanel>();
        }

        private static Text Find(string name)
        {
            GameObject? found = GameObject.Find(name);
            Assert.That(found, Is.Not.Null, $"the debrief should have a '{name}'");
            return found!.GetComponent<Text>();
        }

        private IEnumerator Die()
        {
            _player.GetComponent<CharacterVitals>()!.TakeDamage(new DamageInfo(9999f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheDebriefIsItemised()
        {
            yield return Die();

            Assert.That(Panel().IsShowing, Is.True);

            Assert.That(Find("Verdict").text, Is.Not.Empty, "a debrief with no verdict");
            Assert.That(Find("Summary").text, Is.Not.Empty, "a debrief that says nothing in words");

            string labels = Find("Debrief Labels").text;
            Assert.That(labels, Does.Contain("SUPPLIES"));
            Assert.That(labels, Does.Contain("TIME"));
            Assert.That(labels, Does.Contain("ZONE"));

            // The columns are drawn as two blocks side by side, so they have to agree on how
            // many lines there are or every figure sits against the wrong name.
            Assert.That(
                Find("Debrief Values").text.Split('\n').Length,
                Is.EqualTo(labels.Split('\n').Length),
                "the two columns of the debrief are out of step");
        }

        [UnityTest]
        public IEnumerator TheZoneWaitsForThePlayerToReadIt()
        {
            var flow = Object.FindAnyObjectByType<MissionEndFlow>();
            Assert.That(flow, Is.Not.Null, "the zone should know how to send the squad home");

            yield return Die();

            // Several frames, which is where a four-second countdown would still be running.
            for (int frame = 0; frame < 10; frame++)
            {
                yield return null;
                Assert.That(
                    flow!.IsLeaving,
                    Is.False,
                    "the zone started counting down over the top of the debrief");
            }

            Assert.That(flow!.IsWaitingOnTheDebrief, Is.True);
            Assert.That(Panel().IsShowing, Is.True, "the debrief was taken away unread");
        }

        [UnityTest]
        public IEnumerator TheButtonIsWhatEndsIt()
        {
            yield return Die();

            GameObject? buttonObject = GameObject.Find("Debrief Continue");
            Assert.That(buttonObject, Is.Not.Null, "there should be a way out of the debrief");

            var button = buttonObject!.GetComponent<Button>();
            Assert.That(button, Is.Not.Null);

            // Through the button's own event rather than by calling the panel: the wiring
            // between the two is the part that can be left out of a rebuilt scene.
            button.onClick.Invoke();
            yield return null;

            Assert.That(Panel().IsShowing, Is.False, "the debrief would not go away");

            // Letting go of the debrief sends the squad home, so the zone is on its way out.
            // Waited for here rather than left running, or it lands in the middle of the next
            // test's setup and tears the zone down around it.
            for (int frame = 0; frame < 300; frame++)
            {
                if (SceneManager.GetActiveScene().name != "Mission_Town")
                {
                    break;
                }

                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator TheZoneCanTakeTheClick()
        {
            // The debrief is the first thing the player must click to finish a run. A zone
            // without an event system would hang on a screen that cannot be dismissed.
            Assert.That(
                Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>(),
                Is.Not.Null);

            // And exactly one: two of them deliver clicks about half the time each.
            Assert.That(
                Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>().Length,
                Is.EqualTo(1));

            yield return null;
        }
    }
}
