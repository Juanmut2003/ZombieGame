#nullable enable
using UnityEngine;
using UnityEngine.UI;
using ZombieGame.Combat;
using ZombieGame.Core;
using ZombieGame.Core.Events;

namespace ZombieGame.UI
{
    /// <summary>
    /// Shows what is in the gun and what is left in the pockets.
    /// </summary>
    /// <remarks>
    /// Told by the bus, like every other part of the HUD; no gameplay type is referenced here
    /// beyond the event struct. The count has to be readable <b>before</b> it matters — a player
    /// who learns the gun is empty by pulling the trigger learns it one zombie too late.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class AmmoPresenter : MonoBehaviour
    {
        [Tooltip("The label that carries the count.")]
        [SerializeField]
        private Text? _label;

        [Tooltip("Colour while there are rounds in the gun.")]
        [SerializeField]
        private Color _normalColour = new(0.82f, 0.81f, 0.76f, 1f);

        [Tooltip("Colour once the gun is empty or a magazine is going in.")]
        [SerializeField]
        private Color _emptyColour = new(0.82f, 0.4f, 0.31f, 1f);

        private EventBus? _bus;

        private void Start()
        {
            GameServices.TryGet(out _bus);
            if (_bus is null)
            {
                Debug.LogWarning("[HUD] no event bus; the ammo count will not update.");
                return;
            }

            _bus.Subscribe<AmmoChangedEvent>(OnAmmoChanged);
        }

        private void OnDestroy() => _bus?.Unsubscribe<AmmoChangedEvent>(OnAmmoChanged);

        /// <summary>Writes a count, for tests and for the first draw.</summary>
        public void Show(int loaded, int reserve, bool reloading)
        {
            if (_label is null)
            {
                return;
            }

            _label.text = reloading ? "RELOADING" : $"{loaded} / {reserve}";
            _label.color = reloading || loaded <= 0 ? _emptyColour : _normalColour;
        }

        private void OnAmmoChanged(AmmoChangedEvent ammo)
        {
            // Survivors and companions will publish the same event; this label is the player's.
            if (!ammo.IsPlayer)
            {
                return;
            }

            Show(ammo.Loaded, ammo.Reserve, ammo.IsReloading);
        }
    }
}
