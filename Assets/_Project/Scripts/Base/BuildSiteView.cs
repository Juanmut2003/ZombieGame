#nullable enable
using UnityEngine;
using ZombieGame.Core;

namespace ZombieGame.Base
{
    /// <summary>
    /// The claimed plot as it exists in a scene: its size, its bad ground, and who to ask.
    /// </summary>
    /// <remarks>
    /// The grid itself is plain C# and knows nothing about scenes. This is the one object that
    /// builds it from what the site was laid out as, and hands it to whoever needs it through
    /// the service locator — the placer, the build menu, and later the jobs and the power grid,
    /// none of which should have to go looking for a transform.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class BuildSiteView : MonoBehaviour
    {
        [Tooltip("The world position of the plot's bottom-left corner.")]
        [SerializeField]
        private Vector2 _origin = Vector2.zero;

        [Tooltip("How many cells across the plot is.")]
        [SerializeField]
        [Min(1)]
        private int _width = 14;

        [Tooltip("How many cells up the plot is.")]
        [SerializeField]
        [Min(1)]
        private int _height = 8;

        [Tooltip("Cells nothing may stand on: rock, water, the road in, an existing wall.")]
        [SerializeField]
        private Vector2Int[] _blocked = System.Array.Empty<Vector2Int>();

        /// <summary>The plot. Built in Awake and the same object for the scene's life.</summary>
        public BuildSite Site { get; private set; } = null!;

        private void Awake()
        {
            Site = new BuildSite(_origin, _width, _height);
            foreach (Vector2Int cell in _blocked)
            {
                Site.Block(cell.x, cell.y);
            }

            // Replace rather than Register: re-entering the base scene would otherwise hit a
            // locator that already has a plot in it from last time.
            GameServices.Replace(Site);
        }

        private void OnDestroy()
        {
            // The plot belongs to this scene. Leaving it registered would hand the next base a
            // grid of the last one's buildings, standing on ground that is no longer there.
            GameServices.Unregister<BuildSite>();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Draws the plot and its bad ground, so a site can be laid out by looking at it.
        /// </summary>
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.55f, 0.72f, 0.44f, 0.7f);
            var size = new Vector3(_width, _height, 0f);
            Gizmos.DrawWireCube(
                new Vector3(_origin.x + (_width * 0.5f), _origin.y + (_height * 0.5f), 0f), size);

            Gizmos.color = new Color(0.84f, 0.33f, 0.3f, 0.6f);
            foreach (Vector2Int cell in _blocked)
            {
                Gizmos.DrawWireCube(
                    new Vector3(_origin.x + cell.x + 0.5f, _origin.y + cell.y + 0.5f, 0f),
                    Vector3.one);
            }
        }
#endif
    }
}
