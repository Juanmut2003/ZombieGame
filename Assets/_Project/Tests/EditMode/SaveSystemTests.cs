#nullable enable
using System;
using System.Collections.Generic;
using NUnit.Framework;
using ZombieGame.Core.Saving;

namespace ZombieGame.Tests.EditMode
{
    [TestFixture]
    public sealed class SaveSystemTests
    {
        /// <summary>A save DTO as the real systems will write them: flat, public fields, no logic.</summary>
        [Serializable]
        private sealed class ColonyState
        {
            public int survivors;
            public string baseName = string.Empty;
        }

        [Serializable]
        private sealed class WorldState
        {
            public int day;
        }

        private sealed class FakeColony : ISaveable
        {
            public FakeColony(string key = "colony") => SaveKey = key;

            public string SaveKey { get; }

            public Type StateType => typeof(ColonyState);

            public ColonyState State { get; set; } = new();

            public int RestoreCount { get; private set; }

            public object CaptureState() => State;

            public void RestoreState(object state)
            {
                State = (ColonyState)state;
                RestoreCount++;
            }
        }

        private sealed class FakeWorld : ISaveable
        {
            public string SaveKey => "world";

            public Type StateType => typeof(WorldState);

            public WorldState State { get; set; } = new();

            public object CaptureState() => State;

            public void RestoreState(object state) => State = (WorldState)state;
        }

        /// <summary>A saveable that hands out null, which is a bug in the system, not in the save.</summary>
        private sealed class NullCapturing : ISaveable
        {
            public string SaveKey => "broken";

            public Type StateType => typeof(ColonyState);

            public object CaptureState() => null!;

            public void RestoreState(object state)
            {
            }
        }

        /// <summary>Bumps one version and renames the colony entry on the way.</summary>
        private sealed class RenameColonyMigration : ISaveMigration
        {
            public RenameColonyMigration(int fromVersion, string oldKey, string newKey)
            {
                FromVersion = fromVersion;
                OldKey = oldKey;
                NewKey = newKey;
            }

            public int FromVersion { get; }

            public string OldKey { get; }

            public string NewKey { get; }

            public bool WasRun { get; private set; }

            public SaveFile Migrate(SaveFile file)
            {
                WasRun = true;
                foreach (SaveEntry entry in file.entries)
                {
                    if (entry.key == OldKey)
                    {
                        entry.key = NewKey;
                    }
                }

                return file;
            }
        }

        private UnityJsonSerializer _serializer = null!;
        private InMemorySaveStore _store = null!;

        [SetUp]
        public void CreateDependencies()
        {
            _serializer = new UnityJsonSerializer();
            _store = new InMemorySaveStore();
        }

        private SaveSystem NewSystem(
            IEnumerable<ISaveMigration>? migrations = null,
            int saveVersion = SaveSystem.DefaultSaveVersion) =>
            new(_serializer, _store, migrations, "test", saveVersion);

        [Test]
        public void SaveThenLoad_BringsTheStateBack()
        {
            SaveSystem system = NewSystem();
            var colony = new FakeColony { State = new ColonyState { survivors = 7, baseName = "Mill" } };
            system.Register(colony);

            system.Save("slot1");
            colony.State = new ColonyState();
            LoadResult result = system.Load("slot1");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(colony.State.survivors, Is.EqualTo(7));
            Assert.That(colony.State.baseName, Is.EqualTo("Mill"));
            Assert.That(result.RestoredKeys, Is.EquivalentTo(new[] { "colony" }));
        }

        [Test]
        public void EverySystemGetsItsOwnSlice()
        {
            SaveSystem system = NewSystem();
            var colony = new FakeColony { State = new ColonyState { survivors = 3 } };
            var world = new FakeWorld { State = new WorldState { day = 12 } };
            system.Register(colony);
            system.Register(world);

            system.Save("slot1");
            colony.State = new ColonyState();
            world.State = new WorldState();
            LoadResult result = system.Load("slot1");

            Assert.That(colony.State.survivors, Is.EqualTo(3));
            Assert.That(world.State.day, Is.EqualTo(12));
            Assert.That(result.RestoredKeys, Is.EquivalentTo(new[] { "colony", "world" }));
        }

