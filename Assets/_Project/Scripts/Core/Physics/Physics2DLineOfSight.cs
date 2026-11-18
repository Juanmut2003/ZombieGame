#nullable enable
using UnityEngine;

namespace ZombieGame.Core.Physics
{
    /// <summary>Asks the physics engine whether a wall is in the way.</summary>
    /// <remarks>
    /// A linecast rather than a raycast with a distance: the question is about the segment between
    /// two known points, and phrasing it that way removes the chance of a mismatched length.
    /// Allocation-free — the hit buffer is reused, which matters when hundreds of zombies ask this
    /// every tick.
    /// Sight is filtered by layer: only what sits on <c>Walls</c> is asked about, so the
    /// character being watched is not in the answer at all. The <c>ignore</c> transform is
    /// a belt-and-braces filter for whoever puts a target on a blocking layer anyway — the
    /// line ends inside the thing being looked at, so its own collider is always on it.
    /// </remarks>
    public sealed class Physics2DLineOfSight : ILineOfSight
    {
        private readonly RaycastHit2D[] _hits = new RaycastHit2D[4];
        private readonly ContactFilter2D _filter;
        private readonly Transform? _ignore;

        /// <param name="blockers">The layers that stop sight.</param>
        /// <param name="ignore">
        /// Whatever is being looked at. Its own colliders never count as blocking.
        /// </param>
        public Physics2DLineOfSight(LayerMask blockers, Transform? ignore = null)
        {
            _filter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = blockers,
                useTriggers = false,
            };

            _ignore = ignore;
        }

        /// <inheritdoc />
        public bool IsClear(Vector2 from, Vector2 to)
        {
            int count = Physics2D.Linecast(from, to, _filter, _hits);
            for (int i = 0; i < count; i++)
            {
                Collider2D collider = _hits[i].collider;
                if (collider == null)
                {
                    continue;
                }

                if (_ignore != null && collider.transform.IsChildOf(_ignore))
                {
                    continue;
                }

                return false;
            }

            return true;
        }
    }
}
