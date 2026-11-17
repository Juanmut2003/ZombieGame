#nullable enable

namespace ZombieGame.Core.SceneFlow
{
    /// <summary>One scene load in flight.</summary>
    public interface ISceneLoadOperation
    {
        /// <summary>True once the scene is in and activated.</summary>
        bool IsDone { get; }

        /// <summary>How far along the load is, 0 to 1. Drives the loading bar.</summary>
        float Progress { get; }
    }

    /// <summary>
    /// The engine side of loading a scene. Behind an interface so the loader's rules — no two
    /// loads at once, which events fire when — can be tested without starting Unity.
    /// </summary>
    public interface ISceneLoadBackend
    {
        /// <summary>The scene currently active, or an empty string before the first load.</summary>
        string ActiveSceneName { get; }

        /// <summary>Starts loading <paramref name="sceneName"/>, replacing everything loaded.</summary>
        ISceneLoadOperation LoadSingle(string sceneName);
    }
}
