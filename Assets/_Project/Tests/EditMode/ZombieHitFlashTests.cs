#nullable enable
using NUnit.Framework;
using UnityEngine;
using ZombieGame.AI;
using ZombieGame.Combat;

namespace ZombieGame.Tests.EditMode
{
    /// <summary>
    /// Covers the flash that tells the player a blow actually landed.
    /// </summary>
    /// <remarks>
    /// Without it a hit and a miss look identical: the health that changed is invisible, and the
    /// zombie keeps walking either way.
    /// </remarks>
    [TestFixture]
    public sealed class ZombieHitFlashTests
    {
        private ZombieManager _manager = null!;
        private ZombieDefinition _definition = null!;

        [SetUp]
        public void CreateHorde()
        {
            _definition = ScriptableObject.CreateInstance<ZombieDefinition>();
            _manager = new ZombieManager(capacity: 4);
        }

        [TearDown]
        public void DestroyDefinition() => Object.DestroyImmediate(_definition);

        [Test]
        public void AHitStartsTheFlash()
        {
            _manager.Spawn(_definition, Vector2.zero);
            _manager.Damage(0, new DamageInfo(1f, DamageType.Blunt));

            Assert.That(_manager.Get(0).HitFlash, Is.EqualTo(ZombieManager.HitFlashSeconds));
        }

        [Test]
        public void AFreshZombieIsNotFlashing()
        {
            _manager.Spawn(_definition, Vector2.zero);

            Assert.That(_manager.Get(0).HitFlash, Is.Zero);
        }

        [Test]
        public void TheFlashRunsOut()
        {
            _manager.Spawn(_definition, Vector2.zero);
            _manager.Damage(0, new DamageInfo(1f, DamageType.Blunt));

            _manager.Tick(ZombieManager.HitFlashSeconds + 0.01f);

            Assert.That(_manager.Get(0).HitFlash, Is.Zero);
        }

        [Test]
        public void TheFlashIsStillOnPartWayThrough()
        {
            _manager.Spawn(_definition, Vector2.zero);
            _manager.Damage(0, new DamageInfo(1f, DamageType.Blunt));

            _manager.Tick(ZombieManager.HitFlashSeconds * 0.5f);

            Assert.That(_manager.Get(0).HitFlash, Is.GreaterThan(0f));
        }
    }
}
