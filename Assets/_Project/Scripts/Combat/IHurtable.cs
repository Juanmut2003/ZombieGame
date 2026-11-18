#nullable enable

namespace ZombieGame.Combat
{
    /// <summary>Something a blow can be delivered to.</summary>
    /// <remarks>
    /// An interface so that the side dealing harm never has to know what it is hitting. The horde
    /// lives in <c>ZombieGame.AI</c> and the character it bites lives in
    /// <c>ZombieGame.Characters</c>, which already depends on AI; without this the dependency
    /// would have to run both ways, and two assemblies that reference each other do not compile.
    /// Combat is the layer both already share, so the contract belongs here.
    /// </remarks>
    public interface IHurtable
    {
        /// <summary>Who this is, as reported in <see cref="DiedEvent"/> and its kin.</summary>
        int OwnerId { get; }

        /// <summary>True while there is health left.</summary>
        bool IsAlive { get; }

        /// <summary>Applies a blow after resistances.</summary>
        /// <returns>How much health it actually cost.</returns>
        float TakeDamage(DamageInfo damage);
    }
}
