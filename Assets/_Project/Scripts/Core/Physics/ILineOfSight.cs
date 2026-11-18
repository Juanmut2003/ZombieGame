#nullable enable
using UnityEngine;

namespace ZombieGame.Core.Physics
{
    /// <summary>Answers whether one point can be seen from another.</summary>
    /// <remarks>
    /// An interface because this is the one part of perception that needs the physics engine, and
    /// the rest should not have to drag a scene along to be tested. A test substitutes a wall
    /// wherever it likes; the game uses <see cref="Physics2DLineOfSight"/>.
    /// </remarks>
    public interface ILineOfSight
    {
        /// <summary>True when nothing blocks the straight line between the two points.</summary>
        bool IsClear(Vector2 from, Vector2 to);
    }

    /// <summary>Nothing ever blocks anything. The default when no walls have been set up.</summary>
    public sealed class OpenLineOfSight : ILineOfSight
    {
        /// <summary>The one instance anyone needs; it holds no state.</summary>
        public static readonly OpenLineOfSight Instance = new();

        /// <inheritdoc />
        public bool IsClear(Vector2 from, Vector2 to) => true;
    }
}
