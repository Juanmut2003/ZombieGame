#nullable enable

namespace ZombieGame.AI
{
    /// <summary>What a zombie is currently doing about the world.</summary>
    /// <remarks>
    /// Four states, because each one is a different thing to the player. Standing and shuffling
    /// are both "has not noticed you" and are safe to walk past; investigating is "something is
    /// wrong over there", which is a chance to leave; chasing is a fight you chose or failed to
    /// avoid. Any more states than this and the player cannot read them off a sprite and a
    /// direction of travel, which would make stealth guesswork.
    /// </remarks>
    public enum ZombieBehaviour
    {
        /// <summary>Standing about. The default of an undisturbed zone.</summary>
        Resting = 0,

        /// <summary>Shuffling somewhere near where it started, slowly.</summary>
        Wandering = 1,

        /// <summary>Walking to where it last saw or heard something.</summary>
        Investigating = 2,

        /// <summary>Coming for you.</summary>
        Chasing = 3,
    }
}
