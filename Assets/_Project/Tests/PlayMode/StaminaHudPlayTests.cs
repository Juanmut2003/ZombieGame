#nullable enable
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using ZombieGame.Characters;
using ZombieGame.UI;

namespace ZombieGame.Tests.PlayMode
{
    /// <summary>
    /// Checks that the stamina bar on the HUD actually follows the player's stamina.
    /// </summary>
    /// <remarks>
    /// Edit-mode tests cover the stamina maths. What only the running scene shows is the chain
    /// from a character spending stamina, through the event bus, to a rectangle changing width —
    /// and a bar that never moves is indistinguishable from a bar that is simply always full.
    /// </remarks>
    [TestFixture]
    public sealed class StaminaHudPlayTests
    {
        private CharacterStamina _stamina = null!;
        private PlayerMelee _melee = null!;
        private RectTransform _fill = null!;

        [UnitySetUp]
        public IEnumerator LoadSandbox()
        {
            yield return SceneManager.LoadSceneAsync("Sandbox", LoadSceneMode.Single);

            GameObject? playerObject = GameObject.Find("MC");
            Assert.That(playerObject, Is.Not.Null);
            _stamina = playerObject!.GetComponent<CharacterStamina>();
            _melee = playerObject.GetComponent<PlayerMelee>();

            GameObject? presenterObject = GameObject.Find("Stamina Bar Presenter");
            Assert.That(presenterObject, Is.Not.Null, "the HUD should carry a stamina bar");
            Assert.That(
                presenterObject!.GetComponent<StaminaBarPresenter>(),
                Is.Not.Null);

            GameObject? fillObject = GameObject.Find("Stamina Bar Fill");
            Assert.That(fillObject, Is.Not.Null, "the bar should have a part that fills it");
            _fill = (RectTransform)fillObject!.transform;

            // The first frame is when the character announces its stamina and the bar first draws.
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheBarStartsFull()
        {
            Assert.That(_fill.anchorMax.x, Is.EqualTo(1f).Within(0.001f));
            yield break;
        }

        [UnityTest]
        public IEnumerator TheBarShrinksWhenStaminaIsSpent()
        {
            float widthBefore = _fill.anchorMax.x;

            Assert.That(_melee.TrySwing(), Is.True, "the swing has to happen for the test to mean anything");

            // One frame for the character to notice the change and publish it.
            yield return null;

            Assert.That(_fill.anchorMax.x, Is.LessThan(widthBefore));
            Assert.That(
                _fill.anchorMax.x,
                Is.EqualTo(_stamina.Fraction).Within(0.02f),
                "the bar should show the stamina that is actually left");
        }

        [UnityTest]
        public IEnumerator TheBarFillsBackUp()
        {
            _melee.TrySwing();
            yield return null;

            float drained = _fill.anchorMax.x;
            Assert.That(drained, Is.LessThan(1f));

            // Long enough to clear the regeneration delay and claw some of it back.
            yield return new WaitForSeconds(1.4f);

            Assert.That(_fill.anchorMax.x, Is.GreaterThan(drained));
        }
    }
}
