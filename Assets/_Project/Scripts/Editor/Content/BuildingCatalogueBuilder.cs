#nullable enable
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using ZombieGame.Base;
using ZombieGame.Economy;
using ZombieGame.Editor.Art;

namespace ZombieGame.Editor.Content
{
    /// <summary>
    /// Builds everything a site can put up, with the levels each one climbs.
    /// </summary>
    /// <remarks>
    /// The four buildings of PROJECT.md 4.2 with the ladder of 4.2.1 under them, plus the two
    /// support structures the colony cannot run without. Written from code because a footprint
    /// and a sprite have to match each other to the pixel across every level, and hand-typed
    /// pairs are chances to be quietly wrong.
    /// The split between what is rewritten and what is seeded is the point of this file. Shape
    /// and art are structural and are written every time; beds, posts, yields, draws and prices
    /// are balance and are written only when the asset is first created, so that rebuilding the
    /// catalogue cannot undo an afternoon of tuning.
    /// </remarks>
    public static class BuildingCatalogueBuilder
    {
        private const string Folder = "Assets/_Project/Data/Buildings";
        private const string Sprites = "Assets/_Project/Art/Sprites/Buildings";

        /// <summary>Everything buildable, in the order the build menu offers it.</summary>
        public static readonly string[] Names =
        {
            "Farm", "Barracks", "Factory", "Armory", "Generator", "Well",
        };

        /// <summary>One rung of a building's ladder, as the seed values want it.</summary>
        private sealed class Rung
        {
            public int Width = 3;
            public int Height = 5;
            public int Beds;
            public int Posts;
            public ResourceAmount[] Yield = Array.Empty<ResourceAmount>();
            public int PowerDraw;
            public int PowerSupply;
            public int FuelPerDay;
            public int WaterPerDay;
            public ResourceAmount[] Upgrade = Array.Empty<ResourceAmount>();
        }

        private static ResourceAmount Salvage(int amount) => new(ResourceKind.Materials, amount);

        private static ResourceAmount Water(int amount) => new(ResourceKind.Water, amount);

        private static ResourceAmount Fuel(int amount) => new(ResourceKind.Fuel, amount);

        private static ResourceAmount Food(int amount) => new(ResourceKind.Food, amount);

        /// <summary>Where one of them ends up.</summary>
        public static string PathOf(string name) => $"{Folder}/{name}.asset";

        /// <summary>Builds the catalogue if it is not there yet, for a fresh clone.</summary>
        public static void EnsureAll()
        {
            foreach (string name in Names)
            {
                if (AssetDatabase.LoadAssetAtPath<BuildingDefinition>(PathOf(name)) == null)
                {
                    BuildAll();
                    return;
                }
            }
        }

