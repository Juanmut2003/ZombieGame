#nullable enable
using UnityEngine;
using UnityEngine.UI;
using ZombieGame.Base;

namespace ZombieGame.UI
{
    /// <summary>
    /// One line under the plot: what is in hand, and why it will not go down.
    /// </summary>
    /// <remarks>
    /// The ghost turning red says no. This says which no. Without it, a building the colony
    /// cannot afford and a building over a rock look exactly alike, and the player is left
    /// moving a shed around a field that was never the problem.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class BuildHintPresenter : MonoBehaviour
    {
        [Tooltip("Who is holding the building.")]
        [SerializeField]
        private BuildPlacer? _placer;

        [Tooltip("The line to write on.")]
        [SerializeField]
        private Text? _line;

        // Neonbyte's glyphs are white, so these are the line's whole appearance rather than a
        // tint over one. They are the HUD's dark blue at three strengths: a refusal washes out
        // towards grey instead of turning red, which is the direction the rest of the interface
        // is headed — an option that cannot be taken goes pale, not alarming.
        // Full strength is the same blue as BaseSceneBuilder's HudInk.
        [Tooltip("Colour while the spot will take it.")]
        [SerializeField]
        private Color _allowed = new(0.15f, 0.23f, 0.56f, 1f);

        [Tooltip("Colour while it will not. Pale, rather than a warning of its own.")]
        [SerializeField]
        private Color _refused = new(0.42f, 0.45f, 0.55f, 1f);

        [Tooltip("Colour while nothing is being held.")]
        [SerializeField]
        private Color _idle = new(0.28f, 0.33f, 0.52f, 1f);

        /// <summary>What the line currently says. For tests.</summary>
        public string Text => _line == null ? string.Empty : _line.text;

        [Tooltip("What can be done with a building that is already standing.")]
        [SerializeField]
        private BuildingActions? _actions;

        [Tooltip("The plot, for naming the selected building.")]
        [SerializeField]
        private BuildSiteView? _siteView;

        private void Update()
        {
            if (_line == null || _placer == null)
            {
                return;
            }

            if (_placer.Holding == null)
            {
                if (!ShowSelected())
                {
                    _line.text =
                        "1-6  BUILD    R  TURN    ESC  PUT DOWN    CLICK A BUILDING  ITS MENU";
                    _line.color = _idle;
                }

                return;
            }

            bool ok = _placer.LastVerdict == PlacementVerdict.Ok;
            _line.text = $"{_placer.Holding.DisplayName.ToUpperInvariant()}   {Say(_placer.LastVerdict)}";
            _line.color = ok ? _allowed : _refused;
        }

        /// <summary>
        /// Says what can be done with the building the player has clicked on.
        /// </summary>
        /// <remarks>
        /// The one line that makes tearing down and improving discoverable at all. Without it
        /// both are keys nobody has been told about, on a selection nobody can see.
        /// </remarks>
        private bool ShowSelected()
        {
            if (_actions == null || _siteView == null || _actions.Selected == 0 || _line == null)
            {
                return false;
            }

            PlacedBuilding? standing = _siteView.Site.Find(_actions.Selected);
            if (standing is null || standing.Value.Definition == null)
            {
                return false;
            }

            PlacedBuilding building = standing.Value;
            string name = building.Definition!.DisplayName.ToUpperInvariant();
            int top = building.Definition.TopLevel;

            if (!building.Definition.CanUpgradeFrom(building.Level))
            {
                _line.text = $"{name} {building.Level}/{top}   X  TEAR DOWN";
                _line.color = _idle;
                return true;
            }

            bool ready = _actions.UpgradeVerdict == PlacementVerdict.Ok;
            _line.text = ready
                ? $"{name} {building.Level}/{top}   G  IMPROVE   X  TEAR DOWN"
                : $"{name} {building.Level}/{top}   {Say(_actions.UpgradeVerdict)}   X  TEAR DOWN";
            _line.color = ready ? _allowed : _refused;
            return true;
        }

        private static string Say(PlacementVerdict verdict) => verdict switch
        {
            PlacementVerdict.Ok => "READY",
            PlacementVerdict.OutsideTheSite => "OFF THE PLOT",
            PlacementVerdict.GroundIsBlocked => "GROUND IS NO GOOD",
            PlacementVerdict.SomethingIsThere => "SOMETHING IS THERE",
            _ => "NOT ENOUGH IN THE STORES",
        };
    }
}
