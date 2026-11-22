#nullable enable
using ZombieGame.Core;
using ZombieGame.Economy;

namespace ZombieGame.Base
{
    /// <summary>
    /// Where the current run is kept, and the one place that starts a new one.
    /// </summary>
    /// <remarks>
    /// A run lives in <see cref="GameServices"/> — one of the four approved homes for something
    /// that outlives a scene — and this is the only code that puts one there. Keeping that in
    /// one function matters because of what the absence of a run means: no run is how the base
    /// scene tells "the player started a game" from "somebody opened Base.unity on its own",
    /// and the second must stay a plot you can build on with nothing remembered afterwards.
    /// That is also what keeps the play tests honest: they load the base scene directly, find no
    /// run, and get a bare plot every time rather than the last test's buildings.
    /// </remarks>
    public static class ColonyRuns
    {
        /// <summary>The run in progress, or null when there is none.</summary>
        public static ColonyRun? Current =>
            GameServices.TryGet(out ColonyRun? run) ? run : null;

        /// <summary>
        /// Starts a run, throwing away whatever one was in progress.
        /// </summary>
        /// <remarks>
        /// Pressing START in the menu, and nothing else. Replaces rather than registers, because
        /// a second game started without quitting has to begin from nothing.
        /// </remarks>
        public static ColonyRun Begin() => Begin(new ColonyLedger());

        /// <summary>
        /// The run in progress, or a new one around stores that already exist.
        /// </summary>
        /// <remarks>
        /// For the base scene opened on its own: the stores were made by the scene rather than
        /// by a run, and the run has to adopt them or a raid would bring its haul home to a
        /// ledger nothing is reading.
        /// </remarks>
        public static ColonyRun GetOrBegin(Stockpile stores)
        {
            ColonyRun? run = Current;
            if (run is not null)
            {
                return run;
            }

            // Already opened: the scene filled these stores from the config on its way up, and
            // a ledger that still thinks it is new would hand the colony its starting amounts
            // again the next time the base loads.
            var ledger = new ColonyLedger(stores);
            ledger.Open();
            return Begin(ledger);
        }

        /// <summary>
        /// Puts a run and the stores behind it into the locator together.
        /// </summary>
        /// <remarks>
        /// Both, because the two halves are read by code on opposite sides of an assembly
        /// boundary: the stockpile component can see a ledger and cannot see a run.
        /// </remarks>
        private static ColonyRun Begin(ColonyLedger ledger)
        {
            var run = new ColonyRun(ledger);
            GameServices.Replace(ledger);
            GameServices.Replace(run);

            // Empty until the squad goes out. Registered now so that a zone can report in
            // without wondering whether anybody asked it to, and so that a run started twice
            // cannot come home to the last one's haul.
            GameServices.Replace(new RaidTrip());
            return run;
        }
    }
}
