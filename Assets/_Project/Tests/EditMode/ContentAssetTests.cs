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
        private const string PistolPath = "Assets/_Project/Data/Weapons/Pistol.asset";

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
        public void SwingingTheBatIsHeard()
        {
            WeaponDefinition bat = Load<WeaponDefinition>(BatPath);

            // Melee being quieter than a gun is the reason to carry a bat at all; it still cannot
            // be silent, or there is no reason ever to sneak past anything.
            Assert.That(bat.Noise, Is.GreaterThan(0f), "a silent weapon makes stealth free");
        }

        [Test]
        public void SprintingIsLouderThanWalking()
        {
            BalanceConfig balance = Load<BalanceConfig>(BalancePath);

            // The trade the whole stealth layer rests on: speed costs attention. Equal numbers
            // here would leave sprinting with no downside but stamina.
            Assert.That(balance.SprintNoise, Is.GreaterThan(balance.WalkNoise));
            Assert.That(
                balance.SprintStepSeconds,
                Is.LessThan(balance.WalkStepSeconds),
                "running also puts feet down more often");
        }

        [Test]
        public void AZoneCoolsDownButNotBeforeThePlayerNotices()
        {
            BalanceConfig balance = Load<BalanceConfig>(BalancePath);

            Assert.That(balance.HeatPerNoise, Is.GreaterThan(0f), "noise that does nothing");
            Assert.That(balance.HeatDecayPerSecond, Is.GreaterThan(0f), "a zone that never calms");

            // A stage has to last long enough to be read. Decaying a whole stage in under a
            // couple of seconds would make the meter flicker rather than inform.
            float stageWidth = balance.HeatHuntingAt - balance.HeatStirredAt;
            Assert.That(stageWidth / balance.HeatDecayPerSecond, Is.GreaterThan(2f));
            Assert.That(balance.HeatStirredAt, Is.LessThan(balance.HeatHuntingAt));
            Assert.That(balance.HeatSpawnsPerStage, Is.GreaterThan(0), "escalation with no teeth");
        }

        /// <summary>How hot a stretch of running leaves the zone, decay included.</summary>
        private static float HeatAfterSprinting(BalanceConfig balance, float seconds) =>
            (seconds / balance.SprintStepSeconds * balance.SprintNoise * balance.HeatPerNoise)
            - (seconds * balance.HeatDecayPerSecond);

        [Test]
        public void OneBarOfStaminaSpentSprintingBringsTheZoneDownOnYou()
        {
            BalanceConfig balance = Load<BalanceConfig>(BalancePath);

            // The relationship that nearly got away. Stamina decides how long a sprint can last,
            // and if the meter cannot reach its top stage inside that window, then no amount of
            // running will ever bring the zone to Hunting and the stage is decoration.
            // Three systems, one rule, and no single asset field can state it.
            float sprintSeconds = balance.StaminaMax / balance.SprintStaminaPerSecond;

            Assert.That(
                HeatAfterSprinting(balance, sprintSeconds),
                Is.GreaterThan(balance.HeatHuntingAt),
                $"{sprintSeconds:0.0}s is all the sprinting there is, and it never reaches Hunting");
        }

        [Test]
        public void ASprintAcrossTheRoomIsEnoughToBeNoticed()
        {
            BalanceConfig balance = Load<BalanceConfig>(BalancePath);

            // A few seconds of running should already have consequences, well inside the stamina
            // a rested character carries.
            Assert.That(
                HeatAfterSprinting(balance, 4f),
                Is.GreaterThan(balance.HeatStirredAt),
                "four seconds of sprinting goes unnoticed");
        }

        [Test]
        public void WalkingNeverHeatsAZoneUp()
        {
            BalanceConfig balance = Load<BalanceConfig>(BalancePath);

            float perSecond = balance.WalkNoise / balance.WalkStepSeconds * balance.HeatPerNoise;

            // The other half of the trade: crossing a zone on foot has to be possible all day, or
            // there is no quiet way through and stealth is just a slower way to be found.
            Assert.That(
                perSecond,
                Is.LessThan(balance.HeatDecayPerSecond * 0.9f),
                "walking alone fills the meter");
        }

        [TestCaseSource(nameof(EveryZombie))]
        public void AZombieKeepsListeningLongEnoughToMatter(string path)
        {
            ZombieDefinition zombie = Load<ZombieDefinition>(path);

            // Turning towards a noise for a fraction of a second would be invisible and useless.
            Assert.That(zombie.Senses.ListenSeconds, Is.GreaterThan(0.5f));
        }

        [TestCaseSource(nameof(EveryZombie))]
        public void AZombieCanBeEscapedButNotTrivially(string path)
        {
            ZombieDefinition zombie = Load<ZombieDefinition>(path);
            ZombieInstincts instincts = zombie.Instincts;

            // Both halves matter. Giving up instantly makes any corner a guaranteed escape;
            // never giving up makes hiding pointless and the zone a chase for ever.
            Assert.That(instincts.GiveUpSeconds, Is.InRange(1f, 30f));

            // An undisturbed zone has to look undisturbed, or the player cannot tell at a glance
            // whether they have been noticed.
            Assert.That(instincts.WanderSpeedFactor, Is.LessThan(1f));
            Assert.That(instincts.WanderRadius, Is.GreaterThanOrEqualTo(0f));
        }

        [Test]
        public void ThePistolIsFilledIn()
        {
            WeaponDefinition pistol = Load<WeaponDefinition>(PistolPath);

            Assert.That(pistol.Kind, Is.EqualTo(WeaponKind.Firearm), "a pistol that never fires");
            Assert.That(pistol.Damage, Is.GreaterThan(0f));
            Assert.That(pistol.MagazineSize, Is.GreaterThan(1));
            Assert.That(pistol.ReloadSeconds, Is.GreaterThan(0.2f), "an instant reload is no cost");
            Assert.That(pistol.MuzzleSpeed, Is.GreaterThan(0f));
            Assert.That(pistol.CooldownSeconds, Is.GreaterThan(0f));
        }

        [Test]
        public void ShootingIsLouderThanSwinging()
        {
            WeaponDefinition pistol = Load<WeaponDefinition>(PistolPath);
            WeaponDefinition bat = Load<WeaponDefinition>(BatPath);

            // The trade the whole arsenal is built on: the gun is better at everything except
            // being heard. Take that away and there is no reason to own a bat.
            Assert.That(pistol.Noise, Is.GreaterThan(bat.Noise * 2f));
        }

        [Test]
        public void OneShotIsEnoughToStirTheZone()
        {
            WeaponDefinition pistol = Load<WeaponDefinition>(PistolPath);
            BalanceConfig balance = Load<BalanceConfig>(BalancePath);

            // A gunshot that a zone can ignore is not a gunshot. Half a magazine should be past
            // the first stage on its own, before a single footstep is counted.
            float halfAMagazine = pistol.MagazineSize / 2f * pistol.Noise * balance.HeatPerNoise;

            Assert.That(halfAMagazine, Is.GreaterThan(balance.HeatStirredAt));
        }

        [Test]
        public void ShootingReachesFurtherThanSwinging()
        {
            WeaponDefinition pistol = Load<WeaponDefinition>(PistolPath);
            WeaponDefinition bat = Load<WeaponDefinition>(BatPath);

            // And the reason to carry it: a round arrives before the zombie does.
            Assert.That(pistol.ProjectileReach, Is.GreaterThan(bat.Range * 4f));
        }

        [TestCaseSource(nameof(EveryZombie))]
        public void APistolKillsAZombieInFewerShotsThanSwings(string path)
        {
            WeaponDefinition pistol = Load<WeaponDefinition>(PistolPath);
            ZombieDefinition zombie = Load<ZombieDefinition>(path);

            float perShot = zombie.Resistances.Apply(pistol.CreateDamage(attackerId: 1));
            Assert.That(perShot, Is.GreaterThan(0f));

            int shots = Mathf.CeilToInt(zombie.MaxHealth / perShot);
            Assert.That(
                shots,
                Is.InRange(1, 5),
                $"{zombie.DisplayName} takes {shots} rounds from the {pistol.DisplayName}");
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
