#nullable enable
using ZombieGame.Core.Events;

namespace ZombieGame.Core.SceneFlow
{
    /// <summary>Raised the moment a scene load begins.</summary>
    /// <remarks>
    /// Loading screens, audio fades and anything that must stop touching the old scene listen for
    /// this rather than being called directly by the loader.
    /// </remarks>
    public readonly struct SceneLoadStartedEvent : IEvent
    {
        public SceneLoadStartedEvent(string fromScene, string toScene)
        {
            FromScene = fromScene;
            ToScene = toScene;
        }

        /// <summary>The scene being left. Empty during the very first load.</summary>
        public string FromScene { get; }

        /// <summary>The scene being loaded.</summary>
        public string ToScene { get; }
    }

    /// <summary>Raised once the new scene is in and active.</summary>
    public readonly struct SceneLoadCompletedEvent : IEvent
    {
        public SceneLoadCompletedEvent(string sceneName)
        {
            SceneName = sceneName;
        }

        /// <summary>The scene that is now active.</summary>
        public string SceneName { get; }
    }
}
