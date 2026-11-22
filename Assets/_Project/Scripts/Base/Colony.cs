#nullable enable
using System.Collections.Generic;

namespace ZombieGame.Base
{
    /// <summary>
    /// The people at a base: who is here, where there is room for them, and who works where.
    /// </summary>
    /// <remarks>
    /// Plain C# over the plot, so the awkward questions — is there a bed, is that job taken,
    /// what happens to the farmhand when the farm comes down — are settled in tests rather than
    /// by watching a colony quietly go wrong over ten in-game days.
    /// How many people may live here is the sum of the beds in the barracks standing on the
    /// plot, as PROJECT.md 4.2.1 has it. Nothing about the site itself grants room: a plot with
    /// no barracks on it houses nobody, however large it is.
    /// The founders are the exception and arrive regardless. They are the squad that took the
    /// site; refusing to let them in because nobody has built them a bed yet would leave a base
    /// that cannot be started. They sleep rough until a barracks goes up, which is what makes
    /// the first barracks the obvious first thing to build.
    /// </remarks>
    public sealed class Colony
    {
        private readonly BuildSite _site;
        private readonly List<Survivor> _people = new();
        private int _nextId = 1;

        public Colony(BuildSite site) => _site = site;

        /// <summary>Everybody at the base, founders first.</summary>
        public IReadOnlyList<Survivor> People => _people;

        /// <summary>How many people live here.</summary>
        public int Count => _people.Count;

        /// <summary>How many beds the barracks on the plot add up to.</summary>
        public int Capacity
        {
            get
            {
                int beds = 0;
                foreach (PlacedBuilding building in _site.Buildings)
                {
                    BuildingLevel? rung = building.Rung;
                    if (rung is not null)
                    {
                        beds += rung.Beds;
                    }
                }

                return beds;
            }
        }

        /// <summary>How many people have nowhere to sleep.</summary>
        public int Homeless
        {
            get
            {
                int spare = Count - Capacity;
                return spare < 0 ? 0 : spare;
            }
        }

        /// <summary>True while there is a bed nobody is in.</summary>
        public bool HasSpareBed => Count < Capacity;

        /// <summary>
        /// Brings somebody in who was already here. Ignores the bed count.
        /// </summary>
        public Survivor AddFounder(string name)
        {
            var survivor = new Survivor(_nextId++, name);
            _people.Add(survivor);
            return survivor;
        }

        /// <summary>
        /// Takes somebody in, if there is a bed for them.
        /// </summary>
        /// <returns>The new arrival, or null when the base is full.</returns>
        public Survivor? TryAdd(string name) => HasSpareBed ? AddFounder(name) : null;

        /// <summary>Finds somebody by their number.</summary>
        public Survivor? Find(int id)
        {
            foreach (Survivor survivor in _people)
            {
                if (survivor.Id == id)
                {
                    return survivor;
                }
            }

            return null;
        }

        /// <summary>How many posts a building has, or zero if it is not standing.</summary>
        public int PostsAt(int buildingId)
        {
            foreach (PlacedBuilding building in _site.Buildings)
            {
                if (building.Id == buildingId)
                {
                    return building.Rung is null ? 0 : building.Rung.WorkPosts;
                }
            }

            return 0;
        }

        /// <summary>How many people are working in a building right now.</summary>
        public int WorkersAt(int buildingId)
        {
            if (buildingId == 0)
            {
                return 0;
            }

            int working = 0;
            foreach (Survivor survivor in _people)
            {
                if (survivor.Workplace == buildingId)
                {
                    working++;
                }
            }

            return working;
        }

        /// <summary>Puts somebody to work in a building.</summary>
        /// <returns>False when there is no such building, or no post left in it.</returns>
        public bool Assign(int survivorId, int buildingId)
        {
            Survivor? survivor = Find(survivorId);
            if (survivor is null || buildingId == 0)
            {
                return false;
            }

            if (survivor.Workplace == buildingId)
            {
                return true;
            }

            if (WorkersAt(buildingId) >= PostsAt(buildingId))
            {
                return false;
            }

            survivor.Workplace = buildingId;
            return true;
        }

        /// <summary>The first person with nothing to do, or null when everybody is busy.</summary>
        public Survivor? FirstIdle()
        {
            foreach (Survivor survivor in _people)
            {
                if (survivor.IsIdle)
                {
                    return survivor;
                }
            }

            return null;
        }

        /// <summary>
        /// Sends whoever is free to work in a building.
        /// </summary>
        /// <remarks>
        /// The wheel asks for a worker, not for a named person. Picking them one at a time off
        /// a roster is the long way round for the common case — "somebody go and run the well" —
        /// and the roster is still there for the player who cares which of them goes.
        /// </remarks>
        /// <returns>Whoever went, or null when nobody was free or there was no post left.</returns>
        public Survivor? PutAnyoneToWork(int buildingId)
        {
            Survivor? free = FirstIdle();
            if (free is null)
            {
                return null;
            }

            return Assign(free.Id, buildingId) ? free : null;
        }

        /// <summary>
        /// Takes one person off a building, the last one sent there first.
        /// </summary>
        /// <returns>Whoever was sent home, or null when nobody was working there.</returns>
        public Survivor? SendOneHome(int buildingId)
        {
            if (buildingId == 0)
            {
                return null;
            }

            for (int i = _people.Count - 1; i >= 0; i--)
            {
                if (_people[i].Workplace == buildingId)
                {
                    _people[i].Workplace = 0;
                    return _people[i];
                }
            }

            return null;
        }

        /// <summary>Takes somebody off their job.</summary>
        public void Unassign(int survivorId)
        {
            Survivor? survivor = Find(survivorId);
            if (survivor is not null)
            {
                survivor.Workplace = 0;
            }
        }

        /// <summary>
        /// Sends home anybody whose workplace is no longer standing.
        /// </summary>
        /// <remarks>
        /// Called after a building comes down. Without it a colonist keeps the number of a
        /// building that is gone, counts against the posts of whatever is given that number
        /// next, and works a job nobody can see.
        /// </remarks>
        public int SendHomeFromRubble()
        {
            int sent = 0;
            foreach (Survivor survivor in _people)
            {
                if (survivor.Workplace == 0)
                {
                    continue;
                }

                bool standing = false;
                foreach (PlacedBuilding building in _site.Buildings)
                {
                    if (building.Id == survivor.Workplace)
                    {
                        standing = true;
                        break;
                    }
                }

                if (!standing)
                {
                    survivor.Workplace = 0;
                    sent++;
                }
            }

            return sent;
        }
    }
}
