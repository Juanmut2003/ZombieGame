#nullable enable
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using ZombieGame.Editor.Art;
using ZombieGame.Editor.Importing;

namespace ZombieGame.Editor.Scenes
{
    /// <summary>
    /// Builds the sandbox scene: a lit floor of placeholder tiles with a wall border, and a camera
    /// looking at it.
    /// </summary>
    /// <remarks>
    /// This is the first thing in the project that can actually be looked at, and it exercises the
    /// whole tooling chain end to end: placeholders are generated, the importer forces the pixel
    /// settings onto them, and the scene is assembled from code.
    /// Run from the menu, or with
    /// <c>-executeMethod ZombieGame.Editor.Scenes.SandboxSceneBuilder.Build</c>.
    /// </remarks>
    public static class SandboxSceneBuilder
    {
        private const int FloorWidth = 20;
        private const int FloorHeight = 12;

        /// <summary>
        /// The vertical half-size a camera needs to show exactly the reference resolution at the
        /// project's pixels-per-unit: 270 px tall at 32 px per unit.
        /// </summary>
        /// <remarks>
        /// A plain orthographic camera for now. The Pixel Perfect Camera component replaces this
        /// once the camera rig exists; until then this at least frames the scene correctly.
        /// </remarks>
        private const float CameraHalfHeight = 270f / 2f / PixelArtRules.PixelsPerUnit;

        private const string FloorSprite = "Assets/_Project/Art/Sprites/Tiles/placeholder_floor.png";
        private const string WallSprite = "Assets/_Project/Art/Sprites/Tiles/placeholder_wall.png";
        private const string PlayerSprite = "Assets/_Project/Art/Sprites/Characters/placeholder_mc.png";
        private const string ZombieSprite = "Assets/_Project/Art/Sprites/Zombies/placeholder_walker.png";

        /// <summary>Generates any missing placeholders and (re)builds the sandbox scene.</summary>
        [MenuItem("ZombieGame/Scenes/Build Sandbox Scene")]
        public static void Build()
        {
            PlaceholderSpriteGenerator.GenerateAll();

            SceneBuilder.Build(ProjectScenes.Sandbox, scene =>
            {
                CreateCamera(scene);
                CreateGlobalLight(scene);
                CreateFloor(scene);
                CreateActors(scene);
            });

            Debug.Log($"[Sandbox] built {ProjectScenes.Sandbox}");
        }

        private static void CreateCamera(Scene scene)
        {
            GameObject cameraObject = SceneBuilder.CreateObject(scene, "Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = CameraHalfHeight;
            camera.backgroundColor = new Color(0.06f, 0.07f, 0.08f, 1f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
        }

        private static void CreateGlobalLight(Scene scene)
        {
            // Without a global 2D light a URP 2D scene renders black, which looks exactly like a
            // broken build. It is the single easiest thing to forget when assembling a scene.
            GameObject lightObject = SceneBuilder.CreateObject(scene, "Global Light 2D");
            Light2D light = lightObject.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;
            light.color = Color.white;
        }

        private static void CreateFloor(Scene scene)
        {
            Tile? floor = TileAssetFactory.GetOrCreate(FloorSprite, Tile.ColliderType.None);
            Tile? wall = TileAssetFactory.GetOrCreate(WallSprite, Tile.ColliderType.Sprite);
            if (floor is null || wall is null)
            {
                Debug.LogWarning("[Sandbox] tile placeholders are missing; the floor stays empty.");
                return;
            }

            // One grid, two tilemaps. A tilemap holds hundreds of cells in a few kilobytes and
            // renders them in one batch, where one GameObject per cell would bloat the scene file
            // and cost a draw call each. Splitting floor and walls keeps sorting and collision
            // separable later.
            GameObject gridObject = SceneBuilder.CreateObject(scene, "Grid");
            Grid grid = gridObject.AddComponent<Grid>();
            grid.cellSize = Vector3.one;

            Tilemap floorMap = CreateTilemapLayer(gridObject.transform, "Tilemap_Floor", 0);
            Tilemap wallMap = CreateTilemapLayer(gridObject.transform, "Tilemap_Walls", 1);

            int halfWidth = FloorWidth / 2;
            int halfHeight = FloorHeight / 2;

            for (int y = -halfHeight; y <= halfHeight; y++)
            {
                for (int x = -halfWidth; x <= halfWidth; x++)
                {
                    bool isBorder = x == -halfWidth || x == halfWidth
                                                    || y == -halfHeight || y == halfHeight;
                    var cell = new Vector3Int(x, y, 0);
                    floorMap.SetTile(cell, floor);

                    if (isBorder)
                    {
                        wallMap.SetTile(cell, wall);
                    }
                }
            }
        }

        private static Tilemap CreateTilemapLayer(Transform parent, string name, int sortingOrder)
        {
            var layer = new GameObject(name);
            layer.transform.SetParent(parent, worldPositionStays: false);

            Tilemap tilemap = layer.AddComponent<Tilemap>();
            TilemapRenderer renderer = layer.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = sortingOrder;
            return tilemap;
        }

        private static void CreateActors(Scene scene)
        {
            AddActor(scene, "MC (placeholder)", PlayerSprite, new Vector3(-1.5f, 0f, 0f));
            AddActor(scene, "Walker (placeholder)", ZombieSprite, new Vector3(2.5f, 1f, 0f));
        }

        private static void AddActor(Scene scene, string name, string spritePath, Vector3 position)
        {
            Sprite? sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite is null)
            {
                Debug.LogWarning($"[Sandbox] '{spritePath}' is missing; skipping {name}.");
                return;
            }

            GameObject actor = SceneBuilder.CreateObject(scene, name);
            actor.transform.position = position;

            SpriteRenderer renderer = actor.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;

            // Everything that stands on the floor sorts by how far down the screen it is, so a
            // figure lower in the scene draws in front of one further back.
            renderer.sortingOrder = 10;
        }
    }
}
