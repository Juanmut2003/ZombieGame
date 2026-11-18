#nullable enable
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using ZombieGame.AI;
using ZombieGame.Characters;
using ZombieGame.Combat;
using ZombieGame.Core;
using ZombieGame.Core.Config;
using ZombieGame.Editor.Art;
using ZombieGame.Editor.Content;
using ZombieGame.Editor.Input;
using ZombieGame.UI;
using ZombieGame.Waves;

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
        // Sized so the whole room, walls included, sits inside the camera's view: at 32 pixels per
        // unit the reference resolution shows about 15 by 8.4 units. A sandbox whose edges you
        // cannot see is a sandbox that hides half of what it is supposed to demonstrate.
        private const int FloorWidth = 12;
        private const int FloorHeight = 6;

        private const string FloorSprite = "Assets/_Project/Art/Sprites/Tiles/placeholder_floor.png";
        private const string WallSprite = "Assets/_Project/Art/Sprites/Tiles/placeholder_wall.png";
        private const string PlayerSprite =
            "Assets/_Project/Art/Sprites/Characters/archer_front_walk.png";
        private const string SwingArcSprite = "Assets/_Project/Art/Sprites/FX/swing_arc.png";

        private const string BalanceConfigPath = "Assets/_Project/Data/BalanceConfig.asset";
        private const string BatWeaponPath = "Assets/_Project/Data/Weapons/Bat.asset";
        private const string PistolWeaponPath = "Assets/_Project/Data/Weapons/Pistol.asset";
        private const string BulletSprite = "Assets/_Project/Art/Sprites/FX/bullet.png";

        private const string PlayerLeftSprite =
            "Assets/_Project/Art/Sprites/Characters/archer_side_left_walk.png";
        private const string PlayerRightSprite =
            "Assets/_Project/Art/Sprites/Characters/archer_side_right_walk.png";

        // Room for a real horde, but only a handful put down: enough to watch them come at you,
        // few enough that the sandbox stays readable.
        private const int HordeCapacity = 300;
        private const int HordeOnStart = 26;

        /// <summary>
        /// The kinds of zombie the sandbox puts out, by asset name.
        /// </summary>
        /// <remarks>
        /// Four silhouettes rather than one. A crowd of the same figure repeated reads as a
        /// rendering fault even when every one of them walks its own way, and telling two kinds
        /// apart at a glance is what lets a player decide whether to fight or leave.
        /// Neither numbers nor art here on purpose: the numbers live in the assets, and the looks
        /// are found by name — see <see cref="RequireDefinition"/> and
        /// <see cref="ZombieSkinLibrary"/>.
        /// </remarks>
        private static readonly string[] ZombieKinds =
        {
            "Walker",
            "Stumbler",
            "Bloater",
            "Nurse",
        };

        // Dim enough to read as part of the world rather than a web page laid over it.
        private static readonly Color HudTrack = new(0.09f, 0.1f, 0.1f, 0.78f);
        private static readonly Color HudStamina = new(0.55f, 0.72f, 0.44f, 1f);
        private static readonly Color HudMuted = new(0.6f, 0.62f, 0.59f, 1f);
        private static readonly Color HudCalm = new(0.45f, 0.55f, 0.62f, 1f);
        private static readonly Color HudAmmo = new(0.82f, 0.81f, 0.76f, 1f);

        /// <summary>Generates any missing placeholders and (re)builds the sandbox scene.</summary>
        [MenuItem("ZombieGame/Scenes/Build Sandbox Scene")]
        public static void Build()
        {
            PlaceholderSpriteGenerator.GenerateAll();

            // Before anything is wired: a pack dropped into the sprite folder becomes a skin asset
            // here, and the kinds below pick theirs up by name. Building the scene is the moment
            // somebody would notice a missing look, so it is the moment to go and find them.
            ZombieSkinLibrary.Rebuild();

            SceneBuilder.Build(ProjectScenes.Sandbox, scene =>
            {
                CreateStandaloneBootstrap(scene);
                CreateGlobalLight(scene);
                CreateFloor(scene);

                // Actors first: the camera is built around what it follows, and snapping it onto
                // a target that does not exist yet would just park it at the origin.
                Transform? player = CreateActors(scene);
                Camera camera = CameraRigBuilder.Create(scene, player);
                CreateHorde(scene, player);
                CreateZoneHeat(scene);
                CreateProjectileRunner(scene);
                CreateSwingFlash(scene);
                CreateHud(scene, camera);
            });

            Debug.Log($"[Sandbox] built {ProjectScenes.Sandbox}");
        }

        /// <summary>
        /// Gives the zone its own bootstrap, so it can be opened and played on its own.
        /// </summary>
        /// <remarks>
        /// Without this, opening a zone scene directly leaves the services unregistered: there is
        /// no event bus, so a swing is published to nobody and melee silently does nothing. That
        /// is a miserable thing to debug, and it is exactly what a developer does all day —
        /// open the scene they are working on and press play.
        /// Arriving from Boot is still the normal path; the bootstrap destroys itself when the
        /// services are already up, so the two never fight.
        /// </remarks>
        private static void CreateStandaloneBootstrap(Scene scene)
        {
            GameObject bootstrapObject = SceneBuilder.CreateObject(scene, "Zone Bootstrap");
            GameBootstrap bootstrap = bootstrapObject.AddComponent<GameBootstrap>();

            var serialized = new SerializedObject(bootstrap);
            serialized.FindProperty("_balance").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<BalanceConfig>(BalanceConfigPath);

            // No scene to hand over to: this one is already the zone.
            serialized.FindProperty("_firstScene").stringValue = string.Empty;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Puts the swing arc in the scene, listening for anyone swinging anything.
        /// </summary>
        private static void CreateSwingFlash(Scene scene)
        {
            GameObject flashObject = SceneBuilder.CreateObject(scene, "Swing Flash");
            SwingFlashView view = flashObject.AddComponent<SwingFlashView>();

            var spriteObject = new GameObject("Arc", typeof(SpriteRenderer));
            spriteObject.transform.SetParent(flashObject.transform, worldPositionStays: false);

            SpriteRenderer renderer = spriteObject.GetComponent<SpriteRenderer>();
            renderer.sprite = LoadSprite(SwingArcSprite);
            renderer.enabled = false;

            // Above every body on the floor: a swing that disappears behind the zombie it just
            // hit is worse than no swing at all.
            renderer.sortingOrder = 500;

            if (renderer.sprite is null)
            {
                Debug.LogWarning($"[Sandbox] no sprite at '{SwingArcSprite}'; swings stay invisible.");
            }

            var serialized = new SerializedObject(view);
            serialized.FindProperty("_renderer").objectReferenceValue = renderer;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Builds the in-game HUD: for now the stamina bar and nothing else.
        /// </summary>
        /// <remarks>
        /// Stamina is the one number the player has to watch while playing — sprinting away and
        /// swinging come out of the same pool, so an invisible pool means being caught mid-stride
        /// with no warning. Health gets its bar when the MC can be hurt.
        /// TODO: placeholder framing. The diegetic HUD from PROJECT.md 13 comes with the UX pass.
        /// </remarks>
        private static void CreateHud(Scene scene, Camera camera)
        {
            Canvas canvas = UiSceneParts.CreateCanvas(scene, camera, "HUD Canvas");

            UiSceneParts.CreateLabel(
                canvas.transform,
                "Stamina Label",
                "STAMINA",
                anchoredPosition: new Vector2(-404f, -222f),
                size: new Vector2(120f, 18f),
                fontSize: 12,
                colour: HudMuted,
                alignment: TextAnchor.MiddleLeft);

            Image fill = UiSceneParts.CreateCornerBar(
                canvas.transform,
                "Stamina Bar",
                size: new Vector2(180f, 10f),
                margin: new Vector2(24f, 24f),
                trackColour: HudTrack,
                fillColour: HudStamina);

            var presenterObject = new GameObject("Stamina Bar Presenter", typeof(RectTransform));
            presenterObject.transform.SetParent(canvas.transform, worldPositionStays: false);
            StaminaBarPresenter presenter =
                presenterObject.AddComponent<StaminaBarPresenter>();

            var serialized = new SerializedObject(presenter);
            serialized.FindProperty("_fill").objectReferenceValue = fill;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            CreateHeatBar(canvas);
            CreateAmmoLabel(canvas);
        }

        /// <summary>
        /// The ammunition count, in the opposite corner from the bars.
        /// </summary>
        /// <remarks>
        /// Bottom right rather than next to stamina: it is read at a different moment. Stamina and
        /// heat are watched while moving; the count is looked at between fights.
        /// </remarks>
        private static void CreateAmmoLabel(Canvas canvas)
        {
            Text label = UiSceneParts.CreateLabel(
                canvas.transform,
                "Ammo Label",
                "-- / --",
                anchoredPosition: new Vector2(330f, -222f),
                size: new Vector2(140f, 22f),
                fontSize: 18,
                colour: HudAmmo,
                alignment: TextAnchor.MiddleRight);

            var presenterObject = new GameObject("Ammo Presenter", typeof(RectTransform));
            presenterObject.transform.SetParent(canvas.transform, worldPositionStays: false);
            AmmoPresenter presenter = presenterObject.AddComponent<AmmoPresenter>();

            var serialized = new SerializedObject(presenter);
            serialized.FindProperty("_label").objectReferenceValue = label;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// The zone's heat meter, above the stamina bar.
        /// </summary>
        /// <remarks>
        /// Deliberately next to stamina rather than off in a corner: the two are the same
        /// decision seen from both ends. Sprinting spends one and fills the other.
        /// </remarks>
        private static void CreateHeatBar(Canvas canvas)
        {
            Text label = UiSceneParts.CreateLabel(
                canvas.transform,
                "Heat Label",
                "CALM",
                anchoredPosition: new Vector2(-404f, -192f),
                size: new Vector2(120f, 18f),
                fontSize: 12,
                colour: HudMuted,
                alignment: TextAnchor.MiddleLeft);

            Image fill = UiSceneParts.CreateCornerBar(
                canvas.transform,
                "Heat Bar",
                size: new Vector2(180f, 10f),
                margin: new Vector2(24f, 54f),
                trackColour: HudTrack,
                fillColour: HudCalm);

            var presenterObject = new GameObject("Heat Bar Presenter", typeof(RectTransform));
            presenterObject.transform.SetParent(canvas.transform, worldPositionStays: false);
            HeatBarPresenter presenter = presenterObject.AddComponent<HeatBarPresenter>();

            var serialized = new SerializedObject(presenter);
            serialized.FindProperty("_fill").objectReferenceValue = fill;
            serialized.FindProperty("_label").objectReferenceValue = label;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Puts the thing that flies the rounds in the scene.</summary>
        /// <remarks>
        /// One per zone, not one per gun: survivors, raiders and turrets all fire into the same
        /// pool, and a pool per shooter would mean a hundred half-empty pools in a firefight.
        /// </remarks>
        private static void CreateProjectileRunner(Scene scene)
        {
            GameObject runnerObject = SceneBuilder.CreateObject(scene, "Projectiles");
            ProjectileRunner runner = runnerObject.AddComponent<ProjectileRunner>();

            var serialized = new SerializedObject(runner);
            serialized.FindProperty("_bulletSprite").objectReferenceValue = LoadSprite(BulletSprite);

            // Rounds stop at the same walls that stop sight, for the same reason: it is the one
            // thing in the scene that is actually solid.
            serialized.FindProperty("_barriers").intValue = GameLayers.WallsMask;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Loads the pistol, creating an untuned one if it is missing.
        /// </summary>
        /// <remarks>
        /// Same rule as everywhere else: the asset owns the numbers. The one thing set here is
        /// that it is a firearm, which is not a balance value but what the thing <i>is</i> —
        /// a pistol asset left on Melee would silently never fire.
        /// </remarks>
        private static WeaponDefinition RequirePistol()
        {
            var existing = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(PistolWeaponPath);
            if (existing is not null)
            {
                return existing;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(PistolWeaponPath)!);
            WeaponDefinition pistol = ScriptableObject.CreateInstance<WeaponDefinition>();

            var serialized = new SerializedObject(pistol);
            serialized.FindProperty("_displayName").stringValue = "Pistol";
            serialized.FindProperty("_kind").enumValueIndex = (int)WeaponKind.Firearm;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(pistol, PistolWeaponPath);
            AssetDatabase.SaveAssets();
            Debug.LogWarning(
                $"[Sandbox] {PistolWeaponPath} was missing and has been created untuned. "
                + "Its numbers are the definition's defaults until someone sets them.");
            return pistol;
        }

        /// <summary>Puts the zone's heat meter in the scene, listening to the bus.</summary>
        private static void CreateZoneHeat(Scene scene)
        {
            GameObject heatObject = SceneBuilder.CreateObject(scene, "Zone Heat");
            ZoneHeat heat = heatObject.AddComponent<ZoneHeat>();

            var serialized = new SerializedObject(heat);
            serialized.FindProperty("_balance").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<BalanceConfig>(BalanceConfigPath);
            serialized.ApplyModifiedPropertiesWithoutUndo();
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
            Tile? wall = TileAssetFactory.GetOrCreate(WallSprite, Tile.ColliderType.Grid);
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

            // Unity's cell (0,0) spans 0..1, so its centre sits at (0.5, 0.5) and no tile centre
            // lands on a whole world coordinate. Shifting the grid by half a cell makes
            // "integer world position == centre of a tile", which is the convention the base
            // building grid, placement checks and pathfinding will all want.
            gridObject.transform.position = new Vector3(-0.5f, -0.5f, 0f);

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

            AddWallCollision(wallMap.gameObject);
        }

        /// <summary>
        /// Gives the wall layer its physics shape. Must run <b>after</b> the tiles are painted.
        /// </summary>
        /// <remarks>
        /// A TilemapCollider2D builds its shapes from the tiles present at the time, and outside a
        /// running editor loop it never recomputes. Added to an empty tilemap it reports zero
        /// shapes for good, the walls stop nothing, and nothing in the scene looks wrong — the
        /// tiles still render. This cost an afternoon; the order is the whole trick.
        /// </remarks>
        private static void AddWallCollision(GameObject wallObject)
        {
            // What makes this tilemap "the walls" to the rest of the game: a sight query asks for
            // the Walls layer and gets the walls, and nothing else in the scene has to be excluded
            // one object at a time.
            int walls = GameLayers.WallsIndex;
            if (walls < 0)
            {
                Debug.LogWarning(
                    $"[Sandbox] the project has no '{GameLayers.Walls}' layer; "
                    + "sight will not be blocked by anything.");
            }
            else
            {
                wallObject.layer = walls;
            }

            var body = wallObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;

            // The composite has to exist before the tilemap collider is told to merge into it,
            // for the same reason: a collider handing its shapes to a composite that is not there
            // hands them nowhere.
            CompositeCollider2D composite = wallObject.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;

            TilemapCollider2D tilemapCollider = wallObject.AddComponent<TilemapCollider2D>();
            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;

            // Merging the per-tile boxes into a few outlines is faster, and removes the hairline
            // seams between neighbouring boxes that characters otherwise snag on.
            composite.GenerateGeometry();
        }

        /// <summary>
        /// Puts a small horde in the room, driven by one director rather than by itself.
        /// </summary>
        private static void CreateHorde(Scene scene, Transform? target)
        {
            // Two kinds, so a crowd reads as a crowd. One silhouette repeated eighteen times looks
            // like a rendering bug even when every one of them is walking its own way.
            var kinds = new ZombieDefinition[ZombieKinds.Length];
            for (int i = 0; i < ZombieKinds.Length; i++)
            {
                kinds[i] = RequireDefinition(ZombieKinds[i]);
            }

            GameObject directorObject = SceneBuilder.CreateObject(scene, "Zombie Director");
            directorObject.transform.position = new Vector3(3f, 1f, 0f);
            ZombieDirector director = directorObject.AddComponent<ZombieDirector>();

            var serialized = new SerializedObject(director);
            SerializedProperty definitions = serialized.FindProperty("_definitions");
            definitions.arraySize = kinds.Length;
            for (int i = 0; i < kinds.Length; i++)
            {
                definitions.GetArrayElementAtIndex(i).objectReferenceValue = kinds[i];
            }

            serialized.FindProperty("_balance").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<BalanceConfig>(BalanceConfigPath);
            serialized.FindProperty("_target").objectReferenceValue = target;
            serialized.FindProperty("_capacity").intValue = HordeCapacity;
            serialized.FindProperty("_spawnOnStart").intValue = HordeOnStart;
            serialized.FindProperty("_spawnArea").vector2Value = new Vector2(3.5f, 2f);

            // Only the walls stop sight. Naming the layer is what makes that true: on the default
            // layer the query would also return the floor, the props and the character being
            // looked at, and every zombie would decide it was standing behind a wall.
            serialized.FindProperty("_sightBlockers").intValue = GameLayers.WallsMask;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Loads a zombie asset, creating an untuned one with its art wired up if it is missing.
        /// </summary>
        /// <remarks>
        /// The builder deliberately sets no numbers. Health, speed and body radius belong to the
        /// asset and to whoever is tuning it; a scene builder that wrote them would be a second,
        /// invisible source of truth that quietly disagrees with the asset the moment either one
        /// is edited. What it does wire is the art, which is not a balance value and is tedious
        /// to click together by hand.
        /// A fresh asset therefore comes out at the definition's own declared defaults, with a
        /// warning saying so.
        /// </remarks>
        private static ZombieDefinition RequireDefinition(string displayName)
        {
            string assetPath = $"Assets/_Project/Data/Zombies/{displayName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<ZombieDefinition>(assetPath);

            // The art is rewired even on an existing asset: new poses arrive with new packs, and
            // an asset that kept pointing at only the sprites it was born with would quietly stay
            // half-dressed. Numbers are never touched.
            ZombieDefinition definition = existing is not null
                ? existing
                : ScriptableObject.CreateInstance<ZombieDefinition>();

            var serialized = new SerializedObject(definition);
            serialized.FindProperty("_displayName").stringValue = displayName;

            ZombieSkin[] skins = ZombieSkinLibrary.SkinsFor(displayName);
            if (skins.Length == 0)
            {
                Debug.LogWarning(
                    $"[Sandbox] no skins named '{displayName.ToLowerInvariant()}' in "
                    + $"{ZombieSkinLibrary.SkinFolder}; the {displayName} stays invisible.");
            }

            SerializedProperty list = serialized.FindProperty("_skins");
            list.arraySize = skins.Length;
            for (int i = 0; i < skins.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = skins[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();

            if (existing is not null)
            {
                EditorUtility.SetDirty(definition);
                AssetDatabase.SaveAssets();
                return definition;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(assetPath)!);
            AssetDatabase.CreateAsset(definition, assetPath);
            AssetDatabase.SaveAssets();
            Debug.LogWarning(
                $"[Sandbox] {assetPath} was missing and has been created untuned. "
                + "Its numbers are the definition's defaults until someone sets them.");
            return definition;
        }

        /// <summary>
        /// Loads the weapon the MC starts with, creating an untuned one if it is missing.
        /// </summary>
        /// <remarks>Same rule as the zombies: the asset owns the numbers, not this file.</remarks>
        private static WeaponDefinition RequireBat()
        {
            var existing = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(BatWeaponPath);
            if (existing is not null)
            {
                return existing;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(BatWeaponPath)!);
            WeaponDefinition bat = ScriptableObject.CreateInstance<WeaponDefinition>();

            var serialized = new SerializedObject(bat);
            serialized.FindProperty("_displayName").stringValue = "Bat";
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(bat, BatWeaponPath);
            AssetDatabase.SaveAssets();
            Debug.LogWarning(
                $"[Sandbox] {BatWeaponPath} was missing and has been created untuned. "
                + "Its numbers are the definition's defaults until someone sets them.");
            return bat;
        }

        private static Sprite? LoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        private static Tilemap CreateTilemapLayer(Transform parent, string name, int sortingOrder)
        {
            var layer = new GameObject(name);
            layer.transform.SetParent(parent, worldPositionStays: false);

            Tilemap tilemap = layer.AddComponent<Tilemap>();
            TilemapRenderer renderer = layer.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = sortingOrder;
            return tilemap;
        }

        /// <summary>Places the stand-in actors and returns the player, for the camera to follow.</summary>
        private static Transform? CreateActors(Scene scene)
        {
            Transform? player = AddActor(
                scene, "MC", PlayerSprite, new Vector3(-1.5f, 0f, 0f));
            if (player is not null)
            {
                MakePlayable(player.gameObject);
            }

            return player;
        }

        /// <summary>
        /// Turns the MC placeholder into something that can be steered and that walls can stop.
        /// </summary>
        private static void MakePlayable(GameObject player)
        {
            var body = player.AddComponent<Rigidbody2D>();

            // Dynamic, because only a dynamic body is pushed out of walls by the solver. There is
            // no gravity in a top-down world, and rotation is frozen so a shove against a corner
            // cannot spin the character.
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            // Only the feet collide, not the whole figure. In a top-down view the sprite is drawn
            // standing up but occupies a small patch of floor, so the body is free to overlap a
            // wall it is standing in front of. The art is 58 px tall at 64 pixels per unit, so the
            // figure is about 0.9 units — roughly one tile — and the collider is a fraction of that.
            var collider = player.AddComponent<CapsuleCollider2D>();
            collider.direction = CapsuleDirection2D.Horizontal;
            collider.size = new Vector2(0.35f, 0.2f);
            collider.offset = new Vector2(0f, 0.1f);

            PlayerInputReader input = player.AddComponent<PlayerInputReader>();
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                InputActionsGenerator.AssetPath);
            if (actions is null)
            {
                Debug.LogWarning("[Sandbox] no input actions asset; the MC will not move.");
            }

            var serializedInput = new SerializedObject(input);
            serializedInput.FindProperty("_actions").objectReferenceValue = actions;
            serializedInput.FindProperty("_mapName").stringValue = InputActionsGenerator.GameplayMap;
            serializedInput.ApplyModifiedPropertiesWithoutUndo();

            var balance = AssetDatabase.LoadAssetAtPath<BalanceConfig>(BalanceConfigPath);

            CharacterStamina stamina = player.AddComponent<CharacterStamina>();
            var serializedStamina = new SerializedObject(stamina);
            serializedStamina.FindProperty("_balance").objectReferenceValue = balance;
            serializedStamina.ApplyModifiedPropertiesWithoutUndo();

            CharacterMovement movement = player.AddComponent<CharacterMovement>();
            var serializedMovement = new SerializedObject(movement);
            serializedMovement.FindProperty("_balance").objectReferenceValue = balance;
            serializedMovement.FindProperty("_input").objectReferenceValue = input;
            serializedMovement.FindProperty("_stamina").objectReferenceValue = stamina;
            serializedMovement.ApplyModifiedPropertiesWithoutUndo();

            // Three poses, picked from the direction of travel. Not a walk cycle — the packs ship
            // one frame per direction, so there is nothing to animate yet.
            CharacterFacing facing = player.AddComponent<CharacterFacing>();
            var serializedFacing = new SerializedObject(facing);
            serializedFacing.FindProperty("_front").objectReferenceValue = LoadSprite(PlayerSprite);
            serializedFacing.FindProperty("_left").objectReferenceValue = LoadSprite(PlayerLeftSprite);
            serializedFacing.FindProperty("_right").objectReferenceValue =
                LoadSprite(PlayerRightSprite);
            serializedFacing.ApplyModifiedPropertiesWithoutUndo();

            // Walking is quiet, sprinting is not. The whole stealth half of the game hangs off
            // this component being on the thing the player steers.
            FootstepNoise steps = player.AddComponent<FootstepNoise>();
            var serializedSteps = new SerializedObject(steps);
            serializedSteps.FindProperty("_balance").objectReferenceValue = balance;
            serializedSteps.FindProperty("_input").objectReferenceValue = input;
            serializedSteps.ApplyModifiedPropertiesWithoutUndo();

            // The gun goes on the left mouse button, the bat stays on the right. Loud and finite
            // against quiet and free, one button each.
            PlayerGun gun = player.AddComponent<PlayerGun>();
            var serializedGun = new SerializedObject(gun);
            serializedGun.FindProperty("_weapon").objectReferenceValue = RequirePistol();
            serializedGun.FindProperty("_input").objectReferenceValue = input;
            serializedGun.ApplyModifiedPropertiesWithoutUndo();

            PlayerMelee melee = player.AddComponent<PlayerMelee>();
            var serializedMelee = new SerializedObject(melee);
            serializedMelee.FindProperty("_weapon").objectReferenceValue = RequireBat();
            serializedMelee.FindProperty("_input").objectReferenceValue = input;
            serializedMelee.FindProperty("_stamina").objectReferenceValue = stamina;
            serializedMelee.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Transform? AddActor(
            Scene scene, string name, string spritePath, Vector3 position)
        {
            Sprite? sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite is null)
            {
                Debug.LogWarning($"[Sandbox] no sprite at '{spritePath}'; skipping {name}.");
                return null;
            }

            GameObject actor = SceneBuilder.CreateObject(scene, name);
            actor.transform.position = position;

            SpriteRenderer renderer = actor.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;

            // Everything that stands on the floor sorts by how far down the screen it is, so a
            // figure lower in the scene draws in front of one further back.
            renderer.sortingOrder = 10;

            return actor.transform;
        }
    }
}
