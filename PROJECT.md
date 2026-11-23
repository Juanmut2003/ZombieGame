| Which pixel scale does the art run at? | **64 px per tile, 64 pixels per unit, 960×540 reference.** Raised from 32/480×270 so bought character art of 64 px fits a tile; the visible area and the clean 1080p upscale are unchanged | §13, CLAUDE.md §4 |
| Is cash (€) still credible?# PROJECT.md — Game Design Document

**Working title:** ZombieGame (placeholder)
**Genre:** Top-down survival / base building / squad action
**Engine:** Unity 6 LTS, 2D URP
**Platform:** PC (Windows first, Linux later), keyboard + mouse, full gamepad support
**Players:** Singleplayer only (no multiplayer, not even planned for v1)
**Setting:** Europe, ~18 months after the outbreak

---

## 1. Pitch

You are one survivor in a collapsed Europe. You start with nothing, pick a background that shapes
your skills and your first contacts, and run one opening mission to earn the keys to a first small
shelter. From there you build: take in survivors, grow food, craft gear, fortify walls, and hold the
line against zombie waves whose size is driven by how big, how loud and how advanced your base has
become. When your base outgrows its walls, you scout and claim a larger one. Between waves you drive
across a strategic map of Europe on missions for loot, people and leverage over the other factions
still out there.

**Core loop:**
`Prepare base → travel → run mission → haul loot & survivors home → build & assign jobs → survive the wave → repeat`

**Two interleaved timescales:**
- **Base layer** (slow, strategic): construction, jobs, power, water, morale, seasons.
- **Mission layer** (fast, tactical): real-time twin-stick combat in a loaded zone.

---

## 2. Design pillars

1. **Noise is the enemy.** Every advantage you build — generators, workshops, gunfire, lights — makes
   you louder. Power is strength and power is a dinner bell. Players should constantly trade safety
   for capability.
2. **People are the resource.** Survivors are not a number. They have names, skills, needs, moods and
   can die permanently. Losing a good mechanic hurts more than losing a crate of ammo.
3. **Readable depth.** Deep systems (power grid, water, morale, disease) but always with a clear UI
   answer to "why is this bad right now?"
4. **No handed-out safety.** Bases are claimed, defended and eventually outgrown. Nothing is given.
5. **Grounded Europe.** Real regional flavour — Alpine valleys, Baltic ports, Ruhr industry, French
   farmland — not generic apocalypse ruins.

---

## 3. Game start

### 3.1 Character creation
The player creates one **Main Character (MC)**:
- Name, pixel-portrait (modular: head / hair / facial hair / skin tone / clothing), voice grunt set.
- Base attributes (1–10, start pool ~20 points): `Strength`, `Agility`, `Endurance`, `Perception`,
  `Intellect`, `Charisma`.
- One **Trait** pick from a list (positive + negative pairs, e.g. *Light Sleeper*, *Iron Stomach*,
  *Hemophobia*, *Pack Rat*).

### 3.2 The five backgrounds
Picked at creation, mutually exclusive. Each gives: starting skills, starting gear, a unique passive,
a unique starting contact/faction standing, and a flavoured opening mission variant.

| # | Background | Starting skills | Gear | Passive | Starting contact |
|---|-----------|-----------------|------|---------|------------------|
| 1 | **Ex-Bundeswehr Soldier** | Firearms 3, Tactics 2 | Service pistol, 2 mags, vest | Squad under your command reloads 20% faster | Military remnant: *Restbataillon* |
| 2 | **Field Medic** | Medicine 3, Science 1 | Trauma kit, antibiotics ×2 | Downed squadmates bleed out 50% slower | Red Cross camp: *Genesis* |
| 3 | **Farmer from the Alps** | Farming 3, Survival 2 | Hunting rifle, seed crate, dog | Farm plots yield +15%, animals available earlier | Mountain village network |
| 4 | **Industrial Mechanic** | Engineering 3, Crafting 2 | Toolbox, scrap, welder | Buildings cost 15% fewer materials, repairs 25% faster | Scavenger guild: *The Yard* |
| 5 | **Smuggler** | Negotiation 3, Stealth 2 | Silenced pistol, van, contraband | Better trade prices, starts with a working vehicle | Black market: *Die Börse* |

### 3.3 Prologue mission — "Keys to the Gate"
No base at the start. The player plays one scripted mission (variant per background) that ends with a
choice between **two small starter bases**. Teaches: movement, stealth, noise, melee, looting,
extraction, and recruiting the first survivor (a fixed NPC companion).

### 3.4 Difficulty
Chosen at new game, locked afterwards (shown clearly in the UI):

