#nullable enable
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using ZombieGame.Core.Rendering;

namespace ZombieGame.Editor.Scenes
{
    /// <summary>
    /// The one tilemap in a zone that a rebuild is not allowed to throw away.
    /// </summary>
    /// <remarks>
    /// A zone is built from a drawn map by a function, which is what makes it reviewable and
    /// reproducible — and which also means the builder starts from an empty scene and saves over
    /// whatever was there. Anything painted by hand in the editor would be gone the next time
    /// somebody ran the build, which is a trap rather than a workflow.
    /// So one layer is carried across: its tiles are read out of the old scene before it is
    /// discarded and painted back into the new one afterwards. Everything else in the zone still
    /// comes from <see cref="ZoneMaps"/>, so the two ways of making a map can be used together
    /// without either one eating the other.
    /// The kept layer sits above the generated ground and below the actors, and carries no
    /// collision: a hand-painted wall that stopped people would have to be in the wall grid the
    /// horde reads, and that is baked from the drawn map.
    /// </remarks>
    public sealed class HandPaintedLayer
    {
        /// <summary>What the preserved tilemap is called in a zone.</summary>
        public const string ObjectName = "Tilemap_Handmade";

        private readonly List<Vector3Int> _cells = new();
        private readonly List<TileBase> _tiles = new();

        /// <summary>How many tiles were carried over.</summary>
        public int Count => _cells.Count;

        /// <summary>
        /// Reads the hand-painted layer out of the scene currently saved at
        /// <paramref name="assetPath"/>, if there is one.
        /// </summary>
        /// <remarks>
        /// Opened additively and closed again, so the editor's current scene is left alone — a
        /// build that quietly swapped what the user was looking at would be its own kind of
        /// nasty surprise.
        /// </remarks>
        public static HandPaintedLayer Lift(string assetPath)
        {
            var kept = new HandPaintedLayer();
            if (!File.Exists(assetPath))
            {
                return kept;
            }

            // The zone may already be open — somebody rebuilding the scene they are looking at
            // is the normal case. Opening it again additively hands back a scene with nothing in
            // it, and the painted tiles then look as though they were never there.
            Scene already = SceneManager.GetSceneByPath(assetPath);
            bool wasOpen = already.IsValid() && already.isLoaded;

            Scene existing = wasOpen
                ? already
                : EditorSceneManager.OpenScene(assetPath, OpenSceneMode.Additive);
            try
            {
                Tilemap? painted = Find(existing);
                if (painted is null)
                {
                    return kept;
                }

                painted.CompressBounds();
                foreach (Vector3Int cell in painted.cellBounds.allPositionsWithin)
                {
                    TileBase? tile = painted.GetTile(cell);
                    if (tile is null)
                    {
                        continue;
                    }

                    kept._cells.Add(cell);
                    kept._tiles.Add(tile);
                }

                return kept;
            }
            finally
            {
                // Only what this method opened. Closing a scene somebody else had open would
                // make a rebuild quietly swap what the editor is showing.
                if (!wasOpen)
                {
                    EditorSceneManager.CloseScene(existing, removeScene: true);
                }
            }
        }

        /// <summary>Paints the kept tiles back into a freshly built zone.</summary>
        public void LayInto(Scene scene)
        {
            if (_cells.Count == 0)
            {
                return;
            }

            Tilemap target = Find(scene) ?? Create(scene);
            for (int i = 0; i < _cells.Count; i++)
            {
                target.SetTile(_cells[i], _tiles[i]);
            }
        }

        /// <summary>
        /// Makes the empty layer, so there is something to paint into the first time somebody
        /// opens a zone.
        /// </summary>
        public static Tilemap Create(Scene scene)
        {
            Transform grid = GridOf(scene);
            var layer = new GameObject(ObjectName);
            layer.transform.SetParent(grid, worldPositionStays: false);

            Tilemap map = layer.AddComponent<Tilemap>();
            TilemapRenderer renderer = layer.AddComponent<TilemapRenderer>();

            // Above the generated ground and its markings, below the walls, so a hand-painted
            // path or a patch of rubble lies on the street rather than over the buildings.
            renderer.sortingOrder = SortingBands.Markings;
            return map;
        }

        private static Transform GridOf(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                var grid = root.GetComponent<Grid>();
                if (grid != null)
                {
                    return grid.transform;
                }
            }

            GameObject made = SceneBuilder.CreateObject(scene, "Grid");
            Grid created = made.AddComponent<Grid>();
            created.cellSize = Vector3.one;
            made.transform.position = new Vector3(-0.5f, -0.5f, 0f);
            return made.transform;
        }

        private static Tilemap? Find(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Tilemap map in root.GetComponentsInChildren<Tilemap>(includeInactive: true))
                {
                    if (map.gameObject.name == ObjectName)
                    {
                        return map;
                    }
                }
            }

            return null;
        }
    }
}
