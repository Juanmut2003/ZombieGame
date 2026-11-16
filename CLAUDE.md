# CLAUDE.md

Guidance for Claude Code when working in this repository.

> Read [PROJECT.md](PROJECT.md) before implementing any gameplay feature. It is the single source of
> truth for design. If code and PROJECT.md disagree, ask — do not silently pick one.

---

## 1. Project at a glance

Top-down, slightly pixelated zombie-apocalypse survival game: base building, survivor management,
resource economy, wave defence, and real-time twin-stick missions across a strategic map of Europe.
Singleplayer only.

- **Engine:** Unity 6 LTS (`6000.x`) — do not upgrade the editor version without asking.
- **Render pipeline:** Universal RP, **2D Renderer** (URP 2D lights). Not 3D, not built-in.
- **Language:** C#, .NET Standard 2.1 profile.
- **Input:** new **Input System** package only. Never `UnityEngine.Input` (legacy).
- **Target:** Windows PC first, 1080p/60fps on modest hardware.

---

## 2. Repository layout

```
Assets/
  _Project/                 # all first-party content lives here
    Art/
      Sprites/{Characters,Zombies,Tiles,Buildings,Items,UI,FX}
      Animations/
      Palettes/
    Audio/{SFX,Music,Ambience}
    Data/                   # ScriptableObject assets (items, recipes, buildings, zombies, ...)
    Prefabs/{Characters,Zombies,Buildings,Props,UI,Systems}
    Scenes/
      Boot.unity            # bootstrap, loads persistent systems, then MainMenu
      MainMenu.unity
      Base.unity            # the colony scene
      WorldMap.unity        # strategic Europe map
      Mission_*.unity       # mission zones
    Scripts/
      Core/                 # service locator, save/load, scene loading, time, events
      Characters/           # MC, survivors, companions, stats, inventory
      Combat/               # weapons, damage, stamina, projectiles, melee
      AI/                   # zombie AI, companion AI, guard AI, pathfinding
      Base/                 # placement grid, buildings, power, water, jobs, morale
      Waves/                # wave scheduler, noise/heat, spawn lanes
      World/                # strategic map, travel, regions, nodes, road events
      Missions/             # mission definitions, objectives, extraction
      Factions/             # reputation, trade, raids
      Economy/              # resources, inventory, crafting, spoilage
      UI/                   # views + presenters, HUD, menus
      Utils/
    Settings/               # URP assets, input actions, quality settings
  Plugins/                  # third-party only, never edit by hand
Docs/                       # design notes, diagrams, ADRs
```

**Never** put first-party content outside `Assets/_Project/`. Keep third-party packages in
`Plugins/` or via the Package Manager — never vendored into `_Project`.

---

## 3. Coding standards

- **Naming:** `PascalCase` for types/methods/properties, `camelCase` for locals/parameters,
  `_camelCase` for private fields, `ALL_CAPS` for `const`. One public type per file, filename ==
  type name.
- **Namespaces:** `ZombieGame.<Area>` mirroring the folder, e.g. `ZombieGame.Base.Power`.
- Prefer `[SerializeField] private` over `public` fields. Expose read-only properties instead.
- Use `TryGet*` patterns and guard clauses; avoid deep nesting.
- `#nullable enable` in new files where practical.
- XML doc comments on public APIs of systems; inline comments only for non-obvious *why*, never *what*.
- No `#region`. No `using UnityEngine;` wildcards of convenience in data-only files.

### Unity-specific rules
- **No `GameObject.Find`, `FindObjectOfType`, or `SendMessage` in gameplay code.** Use serialized
  references, the service locator in `Core/`, or events.
- **No singletons except the explicitly approved ones** in `Core/` (`GameServices`, `SaveSystem`,
  `GameClock`, `EventBus`). New singletons require a discussion first.
- Cache component lookups in `Awake`. Never call `GetComponent` in `Update`.
- Nothing allocates per frame in `Update`/`FixedUpdate` — pool projectiles, VFX, damage numbers,
  zombies. There is a pooling utility in `Core/Pooling`; use it.
- Physics for zombies/projectiles runs in `FixedUpdate`; input and camera in `Update`/`LateUpdate`.
- Use `Time.deltaTime`-scaled logic everywhere; the game supports pause and time-scale changes via
  `GameClock`, not by touching `Time.timeScale` directly.
- Zombie counts can reach several hundred. Favour **data-oriented batch updates** (one manager
  ticking an array of zombie states) over hundreds of `MonoBehaviour.Update` calls. When in doubt,
  write the manager-driven version.

### Data-driven design
All content is **ScriptableObjects** in `Assets/_Project/Data/`: `ItemDefinition`, `WeaponDefinition`,
`RecipeDefinition`, `BuildingDefinition`, `ZombieDefinition`, `MissionDefinition`, `BackgroundDefinition`,
`TraitDefinition`, `FactionDefinition`, `RegionDefinition`.

