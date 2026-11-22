#nullable enable

namespace ZombieGame.Base
{
    /// <summary>Whether a building may go here, and if not, why not.</summary>
    /// <remarks>
    /// A reason rather than a bool, because the player is holding a building against the ground
    /// and needs to be told what is wrong with the spot. "No" on its own is the kind of refusal
    /// that has people dragging a barn around a field for a minute looking for the invisible
    /// rock.
    /// </remarks>
    public enum PlacementVerdict
    {
        /// <summary>It fits.</summary>
        Ok = 0,

        /// <summary>Part of it hangs off the claimed ground.</summary>
        OutsideTheSite = 1,

        /// <summary>Something about the ground itself: rock, water, a road, a standing wall.</summary>
        GroundIsBlocked = 2,

        /// <summary>Another building is already standing there.</summary>
        SomethingIsThere = 3,

        /// <summary>
        /// The stores will not cover it. The one reason here that is not about the ground.
        /// </summary>
        /// <remarks>
        /// Kept in the same list rather than reported separately, because from where the player
        /// is standing there is one question — can I put this here — and one place they look
        /// for the answer. The site itself never returns this; only the placer does, which is
        /// the thing that knows what the colony has.
        /// </remarks>
        CannotAfford = 4,
    }
}
