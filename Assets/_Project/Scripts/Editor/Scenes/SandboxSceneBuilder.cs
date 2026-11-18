#nullable enable
using UnityEditor;
using UnityEngine;
using ZombieGame.Editor.Art;
using ZombieGame.Editor.Content;

namespace ZombieGame.Editor.Scenes
{
    /// <summary>
    /// Builds the sandbox: one lit room with a horde in it and nothing to do.
    /// </summary>
    /// <remarks>
    /// The scene to open when something has to be looked at in isolation — a weapon, a crowd, a
    /// HUD element — without a mission wrapped round it. It is also the end-to-end check on the
    /// tooling: placeholders are generated, the importer forces the pixel settings onto them,
    /// skins are rebuilt from whatever art is on disk, and the scene is assembled from code.
    /// Run from the menu, or with
    /// <c>-executeMethod ZombieGame.Editor.Scenes.SandboxSceneBuilder.Build</c>.
    /// </remarks>
    public static class SandboxSceneBuilder
    {
        // Sized so the whole room, walls included, sits inside the camera's view.
        private const int RoomWidth = 13;
        private const int RoomHeight = 7;

        // Room for a real horde, but only a handful put down: enough to watch them come at you,
        // few enough that the sandbox stays readable.
        private const int HordeOnStart = 26;

        /// <summary>Generates any missing placeholders and (re)builds the sandbox scene.</summary>
        [MenuItem("ZombieGame/Scenes/Build Sandbox Scene")]
        public static void Build()
        {
            PlaceholderSpriteGenerator.GenerateAll();

            // Before anything is wired: a pack dropped into the sprite folder becomes a skin asset
            // here, and the kinds pick theirs up by name. Building a scene is the moment somebody
            // would notice a missing look, so it is the moment to go and find them.
            ZombieSkinLibrary.Rebuild();

            SceneBuilder.Build(ProjectScenes.Sandbox, scene =>
            {
                ZoneSceneParts.CreateStandaloneBootstrap(scene);
                ZoneSceneParts.CreateGlobalLight(scene);
                ZoneSceneParts.PaintTiles(scene, ZoneMaps.EmptyRoom(RoomWidth, RoomHeight));

                // Actors first: the camera is built around what it follows, and snapping it onto
                // a target that does not exist yet would just park it at the origin.
                Transform? player = ZoneSceneParts.CreateActors(scene, new Vector2(-1.5f, 0f));
                Camera camera = CameraRigBuilder.Create(scene, player);

                // After the camera, because aiming with the mouse means turning a screen point
                // into a world one, and that needs the camera that will be doing the looking.
                ZoneSceneParts.WireAim(player, camera);
                ZoneSceneParts.CreateHorde(
                    scene, player, new Vector3(3f, 1f, 0f), new Vector2(3.5f, 2f), HordeOnStart);
                ZoneSceneParts.CreateZoneHeat(scene, player);
                ZoneSceneParts.CreateProjectileRunner(scene);
                ZoneSceneParts.CreateSwingFlash(scene);
                ZoneSceneParts.CreateHud(scene, camera);
            });

            Debug.Log($"[Sandbox] built {ProjectScenes.Sandbox}");
        }
    }
}
