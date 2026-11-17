#nullable enable
using UnityEngine;

namespace ZombieGame.AI
{
    /// <summary>
    /// One look a zombie can have: the six poses the art supports, as an asset.
    /// </summary>
    /// <remarks>
    /// Kept apart from <see cref="ZombieDefinition"/> because a kind and a look are different
    /// things. A Walker is a set of numbers; what it looks like is a set of pictures, and a zone
    /// full of Walkers that all wear the same face reads as a rendering fault no matter how
    /// differently they walk. A kind therefore holds several of these and hands one to each
    /// zombie at spawn.
    /// Adding a look is meant to be art plus nothing: drop
    /// <c>{kind}_{variant}_{pose}.png</c> into <c>Art/Sprites/Zombies</c> and run
    /// <c>ZombieGame/Content/Rebuild Zombie Skins</c>, which builds the asset and attaches it to
    /// the kind named by the prefix.
    /// </remarks>
    [CreateAssetMenu(menuName = "ZombieGame/Zombie Skin", fileName = "ZombieSkin")]
    public sealed class ZombieSkin : ScriptableObject
    {
        [Header("Calm")]
        [Tooltip("Facing the camera.")]
        [SerializeField]
        private Sprite? _front;

        [Tooltip("Walking to the left.")]
        [SerializeField]
        private Sprite? _left;

        [Tooltip("Walking to the right.")]
        [SerializeField]
        private Sprite? _right;

        [Header("Alerted")]
        [Tooltip("Facing the camera, arms up. Shown while it has noticed someone.")]
        [SerializeField]
        private Sprite? _frontAlert;

        [Tooltip("Lunging to the left.")]
        [SerializeField]
        private Sprite? _leftAlert;

        [Tooltip("Lunging to the right.")]
        [SerializeField]
        private Sprite? _rightAlert;

        /// <summary>True once there is something to draw at all.</summary>
        public bool HasArt => _front != null;

        /// <summary>
        /// The sprite for a facing, in the pose that matches whether it has noticed anyone.
        /// </summary>
        /// <remarks>
        /// An alerted zombie showing a different pose is the only way a player can tell they have
        /// been seen. Without it, perception is invisible: the first sign of being noticed would
        /// be a zombie arriving, which is far too late to do anything about.
        /// Every step falls back towards the front view, so a half-finished skin draws something
        /// rather than a hole.
        /// </remarks>
        public Sprite? SpriteFor(ZombieFacing facing, bool alerted = false)
        {
            if (!alerted)
            {
                return Calm(facing);
            }

            Sprite? alert = facing switch
            {
                ZombieFacing.Left => _leftAlert != null ? _leftAlert : _frontAlert,
                ZombieFacing.Right => _rightAlert != null ? _rightAlert : _frontAlert,
                _ => _frontAlert,
            };

            return alert != null ? alert : Calm(facing);
        }

        private Sprite? Calm(ZombieFacing facing) => facing switch
        {
            ZombieFacing.Left => _left != null ? _left : _front,
            ZombieFacing.Right => _right != null ? _right : _front,
            _ => _front,
        };
    }
}
