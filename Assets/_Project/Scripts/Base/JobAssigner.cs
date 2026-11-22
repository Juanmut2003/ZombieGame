#nullable enable
using UnityEngine;
using UnityEngine.InputSystem;
using ZombieGame.Core;
using ZombieGame.Core.Input;

namespace ZombieGame.Base
{
    /// <summary>
    /// Putting people to work: pick somebody, click the building you want them in.
    /// </summary>
    /// <remarks>
    /// Shares the left mouse button with <see cref="BuildPlacer"/> and gives way to it. The rule
    /// is the one the player already has in their head: with a building in hand a click puts it
    /// down, with empty hands a click is about what is already standing there.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class JobAssigner : MonoBehaviour
    {
        [Tooltip("The camera the plot is seen through.")]
        [SerializeField]
        private Camera? _camera;

        [Tooltip("The plot.")]
        [SerializeField]
        private BuildSiteView? _siteView;

        [Tooltip("The colony whose people are being sent to work.")]
        [SerializeField]
        private ColonyRuntime? _colony;

        [Tooltip("Who is holding a building, so a click meant for placing is left alone.")]
        [SerializeField]
        private BuildPlacer? _placer;

        private PointerFocus? _pointer;

        /// <summary>Whose turn it is to be given a job, or null when nobody is picked.</summary>
        public Survivor? Picked { get; private set; }

        private void Start()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            GameServices.TryGet(out _pointer);
        }

        private void Update()
        {
            Colony? colony = _colony == null ? null : _colony.Colony;
            if (colony is null)
            {
                return;
            }

            ReadKeys(colony);

            // The placer owns the click while it is holding something.
            if (_placer != null && _placer.Holding != null)
            {
                return;
            }

            // The HUD gets first refusal on a click, the same as everywhere else on the plot.
            if (_pointer is not null && _pointer.OverTheHud)
            {
                return;
            }

            if (Mouse.current is null || !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            if (Picked is null || _camera == null || _siteView == null)
            {
                return;
            }

            Vector2 screen = Mouse.current.position.ReadValue();
            Vector3 world = _camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 0f));
            Vector2Int cell = _siteView.Site.CellAt(world);
            int building = _siteView.Site.OccupantAt(cell.x, cell.y);

            if (building != 0)
            {
                colony.Assign(Picked.Id, building);
            }
        }

        /// <summary>Picks the next person, wrapping round. Public for tests.</summary>
        public void PickNext(Colony colony)
        {
            if (colony.Count == 0)
            {
                Picked = null;
                return;
            }

            int from = 0;
            if (Picked is not null)
            {
                for (int i = 0; i < colony.People.Count; i++)
                {
                    if (colony.People[i].Id == Picked.Id)
                    {
                        from = i + 1;
                        break;
                    }
                }
            }

            Picked = colony.People[from % colony.People.Count];
        }

        private void ReadKeys(Colony colony)
        {
            Keyboard? keyboard = Keyboard.current;
            if (keyboard is null)
            {
                return;
            }

            if (keyboard.tabKey.wasPressedThisFrame)
            {
                PickNext(colony);
            }

            if (keyboard.uKey.wasPressedThisFrame && Picked is not null)
            {
                colony.Unassign(Picked.Id);
            }

            // The same thing the button does. A base is played in days, and reaching for the
            // mouse to end every one of them gets old by the third.
            if (keyboard.eKey.wasPressedThisFrame && _colony != null)
            {
                _colony.EndDay();
            }
        }
    }
}
