#nullable enable
using UnityEngine;
using ZombieGame.Core;
using ZombieGame.Core.Events;

namespace ZombieGame.Combat
{
    /// <summary>
    /// Draws the arc of a melee swing for a fraction of a second, where it was swung.
    /// </summary>
    /// <remarks>
    /// Without this a swing is invisible: the damage lands, the stamina goes down, and the player
    /// has no way to tell a hit from a miss or even that the button did anything. The arc is the
    /// only feedback that says where the weapon reached.
    /// It listens on the bus instead of being owned by the attacker, so the MC, a companion and
    /// later a raider all get the same visible swing without any of them knowing this exists.
    /// TODO: one flash at a time — a second swing during the first restarts it. A pool of these
    /// comes with the VFX pass, when several attackers swing at once.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class SwingFlashView : MonoBehaviour
    {
        [Tooltip("The arc sprite. Pivoted at its centre, opening towards +x.")]
        [SerializeField]
        private SpriteRenderer? _renderer;

        [Tooltip("How long the arc stays visible. Long enough to see, short enough not to linger.")]
        [SerializeField]
        [Min(0.01f)]
        private float _seconds = 0.14f;

        private EventBus? _bus;
        private Transform _flash = null!;
        private float _remaining;
        private Color _colour = Color.white;

        private void Awake()
        {
            _flash = _renderer is null ? transform : _renderer.transform;

            if (_renderer is not null)
            {
                _colour = _renderer.color;
                _renderer.enabled = false;
            }
        }

        private void Start()
        {
            // Start, not Awake: the bootstrap registers the bus in its own Awake, and the order
            // between two objects in a scene is not something to depend on.
            GameServices.TryGet(out _bus);
            if (_bus is null)
            {
                Debug.LogWarning("[FX] no event bus; swings will be invisible.");
                return;
            }

            _bus.Subscribe<MeleeSwungEvent>(OnSwing);
        }

        private void OnDestroy() => _bus?.Unsubscribe<MeleeSwungEvent>(OnSwing);

        private void Update()
        {
            if (_remaining <= 0f || _renderer is null)
            {
                return;
            }

            _remaining -= Time.deltaTime;
            if (_remaining <= 0f)
            {
                _renderer.enabled = false;
                return;
            }

            // Fades out over its life rather than blinking off, which at a seventh of a second is
            // the difference between a swing and a glitch.
            _colour.a = Mathf.Clamp01(_remaining / _seconds);
            _renderer.color = _colour;
        }

        private void OnSwing(MeleeSwungEvent swing)
        {
            if (_renderer is null)
            {
                return;
            }

            _flash.position = new Vector3(swing.Origin.x, swing.Origin.y, 0f);
            _flash.rotation = Quaternion.Euler(
                0f, 0f, Mathf.Atan2(swing.Facing.y, swing.Facing.x) * Mathf.Rad2Deg);

            // The sprite is drawn with its reach as the radius of the texture, so scaling it by
            // the weapon's range makes the arc end exactly where the weapon does.
            _flash.localScale = Vector3.one * Mathf.Max(0.01f, swing.Range);

            _remaining = _seconds;
            _colour.a = 1f;
            _renderer.color = _colour;
            _renderer.enabled = true;
        }
    }
}
