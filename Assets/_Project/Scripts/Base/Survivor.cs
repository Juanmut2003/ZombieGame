#nullable enable

namespace ZombieGame.Base
{
    /// <summary>
    /// One of the people living at the base.
    /// </summary>
    /// <remarks>
    /// A person, not a worker: where they work is one thing about them and not what they are.
    /// Skills, health, morale and relationships all hang off this later; what is here is what
    /// the colony needs today to tell one of them from another and to know where to send them.
    /// </remarks>
    public sealed class Survivor
    {
        public Survivor(int id, string name)
        {
            Id = id;
            Name = name;
        }

        /// <summary>Their number in the colony. Counts from one.</summary>
        public int Id { get; }

        /// <summary>What everybody calls them.</summary>
        public string Name { get; }

        /// <summary>
        /// The building they work in, or zero while they have nothing to do.
        /// </summary>
        /// <remarks>
        /// The building's number on the site, not a reference to it. A building can be torn
        /// down while somebody is standing in it, and a colonist holding a dead reference is a
        /// worse problem than one holding a number that no longer matches anything.
        /// </remarks>
        public int Workplace { get; internal set; }

        /// <summary>True while they have no job.</summary>
        public bool IsIdle => Workplace == 0;
    }
}
