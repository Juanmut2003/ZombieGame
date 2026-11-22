#nullable enable
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using ZombieGame.UI;

namespace ZombieGame.Tests.PlayMode
{
    /// <summary>
    /// The menu's character select, as it is actually built.
    /// </summary>
    /// <remarks>
    /// Built from assets by a script, so the failure to guard against is a card wired to
    /// nothing: a menu that looks finished, with a start button that begins a run as nobody.
    /// </remarks>
    [TestFixture]
    public sealed class CharacterSelectPlayTests
    {
        private CharacterSelectPresenter _select = null!;

        [UnitySetUp]
        public IEnumerator LoadMenu()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            yield return null;

            GameObject? presenter = GameObject.Find("Character Select");
            Assert.That(presenter, Is.Not.Null, "the menu should offer a choice of character");
            _select = presenter!.GetComponent<CharacterSelectPresenter>();

            yield return null;
        }

        private static CharacterCardView Card(int index)
        {
            GameObject? card = GameObject.Find($"Character Card {index}");
            Assert.That(card, Is.Not.Null, $"card {index} is missing");
            return card!.GetComponent<CharacterCardView>();
        }

        [UnityTest]
        public IEnumerator BothCharactersAreOnTheCards()
        {
            Assert.That(Card(0).Character, Is.Not.Null);
            Assert.That(Card(1).Character, Is.Not.Null);
            Assert.That(Card(0).Character, Is.Not.EqualTo(Card(1).Character));
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheSoldierIsPickedToBeginWith()
        {
            // Opening the menu and pressing start without touching anything has to do the
            // obvious thing, which is play the one character who is finished.
            Assert.That(_select.Picked, Is.Not.Null);
            Assert.That(_select.Picked!.DisplayName, Is.EqualTo("Soldier"));
            Assert.That(_select.CanStart, Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheUnfinishedOneSaysSoAndCannotBePressed()
        {
            CharacterCardView card = Card(1);
            Assert.That(card.IsPickable, Is.False);

            var button = card.GetComponent<Button>();
            Assert.That(button, Is.Not.Null);
            Assert.That(button.interactable, Is.False, "a card nobody can have is still clickable");

            GameObject? blurb = GameObject.Find("Blurb");
            Assert.That(blurb, Is.Not.Null);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PressingTheUnfinishedCardChangesNothing()
        {
            CharacterCardView card = Card(1);

            // Through the card's own event, which is the route a click would take even if the
            // button were left interactable by mistake.
            card.GetComponent<Button>().onClick.Invoke();
            yield return null;

            Assert.That(_select.Picked, Is.Not.Null);
            Assert.That(
                _select.Picked!.DisplayName,
                Is.EqualTo("Soldier"),
                "the run would have started as somebody with no walk");
        }

        [UnityTest]
        public IEnumerator StartIsThereAndAlive()
        {
            GameObject? startObject = GameObject.Find("New Run Button");
            Assert.That(startObject, Is.Not.Null, "there is no way into the game");

            var start = startObject!.GetComponent<Button>();
            Assert.That(start.interactable, Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheMenuCanTakeAClick()
        {
            Assert.That(
                Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>(),
                Is.Not.Null,
                "without one, every card and button here is a picture");

            yield return null;
        }
    }
}
