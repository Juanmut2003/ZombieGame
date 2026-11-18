#nullable enable
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ZombieGame.Editor.Art
{
    /// <summary>
    /// The town set: roofs, house fronts, roads, pavement, scrub and wrecks, as tiles.
    /// </summary>
    /// <remarks>
    /// Drawn for this game rather than borrowed, so unlike <see cref="CainosArt"/> there is no
    /// third-party folder to keep out of and the sprites sit in <c>_Project/Art</c> with
    /// everything else. One file per tile at the project's 64 pixels, which means the import
    /// rules apply to them automatically and adding a tile is dropping in a PNG.
    /// Everything here returns an empty set when the art is missing, so a project without it
    /// falls back to what it had rather than painting nothing.
    /// </remarks>
    internal static class TownArt
    {
        private const string Tiles = "Assets/_Project/Art/Sprites/Tiles/Town";
        private const string Props = "Assets/_Project/Art/Sprites/Props/Town";

        /// <summary>Gardens, verges and the park.</summary>
        internal static Tile[] Grass() =>
            Set(Tiles, Tile.ColliderType.None, "grass_a", "grass_b", "grass_flowers");

        /// <summary>The strip between the road and the gardens.</summary>
        internal static Tile[] Pavement() =>
            Set(
                Tiles,
                Tile.ColliderType.None,
                "pavement_cracked_a",
                "pavement_cracked_b",
                "pavement_weeds_a",
                "pavement_weeds_b");

        /// <summary>
        /// The carriageway. Cracked asphalt is the common case and the rest are seasoning — a
        /// manhole on every third tile reads as a pattern rather than as a street.
        /// </summary>
        internal static Tile[] Road() =>
            Set(
                Tiles,
                Tile.ColliderType.None,
                "road_cracked",
                "road_cracked",
                "road_cracked",
                "road_cracked",
                "road_cracked",
                "road_cracked",
                "road_cracked",
                "road_cracked",
                "road_cracked",
                "road_cracked",
                "road_cracked",
                "road_cracked",
                "road_rubble",
                "road_rubble",
                "road_manhole",
                "road_blood");

        /// <summary>The painted line down the middle of a street, which is a whole tile here.</summary>
        internal static Tile? CentreLine(bool across) =>
            One(
                Tiles,
                "road_centre_line",
                across ? "town_line_across" : "town_line_along",
                Tile.ColliderType.None,
                across ? 90f : 0f);

        /// <summary>Floors indoors.</summary>
        internal static Tile[] Floor() =>
            Set(Tiles, Tile.ColliderType.None, "gravel", "pavement_cracked_a");

        /// <summary>
        /// The body of a building, which from above is its roof.
        /// </summary>
        /// <remarks>
        /// One tile, no variants. The set has cracked, holed and ivied roofs, and scattered along
        /// a terrace they do not read as weathering — they read as faults in the drawing. They are
        /// there to be placed deliberately later, not sprinkled.
        /// </remarks>
        internal static Tile[] Roof() =>
            Set(Tiles, Tile.ColliderType.Grid, "roof_plain");

        /// <summary>
        /// The one side of a building you are looking at: its front.
        /// </summary>
        /// <remarks>
        /// The art is drawn as a facade seen from slightly above — an eave along the top, a window
        /// below it — which only makes sense on the wall whose outside is below it on the screen.
        /// Everywhere else the building is roof. That one rule is the difference between a town of
        /// houses and a town of grey rectangles.
        /// </remarks>
        internal static Tile[] Facade() =>
            Set(Tiles, Tile.ColliderType.Grid, "facade_brick_window_b");

        /// <summary>The way into a house.</summary>
        internal static Tile[] Doorway() =>
            Set(Tiles, Tile.ColliderType.None, "facade_door");

        /// <summary>
        /// Scrub: walked through, and the zone loses sight of whoever is in it.
        /// </summary>
        /// <remarks>
        /// Trees among the bushes on purpose. A wood that is all one shrub reads as wallpaper, and
        /// the trees are drawn taller than a cell, so a wood gets a canopy that breaks the grid.
        /// </remarks>
        internal static Tile[] Scrub() =>
            Set(
                Props,
                Tile.ColliderType.None,
                "bush",
                "bush_small",
                "bush_flowers",
                "bush_dead",
                "tree_a",
                "tree_b",
                "tree_bare");

        /// <summary>Burnt-out cars, which block a street without walling it off.</summary>
        internal static Tile[] Wrecks() =>
            Set(Props, Tile.ColliderType.Grid, "wreck_car", "wreck_police", "wreck_pickup");

        /// <summary>Garden fencing.</summary>
        internal static Tile[] Fence() =>
            Set(Tiles, Tile.ColliderType.Grid, "fence", "fence_low");

        /// <summary>
        /// Builds a set of tiles from sprite names. A name repeated appears in the set more than
        /// once — which is how a tile is made commoner than its neighbours — but is only ever
        /// turned into one asset.
        /// </summary>
        private static Tile[] Set(
            string folder, Tile.ColliderType colliderType, params string[] names)
        {
            var made = new Dictionary<string, Tile>();
            var built = new List<Tile>(names.Length);

            foreach (string name in names)
            {
                if (!made.TryGetValue(name, out Tile tile))
                {
                    Tile? created = One(folder, name, $"town_{name}", colliderType, 0f);
                    if (created is null)
                    {
                        continue;
                    }

                    tile = created;
                    made[name] = tile;
                }

                built.Add(tile);
            }

            return built.ToArray();
        }

        private static Tile? One(
            string folder,
            string spriteName,
            string assetName,
            Tile.ColliderType colliderType,
            float degrees)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/town_{spriteName}.png");
            if (sprite is null)
            {
                return null;
            }

            return TileAssetFactory.GetOrCreate(
                sprite,
                assetName,
                colliderType,
                Color.white,
                Matrix4x4.TRS(
                    Centred(sprite), Quaternion.Euler(0f, 0f, degrees), Vector3.one));
        }

        /// <summary>
        /// How far to shift a sprite so it sits in the middle of its cell.
        /// </summary>
        /// <remarks>
        /// The ground tiles are pivoted in the middle and need nothing. The props are pivoted at
        /// the foot, the way anything standing on a floor is, and without this a tree grows out of
        /// the cell above its own.
        /// </remarks>
        private static Vector3 Centred(Sprite sprite)
        {
            float fromBottom = sprite.pivot.y / sprite.pixelsPerUnit;
            float middle = sprite.rect.height / sprite.pixelsPerUnit / 2f;
            return new Vector3(0f, fromBottom - middle, 0f);
        }
    }
}
