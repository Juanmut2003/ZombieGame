#nullable enable
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using ZombieGame.Base;
using ZombieGame.Core.Config;
using ZombieGame.Core.Rendering;
using ZombieGame.Economy;
using ZombieGame.Editor.Art;
using ZombieGame.Editor.Content;
using ZombieGame.UI;

namespace ZombieGame.Editor.Scenes
{
    /// <summary>
    /// Builds the colony scene: a claimed plot and the means to build on it.
    /// </summary>
    /// <remarks>
    /// The first half of M2. What is here is the ground and free placement — pick a building,
    /// hold it over the plot, turn it, put it down. What is deliberately not here is everything
    /// that makes a building <i>do</i> something: no costs, no power, no jobs, nobody living in
    /// it. Those are the next steps, and a scene that pretended to have them would be harder to
    /// take apart than one that plainly does not.
    /// There is no MC walking about either. The base will want one once there is work to do in
    /// it; until then a character with nothing to do is a thing to maintain rather than a thing
    /// to use.
    /// The plot is sized to fit on one screen on purpose. PROJECT.md 4.1 calls a Tier-1 site a
    /// small plot, and a plot that fits is a plot that needs no camera panning — which is a
    /// whole feature this step does not have to grow.
    /// </remarks>
    public static class BaseSceneBuilder
    {
        /// <summary>How many cells across the Tier-1 plot is.</summary>
        private const int SiteWidth = 14;

        /// <summary>How many cells up it is.</summary>
        private const int SiteHeight = 8;

        /// <summary>
        /// The world position of the plot's bottom-left corner.
        /// </summary>
        /// <remarks>
        /// Half a cell out, which lines the plot's cells up with the tilemap's. The grid below
        /// is shifted the same way so that a whole world coordinate is the middle of a tile, the
        /// convention the zones already keep.
        /// </remarks>
        private static readonly Vector2 SiteOrigin = new(-0.5f, -0.5f);

        /// <summary>How far the grass runs past the plot, in cells.</summary>
        private const int Margin = 4;

        /// <summary>Where the wheel's pieces are kept.</summary>
        private const string UiSprites = "Assets/_Project/Art/Sprites/UI";

        /// <summary>Where the colony's starting amounts come from.</summary>
        private const string BalanceConfigPath = "Assets/_Project/Data/BalanceConfig.asset";

        /// <summary>The colour the readout and the hint are written in.</summary>
        private static readonly Color HudInk = new(0.86f, 0.86f, 0.82f, 1f);

        /// <summary>
        /// Ground nothing may be built on: the forecourt where the road comes in.
        /// </summary>
        private static readonly Vector2Int[] BlockedCells =
        {
            new(0, 0), new(1, 0), new(2, 0),
            new(0, 1), new(1, 1),
        };

        /// <summary>Builds the colony scene.</summary>
        [MenuItem("ZombieGame/Scenes/Build Base Scene")]
        public static void Build()
        {
            SceneBuilder.Build(ProjectScenes.Base, scene =>
            {
                PlaceholderSpriteGenerator.GenerateAll();
                BuildingCatalogueBuilder.EnsureAll();

                Camera camera = CameraRigBuilder.Create(scene);

                // Parked on the middle of the plot. Nothing follows anything here yet.
                camera.transform.position = new Vector3(
                    SiteOrigin.x + (SiteWidth * 0.5f),
                    SiteOrigin.y + (SiteHeight * 0.5f),
                    camera.transform.position.z);

                ZoneSceneParts.CreateStandaloneBootstrap(scene);

                PaintGround(scene);
                (BuildPlacer placer, BuildSiteView site, Transform built) =
                    CreatePlacement(scene, camera);
                Canvas canvas = CreateStoresHud(scene, camera, placer);
                CreateColony(scene, camera, canvas, placer, site, built);
            });

            Debug.Log($"[Scenes] built {ProjectScenes.Base}");
        }

