#nullable enable
using UnityEngine;
using ZombieGame.Core.Pooling;
using ZombieGame.Core.Rendering;

namespace ZombieGame.Combat
{
    /// <summary>The visible streak of one round: a transform and a sprite.</summary>
    /// <remarks>
    /// No <c>Update</c> of its own, same as the zombie bodies. The manager owns the flight and
    /// writes here once a frame; this exists because the renderer needs something to draw.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class ProjectileView : MonoBehaviour, IPoolable
    {
        private SpriteRenderer _renderer = null!;
        private Transform _transform = null!;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _transform = transform;
        }

        /// <summary>Puts the round where it is and turns it the way it is going.</summary>
        public void Show(Vector2 position, Vector2 velocity)
        {
            _transform.position = new Vector3(position.x, position.y, 0f);

            if (velocity.sqrMagnitude > 0.000001f)
            {
                float degrees = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
                _transform.rotation = Quaternion.Euler(0f, 0f, degrees);
            }

            // Always above the bodies: a round hidden behind the zombie it is about to hit tells
            // the player nothing.
            _renderer.sortingOrder = SortingBands.Projectile;
        }

        /// <summary>The sprite every round draws with.</summary>
        public Sprite? Sprite
        {
            set
            {
                if (_renderer.sprite != value)
                {
                    _renderer.sprite = value;
                }
            }
        }

        /// <inheritdoc />
        public void OnTakenFromPool() => gameObject.SetActive(true);

        /// <inheritdoc />
        public void OnReturnedToPool() => gameObject.SetActive(false);
    }
}
