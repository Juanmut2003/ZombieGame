#nullable enable
using System;
using UnityEngine;

namespace ZombieGame.Economy
{
    /// <summary>So much of one thing: the unit a price or a yield is written in.</summary>
    /// <remarks>
    /// Serializable, because this is what a designer fills a building's cost list with in the
    /// inspector. Whole numbers only — half a plank is not a thing a colony can owe.
    /// </remarks>
    [Serializable]
    public struct ResourceAmount
    {
        [Tooltip("Which store it comes out of.")]
        [SerializeField]
        private ResourceKind _kind;

        [Tooltip("How much of it.")]
        [SerializeField]
        [Min(0)]
        private int _amount;

        public ResourceAmount(ResourceKind kind, int amount)
        {
            _kind = kind;
            _amount = Mathf.Max(0, amount);
        }

        /// <summary>Which store it comes out of.</summary>
        public readonly ResourceKind Kind => _kind;

        /// <summary>How much of it. Never negative.</summary>
        public readonly int Amount => Mathf.Max(0, _amount);

        /// <inheritdoc />
        public readonly override string ToString() => $"{Amount} {Kind}";
    }
}
