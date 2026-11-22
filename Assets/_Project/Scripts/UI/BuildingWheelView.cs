#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ZombieGame.Base;
using ZombieGame.Core;
using ZombieGame.Core.Input;
using ZombieGame.Economy;

namespace ZombieGame.UI
{
    /// <summary>
    /// The ring of options that opens around a building when it is clicked.
    /// </summary>
    /// <remarks>
    /// Everything a standing building can be told to do, on the building itself rather than on
    /// a line at the bottom of the screen. The keys are still there for anyone who has learnt
    /// them; this is how the rest of them are found at all.
    /// The ring is built here rather than laid out in the scene. Four slices that have to meet
    /// exactly, each with a label on its own bisector, is geometry — and geometry hand-placed in
    /// a scene file is geometry that drifts the first time somebody nudges one of them.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class BuildingWheelView : MonoBehaviour
    {
        /// <summary>Where each slice's middle points, in degrees anticlockwise from east.</summary>
        private static readonly float[] Bisectors = { 135f, 45f, 315f, 225f };

        [Tooltip("Who holds the selection and does the work.")]
        [SerializeField]
        private BuildingActions? _actions;

        [Tooltip("The plot, for finding the building the wheel is drawn around.")]
        [SerializeField]
        private BuildSiteView? _siteView;

        [Tooltip("The camera the plot is seen through.")]
        [SerializeField]
        private Camera? _camera;

        [Tooltip("The canvas the wheel is drawn on.")]
        [SerializeField]
        private Canvas? _canvas;

        [Tooltip("One quarter of the ring, drawn pointing up.")]
        [SerializeField]
        private Sprite? _wedge;

        [Tooltip("The disc in the middle.")]
        [SerializeField]
        private Sprite? _hub;

        [Tooltip("The font the HUD is set in.")]
        [SerializeField]
        private Font? _font;

        [Tooltip("How far out the ring reaches, in canvas units.")]
        [SerializeField]
        [Min(40f)]
        private float _radius = 118f;

        [Tooltip("Colour of a slice that can be pressed.")]
        [SerializeField]
        private Color _ready = new(0.78f, 0.84f, 0.74f, 1f);

        [Tooltip("Colour of a slice that would do nothing.")]
        [SerializeField]
        private Color _barred = new(0.44f, 0.45f, 0.43f, 1f);

        private RectTransform? _root;
        private RectTransform? _canvasRect;
        private Text? _plate;
        private readonly List<Image> _slices = new();
        private readonly List<Text> _titles = new();
        private readonly List<Text> _details = new();
        private PointerFocus _pointer = new();
        private Stockpile? _stores;
        private int _shown;

        /// <summary>Which building the wheel is open on, or zero while it is shut.</summary>
        public int OpenOn => _shown;

        /// <summary>What the slices currently read, top-left first. For tests.</summary>
        public IReadOnlyList<Text> Slices => _titles;

        private void Awake()
        {
            // Registered rather than looked up: this is the only thing in the project that can
            // answer whether the pointer is over the HUD, and the plot has to be able to ask.
            GameServices.Replace(_pointer);
            Build();
        }

        private void OnDestroy() => GameServices.Unregister<PointerFocus>();

        private void Start() => GameServices.TryGet(out _stores);

        private void Update()
        {
            _pointer.OverTheHud = EventSystem.current != null
                && EventSystem.current.IsPointerOverGameObject();

            int id = _actions == null ? 0 : _actions.Selected;
            if (id == 0 || _siteView == null || _root == null)
            {
                Close();
                return;
            }

            PlacedBuilding? standing = _siteView.Site.Find(id);
            if (standing is null)
            {
                Close();
                return;
            }

            _shown = id;
            _root.gameObject.SetActive(true);
            Place(standing.Value);
            Fill(id);
        }

        private void Close()
        {
            _shown = 0;
            if (_root != null)
            {
                _root.gameObject.SetActive(false);
            }
        }

        /// <summary>Moves the ring over the building it belongs to.</summary>
        private void Place(PlacedBuilding building)
        {
            if (_camera == null || _canvas == null || _canvasRect == null || _root == null
                || _siteView == null)
            {
                return;
            }

            Vector2 centre = _siteView.Site.CentreOf(building.Cell, building.Footprint);
            Vector3 screen = _camera.WorldToScreenPoint(new Vector3(centre.x, centre.y, 0f));
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRect, screen, _canvas.worldCamera, out Vector2 local))
            {
                _root.anchoredPosition = local;
            }
        }

        /// <summary>Writes what each slice offers right now.</summary>
        private void Fill(int id)
        {
            if (_siteView == null || _actions == null || _plate == null)
            {
                return;
            }

            _plate.text = BuildingMenu.Headline(_siteView.Site, id);

            IReadOnlyList<BuildingChoice> choices = BuildingMenu.Describe(
                _siteView.Site, id, _actions.People, _stores, _actions.RefundShare);

            for (int i = 0; i < _titles.Count; i++)
            {
                bool has = i < choices.Count;
                _titles[i].text = has ? choices[i].Title : string.Empty;
                _details[i].text = has ? choices[i].Detail : string.Empty;

                Color ink = has && choices[i].Allowed ? _ready : _barred;
                _titles[i].color = ink;
                _details[i].color = ink;
                _slices[i].color = has && choices[i].Allowed
                    ? Color.white
                    : new Color(1f, 1f, 1f, 0.55f);
            }
        }

        /// <summary>Puts the ring together: a hub, a name plate, and four slices.</summary>
        private void Build()
        {
            if (_canvas == null)
            {
                return;
            }

            _canvasRect = _canvas.GetComponent<RectTransform>();

            var rootObject = new GameObject("Building Wheel", typeof(RectTransform));
            _root = (RectTransform)rootObject.transform;
            _root.SetParent(_canvas.transform, worldPositionStays: false);
            _root.anchorMin = new Vector2(0.5f, 0.5f);
            _root.anchorMax = new Vector2(0.5f, 0.5f);
            _root.pivot = new Vector2(0.5f, 0.5f);
            _root.sizeDelta = new Vector2(_radius * 2f, _radius * 2f);

            for (int i = 0; i < BuildingMenu.Slices; i++)
            {
                AddSlice(i);
            }

            // After the slices, so the hub draws over where their inner corners meet.
            AddHub();

            _plate = AddText(
                _root, "Name", new Vector2(0f, _radius + 16f), new Vector2(260f, 22f), 15,
                TextAnchor.MiddleCenter);
            _plate.color = _ready;

            _root.gameObject.SetActive(false);
        }

        private void AddHub()
        {
            var hubObject = new GameObject("Hub", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)hubObject.transform;
            rect.SetParent(_root, worldPositionStays: false);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(_radius * 0.82f, _radius * 0.82f);

            var image = hubObject.GetComponent<Image>();
            image.sprite = _hub;
            image.raycastTarget = false;
        }

        private void AddSlice(int index)
        {
            var option = (BuildingOption)index;

            var sliceObject = new GameObject(
                $"Slice {option}",
                typeof(RectTransform),
                typeof(Image),
                typeof(WheelSliceArea),
                typeof(Button));
            var rect = (RectTransform)sliceObject.transform;
            rect.SetParent(_root, worldPositionStays: false);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(_radius * 2f, _radius * 2f);

            // One sprite drawn pointing up, turned into each of the four quarters. The sprite's
            // own middle is the wheel's middle, so the turn needs no offset.
            rect.localRotation = Quaternion.Euler(0f, 0f, Bisectors[index] - 90f);

            var image = sliceObject.GetComponent<Image>();
            image.sprite = _wedge;
            image.type = Image.Type.Simple;

            var button = sliceObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => _actions?.Do(option));

            // The labels hang off the wheel, not off the turned wedge, or they would be upside
            // down on half of it.
            float radians = Bisectors[index] * Mathf.Deg2Rad;
            var at = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * (_radius * 0.68f);

            Text title = AddText(
                _root, $"{option} Title", at + new Vector2(0f, 9f), new Vector2(120f, 18f), 13,
                TextAnchor.MiddleCenter);
            Text detail = AddText(
                _root, $"{option} Detail", at - new Vector2(0f, 9f), new Vector2(120f, 18f), 11,
                TextAnchor.MiddleCenter);

            _slices.Add(image);
            _titles.Add(title);
            _details.Add(detail);
        }

        private Text AddText(
            RectTransform? parent,
            string name,
            Vector2 at,
            Vector2 size,
            int fontSize,
            TextAnchor alignment)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            var rect = (RectTransform)textObject.transform;
            rect.SetParent(parent, worldPositionStays: false);
            rect.anchoredPosition = at;
            rect.sizeDelta = size;

            var text = textObject.GetComponent<Text>();
            text.font = _font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            // Labels never take a click: the wedge under them is the button.
            text.raycastTarget = false;
            return text;
        }
    }
}
