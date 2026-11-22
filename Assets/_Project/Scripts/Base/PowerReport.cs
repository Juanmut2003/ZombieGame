#nullable enable

namespace ZombieGame.Base
{
    /// <summary>What the grid came to on a given day.</summary>
    public readonly struct PowerReport
    {
        public PowerReport(int supply, int wanted, int met)
        {
            Supply = supply;
            Wanted = wanted;
            Met = met;
        }

        /// <summary>How much the generators put in.</summary>
        public int Supply { get; }

        /// <summary>How much everything standing would draw if it all ran.</summary>
        public int Wanted { get; }

        /// <summary>How much was actually drawn by the buildings that got power.</summary>
        public int Met { get; }

        /// <summary>How much was asked for and not there.</summary>
        public int Shortfall => Wanted - Met;

        /// <summary>True when everything that wanted power got it.</summary>
        public bool Lit => Shortfall <= 0;
    }
}
