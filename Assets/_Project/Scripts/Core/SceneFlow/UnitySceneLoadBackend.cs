#nullable enable
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ZombieGame.Core.SceneFlow
{
    /// <summary>The real backend: Unity's scene manager.</summary>
    public sealed class UnitySceneLoadBackend : ISceneLoadBackend
    {
        /// <inheritdoc />
        public string ActiveSceneName => SceneManager.GetActiveScene().name ?? string.Empty;

        /// <inheritdoc />
        public ISceneLoadOperation LoadSingle(string sceneName)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single)
                                       ?? throw new InvalidOperationException(
                                           $"Unity refused to load '{sceneName}'. Is it in the build settings?");

            return new UnitySceneLoadOperation(operation);
        }

        private sealed class UnitySceneLoadOperation : ISceneLoadOperation
        {
            private readonly AsyncOperation _operation;

            public UnitySceneLoadOperation(AsyncOperation operation) => _operation = operation;

            public bool IsDone => _operation.isDone;

            /// <remarks>
            /// Unity stops at 0.9 while the scene waits to be activated, which would leave a
            /// loading bar hanging just short of full. Rescaled so 0.9 reads as done loading.
            /// </remarks>
            public float Progress => Mathf.Clamp01(_operation.progress / 0.9f);
        }
    }
}
