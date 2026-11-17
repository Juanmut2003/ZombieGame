#nullable enable

namespace ZombieGame.Editor.Scenes
{
    /// <summary>
    /// Where the project's scenes live. One place to change a path, so a rename does not turn into
    /// a hunt through string literals.
    /// </summary>
    public static class ProjectScenes
    {
        /// <summary>The folder every scene is saved into.</summary>
        public const string Folder = "Assets/_Project/Scenes";

        /// <summary>Bootstrap: loads persistent systems, then hands over to the main menu.</summary>
        public const string Boot = Folder + "/Boot.unity";

        /// <summary>The main menu.</summary>
        public const string MainMenu = Folder + "/MainMenu.unity";

        /// <summary>The colony scene.</summary>
        public const string Base = Folder + "/Base.unity";

        /// <summary>The strategic map of Europe.</summary>
        public const string WorldMap = Folder + "/WorldMap.unity";

        /// <summary>
        /// A throwaway playground for trying things out. Not part of the game, not in the build —
        /// it exists so there is somewhere to look at a change without touching a real scene.
        /// </summary>
        public const string Sandbox = Folder + "/Sandbox.unity";
    }
}
