#nullable enable
using System;
using NUnit.Framework;
using UnityEngine;
using ZombieGame.Combat;

namespace ZombieGame.Tests.EditMode
{
    [TestFixture]
    public sealed class MeleeAndStaminaTests
    {
        private static StaminaSettings Settings(
            float max = 100f, float regen = 20f, float delay = 1f) => new(max, regen, delay);

        [Test]
        public void AFreshCharacterIsRested()
        {
            var stamina = new Stamina(Settings());

            Assert.That(stamina.Current, Is.EqualTo(100f));
            Assert.That(stamina.Fraction, Is.EqualTo(1f));
        }

        [Test]
        public void SpendingTakesItAndRefusesWhenThereIsNotEnough()
        {
            var stamina = new Stamina(Settings(max: 20f));

            Assert.That(stamina.TrySpend(15f), Is.True);
            Assert.That(stamina.Current, Is.EqualTo(5f));
            Assert.That(stamina.TrySpend(15f), Is.False, "not enough left for a second swing");
            Assert.That(stamina.Current, Is.EqualTo(5f), "a refused swing costs nothing");
        }

        [Test]
        public void DrainingTakesWhateverIsLeft()
        {
            var stamina = new Stamina(Settings(max: 10f));

            Assert.That(stamina.Drain(4f), Is.EqualTo(4f));
            Assert.That(stamina.Drain(100f), Is.EqualTo(6f), "a sprint ends when the tank does");
            Assert.That(stamina.Drain(1f), Is.Zero);
        }

        [Test]
        public void ItComesBackButOnlyAfterThePause()
        {
            var stamina = new Stamina(Settings(max: 100f, regen: 20f, delay: 1f));
            stamina.TrySpend(50f);

            stamina.Tick(0.5f);
            Assert.That(stamina.Current, Is.EqualTo(50f), "still inside the pause");

            stamina.Tick(0.6f);
            stamina.Tick(1f);
            Assert.That(stamina.Current, Is.GreaterThan(50f), "and now it recovers");
        }

        [Test]
        public void SpendingAgainRestartsThePause()
        {
            var stamina = new Stamina(Settings(max: 100f, regen: 20f, delay: 1f));
            stamina.TrySpend(50f);
            stamina.Tick(0.9f);

            stamina.TrySpend(10f);
            stamina.Tick(0.9f);

            Assert.That(
                stamina.Current,
                Is.EqualTo(40f),
                "tapping sprint must not be better than sprinting");
        }

        [Test]
        public void ItNeverGoesPastFull()
        {
            var stamina = new Stamina(Settings(max: 50f, regen: 1000f, delay: 0f));
            stamina.TrySpend(10f);

            stamina.Tick(10f);

            Assert.That(stamina.Current, Is.EqualTo(50f));
        }

        [Test]
        public void StaminaRejectsNegativeAmounts()
        {
            var stamina = new Stamina(Settings());

            Assert.Throws<ArgumentOutOfRangeException>(() => stamina.TrySpend(-1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => stamina.Drain(-1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new StaminaSettings(0f));
        }

        [Test]
        public void ASwingReachesWhatIsInFrontOfIt()
        {
            bool hit = MeleeArc.Covers(
                origin: Vector2.zero,
                facing: Vector2.right,
                target: new Vector2(1f, 0f),
                targetRadius: 0.3f,
                range: 1.2f,
                halfAngleDegrees: 60f);

            Assert.That(hit, Is.True);
        }

        [Test]
        public void ASwingMissesWhatIsBehindIt()
        {
            bool hit = MeleeArc.Covers(
                Vector2.zero, Vector2.right, new Vector2(-1f, 0f), 0.3f, 1.2f, 60f);

            Assert.That(hit, Is.False, "a bat swung forwards does not hit your back");
        }

        [Test]
        public void ASwingMissesWhatIsOutOfReach()
        {
            bool hit = MeleeArc.Covers(
                Vector2.zero, Vector2.right, new Vector2(5f, 0f), 0.3f, 1.2f, 60f);

            Assert.That(hit, Is.False);
        }

        [Test]
        public void ReachIsMeasuredToTheNearEdgeOfABody()
        {
            // Centre at 1.4 is past a reach of 1.2, but a body half a unit wide is not.
            Assert.That(
                MeleeArc.Covers(Vector2.zero, Vector2.right, new Vector2(1.4f, 0f), 0.5f, 1.2f, 60f),
                Is.True);
            Assert.That(
                MeleeArc.Covers(Vector2.zero, Vector2.right, new Vector2(1.4f, 0f), 0.05f, 1.2f, 60f),
                Is.False);
        }

        [Test]
        public void AWideBodyLeaningIntoTheWedgeIsCaught()
        {
            // Dead on the 60 degree edge the centre would be marginal; a wide body is not.
            var target = new Vector2(0.6f, 1.05f);

            Assert.That(
                MeleeArc.Covers(Vector2.zero, Vector2.up, target, 0.35f, 1.4f, 45f),
                Is.True,
                "a shoulder inside the swing counts as a hit");
        }

        [Test]
        public void SomethingStandingOnTopOfYouIsAlwaysHit()
        {
            Assert.That(
                MeleeArc.Covers(Vector2.zero, Vector2.right, Vector2.zero, 0.3f, 1f, 30f),
                Is.True,
                "a zombie already inside you cannot be missed by angle");
        }

        [Test]
        public void AFullCircleHitsEverythingInReach()
        {
            Assert.That(
                MeleeArc.Covers(Vector2.zero, Vector2.right, new Vector2(-0.8f, 0f), 0.1f, 1f, 180f),
                Is.True);
        }

        [Test]
        public void ASwingWithNoReachOrNoWidthHitsNothing()
        {
            Assert.That(
                MeleeArc.Covers(Vector2.zero, Vector2.right, new Vector2(0.5f, 0f), 0.1f, 0f, 60f),
                Is.False);
            Assert.That(
                MeleeArc.Covers(Vector2.zero, Vector2.right, new Vector2(0.5f, 0f), 0.1f, 1f, 0f),
                Is.False);
        }

        [Test]
        public void AWeaponTurnsIntoTheBlowItDeals()
        {
            var weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
            try
            {
                var serialized = new UnityEditor.SerializedObject(weapon);
                serialized.FindProperty("_damage").floatValue = 22f;
                serialized.FindProperty("_damageType").enumValueIndex = (int)DamageType.Sharp;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                DamageInfo blow = weapon.CreateDamage(attackerId: 99);

                Assert.That(blow.Amount, Is.EqualTo(22f));
                Assert.That(blow.Type, Is.EqualTo(DamageType.Sharp));
                Assert.That(blow.SourceId, Is.EqualTo(99));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(weapon);
            }
        }
    }
}
