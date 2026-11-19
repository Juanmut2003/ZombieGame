#nullable enable
using UnityEngine;
using ZombieGame.Combat;
using ZombieGame.Core.Config;

namespace ZombieGame.Characters
{
    /// <summary>
    /// Whether the character is standing up or keeping low, and what that costs.
    /// </summary>
    /// <remarks>
    /// Its own component rather than a flag on the movement, because crouching is not a movement
    /// idea: it changes how fast you go, how loud you are and how far away you can be seen, and
    /// three different systems want one of those three each. Putting it here means none of them
    /// has to know about the other two.
    /// PROJECT.md §9 asks for exactly these three — quiet, slower, smaller detection cone — and
    /// all three numbers come from the balance asset.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class CharacterStance : MonoBehaviour, IConcealable
    {
        [Tooltip("Where the crouch numbers come from.")]
        [SerializeField]
        private BalanceConfig? _balance;

        [Tooltip("What says whether the crouch input is held.")]
        [SerializeField]
        private PlayerInputReader? _input;

        /// <summary>True while the character is keeping low.</summary>
        public bool IsCrouching { get; private set; }

        /// <summary>What the current stance multiplies movement speed by.</summary>
        public float SpeedFactor => IsCrouching ? _balance!.CrouchSpeedFactor : 1f;

        /// <inheritdoc />
        public float Visibility => IsCrouching ? _balance!.CrouchVisibility : 1f;

        private void Awake()
        {
            if (_balance is null)
            {
                Debug.LogWarning($"[Stance] {name} has no BalanceConfig; using the defaults.");
                _balance = BalanceConfig.CreateDefault();
            }
        }

        private void Update()
        {
            if (_input != null)
            {
                IsCrouching = _input.CrouchHeld;
            }
        }

        /// <summary>Sets the stance directly, for tests and for anything scripted.</summary>
        public void SetCrouching(bool crouching) => IsCrouching = crouching;
    }
}
