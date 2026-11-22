#nullable enable
using UnityEngine;
using ZombieGame.Core;
using ZombieGame.Core.Roster;

namespace ZombieGame.Characters
{
    /// <summary>
    /// Dresses the character in whoever the player picked in the menu.
    /// </summary>
    /// <remarks>
    /// The zone is built with somebody in it already, because a scene has to be openable on its
    /// own and a zone with an invisible MC is no use to anyone working on the map. This runs
    /// after that and swaps the drawings when, and only when, a choice was actually made — so
    /// pressing play on the town gives the character the scene was built with, and starting a
    /// run from the menu gives the one on the card.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterFacing))]
    public sealed class CharacterOutfit : MonoBehaviour
    {
        [Tooltip("Who to be when nobody has picked. Usually the character the zone was built with.")]
        [SerializeField]
        private PlayableCharacter? _fallback;

        private CharacterFacing _facing = null!;

        /// <summary>Who is actually being worn, once Start has run.</summary>
        public PlayableCharacter? Wearing { get; private set; }

        private void Awake() => _facing = GetComponent<CharacterFacing>();

        // Start, not Awake: the boot scene registers the selection in its own Awake, and a zone
        // loaded straight after would otherwise ask before anyone had answered.
        private void Start()
        {
            PlayableCharacter? character = Pick();
            if (character == null)
            {
                return;
            }

            Wearing = character;
            _facing.Wear(character.Front, character.Back, character.Left, character.Right);
        }

        private PlayableCharacter? Pick()
        {
            if (GameServices.TryGet(out CharacterSelection? selection)
                && selection is not null
                && selection.HasChoice)
            {
                return selection.Chosen;
            }

            return _fallback == null ? null : _fallback;
        }
    }
}
