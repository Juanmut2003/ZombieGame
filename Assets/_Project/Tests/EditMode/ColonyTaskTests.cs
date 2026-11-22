#nullable enable
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ZombieGame.Base;
using ZombieGame.Economy;

namespace ZombieGame.Tests.EditMode
{
    /// <summary>
    /// The orders a new colony is given, and what ticks them.
    /// </summary>
    /// <remarks>
    /// These are the first four lines of text a player reads in the game, and the only
    /// instructions they get. A line that ticks when it should not teaches the wrong thing about
    /// the base; one that never ticks leaves a player looking for a building they have already
    /// built.
    /// </remarks>
    [TestFixture]
    public sealed class ColonyTaskTests
    {
        private static BuildingDefinition Building(BuildingCategory kind, int posts = 2)
        {
            var building = ScriptableObject.CreateInstance<BuildingDefinition>();
            var serialized = new UnityEditor.SerializedObject(building);
            serialized.FindProperty("_displayName").stringValue = kind.ToString();
            serialized.FindProperty("_category").enumValueIndex = (int)kind;

            UnityEditor.SerializedProperty levels = serialized.FindProperty("_levels");
            levels.arraySize = 1;
            UnityEditor.SerializedProperty rung = levels.GetArrayElementAtIndex(0);
            rung.FindPropertyRelative("_width").intValue = 2;
            rung.FindPropertyRelative("_height").intValue = 2;
            rung.FindPropertyRelative("_workPosts").intValue = posts;
            rung.FindPropertyRelative("_beds").intValue = 4;

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return building;
        }

        private static ColonyTask Line(IReadOnlyList<ColonyTask> tasks, int index) => tasks[index];

        [Test]
        public void AnEmptyPlotHasEverythingStillToDo()
        {
            var site = new BuildSite(Vector2.zero, 10, 10);
            IReadOnlyList<ColonyTask> tasks =
                ColonyTasks.Describe(site, new Colony(site), run: null, haulWanted: 10);

            Assert.That(tasks.Count, Is.EqualTo(ColonyTasks.Count));
            Assert.That(ColonyTasks.AllDone(tasks), Is.False);
            foreach (ColonyTask task in tasks)
            {
                Assert.That(task.Done, Is.False, $"'{task.Title}' starts ticked");
                Assert.That(task.Title, Is.Not.Empty);
            }
        }

        [Test]
        public void ABarracksTicksTheBarracksLineAndNothingElse()
        {
            var site = new BuildSite(Vector2.zero, 10, 10);
            site.TryPlace(
                new BuildingFootprint(2, 2),
                Vector2Int.zero,
                Building(BuildingCategory.Barracks),
                out _);

            IReadOnlyList<ColonyTask> tasks =
                ColonyTasks.Describe(site, new Colony(site), run: null, haulWanted: 10);

            Assert.That(Line(tasks, 0).Done, Is.True, "the barracks line did not tick");
            Assert.That(Line(tasks, 1).Done, Is.False, "a barracks is not a farm");
        }

        [Test]
        public void TearingTheFarmDownUnticksItsLine()
        {
            var site = new BuildSite(Vector2.zero, 10, 10);
            site.TryPlace(
                new BuildingFootprint(2, 2),
                Vector2Int.zero,
                Building(BuildingCategory.Farm),
                out int id);

            Assert.That(
                Line(ColonyTasks.Describe(site, null, null, 10), 1).Done,
                Is.True);

            site.Remove(id);

            Assert.That(
                Line(ColonyTasks.Describe(site, null, null, 10), 1).Done,
                Is.False,
                "the list remembers a farm that is no longer there");
        }

        [Test]
        public void SomebodyWorkingTicksTheWorkLineAndSaysHowMany()
        {
            var site = new BuildSite(Vector2.zero, 10, 10);
            site.TryPlace(
                new BuildingFootprint(2, 2),
                Vector2Int.zero,
                Building(BuildingCategory.Farm),
                out int id);

            var colony = new Colony(site);
            Survivor rae = colony.AddFounder("Rae");
            colony.AddFounder("Osei");

            Assert.That(Line(ColonyTasks.Describe(site, colony, null, 10), 2).Done, Is.False);

            Assert.That(colony.Assign(rae.Id, id), Is.True);

            ColonyTask work = Line(ColonyTasks.Describe(site, colony, null, 10), 2);
            Assert.That(work.Done, Is.True, "nobody counted as working");
            Assert.That(work.Detail, Does.Contain("1"));
        }

        [Test]
        public void TheRaidLineCountsWhatCameHomeOverTheWholeRun()
        {
            var site = new BuildSite(Vector2.zero, 10, 10);
            var run = new ColonyRun(new ColonyLedger());

            ColonyTask raid = Line(ColonyTasks.Describe(site, null, run, 10), 3);
            Assert.That(raid.Done, Is.False);
            Assert.That(raid.Detail, Is.EqualTo("0 / 10"));

            run.Bank(6);
            Assert.That(Line(ColonyTasks.Describe(site, null, run, 10), 3).Done, Is.False);

            run.Bank(4);
            raid = Line(ColonyTasks.Describe(site, null, run, 10), 3);
            Assert.That(raid.Done, Is.True, "two trips of five should count");
            Assert.That(raid.Detail, Is.EqualTo("10 / 10"));
        }

        [Test]
        public void AColonyThatHasDoneItAllSaysSo()
        {
            var site = new BuildSite(Vector2.zero, 10, 10);
            site.TryPlace(
                new BuildingFootprint(2, 2),
                Vector2Int.zero,
                Building(BuildingCategory.Barracks),
                out _);
            site.TryPlace(
                new BuildingFootprint(2, 2),
                new Vector2Int(4, 4),
                Building(BuildingCategory.Farm),
                out int farm);

            var colony = new Colony(site);
            Survivor rae = colony.AddFounder("Rae");
            colony.Assign(rae.Id, farm);

            var run = new ColonyRun(new ColonyLedger());
            run.Bank(10);

            Assert.That(
                ColonyTasks.AllDone(ColonyTasks.Describe(site, colony, run, 10)),
                Is.True);
        }

        [Test]
        public void AskingForNothingIsAlreadyDone()
        {
            var site = new BuildSite(Vector2.zero, 10, 10);
            ColonyTask raid = Line(ColonyTasks.Describe(site, null, null, 0), 3);

            Assert.That(raid.Done, Is.True, "a haul of nothing cannot be outstanding");
        }
    }
}
