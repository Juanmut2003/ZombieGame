#nullable enable
using System.IO;
using UnityEngine;
using ZombieGame.Core.Clock;
using ZombieGame.Core.Config;
using ZombieGame.Core.Events;
using ZombieGame.Core.Roster;
using ZombieGame.Core.Saving;
using ZombieGame.Core.SceneFlow;

namespace ZombieGame.Core
{
    /// <summary>
    /// Lives in the Boot scene, brings the persistent systems up, and hands over to the main menu.
    /// </summary>
    /// <remarks>
    /// Everything that must outlive a scene change is created here, in one visible order, and put
    /// into <see cref="GameServices"/>. That is the whole reason the Boot scene exists: without it
    /// each scene would have to wonder whether the systems it needs happen to exist yet.
    /// This object survives scene loads; the systems it owns are plain C# objects, not components.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Tooltip("The tunable numbers the clock and later systems run on.")]
        [SerializeField]
        private BalanceConfig? _balance;

        [Tooltip("Scene to hand over to once the systems are up.")]
        [SerializeField]
        private string _firstScene = GameScenes.MainMenu;

        private GameClock? _clock;
        private SceneLoader? _sceneLoader;

        private void Awake()
        {
            // Re-entering Boot would otherwise register everything a second time and throw.
            if (GameServices.IsRegistered<SceneLoader>())
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);

            var bus = new EventBus();
            GameServices.Register(bus);

            ClockSettings clockSettings = _balance is null
                ? new ClockSettings()
                : _balance.CreateClockSettings();
            if (_balance is null)
            {
                Debug.LogWarning(
                    "[Boot] no BalanceConfig assigned; falling back to the clock's defaults.");
            }

            _clock = new GameClock(clockSettings, bus);
            GameServices.Register(_clock);

            GameServices.Register(CreateSaveSystem());

            _sceneLoader = new SceneLoader(new UnitySceneLoadBackend(), bus);
            GameServices.Register(_sceneLoader);

            // Empty until the menu fills it. It has to exist before then so that the menu has
            // somewhere to put the player's choice, and it has to outlive the menu so that the
            // zone loading afterwards can read it.
            GameServices.Register(new CharacterSelection());

            Debug.Log($"[Boot] {GameServices.Count} services up.");
        }

        private void Start()
        {
            if (_sceneLoader is null || string.IsNullOrWhiteSpace(_firstScene))
            {
                return;
            }

            _sceneLoader.Load(_firstScene);
        }

        private void Update()
        {
            // The clock is told about unscaled time and applies its own scale, so pausing the game
            // never means touching Time.timeScale.
            _clock?.Tick(Time.unscaledDeltaTime);
            _sceneLoader?.Tick();
        }

        private void OnDestroy()
        {
            // Only the instance that registered the services clears them; a duplicate that
            // destroyed itself in Awake must not pull the real one's services out from under it.
            if (_sceneLoader is null
                || !GameServices.TryGet(out SceneLoader? registered)
                || !ReferenceEquals(registered, _sceneLoader))
            {
                return;
            }

            GameServices.Reset();
        }

        private static SaveSystem CreateSaveSystem()
        {
            string directory = Path.Combine(Application.persistentDataPath, "Saves");
            return new SaveSystem(
                new UnityJsonSerializer(prettyPrint: Debug.isDebugBuild),
                new FileSaveStore(directory),
                migrations: null,
                gameVersion: Application.version);
        }
    }
}
