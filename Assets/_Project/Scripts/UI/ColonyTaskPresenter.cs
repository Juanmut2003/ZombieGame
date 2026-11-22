#nullable enable
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using ZombieGame.Base;
using ZombieGame.Core.Config;

namespace ZombieGame.UI
{
    /// <summary>
    /// What the colony has been asked to do, and the button that takes the squad out.
    /// </summary>
    /// <remarks>
    /// The panel and the button belong together: three of the four things a new colony is asked
    /// for happen on the plot, and the fourth is the one that needs a way off it. Splitting them
    /// would leave the last line of the list pointing at a button somewhere else on the screen.
    /// All the judgement lives in <see cref="ColonyTasks"/>; this turns a list of them into text
    /// and keeps the button in step with whether the squad has already been out today.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class ColonyTaskPresenter : MonoBehaviour
    {
        [Tooltip("The plot, for what is standing on it.")]
        [SerializeField]
        private BuildSiteView? _siteView;

        [Tooltip("The people, for who is working.")]
        [SerializeField]
        private ColonyRuntime? _colony;

        [Tooltip("The way out of the base.")]
        [SerializeField]
        private RaidLauncher? _raid;

        [Tooltip("What the squad came home with, if they were out.")]
        [SerializeField]
        private RaidReturn? _return;

        [Tooltip("Where the haul the objectives ask for comes from.")]
        [SerializeField]
        private BalanceConfig? _balance;

        [Tooltip("The list of orders.")]
        [SerializeField]
        private Text? _list;

        [Tooltip("The button that sends the squad out.")]
        [SerializeField]
        private Button? _raidButton;

        [Tooltip("The label on that button.")]
        [SerializeField]
        private Text? _raidLabel;

        [Tooltip("Colour of a line that is still outstanding.")]
        [SerializeField]
        private Color _open = new(0.86f, 0.86f, 0.82f, 1f);

        [Tooltip("Colour of a line that is done.")]
        [SerializeField]
        private Color _done = new(0.55f, 0.72f, 0.44f, 1f);

        private readonly StringBuilder _text = new();

        /// <summary>What the panel currently says. For tests.</summary>
        public string ListText => _list == null ? string.Empty : _list.text;

        /// <summary>What the button currently says. For tests.</summary>
        public string RaidLabelText => _raidLabel == null ? string.Empty : _raidLabel.text;

        /// <summary>True while the button can be pressed.</summary>
        public bool RaidOffered => _raidButton != null && _raidButton.interactable;

        private void Start()
        {
            if (_raidButton != null)
            {
                _raidButton.onClick.AddListener(OnRaid);
            }
        }

        private void OnDestroy()
        {
            if (_raidButton != null)
            {
                _raidButton.onClick.RemoveListener(OnRaid);
            }
        }

        private void OnRaid()
        {
            if (_raid != null)
            {
                _raid.Go();
            }
        }

        private void Update()
        {
            if (_siteView == null)
            {
                return;
            }

            ShowList();
            ShowButton();
        }

        private void ShowList()
        {
            if (_list == null || _siteView == null)
            {
                return;
            }

            int wanted = _balance == null ? 0 : _balance.FirstRaidHaul;
            IReadOnlyList<ColonyTask> tasks = ColonyTasks.Describe(
                _siteView.Site,
                _colony == null ? null : _colony.Colony,
                ColonyRuns.Current,
                wanted);

            _text.Clear();
            _text.Append(ColonyTasks.AllDone(tasks) ? "ORDERS  ALL DONE" : "ORDERS");

            foreach (ColonyTask task in tasks)
            {
                _text.Append('\n');
                _text.Append(task.Done ? "[X] " : "[ ] ");
                _text.Append(task.Title);

                if (!string.IsNullOrEmpty(task.Detail))
                {
                    _text.Append("   ").Append(task.Detail);
                }
            }

            // Said here rather than in a line of its own, because the trip is one of the orders
            // and what it came to is the answer to that line.
            if (_return != null && _return.CameHome)
            {
                _text.Append("\n\n").Append(_return.Line());
            }

            _list.text = _text.ToString();
            _list.color = ColonyTasks.AllDone(tasks) ? _done : _open;
        }

        private void ShowButton()
        {
            if (_raidButton == null)
            {
                return;
            }

            bool offered = _raid != null && _raid.CanGo;
            _raidButton.interactable = offered;

            if (_raidLabel != null)
            {
                _raidLabel.text = offered ? "RAID TOWN" : "BACK TOMORROW";
            }
        }
    }
}
