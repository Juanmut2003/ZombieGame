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

            return GetOrCreate(
                sprite,
                $"{Path.GetFileNameWithoutExtension(spritePath)}_tile",
                colliderType,
                Color.white,
                Matrix4x4.identity);
        }

        /// <summary>
        /// Returns the tile asset named <paramref name="assetName"/>, creating it if it does not
        /// exist yet and painting it with <paramref name="sprite"/>.
        /// </summary>
        /// <remarks>
        /// The overload that takes a path cannot reach a sprite inside a sliced sheet, and the
        /// third-party sets are all sliced sheets. This one takes the sprite itself, so a tile can
        /// be built from art this project does not own without copying or editing any of it.
        /// </remarks>
        /// <param name="sprite">The sprite to paint with.</param>
        /// <param name="assetName">File name of the tile asset, without extension.</param>
        /// <param name="colliderType">What, if anything, the tile should stop.</param>
        /// <param name="tint">Multiplied into the sprite as it is drawn.</param>
        /// <param name="placement">Moves or turns the sprite within its cell.</param>
        public static Tile? GetOrCreate(
            Sprite sprite,
            string assetName,
            Tile.ColliderType colliderType,
            Color tint,
            Matrix4x4 placement)
        {
            Directory.CreateDirectory(TileFolder);
            string tilePath = $"{TileFolder}/{assetName}.asset";

            var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            bool isNew = tile is null;
            if (isNew)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
            }

            tile!.sprite = sprite;
            tile.colliderType = colliderType;
            tile.color = tint;
            tile.transform = placement;
            tile.flags = TileFlags.LockTransform | TileFlags.LockColor;

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
