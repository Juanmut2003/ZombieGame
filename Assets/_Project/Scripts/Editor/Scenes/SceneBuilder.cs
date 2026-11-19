#nullable enable
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ZombieGame.Editor.Scenes
{
    /// <summary>
    /// Builds scenes from code instead of by hand in the editor.
    /// </summary>
    /// <remarks>
    /// Scenes are the worst thing in a Unity repository to review or merge: a one-line change
    /// shows up as a wall of YAML, and two people editing one scene means a conflict nobody can
    /// resolve. A scene that is produced by a function is reviewable as that function, can be
    /// rebuilt identically at any time, and makes the conflict rule in CLAUDE.md cheap to follow.
    /// </remarks>
    public static class SceneBuilder
    {
        /// <summary>
        /// Creates an empty scene, lets <paramref name="populate"/> fill it, and saves it to
        /// <paramref name="assetPath"/>, replacing whatever was there.
        /// </summary>
        /// <param name="assetPath">Target path, for example <see cref="ProjectScenes.Sandbox"/>.</param>
        /// <param name="populate">Adds the scene's contents. Objects it creates must be rooted in the scene.</param>
        /// <returns>The saved scene.</returns>
        /// <exception cref="InvalidOperationException">Saving failed.</exception>
        public static Scene Build(string assetPath, Action<Scene> populate)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                throw new ArgumentException("A scene path is required.", nameof(assetPath));
            }

            if (populate is null)
            {
                throw new ArgumentNullException(nameof(populate));
            }

            string? directory = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory!);
            }

            // Taken out of the old scene before it is thrown away, and put back afterwards.
            HandPaintedLayer kept = HandPaintedLayer.Lift(assetPath);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            populate(scene);
            kept.LayInto(scene);

            if (!EditorSceneManager.SaveScene(scene, assetPath))
            {
                throw new InvalidOperationException($"Could not save the scene to '{assetPath}'.");
            }

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            return scene;
        }

        /// <summary>
        /// Creates a GameObject already parented into <paramref name="scene"/>.
        /// </summary>
        /// <remarks>
        /// Plain <c>new GameObject()</c> lands in whatever scene happens to be active, which during
        /// a batch build is not necessarily the one being written.
        /// </remarks>
        public static GameObject CreateObject(Scene scene, string name, Transform? parent = null)
        {
            var created = new GameObject(name);
            if (parent is null)
            {
                SceneManager.MoveGameObjectToScene(created, scene);
            }
            else
            {
                created.transform.SetParent(parent, worldPositionStays: false);
            }

            return created;
        }
    }
}
