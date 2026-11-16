#nullable enable
using System;

namespace ZombieGame.Core.Saving
{
    /// <summary>
    /// Turns DTOs into text and back. Sits behind an interface so the save pipeline can be tested
    /// without a real serialiser, and so the format could be swapped without touching the systems.
    /// </summary>
    public interface ISaveSerializer
    {
        /// <summary>Serialises a DTO.</summary>
        string Serialize(object state);

        /// <summary>
        /// Deserialises text into <paramref name="type"/>. Returns null when the text cannot be
        /// read as that type, rather than throwing — a corrupt entry must not take down the load.
        /// </summary>
        object? Deserialize(string json, Type type);
    }
}
