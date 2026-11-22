#nullable enable
namespace ZombieGame.Core.Input
{
    /// <summary>
    /// Whether the mouse is currently over something on the HUD.
    /// </summary>
    /// <remarks>
    /// A click on a button is also a click on the ground behind it. Without somewhere to say so,
    /// pressing TEAR DOWN on the building menu also puts a shed down on whatever the wheel was
    /// drawn over, and the player's last act is undone by the same gesture that asked for it.
    /// The HUD is the only thing that can answer this — it is the side that knows what is under
    /// the pointer — so the UI sets it and gameplay reads it, which keeps the dependency
    /// pointing the way round the project allows. Gameplay with no HUD finds nothing registered
    /// here and carries on as though the pointer were always free, which is correct.
    /// </remarks>
    public sealed class PointerFocus
    {
        /// <summary>True while the pointer is over a part of the HUD that handles clicks.</summary>
        public bool OverTheHud { get; set; }
    }
}