| Mode | Survivor death | MC death | Waves | Resources |
|------|----------------|----------|-------|-----------|
| **Casual** | Wounded only, always recoverable | Wounded, wakes up at base | −25% size | +25% |
| **Survival** (default) | Permadeath | Severe injury, long recovery, loses carried gear | Normal | Normal |
| **Ironman** | Permadeath | Permadeath — run ends or continue as a chosen survivor | +25% size, faster escalation | −25%, single save slot, no manual saves |

---

## 4. Bases

### 4.1 Progression
Bases are **claimed sites on the strategic map**, not built from scratch on open ground. They are
graded in tiers; higher tiers unlock when the colony meets thresholds across several axes at once —
never a single number.

The **max residents** column is the most a plot could ever support. What a base can actually house
is the bed capacity of the barracks standing on it — see 4.2.1.

| Tier | Example sites | Max residents | Build slots | Unlock requirements (all must be met) |
|------|---------------|---------------|-------------|----------------------------|
| 1 | Petrol station, farmhouse | 6 | small plot | Prologue complete |
| 2 | Village school, river mill | 12 | medium plot | 6 residents, Colony Level 2, stockpile 8k TV, 1 Tier-2 site scouted |
| 3 | Industrial yard, supermarket block | 24 | large plot | 12 residents, Colony Level 3, stockpile 25k TV, power + water Tier 2 |
| 4 | Hospital wing, fortified town quarter | 40 | very large plot | 24 residents, Colony Level 4, stockpile 60k TV, faction ally, defence rating ≥ X |
| 5 | Castle / military depot (endgame) | 60+ | huge, multi-zone | 40 residents, Colony Level 5, stockpile 150k TV, two faction allies |

**Choice matters:** at each tier the player is offered **2–3 candidate sites**, **drawn per run** from
a larger handcrafted pool, so no two runs offer the same ladder of homes. Each site differs in
stats — slot count, natural defensibility (wall %, chokepoints), existing infrastructure (pre-built
well, generator, greenhouse), terrain fertility, noise exposure, and distance to regions/trade. A
castle is defensible but far from loot; an industrial yard has a free workshop but terrible walls.
Claiming a site requires a **Claim Mission** (clear it out, sometimes contest a faction for it).

**Moving house:** when claiming a new base, the player chooses what to carry over. Moving costs time,
fuel and crate space; buildings don't move, materials are partially refunded (60%). Old bases can be
kept as **outposts** (small passive income/storage, must be garrisoned, can be lost).

### 4.2 Base building (free placement)
A claimed site is a grid-based editable space. Buildings are placed freely on buildable tiles.

**Building categories:**

- **Farms** — crop plots (wheat, potatoes, cabbage, herbs), greenhouse (ignores season), livestock pen
  (chickens, goats), water trough. Yields depend on soil fertility, season, worker Farming skill,
  water supply.
- **Barracks** — beds and living space. Caps residents; bed quality and privacy affect morale and
  rest quality. The bunk room → dorm → private quarters progression is the level ladder in 4.2.1,
  not three separate buildings.
- **Factory / Workshop** — the crafting building. Recipe-based production queue: ammo, medicine,
  building materials, tools, traps, fuel refining. Tiered workbenches gate recipe tiers. Loud.
- **Armory** — weapon and ammo storage, weapon repair and modding bench, squad loadout station.
  Stored weapons degrade slower; raids target it first.

**Support structures:**
- Power: generator (diesel, loud), solar panels (quiet, weather-dependent, expensive), wind turbine,
  battery bank, power poles/cables.
- Water: well, rain collector, water tank, filtration (dirty water → disease).
- Storage: material depot, food store (spoilage without cold storage), fuel depot (fire risk).
- Medical: infirmary (treat wounds, disease, surgery), quarantine room.
- Social: canteen, common room, chapel/memorial, radio tower (recruitment, faction comms, +noise).
- Defence: wooden/metal/concrete walls, gates, watchtowers, spike traps, noise traps (lure zombies
  away), floodlights (vision at night, +noise), sandbag fighting positions, auto-turrets (late).

#### 4.2.1 Building levels

Each of the four main buildings is **upgraded in place through four levels** rather than replaced
by a bigger variant. A level costs materials and time; what it buys is capacity, worker posts and
unlocked stations. The reference sheets are in `Docs/Buildings/` — one image per building, all four
levels side by side, and they are the authority on what each level contains and looks like.

**Farm** (`Docs/Buildings/farm_levels.webp`)

| Level | Plots | Crates | Water | Processing | Workers |
|-------|-------|--------|-------|------------|---------|
| 1 | 4 | 1 | barrel | — | 1 |
| 2 | 8 | 2 | tank | compost | 2 |
| 3 | 12 | 3 | tank (improved) | 1 station | 3 |
| 4 | 16 | 4 | irrigation | 1 station (improved) | 4 |

**Barracks** (`Docs/Buildings/barracks_levels.webp`)

