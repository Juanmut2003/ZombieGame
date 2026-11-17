#nullable enable
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ZombieGame.Editor.Art
{
    /// <summary>
    /// Creates the <see cref="Tile"/> assets a tilemap paints with, one per sprite.
    /// </summary>
    /// <remarks>
    /// Tile assets are ScriptableObjects, so by the rules in CLAUDE.md they belong in
    /// <c>_Project/Data</c> rather than next to the art.
    /// </remarks>
    public static class TileAssetFactory
    {
        /// <summary>Where generated tile assets live.</summary>
        public const string TileFolder = "Assets/_Project/Data/Tiles";

        /// <summary>
        /// Returns the tile asset for <paramref name="spritePath"/>, creating it if it does not
        /// exist yet. An existing asset is reused and its sprite refreshed, so references from
        /// scenes and palettes survive a regeneration.
        /// </summary>
        /// <param name="spritePath">Asset path of the sprite to paint with.</param>
        /// <param name="colliderType">
        /// <see cref="Tile.ColliderType.None"/> for floors, <see cref="Tile.ColliderType.Sprite"/>
        /// for anything that should block movement.
        /// </param>
        public static Tile? GetOrCreate(string spritePath, Tile.ColliderType colliderType)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite is null)
            {
                Debug.LogWarning($"[Tiles] no sprite at '{spritePath}'.");
                return null;
            }

            Directory.CreateDirectory(TileFolder);
            string tilePath = $"{TileFolder}/{Path.GetFileNameWithoutExtension(spritePath)}_tile.asset";

            var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            bool isNew = tile is null;
            if (isNew)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
            }

            tile!.sprite = sprite;
            tile.colliderType = colliderType;
            tile.color = Color.white;

            if (isNew)
            {
                AssetDatabase.CreateAsset(tile, tilePath);
            }
            else
            {
                EditorUtility.SetDirty(tile);
            }

            AssetDatabase.SaveAssetIfDirty(tile);
            return tile;
        }
    }
}
