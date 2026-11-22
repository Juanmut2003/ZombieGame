#nullable enable
using System;
using UnityEngine;
using UnityEngine.UI;
using ZombieGame.Core;
using ZombieGame.Core.Roster;

namespace ZombieGame.UI
{
    /// <summary>
    /// The character select: who is on the roster, who you may be, and the button that starts.
    /// </summary>
    /// <remarks>
    /// Picks the first character who is actually available as soon as the menu opens, so that
    /// pressing start without touching anything does the obvious thing. With one name on the
    /// roster finished, that is the whole of the interaction — but the roster, the cards and
    /// the refusal to start without a choice are all real, so the second character is an asset
    /// and some art rather than a rewrite.
    /// No gameplay type knows this class exists; it writes the choice into
    /// <see cref="CharacterSelection"/>, which the zone reads for itself.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class CharacterSelectPresenter : MonoBehaviour
    {
        [Tooltip("Everybody the game knows about, in the order they are shown.")]
        [SerializeField]
        private PlayableCharacter[] _roster = Array.Empty<PlayableCharacter>();

        [Tooltip("The cards, in the same order. Spare cards are switched off.")]
        [SerializeField]
        private CharacterCardView[] _cards = Array.Empty<CharacterCardView>();

        [Tooltip("Says who is picked, under the cards.")]
        [SerializeField]
        private Text? _chosenLabel;

        [Tooltip("Starts the run. Dead until somebody is picked.")]
        [SerializeField]
        private Button? _startButton;

        private CharacterSelection? _selection;

        /// <summary>Who is picked right now, if anybody.</summary>
        public PlayableCharacter? Picked => _selection?.Chosen;

        /// <summary>True when the run can be started.</summary>
        public bool CanStart => _selection is not null && _selection.HasChoice;

        private void Awake()
        {
            // The menu can be opened on its own in the editor, with no Boot behind it. Then the
            // choice has nowhere to live, so it lives here for as long as the menu does.
            if (!GameServices.TryGet(out _selection) || _selection is null)
            {
                _selection = new CharacterSelection();
            }

            for (int i = 0; i < _cards.Length; i++)
            {
                CharacterCardView card = _cards[i];
                if (card == null)
                {
                    continue;
                }

                PlayableCharacter? character = i < _roster.Length ? _roster[i] : null;
                card.Show(character);

                if (card.Button != null)
                {
                    // Captured per card rather than read back from the event, which does not
                    // say which button it came from.
                    CharacterCardView captured = card;
                    card.Button.onClick.AddListener(() => Pick(captured.Character));
                }
            }

            PickFirstAvailable();
        }

        private void OnDestroy()
        {
            foreach (CharacterCardView card in _cards)
            {
                if (card != null && card.Button != null)
                {
                    card.Button.onClick.RemoveAllListeners();
                }
            }
        }

        /// <summary>Picks a character. Anyone not yet available is ignored.</summary>
        public void Pick(PlayableCharacter? character)
        {
            if (_selection is null || !_selection.Choose(character))
            {
                return;
            }

            Refresh();
        }

        private void PickFirstAvailable()
        {
            foreach (PlayableCharacter character in _roster)
            {
                if (character != null && character.Available && character.CanBeDrawn)
                {
                    Pick(character);
                    return;
                }
            }

            Refresh();
        }

        private void Refresh()
        {
            PlayableCharacter? chosen = _selection?.Chosen;

            foreach (CharacterCardView card in _cards)
            {
                if (card != null)
                {
                    card.SetPicked(card.Character != null && card.Character == chosen);
                }
            }

            if (_chosenLabel != null)
            {
                _chosenLabel.text = chosen == null
                    ? "CHOOSE SOMEBODY"
                    : chosen.DisplayName.ToUpperInvariant();
            }

            if (_startButton != null)
            {
                _startButton.interactable = CanStart;
            }
        }
    }
}
