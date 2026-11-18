#nullable enable
using UnityEngine;
using UnityEngine.UI;
using ZombieGame.Combat;
using ZombieGame.Core;
using ZombieGame.Core.Events;

namespace ZombieGame.UI
{
    /// <summary>
    /// Shows the player's health as a bar that only ever goes one way.
    /// </summary>
    /// <remarks>
    /// Built like <see cref="StaminaBarPresenter"/> — told by the bus, resized by its anchors —
    /// but it reads differently on purpose: stamina comes back on its own and this does not, so
    /// the warning colour arrives earlier and the bar is the one on top.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class HealthBarPresenter : MonoBehaviour
    {
        [Tooltip("The coloured part of the bar. Its width is driven from the right anchor.")]
        [SerializeField]
        private Image? _fill;

        [Tooltip("Colour while the squad is in one piece.")]
        [SerializeField]
        private Color _normalColour = new(0.74f, 0.26f, 0.24f, 1f);

        [Tooltip("Colour once one more mistake would end the run.")]
        [SerializeField]
        private Color _lowColour = new(0.93f, 0.68f, 0.24f, 1f);

        [Tooltip("Below this fraction the bar turns to the warning colour.")]
        [SerializeField]
        [Range(0f, 1f)]
        private float _lowFraction = 0.35f;

        /// <summary>How far the fill sits inside its track, in reference pixels.</summary>
        private const float Inset = 2f;

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
                Debug.LogWarning("[HUD] no event bus; the health bar will not move.");
                return;
            }

            _bus.Subscribe<HealthChangedEvent>(OnHealthChanged);
        }

        private void OnDestroy() => _bus?.Unsubscribe<HealthChangedEvent>(OnHealthChanged);

        /// <summary>Puts the bar at a fraction of full, for tests and for the first draw.</summary>
        public void Show(float fraction)
        {
            if (_fillRect is null || _fill is null)
            {
                return;
            }

            float clamped = Mathf.Clamp01(fraction);

            _fillRect.anchorMin = new Vector2(0f, 0f);
            _fillRect.anchorMax = new Vector2(clamped, 1f);
            // Inset, so the track around it reads as a frame rather than as a second bar.
            _fillRect.offsetMin = new Vector2(Inset, Inset);
            _fillRect.offsetMax = new Vector2(-Inset, -Inset);

            _fill.color = clamped <= _lowFraction ? _lowColour : _normalColour;
        }

        private void OnHealthChanged(HealthChangedEvent change)
        {
            if (!change.IsPlayer)
            {
                return;
            }

            Show(change.Fraction);
        }
    }
}