| Level | Beds | Chests | Lockers | Extra | Residents |
|-------|------|--------|---------|-------|-----------|
| 1 | 2 | 1 | — | — | 2 |
| 2 | 4 | 2 | 1 | — | 4 |
| 3 | 6 | 3 | 2 | equipment area | 6 |
| 4 | 8 | 4 | 3 | equipment + medical area | 8 |

**Factory / Workshop** (`Docs/Buildings/factory_levels.webp`)

| Level | Workbench | Crates | Production stations | Research | Workers |
|-------|-----------|--------|---------------------|----------|---------|
| 1 | basic | 1 | — | — | 1 |
| 2 | improved | 2 | 1 (tool parts) | — | 2 |
| 3 | advanced | 3 | 2 (weapon + tool parts) | 1 (upgrades) | 3 |
| 4 | industrial | 4 | 3 (weapons, armour, ammunition) | 1 (improved) | 4 |

**Armory** (`Docs/Buildings/armory_levels.webp`)

| Level | Racks | Ammunition | Unlocks | Workers |
|-------|-------|------------|---------|---------|
| 1 | 2 | 200 | weapon production, maintenance | 1 |
| 2 | 4 | 400 | + weapon cabinet, workbench | 2 |
| 3 | 6 | 700 | + weapon upgrades | 3 |
| 4 | 8 | 1200 | + master workshop (unique mods) | 4 |

The racks and ammunition figures are not on the sheet; they are set here to the same ladder the
other three follow — one more worker per level, capacity roughly doubling at first and then
tapering. **Racks** cap how many weapons can be stored without degrading; squad loadouts are drawn
from them. Weapons kept over that cap wear out at the normal rate instead of the slowed one.

**Generator** (`Docs/Buildings/generator_levels.webp`)

| Level | Power / day | Machinery | Storage | Workers |
|-------|-------------|-----------|---------|---------|
| 1 | +10 | 1 generator (small) | 1 fuel tank | 1 |
| 2 | +25 | 2 generators (improved) | 2 fuel tanks, distribution box | 2 |
| 3 | +50 | 1 generator (large) | 3 battery banks, control room | 3 |
| 4 | +100 | 1 main generator (industrial) | 6 battery banks, switchgear, standby set | 4 |

**Well** (`Docs/Buildings/well_levels.webp`)

| Level | Water / day | Pump | Storage | Workers |
|-------|-------------|------|---------|---------|
| 1 | +10 | hand pump | 1 barrel | 1 |
| 2 | +25 | well (improved) | 3 barrels, 1 tank | 2 |
| 3 | +50 | electric pump | 2 tanks, 1 filter | 3 |
| 4 | +100 | treatment plant | 3 large tanks, improved filter, pipework | 4 |

Both figures are the building's output **at full staffing**; the yield per worker in the asset is
that figure shared out over the posts, so a half-crewed well brings in roughly half as much. The
well's pump is electric from level 3, which is where fresh water starts costing power.

The generator's worker column is **not modelled yet**. A generator's output is a property of the
machine rather than of who is standing beside it, so giving it posts today would be a job that
changes nothing. It gets posts when a generator can be run badly — maintenance, fuel efficiency,
or a breakdown that a crew prevents.

Every sheet draws the building with its **entrance at the bottom**, which is the facing the sprites
are expected to keep.

**How much ground a level takes.** The panels are drawn bigger at every level, and the footprints
follow them. For the four main buildings: **L1 3x5, L2 4x5, L3 4x6, L4 5x6 cells.** The two
support structures are drawn to their own proportions — the generator **2x2, 3x3, 4x4, 5x4** and
the well **2x3, 3x4, 4x5, 5x5**. An upgrade therefore needs free ground
beside and above the building — a barracks hemmed in on all four sides cannot be upgraded until
something next to it moves. That is deliberate: on a small plot, where things are put down is a
decision that is still being paid for twenty levels later.

A Tier-1 plot of 14x8 holds four level-1 buildings side by side and almost nothing else. Upgrading
all four is impossible there, which is one of the reasons to go looking for a Tier-2 site.

**Residents are capped by barracks, not by the site.** The number of people who can live at a base
is the sum of the bed capacity of every barracks standing on it — 2 at level 1, rising to 8 at
level 4. The "max residents" column in 4.1 is the *ceiling the plot can physically support*, not a
figure handed out for free: a site with no barracks houses nobody, however large it is.

**Still open:**

- The sheets are concept art at illustration resolution. They have been cut into tile-aligned
  sprites in `Art/Sprites/Buildings/` (`building_<name>_<level>.png`), one per level, but the
  originals in `Docs/Buildings/` stay the reference.
- The generator and the well now have sheets of their own and the same four-level treatment.
  Storage and defence structures still have none.

**Buildings do not turn.** Each one is drawn one way round — roof at the top, steps at the bottom
— so a quarter turn would need a second drawing of every building at every level, and until there
is one, turning only ever moved the ground out from under the picture: a turned farm had its fence
across the middle of its own fields and open air where the gate was. A building goes down the way
it is drawn, and fitting it on the plot is part of the puzzle.

