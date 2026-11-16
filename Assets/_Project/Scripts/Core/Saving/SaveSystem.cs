#nullable enable
using System;
using System.Collections.Generic;

namespace ZombieGame.Core.Saving
{
    /// <summary>
    /// Collects a snapshot from every registered system, writes it as one versioned file, and puts
    /// it back on load — migrating older files forward on the way in.
    /// </summary>
    /// <remarks>
    /// The rule this class exists to enforce: <b>an old save never stops loading</b>. Raising
    /// <see cref="CurrentSaveVersion"/> without shipping the matching <see cref="ISaveMigration"/>
    /// makes every older file fail with <see cref="LoadStatus.MigrationMissing"/> instead of
    /// silently losing data — loud, on purpose.
    /// </remarks>
    public sealed class SaveSystem
    {
        /// <summary>
        /// The format version this build writes. Raise it whenever the shape of any DTO changes
        /// incompatibly, and add an <see cref="ISaveMigration"/> from the previous number.
        /// </summary>
        public const int DefaultSaveVersion = 1;

        private readonly ISaveSerializer _serializer;
        private readonly ISaveStore _store;
        private readonly Dictionary<int, ISaveMigration> _migrations = new();
        private readonly Dictionary<string, ISaveable> _saveables = new(StringComparer.Ordinal);
        private readonly string _gameVersion;

        /// <param name="serializer">Turns DTOs into text.</param>
        /// <param name="store">Where the text goes.</param>
        /// <param name="migrations">One per version step; order does not matter.</param>
        /// <param name="gameVersion">Recorded in the file for diagnostics.</param>
        /// <param name="saveVersion">
        /// The version this instance writes. Defaults to <see cref="DefaultSaveVersion"/>, which is
        /// what the game uses; tests pass a higher number to exercise the migration chain.
        /// </param>
        public SaveSystem(
            ISaveSerializer serializer,
            ISaveStore store,
            IEnumerable<ISaveMigration>? migrations = null,
            string gameVersion = "",
            int saveVersion = DefaultSaveVersion)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _gameVersion = gameVersion;

            if (saveVersion < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(saveVersion), saveVersion, "Save versions start at 1.");
            }

            CurrentSaveVersion = saveVersion;

            if (migrations is null)
            {
                return;
            }

