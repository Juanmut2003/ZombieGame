#nullable enable
using System;
using System.Collections.Generic;
using NUnit.Framework;
using ZombieGame.Core.Events;

namespace ZombieGame.Tests.EditMode
{
    [TestFixture]
    public sealed class EventBusTests
    {
        private readonly struct SurvivorDied : IEvent
        {
            public SurvivorDied(int survivorId) => SurvivorId = survivorId;

            public int SurvivorId { get; }
        }

        private readonly struct WaveStarted : IEvent
        {
            public WaveStarted(int size) => Size = size;

            public int Size { get; }
        }

        private EventBus _bus = null!;

        [SetUp]
        public void CreateBus() => _bus = new EventBus();

        [Test]
        public void Publish_ReachesTheSubscriber()
        {
            int received = -1;
            _bus.Subscribe<SurvivorDied>(e => received = e.SurvivorId);

            _bus.Publish(new SurvivorDied(42));

            Assert.That(received, Is.EqualTo(42));
        }

        [Test]
        public void Publish_WithNoSubscribersIsHarmless()
        {
            Assert.DoesNotThrow(() => _bus.Publish(new WaveStarted(10)));
        }

        [Test]
        public void Publish_ReachesEverySubscriberInOrder()
        {
            var order = new List<string>();
            _bus.Subscribe<WaveStarted>(_ => order.Add("first"));
            _bus.Subscribe<WaveStarted>(_ => order.Add("second"));
            _bus.Subscribe<WaveStarted>(_ => order.Add("third"));

            _bus.Publish(new WaveStarted(1));

            Assert.That(order, Is.EqualTo(new[] { "first", "second", "third" }));
        }

        [Test]
        public void EventTypesDoNotLeakIntoEachOther()
        {
            int waveCalls = 0;
            _bus.Subscribe<WaveStarted>(_ => waveCalls++);

            _bus.Publish(new SurvivorDied(1));

            Assert.That(waveCalls, Is.Zero);
        }

        [Test]
        public void Unsubscribe_StopsDelivery()
        {
            int calls = 0;
            void Handler(WaveStarted _) => calls++;
            _bus.Subscribe<WaveStarted>(Handler);

            Assert.That(_bus.Unsubscribe<WaveStarted>(Handler), Is.True);
            _bus.Publish(new WaveStarted(1));

            Assert.That(calls, Is.Zero);
        }

        [Test]
        public void Unsubscribe_TwiceIsHarmless()
        {
            void Handler(WaveStarted _) { }
            _bus.Subscribe<WaveStarted>(Handler);
            _bus.Unsubscribe<WaveStarted>(Handler);

            Assert.That(_bus.Unsubscribe<WaveStarted>(Handler), Is.False);
        }

        [Test]
        public void Subscribe_RejectsNull()
        {
            Assert.Throws<ArgumentNullException>(() => _bus.Subscribe<WaveStarted>(null!));
        }

        [Test]
        public void HandlerCanUnsubscribeItselfDuringPublish()
        {
            int calls = 0;
            Action<WaveStarted>? handler = null;
            handler = _ =>
            {
                calls++;
                _bus.Unsubscribe(handler!);
            };
            _bus.Subscribe(handler);

            _bus.Publish(new WaveStarted(1));
            _bus.Publish(new WaveStarted(2));

            Assert.That(calls, Is.EqualTo(1), "the handler should only have run before it left");
            Assert.That(_bus.SubscriberCount<WaveStarted>(), Is.Zero);
        }

        [Test]
        public void HandlerSubscribedDuringPublishOnlyGetsTheNextEvent()
        {
            int lateCalls = 0;
            _bus.Subscribe<WaveStarted>(_ => _bus.Subscribe<WaveStarted>(__ => lateCalls++));

            _bus.Publish(new WaveStarted(1));
            Assert.That(lateCalls, Is.Zero, "a handler added mid-publish must not see that event");

            _bus.Publish(new WaveStarted(2));
            Assert.That(lateCalls, Is.EqualTo(1));
        }

        [Test]
        public void HandlerRemovedDuringPublishDoesNotRunAfterwards()
        {
            var calls = new List<string>();
            void Second(WaveStarted _) => calls.Add("second");
            _bus.Subscribe<WaveStarted>(_ =>
            {
                calls.Add("first");
                _bus.Unsubscribe<WaveStarted>(Second);
            });
            _bus.Subscribe<WaveStarted>(Second);

            _bus.Publish(new WaveStarted(1));

            Assert.That(calls, Is.EqualTo(new[] { "first" }));
        }

        [Test]
        public void SubscriberCount_TracksSubscriptions()
        {
            Assert.That(_bus.SubscriberCount<WaveStarted>(), Is.Zero);

            void Handler(WaveStarted _) { }
            _bus.Subscribe<WaveStarted>(Handler);
            Assert.That(_bus.SubscriberCount<WaveStarted>(), Is.EqualTo(1));

            _bus.Unsubscribe<WaveStarted>(Handler);
            Assert.That(_bus.SubscriberCount<WaveStarted>(), Is.Zero);
        }

        [Test]
        public void Clear_DropsEverySubscription()
        {
            int calls = 0;
            _bus.Subscribe<WaveStarted>(_ => calls++);
            _bus.Subscribe<SurvivorDied>(_ => calls++);

            _bus.Clear();
            _bus.Publish(new WaveStarted(1));
            _bus.Publish(new SurvivorDied(1));

            Assert.That(calls, Is.Zero);
        }

        [Test]
        public void AThrowingHandlerLeavesTheBusUsable()
        {
            _bus.Subscribe<WaveStarted>(_ => throw new InvalidOperationException("boom"));

            Assert.Throws<InvalidOperationException>(() => _bus.Publish(new WaveStarted(1)));

            int calls = 0;
            _bus.Subscribe<SurvivorDied>(_ => calls++);
            _bus.Publish(new SurvivorDied(1));
            Assert.That(calls, Is.EqualTo(1), "the bus must not stay stuck in a publish");
        }
    }
}
