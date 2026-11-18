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
        [TestCase("Reload")]
        [TestCase("Sprint")]
        [TestCase("Aim")]
        [TestCase("AimStick")]
        [TestCase("Slot1")]
        [TestCase("Slot2")]
        [TestCase("Slot3")]
        [TestCase("CycleWeapon")]
        public void TheGameplayMapHasTheAction(string actionName)
        {
            InputActionMap? map =
                _asset.FindActionMap(InputActionsGenerator.GameplayMap, throwIfNotFound: false);

            Assert.That(map, Is.Not.Null);
            Assert.That(map!.FindAction(actionName, throwIfNotFound: false), Is.Not.Null);
        }

        [TestCase("<Mouse>/leftButton")]
        [TestCase("<Gamepad>/rightTrigger")]
        [TestCase("<Gamepad>/buttonWest")]
        public void AttackingIsBoundTo(string path) =>
            Assert.That(PathsOf("Attack"), Contains.Item(path));

        [TestCase("<Keyboard>/r")]
        public void ReloadingIsBoundTo(string path) =>
            Assert.That(PathsOf("Reload"), Contains.Item(path));

        [TestCase("<Keyboard>/1", "Slot1")]
        [TestCase("<Keyboard>/2", "Slot2")]
        [TestCase("<Keyboard>/3", "Slot3")]
        [TestCase("<Mouse>/scroll/y", "CycleWeapon")]
        public void TheHotbarIsBoundTo(string path, string action) =>
            Assert.That(PathsOf(action), Contains.Item(path));

        [Test]
        public void AimingReadsThePointer() =>
            Assert.That(PathsOf("Aim"), Contains.Item("<Mouse>/position"));

        [Test]
        public void EveryWeaponSharesOneAttackButton()
        {
            // The rule the bindings now keep: there is exactly one way to attack, and which
            // weapon it uses is the hotbar's business. A second attack action would have to be
            // rebound for every weapon ever added.
            InputActionMap map =
                _asset.FindActionMap(InputActionsGenerator.GameplayMap, throwIfNotFound: true);

            Assert.That(map.FindAction("Fire", throwIfNotFound: false), Is.Null);
            Assert.That(PathsOf("Attack"), Contains.Item("<Mouse>/leftButton"));
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
