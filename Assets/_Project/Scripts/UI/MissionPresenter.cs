#nullable enable
using UnityEngine;
using UnityEngine.UI;
using ZombieGame.Core;
using ZombieGame.Core.Events;
using ZombieGame.Missions;

namespace ZombieGame.UI
{
    /// <summary>
    /// Shows what the run is for, how it is going, and what the character could do right now.
    /// </summary>
    /// <remarks>
    /// Three lines, because a scavenge run only ever raises three questions: what am I here for,
    /// have I got enough, and can I leave yet. Anything more belongs on a screen the player opens
    /// rather than on the one they are fighting through.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class MissionPresenter : MonoBehaviour
    {
        [Tooltip("The objective line, at the top of the screen.")]
        [SerializeField]
        private Text? _objective;

        [Tooltip("The count of what has been found.")]
        [SerializeField]
        private Text? _supplies;

        [Tooltip("The prompt that appears next to something worth using.")]
        [SerializeField]
        private Text? _prompt;

        [Tooltip("Colour of the supply count before there is enough.")]
        [SerializeField]
        private Color _shortColour = new(0.82f, 0.81f, 0.76f, 1f);

        [Tooltip("Colour once the run has what it came for.")]
        [SerializeField]
        private Color _metColour = new(0.55f, 0.78f, 0.47f, 1f);

        private EventBus? _bus;

        private void Start()
        {
            GameServices.TryGet(out _bus);
            if (_bus is null)
            {
                Debug.LogWarning("[HUD] no event bus; the mission lines will stay blank.");
                return;
            }

            _bus.Subscribe<MissionProgressEvent>(OnProgress);
            _bus.Subscribe<MissionEndedEvent>(OnEnded);
            _bus.Subscribe<InteractionPromptEvent>(OnPrompt);
        }

        private void OnDestroy()
        {
            if (_bus is null)
            {
                return;
            }

            _bus.Unsubscribe<MissionProgressEvent>(OnProgress);
            _bus.Unsubscribe<MissionEndedEvent>(OnEnded);
            _bus.Unsubscribe<InteractionPromptEvent>(OnPrompt);
        }

        /// <summary>Writes the mission lines, for tests and for the first draw.</summary>
        public void Show(string objective, int carried, int wanted, MissionState state, float hold)
        {
            if (_objective is not null)
            {
                _objective.text = state switch
                {
                    MissionState.Extracting => $"EXTRACTING  {Mathf.RoundToInt(hold * 100f)}%",
                    MissionState.Extracted => "EXTRACTED",
                    MissionState.Failed => "LOST",
                    _ => objective.ToUpperInvariant(),
                };
            }

            if (_supplies is null)
            {
                return;
            }

            _supplies.text = $"SUPPLIES {carried} / {wanted}";
            _supplies.color = carried >= wanted ? _metColour : _shortColour;
        }

        /// <summary>Writes the interaction prompt.</summary>
        public void ShowPrompt(string text, float fraction)
        {
            if (_prompt is null)
            {
                return;
            }

            _prompt.text = fraction > 0f && text.Length > 0
                ? $"{text}  {Mathf.RoundToInt(fraction * 100f)}%"
                : text;
        }

        private void OnProgress(MissionProgressEvent progress) =>
            Show(
                progress.Objective,
                progress.Carried,
                progress.Wanted,
                progress.State,
                progress.ExtractionFraction);

        /// <summary>Writes the debrief over the mission lines.</summary>
        /// <remarks>
        /// Two lines, the same two the run used: the verdict where the objective was, and the
        /// count where the count was. Keeping them in place means the player's eyes do not have
        /// to find anything new at the one moment they are being told how it went.
        /// </remarks>
        public void ShowResult(MissionEndedEvent ended)
        {
            if (_objective is not null)
            {
                _objective.text = ended.Outcome switch
                {
                    MissionOutcome.Extracted => "EXTRACTED",
                    MissionOutcome.ExtractedShort => "OUT, BUT SHORT",
                    MissionOutcome.SquadLost => "SQUAD LOST",
                    _ => "RUN OVER",
                };
            }

            if (_supplies is null)
            {
                return;
            }

            int minutes = Mathf.FloorToInt(ended.Seconds / 60f);
            int seconds = Mathf.FloorToInt(ended.Seconds - (minutes * 60f));
            string clock = $"{minutes}:{seconds:00}";

            // What was carried and did not come home is the whole point of making extraction
            // compulsory, so a lost run says the number out loud rather than just "nothing".
            _supplies.text = ended.Outcome == MissionOutcome.SquadLost
                ? $"LOST {ended.Lost} OF {ended.Wanted}   {clock}"
                : $"BANKED {ended.Banked} OF {ended.Wanted}   {clock}";

            _supplies.color = ended.Banked >= ended.Wanted && ended.Wanted > 0
                ? _metColour
                : _shortColour;
        }

        private void OnEnded(MissionEndedEvent ended) => ShowResult(ended);

        private void OnPrompt(InteractionPromptEvent prompt) =>
            ShowPrompt(prompt.Text, prompt.Fraction);
    }
}
