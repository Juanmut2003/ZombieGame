#nullable enable

namespace ZombieGame.Base
{
    /// <summary>What a day at the base came to.</summary>
    /// <remarks>
    /// Returned rather than logged, so the screen can say what happened and a test can check it.
    /// A colony that silently loses a day of food is the hardest kind of bug to notice.
    /// </remarks>
    public readonly struct DayReport
    {
        public DayReport(
            int worked,
            int idle,
            int foodGrown,
            int foodEaten,
            int wentHungry,
            int wentThirsty,
            int darkened,
            PowerReport power)
        {
            Worked = worked;
            Idle = idle;
            FoodGrown = foodGrown;
            FoodEaten = foodEaten;
            WentHungry = wentHungry;
            WentThirsty = wentThirsty;
            Darkened = darkened;
            Power = power;
        }

        /// <summary>How many people did a day's work.</summary>
        public int Worked { get; }

        /// <summary>How many had nothing to do, or nothing working to do it in.</summary>
        public int Idle { get; }

        /// <summary>How much food came in.</summary>
        public int FoodGrown { get; }

        /// <summary>How much food was eaten.</summary>
        public int FoodEaten { get; }

        /// <summary>How many people did not get fed.</summary>
        public int WentHungry { get; }

        /// <summary>How many people did not get a drink.</summary>
        public int WentThirsty { get; }

        /// <summary>How many buildings stood dark for want of power.</summary>
        public int Darkened { get; }

        /// <summary>What the grid came to.</summary>
        public PowerReport Power { get; }

        /// <summary>True when the colony fed and watered itself.</summary>
        public bool Fed => WentHungry == 0 && WentThirsty == 0;
    }
}
