#nullable enable
using UnityEngine;
using ZombieGame.Core;
using ZombieGame.Core.Config;

namespace ZombieGame.Economy
{
    /// <summary>
    /// The colony's stores as they exist in a scene: filled at the start, and findable.
    /// </summary>
    /// <remarks>
    /// The ledger itself is plain C# and knows nothing about scenes. This is the one object
    /// that starts it off and hands it to whoever needs it — the build placer now, the farms
    /// and the canteen later, none of which should have to go looking for a transform.
    /// What a colony starts with is a balance number and lives in the config, not here.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class ColonyStockpile : MonoBehaviour
    {
        [Tooltip("Where the starting amounts come from.")]
        [SerializeField]
        private BalanceConfig? _balance;

        /// <summary>The stores. Built in Awake and the same object for the scene's life.</summary>
        public Stockpile Stockpile { get; private set; } = null!;

        private void Awake()
        {
            // Compared with Unity's own operator: an unassigned serialized reference is a live
            // wrapper around nothing, not a C# null.
            if (_balance == null)
            {
                Debug.LogWarning($"[Stores] {name} has no BalanceConfig; using the defaults.");
                _balance = BalanceConfig.CreateDefault();
            }

            Stockpile = new Stockpile();
            Stockpile.Set(ResourceKind.Materials, _balance.StartingMaterials);
            Stockpile.Set(ResourceKind.Food, _balance.StartingFood);
            Stockpile.Set(ResourceKind.Water, _balance.StartingWater);
            Stockpile.Set(ResourceKind.Fuel, _balance.StartingFuel);

            // Replace rather than Register: coming back to the base would otherwise find a
            // locator that already holds the stores from last time.
            GameServices.Replace(Stockpile);
        }

        private void OnDestroy()
        {
            // The stores belong to this scene for now. When a run spans the base and a mission
            // they will have to outlive both, and that is the save system's business.
            GameServices.Unregister<Stockpile>();
        }
    }
}
