#nullable enable
using System.Collections.Generic;

namespace ZombieGame.Core.Roster
{
    /// <summary>
    /// Reading the roster: who is left once the player has taken somebody.
    /// </summary>
    /// <remarks>
    /// The menu offers a choice, and everybody on it who was not chosen is still a person. The
    /// colony puts one of them on the plot as its first survivor, which is why this is here and
    /// not inside the menu: both halves of the game read the same roster, and neither may reach
    /// the other.
    /// </remarks>
    public static class RosterChoice
    {
        /// <summary>
        /// The first person on the roster who is not the one taken, can be played, and has
        /// drawings to be seen in.
        /// </summary>
        /// <param name="roster">Everybody the menu offers. Holes are skipped.</param>
        /// <param name="taken">Who the player is playing, or null if nobody has picked yet.</param>
        /// <returns>Somebody else, or null when the roster has nobody else to offer.</returns>
        public static PlayableCharacter? SomebodyElse(
            IReadOnlyList<PlayableCharacter?>? roster, PlayableCharacter? taken)
        {
            if (roster is null)
            {
                return null;
            }

            for (int i = 0; i < roster.Count; i++)
            {
                PlayableCharacter? who = roster[i];

                // Unfinished characters are skipped on the same terms as in the menu: somebody
                // with no walk standing in the base is worse than an empty plot.
                if (who == null || !who.Available || !who.CanBeDrawn)
                {
                    continue;
                }

                if (taken != null && who == taken)
                {
                    continue;
                }

                return who;
            }

            return null;
        }
    }
}
