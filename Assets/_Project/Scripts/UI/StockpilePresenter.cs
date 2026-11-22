#nullable enable
using UnityEngine;
using UnityEngine.UI;
using ZombieGame.Core;
using ZombieGame.Economy;

namespace ZombieGame.UI
{
    /// <summary>
    /// What the colony has in its stores, in the corner of the screen.
    /// </summary>
    /// <remarks>
    /// Four numbers that have to be on screen before a price means anything. A building that
    /// costs forty-five materials is only a decision if the player can see they have a hundred
    /// and twenty, and a refusal they cannot explain is just a button that does not work.
    /// Redrawn when the stores change rather than every frame: nothing here moves on its own.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class StockpilePresenter : MonoBehaviour
    {
        [Tooltip("The line for salvage.")]
        [SerializeField]
        private Text? _materials;

        [Tooltip("The line for food.")]
        [SerializeField]
        private Text? _food;

        [Tooltip("The line for water.")]
        [SerializeField]
        private Text? _water;

        [Tooltip("The line for fuel.")]
        [SerializeField]
        private Text? _fuel;

        private Stockpile? _stores;

        private void Start()
        {
            if (!GameServices.TryGet(out _stores) || _stores is null)
            {
                Debug.LogWarning("[Stores] no stockpile to report on.");
                return;
            }

            _stores.Changed += OnChanged;
            ShowAll();
        }

        private void OnDestroy()
        {
            if (_stores is not null)
            {
                _stores.Changed -= OnChanged;
            }
        }

        /// <summary>Puts every line up again. Public for tests.</summary>
        public void ShowAll()
        {
            if (_stores is null)
            {
                return;
            }

            foreach (ResourceKind kind in new[]
                     {
                         ResourceKind.Materials,
                         ResourceKind.Food,
                         ResourceKind.Water,
                         ResourceKind.Fuel,
                     })
            {
                OnChanged(kind, _stores.AmountOf(kind));
            }
        }

        private void OnChanged(ResourceKind kind, int amount)
        {
            Text? line = kind switch
            {
                ResourceKind.Materials => _materials,
                ResourceKind.Food => _food,
                ResourceKind.Water => _water,
                _ => _fuel,
            };

            if (line != null)
            {
                line.text = $"{Label(kind)}  {amount}";
            }
        }

        private static string Label(ResourceKind kind) => kind switch
        {
            ResourceKind.Materials => "SALVAGE",
            ResourceKind.Food => "FOOD",
            ResourceKind.Water => "WATER",
            _ => "FUEL",
        };
    }
}
