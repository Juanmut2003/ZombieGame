#nullable enable

namespace ZombieGame.Missions
{
    /// <summary>One line of a debrief: what it is, and what it came to.</summary>
    /// <remarks>
    /// A pair rather than a formatted string, so that the screen can set the two columns
    /// separately. The game's font is not monospaced, and a line padded with spaces to line its
    /// numbers up looks straight in a code editor and ragged everywhere else.
    /// </remarks>
    public readonly struct DebriefRow
    {
        public DebriefRow(string label, string value)
        {
            Label = label;
            Value = value;
        }

        /// <summary>What is being reported, in the game's shouting capitals.</summary>
        public string Label { get; }

        /// <summary>What it came to.</summary>
        public string Value { get; }
    }
}
