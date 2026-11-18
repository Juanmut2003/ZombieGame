#nullable enable
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ZombieGame.Editor.Input
{
    /// <summary>
    /// Writes the project's input actions asset from code.
    /// </summary>
    /// <remarks>
    /// An <c>.inputactions</c> asset is JSON, and hand-editing it in the inspector is exactly the
    /// kind of change that produces an unreviewable diff. Generating it means the bindings are
    /// readable as code and can be rebuilt identically.
    /// Run with
    /// <c>-executeMethod ZombieGame.Editor.Input.InputActionsGenerator.Generate</c>.
    /// </remarks>
    public static class InputActionsGenerator
    {
        /// <summary>Where the generated asset lives.</summary>
        public const string AssetPath = "Assets/_Project/Settings/ZombieGame.inputactions";

        /// <summary>The map that is active while the player is in control of a character.</summary>
        public const string GameplayMap = "Gameplay";

        /// <summary>Writes the asset, replacing whatever is there.</summary>
        [MenuItem("ZombieGame/Input/Generate Input Actions")]
        public static void Generate()
        {
            InputActionAsset asset = Build();

            Directory.CreateDirectory(Path.GetDirectoryName(AssetPath)!);
            File.WriteAllText(AssetPath, asset.ToJson());
            Object.DestroyImmediate(asset);

            AssetDatabase.ImportAsset(AssetPath, ImportAssetOptions.ForceUpdate);
            Debug.Log($"[Input] wrote {AssetPath}");
        }

        /// <summary>
        /// Builds the action asset in memory. Separated from writing it so a test could inspect
        /// the result without touching the project.
        /// </summary>
        public static InputActionAsset Build()
        {
            InputActionAsset asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "ZombieGame";

            InputActionMap gameplay = asset.AddActionMap(GameplayMap);

            InputAction move = gameplay.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick");

            // Both mouse buttons swing. Left is the reflex everyone brings to a game; right is the
            // one a player who has met a shooter expects for the melee they fall back on. Once
            // firearms arrive, Fire takes the left button and this keeps the right one.
            // Now that firearms exist, the split the comment above always promised: the left
            // button is the gun, the right is the melee you fall back on when it is empty or when
            // you would rather the zone did not hear you.
            InputAction attack = gameplay.AddAction("Attack", InputActionType.Button);
            attack.AddBinding("<Mouse>/rightButton");
            attack.AddBinding("<Gamepad>/buttonWest");

            InputAction fire = gameplay.AddAction("Fire", InputActionType.Button);
            fire.AddBinding("<Mouse>/leftButton");
            fire.AddBinding("<Gamepad>/rightTrigger");

            InputAction reload = gameplay.AddAction("Reload", InputActionType.Button);
            reload.AddBinding("<Keyboard>/r");
            reload.AddBinding("<Gamepad>/buttonNorth");

            InputAction sprint = gameplay.AddAction("Sprint", InputActionType.Button);
            sprint.AddBinding("<Keyboard>/leftShift");
            sprint.AddBinding("<Gamepad>/leftStickPress");

            return asset;
        }
    }
}
