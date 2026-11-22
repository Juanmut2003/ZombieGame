#nullable enable
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ZombieGame.Core;
using ZombieGame.Core.Config;
using ZombieGame.Core.Roster;
using ZombieGame.Core.SceneFlow;
using ZombieGame.Editor.Content;
using ZombieGame.UI;

namespace ZombieGame.Editor.Scenes
{
    /// <summary>
    /// Builds the scenes the game actually boots through, and registers them in the build
    /// settings in the right order.
    /// </summary>
    /// <remarks>
    /// Run with
    /// <c>-executeMethod ZombieGame.Editor.Scenes.GameSceneBuilders.BuildAll</c>.
    /// </remarks>
    public static class GameSceneBuilders
    {
        private const string BalanceConfigPath = "Assets/_Project/Data/BalanceConfig.asset";

        private static readonly Color Ink = new(0.84f, 0.85f, 0.83f, 1f);
        private static readonly Color Muted = new(0.45f, 0.47f, 0.45f, 1f);
        private static readonly Color ButtonFill = new(0.17f, 0.19f, 0.19f, 1f);
        private static readonly Color CardFill = new(0.12f, 0.14f, 0.13f, 1f);
        private static readonly Color CardPicked = new(0.55f, 0.72f, 0.44f, 1f);

        /// <summary>Builds Boot, MainMenu and the sandbox, then fixes up the build settings.</summary>
        [MenuItem("ZombieGame/Scenes/Build All Scenes")]
        public static void BuildAll()
        {
            BuildBoot();
            BuildMainMenu();
            BaseSceneBuilder.Build();
            SandboxSceneBuilder.Build();
            MissionSceneBuilder.Build();
            RegisterInBuildSettings();
        }

        /// <summary>
        /// Builds the Boot scene: a camera and the bootstrap that brings the services up.
        /// </summary>
        [MenuItem("ZombieGame/Scenes/Build Boot Scene")]
        public static void BuildBoot()
        {
            BalanceConfig balance = GetOrCreateBalanceConfig();

            SceneBuilder.Build(ProjectScenes.Boot, scene =>
            {
                CameraRigBuilder.Create(scene);

                GameObject bootstrapObject = SceneBuilder.CreateObject(scene, "Game Bootstrap");
                GameBootstrap bootstrap = bootstrapObject.AddComponent<GameBootstrap>();

                // The fields are private and serialized, as they should be, so the reference is
                // wired the same way the inspector would do it.
                var serialized = new SerializedObject(bootstrap);
                serialized.FindProperty("_balance").objectReferenceValue = balance;
                serialized.FindProperty("_firstScene").stringValue = GameScenes.MainMenu;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            });

            Debug.Log($"[Scenes] built {ProjectScenes.Boot}");
        }

