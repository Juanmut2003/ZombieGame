#nullable enable
using System;
using System.Collections.Generic;

namespace ZombieGame.Core.Saving
{
    /// <summary>
    /// The envelope every save is wrapped in: a version, some provenance, and one entry per
    /// system. The envelope's own shape must stay readable by every future version of the game —
    /// it is what tells us which migrations to run, so it can never need migrating itself.
    /// </summary>
    /// <remarks>
    /// Fields are public and camelCase because they are the JSON keys and because Unity's
    /// JsonUtility only serialises fields. This is the one corner of the project where the naming
    /// rules bend; everything reading this data does so through <see cref="SaveSystem"/>.
    /// </remarks>
    [Serializable]
    public sealed class SaveFile
    {
        /// <summary>Format version of this file. See <see cref="SaveSystem.CurrentSaveVersion"/>.</summary>
        public int saveVersion;

        /// <summary>When the save was written, ISO 8601 UTC. Shown in the load menu.</summary>
        public string createdUtc = string.Empty;

        /// <summary>Which build wrote it. Diagnostic only — never branch game logic on this.</summary>
        public string gameVersion = string.Empty;

        /// <summary>One entry per registered system, keyed by its save key.</summary>
        public List<SaveEntry> entries = new();

        /// <summary>Finds an entry by key, or null when the file does not carry that system.</summary>
        public SaveEntry? FindEntry(string key)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (string.Equals(entries[i].key, key, StringComparison.Ordinal))
                {
                    return entries[i];
                }
            }

            return null;
        }
    }

    /// <summary>One system's slice of a save file.</summary>
    [Serializable]
    public sealed class SaveEntry
    {
        /// <summary>The owning system's <see cref="ISaveable.SaveKey"/>.</summary>
        public string key = string.Empty;

        /// <summary>The system's DTO, already serialised. Opaque to everything but that system.</summary>
        public string json = string.Empty;

        public SaveEntry()
        {
        }

        public SaveEntry(string key, string json)
        {
            this.key = key;
            this.json = json;
        }
    }
}
