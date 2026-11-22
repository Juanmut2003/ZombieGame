#nullable enable
using System.Collections.Generic;
using UnityEngine;
using ZombieGame.Core;
using ZombieGame.Core.Events;
using ZombieGame.Economy;
using ZombieGame.Waves;

namespace ZombieGame.Missions
{
    /// <summary>
    /// A shelf, a crate or a locker: something with supplies in it that takes a moment to go
    /// through.
    /// </summary>
    /// <remarks>
    /// The moment is the whole design. Loot that is picked up by walking over it would make a
    /// zone a race; loot that has to be stood next to, for seconds, with the noise it makes, is a
    /// decision about where the zombies are right now. That is the loop PROJECT.md §8 asks for.
    /// Searching is driven from outside — whoever is doing it advances the timer — so the same
    /// container works for the player, for a survivor sent to loot, and for a test.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class LootContainer : MonoBehaviour
    {
        [Tooltip("What is inside, by store. The mission's loot table fills this in.")]
        [SerializeField]
        private ResourceAmount[] _contents =
        {
            new(ResourceKind.Materials, 2),
        };

        [Tooltip("And in rounds of ammunition.")]
        [SerializeField]
        [Min(0)]
        private int _ammo;

        [Tooltip("How long it takes to go through.")]
        [SerializeField]
        [Min(0.1f)]
        private float _searchSeconds = 1.5f;

        [Tooltip("How loud the rummaging is. Quiet, but not silent.")]
        [SerializeField]
        [Min(0f)]
        private float _noise = 0.8f;

        [Tooltip("What it looks like once it has been emptied.")]
        [SerializeField]
        private Sprite? _emptiedSprite;

        private SpriteRenderer? _renderer;
        private EventBus? _bus;
        private float _searched;

        /// <summary>True once there is nothing left in it.</summary>
        public bool IsEmpty { get; private set; }

        /// <summary>What is inside, by store.</summary>
        public IReadOnlyList<ResourceAmount> Contents => _contents;

        /// <summary>What the display name of this container is, for the prompt.</summary>
        public string Label => IsEmpty ? "Empty" : "Search";

        /// <summary>How far through a search this is, 0 to 1.</summary>
        public float SearchFraction =>
            _searchSeconds <= 0f ? 1f : Mathf.Clamp01(_searched / _searchSeconds);

        private void Awake() => _renderer = GetComponent<SpriteRenderer>();

        private void Start() => GameServices.TryGet(out _bus);

        /// <summary>
        /// Spends a moment going through it, and empties it once the moment is up.
        /// </summary>
        /// <returns>True on the step it is emptied.</returns>
        public bool Advance(float deltaSeconds)
        {
            if (IsEmpty || deltaSeconds <= 0f)
            {
                return false;
            }

            _searched += deltaSeconds;
            if (_searched < _searchSeconds)
            {
                return false;
            }

            Empty();
            return true;
        }

        /// <summary>Forgets a half-finished search. Walking away costs the progress.</summary>
        public void Interrupt() => _searched = 0f;

        private void Empty()
        {
            IsEmpty = true;
            _searched = 0f;

            if (_emptiedSprite != null && _renderer is not null)
            {
                _renderer.sprite = _emptiedSprite;
            }

            _bus?.Publish(new LootTakenEvent(transform.position, _contents, _ammo));

            // Rummaging is heard. Not as far as a shot, but far enough that looting a shelf with
            // something standing on the other side of the wall is a decision.
            _bus?.Publish(new NoiseEvent(transform.position, _noise));
        }
    }
}
