#nullable enable
using UnityEngine;
using ZombieGame.Core.Pooling;

namespace ZombieGame.AI
{
    /// <summary>
    /// The visible body of one zombie: a transform and a sprite, and nothing else.
    /// </summary>
    /// <remarks>
    /// Deliberately has no <c>Update</c>. The manager owns the thinking and writes the position
    /// here once per frame; this object exists only because the renderer needs something to draw.
    /// Pooled, so a horde costs its peak size in objects and never allocates mid-fight.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class ZombieView : MonoBehaviour, IPoolable
    {
        private SpriteRenderer _renderer = null!;
        private Transform _transform = null!;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _transform = transform;
        }

        /// <summary>Moves the body and shows the right pose.</summary>
        public void Show(Vector2 position, Sprite? sprite)
        {
            _transform.position = new Vector3(position.x, position.y, 0f);

            if (sprite != null && _renderer.sprite != sprite)
            {
                _renderer.sprite = sprite;
            }

            // Sorted by how far down the screen it stands, so a zombie in front of another
            // overlaps it the right way round. Negated because y grows upwards.
            _renderer.sortingOrder = 10 - Mathf.RoundToInt(position.y * 10f);
        }

        /// <inheritdoc />
        public void OnTakenFromPool() => gameObject.SetActive(true);

        /// <inheritdoc />
        public void OnReturnedToPool() => gameObject.SetActive(false);
    }
}
