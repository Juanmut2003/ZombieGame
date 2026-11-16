#nullable enable

namespace ZombieGame.Core.Saving
{
    /// <summary>
    /// Lifts a save file one version forward. Every bump of
    /// <see cref="SaveSystem.CurrentSaveVersion"/> ships with the migration that gets old files
    /// there — that is the whole deal that keeps players' runs alive across updates.
    /// </summary>
    /// <remarks>
    /// Migrations work on the envelope and on raw entry JSON, never on live DTOs: the C# type a
    /// save was written from may not exist in this shape any more, which is precisely why the
    /// migration is needed.
    /// </remarks>
    public interface ISaveMigration
    {
        /// <summary>The version this migration reads. It always produces <c>FromVersion + 1</c>.</summary>
        int FromVersion { get; }

        /// <summary>
        /// Rewrites the file. The instance passed in may be modified and returned; the save system
        /// sets the new version number itself afterwards.
        /// </summary>
        SaveFile Migrate(SaveFile file);
    }
}
