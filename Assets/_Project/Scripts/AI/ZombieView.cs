#nullable enable
using UnityEngine;
using ZombieGame.Core.Pooling;
using ZombieGame.Core.Rendering;

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

        /// <summary>The tint of a body that was just hit. Pale, not a cartoon flash of red.</summary>
        private static readonly Color HitTint = new(1f, 0.62f, 0.58f, 1f);

        /// <summary>Moves the body, shows the right pose, and tints it while it is reeling.</summary>
        public void Show(Vector2 position, Sprite? sprite, bool hit = false)
        {
            _transform.position = new Vector3(position.x, position.y, 0f);

            if (sprite != null && _renderer.sprite != sprite)
            {
                _renderer.sprite = sprite;
            }

            // Only written when it changes: assigning a renderer's colour dirties its material
            // block, and this runs for every zombie every frame.
            Color tint = hit ? HitTint : Color.white;
            if (_renderer.color != tint)
            {
                _renderer.color = tint;
            }

            // Sorted by how far down the screen it stands, so a zombie in front of another
            // overlaps it the right way round — inside a band that cannot reach the ground.
            _renderer.sortingOrder = SortingBands.ForGround(position.y);
        }

        /// <inheritdoc />
        public void OnTakenFromPool() => gameObject.SetActive(true);

        /// <inheritdoc />
        public void OnReturnedToPool() => gameObject.SetActive(false);
    }
}
