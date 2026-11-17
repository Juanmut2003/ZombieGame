#nullable enable
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ZombieGame.Editor.Scenes
{
    /// <summary>
    /// The handful of UI objects a scene built from code needs: a canvas, an event system, labels
    /// and buttons.
    /// </summary>
    /// <remarks>
    /// Assembling uGUI by hand is fiddly and easy to get subtly wrong — a missing raycaster, a
    /// canvas that does not scale, an event system that cannot read the new Input System. Doing it
    /// in one place means getting it right once.
    /// TODO: placeholder styling with the built-in font. The pixel font and diegetic framing come
    /// with the UX pass.
    /// </remarks>
    public static class UiSceneParts
    {
        /// <summary>The design resolution the UI is laid out against.</summary>
        public static readonly Vector2 ReferenceResolution = new(480f, 270f);

        /// <summary>Creates a screen-space canvas that scales with the window.</summary>
        /// <param name="camera">
        /// The camera the canvas renders through. Required: an Overlay canvas draws straight to
        /// the screen and never appears in a camera's render texture, which would make every
        /// automated screenshot of a menu come back empty.
        /// </param>
        public static Canvas CreateCanvas(Scene scene, Camera camera, string name = "UI Canvas")
        {
            GameObject canvasObject = SceneBuilder.CreateObject(scene, name);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        /// <summary>
        /// Creates the event system that turns input into clicks.
        /// </summary>
        /// <remarks>
        /// Uses <see cref="InputSystemUIInputModule"/>, not the legacy standalone module: the
        /// project runs on the new Input System only, and the old module would silently do
        /// nothing.
        /// </remarks>
        public static EventSystem CreateEventSystem(Scene scene)
        {
            GameObject eventSystemObject = SceneBuilder.CreateObject(scene, "EventSystem");
            EventSystem eventSystem = eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
            return eventSystem;
        }

        /// <summary>Creates a text label.</summary>
        public static Text CreateLabel(
            Transform parent,
            string name,
            string content,
            Vector2 anchoredPosition,
            Vector2 size,
            int fontSize,
            Color colour,
            TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var labelObject = new GameObject(name, typeof(RectTransform));
            labelObject.transform.SetParent(parent, worldPositionStays: false);

            var rect = (RectTransform)labelObject.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Text text = labelObject.AddComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.color = colour;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        /// <summary>Creates a button with a centred label and returns it.</summary>
        public static Button CreateButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchoredPosition,
            Vector2 size,
            Color background,
            Color labelColour)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform));
            buttonObject.transform.SetParent(parent, worldPositionStays: false);

            var rect = (RectTransform)buttonObject.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image = buttonObject.AddComponent<Image>();
            image.color = background;

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            Text text = CreateLabel(
                buttonObject.transform,
                $"{name} Label",
                label,
                Vector2.zero,
                size,
                14,
                labelColour);
            text.raycastTarget = false;

            return button;
        }
    }
}
