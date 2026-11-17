#nullable enable
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ZombieGame.Editor.Content
{
    /// <summary>
    /// Writes every field of the data assets back to disk, including the ones that were never
    /// saved.
    /// </summary>
    /// <remarks>
    /// Unity only serialises the fields that existed when an asset was last written. A field added
    /// to a ScriptableObject afterwards is absent from the YAML and silently takes the value of
    /// its C# initialiser — so the asset a designer is supposed to tune does not actually contain
    /// the numbers the game runs on, and a change to the initialiser moves the game without
    /// anybody touching the data.
    /// Running this after adding a field puts the values where they belong. It changes no value:
    /// what it writes is what the asset already resolves to.
    /// Run from the menu, or with
    /// <c>-executeMethod ZombieGame.Editor.Content.DataAssetReserializer.Reserialize</c>.
    /// </remarks>
    public static class DataAssetReserializer
    {
        /// <summary>The folder holding every content asset the game reads.</summary>
        public const string DataRoot = "Assets/_Project/Data";

        /// <summary>Rewrites every asset under <see cref="DataRoot"/>.</summary>
        [MenuItem("ZombieGame/Content/Rewrite Data Assets")]
        public static void Reserialize()
        {
            string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { DataRoot });
            var paths = new List<string>(guids.Length);
            foreach (string guid in guids)
            {
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            }

            if (paths.Count == 0)
            {
                Debug.LogWarning($"[Content] no data assets under {DataRoot}.");
                return;
            }

            AssetDatabase.ForceReserializeAssets(paths);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Content] rewrote {paths.Count} data assets.");
        }
    }
}
