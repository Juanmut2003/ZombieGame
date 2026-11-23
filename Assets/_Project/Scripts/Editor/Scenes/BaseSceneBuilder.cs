#nullable enable
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using ZombieGame.Base;
using ZombieGame.Core.Config;
using ZombieGame.Core.Rendering;
using ZombieGame.Core.SceneFlow;
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

        /// <summary>How far the grass runs past the plot to each side, in cells.</summary>
        /// <remarks>
        /// Wide enough that the ground still reaches the sides of the window on an ultrawide
        /// monitor. The camera shows one tile per 64 screen pixels while the pixel-perfect zoom is
        /// one, which it is for every window under 1080 tall — so a 3840-wide window looks at 60
        /// cells, and ground painted any narrower ends partway across the screen with the camera's
        /// background colour beyond it. The HUD is anchored to the window rather than to the
        /// ground, so that gap is where its edge lines end up sitting.
        /// </remarks>
        private const int MarginX = 24;

        /// <summary>How far the grass runs past the plot above and below, in cells.</summary>
        /// <remarks>
        /// Smaller than <see cref="MarginX"/> because height is what the zoom is computed from:
        /// a window tall enough to need more than this has already stepped up to zoom two, which
        /// halves the cells on screen again.
        /// </remarks>
        private const int MarginY = 8;

        /// <summary>Where the wheel's pieces are kept.</summary>
        private const string UiSprites = "Assets/_Project/Art/Sprites/UI";

        /// <summary>Where the colony's starting amounts come from.</summary>
        private const string BalanceConfigPath = "Assets/_Project/Data/BalanceConfig.asset";

        /// <summary>
        /// What the HUD's text is written in.
        /// </summary>
        /// <remarks>
        /// Dark blue. Neonbyte's glyphs are white, so this colour is the whole appearance of the
        /// text rather than a tint over it. Kept light enough to hold its own against the grass
        /// it is read over — a true navy disappears into the darker tiles.
        /// </remarks>
        private static readonly Color HudInk = new(0.15f, 0.23f, 0.56f, 1f);

        /// <summary>
        /// The size the HUD's body text is set at.
        /// </summary>
        /// <remarks>
        /// Twice Neonbyte's seven-pixel body, so every pixel of the face lands on exactly four
        /// screen pixels. A size that is not a whole multiple gives some columns of a letter one
        /// pixel and others two, which on a five-pixel-wide glyph is the difference between an E
        /// and an F.
        /// </remarks>
        private const int BodyText = 14;

        /// <summary>
        /// How wide one character of <see cref="BodyText"/> is.
        /// </summary>
        /// <remarks>
        /// Neonbyte is monospaced — five pixels and a gap, at twice scale — so the width of a
        /// line is its length times this, exactly. Every rect below is measured that way rather
        /// than guessed, which is what the old proportional face forced.
        /// </remarks>
        private const float BodyAdvance = 12f;

        /// <summary>Baseline to baseline for <see cref="BodyText"/>, from the font's line spacing.</summary>
        private const float BodyLine = 20f;

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

            for (int row = -MarginY; row < SiteHeight + MarginY; row++)
            {
                for (int column = -MarginX; column < SiteWidth + MarginX; column++)
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
                    // Spaced by the font's own line height, so the four read as one block.
                    anchoredPosition: new Vector2(-340f, 238f - (i * BodyLine)),
                    size: new Vector2(200f, BodyLine),
                    fontSize: BodyText,
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
                // The lowest of the three bottom rows. Sixty-eight characters at twelve pixels
                // is 816 wide, which is most of the frame — it gets a row to itself. Twenty-two
                // pixels of clearance under it: the frame ends at 270 and the row is 20 tall.
                anchoredPosition: new Vector2(0f, -248f),
                size: new Vector2(68f * BodyAdvance, BodyLine),
                fontSize: BodyText,
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

            // So that a colony coming home from a raid can look its buildings up and raise them
            // the same way a click does.
            serializedColony.FindProperty("_placer").objectReferenceValue = placer;
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
                // Set against the right edge rather than from a left margin, so a long line —
                // a full name beside a building and its level — grows inwards across the plot
                // instead of off the side of the frame. The longest it can reach is about forty
                // characters, which is why the box is as wide as it is.
                anchoredPosition: new Vector2(229f, 150f),
                size: new Vector2(470f, 200f),
                fontSize: BodyText,
                colour: HudInk,
                alignment: TextAnchor.UpperRight);

            Text day = UiSceneParts.CreateLabel(
                canvas.transform,
                "Colony Day",
                "TAB  PICK      CLICK A BUILDING  PUT TO WORK      U  STOP",
                // Written from the left edge rather than out from the middle. A day with all
                // three of its troubles to report runs to about seventy characters — the longest
                // line the HUD can produce — so it gets its own row above the hint, clear of the
                // buttons rather than squeezed in beside them.
                anchoredPosition: new Vector2(-29f, -222f),
                size: new Vector2(870f, BodyLine),
                fontSize: BodyText,
                colour: HudInk,
                alignment: TextAnchor.MiddleLeft);

            Button endDay = UiSceneParts.CreateButton(
                canvas.transform,
                "End Day Button",
                "END DAY",
                // Pinned by its right edge, a margin in from the side of the frame.
                anchoredPosition: new Vector2(464f, -184f),
                size: new Vector2(180f, 40f),
                background: new Color(0.17f, 0.19f, 0.19f, 1f),
                labelColour: HudInk,
                pivot: new Vector2(1f, 0.5f));

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
            CreateOrders(scene, canvas, colony, site, balance);
        }

        /// <summary>
        /// The colony's orders, the way out, and what comes back in.
        /// </summary>
        /// <remarks>
        /// PROJECT.md 4.6. The list is derived from the plot and the people every frame, so what
        /// is written into the saved scene is only what an empty colony reads — which is also
        /// exactly what the player sees a frame after a new run starts.
        /// </remarks>
        private static void CreateOrders(
            Scene scene,
            Canvas canvas,
            ColonyRuntime colony,
            BuildSiteView site,
            BalanceConfig? balance)
        {
            GameObject launcherObject = SceneBuilder.CreateObject(scene, "Raid Launcher");
            RaidLauncher launcher = launcherObject.AddComponent<RaidLauncher>();

            var serializedLauncher = new SerializedObject(launcher);
            serializedLauncher.FindProperty("_siteView").objectReferenceValue = site;
            serializedLauncher.FindProperty("_colony").objectReferenceValue = colony;
            serializedLauncher.FindProperty("_zoneScene").stringValue = GameScenes.MissionTown;
            serializedLauncher.ApplyModifiedPropertiesWithoutUndo();

            GameObject returnObject = SceneBuilder.CreateObject(scene, "Raid Return");
            RaidReturn came = returnObject.AddComponent<RaidReturn>();

            int wanted = balance == null ? 10 : balance.FirstRaidHaul;
            Text list = UiSceneParts.CreateLabel(
                canvas.transform,
                "Colony Orders",
                "ORDERS\n[ ] BUILD A BARRACKS\n[ ] BUILD A FARM\n"
                + $"[ ] PUT SOMEBODY TO WORK\n[ ] RAID THE TOWN FOR {wanted} SUPPLIES   0 / {wanted}",
                // Under the four store lines and flush with their left edge. The longest order
                // runs to about forty-five characters, and the squad's haul is added underneath
                // it, so the box is both wider and taller than the list that fits an empty
                // colony — what is written into the saved scene is the short version.
                anchoredPosition: new Vector2(-160f, 55f),
                size: new Vector2(560f, 190f),
                fontSize: BodyText,
                colour: HudInk,
                alignment: TextAnchor.UpperLeft);

            Button raid = UiSceneParts.CreateButton(
                canvas.transform,
                "Raid Button",
                "RAID TOWN",
                // Pinned by its right edge to the left of the end-day button, so the wider label
                // it carries once the squad has been out grows into the gap rather than over it.
                anchoredPosition: new Vector2(268f, -184f),
                size: new Vector2(160f, 40f),
                background: new Color(0.21f, 0.17f, 0.15f, 1f),
                labelColour: HudInk,
                widestLabel: "BACK TOMORROW",
                pivot: new Vector2(1f, 0.5f));

            GameObject presenterObject = SceneBuilder.CreateObject(scene, "Colony Task Presenter");
            ColonyTaskPresenter presenter = presenterObject.AddComponent<ColonyTaskPresenter>();

            var serialized = new SerializedObject(presenter);
            serialized.FindProperty("_siteView").objectReferenceValue = site;
            serialized.FindProperty("_colony").objectReferenceValue = colony;
            serialized.FindProperty("_raid").objectReferenceValue = launcher;
            serialized.FindProperty("_return").objectReferenceValue = came;
            serialized.FindProperty("_balance").objectReferenceValue = balance;
            serialized.FindProperty("_list").objectReferenceValue = list;
            serialized.FindProperty("_raidButton").objectReferenceValue = raid;
            serialized.FindProperty("_raidLabel").objectReferenceValue =
                raid.GetComponentInChildren<Text>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
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
