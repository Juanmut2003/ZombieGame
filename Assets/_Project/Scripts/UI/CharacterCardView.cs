#nullable enable
using UnityEngine;
using UnityEngine.UI;
using ZombieGame.Core.Roster;

namespace ZombieGame.UI
{
    /// <summary>
    /// One card in the character select: a face, a name, and whether you can have them.
    /// </summary>
    /// <remarks>
    /// Holds no logic beyond putting a character's own words on screen and showing which state
    /// it is in. Who may be picked is <see cref="CharacterSelection"/>'s to say, and what
    /// happens when one is picked is the presenter's.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class CharacterCardView : MonoBehaviour
    {
        [Tooltip("The whole card, which is also the button.")]
        [SerializeField]
        private Button? _button;

        [Tooltip("The character's picture.")]
        [SerializeField]
        private Image? _portrait;

        [Tooltip("Their name.")]
        [SerializeField]
        private Text? _name;

        [Tooltip("One line about them, or what they are waiting on.")]
        [SerializeField]
        private Text? _blurb;

        [Tooltip("The frame drawn round the card while it is the one picked.")]
        [SerializeField]
        private Image? _highlight;

        [Tooltip("What an unavailable card's picture and text are tinted with.")]
        [SerializeField]
        private Color _dimmed = new(0.42f, 0.44f, 0.42f, 1f);

        [Tooltip("What an available card's picture and text are tinted with.")]
        [SerializeField]
        private Color _ready = new(0.88f, 0.88f, 0.84f, 1f);

        /// <summary>Who this card is for.</summary>
        public PlayableCharacter? Character { get; private set; }

        /// <summary>The button, for the presenter to listen to.</summary>
        public Button? Button => _button;

        /// <summary>True once the card is showing somebody who can be picked.</summary>
        public bool IsPickable => Character != null && Character.Available;

        /// <summary>Puts a character on the card.</summary>
        public void Show(PlayableCharacter? character)
        {
            Character = character;

            if (character == null)
            {
                gameObject.SetActive(false);
                return;
            }

            bool ready = character.Available;
            Color tint = ready ? _ready : _dimmed;

            if (_name != null)
            {
                _name.text = character.DisplayName.ToUpperInvariant();
                _name.color = tint;
            }

            if (_blurb != null)
            {
                // The waiting ones say so on the card rather than being left off the roster.
                // A menu with one name on it looks like a game with one character; a menu with
                // a name and a COMING SOON looks like a game with a cast.
                _blurb.text = ready ? character.Blurb : "COMING SOON";
                _blurb.color = tint;
            }

            if (_portrait != null)
            {
                _portrait.sprite = character.Portrait;
                _portrait.enabled = character.Portrait != null;
                _portrait.color = tint;
            }

            if (_button != null)
            {
                _button.interactable = ready;
            }

            SetPicked(false);
        }

        /// <summary>Draws the card as the one chosen, or not.</summary>
        public void SetPicked(bool picked)
        {
            if (_highlight != null)
            {
                _highlight.enabled = picked;
            }
        }
    }
}
