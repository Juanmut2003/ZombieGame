#nullable enable
using System;
using UnityEngine;
using ZombieGame.Core.Rendering;

namespace ZombieGame.Characters
{
    /// <summary>
    /// Keeps track of which way a character is turned, and plays the matching walk.
    /// </summary>
    /// <remarks>
    /// Four views — front, back and the two sides — and as many drawings per view as the art
    /// happens to have. A set with one drawing in it is shown as a standing pose and left to
    /// <see cref="CharacterGait"/> to make it look alive, which is what every character in the
    /// game had until the soldier arrived with a real side walk.
    /// The frame comes from the gait's own phase rather than from a timer of its own. That is
    /// the whole trick: the stride, the lift and the footstep sound are then three readings of
    /// one number, so the foot that lands is the foot that is drawn down and heard.
    /// The direction is remembered when the character stops, so standing still faces wherever
    /// the last step went instead of snapping back to the front. A swing goes the same way.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class CharacterFacing : MonoBehaviour
    {
        [Tooltip("The sprite to change. Defaults to the first one found below this object.")]
        [SerializeField]
        private SpriteRenderer? _spriteRenderer;

        [Tooltip("Where in the stride the character is. Without one, the first frame is shown.")]
        [SerializeField]
        private CharacterGait? _gait;

        [Tooltip("Facing the camera, and what a character standing still falls back to.")]
        [SerializeField]
        private Sprite[] _front = Array.Empty<Sprite>();

        [Tooltip("Walking away from the camera. Falls back to the front view when empty.")]
        [SerializeField]
        private Sprite[] _back = Array.Empty<Sprite>();

        [Tooltip("Walking left.")]
        [SerializeField]
        private Sprite[] _left = Array.Empty<Sprite>();

        [Tooltip("Walking right.")]
        [SerializeField]
        private Sprite[] _right = Array.Empty<Sprite>();

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

        /// <summary>Which drawing of the current view is on screen.</summary>
        public int Frame { get; private set; }

        private void Awake()
        {
            // The sprite hangs off the character rather than sitting on it: the gait moves the
            // picture a couple of pixels and the collider must not come along. Compared with
            // Unity's own operator, since an unassigned serialized reference is not a C# null.
            _renderer = _spriteRenderer == null
                ? GetComponentInChildren<SpriteRenderer>()
                : _spriteRenderer;

            _body = GetComponent<Rigidbody2D>();

            if (_gait == null)
            {
                _gait = GetComponent<CharacterGait>();
            }

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
                // camera the instant they stop reads as being startled every time. The pose is
                // still refreshed, so stopping settles onto the standing frame.
                Apply(Direction);
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

        /// <summary>
        /// Hands the character a different set of drawings, for when the player picks who
        /// they are playing. Any view left empty keeps what it had.
        /// </summary>
        public void Wear(Sprite[]? front, Sprite[]? back, Sprite[]? left, Sprite[]? right)
        {
            if (front is { Length: > 0 })
            {
                _front = front;
            }

            if (back is { Length: > 0 })
            {
                _back = back;
            }

            if (left is { Length: > 0 })
            {
                _left = left;
            }

            if (right is { Length: > 0 })
            {
                _right = right;
            }

            Apply(Direction);
        }

        private void Apply(Vector2 direction)
        {
            Direction = direction;

            if (_renderer == null)
            {
                return;
            }

            Sprite[] view = ViewFor(direction);
            if (view.Length == 0)
            {
                return;
            }

            // Standing still parks the gait at zero, so this lands on the first drawing of the
            // set — which is why the first drawing of every walk is the one to stand in.
            float phase = _gait == null ? 0f : _gait.Phase;
            Frame = StepCycle.Frame(phase, view.Length);

            Sprite pose = view[Frame];
            if (pose != null && _renderer.sprite != pose)
            {
                _renderer.sprite = pose;
            }
        }

        private Sprite[] ViewFor(Vector2 direction)
        {
            if (Mathf.Abs(direction.x) >= _sidewaysThreshold)
            {
                Sprite[] sideways = direction.x > 0f ? _right : _left;
                return sideways.Length > 0 ? sideways : _front;
            }

            // Walking up the screen is walking away, which is the one thing a top-down game can
            // show without a turn animation. Art that has no back view keeps facing the camera.
            if (direction.y > 0f && _back.Length > 0)
            {
                return _back;
            }

            return _front;
        }
    }
}