        /// <summary>Rebuilds everything.</summary>
        [MenuItem("ZombieGame/Content/Build Building Catalogue")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(Folder);
            PlaceholderSpriteGenerator.GenerateAll();

            Write(
                "Farm",
                "Crop plots and a water trough. Feeds the colony.",
                BuildingCategory.Farm,
                new[] { Salvage(45), Water(10) },
                new[]
                {
                    new Rung { Width = 3, Height = 5, Posts = 1, Yield = new[] { Food(6) }, WaterPerDay = 4 },
                    new Rung
                    {
                        Width = 4, Height = 5, Posts = 2, Yield = new[] { Food(6) }, WaterPerDay = 6,
                        Upgrade = new[] { Salvage(60), Water(10) },
                    },
                    new Rung
                    {
                        Width = 4, Height = 6, Posts = 3, Yield = new[] { Food(6) }, WaterPerDay = 8,
                        PowerDraw = 3, Upgrade = new[] { Salvage(90), Water(15) },
                    },
                    new Rung
                    {
                        Width = 5, Height = 6, Posts = 4, Yield = new[] { Food(6) }, WaterPerDay = 10,
                        PowerDraw = 5, Upgrade = new[] { Salvage(130), Water(20) },
                    },
                });

            Write(
                "Barracks",
                "Beds and somewhere to put them. Caps who can live here.",
                BuildingCategory.Barracks,
                new[] { Salvage(35) },
                new[]
                {
                    new Rung { Width = 3, Height = 5, Beds = 2 },
                    new Rung { Width = 4, Height = 5, Beds = 4, Upgrade = new[] { Salvage(50) } },
                    new Rung
                    {
                        Width = 4, Height = 6, Beds = 6, PowerDraw = 2,
                        Upgrade = new[] { Salvage(75) },
                    },
                    new Rung
                    {
                        Width = 5, Height = 6, Beds = 8, PowerDraw = 3,
                        Upgrade = new[] { Salvage(110) },
                    },
                });

            Write(
                "Factory",
                "The workbench everything else is made at. Loud and hungry for power.",
                BuildingCategory.Factory,
                new[] { Salvage(70), Fuel(10) },
                new[]
                {
                    new Rung { Width = 3, Height = 5, Posts = 1, PowerDraw = 4 },
                    new Rung
                    {
                        Width = 4, Height = 5, Posts = 2, PowerDraw = 6,
                        Upgrade = new[] { Salvage(95), Fuel(10) },
                    },
                    new Rung
                    {
                        Width = 4, Height = 6, Posts = 3, PowerDraw = 9,
                        Upgrade = new[] { Salvage(140), Fuel(15) },
                    },
                    new Rung
                    {
                        Width = 5, Height = 6, Posts = 4, PowerDraw = 12,
                        Upgrade = new[] { Salvage(200), Fuel(25) },
                    },
                });

            Write(
                "Armory",
                "Weapons, ammunition and the loadout bench. Raids come here first.",
                BuildingCategory.Armory,
                new[] { Salvage(40), Fuel(5) },
                new[]
                {
                    new Rung { Width = 3, Height = 5, Posts = 1, PowerDraw = 2 },
                    new Rung
                    {
                        Width = 4, Height = 5, Posts = 2, PowerDraw = 3,
                        Upgrade = new[] { Salvage(60), Fuel(5) },
                    },
                    new Rung
                    {
                        Width = 4, Height = 6, Posts = 3, PowerDraw = 4,
                        Upgrade = new[] { Salvage(90), Fuel(10) },
                    },
                    new Rung
                    {
                        Width = 5, Height = 6, Posts = 4, PowerDraw = 6,
                        Upgrade = new[] { Salvage(135), Fuel(15) },
                    },
                });

            // The two the colony cannot run without, off the sheets in Docs/Buildings: a shed
            // with one diesel set, then two, then a plant room, then an industrial hall.
            // The sheets also give each rung a crew. A generator's output is a property of the
            // machine and not of who is standing next to it, so posts here would be a job that
            // changes nothing — left at zero until a generator can actually be run badly.
            Write(
                "Generator",
                "Diesel. Loud, thirsty, and the only reason anything else has power.",
                BuildingCategory.Support,
                new[] { Salvage(50), Fuel(10) },
                new[]
                {
                    new Rung { Width = 2, Height = 2, PowerSupply = 10, FuelPerDay = 2 },
                    new Rung
                    {
                        Width = 3, Height = 3, PowerSupply = 25, FuelPerDay = 4,
                        Upgrade = new[] { Salvage(70), Fuel(15) },
                    },
                    new Rung
                    {
                        Width = 4, Height = 4, PowerSupply = 50, FuelPerDay = 7,
                        Upgrade = new[] { Salvage(110), Fuel(25) },
                    },
                    new Rung
                    {
                        Width = 5, Height = 4, PowerSupply = 100, FuelPerDay = 12,
                        Upgrade = new[] { Salvage(170), Fuel(40) },
                    },
                });

            // The well's sheet gives a day's water at full staffing; the yield here is that
            // figure shared out over the posts, so a half-crewed well brings in half as much.
            // From level three the pump is electric, which is where water starts costing power.
            Write(
                "Well",
                "A hole and a pump. Somebody has to work it.",
                BuildingCategory.Support,
                new[] { Salvage(30) },
                new[]
                {
                    new Rung { Width = 2, Height = 3, Posts = 1, Yield = new[] { Water(10) } },
                    new Rung
                    {
                        Width = 3, Height = 4, Posts = 2, Yield = new[] { Water(13) },
                        Upgrade = new[] { Salvage(45) },
                    },
                    new Rung
                    {
                        Width = 4, Height = 5, Posts = 3, Yield = new[] { Water(17) },
                        PowerDraw = 3, Upgrade = new[] { Salvage(70), Fuel(5) },
                    },
                    new Rung
                    {
                        Width = 5, Height = 5, Posts = 4, Yield = new[] { Water(25) },
                        PowerDraw = 5, Upgrade = new[] { Salvage(110), Fuel(10) },
                    },
                });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Buildings] built {Names.Length} building(s).");
        }

