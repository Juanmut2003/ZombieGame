#nullable enable
using UnityEngine;

namespace ZombieGame.Core.Physics
{
    /// <summary>Says which way to walk from here to get where the field leads.</summary>
    /// <remarks>
    /// A flow field rather than a path per body. Three hundred zombies all want to reach the same
    /// place, so the work is done once for the whole zone and each of them does a lookup: the
    /// cost of the search does not depend on how many are searching.
    /// It is also the only shape of pathfinding that stays honest with a crowd. Hand three
    /// hundred bodies their own route and they walk it in single file through the same doorway;
    /// hand them a gradient and they spread across it.
    /// </remarks>
    public interface IFlowField
    {
        /// <summary>Which way to step from <paramref name="from"/>, or zero when there is nowhere to go.</summary>
        Vector2 Direction(Vector2 from);

        /// <summary>Whether anything leads from here to where the field points.</summary>
        bool Reaches(Vector2 from);

        /// <summary>Points the field at a new goal.</summary>
        /// <returns>False when the goal is off the map or inside a wall.</returns>
        bool Rebuild(Vector2 goal);
    }

    /// <summary>Leads nowhere. The default, and what tests get by not passing one.</summary>
    public sealed class NoFlow : IFlowField
    {
        /// <summary>The one instance; it holds no state.</summary>
        public static readonly NoFlow Instance = new();

        private NoFlow()
        {
        }

        /// <inheritdoc />
        public Vector2 Direction(Vector2 from) => Vector2.zero;

        /// <inheritdoc />
        public bool Reaches(Vector2 from) => false;

        /// <inheritdoc />
        public bool Rebuild(Vector2 goal) => false;
    }
}
