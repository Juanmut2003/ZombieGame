#nullable enable
using System;

namespace ZombieGame.Core.Saving
{
    /// <summary>
    /// Implemented by every system that survives a restart. The system hands out a plain
    /// serializable DTO and takes one back; it never touches files, JSON or versions itself.
    /// </summary>
    public interface ISaveable
    {
        /// <summary>
        /// Stable identifier of this system inside the save file, for example "colony" or
        /// "worldMap". Never rename one without a migration — it is the key the data hangs on.
        /// </summary>
        string SaveKey { get; }

        /// <summary>The DTO type <see cref="CaptureState"/> returns and <see cref="RestoreState"/> expects.</summary>
        Type StateType { get; }

        /// <summary>Builds a snapshot of everything this system needs to come back.</summary>
        object CaptureState();

        /// <summary>
        /// Applies a snapshot. The state is guaranteed to be of <see cref="StateType"/> and to have
        /// been migrated to the current save version before it arrives here.
        /// </summary>
        void RestoreState(object state);
    }
}
