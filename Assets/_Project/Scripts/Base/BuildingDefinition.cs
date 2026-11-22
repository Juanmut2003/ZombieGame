#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieGame.Economy;

namespace ZombieGame.Base
{
    /// <summary>
    /// Something that can be put up on a claimed site, and the ladder it climbs.
    /// </summary>
    /// <remarks>
    /// What the building <i>is</i> lives here — its name, what it belongs to, what putting the
    /// first one up costs. Everything that changes as it is improved lives on its levels, so
    /// that a farm is one asset with four rungs rather than four assets pretending to be
    /// related. See <c>Docs/Buildings</c> for the sheets the rungs are read off.
    /// What it draws from the power grid is still missing. That is the next step, and a field
    /// of zeroes now would be a balance number in C# pretending to be data.
    /// </remarks>
    [CreateAssetMenu(fileName = "Building", menuName = "ZombieGame/Building", order = 50)]
    public sealed class BuildingDefinition : ScriptableObject
    {
        [Tooltip("What the build menu calls it.")]
        [SerializeField]
        private string _displayName = "Building";

        [Tooltip("One line about what it is for.")]
        [SerializeField]
        [TextArea(1, 3)]
        private string _blurb = string.Empty;

        [Tooltip("What it belongs to, for grouping the build menu.")]
        [SerializeField]
        private BuildingCategory _category = BuildingCategory.Support;

        [Tooltip("What putting the first one up costs. Empty means free.")]
        [SerializeField]
        private ResourceAmount[] _cost = Array.Empty<ResourceAmount>();

        [Tooltip("Every level it can be improved to, the first one first.")]
        [SerializeField]
        private BuildingLevel[] _levels = Array.Empty<BuildingLevel>();

        /// <summary>What the build menu calls it.</summary>
        public string DisplayName => _displayName;

        /// <summary>One line about what it is for.</summary>
        public string Blurb => _blurb;

        /// <summary>What it belongs to.</summary>
        public BuildingCategory Category => _category;

        /// <summary>
        /// What putting the first one up costs, in the colony's stores.
        /// </summary>
        /// <remarks>
        /// Tuned in the asset, never written from code after the asset exists. The catalogue
        /// builder seeds a price when it first creates a building and then leaves it alone, so
        /// that rebuilding the catalogue cannot undo an afternoon of balancing.
        /// </remarks>
        public IReadOnlyList<ResourceAmount> Cost => _cost;

        /// <summary>How many levels it has. At least one for anything buildable.</summary>
        public int LevelCount => _levels.Length;

        /// <summary>The highest level it can reach, counting from one.</summary>
        public int TopLevel => _levels.Length;

        /// <summary>
        /// One level, counting from one.
        /// </summary>
        /// <remarks>
        /// Clamped rather than thrown on. A building standing at a level its asset no longer has
        /// — somebody deleted a rung while a save was on it — should keep working at the nearest
        /// one it does have, not take the colony down with it.
        /// </remarks>
        public BuildingLevel? Level(int level)
        {
            if (_levels.Length == 0)
            {
                return null;
            }

            int index = Mathf.Clamp(level, 1, _levels.Length) - 1;
            return _levels[index];
        }

        /// <summary>True when there is a rung above this one.</summary>
        public bool CanUpgradeFrom(int level) => level < _levels.Length;

        /// <summary>
        /// Everything paid to get a building to this level, for working out a refund.
        /// </summary>
        /// <remarks>
        /// The build cost plus every upgrade along the way. A colony tearing down a barracks it
        /// has improved twice should get something back for the improvements, not only for the
        /// hut it started as.
        /// </remarks>
        public List<ResourceAmount> PaidUpTo(int level)
        {
            var paid = new List<ResourceAmount>(_cost);
            for (int rung = 2; rung <= level && rung <= _levels.Length; rung++)
            {
                paid.AddRange(_levels[rung - 1].UpgradeCost);
            }

            return paid;
        }

        /// <summary>How much ground the first one stands on, for the ghost in the hand.</summary>
        public BuildingFootprint Footprint =>
            Level(1)?.Footprint ?? new BuildingFootprint(1, 1);

        /// <summary>What the first one looks like, for the build menu and the ghost.</summary>
        public Sprite? Sprite => Level(1)?.Sprite;
    }
}
