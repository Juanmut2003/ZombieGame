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
        [Tooltip("The sprite to draw. Defaults to the one on this object.")]
        [SerializeField]
        private SpriteRenderer? _renderer;

        /// <summary>Its number on the plot.</summary>
        public int Id { get; private set; }

        private void Awake()
        {
            if (_renderer == null)
            {
                _renderer = GetComponent<SpriteRenderer>();
            }
        }

        /// <summary>Draws a building as it now stands.</summary>
        public void Show(PlacedBuilding building, BuildSite site)
        {
            Id = building.Id;

            if (_renderer == null)
            {
                _renderer = GetComponent<SpriteRenderer>();
                if (_renderer == null)
                {
                    return;
                }
            }

            BuildingLevel? rung = building.Rung;
            if (rung is not null)
            {
                _renderer.sprite = rung.Sprite;
            }

            Vector2 centre = site.CentreOf(building.Cell, building.Footprint);
            transform.position = new Vector3(centre.x, centre.y, 0f);

            // Sorted by the bottom edge rather than the middle, so a shed in front of a barn
            // overlaps it the right way round however tall either of them is drawn.
            _renderer.sortingOrder =
                SortingBands.ForGround(centre.y - (building.Footprint.Height * 0.5f));

            name = building.Definition == null
                ? $"Building {building.Id}"
                : $"{building.Definition.DisplayName} {building.Id} (L{building.Level})";
        }
    }
}