        /// <summary>
        /// One card in the character select: a frame, a face, a name and a line underneath.
        /// </summary>
        /// <remarks>
        /// The whole card is the button, because a card with a small button inside it is a thing
        /// people click on and nothing happens. The highlight is the first child, so it sits
        /// behind the picture and reads as a frame round the card rather than a pane over it.
        /// </remarks>
        private static CharacterCardView CreateCharacterCard(
            Transform parent, string name, Vector2 position, PlayableCharacter? character)
        {
            // The frame goes in before the card and beside it, not inside it. A child would be
            // drawn over the card's own background and flood it; a slightly larger sibling made
            // first is drawn behind, and all that shows is the border.
            var highlightObject = new GameObject($"{name} Highlight", typeof(RectTransform));
            highlightObject.transform.SetParent(parent, worldPositionStays: false);
            var highlightRect = (RectTransform)highlightObject.transform;
            highlightRect.anchorMin = new Vector2(0.5f, 0.5f);
            highlightRect.anchorMax = new Vector2(0.5f, 0.5f);
            highlightRect.pivot = new Vector2(0.5f, 0.5f);
            highlightRect.anchoredPosition = position;
            highlightRect.sizeDelta = new Vector2(228f, 238f);
            Image highlight = highlightObject.AddComponent<Image>();
            highlight.color = CardPicked;
            highlight.raycastTarget = false;
            highlight.enabled = false;

            var cardObject = new GameObject(name, typeof(RectTransform));
            cardObject.transform.SetParent(parent, worldPositionStays: false);

            var rect = (RectTransform)cardObject.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(220f, 230f);

            Image face = cardObject.AddComponent<Image>();
            face.color = CardFill;

            Button button = cardObject.AddComponent<Button>();
            button.targetGraphic = face;

            var portraitObject = new GameObject("Portrait", typeof(RectTransform));
            portraitObject.transform.SetParent(cardObject.transform, worldPositionStays: false);
            var portraitRect = (RectTransform)portraitObject.transform;
            portraitRect.anchorMin = new Vector2(0.5f, 0.5f);
            portraitRect.anchorMax = new Vector2(0.5f, 0.5f);
            portraitRect.pivot = new Vector2(0.5f, 0.5f);
            portraitRect.anchoredPosition = new Vector2(0f, 34f);
            portraitRect.sizeDelta = new Vector2(128f, 128f);
            Image portrait = portraitObject.AddComponent<Image>();
            portrait.raycastTarget = false;
            portrait.preserveAspect = true;

            Text label = UiSceneParts.CreateLabel(
                cardObject.transform, "Name", string.Empty,
                new Vector2(0f, -56f), new Vector2(200f, 28f), 20, Ink);
            label.raycastTarget = false;

            Text blurb = UiSceneParts.CreateLabel(
                cardObject.transform, "Blurb", string.Empty,
                new Vector2(0f, -88f), new Vector2(188f, 48f), 12, Muted);
            blurb.raycastTarget = false;

            // The one label in the game with a sentence in it rather than a word, so it is also
            // the one that has to wrap instead of running off the side of what it sits on.
            blurb.horizontalOverflow = HorizontalWrapMode.Wrap;
            blurb.alignment = TextAnchor.UpperCenter;

            CharacterCardView view = cardObject.AddComponent<CharacterCardView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("_button").objectReferenceValue = button;
            serialized.FindProperty("_portrait").objectReferenceValue = portrait;
            serialized.FindProperty("_name").objectReferenceValue = label;
            serialized.FindProperty("_blurb").objectReferenceValue = blurb;
            serialized.FindProperty("_highlight").objectReferenceValue = highlight;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Filled in here as well as at runtime, so that the saved scene already shows who
            // is on the card. A menu that is only correct once the game is running is a menu
            // nobody can lay out, and a blank white square is what it looks like meanwhile.
            view.Show(character);

            return view;
        }

