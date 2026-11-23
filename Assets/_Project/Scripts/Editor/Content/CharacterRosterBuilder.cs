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

        /// <summary>Where the one who walks the plot ends up.</summary>
        public const string DrifterPath = Folder + "/Drifter.asset";

        /// <summary>Where nobody in particular ends up: the colony's other people.</summary>
        public const string ColonistPath = Folder + "/Colonist.asset";

        /// <summary>
        /// Everybody on the roster, in the order the menu offers them.
        /// </summary>
        /// <remarks>
        /// One list, read by the menu that shows the cards and by the colony that puts whoever
        /// was not picked on the plot. Two lists would eventually disagree, and the way that
        /// would show up is the player meeting themselves in their own base.
        /// </remarks>
        public static readonly string[] AllPaths = { DrifterPath, SoldierPath, ArcherPath };

        /// <summary>Loads everybody on the roster, in the order the menu offers them.</summary>
        public static PlayableCharacter[] LoadAll()
        {
            var everybody = new PlayableCharacter[AllPaths.Length];
            for (int i = 0; i < AllPaths.Length; i++)
            {
                everybody[i] = AssetDatabase.LoadAssetAtPath<PlayableCharacter>(AllPaths[i]);
            }

            return everybody;
        }

        /// <summary>
        /// Builds the roster if it is not there yet, for a scene build on a fresh clone.
        /// </summary>
        public static void EnsureAll()
        {
            if (AssetDatabase.LoadAssetAtPath<PlayableCharacter>(SoldierPath) != null
                && AssetDatabase.LoadAssetAtPath<PlayableCharacter>(ArcherPath) != null
                && AssetDatabase.LoadAssetAtPath<PlayableCharacter>(ColonistPath) != null
                && AssetDatabase.LoadAssetAtPath<PlayableCharacter>(DrifterPath) != null)
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

            BuildDrifter();
            BuildSoldier();
            BuildArcher();
            BuildColonist();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Roster] built 4 character(s).");
        }

        /// <summary>
        /// The one with the long hair and the shotgun, who walks the colony's own plot.
        /// </summary>
        /// <remarks>
        /// The only one on the roster with a walk in every direction and more than one standing
        /// frame, which is why the base scene is built with them: a figure the player looks at
        /// for a whole day at a time has to be alive standing still.
        /// </remarks>
        private static void BuildDrifter() =>
            Write(
                DrifterPath,
                displayName: "Drifter",
                blurb: "Walked in off the road and never said where from.",
                available: true,
                portrait: "drifter_front_walk_0",
                front: Numbered("drifter_front_walk", 4),
                back: new[] { "drifter_back_walk" },
                left: Numbered("drifter_side_left_walk", 8),
                right: Numbered("drifter_side_right_walk", 8));

        /// <summary>
        /// Nobody in particular: the face the colony's other people wear.
        /// </summary>
        /// <remarks>
        /// Not on the menu — <see cref="Available"/> is off and they are not in
        /// <see cref="AllPaths"/> — because this is not somebody the player can be. It is the
        /// figure standing for the colonists who are only names today, cut to the same stature
        /// as everybody else so the plot does not look like two games at once.
        /// Placeholder art, and said plainly: one drawing per direction, so they bob as they
        /// walk but never take a step. A sheet of their own replaces this and nothing else
        /// changes.
        /// </remarks>
        private static void BuildColonist() =>
            Write(
                ColonistPath,
                displayName: "Colonist",
                blurb: "One of the people who were here before you.",
                available: false,
                portrait: "survivor_front_walk",
                front: new[] { "survivor_front_walk" },
                back: System.Array.Empty<string>(),
                left: new[] { "survivor_side_left_walk" },
                right: new[] { "survivor_side_right_walk" });

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
