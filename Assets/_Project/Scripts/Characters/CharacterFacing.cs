#nullable enable
using UnityEngine;

namespace ZombieGame.Characters
{
    /// <summary>
    /// Keeps track of which way a character is turned, and shows the matching pose.
    /// </summary>
    /// <remarks>
    /// The art is three poses — front, left, right — not a walk cycle, so this switches between
    /// them rather than playing frames. A figure walking up or down keeps the front view, which is
    /// what a top-down game can get away with and what the packs actually provide.
    /// The direction is remembered when the character stops, so standing still faces wherever the
    /// last step went instead of snapping back to the front. A swing goes the same way.
    /// TODO: a real walk cycle needs more than one frame per direction. When those exist, this
    /// becomes the thing that picks the clip rather than the sprite.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class CharacterFacing : MonoBehaviour
    {
        [Tooltip("Facing the camera, and what a character standing still falls back to.")]
        [SerializeField]
        private Sprite? _front;

        [Tooltip("Walking left.")]
        [SerializeField]
        private Sprite? _left;

        [Tooltip("Walking right.")]
        [SerializeField]
        private Sprite? _right;

        [Tooltip("How fast the character has to be moving before the pose changes.")]
        [SerializeField]
        [Min(0f)]
        private float _moveThreshold = 0.15f;

        [Tooltip("How far sideways the movement has to lean before a side pose is used.")]
        [SerializeField]
        [Range(0.05f, 1f)]
        private float _sidewaysThreshold = 0.35f;

        private SpriteRenderer? _renderer;
        private Rigidbody2D? _body;

        /// <summary>The way the character is turned. Never zero.</summary>
        public Vector2 Direction { get; private set; } = Vector2.down;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _body = GetComponent<Rigidbody2D>();
            Apply(Vector2.down);
        }

        private void Update()
        {
            if (_body is null)
            {
                return;
            }

            Vector2 velocity = _body.linearVelocity;
            if (velocity.sqrMagnitude < _moveThreshold * _moveThreshold)
            {
                // Standing still keeps the last direction: a character who turns to face the
                // camera the instant they stop reads as being startled every time.
                return;
            }

            Apply(velocity.normalized);
        }

        /// <summary>Turns the character by hand, for cutscenes and tests.</summary>
        public void Face(Vector2 direction)
        {
            if (direction.sqrMagnitude > 0.000001f)
            {
                Apply(direction.normalized);
            }
        }

        private void Apply(Vector2 direction)
        {
            Direction = direction;

            if (_renderer is null)
            {
                return;
            }

            Sprite? pose = Mathf.Abs(direction.x) >= _sidewaysThreshold
                ? (direction.x > 0f ? _right : _left)
                : _front;

            if (pose != null && _renderer.sprite != pose)
            {
                _renderer.sprite = pose;
            }
        }
    }
}
