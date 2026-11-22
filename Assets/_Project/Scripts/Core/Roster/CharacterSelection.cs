#nullable enable

namespace ZombieGame.Core.Roster
{
    /// <summary>
    /// Who the player picked in the menu, for the zone that loads next to read.
    /// </summary>
    /// <remarks>
    /// The one thing that has to survive a scene change between choosing somebody and playing
    /// them. Registered with <see cref="GameServices"/> at boot like the clock and the scene
    /// loader, rather than being a static of its own — a second way of keeping global state is
    /// the thing CLAUDE.md rules out, not global state as such.
    /// Nothing is chosen until somebody chooses: a zone opened straight from the editor finds
    /// nothing here and keeps the character the scene was built with, which is what makes a
    /// zone openable on its own.
    /// </remarks>
    public sealed class CharacterSelection
    {
        /// <summary>Who the player picked, or null while nobody has.</summary>
        public PlayableCharacter? Chosen { get; private set; }

        /// <summary>True once somebody is picked and there is art to draw them with.</summary>
        public bool HasChoice => Chosen != null && Chosen.CanBeDrawn;

        /// <summary>
        /// Picks somebody. Anyone not yet available is refused, so a card the menu drew as to
        /// come cannot start a run by some other route.
        /// </summary>
        /// <returns>True when the choice was taken.</returns>
        public bool Choose(PlayableCharacter? character)
        {
            if (character == null || !character.Available || !character.CanBeDrawn)
            {
                return false;
            }

            Chosen = character;
            return true;
        }

        /// <summary>Forgets the choice, for returning to the menu and for tests.</summary>
        public void Clear() => Chosen = null;
    }
}
