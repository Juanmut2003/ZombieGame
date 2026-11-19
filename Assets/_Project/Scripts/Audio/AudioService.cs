#nullable enable
using UnityEngine;
using ZombieGame.Core;

namespace ZombieGame.Audio
{
    /// <summary>
    /// The zone's speakers: a fixed set of audio sources, handed out and taken back.
    /// </summary>
    /// <remarks>
    /// Pooled because CLAUDE.md forbids per-frame allocation anywhere the horde can reach, and a
    /// crowd dying is exactly that. The pool is also the ceiling — when every source is busy the
    /// sound is dropped rather than queued, because a late impact is a lie about when it
    /// happened.
    /// <c>AudioSource.PlayClipAtPoint</c> is explicitly not used: it creates and destroys a
    /// GameObject per sound and answers to no volume setting.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class AudioService : MonoBehaviour, ISoundService
    {
        [Tooltip("Which sounds exist, how loud, and on which bus.")]
        [SerializeField]
        private SoundBank? _bank;

        [Tooltip("How many sounds can be playing at once. One audio source each.")]
        [SerializeField]
        [Min(1)]
        private int _sources = 16;

        private AudioSource[] _pool = System.Array.Empty<AudioSource>();
        private SoundLimiter _limiter = null!;
        private System.Random _random = null!;

        /// <summary>How many sounds this service has been asked for since it started.</summary>
        public int Requested { get; private set; }

        /// <summary>How many it actually played.</summary>
        public int Played { get; private set; }

        private void Awake()
        {
            if (_bank is null)
            {
                Debug.LogWarning($"[Audio] {name} has no sound bank; the zone stays quiet.");
                _bank = SoundBank.CreateEmpty();
            }

            _limiter = new SoundLimiter(_bank.RepeatGapSeconds, Mathf.Max(1, _sources));

            // Fixed so a zone sounds the same way twice, like the horde's own scatter.
            _random = new System.Random(8613);

            _pool = new AudioSource[Mathf.Max(1, _sources)];
            for (int i = 0; i < _pool.Length; i++)
            {
                var speaker = new GameObject($"Speaker {i + 1}");
                speaker.transform.SetParent(transform, worldPositionStays: false);

                AudioSource source = speaker.AddComponent<AudioSource>();
                source.playOnAwake = false;

                // Flat, for now. Positions are carried by the events and used to pick what is
                // worth hearing rather than to pan it; proper spatial audio wants a listener
                // placed with intent, which belongs with the camera work rather than here.
                source.spatialBlend = 0f;
                _pool[i] = source;
            }

            GameServices.Replace<ISoundService>(this);
        }

        private void OnDestroy()
        {
            if (GameServices.TryGet(out ISoundService? registered) && ReferenceEquals(registered, this))
            {
                GameServices.Unregister<ISoundService>();
            }
        }

        /// <inheritdoc />
        public void Play(GameSound sound, Vector2 at) => Play(sound);

        /// <inheritdoc />
        public void Play(GameSound sound)
        {
            Requested++;

            SoundEntry? entry = _bank!.Entry(sound);
            if (entry is null || entry.ClipCount == 0)
            {
                return;
            }

            float now = Time.unscaledTime;
            AudioClip? clip = entry.Clip(_random.Next(entry.ClipCount));
            if (clip is null)
            {
                return;
            }

            if (!_limiter.MayPlay(sound, now, clip.length))
            {
                return;
            }

            AudioSource? free = FreeSource();
            if (free is null)
            {
                return;
            }

            float spread = entry.PitchSpread;
            free.clip = clip;
            free.volume = entry.Volume * _bank.VolumeOf(entry.Bus);
            free.pitch = spread <= 0f
                ? 1f
                : 1f + (((float)_random.NextDouble() * 2f) - 1f) * spread;

            free.Play();
            Played++;
        }

        private AudioSource? FreeSource()
        {
            foreach (AudioSource source in _pool)
            {
                if (!source.isPlaying)
                {
                    return source;
                }
            }

            return null;
        }
    }
}
