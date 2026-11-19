#nullable enable
using System;
using NUnit.Framework;
using ZombieGame.Audio;

namespace ZombieGame.Tests.EditMode
{
    /// <summary>
    /// Which of the sounds a zone asks for actually get played.
    /// </summary>
    /// <remarks>
    /// This is the whole reason the audio layer is not just "play the clip". A shotgun into a
    /// crowd is twenty impacts in one frame; played as twenty they land on the same sample at the
    /// same instant and sum into one flat crack at twenty times the volume — louder, uglier and
    /// less informative than a single hit. A horde dying together does the same thing.
    /// Rules a player would notice, so rules worth pinning down.
    /// </remarks>
    [TestFixture]
    public sealed class SoundLimiterTests
    {
        [Test]
        public void TheFirstOneAlwaysPlays()
        {
            var limiter = new SoundLimiter();

            Assert.That(limiter.MayPlay(GameSound.Gunshot, now: 0f), Is.True);
        }

        [Test]
        public void TwentyImpactsInOneFrameAreNotTwentyImpacts()
        {
            var limiter = new SoundLimiter(gapSeconds: 0.06f, atOnce: 16);

            int played = 0;
            for (int i = 0; i < 20; i++)
            {
                if (limiter.MayPlay(GameSound.Impact, now: 0f))
                {
                    played++;
                }
            }

            Assert.That(played, Is.EqualTo(1), "one hit, heard once");
        }

        [Test]
        public void DifferentSoundsInTheSameFrameAllGetThrough()
        {
            // The gap is per sound, not global: a shot and the impact it caused happen together
            // and are two different things to hear.
            var limiter = new SoundLimiter(gapSeconds: 0.06f, atOnce: 16);

            Assert.That(limiter.MayPlay(GameSound.Gunshot, 0f), Is.True);
            Assert.That(limiter.MayPlay(GameSound.Impact, 0f), Is.True);
            Assert.That(limiter.MayPlay(GameSound.ZombieDied, 0f), Is.True);
        }

        [Test]
        public void ItComesBackOnceTheGapHasPassed()
        {
            var limiter = new SoundLimiter(gapSeconds: 0.06f);

            Assert.That(limiter.MayPlay(GameSound.MeleeSwing, 0f), Is.True);
            Assert.That(limiter.MayPlay(GameSound.MeleeSwing, 0.03f), Is.False);
            Assert.That(limiter.MayPlay(GameSound.MeleeSwing, 0.07f), Is.True);
        }

        [Test]
        public void NoMoreThanTheCeilingSoundAtOnce()
        {
            // Every sound is a different one, so the gap cannot be what stops them: this is the
            // ceiling doing its job.
            var limiter = new SoundLimiter(gapSeconds: 0f, atOnce: 3);
            var sounds = new[]
            {
                GameSound.Gunshot, GameSound.Impact, GameSound.ZombieDied,
                GameSound.LootTaken, GameSound.MeleeSwing,
            };

            int played = 0;
            foreach (GameSound sound in sounds)
            {
                if (limiter.MayPlay(sound, now: 0f, lengthSeconds: 1f))
                {
                    played++;
                }
            }

            Assert.That(played, Is.EqualTo(3));
        }

        [Test]
        public void ASlotComesFreeWhenItsSoundHasFinished()
        {
            var limiter = new SoundLimiter(gapSeconds: 0f, atOnce: 1);

            Assert.That(limiter.MayPlay(GameSound.Gunshot, 0f, lengthSeconds: 0.5f), Is.True);
            Assert.That(limiter.MayPlay(GameSound.Impact, 0.2f, lengthSeconds: 0.5f), Is.False);
            Assert.That(limiter.MayPlay(GameSound.Impact, 0.6f, lengthSeconds: 0.5f), Is.True);
        }

        [Test]
        public void NothingIsNotASound()
        {
            var limiter = new SoundLimiter();

            Assert.That(limiter.MayPlay(GameSound.None, 0f), Is.False);
        }

        [Test]
        public void ItReportsHowManyAreSounding()
        {
            var limiter = new SoundLimiter(gapSeconds: 0f, atOnce: 8);

            limiter.MayPlay(GameSound.Gunshot, 0f, lengthSeconds: 1f);
            limiter.MayPlay(GameSound.Impact, 0f, lengthSeconds: 1f);
            limiter.MayPlay(GameSound.ZombieDied, 0f, lengthSeconds: 0.1f);

            // Asked at a moment when the short one has finished and the long ones have not.
            limiter.MayPlay(GameSound.None, 0.5f);

            Assert.That(limiter.Sounding, Is.EqualTo(2));
        }

        [Test]
        public void ASilentSystemIsNotASystem()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SoundLimiter(atOnce: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SoundLimiter(gapSeconds: -1f));
        }
    }
}
