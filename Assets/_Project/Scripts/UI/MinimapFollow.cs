#nullable enable
using UnityEngine;

namespace ZombieGame.UI
{
    /// <summary>
    /// Keeps the minimap camera over the squad.
    /// </summary>
    /// <remarks>
    /// A second camera rendering the same world into a texture, rather than a drawn map kept in
    /// step with the zone. A drawn one would be a second copy of the level to keep correct, and
    /// the first thing to go stale the moment a zone changes; this one cannot be wrong about the
    /// zone because it is looking at it.
    /// It deliberately does not rotate with the character. A map that spins under you is harder
    /// to read than one that always has north at the top, and this one exists to answer "which
    /// way is the way out" rather than "which way am I facing".
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class MinimapFollow : MonoBehaviour
    {
        [Tooltip("Who the map is centred on.")]
        [SerializeField]
        private Transform? _squad;

        [Tooltip("How high above the world the map camera sits.")]
        [SerializeField]
        private float _height = 40f;

        private void LateUpdate()
        {
            if (_squad == null)
            {
                return;
            }

            Vector3 at = _squad.position;
            transform.position = new Vector3(at.x, at.y, -_height);
        }
    }
}
