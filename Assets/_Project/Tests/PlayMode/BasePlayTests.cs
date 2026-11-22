#nullable enable
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using ZombieGame.Base;
using ZombieGame.Core;
using ZombieGame.Economy;
using ZombieGame.UI;

namespace ZombieGame.Tests.PlayMode
{
    /// <summary>
    /// The colony scene as it is actually built: a plot, a catalogue, and building on it.
    /// </summary>
    /// <remarks>
    /// The rules are settled in edit mode. What only the built scene shows is that the pieces
    /// are in it and wired to each other — a placer holding a catalogue of nothing, or a plot
    /// whose blocked ground was never marked, looks exactly like a working base until somebody
    /// tries to put a barn on the forecourt.
    /// </remarks>
    [TestFixture]
    public sealed class BasePlayTests
    {
        private BuildPlacer _placer = null!;

        [UnitySetUp]
        public IEnumerator LoadBase()
        {
            yield return SceneManager.LoadSceneAsync("Base", LoadSceneMode.Single);
            yield return null;

            GameObject? placerObject = GameObject.Find("Build Placer");
            Assert.That(placerObject, Is.Not.Null, "the base has no way to build anything");
            _placer = placerObject!.GetComponent<BuildPlacer>();

            yield return null;
        }

        private BuildingDefinition OfCategory(BuildingCategory category)
        {
            foreach (BuildingDefinition building in _placer.Catalogue)
            {
                if (building != null && building.Category == category)
                {
                    return building;
                }
            }

            Assert.Fail($"the catalogue has no {category} in it");
            return null!;
        }

        private BuildingDefinition Armory() => OfCategory(BuildingCategory.Armory);

        [UnityTest]
        public IEnumerator ThePlotIsThereAndAnyoneCanAskForIt()
        {
            Assert.That(
                GameServices.TryGet(out BuildSite? site),
                Is.True,
                "nothing else will ever find the plot");

            Assert.That(site!.Width, Is.GreaterThan(0));
            Assert.That(site.Height, Is.GreaterThan(0));
            Assert.That(site.Buildings, Is.Empty, "the plot starts built on");

            yield return null;
        }

        [UnityTest]
        public IEnumerator TheForecourtIsNotBuildable()
        {
            Assert.That(GameServices.TryGet(out BuildSite? site), Is.True);

            // The way in. Laid out as blocked in the builder and drawn as road, so this is the
            // one place on the plot where the ground itself refuses.
            Assert.That(site!.IsBlocked(0, 0), Is.True, "the forecourt was never marked");
            Assert.That(site.IsBlocked(7, 4), Is.False, "the middle of the plot is barred");

            yield return null;
        }

