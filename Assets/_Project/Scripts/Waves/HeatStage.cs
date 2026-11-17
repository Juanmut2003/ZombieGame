#nullable enable

namespace ZombieGame.Waves
{
    /// <summary>How worked up a zone is, as the three states the player can read at a glance.</summary>
    /// <remarks>
    /// A meter alone tells a player a number is going up. A named stage tells them what it means
    /// and, through <see cref="HeatChangedEvent"/>, tells the zone when to act on it. Three is on
    /// purpose: quiet, noticed, and in trouble is the whole decision a player makes while looting.
    /// </remarks>
    public enum HeatStage
    {
        /// <summary>Nothing has heard anything worth walking towards.</summary>
        Calm = 0,

        /// <summary>The zone has noticed. More of it is awake than was a minute ago.</summary>
        Stirred = 1,

        /// <summary>It is coming. Looting time is over.</summary>
        Hunting = 2,
    }
}
