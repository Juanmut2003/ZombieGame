#nullable enable
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ZombieGame.Base;
using ZombieGame.Economy;

namespace ZombieGame.Tests.EditMode
{
    /// <summary>
    /// The grid: who gets power, who goes dark, and what a dark building costs the colony.
    /// </summary>
    /// <remarks>
    /// Power is the first system where the colony can fail quietly. A farm with no power is a
    /// farm that looks exactly like a working one and grows nothing, and a generator that browns
    /// itself out takes the base down with it and can never come back.
    /// </remarks>
    [TestFixture]
    public sealed class PowerGridTests
    {
        private static BuildingDefinition Building(
            int draw = 0,
            int supply = 0,
            int posts = 0,
            int foodPerWorker = 0,
            int fuelPerDay = 0,
            int waterPerDay = 0)
        {
            var building = ScriptableObject.CreateInstance<BuildingDefinition>();
            var serialized = new UnityEditor.SerializedObject(building);

            UnityEditor.SerializedProperty levels = serialized.FindProperty("_levels");
            levels.arraySize = 1;
            UnityEditor.SerializedProperty rung = levels.GetArrayElementAtIndex(0);
            rung.FindPropertyRelative("_width").intValue = 2;
            rung.FindPropertyRelative("_height").intValue = 2;
            rung.FindPropertyRelative("_workPosts").intValue = posts;
            rung.FindPropertyRelative("_powerDraw").intValue = draw;
            rung.FindPropertyRelative("_powerSupply").intValue = supply;
            rung.FindPropertyRelative("_fuelPerDay").intValue = fuelPerDay;
            rung.FindPropertyRelative("_waterPerDay").intValue = waterPerDay;

            UnityEditor.SerializedProperty yield = rung.FindPropertyRelative("_yieldPerWorker");
            yield.arraySize = foodPerWorker > 0 ? 1 : 0;
            if (foodPerWorker > 0)
            {
                UnityEditor.SerializedProperty line = yield.GetArrayElementAtIndex(0);
                line.FindPropertyRelative("_kind").enumValueIndex = (int)ResourceKind.Food;
                line.FindPropertyRelative("_amount").intValue = foodPerWorker;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return building;
        }

        private static BuildSite Site() => new(Vector2.zero, 30, 30);

        private static int Place(BuildSite site, BuildingDefinition what, int column)
        {
            site.TryPlace(new BuildingFootprint(2, 2), new Vector2Int(column, 0), what, out int id);
            return id;
        }

        [Test]
        public void AnEmptyPlotNeedsNothing()
        {
            var running = new HashSet<int>();
            PowerReport report = PowerGrid.Resolve(Site(), running);

            Assert.That(report.Supply, Is.Zero);
            Assert.That(report.Wanted, Is.Zero);
            Assert.That(report.Lit, Is.True);
            Assert.That(running, Is.Empty);
        }

        [Test]
        public void SomethingThatNeedsNoPowerIsNeverDark()
        {
            BuildSite site = Site();
            int barracks = Place(site, Building(), 0);

            var running = new HashSet<int>();
            PowerGrid.Resolve(site, running);

            Assert.That(running, Does.Contain(barracks));
        }

        [Test]
        public void AGeneratorRunsItself()
        {
            BuildSite site = Site();
            int generator = Place(site, Building(supply: 12, fuelPerDay: 3), 0);

            var running = new HashSet<int>();
            PowerReport report = PowerGrid.Resolve(site, running);

            // A generator that browned itself out would take the grid down and never come back.
            Assert.That(running, Does.Contain(generator));
            Assert.That(report.Supply, Is.EqualTo(12));
            Assert.That(report.Wanted, Is.Zero, "a generator must not draw on its own supply");
        }

        [Test]
        public void EnoughPowerLightsEverything()
        {
            BuildSite site = Site();
            Place(site, Building(supply: 12), 0);
            int shop = Place(site, Building(draw: 4), 3);
            int armory = Place(site, Building(draw: 2), 6);

            var running = new HashSet<int>();
            PowerReport report = PowerGrid.Resolve(site, running);

            Assert.That(running, Does.Contain(shop));
            Assert.That(running, Does.Contain(armory));
            Assert.That(report.Wanted, Is.EqualTo(6));
            Assert.That(report.Met, Is.EqualTo(6));
            Assert.That(report.Lit, Is.True);
        }

        [Test]
        public void TheNewestThingIsTheOneThatGoesDark()
        {
            BuildSite site = Site();
            Place(site, Building(supply: 5), 0);
            int first = Place(site, Building(draw: 4), 3);
            int second = Place(site, Building(draw: 4), 6);

            var running = new HashSet<int>();
            PowerReport report = PowerGrid.Resolve(site, running);

            // Deliberately not random: the player has to be able to reason about which thing
            // tripped, and a test has to be able to say what happened.
            Assert.That(running, Does.Contain(first));
            Assert.That(running, Has.No.Member(second));
            Assert.That(report.Shortfall, Is.EqualTo(4));
            Assert.That(report.Lit, Is.False);
        }

        [Test]
        public void NoGeneratorMeansNothingThatNeedsPowerRuns()
        {
            BuildSite site = Site();
            int shop = Place(site, Building(draw: 4), 0);

            var running = new HashSet<int>();
            PowerReport report = PowerGrid.Resolve(site, running);

            Assert.That(running, Has.No.Member(shop));
            Assert.That(report.Shortfall, Is.EqualTo(4));
        }

        [Test]
        public void ADarkBuildingGrowsNothingAndItsWorkersStandAbout()
        {
            BuildSite site = Site();
            var colony = new Colony(site);
            var stores = new Stockpile();

            int farm = Place(site, Building(draw: 4, posts: 1, foodPerWorker: 6), 0);
            Survivor rae = colony.AddFounder("Rae");
            colony.Assign(rae.Id, farm);

            // No generator anywhere.
            DayReport report = ColonyDay.Resolve(colony, site, stores, foodPerPerson: 0);

            Assert.That(report.FoodGrown, Is.Zero, "a dark farm grew a crop");
            Assert.That(report.Worked, Is.Zero);
            Assert.That(report.Idle, Is.EqualTo(1), "somebody was counted as working in the dark");
            Assert.That(report.Darkened, Is.EqualTo(1));
        }

        [Test]
        public void AGeneratorBurnsItsFuelForBeingOn()
        {
            BuildSite site = Site();
            var colony = new Colony(site);
            var stores = new Stockpile();
            stores.Set(ResourceKind.Fuel, 10);

            Place(site, Building(supply: 12, fuelPerDay: 3), 0);
            ColonyDay.Resolve(colony, site, stores, foodPerPerson: 0);

            // Nothing was plugged into it. It burnt the diesel anyway, which is the decision the
            // player gets to regret.
            Assert.That(stores.AmountOf(ResourceKind.Fuel), Is.EqualTo(7));
        }

        [Test]
        public void AFarmDrinksWhileItWorks()
        {
            BuildSite site = Site();
            var colony = new Colony(site);
            var stores = new Stockpile();
            stores.Set(ResourceKind.Water, 10);

            int farm = Place(site, Building(posts: 2, foodPerWorker: 6, waterPerDay: 4), 0);
            Survivor rae = colony.AddFounder("Rae");
            Survivor osei = colony.AddFounder("Osei");
            colony.Assign(rae.Id, farm);
            colony.Assign(osei.Id, farm);

            DayReport report = ColonyDay.Resolve(colony, site, stores, foodPerPerson: 0);

            // The beds want the same soaking whether one person or two turned up.
            Assert.That(stores.AmountOf(ResourceKind.Water), Is.EqualTo(6));
            Assert.That(report.FoodGrown, Is.EqualTo(12));
        }

        [Test]
        public void AFarmWithNoWaterGrowsNothing()
        {
            BuildSite site = Site();
            var colony = new Colony(site);
            var stores = new Stockpile();

            int farm = Place(site, Building(posts: 1, foodPerWorker: 6, waterPerDay: 4), 0);
            Survivor rae = colony.AddFounder("Rae");
            colony.Assign(rae.Id, farm);

            DayReport report = ColonyDay.Resolve(colony, site, stores, foodPerPerson: 0);

            Assert.That(report.FoodGrown, Is.Zero);
            Assert.That(report.Idle, Is.EqualTo(1));
        }

        [Test]
        public void PeopleDrinkAndSaySoWhenTheyCannot()
        {
            BuildSite site = Site();
            var colony = new Colony(site);
            var stores = new Stockpile();
            stores.Set(ResourceKind.Water, 2);

            for (int i = 0; i < 4; i++)
            {
                colony.AddFounder($"Survivor {i}");
            }

            DayReport report = ColonyDay.Resolve(
                colony, site, stores, foodPerPerson: 0, waterPerPerson: 1);

            Assert.That(stores.AmountOf(ResourceKind.Water), Is.Zero);
            Assert.That(report.WentThirsty, Is.EqualTo(2));
            Assert.That(report.Fed, Is.False, "a thirsty colony counts as a bad day");
        }

        [Test]
        public void AWellFillsTheTanksBeforeAnybodyDrinks()
        {
            BuildSite site = Site();
            var colony = new Colony(site);
            var stores = new Stockpile();

            int well = Place(site, Building(posts: 1), 0);
            var serialized = new UnityEditor.SerializedObject(site.Buildings[0].Definition);
            UnityEditor.SerializedProperty yield = serialized
                .FindProperty("_levels").GetArrayElementAtIndex(0)
                .FindPropertyRelative("_yieldPerWorker");
            yield.arraySize = 1;
            yield.GetArrayElementAtIndex(0).FindPropertyRelative("_kind").enumValueIndex =
                (int)ResourceKind.Water;
            yield.GetArrayElementAtIndex(0).FindPropertyRelative("_amount").intValue = 10;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Survivor rae = colony.AddFounder("Rae");
            colony.Assign(rae.Id, well);

            // Nothing in the tanks at dawn. The day's pumping has to be in before the drinking.
            DayReport report = ColonyDay.Resolve(
                colony, site, stores, foodPerPerson: 0, waterPerPerson: 1);

            Assert.That(report.WentThirsty, Is.Zero);
            Assert.That(stores.AmountOf(ResourceKind.Water), Is.EqualTo(9));
        }
    }
}
