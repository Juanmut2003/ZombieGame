#nullable enable

namespace ZombieGame.Core.SceneFlow
{
    /// <summary>
    /// The names of the scenes the game loads. One place to change a name, and the single source
    /// of truth that the editor-side scene paths are built from.
    /// </summary>
    public static class GameScenes
    {
        /// <summary>Bootstrap: brings the persistent systems up, then hands over to the menu.</summary>
        public const string Boot = "Boot";

        /// <summary>The main menu.</summary>
        public const string MainMenu = "MainMenu";

        /// <summary>The colony scene.</summary>
        public const string Base = "Base";

        /// <summary>The strategic map of Europe.</summary>
        public const string WorldMap = "WorldMap";

        /// <summary>A playground for trying things out. Not part of the game.</summary>
        public const string Sandbox = "Sandbox";

        /// <summary>The first handcrafted mission zone: a supply depot to loot and leave.</summary>
        public const string MissionDepot = "Mission_Depot";
    }
}
