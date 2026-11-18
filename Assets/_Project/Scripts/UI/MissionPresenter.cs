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

        private void OnEnded(MissionEndedEvent ended)
        {
            if (_objective is null)
            {
                return;
            }

            _objective.text = ended.Extracted
                ? $"EXTRACTED WITH {ended.Banked}"
                : "LOST EVERYTHING";
        }

        private void OnPrompt(InteractionPromptEvent prompt) =>
            ShowPrompt(prompt.Text, prompt.Fraction);
    }
}
