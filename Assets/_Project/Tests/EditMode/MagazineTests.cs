#nullable enable
using System;
using NUnit.Framework;
using ZombieGame.Combat;

namespace ZombieGame.Tests.EditMode
{
    /// <summary>Counting rounds: firing, running dry, and topping up.</summary>
    [TestFixture]
    public sealed class MagazineTests
    {
        [Test]
        public void FiringSpendsARound()
        {
            var magazine = new Magazine(capacity: 12, loaded: 12, reserve: 24);

            Assert.That(magazine.TryFire(), Is.True);
            Assert.That(magazine.Loaded, Is.EqualTo(11));
            Assert.That(magazine.Reserve, Is.EqualTo(24), "the pockets are not the magazine");
        }

        [Test]
        public void AnEmptyGunDoesNotFire()
        {
            var magazine = new Magazine(12, loaded: 0, reserve: 24);

            Assert.That(magazine.TryFire(), Is.False);
            Assert.That(magazine.IsEmpty, Is.True);
        }

        [Test]
        public void ReloadingFillsFromThePockets()
        {
            var magazine = new Magazine(12, loaded: 2, reserve: 24);

            Assert.That(magazine.Reload(), Is.EqualTo(10));
            Assert.That(magazine.Loaded, Is.EqualTo(12));
            Assert.That(magazine.Reserve, Is.EqualTo(14));
        }

        [Test]
        public void ReloadingTakesWhatIsLeftWhenItIsNotEnough()
        {
            var magazine = new Magazine(12, loaded: 0, reserve: 5);

            Assert.That(magazine.Reload(), Is.EqualTo(5));
            Assert.That(magazine.Loaded, Is.EqualTo(5));
            Assert.That(magazine.Reserve, Is.Zero);
        }

        [Test]
        public void AFullGunHasNothingToReload()
        {
            var magazine = new Magazine(12, loaded: 12, reserve: 24);

            Assert.That(magazine.CanReload, Is.False);
            Assert.That(magazine.Reload(), Is.Zero);
            Assert.That(magazine.Reserve, Is.EqualTo(24), "reloading a full gun must not eat ammo");
        }

        [Test]
        public void EmptyPocketsMeanNoReload()
        {
            var magazine = new Magazine(12, loaded: 0, reserve: 0);

            Assert.That(magazine.CanReload, Is.False);
            Assert.That(magazine.Reload(), Is.Zero);
        }

        [Test]
        public void TheLastRoundStillFires()
        {
            var magazine = new Magazine(2, loaded: 2, reserve: 0);

            Assert.That(magazine.TryFire(), Is.True);
            Assert.That(magazine.TryFire(), Is.True);
            Assert.That(magazine.TryFire(), Is.False);
            Assert.That(magazine.Loaded, Is.Zero, "a count that goes negative is a count nobody trusts");
        }

        [Test]
        public void FoundAmmunitionGoesInThePockets()
        {
            var magazine = new Magazine(12, loaded: 0, reserve: 0);
            magazine.AddReserve(30);

            Assert.That(magazine.Reserve, Is.EqualTo(30));
            Assert.That(magazine.CanReload, Is.True);
        }

        [Test]
        public void AMagazineRefusesNonsense()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Magazine(0, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Magazine(12, 13, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Magazine(12, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Magazine(12, 0, -1));
        }
    }
}
