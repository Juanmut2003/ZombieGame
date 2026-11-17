#nullable enable
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ZombieGame.AI;
using ZombieGame.Combat;
using ZombieGame.Core.Config;

namespace ZombieGame.Tests.EditMode
{
    /// <summary>
    /// Checks the content assets the game actually ships with.
    /// </summary>
    /// <remarks>
    /// Every other test builds its own numbers and so says nothing about the game as configured.
    /// These open the real assets instead, because the usual way for a data-driven game to break
    /// is an asset that was renamed, emptied or never tuned — none of which any amount of
    /// well-tested code would notice.
    /// They assert ranges and relationships, never exact values: pinning the damage of the bat to
    /// a number would mean a failing test every time somebody does their job and tunes it.
    /// </remarks>
    [TestFixture]
    public sealed class ContentAssetTests
    {
        private const string BalancePath = "Assets/_Project/Data/BalanceConfig.asset";
        private const string WalkerPath = "Assets/_Project/Data/Zombies/Walker.asset";
        private const string StumblerPath = "Assets/_Project/Data/Zombies/Stumbler.asset";
        private const string BatPath = "Assets/_Project/Data/Weapons/Bat.asset";

        private static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.That(asset, Is.Not.Null, $"'{path}' is missing; the game has no content there");
            return asset!;
        }

        /// <summary>
        /// Every zombie asset in the project, found rather than listed.
        /// </summary>
        /// <remarks>
        /// A list would have to be kept up to date by whoever adds a kind, which is exactly the
        /// moment an untuned asset slips through. Finding them means a new kind is tested from
        /// the minute it exists.
        /// </remarks>
        private static string[] EveryZombie()
        {
            string[] guids = AssetDatabase.FindAssets(
                "t:ZombieDefinition", new[] { "Assets/_Project/Data/Zombies" });
            var paths = new string[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                paths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
            }

            Assert.That(paths, Is.Not.Empty, "a game with no zombies in it");
            return paths;
        }

        [TestCaseSource(nameof(EveryZombie))]
        public void AZombieAssetIsFilledIn(string path)
        {
            ZombieDefinition zombie = Load<ZombieDefinition>(path);

            Assert.That(zombie.DisplayName, Is.Not.Empty);
            Assert.That(zombie.MaxHealth, Is.GreaterThan(0f));
            Assert.That(zombie.MoveSpeed, Is.GreaterThan(0f));
            Assert.That(zombie.BodyRadius, Is.GreaterThan(0f));
            Assert.That(zombie.SpriteFor(ZombieFacing.Front), Is.Not.Null, "it needs a body");
            Assert.That(zombie.SpriteFor(ZombieFacing.Left), Is.Not.Null);
            Assert.That(zombie.SpriteFor(ZombieFacing.Right), Is.Not.Null);
        }

        [TestCaseSource(nameof(EveryZombie))]
        public void AZombieCanNoticeSomething(string path)
        {
            ZombieDefinition zombie = Load<ZombieDefinition>(path);
            ZombieSenses senses = zombie.Senses;

            Assert.That(senses.ViewDistance, Is.GreaterThan(0f), "a blind zombie never reacts");
            Assert.That(senses.ViewHalfAngleDegrees, Is.GreaterThan(0f));
            Assert.That(
                senses.ViewHalfAngleDegrees,
                Is.LessThan(180f),
                "eyes in the back of its head would make sneaking pointless");
            Assert.That(senses.HearingRadius, Is.GreaterThan(0f));
        }

        [TestCaseSource(nameof(EveryZombie))]
        public void AZombieLooksDifferentOnceItHasNoticedYou(string path)
        {
            ZombieDefinition zombie = Load<ZombieDefinition>(path);

            // Being seen has to be visible, or the first sign of it is a zombie arriving.
            Assert.That(
                zombie.SpriteFor(ZombieFacing.Front, alerted: true),
                Is.Not.EqualTo(zombie.SpriteFor(ZombieFacing.Front, alerted: false)));
            Assert.That(
                zombie.SpriteFor(ZombieFacing.Right, alerted: true),
                Is.Not.EqualTo(zombie.SpriteFor(ZombieFacing.Right, alerted: false)));
        }

