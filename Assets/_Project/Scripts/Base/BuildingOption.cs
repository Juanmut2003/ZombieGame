#nullable enable
namespace ZombieGame.Base
{
    /// <summary>
    /// Everything that can be done to a building that is already standing.
    /// </summary>
    /// <remarks>
    /// The four slices of the wheel, in the order they are drawn round it. The order is part of
    /// the interface: the two that cost the colony something sit on the left, the two that move
    /// people about sit on the right, and tearing down is diagonally opposite improving so the
    /// two that cannot be undone are never neighbours.
    /// </remarks>
    public enum BuildingOption
    {
        /// <summary>Put it up a level.</summary>
        Improve,

        /// <summary>Send somebody who is idle to work here.</summary>
        PutToWork,

        /// <summary>Take one worker off it.</summary>
        SendHome,

        /// <summary>Pull it down for part of what it cost.</summary>
        TearDown,
    }
}
