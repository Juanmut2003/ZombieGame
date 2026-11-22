#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using ZombieGame.Economy;

namespace ZombieGame.Missions
{
    /// <summary>
    /// One mission, as data: what it asks for and how hard it is to leave.
    /// </summary>
    /// <remarks>
    /// The scavenge of PROJECT.md §8, with the parts that exist today. Rescue, claim and the rest
    /// are more fields on this asset or siblings of it, never subclasses — the moment a mission
    /// type is a class, designers cannot make one.
    /// </remarks>
    [CreateAssetMenu(menuName = "ZombieGame/Mission Definition", fileName = "MissionDefinition")]
    public sealed class MissionDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField]
        private string _displayName = "Supply Run";

        [Tooltip("One line, shown on the HUD while the run is on.")]
        [SerializeField]
        private string _objectiveText = "Find supplies and get out";

        [Header("Objective")]
        [Tooltip("How many supplies the run was sent for.")]
        [SerializeField]
        [Min(0)]
        private int _suppliesWanted = 6;

        [Tooltip("How long the squad has to hold the extraction point before it counts.")]
        [SerializeField]
        [Min(0f)]
        private float _extractionSeconds = 4f;

        [Tooltip("What the containers hold, dealt round them in the order the map lists them.")]
        [SerializeField]
        private ResourceAmount[] _lootTable =
        {
            new(ResourceKind.Materials, 3),
            new(ResourceKind.Food, 2),
            new(ResourceKind.Materials, 2),
            new(ResourceKind.Water, 3),
            new(ResourceKind.Materials, 3),
            new(ResourceKind.Fuel, 2),
            new(ResourceKind.Food, 3),
            new(ResourceKind.Water, 2),
        };

        [Header("Zone")]
        [Tooltip("How many zombies the zone starts with.")]
        [SerializeField]
        [Min(0)]
        private int _zombiesOnStart = 18;

        [Tooltip("How hot the zone already is when the squad arrives, 0 to 1.")]
        [SerializeField]
        [Range(0f, 1f)]
        private float _startingHeat;

        [Tooltip("What this zone multiplies every noise by. Above 1 for a place already on edge.")]
        [SerializeField]
        [Range(0.1f, 4f)]
        private float _noiseFactor = 1f;

        /// <summary>Shown on the world map and in the debrief.</summary>
        public string DisplayName => _displayName;

        /// <summary>One line of what to do.</summary>
        public string ObjectiveText => _objectiveText;

        /// <summary>How many supplies the run was sent for.</summary>
        public int SuppliesWanted => _suppliesWanted;

        /// <summary>How long the extraction hold lasts.</summary>
        public float ExtractionSeconds => _extractionSeconds;

        /// <summary>How many zombies the zone starts with.</summary>
        public int ZombiesOnStart => _zombiesOnStart;

        /// <summary>How hot the zone is on arrival.</summary>
        public float StartingHeat => _startingHeat;

        /// <summary>What this zone multiplies every noise by.</summary>
        public float NoiseFactor => _noiseFactor;

        /// <summary>
        /// What one container holds, counting round the table.
        /// </summary>
        /// <remarks>
        /// The map says where containers stand and the mission says what is in them
        /// (PROJECT.md §8), so the two meet here: container <c>n</c> gets entry <c>n</c> of the
        /// table, wrapping round. Dealt rather than drawn at random, so that a zone plays the
        /// same way twice and a designer can see what they have made by reading the list.
        /// </remarks>
        public ResourceAmount[] LootFor(int container)
        {
            if (_lootTable.Length == 0)
            {
                return Array.Empty<ResourceAmount>();
            }

            int index = ((container % _lootTable.Length) + _lootTable.Length) % _lootTable.Length;
            return new[] { _lootTable[index] };
        }

        /// <summary>What the containers hold between them, one entry per container fill.</summary>
        public IReadOnlyList<ResourceAmount> LootTable => _lootTable;

        /// <summary>Builds the run's rules from this asset.</summary>
        public MissionProgress CreateProgress() => new(_suppliesWanted, _extractionSeconds);
    }
}