        [TestCaseSource(nameof(EveryZombie))]
        public void AZombieIsSlowerThanTheCharacterCanWalk(string path)
        {
            ZombieDefinition zombie = Load<ZombieDefinition>(path);
            BalanceConfig balance = Load<BalanceConfig>(BalancePath);

            // The whole shape of the game depends on this: walking away has to be possible, or
            // every encounter is a fight. Runners, when they arrive, break it on purpose.
            Assert.That(zombie.MoveSpeed, Is.LessThan(balance.WalkSpeed));
        }

        [Test]
        public void TheTwoKindsOfZombieAreActuallyDifferent()
        {
            ZombieDefinition walker = Load<ZombieDefinition>(WalkerPath);
            ZombieDefinition stumbler = Load<ZombieDefinition>(StumblerPath);

            Assert.That(
                walker.MaxHealth, Is.Not.EqualTo(stumbler.MaxHealth).Within(0.001f),
                "two types with the same numbers are one type with two sprites");
            Assert.That(walker.MoveSpeed, Is.Not.EqualTo(stumbler.MoveSpeed).Within(0.001f));
        }

        [Test]
        public void TheBatIsFilledIn()
        {
            WeaponDefinition bat = Load<WeaponDefinition>(BatPath);

            Assert.That(bat.DisplayName, Is.Not.Empty);
            Assert.That(bat.Damage, Is.GreaterThan(0f));
            Assert.That(bat.Range, Is.GreaterThan(0f));
            Assert.That(bat.HalfAngleDegrees, Is.InRange(5f, 179f), "a swing is not a full circle");
            Assert.That(bat.MaxTargets, Is.GreaterThanOrEqualTo(1));
            Assert.That(bat.CooldownSeconds, Is.GreaterThan(0f));
        }

        [Test]
        public void SwingingTheBatCostsStaminaButNotAllOfIt()
        {
            WeaponDefinition bat = Load<WeaponDefinition>(BatPath);
            BalanceConfig balance = Load<BalanceConfig>(BalancePath);

            Assert.That(bat.StaminaCost, Is.GreaterThan(0f), "a free swing is a held button");
            Assert.That(
                bat.StaminaCost,
                Is.LessThan(balance.StaminaMax * 0.5f),
                "a rested character should manage more than one swing");
        }

        [TestCaseSource(nameof(EveryZombie))]
        public void TheBatKillsAZombieInAFewSwings(string path)
        {
            WeaponDefinition bat = Load<WeaponDefinition>(BatPath);
            ZombieDefinition zombie = Load<ZombieDefinition>(path);

            float perSwing = zombie.Resistances.Apply(bat.CreateDamage(attackerId: 1));
            Assert.That(perSwing, Is.GreaterThan(0f), "a bat that does nothing is not a weapon");

            int swings = Mathf.CeilToInt(zombie.MaxHealth / perSwing);

            // The relationship between two assets, which neither one can state on its own: one
            // swing and the horde is harmless, ten and the bat is a prop.
            Assert.That(
                swings,
                Is.InRange(2, 6),
                $"{zombie.DisplayName} takes {swings} swings of the {bat.DisplayName}");
        }

        [Test]
        public void TheCrowdCellIsWideEnoughForTheWidestZombie()
        {
            BalanceConfig balance = Load<BalanceConfig>(BalancePath);

            float widest = 0f;
            foreach (string path in EveryZombie())
            {
                widest = Mathf.Max(widest, Load<ZombieDefinition>(path).BodyRadius);
            }

            // A bucket narrower than a body lets a neighbour sit outside the nine cells the
            // separation search looks at, and the crowd starts merging again in exactly the way
            // that took an afternoon to find the first time.
            Assert.That(balance.ZombieCrowdCellSize, Is.GreaterThanOrEqualTo(widest * 2f));
        }
    }
}
