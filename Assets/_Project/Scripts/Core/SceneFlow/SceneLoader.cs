#nullable enable
using System;
using ZombieGame.Core.Events;

namespace ZombieGame.Core.SceneFlow
{
    /// <summary>
    /// The one way scenes change. Everything that wants to go somewhere asks this, so there is a
    /// single place that knows what is loading and can say no.
    /// </summary>
    /// <remarks>
    /// Two loads at once is the classic way to end up with a half-built world: a button pressed
    /// twice, or a system reacting to an event by loading while a load is already running. This
    /// refuses the second one instead of racing.
    /// The loader does not pump the operation itself — a <c>MonoBehaviour</c> ticks it, because
    /// only the engine knows when a frame passed. Keeping that outside leaves the rules testable.
    /// </remarks>
    public sealed class SceneLoader
    {
        private readonly ISceneLoadBackend _backend;
        private readonly EventBus? _bus;
        private ISceneLoadOperation? _operation;

        /// <param name="backend">The engine side of loading.</param>
        /// <param name="bus">Optional; when given, load events are published on it.</param>
        public SceneLoader(ISceneLoadBackend backend, EventBus? bus = null)
        {
            _backend = backend ?? throw new ArgumentNullException(nameof(backend));
            _bus = bus;
        }

        /// <summary>True while a scene is on its way in.</summary>
        public bool IsLoading => _operation is not null;

        /// <summary>The scene being loaded, or an empty string when nothing is loading.</summary>
        public string PendingScene { get; private set; } = string.Empty;

        /// <summary>How far the current load has come, 0 to 1. Reads 1 when nothing is loading.</summary>
        public float Progress => _operation?.Progress ?? 1f;

        /// <summary>The scene that is active right now.</summary>
        public string ActiveScene => _backend.ActiveSceneName;

        /// <summary>Raised when a load starts.</summary>
        public event Action<SceneLoadStartedEvent>? LoadStarted;

        /// <summary>Raised when a load finishes.</summary>
        public event Action<SceneLoadCompletedEvent>? LoadCompleted;

        /// <summary>
        /// Starts loading a scene.
        /// </summary>
        /// <returns>
        /// False when a load is already running, or when that scene is already the active one —
        /// both are no-ops rather than errors, because they usually come from a double click.
        /// </returns>
        /// <exception cref="ArgumentException">The scene name is empty.</exception>
        public bool Load(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                throw new ArgumentException("A scene name is required.", nameof(sceneName));
            }

            if (IsLoading)
            {
                return false;
            }

            string from = _backend.ActiveSceneName;
            if (string.Equals(from, sceneName, StringComparison.Ordinal))
            {
                return false;
            }

            _operation = _backend.LoadSingle(sceneName);
            PendingScene = sceneName;

            var started = new SceneLoadStartedEvent(from, sceneName);
            LoadStarted?.Invoke(started);
            _bus?.Publish(started);

            // A backend that finishes instantly must not leave the loader stuck as "loading".
            Tick();
            return true;
        }

        /// <summary>
        /// Checks whether the running load has finished, and fires the completion event if so.
        /// Call once per frame while <see cref="IsLoading"/> is true.
        /// </summary>
        public void Tick()
        {
            if (_operation is null || !_operation.IsDone)
            {
                return;
            }

            string loaded = PendingScene;
            _operation = null;
            PendingScene = string.Empty;

            var completed = new SceneLoadCompletedEvent(loaded);
            LoadCompleted?.Invoke(completed);
            _bus?.Publish(completed);
        }
    }
}
