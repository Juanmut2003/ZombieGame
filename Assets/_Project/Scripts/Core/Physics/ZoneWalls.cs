#nullable enable
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ZombieGame.Core.Physics
{
    /// <summary>
    /// Reads a zone's wall tilemap once and offers it as something the horde can ask.
    /// </summary>
    /// <remarks>
    /// The walls already exist twice in a zone: as tiles that are drawn and as colliders that
    /// stop the character. Neither is usable by hundreds of zombies every step — the first is
    /// rendering, the second is a physics query per body — so this bakes a third, flat
    /// representation at load and registers it for whoever needs to know what is solid.
    /// Baked in <c>Awake</c>, before anything has moved, and never updated. A wall that comes
    /// down later will have to say so; nothing in the game can knock one down yet.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class ZoneWalls : MonoBehaviour
    {
        [Tooltip("The tilemap holding the walls.")]
        [SerializeField]
        private Tilemap? _walls;

        private WallGrid? _grid;
        private GridFlowField? _routes;

        /// <summary>What is solid in this zone.</summary>
        public IWallMap Map => _grid ?? (IWallMap)OpenGround.Instance;

        /// <summary>How to get about in this zone, once somebody points it at a goal.</summary>
        public IFlowField Routes => _routes ?? (IFlowField)NoFlow.Instance;

        private void Awake()
        {
            if (_walls is null)
            {
                Debug.LogWarning($"[Walls] {name} has no tilemap; nothing in this zone is solid.");
                return;
            }

            _walls.CompressBounds();
            BoundsInt bounds = _walls.cellBounds;
            if (bounds.size.x <= 0 || bounds.size.y <= 0)
            {
                Debug.LogWarning($"[Walls] {name} has an empty tilemap.");
                return;
            }

            // The grid's origin is the world corner of the first cell, so a point can be turned
            // into a cell with one subtraction and a floor.
            Vector3 corner = _walls.CellToWorld(bounds.min);
            _grid = new WallGrid(new Vector2(corner.x, corner.y), bounds.size.x, bounds.size.y);

            for (int row = 0; row < bounds.size.y; row++)
            {
                for (int column = 0; column < bounds.size.x; column++)
                {
                    var cell = new Vector3Int(bounds.xMin + column, bounds.yMin + row, 0);
                    if (_walls.HasTile(cell))
                    {
                        _grid.SetSolid(column, row);
                    }
                }
            }

            GameServices.Replace<IWallMap>(_grid);

            // The routes over the same walls, built here because this is the object that knows
            // how big the zone is and where its corner sits. Nothing is swept yet — it is
            // pointed at a goal by whoever is doing the hunting.
            _routes = new GridFlowField(_grid.Origin, _grid.Width, _grid.Height);
            _routes.ReadWalls(_grid);
            GameServices.Replace<IFlowField>(_routes);
        }

        private void OnDestroy()
        {
            // Only if it is still ours: another zone loading first would have replaced it.
            if (GameServices.TryGet(out IWallMap? registered) && ReferenceEquals(registered, _grid))
            {
                GameServices.Unregister<IWallMap>();
            }

            if (GameServices.TryGet(out IFlowField? routes) && ReferenceEquals(routes, _routes))
            {
                GameServices.Unregister<IFlowField>();
            }
        }
    }
}
