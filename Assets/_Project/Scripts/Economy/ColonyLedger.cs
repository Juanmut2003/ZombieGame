#nullable enable

namespace ZombieGame.Economy
{
    /// <summary>
    /// The stores a whole run spends out of, and whether they have been opened yet.
    /// </summary>
    /// <remarks>
    /// A base scene builds its stores in <c>Awake</c> and fills them from the config, which is
    /// right the first time and wrong every time afterwards: a colony coming home from a raid
    /// would be handed its opening amounts again. So the ledger outlives the scene and carries
    /// the one bit the scene needs in order to know which of the two it is doing.
    /// Here rather than with the rest of the run, because the stores are the only part of a
    /// colony that nothing about a plot or a person is needed to describe — and the stockpile
    /// component that fills them cannot see the base layer at all.
    /// </remarks>
    public sealed class ColonyLedger
    {
        /// <param name="stores">
        /// Stores that already exist, for a scene that made its own before a run was started.
        /// Leave it out for a run beginning from nothing.
        /// </param>
        public ColonyLedger(Stockpile? stores = null) => Stores = stores ?? new Stockpile();

        /// <summary>The ledger itself. One object for the life of the run.</summary>
        public Stockpile Stores { get; }

        /// <summary>True once the opening amounts are in, so they are not put in twice.</summary>
        public bool Opened { get; private set; }

        /// <summary>Marks the opening amounts as put in.</summary>
        public void Open() => Opened = true;
    }
}