        /// <summary>Lays the plot down as tiles, with grass running off round it.</summary>
        private static void PaintGround(Scene scene)
        {
            Tile[] grass = TownArt.Grass();
            Tile[] pavement = TownArt.Pavement();
            Tile[] road = TownArt.Road();

            if (grass.Length == 0 || pavement.Length == 0)
            {
                Debug.LogWarning("[Base] no town tiles; the plot stays bare.");
                return;
            }

            GameObject gridObject = SceneBuilder.CreateObject(scene, "Grid");
            Grid grid = gridObject.AddComponent<Grid>();
            grid.cellSize = Vector3.one;

            // The same half-cell shift the zones use, so a whole world coordinate is the middle
            // of a tile — and so the plot's cells and the tilemap's are the same cells.
            gridObject.transform.position = new Vector3(-0.5f, -0.5f, 0f);

            Tilemap floor = ZoneSceneParts.CreateTilemapLayer(
                gridObject.transform, "Tilemap_Floor", SortingBands.Floor);

            for (int row = -Margin; row < SiteHeight + Margin; row++)
            {
                for (int column = -Margin; column < SiteWidth + Margin; column++)
                {
                    bool onPlot = (uint)column < (uint)SiteWidth && (uint)row < (uint)SiteHeight;
                    Tile[] set = onPlot
                        ? IsBlocked(column, row) && road.Length > 0 ? road : pavement
                        : grass;

                    floor.SetTile(new Vector3Int(column, row, 0), Pick(set, column, row));
                }
            }
        }

        /// <summary>Puts the plot, the ghost and the placer into the scene and ties them up.</summary>
        private static (BuildPlacer Placer, BuildSiteView Site, Transform Built) CreatePlacement(
            Scene scene, Camera camera)
        {
            GameObject siteObject = SceneBuilder.CreateObject(scene, "Build Site");
            BuildSiteView site = siteObject.AddComponent<BuildSiteView>();

            var serializedSite = new SerializedObject(site);
            serializedSite.FindProperty("_origin").vector2Value = SiteOrigin;
            serializedSite.FindProperty("_width").intValue = SiteWidth;
            serializedSite.FindProperty("_height").intValue = SiteHeight;

            SerializedProperty blocked = serializedSite.FindProperty("_blocked");
            blocked.arraySize = BlockedCells.Length;
            for (int i = 0; i < BlockedCells.Length; i++)
            {
                blocked.GetArrayElementAtIndex(i).vector2IntValue = BlockedCells[i];
            }

            serializedSite.ApplyModifiedPropertiesWithoutUndo();

            GameObject ghostObject = SceneBuilder.CreateObject(scene, "Build Ghost");
            SpriteRenderer ghostRenderer = ghostObject.AddComponent<SpriteRenderer>();
            ghostRenderer.enabled = false;
            BuildGhost ghost = ghostObject.AddComponent<BuildGhost>();

            var serializedGhost = new SerializedObject(ghost);
            serializedGhost.FindProperty("_renderer").objectReferenceValue = ghostRenderer;
            serializedGhost.ApplyModifiedPropertiesWithoutUndo();

            // Everything put up goes under here, so the scene stays readable after an hour of
            // building rather than becoming a hundred loose objects at the root.
            GameObject built = SceneBuilder.CreateObject(scene, "Buildings");

            GameObject placerObject = SceneBuilder.CreateObject(scene, "Build Placer");
            BuildPlacer placer = placerObject.AddComponent<BuildPlacer>();

            var serializedPlacer = new SerializedObject(placer);
            serializedPlacer.FindProperty("_camera").objectReferenceValue = camera;
            serializedPlacer.FindProperty("_siteView").objectReferenceValue = site;
            serializedPlacer.FindProperty("_ghost").objectReferenceValue = ghost;
            serializedPlacer.FindProperty("_builtParent").objectReferenceValue = built.transform;

            SerializedProperty catalogue = serializedPlacer.FindProperty("_catalogue");
            catalogue.arraySize = BuildingCatalogueBuilder.Names.Length;
            for (int i = 0; i < BuildingCatalogueBuilder.Names.Length; i++)
            {
                var building = AssetDatabase.LoadAssetAtPath<BuildingDefinition>(
                    BuildingCatalogueBuilder.PathOf(BuildingCatalogueBuilder.Names[i]));

                if (building == null)
                {
                    Debug.LogWarning(
                        $"[Base] '{BuildingCatalogueBuilder.Names[i]}' is missing from the "
                        + "catalogue; it cannot be built.");
                }

                catalogue.GetArrayElementAtIndex(i).objectReferenceValue = building;
            }

            serializedPlacer.ApplyModifiedPropertiesWithoutUndo();
            return (placer, site, built.transform);
        }

