#nullable enable
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using ZombieGame.Core.Roster;

namespace ZombieGame.Editor.Content
{
    /// <summary>
    /// Builds the roster assets the menu shows and the zone dresses the MC from.
    /// </summary>
    /// <remarks>
    /// Written from code for the same reason the zombie skins are: the art is a folder of
    /// numbered files, and wiring six frames into four fields by hand is the kind of job that
    /// is wrong once and then wrong for months. Run it again after adding frames and the assets
    /// catch up.
    /// </remarks>
    public static class CharacterRosterBuilder
    {
        private const string Folder = "Assets/_Project/Data/Characters";
        private const string Sprites = "Assets/_Project/Art/Sprites/Characters";

        /// <summary>Where the soldier ends up, for the scene builders to load.</summary>
        public const string SoldierPath = Folder + "/Soldier.asset";

        /// <summary>Where the hooded one ends up.</summary>
        public const string ArcherPath = Folder + "/Archer.asset";

        /// <summary>
        /// Builds the roster if it is not there yet, for a scene build on a fresh clone.
        /// </summary>
        public static void EnsureAll()
        {
            if (AssetDatabase.LoadAssetAtPath<PlayableCharacter>(SoldierPath) != null
                && AssetDatabase.LoadAssetAtPath<PlayableCharacter>(ArcherPath) != null)
            {
                return;
            }

            BuildAll();
        }

        /// <summary>Rebuilds every character on the roster.</summary>
        [MenuItem("ZombieGame/Content/Build Character Roster")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(Folder);

            BuildSoldier();
            BuildArcher();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Roster] built 2 character(s).");
        }

        private static void BuildSoldier() =>
            Write(
                SoldierPath,
                displayName: "Soldier",
                blurb: "Came in with the last convoy.",
                available: true,
                portrait: "soldier_front_walk",
                front: new[] { "soldier_front_walk" },
                back: new[] { "soldier_back_walk" },
                left: Numbered("soldier_side_left_walk", 6),
                right: Numbered("soldier_side_right_walk", 6));

        private static void BuildArcher() =>
            Write(
                ArcherPath,
                displayName: "Archer",
                blurb: "Needs a walk of their own first.",
                available: false,
                portrait: "survivor_front_walk",
                front: new[] { "survivor_front_walk" },
                back: System.Array.Empty<string>(),
                left: new[] { "survivor_side_left_walk" },
                right: new[] { "survivor_side_right_walk" });

        private static string[] Numbered(string stem, int count)
        {
            var names = new string[count];
            for (int i = 0; i < count; i++)
            {
                names[i] = $"{stem}_{i}";
            }

            return names;
        }

        private static void Write(
            string path,
            string displayName,
            string blurb,
            bool available,
            string portrait,
            string[] front,
            string[] back,
            string[] left,
            string[] right)
        {
            var character = AssetDatabase.LoadAssetAtPath<PlayableCharacter>(path);
            if (character == null)
            {
                character = ScriptableObject.CreateInstance<PlayableCharacter>();
                AssetDatabase.CreateAsset(character, path);
            }

            var serialized = new SerializedObject(character);
            serialized.FindProperty("_displayName").stringValue = displayName;
            serialized.FindProperty("_blurb").stringValue = blurb;
            serialized.FindProperty("_available").boolValue = available;
            serialized.FindProperty("_portrait").objectReferenceValue = Load(portrait);

            Fill(serialized.FindProperty("_front"), front);
            Fill(serialized.FindProperty("_back"), back);
            Fill(serialized.FindProperty("_left"), left);
            Fill(serialized.FindProperty("_right"), right);

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(character);
        }

        private static void Fill(SerializedProperty array, IReadOnlyList<string> names)
        {
            var found = new List<Sprite>(names.Count);
            foreach (string name in names)
            {
                Sprite? sprite = Load(name);
                if (sprite == null)
                {
                    // Said out loud: a missing frame in the middle of a walk shortens the cycle
                    // rather than breaking it, and would otherwise only show up as a limp.
                    Debug.LogWarning($"[Roster] no sprite '{name}'; leaving it out of the walk.");
                    continue;
                }

                found.Add(sprite);
            }

            array.arraySize = found.Count;
            for (int i = 0; i < found.Count; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = found[i];
            }
        }

        private static Sprite? Load(string name) =>
            AssetDatabase.LoadAssetAtPath<Sprite>($"{Sprites}/{name}.png");
    }
}
