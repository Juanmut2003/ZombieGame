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
        private float _lean;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _transform = transform;
        }

        /// <summary>The tint of a body that was just hit. Pale, not a cartoon flash of red.</summary>
        private static readonly Color HitTint = new(1f, 0.62f, 0.58f, 1f);

        /// <summary>Moves the body, shows the right pose, and tints it while it is reeling.</summary>
        /// <param name="position">Where the zombie is standing.</param>
        /// <param name="sprite">The pose to draw.</param>
        /// <param name="hit">Whether it is still reeling from a hit.</param>
        /// <param name="bob">How far its stride has lifted it off its feet, in world units.</param>
        /// <param name="lean">How far its stride has rocked it over, in degrees.</param>
        /// <remarks>
        /// The lift goes into the drawn position and not into the sorting below, which still
        /// uses the feet: two zombies side by side swapping which one is in front in time with
        /// their own footsteps is far more noticeable than the lift that caused it.
        /// </remarks>
        public void Show(
            Vector2 position, Sprite? sprite, bool hit = false, float bob = 0f, float lean = 0f)
        {
            _transform.position = new Vector3(position.x, position.y + bob, 0f);

            // Written only when it changes: a rotation is a quaternion rebuild and a dirty
            // transform, and this runs for every zombie in the zone every frame.
            if (lean != _lean)
            {
                _lean = lean;
                _transform.localRotation =
                    lean == 0f ? Quaternion.identity : Quaternion.Euler(0f, 0f, lean);
            }

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
        public void OnReturnedToPool()
        {
            // Stood back up before it is put away. A body is handed to whoever needs one next,
            // and inheriting the last occupant's lean would show as a zombie spawning sideways.
            _lean = 0f;
            _transform.localRotation = Quaternion.identity;

            gameObject.SetActive(false);
        }
    }
}
