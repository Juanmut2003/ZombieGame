#nullable enable
using UnityEngine;
using UnityEngine.UI;
using ZombieGame.Combat;
using ZombieGame.Core;
using ZombieGame.Core.Events;

namespace ZombieGame.UI
{
    /// <summary>
    /// Shows the player's stamina as a bar that drains and refills.
    /// </summary>
    /// <remarks>
    /// The bar is told its value by the bus and never asks a character for it, which is the
    /// view-and-presenter split CLAUDE.md requires: no gameplay type is referenced here beyond the
    /// event struct, and no gameplay code knows this class exists.
    /// The fill is resized by its anchors rather than by a filled <c>Image</c>, so it needs no
    /// sprite at all — a filled image with no sprite silently ignores its fill amount and shows a
    /// full bar for ever, which looks like working code.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class StaminaBarPresenter : MonoBehaviour
    {
        [Tooltip("The coloured part of the bar. Its width is driven from the right anchor.")]
        [SerializeField]
        private Image? _fill;

        [Tooltip("Colour while there is stamina to spare.")]
        [SerializeField]
        private Color _normalColour = new(0.55f, 0.72f, 0.44f, 1f);

        [Tooltip("Colour once it is nearly empty, as a warning before the sprint cuts out.")]
        [SerializeField]
        private Color _lowColour = new(0.78f, 0.35f, 0.27f, 1f);

        [Tooltip("Below this fraction the bar turns to the warning colour.")]
        [SerializeField]
        [Range(0f, 1f)]
        private float _lowFraction = 0.25f;

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
            // Subscribed in Start, not Awake: the bus is registered by the bootstrap's Awake, and
            // every Awake has run by the time the first Start does.
            GameServices.TryGet(out _bus);
            if (_bus is null)
            {
                Debug.LogWarning("[HUD] no event bus; the stamina bar will not move.");
                return;
            }

            _bus.Subscribe<StaminaChangedEvent>(OnStaminaChanged);
        }

        private void OnDestroy() => _bus?.Unsubscribe<StaminaChangedEvent>(OnStaminaChanged);

        /// <summary>Puts the bar at a fraction of full, for tests and for the first draw.</summary>
        public void Show(float fraction)
        {
            if (_fillRect is null || _fill is null)
            {
                return;
            }

            float clamped = Mathf.Clamp01(fraction);

            // Stretched from the left edge: only the right anchor moves, so the bar keeps its
            // height and its position whatever the screen size turns out to be.
            _fillRect.anchorMin = new Vector2(0f, 0f);
            _fillRect.anchorMax = new Vector2(clamped, 1f);
            _fillRect.offsetMin = Vector2.zero;
            _fillRect.offsetMax = Vector2.zero;

            _fill.color = clamped <= _lowFraction ? _lowColour : _normalColour;
        }

        private void OnStaminaChanged(StaminaChangedEvent change)
        {
            // Survivors and companions will publish the same event; this bar is the player's.
            if (!change.IsPlayer)
            {
                return;
            }

            Show(change.Fraction);
        }
    }
}
