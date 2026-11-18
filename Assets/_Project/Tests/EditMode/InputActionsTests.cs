#nullable enable
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using ZombieGame.Editor.Input;

namespace ZombieGame.Tests.EditMode
{
    /// <summary>
    /// Pins down the bindings the generated actions asset has to carry.
    /// </summary>
    /// <remarks>
    /// Bindings are the one part of the project a player notices immediately and no other test
    /// touches: a swing bound to a button nobody presses looks exactly like broken combat.
    /// </remarks>
    [TestFixture]
    public sealed class InputActionsTests
    {
        private InputActionAsset _asset = null!;

        [SetUp]
        public void BuildAsset() => _asset = InputActionsGenerator.Build();

        [TearDown]
        public void DestroyAsset() => Object.DestroyImmediate(_asset);

        [TestCase("Move")]
        [TestCase("Attack")]
        [TestCase("Fire")]
        [TestCase("Reload")]
        [TestCase("Sprint")]
        public void TheGameplayMapHasTheAction(string actionName)
        {
            InputActionMap? map =
                _asset.FindActionMap(InputActionsGenerator.GameplayMap, throwIfNotFound: false);

            Assert.That(map, Is.Not.Null);
            Assert.That(map!.FindAction(actionName, throwIfNotFound: false), Is.Not.Null);
        }

        [TestCase("<Mouse>/rightButton")]
        [TestCase("<Gamepad>/buttonWest")]
        public void SwingingIsBoundTo(string path) =>
            Assert.That(PathsOf("Attack"), Contains.Item(path));

        [TestCase("<Mouse>/leftButton")]
        [TestCase("<Gamepad>/rightTrigger")]
        public void FiringIsBoundTo(string path) =>
            Assert.That(PathsOf("Fire"), Contains.Item(path));

        [TestCase("<Keyboard>/r")]
        public void ReloadingIsBoundTo(string path) =>
            Assert.That(PathsOf("Reload"), Contains.Item(path));

        [Test]
        public void TheTwoWeaponsDoNotShareAButton()
        {
            // The one rule the bindings have to keep: pulling the trigger must never also swing
            // the bat, or every shot would be followed by a free melee attack.
            Assert.That(PathsOf("Attack"), Has.No.Member("<Mouse>/leftButton"));
            Assert.That(PathsOf("Fire"), Has.No.Member("<Mouse>/rightButton"));
        }

        private string[] PathsOf(string actionName) => _asset
            .FindActionMap(InputActionsGenerator.GameplayMap, throwIfNotFound: true)
            .FindAction(actionName, throwIfNotFound: true)
            .bindings
            .Select(binding => binding.path)
            .ToArray();

        [Test]
        public void MovingIsBoundToBothKeyboardLayoutsAndTheStick()
        {
            InputAction move = _asset
                .FindActionMap(InputActionsGenerator.GameplayMap, throwIfNotFound: true)
                .FindAction("Move", throwIfNotFound: true);

            string[] paths = move.bindings.Select(binding => binding.path).ToArray();

            Assert.That(paths, Contains.Item("<Keyboard>/w"));
            Assert.That(paths, Contains.Item("<Keyboard>/upArrow"));
            Assert.That(paths, Contains.Item("<Gamepad>/leftStick"));
        }
    }
}
