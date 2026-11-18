#nullable enable
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZombieGame.Editor.Art;
using ZombieGame.Editor.Content;
using ZombieGame.Missions;

namespace ZombieGame.Editor.Scenes
{
    /// <summary>
    /// Builds the depot: the first zone with something to do in it.
    /// </summary>
    /// <remarks>
    /// The handcrafted half of PROJECT.md §7. The rooms come from a drawn map, the systems come
    /// from <see cref="ZoneSceneParts"/>, and what is placed on top — shelves worth going
    /// through and a gate to leave by — is what makes it a mission rather than a room.
    /// Run from the menu, or with
    /// <c>-executeMethod ZombieGame.Editor.Scenes.MissionSceneBuilder.Build</c>.
    /// </remarks>
    public static class MissionSceneBuilder
    {
        private const string DepotMissionPath = "Assets/_Project/Data/Missions/DepotRun.asset";
        private const string TownMissionPath = "Assets/_Project/Data/Missions/TownRun.asset";
        private const string CrateSprite = "Assets/_Project/Art/Sprites/Items/placeholder_crate.png";
        private const string PadSprite = "Assets/_Project/Art/Sprites/FX/extraction_pad.png";

        /// <summary>Generates any missing placeholders and (re)builds every mission zone.</summary>
        [MenuItem("ZombieGame/Scenes/Build Mission Zones")]
        public static void Build()
        {
            BuildTown();
            BuildDepot();
        }

        /// <summary>(Re)builds the town, which is the zone a new player starts in.</summary>
        [MenuItem("ZombieGame/Scenes/Build Town Mission")]
        public static void BuildTown() =>
            BuildZone(
                ProjectScenes.MissionTown,
                ZoneMaps.Town,
                TownMissionPath,
                "Town Run",
                "Find ten supplies and leave through the park",
                suppliesWanted: 10,
                zombies: 26);

        /// <summary>(Re)builds the depot.</summary>
        [MenuItem("ZombieGame/Scenes/Build Depot Mission")]
        public static void BuildDepot() =>
            BuildZone(
                ProjectScenes.MissionDepot,
                ZoneMaps.Depot,
                DepotMissionPath,
                "Depot Run",
                "Strip the depot and get out through the yard",
                suppliesWanted: 6,
                zombies: 18);

        /// <summary>
        /// Assembles one mission zone: a drawn map, the shared systems, and what makes it a run.
        /// </summary>
        private static void BuildZone(
            string scenePath,
            string[] drawing,
            string missionPath,
            string missionName,
            string objective,
            int suppliesWanted,
            int zombies)
        {
            PlaceholderSpriteGenerator.GenerateAll();
            ZombieSkinLibrary.Rebuild();

            SceneBuilder.Build(scenePath, scene =>
            {
                // Loaded inside the callback on purpose. Opening a new scene unloads assets that
                // nothing in the project is holding on to, and an asset fetched before that call
                // comes back as a dead reference — which then saves into the scene as "none"
                // and leaves a mission with no objective in it.
                MissionDefinition mission =
                    RequireMission(missionPath, missionName, objective, suppliesWanted, zombies);

                ZoneSceneParts.CreateStandaloneBootstrap(scene);
                ZoneSceneParts.CreateGlobalLight(scene);

                ZoneMap map = ZoneSceneParts.PaintTiles(scene, drawing);

                Transform? player = ZoneSceneParts.CreateActors(scene, map.PlayerStart);
                Camera camera = CameraRigBuilder.Create(scene, player);
                ZoneSceneParts.WireAim(player, camera);

                // Scattered around the rooms the map marked, not evenly over the whole zone:
                // zombies spread by the square would end up standing inside the walls.
                ZoneSceneParts.CreateHorde(
                    scene,
                    player,
                    Vector3.zero,
                    new Vector2(10f, 8f),
                    mission.ZombiesOnStart,
                    map.ZombieNests);

                ZoneSceneParts.CreateZoneHeat(scene);
                ZoneSceneParts.CreateProjectileRunner(scene);
                ZoneSceneParts.CreateSwingFlash(scene);
                ZoneSceneParts.CreateHud(scene, camera);

                CreateLoot(scene, map);
                ExtractionPoint extraction = CreateExtraction(scene, map.Extraction, player);
                CreateMissionRunner(scene, mission, extraction);
                SceneBuilder.CreateObject(scene, "Mission End").AddComponent<MissionEndFlow>();
            });

            Debug.Log($"[Mission] built {scenePath}");
        }

