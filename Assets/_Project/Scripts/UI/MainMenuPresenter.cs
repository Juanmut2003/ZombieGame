#nullable enable
using UnityEngine;
using UnityEngine.UI;
using ZombieGame.Base;
using ZombieGame.Core;
using ZombieGame.Core.SceneFlow;

namespace ZombieGame.UI
{
    /// <summary>
    /// Wires the main menu's buttons to what they actually do.
    /// </summary>
    /// <remarks>
    /// The view is the scene's canvas and buttons; this presenter holds the only logic, and even
    /// that is limited to asking a service to do something. No gameplay type knows this class
    /// exists, which is the split CLAUDE.md asks for.
    /// TODO: the menu is a placeholder — plain buttons on a flat canvas. The diegetic UI from
    /// PROJECT.md 13 comes with the UX pass in M7.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class MainMenuPresenter : MonoBehaviour
    {
        [Tooltip("Starts a run by loading the first playable scene.")]
        [SerializeField]
        private Button? _newRunButton;

        [Tooltip("Leaves the game.")]
        [SerializeField]
        private Button? _quitButton;

        [Tooltip("Scene a new run starts in.")]
        [SerializeField]
        private string _firstRunScene = GameScenes.Base;

        private SceneLoader? _sceneLoader;

        private void Awake()
        {
            // The menu can be opened on its own while working in the editor, without Boot ever
            // having run. Then there is no loader, and the button simply says so instead of
            // throwing a null reference at whoever pressed it.
            GameServices.TryGet(out _sceneLoader);

            if (_newRunButton is not null)
            {
                _newRunButton.onClick.AddListener(OnNewRun);
            }

            if (_quitButton is not null)
            {
                _quitButton.onClick.AddListener(OnQuit);
            }
        }

        private void OnDestroy()
        {
            if (_newRunButton is not null)
            {
                _newRunButton.onClick.RemoveListener(OnNewRun);
            }

            if (_quitButton is not null)
            {
                _quitButton.onClick.RemoveListener(OnQuit);
            }
        }

        private void OnNewRun()
        {
            if (_sceneLoader is null)
            {
                Debug.LogWarning("[MainMenu] no scene loader — start from the Boot scene.");
                return;
            }

            // Here and nowhere else. A run starting is what makes the colony that follows one
            // colony across however many scenes it lives through, and pressing START is the only
            // moment in the game that means "forget the last one".
            ColonyRuns.Begin();

            _sceneLoader.Load(_firstRunScene);
        }

        private void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