        private static void Write(
            string name,
            string blurb,
            BuildingCategory category,
            ResourceAmount[] seedCost,
            Rung[] seedLevels)
        {
            string path = PathOf(name);
            var building = AssetDatabase.LoadAssetAtPath<BuildingDefinition>(path);
            bool created = building == null;
            if (created)
            {
                building = ScriptableObject.CreateInstance<BuildingDefinition>();
                AssetDatabase.CreateAsset(building, path);
            }

            var serialized = new SerializedObject(building);
            serialized.FindProperty("_displayName").stringValue = name;
            serialized.FindProperty("_blurb").stringValue = blurb;
            serialized.FindProperty("_category").enumValueIndex = (int)category;

            SerializedProperty levels = serialized.FindProperty("_levels");

            // A rung that was not there a moment ago has nothing to protect, so it is seeded
            // like a new asset. Without this a building that grows a ladder gets four levels of
            // the right size and shape, all of them worth nothing.
            int had = levels.arraySize;
            levels.arraySize = seedLevels.Length;

            for (int i = 0; i < seedLevels.Length; i++)
            {
                SerializedProperty rung = levels.GetArrayElementAtIndex(i);

                // Structural, every time: the picture has to be exactly as big as the ground it
                // claims, or free placement looks broken.
                string spriteName = $"building_{name.ToLowerInvariant()}_{i + 1}";
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{Sprites}/{spriteName}.png");
                if (sprite == null)
                {
                    Debug.LogWarning($"[Buildings] no sprite '{spriteName}'.");
                }

                rung.FindPropertyRelative("_sprite").objectReferenceValue = sprite;
                rung.FindPropertyRelative("_width").intValue = seedLevels[i].Width;
                rung.FindPropertyRelative("_height").intValue = seedLevels[i].Height;

                if (!created && i < had)
                {
                    continue;
                }

                rung.FindPropertyRelative("_beds").intValue = seedLevels[i].Beds;
                rung.FindPropertyRelative("_workPosts").intValue = seedLevels[i].Posts;
                rung.FindPropertyRelative("_powerDraw").intValue = seedLevels[i].PowerDraw;
                rung.FindPropertyRelative("_powerSupply").intValue = seedLevels[i].PowerSupply;
                rung.FindPropertyRelative("_fuelPerDay").intValue = seedLevels[i].FuelPerDay;
                rung.FindPropertyRelative("_waterPerDay").intValue = seedLevels[i].WaterPerDay;

                FillList(rung.FindPropertyRelative("_yieldPerWorker"), seedLevels[i].Yield);
                FillList(rung.FindPropertyRelative("_upgradeCost"), seedLevels[i].Upgrade);
            }

            if (created)
            {
                FillList(serialized.FindProperty("_cost"), seedCost);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(building);
        }

        private static void FillList(SerializedProperty array, ResourceAmount[] lines)
        {
            array.arraySize = lines.Length;
            for (int i = 0; i < lines.Length; i++)
            {
                SerializedProperty line = array.GetArrayElementAtIndex(i);
                line.FindPropertyRelative("_kind").enumValueIndex = (int)lines[i].Kind;
                line.FindPropertyRelative("_amount").intValue = lines[i].Amount;
            }
        }
    }
}
