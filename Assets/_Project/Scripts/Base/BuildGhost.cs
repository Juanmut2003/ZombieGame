#nullable enable
using UnityEngine;
using ZombieGame.Core.Rendering;

namespace ZombieGame.Base
{
    /// <summary>
    /// The building being held over the ground, before it is really there.
    /// </summary>
    /// <remarks>
    /// Shows the building itself rather than a coloured rectangle the size of its footprint.
    /// Free placement is a question of whether the thing in your hand fits between two other
    /// things, and a box is no help in answering it.
    /// Tinted by whether the spot will take it, because a ghost that looks the same everywhere
    /// turns placement into clicking and hoping.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class BuildGhost : MonoBehaviour
    {
        [Tooltip("The sprite to show. Defaults to the one on this object.")]
        [SerializeField]
        private SpriteRenderer? _renderer;

        [Tooltip("Tint while the spot will take it.")]
        [SerializeField]
        private Color _allowed = new(0.55f, 0.82f, 0.48f, 0.65f);

        [Tooltip("Tint while it will not fit.")]
        [SerializeField]
        private Color _refused = new(0.86f, 0.34f, 0.3f, 0.65f);

        /// <summary>True while a building is being held.</summary>
        public bool IsShowing => _renderer != null && _renderer.enabled;

        private void Awake()
        {
            // Unity's own operator, not `is null`: an unassigned serialized reference is a live
            // wrapper around nothing rather than a C# null.
            if (_renderer == null)
            {
                _renderer = GetComponent<SpriteRenderer>();
            }

            Hide();
        }

        /// <summary>Holds a building over a spot and says whether it may go there.</summary>
        public void Show(Sprite? sprite, Vector2 centre, bool allowed)
        {
            if (_renderer == null)
            {
                return;
            }

            _renderer.enabled = sprite != null;
            if (sprite == null)
            {
                return;
            }

            if (_renderer.sprite != sprite)
            {
                _renderer.sprite = sprite;
            }

            // Shifted by where the drawing sits relative to its pivot: building sprites pivot at
            // the bottom, so a ghost put straight onto the middle of the cells hovers half a
            // building up the plot — and the player places by the ghost, so the building would
            // land somewhere they were never shown.
            Vector3 drawn = sprite.bounds.center;
            transform.position = new Vector3(centre.x - drawn.x, centre.y - drawn.y, 0f);

            // Above anything already standing, so the thing being placed is never hidden behind
            // the thing it is being placed next to.
            _renderer.sortingOrder = SortingBands.Projectile;

            Color tint = allowed ? _allowed : _refused;
            if (_renderer.color != tint)
            {
                _renderer.color = tint;
            }
        }

        /// <summary>Puts it away.</summary>
        public void Hide()
        {
            if (_renderer != null)
            {
                _renderer.enabled = false;
            }
        }
    }
}
