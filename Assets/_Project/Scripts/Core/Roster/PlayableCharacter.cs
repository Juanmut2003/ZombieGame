#nullable enable
using System;
using UnityEngine;

namespace ZombieGame.Core.Roster
{
    /// <summary>
    /// Somebody the player can be: a name, a face, and the drawings they walk with.
    /// </summary>
    /// <remarks>
    /// An asset rather than a class per character, so that adding somebody is art plus a menu
    /// click — the same bargain the zombie skins strike. The one who is not finished yet is a
    /// listed asset with <see cref="Available"/> switched off, which is why the menu can show
    /// them greyed out and honest instead of hiding the fact that there is a roster at all.
    /// Kept in Core because both halves need it and neither may reach the other: the zone puts
    /// these drawings on the character, and the menu shows the same assets as cards.
    /// </remarks>
    [CreateAssetMenu(
        fileName = "PlayableCharacter",
        menuName = "ZombieGame/Playable Character",
        order = 40)]
    public sealed class PlayableCharacter : ScriptableObject
    {
        [Tooltip("What the menu calls them.")]
        [SerializeField]
        private string _displayName = "Survivor";

        [Tooltip("One line about who they are.")]
        [SerializeField]
        [TextArea(1, 3)]
        private string _blurb = string.Empty;

        [Tooltip("Whether they can be picked yet. Off means the menu shows them as to come.")]
        [SerializeField]
        private bool _available = true;

        [Tooltip("The picture on the card in the menu.")]
        [SerializeField]
        private Sprite? _portrait;

        [Tooltip("Facing the camera. The first frame is the one they stand in.")]
        [SerializeField]
        private Sprite[] _front = Array.Empty<Sprite>();

        [Tooltip("Walking away from the camera. May be empty.")]
        [SerializeField]
        private Sprite[] _back = Array.Empty<Sprite>();

        [Tooltip("Walking left.")]
        [SerializeField]
        private Sprite[] _left = Array.Empty<Sprite>();

        [Tooltip("Walking right.")]
        [SerializeField]
        private Sprite[] _right = Array.Empty<Sprite>();

        /// <summary>What the menu calls them.</summary>
        public string DisplayName => _displayName;

        /// <summary>One line about who they are.</summary>
        public string Blurb => _blurb;

        /// <summary>Whether they can be picked yet.</summary>
        public bool Available => _available;

        /// <summary>The picture on the card in the menu.</summary>
        public Sprite? Portrait => _portrait;

        /// <summary>Facing the camera.</summary>
        public Sprite[] Front => _front;

        /// <summary>Walking away from the camera.</summary>
        public Sprite[] Back => _back;

        /// <summary>Walking left.</summary>
        public Sprite[] Left => _left;

        /// <summary>Walking right.</summary>
        public Sprite[] Right => _right;

        /// <summary>True when there is enough here to put somebody on screen.</summary>
        public bool CanBeDrawn => _front.Length > 0;
    }
}
