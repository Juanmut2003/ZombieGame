#nullable enable
using System.Threading;
using UnityEngine;

namespace ZombieGame.Core
{
    /// <summary>
    /// Hands out the ids that events use to say who did something to whom.
    /// </summary>
    /// <remarks>
    /// Deliberately not Unity's object identity. <c>GetInstanceID</c> is retired in Unity 6.6 and
    /// its replacement is explicitly not going to stay an integer; neither was ever stable across
    /// a save and a reload anyway. An id handed out here means the same thing for as long as the
    /// session lasts, which is all an event needs.
    /// Ids run negative so they cannot collide with the positive ones the zombie manager hands
    /// out for its own array — a swing has to be able to tell the swinger from the swung-at.
    /// </remarks>
    public static class ActorIds
    {
        private static int _next;

        /// <summary>A fresh id, unique within this session.</summary>
        public static int Next() => -Interlocked.Increment(ref _next);

        /// <summary>
        /// Starts counting again. Called before play so a disabled domain reload cannot carry ids
        /// over from the previous session.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset() => Interlocked.Exchange(ref _next, 0);
    }
}
