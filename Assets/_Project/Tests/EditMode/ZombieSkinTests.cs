#nullable enable
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ZombieGame.AI;
using ZombieGame.Core;

namespace ZombieGame.Tests.EditMode
{
    /// <summary>
    /// Skins: that every kind has at least one look, and that a kind with several hands them out.
    /// </summary>
    /// <remarks>
    /// The failure this guards against is silent. A zombie with no skin draws nothing at all, and
    /// a kind whose variants are never picked looks exactly like a kind that has only one — both
    /// read as "the art is fine" right up until somebody looks at a screenshot.
    /// </remarks>
    [TestFixture]
    public sealed class ZombieSkinTests
    {
        private const string SkinFolder = "Assets/_Project/Data/Zombies/Skins";
        private const string ZombieFolder = "Assets/_Project/Data/Zombies";

        private static string[] EverySkin() => Paths("t:ZombieSkin", SkinFolder, "no skins at all");

        private static string[] EveryZombie() =>
            Paths("t:ZombieDefinition", ZombieFolder, "a game with no zombies in it");

        private static string[] Paths(string filter, string folder, string complaint)
        {
            string[] guids = AssetDatabase.FindAssets(filter, new[] { folder });
            var paths = new string[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                paths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
            }

            Assert.That(paths, Is.Not.Empty, complaint);
            return paths;
        }

        [TestCaseSource(nameof(EverySkin))]
        public void ASkinHasAllSixPoses(string path)
        {
            var skin = AssetDatabase.LoadAssetAtPath<ZombieSkin>(path);
            Assert.That(skin, Is.Not.Null, path);

            foreach (ZombieFacing facing in new[]
                     { ZombieFacing.Front, ZombieFacing.Left, ZombieFacing.Right })
            {
                Assert.That(skin!.SpriteFor(facing), Is.Not.Null, $"{facing}, calm");
                Assert.That(skin.SpriteFor(facing, alerted: true), Is.Not.Null, $"{facing}, alerted");

                // Falling back is allowed, but a skin where every pose fell back to the front view
                // would pass the null checks above while showing one picture forever.
                Assert.That(
                    skin.SpriteFor(facing, alerted: true),
                    Is.Not.EqualTo(skin.SpriteFor(facing)),
                    $"{facing} looks the same whether or not it has noticed you");
            }
        }

        [TestCaseSource(nameof(EveryZombie))]
        public void AKindHasSomethingToWear(string path)
        {
            var zombie = AssetDatabase.LoadAssetAtPath<ZombieDefinition>(path);
            Assert.That(zombie, Is.Not.Null, path);
            Assert.That(zombie!.SkinCount, Is.GreaterThan(0), "a zombie with no skin draws nothing");

            for (int i = 0; i < zombie.SkinCount; i++)
            {
                Assert.That(zombie.Skin(i), Is.Not.Null, $"skin {i} is an empty slot");
                Assert.That(zombie.SpriteFor(i, ZombieFacing.Front), Is.Not.Null, $"skin {i}");
            }
        }

        [TestCaseSource(nameof(EveryZombie))]
        public void TheSkinsOfOneKindAreActuallyDifferent(string path)
        {
            var zombie = AssetDatabase.LoadAssetAtPath<ZombieDefinition>(path);
            Assert.That(zombie, Is.Not.Null, path);

            for (int i = 1; i < zombie!.SkinCount; i++)
            {
                Assert.That(
                    zombie.SpriteFor(i, ZombieFacing.Front),
                    Is.Not.EqualTo(zombie.SpriteFor(0, ZombieFacing.Front)),
                    "two skins showing the same sprite are one skin listed twice");
            }
        }

        [Test]
        public void AnIndexPastTheEndFallsBackInsteadOfDrawingNothing()
        {
            var zombie = AssetDatabase.LoadAssetAtPath<ZombieDefinition>(EveryZombie()[0]);

            // Skins are chosen at spawn and the asset can lose one afterwards. A wrong face beats
            // an invisible zombie.
            Assert.That(zombie.Skin(999), Is.Not.Null);
            Assert.That(zombie.Skin(-1), Is.Not.Null);
        }

        [Test]
        public void EveryZombieOfAKindDoesNotWearTheSameFace()
        {
            ZombieDefinition? several = null;
            foreach (string path in EveryZombie())
            {
                var candidate = AssetDatabase.LoadAssetAtPath<ZombieDefinition>(path);
                if (candidate != null && candidate.SkinCount > 1)
                {
                    several = candidate;
                    break;
                }
            }

            if (several is null)
            {
                Assert.Ignore("no kind has more than one skin yet");
                return;
            }

            var manager = new ZombieManager(capacity: 64, crowd: CrowdSettings.None);
            var seen = new bool[several.SkinCount];
            for (int i = 0; i < 64; i++)
            {
                manager.Spawn(several, new Vector2(i, 0f));
                seen[Mathf.Clamp(manager.Get(i).SkinId, 0, seen.Length - 1)] = true;
            }

            Assert.That(seen, Has.All.True, $"{several.DisplayName} never wore one of its skins");
        }

        [Test]
        public void OneSkinMeansEveryoneWearsIt()
        {
            var kind = ScriptableObject.CreateInstance<ZombieDefinition>();
            try
            {
                var manager = new ZombieManager(capacity: 8, crowd: CrowdSettings.None);
                for (int i = 0; i < 8; i++)
                {
                    manager.Spawn(kind, Vector2.zero);
                    Assert.That(manager.Get(i).SkinId, Is.Zero);
                }
            }
            finally
            {
                Object.DestroyImmediate(kind);
            }
        }

        [Test]
        public void TheProjectHasAWallsLayer()
        {
            // Sight is filtered by this layer. Without it the mask is zero, nothing blocks sight,
            // and the zombies quietly see through every wall in the game.
            Assert.That(
                GameLayers.WallsIndex,
                Is.GreaterThanOrEqualTo(0),
                $"'{GameLayers.Walls}' is missing from the project's layers");
            Assert.That(GameLayers.WallsMask, Is.Not.Zero);
        }

        [Test]
        public void TheProjectHasAHordeLayer()
        {
            // The minimap is told to skip this layer. Without it the mask is zero, the skip
            // skips nothing, and the map lists every zombie in the zone — which quietly undoes
            // the sight cones, the line of sight and the crouching all at once.
            // Both of these guard a project settings file, which an imported package has
            // overwritten once before.
            Assert.That(
                GameLayers.HordeIndex,
                Is.GreaterThanOrEqualTo(0),
                $"'{GameLayers.Horde}' is missing from the project's layers");
            Assert.That(GameLayers.HordeMask, Is.Not.Zero);
        }

        [Test]
        public void AMapShowsEverythingButTheHorde()
        {
            Assert.That(
                GameLayers.EverythingButTheHorde & GameLayers.HordeMask,
                Is.Zero,
                "the horde must not be in what a map may show");

            Assert.That(
                GameLayers.EverythingButTheHorde & GameLayers.WallsMask,
                Is.Not.Zero,
                "but the walls must be, or the map is blank");
        }
    }
}
