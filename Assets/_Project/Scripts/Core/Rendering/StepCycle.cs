#nullable enable
using UnityEngine;

namespace ZombieGame.Core.Rendering
{
    /// <summary>
    /// Where a walking figure is in its stride, and the little rise and sway that go with it.
    /// </summary>
    /// <remarks>
    /// The art ships one frame per direction, so nothing can be animated by swapping pictures.
    /// What can be done without art is move the picture: a walking body rises between footfalls
    /// and settles onto the planted foot, and leans a little towards it. Two pixels of lift and
    /// a couple of degrees of sway are enough to stop a figure reading as a chess piece being
    /// slid across the board, which is what every figure in the game did before this.
    /// A <i>cycle</i> here is two footfalls — left then right — so phase 0 and phase 0.5 are the
    /// moments a foot lands. That is deliberately the same beat the footstep sound is on: the
    /// body is at its lowest exactly when the step is heard, and a sound that lands on a visible
    /// footfall is the difference between a noise and a step.
    /// Pure maths on purpose. Hundreds of zombies ask it the same question every frame, and it
    /// is the sort of thing that is easy to get subtly wrong and impossible to spot by eye.
    /// </remarks>
    public static class StepCycle
    {
        /// <summary>A full turn, for the sine the rise and the sway are both built on.</summary>
        private const float Tau = 6.2831853f;

        /// <summary>
        /// Brings any phase back into 0 to 1, keeping where in the stride it was.
        /// </summary>
        public static float Wrap(float phase)
        {
            float wrapped = phase - Mathf.Floor(phase);

            // Floor already handles negatives, but a phase of exactly -0 or a denormal can come
            // back as 1 after the subtraction, and a phase of 1 is a phase of 0 one step late.
            return wrapped >= 1f ? 0f : wrapped;
        }

        /// <summary>
        /// The phase of something that has walked <paramref name="distance"/> world units.
        /// </summary>
        /// <param name="distance">How far the figure has travelled, in world units.</param>
        /// <param name="stride">How far one footfall carries it. Zero or less stands still.</param>
        /// <remarks>
        /// For the horde, which has no footstep sound to agree with. Tying their gait to the
        /// ground they cover rather than to a clock means a bloater lumbering at half speed bobs
        /// at half the rate for free, and one held up by the crowd stops bobbing while it is
        /// stuck instead of marching on the spot.
        /// </remarks>
        public static float FromDistance(float distance, float stride)
        {
            if (stride <= 0f)
            {
                return 0f;
            }

            // Two footfalls to the cycle, so a stride is half of one.
            return Wrap(distance / (stride * 2f));
        }

        /// <summary>
        /// The phase of something that has been walking for <paramref name="seconds"/>.
        /// </summary>
        /// <param name="seconds">How long the figure has been on the move.</param>
        /// <param name="stepSeconds">Seconds between footfalls. Zero or less stands still.</param>
        /// <remarks>
        /// For the squad, whose footsteps are already on a timer that the noise system and the
        /// audio both read. Driving the gait from the same number is what keeps the body's dip
        /// and the sound of the boot on the same frame at a walk, a sprint and a crouch alike.
        /// </remarks>
        public static float FromTime(float seconds, float stepSeconds)
        {
            if (stepSeconds <= 0f)
            {
                return 0f;
            }

            return Wrap(seconds / (stepSeconds * 2f));
        }

        /// <summary>
        /// Which drawing of a walk cycle belongs at this point in the stride.
        /// </summary>
        /// <param name="phase">Where in the stride the figure is, 0 to 1.</param>
        /// <param name="count">How many drawings the cycle has.</param>
        /// <remarks>
        /// The frames are spread evenly over the cycle rather than played on a clock of their
        /// own, so a six-frame walk and a two-frame one both finish exactly as the foot lands.
        /// A set with one drawing in it always answers zero, which is how a character whose art
        /// is a single pose keeps working: the lift below is then the whole of the animation.
        /// </remarks>
        public static int Frame(float phase, int count)
        {
            if (count <= 1)
            {
                return 0;
            }

            int index = Mathf.FloorToInt(Wrap(phase) * count);
            return index >= count ? count - 1 : index;
        }

        /// <summary>
        /// How far the body is lifted at this point in the stride, in world units.
        /// </summary>
        /// <remarks>
        /// Zero at both footfalls and highest halfway between, which is what a body actually
        /// does: lowest when the weight is shared between two feet, highest when it is riding
        /// over one straight leg. Never negative — a figure may rise off the ground it stands on
        /// but must not sink into it.
        /// </remarks>
        public static float Bob(float phase, float height) =>
            height <= 0f ? 0f : Mathf.Abs(Mathf.Sin(Wrap(phase) * Tau)) * height;

        /// <summary>
        /// How far the body is tipped at this point in the stride, in degrees.
        /// </summary>
        /// <remarks>
        /// One way for the first footfall and the other way for the second, so the figure rocks
        /// between its feet rather than wobbling in place. Signed, unlike the lift.
        /// </remarks>
        public static float Lean(float phase, float degrees) =>
            degrees == 0f ? 0f : Mathf.Sin(Wrap(phase) * Tau) * degrees;
    }
}
