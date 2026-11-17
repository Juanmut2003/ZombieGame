#nullable enable

namespace ZombieGame.Core.Rendering
{
    /// <summary>
    /// The numbers the whole look of the game hangs on: how many pixels a unit is, and how many
    /// of them the camera shows.
    /// </summary>
    /// <remarks>
    /// These are not balance values and not designer-facing — changing one changes how every
    /// sprite, tile and collider lines up, so CLAUDE.md asks for a design discussion before any
    /// of them move. They live here so the importer, the camera rig and the UI all read the same
    /// source instead of each carrying their own copy.
    /// 540 divides 1080 exactly twice, so the target resolution upscales without a remainder. The
    /// visible area is 15 by 8.4 tiles, the same as before the scale went up.
    /// </remarks>
    public static class PixelPerfectSettings
    {
        /// <summary>Pixels per world unit. Every sprite is imported at this scale.</summary>
        public const int PixelsPerUnit = 64;

        /// <summary>How many pixels wide the camera shows.</summary>
        public const int ReferenceWidth = 960;

        /// <summary>How many pixels tall the camera shows.</summary>
        public const int ReferenceHeight = 540;

        /// <summary>
        /// The orthographic half-height that shows exactly <see cref="ReferenceHeight"/> pixels.
        /// </summary>
        /// <remarks>
        /// Unity measures orthographic size as half the visible height in world units, which is
        /// why this is divided by two as well as by the pixels per unit.
        /// </remarks>
        public static float OrthographicSize =>
            OrthographicSizeFor(ReferenceHeight, PixelsPerUnit);

        /// <summary>The orthographic half-height for an arbitrary pixel height and scale.</summary>
        public static float OrthographicSizeFor(int referenceHeight, int pixelsPerUnit) =>
            referenceHeight / 2f / pixelsPerUnit;
    }
}
