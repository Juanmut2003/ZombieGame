#nullable enable
using System;
using System.Collections.Generic;

namespace ZombieGame.Core.Pooling
{
    /// <summary>
    /// A plain reuse pool. Projectiles, damage numbers, VFX and zombie state objects come from here
    /// instead of being allocated per frame.
    /// </summary>
    /// <remarks>
    /// Deliberately free of Unity types so it can be unit tested; pooling actual GameObjects is a
    /// thin layer on top of this.
    /// TODO: add that GameObject layer once the first prefabs exist (M1, projectiles).
    /// </remarks>
    /// <typeparam name="T">The pooled type. Implement <see cref="IPoolable"/> to get reset hooks.</typeparam>
    public sealed class ObjectPool<T> where T : class
    {
        private readonly Func<T> _factory;
        private readonly Stack<T> _idle;
        private readonly int _maxRetained;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Only tracked outside release builds: returning an object twice, or returning one that was
        // never taken, is a bug that is near impossible to find once it corrupts the pool.
        private readonly HashSet<T> _issued = new();
#endif

        /// <param name="factory">Creates a fresh instance when the pool runs dry.</param>
        /// <param name="prewarm">How many instances to build up front.</param>
        /// <param name="maxRetained">
        /// Upper bound on idle instances kept around; anything returned beyond it is dropped for
        /// the GC. Zero means unbounded.
        /// </param>
        public ObjectPool(Func<T> factory, int prewarm = 0, int maxRetained = 0)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));

            if (prewarm < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(prewarm), prewarm, "Cannot be negative.");
            }

            if (maxRetained < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxRetained), maxRetained, "Cannot be negative. Use 0 for unbounded.");
            }

            _maxRetained = maxRetained;
            _idle = new Stack<T>(prewarm > 0 ? prewarm : 8);
            Prewarm(prewarm);
        }

        /// <summary>How many instances sit idle, ready to be handed out.</summary>
        public int CountIdle => _idle.Count;

        /// <summary>How many instances are currently out in the world.</summary>
        public int CountActive { get; private set; }

        /// <summary>Idle plus active — every instance this pool has ever created and kept.</summary>
        public int CountTotal => CountIdle + CountActive;

        /// <summary>Builds <paramref name="count"/> instances ahead of time.</summary>
        public void Prewarm(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "Cannot be negative.");
            }

            for (int i = 0; i < count; i++)
            {
                _idle.Push(_factory());
            }
        }

        /// <summary>Takes an instance from the pool, creating one if none is idle.</summary>
        public T Get()
        {
            T item = _idle.Count > 0 ? _idle.Pop() : _factory();
            CountActive++;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _issued.Add(item);
#endif

            if (item is IPoolable poolable)
            {
                poolable.OnTakenFromPool();
            }

            return item;
        }

        /// <summary>Hands an instance back.</summary>
        /// <exception cref="ArgumentNullException">The item is null.</exception>
        /// <exception cref="InvalidOperationException">
        /// Outside release builds: the item did not come from this pool, or was already returned.
        /// </exception>
        public void Return(T item)
        {
            if (item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!_issued.Remove(item))
            {
                throw new InvalidOperationException(
                    $"An instance of {typeof(T).Name} was returned to a pool it did not come from, " +
                    "or was returned twice.");
            }
#endif

            CountActive--;

            if (item is IPoolable poolable)
            {
                poolable.OnReturnedToPool();
            }

            if (_maxRetained == 0 || _idle.Count < _maxRetained)
            {
                _idle.Push(item);
            }
        }

        /// <summary>
        /// Drops every idle instance. Instances still out in the world are untouched and can be
        /// returned afterwards.
        /// </summary>
        public void Clear() => _idle.Clear();
    }
}
