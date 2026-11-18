#nullable enable
using System;
using UnityEngine;

namespace ZombieGame.Combat
{
    /// <summary>
    /// What is in the gun and what is left in the pockets.
    /// </summary>
    /// <remarks>
    /// Kept apart from the component that fires, because counting rounds is the part that has
    /// edges worth being exact about: reloading a half-empty magazine, reloading with less than a
    /// magazine left, firing the last round. None of that needs a scene to be settled.
    /// Reloading tops the gun up and keeps what was in it. The stricter rule — a part-used
    /// magazine is thrown away — makes reloading early a punishment, and with one pistol and no
    /// inventory screen that is a trap rather than a decision. It can come back when magazines
    /// are items in their own right.
    /// </remarks>
    public sealed class Magazine
    {
        public Magazine(int capacity, int loaded, int reserve)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(capacity), capacity, "A magazine holds at least one round.");
            }

            if (loaded < 0 || loaded > capacity)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(loaded), loaded, "Cannot start with more rounds than it holds.");
            }

            if (reserve < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(reserve), reserve, "Cannot be negative.");
            }

            Capacity = capacity;
            Loaded = loaded;
            Reserve = reserve;
        }

        /// <summary>How many rounds a full magazine holds.</summary>
        public int Capacity { get; }

        /// <summary>How many are in the gun.</summary>
        public int Loaded { get; private set; }

        /// <summary>How many are in the pockets.</summary>
        public int Reserve { get; private set; }

        /// <summary>True when the gun is empty. Pulling the trigger now does nothing.</summary>
        public bool IsEmpty => Loaded <= 0;

        /// <summary>True when reloading would actually change anything.</summary>
        public bool CanReload => Reserve > 0 && Loaded < Capacity;

        /// <summary>Spends one round.</summary>
        /// <returns>False when the gun was empty.</returns>
        public bool TryFire()
        {
            if (Loaded <= 0)
            {
                return false;
            }

            Loaded--;
            return true;
        }

        /// <summary>
        /// Puts a fresh magazine in, as far as the pockets allow.
        /// </summary>
        /// <returns>How many rounds were loaded, zero when there was nothing to do.</returns>
        public int Reload()
        {
            if (!CanReload)
            {
                return 0;
            }

            int wanted = Math.Min(Capacity - Loaded, Reserve);
            Reserve -= wanted;
            Loaded += wanted;
            return wanted;
        }

        /// <summary>Adds found ammunition to the pockets.</summary>
        public void AddReserve(int rounds) => Reserve += Mathf.Max(0, rounds);
    }
}
