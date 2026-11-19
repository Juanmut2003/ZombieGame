#nullable enable
using UnityEngine;
using ZombieGame.Combat;
using ZombieGame.Core;
using ZombieGame.Core.Events;
using ZombieGame.Missions;

namespace ZombieGame.Audio
{
    /// <summary>
    /// Turns the things that happen in a zone into the sounds they make.
    /// </summary>
    /// <remarks>
    /// Everything it needs is already on the bus. No weapon, no zombie and no container knows
    /// that sound exists — the swing, the shot, the blow and the death were being published long
    /// before anything was listening, which is the whole point of having had a bus all along.
    /// One object per zone, and the only place in the game where an event is mapped to a sound.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class SoundRouter : MonoBehaviour
    {
        [Tooltip("Who the squad is, so its own injuries sound different from everybody else's.")]
        [SerializeField]
        private Transform? _squad;

        private EventBus? _bus;
        private ISoundService _sound = Silence.Instance;
        private int _squadId = -1;

        private void Start()
        {
            // Start, not Awake: the bootstrap registers the bus in its own Awake, and the audio
            // service registers itself in one too.
            GameServices.TryGet(out _bus);
            if (GameServices.TryGet(out ISoundService? service) && service is not null)
            {
                _sound = service;
            }

            if (_squad != null && _squad.TryGetComponent(out IHurtable hurtable))
            {
                _squadId = hurtable.OwnerId;
            }

            if (_bus is null)
            {
                Debug.LogWarning("[Audio] no event bus; the zone will make no sound.");
                return;
            }

            _bus.Subscribe<MeleeSwungEvent>(OnSwing);
            _bus.Subscribe<ShotFiredEvent>(OnShot);
            _bus.Subscribe<DamageTakenEvent>(OnHurt);
            _bus.Subscribe<DiedEvent>(OnDied);
            _bus.Subscribe<LootTakenEvent>(OnLoot);
            _bus.Subscribe<LoadoutChangedEvent>(OnLoadout);
            _bus.Subscribe<MissionEndedEvent>(OnEnded);
        }

        private void OnDestroy()
        {
            if (_bus is null)
            {
                return;
            }

            _bus.Unsubscribe<MeleeSwungEvent>(OnSwing);
            _bus.Unsubscribe<ShotFiredEvent>(OnShot);
            _bus.Unsubscribe<DamageTakenEvent>(OnHurt);
            _bus.Unsubscribe<DiedEvent>(OnDied);
            _bus.Unsubscribe<LootTakenEvent>(OnLoot);
            _bus.Unsubscribe<LoadoutChangedEvent>(OnLoadout);
            _bus.Unsubscribe<MissionEndedEvent>(OnEnded);
        }

        private void OnSwing(MeleeSwungEvent swing) =>
            _sound.Play(GameSound.MeleeSwing, swing.Origin);

        private void OnShot(ShotFiredEvent shot) =>
            _sound.Play(GameSound.Gunshot, shot.Origin);

        private void OnHurt(DamageTakenEvent hurt) =>
            _sound.Play(hurt.TargetId == _squadId ? GameSound.SquadHurt : GameSound.Impact);

        private void OnDied(DiedEvent died)
        {
            // The squad's own death is the end of the run, and the run has its own sound. Two at
            // once would step on each other.
            if (died.TargetId != _squadId)
            {
                _sound.Play(GameSound.ZombieDied);
            }
        }

        private void OnLoot(LootTakenEvent loot) =>
            _sound.Play(GameSound.LootTaken, loot.Where);

        private void OnLoadout(LoadoutChangedEvent loadout)
        {
            if (loadout.IsPlayer)
            {
                _sound.Play(GameSound.WeaponSwitched);
            }
        }

        private void OnEnded(MissionEndedEvent ended) =>
            _sound.Play(ended.Extracted ? GameSound.MissionWon : GameSound.MissionLost);
    }
}