#### 4.2.2 Buildings are rooms you walk into

A building is **a room with its roof cut away**, which is how the art draws it: beds and a table
inside, shelves along the walls, a low fence across the front and steps up to a doorway in the
middle of it. The footprints match that drawing cell for cell. So:

- **The walls are solid, the floor is not.** A building blocks its four edges — the back wall is
  the roof strip, three quarters of a cell deep; the sides and front are a quarter — and leaves
  **one cell of doorway** in the middle of the front wall, where the steps are. Nobody walks
  through a wall and everybody can walk in.
- **Buildings draw below every figure**, in a band of their own above the ground and under the
  actors. Sorting a building with the actors put the floor of the barracks on top of whoever had
  walked in, which read as the survivor being swallowed by their own bed.
- Turning a building round and improving it into a bigger one **rebuild the room**: walls,
  doorway and posts all move with it.

**A survivor at work stands at their post.** Each level has as many posts as it has work posts,
spread along the back of the room where the furniture is drawn. Put somebody to work and the
figure walks to the doorstep, in through the door, and stands at their bench until the job is
taken away; send them home and they go back to wandering the plot. Two workers in one building
stand at two different posts. Going to the door first and the post second is deliberate: it is
what a person does, and it is the whole of the pathfinding a one-cell doorway needs.

The colony panel still decides **who** works where (§4.3). The plot only shows the answer.

#### 4.2.3 The building menu

Clicking a standing building opens a **ring of four options around it**, rather than putting a
line of keys at the bottom of the screen: improve (upper left, with the price), put somebody to
work (upper right, with how many posts are filled), send one worker home (lower right), tear it
down (lower left, with what comes back). A slice that cannot be pressed is greyed out **and still
says why** — "NO ROOM", "AT ITS BEST", "NO JOBS HERE" — because a slice that goes blank reads as
a broken menu rather than as an answer.

The keys `G` and `X` still improve and tear down for anyone who has learnt them; the ring is how
the rest of it is found at all. Nothing on the plot acts on a click that landed on the HUD, so
pressing a slice never also puts a building down behind it.

### 4.3 Colony systems

- **Power grid**: each building has a draw; generators/solar supply. Deficit causes brownouts (random
  buildings stop working, lights fail, morale drops, cold storage spoils food).
- **Water**: residents consume; farms, infirmary and canteen consume. Unfiltered water → *Dysentery*.
- **Food**: produced by farms + looted. Spoilage timer per food type. Shortage → starvation debuff →
  morale collapse → survivors leave or steal.
- **Morale** (per survivor 0–100, plus colony average): driven by food quality & variety, bed/privacy,
  warmth, safety, recent deaths, workload, relationships, recreation. Low morale → slow work, refusal
  to work, fights, desertion. High morale → work speed bonus, occasional bonus events.
- **Health & disease**: injuries (bleeding, fracture, infection), illnesses (flu, dysentery,
  pneumonia in winter), and the **Bite** — bites have a chance of infection; amputation within a time
  window is the only cure, otherwise the survivor turns inside the base. **Survivors only** — the MC
  cannot be bitten or turn (see §9).
- **Jobs**: each survivor is assigned to a building/role with a priority list (Farmer, Crafter,
  Guard, Medic, Builder, Hauler, Cook, Engineer). Skill improves with work. Idle survivors wander and
  lose morale.

### 4.4 Colony Level
A derived level (1–5) from population, building tiers, total skill, defence rating and stockpile
value (TV). It gates base tier unlocks, faction attention and — crucially — **wave scaling**.

### 4.5 Losing a base — the Exodus
A base is never a soft reset. When a wave or raid overruns the site, the colony does not restart at
Tier 1 with a clean slate — it flees as a broken group and has to claw its way back.

- **The pick:** the player chooses **4 survivors** (the MC always escapes on top of those) who get
  out with the squad. They keep what they personally carry — carry weight is suddenly the most
  important stat in the game.
- **The left behind:** every other resident stays in the world as a **scattered survivor**, not a
  corpse and not a save-game entry that quietly disappears. Each gets a hidden fate roll over the
  following days, weighted by their Survival and Stealth skills, their health when the base fell, and
  how deep into the fall they were abandoned.
- **Reunions:** a scattered survivor may later **find the new base on their own** and show up at the
  gate, be discovered as a rescue objective on a mission, turn up in a faction's camp (buy them back,
  or earn them), or simply never be seen again. Some arrive wounded, traumatised or carrying a bite.
- **The cost is real:** grief debuffs for friends left behind, a morale floor until the colony is
  housed again, and the loss of every building. Materials are **not** refunded — this is a rout, not
  a move.
