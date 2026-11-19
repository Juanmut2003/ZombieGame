#nullable enable

namespace ZombieGame.Combat
{
    /// <summary>Something whose visibility changes with what it is doing.</summary>
    /// <remarks>
    /// The horde lives in <c>ZombieGame.AI</c> and the character it is watching lives in
    /// <c>ZombieGame.Characters</c>, which already depends on AI. The contract therefore belongs
    /// in the layer both already share, exactly as <see cref="IHurtable"/> does.
    /// </remarks>
    public interface IConcealable
    {
        /// <summary>
        /// How easily this is seen, against an onlooker's full view distance. One is standing up
        /// in the open; lower is harder to spot.
        /// </summary>
        float Visibility { get; }
    }

    /// <summary>Kills an unaware zombie quietly, if there is one to kill.</summary>
    /// <remarks>
    /// Asked of the service locator rather than of the horde, the same way shooting is: the
    /// character does not know what it is sneaking up on, only that something is registered to
    /// answer for it.
    /// </remarks>
    public interface ISilentKill
    {
        /// <summary>
        /// Takes down whatever can be crept up on from here.
        /// </summary>
        /// <param name="from">Where the attacker is standing.</param>
        /// <param name="reach">How far the attacker can reach.</param>
        /// <param name="behindDegrees">How far round the back counts as behind.</param>
        /// <param name="where">Where it happened, when it did.</param>
        /// <returns>False when there was nobody unaware within reach.</returns>
        bool TryTakedown(float reach, float behindDegrees, UnityEngine.Vector2 from, out UnityEngine.Vector2 where);

        /// <summary>
        /// Whether there is anybody to creep up on from here, without doing it.
        /// </summary>
        /// <remarks>
        /// Asked every frame so the HUD can say when the move is on. Without it the rules are
        /// invisible: the player presses the key, nothing happens, and there is no way to tell
        /// whether they were too far away, not far enough round the back, or had already been
        /// noticed.
        /// </remarks>
        bool CanTakedown(float reach, float behindDegrees, UnityEngine.Vector2 from);
    }
}
