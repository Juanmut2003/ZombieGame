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
        public IEnumerator EveryBuildingStandsOnExactlyTheGroundItIsDrawnOn()
        {
            // The invariant the walls rest on, and the one that broke when a building could be
            // turned: the ground a building takes is the size of its own picture. A drawing and
            // a footprint that disagree put the fence across the middle of the fields and open
            // air where the gate is.
            foreach (BuildingDefinition what in _placer.Catalogue)
            {
                if (what == null)
                {
                    continue;
                }

                for (int level = 1; level <= what.TopLevel; level++)
                {
                    BuildingLevel? rung = what.Level(level);
                    if (rung?.Sprite == null)
                    {
                        continue;
                    }

                    Vector2 drawn = rung.Sprite.rect.size / rung.Sprite.pixelsPerUnit;

                    Assert.That(
                        drawn.x,
                        Is.EqualTo((float)rung.Footprint.Width).Within(0.001f),
                        $"{what.DisplayName} L{level} is drawn {drawn.x} cells wide "
                        + $"and stands on {rung.Footprint.Width}");
                    Assert.That(
                        drawn.y,
                        Is.EqualTo((float)rung.Footprint.Height).Within(0.001f),
                        $"{what.DisplayName} L{level} is drawn {drawn.y} cells deep "
                        + $"and stands on {rung.Footprint.Height}");
                }
            }

            // And what the placer offers is the way it is drawn, never turned.
            _placer.Hold(Armory());
            Assert.That(_placer.HeldFootprint, Is.EqualTo(Armory().Footprint));

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
        public IEnumerator SomebodyIsStandingOnThePlotAndCanBeWalkedAbout()
        {
            yield return null;
            yield return null;

            GameObject? mc = GameObject.Find("MC");
            Assert.That(mc, Is.Not.Null, "the plot is a diagram, not a place");

            Assert.That(
                mc!.GetComponent<ZombieGame.Characters.CharacterMovement>(),
                Is.Not.Null,
                "nobody can be steered");
            Assert.That(
                mc.GetComponent<Rigidbody2D>(), Is.Not.Null, "walls will not stop them");
            Assert.That(
                mc.GetComponent<ZombieGame.Characters.CharacterFacing>(),
                Is.Not.Null,
                "they will face one way for ever");

            // PROJECT.md 4.6: no belt on the plot. The mouse button is building, and a weapon
            // that can only ever hit your own farm is worse than none.
            Assert.That(
                mc.GetComponent<ZombieGame.Characters.PlayerGun>(),
                Is.Null,
                "the colonist is armed on their own farm");
            Assert.That(mc.GetComponent<ZombieGame.Characters.PlayerMelee>(), Is.Null);
        }

        [UnityTest]
        public IEnumerator TheOneOnThePlotIsTheDrifter()
        {
            yield return null;
            yield return null;

            GameObject? mc = GameObject.Find("MC");
            Assert.That(mc, Is.Not.Null);

            var outfit = mc!.GetComponent<ZombieGame.Characters.CharacterOutfit>();
            Assert.That(outfit, Is.Not.Null, "nobody decides who they are");
            Assert.That(outfit.Wearing, Is.Not.Null, "the base was built with nobody in it");
            Assert.That(outfit.Wearing!.DisplayName, Is.EqualTo("Drifter"));

            // Four views and a walk with real frames in it, which is what the sheet was cut for.
            Assert.That(outfit.Wearing.Front, Has.Length.GreaterThan(1), "one standing pose");
            Assert.That(outfit.Wearing.Back, Is.Not.Empty, "they turn round when walking up");
            Assert.That(outfit.Wearing.Left, Has.Length.GreaterThan(4));
            Assert.That(outfit.Wearing.Right, Has.Length.GreaterThan(4));
        }

        [UnityTest]
        public IEnumerator ABuildingIsARoomWithWallsAndOneDoorway()
        {
            _placer.Hold(OfCategory(BuildingCategory.Barracks));
            Assert.That(
                _placer.PlaceAt(new Vector2(5f, 5f)), Is.True, "the barracks would not go down");
            _placer.Hold(null);
            yield return null;

            BuildingView[] built =
                Object.FindObjectsByType<BuildingView>(FindObjectsInactive.Exclude);
            Assert.That(built, Has.Length.EqualTo(1));

            BoxCollider2D[] walls = built[0].GetComponents<BoxCollider2D>();
            Assert.That(walls, Is.Not.Empty, "the barracks is a picture on the floor");

            BuildingFootprint footprint = _placer.Site!.Find(built[0].Id)!.Value.Footprint;
            Assert.That(
                built[0].Footprint, Is.EqualTo(footprint), "the view forgot what it stands on");

            foreach (BoxCollider2D wall in walls)
            {
                Assert.That(wall.isTrigger, Is.False, "walking through it is still free");
            }

            // The doorway is a hole in the wall and not a thinner bit of it: a figure standing on
            // the steps has to be inside nothing at all, or there is no way in.
            Rect door = BuildingWalls.Doorway(footprint);
            Vector2 steps = (Vector2)built[0].transform.position + door.center;

            foreach (BoxCollider2D wall in walls)
            {
                Assert.That(
                    wall.enabled && wall.OverlapPoint(steps),
                    Is.False,
                    $"the doorway is walled up by {wall.offset} {wall.size}");
            }

            // And the walls really are there: the middle of the back wall is solid.
            WallSegment back = BuildingWalls.Segment(footprint, 0);
            Vector2 roof = (Vector2)built[0].transform.position + back.Offset;
            bool solid = false;
            foreach (BoxCollider2D wall in walls)
            {
                solid |= wall.enabled && wall.OverlapPoint(roof);
            }

            Assert.That(solid, Is.True, "the back wall is open ground");
        }

        [UnityTest]
        public IEnumerator TheDrawingSitsOnTheGroundTheBuildingStandsOn()
        {
            // The one the eye catches and no other test did: the walls were on the cells and the
            // picture was half a building further up the plot, so the farm's fence stood in open
            // ground and you were stopped by nothing you could see. Building sprites pivot at
            // the bottom of the drawing; the room is measured from the middle of the footprint.
            foreach (BuildingCategory category in
                     new[] { BuildingCategory.Farm, BuildingCategory.Barracks })
            {
                _placer.Hold(OfCategory(category));
                Assert.That(_placer.PlaceAt(new Vector2(6f, 4f)), Is.True, $"no {category}");
                _placer.Hold(null);
                yield return null;

                BuildingView[] built =
                    Object.FindObjectsByType<BuildingView>(FindObjectsInactive.Exclude);
                BuildingView view = built[built.Length - 1];

                var renderer = view.GetComponentInChildren<SpriteRenderer>();
                Assert.That(renderer, Is.Not.Null, $"the {category} is invisible");
                Assert.That(renderer.sprite, Is.Not.Null);

                Bounds drawn = renderer.bounds;
                Vector2 ground = view.transform.position;

                Assert.That(
                    drawn.center.x,
                    Is.EqualTo(ground.x).Within(0.01f),
                    $"the {category} is drawn sideways off its own ground");
                Assert.That(
                    drawn.center.y,
                    Is.EqualTo(ground.y).Within(0.01f),
                    $"the {category} is drawn {drawn.center.y - ground.y} cells off its ground");

                Assert.That(drawn.size.x, Is.EqualTo((float)view.Footprint.Width).Within(0.01f));
                Assert.That(drawn.size.y, Is.EqualTo((float)view.Footprint.Height).Within(0.01f));

                // And the wall really is under the wall: the back of the drawing is solid.
                WallSegment back = BuildingWalls.Segment(view.Footprint, 0);
                Vector2 roof = ground + back.Offset;
                bool solid = false;
                foreach (BoxCollider2D box in view.GetComponents<BoxCollider2D>())
                {
                    solid |= box.enabled && box.OverlapPoint(roof);
                }

                Assert.That(solid, Is.True, $"the {category}'s back wall is not where it is drawn");

                _placer.Site!.Remove(view.Id);
                Object.DestroyImmediate(view.gameObject);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator WalkingIntoAWallStopsYou()
        {
            // Put up a barracks and walk the MC at its side wall. Before the walls existed, the
            // MC crossed the whole room as if it were painted on the ground.
            _placer.Hold(OfCategory(BuildingCategory.Barracks));
            Assert.That(
                _placer.PlaceAt(new Vector2(6f, 4f)), Is.True, "the barracks would not go down");
            _placer.Hold(null);
            yield return null;

            GameObject? mc = GameObject.Find("MC");
            Assert.That(mc, Is.Not.Null);

            BuildingView[] built =
                Object.FindObjectsByType<BuildingView>(FindObjectsInactive.Exclude);
            Vector2 wall = built[0].transform.position;

            // Measured off the footprint rather than off a collider: the walls are five boxes
            // now, and the left one is a quarter of a cell thick.
            float halfWide = built[0].Footprint.Width * 0.5f;

            // Lined up with the middle of the building and one clear cell to its left, measured
            // off the building rather than guessed: a barracks that grows a cell wider one day
            // would otherwise start this test inside the wall.
            mc!.transform.position = new Vector3(wall.x - halfWide - 1f, wall.y, 0f);
            var input = mc.GetComponent<ZombieGame.Characters.PlayerInputReader>();
            input.SetInputOverride(Vector2.right, sprint: false);

            // Counted in physics steps: a batch run with nothing to draw gets through hundreds
            // of frames in a fraction of a second of game time.
            float nearest = float.MaxValue;
            for (int step = 0; step < 300; step++)
            {
                yield return new WaitForFixedUpdate();
                nearest = Mathf.Min(nearest, wall.x - mc.transform.position.x);
            }

            input.ClearInputOverride();

            float stoppedAt = wall.x - mc.transform.position.x;
            Assert.That(
                nearest,
                Is.LessThan(halfWide + 0.9f),
                "the MC never set off towards the barracks");

            // Stopped outside its half-width, give or take the skin the solver leaves. Walking
            // through would have put them the far side of it, which is a negative gap.
            Assert.That(
                stoppedAt,
                Is.GreaterThan(halfWide - 0.1f),
                $"the MC walked into the barracks and came out at {mc.transform.position}");
        }

        [UnityTest]
        public IEnumerator AWorkerWalksInAndStandsAtTheirPost()
        {
            // The farm, because a barracks is beds and has no work in it at all.
            _placer.Hold(OfCategory(BuildingCategory.Farm));
            Assert.That(
                _placer.PlaceAt(new Vector2(6f, 4f)), Is.True, "the farm would not go down");
            _placer.Hold(null);
            yield return null;

            GameObject? colonist = GameObject.Find("First Survivor");
            Assert.That(colonist, Is.Not.Null);
            var body = colonist!.GetComponent<SurvivorBody>();
            var stroll = colonist.GetComponent<ZombieGame.Characters.CharacterStroll>();
            Assert.That(body, Is.Not.Null, "nothing ties the figure to the colony");

            var colony = GameObject.Find("Colony")!.GetComponent<ColonyRuntime>();
            Assert.That(colony.Colony, Is.Not.Null);

            BuildingView farm =
                Object.FindObjectsByType<BuildingView>(FindObjectsInactive.Exclude)[0];
            Vector2 middle = farm.transform.position;

            // Started square in front of the gate: getting in through a one-cell gap is the
            // thing being tested here, and the way across the plot to it is the test below.
            Rect door = BuildingWalls.Doorway(farm.Footprint);
            colonist.transform.position =
                middle + new Vector2(door.center.x, door.yMin - 1.5f);

            Survivor first = colony.Colony!.People[0];
            Assert.That(
                colony.Colony.Assign(first.Id, farm.Id), Is.True, "nobody could be put to work");

            // Up to half a minute of game time: a walk across the yard, in through the gate and
            // over to the bench, with the pauses that come of bumping a gatepost on the way.
            for (int step = 0; step < 1500 && !stroll.AtWork; step++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(
                stroll.AtWork,
                Is.True,
                $"the worker never got to their post; they are at {colonist.transform.position}");

            Assert.That(body.Workplace, Is.EqualTo(farm.Id));
            Assert.That(
                ((Vector2)colonist.transform.position - body.Post).magnitude,
                Is.LessThan(0.6f),
                "they stopped somewhere that is not their post");

            // And it is a post inside the room, not a spot in the yard outside it.
            Rect room = BuildingWalls.Room(farm.Footprint);
            Vector2 local = (Vector2)colonist.transform.position - middle;
            Assert.That(
                room.xMin - 0.3f <= local.x && local.x <= room.xMax + 0.3f,
                Is.True,
                $"the worker is standing outside the farm at {local}");
            Assert.That(
                room.yMin - 0.3f <= local.y && local.y <= room.yMax + 0.3f,
                Is.True,
                $"the worker is standing outside the farm at {local}");
        }

        [UnityTest]
        public IEnumerator AWorkerGetsToTheirPostFromWhereverTheyWereStanding()
        {
            // The same journey as the test above, but starting where the scene actually puts
            // people instead of two steps in front of the gate. This is the one the player sees:
            // somebody is told to work and has to cross the plot, round the building, and find
            // the door — and a figure that steers straight at the bench leans on the back wall
            // for the rest of the day.
            _placer.Hold(OfCategory(BuildingCategory.Farm));
            Assert.That(
                _placer.PlaceAt(new Vector2(3f, 5f)), Is.True, "the farm would not go down");
            _placer.Hold(null);
            yield return null;

            var colony = GameObject.Find("Colony")!.GetComponent<ColonyRuntime>();
            BuildingView farm =
                Object.FindObjectsByType<BuildingView>(FindObjectsInactive.Exclude)[0];

            Survivor first = colony.Colony!.People[0];
            Assert.That(colony.Colony.Assign(first.Id, farm.Id), Is.True);

            SurvivorBody body = TheBodyOf(first.Id);
            var stroll = body.GetComponent<ZombieGame.Characters.CharacterStroll>();
            Vector2 from = body.transform.position;

            for (int step = 0; step < 2400 && !stroll.AtWork; step++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(
                stroll.AtWork,
                Is.True,
                $"set off from {from} and got no further than {body.transform.position}");
        }

        private SurvivorBody TheBodyOf(int survivorId)
        {
            foreach (SurvivorBody body in
                     Object.FindObjectsByType<SurvivorBody>(FindObjectsInactive.Exclude))
            {
                if (body.Answers == survivorId)
                {
                    return body;
                }
            }

            Assert.Fail($"nobody on the plot stands in for survivor {survivorId}");
            return null!;
        }

        [UnityTest]
        public IEnumerator SendingAWorkerHomeGivesThemThePlotBack()
        {
            _placer.Hold(OfCategory(BuildingCategory.Farm));
            Assert.That(
                _placer.PlaceAt(new Vector2(6f, 4f)), Is.True, "the farm would not go down");
            _placer.Hold(null);
            yield return null;

            GameObject? colonist = GameObject.Find("First Survivor");
            var stroll = colonist!.GetComponent<ZombieGame.Characters.CharacterStroll>();
            var colony = GameObject.Find("Colony")!.GetComponent<ColonyRuntime>();
            BuildingView farm =
                Object.FindObjectsByType<BuildingView>(FindObjectsInactive.Exclude)[0];

            Survivor first = colony.Colony!.People[0];
            Assert.That(colony.Colony.Assign(first.Id, farm.Id), Is.True);
            yield return null;
            yield return null;

            Assert.That(stroll.HasAJob, Is.True, "being put to work told the figure nothing");

            colony.Colony.Unassign(first.Id);
            yield return null;
            yield return null;

            Assert.That(stroll.HasAJob, Is.False, "they are still standing at the bench");
            Assert.That(stroll.AtWork, Is.False);
        }

        [UnityTest]
        public IEnumerator EverybodyInTheColonyHasABodyAndNoTwoShareOne()
        {
            yield return null;
            yield return null;

            var colony = GameObject.Find("Colony")!.GetComponent<ColonyRuntime>();
            Assert.That(colony.Colony, Is.Not.Null);

            SurvivorBody[] bodies =
                Object.FindObjectsByType<SurvivorBody>(FindObjectsInactive.Exclude);

            // One figure per person. The founders are built into the scene and the bodies are
            // placed by hand beside them, so the two lists drifting apart is the fault to catch:
            // a colony of four with three figures is a name nobody can see.
            Assert.That(
                bodies,
                Has.Length.EqualTo(colony.Colony!.Count),
                "the plot and the roster disagree about how many people live here");

            var seen = new System.Collections.Generic.HashSet<int>();
            foreach (SurvivorBody body in bodies)
            {
                var who = body.GetComponent<ZombieGame.Characters.CharacterStroll>();
                Assert.That(who, Is.Not.Null, $"{body.name} has no legs");
                Assert.That(
                    seen.Add(body.Answers),
                    Is.True,
                    $"two figures are both standing in for survivor {body.Answers}");
                Assert.That(colony.Colony.Find(body.Answers), Is.Not.Null, "a body with no name");
            }
        }

        [UnityTest]
        public IEnumerator PuttingOnePersonToWorkMovesOnlyTheirFigure()
        {
            _placer.Hold(OfCategory(BuildingCategory.Farm));
            Assert.That(
                _placer.PlaceAt(new Vector2(6f, 4f)), Is.True, "the farm would not go down");
            _placer.Hold(null);
            yield return null;

            var colony = GameObject.Find("Colony")!.GetComponent<ColonyRuntime>();
            BuildingView farm =
                Object.FindObjectsByType<BuildingView>(FindObjectsInactive.Exclude)[0];

            Survivor first = colony.Colony!.People[0];
            Assert.That(colony.Colony.Assign(first.Id, farm.Id), Is.True);
            yield return null;
            yield return null;

            foreach (SurvivorBody body in
                     Object.FindObjectsByType<SurvivorBody>(FindObjectsInactive.Exclude))
            {
                var stroll = body.GetComponent<ZombieGame.Characters.CharacterStroll>();
                bool theirs = body.Answers == first.Id;

                Assert.That(
                    stroll.HasAJob,
                    Is.EqualTo(theirs),
                    theirs
                        ? "the one put to work was never told"
                        : $"{body.name} went to work in somebody else's place");
            }
        }

        [UnityTest]
        public IEnumerator TheColonyHasAFirstSurvivorWithABodyOnThePlot()
        {
            yield return null;
            yield return null;

            GameObject? colonist = GameObject.Find("First Survivor");
            Assert.That(colonist, Is.Not.Null, "the colony is a list of names again");

            Assert.That(
                colonist!.GetComponent<ZombieGame.Characters.CharacterStroll>(),
                Is.Not.Null,
                "they will stand on one spot all game");
            Assert.That(
                colonist.GetComponent<Rigidbody2D>(), Is.Not.Null, "walls will not stop them");

            // No keys and no belt. A second input reader in the scene would read the same keys
            // as the MC, and the pair would cross the plot in step like a parade.
            Assert.That(
                colonist.GetComponent<ZombieGame.Characters.PlayerInputReader>(),
                Is.Null,
                "the survivor is steered by the player's keys");
            Assert.That(colonist.GetComponent<ZombieGame.Characters.PlayerGun>(), Is.Null);
            Assert.That(colonist.GetComponent<ZombieGame.Characters.PlayerMelee>(), Is.Null);
        }

        [UnityTest]
        public IEnumerator TheFirstSurvivorIsNotWhoeverThePlayerIs()
        {
            yield return null;
            yield return null;

            GameObject? mc = GameObject.Find("MC");
            GameObject? colonist = GameObject.Find("First Survivor");
            Assert.That(mc, Is.Not.Null);
            Assert.That(colonist, Is.Not.Null);

            var worn = mc!.GetComponent<ZombieGame.Characters.CharacterOutfit>();
            var spare = colonist!.GetComponent<ZombieGame.Characters.SpareOutfit>();
            Assert.That(spare, Is.Not.Null, "nobody decides who the survivor is");
            Assert.That(spare.Wearing, Is.Not.Null, "the plot was built with a nameless figure");

            // Compared against whoever is actually being played rather than against one name:
            // the point is that the base never shows the player twice, whichever card was
            // pressed on the way in.
            Assert.That(
                spare.Wearing!.DisplayName,
                Is.Not.EqualTo(worn.Wearing!.DisplayName),
                "the player is standing next to a copy of themselves");

            Assert.That(spare.Wearing.CanBeDrawn, Is.True, "the survivor has no walk");
        }

        [UnityTest]
        public IEnumerator TheFirstSurvivorWalksOffOnTheirOwnAndStaysOnThePlot()
        {
            yield return null;

            GameObject? colonist = GameObject.Find("First Survivor");
            Assert.That(colonist, Is.Not.Null);

            var stroll = colonist!.GetComponent<ZombieGame.Characters.CharacterStroll>();
            Vector2 started = colonist.transform.position;
            float furthest = 0f;

            // Counted in physics steps, not rendered frames. Walking happens in FixedUpdate, and
            // a batch run with nothing to draw rattles through hundreds of frames in a fraction
            // of a second of game time — in frames, this test watched for half a second and
            // concluded the survivor was scenery.
            // Twelve seconds: long enough for the first pause, a walk, and a stop.
            for (int step = 0; step < 600; step++)
            {
                yield return new WaitForFixedUpdate();

                Vector2 here = colonist.transform.position;
                furthest = Mathf.Max(furthest, (here - started).magnitude);

                // A little slack on the edges, because stopping takes a step and the spot they
                // are heading for is the last cell rather than the fence.
                Assert.That(
                    stroll.Area.xMin - 1.5f <= here.x && here.x <= stroll.Area.xMax + 1.5f,
                    Is.True,
                    $"the survivor walked off the plot at {here}");
                Assert.That(
                    stroll.Area.yMin - 1.5f <= here.y && here.y <= stroll.Area.yMax + 1.5f,
                    Is.True,
                    $"the survivor walked off the plot at {here}");
            }

            Assert.That(furthest, Is.GreaterThan(0.25f), "the survivor never left their spot");
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
