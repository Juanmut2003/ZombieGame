#nullable enable
using UnityEngine;

namespace ZombieGame.Missions
{
    /// <summary>
    /// The way out: a patch of ground the squad has to stand on to end the run.
    /// </summary>
    /// <remarks>
    /// A distance check rather than a trigger collider. The squad is one transform today and a
    /// handful tomorrow, and "is anyone standing here" is a question this can answer directly
    /// without every body in the zone needing a collider on the right layer.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class ExtractionPoint : MonoBehaviour
    {
        [Tooltip("Who has to stand here. Usually the MC.")]
        [SerializeField]
        private Transform? _squad;

        [Tooltip("How big the patch is, in world units.")]
        [SerializeField]
        [Min(0.2f)]
        private float _radius = 1.4f;

        /// <summary>True while the squad is standing in it.</summary>
        public bool SquadInside { get; private set; }

        /// <summary>How big the patch is.</summary>
        public float Radius => _radius;

        /// <summary>Tells the point who it is waiting for, for scenes wired at runtime.</summary>
        public void Watch(Transform squad) => _squad = squad;

        private void Update()
        {
            if (_squad is null)
            {
                SquadInside = false;
                return;
            }

            Vector2 gap = (Vector2)_squad.position - (Vector2)transform.position;
            SquadInside = gap.sqrMagnitude <= _radius * _radius;
        }

#if UNITY_EDITOR
        /// <summary>Draws the patch, so it can be placed by eye rather than by arithmetic.</summary>
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.45f, 0.78f, 0.52f, 0.7f);
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
#endif
    }
}
