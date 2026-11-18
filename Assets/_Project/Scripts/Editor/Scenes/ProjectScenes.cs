#nullable enable
using ZombieGame.Core.SceneFlow;

namespace ZombieGame.Editor.Scenes
{
    /// <summary>
    /// Where the project's scenes live on disk. The names come from
    /// <see cref="GameScenes"/>, so the runtime and the editor can never disagree about what a
    /// scene is called.
    /// </summary>
    public static class ProjectScenes
    {
        /// <summary>The folder every scene is saved into.</summary>
        public const string Folder = "Assets/_Project/Scenes";

        /// <summary>Bootstrap: loads persistent systems, then hands over to the main menu.</summary>
        public static readonly string Boot = PathFor(GameScenes.Boot);

        /// <summary>The main menu.</summary>
        public static readonly string MainMenu = PathFor(GameScenes.MainMenu);

        /// <summary>The colony scene.</summary>
        public static readonly string Base = PathFor(GameScenes.Base);

        /// <summary>The strategic map of Europe.</summary>
        public static readonly string WorldMap = PathFor(GameScenes.WorldMap);

        /// <summary>
        /// A throwaway playground for trying things out. Not part of the game — it exists so there
        /// is somewhere to look at a change without touching a real scene.
        /// </summary>
        public static readonly string Sandbox = PathFor(GameScenes.Sandbox);

        /// <summary>The first handcrafted mission zone.</summary>
        public static readonly string MissionDepot = PathFor(GameScenes.MissionDepot);

        /// <summary>The asset path of a scene with that name.</summary>
        public static string PathFor(string sceneName) => $"{Folder}/{sceneName}.unity";
    }
}
