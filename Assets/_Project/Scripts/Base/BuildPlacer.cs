#nullable enable
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using ZombieGame.Core;
using ZombieGame.Core.Events;
using ZombieGame.Core.Input;
using ZombieGame.Core.Rendering;
using ZombieGame.Economy;

namespace ZombieGame.Base
{
    /// <summary>
    /// Holding a building over the plot and putting it down.
    /// </summary>
    /// <remarks>
    /// The whole of free placement from the player's side: pick something, move it about, turn
    /// it, drop it. Whether a spot will take it is <see cref="BuildSite"/>'s to answer; this
    /// only asks, shows the answer, and acts on a click.
    /// Reading the mouse straight from the Input System rather than through an action map. The
    /// base has no action map yet — the Gameplay one is about walking and shooting — and
    /// inventing one for two buttons would be a guess at a layout the build menu has not been
    /// designed around. It comes when the menu does.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class BuildPlacer : MonoBehaviour
    {
        [Tooltip("The camera the plot is seen through, for turning a pointer into a place.")]
        [SerializeField]
        private Camera? _camera;

        [Tooltip("The plot being built on.")]
        [SerializeField]
        private BuildSiteView? _siteView;

        [Tooltip("The building held over the ground.")]
        [SerializeField]
        private BuildGhost? _ghost;

        [Tooltip("What everything put up is parented to, to keep the scene readable.")]
        [SerializeField]
        private Transform? _builtParent;

        [Tooltip("What can be built, in the order the number keys pick them.")]
        [SerializeField]
        private BuildingDefinition[] _catalogue = Array.Empty<BuildingDefinition>();

        private EventBus? _bus;
        private Stockpile? _stores;
        private PointerFocus? _pointer;

        /// <summary>What is being held, or null while the hands are empty.</summary>
        public BuildingDefinition? Holding { get; private set; }

        /// <summary>What the site last said about where the building is being held.</summary>
        public PlacementVerdict LastVerdict { get; private set; } = PlacementVerdict.OutsideTheSite;

        /// <summary>
        /// How much ground what is being held would take.
        /// </summary>
        /// <remarks>
        /// Always the way it is drawn. Buildings used to turn a quarter turn with <c>R</c>,
        /// which turned the ground they stood on and left the picture where it was: once the
        /// walls became real, a turned farm had its fence across the middle of its own fields
        /// and open air where the gate was drawn. The art is a room seen one way round — roof at
        /// the top, steps at the bottom — so the building does not turn. See PROJECT.md 4.2.
        /// </remarks>
        public BuildingFootprint HeldFootprint =>
            Holding == null ? new BuildingFootprint(1, 1) : Holding.Footprint;

        /// <summary>What can be built, in the order the number keys pick them.</summary>
        public BuildingDefinition[] Catalogue => _catalogue;

        /// <summary>The plot. Null until Start has run.</summary>
        public BuildSite? Site => _siteView == null ? null : _siteView.Site;

        /// <summary>True while the HUD has the pointer and a click is not meant for the plot.</summary>
        public bool OverTheHud => _pointer is not null && _pointer.OverTheHud;

        /// <summary>
        /// True when the colony can pay for what is being held.
        /// </summary>
        /// <remarks>
        /// A scene with no stores in it builds for free. That is the sandbox and the test
        /// scenes, where a price would only be in the way; the colony scene has stores, and
        /// there everything is paid for.
        /// </remarks>
        public bool CanAfford =>
            Holding == null || _stores is null || _stores.CanAfford(Holding.Cost);

        private void Start()
        {
            GameServices.TryGet(out _bus);
            GameServices.TryGet(out _stores);
            GameServices.TryGet(out _pointer);

            if (_camera == null)
            {
                _camera = Camera.main;
            }
        }

        private void Update()
        {
            ReadKeys();

            if (Holding == null)
            {
                _ghost?.Hide();
                return;
            }

            if (!TryPointer(out Vector2 pointer))
            {
                _ghost?.Hide();
                return;
            }

            BuildSite? site = Site;
            if (site is null)
            {
                return;
            }

            BuildingFootprint footprint = HeldFootprint;
            Vector2Int cell = site.CornerUnder(pointer, footprint);
            LastVerdict = Judge(footprint, cell);

            _ghost?.Show(
                Holding.Sprite, site.CentreOf(cell, footprint), LastVerdict == PlacementVerdict.Ok);

            // A click that landed on the HUD was meant for the HUD. Without this, pressing a
            // button on the building menu also drops a shed on the ground behind it.
            if (Mouse.current is not null && Mouse.current.leftButton.wasPressedThisFrame
                && !OverTheHud)
            {
                PlaceAt(pointer);
            }
        }

        /// <summary>Takes a building in hand. Null empties the hands.</summary>
        public void Hold(BuildingDefinition? what)
        {
            Holding = what;

            if (what == null)
            {
                _ghost?.Hide();
            }
        }

        /// <summary>Picks the nth thing in the catalogue, counting from zero.</summary>
        public void Hold(int index) =>
            Hold((uint)index < (uint)_catalogue.Length ? _catalogue[index] : null);

        /// <summary>
        /// Puts what is being held down, with its middle as near this point as the grid allows.
        /// </summary>
        /// <returns>True when something went up.</returns>
        public bool PlaceAt(Vector2 world)
        {
            BuildSite? site = Site;
            if (Holding == null || site is null)
            {
                return false;
            }

            BuildingFootprint footprint = HeldFootprint;
            Vector2Int cell = site.CornerUnder(world, footprint);
            LastVerdict = Judge(footprint, cell);

            if (LastVerdict != PlacementVerdict.Ok)
            {
                return false;
            }

            if (!site.TryPlace(footprint, cell, Holding, out int id))
            {
                return false;
            }

            // Only once the ground is actually claimed. Paying first and then finding the spot
            // taken is how a colony ends up poorer with nothing to show for it.
            _stores?.TrySpend(Holding.Cost);

            Raise(id);
            _bus?.Publish(new BuildingPlacedEvent(id, cell, Holding));
            return true;
        }

        /// <summary>
        /// Puts a body in the scene for a building that is already on the grid.
        /// </summary>
        /// <remarks>
        /// Public because a colony coming home from a raid has a plot full of buildings and no
        /// scene objects for any of them, and it must raise them the same way a click does.
        /// Drawing them a second way is how a restored barracks ends up sorted in front of the
        /// shed it is behind.
        /// </remarks>
        public void Raise(int id)
        {
            BuildSite? site = Site;
            if (site is null)
            {
                return;
            }

            Raise(site, id);
        }

        /// <summary>
        /// Whether what is being held may go on this cell: the ground first, then the bill.
        /// </summary>
        /// <remarks>
        /// The ground's answer comes first on purpose. Being told a spot is taken is more use
        /// than being told the colony is poor, when both are true and only one of them can be
        /// fixed by moving the mouse.
        /// </remarks>
        public PlacementVerdict Judge(BuildingFootprint footprint, Vector2Int cell)
        {
            BuildSite? site = Site;
            if (Holding == null || site is null)
            {
                return PlacementVerdict.OutsideTheSite;
            }

            PlacementVerdict ground = site.Check(footprint, cell);
            if (ground != PlacementVerdict.Ok)
            {
                return ground;
            }

            return CanAfford ? PlacementVerdict.Ok : PlacementVerdict.CannotAfford;
        }

        private void ReadKeys()
        {
            Keyboard? keyboard = Keyboard.current;
            if (keyboard is null)
            {
                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                Hold(null);
                return;
            }

            // The first six of the catalogue on the number keys. A real build menu replaces
            // this; until there is one, something has to be able to pick a building.
            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                Hold(0);
            }
            else if (keyboard.digit2Key.wasPressedThisFrame)
            {
                Hold(1);
            }
            else if (keyboard.digit3Key.wasPressedThisFrame)
            {
                Hold(2);
            }
            else if (keyboard.digit4Key.wasPressedThisFrame)
            {
                Hold(3);
            }
            else if (keyboard.digit5Key.wasPressedThisFrame)
            {
                Hold(4);
            }
            else if (keyboard.digit6Key.wasPressedThisFrame)
            {
                Hold(5);
            }
        }

        private bool TryPointer(out Vector2 world)
        {
            world = Vector2.zero;
            if (_camera == null || Mouse.current is null)
            {
                return false;
            }

            Vector2 screen = Mouse.current.position.ReadValue();
            world = _camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 0f));
            return true;
        }

        /// <summary>Puts the real thing in the scene where the ghost was.</summary>
        private void Raise(BuildSite site, int id)
        {
            PlacedBuilding? standing = site.Find(id);
            if (standing is null)
            {
                return;
            }

            // The picture hangs off the building rather than sitting on it. Building sprites
            // pivot at the bottom of the drawing — they stand on the ground, like a character —
            // while the walls and the posts are measured from the middle of the footprint. One
            // object cannot be both, and the child is the half that moves.
            var built = new GameObject("Building");
            var body = new GameObject(BuildingView.BodyName, typeof(SpriteRenderer));
            body.transform.SetParent(built.transform, worldPositionStays: false);
            built.transform.SetParent(
                _builtParent == null ? transform : _builtParent, worldPositionStays: true);

            // The view does the drawing, so that a building put up and a building improved end
            // up looking the same way rather than through two copies of the same code.
            built.AddComponent<BuildingView>().Show(standing.Value, site);
        }
    }
}
