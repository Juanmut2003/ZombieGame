#nullable enable
using System;
using System.Collections.Generic;
using NUnit.Framework;
using ZombieGame.Combat;
using ZombieGame.Core.Events;

namespace ZombieGame.Tests.EditMode
{
    [TestFixture]
    public sealed class HealthTests
    {
        private static DamageInfo Hit(float amount, DamageType type = DamageType.Blunt) =>
            new(amount, type);

        [Test]
        public void AFreshHealthStartsFull()
        {
            var health = new Health(100f);

            Assert.That(health.Current, Is.EqualTo(100f));
            Assert.That(health.Maximum, Is.EqualTo(100f));
            Assert.That(health.IsAlive, Is.True);
            Assert.That(health.Fraction, Is.EqualTo(1f));
        }

        [Test]
        public void DamageComesOffTheCurrentHealth()
        {
            var health = new Health(100f);

            float applied = health.TakeDamage(Hit(30f));

            Assert.That(applied, Is.EqualTo(30f));
            Assert.That(health.Current, Is.EqualTo(70f));
            Assert.That(health.Fraction, Is.EqualTo(0.7f).Within(0.0001f));
        }

        [Test]
        public void RunningOutOfHealthKills()
        {
            var health = new Health(50f);

            health.TakeDamage(Hit(50f));

            Assert.That(health.IsAlive, Is.False);
            Assert.That(health.Current, Is.Zero);
        }

        [Test]
        public void HealthNeverGoesBelowZero()
        {
            var health = new Health(20f);

            float applied = health.TakeDamage(Hit(1000f));

            Assert.That(health.Current, Is.Zero);
            Assert.That(applied, Is.EqualTo(20f), "it only ever costs what was left");
        }

        [Test]
        public void HittingACorpseDoesNothing()
        {
            var health = new Health(10f);
            health.TakeDamage(Hit(10f));

            float applied = health.TakeDamage(Hit(10f));

            Assert.That(applied, Is.Zero);
            Assert.That(health.Current, Is.Zero);
        }

        [Test]
        public void DeathIsRaisedExactlyOnceNoMatterHowOftenItIsHit()
        {
            var health = new Health(10f);
            int deaths = 0;
            health.Died += _ => deaths++;

            health.TakeDamage(Hit(10f));
            health.TakeDamage(Hit(10f));
            health.TakeDamage(Hit(10f));
            health.Kill();

            Assert.That(deaths, Is.EqualTo(1), "loot and pooling cannot survive a second death");
        }

        [Test]
        public void SurvivingAHitRaisesDamageTakenButNotDied()
        {
            var health = new Health(100f);
            var taken = new List<DamageTakenEvent>();
            int deaths = 0;
            health.DamageTaken += taken.Add;
            health.Died += _ => deaths++;

            health.TakeDamage(new DamageInfo(25f, DamageType.Sharp, sourceId: 7));

            Assert.That(deaths, Is.Zero);
            Assert.That(taken, Has.Count.EqualTo(1));
            Assert.That(taken[0].Applied, Is.EqualTo(25f));
            Assert.That(taken[0].Remaining, Is.EqualTo(75f));
            Assert.That(taken[0].Damage.SourceId, Is.EqualTo(7));
        }

        [Test]
        public void TheKillingBlowRaisesDiedAndNotDamageTaken()
        {
            var health = new Health(10f);
            int taken = 0;
            DiedEvent? died = null;
            health.DamageTaken += _ => taken++;
            health.Died += e => died = e;

            health.TakeDamage(new DamageInfo(10f, DamageType.Ballistic, sourceId: 3));

            Assert.That(taken, Is.Zero, "a death is not also a flinch");
            Assert.That(died.HasValue, Is.True);
            Assert.That(died!.Value.KillingBlow.Type, Is.EqualTo(DamageType.Ballistic));
            Assert.That(died.Value.KillingBlow.SourceId, Is.EqualTo(3));
        }

        [Test]
        public void ResistancesScaleTheDamage()
        {
            // Blunt, Sharp, Ballistic, Fire, Toxic — an armoured corpse, as PROJECT.md 6 describes.
            var armoured = new DamageResistances(1f, 0.5f, 0.2f, 1.5f, 1f);
            var health = new Health(100f, resistances: armoured);

            Assert.That(health.TakeDamage(Hit(10f, DamageType.Ballistic)), Is.EqualTo(2f));
            Assert.That(health.TakeDamage(Hit(10f, DamageType.Blunt)), Is.EqualTo(10f));
            Assert.That(health.TakeDamage(Hit(10f, DamageType.Fire)), Is.EqualTo(15f));
        }

