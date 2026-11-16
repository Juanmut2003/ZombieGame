#nullable enable
using System;
using NUnit.Framework;
using ZombieGame.Core.Pooling;

namespace ZombieGame.Tests.EditMode
{
    [TestFixture]
    public sealed class ObjectPoolTests
    {
        /// <summary>A pooled type that knows nothing about pooling.</summary>
        private sealed class Projectile
        {
        }

        private sealed class PoolableProjectile : IPoolable
        {
            public int TakenCount { get; private set; }

            public int ReturnedCount { get; private set; }

            public void OnTakenFromPool() => TakenCount++;

            public void OnReturnedToPool() => ReturnedCount++;
        }

        [Test]
        public void Get_CreatesAnInstanceWhenThePoolIsEmpty()
        {
            var pool = new ObjectPool<Projectile>(() => new Projectile());

            Projectile item = pool.Get();

            Assert.That(item, Is.Not.Null);
            Assert.That(pool.CountActive, Is.EqualTo(1));
            Assert.That(pool.CountIdle, Is.Zero);
        }

        [Test]
        public void AReturnedInstanceIsHandedOutAgainInsteadOfANewOne()
        {
            var pool = new ObjectPool<Projectile>(() => new Projectile());
            Projectile first = pool.Get();
            pool.Return(first);

            Projectile second = pool.Get();

            Assert.That(second, Is.SameAs(first));
            Assert.That(pool.CountTotal, Is.EqualTo(1), "nothing new should have been allocated");
        }

        [Test]
        public void Prewarm_BuildsInstancesUpFront()
        {
            int created = 0;
            var pool = new ObjectPool<Projectile>(
                () =>
                {
                    created++;
                    return new Projectile();
                },
                prewarm: 5);

            Assert.That(created, Is.EqualTo(5));
            Assert.That(pool.CountIdle, Is.EqualTo(5));
            Assert.That(pool.CountActive, Is.Zero);

            pool.Get();
            Assert.That(created, Is.EqualTo(5), "a prewarmed instance should have been reused");
        }

        [Test]
        public void CountsTrackGetAndReturn()
        {
            var pool = new ObjectPool<Projectile>(() => new Projectile());

            Projectile a = pool.Get();
            Projectile b = pool.Get();
            Assert.That(pool.CountActive, Is.EqualTo(2));
            Assert.That(pool.CountTotal, Is.EqualTo(2));

            pool.Return(a);
            Assert.That(pool.CountActive, Is.EqualTo(1));
            Assert.That(pool.CountIdle, Is.EqualTo(1));

            pool.Return(b);
            Assert.That(pool.CountActive, Is.Zero);
            Assert.That(pool.CountIdle, Is.EqualTo(2));
        }

        [Test]
        public void MaxRetained_DropsInstancesBeyondTheCap()
        {
            var pool = new ObjectPool<Projectile>(() => new Projectile(), maxRetained: 1);
            Projectile a = pool.Get();
            Projectile b = pool.Get();

            pool.Return(a);
            pool.Return(b);

            Assert.That(pool.CountIdle, Is.EqualTo(1), "the pool must not grow past its cap");
            Assert.That(pool.CountActive, Is.Zero);
        }

        [Test]
        public void IPoolableHooksRunOnGetAndReturn()
        {
            var pool = new ObjectPool<PoolableProjectile>(() => new PoolableProjectile());

            PoolableProjectile item = pool.Get();
            Assert.That(item.TakenCount, Is.EqualTo(1));
            Assert.That(item.ReturnedCount, Is.Zero);

            pool.Return(item);
            Assert.That(item.ReturnedCount, Is.EqualTo(1));

            pool.Get();
            Assert.That(item.TakenCount, Is.EqualTo(2), "reuse must reset the instance again");
        }

        [Test]
        public void Return_RejectsTheSameInstanceTwice()
        {
            var pool = new ObjectPool<Projectile>(() => new Projectile());
            Projectile item = pool.Get();
            pool.Return(item);

            Assert.Throws<InvalidOperationException>(() => pool.Return(item));
        }

        [Test]
        public void Return_RejectsAnInstanceThatNeverCameFromThePool()
        {
            var pool = new ObjectPool<Projectile>(() => new Projectile());

            Assert.Throws<InvalidOperationException>(() => pool.Return(new Projectile()));
        }

        [Test]
        public void Return_RejectsNull()
        {
            var pool = new ObjectPool<Projectile>(() => new Projectile());

            Assert.Throws<ArgumentNullException>(() => pool.Return(null!));
        }

        [Test]
        public void Clear_DropsIdleInstancesButLeavesActiveOnesAlone()
        {
            var pool = new ObjectPool<Projectile>(() => new Projectile(), prewarm: 3);
            Projectile active = pool.Get();

            pool.Clear();

            Assert.That(pool.CountIdle, Is.Zero);
            Assert.That(pool.CountActive, Is.EqualTo(1));
            Assert.DoesNotThrow(() => pool.Return(active), "an issued instance can still come back");
        }

        [Test]
        public void Constructor_RejectsNonsense()
        {
            Assert.Throws<ArgumentNullException>(() => new ObjectPool<Projectile>(null!));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new ObjectPool<Projectile>(() => new Projectile(), prewarm: -1));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new ObjectPool<Projectile>(() => new Projectile(), maxRetained: -1));
        }
    }
}
