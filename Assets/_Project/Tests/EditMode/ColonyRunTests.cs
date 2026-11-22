#nullable enable
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ZombieGame.Base;
using ZombieGame.Economy;

namespace ZombieGame.Tests.EditMode
{
    /// <summary>
    /// The run that spans the base and a zone: what it remembers, and what it refuses.
    /// </summary>
    /// <remarks>
    /// Every one of these is a bug the player would read as the game losing their colony. A
    /// snapshot that drops a level turns an improved barracks into a hut; one that restores
    /// people before buildings puts the farmhand in the armoury; a trip that can be spent twice
    /// is the one rule PROJECT.md 4.6 asks for, gone. None of it is visible until a raid has
    /// been played, which is exactly why it is settled here.
    /// </remarks>
    [TestFixture]
    public sealed class ColonyRunTests
    {
        /// <summary>A building of a kind, with a ladder of square rungs that grow by one.</summary>
        private static BuildingDefinition Building(
            string name, BuildingCategory kind, int rungs = 3, int posts = 2, int beds = 0)
        {
            var building = ScriptableObject.CreateInstance<BuildingDefinition>();
            var serialized = new UnityEditor.SerializedObject(building);
            serialized.FindProperty("_displayName").stringValue = name;
            serialized.FindProperty("_category").enumValueIndex = (int)kind;

            UnityEditor.SerializedProperty levels = serialized.FindProperty("_levels");
            levels.arraySize = rungs;
            for (int i = 0; i < rungs; i++)
            {
                UnityEditor.SerializedProperty rung = levels.GetArrayElementAtIndex(i);
                rung.FindPropertyRelative("_width").intValue = 2 + i;
                rung.FindPropertyRelative("_height").intValue = 2 + i;
                rung.FindPropertyRelative("_workPosts").intValue = posts;
                rung.FindPropertyRelative("_beds").intValue = beds;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return building;
        }

        private static ColonyRun Run() => new(new ColonyLedger());

        [Test]
        public void ACapturedColonyComesBackWhereItWas()
        {
            BuildingDefinition barracks = Building("Barracks", BuildingCategory.Barracks, beds: 4);
            BuildingDefinition farm = Building("Farm", BuildingCategory.Farm);
            var catalogue = new List<BuildingDefinition> { barracks, farm };

            var before = new BuildSite(Vector2.zero, 12, 12);
            before.TryPlace(new BuildingFootprint(2, 2), new Vector2Int(1, 1), barracks, out _);
            before.TryPlace(new BuildingFootprint(3, 2), new Vector2Int(6, 4), farm, out _);

            ColonyRun run = Run();
            run.Capture(before, colony: null, daysLived: 3);

            var after = new BuildSite(Vector2.zero, 12, 12);
            var colony = new Colony(after);
            run.Restore(after, colony, catalogue);

            Assert.That(after.Buildings.Count, Is.EqualTo(2), "something did not come back");
            Assert.That(after.Buildings[0].Cell, Is.EqualTo(new Vector2Int(1, 1)));
            Assert.That(after.Buildings[0].Definition, Is.SameAs(barracks));
            Assert.That(after.Buildings[1].Cell, Is.EqualTo(new Vector2Int(6, 4)));
            Assert.That(after.Buildings[1].Footprint.Width, Is.EqualTo(3), "it came back a square");
            Assert.That(run.DaysLived, Is.EqualTo(3));
        }

        [Test]
        public void AnImprovedBuildingDoesNotComeBackAsAHut()
        {
            BuildingDefinition barracks = Building("Barracks", BuildingCategory.Barracks, beds: 4);
            var catalogue = new List<BuildingDefinition> { barracks };

            var before = new BuildSite(Vector2.zero, 12, 12);
            before.TryPlace(new BuildingFootprint(2, 2), Vector2Int.zero, barracks, out int id);
            before.TryUpgrade(id, new BuildingFootprint(3, 3));

            ColonyRun run = Run();
            run.Capture(before, colony: null, daysLived: 0);

            var after = new BuildSite(Vector2.zero, 12, 12);
            run.Restore(after, new Colony(after), catalogue);

            Assert.That(after.Buildings[0].Level, Is.EqualTo(2), "the ladder has to be climbed again");
            Assert.That(after.Buildings[0].Footprint.Width, Is.EqualTo(3));

            // And the ground it claims is the ground it grew onto, not the ground it started on.
            Assert.That(after.OccupantAt(2, 2), Is.EqualTo(after.Buildings[0].Id));
        }

        [Test]
        public void SomebodyComesBackToTheBuildingTheyWorkedIn()
        {
            BuildingDefinition barracks = Building("Barracks", BuildingCategory.Barracks, beds: 4);
            BuildingDefinition farm = Building("Farm", BuildingCategory.Farm);
            var catalogue = new List<BuildingDefinition> { barracks, farm };

            var before = new BuildSite(Vector2.zero, 12, 12);
            before.TryPlace(new BuildingFootprint(2, 2), Vector2Int.zero, barracks, out _);
            before.TryPlace(new BuildingFootprint(2, 2), new Vector2Int(5, 5), farm, out int farmId);

            var was = new Colony(before);
            Survivor rae = was.AddFounder("Rae");
            was.AddFounder("Osei");
            Assert.That(was.Assign(rae.Id, farmId), Is.True);

            ColonyRun run = Run();
            run.Capture(before, was, daysLived: 1);

            var after = new BuildSite(Vector2.zero, 12, 12);
            var now = new Colony(after);
            run.Restore(after, now, catalogue);

            Assert.That(now.Count, Is.EqualTo(2), "somebody was left behind");
            Assert.That(now.People[0].Name, Is.EqualTo("Rae"));
            Assert.That(now.People[0].Workplace, Is.EqualTo(farmId), "Rae lost her job on the way home");
            Assert.That(now.People[1].IsIdle, Is.True);
        }

        [Test]
        public void ABuildingTheCatalogueNoLongerHasIsGoneRatherThanGuessedAt()
        {
            BuildingDefinition farm = Building("Farm", BuildingCategory.Farm);

            var before = new BuildSite(Vector2.zero, 12, 12);
            before.TryPlace(new BuildingFootprint(2, 2), Vector2Int.zero, farm, out _);

            ColonyRun run = Run();
            run.Capture(before, colony: null, daysLived: 0);

            var after = new BuildSite(Vector2.zero, 12, 12);
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Farm"));
            run.Restore(after, new Colony(after), new List<BuildingDefinition>());

            Assert.That(after.Buildings, Is.Empty);
        }

        [Test]
        public void TheSquadGoesOutOnceADay()
        {
            ColonyRun run = Run();

            Assert.That(run.CanRaid, Is.True, "a new colony cannot leave the plot");
            Assert.That(run.TrySpendTheDaysRaid(), Is.True);
            Assert.That(run.CanRaid, Is.False, "they went out twice on day one");
            Assert.That(run.TrySpendTheDaysRaid(), Is.False);

            run.DayPassed();

            Assert.That(run.CanRaid, Is.True, "a new day did not give the trip back");
            Assert.That(run.TrySpendTheDaysRaid(), Is.True);
        }

        [Test]
        public void AHaulLandsInTheStoresAndIsCountedOverTheWholeRun()
        {
            ColonyRun run = Run();
            int opening = run.Stores.AmountOf(ResourceKind.Materials);

            run.Bank(6);
            run.Bank(4);

            Assert.That(run.Hauled, Is.EqualTo(10), "the objective will never tick");
            Assert.That(
                run.Stores.AmountOf(ResourceKind.Materials), Is.EqualTo(opening + 10));
        }

        [Test]
        public void ARunThatCameBackWithNothingChangesNothing()
        {
            ColonyRun run = Run();
            run.Bank(0);
            run.Bank(-5);

            Assert.That(run.Hauled, Is.Zero);
            Assert.That(run.Stores.AmountOf(ResourceKind.Materials), Is.Zero);
        }

        [Test]
        public void ACapturedColonyReplacesTheLastOneRatherThanPilingUp()
        {
            BuildingDefinition farm = Building("Farm", BuildingCategory.Farm);

            var site = new BuildSite(Vector2.zero, 12, 12);
            site.TryPlace(new BuildingFootprint(2, 2), Vector2Int.zero, farm, out _);

            ColonyRun run = Run();
            run.Capture(site, colony: null, daysLived: 0);
            run.Capture(site, colony: null, daysLived: 0);

            Assert.That(run.Buildings.Count, Is.EqualTo(1), "the plot doubled on the way home");
        }
    }
}
