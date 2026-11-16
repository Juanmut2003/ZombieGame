#nullable enable

namespace ZombieGame.Core.Pooling
{
    /// <summary>
    /// Implemented by anything that needs to reset itself as it moves in and out of a pool.
    /// <see cref="ObjectPool{T}"/> calls these automatically.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>Called right before the object is handed out. Reset state here.</summary>
        void OnTakenFromPool();

        /// <summary>Called as the object goes back. Release references and stop work here.</summary>
        void OnReturnedToPool();
    }
}
