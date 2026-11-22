#nullable enable
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ZombieGame.Base;
using ZombieGame.Economy;

namespace ZombieGame.Tests.EditMode
{
    /// <summary>
    /// The wheel that opens on a building: what it offers, and what it refuses.
    /// </summary>
    /// <remarks>
    /// Every one of these is something the player reads off four small labels and then acts on.
    /// A slice that says a price the colony cannot meet, or that greys out an upgrade there is
    /// room and money for, is worse than no menu — it teaches the wrong thing about the base
    /// and is invisible in a screenshot.
    /// </remarks>
    [TestFixture]
    public sealed class BuildingMenuTests
    {
        private const float HalfBack = 0.5f;

        /// <summary>A building with as many rungs as there are footprints given.</summary>
        private static BuildingDefinition Building(
            int posts,
            params (int Width, int Height, int Upgrade)[] rungs)
        {
            var building = ScriptableObject.CreateInstance<BuildingDefinition>();
            var serialized = new UnityEditor.SerializedObject(building);
            serialized.FindProperty("_displayName").stringValue = "Shed";

            UnityEditor.SerializedProperty cost = serialized.FindProperty("_cost");
            cost.arraySize = 1;
            cost.GetArrayElementAtIndex(0).FindPropertyRelative("_kind").enumValueIndex =
                (int)ResourceKind.Materials;
            cost.GetArrayElementAtIndex(0).FindPropertyRelative("_amount").intValue = 40;

            UnityEditor.SerializedProperty levels = serialized.FindProperty("_levels");
            levels.arraySize = rungs.Length;
            for (int i = 0; i < rungs.Length; i++)
            {
                UnityEditor.SerializedProperty rung = levels.GetArrayElementAtIndex(i);
                rung.FindPropertyRelative("_width").intValue = rungs[i].Width;
                rung.FindPropertyRelative("_height").intValue = rungs[i].Height;
                rung.FindPropertyRelative("_workPosts").intValue = posts;

                UnityEditor.SerializedProperty price =
                    rung.FindPropertyRelative("_upgradeCost");
                price.arraySize = rungs[i].Upgrade > 0 ? 1 : 0;
                if (rungs[i].Upgrade > 0)
                {
                    UnityEditor.SerializedProperty line = price.GetArrayElementAtIndex(0);
                    line.FindPropertyRelative("_kind").enumValueIndex =
                        (int)ResourceKind.Materials;
                    line.FindPropertyRelative("_amount").intValue = rungs[i].Upgrade;
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return building;
        }

        private static (BuildSite Site, int Id) Plot(
            BuildingDefinition what, int width = 2, int height = 2, int siteSize = 10)
        {
            var site = new BuildSite(Vector2.zero, siteSize, siteSize);
            site.TryPlace(new BuildingFootprint(width, height), Vector2Int.zero, what, out int id);
            return (site, id);
        }

        private static BuildingChoice SliceOf(
            IReadOnlyList<BuildingChoice> choices, BuildingOption option)
        {
            foreach (BuildingChoice choice in choices)
            {
                if (choice.Option == option)
                {
                    return choice;
                }
            }

            Assert.Fail($"the wheel had no {option} slice");
            return default;
        }

        [Test]
        public void TheWheelAlwaysHasTheSameFourSlices()
        {
            (BuildSite site, int id) = Plot(Building(0, (2, 2, 0)));

            IReadOnlyList<BuildingChoice> choices =
                BuildingMenu.Describe(site, id, null, null, HalfBack);

            Assert.That(choices.Count, Is.EqualTo(BuildingMenu.Slices));
            Assert.That(choices[0].Option, Is.EqualTo(BuildingOption.Improve));
            Assert.That(choices[1].Option, Is.EqualTo(BuildingOption.PutToWork));
            Assert.That(choices[2].Option, Is.EqualTo(BuildingOption.SendHome));
            Assert.That(choices[3].Option, Is.EqualTo(BuildingOption.TearDown));
        }

        [Test]
        public void NothingOpensOnABuildingThatIsNotThere()
        {
            var site = new BuildSite(Vector2.zero, 10, 10);

            Assert.That(BuildingMenu.Describe(site, 17, null, null, HalfBack), Is.Empty);
            Assert.That(BuildingMenu.Headline(site, 17), Is.Empty);
        }

        [Test]
        public void ABuildingAtItsBestCannotBeImproved()
        {
            (BuildSite site, int id) = Plot(Building(0, (2, 2, 0)));

            BuildingChoice improve = SliceOf(
                BuildingMenu.Describe(site, id, null, null, HalfBack), BuildingOption.Improve);

            Assert.That(improve.Allowed, Is.False);
            Assert.That(improve.Detail, Is.EqualTo("AT ITS BEST"));
        }

        [Test]
        public void AnUpgradeTheColonyCanPayForSaysWhatItCosts()
        {
            (BuildSite site, int id) = Plot(Building(0, (2, 2, 0), (3, 3, 55)));
            var stores = new Stockpile();
            stores.Set(ResourceKind.Materials, 100);

            BuildingChoice improve = SliceOf(
                BuildingMenu.Describe(site, id, null, stores, HalfBack), BuildingOption.Improve);

            Assert.That(improve.Allowed, Is.True);
            Assert.That(improve.Detail, Is.EqualTo("55 MAT"));
        }

        [Test]
        public void AnUpgradeTheColonyCannotPayForIsStillPriced()
        {
            (BuildSite site, int id) = Plot(Building(0, (2, 2, 0), (3, 3, 55)));
            var stores = new Stockpile();
            stores.Set(ResourceKind.Materials, 10);

            BuildingChoice improve = SliceOf(
                BuildingMenu.Describe(site, id, null, stores, HalfBack), BuildingOption.Improve);

            // Greyed out, but the price stays on it. A slice that goes blank tells the player
            // the building has run out of levels, which is a different problem with a different
            // answer.
            Assert.That(improve.Allowed, Is.False);
            Assert.That(improve.Detail, Is.EqualTo("55 MAT"));
        }

        [Test]
        public void AnUpgradeWithNowhereToGrowSaysSo()
        {
            // The plot is exactly as big as the building, so there is no ground to grow onto.
            (BuildSite site, int id) = Plot(
                Building(0, (2, 2, 0), (3, 3, 10)), siteSize: 2);
            var stores = new Stockpile();
            stores.Set(ResourceKind.Materials, 1000);

            BuildingChoice improve = SliceOf(
                BuildingMenu.Describe(site, id, null, stores, HalfBack), BuildingOption.Improve);

            Assert.That(improve.Allowed, Is.False);
            Assert.That(improve.Detail, Is.EqualTo("OFF THE PLOT"));
        }

        [Test]
        public void SomewhereWithNoJobsDoesNotOfferWork()
        {
            (BuildSite site, int id) = Plot(Building(0, (2, 2, 0)));
            var colony = new Colony(site);
            colony.AddFounder("Rae");

            BuildingChoice work = SliceOf(
                BuildingMenu.Describe(site, id, colony, null, HalfBack),
                BuildingOption.PutToWork);

            Assert.That(work.Allowed, Is.False);
            Assert.That(work.Detail, Is.EqualTo("NO JOBS HERE"));
        }

        [Test]
        public void AnEmptyPostWithSomebodyFreeCanBeFilled()
        {
            (BuildSite site, int id) = Plot(Building(2, (2, 2, 0)));
            var colony = new Colony(site);
            colony.AddFounder("Rae");

            BuildingChoice work = SliceOf(
                BuildingMenu.Describe(site, id, colony, null, HalfBack),
                BuildingOption.PutToWork);

            Assert.That(work.Allowed, Is.True);
            Assert.That(work.Detail, Is.EqualTo("0 / 2"));
        }

        [Test]
        public void AFullBuildingTakesNobodyElse()
        {
            (BuildSite site, int id) = Plot(Building(1, (2, 2, 0)));
            var colony = new Colony(site);
            Survivor rae = colony.AddFounder("Rae");
            colony.AddFounder("Osei");
            colony.Assign(rae.Id, id);

            IReadOnlyList<BuildingChoice> choices =
                BuildingMenu.Describe(site, id, colony, null, HalfBack);

            Assert.That(SliceOf(choices, BuildingOption.PutToWork).Allowed, Is.False);
            Assert.That(SliceOf(choices, BuildingOption.PutToWork).Detail, Is.EqualTo("1 / 1"));
            Assert.That(SliceOf(choices, BuildingOption.SendHome).Allowed, Is.True);
            Assert.That(SliceOf(choices, BuildingOption.SendHome).Detail, Is.EqualTo("1 HERE"));
        }

        [Test]
        public void NobodyCanBeSentHomeFromAnEmptyBuilding()
        {
            (BuildSite site, int id) = Plot(Building(1, (2, 2, 0)));
            var colony = new Colony(site);

            BuildingChoice home = SliceOf(
                BuildingMenu.Describe(site, id, colony, null, HalfBack),
                BuildingOption.SendHome);

            Assert.That(home.Allowed, Is.False);
            Assert.That(home.Detail, Is.EqualTo("0 HERE"));
        }

        [Test]
        public void TearingDownSaysWhatComesBack()
        {
            (BuildSite site, int id) = Plot(Building(0, (2, 2, 0)));

            BuildingChoice down = SliceOf(
                BuildingMenu.Describe(site, id, null, null, HalfBack), BuildingOption.TearDown);

            // Half of the forty it cost, and it can always be done.
            Assert.That(down.Allowed, Is.True);
            Assert.That(down.Detail, Is.EqualTo("+20 MAT"));
        }

        [Test]
        public void ThePlateNamesTheBuildingAndWhereItIsOnItsLadder()
        {
            (BuildSite site, int id) = Plot(Building(0, (2, 2, 0), (3, 3, 10)));

            Assert.That(BuildingMenu.Headline(site, id), Is.EqualTo("SHED   1 / 2"));
        }
    }
}
