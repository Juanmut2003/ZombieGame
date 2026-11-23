#nullable enable
using UnityEngine;
using ZombieGame.Core;
using ZombieGame.Core.Roster;

namespace ZombieGame.Characters
{
    /// <summary>
    /// Dresses a character in somebody from the roster the player did not take.
    /// </summary>
    /// <remarks>
    /// The other side of <see cref="CharacterOutfit"/>. The menu offers a choice between people,
    /// and the one left behind is the colony's first survivor — so whichever card the player
    /// presses, the figure already living in the base is the other one, and the base never shows
    /// the player twice.
    /// Runs in Start for the same reason the outfit does: the choice is registered during the
    /// boot scene, and a scene loaded straight afterwards would otherwise ask before anybody had
    /// answered. With nobody picked — a scene opened on its own in the editor — the first person
    /// on the roster is worn, so the plot is never empty.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterFacing))]
    public sealed class SpareOutfit : MonoBehaviour
    {
        [Tooltip("Everybody the menu offers, in the order it offers them.")]
        [SerializeField]
        private PlayableCharacter?[] _roster = System.Array.Empty<PlayableCharacter?>();

        [Tooltip("Who the MC beside them is when nobody has picked. Never worn.")]
        [SerializeField]
        private PlayableCharacter? _insteadOf;

        private CharacterFacing _facing = null!;

        /// <summary>Who is actually being worn, once Start has run.</summary>
        public PlayableCharacter? Wearing { get; private set; }

        private void Awake() => _facing = GetComponent<CharacterFacing>();

        private void Start()
        {
            // With nobody picked — the colony opened straight from the editor — the figure
            // beside them is wearing the scene's own fallback, so that is the person to avoid.
            // Without this, opening the base in the editor shows one character twice.
            PlayableCharacter? taken = _insteadOf == null ? null : _insteadOf;
            if (GameServices.TryGet(out CharacterSelection? selection)
                && selection is not null
                && selection.Chosen != null)
            {
                taken = selection.Chosen;
            }

            PlayableCharacter? who = RosterChoice.SomebodyElse(_roster, taken);
            if (who == null)
            {
                Debug.LogWarning($"[Outfit] {name} has nobody left on the roster to be.");
                return;
            }

            Wearing = who;
            _facing.Wear(who.Front, who.Back, who.Left, who.Right);
        }
    }
}