- **Refugee state:** until a new site is claimed, the group travels light with no base layer at all.
  There is a grace period before the first wave targets them again, scaled by difficulty.

This makes an overrun base a story beat instead of a game over, and gives the player a reason to go
looking for their own people later.

---

### 4.6 The run: the base, the day and the raid

The base and the zones are one run, not two screens. A run begins in the main menu, lives on the
plot, and leaves it one zone at a time.

**What a run holds.** A run is the colony's whole state between scenes: the stores, the people, the
buildings on the plot, how many days have been lived through, and which day the squad last went out
on. It outlives a scene change, because a raid unloads the base scene and the colony has to still be
there when the squad gets home. Starting a new run in the menu is what creates it; nothing else does.

**Two buttons, two kinds of time.** The base is turn-shaped (§12): nothing moves until the player
says the day is over. So the base has exactly two ways to spend a day.

| | What it does | How often |
|---|---|---|
| **END DAY** | Resolves the day — power, work, fuel, meals — and rolls the calendar over. | Any time |
| **RAID** | Takes the squad out to a zone. | **Once per day** |

One raid a day is the whole reason a day is a decision. A squad that can go out again the moment it
gets back turns the base into a shop, and the colony's day into something that happens in the
background. One trip means the player has to choose what that trip is for, and then live with the
rest of the day as it was built.

**Leaving and coming home.** The base writes down where home is and that the squad is out, captures
the colony, and loads the zone. The zone neither knows nor cares that it was reached from a base: it
runs the mission of §8 as it always does, and extraction is still required. What it banks is what
comes home, into the stores as salvage. A run that ends with the squad lost comes home with nothing,
and the day is still spent — which is exactly the cost that makes leaving early a real option.

**The opening objectives.** A new colony is given a short list rather than a tutorial: a barracks, a
farm, somebody working, and one raid brought home. They are a checklist derived from the plot and the
people, not a scripted sequence — a barracks torn down un-ticks its line. Together they are the
smallest set that makes the player touch every system the base has: placement, cost, jobs, the day,
and the way out.

**The MC stands on the plot.** The base is not a map screen with a cursor on it: the character the
player controls everywhere else is down there among the buildings, and is walked about with the same
keys as a zone. That is what §6 assumes when it says the defence is played in real time at the base
— a wave cannot be fought by something that is not there — and it is what makes the plot a place
rather than a diagram.

A walking character and a turn-shaped base are not in conflict, because **the clock is not a
movement clock**. Time of day in the base only moves when the player ends the day (§12); walking
about, like turning a building round or reading a panel, costs nothing and takes as long as it
takes. What the player spends by acting is a **day**, not a second.

Because the person on the plot is the MC, they are **whoever was picked in the menu** — the base
does not have a cast of its own. The roster's default is therefore the character the colony scene is
built around, so that pressing START and walking about are the same person without the player having
to choose correctly first.

**The one not picked is already living there.** The menu offers a choice between people, so the
roster character the player turns down is the colony's **first survivor**, with a body on the plot
that walks about on its own. Whichever card is pressed, the base holds two figures and never the
same figure twice: the colony reads as somebody else's home the MC has walked into, rather than an
empty plot with a list of names attached to it.

That survivor is a **body, not a worker**. They keep to the plot, carry nothing and are wired to no
system: who does which job is still decided in the colony panel (§4.4), an assigned survivor does
not walk to their farm, and the panel does not care where this one is standing. Strolling is the
same legs as the MC's — the same walls stop them and the same gait draws them — driven by a spot on
the plot and a pause, with the pace, the pauses and the patience in the `BalanceConfig`. When the
colony grows bodies for everybody, it grows them from this.

The MC on the plot **carries no weapon**. There is nothing there to shoot, the mouse button is
already spoken for by building, and a gun that only ever hits your own farm is worse than no gun.
They get one back when something arrives worth shooting at.

---

## 5. The Noise / Heat system

A single visible meter in the base HUD, shown as a colour-coded gauge with a breakdown tooltip.

```
Base Noise = Σ building noise (running generators, workshops, radio tower, floodlights)
           + population factor
           + recent gunfire on-site
           + Colony Level factor
           - dampening (walls, silencers, insulation, soundproofed workshop upgrade,
                        rain/snow weather, noise traps placed away from base)
```

- Noise directly feeds **wave size, wave frequency, and special-zombie chance**.
- Players can deliberately run "quiet weeks" (shut down generators, pause the factory) before an
  expected big wave — at the cost of production.
- **Noise traps** (car alarms, speaker rigs placed on the map away from the base) divert a share of a
  wave to a different approach lane.
- Missions have their own local heat meter: gunfire and alarms escalate zone spawns in real time.

---

## 6. Zombie waves & defence