        /// <summary>
        /// The stores in one corner and a line saying what is in hand in the other.
        /// </summary>
        /// <remarks>
        /// Both are needed before a price is a decision rather than a refusal: the numbers say
        /// what the colony has, and the line says which of the four reasons a spot is red.
        /// </remarks>
        private static Canvas CreateStoresHud(Scene scene, Camera camera, BuildPlacer placer)
        {
            var balance = AssetDatabase.LoadAssetAtPath<BalanceConfig>(BalanceConfigPath);

            GameObject storesObject = SceneBuilder.CreateObject(scene, "Colony Stockpile");
            ColonyStockpile stores = storesObject.AddComponent<ColonyStockpile>();

            var serializedStores = new SerializedObject(stores);
            serializedStores.FindProperty("_balance").objectReferenceValue = balance;
            serializedStores.ApplyModifiedPropertiesWithoutUndo();

            // What the readout will say the moment the scene starts. Written into the saved
            // scene as well, so the HUD can be laid out by looking at it rather than by
            // entering play mode and squinting at four lines that were blank a second ago.
            string[] opening = balance == null
                ? new[] { "SALVAGE", "FOOD", "WATER", "FUEL" }
                : new[]
                {
                    $"SALVAGE  {balance.StartingMaterials}",
                    $"FOOD  {balance.StartingFood}",
                    $"WATER  {balance.StartingWater}",
                    $"FUEL  {balance.StartingFuel}",
                };

            Canvas canvas = UiSceneParts.CreateCanvas(scene, camera);
            UiSceneParts.CreateEventSystem(scene);

            var lines = new Text[4];
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = UiSceneParts.CreateLabel(
                    canvas.transform,
                    $"Store {i}",
                    opening[i],
                    // Far enough in that the first letter clears the edge of the screen: the
                    // label is anchored at its middle and reaches half its width to the left.
                    anchoredPosition: new Vector2(-340f, 238f - (i * 22f)),
                    size: new Vector2(200f, 20f),
                    fontSize: 14,
                    colour: HudInk,
                    alignment: TextAnchor.MiddleLeft);
            }

            GameObject storesPresenterObject =
                SceneBuilder.CreateObject(scene, "Stockpile Presenter");
            StockpilePresenter storesPresenter =
                storesPresenterObject.AddComponent<StockpilePresenter>();

            var serializedPresenter = new SerializedObject(storesPresenter);
            serializedPresenter.FindProperty("_materials").objectReferenceValue = lines[0];
            serializedPresenter.FindProperty("_food").objectReferenceValue = lines[1];
            serializedPresenter.FindProperty("_water").objectReferenceValue = lines[2];
            serializedPresenter.FindProperty("_fuel").objectReferenceValue = lines[3];
            serializedPresenter.ApplyModifiedPropertiesWithoutUndo();

            Text hint = UiSceneParts.CreateLabel(
                canvas.transform,
                "Build Hint",
                "1-6  BUILD    R  TURN    ESC  PUT DOWN    CLICK A BUILDING  ITS MENU",
                anchoredPosition: new Vector2(0f, -238f),
                size: new Vector2(900f, 24f),
                fontSize: 14,
                colour: HudInk);

