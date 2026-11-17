#nullable enable
using UnityEngine;
using ZombieGame.Core.Clock;

namespace ZombieGame.Core.Rendering
{
    /// <summary>
    /// Keeps the camera on whatever it has been told to watch.
    /// </summary>
    /// <remarks>
    /// Runs in <c>LateUpdate</c>, after everything has moved for this frame: following in
    /// <c>Update</c> would chase last frame's position and show up as a camera that judders one
    /// frame behind the player.
    /// Snapping to the pixel grid is deliberately not done here — the Pixel Perfect Camera
    /// component does it for the whole view, and a second snap on top would fight it.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class CameraFollow : MonoBehaviour
    {
        [Tooltip("What to keep in view. Usually the main character.")]
        [SerializeField]
        private Transform? _target;

        [Tooltip("How fast the camera closes the gap, per second. 0 pins it in place.")]
        [SerializeField]
        [Min(0f)]
        private float _sharpness = 8f;

        [Tooltip("How far the target may drift from the centre before the camera follows.")]
        [SerializeField]
        [Min(0f)]
        private float _deadzoneRadius = 0.5f;

        private GameClock? _clock;

        /// <summary>What the camera is watching. Null parks it where it stands.</summary>
        public Transform? Target
        {
            get => _target;
            set => _target = value;
        }

        private void Awake() => GameServices.TryGet(out _clock!);

        private void LateUpdate()
        {
            if (_target is null)
            {
                return;
            }

            // A paused game should hold still. The pause lives in the clock, never in
            // Time.timeScale, so the camera has to ask rather than rely on deltaTime going to zero.
            if (_clock is { IsPaused: true })
            {
                return;
            }

            Vector3 position = transform.position;
            Vector2 next = CameraFollowMath.Advance(
                new Vector2(position.x, position.y),
                new Vector2(_target.position.x, _target.position.y),
                new CameraFollowSettings(_sharpness, _deadzoneRadius),
                Time.deltaTime);

            // The z stays put: in a 2D scene it is the camera's distance from the world, not
            // something the follow has any business changing.
            transform.position = new Vector3(next.x, next.y, position.z);
        }

        /// <summary>Puts the camera on its target immediately, with no easing.</summary>
        /// <remarks>Used when a scene starts or the view jumps somewhere else entirely.</remarks>
        public void SnapToTarget()
        {
            if (_target is null)
            {
                return;
            }

            Vector3 position = transform.position;
            transform.position = new Vector3(_target.position.x, _target.position.y, position.z);
        }
    }
}
