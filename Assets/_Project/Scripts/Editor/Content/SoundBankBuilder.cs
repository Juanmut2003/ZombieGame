#nullable enable
using System.IO;
using UnityEditor;
using UnityEngine;
using ZombieGame.Audio;

namespace ZombieGame.Editor.Content
{
    /// <summary>
    /// Fills the sound bank from the clips the project has, once.
    /// </summary>
    /// <remarks>
    /// Written as a tool rather than by hand in the inspector for the same reason the scenes are:
    /// a bank assembled by clicking is a thing nobody can review and nobody can rebuild. Run it
    /// again after dropping new clips in and it refills what is still empty.
    /// An existing bank is not overwritten — tuning a volume in the inspector has to survive the
    /// next person running this.
    /// </remarks>
    public static class SoundBankBuilder
    {
        private const string BankPath = "Assets/_Project/Data/Audio/SoundBank.asset";
        private const string Pack = "Assets/Plugins/Undead Survivor/Audio";

        /// <summary>Which clips stand for what, and how loud each one sits.</summary>
        private static readonly (GameSound Sound, string[] Files, float Volume, SoundBus Bus)[] Wiring =
        {
            // Two clips, so a flurry of swings does not sound like one sample on repeat.
            (GameSound.MeleeSwing, new[] { "Melee0", "Melee1" }, 0.55f, SoundBus.Effects),
            (GameSound.Gunshot, new[] { "Range" }, 0.8f, SoundBus.Effects),
            (GameSound.Impact, new[] { "Hit0", "Hit1" }, 0.45f, SoundBus.Effects),
            (GameSound.ZombieDied, new[] { "Dead" }, 0.5f, SoundBus.Effects),

            // The squad's own injuries are the one sound that must cut through a fight.
            (GameSound.SquadHurt, new[] { "Hit1" }, 0.95f, SoundBus.Effects),
            (GameSound.LootTaken, new[] { "LevelUp" }, 0.6f, SoundBus.Interface),
            (GameSound.WeaponSwitched, new[] { "Select" }, 0.5f, SoundBus.Interface),
            (GameSound.MissionWon, new[] { "Win" }, 0.8f, SoundBus.Interface),
            (GameSound.MissionLost, new[] { "Lose" }, 0.8f, SoundBus.Interface),
        };

        /// <summary>Creates the bank if it is missing and fills any entry that has no clips.</summary>
        [MenuItem("ZombieGame/Content/Build Sound Bank")]
        public static void Build()
        {
            SoundBank bank = Require();

            var serialized = new SerializedObject(bank);
            SerializedProperty entries = serialized.FindProperty("_entries");

            int filled = 0;
            foreach ((GameSound sound, string[] files, float volume, SoundBus bus) in Wiring)
            {
                if (bank.Entry(sound) is not null)
                {
                    continue;
                }

                SerializedProperty entry = Append(entries);
                entry.FindPropertyRelative("_sound").enumValueIndex = (int)sound;
                entry.FindPropertyRelative("_volume").floatValue = volume;
                entry.FindPropertyRelative("_bus").enumValueIndex = (int)bus;

                SerializedProperty clips = entry.FindPropertyRelative("_clips");
                clips.arraySize = 0;
                foreach (string file in files)
                {
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{Pack}/{file}.wav");
                    if (clip is null)
                    {
                        Debug.LogWarning($"[Audio] no clip '{file}.wav' in the pack.");
                        continue;
                    }

                    clips.arraySize++;
                    clips.GetArrayElementAtIndex(clips.arraySize - 1).objectReferenceValue = clip;
                }

                filled++;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(bank);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Audio] sound bank at {BankPath}, {filled} entry(ies) filled in.");
        }

        /// <summary>The bank asset, created empty if it is not there yet.</summary>
        public static SoundBank Require()
        {
            var existing = AssetDatabase.LoadAssetAtPath<SoundBank>(BankPath);
            if (existing is not null)
            {
                return existing;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(BankPath)!);
            SoundBank created = ScriptableObject.CreateInstance<SoundBank>();
            AssetDatabase.CreateAsset(created, BankPath);
            return created;
        }

        /// <summary>Where the bank lives, for the scene builder.</summary>
        public static string AssetPath => BankPath;

        private static SerializedProperty Append(SerializedProperty array)
        {
            array.arraySize++;
            return array.GetArrayElementAtIndex(array.arraySize - 1);
        }
    }
}