            GameObject hintPresenterObject = SceneBuilder.CreateObject(scene, "Build Hint Presenter");
            BuildHintPresenter hintPresenter =
                hintPresenterObject.AddComponent<BuildHintPresenter>();

            var serializedHint = new SerializedObject(hintPresenter);
            serializedHint.FindProperty("_placer").objectReferenceValue = placer;
            serializedHint.FindProperty("_line").objectReferenceValue = hint;
            serializedHint.ApplyModifiedPropertiesWithoutUndo();

            _hintPresenter = hintPresenter;
            return canvas;
        }

        /// <summary>
        /// Held on to so that the colony step can finish wiring the hint line, which needs the
        /// building actions that do not exist until then.
        /// </summary>
        private static BuildHintPresenter? _hintPresenter;

        /// <summary>
        /// The people, the day, and the two ways of telling them what to do.
        /// </summary>
        /// <remarks>
        /// Days here do not pass on their own — the clock only moves when the player says the
        /// day is over. That is what makes the base turn-shaped against the zones' real time,
        /// and it means there has to be a button for it or the colony never eats.
        /// </remarks>
        private static void CreateColony(
            Scene scene,
            Camera camera,
            Canvas canvas,
            BuildPlacer placer,
            BuildSiteView site,
            Transform built)
        {
            var balance = AssetDatabase.LoadAssetAtPath<BalanceConfig>(BalanceConfigPath);

            GameObject colonyObject = SceneBuilder.CreateObject(scene, "Colony");
            ColonyRuntime colony = colonyObject.AddComponent<ColonyRuntime>();

            var serializedColony = new SerializedObject(colony);
            serializedColony.FindProperty("_siteView").objectReferenceValue = site;
            serializedColony.FindProperty("_balance").objectReferenceValue = balance;
            serializedColony.ApplyModifiedPropertiesWithoutUndo();

            GameObject actionsObject = SceneBuilder.CreateObject(scene, "Building Actions");
            BuildingActions actions = actionsObject.AddComponent<BuildingActions>();

            var serializedActions = new SerializedObject(actions);
            serializedActions.FindProperty("_camera").objectReferenceValue = camera;
            serializedActions.FindProperty("_siteView").objectReferenceValue = site;
            serializedActions.FindProperty("_colony").objectReferenceValue = colony;
            serializedActions.FindProperty("_placer").objectReferenceValue = placer;
            serializedActions.FindProperty("_balance").objectReferenceValue = balance;
            serializedActions.FindProperty("_built").objectReferenceValue = built;
            serializedActions.ApplyModifiedPropertiesWithoutUndo();

            if (_hintPresenter != null)
            {
                var serializedHint = new SerializedObject(_hintPresenter);
                serializedHint.FindProperty("_actions").objectReferenceValue = actions;
                serializedHint.FindProperty("_siteView").objectReferenceValue = site;
                serializedHint.ApplyModifiedPropertiesWithoutUndo();
            }

            GameObject assignerObject = SceneBuilder.CreateObject(scene, "Job Assigner");
            JobAssigner assigner = assignerObject.AddComponent<JobAssigner>();

            var serializedAssigner = new SerializedObject(assigner);
            serializedAssigner.FindProperty("_camera").objectReferenceValue = camera;
            serializedAssigner.FindProperty("_siteView").objectReferenceValue = site;
            serializedAssigner.FindProperty("_colony").objectReferenceValue = colony;
            serializedAssigner.FindProperty("_placer").objectReferenceValue = placer;
            serializedAssigner.ApplyModifiedPropertiesWithoutUndo();

            Text roster = UiSceneParts.CreateLabel(
                canvas.transform,
                "Colony Roster",
                "COLONY",
                // Far enough down that the first line clears the top of the screen: the label is
                // anchored at its middle and reaches half its height upwards.
                anchoredPosition: new Vector2(330f, 160f),
                size: new Vector2(240f, 160f),
                fontSize: 13,
                colour: HudInk,
                alignment: TextAnchor.UpperLeft);

            Text day = UiSceneParts.CreateLabel(
                canvas.transform,
                "Colony Day",
                "TAB  PICK      CLICK A BUILDING  PUT TO WORK      U  STOP",
                // Shifted left and kept clear of the button, which says the same thing the
                // missing hint used to.
                anchoredPosition: new Vector2(-110f, -212f),
                size: new Vector2(660f, 22f),
                fontSize: 13,
                colour: HudInk);

            Button endDay = UiSceneParts.CreateButton(
                canvas.transform,
                "End Day Button",
                "END DAY",
                anchoredPosition: new Vector2(366f, -212f),
                size: new Vector2(180f, 36f),
                background: new Color(0.17f, 0.19f, 0.19f, 1f),
                labelColour: HudInk);

            GameObject presenterObject = SceneBuilder.CreateObject(scene, "Colony Presenter");
            ColonyPresenter presenter = presenterObject.AddComponent<ColonyPresenter>();

            var serializedPresenter = new SerializedObject(presenter);
            serializedPresenter.FindProperty("_colony").objectReferenceValue = colony;
            serializedPresenter.FindProperty("_assigner").objectReferenceValue = assigner;
            serializedPresenter.FindProperty("_siteView").objectReferenceValue = site;
            serializedPresenter.FindProperty("_roster").objectReferenceValue = roster;
            serializedPresenter.FindProperty("_day").objectReferenceValue = day;
            serializedPresenter.FindProperty("_endDay").objectReferenceValue = endDay;
            serializedPresenter.ApplyModifiedPropertiesWithoutUndo();

            CreateBuildingWheel(scene, camera, canvas, site, actions);
        }