**Never hardcode balance numbers in C#.** Damage, costs, yields, wave formulas, noise weights and
unlock thresholds live in SO assets or in a `BalanceConfig` SO. Code reads them; designers tune them.

### Architecture
- Systems communicate through the **EventBus** (`Core/Events`) for cross-domain signals
  (`SurvivorDied`, `WaveStarted`, `PowerDeficitChanged`), and direct references within a domain.
- UI uses a **view + presenter** split: `UI/` never contains gameplay logic, and gameplay never
  references a UI type.
- Saving: every persistent system implements `ISaveable` with a plain serializable DTO. Saves are
  JSON, versioned with a `saveVersion` field, and must migrate forward — **never break old saves
  without a migration step**.

---

## 4. Art & audio pipeline

- Sprites: **Point (no filter)**, Compression **None**, `Pixels Per Unit = 32`, Mesh Type `Full Rect`,
  pivot documented per sprite type. No mipmaps.
- Pixel Perfect Camera component on the main camera — do not change its reference resolution
  (`480×270`) without a design discussion.
- Stick to the palette in `Art/Palettes/`. New colours need a reason.
- Sprite atlases per category to keep draw calls low. One atlas per `Sprites/` subfolder.
- Audio: `.ogg` for music/ambience, `.wav` for short SFX. SFX play through an audio service with
  per-category volume buses; never `AudioSource.PlayClipAtPoint`.

---

## 5. Git workflow

- `main` is protected and always buildable. Work on branches: `feature/…`, `fix/…`, `docs/…`,
  `balance/…`.
- **Conventional commits:** `feat(base): add power grid brownout handling`,
  `fix(ai): stop companions blocking doorways`, `docs: expand wave scaling notes`.
- Commit `.meta` files **always**, together with their asset. A missing `.meta` breaks everyone's
  project.
- Never commit: `Library/`, `Temp/`, `Obj/`, `Build/`, `Builds/`, `Logs/`, `UserSettings/`,
  `*.csproj`, `*.sln`. These are in `.gitignore` — keep it that way.
- Large binaries (audio, big textures) go through **Git LFS**. Check `.gitattributes` before adding
  new binary types.
- **Unity scenes and prefabs merge badly.** Only one person/branch touches a given scene at a time.
  Prefer prefabs over scene edits. For gameplay changes, prefer code + ScriptableObjects over scene
  wiring. If a scene conflict happens, ask the user rather than resolving it blind.
- Do not commit or push unless the user asks.

---

## 6. Testing & verification

- Unit-testable logic (wave formulas, noise calculation, economy, save DTOs, job assignment) lives in
  **plain C# classes with no `MonoBehaviour` dependency**, tested with Unity Test Framework EditMode
  tests in `Assets/_Project/Tests/EditMode/`.
- PlayMode tests in `Tests/PlayMode/` cover integration (spawning, damage, placement validity).
- Before claiming a change works: compile cleanly with **zero warnings in `_Project` code**, and run
  the EditMode tests.
- Claude cannot open the Unity Editor. **Do not claim something is visually verified.** State plainly
  what was and was not tested, and list the manual steps the user should run in the Editor.

---

## 7. Working agreements for Claude

1. **Design changes go to PROJECT.md first.** If a task implies a design decision not covered there,
   ask, then update PROJECT.md in the same change.
2. **Ask before:** adding a third-party package or asset store dependency, changing the Unity or URP
   version, restructuring folders, introducing a new singleton or architectural pattern, touching
   `.gitignore`/`.gitattributes`/project settings.
3. **Scope discipline.** Implement what was asked. Note adjacent problems instead of fixing them
   unasked.
4. **No placeholder stubs presented as finished.** If something is a stub, say so and mark it
   `// TODO:` with context.
5. **Performance is a feature.** Any system touching zombies, lights or tiles must consider hundreds
   of instances from the start.
6. **Keep docs in English**, including commit messages, code comments and both markdown docs — even
   when the conversation is in German.
7. When a feature spans base + mission layers, implement the shared logic once in `Core/` or the
   owning domain, never twice.
8. Prefer small, reviewable commits over one large change.

---

## 8. Glossary

| Term | Meaning |
|------|---------|
| **MC** | Main Character — the player-created, always-controlled character |
| **Colony** | The player's whole organisation: survivors, resources, reputation |
| **Base / Site** | The physical claimed location the colony occupies |
| **Colony Level** | Derived 1–5 rating gating base tiers and wave scaling |
| **Noise / Heat** | Visible meter driving wave size and mission escalation |
| **Lane** | A path zombies use to approach the base |
| **Zone** | A loaded, playable mission location on the world map |
| **Node** | A point on the strategic Europe map that loads into a zone |
| **Claim Mission** | Mission required to take over a new base site |
