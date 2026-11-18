#nullable enable
using UnityEngine;
using UnityEngine.UI;
using ZombieGame.Combat;
using ZombieGame.Core;
using ZombieGame.Core.Events;

namespace ZombieGame.UI
{
    /// <summary>
    /// Shows the belt: what is in each slot and which one is in hand.
    /// </summary>
    /// <remarks>
    /// Told by the bus like the rest of the HUD; it never touches a weapon asset. The lit slot is
    /// the only part that really matters — a player who has to guess what the attack button is
    /// about to do will stop using the belt and carry one weapon for the whole mission.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class HotbarPresenter : MonoBehaviour
    {
        [Tooltip("One label per slot, in hotbar order.")]
        [SerializeField]
        private Text[] _slots = System.Array.Empty<Text>();

        [Tooltip("The backing plate of each slot, in the same order.")]
        [SerializeField]
        private Image[] _plates = System.Array.Empty<Image>();

        [Tooltip("Colour of the slot in hand.")]
        [SerializeField]
        private Color _selectedPlate = new(0.78f, 0.69f, 0.38f, 0.85f);

        [Tooltip("Colour of the slots that are not.")]
        [SerializeField]
        private Color _idlePlate = new(0.09f, 0.1f, 0.1f, 0.78f);

        [Tooltip("Text colour of the slot in hand.")]
        [SerializeField]
        private Color _selectedText = new(0.1f, 0.1f, 0.09f, 1f);

        [Tooltip("Text colour of the slots that are not.")]
        [SerializeField]
        private Color _idleText = new(0.72f, 0.72f, 0.68f, 1f);

        private EventBus? _bus;

        private void Start()
        {
            GameServices.TryGet(out _bus);
            if (_bus is null)
            {
                Debug.LogWarning("[HUD] no event bus; the hotbar will not update.");
                return;
            }

            _bus.Subscribe<LoadoutChangedEvent>(OnLoadoutChanged);
        }

        private void OnDestroy() => _bus?.Unsubscribe<LoadoutChangedEvent>(OnLoadoutChanged);

        /// <summary>Writes the belt, for tests and for the first draw.</summary>
        public void Show(string[] names, int selected)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                bool filled = i < names.Length;
                bool lit = filled && i == selected;

                // The number is on the label rather than in a second element: it is the key the
                // player presses, so it belongs next to the name it selects.
                _slots[i].text = filled ? $"{i + 1} {names[i]}" : string.Empty;
                _slots[i].color = lit ? _selectedText : _idleText;

                if (i < _plates.Length)
                {
                    _plates[i].color = lit ? _selectedPlate : _idlePlate;
                    _plates[i].enabled = filled;
                }
            }
        }

        private void OnLoadoutChanged(LoadoutChangedEvent loadout)
        {
            // Survivors and companions will carry belts too; this one is the player's.
            if (!loadout.IsPlayer)
            {
                return;
            }

            Show(loadout.SlotNames, loadout.Selected);
        }
    }
}
