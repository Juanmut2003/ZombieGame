#nullable enable
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using ZombieGame.Editor.Scenes;

namespace ZombieGame.Tests.EditMode
{
    /// <summary>
    /// That every zone has a layer to paint into by hand.
    /// </summary>
    /// <remarks>
    /// A zone is generated from a drawn map, so the builder starts from an empty scene and saves
    /// over whatever was there. <see cref="HandPaintedLayer"/> carries one tilemap across that,
    /// which is what makes painting into a zone a workflow rather than a trap.
    /// What is checked here is that the layer exists in a freshly built zone. Whether its tiles
    /// survive a rebuild is <b>not</b> covered, and not for want of trying: a tile written into a
    /// loaded scene from a batch-mode script does not stick, so the test could only ever have
    /// proved something about batch mode. It has to be checked by hand in the editor — paint a
    /// tile, run the build, look.
    /// </remarks>
    [TestFixture]
    public sealed class HandPaintedLayerTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/HandPaintedLayerTest.unity";

        [TearDown]
        public void RemoveTheScene()
        {
            if (File.Exists(ScenePath))
            {
                AssetDatabase.DeleteAsset(ScenePath);
            }
        }

        /// <summary>A zone with nothing in it but a grid and the layer under test.</summary>
        private static void BuildBare() =>
            SceneBuilder.Build(ScenePath, scene =>
            {
                GameObject gridObject = SceneBuilder.CreateObject(scene, "Grid");
                Grid grid = gridObject.AddComponent<Grid>();
                grid.cellSize = Vector3.one;
                gridObject.transform.position = new Vector3(-0.5f, -0.5f, 0f);

                HandPaintedLayer.Create(scene);
            });

        [Test]
        public void AFreshZoneHasSomewhereToPaint()
        {
            BuildBare();

            Tilemap? found = null;
            foreach (GameObject root in
                     UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (Tilemap map in root.GetComponentsInChildren<Tilemap>(true))
                {
                    if (map.gameObject.name == HandPaintedLayer.ObjectName)
                    {
                        found = map;
                    }
                }
            }

            Assert.That(found, Is.Not.Null, $"no '{HandPaintedLayer.ObjectName}' to paint into");
        }

        [Test]
        public void TheLayerDrawsAboveTheGroundAndBelowTheWalls()
        {
            // Where it sits is the difference between a hand-painted path lying on the street
            // and one lying over the buildings.
            BuildBare();

            foreach (GameObject root in
                     UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (TilemapRenderer renderer in
                         root.GetComponentsInChildren<TilemapRenderer>(true))
                {
                    if (renderer.gameObject.name != HandPaintedLayer.ObjectName)
                    {
                        continue;
                    }

                    Assert.That(
                        renderer.sortingOrder,
                        Is.EqualTo(ZombieGame.Core.Rendering.SortingBands.Markings));
                    return;
                }
            }

            Assert.Fail("the layer has no renderer");
        }
    }
}
