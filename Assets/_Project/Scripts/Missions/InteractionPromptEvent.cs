#nullable enable
using ZombieGame.Core.Events;

namespace ZombieGame.Missions
{
    /// <summary>What the character could do right now, and how far through doing it they are.</summary>
    /// <remarks>
    /// The HUD needs to say "hold E to search" at the moment it is true and nothing at any other
    /// moment, which is a thing only whoever is standing next to the shelf knows.
    /// </remarks>
    public readonly struct InteractionPromptEvent : IEvent
    {
        public InteractionPromptEvent(string text, float fraction)
        {
            Text = text;
            Fraction = fraction;
        }

        /// <summary>What to show. Empty means show nothing.</summary>
        public string Text { get; }

        /// <summary>How far along the action is, 0 to 1.</summary>
        public float Fraction { get; }
    }
}
