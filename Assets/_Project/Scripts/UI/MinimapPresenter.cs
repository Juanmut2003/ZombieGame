#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace ZombieGame.UI
{
    /// <summary>
    /// Switches the minimap between the corner and a map you can actually read.
    /// </summary>
    /// <remarks>
    /// Both views are the same camera and the same texture, moved and zoomed. A second camera
    /// for the big one would render the zone twice for a picture only one of them is showing.
    /// The corner view answers "what is round this corner" and the wide one answers "where am I
    /// and where is the way out", which are different questions and want different zooms. Being
    /// one toggle rather than a slider keeps it a glance rather than a fiddle.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class MinimapPresenter : MonoBehaviour
    {
        [Tooltip("The framed panel the map sits in.")]
        [SerializeField]
        private RectTransform? _frame;

        [Tooltip("The camera looking down on the zone.")]
        [SerializeField]
        private Camera? _view;

        [Tooltip("The button that switches between the two.")]
        [SerializeField]
        private Button? _toggle;

        [Tooltip("What the button says in each state.")]
        [SerializeField]
        private Text? _toggleLabel;

        [Tooltip("How far the corner view sees, in world units from the middle.")]
        [SerializeField]
        [Min(1f)]
        private float _cornerZoom = 7f;

        [Tooltip("How far the wide view sees. Enough for a whole zone.")]
        [SerializeField]
        [Min(1f)]
        private float _wideZoom = 27f;

        [Tooltip("Size of the corner panel, in reference pixels.")]
        [SerializeField]
        private Vector2 _cornerSize = new(124f, 124f);

        [Tooltip("Size of the wide panel.")]
        [SerializeField]
        private Vector2 _wideSize = new(340f, 340f);

        [Tooltip("Where the corner panel sits, from the top-left corner.")]
        [SerializeField]
        private Vector2 _cornerAt = new(10f, -10f);

        /// <summary>True while the big map is up.</summary>
        public bool IsWide { get; private set; }

        private void Start()
        {
            if (_toggle != null)
            {
                _toggle.onClick.AddListener(Toggle);
            }

            // Applied once at the start rather than trusted from the scene, so the two views
            // cannot drift apart from whatever the builder happened to leave behind.
            Show(wide: false);
        }

        private void OnDestroy()
        {
            if (_toggle != null)
            {
                _toggle.onClick.RemoveListener(Toggle);
            }
        }

        /// <summary>Switches between the corner and the wide view.</summary>
        public void Toggle() => Show(!IsWide);

        /// <summary>Puts the map into one of its two views.</summary>
        public void Show(bool wide)
        {
            IsWide = wide;

            if (_view != null)
            {
                _view.orthographicSize = wide ? _wideZoom : _cornerZoom;
            }

            if (_toggleLabel != null)
            {
                _toggleLabel.text = wide ? "CLOSE" : "MAP";
            }

            if (_frame == null)
            {
                return;
            }

            if (wide)
            {
                // Centred, because a big map is something you stop and read rather than glance
                // at, and the middle of the screen is where your eyes already are.
                _frame.anchorMin = new Vector2(0.5f, 0.5f);
                _frame.anchorMax = new Vector2(0.5f, 0.5f);
                _frame.pivot = new Vector2(0.5f, 0.5f);
                _frame.anchoredPosition = Vector2.zero;
                _frame.sizeDelta = _wideSize;
                return;
            }

            _frame.anchorMin = new Vector2(0f, 1f);
            _frame.anchorMax = new Vector2(0f, 1f);
            _frame.pivot = new Vector2(0f, 1f);
            _frame.anchoredPosition = _cornerAt;
            _frame.sizeDelta = _cornerSize;
        }
    }
}
