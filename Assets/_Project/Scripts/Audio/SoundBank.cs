#nullable enable
using System;
using UnityEngine;

namespace ZombieGame.Audio
{
    /// <summary>One entry: which clips a sound may use, how loud, and on which bus.</summary>
    [Serializable]
    public sealed class SoundEntry
    {
        [Tooltip("Which sound this is.")]
        [SerializeField]
        private GameSound _sound = GameSound.None;

        [Tooltip("One of these is picked each time. Several stop a repeated sound sounding mechanical.")]
        [SerializeField]
        private AudioClip[] _clips = Array.Empty<AudioClip>();

        [Tooltip("How loud, relative to its bus.")]
        [SerializeField]
        [Range(0f, 1f)]
        private float _volume = 0.8f;

        [Tooltip("How far the pitch wanders either way. A little keeps repeats from grating.")]
        [SerializeField]
        [Range(0f, 0.5f)]
        private float _pitchSpread = 0.08f;

        [Tooltip("Which volume slider it answers to.")]
        [SerializeField]
        private SoundBus _bus = SoundBus.Effects;

        /// <summary>Which sound this is.</summary>
        public GameSound Sound => _sound;

        /// <summary>How loud, relative to its bus.</summary>
        public float Volume => _volume;

        /// <summary>How far the pitch may wander either way.</summary>
        public float PitchSpread => _pitchSpread;

        /// <summary>Which volume slider it answers to.</summary>
        public SoundBus Bus => _bus;

        /// <summary>How many clips it can choose between.</summary>
        public int ClipCount => _clips.Length;

        /// <summary>One of the clips, or null when the entry has none.</summary>
        public AudioClip? Clip(int index) =>
            _clips.Length == 0 ? null : _clips[Mathf.Abs(index) % _clips.Length];
    }

    /// <summary>
    /// What every sound in the game is made of, as an asset.
    /// </summary>
    /// <remarks>
    /// CLAUDE.md keeps numbers out of C# and the same applies here: which file, how loud and how
    /// much the pitch wanders are a designer's business. Code asks for
    /// <see cref="GameSound.Gunshot"/> and finds out nothing more about it.
    /// Bus volumes live here too rather than in BalanceConfig, because they are the player's
    /// settings before they are anybody's balance.
    /// </remarks>
    [CreateAssetMenu(menuName = "ZombieGame/Sound Bank", fileName = "SoundBank")]
    public sealed class SoundBank : ScriptableObject
    {
        [Header("Sounds")]
        [SerializeField]
        private SoundEntry[] _entries = Array.Empty<SoundEntry>();

        [Header("Buses")]
        [SerializeField]
        [Range(0f, 1f)]
        private float _master = 0.9f;

        [SerializeField]
        [Range(0f, 1f)]
        private float _effects = 0.85f;

        [SerializeField]
        [Range(0f, 1f)]
        private float _interface = 0.7f;

        [SerializeField]
        [Range(0f, 1f)]
        private float _music = 0.5f;

        [Header("Crowd control")]
        [Tooltip("How long the same sound must wait before it may start again.")]
        [SerializeField]
        [Min(0f)]
        private float _repeatGapSeconds = 0.06f;

        [Tooltip("The most sounds that may be sounding at once.")]
        [SerializeField]
        [Min(1)]
        private int _atOnce = 16;

        /// <summary>How long the same sound must wait before it may start again.</summary>
        public float RepeatGapSeconds => _repeatGapSeconds;

        /// <summary>The most sounds that may be sounding at once.</summary>
        public int AtOnce => _atOnce;

        /// <summary>The entry for a sound, or null when the bank has nothing for it.</summary>
        public SoundEntry? Entry(GameSound sound)
        {
            foreach (SoundEntry entry in _entries)
            {
                if (entry.Sound == sound)
                {
                    return entry;
                }
            }

            return null;
        }

        /// <summary>How loud a bus is, master already applied.</summary>
        public float VolumeOf(SoundBus bus) => _master * bus switch
        {
            SoundBus.Interface => _interface,
            SoundBus.Music => _music,
            _ => _effects,
        };

        /// <summary>A bank with nothing in it, so a scene without one is silent, not broken.</summary>
        public static SoundBank CreateEmpty() => CreateInstance<SoundBank>();
    }
}
