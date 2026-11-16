#nullable enable
using System;
using System.IO;
using NUnit.Framework;
using ZombieGame.Core.Saving;

namespace ZombieGame.Tests.EditMode
{
    [TestFixture]
    public sealed class FileSaveStoreTests
    {
        private string _directory = string.Empty;
        private FileSaveStore _store = null!;

        [SetUp]
        public void CreateTempDirectory()
        {
            _directory = Path.Combine(
                Path.GetTempPath(), "ZombieGameSaveTests", Guid.NewGuid().ToString("N"));
            _store = new FileSaveStore(_directory);
        }

        [TearDown]
        public void RemoveTempDirectory()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }

        [Test]
        public void WriteThenRead_ReturnsTheSameText()
        {
            _store.Write("slot1", "hello");

            Assert.That(_store.TryRead("slot1", out string contents), Is.True);
            Assert.That(contents, Is.EqualTo("hello"));
        }

        [Test]
        public void TheDirectoryIsCreatedOnDemand()
        {
            Assert.That(Directory.Exists(_directory), Is.False, "nothing exists before the first write");

            _store.Write("slot1", "hello");

            Assert.That(Directory.Exists(_directory), Is.True);
        }

        [Test]
        public void Exists_IsFalseBeforeAndTrueAfterWriting()
        {
            Assert.That(_store.Exists("slot1"), Is.False);

            _store.Write("slot1", "hello");

            Assert.That(_store.Exists("slot1"), Is.True);
        }

        [Test]
        public void TryRead_ReportsAMissingSlot()
        {
            Assert.That(_store.TryRead("slot1", out string contents), Is.False);
            Assert.That(contents, Is.Empty);
        }

        [Test]
        public void WritingTwiceReplacesTheContent()
        {
            _store.Write("slot1", "first");
            _store.Write("slot1", "second");

            _store.TryRead("slot1", out string contents);
            Assert.That(contents, Is.EqualTo("second"));
        }

        [Test]
        public void NoTemporaryFileIsLeftBehind()
        {
            _store.Write("slot1", "first");
            _store.Write("slot1", "second");

            Assert.That(
                Directory.GetFiles(_directory, "*.tmp"),
                Is.Empty,
                "the staged write must be moved into place, not left lying around");
        }

        [Test]
        public void ListSlots_NamesEverySavedSlot()
        {
            _store.Write("alpha", "a");
            _store.Write("beta", "b");

            Assert.That(_store.ListSlots(), Is.EquivalentTo(new[] { "alpha", "beta" }));
        }

        [Test]
        public void ListSlots_IsEmptyWhenNothingWasSavedYet()
        {
            Assert.That(_store.ListSlots(), Is.Empty);
        }

        [Test]
        public void Delete_RemovesTheSlotAndReportsWhetherThereWasOne()
        {
            _store.Write("slot1", "hello");

            Assert.That(_store.Delete("slot1"), Is.True);
            Assert.That(_store.Exists("slot1"), Is.False);
            Assert.That(_store.Delete("slot1"), Is.False);
        }

        [Test]
        public void SlotNamesThatCouldEscapeTheFolderAreRefused()
        {
            Assert.Throws<ArgumentException>(() => _store.Write("../escape", "x"));
            Assert.Throws<ArgumentException>(() => _store.Write("sub/slot", "x"));
            Assert.Throws<ArgumentException>(() => _store.Write("..", "x"));
            Assert.Throws<ArgumentException>(() => _store.Write(" ", "x"));
        }

        [Test]
        public void ReadingAnInvalidSlotNameFailsQuietly()
        {
            Assert.That(_store.TryRead("../escape", out _), Is.False);
            Assert.That(_store.Delete("../escape"), Is.False);
            Assert.That(_store.Exists("../escape"), Is.False);
        }

        [Test]
        public void Constructor_RejectsAnEmptyDirectory()
        {
            Assert.Throws<ArgumentException>(() => new FileSaveStore(" "));
        }

        [Test]
        public void ASaveSystemCanRunOnTopOfTheFileStore()
        {
            var serializer = new UnityJsonSerializer(prettyPrint: true);
            var system = new SaveSystem(serializer, _store);
            var file = new SaveFile { saveVersion = SaveSystem.DefaultSaveVersion };

            _store.Write("slot1", serializer.Serialize(file));

            Assert.That(system.SlotExists("slot1"), Is.True);
            Assert.That(system.Load("slot1").IsSuccess, Is.True);
        }
    }
}
