#nullable enable
using UnityEngine;

namespace ZombieGame.Base
{
    /// <summary>
    /// Where somebody working in a building stands.
    /// </summary>
    /// <remarks>
    /// A post for every job the building has, spread along the back of the room — which is where
    /// the art puts the beds, the benches and the shelves. Relative to the building's middle and
    /// in world units, so a building that grows a cell when it is improved moves its posts with
    /// it rather than leaving its workers standing in a wall.
    /// Pure maths. Who is at which post is the colony's business (§4.3); this only answers where
    /// that post is.
    /// </remarks>
    public static class BuildingPosts
    {
        /// <summary>
        /// Where the <paramref name="slot"/>th of <paramref name="slots"/> workers stands,
        /// relative to the building's middle.
        /// </summary>
        public static Vector2 At(BuildingFootprint footprint, int slot, int slots)
        {
            Rect room = BuildingWalls.Room(footprint);
            int posts = Mathf.Max(1, slots);
            int which = Mathf.Clamp(slot, 0, posts - 1);

            // Spread across the room by the middle of each share of it, so two posts sit a third
            // and two thirds of the way along rather than both against the same wall.
            float across = (which + 0.5f) / posts;
            float x = room.xMin + (across * room.width);

            // Half a cell inside the back of the room: against the furniture, not on top of the
            // doorway, and never outside a room too shallow to have a back.
            float y = Mathf.Max(room.yMin, room.yMax - 0.5f);
            return new Vector2(x, y);
        }
    }
}
