#nullable enable
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ZombieGame.Core;
using ZombieGame.Core.Config;
using ZombieGame.Core.SceneFlow;
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

        /// <summary>Builds Boot, MainMenu and the sandbox, then fixes up the build settings.</summary>
        [MenuItem("ZombieGame/Scenes/Build All Scenes")]
        public static void BuildAll()
        {
            BuildBoot();
            BuildMainMenu();
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

                UiSceneParts.CreateLabel(
                    root, "Title", "ZOMBIEGAME", new Vector2(0f, 140f), new Vector2(800f, 80f), 56, Ink);
                UiSceneParts.CreateLabel(
                    root,
                    "Subtitle",
                    "placeholder main menu",
                    new Vector2(0f, 88f),
                    new Vector2(800f, 40f),
                    24,
                    Muted);

                Button newRun = UiSceneParts.CreateButton(
                    root, "New Run Button", "New run", new Vector2(0f, -20f),
                    new Vector2(320f, 64f), ButtonFill, Ink);
                Button quit = UiSceneParts.CreateButton(
                    root, "Quit Button", "Quit", new Vector2(0f, -104f),
                    new Vector2(320f, 64f), ButtonFill, Muted);

                GameObject presenterObject = SceneBuilder.CreateObject(scene, "Main Menu Presenter");
                MainMenuPresenter presenter = presenterObject.AddComponent<MainMenuPresenter>();

                var serialized = new SerializedObject(presenter);
                serialized.FindProperty("_newRunButton").objectReferenceValue = newRun;
                serialized.FindProperty("_quitButton").objectReferenceValue = quit;
                serialized.FindProperty("_firstRunScene").stringValue = GameScenes.Sandbox;
                serialized.ApplyModifiedPropertiesWithoutUndo();
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
                ProjectScenes.Sandbox,
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
