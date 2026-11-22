#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieGame.Economy;

namespace ZombieGame.Missions
{
    /// <summary>
    /// The rules of a scavenge run: what has been picked up, and what the squad actually got to keep.
    /// </summary>
    /// <remarks>
    /// The rule this class exists to enforce is one line of PROJECT.md §8: <b>extraction is always
    /// required — loot only counts once the squad is in the vehicle.</b> Everything carried is
    /// provisional, and a run that ends badly ends with nothing. That single rule is what turns a
    /// scavenge from "grab things until bored" into a decision about when to leave, and it is
    /// worth having in a plain class that can be reasoned about without a scene.
    /// </remarks>
    public sealed class MissionProgress
    {
        private readonly float _extractionSeconds;
        private readonly int[] _carried = new int[Stockpile.Kinds];

        /// <param name="suppliesWanted">What the run was sent for. Zero means anything will do.</param>
        /// <param name="extractionSeconds">How long the squad has to hold the extraction point.</param>
        public MissionProgress(int suppliesWanted, float extractionSeconds)
        {
            if (suppliesWanted < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(suppliesWanted), suppliesWanted, "Cannot be negative.");
            }

            if (extractionSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(extractionSeconds), extractionSeconds, "Cannot be negative.");
            }

            SuppliesWanted = suppliesWanted;
            _extractionSeconds = extractionSeconds;
        }

        /// <summary>What the run was sent for.</summary>
        public int SuppliesWanted { get; }

        /// <summary>What is in the squad's hands right now, and would be lost with them.</summary>
        public int Carried { get; private set; }

        /// <summary>What came home. Zero until the squad is out.</summary>
        public int Banked { get; private set; }

        /// <summary>Where the run stands.</summary>
        public MissionState State { get; private set; } = MissionState.Running;

        /// <summary>How far through the extraction hold the squad is, 0 to 1.</summary>
        public float ExtractionFraction =>
            _extractionSeconds <= 0f ? 1f : Mathf.Clamp01(_held / _extractionSeconds);

        /// <summary>True once enough has been found to satisfy what the run was sent for.</summary>
        public bool ObjectiveMet => Carried >= SuppliesWanted;

        /// <summary>True while the run can still change.</summary>
        public bool IsOver => State is MissionState.Extracted or MissionState.Failed;

        private float _held;

        /// <summary>
        /// Picks something up, by store.
        /// </summary>
        /// <remarks>
        /// Kept as a tally per store rather than as the list it arrived in: a run that opens
        /// twenty containers would otherwise hold twenty entries, nineteen of which say the same
        /// word, and every reader of them would have to add up before it could say anything.
        /// </remarks>
        public void Collect(IReadOnlyList<ResourceAmount>? contents)
        {
            if (IsOver || contents is null)
            {
                return;
            }

            foreach (ResourceAmount amount in contents)
            {
                if (amount.Amount <= 0)
                {
                    continue;
                }

                _carried[(int)amount.Kind] += amount.Amount;
                Carried += amount.Amount;
            }
        }

        /// <summary>Picks up salvage, for a test or a zone with no table behind it.</summary>
        public void Collect(int supplies) =>
            Collect(new[] { new ResourceAmount(ResourceKind.Materials, supplies) });

        /// <summary>How much of one store is in the squad's hands.</summary>
        public int CarriedOf(ResourceKind kind) => _carried[(int)kind];

        /// <summary>
        /// What came home, by store. Empty until the squad is out.
        /// </summary>
        /// <remarks>
        /// Built on demand rather than kept, because the only moment it is asked for is the one
        /// frame a run ends, and a list kept in step with four counters is four chances to be
        /// wrong about what the colony is owed.
        /// </remarks>
        public IReadOnlyList<ResourceAmount> Haul
        {
            get
            {
                if (State != MissionState.Extracted)
                {
                    return Array.Empty<ResourceAmount>();
                }

                var haul = new List<ResourceAmount>(Stockpile.Kinds);
                for (int kind = 0; kind < _carried.Length; kind++)
                {
                    if (_carried[kind] > 0)
                    {
                        haul.Add(new ResourceAmount((ResourceKind)kind, _carried[kind]));
                    }
                }

                return haul;
            }
        }

        /// <summary>
        /// Moves the extraction clock, which only runs while the squad stands in the zone.
        /// </summary>
        /// <remarks>
        /// Stepping out resets it rather than pausing it. A hold that can be done in instalments
        /// is not a hold — the point is that leaving is the most dangerous part of the run,
        /// because standing still is exactly what a zone full of zombies is waiting for.
        /// </remarks>
        /// <param name="deltaSeconds">How much time passed.</param>
        /// <param name="inZone">Whether the squad is standing in the extraction point.</param>
        /// <returns>True on the step the run is won.</returns>
        public bool TickExtraction(float deltaSeconds, bool inZone)
        {
            if (IsOver)
            {
                return false;
            }

            if (!inZone || deltaSeconds <= 0f)
            {
                _held = 0f;
                State = MissionState.Running;
                return false;
            }

            State = MissionState.Extracting;
            _held += deltaSeconds;
            if (_held < _extractionSeconds)
            {
                return false;
            }

            State = MissionState.Extracted;
            Banked = Carried;
            return true;
        }

        /// <summary>Ends the run badly. Everything carried is lost.</summary>
        public void Fail()
        {
            if (IsOver)
            {
                return;
            }

            State = MissionState.Failed;
            Banked = 0;
            _held = 0f;
        }
    }
}
