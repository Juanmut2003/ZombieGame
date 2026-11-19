#nullable enable
using UnityEngine;

namespace ZombieGame.Audio
{
    /// <summary>Plays a sound. The only thing the rest of the game knows about audio.</summary>
    /// <remarks>
    /// An interface so that nothing has to find a speaker, and so a test can count what was asked
    /// for without anything being heard.
    /// </remarks>
    public interface ISoundService
    {
        /// <summary>Plays a sound somewhere in the world.</summary>
        /// <param name="sound">What happened.</param>
        /// <param name="at">Where it happened.</param>
        void Play(GameSound sound, Vector2 at);

        /// <summary>Plays a sound that has no place: the belt, a menu, the end of a run.</summary>
        void Play(GameSound sound);
    }

    /// <summary>Hears nothing. The default, and what a scene without audio gets.</summary>
    public sealed class Silence : ISoundService
    {
        /// <summary>The one instance; it holds no state.</summary>
        public static readonly Silence Instance = new();

        private Silence()
        {
        }

        /// <inheritdoc />
        public void Play(GameSound sound, Vector2 at)
        {
        }

        /// <inheritdoc />
        public void Play(GameSound sound)
        {
        }
    }
}
