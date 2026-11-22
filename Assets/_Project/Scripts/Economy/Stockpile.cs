#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZombieGame.Economy
{
    /// <summary>
    /// What the colony has in its stores, and the only way in or out of them.
    /// </summary>
    /// <remarks>
    /// Plain C# with no scene behind it, because every interesting mistake here is arithmetic:
    /// a price paid twice, a store that goes below nothing, a part-paid bill that takes the
    /// materials and leaves the fuel. All of that is settled in tests and none of it is visible
    /// in play until a colony has quietly been given free buildings for an hour.
    /// Spending is all or nothing. A cost is a single transaction, not a list of withdrawals:
    /// a farm that costs materials and fuel either comes out of both or out of neither, and
    /// anything else leaves the player paying for something they did not get.
    /// </remarks>
    public sealed class Stockpile
    {
        /// <summary>
        /// How many kinds there are. Checked against the enum by a test, so that adding a fifth
        /// store and forgetting this cannot quietly make the last one unaffordable.
        /// </summary>
        public const int Kinds = 4;

        private readonly Dictionary<ResourceKind, int> _held = new();

        /// <summary>Raised whenever an amount changes, with the kind and its new total.</summary>
        /// <remarks>
        /// A plain event rather than a trip through the bus: the readout that listens is looking
        /// at this one object, and a colony-wide announcement for every crate of salvage would
        /// be noise for everyone else.
        /// </remarks>
        public event Action<ResourceKind, int>? Changed;

        /// <summary>How much of something is in the stores.</summary>
        public int AmountOf(ResourceKind kind) => _held.TryGetValue(kind, out int held) ? held : 0;

        /// <summary>Puts something into the stores. Nothing and less than nothing are ignored.</summary>
        public void Add(ResourceKind kind, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Set(kind, AmountOf(kind) + amount);
        }

        /// <summary>
        /// Sets a store outright. For starting a colony and for loading a save, not for play.
        /// </summary>
        public void Set(ResourceKind kind, int amount)
        {
            int clamped = Mathf.Max(0, amount);
            if (AmountOf(kind) == clamped)
            {
                return;
            }

            _held[kind] = clamped;
            Changed?.Invoke(kind, clamped);
        }

        /// <summary>True when the stores would cover this price.</summary>
        public bool CanAfford(ResourceKind kind, int amount) => AmountOf(kind) >= Mathf.Max(0, amount);

        /// <summary>True when the stores would cover every line of this price.</summary>
        /// <remarks>
        /// A price may name the same store twice — two lines of materials for two parts of the
        /// same building — so the lines are totalled before they are compared. Checking them one
        /// at a time would pass a bill the colony cannot actually pay.
        /// </remarks>
        public bool CanAfford(IReadOnlyList<ResourceAmount>? cost)
        {
            if (cost is null || cost.Count == 0)
            {
                return true;
            }

            // On the stack, because the ghost asks this every frame while a building is being
            // held and a dictionary per frame is rubbish for the collector to clear up.
            Span<int> wanted = stackalloc int[Kinds];
            foreach (ResourceAmount line in cost)
            {
                wanted[(int)line.Kind] += line.Amount;
            }

            for (int kind = 0; kind < Kinds; kind++)
            {
                if (wanted[kind] > 0 && AmountOf((ResourceKind)kind) < wanted[kind])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Takes a price out of the stores, or takes nothing at all.</summary>
        /// <returns>True when it was paid.</returns>
        public bool TrySpend(IReadOnlyList<ResourceAmount>? cost)
        {
            if (!CanAfford(cost))
            {
                return false;
            }

            if (cost is null)
            {
                return true;
            }

            // Only now that every line is known to be covered. Deciding and paying in one pass
            // is what leaves a colony short of fuel and holding no farm.
            foreach (ResourceAmount line in cost)
            {
                if (line.Amount > 0)
                {
                    Set(line.Kind, AmountOf(line.Kind) - line.Amount);
                }
            }

            return true;
        }

        /// <summary>Takes one thing out of the stores, or takes nothing.</summary>
        public bool TrySpend(ResourceKind kind, int amount)
        {
            int wanted = Mathf.Max(0, amount);
            if (AmountOf(kind) < wanted)
            {
                return false;
            }

            Set(kind, AmountOf(kind) - wanted);
            return true;
        }

        /// <summary>Gives a price back, for a building torn down or a job cancelled.</summary>
        public void Refund(IReadOnlyList<ResourceAmount>? cost)
        {
            if (cost is null)
            {
                return;
            }

            foreach (ResourceAmount line in cost)
            {
                Add(line.Kind, line.Amount);
            }
        }
    }
}
