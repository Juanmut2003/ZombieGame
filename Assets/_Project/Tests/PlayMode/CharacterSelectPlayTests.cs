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

        /// <summary>
        /// The card for somebody who is not finished yet, found by asking rather than by
        /// counting.
        /// </summary>
        /// <remarks>
        /// By index, this test said nothing the day somebody was added to the roster ahead of
        /// the unfinished one: it went on passing while checking a card that was finished all
        /// along. Which seat they sit in is not what is being tested.
        /// </remarks>
        private static CharacterCardView UnfinishedCard()
        {
            CharacterCardView[] cards =
                Object.FindObjectsByType<CharacterCardView>(FindObjectsInactive.Exclude);

            foreach (CharacterCardView card in cards)
            {
                if (card.Character != null && !card.Character.Available)
                {
                    return card;
                }
            }

            Assert.Fail("the roster has nobody left to finish, so this test has nothing to say");
            return null!;
        }

        [UnityTest]
        public IEnumerator EveryCardHasSomebodyOnItAndNobodyTwice()
        {
            CharacterCardView[] cards =
                Object.FindObjectsByType<CharacterCardView>(FindObjectsInactive.Exclude);

            Assert.That(cards, Has.Length.GreaterThanOrEqualTo(2), "there is no choice to make");

            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (CharacterCardView card in cards)
            {
                Assert.That(card.Character, Is.Not.Null, $"{card.name} is wired to nobody");
                Assert.That(
                    seen.Add(card.Character!.DisplayName),
                    Is.True,
                    $"{card.Character.DisplayName} is on two cards");
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator TheDrifterIsPickedToBeginWith()
        {
            // Opening the menu and pressing start without touching anything has to do the
            // obvious thing, which is play the character the game is built around — the one the
            // colony scene puts on the plot, so that starting a run and walking about are the
            // same person.
            Assert.That(_select.Picked, Is.Not.Null);
            Assert.That(_select.Picked!.DisplayName, Is.EqualTo("Drifter"));
            Assert.That(_select.CanStart, Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheUnfinishedOneSaysSoAndCannotBePressed()
        {
            CharacterCardView card = UnfinishedCard();
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
            CharacterCardView card = UnfinishedCard();
            string before = _select.Picked == null ? string.Empty : _select.Picked.DisplayName;

            // Through the card's own event, which is the route a click would take even if the
            // button were left interactable by mistake.
            card.GetComponent<Button>().onClick.Invoke();
            yield return null;

            Assert.That(_select.Picked, Is.Not.Null);
            Assert.That(
                _select.Picked!.DisplayName,
                Is.EqualTo(before),
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
