#nullable enable
using System;
using System.Collections.Generic;

namespace ZombieGame.Core.Saving
{
    /// <summary>
    /// A store that keeps saves in memory. Used by tests, and useful for a run that must not
    /// touch the disk.
    /// </summary>
    public sealed class InMemorySaveStore : ISaveStore
    {
        private readonly Dictionary<string, string> _slots = new(StringComparer.Ordinal);

        /// <inheritdoc />
        public IReadOnlyList<string> ListSlots() => new List<string>(_slots.Keys);

        /// <inheritdoc />
        public bool Exists(string slot) => _slots.ContainsKey(slot);

        /// <inheritdoc />
        public void Write(string slot, string contents)
        {
            if (string.IsNullOrEmpty(slot))
            {
                throw new ArgumentException("A slot name is required.", nameof(slot));
            }

            _slots[slot] = contents ?? throw new ArgumentNullException(nameof(contents));
        }

        /// <inheritdoc />
        public bool TryRead(string slot, out string contents)
        {
            if (!string.IsNullOrEmpty(slot) && _slots.TryGetValue(slot, out string? found))
            {
                contents = found;
                return true;
            }

            contents = string.Empty;
            return false;
        }

        /// <inheritdoc />
        public bool Delete(string slot) => !string.IsNullOrEmpty(slot) && _slots.Remove(slot);
    }
}
