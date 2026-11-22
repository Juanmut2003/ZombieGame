#nullable enable
using UnityEngine;
using UnityEngine.InputSystem;
using ZombieGame.Core;
using ZombieGame.Core.Config;
using ZombieGame.Core.Input;
using ZombieGame.Economy;

namespace ZombieGame.Base
{
    /// <summary>
    /// What can be done with a building that is already standing: improve it, or pull it down.
    /// </summary>
    /// <remarks>
    /// One selection and two keys. Clicking anything on the plot picks it out — the same click
    /// that puts somebody to work, because from where the player sits there is one gesture and
    /// it means "this one".
    /// Tearing down gives part of the materials back. Not all of them: a building taken apart
    /// in a hurry is not a building unbuilt, and a full refund would make placement free to get
    /// wrong, which is the one decision a small plot is supposed to make you think about.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class BuildingActions : MonoBehaviour
    {
        [Tooltip("The camera the plot is seen through.")]
        [SerializeField]
        private Camera? _camera;

        [Tooltip("The plot.")]
        [SerializeField]
        private BuildSiteView? _siteView;

        [Tooltip("The colony, so a torn-down building does not leave people working in rubble.")]
        [SerializeField]
        private ColonyRuntime? _colony;

        [Tooltip("Who is holding a building, so a click meant for placing is left alone.")]
        [SerializeField]
        private BuildPlacer? _placer;

        [Tooltip("Where the buildings in the scene live, for finding the one to change.")]
        [SerializeField]
        private Transform? _built;

        [Tooltip("Where the refund fraction comes from.")]
        [SerializeField]
        private BalanceConfig? _balance;

        private Stockpile? _stores;
        private PointerFocus? _pointer;

        /// <summary>Which building is picked out, or zero when none is.</summary>
        public int Selected { get; private set; }

        /// <summary>What was last said about an upgrade, for the line on screen.</summary>
        public PlacementVerdict UpgradeVerdict { get; private set; } = PlacementVerdict.Ok;

        private void Start()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            if (_balance == null)
            {
                Debug.LogWarning("[Buildings] no BalanceConfig; using the defaults.");
                _balance = BalanceConfig.CreateDefault();
            }

            GameServices.TryGet(out _stores);
            GameServices.TryGet(out _pointer);
        }

        private void Update()
        {
            if (_siteView == null)
            {
                return;
            }

            ReadClick();
            ReadKeys();
            Judge();
        }

        /// <summary>How much of what was paid comes back when a building is torn down.</summary>
        public float RefundShare => _balance == null ? 0.5f : _balance.DemolitionRefund;

        /// <summary>The people, for the two options about work. Null outside the colony scene.</summary>
        public Colony? People => _colony == null ? null : _colony.Colony;

        /// <summary>Picks a building out by its number. Zero picks nothing.</summary>
        public void Select(int id) => Selected = id;

        /// <summary>Does one of the four things the wheel offers to the picked building.</summary>
        /// <returns>True when something actually changed.</returns>
        public bool Do(BuildingOption option) => option switch
        {
            BuildingOption.Improve => Upgrade(),
            BuildingOption.PutToWork => PutToWork(),
            BuildingOption.SendHome => SendHome(),
            _ => Demolish(),
        };

        /// <summary>Sends somebody idle to work in the picked building.</summary>
        public bool PutToWork() =>
            Selected != 0 && People?.PutAnyoneToWork(Selected) is not null;

        /// <summary>Takes one worker off the picked building.</summary>
        public bool SendHome() => Selected != 0 && People?.SendOneHome(Selected) is not null;

        /// <summary>
        /// Takes the picked building down, hands part of its cost back and frees its people.
        /// </summary>
        /// <returns>True when something came down.</returns>
        public bool Demolish()
        {
            if (_siteView == null || Selected == 0)
            {
                return false;
            }

            PlacedBuilding? standing = _siteView.Site.Find(Selected);
            if (standing is null)
            {
                Selected = 0;
                return false;
            }

            PlacedBuilding gone = standing.Value;
            if (!_siteView.Site.Remove(Selected))
            {
                return false;
            }

            Refund(gone);
            Clear(gone.Id);

            // Before anything else gets that number. A colonist left holding it would count
            // against the posts of whatever is built next.
            _colony?.Colony?.SendHomeFromRubble();

            Selected = 0;
            return true;
        }