        [UnityTest]
        public IEnumerator EveryBuildingIsOnTheShelf()
        {
            // The four buildings of PROJECT.md 4.2 plus the generator and the well, which the
            // colony cannot run without.
            Assert.That(_placer.Catalogue.Length, Is.EqualTo(6), "the build catalogue is short");

            foreach (BuildingDefinition building in _placer.Catalogue)
            {
                Assert.That(building, Is.Not.Null, "an empty slot in the catalogue");
                Assert.That(building.Sprite, Is.Not.Null, $"{building.DisplayName} is invisible");
                Assert.That(building.Footprint.Area, Is.GreaterThan(0));
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator PuttingSomethingUpLeavesItStanding()
        {
            GameObject? built = GameObject.Find("Buildings");
            Assert.That(built, Is.Not.Null, "there is nowhere to put what gets built");
            Assert.That(built!.transform.childCount, Is.Zero);

            _placer.Hold(Armory());
            Assert.That(_placer.PlaceAt(new Vector2(7f, 4f)), Is.True, "open ground refused it");
            yield return null;

            Assert.That(
                built.transform.childCount,
                Is.EqualTo(1),
                "the plot thinks it is there and the scene does not");

            Assert.That(GameServices.TryGet(out BuildSite? site), Is.True);
            Assert.That(site!.Buildings.Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator NothingGoesUpOnTheForecourt()
        {
            _placer.Hold(Armory());

            // Overlapping the blocked strip by part of itself, which is the case a corner-only
            // check would wave through. The corner lands on 0,0 and the building reaches up
            // and across from there, over the forecourt.
            Assert.That(_placer.PlaceAt(new Vector2(1f, 2f)), Is.False);
            Assert.That(_placer.LastVerdict, Is.EqualTo(PlacementVerdict.GroundIsBlocked));

            yield return null;
        }

        [UnityTest]
        public IEnumerator TwoThingsWillNotShareASpot()
        {
            _placer.Hold(Armory());
            Assert.That(_placer.PlaceAt(new Vector2(7f, 4f)), Is.True);

            Assert.That(_placer.PlaceAt(new Vector2(7f, 4f)), Is.False);
            Assert.That(_placer.LastVerdict, Is.EqualTo(PlacementVerdict.SomethingIsThere));

            yield return null;
        }

        [UnityTest]
        public IEnumerator TurningWhatIsHeldSwapsItsSides()
        {
            BuildingDefinition armory = Armory();
            _placer.Hold(armory);

            BuildingFootprint upright = _placer.HeldFootprint;
            _placer.Rotate();

            Assert.That(_placer.HeldFootprint, Is.EqualTo(upright.Rotated()));
            Assert.That(_placer.HeldFootprint, Is.Not.EqualTo(upright), "the armory is square");

            yield return null;
        }

        [UnityTest]
        public IEnumerator EmptyHandsBuildNothing()
        {
            _placer.Hold(null);
            Assert.That(_placer.PlaceAt(new Vector2(7f, 4f)), Is.False);

            // And picking something off the end of the catalogue empties them rather than
            // throwing, which is what a build menu with a stale index would do.
            _placer.Hold(99);
            Assert.That(_placer.Holding, Is.Null);
            Assert.That(_placer.PlaceAt(new Vector2(7f, 4f)), Is.False);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TheColonyStartsWithSomethingToBuildWith()
        {
            Assert.That(
                GameServices.TryGet(out Stockpile? stores),
                Is.True,
                "nothing can be paid for");

            Assert.That(
                stores!.AmountOf(ResourceKind.Materials),
                Is.GreaterThan(0),
                "a colony that cannot afford its first building");

            yield return null;
        }

        [UnityTest]
        public IEnumerator PuttingSomethingUpIsPaidFor()
        {
            Assert.That(GameServices.TryGet(out Stockpile? stores), Is.True);

            BuildingDefinition armory = Armory();
            Assert.That(armory.Cost.Count, Is.GreaterThan(0), "the armory is free");

            int before = stores!.AmountOf(ResourceKind.Materials);
            _placer.Hold(armory);
            Assert.That(_placer.PlaceAt(new Vector2(7f, 4f)), Is.True);
            yield return null;

            Assert.That(
                stores.AmountOf(ResourceKind.Materials),
                Is.LessThan(before),
                "the building went up and nobody paid for it");
        }

        [UnityTest]
        public IEnumerator ARefusedSpotCostsNothing()
        {
            Assert.That(GameServices.TryGet(out Stockpile? stores), Is.True);

            _placer.Hold(Armory());
            int before = stores!.AmountOf(ResourceKind.Materials);

            // Straight onto the blocked forecourt.
            Assert.That(_placer.PlaceAt(new Vector2(1f, 2f)), Is.False);
            yield return null;

            Assert.That(
                stores.AmountOf(ResourceKind.Materials),
                Is.EqualTo(before),
                "the colony paid for a building it never got");
        }

        [UnityTest]
        public IEnumerator AnEmptyStoreStopsTheBuilding()
        {
            Assert.That(GameServices.TryGet(out Stockpile? stores), Is.True);
            stores!.Set(ResourceKind.Materials, 0);
            stores.Set(ResourceKind.Fuel, 0);

            _placer.Hold(Armory());
            Assert.That(_placer.PlaceAt(new Vector2(7f, 4f)), Is.False);
            Assert.That(_placer.LastVerdict, Is.EqualTo(PlacementVerdict.CannotAfford));

            Assert.That(GameServices.TryGet(out BuildSite? site), Is.True);
            Assert.That(site!.Buildings, Is.Empty);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TheHintSaysWhyNot()
        {
            GameObject? hintObject = GameObject.Find("Build Hint Presenter");
            Assert.That(hintObject, Is.Not.Null, "nothing tells the player why a spot is red");
            var hint = hintObject!.GetComponent<BuildHintPresenter>();

            Assert.That(GameServices.TryGet(out Stockpile? stores), Is.True);
            stores!.Set(ResourceKind.Materials, 0);
            stores.Set(ResourceKind.Fuel, 0);

            _placer.Hold(Armory());
            _placer.PlaceAt(new Vector2(7f, 4f));

            // The hint is written in Update, so it lands on the next frame.
            yield return null;
            yield return null;

            Assert.That(hint.Text, Does.Contain("STORES"));
        }

        [UnityTest]
        public IEnumerator TheStoresAreOnScreen()
        {
            GameObject? line = GameObject.Find("Store 0");
            Assert.That(line, Is.Not.Null, "the colony cannot see what it has");

            yield return null;

            Assert.That(
                line!.GetComponent<UnityEngine.UI.Text>().text,
                Does.Contain("SALVAGE"),
                "the readout never filled in");
        }

        [UnityTest]
        public IEnumerator TheFoundersAreHereAndSleepingRough()
        {
            GameObject? colonyObject = GameObject.Find("Colony");
            Assert.That(colonyObject, Is.Not.Null, "nobody lives at the base");
            var runtime = colonyObject!.GetComponent<ColonyRuntime>();

            Assert.That(runtime.Colony, Is.Not.Null);
            Assert.That(runtime.Colony!.Count, Is.GreaterThan(0));

            // No barracks has been built, so the people who took the site have no beds. That is
            // the whole reason the first barracks matters.
            Assert.That(runtime.Colony.Capacity, Is.Zero);
            Assert.That(runtime.Colony.Homeless, Is.EqualTo(runtime.Colony.Count));

            yield return null;
        }

        [UnityTest]
        public IEnumerator ABarracksGivesThemBeds()
        {
            var runtime = GameObject.Find("Colony")!.GetComponent<ColonyRuntime>();
            BuildingDefinition barracks = OfCategory(BuildingCategory.Barracks);

            _placer.Hold(barracks);
            Assert.That(_placer.PlaceAt(new Vector2(7f, 4f)), Is.True);
            yield return null;

            Assert.That(runtime.Colony!.Capacity, Is.EqualTo(barracks.Level(1)!.Beds));
            Assert.That(barracks.Level(1)!.Beds, Is.GreaterThan(0), "the barracks has no beds in it");
        }

        [UnityTest]
        public IEnumerator AFarmerFeedsTheColony()
        {
            var runtime = GameObject.Find("Colony")!.GetComponent<ColonyRuntime>();
            Assert.That(GameServices.TryGet(out Stockpile? stores), Is.True);

            BuildingDefinition farm = OfCategory(BuildingCategory.Farm);
            Assert.That(farm.Level(1)!.WorkPosts, Is.GreaterThan(0), "nobody can work the farm");
            Assert.That(farm.Level(1)!.HasWork, Is.True, "the farm grows nothing");

            _placer.Hold(farm);
            Assert.That(_placer.PlaceAt(new Vector2(7f, 4f)), Is.True);
            yield return null;

            Colony colony = runtime.Colony!;
            int farmId = 0;
            Assert.That(GameServices.TryGet(out BuildSite? site), Is.True);
            foreach (PlacedBuilding building in site!.Buildings)
            {
                farmId = building.Id;
            }

            Assert.That(colony.Assign(colony.People[0].Id, farmId), Is.True);

            int foodBefore = stores!.AmountOf(ResourceKind.Food);
            runtime.EndDay();
            yield return null;

            Assert.That(runtime.DaysLived, Is.EqualTo(1));
            Assert.That(runtime.LastReport.Worked, Is.EqualTo(1));
            Assert.That(
                runtime.LastReport.FoodGrown,
                Is.GreaterThan(0),
                "a day on the farm grew nothing");

            // The larder moved by exactly what the report says it did, which is the thing worth
            // pinning down: a day that grows six and eats six is allowed to come out level, but
            // a day whose books do not add up is a bug wherever the total lands.
            Assert.That(
                stores.AmountOf(ResourceKind.Food),
                Is.EqualTo(foodBefore + runtime.LastReport.FoodGrown - runtime.LastReport.FoodEaten),
                "the day's books do not add up");

            Assert.That(
                runtime.LastReport.FoodEaten,
                Is.GreaterThan(0),
                "a day passed and nobody ate");
        }

        [UnityTest]
        public IEnumerator AnIdleColonyJustEats()
        {
            var runtime = GameObject.Find("Colony")!.GetComponent<ColonyRuntime>();
            Assert.That(GameServices.TryGet(out Stockpile? stores), Is.True);
            stores!.Set(ResourceKind.Food, 50);

            runtime.EndDay();
            yield return null;

            Assert.That(runtime.LastReport.Worked, Is.Zero);
            Assert.That(runtime.LastReport.Idle, Is.EqualTo(runtime.Colony!.Count));
            Assert.That(runtime.LastReport.FoodEaten, Is.GreaterThan(0), "nobody ate");
            Assert.That(stores.AmountOf(ResourceKind.Food), Is.LessThan(50));
        }

        [UnityTest]
        public IEnumerator TheRosterIsOnScreen()
        {
            GameObject? rosterObject = GameObject.Find("Colony Roster");
            Assert.That(rosterObject, Is.Not.Null, "there is no way to see who is here");

            yield return null;
            yield return null;

            string text = rosterObject!.GetComponent<UnityEngine.UI.Text>().text;
            Assert.That(text, Does.Contain("BEDS"));
            Assert.That(text, Does.Contain("IDLE"), "nobody is listed as having nothing to do");
        }

        private BuildingActions Actions()
        {
            GameObject? found = GameObject.Find("Building Actions");
            Assert.That(found, Is.Not.Null, "nothing can be done with a building once it is up");
            return found!.GetComponent<BuildingActions>();
        }

        [UnityTest]
        public IEnumerator TearingSomethingDownGivesPartOfItBack()
        {
            Assert.That(GameServices.TryGet(out Stockpile? stores), Is.True);

            BuildingDefinition armory = OfCategory(BuildingCategory.Armory);
            _placer.Hold(armory);
            Assert.That(_placer.PlaceAt(new Vector2(7f, 4f)), Is.True);
            yield return null;

            int afterBuilding = stores!.AmountOf(ResourceKind.Materials);
            Assert.That(GameServices.TryGet(out BuildSite? site), Is.True);
            int id = site!.Buildings[0].Id;

            BuildingActions actions = Actions();
            actions.Select(id);
            Assert.That(actions.Demolish(), Is.True);
            yield return null;

            Assert.That(site.Buildings, Is.Empty, "the plot still thinks it is there");
            Assert.That(
                stores.AmountOf(ResourceKind.Materials),
                Is.GreaterThan(afterBuilding),
                "nothing came back from the rubble");

            // Part of it, not all: misplacing a building has to cost something.
            Assert.That(
                stores.AmountOf(ResourceKind.Materials),
                Is.LessThan(afterBuilding + armory.Cost[0].Amount),
                "a demolition refunded the whole price");
        }

        [UnityTest]
        public IEnumerator TearingSomethingDownTakesItOffTheScreen()
        {
            GameObject? built = GameObject.Find("Buildings");
            Assert.That(built, Is.Not.Null);

            _placer.Hold(OfCategory(BuildingCategory.Armory));
            Assert.That(_placer.PlaceAt(new Vector2(7f, 4f)), Is.True);
            yield return null;
            Assert.That(built!.transform.childCount, Is.EqualTo(1));

            Assert.That(GameServices.TryGet(out BuildSite? site), Is.True);
            BuildingActions actions = Actions();
            actions.Select(site!.Buildings[0].Id);
            actions.Demolish();

            // Destroy lands at the end of the frame.
            yield return null;
            yield return null;

            Assert.That(
                built.transform.childCount,
                Is.Zero,
                "the building is gone from the plot and still on the screen");
        }

        [UnityTest]
        public IEnumerator TearingSomethingDownSendsItsWorkersHome()
        {
            var runtime = GameObject.Find("Colony")!.GetComponent<ColonyRuntime>();

            _placer.Hold(OfCategory(BuildingCategory.Farm));
            Assert.That(_placer.PlaceAt(new Vector2(7f, 4f)), Is.True);
            yield return null;

            Assert.That(GameServices.TryGet(out BuildSite? site), Is.True);
            int farm = site!.Buildings[0].Id;
            Colony colony = runtime.Colony!;
            Assert.That(colony.Assign(colony.People[0].Id, farm), Is.True);

            BuildingActions actions = Actions();
            actions.Select(farm);
            actions.Demolish();
            yield return null;

            Assert.That(
                colony.People[0].IsIdle,
                Is.True,
                "somebody is still working a building that is rubble");
        }

        [UnityTest]
        public IEnumerator ImprovingABuildingGrowsItAndCostsSomething()
        {
            Assert.That(GameServices.TryGet(out Stockpile? stores), Is.True);
            stores!.Set(ResourceKind.Materials, 500);
            stores.Set(ResourceKind.Water, 500);

            BuildingDefinition farm = OfCategory(BuildingCategory.Farm);
            Assert.That(farm.TopLevel, Is.EqualTo(4), "the farm has no ladder");

            _placer.Hold(farm);
            Assert.That(_placer.PlaceAt(new Vector2(7f, 4f)), Is.True);
            yield return null;

            Assert.That(GameServices.TryGet(out BuildSite? site), Is.True);
            int id = site!.Buildings[0].Id;
            BuildingFootprint before = site.Buildings[0].Footprint;
            int salvageBefore = stores.AmountOf(ResourceKind.Materials);

            BuildingActions actions = Actions();
            actions.Select(id);
            Assert.That(actions.Upgrade(), Is.True);
            yield return null;

            PlacedBuilding grown = site.Find(id)!.Value;
            Assert.That(grown.Level, Is.EqualTo(2));
            Assert.That(grown.Footprint.Area, Is.GreaterThan(before.Area), "it did not grow");
            Assert.That(
                stores.AmountOf(ResourceKind.Materials),
                Is.LessThan(salvageBefore),
                "the upgrade was free");

            // And the level-two farm takes more hands than the level-one one did.
            Assert.That(
                grown.Rung!.WorkPosts,
                Is.GreaterThan(farm.Level(1)!.WorkPosts),
                "a bigger farm with the same crew");
        }

        [UnityTest]
        public IEnumerator AnEmptyStoreStopsAnImprovement()
        {
            Assert.That(GameServices.TryGet(out Stockpile? stores), Is.True);

            _placer.Hold(OfCategory(BuildingCategory.Farm));
            Assert.That(_placer.PlaceAt(new Vector2(7f, 4f)), Is.True);
            yield return null;

            stores!.Set(ResourceKind.Materials, 0);
            stores.Set(ResourceKind.Water, 0);

            Assert.That(GameServices.TryGet(out BuildSite? site), Is.True);
            BuildingActions actions = Actions();
            actions.Select(site!.Buildings[0].Id);

            Assert.That(actions.Upgrade(), Is.False);
            Assert.That(site.Buildings[0].Level, Is.EqualTo(1));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ABuildingAtTheTopOfItsLadderStaysThere()
        {
            Assert.That(GameServices.TryGet(out Stockpile? stores), Is.True);
            stores!.Set(ResourceKind.Materials, 5000);
            stores.Set(ResourceKind.Water, 5000);
            stores.Set(ResourceKind.Fuel, 5000);

            BuildingDefinition barracks = OfCategory(BuildingCategory.Barracks);
            _placer.Hold(barracks);

            // Bottom-left of the plot, clear of the forecourt, with room to grow.
            Assert.That(_placer.PlaceAt(new Vector2(6f, 3f)), Is.True);
            yield return null;

            Assert.That(GameServices.TryGet(out BuildSite? site), Is.True);
            int id = site!.Buildings[0].Id;
            BuildingActions actions = Actions();
            actions.Select(id);

            for (int step = 0; step < barracks.TopLevel - 1; step++)
            {
                Assert.That(actions.Upgrade(), Is.True, $"step {step + 1} refused");
                yield return null;
            }

            Assert.That(site.Find(id)!.Value.Level, Is.EqualTo(barracks.TopLevel));
            Assert.That(actions.Upgrade(), Is.False, "it went past the top of its ladder");
        }

        [UnityTest]
        public IEnumerator TheGhostIsPutAwayWhenNothingIsHeld()
        {
            GameObject? ghostObject = GameObject.Find("Build Ghost");
            Assert.That(ghostObject, Is.Not.Null, "there is no preview to hold");

            var ghost = ghostObject!.GetComponent<BuildGhost>();
            _placer.Hold(null);
            yield return null;

            Assert.That(ghost.IsShowing, Is.False, "a ghost of nothing is being drawn");
        }

        [UnityTest]
        public IEnumerator TheWheelOpensOnWhateverIsPickedAndShutsWithIt()
        {
            GameObject? wheelObject = GameObject.Find("Building Wheel");
            Assert.That(wheelObject, Is.Not.Null, "a building has no menu");
            var wheel = wheelObject!.GetComponent<BuildingWheelView>();

            _placer.Hold(OfCategory(BuildingCategory.Barracks));
            Assert.That(_placer.PlaceAt(new Vector2(7f, 4f)), Is.True);
            _placer.Hold(null);
            yield return null;

            Assert.That(GameServices.TryGet(out BuildSite? site), Is.True);
            int id = site!.Buildings[0].Id;

            Actions().Select(id);
            yield return null;

            Assert.That(wheel.OpenOn, Is.EqualTo(id), "the wheel did not open");
            Assert.That(wheel.Slices.Count, Is.EqualTo(BuildingMenu.Slices));
            foreach (UnityEngine.UI.Text slice in wheel.Slices)
            {
                Assert.That(slice.text, Is.Not.Empty, "a slice of the wheel says nothing");
            }

            // Clicking off the building has to put it away again, or the wheel stays over a
            // plot the player is trying to build on.
            Actions().Select(0);
            yield return null;

            Assert.That(wheel.OpenOn, Is.Zero, "the wheel stayed open on nothing");
        }

        [UnityTest]
        public IEnumerator TheWheelTellsThePlotWhenItHasThePointer()
        {
            yield return null;

            Assert.That(
                GameServices.TryGet(out ZombieGame.Core.Input.PointerFocus? pointer),
                Is.True,
                "nothing can tell whether a click landed on the HUD");
            Assert.That(pointer!.OverTheHud, Is.False, "the HUD claims a pointer that is nowhere");
        }

        [UnityTest]
        public IEnumerator TheBaseHasAWayOutAndItIsWiredToThePlot()
        {
            yield return null;

            GameObject? launcherObject = GameObject.Find("Raid Launcher");
            Assert.That(launcherObject, Is.Not.Null, "there is no way off the plot");

            RaidLauncher launcher = launcherObject!.GetComponent<RaidLauncher>();
            Assert.That(launcher.CanGo, Is.True, "the squad cannot leave on the first day");
            Assert.That(launcher.Why, Is.Empty);

            // Opened on its own there is no run yet, and that is the point: nothing is
            // remembered about a base nobody started a game in.
            Assert.That(launcher.Run, Is.Null, "a run started itself");
        }

        [UnityTest]
        public IEnumerator TheOrdersPanelListsWhatTheColonyIsAskedFor()
        {
            yield return null;
            yield return null;

            GameObject? presenterObject = GameObject.Find("Colony Task Presenter");
            Assert.That(presenterObject, Is.Not.Null, "the colony is given no orders");

            ColonyTaskPresenter presenter = presenterObject!.GetComponent<ColonyTaskPresenter>();

            Assert.That(presenter.ListText, Does.Contain("BARRACKS"));
            Assert.That(presenter.ListText, Does.Contain("FARM"));
            Assert.That(presenter.ListText, Does.Contain("WORK"));
            Assert.That(presenter.ListText, Does.Contain("["), "nothing can be ticked off");
            Assert.That(presenter.RaidOffered, Is.True, "the raid button is dead on day one");
            Assert.That(presenter.RaidLabelText, Is.EqualTo("RAID TOWN"));
        }
    }
}