        [Test]
        public void TheWrittenFileCarriesTheVersion()
        {
            SaveSystem system = NewSystem();
            system.Register(new FakeColony());

            system.Save("slot1");

            Assert.That(_store.TryRead("slot1", out string contents), Is.True);
            var file = (SaveFile)_serializer.Deserialize(contents, typeof(SaveFile))!;
            Assert.That(file.saveVersion, Is.EqualTo(SaveSystem.DefaultSaveVersion));
            Assert.That(file.gameVersion, Is.EqualTo("test"));
            Assert.That(file.createdUtc, Is.Not.Empty);
        }

        [Test]
        public void Register_RejectsADuplicateSaveKey()
        {
            SaveSystem system = NewSystem();
            system.Register(new FakeColony());

            Assert.Throws<InvalidOperationException>(() => system.Register(new FakeColony()));
        }

        [Test]
        public void Register_RejectsNull()
        {
            SaveSystem system = NewSystem();

            Assert.Throws<ArgumentNullException>(() => system.Register(null!));
        }

        [Test]
        public void Unregister_TakesASystemOutOfTheSave()
        {
            SaveSystem system = NewSystem();
            var colony = new FakeColony();
            system.Register(colony);

            Assert.That(system.Unregister(colony), Is.True);
            Assert.That(system.RegisteredCount, Is.Zero);
            Assert.That(system.Unregister(colony), Is.False);
        }

        [Test]
        public void Save_RefusesASystemThatCapturesNull()
        {
            SaveSystem system = NewSystem();
            system.Register(new NullCapturing());

            Assert.Throws<InvalidOperationException>(() => system.Save("slot1"));
        }

        [Test]
        public void Load_ReportsAMissingSlot()
        {
            SaveSystem system = NewSystem();

            LoadResult result = system.Load("nothing-here");

            Assert.That(result.Status, Is.EqualTo(LoadStatus.SlotNotFound));
            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void Load_ReportsGarbageAsCorrupt()
        {
            _store.Write("slot1", "this is not a save file");
            SaveSystem system = NewSystem();

            Assert.That(system.Load("slot1").Status, Is.EqualTo(LoadStatus.Corrupt));
        }

        [Test]
        public void Load_ReportsAFileWithoutAVersionAsCorrupt()
        {
            _store.Write("slot1", "{}");
            SaveSystem system = NewSystem();

            Assert.That(system.Load("slot1").Status, Is.EqualTo(LoadStatus.Corrupt));
        }

        [Test]
        public void Load_RefusesASaveFromANewerBuild()
        {
            SaveSystem future = NewSystem(saveVersion: 5);
            future.Register(new FakeColony());
            future.Save("slot1");

            LoadResult result = NewSystem().Load("slot1");

            Assert.That(result.Status, Is.EqualTo(LoadStatus.FromNewerVersion));
            Assert.That(result.FileVersion, Is.EqualTo(5));
        }

        [Test]
        public void Load_RefusesWhenAMigrationStepIsMissing()
        {
            SaveSystem old = NewSystem(saveVersion: 1);
            old.Register(new FakeColony());
            old.Save("slot1");

            SaveSystem current = NewSystem(saveVersion: 3);
            current.Register(new FakeColony());
            LoadResult result = current.Load("slot1");

            Assert.That(result.Status, Is.EqualTo(LoadStatus.MigrationMissing));
            Assert.That(result.FileVersion, Is.EqualTo(1));
        }

        [Test]
        public void AnOldSaveIsMigratedForwardStepByStep()
        {
            SaveSystem old = NewSystem(saveVersion: 1);
            old.Register(new FakeColony("settlement"));
            old.Save("slot1");

            var first = new RenameColonyMigration(1, "settlement", "outpost");
            var second = new RenameColonyMigration(2, "outpost", "colony");
            SaveSystem current = NewSystem(new ISaveMigration[] { second, first }, saveVersion: 3);
            var colony = new FakeColony();
            current.Register(colony);

            LoadResult result = current.Load("slot1");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(first.WasRun, Is.True, "the 1 -> 2 step must run");
            Assert.That(second.WasRun, Is.True, "the 2 -> 3 step must run");
            Assert.That(result.RestoredKeys, Is.EquivalentTo(new[] { "colony" }));
            Assert.That(result.FileVersion, Is.EqualTo(1), "the result reports the version on disk");
        }

        [Test]
        public void MigratedDataSurvivesTheJourney()
        {
            SaveSystem old = NewSystem(saveVersion: 1);
            old.Register(new FakeColony("settlement")
            {
                State = new ColonyState { survivors = 9, baseName = "Petrol station" },
            });
            old.Save("slot1");

            SaveSystem current = NewSystem(
                new ISaveMigration[] { new RenameColonyMigration(1, "settlement", "colony") },
                saveVersion: 2);
            var colony = new FakeColony();
            current.Register(colony);

            current.Load("slot1");

            Assert.That(colony.State.survivors, Is.EqualTo(9));
            Assert.That(colony.State.baseName, Is.EqualTo("Petrol station"));
        }

        [Test]
        public void Constructor_RejectsTwoMigrationsForTheSameVersion()
        {
            var migrations = new ISaveMigration[]
            {
                new RenameColonyMigration(1, "a", "b"),
                new RenameColonyMigration(1, "c", "d"),
            };

            Assert.Throws<ArgumentException>(() => NewSystem(migrations, saveVersion: 2));
        }

        [Test]
        public void Load_ReportsEntriesNobodyClaims()
        {
            SaveSystem writer = NewSystem();
            writer.Register(new FakeColony());
            writer.Register(new FakeWorld());
            writer.Save("slot1");

            SaveSystem reader = NewSystem();
            reader.Register(new FakeColony());
            LoadResult result = reader.Load("slot1");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.UnknownKeys, Is.EquivalentTo(new[] { "world" }));
        }

