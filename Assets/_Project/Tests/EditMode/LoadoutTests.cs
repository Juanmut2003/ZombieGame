#nullable enable
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ZombieGame.Characters;
using ZombieGame.Combat;

namespace ZombieGame.Tests.EditMode
{
    /// <summary>Picking a weapon off the belt.</summary>
    /// <remarks>
    /// Small rules, but the ones a player feels every few seconds: the wheel has to run round the
    /// belt rather than stop at its ends, and the slot in hand has to be the weapon that the
    /// attack button then uses.
    /// </remarks>
    [TestFixture]
    public sealed class LoadoutTests
    {
        private GameObject _character = null!;
        private Loadout _loadout = null!;
        private WeaponDefinition _bat = null!;
        private WeaponDefinition _pistol = null!;

        [SetUp]
        public void CreateCharacter()
        {
            _bat = ScriptableObject.CreateInstance<WeaponDefinition>();
            _pistol = ScriptableObject.CreateInstance<WeaponDefinition>();

            var serializedPistol = new SerializedObject(_pistol);
            serializedPistol.FindProperty("_kind").enumValueIndex = (int)WeaponKind.Firearm;
            serializedPistol.FindProperty("_displayName").stringValue = "Pistol";
            serializedPistol.ApplyModifiedPropertiesWithoutUndo();

            _character = new GameObject("Character");
            _loadout = _character.AddComponent<Loadout>();

            var serialized = new SerializedObject(_loadout);
            SerializedProperty slots = serialized.FindProperty("_slots");
            slots.arraySize = 2;
            slots.GetArrayElementAtIndex(0).objectReferenceValue = _bat;
            slots.GetArrayElementAtIndex(1).objectReferenceValue = _pistol;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void DestroyCharacter()
        {
            Object.DestroyImmediate(_character);
            Object.DestroyImmediate(_bat);
            Object.DestroyImmediate(_pistol);
        }

        [Test]
        public void TheFirstSlotStartsInHand()
        {
            Assert.That(_loadout.SlotCount, Is.EqualTo(2));
            Assert.That(_loadout.Selected, Is.Zero);
            Assert.That(_loadout.Current, Is.EqualTo(_bat));
        }

        [Test]
        public void SelectingPutsAnotherWeaponInHand()
        {
            Assert.That(_loadout.Select(1), Is.True);
            Assert.That(_loadout.Current, Is.EqualTo(_pistol));
        }

        [Test]
        public void TheBeltWrapsRoundInsteadOfStopping()
        {
            _loadout.Select(2);
            Assert.That(_loadout.Selected, Is.Zero, "past the end comes back to the start");

            _loadout.Select(-1);
            Assert.That(_loadout.Selected, Is.EqualTo(1), "and before the start to the end");
        }

        [Test]
        public void AnEmptyBeltHasNothingToSelect()
        {
            var bare = new GameObject("Bare").AddComponent<Loadout>();
            try
            {
                Assert.That(bare.Select(0), Is.False);
                Assert.That(bare.Current, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(bare.gameObject);
            }
        }

        [Test]
        public void ReadingPastTheBeltIsNotAnError()
        {
            Assert.That(_loadout.At(5), Is.Null);
            Assert.That(_loadout.At(-1), Is.Null);
        }

        [Test]
        public void OnlyTheMeleeComponentAnswersForAMeleeSlot()
        {
            PlayerMelee melee = _character.AddComponent<PlayerMelee>();
            PlayerGun gun = _character.AddComponent<PlayerGun>();
            Wire(melee, "_loadout");
            Wire(gun, "_loadout");

            Assert.That(melee.Weapon, Is.EqualTo(_bat));
            Assert.That(gun.Weapon, Is.Null, "a bat is not a gun");

            _loadout.Select(1);

            Assert.That(melee.Weapon, Is.Null, "a gun is not a bat");
            Assert.That(gun.Weapon, Is.EqualTo(_pistol));
        }

        private void Wire(Component component, string field)
        {
            var serialized = new SerializedObject(component);
            serialized.FindProperty(field).objectReferenceValue = _loadout;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
