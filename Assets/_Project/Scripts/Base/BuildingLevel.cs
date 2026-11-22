#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieGame.Economy;

namespace ZombieGame.Base
{
    /// <summary>
    /// One rung of a building's ladder: how big it is, what it holds, what it brings in.
    /// </summary>
    /// <remarks>
    /// Every one of these is read off the sheets in <c>Docs/Buildings</c>, where each building is
    /// drawn four times over. A level is not a multiplier on the one below it — a farm at level
    /// three has twelve beds of soil and a processing bench, and that is a different building to
    /// look at as well as to run.
    /// The footprint grows with the level, which is the part that costs the player something:
    /// upgrading needs free ground beside the building, so where a thing was put down is still
    /// being paid for several levels later.
    /// </remarks>
    [Serializable]
    public sealed class BuildingLevel
    {
        [Tooltip("What it looks like at this level.")]
        [SerializeField]
        private Sprite? _sprite;

        [Tooltip("How many cells of ground it stands on, across.")]
        [SerializeField]
        [Min(1)]
        private int _width = 3;

        [Tooltip("How many cells of ground it stands on, up.")]
        [SerializeField]
        [Min(1)]
        private int _height = 5;

        [Tooltip("How many people can sleep here.")]
        [SerializeField]
        [Min(0)]
        private int _beds;

        [Tooltip("How many people can work here at once.")]
        [SerializeField]
        [Min(0)]
        private int _workPosts;

        [Tooltip("What one worker brings in over a day. Empty means it makes nothing yet.")]
        [SerializeField]
        private ResourceAmount[] _yieldPerWorker = Array.Empty<ResourceAmount>();

        [Tooltip("What getting to this level costs. Ignored on the first one.")]
        [SerializeField]
        private ResourceAmount[] _upgradeCost = Array.Empty<ResourceAmount>();

        [Tooltip("How much power it needs to run.")]
        [SerializeField]
        [Min(0)]
        private int _powerDraw;

        [Tooltip("How much power it puts into the grid. Generators only.")]
        [SerializeField]
        [Min(0)]
        private int _powerSupply;

        [Tooltip("Fuel burnt over a day while it is running. Generators only.")]
        [SerializeField]
        [Min(0)]
        private int _fuelPerDay;

        [Tooltip("Water drawn over a day while it is working.")]
        [SerializeField]
        [Min(0)]
        private int _waterPerDay;

        /// <summary>What it looks like at this level.</summary>
        public Sprite? Sprite => _sprite;

        /// <summary>How much ground it stands on at this level.</summary>
        public BuildingFootprint Footprint => new(_width, _height);

        /// <summary>How many people can sleep here.</summary>
        public int Beds => _beds;

        /// <summary>How many people can work here at once.</summary>
        public int WorkPosts => _workPosts;

        /// <summary>What one worker brings in over a day.</summary>
        public IReadOnlyList<ResourceAmount> YieldPerWorker => _yieldPerWorker;

        /// <summary>What getting to this level costs. The first level is paid for by building it.</summary>
        public IReadOnlyList<ResourceAmount> UpgradeCost => _upgradeCost;

        /// <summary>How much power it needs to run.</summary>
        public int PowerDraw => _powerDraw;

        /// <summary>How much power it puts into the grid.</summary>
        public int PowerSupply => _powerSupply;

        /// <summary>Fuel burnt over a day while it is running.</summary>
        public int FuelPerDay => _fuelPerDay;

        /// <summary>Water drawn over a day while it is working.</summary>
        public int WaterPerDay => _waterPerDay;

        /// <summary>True when it puts more into the grid than it takes out.</summary>
        public bool IsGenerator => _powerSupply > _powerDraw;

        /// <summary>True when putting somebody in here actually brings something in.</summary>
        public bool HasWork => _workPosts > 0 && _yieldPerWorker.Length > 0;
    }
}
