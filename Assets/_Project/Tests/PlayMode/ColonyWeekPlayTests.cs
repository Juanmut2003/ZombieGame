#nullable enable
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using ZombieGame.Base;
using ZombieGame.Core;
using ZombieGame.Economy;

namespace ZombieGame.Tests.PlayMode
{
    /// <summary>
    /// M2's definition of done: a base that survives five in-game days with jobs assigned.
    /// </summary>
    /// <remarks>
    /// Every rule a day is made of is already tested one at a time. What none of those tests can
    /// say is whether the numbers add up over a week — whether a colony that does the obvious
    /// thing on day one is still fed on day five, or quietly runs out of water on day three
    /// while every single day was resolved correctly. That is a question about the balance
    /// assets, not about the code, and it is the question PROJECT.md §14 asks of M2.
    /// Run in the real scene rather than over a hand-built plot on purpose: the opening stores,
    /// the catalogue's prices and the ladders' yields are the things under test, and a test that
    /// made up its own would prove the arithmetic and nothing about the game.
    /// The colony built here is the obvious opening: beds, something to eat, something to drink.
    /// No generator, because nothing on the first rung of any of the three draws power — which
    /// is itself a design claim worth failing out loud if it stops being true.
    /// </remarks>
    [TestFixture]
    public sealed class ColonyWeekPlayTests
    {
        private const int Days = 5;

        private BuildPlacer _placer = null!;
        private ColonyRuntime _colony = null!;
        private Stockpile _stores = null!;

        [UnitySetUp]
        public IEnumerator LoadBase()
        {
            yield return SceneManager.LoadSceneAsync("Base", LoadSceneMode.Single);
            yield return null;

            _placer = GameObject.Find("Build Placer")!.GetComponent<BuildPlacer>();
            _colony = GameObject.Find("Colony")!.GetComponent<ColonyRuntime>();

            Assert.That(GameServices.TryGet(out Stockpile? stores), Is.True, "no stores");
            _stores = stores!;

            yield return null;
        }

        private BuildingDefinition Of(BuildingCategory category)
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

        /// <summary>The well, which is support like the generator and told apart by name.</summary>
        private BuildingDefinition Well()
        {
            foreach (BuildingDefinition building in _placer.Catalogue)
            {
                if (building != null && building.DisplayName == "Well")
                {
                    return building;
                }
            }

            Assert.Fail("the catalogue has no well in it");
            return null!;
        }

        private void Put(BuildingDefinition what, Vector2 middle)
        {
            _placer.Hold(what);
            Assert.That(
                _placer.PlaceAt(middle),
                Is.True,
                $"{what.DisplayName} would not go down at {middle} — "
                + $"the site said {_placer.LastVerdict}");
            _placer.Hold(null);
        }

        [UnityTest]
        public IEnumerator AColonyWithBedsAFarmAndAWellLivesThroughFiveDays()
        {
            // Along the left of the plot, clear of the blocked forecourt in the bottom corner.
            Put(Of(BuildingCategory.Barracks), new Vector2(1f, 5f));
            Put(Of(BuildingCategory.Farm), new Vector2(5f, 5f));
            Put(Well(), new Vector2(8.5f, 1f));
            yield return null;

            Assert.That(GameServices.TryGet(out BuildSite? site), Is.True);
            Assert.That(site!.Buildings.Count, Is.EqualTo(3), "something did not go up");

            int farm = site.Buildings[1].Id;
            int well = site.Buildings[2].Id;

            Colony? colony = _colony.Colony;
            Assert.That(colony, Is.Not.Null, "nobody lives here");
            Assert.That(colony!.Count, Is.EqualTo(3), "the founders did not arrive");

            Assert.That(colony.Assign(colony.People[0].Id, farm), Is.True, "nobody can farm");
            Assert.That(colony.Assign(colony.People[1].Id, well), Is.True, "nobody can pump");

            int food = _stores.AmountOf(ResourceKind.Food);
            int water = _stores.AmountOf(ResourceKind.Water);
            int fuel = _stores.AmountOf(ResourceKind.Fuel);

            for (int day = 1; day <= Days; day++)
            {
                _colony.EndDay();
                yield return null;

                DayReport report = _colony.LastReport;

                Assert.That(report.WentHungry, Is.Zero, $"somebody went hungry on day {day}");
                Assert.That(report.WentThirsty, Is.Zero, $"somebody went thirsty on day {day}");
                Assert.That(report.Darkened, Is.Zero, $"something went dark on day {day}");
                Assert.That(report.Worked, Is.EqualTo(2), $"the posts emptied on day {day}");
                Assert.That(report.FoodGrown, Is.GreaterThan(0), $"the farm idled on day {day}");
            }

            Assert.That(_colony.DaysLived, Is.EqualTo(Days));

            // The claim M2 is making: five days on, the colony is no worse off than it started.
            // Flat is the right answer and not a weak one — a first colony that gains food while
            // feeding three people would make the farm ladder pointless.
            Assert.That(
                _stores.AmountOf(ResourceKind.Food),
                Is.GreaterThanOrEqualTo(food),
                "the colony ate into its larder over five days");
            Assert.That(
                _stores.AmountOf(ResourceKind.Water),
                Is.GreaterThanOrEqualTo(water),
                "the colony drank its water down over five days");

            // Nothing here burns diesel, so nothing should have touched it.
            Assert.That(_stores.AmountOf(ResourceKind.Fuel), Is.EqualTo(fuel));

            // One of the three is sleeping rough, because the first barracks has two beds and
            // the opening stores do not stretch to a second. Asserted rather than avoided: it
            // is what the opening actually looks like, and it costs nothing today because
            // morale does not exist yet.
            Assert.That(colony.Homeless, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator TheSameColonyWithNobodyWorkingEatsItselfEmpty()
        {
            // The same five days, the same buildings, nobody put to work. The test above only
            // means something if not doing the jobs fails, and this is the shape of that
            // failure: the stores fall every day with nothing coming in to meet them.
            Put(Of(BuildingCategory.Barracks), new Vector2(1f, 5f));
            Put(Of(BuildingCategory.Farm), new Vector2(5f, 5f));
            Put(Well(), new Vector2(8.5f, 1f));
            yield return null;

            int food = _stores.AmountOf(ResourceKind.Food);
            int water = _stores.AmountOf(ResourceKind.Water);

            for (int day = 1; day <= Days; day++)
            {
                _colony.EndDay();
                yield return null;

                Assert.That(_colony.LastReport.Worked, Is.Zero, $"somebody worked on day {day}");
                Assert.That(_colony.LastReport.FoodGrown, Is.Zero, $"the farm ran on day {day}");
            }

            Assert.That(
                _stores.AmountOf(ResourceKind.Food),
                Is.LessThan(food),
                "five days of nobody working cost the colony nothing");
            Assert.That(_stores.AmountOf(ResourceKind.Water), Is.LessThan(water));
        }
    }
}