        [Test]
        public void AZeroMultiplierIsImmunity()
        {
            var immune = new DamageResistances(0f, 1f, 1f, 1f, 1f);
            var health = new Health(100f, resistances: immune);

            Assert.That(health.TakeDamage(Hit(999f, DamageType.Blunt)), Is.Zero);
            Assert.That(health.Current, Is.EqualTo(100f));
        }

        [Test]
        public void AFullyResistedHitRaisesNothing()
        {
            var immune = new DamageResistances(0f, 1f, 1f, 1f, 1f);
            var health = new Health(100f, resistances: immune);
            int events = 0;
            health.DamageTaken += _ => events++;

            health.TakeDamage(Hit(50f, DamageType.Blunt));

            Assert.That(events, Is.Zero, "nothing happened, so nothing should be announced");
        }

        [Test]
        public void WithoutResistancesEverythingLandsInFull()
        {
            var health = new Health(100f);

            Assert.That(health.TakeDamage(Hit(10f, DamageType.Toxic)), Is.EqualTo(10f));
            Assert.That(DamageResistances.None.Multiplier(DamageType.Fire), Is.EqualTo(1f));
        }

        [Test]
        public void HealingRestoresButNeverPastFull()
        {
            var health = new Health(100f);
            health.TakeDamage(Hit(40f));

            Assert.That(health.Heal(10f), Is.EqualTo(10f));
            Assert.That(health.Current, Is.EqualTo(70f));
            Assert.That(health.Heal(999f), Is.EqualTo(30f), "only the missing part is restored");
            Assert.That(health.Current, Is.EqualTo(100f));
        }

        [Test]
        public void HealingDoesNotRaiseTheDead()
        {
            var health = new Health(10f);
            health.TakeDamage(Hit(10f));

            Assert.That(health.Heal(100f), Is.Zero);
            Assert.That(health.IsAlive, Is.False);
        }

        [Test]
        public void Kill_EndsItOutright()
        {
            var health = new Health(100f);
            DiedEvent? died = null;
            health.Died += e => died = e;

            Assert.That(health.Kill(new DamageInfo(0f, DamageType.Toxic)), Is.True);
            Assert.That(health.IsAlive, Is.False);
            Assert.That(died!.Value.KillingBlow.Type, Is.EqualTo(DamageType.Toxic));
            Assert.That(health.Kill(), Is.False, "it was already dead");
        }

        [Test]
        public void ResetToFull_PreparesAPooledBodyWithoutAnnouncingAnything()
        {
            var health = new Health(30f);
            int events = 0;
            health.Died += _ => events++;
            health.DamageTaken += _ => events++;
            health.TakeDamage(Hit(30f));
            events = 0;

            health.ResetToFull();

            Assert.That(health.Current, Is.EqualTo(30f));
            Assert.That(health.IsAlive, Is.True);
            Assert.That(events, Is.Zero);
            Assert.That(health.TakeDamage(Hit(5f)), Is.EqualTo(5f), "and it can be hurt again");
        }

        [Test]
        public void EventsAlsoGoOutOnTheEventBus()
        {
            var bus = new EventBus();
            var taken = new List<DamageTakenEvent>();
            var deaths = new List<DiedEvent>();
            bus.Subscribe<DamageTakenEvent>(taken.Add);
            bus.Subscribe<DiedEvent>(deaths.Add);
            var health = new Health(20f, ownerId: 42, bus: bus);

            health.TakeDamage(Hit(5f));
            health.TakeDamage(Hit(15f));

            Assert.That(taken, Has.Count.EqualTo(1));
            Assert.That(taken[0].TargetId, Is.EqualTo(42));
            Assert.That(deaths, Has.Count.EqualTo(1));
            Assert.That(deaths[0].TargetId, Is.EqualTo(42));
        }

        [Test]
        public void ConstructorRejectsNonsense()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Health(0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Health(-10f));
        }

        [Test]
        public void DamageAndHealingRejectNegativeAmounts()
        {
            var health = new Health(10f);

            Assert.Throws<ArgumentOutOfRangeException>(() => new DamageInfo(-1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => health.Heal(-1f));
        }

        [Test]
        public void ResistancesRejectAWrongLengthOrNegativeValues()
        {
            Assert.Throws<ArgumentException>(() => new DamageResistances(1f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new DamageResistances(1f, 1f, -0.5f, 1f, 1f));
        }
    }
}
