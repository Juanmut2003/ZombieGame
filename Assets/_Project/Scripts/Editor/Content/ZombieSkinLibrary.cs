#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ZombieGame.AI;

namespace ZombieGame.Editor.Content
{
    /// <summary>
    /// Turns the sprites in <c>Art/Sprites/Zombies</c> into <see cref="ZombieSkin"/> assets.
    /// </summary>
    /// <remarks>
    /// The point of this file is that adding a new look costs no code. Art packs arrive at their
    /// own pace and most of them are not new kinds of zombie — they are another face for a kind
    /// that already exists, which is the only thing that keeps a crowd of two hundred Walkers from
    /// looking like one Walker drawn two hundred times.
    /// The naming convention is the whole interface:
    /// <c>{kind}_{variant}_{pose}.png</c>, where <c>variant</c> may be left out and <c>pose</c> is
    /// one of the six the art supports. <c>nurse_front_walk.png</c> is the Nurse's first look;
    /// <c>nurse_pale_front_walk.png</c> is another one. The part before the first underscore names
    /// the kind, matched against the zombie asset's display name, so a dropped-in skin finds its
    /// own way home.
    /// </remarks>
    public static class ZombieSkinLibrary
    {
        /// <summary>Where the sprites are dropped.</summary>
        public const string SpriteFolder = "Assets/_Project/Art/Sprites/Zombies";

        /// <summary>Where the generated skin assets live.</summary>
        public const string SkinFolder = "Assets/_Project/Data/Zombies/Skins";

        /// <summary>
        /// The six poses, as the file-name suffix and the field each one fills.
        /// </summary>
        /// <remarks>
        /// Ordered longest first, so <c>side_left_walk</c> is tested before <c>walk</c> could ever
        /// match part of it. The sheets also hold a back view, which has nowhere to go until the
        /// art grows a fourth facing.
        /// </remarks>
        private static readonly (string Pose, string Field)[] Poses =
        {
            ("side_left_attack", "_leftAlert"),
            ("side_right_attack", "_rightAlert"),
            ("side_left_walk", "_left"),
            ("side_right_walk", "_right"),
            ("front_attack", "_frontAlert"),
            ("front_walk", "_front"),
        };

        /// <summary>Rebuilds every skin asset from whatever sprites are on disk.</summary>
        [MenuItem("ZombieGame/Content/Rebuild Zombie Skins")]
        public static void RebuildFromMenu()
        {
            IReadOnlyList<ZombieSkin> skins = Rebuild();
            Debug.Log(
                $"[Skins] {skins.Count} skin asset(s) in {SkinFolder}, built from {SpriteFolder}.");
        }

        /// <summary>
        /// Creates or updates one skin asset per look found in the sprite folder.
        /// </summary>
        /// <returns>Every skin asset that was written.</returns>
        public static IReadOnlyList<ZombieSkin> Rebuild()
        {
            Directory.CreateDirectory(SkinFolder);

            var skins = new List<ZombieSkin>();
            foreach (KeyValuePair<string, List<(string Field, string Path)>> look in FindLooks())
            {
                skins.Add(WriteSkin(look.Key, look.Value));
            }

            AssetDatabase.SaveAssets();
            return skins;
        }

        /// <summary>
        /// The skins belonging to one kind, with the kind's plain look first.
        /// </summary>
        /// <remarks>
        /// The order matters a little: the first skin is what tools, tests and the bestiary show
        /// when they want "a Walker", and that should be the look the kind was designed around
        /// rather than whichever variant happens to sort first.
        /// </remarks>
        /// <param name="displayName">The zombie asset's display name, e.g. "Nurse".</param>
        public static ZombieSkin[] SkinsFor(string displayName)
        {
            string kind = displayName.ToLowerInvariant();

            var found = new List<(string Name, ZombieSkin Skin)>();
            foreach (string guid in AssetDatabase.FindAssets("t:ZombieSkin", new[] { SkinFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = Path.GetFileNameWithoutExtension(path);
                if (name != kind && !name.StartsWith(kind + "_", StringComparison.Ordinal))
                {
                    continue;
                }

                var skin = AssetDatabase.LoadAssetAtPath<ZombieSkin>(path);
                if (skin != null)
                {
                    found.Add((name, skin));
                }
            }

            return found
                .OrderBy(entry => entry.Name == kind ? 0 : 1)
                .ThenBy(entry => entry.Name, StringComparer.Ordinal)
                .Select(entry => entry.Skin)
                .ToArray();
        }

        /// <summary>
        /// Groups the sprite files by the look they belong to: skin name to field and file.
        /// </summary>
        private static SortedDictionary<string, List<(string Field, string Path)>> FindLooks()
        {
            var looks = new SortedDictionary<string, List<(string Field, string Path)>>(
                StringComparer.Ordinal);

            foreach (string path in Directory.GetFiles(SpriteFolder, "*.png"))
            {
                string name = Path.GetFileNameWithoutExtension(path);
                foreach ((string pose, string field) in Poses)
                {
                    string suffix = "_" + pose;
                    if (!name.EndsWith(suffix, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    string skinName = name[..^suffix.Length];
                    if (skinName.Length == 0)
                    {
                        break;
                    }

                    if (!looks.TryGetValue(skinName, out List<(string, string)> poses))
                    {
                        poses = new List<(string, string)>();
                        looks[skinName] = poses;
                    }

                    // Unity wants forward slashes in asset paths; Directory hands back the
                    // platform's own, which on Windows is the one that makes a load return null.
                    poses.Add((field, path.Replace('\\', '/')));
                    break;
                }
            }

            return looks;
        }

        /// <summary>Writes one skin asset, creating it when it is not there yet.</summary>
        private static ZombieSkin WriteSkin(
            string skinName, List<(string Field, string Path)> poses)
        {
            string assetPath = $"{SkinFolder}/{skinName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<ZombieSkin>(assetPath);
            ZombieSkin skin = existing != null
                ? existing
                : ScriptableObject.CreateInstance<ZombieSkin>();

            var serialized = new SerializedObject(skin);
            foreach ((string field, string path) in poses)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null)
                {
                    Debug.LogWarning($"[Skins] '{path}' has no sprite in it; '{field}' left alone.");
                    continue;
                }

                serialized.FindProperty(field).objectReferenceValue = sprite;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();

            if (existing != null)
            {
                EditorUtility.SetDirty(skin);
                return skin;
            }

            AssetDatabase.CreateAsset(skin, assetPath);
            return skin;
        }
    }
}
