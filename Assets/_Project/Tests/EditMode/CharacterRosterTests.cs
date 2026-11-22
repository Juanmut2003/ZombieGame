#nullable enable
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ZombieGame.Core.Roster;

namespace ZombieGame.Tests.EditMode
{
    /// <summary>
    /// The roster: who is on it, who may be picked, and what they have to walk with.
    /// </summary>
    /// <remarks>
    /// The menu is built from these assets, so a roster that quietly loses its frames is a menu
    /// that still looks right and a character who stands still all game. The one who is not
    /// finished matters just as much: a card marked as to come has to actually refuse to be
    /// picked, or the player starts a run as somebody with no art.
    /// </remarks>
    [TestFixture]
    public sealed class CharacterRosterTests
    {
        private const string Folder = "Assets/_Project/Data/Characters";

        private static PlayableCharacter Load(string name)
        {
            var character = AssetDatabase.LoadAssetAtPath<PlayableCharacter>($"{Folder}/{name}.asset");
            Assert.That(
                character,
                Is.Not.Null,
                $"'{name}' is missing — run ZombieGame/Content/Build Character Roster");
            return character!;
        }

        [Test]
        public void TheSoldierCanBePlayed()
        {
            PlayableCharacter soldier = Load("Soldier");

            Assert.That(soldier.Available, Is.True);
            Assert.That(soldier.CanBeDrawn, Is.True);
            Assert.That(soldier.DisplayName, Is.Not.Empty);
        }

        [Test]
        public void TheSoldierHasAWalkAndNotJustAPose()
        {
            PlayableCharacter soldier = Load("Soldier");

            // The side views are the ones the sheets actually animate. If these ever drop to a
            // single drawing, the soldier is sliding again and nothing else would say so.
            Assert.That(soldier.Left.Length, Is.GreaterThan(1), "the left walk is one pose");
            Assert.That(soldier.Right.Length, Is.GreaterThan(1), "the right walk is one pose");
            Assert.That(soldier.Left.Length, Is.EqualTo(soldier.Right.Length), "lopsided walk");
        }

        [Test]
        public void TheSoldierFacesEveryWay()
        {
            PlayableCharacter soldier = Load("Soldier");

            Assert.That(soldier.Front.Length, Is.GreaterThan(0));
            Assert.That(soldier.Back.Length, Is.GreaterThan(0), "walking up shows the front view");
            Assert.That(soldier.Left.Length, Is.GreaterThan(0));
            Assert.That(soldier.Right.Length, Is.GreaterThan(0));
        }

        [Test]
        public void EveryFrameOfEveryWalkIsThere()
        {
            foreach (string name in new[] { "Soldier", "Archer" })
            {
                PlayableCharacter character = Load(name);
                foreach (Sprite[] view in new[]
                         { character.Front, character.Back, character.Left, character.Right })
                {
                    foreach (Sprite frame in view)
                    {
                        Assert.That(frame, Is.Not.Null, $"{name} has a hole in a walk");
                    }
                }
            }
        }

        [Test]
        public void TheArcherIsOnTheRosterButNotYetPlayable()
        {
            PlayableCharacter archer = Load("Archer");

            Assert.That(
                archer.Available,
                Is.False,
                "the archer has no walk of their own yet");
        }

        [Test]
        public void SomebodyUnfinishedCannotBeChosen()
        {
            var selection = new CharacterSelection();

            Assert.That(selection.Choose(Load("Archer")), Is.False);
            Assert.That(selection.HasChoice, Is.False);

            Assert.That(selection.Choose(Load("Soldier")), Is.True);
            Assert.That(selection.Chosen, Is.EqualTo(Load("Soldier")));
        }

        [Test]
        public void ChoosingNobodyLeavesTheChoiceAlone()
        {
            var selection = new CharacterSelection();
            Assert.That(selection.Choose(Load("Soldier")), Is.True);

            // A card with nothing on it must not clear a good choice.
            Assert.That(selection.Choose(null), Is.False);
            Assert.That(selection.HasChoice, Is.True);

            selection.Clear();
            Assert.That(selection.HasChoice, Is.False);
        }
    }
}
