#nullable enable
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ZombieGame.Editor.Importing
{
    /// <summary>
    /// Forces every governed sprite through the importer again.
    /// </summary>
    /// <remarks>
    /// <see cref="PixelArtImporter"/> only runs when an asset is imported, so changing a rule — the
    /// pixels per unit, a pivot, the filtering — leaves everything already in the project on the
    /// old settings. Nothing looks broken either: the sprites still render, just at the wrong
    /// scale, which is far easier to miss than an error.
    /// Run from the menu after touching <see cref="PixelArtRules"/>, or with
    /// <c>-executeMethod ZombieGame.Editor.Importing.PixelArtReimporter.ReimportAll</c>.
    /// </remarks>
    public static class PixelArtReimporter
    {
        /// <summary>Reimports every sprite under the project's sprite root.</summary>
        [MenuItem("ZombieGame/Art/Reimport All Sprites")]
        public static void ReimportAll()
        {
            string root = PixelArtRules.SpriteRoot.TrimEnd('/');
            var paths = new List<string>();

            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { root }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (PixelArtRules.AppliesTo(path))
                {
                    paths.Add(path);
                }
            }

            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (string path in paths)
                {
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.Refresh();
            Debug.Log($"[Sprites] reimported {paths.Count} sprite(s) at " +
                      $"{PixelArtRules.PixelsPerUnit} pixels per unit.");
        }
    }
}
