#nullable enable
using System.Collections.Generic;
using UnityEngine;
using ZombieGame.Characters;

namespace ZombieGame.Base
{
    /// <summary>
    /// Ties one of the colony's people to the figure standing for them on the plot.
    /// </summary>
    /// <remarks>
    /// PROJECT.md 4.2.2. Put somebody to work in the barracks and the figure walks over, in
    /// through the door and stands at a bed; send them home and they go back to wandering the
    /// plot. That is the whole of it: the colony still decides who works where (§4.3), and this
    /// only shows the answer.
    /// It reads the colony rather than being told, because a job can change in four different
    /// ways — the ring menu, a key, a building torn down under somebody, a day resolving — and a
    /// message from each of them is four chances to miss one.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class SurvivorBody : MonoBehaviour
    {
        [Tooltip("The colony whose people this figure is one of.")]
        [SerializeField]
        private ColonyRuntime? _colony;

        [Tooltip("The plot, for working out where a building stands.")]
        [SerializeField]
        private BuildSiteView? _siteView;

        [Tooltip("What the buildings are parented to.")]
        [SerializeField]
        private Transform? _built;

        [Tooltip("The legs. Usually on the same object.")]
        [SerializeField]
        private CharacterStroll? _stroll;

        [Tooltip("Which of the colony's people this figure is. Counts from one.")]
        [SerializeField]
        [Min(1)]
        private int _survivorId = 1;

        private readonly List<Rect> _walls = new();
        private readonly List<Vector2> _route = new();
        private BuildingView? _workplace;
        private BuildingFootprint _asBuilt;
        private int _sentTo;

        /// <summary>Which of the colony's people this figure stands for.</summary>
        public int Answers => _survivorId;

        /// <summary>The building they are working in, or zero while they have nothing to do.</summary>
        public int Workplace => _sentTo;

        /// <summary>Where they are standing while at work, in world units.</summary>
        public Vector2 Post { get; private set; }

        private void Awake()
        {
            if (_stroll == null)
            {
                _stroll = GetComponent<CharacterStroll>();
            }
        }

        private void Update()
        {
            if (_stroll == null)
            {
                return;
            }

            int wanted = JobOf(_survivorId);

            // The building is looked up only when the job changes, never every frame. Its
            // footprint is checked every frame and costs nothing: improving a barracks moves the
            // beds, and a worker standing where the old wall was is the bug that would follow.
            if (wanted != _sentTo || (_workplace != null && !_workplace.Footprint.Equals(_asBuilt)))
            {
                _sentTo = wanted;
                _workplace = wanted == 0 ? null : Find(wanted);
                SendThem();
            }
        }

        private int JobOf(int survivorId)
        {
            Colony? colony = _colony == null ? null : _colony.Colony;
            Survivor? who = colony?.Find(survivorId);
            return who is null ? 0 : who.Workplace;
        }

        private void SendThem()
        {
            BuildSite? site = _siteView == null ? null : _siteView.Site;
            PlacedBuilding? standing =
                _workplace == null || site is null ? null : site.Find(_workplace.Id);

            if (_workplace == null || site is null || standing is null)
            {
                _asBuilt = default;
                _stroll!.Roam();
                return;
            }

            BuildingFootprint footprint = standing.Value.Footprint;
            _asBuilt = footprint;

            Vector2 middle = _workplace.transform.position;
            BuildingLevel? rung = standing.Value.Rung;
            int posts = Mathf.Max(1, rung is null ? 1 : rung.WorkPosts);
            Post = middle + BuildingPosts.At(footprint, PlaceInTheQueue(), posts);

            // Round the walls rather than at them. Every building on the plot is in the way,
            // this one included — its own door is the gap the route goes through.
            _walls.Clear();
            foreach (PlacedBuilding other in site.Buildings)
            {
                BuildingWalls.AppendTo(
                    _walls, other.Footprint, site.CentreOf(other.Cell, other.Footprint));
            }

            if (!PlotRoutes.TryFind(transform.position, Post, site.Ground, _walls, _route))
            {
                // Nowhere to walk: a post walled in by somebody else's barn. Better to potter
                // about the plot than to stand pushing at a wall all day.
                Debug.LogWarning($"[Colony] {name} cannot reach their post in {_workplace.name}.");
                _stroll!.Roam();
                return;
            }

            _stroll!.Follow(_route);
        }

        /// <summary>
        /// Which post is theirs: their place among everybody working in the same building, so
        /// that two workers stand at two benches rather than inside each other.
        /// </summary>
        private int PlaceInTheQueue()
        {
            Colony? colony = _colony == null ? null : _colony.Colony;
            if (colony is null)
            {
                return 0;
            }

            int place = 0;
            foreach (Survivor person in colony.People)
            {
                if (person.Id == _survivorId)
                {
                    return place;
                }

                if (person.Workplace == _sentTo)
                {
                    place++;
                }
            }

            return place;
        }

        private BuildingView? Find(int id)
        {
            if (_built == null)
            {
                return null;
            }

            foreach (BuildingView view in _built.GetComponentsInChildren<BuildingView>(true))
            {
                if (view.Id == id)
                {
                    return view;
                }
            }

            return null;
        }
    }
}