        /// <summary>
        /// The ring of options that opens on the building the player clicks.
        /// </summary>
        /// <remarks>
        /// Only the wiring is here. The ring itself builds its own slices at runtime, because
        /// four quarters that have to meet exactly is geometry rather than layout, and geometry
        /// saved into a scene file is geometry waiting to be nudged out of true.
        /// </remarks>
        private static void CreateBuildingWheel(
            Scene scene,
            Camera camera,
            Canvas canvas,
            BuildSiteView site,
            BuildingActions actions)
        {
            GameObject wheelObject = SceneBuilder.CreateObject(scene, "Building Wheel");
            BuildingWheelView wheel = wheelObject.AddComponent<BuildingWheelView>();

            var wedge = AssetDatabase.LoadAssetAtPath<Sprite>($"{UiSprites}/ui_wheel_wedge.png");
            var hub = AssetDatabase.LoadAssetAtPath<Sprite>($"{UiSprites}/ui_wheel_hub.png");
            if (wedge == null || hub == null)
            {
                Debug.LogWarning("[Base] no wheel sprites; the building menu will be invisible.");
            }

            var serialized = new SerializedObject(wheel);
            serialized.FindProperty("_actions").objectReferenceValue = actions;
            serialized.FindProperty("_siteView").objectReferenceValue = site;
            serialized.FindProperty("_camera").objectReferenceValue = camera;
            serialized.FindProperty("_canvas").objectReferenceValue = canvas;
            serialized.FindProperty("_wedge").objectReferenceValue = wedge;
            serialized.FindProperty("_hub").objectReferenceValue = hub;
            serialized.FindProperty("_font").objectReferenceValue =
                HudArt.PixelFont() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static bool IsBlocked(int column, int row)
        {
            foreach (Vector2Int cell in BlockedCells)
            {
                if (cell.x == column && cell.y == row)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Picks one of a tile set for a cell, the same way every time.
        /// </summary>
        /// <remarks>
        /// Hashed from the coordinates rather than drawn at random, so rebuilding the scene does
        /// not reshuffle the whole plot and turn every rebuild into a large meaningless diff.
        /// </remarks>
        private static Tile? Pick(Tile[] set, int column, int row)
        {
            if (set.Length == 0)
            {
                return null;
            }

            int hash = (column * 73856093) ^ (row * 19349663);
            return set[(hash & 0x7fffffff) % set.Length];
        }
    }
}
