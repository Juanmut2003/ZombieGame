#nullable enable
using System;
using System.Collections.Generic;
using System.IO;

namespace ZombieGame.Core.Saving
{
    /// <summary>
    /// Saves as files in a folder, one file per slot.
    /// </summary>
    /// <remarks>
    /// Writes go to a temporary file first and are then moved into place, so a crash or a pulled
    /// plug mid-write leaves the previous save intact instead of a half-written one. Losing a
    /// 20-hour run to a power cut is not a bug we are willing to ship.
    /// </remarks>
    public sealed class FileSaveStore : ISaveStore
    {
        private const string SaveExtension = ".json";
        private const string TempExtension = ".tmp";
        private const string BackupExtension = ".bak";

        private readonly string _directory;

        /// <param name="directory">
        /// Folder to keep saves in; created on demand. In the game this is
        /// <c>Application.persistentDataPath</c> plus a subfolder, passed in rather than read here
        /// so this class stays testable.
        /// </param>
        public FileSaveStore(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new ArgumentException("A directory is required.", nameof(directory));
            }

            _directory = directory;
        }

        /// <inheritdoc />
        public IReadOnlyList<string> ListSlots()
        {
            var slots = new List<string>();
            if (!Directory.Exists(_directory))
            {
                return slots;
            }

            foreach (string path in Directory.GetFiles(_directory, "*" + SaveExtension))
            {
                slots.Add(Path.GetFileNameWithoutExtension(path));
            }

            return slots;
        }

        /// <inheritdoc />
        public bool Exists(string slot) => IsValidSlot(slot) && File.Exists(PathFor(slot));

        /// <inheritdoc />
        public void Write(string slot, string contents)
        {
            if (!IsValidSlot(slot))
            {
                throw new ArgumentException($"'{slot}' is not a usable slot name.", nameof(slot));
            }

            if (contents is null)
            {
                throw new ArgumentNullException(nameof(contents));
            }

            Directory.CreateDirectory(_directory);

            string target = PathFor(slot);
            string temp = target + TempExtension;
            File.WriteAllText(temp, contents);

            if (File.Exists(target))
            {
                // Replace keeps a backup copy around until the swap succeeded.
                File.Replace(temp, target, target + BackupExtension, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(temp, target);
            }
        }

        /// <inheritdoc />
        public bool TryRead(string slot, out string contents)
        {
            contents = string.Empty;
            if (!IsValidSlot(slot))
            {
                return false;
            }

            string path = PathFor(slot);
            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                contents = File.ReadAllText(path);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        /// <inheritdoc />
        public bool Delete(string slot)
        {
            if (!IsValidSlot(slot))
            {
                return false;
            }

            string path = PathFor(slot);
            if (!File.Exists(path))
            {
                return false;
            }

            File.Delete(path);
            return true;
        }

        private string PathFor(string slot) => Path.Combine(_directory, slot + SaveExtension);

        /// <summary>
        /// Slot names become file names, so anything that could escape the folder or upset the
        /// filesystem is rejected rather than sanitised — silently renaming someone's save is worse.
        /// </summary>
        private static bool IsValidSlot(string slot) =>
            !string.IsNullOrWhiteSpace(slot)
            && slot.IndexOfAny(Path.GetInvalidFileNameChars()) < 0
            && slot != "." && slot != "..";
    }
}
