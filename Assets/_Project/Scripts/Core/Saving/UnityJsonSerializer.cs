#nullable enable
using System;
using UnityEngine;

namespace ZombieGame.Core.Saving
{
    /// <summary>
    /// The production serialiser, backed by <see cref="JsonUtility"/>.
    /// </summary>
    /// <remarks>
    /// JsonUtility is fast and allocation-light but picky: DTOs must be <c>[Serializable]</c>
    /// classes with public fields, and it cannot do dictionaries, interfaces, polymorphism or
    /// nullable value types. That is a feature here — it keeps save DTOs flat and dumb.
    /// </remarks>
    public sealed class UnityJsonSerializer : ISaveSerializer
    {
        private readonly bool _prettyPrint;

        /// <param name="prettyPrint">Readable output. Handy while developing, bigger on disk.</param>
        public UnityJsonSerializer(bool prettyPrint = false) => _prettyPrint = prettyPrint;

        /// <inheritdoc />
        public string Serialize(object state)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return JsonUtility.ToJson(state, _prettyPrint);
        }

        /// <inheritdoc />
        public object? Deserialize(string json, Type type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return JsonUtility.FromJson(json, type);
            }
            catch (ArgumentException)
            {
                // Malformed JSON. The caller decides what a missing slice means.
                return null;
            }
        }
    }
}
