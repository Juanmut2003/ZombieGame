#nullable enable
using System.Collections.Generic;
using ZombieGame.Waves;

namespace ZombieGame.Missions
{
    /// <summary>
    /// Turns a finished run into the handful of lines a player wants to read about it.
    /// </summary>
    /// <remarks>
    /// Plain C# and no UI types, so the wording can be tested without a scene — the sort of
    /// thing that is otherwise only ever checked by dying on purpose and squinting at the
    /// screen. What it reports is only what the run actually carried home; nothing here works
    /// anything out, and in particular the heat is reported as the stage the meter was showing
    /// rather than recomputed from thresholds that live in the balance asset.
    /// A row is left out when it has nothing to say. A debrief that lists "LOST 0" after a
    /// clean run is a form, not a report.
    /// </remarks>
    public static class MissionDebrief
    {
        /// <summary>The verdict, in the largest type on the screen.</summary>
        public static string Verdict(MissionOutcome outcome) => outcome switch
        {
            MissionOutcome.Extracted => "EXTRACTED",
            MissionOutcome.ExtractedShort => "OUT, BUT SHORT",
            MissionOutcome.SquadLost => "SQUAD LOST",
            _ => "RUN OVER",
        };

        /// <summary>
        /// One line under the verdict saying what kind of run it was, in words.
        /// </summary>
        /// <remarks>
        /// The numbers below say what happened; this says what it means. PROJECT.md §8 asks the
        /// debrief to make the difference between getting out rich and getting out at all feel
        /// like a difference, and a column of figures on its own does not.
        /// </remarks>
        public static string Summary(in MissionEndedEvent ended) => ended.Outcome switch
        {
            MissionOutcome.Extracted => "The squad came home with what it went in for.",
            MissionOutcome.ExtractedShort => "The squad came home. The shelves did not.",
            MissionOutcome.SquadLost when ended.Lost > 0 =>
                "Nobody came back. Whatever they were carrying is still out there.",
            MissionOutcome.SquadLost => "Nobody came back.",
            _ => "The run is over.",
        };

        /// <summary>The itemised lines, in reading order.</summary>
        public static IReadOnlyList<DebriefRow> Rows(in MissionEndedEvent ended)
        {
            var rows = new List<DebriefRow>(4)
            {
                new("SUPPLIES", $"{ended.Banked} / {ended.Wanted}"),
            };

            if (ended.Lost > 0)
            {
                rows.Add(new DebriefRow("LEFT BEHIND", ended.Lost.ToString()));
            }

            rows.Add(new DebriefRow("TIME", Clock(ended.Seconds)));
            rows.Add(new DebriefRow("ZONE", Zone(ended.StageAtEnd)));
            return rows;
        }

        /// <summary>A length of time as minutes and seconds.</summary>
        public static string Clock(float seconds)
        {
            // Negative time cannot happen from a running mission, but a test or a save loaded
            // from a broken file can hand one over, and "-1:-5" would be a strange thing to
            // print on a results screen.
            if (seconds < 0f)
            {
                seconds = 0f;
            }

            int whole = (int)seconds;
            return $"{whole / 60}:{whole % 60:00}";
        }

        /// <summary>How the zone was left, in the same words the meter uses.</summary>
        public static string Zone(HeatStage stage) => stage switch
        {
            HeatStage.Hunting => "HUNTING",
            HeatStage.Stirred => "STIRRED",
            _ => "QUIET",
        };
    }
}
