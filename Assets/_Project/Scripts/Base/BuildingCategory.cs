#nullable enable

namespace ZombieGame.Base
{
    /// <summary>What kind of thing a building is, for grouping the build menu.</summary>
    /// <remarks>
    /// The four that PROJECT.md 4.2 names as the buildings proper, plus the catch-all the
    /// support structures live in until there are enough of them to be worth splitting. A
    /// grouping, not a rule: nothing about placement depends on which of these a building is.
    /// </remarks>
    public enum BuildingCategory
    {
        /// <summary>Crops, greenhouses, pens. Food.</summary>
        Farm = 0,

        /// <summary>Beds and living space. Caps who can live here.</summary>
        Barracks = 1,

        /// <summary>The crafting building. Loud.</summary>
        Factory = 2,

        /// <summary>Weapons, ammunition, the loadout bench. Raids come for this first.</summary>
        Armory = 3,

        /// <summary>Power, water, storage, medical, social, defence.</summary>
        Support = 4,
    }
}
