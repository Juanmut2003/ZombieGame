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
        [TestCase("Sprint")]
        public void TheGameplayMapHasTheAction(string actionName)
        {
            InputActionMap? map =
                _asset.FindActionMap(InputActionsGenerator.GameplayMap, throwIfNotFound: false);

            Assert.That(map, Is.Not.Null);
            Assert.That(map!.FindAction(actionName, throwIfNotFound: false), Is.Not.Null);
        }

        [TestCase("<Mouse>/leftButton")]
        [TestCase("<Mouse>/rightButton")]
        [TestCase("<Gamepad>/buttonWest")]
        public void SwingingIsBoundTo(string path)
        {
            InputAction attack = _asset
                .FindActionMap(InputActionsGenerator.GameplayMap, throwIfNotFound: true)
                .FindAction("Attack", throwIfNotFound: true);

            Assert.That(attack.bindings.Select(binding => binding.path), Contains.Item(path));
        }

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
