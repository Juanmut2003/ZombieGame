#nullable enable

namespace ZombieGame.AI
{
    /// <summary>Which way a zombie is facing, for picking its sprite.</summary>
    /// <remarks>
    /// Three poses, not eight: the art has front and both sides, and a top-down figure walking
    /// away reads well enough from the front view.
    /// </remarks>
    public enum ZombieFacing
    {
        /// <summary>Towards the camera.</summary>
        Front,

        /// <summary>Moving left.</summary>
        Left,

        /// <summary>Moving right.</summary>
        Right,
    }
}
