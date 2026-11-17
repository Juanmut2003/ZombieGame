#nullable enable
using System;
using NUnit.Framework;
using ZombieGame.Core;

namespace ZombieGame.Tests.EditMode
{
    [TestFixture]
    public sealed class GameServicesTests
    {
        private sealed class DummyService
        {
            public string Name { get; set; } = string.Empty;
        }

        private sealed class OtherService
        {
        }

        [SetUp]
        public void ClearBeforeTest() => GameServices.Reset();

        [TearDown]
        public void ClearAfterTest() => GameServices.Reset();

        [Test]
        public void Get_ReturnsTheRegisteredInstance()
        {
            var service = new DummyService { Name = "colony" };
            GameServices.Register(service);

            Assert.That(GameServices.Get<DummyService>(), Is.SameAs(service));
        }

        [Test]
        public void Get_ThrowsWhenNothingIsRegistered()
        {
            Assert.Throws<InvalidOperationException>(() => GameServices.Get<DummyService>());
        }

        [Test]
        public void Register_ThrowsOnDoubleRegistration()
        {
            GameServices.Register(new DummyService());

            Assert.Throws<InvalidOperationException>(() => GameServices.Register(new DummyService()));
        }

        [Test]
        public void Register_ThrowsOnNull()
        {
            Assert.Throws<ArgumentNullException>(() => GameServices.Register<DummyService>(null!));
        }

        [Test]
        public void Replace_SwapsTheInstanceAndReturnsThePrevious()
        {
            var first = new DummyService { Name = "first" };
            var second = new DummyService { Name = "second" };
            GameServices.Register(first);

            DummyService? previous = GameServices.Replace(second);

            Assert.That(previous, Is.SameAs(first));
            Assert.That(GameServices.Get<DummyService>(), Is.SameAs(second));
        }

        [Test]
        public void Replace_WorksWhenNothingWasRegistered()
        {
            Assert.That(GameServices.Replace(new DummyService()), Is.Null);
            Assert.That(GameServices.IsRegistered<DummyService>(), Is.True);
        }

        [Test]
        public void TryGet_ReportsMissingServiceWithoutThrowing()
        {
            Assert.That(GameServices.TryGet(out DummyService? _), Is.False);
        }

        [Test]
        public void TryGet_HandsOutTheServiceWhenPresent()
        {
            var service = new DummyService();
            GameServices.Register(service);

            Assert.That(GameServices.TryGet(out DummyService? found), Is.True);
            Assert.That(found, Is.SameAs(service));
        }

        [Test]
        public void ServicesAreKeptApartByType()
        {
            var dummy = new DummyService();
            var other = new OtherService();
            GameServices.Register(dummy);
            GameServices.Register(other);

            Assert.That(GameServices.Count, Is.EqualTo(2));
            Assert.That(GameServices.Get<DummyService>(), Is.SameAs(dummy));
            Assert.That(GameServices.Get<OtherService>(), Is.SameAs(other));
        }

        [Test]
        public void Unregister_RemovesOnlyThatService()
        {
            GameServices.Register(new DummyService());
            GameServices.Register(new OtherService());

            Assert.That(GameServices.Unregister<DummyService>(), Is.True);
            Assert.That(GameServices.IsRegistered<DummyService>(), Is.False);
            Assert.That(GameServices.IsRegistered<OtherService>(), Is.True);
        }

        [Test]
        public void Unregister_ReturnsFalseWhenThereWasNothingToRemove()
        {
            Assert.That(GameServices.Unregister<DummyService>(), Is.False);
        }

        [Test]
        public void Reset_EmptiesTheLocator()
        {
            GameServices.Register(new DummyService());
            GameServices.Register(new OtherService());

            GameServices.Reset();

            Assert.That(GameServices.Count, Is.Zero);
        }
    }
}
