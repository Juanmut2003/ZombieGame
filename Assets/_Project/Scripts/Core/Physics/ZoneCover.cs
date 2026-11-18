#nullable enable
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ZombieGame.Core.Physics
{
    /// <summary>
    /// Reads a zone's scrub tilemap once and offers it as somewhere to disappear into.
    /// </summary>
    /// <remarks>
    /// The twin of <see cref="ZoneWalls"/>, and deliberately a separate object: the two grids
    /// answer different questions about the same zone, and keeping them apart is what lets a
    /// hedge hide a character without also stopping one.
    /// Baked in <c>Awake</c>, before anything has moved, and never updated.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class ZoneCover : MonoBehaviour
    {
        [Tooltip("The tilemap holding the scrub.")]
        [SerializeField]
        private Tilemap? _scrub;

        private CoverGrid? _grid;

        /// <summary>Where this zone hides people.</summary>
        public ICoverMap Map => _grid ?? (ICoverMap)OpenSight.Instance;

        private void Awake()
        {
            if (_scrub is null)
            {
                return;
            }

            _scrub.CompressBounds();
            BoundsInt bounds = _scrub.cellBounds;
            if (bounds.size.x <= 0 || bounds.size.y <= 0)
            {
                return;
            }

            Vector3 corner = _scrub.CellToWorld(bounds.min);
            _grid = new CoverGrid(new Vector2(corner.x, corner.y), bounds.size.x, bounds.size.y);

            for (int row = 0; row < bounds.size.y; row++)
            {
                for (int column = 0; column < bounds.size.x; column++)
                {
                    var cell = new Vector3Int(bounds.xMin + column, bounds.yMin + row, 0);
                    if (_scrub.HasTile(cell))
                    {
                        _grid.SetCover(column, row);
                    }
                }
            }

            GameServices.Replace<ICoverMap>(_grid);
        }

        private void OnDestroy()
        {
            if (GameServices.TryGet(out ICoverMap? registered) && ReferenceEquals(registered, _grid))
            {
                GameServices.Unregister<ICoverMap>();
            }
        }
    }
}
