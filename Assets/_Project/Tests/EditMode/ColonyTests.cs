#nullable enable
using NUnit.Framework;
using UnityEngine;
using ZombieGame.Base;
using ZombieGame.Economy;

namespace ZombieGame.Tests.EditMode
{
    /// <summary>
    /// The people at the base: where they sleep, where they work, and what a day costs.
    /// </summary>
    /// <remarks>
    /// All of it invisible until it has been wrong for a week of game time. A colonist still
    /// working a demolished building, a farm fed by nobody that produces anyway, a larder that
    /// refuses to feed four people because it cannot feed five — none of these announce
    /// themselves, they just make the colony quietly wrong.
    /// </remarks>
    [TestFixture]
    public sealed class ColonyTests
    {
        private static BuildingDefinition Building(
            int beds = 0, int posts = 0, int foodPerWorker = 0)
        {
            var building = ScriptableObject.CreateInstance<BuildingDefinition>();
            var serialized = new UnityEditor.SerializedObject(building);

            UnityEditor.SerializedProperty levels = serialized.FindProperty("_levels");
            levels.arraySize = 1;
            UnityEditor.SerializedProperty rung = levels.GetArrayElementAtIndex(0);
            rung.FindPropertyRelative("_beds").intValue = beds;
            rung.FindPropertyRelative("_workPosts").intValue = posts;
            rung.FindPropertyRelative("_width").intValue = 3;
            rung.FindPropertyRelative("_height").intValue = 5;

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

        private static BuildSite Site() => new(Vector2.zero, 20, 20);

        [Test]
        public void ABaseWithNoBarracksHousesNobody()
        {
            var colony = new Colony(Site());

            Assert.That(colony.Capacity, Is.Zero);
            Assert.That(colony.HasSpareBed, Is.False);
            Assert.That(colony.TryAdd("Rae"), Is.Null, "somebody moved into a field");
        }

        [Test]
        public void BedsComeFromTheBarracksAndNowhereElse()
        {
            BuildSite site = Site();
            var colony = new Colony(site);

            BuildingDefinition barracks = Building(beds: 2);
            BuildingDefinition farm = Building(posts: 1, foodPerWorker: 6);

            site.TryPlace(new BuildingFootprint(3, 5), new Vector2Int(0, 0), farm, out int _);
            Assert.That(colony.Capacity, Is.Zero, "a farm is not somewhere to sleep");

            site.TryPlace(new BuildingFootprint(3, 5), new Vector2Int(4, 0), barracks, out int _);
            Assert.That(colony.Capacity, Is.EqualTo(2));

            // A second barracks adds its own beds. The cap is the sum, not the best one.
            site.TryPlace(new BuildingFootprint(3, 5), new Vector2Int(8, 0), barracks, out int _);
            Assert.That(colony.Capacity, Is.EqualTo(4));
        }

        [Test]
        public void TheFoundersArriveWhetherOrNotThereIsABed()
        {
            var colony = new Colony(Site());
            colony.AddFounder("Rae");
            colony.AddFounder("Osei");

            // Refusing them would leave a base nobody can start.
            Assert.That(colony.Count, Is.EqualTo(2));
            Assert.That(colony.Homeless, Is.EqualTo(2), "nobody noticed they are sleeping outside");
        }

        [Test]
        public void ABarracksTakesThePeopleSleepingRoughIndoors()
        {
            BuildSite site = Site();
            var colony = new Colony(site);
            colony.AddFounder("Rae");
            colony.AddFounder("Osei");

            site.TryPlace(
                new BuildingFootprint(3, 5), new Vector2Int(0, 0), Building(beds: 2), out int _);

            Assert.That(colony.Homeless, Is.Zero);
            Assert.That(colony.HasSpareBed, Is.False, "the barracks is full");
        }

        [Test]
        public void NobodyMovesIntoAFullBase()
        {
            BuildSite site = Site();
            var colony = new Colony(site);
            site.TryPlace(
                new BuildingFootprint(3, 5), new Vector2Int(0, 0), Building(beds: 1), out int _);

            Assert.That(colony.TryAdd("Rae"), Is.Not.Null);
            Assert.That(colony.TryAdd("Osei"), Is.Null, "two people in one bed");
            Assert.That(colony.Count, Is.EqualTo(1));
        }

        [Test]
        public void AJobHasAsManyPostsAsTheBuildingSays()
        {
            BuildSite site = Site();
            var colony = new Colony(site);
            site.TryPlace(
                new BuildingFootprint(3, 5), new Vector2Int(0, 0),
                Building(posts: 1, foodPerWorker: 6), out int farm);

            Survivor rae = colony.AddFounder("Rae");
            Survivor osei = colony.AddFounder("Osei");

            Assert.That(colony.Assign(rae.Id, farm), Is.True);
            Assert.That(colony.Assign(osei.Id, farm), Is.False, "two people on a one-post farm");
            Assert.That(colony.WorkersAt(farm), Is.EqualTo(1));
            Assert.That(osei.IsIdle, Is.True);
        }

        [Test]
        public void AssigningSomebodyWhereTheyAlreadyAreChangesNothing()
        {
            BuildSite site = Site();
            var colony = new Colony(site);
            site.TryPlace(
                new BuildingFootprint(3, 5), new Vector2Int(0, 0), Building(posts: 1), out int farm);

            Survivor rae = colony.AddFounder("Rae");
            Assert.That(colony.Assign(rae.Id, farm), Is.True);

            // Must not be read as a second worker arriving and refused for want of a post.
            Assert.That(colony.Assign(rae.Id, farm), Is.True);
            Assert.That(colony.WorkersAt(farm), Is.EqualTo(1));
        }

        [Test]
        public void NobodyWorksInABuildingThatIsNotThere()
        {
            var colony = new Colony(Site());
            Survivor rae = colony.AddFounder("Rae");

            Assert.That(colony.Assign(rae.Id, 99), Is.False);
            Assert.That(colony.Assign(rae.Id, 0), Is.False, "zero means no job, not a job");
            Assert.That(rae.IsIdle, Is.True);
        }

        [Test]
        public void TearingABuildingDownSendsItsWorkersHome()
        {
            BuildSite site = Site();
            var colony = new Colony(site);
            site.TryPlace(
                new BuildingFootprint(3, 5), new Vector2Int(0, 0),
                Building(posts: 2, foodPerWorker: 6), out int farm);

            Survivor rae = colony.AddFounder("Rae");
            colony.Assign(rae.Id, farm);
            site.Remove(farm);

            // Until they are sent home they hold the number of a building that is gone, and
            // count against the posts of whatever is given that number next.
            Assert.That(colony.SendHomeFromRubble(), Is.EqualTo(1));
            Assert.That(rae.IsIdle, Is.True);
            Assert.That(colony.SendHomeFromRubble(), Is.Zero, "sent home twice");
        }

        [Test]
        public void ADayOfFarmingFeedsMoreThanTheFarmer()
        {
            BuildSite site = Site();
            var colony = new Colony(site);
            var stores = new Stockpile();

            site.TryPlace(
                new BuildingFootprint(3, 5), new Vector2Int(0, 0),
                Building(posts: 1, foodPerWorker: 6), out int farm);

            Survivor rae = colony.AddFounder("Rae");
            colony.AddFounder("Osei");
            colony.Assign(rae.Id, farm);

            DayReport report = ColonyDay.Resolve(colony, site, stores, foodPerPerson: 2);

            Assert.That(report.Worked, Is.EqualTo(1));
            Assert.That(report.Idle, Is.EqualTo(1));
            Assert.That(report.FoodGrown, Is.EqualTo(6));
            Assert.That(report.FoodEaten, Is.EqualTo(4));
            Assert.That(report.Fed, Is.True);
            Assert.That(stores.AmountOf(ResourceKind.Food), Is.EqualTo(2));
        }

        [Test]
        public void TheDaysWorkIsInBeforeTheDaysMeals()
        {
            BuildSite site = Site();
            var colony = new Colony(site);
            var stores = new Stockpile();

            site.TryPlace(
                new BuildingFootprint(3, 5), new Vector2Int(0, 0),
                Building(posts: 1, foodPerWorker: 4), out int farm);

            Survivor rae = colony.AddFounder("Rae");
            colony.Assign(rae.Id, farm);

            // Nothing in the larder at dawn. If meals were taken first the farmer would starve
            // standing in a field they had just harvested.
            DayReport report = ColonyDay.Resolve(colony, site, stores, foodPerPerson: 2);

            Assert.That(report.Fed, Is.True);
            Assert.That(stores.AmountOf(ResourceKind.Food), Is.EqualTo(2));
        }

        [Test]
        public void AShortLarderFeedsWhoItCan()
        {
            BuildSite site = Site();
            var colony = new Colony(site);
            var stores = new Stockpile();
            stores.Set(ResourceKind.Food, 5);

            for (int i = 0; i < 4; i++)
            {
                colony.AddFounder($"Survivor {i}");
            }

            DayReport report = ColonyDay.Resolve(colony, site, stores, foodPerPerson: 2);

            // Two full meals out of five, a third refused, and the last crumb left alone rather
            // than the whole colony going hungry over one missing portion.
            Assert.That(report.FoodEaten, Is.EqualTo(4));
            Assert.That(report.WentHungry, Is.EqualTo(2));
            Assert.That(report.Fed, Is.False);
            Assert.That(stores.AmountOf(ResourceKind.Food), Is.EqualTo(1));
        }

        [Test]
        public void AnEmptyBuildingProducesNothing()
        {
            BuildSite site = Site();
            var colony = new Colony(site);
            var stores = new Stockpile();

            site.TryPlace(
                new BuildingFootprint(3, 5), new Vector2Int(0, 0),
                Building(posts: 2, foodPerWorker: 6), out int _);

            DayReport report = ColonyDay.Resolve(colony, site, stores, foodPerPerson: 2);

            Assert.That(report.FoodGrown, Is.Zero, "a farm nobody worked grew a crop");
            Assert.That(stores.AmountOf(ResourceKind.Food), Is.Zero);
        }

        [Test]
        public void AJobWithNoRecipesStillCountsAsAJob()
        {
            BuildSite site = Site();
            var colony = new Colony(site);
            var stores = new Stockpile();

            // The workshop: posts, but nothing to make until recipes exist.
            site.TryPlace(
                new BuildingFootprint(3, 5), new Vector2Int(0, 0), Building(posts: 1), out int shop);

            Survivor rae = colony.AddFounder("Rae");
            Assert.That(colony.Assign(rae.Id, shop), Is.True);

            DayReport report = ColonyDay.Resolve(colony, site, stores, foodPerPerson: 0);

            Assert.That(report.Worked, Is.EqualTo(1));
            Assert.That(report.Idle, Is.Zero, "somebody at a bench is not idle");
            Assert.That(report.FoodGrown, Is.Zero);
        }

        [Test]
        public void AskingForAWorkerSendsWhoeverIsFree()
        {
            BuildSite site = Site();
            var colony = new Colony(site);
            site.TryPlace(
                new BuildingFootprint(3, 5), new Vector2Int(0, 0), Building(posts: 1), out int shop);

            Survivor rae = colony.AddFounder("Rae");
            colony.AddFounder("Osei");

            Survivor? sent = colony.PutAnyoneToWork(shop);

            // The wheel asks for a body, not for a name. The first one with nothing to do goes.
            Assert.That(sent, Is.Not.Null);
            Assert.That(sent!.Id, Is.EqualTo(rae.Id));
            Assert.That(colony.WorkersAt(shop), Is.EqualTo(1));
        }

        [Test]
        public void AskingForAWorkerWithNobodyFreeSendsNobody()
        {
            BuildSite site = Site();
            var colony = new Colony(site);
            site.TryPlace(
                new BuildingFootprint(3, 5), new Vector2Int(0, 0), Building(posts: 4), out int shop);

            Survivor rae = colony.AddFounder("Rae");
            colony.Assign(rae.Id, shop);

            Assert.That(colony.PutAnyoneToWork(shop), Is.Null);
            Assert.That(colony.WorkersAt(shop), Is.EqualTo(1), "somebody was cloned");
        }

        [Test]
        public void AskingForAWorkerAtAFullBuildingSendsNobody()
        {
            BuildSite site = Site();
            var colony = new Colony(site);
            site.TryPlace(
                new BuildingFootprint(3, 5), new Vector2Int(0, 0), Building(posts: 1), out int shop);

            Survivor rae = colony.AddFounder("Rae");
            colony.AddFounder("Osei");
            colony.Assign(rae.Id, shop);

            Assert.That(colony.PutAnyoneToWork(shop), Is.Null);
            Assert.That(colony.FirstIdle(), Is.Not.Null, "the spare hand was taken anyway");
        }

        [Test]
        public void SendingOneHomeLeavesTheRestAtWork()
        {
            BuildSite site = Site();
            var colony = new Colony(site);
            site.TryPlace(
                new BuildingFootprint(3, 5), new Vector2Int(0, 0), Building(posts: 2), out int shop);

            Survivor rae = colony.AddFounder("Rae");
            Survivor osei = colony.AddFounder("Osei");
            colony.Assign(rae.Id, shop);
            colony.Assign(osei.Id, shop);

            Survivor? home = colony.SendOneHome(shop);

            // The last one sent is the first one back, so pressing the slice twice by mistake
            // undoes two deliberate choices rather than an arbitrary one.
            Assert.That(home, Is.Not.Null);
            Assert.That(home!.Id, Is.EqualTo(osei.Id));
            Assert.That(colony.WorkersAt(shop), Is.EqualTo(1));
        }

        [Test]
        public void SendingSomebodyHomeFromAnEmptyBuildingDoesNothing()
        {
            BuildSite site = Site();
            var colony = new Colony(site);
            site.TryPlace(
                new BuildingFootprint(3, 5), new Vector2Int(0, 0), Building(posts: 2), out int shop);
            colony.AddFounder("Rae");

            Assert.That(colony.SendOneHome(shop), Is.Null);
            Assert.That(colony.FirstIdle(), Is.Not.Null);
        }
    }
}