        /// <summary>Puts a container on every spot the map marked.</summary>
        /// <remarks>
        /// Each one carries a trigger collider, which is how the character finds what is within
        /// reach: a physics overlap around whoever is holding the button, rather than a list that
        /// something has to keep up to date.
        /// </remarks>
        private static void CreateLoot(Scene scene, ZoneMap map)
        {
            var crate = AssetDatabase.LoadAssetAtPath<Sprite>(CrateSprite);
            if (crate is null)
            {
                Debug.LogWarning($"[Mission] no crate sprite at '{CrateSprite}'.");
            }

            for (int i = 0; i < map.Loot.Count; i++)
            {
                GameObject container = SceneBuilder.CreateObject(scene, $"Crate {i + 1}");
                container.transform.position = new Vector3(map.Loot[i].x, map.Loot[i].y, 0f);

                var renderer = container.AddComponent<SpriteRenderer>();
                renderer.sprite = crate;
                renderer.sortingOrder = 5;

                var reach = container.AddComponent<CircleCollider2D>();
                reach.isTrigger = true;
                reach.radius = 0.5f;

                container.AddComponent<LootContainer>();
            }
        }

        /// <summary>Marks the way out and tells it who it is waiting for.</summary>
        private static ExtractionPoint CreateExtraction(
            Scene scene, Vector2 at, Transform? squad)
        {
            GameObject point = SceneBuilder.CreateObject(scene, "Extraction");
            point.transform.position = new Vector3(at.x, at.y, 0f);

            // Painted on the ground, because a way out the player cannot find is a mission with
            // its last decision missing.
            var pad = new GameObject("Pad", typeof(SpriteRenderer));
            pad.transform.SetParent(point.transform, worldPositionStays: false);
            SpriteRenderer padRenderer = pad.GetComponent<SpriteRenderer>();
            padRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PadSprite);

            // Under the bodies but over the floor: it is floor paint, not a thing to walk round.
            padRenderer.sortingOrder = 1;

            ExtractionPoint extraction = point.AddComponent<ExtractionPoint>();
            var serialized = new SerializedObject(extraction);
            serialized.FindProperty("_squad").objectReferenceValue = squad;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return extraction;
        }

        /// <summary>Puts the mission's bookkeeping in the scene.</summary>
        private static void CreateMissionRunner(
            Scene scene, MissionDefinition mission, ExtractionPoint extraction)
        {
            GameObject runnerObject = SceneBuilder.CreateObject(scene, "Mission");
            MissionRunner runner = runnerObject.AddComponent<MissionRunner>();

            var serialized = new SerializedObject(runner);
            serialized.FindProperty("_mission").objectReferenceValue = mission;
            serialized.FindProperty("_extraction").objectReferenceValue = extraction;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Loads a mission, creating one if it is missing.
        /// </summary>
        /// <remarks>
        /// What this writes into a new asset is what the zone <i>is</i> — its name, its objective
        /// and how much it asks for — rather than how it feels to play. An asset that already
        /// exists is left alone entirely, so tuning a mission in the inspector survives the next
        /// time somebody rebuilds the scene.
        /// </remarks>
        private static MissionDefinition RequireMission(
            string assetPath, string displayName, string objective, int suppliesWanted, int zombies)
        {
            var existing = AssetDatabase.LoadAssetAtPath<MissionDefinition>(assetPath);
            if (existing is not null)
            {
                return existing;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(assetPath)!);
            MissionDefinition mission = ScriptableObject.CreateInstance<MissionDefinition>();

            var serialized = new SerializedObject(mission);
            serialized.FindProperty("_displayName").stringValue = displayName;
            serialized.FindProperty("_objectiveText").stringValue = objective;
            serialized.FindProperty("_suppliesWanted").intValue = suppliesWanted;
            serialized.FindProperty("_zombiesOnStart").intValue = zombies;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(mission, assetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Mission] created {assetPath}.");
            return mission;
        }
    }
}