        /// <summary>Builds the main menu: a title, two buttons and the presenter behind them.</summary>
        [MenuItem("ZombieGame/Scenes/Build Main Menu Scene")]
        public static void BuildMainMenu()
        {
            SceneBuilder.Build(ProjectScenes.MainMenu, scene =>
            {
                Camera camera = CameraRigBuilder.Create(scene);
                UiSceneParts.CreateEventSystem(scene);

                Canvas canvas = UiSceneParts.CreateCanvas(scene, camera);
                Transform root = canvas.transform;

                CharacterRosterBuilder.EnsureAll();

                UiSceneParts.CreateLabel(
                    root, "Title", "ZOMBIEGAME", new Vector2(0f, 214f), new Vector2(800f, 64f), 44, Ink);
                UiSceneParts.CreateLabel(
                    root,
                    "Subtitle",
                    "CHOOSE YOUR SURVIVOR",
                    new Vector2(0f, 172f),
                    new Vector2(800f, 32f),
                    18,
                    Muted);

                var roster = new[]
                {
                    AssetDatabase.LoadAssetAtPath<PlayableCharacter>(
                        CharacterRosterBuilder.SoldierPath),
                    AssetDatabase.LoadAssetAtPath<PlayableCharacter>(
                        CharacterRosterBuilder.ArcherPath),
                };

                var cards = new CharacterCardView[roster.Length];
                for (int i = 0; i < roster.Length; i++)
                {
                    float x = -125f + (i * 250f);
                    cards[i] = CreateCharacterCard(
                        root, $"Character Card {i}", new Vector2(x, 20f), roster[i]);
                }

                Text chosen = UiSceneParts.CreateLabel(
                    root, "Chosen", string.Empty, new Vector2(0f, -124f),
                    new Vector2(600f, 28f), 20, Ink);

                Button newRun = UiSceneParts.CreateButton(
                    root, "New Run Button", "START", new Vector2(0f, -170f),
                    new Vector2(320f, 56f), ButtonFill, Ink);
                Button quit = UiSceneParts.CreateButton(
                    root, "Quit Button", "Quit", new Vector2(0f, -236f),
                    new Vector2(320f, 48f), ButtonFill, Muted);

                GameObject presenterObject = SceneBuilder.CreateObject(scene, "Main Menu Presenter");
                MainMenuPresenter presenter = presenterObject.AddComponent<MainMenuPresenter>();

                var serialized = new SerializedObject(presenter);
                serialized.FindProperty("_newRunButton").objectReferenceValue = newRun;
                serialized.FindProperty("_quitButton").objectReferenceValue = quit;

                // The base, not a zone. A run starts at home and leaves from there one trip at
                // a time, PROJECT.md 4.6 — starting in a zone was the stand-in for a base that
                // had nothing in it to come back to.
                serialized.FindProperty("_firstRunScene").stringValue = GameScenes.Base;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                GameObject selectObject = SceneBuilder.CreateObject(scene, "Character Select");
                CharacterSelectPresenter select =
                    selectObject.AddComponent<CharacterSelectPresenter>();

                var serializedSelect = new SerializedObject(select);
                SerializedProperty rosterProperty = serializedSelect.FindProperty("_roster");
                rosterProperty.arraySize = roster.Length;
                SerializedProperty cardProperty = serializedSelect.FindProperty("_cards");
                cardProperty.arraySize = cards.Length;
                for (int i = 0; i < roster.Length; i++)
                {
                    rosterProperty.GetArrayElementAtIndex(i).objectReferenceValue = roster[i];
                    cardProperty.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
                }

                serializedSelect.FindProperty("_chosenLabel").objectReferenceValue = chosen;
                serializedSelect.FindProperty("_startButton").objectReferenceValue = newRun;
                serializedSelect.ApplyModifiedPropertiesWithoutUndo();

                // Shown as picked in the saved scene too, for the same reason the cards are
                // filled in: the menu is laid out by looking at it, and at rest it should look
                // like what the player will see a frame after it opens.
                for (int i = 0; i < roster.Length; i++)
                {
                    bool first = roster[i] != null && roster[i].Available;
                    cards[i].SetPicked(first);
                    if (first)
                    {
                        chosen.text = roster[i].DisplayName.ToUpperInvariant();
                        break;
                    }
                }
            });

            Debug.Log($"[Scenes] built {ProjectScenes.MainMenu}");
        }

        /// <summary>
        /// Puts the scenes into the build settings, Boot first.
        /// </summary>
        /// <remarks>
        /// Order matters: index 0 is what a built game starts with, and loading a scene by name
        /// only works if it is listed here at all.
        /// </remarks>
        [MenuItem("ZombieGame/Scenes/Register Scenes In Build Settings")]
        public static void RegisterInBuildSettings()
        {
            string[] ordered =
            {
                ProjectScenes.Boot,
                ProjectScenes.MainMenu,
                ProjectScenes.Base,
                ProjectScenes.Sandbox,
                ProjectScenes.MissionTown,
                ProjectScenes.MissionDepot,
            };

            var scenes = new List<EditorBuildSettingsScene>();
            foreach (string path in ordered)
            {
                if (File.Exists(path))
                {
                    scenes.Add(new EditorBuildSettingsScene(path, enabled: true));
                }
                else
                {
                    Debug.LogWarning($"[Scenes] '{path}' does not exist and was not registered.");
                }
            }

            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log($"[Scenes] {scenes.Count} scene(s) in the build settings.");
        }

        private static BalanceConfig GetOrCreateBalanceConfig()
        {
            var existing = AssetDatabase.LoadAssetAtPath<BalanceConfig>(BalanceConfigPath);
            if (existing is not null)
            {
                return existing;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(BalanceConfigPath)!);
            BalanceConfig created = ScriptableObject.CreateInstance<BalanceConfig>();
            AssetDatabase.CreateAsset(created, BalanceConfigPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Scenes] created {BalanceConfigPath}");
            return created;
        }
    }
}
