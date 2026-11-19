#nullable enable
using UnityEngine;
using UnityEngine.UI;
using ZombieGame.Core;
using ZombieGame.Core.Events;
using ZombieGame.Missions;

namespace ZombieGame.UI
{
    /// <summary>
    /// The screen that comes up the moment a run ends.
    /// </summary>
    /// <remarks>
    /// Before this, a lost run said one line at the top of the HUD and left the player standing
    /// in a zone that no longer wanted anything from them, apparently still playing, for the few
    /// seconds before the menu loaded. Dying has to look like dying at the instant it happens.
    /// The panel is built hidden and shown by an event, rather than loaded as its own scene: a
    /// scene change would throw away the zone behind it, and seeing where you died is half of
    /// what a result screen is for.
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

        [Tooltip("What came home, and how long it took.")]
        [SerializeField]
        private Text? _detail;

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
        }

        private void OnDestroy() => _bus?.Unsubscribe<MissionEndedEvent>(OnEnded);

        /// <summary>Puts the result up. Public for tests and for a debrief button later.</summary>
        public void Show(MissionEndedEvent ended)
        {
            IsShowing = true;

            if (_panel != null)
            {
                _panel.SetActive(true);
            }

            if (_verdict != null)
            {
                _verdict.text = ended.Outcome switch
                {
                    MissionOutcome.Extracted => "EXTRACTED",
                    MissionOutcome.ExtractedShort => "OUT, BUT SHORT",
                    MissionOutcome.SquadLost => "SQUAD LOST",
                    _ => "RUN OVER",
                };

                _verdict.color = ended.Extracted ? _wonColour : _lostColour;
            }

            if (_detail == null)
            {
                return;
            }

            int minutes = Mathf.FloorToInt(ended.Seconds / 60f);
            int seconds = Mathf.FloorToInt(ended.Seconds - (minutes * 60f));

            _detail.text = ended.Outcome == MissionOutcome.SquadLost
                ? $"LOST {ended.Lost} OF {ended.Wanted} SUPPLIES   {minutes}:{seconds:00}"
                : $"BANKED {ended.Banked} OF {ended.Wanted} SUPPLIES   {minutes}:{seconds:00}";
        }

        /// <summary>Takes it down again.</summary>
        public void Hide()
        {
            IsShowing = false;
            if (_panel != null)
            {
                _panel.SetActive(false);
            }
        }

        private void OnEnded(MissionEndedEvent ended) => Show(ended);
    }
}
