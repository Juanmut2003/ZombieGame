#nullable enable
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ZombieGame.Core.Rendering;
using ZombieGame.Editor.Art;

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
        /// <summary>
        /// The design resolution the UI is laid out against — the same one the world renders at, so
        /// a UI pixel and a world pixel are the same size on screen.
        /// </summary>
        public static readonly Vector2 ReferenceResolution =
            new(PixelPerfectSettings.ReferenceWidth, PixelPerfectSettings.ReferenceHeight);

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

            // Above everything in the world. Sprites and tilemaps sort in the same default layer,
            // and a HUD at order zero disappears behind whatever wall the character walks past —
            // which is exactly when the player needs to read it.
            canvas.sortingOrder = 1000;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            // The HUD art is drawn at eighteen pixels to the unit, and a nine-slice border is
            // measured in those. Leave this at the default hundred and a two-pixel border is
            // stretched to eleven, which on a bar ten pixels tall is the whole bar.
            scaler.referencePixelsPerUnit = HudArt.PixelsPerUnit;

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
        /// <remarks>
        /// Gives back the one the scene already has rather than adding a second. Two event
        /// systems in a scene is not an error Unity raises; it is one that shows up as clicks
        /// landing roughly half the time, and more than one thing in a zone needs clicking now.
        /// </remarks>
        public static EventSystem CreateEventSystem(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                EventSystem existing = root.GetComponentInChildren<EventSystem>(includeInactive: true);
                if (existing != null)
                {
                    return existing;
                }
            }

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

            // The pack's pixel font where it is installed. A HUD drawn at 64 pixels to the tile
            // and then labelled in a smooth system face reads as two games at once.
            text.font = HudArt.PixelFont()
                        ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.color = colour;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        /// <summary>
        /// Creates a bar pinned to the bottom-left corner and returns the part that fills it.
        /// </summary>
        /// <remarks>
        /// Anchored to the corner rather than positioned by hand, so it stays where it belongs at
        /// any window size. The fill is a plain image resized by its anchors; see
        /// <c>StaminaBarPresenter</c> for why it is not a filled image.
        /// </remarks>
        /// <param name="parent">The canvas the bar belongs to.</param>
        /// <param name="name">Name of the bar's root object.</param>
        /// <param name="size">Width and height of the bar in reference pixels.</param>
        /// <param name="margin">Distance from the bottom-left corner of the screen.</param>
        /// <param name="trackColour">The empty bar behind the fill.</param>
        /// <param name="fillColour">Starting colour of the fill.</param>
        public static Image CreateCornerBar(
            Transform parent,
            string name,
            Vector2 size,
            Vector2 margin,
            Color trackColour,
            Color fillColour)
        {
            var barObject = new GameObject(name, typeof(RectTransform));
            barObject.transform.SetParent(parent, worldPositionStays: false);

            var rect = (RectTransform)barObject.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = margin;
            rect.sizeDelta = size;

            Image track = barObject.AddComponent<Image>();
            track.color = trackColour;
            track.raycastTarget = false;

            // The pack's own track and fill, stretched by their nine-slice borders so the ends
            // keep their shape at any width. Without the art both stay flat rectangles, which is
            // what the HUD looked like before.
            Sprite? trackSprite = HudArt.BarTrack();
            if (trackSprite is not null)
            {
                track.sprite = trackSprite;
                track.type = Image.Type.Sliced;
                track.color = Color.white;
            }

            var fillObject = new GameObject($"{name} Fill", typeof(RectTransform));
            fillObject.transform.SetParent(barObject.transform, worldPositionStays: false);

            var fillRect = (RectTransform)fillObject.transform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.pivot = Vector2.zero;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            Image fill = fillObject.AddComponent<Image>();
            fill.color = fillColour;
            fill.raycastTarget = false;

            Sprite? fillSprite = HudArt.BarFill();
            if (fillSprite is not null)
            {
                fill.sprite = fillSprite;
                fill.type = Image.Type.Sliced;
            }

            // Inset, so the track reads as a frame with something inside it rather than as a
            // second bar behind the first.
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);
            return fill;
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
                28,
                labelColour);
            text.raycastTarget = false;

            return button;
        }
    }
}