- Waves arrive on a visible **countdown** (days). The player sees an estimated strength ("Est. 40–55,
  two lanes, Howler likely") from scouts/watchtowers — accuracy scales with Perception and tower level.
- Wave composition scales with **Noise + Colony Level + base size + days survived + season**.
- **Approach lanes**: zombies path from map edges toward the loudest point. Base layout determines
  where they hit. Smart layouts funnel them into kill zones.
- Defence is played in real time at the base: the player controls their squad on the walls, while
  assigned **Guards** hold positions automatically. Traps and turrets fire on their own.
- Losing: zombies breaching walls destroy buildings, kill or wound residents, eat food stores. A
  catastrophic breach can force an **evacuation** — flee with what you can carry, lose the base. This
  is the Exodus: see §4.5 for who gets out and what happens to everyone left behind.

**Zombie types:**
| Type | Role |
|------|------|
| Walker | Baseline, slow, in numbers |
| Runner | Fast, fragile, appears from night 10+ |
| Howler | Screams → massive local noise spike, calls more |
| Bloater | Explodes into toxic gas, ruins crops and chokes defenders |
| Armored | Riot/military corpse, resists bullets, weak to blunt/fire |
| Crawler | Low profile, slips under fences, ignores some traps |
| **The Horde Mother** (regional boss) | Rare, draws a permanent mega-horde to its region until killed |

---

## 7. World map & travel

- A **strategic map of Europe** (stylised, hand-drawn pixel), divided into **regions** (e.g. North
  German Plain, Ruhr Valley, Alpine Corridor, Benelux Ports, Czech Borderlands, French Farmland,
  Baltic Coast). Regions unlock progressively by distance, fuel capacity, faction access and story
  beats.
- The player selects a **node** (town, factory, hospital, farm, military checkpoint, faction camp) and
  travels there. Travel consumes **time, fuel, food and vehicle condition**, and rolls
  **road events** (ambush, blocked road, stranded survivor, broken bridge, trader convoy) resolved
  with a short choice screen or by dropping into a small combat zone.
- Arriving loads a **handcrafted-template + procedurally-dressed zone** that is fully playable
  top-down. Zones are persistent per node: loot depletes and slowly respawns, cleared areas stay
  cleared for a while, and the player's changes (opened doors, broken walls) persist for a few weeks.
- **Vehicles**: van, pickup, armoured bus (late). Stats: fuel efficiency, cargo slots, passenger
  seats, durability, noise. Repairable and upgradable at the workshop. Losing a vehicle on the road
  means a long, dangerous walk home.

---

## 8. Missions

Missions are started from the world map, from the radio tower, or from faction contacts.

**Mission types:**
- **Scavenge** — loot a zone against a timer/heat meter and extract.
- **Rescue** — reach a survivor before they die; they join the colony.
- **Claim** — clear and secure a candidate base site.
- **Supply run** — fetch a specific resource a faction or your colony urgently needs.
- **Sabotage/Raid** — hit a rival faction or raider camp (human enemies, reputation consequences).
- **Escort / Convoy** — protect a vehicle or group through a zone.
- **Investigate** — story-flavoured regional mysteries (why is this region overrun?), leading to
  regional objectives such as killing a Horde Mother or restoring a bridge/railway (which permanently
  improves travel in that region).

Each mission defines: objective(s), extraction point, zone heat profile, loot tables, zombie density,
optional objectives, and a failure state. **Extraction is always required** — loot only counts once
the squad is in the vehicle.

**Loot is named, not generic.** A container does not hold "supplies" — it holds salvage, food,
water or fuel, the same four stores the colony runs on (§4.3). What a run came home with therefore
answers a question the base is already asking: a colony two days from going thirsty wants a zone
with water in it, and a raid that comes back with forty salvage and no food has not solved the
problem the player went out to solve.

The **loot table** is the mission's, not the zone's. The map says where containers stand; the
mission says what is in them, as a list of fills dealt round the containers in map order. Two
consequences are deliberate. The same streets can be run twice for different things, which is what
makes a handcrafted zone worth revisiting. And the kinds are spread across the zone rather than
pooled in one room, so a run cannot be a single trip to the water cupboard — getting all four
stores out of one town means crossing it.

The **objective still counts in supplies**, totalled across kinds. What a run is sent for is a
quantity of stuff, because that is the decision the player makes while standing in a zone with
zombies in it; which stores it turns into is a thing they read on the debrief and act on back home.

**Note on the campaign:** there is **no fixed story ending**. The game is an open-ended survival
sandbox with layered mission chains and regional objectives; the long-term goal is building the
strongest colony in Europe and clearing/stabilising regions. (An endgame scenario can be added later
as DLC-style content without touching the core design.)

---

## 9. Squad & combat

- The MC is always the controlled character. Up to **3 companions** follow as AI; they cannot be
  switched to. The player gives them **simple orders** via a radial menu: *Follow / Hold here /
  Focus target / Stealth / Fall back / Use item*.
- **The MC cannot be bitten and cannot turn.** Zombies wound the MC like any other threat, but the
  infection line is closed for the player character — turning is a fate reserved for survivors and
  companions (see §4.3). This keeps the Exodus (§4.5) and difficulty-mode MC rules (§3.4) the only
  ways a run can end, instead of a silent timer after one unlucky hit.
- Combat is **real-time twin-stick**: WASD move, mouse aim/fire, gamepad dual-stick. Dodge roll,
  sprint (loud, drains stamina), crouch (quiet, slower, smaller detection cone).
- Weapons: melee (bat, axe, crowbar, machete — quiet, stamina-based, durability) and firearms
  (pistol, shotgun, SMG, rifle, DMR — loud, ammo-limited, jam chance when worn). Attachments:
  silencer, sights, extended mag, grip. Throwables: molotov, pipe bomb, flashbang, noisemaker.
- **Stealth** is a first-class option: vision cones, sound radii, line of sight through foliage and
  buildings, silent takedowns on unaware zombies. Entire missions can be done without firing a shot.
- **Stamina** governs sprint, melee swings and dodges. **Carry weight** limits loot and slows the
  squad.
- Companion AI uses a behaviour-tree stack: order → threat response → self-preservation → idle follow.
  They must never block doorways or shoot through the MC.

---

## 10. Survivors

- Generated with name, nationality, portrait, 1–2 traits, skill spread, backstory line.
- Recruited via Rescue missions, the radio tower, faction rewards, and random base arrivals
  (which the player may turn away — refusing costs morale with some survivors).
- Skills: Firearms, Melee, Medicine, Engineering, Farming, Crafting, Negotiation, Stealth, Survival.
  All improve through use.
- Relationships: friendships and rivalries form over time, affecting morale and work output.
  Deaths create grief debuffs for friends.
- Any survivor can be taken on missions as a companion; losing them in Survival/Ironman is permanent.

---

## 11. Factions & trade

Named groups with territory, demands and attitudes. Reputation per faction (−100 … +100).

| Faction | Flavour | Wants | Offers |
|---------|---------|-------|--------|
| **Restbataillon** | Remnant army, disciplined, authoritarian | Fuel, order, conscripts | Military weapons, ammo, armour |
| **Genesis** | Medical/aid convoy, idealistic | Medicine, safe passage, rescued civilians | Medicine, doctors, disease cures |
| **The Yard** | Scavenger guild, pragmatic | Scrap, machine parts | Vehicles, workshop blueprints, repairs |
| **Die Börse** | Black market cartel | Cash, metals, contraband, silence | Anything, at a price; information |
| **Raider clans** | Hostile, opportunistic | Nothing | Loot (by force), base raids |

- **Trade is barter.** Cash died with the state; nobody outside a vault takes euros. Goods are valued
  in an abstract **Trade Value (TV)** that the UI shows on both sides of a deal — ammo, fuel, meds,
  batteries and liquor are the hard currencies. Prices shift by region, season, scarcity and
  reputation, and every faction over- and undervalues what it needs (see the table above). **Die Börse
  is the one exception**: the cartel still deals in cash and precious metals, which is exactly what
  makes it sinister — it is betting on a world that comes back. Caravans can be sent out from the
  base with assigned survivors (risk of ambush).
- **Consequences**: helping one faction angers another. High reputation unlocks allied reinforcements
  during waves, exclusive tech and better base candidates. Hostile reputation triggers **human raids**
  on the base — a different defence challenge than zombies (they use cover, guns and fire).

---

## 12. Time, weather & seasons

- **The base layer is turn-based by day.** Base time only advances when the player **confirms the end
  of the day** from the base HUD. Nothing ticks while the player is laying out buildings, assigning
  jobs or reading reports — jobs, production, farm growth, healing, construction, food spoilage and
  the wave countdown all resolve in the **end-of-day step**, which reports back as a morning digest.
  Missions, travel encounters and wave defence are the opposite: **fully real-time** while the player
  is in a zone. Confirming a day is therefore the game's one deliberate heartbeat, and the moment the
  player chooses to stop preparing.
- **Day/night cycle** (~24 min real-time per in-game day, configurable) — this clock runs **while the
  player is in a zone**: missions, travel and wave defence. The base layer jumps straight to the next
  morning when the day is confirmed. Night: zombie senses sharper, vision cone shorter, runners more
  active, stealth easier, light sources = noise/visibility beacons.
- **Weather**: clear, rain (dampens noise, hurts visibility, fills water tanks), fog (strong stealth,
  surprise encounters), storm (power damage to solar/wind, travel blocked), snow.
- **Seasons** (each ~15–20 in-game days): Spring (planting), Summer (peak yield, drought risk),
  Autumn (harvest, best scavenging), **Winter** (no outdoor crops, heating fuel required, illness,
  frozen roads, but the quietest and slowest zombies).
- Seasons create the long rhythm: everything you build in summer is spent surviving winter.

---

## 13. Art & audio direction

- **Visuals:** top-down, slightly pixelated. Target sprite base **64×64 px per tile**, characters
  roughly one tile tall (about 48×64), rendered at **64 pixels per unit** with a pixel-perfect
  camera at a **960×540** reference resolution — which shows 15 by 8.4 tiles and upscales to 1080p
  exactly twice. Limited palette (~48 colours),
  muted desaturated European tones; blood, fire and faction colours are the only saturated accents.
- **Lighting:** URP 2D lights — day/night global light ramp, flashlights with cone shadows, fire,
  muzzle flashes, floodlights, window glow. Lighting is a gameplay system, not decoration.
- **UI:** diegetic where possible — a clipboard for base management, a paper map for travel, a radio
  for faction comms.
- **HUD type:** the whole interface is set in one **pixel face**, never a smooth system font — a
  HUD drawn at 64 pixels to the tile and labelled in a smooth face reads as two games at once.
  Glyphs are drawn **white** and tinted by the label, so a label's colour is the text's entire
  appearance and never a tint fighting something baked in. The HUD is written in **dark blue**,
  light enough to hold against the grass it is read over.
  **A line that does not apply washes out towards grey — it never turns red.** An option that cannot
  be taken goes pale and recedes; alarm colours are reserved for the world, not the readout.
  Sizes are whole multiples of the seven-pixel body (14 for body text, 21 for buttons); anything
  else lands a five-pixel-wide letter on fractional screen pixels and changes its shape. Being
  monospaced, a line's width is its length times the advance exactly — lay HUD rects out by counting
  characters, not by eye.
- **Audio:** sparse ambient wind and distant moans; music only on waves, discoveries and death.
  Occlusion-aware positional SFX — hearing matters as much for the player as for the zombies.

---

## 14. Scope & milestones

| Milestone | Content | Definition of done |
|-----------|---------|--------------------|
| **M0 — Foundations** | Project setup, input, pixel camera, tilemap rendering, save system skeleton | A character walks around a hand-made test zone |
| **M1 — Combat vertical slice** | Melee + firearms, stamina, zombie AI (walker), noise propagation, stealth cones, one handcrafted zone | A fun 10-minute scavenge-and-extract loop |
| **M2 — Base core** | Claimed Tier-1 base, free placement, Farms/Barracks/Factory/Armory, jobs, power, water, food | Base survives 5 in-game days with assigned jobs |
| **M3 — Waves** | Wave scheduler, noise meter, approach lanes, defence structures, guard AI, breach consequences | Three escalating waves playable end to end |
| **M4 — World & missions** | Strategic map, 2 regions, travel + fuel + road events, 4 mission types, vehicles | Travel out, loot, return, build — full loop |
| **M5 — Depth** | Survivors, morale, health/disease, relationships, companions + orders, difficulty modes | A 2-hour run feels different each time |
| **M6 — Factions & seasons** | 4 factions, trade, reputation, human raids, seasons & weather | A full in-game year is survivable and interesting |
| **M7 — Content & polish** | 5 backgrounds, base tiers 1–5, remaining regions, special zombies, audio, UX pass, balancing | Vertical slice becomes a shippable Early Access build |

**Explicitly out of scope for v1:** multiplayer/co-op, console ports, modding API, procedural zone
generation beyond template dressing, a scripted ending campaign.

---

## 15. Settled decisions

The open questions of the first draft, plus what has been settled since. Each one is written into
the sections above; they are recorded here so the reasoning is not lost.

| Question | Decision | Written into |
|----------|----------|--------------|
| How punishing should base loss be? | Neither a full Tier-1 restart nor a free pass: **the Exodus** — pick 4 survivors to escape with the MC, everyone else becomes a scattered survivor who may return, be rescued, be bought back, or be lost | §4.5, §6 |
| Can the MC be bitten and turn? | **No.** The MC can be wounded but never infected; turning belongs to survivors and companions | §9, §4.3 |
| Real-time base management or day confirmation? | **Day confirmation.** The base layer is turn-based and resolves in an end-of-day step; zones stay fully real-time | §12 |
| Fixed or randomised base candidate sites? | **Randomised per run**, drawn from a larger handcrafted pool | §4.1 |
| Is cash (€) still credible? | **No — barter replaces it.** Goods are priced in abstract Trade Value (TV); Die Börse is the one faction still dealing in cash and metals | §11, §4.1 |
| Which pixel scale does the art run at? | **64 px per tile, 64 pixels per unit, 960×540 reference.** Raised from 32 and 480×270 so bought 64 px character art fits a tile; the visible area and the exact 1080p upscale are unchanged | §13, CLAUDE.md §4 |

New open questions go here as they come up — this section should never be empty for long.
