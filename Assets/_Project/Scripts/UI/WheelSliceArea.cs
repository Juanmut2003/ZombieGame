#nullable enable
using UnityEngine;

namespace ZombieGame.UI
{
    /// <summary>
    /// Makes one slice of the building wheel take clicks only where it is actually drawn.
    /// </summary>
    /// <remarks>
    /// A wedge sprite is a square image with a quarter of a ring in it; three quarters of that
    /// square is nothing. Left alone, each slice would swallow the clicks meant for the two
    /// beside it and the middle would belong to whichever happened to be last in the hierarchy.
    /// Tested as geometry rather than against the texture's alpha, which would need the sprite
    /// to be readable at runtime — a memory cost, and one that fails silently as a missed click
    /// if an import setting ever changes.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class WheelSliceArea : MonoBehaviour, ICanvasRaycastFilter
    {
        [Tooltip("Half the angle the slice covers, in degrees, measured from its middle.")]
        [SerializeField]
        [Range(5f, 90f)]
        private float _halfAngle = 42f;

        [Tooltip("Where the ring starts, as a fraction of its outer edge.")]
        [SerializeField]
        [Range(0f, 0.95f)]
        private float _hole = 0.4f;

        private RectTransform? _rect;

        private void Awake() => _rect = (RectTransform)transform;

        /// <inheritdoc />
        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera? eventCamera)
        {
            if (_rect == null)
            {
                _rect = (RectTransform)transform;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rect, screenPoint, eventCamera, out Vector2 local))
            {
                return false;
            }

            // Local space is the slice's own, so the drawn wedge always points straight up here
            // however the slice has been turned.
            Rect area = _rect.rect;
            float outer = Mathf.Min(area.width, area.height) * 0.5f;
            if (outer <= 0f)
            {
                return false;
            }

            float distance = local.magnitude;
            if (distance > outer || distance < outer * _hole)
            {
                return false;
            }

            return local.y / distance >= Mathf.Cos(_halfAngle * Mathf.Deg2Rad);
        }
    }
}
