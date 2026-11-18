#nullable enable
using UnityEngine;

namespace ZombieGame.Characters
{
    /// <summary>
    /// Works out where the player is pointing, for the weapons to attack along.
    /// </summary>
    /// <remarks>
    /// This is what makes the game twin-stick rather than tank-controlled: PROJECT.md §9 asks for
    /// "WASD move, mouse aim/fire", and the two have to be able to disagree. Backing away from a
    /// zombie while still swinging at it is the single most common thing a player does in a
    /// top-down fight, and it is impossible when attacks follow the direction of travel.
    /// The body is deliberately left alone. Which way a character is <i>turned</i> stays a
    /// question of where they are walking — that is what the three-pose art shows and what reads
    /// correctly at a glance — while where they <i>attack</i> follows the pointer. Letting the
    /// pointer spin the sprite as well made the figure look like it was sliding sideways through
    /// the room, because the pose and the direction of travel were telling different stories.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class PlayerAim : MonoBehaviour
    {
        [Tooltip("Who is steering.")]
        [SerializeField]
        private PlayerInputReader? _input;

        [Tooltip("The camera the mouse position is read through.")]
        [SerializeField]
        private Camera? _camera;

        [Tooltip("How far the stick has to be pushed before it is taken as aiming.")]
        [SerializeField]
        [Range(0.05f, 0.9f)]
        private float _stickDeadzone = 0.25f;

        private Vector2 _last = Vector2.down;

        /// <summary>
        /// Where the player is pointing, as a direction. Never zero.
        /// </summary>
        /// <remarks>
        /// Worked out when it is asked for rather than once a frame. A shot fired in the same
        /// frame the mouse moved should go where the mouse is now, not where it was when this
        /// component's Update happened to run — and script order between two components is not
        /// something a weapon should have to care about.
        /// </remarks>
        public Vector2 Direction
        {
            get
            {
                _last = Resolve();
                return _last;
            }
        }

        private void Awake()
        {
            if (_camera is null)
            {
                Debug.LogWarning($"[Aim] {name} has no camera; it will aim where it walks.");
            }
        }

        /// <summary>
        /// Works out where the player is pointing, preferring the stick while it is pushed.
        /// </summary>
        /// <remarks>
        /// The two devices say different things: a stick gives a direction, a mouse gives a point
        /// on the screen that only becomes a direction once it is measured from the character. A
        /// player on a gamepad whose mouse is lying still would otherwise be dragged towards
        /// wherever the pointer happens to rest.
        /// </remarks>
        private Vector2 Resolve()
        {
            if (_input is null)
            {
                return _last;
            }

            Vector2 stick = _input.AimStick;
            if (stick.sqrMagnitude >= _stickDeadzone * _stickDeadzone)
            {
                return stick.normalized;
            }

            if (_camera is null)
            {
                return _last;
            }

            Vector2 pointer = _camera.ScreenToWorldPoint(_input.AimScreen);
            Vector2 away = pointer - (Vector2)transform.position;

            // Standing exactly under the pointer has no direction in it; keep the last one rather
            // than snapping to a default the player did not ask for.
            return away.sqrMagnitude > 0.0001f ? away.normalized : _last;
        }
    }
}
