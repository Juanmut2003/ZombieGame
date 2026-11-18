#nullable enable
using UnityEngine;
using ZombieGame.Combat;
using ZombieGame.Core;
using ZombieGame.Core.Events;

namespace ZombieGame.Characters
{
    /// <summary>
    /// What a character is carrying and which of it is in hand.
    /// </summary>
    /// <remarks>
    /// The hotbar from the player's side. One button attacks, and this decides what that attack
    /// is — which is the only arrangement that survives a growing arsenal, since the alternative
    /// is a new button for every weapon ever added.
    /// The weapons themselves stay assets; this holds a list of them and an index. When the
    /// inventory of PROJECT.md §11 arrives it fills these slots instead of the inspector, and
    /// nothing above this line has to change.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class Loadout : MonoBehaviour
    {
        [Tooltip("The weapons on the belt, in hotbar order.")]
        [SerializeField]
        private WeaponDefinition?[] _slots = System.Array.Empty<WeaponDefinition?>();

        [Tooltip("Which slot starts in hand, counting from zero.")]
        [SerializeField]
        [Min(0)]
        private int _selected;

        [Tooltip("Who is steering. Without one the slots can still be switched from code.")]
        [SerializeField]
        private PlayerInputReader? _input;

        private EventBus? _bus;
        private float _scrollRest = 1f;
        private bool _announcedOnce;

        /// <summary>How many slots there are.</summary>
        public int SlotCount => _slots.Length;

        /// <summary>Which slot is in hand.</summary>
        public int Selected => _selected;

        /// <summary>What is in hand, or null when the slot is empty.</summary>
        public WeaponDefinition? Current =>
            (uint)_selected < (uint)_slots.Length ? _slots[_selected] : null;

        /// <summary>What is in one slot, or null when it is empty or out of range.</summary>
        public WeaponDefinition? At(int slot) =>
            (uint)slot < (uint)_slots.Length ? _slots[slot] : null;

        private void Start()
        {
            GameServices.TryGet(out _bus);
            _selected = Mathf.Clamp(_selected, 0, Mathf.Max(0, _slots.Length - 1));
        }

        private void Update()
        {
            // The belt announces itself on the first frame rather than in Start: a hotbar that
            // subscribes in its own Start would miss it and draw empty slots until the player
            // switched weapons to find out what they were carrying.
            if (!_announcedOnce)
            {
                _announcedOnce = true;
                Announce();
            }

            if (_input is null || _slots.Length == 0)
            {
                return;
            }

            if (_input.SlotPressed > 0)
            {
                Select(_input.SlotPressed - 1);
                return;
            }

            // The wheel reports how far it turned, not that it clicked; anything past a rest
            // threshold counts as one step, so a slow turn does not run through the whole belt.
            float scroll = _input.WeaponScroll;
            if (Mathf.Abs(scroll) >= _scrollRest)
            {
                Select(_selected + (scroll > 0f ? -1 : 1));
            }
        }

        /// <summary>
        /// Puts a slot in hand. Out-of-range wraps, so the wheel runs round the belt.
        /// </summary>
        /// <returns>False when there is nothing to select.</returns>
        public bool Select(int slot)
        {
            if (_slots.Length == 0)
            {
                return false;
            }

            int wrapped = ((slot % _slots.Length) + _slots.Length) % _slots.Length;
            if (wrapped == _selected)
            {
                return true;
            }

            _selected = wrapped;
            Announce();
            return true;
        }

        private void Announce()
        {
            if (_bus is null)
            {
                return;
            }

            var names = new string[_slots.Length];
            for (int i = 0; i < _slots.Length; i++)
            {
                names[i] = _slots[i] != null ? _slots[i]!.DisplayName : "—";
            }

            _bus.Publish(new LoadoutChangedEvent(names, _selected, isPlayer: true));
        }
    }
}
