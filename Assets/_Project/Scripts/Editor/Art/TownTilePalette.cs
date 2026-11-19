#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ZombieGame.Editor.Art
{
    /// <summary>
    /// Turns every sprite of the town set into a tile, and gathers them into a palette you can
    /// paint from in the editor.
    /// </summary>
    /// <remarks>
    /// The scene builder only ever creates tiles for the sprites it uses itself, so most of the
    /// set existed as PNGs and nothing else — invisible to the Tile Palette window, which lists
    /// palettes rather than folders. This makes the other half exist and puts all of it where a
    /// person can reach it.
    /// A palette is not an asset type of its own: it is a prefab holding a grid and a tilemap,
    /// with a <see cref="GridPalette"/> stored inside it. Built here by hand rather than through
    /// the editor's own helper, because that helper's signature has moved between Unity versions
    /// and this has to keep working.
    /// </remarks>
    public static class TownTilePalette
    {
        private const string TileFolder = TileAssetFactory.TileFolder;
        private const string PalettePath = "Assets/_Project/Art/Palettes/Town.prefab";

        private static readonly string[] SpriteFolders =
        {
            "Assets/_Project/Art/Sprites/Tiles/Town",
            "Assets/_Project/Art/Sprites/Props/Town",
        };

        /// <summary>
        /// What each kind of tile stops. Painting is the person's business; what a wall does is
        /// not, and a palette that hands out roofs with no collision would make a house you can
        /// walk through.
        /// </summary>
        private static readonly (string Prefix, Tile.ColliderType Collider)[] Solidity =
        {
            ("town_roof", Tile.ColliderType.Grid),
            ("town_facade_door", Tile.ColliderType.None),
            ("town_facade", Tile.ColliderType.Grid),
            ("town_fence_gate", Tile.ColliderType.None),
            ("town_fence", Tile.ColliderType.Grid),
            ("town_wreck", Tile.ColliderType.Grid),
            ("town_steps", Tile.ColliderType.None),
        };

        /// <summary>Makes a tile out of every town sprite and rebuilds the palette.</summary>
        [MenuItem("ZombieGame/Content/Rebuild Town Tile Palette")]
        public static void Rebuild()
        {
            List<Tile> tiles = BuildTiles();
            if (tiles.Count == 0)
            {
                Debug.LogWarning("[Palette] no town sprites found; nothing to build.");
                return;
            }

            BuildPalette(tiles);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Palette] {tiles.Count} tile(s) in {PalettePath}");
        }

        /// <summary>Creates or refreshes the tile asset for every sprite in the town folders.</summary>
        public static List<Tile> BuildTiles()
        {
            var made = new List<Tile>();

            foreach (string folder in SpriteFolders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    continue;
                }

                foreach (string path in Directory
                             .GetFiles(folder, "*.png")
                             .OrderBy(each => each, StringComparer.Ordinal))
                {
                    string assetPath = path.Replace('\\', '/');
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                    if (sprite is null)
                    {
                        continue;
                    }

                    string name = Path.GetFileNameWithoutExtension(assetPath);
                    Tile? tile = TileAssetFactory.GetOrCreate(
                        sprite,
                        name,
                        ColliderFor(name),
                        Color.white,
                        Matrix4x4.TRS(Centred(sprite), Quaternion.identity, Vector3.one));

                    if (tile is not null)
                    {
                        made.Add(tile);
                    }
                }
            }

            return made;
        }

        private static Tile.ColliderType ColliderFor(string name)
        {
            foreach ((string prefix, Tile.ColliderType collider) in Solidity)
            {
                if (name.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return collider;
                }
            }

            // Ground, roads, plants and litter. Scrub is cover rather than a barrier, which is
            // the whole point of being able to walk into it.
            return Tile.ColliderType.None;
        }

        /// <summary>How far to shift a sprite so it sits in the middle of its cell.</summary>
        private static Vector3 Centred(Sprite sprite)
        {
            float fromBottom = sprite.pivot.y / sprite.pixelsPerUnit;
            float middle = sprite.rect.height / sprite.pixelsPerUnit / 2f;
            return new Vector3(0f, fromBottom - middle, 0f);
        }

        /// <summary>Lays the tiles out in a prefab the Tile Palette window can open.</summary>
        private static void BuildPalette(IReadOnlyList<Tile> tiles)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PalettePath)!);

            var root = new GameObject("Town");
            try
            {
                Grid grid = root.AddComponent<Grid>();
                grid.cellSize = Vector3.one;

                var layer = new GameObject("Layer1");
                layer.transform.SetParent(root.transform, worldPositionStays: false);

                Tilemap map = layer.AddComponent<Tilemap>();
                layer.AddComponent<TilemapRenderer>();

                // Twelve across, which is how the source sheet is laid out, so a tile sits where
                // you remember seeing it. Rows run downwards from the origin.
                const int across = 12;
                for (int i = 0; i < tiles.Count; i++)
                {
                    map.SetTile(new Vector3Int(i % across, -(i / across), 0), tiles[i]);
                }

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PalettePath);
                AttachPaletteSettings(saved);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// Puts the <see cref="GridPalette"/> inside the prefab, which is what makes the Tile
        /// Palette window treat it as a palette rather than as an ordinary prefab.
        /// </summary>
        private static void AttachPaletteSettings(GameObject prefab)
        {
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(PalettePath))
            {
                if (asset is GridPalette existing)
                {
                    existing.cellSizing = GridPalette.CellSizing.Automatic;
                    EditorUtility.SetDirty(existing);
                    return;
                }
            }

            var settings = ScriptableObject.CreateInstance<GridPalette>();
            settings.name = "Palette Settings";
            settings.cellSizing = GridPalette.CellSizing.Automatic;

            AssetDatabase.AddObjectToAsset(settings, prefab);
            EditorUtility.SetDirty(settings);
        }
    }
}