            foreach (ISaveMigration migration in migrations)
            {
                if (migration is null)
                {
                    throw new ArgumentException("A migration is null.", nameof(migrations));
                }

                if (_migrations.ContainsKey(migration.FromVersion))
                {
                    throw new ArgumentException(
                        $"Two migrations both claim to read save version {migration.FromVersion}.",
                        nameof(migrations));
                }

                _migrations.Add(migration.FromVersion, migration);
            }
        }

        /// <summary>The save version this instance writes and migrates towards.</summary>
        public int CurrentSaveVersion { get; }

        /// <summary>How many systems currently take part in saving.</summary>
        public int RegisteredCount => _saveables.Count;

        /// <summary>Adds a system to the save file.</summary>
        /// <exception cref="InvalidOperationException">
        /// Another system already uses that save key. Two systems sharing a key would overwrite
        /// each other's data, so this fails rather than guessing.
        /// </exception>
        public void Register(ISaveable saveable)
        {
            if (saveable is null)
            {
                throw new ArgumentNullException(nameof(saveable));
            }

            if (string.IsNullOrWhiteSpace(saveable.SaveKey))
            {
                throw new ArgumentException("A saveable needs a non-empty save key.", nameof(saveable));
            }

            if (saveable.StateType is null)
            {
                throw new ArgumentException("A saveable must declare its state type.", nameof(saveable));
            }

            if (_saveables.TryGetValue(saveable.SaveKey, out ISaveable? existing))
            {
                throw new InvalidOperationException(
                    $"Save key '{saveable.SaveKey}' is already taken by {existing.GetType().Name}.");
            }

            _saveables.Add(saveable.SaveKey, saveable);
        }

        /// <summary>Removes a system again. Returns false when it was not registered.</summary>
        public bool Unregister(ISaveable saveable)
        {
            if (saveable is null)
            {
                throw new ArgumentNullException(nameof(saveable));
            }

            return _saveables.Remove(saveable.SaveKey);
        }

        /// <summary>Forgets every registered system. Used when tearing down a session.</summary>
        public void Clear() => _saveables.Clear();

        /// <summary>True when that slot holds a save.</summary>
        public bool SlotExists(string slot) => _store.Exists(slot);

        /// <summary>Every slot that holds a save.</summary>
        public IReadOnlyList<string> ListSlots() => _store.ListSlots();

        /// <summary>Deletes a save. Returns false when there was nothing there.</summary>
        public bool DeleteSlot(string slot) => _store.Delete(slot);

        /// <summary>Snapshots every registered system and writes the slot.</summary>
        public void Save(string slot)
        {
            if (string.IsNullOrWhiteSpace(slot))
            {
                throw new ArgumentException("A slot name is required.", nameof(slot));
            }

            var file = new SaveFile
            {
                saveVersion = CurrentSaveVersion,
                createdUtc = DateTime.UtcNow.ToString("o"),
                gameVersion = _gameVersion,
            };

            foreach (KeyValuePair<string, ISaveable> pair in _saveables)
            {
                object state = pair.Value.CaptureState();
                if (state is null)
                {
                    throw new InvalidOperationException(
                        $"System '{pair.Key}' captured a null state. Return an empty DTO instead.");
                }

                file.entries.Add(new SaveEntry(pair.Key, _serializer.Serialize(state)));
            }

            _store.Write(slot, _serializer.Serialize(file));
        }

        /// <summary>
        /// Reads a slot, migrates it to the current version if needed, and hands each system its
        /// slice back.
        /// </summary>
        public LoadResult Load(string slot)
        {
            if (string.IsNullOrWhiteSpace(slot))
            {
                throw new ArgumentException("A slot name is required.", nameof(slot));
            }

            if (!_store.TryRead(slot, out string contents))
            {
                return LoadResult.Failure(LoadStatus.SlotNotFound);
            }

            if (_serializer.Deserialize(contents, typeof(SaveFile)) is not SaveFile file)
            {
                return LoadResult.Failure(LoadStatus.Corrupt);
            }

            int fileVersion = file.saveVersion;
            if (fileVersion <= 0)
            {
                return LoadResult.Failure(LoadStatus.Corrupt, fileVersion);
            }

            if (fileVersion > CurrentSaveVersion)
            {
                return LoadResult.Failure(LoadStatus.FromNewerVersion, fileVersion);
            }

            if (!TryMigrate(file, out SaveFile migrated))
            {
                return LoadResult.Failure(LoadStatus.MigrationMissing, fileVersion);
            }

            return Restore(migrated, fileVersion);
        }

        private bool TryMigrate(SaveFile file, out SaveFile migrated)
        {
            migrated = file;

            while (migrated.saveVersion < CurrentSaveVersion)
            {
                if (!_migrations.TryGetValue(migrated.saveVersion, out ISaveMigration? migration))
                {
                    return false;
                }

                int before = migrated.saveVersion;
                migrated = migration.Migrate(migrated)
                           ?? throw new InvalidOperationException(
                               $"Migration from version {before} returned null.");

                // The migration owns the data, the save system owns the version number — so a
                // migration that forgets to bump it cannot spin us in a loop.
                migrated.saveVersion = before + 1;
            }

            return true;
        }

        private LoadResult Restore(SaveFile file, int fileVersion)
        {
            var restored = new List<string>();
            var unknown = new List<string>();
            var missing = new List<string>();
            var failed = new List<string>();

            foreach (SaveEntry entry in file.entries)
            {
                if (!_saveables.ContainsKey(entry.key))
                {
                    unknown.Add(entry.key);
                }
            }

            foreach (KeyValuePair<string, ISaveable> pair in _saveables)
            {
                SaveEntry? entry = file.FindEntry(pair.Key);
                if (entry is null)
                {
                    missing.Add(pair.Key);
                    continue;
                }

                object? state = _serializer.Deserialize(entry.json, pair.Value.StateType);
                if (state is null)
                {
                    // One damaged slice must not cost the player the whole run.
                    failed.Add(pair.Key);
                    continue;
                }

                pair.Value.RestoreState(state);
                restored.Add(pair.Key);
            }

            return LoadResult.Success(fileVersion, restored, unknown, missing, failed);
        }
    }
}
