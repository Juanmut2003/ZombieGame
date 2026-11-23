#nullable enable
using UnityEngine;
using ZombieGame.Core.Rendering;

namespace ZombieGame.Base
{
    /// <summary>
    /// The visible body of one building, and the number that ties it to the plot.
    /// </summary>
    /// <remarks>
    /// The plot is a grid of numbers and knows nothing about scenes; this is how the scene finds
    /// its way back. Searching the hierarchy for a name would work until somebody renamed one,
    /// and a building that cannot be found is a building that cannot be torn down.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class BuildingView : MonoBehaviour
    {
        /// <summary>What the child carrying the drawing is called.</summary>
        public const string BodyName = "Body";

        [Tooltip("The sprite to draw. Defaults to the one on the Body child.")]
        [SerializeField]
        private SpriteRenderer? _renderer;

        private readonly BoxCollider2D?[] _walls = new BoxCollider2D?[BuildingWalls.Count];

        /// <summary>Its number on the plot.</summary>
        public int Id { get; private set; }

        /// <summary>How much ground it stands on, in cells.</summary>
        public BuildingFootprint Footprint { get; private set; }

        private void Awake() => FindTheRenderer();

        private void FindTheRenderer()
        {
            if (_renderer == null)
            {
                _renderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        /// <summary>Draws a building as it now stands.</summary>
        public void Show(PlacedBuilding building, BuildSite site)
        {
            Id = building.Id;

            Vector2 centre = site.CentreOf(building.Cell, building.Footprint);
            transform.position = new Vector3(centre.x, centre.y, 0f);

            // The cells it stands on are the cells nobody walks through — not the drawing, which
            // may be a storey taller than its ground. Set here rather than once at placement, so
            // that turning a shed round and improving it into a bigger one both move the wall
            // with the building instead of leaving it where the first version stood.
            // Before the drawing, and never behind the same guard: a building with no sprite yet
            // is still a building, and one that can be walked through is worse than an invisible
            // one.
            Occupy(building.Footprint);
            Name(building);

            FindTheRenderer();
            if (_renderer == null)
            {
                return;
            }

            BuildingLevel? rung = building.Rung;
            if (rung is not null)
            {
                _renderer.sprite = rung.Sprite;
            }

            LineItUp();

            // In the building band, under every figure: these are rooms with the roof cut away,
            // and somebody who has walked in through the door has to be visible inside. Still
            // sorted by the bottom edge within that band, so a shed in front of a barn overlaps
            // it the right way round.
            _renderer.sortingOrder =
                SortingBands.ForStructure(centre.y - (building.Footprint.Height * 0.5f));
        }

        /// <summary>
        /// Puts the drawing over the ground the building stands on.
        /// </summary>
        /// <remarks>
        /// Building sprites pivot at the bottom of the drawing, the way a character pivots at
        /// its feet, while everything else about a building — its walls, its door, the posts
        /// inside it — is measured out from the middle of its footprint. Drawn straight onto
        /// the middle, a farm appeared half its own height up the plot with its walls standing
        /// in open ground below it, which is exactly how it looked.
        /// Taken from the sprite rather than from the footprint, so it stays right whatever the
        /// pivot of the next sprite anybody draws turns out to be.
        /// </remarks>
        private void LineItUp()
        {
            if (_renderer == null || _renderer.sprite == null)
            {
                return;
            }

            if (_renderer.transform == transform)
            {
                // Nothing to move: an older scene with the renderer on the building itself.
                return;
            }

            _renderer.transform.localPosition = -_renderer.sprite.bounds.center;
        }

        /// <summary>
        /// Makes the ground the building stands on solid, exactly as wide as its footprint.
        /// </summary>
        /// <remarks>
        /// A box and not the sprite, because the sprite is a picture of a building seen from
        /// above and in front at once: a barracks two cells deep is drawn three cells tall so
        /// that its roof reads, and a wall cut from the drawing would stop somebody a cell
        /// before the wall is. The cells are the truth — they are what the plot refuses to build
        /// on, and now also what nobody walks through.
        /// </remarks>
        private void Occupy(BuildingFootprint footprint)
        {
            Footprint = footprint;

            for (int i = 0; i < _walls.Length; i++)
            {
                WallSegment wall = BuildingWalls.Segment(footprint, i);

                if (_walls[i] == null)
                {
                    // Several boxes on the one object rather than a child each: nothing has to be
                    // created or destroyed when a building is turned round, and a room cannot end
                    // up with last version's walls still standing inside it.
                    _walls[i] = gameObject.AddComponent<BoxCollider2D>();
                }

                BoxCollider2D box = _walls[i]!;

                // A stretch with nothing in it — the front wall of a one-wide shed, which is all
                // doorway — is switched off rather than left as a box of size zero.
                box.enabled = wall.Exists;
                if (!wall.Exists)
                {
                    continue;
                }

                box.offset = wall.Offset;
                box.size = wall.Size;
            }
        }

        private void Name(PlacedBuilding building)
        {
            name = building.Definition == null
                ? $"Building {building.Id}"
                : $"{building.Definition.DisplayName} {building.Id} (L{building.Level})";
        }
    }
}
