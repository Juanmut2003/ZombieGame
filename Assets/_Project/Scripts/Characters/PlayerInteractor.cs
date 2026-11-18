#nullable enable
using UnityEngine;
using ZombieGame.Core;
using ZombieGame.Core.Events;
using ZombieGame.Missions;

namespace ZombieGame.Characters
{
    /// <summary>
    /// Lets the character go through whatever they are standing next to.
    /// </summary>
    /// <remarks>
    /// Held rather than pressed: searching takes time, and time spent standing still in a zone is
    /// the price of loot. Letting go — or walking away — loses the progress, which is what makes
    /// a container near a lane a different proposition from one in a back room.
    /// Nearby containers are found with a physics overlap rather than a search of the scene: it
    /// only runs while the button is held, the buffer is reused, and nothing in the zone has to
    /// keep a list of what is lootable.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [Tooltip("Who is steering.")]
        [SerializeField]
        private PlayerInputReader? _input;

        [Tooltip("How far the character can reach, in world units.")]
        [SerializeField]
        [Min(0.2f)]
        private float _reach = 1.2f;

        private readonly Collider2D[] _nearby = new Collider2D[8];
        private ContactFilter2D _filter;
        private EventBus? _bus;
        private LootContainer? _busy;
        private string _lastPrompt = string.Empty;

        /// <summary>What is being searched right now, if anything.</summary>
        public LootContainer? Searching => _busy;

        private void Awake() =>
            _filter = new ContactFilter2D { useTriggers = true, useLayerMask = false };

        private void Start() => GameServices.TryGet(out _bus);

        private void Update()
        {
            LootContainer? target = FindNearest();

            if (_input is null || !_input.InteractHeld || target is null)
            {
                Release();
                Prompt(target is not null && !target.IsEmpty ? "Hold E to search" : string.Empty, 0f);
                return;
            }

            if (!ReferenceEquals(target, _busy))
            {
                Release();
                _busy = target;
            }

            if (target.IsEmpty)
            {
                Prompt(string.Empty, 0f);
                return;
            }

            target.Advance(Time.deltaTime);
            Prompt(target.IsEmpty ? string.Empty : "Searching", target.SearchFraction);
        }

        /// <summary>Drops a half-finished search.</summary>
        private void Release()
        {
            _busy?.Interrupt();
            _busy = null;
        }

        /// <summary>
        /// The nearest container within reach, or null.
        /// </summary>
        private LootContainer? FindNearest()
        {
            int count = Physics2D.OverlapCircle(transform.position, _reach, _filter, _nearby);

            LootContainer? best = null;
            float nearest = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                var container = _nearby[i].GetComponent<LootContainer>();
                if (container is null)
                {
                    continue;
                }

                float distance =
                    ((Vector2)container.transform.position - (Vector2)transform.position)
                    .sqrMagnitude;
                if (distance >= nearest)
                {
                    continue;
                }

                nearest = distance;
                best = container;
            }

            return best;
        }

        /// <summary>
        /// Says what the character could do, but only when it changed or while it is running.
        /// </summary>
        private void Prompt(string text, float fraction)
        {
            if (text.Length == 0 && _lastPrompt.Length == 0)
            {
                return;
            }

            _lastPrompt = text;
            _bus?.Publish(new InteractionPromptEvent(text, fraction));
        }
    }
}
