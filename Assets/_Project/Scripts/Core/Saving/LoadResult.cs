#nullable enable
using System.Collections.Generic;

namespace ZombieGame.Core.Saving
{
    /// <summary>Why a load ended the way it did.</summary>
    public enum LoadStatus
    {
        /// <summary>Everything that could be restored was restored.</summary>
        Success,

        /// <summary>There is no save in that slot.</summary>
        SlotNotFound,

        /// <summary>The file is not a save file, or is damaged beyond the envelope.</summary>
        Corrupt,

        /// <summary>Written by a newer build. Loading it would quietly lose whatever we cannot read.</summary>
        FromNewerVersion,

        /// <summary>An older file needs a migration step that this build does not ship.</summary>
        MigrationMissing,
    }

    /// <summary>
    /// The outcome of a load. Expected trouble — a missing slot, a save from tomorrow's build —
    /// comes back as a result the UI can act on, not as an exception.
    /// </summary>
    public sealed class LoadResult
    {
        private static readonly IReadOnlyList<string> None = new List<string>();

        private LoadResult(
            LoadStatus status,
            int fileVersion,
            IReadOnlyList<string>? restoredKeys = null,
            IReadOnlyList<string>? unknownKeys = null,
            IReadOnlyList<string>? missingKeys = null,
            IReadOnlyList<string>? failedKeys = null)
        {
            Status = status;
            FileVersion = fileVersion;
            RestoredKeys = restoredKeys ?? None;
            UnknownKeys = unknownKeys ?? None;
            MissingKeys = missingKeys ?? None;
            FailedKeys = failedKeys ?? None;
        }

        /// <summary>How the load ended.</summary>
        public LoadStatus Status { get; }

        /// <summary>True only when <see cref="Status"/> is <see cref="LoadStatus.Success"/>.</summary>
        public bool IsSuccess => Status == LoadStatus.Success;

        /// <summary>The version the file was written in, before any migration. −1 when unreadable.</summary>
        public int FileVersion { get; }

        /// <summary>Systems that got their state back.</summary>
        public IReadOnlyList<string> RestoredKeys { get; }

        /// <summary>
        /// Entries in the file that no registered system claimed — usually a system that was
        /// renamed or removed. Their data is dropped on the next save.
        /// </summary>
        public IReadOnlyList<string> UnknownKeys { get; }

        /// <summary>
        /// Registered systems the file had nothing for — normal when loading a save from before
        /// that system existed. Those systems keep whatever they already had.
        /// </summary>
        public IReadOnlyList<string> MissingKeys { get; }

        /// <summary>
        /// Systems whose slice could not be read. The rest of the save still loaded; these are
        /// the ones to warn the player about.
        /// </summary>
        public IReadOnlyList<string> FailedKeys { get; }

        internal static LoadResult Success(
            int fileVersion,
            IReadOnlyList<string> restored,
            IReadOnlyList<string> unknown,
            IReadOnlyList<string> missing,
            IReadOnlyList<string> failed) =>
            new(LoadStatus.Success, fileVersion, restored, unknown, missing, failed);

        internal static LoadResult Failure(LoadStatus status, int fileVersion = -1) =>
            new(status, fileVersion);
    }
}
