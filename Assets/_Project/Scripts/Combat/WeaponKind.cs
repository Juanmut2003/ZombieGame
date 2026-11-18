#nullable enable

namespace ZombieGame.Combat
{
    /// <summary>How a weapon reaches its target.</summary>
    /// <remarks>
    /// The split that matters to the player is not "bat or pistol" but "quiet and free" against
    /// "loud and finite". One enum on the asset decides which half of the code picks it up, so a
    /// new weapon stays an asset rather than a class.
    /// </remarks>
    public enum WeaponKind
    {
        /// <summary>Swung. Costs stamina, carries barely any noise, never runs out.</summary>
        Melee = 0,

        /// <summary>Fired. Costs ammunition, carries a long way, and the zone hears all of it.</summary>
        Firearm = 1,
    }
}
