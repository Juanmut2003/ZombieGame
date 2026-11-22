#nullable enable
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using ZombieGame.Core;
using ZombieGame.Core.Events;
using ZombieGame.Missions;

namespace ZombieGame.UI
{
    /// <summary>
    /// The debrief: what the run came home with, the moment it ends.
    /// </summary>
    /// <remarks>
    /// Before this, a finished run said one line at the top of the HUD and left the player
    /// standing in a zone that no longer wanted anything from them, apparently still playing,
    /// until the menu loaded four seconds later. Dying has to look like dying at the instant it
    /// happens, and a run worth doing has to be worth reading about afterwards.
    /// The panel is built hidden and raised by an event rather than loaded as its own scene: a
    /// scene change would throw away the zone behind it, and seeing where you died is half of
    /// what a result screen is for.
    /// It waits for the player. The countdown that used to pull the screen away belongs to
    /// nobody who knows whether the player has finished reading, so this announces itself with
    /// <see cref="DebriefOpenedEvent"/> and releases the zone with
    /// <see cref="DebriefClosedEvent"/> when the button is pressed. The wording and the figures
    /// are <see cref="MissionDebrief"/>'s, which is plain C# and can be tested without a scene;
    /// this file only puts the strings into labels.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class MissionResultPanel : MonoBehaviour
    {
        [Tooltip("Everything that makes up the panel. Switched on when the run ends.")]
        [SerializeField]
        private GameObject? _panel;

        [Tooltip("The verdict, large.")]
        [SerializeField]
        private Text? _verdict;

        [Tooltip("What kind of run it was, in words.")]
        [SerializeField]
        private Text? _summary;

        [Tooltip("The left column of the debrief: what is being reported.")]
        [SerializeField]
        private Text? _labels;

        [Tooltip("The right column: what each line came to.")]
        [SerializeField]
        private Text? _values;

        [Tooltip("The button that leaves the zone. The run waits on it.")]
        [SerializeField]
        private Button? _continue;

        [Tooltip("Colour of a run that ended well.")]
        [SerializeField]
        private Color _wonColour = new(0.62f, 0.81f, 0.54f, 1f);

        [Tooltip("Colour of one that did not.")]
        [SerializeField]
        private Color _lostColour = new(0.84f, 0.33f, 0.3f, 1f);

        private EventBus? _bus;

        /// <summary>True once the result is up.</summary>
        public bool IsShowing { get; private set; }

        private void Start()
        {
            Hide();

            GameServices.TryGet(out _bus);
            _bus?.Subscribe<MissionEndedEvent>(OnEnded);

            if (_continue != null)
            {
                _continue.onClick.AddListener(Dismiss);
            }
        }

        private void OnDestroy()
        {
            _bus?.Unsubscribe<MissionEndedEvent>(OnEnded);

            if (_continue != null)
            {
                _continue.onClick.RemoveListener(Dismiss);
            }
        }

        /// <summary>Puts the debrief up. Public for tests and for a replay of the last run.</summary>
        public void Show(MissionEndedEvent ended)
        {
            IsShowing = true;

            if (_panel != null)
            {
                _panel.SetActive(true);
            }

            if (_verdict != null)
            {
                _verdict.text = MissionDebrief.Verdict(ended.Outcome);
                _verdict.color = ended.Extracted ? _wonColour : _lostColour;
            }

            if (_summary != null)
            {
                _summary.text = MissionDebrief.Summary(ended);
            }

            Fill(MissionDebrief.Rows(ended));

            // Said after the screen is filled, not before: whoever is listening may well act on
            // it in the same frame, and a debrief announced while still blank is a flicker.
            _bus?.Publish(new DebriefOpenedEvent());
        }

        /// <summary>Takes it down again and lets the zone move on.</summary>
        public void Dismiss()
        {
            if (!IsShowing)
            {
                return;
            }

            Hide();
            _bus?.Publish(new DebriefClosedEvent());
        }

        /// <summary>Takes it down without telling anyone. For building and for tests.</summary>
        public void Hide()
        {
            IsShowing = false;
            if (_panel != null)
            {
                _panel.SetActive(false);
            }
        }

        private void Fill(IReadOnlyList<DebriefRow> rows)
        {
            if (_labels == null && _values == null)
            {
                return;
            }

            var labels = new StringBuilder();
            var values = new StringBuilder();

            for (int i = 0; i < rows.Count; i++)
            {
                if (i > 0)
                {
                    labels.Append('\n');
                    values.Append('\n');
                }

                labels.Append(rows[i].Label);
                values.Append(rows[i].Value);
            }

            if (_labels != null)
            {
                _labels.text = labels.ToString();
            }

            if (_values != null)
            {
                _values.text = values.ToString();
            }
        }

        private void OnEnded(MissionEndedEvent ended) => Show(ended);
    }
}
