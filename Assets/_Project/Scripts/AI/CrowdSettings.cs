#nullable enable
using System;

namespace ZombieGame.AI
{
    /// <summary>
    /// How hard zombies push each other apart, and how the neighbour search is bucketed.
    /// </summary>
    /// <remarks>
    /// Without this a horde is one zombie: every body seeks the same point at the same speed, so
    /// they converge on the player and end up drawn exactly on top of each other. Separation is
    /// what turns that single blob back into a crowd.
    /// </remarks>
    public readonly struct CrowdSettings
    {
        /// <param name="separationStrength">
        /// How strongly an overlap pushes two zombies apart, relative to walking speed. Zero turns
        /// separation off entirely and skips the neighbour search.
        /// </param>
        /// <param name="cellSize">
        /// Side length of a neighbour-search bucket, in world units. Must be at least as large as
        /// the widest zombie, or a neighbour could sit outside the nine cells that are searched.
        /// </param>
        public CrowdSettings(float separationStrength = 1.6f, float cellSize = 1f)
        {
            if (separationStrength < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(separationStrength),
                    separationStrength,
                    "A negative push would pull them together.");
            }

            if (cellSize <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cellSize), cellSize, "A bucket needs a size.");
            }

            SeparationStrength = separationStrength;
            CellSize = cellSize;
        }

        /// <summary>How strongly overlapping zombies push apart.</summary>
        public float SeparationStrength { get; }

        /// <summary>Side length of one neighbour-search bucket.</summary>
        public float CellSize { get; }

        /// <summary>A sensible crowd: they jostle, they do not merge.</summary>
        /// <remarks>
        /// For tests and for code constructing a manager on its own. The game does not come
        /// through here: a zone's director builds these from its BalanceConfig, which is where the
        /// tuned values live.
        /// Spelled out rather than written as <c>new()</c>: on a struct that calls the implicit
        /// zero constructor, not this type's one with its default arguments, and the result is a
        /// separation strength of zero — which is exactly the bug this field exists to prevent.
        /// </remarks>
        public static CrowdSettings Default => new(1.6f, 1f);

        /// <summary>No separation at all. Cheaper, and the horde becomes one body.</summary>
        public static CrowdSettings None => new(0f, 1f);
    }
}
