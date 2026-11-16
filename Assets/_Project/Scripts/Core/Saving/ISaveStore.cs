#nullable enable
using System.Collections.Generic;

namespace ZombieGame.Core.Saving
{
    /// <summary>
    /// Where save text actually lives. The game uses a folder on disk; tests use memory.
    /// </summary>
    public interface ISaveStore
    {
        /// <summary>Every slot that currently holds a save, in no particular order.</summary>
        IReadOnlyList<string> ListSlots();

        /// <summary>True when that slot holds a save.</summary>
        bool Exists(string slot);

        /// <summary>Writes a slot, replacing whatever was there.</summary>
        void Write(string slot, string contents);

        /// <summary>Reads a slot. Returns false when it does not exist or cannot be read.</summary>
        bool TryRead(string slot, out string contents);

        /// <summary>Deletes a slot. Returns false when there was nothing to delete.</summary>
        bool Delete(string slot);
    }
}