        /// <summary>
        /// Puts the picked building up a level, if there is room and the stores can pay.
        /// </summary>
        /// <returns>True when it grew.</returns>
        public bool Upgrade()
        {
            if (_siteView == null || Selected == 0)
            {
                return false;
            }

            PlacedBuilding? standing = _siteView.Site.Find(Selected);
            if (standing is null || standing.Value.Definition == null)
            {
                return false;
            }

            PlacedBuilding was = standing.Value;
            BuildingDefinition what = was.Definition!;
            if (!what.CanUpgradeFrom(was.Level))
            {
                return false;
            }

            BuildingLevel? next = what.Level(was.Level + 1);
            if (next is null)
            {
                return false;
            }

            if (_siteView.Site.CheckUpgrade(was.Id, next.Footprint) != PlacementVerdict.Ok)
            {
                return false;
            }

            if (_stores is not null && !_stores.CanAfford(next.UpgradeCost))
            {
                return false;
            }

            if (!_siteView.Site.TryUpgrade(was.Id, next.Footprint))
            {
                return false;
            }

            // Only once the ground is actually claimed, the same way building is paid for.
            _stores?.TrySpend(next.UpgradeCost);
            Redraw(was.Id);
            return true;
        }

        private void ReadClick()
        {
            if (_placer != null && _placer.Holding != null)
            {
                return;
            }

            // A click on the wheel is a click on the building the wheel is drawn over.
            // Letting it through here would unpick the building the moment it was pressed.
            if (_pointer is not null && _pointer.OverTheHud)
            {
                return;
            }

            if (_camera == null || Mouse.current is null
                || !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            Vector2 screen = Mouse.current.position.ReadValue();
            Vector3 world = _camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 0f));
            Vector2Int cell = _siteView!.Site.CellAt(world);
            Selected = _siteView.Site.OccupantAt(cell.x, cell.y);
        }

        private void ReadKeys()
        {
            Keyboard? keyboard = Keyboard.current;
            if (keyboard is null || Selected == 0)
            {
                return;
            }

            if (keyboard.gKey.wasPressedThisFrame)
            {
                Upgrade();
            }

            if (keyboard.xKey.wasPressedThisFrame)
            {
                Demolish();
            }
        }

        private void Judge()
        {
            if (_siteView == null || Selected == 0)
            {
                UpgradeVerdict = PlacementVerdict.Ok;
                return;
            }

            PlacedBuilding? standing = _siteView.Site.Find(Selected);
            if (standing is null || standing.Value.Definition == null)
            {
                UpgradeVerdict = PlacementVerdict.Ok;
                return;
            }

            BuildingLevel? next = standing.Value.Definition!.Level(standing.Value.Level + 1);
            if (next is null || !standing.Value.Definition!.CanUpgradeFrom(standing.Value.Level))
            {
                UpgradeVerdict = PlacementVerdict.Ok;
                return;
            }

            PlacementVerdict ground = _siteView.Site.CheckUpgrade(Selected, next.Footprint);
            UpgradeVerdict = ground != PlacementVerdict.Ok
                ? ground
                : _stores is null || _stores.CanAfford(next.UpgradeCost)
                    ? PlacementVerdict.Ok
                    : PlacementVerdict.CannotAfford;
        }

        private void Refund(PlacedBuilding gone)
        {
            if (_stores is null || gone.Definition == null)
            {
                return;
            }

            float share = _balance!.DemolitionRefund;
            foreach (ResourceAmount line in gone.Definition.PaidUpTo(gone.Level))
            {
                _stores.Add(line.Kind, Mathf.FloorToInt(line.Amount * share));
            }
        }

        private void Clear(int id)
        {
            BuildingView? view = Find(id);
            if (view != null)
            {
                Destroy(view.gameObject);
            }
        }

        private void Redraw(int id)
        {
            if (_siteView == null)
            {
                return;
            }

            PlacedBuilding? standing = _siteView.Site.Find(id);
            BuildingView? view = Find(id);
            if (standing is null || view == null)
            {
                return;
            }

            view.Show(standing.Value, _siteView.Site);
        }

        private BuildingView? Find(int id)
        {
            Transform? parent = _built == null ? null : _built;
            if (parent == null)
            {
                return null;
            }

            foreach (BuildingView view in parent.GetComponentsInChildren<BuildingView>(true))
            {
                if (view.Id == id)
                {
                    return view;
                }
            }

            return null;
        }
    }
}
