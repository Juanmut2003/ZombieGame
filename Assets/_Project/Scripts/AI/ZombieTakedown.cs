#nullable enable
using UnityEngine;
using ZombieGame.Combat;

namespace ZombieGame.AI
{
    /// <summary>Answers for the horde when somebody creeps up behind one of them.</summary>
    /// <remarks>
    /// The twin of <see cref="ZombieHitTest"/>, and there for the same reason: whoever is doing
    /// the creeping asks the service locator rather than holding the horde. A character that knew
    /// about <see cref="ZombieManager"/> would be a character that could only ever sneak up on
    /// zombies.
    /// </remarks>
    public sealed class ZombieTakedown : ISilentKill
    {
        private readonly ZombieManager _horde;

        public ZombieTakedown(ZombieManager horde) => _horde = horde;

        /// <inheritdoc />
        public bool CanTakedown(float reach, float behindDegrees, Vector2 from) =>
            _horde.FindTakedown(from, reach, behindDegrees) >= 0;

        /// <inheritdoc />
        public bool TryTakedown(float reach, float behindDegrees, Vector2 from, out Vector2 where)
        {
            where = from;

            int index = _horde.FindTakedown(from, reach, behindDegrees);
            if (index < 0)
            {
                return false;
            }

            where = _horde.Get(index).Position;

            // Killed outright rather than damaged. A takedown that a tough zombie survives is a
            // takedown that turns into the fight it was meant to avoid, which would make the
            // whole move a gamble instead of a plan.
            _horde.Kill(index, new DamageInfo(0f, DamageType.Sharp));
            return true;
        }
    }
}
