#nullable enable
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using ZombieGame.Economy;

namespace ZombieGame.Base
{
    /// <summary>One slice of the wheel: what it offers, what it costs, and whether it can.</summary>
    public readonly struct BuildingChoice
    {
        public BuildingChoice(BuildingOption option, string title, string detail, bool allowed)
        {
            Option = option;
            Title = title;
            Detail = detail;
            Allowed = allowed;
        }

        /// <summary>Which of the four this is.</summary>
        public BuildingOption Option { get; }

        /// <summary>The word on the slice.</summary>
        public string Title { get; }

        /// <summary>The line under it: a price, a count, or why it cannot be done.</summary>
        public string Detail { get; }

        /// <summary>False when pressing it would do nothing.</summary>
        public bool Allowed { get; }
    }

    /// <summary>
    /// Reads a standing building and says what the player may do with it.
    /// </summary>
    /// <remarks>
    /// Plain C# on purpose. Which slice is greyed out, and what the line under it says, is the
    /// whole of what the menu communicates — a wheel that offers an upgrade the colony cannot
    /// pay for, or hides one it can, is worse than no wheel. Settled in tests rather than by
    /// clicking round a base.
    /// A disabled slice still says why. "IMPROVE — NO ROOM" tells the player to clear the
    /// ground beside it; a slice that is simply missing tells them the game is broken.
    /// </remarks>
    public static class BuildingMenu
    {
        /// <summary>How many slices the wheel always has.</summary>
        public const int Slices = 4;

        /// <summary>
        /// Describes the four slices for a building, in wheel order.
        /// </summary>
        /// <param name="site">The plot it stands on.</param>
        /// <param name="id">Its number on the plot.</param>
        /// <param name="colony">The people, for the two slices about work. May be null.</param>
        /// <param name="stores">What the colony has. Null means a scene with no economy.</param>
        /// <param name="refundShare">How much of what was paid comes back when it is torn down.</param>
        /// <returns>Four slices, or an empty list when there is no such building.</returns>
        public static IReadOnlyList<BuildingChoice> Describe(
            BuildSite site,
            int id,
            Colony? colony,
            Stockpile? stores,
            float refundShare)
        {
            PlacedBuilding? standing = site.Find(id);
            if (standing is null || standing.Value.Definition == null)
            {
                return System.Array.Empty<BuildingChoice>();
            }

            PlacedBuilding building = standing.Value;
            return new[]
            {
                Improve(site, building, stores),
                PutToWork(building, colony),
                SendHome(building, colony),
                TearDown(building, refundShare),
            };
        }

        /// <summary>The name and level plate that sits over the wheel.</summary>
        public static string Headline(BuildSite site, int id)
        {
            PlacedBuilding? standing = site.Find(id);
            if (standing is null || standing.Value.Definition == null)
            {
                return string.Empty;
            }

            PlacedBuilding building = standing.Value;
            return $"{building.Definition!.DisplayName.ToUpperInvariant()}   "
                + $"{building.Level} / {building.Definition.TopLevel}";
        }

        /// <summary>Resource lines as one short line of text, or a dash when there are none.</summary>
        public static string Price(IReadOnlyList<ResourceAmount> lines)
        {
            if (lines.Count == 0)
            {
                return "FREE";
            }

            var text = new StringBuilder();
            for (int i = 0; i < lines.Count; i++)
            {
                if (i > 0)
                {
                    text.Append("  ");
                }

                text.Append(lines[i].Amount).Append(' ').Append(Short(lines[i].Kind));
            }

            return text.ToString();
        }

        private static BuildingChoice Improve(
            BuildSite site, PlacedBuilding building, Stockpile? stores)
        {
            BuildingDefinition what = building.Definition!;
            if (!what.CanUpgradeFrom(building.Level))
            {
                return new BuildingChoice(BuildingOption.Improve, "IMPROVE", "AT ITS BEST", false);
            }

            BuildingLevel? next = what.Level(building.Level + 1);
            if (next is null)
            {
                return new BuildingChoice(BuildingOption.Improve, "IMPROVE", "AT ITS BEST", false);
            }

            string price = Price(next.UpgradeCost);
            PlacementVerdict ground = site.CheckUpgrade(building.Id, next.Footprint);
            if (ground != PlacementVerdict.Ok)
            {
                return new BuildingChoice(BuildingOption.Improve, "IMPROVE", Say(ground), false);
            }

            bool afford = stores is null || stores.CanAfford(next.UpgradeCost);
            return new BuildingChoice(BuildingOption.Improve, "IMPROVE", price, afford);
        }

        private static BuildingChoice PutToWork(PlacedBuilding building, Colony? colony)
        {
            BuildingLevel? rung = building.Rung;
            int posts = rung is null ? 0 : rung.WorkPosts;
            if (posts == 0)
            {
                return new BuildingChoice(BuildingOption.PutToWork, "WORK", "NO JOBS HERE", false);
            }

            if (colony is null)
            {
                return new BuildingChoice(BuildingOption.PutToWork, "WORK", $"0 / {posts}", false);
            }

            int working = colony.WorkersAt(building.Id);
            string detail = $"{working} / {posts}";
            bool room = working < posts && colony.FirstIdle() is not null;
            return new BuildingChoice(BuildingOption.PutToWork, "WORK", detail, room);
        }

        private static BuildingChoice SendHome(PlacedBuilding building, Colony? colony)
        {
            int working = colony is null ? 0 : colony.WorkersAt(building.Id);
            string detail = working == 1 ? "1 HERE" : $"{working} HERE";
            return new BuildingChoice(
                BuildingOption.SendHome, "SEND HOME", detail, working > 0);
        }

        private static BuildingChoice TearDown(PlacedBuilding building, float refundShare)
        {
            var back = new List<ResourceAmount>();
            foreach (ResourceAmount line in building.Definition!.PaidUpTo(building.Level))
            {
                int amount = Mathf.FloorToInt(line.Amount * refundShare);
                if (amount > 0)
                {
                    back.Add(new ResourceAmount(line.Kind, amount));
                }
            }

            string detail = back.Count == 0 ? "NOTHING BACK" : $"+{Price(back)}";
            return new BuildingChoice(BuildingOption.TearDown, "TEAR DOWN", detail, true);
        }

        private static string Say(PlacementVerdict verdict) => verdict switch
        {
            PlacementVerdict.Ok => "READY",
            PlacementVerdict.OutsideTheSite => "OFF THE PLOT",
            PlacementVerdict.GroundIsBlocked => "NO ROOM",
            PlacementVerdict.SomethingIsThere => "NO ROOM",
            _ => "CANNOT PAY",
        };

        private static string Short(ResourceKind kind) => ResourceNames.Short(kind);
    }
}
