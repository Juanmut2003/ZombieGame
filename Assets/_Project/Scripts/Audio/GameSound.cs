#nullable enable

namespace ZombieGame.Audio
{
    /// <summary>Every sound the game asks for, by what it means rather than by its file name.</summary>
    /// <remarks>
    /// Code says "a shot was fired", not "play Range.wav". Which file that is, how loud, and how
    /// many variants it has are answered by the <see cref="SoundBank"/> asset, so swapping the
    /// whole set is a change to an asset and not to any of this.
    /// </remarks>
    public enum GameSound
    {
        /// <summary>Nothing. The default, so an unfilled field is silent rather than wrong.</summary>
        None = 0,

        /// <summary>A melee weapon swung, hit or not.</summary>
        MeleeSwing = 1,

        /// <summary>A firearm went off.</summary>
        Gunshot = 2,

        /// <summary>Something took a blow and survived it.</summary>
        Impact = 3,

        /// <summary>A zombie went down.</summary>
        ZombieDied = 4,

        /// <summary>The squad was hurt.</summary>
        SquadHurt = 5,

        /// <summary>Supplies came out of a container.</summary>
        LootTaken = 6,

        /// <summary>A different weapon came off the belt.</summary>
        WeaponSwitched = 7,

        /// <summary>The squad got out.</summary>
        MissionWon = 8,

        /// <summary>The squad did not.</summary>
        MissionLost = 9,
    }

    /// <summary>Which volume slider a sound answers to.</summary>
    public enum SoundBus
    {
        /// <summary>Everything that happens in the world.</summary>
        Effects = 0,

        /// <summary>Menus, the belt, anything the player did rather than the world.</summary>
        Interface = 1,

        /// <summary>Music.</summary>
        Music = 2,
    }
}
