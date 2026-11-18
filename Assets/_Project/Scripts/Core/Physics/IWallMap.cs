#nullable enable
using UnityEngine;

namespace ZombieGame.Core.Physics
{
    /// <summary>Answers whether a spot in the world is solid.</summary>
    /// <remarks>
    /// Asked hundreds of times a step by the horde, so it is a lookup rather than a physics
    /// query: a zone's walls do not move, and a grid of booleans answers in constant time where a
    /// collider cast would cost an order of magnitude more for the same answer.
    /// An interface because the thing that knows the walls is a tilemap, which belongs to the
    /// scene, while the thing that asks is a plain class that must stay testable without one.
    /// </remarks>
    public interface IWallMap
    {
        /// <summary>True when something solid stands at this spot.</summary>
        bool IsSolid(Vector2 point);
    }

    /// <summary>Open ground everywhere. The default, and what tests use by not passing one.</summary>
    public sealed class OpenGround : IWallMap
    {
        /// <summary>The one instance; it holds no state.</summary>
        public static readonly OpenGround Instance = new();

        private OpenGround()
        {
        }

        /// <inheritdoc />
        public bool IsSolid(Vector2 point) => false;
    }
}
