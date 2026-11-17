#nullable enable
using UnityEngine;
using UnityEngine.UI;
using ZombieGame.Core;
using ZombieGame.Core.Events;
using ZombieGame.Waves;

namespace ZombieGame.UI
{
    /// <summary>
    /// Shows how much attention the zone is paying, as a bar that fills with noise.
    /// </summary>
    /// <remarks>
    /// PROJECT.md §5 asks for a single visible meter, and visible is the operative word: a player
    /// who cannot see what sprinting costs will sprint everywhere, and then the stealth half of
    /// the game never happens. The colour carries the stage, so the reading is available without
    /// comparing bar lengths across minutes.
    /// Same split as the stamina bar — the bus tells it a number, it owns no gameplay state, and
    /// nothing in gameplay knows it exists.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class HeatBarPresenter : MonoBehaviour
    {
        [Tooltip("The coloured part of the bar. Its width is driven from the right anchor.")]
        [SerializeField]
        private Image? _fill;

        [Tooltip("Optional label, shown as the name of the current stage.")]
        [SerializeField]
        private Text? _label;

        [Tooltip("Colour while nothing has noticed.")]
        [SerializeField]
        private Color _calmColour = new(0.45f, 0.55f, 0.62f, 1f);

        [Tooltip("Colour once the zone has noticed.")]
        [SerializeField]
        private Color _stirredColour = new(0.85f, 0.69f, 0.33f, 1f);

        [Tooltip("Colour once it is hunting.")]
        [SerializeField]
        private Color _huntingColour = new(0.82f, 0.31f, 0.25f, 1f);

        private EventBus? _bus;
        private RectTransform? _fillRect;

        private void Awake()
        {
            if (_fill is not null)
            {
                _fillRect = (RectTransform)_fill.transform;
            }
        }

        private void Start()
        {
            GameServices.TryGet(out _bus);
            if (_bus is null)
            {
                Debug.LogWarning("[HUD] no event bus; the heat bar will not move.");
                return;
            }

            _bus.Subscribe<HeatChangedEvent>(OnHeatChanged);
        }

        private void OnDestroy() => _bus?.Unsubscribe<HeatChangedEvent>(OnHeatChanged);

        /// <summary>Puts the bar at a fraction of full, for tests and for the first draw.</summary>
        public void Show(float fraction, HeatStage stage)
        {
            if (_fillRect is null || _fill is null)
            {
                return;
            }

            float clamped = Mathf.Clamp01(fraction);

            _fillRect.anchorMin = Vector2.zero;
            _fillRect.anchorMax = new Vector2(clamped, 1f);
            _fillRect.offsetMin = Vector2.zero;
            _fillRect.offsetMax = Vector2.zero;

            _fill.color = stage switch
            {
                HeatStage.Hunting => _huntingColour,
                HeatStage.Stirred => _stirredColour,
                _ => _calmColour,
            };

            if (_label is not null)
            {
                _label.text = stage switch
                {
                    HeatStage.Hunting => "HUNTING",
                    HeatStage.Stirred => "STIRRED",
                    _ => "CALM",
                };
            }
        }

        private void OnHeatChanged(HeatChangedEvent heat) => Show(heat.Value, heat.Stage);
    }
}