        [Test]
        public void Load_ReportsSystemsTheFileHasNothingFor()
        {
            SaveSystem writer = NewSystem();
            writer.Register(new FakeColony());
            writer.Save("slot1");

            SaveSystem reader = NewSystem();
            reader.Register(new FakeColony());
            var world = new FakeWorld { State = new WorldState { day = 4 } };
            reader.Register(world);
            LoadResult result = reader.Load("slot1");

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.MissingKeys, Is.EquivalentTo(new[] { "world" }));
            Assert.That(world.State.day, Is.EqualTo(4), "a system without data keeps what it had");
        }

        [Test]
        public void OneDamagedSliceDoesNotCostTheWholeSave()
        {
            var file = new SaveFile { saveVersion = SaveSystem.DefaultSaveVersion };
            file.entries.Add(new SaveEntry("colony", "###not json###"));
            file.entries.Add(new SaveEntry("world", _serializer.Serialize(new WorldState { day = 8 })));
            _store.Write("slot1", _serializer.Serialize(file));

            SaveSystem system = NewSystem();
            var colony = new FakeColony();
            var world = new FakeWorld();
            system.Register(colony);
            system.Register(world);

            LoadResult result = system.Load("slot1");

            Assert.That(result.FailedKeys, Is.EquivalentTo(new[] { "colony" }));
            Assert.That(result.RestoredKeys, Is.EquivalentTo(new[] { "world" }));
            Assert.That(world.State.day, Is.EqualTo(8), "the healthy slice still loaded");
            Assert.That(colony.RestoreCount, Is.Zero, "a broken slice is never handed over");
        }

        [Test]
        public void SlotsCanBeListedAndDeleted()
        {
            SaveSystem system = NewSystem();
            system.Register(new FakeColony());

            system.Save("slot1");
            system.Save("slot2");

            Assert.That(system.ListSlots(), Is.EquivalentTo(new[] { "slot1", "slot2" }));
            Assert.That(system.SlotExists("slot1"), Is.True);
            Assert.That(system.DeleteSlot("slot1"), Is.True);
            Assert.That(system.SlotExists("slot1"), Is.False);
            Assert.That(system.DeleteSlot("slot1"), Is.False);
        }

        [Test]
        public void SavingTwiceOverwritesTheSlot()
        {
            SaveSystem system = NewSystem();
            var colony = new FakeColony { State = new ColonyState { survivors = 1 } };
            system.Register(colony);
            system.Save("slot1");

            colony.State = new ColonyState { survivors = 2 };
            system.Save("slot1");
            colony.State = new ColonyState();
            system.Load("slot1");

            Assert.That(colony.State.survivors, Is.EqualTo(2));
        }

        [Test]
        public void SaveAndLoad_RejectAnEmptySlotName()
        {
            SaveSystem system = NewSystem();

            Assert.Throws<ArgumentException>(() => system.Save(" "));
            Assert.Throws<ArgumentException>(() => system.Load(string.Empty));
        }

        [Test]
        public void Constructor_RejectsMissingDependencies()
        {
            Assert.Throws<ArgumentNullException>(() => new SaveSystem(null!, _store));
            Assert.Throws<ArgumentNullException>(() => new SaveSystem(_serializer, null!));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new SaveSystem(_serializer, _store, saveVersion: 0));
        }
    }
}
