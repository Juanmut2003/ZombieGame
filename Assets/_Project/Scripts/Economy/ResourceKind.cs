#nullable enable

namespace ZombieGame.Economy
{
    /// <summary>The things a colony keeps in its stores.</summary>
    /// <remarks>
    /// Four, and deliberately not more. PROJECT.md 4.3 runs the colony on food, water and power,
    /// and everything that gets built or made comes out of salvage; splitting salvage into wood,
    /// steel, cloth and wire is a second game about sorting, which is not the one being made.
    /// Power is not here because power is not stored in crates — it is a grid that balances
    /// every tick, and it arrives with the generators in its own right.
    /// </remarks>
    public enum ResourceKind
    {
        /// <summary>Salvage: what buildings are put up and repaired with.</summary>
        Materials = 0,

        /// <summary>What the colony eats. Spoils.</summary>
        Food = 1,

        /// <summary>Drunk, farmed with, treated with. Dirty water is its own problem later.</summary>
        Water = 2,

        /// <summary>Burnt by generators and vehicles. Loud and dangerous to store.</summary>
        Fuel = 3,
    }
}
