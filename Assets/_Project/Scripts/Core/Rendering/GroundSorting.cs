#nullable enable
using UnityEngine;

namespace ZombieGame.Core.Rendering
{
    /// <summary>
    /// Keeps one sprite sorted by how far down the screen the thing it belongs to stands.
    /// </summary>
    /// <remarks>
    /// For the handful of things that move and are not the horde — the horde does the same sum
    /// itself, once for all of it, because three hundred of these would be three hundred
    /// <c>Update</c> calls to do what one loop already does.
    /// Something that never moves can have this and never pay for it: the order is only written
    /// when it actually changes.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class GroundSorting : MonoBehaviour
    {
        [Tooltip("The sprite to sort. Defaults to the one on this object.")]
        [SerializeField]
        private SpriteRenderer? _renderer;

        private int _written = int.MinValue;

        // Compared with Unity's own operator rather than with `is null` or `??=`. A serialized
        // reference nobody filled in is not a C# null — it is a live wrapper with nothing behind
        // it — so the null-coalescing assignment quietly does nothing and every access after it
        // throws. This cost a whole PlayMode suite to find.
        private void Awake()
        {
            if (_renderer == null)
            {
                _renderer = GetComponent<SpriteRenderer>();
            }
        }

        private void LateUpdate()
        {
            if (_renderer == null)
            {
                return;
            }

            int order = SortingBands.ForGround(transform.position.y);
            if (order == _written)
            {
                return;
            }

            _written = order;
            _renderer.sortingOrder = order;
        }
    }
}
