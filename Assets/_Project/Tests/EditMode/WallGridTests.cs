#nullable enable
using System;
using NUnit.Framework;
using UnityEngine;
using ZombieGame.AI;
using ZombieGame.Core.Physics;

namespace ZombieGame.Tests.EditMode
{
    /// <summary>
    /// Walls, and the horde walking into them.
    /// </summary>
    /// <remarks>
    /// Until this existed the zombies walked through the building, which in an open sandbox was
    /// invisible and in a zone with rooms in it was the first thing anyone noticed.
    /// </remarks>
    [TestFixture]
    public sealed class WallGridTests
    {
        /// <summary>A wall down the line x = 2, three cells tall.</summary>
        private static WallGrid WallAtTwo()
        {
            var grid = new WallGrid(Vector2.zero, 5, 5);
            for (int row = 0; row < 5; row++)
            {
                grid.SetSolid(2, row);
            }

            return grid;
        }

        [Test]
        public void ACellThatWasMarkedIsSolid()
        {
            WallGrid grid = WallAtTwo();

            Assert.That(grid.IsSolid(new Vector2(2.5f, 1.5f)), Is.True);
            Assert.That(grid.IsSolid(new Vector2(1.5f, 1.5f)), Is.False);
        }

        [Test]
        public void TheWorldOutsideTheGridIsOpen()
        {
            WallGrid grid = WallAtTwo();

            // Open rather than solid: a body nudged past the edge has to be able to walk back.
            Assert.That(grid.IsSolid(new Vector2(-40f, 0f)), Is.False);
            Assert.That(grid.IsSolid(new Vector2(0f, 99f)), Is.False);
        }

        [Test]
        public void TheGridCanSitAnywhere()
        {
            var grid = new WallGrid(new Vector2(-10f, -10f), 4, 4);
            grid.SetSolid(0, 0);

            Assert.That(grid.IsSolid(new Vector2(-9.5f, -9.5f)), Is.True);
            Assert.That(grid.IsSolid(new Vector2(-8.5f, -9.5f)), Is.False);
        }

        [Test]
        public void AZoneWithNoGroundIsRefused() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => new WallGrid(Vector2.zero, 0, 4));

        [Test]
        public void AZombieStopsAtAWall()
        {
            WallGrid grid = WallAtTwo();
            var definition = ScriptableObject.CreateInstance<ZombieDefinition>();
            try
            {
                var horde = new ZombieManager(capacity: 4, crowd: CrowdSettings.None);
                horde.Spawn(definition, new Vector2(0.5f, 1.5f));
                horde.SetDestination(0, new Vector2(4.5f, 1.5f));

                for (int i = 0; i < 200; i++)
                {
                    horde.Tick(0.05f, grid);
                }

                Assert.That(
                    horde.Get(0).Position.x,
                    Is.LessThan(2f),
                    "it walked straight through the wall");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void AZombieSlidesAlongAWallInsteadOfSticking()
        {
            WallGrid grid = WallAtTwo();
            var definition = ScriptableObject.CreateInstance<ZombieDefinition>();
            try
            {
                var horde = new ZombieManager(capacity: 4, crowd: CrowdSettings.None);
                horde.Spawn(definition, new Vector2(1.5f, 0.5f));

                // Diagonally into the wall: the part of the step that runs along it has to survive.
                horde.SetDestination(0, new Vector2(4.5f, 4.5f));

                for (int i = 0; i < 100; i++)
                {
                    horde.Tick(0.05f, grid);
                }

                Assert.That(horde.Get(0).Position.y, Is.GreaterThan(2f), "it stuck on the wall");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void WithoutAWallMapNothingHasChanged()
        {
            var definition = ScriptableObject.CreateInstance<ZombieDefinition>();
            try
            {
                var horde = new ZombieManager(capacity: 4, crowd: CrowdSettings.None);
                horde.Spawn(definition, new Vector2(0.5f, 1.5f));
                horde.SetDestination(0, new Vector2(4.5f, 1.5f));

                for (int i = 0; i < 200; i++)
                {
                    horde.Tick(0.05f);
                }

                Assert.That(horde.Get(0).Position.x, Is.EqualTo(4.5f).Within(0.01f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }
    }
}
