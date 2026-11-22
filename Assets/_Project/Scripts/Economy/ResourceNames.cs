#nullable enable
using System.Collections.Generic;
using System.Text;

namespace ZombieGame.Economy
{
    /// <summary>
    /// What the four stores are called on screen.
    /// </summary>
    /// <remarks>
    /// One copy, because the player reads these words in four places — the stores readout, a
    /// building's price, a debrief, the line when a squad gets home — and four lists that have
    /// to agree is three lists that will not. The enum's own names are no use: "Materials" is
    /// not what anybody in this game calls salvage.
    /// </remarks>
    public static class ResourceNames
    {
        /// <summary>The short form, for a price or a column of figures.</summary>
        public static string Short(ResourceKind kind) => kind switch
        {
            ResourceKind.Materials => "MAT",
            ResourceKind.Food => "FOOD",
            ResourceKind.Water => "WATER",
            _ => "FUEL",
        };

        /// <summary>The word the stores readout uses.</summary>
        public static string Long(ResourceKind kind) => kind switch
        {
            ResourceKind.Materials => "SALVAGE",
            ResourceKind.Food => "FOOD",
            ResourceKind.Water => "WATER",
            _ => "FUEL",
        };

        /// <summary>
        /// A list of amounts as one line, in the order of the stores rather than the list.
        /// </summary>
        /// <remarks>
        /// Totalled by kind first: a haul of two crates of food is "5 FOOD", not "3 FOOD 2
        /// FOOD". Sorted by the enum so that two runs that came back with the same things read
        /// the same way round, whatever order the containers happened to be opened in.
        /// </remarks>
        public static string Line(IReadOnlyList<ResourceAmount>? amounts)
        {
            if (amounts is null || amounts.Count == 0)
            {
                return string.Empty;
            }

            var totals = new int[Stockpile.Kinds];
            foreach (ResourceAmount amount in amounts)
            {
                totals[(int)amount.Kind] += amount.Amount;
            }

            var text = new StringBuilder();
            for (int kind = 0; kind < totals.Length; kind++)
            {
                if (totals[kind] <= 0)
                {
                    continue;
                }

                if (text.Length > 0)
                {
                    text.Append("  ");
                }

                text.Append(totals[kind]).Append(' ').Append(Short((ResourceKind)kind));
            }

            return text.ToString();
        }

        /// <summary>Everything in a list added up, whatever kind it is.</summary>
        public static int Total(IReadOnlyList<ResourceAmount>? amounts)
        {
            if (amounts is null)
            {
                return 0;
            }

            int total = 0;
            foreach (ResourceAmount amount in amounts)
            {
                total += amount.Amount;
            }

            return total;
        }
    }
}
