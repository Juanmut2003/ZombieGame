# PROJECT.md — Game Design Document

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
  rest quality. Variants: bunk room (cheap, crowded) → dorm → private quarters.
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

- **Visuals:** top-down, slightly pixelated. Target sprite base 32×32 px per tile, characters ~24×32,
  rendered at a fixed pixels-per-unit with a pixel-perfect camera. Limited palette (~48 colours),
  muted desaturated European tones; blood, fire and faction colours are the only saturated accents.
- **Lighting:** URP 2D lights — day/night global light ramp, flashlights with cone shadows, fire,
  muzzle flashes, floodlights, window glow. Lighting is a gameplay system, not decoration.
- **UI:** diegetic where possible — a clipboard for base management, a paper map for travel, a radio
  for faction comms. Pixel font with a clean fallback for readability.
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

These were the open questions of the first draft. All five are now decided and written into the
sections above; they are recorded here so the reasoning is not lost.

| Question | Decision | Written into |
|----------|----------|--------------|
| How punishing should base loss be? | Neither a full Tier-1 restart nor a free pass: **the Exodus** — pick 4 survivors to escape with the MC, everyone else becomes a scattered survivor who may return, be rescued, be bought back, or be lost | §4.5, §6 |
| Can the MC be bitten and turn? | **No.** The MC can be wounded but never infected; turning belongs to survivors and companions | §9, §4.3 |
| Real-time base management or day confirmation? | **Day confirmation.** The base layer is turn-based and resolves in an end-of-day step; zones stay fully real-time | §12 |
| Fixed or randomised base candidate sites? | **Randomised per run**, drawn from a larger handcrafted pool | §4.1 |
| Is cash (€) still credible? | **No — barter replaces it.** Goods are priced in abstract Trade Value (TV); Die Börse is the one faction still dealing in cash and metals | §11, §4.1 |

New open questions go here as they come up — this section should never be empty for long.
