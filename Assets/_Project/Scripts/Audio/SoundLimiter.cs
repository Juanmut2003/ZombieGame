#nullable enable
using System;

namespace ZombieGame.Audio
{
    /// <summary>
    /// Decides which of the sounds the game asks for actually get played.
    /// </summary>
    /// <remarks>
    /// A shotgun into a crowd is twenty impacts in one frame. Played as twenty, they land on the
    /// same sample at the same moment and sum into one flat crack at twenty times the volume —
    /// louder, uglier and less informative than one. The same goes for a horde dying together.
    /// So two rules. The same sound cannot restart within a short gap, and there is a ceiling on
    /// how many play at once. Both are plain arithmetic with a clock passed in, which is what
    /// lets them be tested without a scene, a speaker or a frame.
    /// Deliberately not a queue: a sound that was too late is dropped rather than delayed. A
    /// delayed impact is a lie about when the thing happened.
    /// </remarks>
    public sealed class SoundLimiter
    {
        private readonly float _gapSeconds;
        private readonly int _atOnce;
        private readonly float[] _lastPlayed;
        private readonly float[] _endsAt;

        /// <param name="gapSeconds">
        /// How long the same sound must wait before it may start again. Zero lets everything
        /// through, which is what a test wants when it is measuring something else.
        /// </param>
        /// <param name="atOnce">The most sounds that may be sounding at the same time.</param>
        public SoundLimiter(float gapSeconds = 0.06f, int atOnce = 16)
        {
            if (gapSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(gapSeconds), gapSeconds, "A gap cannot run backwards.");
            }

            if (atOnce <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(atOnce), atOnce, "Silence is not a sound system.");
            }

            _gapSeconds = gapSeconds;
            _atOnce = atOnce;
            _lastPlayed = new float[Enum.GetValues(typeof(GameSound)).Length];
            _endsAt = new float[atOnce];

            for (int i = 0; i < _lastPlayed.Length; i++)
            {
                _lastPlayed[i] = float.NegativeInfinity;
            }
        }

        /// <summary>How many are sounding right now, as of the last question asked.</summary>
        public int Sounding { get; private set; }

        /// <summary>
        /// Whether <paramref name="sound"/> may start now, and books it in if so.
        /// </summary>
        /// <param name="sound">What is being asked for.</param>
        /// <param name="now">The time, in seconds.</param>
        /// <param name="lengthSeconds">How long it would sound for.</param>
        public bool MayPlay(GameSound sound, float now, float lengthSeconds = 0.25f)
        {
            // Retired first, whatever was asked for. Counting what has finished is about the
            // clock rather than about the question, and doing it only for real requests left
            // Sounding reporting whatever was true the last time somebody asked for a noise.
            int free = Retire(now);

            if (sound == GameSound.None)
            {
                return false;
            }

            int index = (int)sound;

            if (index < 0 || index >= _lastPlayed.Length)
            {
                return false;
            }

            if (now - _lastPlayed[index] < _gapSeconds)
            {
                return false;
            }

            if (free < 0)
            {
                return false;
            }

            _lastPlayed[index] = now;
            _endsAt[free] = now + Math.Max(0f, lengthSeconds);
            Sounding++;
            return true;
        }

        /// <summary>Forgets everything, for the start of a run and for tests.</summary>
        public void Reset()
        {
            for (int i = 0; i < _lastPlayed.Length; i++)
            {
                _lastPlayed[i] = float.NegativeInfinity;
            }

            for (int i = 0; i < _endsAt.Length; i++)
            {
                _endsAt[i] = 0f;
            }

            Sounding = 0;
        }

        /// <summary>
        /// Counts what has finished and hands back a free slot, or −1 when they are all busy.
        /// </summary>
        private int Retire(float now)
        {
            int free = -1;
            int busy = 0;

            for (int i = 0; i < _atOnce; i++)
            {
                if (_endsAt[i] > now)
                {
                    busy++;
                    continue;
                }

                if (free < 0)
                {
                    free = i;
                }
            }

            Sounding = busy;
            return free;
        }
    }
}
