#nullable enable
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using ZombieGame.Base;

namespace ZombieGame.UI
{
    /// <summary>
    /// Who is at the base, what they are doing, and how the last day went.
    /// </summary>
    /// <remarks>
    /// A colony that works invisibly is a colony nobody can run. The list is the whole interface
    /// to the job system for now: it says who is picked, where each person is, and — the line
    /// that matters most — how many are standing about doing nothing.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class ColonyPresenter : MonoBehaviour
    {
        [Tooltip("The colony being reported on.")]
        [SerializeField]
        private ColonyRuntime? _colony;

        [Tooltip("Who is picked to be given a job.")]
        [SerializeField]
        private JobAssigner? _assigner;

        [Tooltip("The plot, for naming the building somebody is working in.")]
        [SerializeField]
        private BuildSiteView? _siteView;

        [Tooltip("The list of people.")]
        [SerializeField]
        private Text? _roster;

        [Tooltip("The line about the last day.")]
        [SerializeField]
        private Text? _day;

        [Tooltip("The button that calls it a day.")]
        [SerializeField]
        private Button? _endDay;

        private readonly StringBuilder _text = new();

        /// <summary>What the roster currently says. For tests.</summary>
        public string RosterText => _roster == null ? string.Empty : _roster.text;

        /// <summary>What the day line currently says. For tests.</summary>
        public string DayText => _day == null ? string.Empty : _day.text;

        private void Start()
        {
            if (_endDay != null)
            {
                _endDay.onClick.AddListener(OnEndDay);
            }
        }

        private void OnDestroy()
        {
            if (_endDay != null)
            {
                _endDay.onClick.RemoveListener(OnEndDay);
            }
        }

        private void OnEndDay()
        {
            if (_colony != null)
            {
                _colony.EndDay();
            }
        }

        private void Update()
        {
            Colony? colony = _colony == null ? null : _colony.Colony;
            if (colony is null)
            {
                return;
            }

            ShowRoster(colony);
            ShowDay(colony);
        }

        private void ShowRoster(Colony colony)
        {
            if (_roster == null)
            {
                return;
            }

            _text.Clear();
            _text.Append("COLONY  ").Append(colony.Count).Append(" / ").Append(colony.Capacity)
                .Append(" BEDS");

            if (colony.Homeless > 0)
            {
                _text.Append("   ").Append(colony.Homeless).Append(" SLEEPING ROUGH");
            }

            Survivor? picked = _assigner == null ? null : _assigner.Picked;
            foreach (Survivor survivor in colony.People)
            {
                _text.Append('\n');
                _text.Append(picked is not null && picked.Id == survivor.Id ? "> " : "  ");
                _text.Append(survivor.Name.ToUpperInvariant());
                _text.Append("  ").Append(JobOf(survivor));
            }

            _roster.text = _text.ToString();
        }

        private string JobOf(Survivor survivor)
        {
            if (survivor.IsIdle)
            {
                return "IDLE";
            }

            if (_siteView == null)
            {
                return "WORKING";
            }

            foreach (PlacedBuilding building in _siteView.Site.Buildings)
            {
                if (building.Id == survivor.Workplace && building.Definition != null)
                {
                    string name = building.Definition.DisplayName.ToUpperInvariant();
                    return building.Rung is { HasWork: true }
                        ? $"{name} {building.Level}"
                        : $"{name} {building.Level} (NO WORK YET)";
                }
            }

            return "WORKING";
        }

        private void ShowDay(Colony colony)
        {
            if (_day == null || _colony == null)
            {
                return;
            }

            if (_colony.DaysLived == 0)
            {
                _day.text = "TAB  PICK      CLICK A BUILDING  PUT TO WORK      U  STOP";
                return;
            }

            DayReport report = _colony.LastReport;
            _text.Clear();
            _text.Append("DAY ").Append(_colony.DaysLived);
            _text.Append("   ").Append(report.Worked).Append(" WORKED, ")
                .Append(report.Idle).Append(" IDLE");
            _text.Append("   POWER ").Append(report.Power.Met).Append('/')
                .Append(report.Power.Wanted);

            // The three ways a day can go wrong, said plainly. A colony that is quietly failing
            // is one the player cannot fix.
            if (report.Darkened > 0)
            {
                _text.Append("   ").Append(report.Darkened).Append(" DARK");
            }

            if (report.WentHungry > 0)
            {
                _text.Append("   ").Append(report.WentHungry).Append(" HUNGRY");
            }

            if (report.WentThirsty > 0)
            {
                _text.Append("   ").Append(report.WentThirsty).Append(" THIRSTY");
            }

            if (report.Fed && report.Darkened == 0)
            {
                _text.Append("   +").Append(report.FoodGrown).Append(" FOOD");
            }

            _day.text = _text.ToString();
        }
    }
}
