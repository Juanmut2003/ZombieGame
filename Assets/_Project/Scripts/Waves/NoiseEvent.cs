#nullable enable
using UnityEngine;
using ZombieGame.Core.Events;

namespace ZombieGame.Waves
{
    /// <summary>
    /// Something made a sound somewhere. Whoever has ears decides whether it reached them.
    /// </summary>
    /// <remarks>
    /// The whole noise system rests on one rule from PROJECT.md §1: noise is the enemy. Every
    /// advantage — sprinting away, swinging a bat, later a gunshot or a generator — is paid for in
    /// attention. Keeping that as one event means a new source of noise is one publish, and
    /// everything that reacts to noise reacts to it without being told it exists.
    /// Loudness is a multiplier on a listener's own hearing range, not a distance: 1 is an
    /// ordinary noise, 4 carries four times as far. It is deliberately not in decibels — the
    /// number a designer tunes should be the number the gameplay uses.
    /// </remarks>
    public readonly struct NoiseEvent : IEvent
    {
        public NoiseEvent(Vector2 origin, float loudness, int sourceId = 0)
        {
            Origin = origin;
            Loudness = Mathf.Max(0f, loudness);
            SourceId = sourceId;
        }

        /// <summary>Where the sound came from.</summary>
        public Vector2 Origin { get; }

        /// <summary>How far it carries, as a multiple of an ordinary noise.</summary>
        public float Loudness { get; }

        /// <summary>Who made it, so a listener can ignore its own racket.</summary>
        public int SourceId { get; }
    }
}
