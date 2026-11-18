#nullable enable
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ZombieGame.Editor.Art
{
    /// <summary>
    /// The Cainos "Pixel Art Top Down - Basic" set, as tiles this project can paint with.
    /// </summary>
    /// <remarks>
    /// The pack ships textures and its own tile assets, but neither is used directly. Its tiles
    /// carry no collision and their colour is its own, and a zone needs walls that stop people and
    /// a road that is darker than the pavement beside it. So the sprites are borrowed and the tile
    /// assets are ours, generated into <c>_Project/Data/Tiles</c> — nothing under
    /// <c>Plugins/</c> is written to, which is what keeps a reimport of the pack harmless.
    /// Every lookup returns an empty array when the pack is not installed. A zone then paints
    /// itself with the placeholders it always had, rather than coming out empty.
    /// </remarks>
    internal static class CainosArt
    {
        private const string Root = "Assets/Plugins/Cainos/Pixel Art Top Down - Basic/Texture";
        private const string GrassTexture = Root + "/TX Tileset Grass.png";
        private const string StoneTexture = Root + "/TX Tileset Stone Ground.png";
        private const string WallTexture = Root + "/TX Tileset Wall.png";
        private const string PlantTexture = Root + "/TX Plant.png";
        private const string PropTexture = Root + "/TX Props.png";

        /// <summary>Ours, not the pack's: no top-down tileset ships road markings.</summary>
        private const string RoadDashSprite = "Assets/_Project/Art/Sprites/Tiles/road_dash.png";

        /// <summary>Asphalt. The slabs are a warm light grey; a road is neither.</summary>
        private static readonly Color RoadTint = new(0.42f, 0.42f, 0.44f);

        /// <summary>Indoors, where the same slabs are lit by whatever is left of the roof.</summary>
        private static readonly Color FloorTint = new(0.78f, 0.76f, 0.72f);

        /// <summary>Whether the pack is in the project at all.</summary>
        internal static bool IsInstalled =>
            AssetDatabase.LoadMainAssetAtPath(GrassTexture) is not null;

        /// <summary>Open ground: mostly plain, with flowers often enough to break the repetition.</summary>
        internal static Tile[] Grass()
        {
            var names = new List<string>();
            for (int i = 0; i < 16; i++)
            {
                names.Add($"TX Tileset Grass {i}");
            }

            // Two in eighteen, so a field reads as a field rather than as a pattern.
            names.Add("TX Tileset Grass Flower 3");
            names.Add("TX Tileset Grass Flower 9");

            return Tiles(GrassTexture, names, "grass", Tile.ColliderType.None, Color.white);
        }

        /// <summary>Paving slabs set into the grass, for the strip between road and garden.</summary>
        internal static Tile[] Pavement()
        {
            var names = new List<string>();
            for (int i = 0; i < 6; i++)
            {
                names.Add($"TX Tileset Grass Pavement {i}");
            }

            return Tiles(GrassTexture, names, "pavement", Tile.ColliderType.None, Color.white);
        }

        /// <summary>The roadway, which is the stone ground darkened until it reads as tarmac.</summary>
        internal static Tile[] Road() => Stone("road", RoadTint);

        /// <summary>Floors indoors: the same stone, a shade off the daylight outside.</summary>
        internal static Tile[] Floor() => Stone("floor", FloorTint);

        /// <summary>Brickwork, and the only tiles here that stop anybody.</summary>
        /// <remarks>
        /// One tile, not a set. The sheet's other solid faces carry cracks and patches, and
        /// scattered along a terraced street they do not read as weathering — they read as holes
        /// and windows in a wall that has neither, which is worse than a wall that repeats.
        /// </remarks>
        internal static Tile[] Wall() =>
            Tiles(
                WallTexture,
                new[] { "TX Tileset Wall_29" },
                "wall",
                Tile.ColliderType.Grid,
                Color.white);

        /// <summary>
        /// A threshold: the floor of a doorway, lit from outside so the gap in a wall can be seen
        /// from across the street.
        /// </summary>
        internal static Tile[] Doorway() =>
            Tiles(
                StoneTexture,
                new[] { "TX Tileset Stone Ground_3" },
                "doorway",
                Tile.ColliderType.None,
                // Lighter than either the brick around it or the floor behind it. A darker
                // threshold reads as a hole punched in the wall; a bright one reads as daylight
                // falling through a door, which is the thing the player is looking for.
                new Color(1.1f, 1.06f, 0.94f));

        /// <summary>
        /// Scrub, as tiles rather than objects: the forest around a zone is well over a thousand
        /// cells, and a GameObject each would cost more than everything else in the scene put
        /// together.
        /// Nothing here stops anybody. Scrub is cover, not a wall — you push through it, and the
        /// zone loses track of you while you are in it.
        /// </summary>
        internal static Tile[] Bushes()
        {
            var names = new List<string>();
            for (int i = 1; i <= 6; i++)
            {
                names.Add($"TX Bush T{i}");
            }

            return Tiles(PlantTexture, names, "bush", Tile.ColliderType.None, Color.white);
        }

        /// <summary>The chest sprite, for a container worth walking across a town for.</summary>
        internal static Sprite? Chest() => SpriteNamed(PropTexture, "TX Props Chest");

        /// <summary>
        /// One dash of a road's centre line. The pack has no road markings, so this one is drawn
        /// by the project's own sprite generator and turned a quarter turn for a street running
        /// the other way — a line is a line, and one sprite serves both.
        /// </summary>
        internal static Tile? RoadLine(bool across)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(RoadDashSprite);
            if (sprite is null)
            {
                return null;
            }

            return TileAssetFactory.GetOrCreate(
                sprite,
                across ? "road_line_across" : "road_line_along",
                Tile.ColliderType.None,
                Color.white,
                Matrix4x4.TRS(
                    Vector3.zero,
                    Quaternion.Euler(0f, 0f, across ? 90f : 0f),
                    Vector3.one));
        }

        private static Tile[] Stone(string purpose, Color tint)
        {
            var names = new List<string>();
            for (int i = 0; i < 8; i++)
            {
                names.Add($"TX Tileset Stone Ground_{i}");
            }

            return Tiles(StoneTexture, names, purpose, Tile.ColliderType.None, tint);
        }

        private static Tile[] Tiles(
            string texturePath,
            IReadOnlyList<string> spriteNames,
            string purpose,
            Tile.ColliderType colliderType,
            Color tint)
        {
            if (!IsInstalled)
            {
                return System.Array.Empty<Tile>();
            }

            var built = new List<Tile>(spriteNames.Count);
            foreach (string spriteName in spriteNames)
            {
                Sprite? sprite = SpriteNamed(texturePath, spriteName);
                if (sprite is null)
                {
                    Debug.LogWarning($"[Cainos] no sprite '{spriteName}' in '{texturePath}'.");
                    continue;
                }

                Tile? tile = TileAssetFactory.GetOrCreate(
                    sprite,
                    $"cainos_{purpose}_{built.Count}",
                    colliderType,
                    tint,
                    Matrix4x4.TRS(Centred(sprite), Quaternion.identity, Vector3.one));
                if (tile is not null)
                {
                    built.Add(tile);
                }
            }

            return built.ToArray();
        }

        /// <summary>
        /// How far to shift a sprite so it sits in the middle of its cell.
        /// </summary>
        /// <remarks>
        /// A tilemap puts the sprite's pivot on the cell's anchor. The ground sheets are pivoted
        /// in the middle and need nothing; the plants are pivoted at the foot, the way a prop is
        /// that stands on the floor, and without this they grow out of the cell above.
        /// </remarks>
        private static Vector3 Centred(Sprite sprite)
        {
            float fromBottom = sprite.pivot.y / sprite.pixelsPerUnit;
            float middle = sprite.rect.height / sprite.pixelsPerUnit / 2f;
            return new Vector3(0f, fromBottom - middle, 0f);
        }

        /// <summary>
        /// Finds one sprite inside a sliced sheet. <see cref="AssetDatabase.LoadAssetAtPath{T}"/>
        /// returns only the first, which on these sheets is whichever one happened to be cut first.
        /// </summary>
        private static Sprite? SpriteNamed(string texturePath, string spriteName)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(texturePath))
            {
                if (asset is Sprite sprite && sprite.name == spriteName)
                {
                    return sprite;
                }
            }

            return null;
        }
    }
}
