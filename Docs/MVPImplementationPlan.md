# Dagon - MVP Implementation Plan

## Current State Snapshot

This document started as the original MVP plan. The project has moved well past that narrow scope, so this top section records the actual implemented context as of March 2026. The older MVP breakdown below is still useful as historical planning, but it no longer reflects the full game state.

## What Exists Now

### Runtime architecture

- `BlackMire` is the main gameplay scene and canonical run path.
- Runtime assembly is handled by `SceneRuntimeBuilder`, not the old prototype bootstrap.
- The main run is now continuous: biome progression happens in-place without scene loads, player repositioning, or run-state reset.
- `WorldProgressionDirector` advances the active biome after boss kills and refreshes the world locally around the player.
- `DeveloperSandbox` is a debug scene/runtime path with manual controls and an asset showcase.

### Player / characters

- The game now has three playable characters selected before the run:
  - `Sailor`: `Harpoon Cast` + `Brine Surge`
  - `Deckhand`: `Anchor Chain` + `Bilge Dash`
  - `Captain`: `Rot Lantern` + `Frenzy`
- Character selection is wired into the main menu flow.
- Character profiles are data-driven through `CharacterProfileDefinition` and `RuntimeCharacterCatalog`.
- Character art is now using real imported assets for `Deckhand` and `Captain`, while keeping portrait-path and runtime-sprite-path separate so portraits can diverge later.

### Weapons / actives

- Base and alternate weapons implemented in runtime:
  - `Harpoon Cast`
  - `Anchor Chain`
  - `Rot Lantern`
  - `Bilge Spray`
  - `Rot Beacon Bomb`
  - `Floodline`
  - `Tideburst`
- Active abilities implemented:
  - `Brine Surge`
  - `Bilge Dash`
  - `Frenzy`
- Signature actives can now level up through the reward flow instead of being static one-off skills.
- Weapon range indicators and active attack-area overlays have been added and iterated for readability.

### Enemy roster

- Fodder:
  - `Mire Wretch`
  - `Parasite`
- Specialists:
  - `Drowned Acolyte`
  - `Mermaid`
  - `Watcher Eye`
- Elites:
  - `Deep Spawn`
- Boss-only summons:
  - `Wide Leech`
  - `Tall Leech`

### Boss roster

- `Mire Colossus`
- `Monolith of the Mire`
- `Drowned Admiral`

Bosses are now regular interchangeable run bosses:

- the run randomly selects from the boss pool
- bosses do not repeat until the available pool is exhausted
- later boss spawns scale based on total bosses previously defeated
- sandbox boss spawns use the same runtime definition path

### Spawning / progression

- Enemy spawning now uses ambient pressure plus stronger ramping instead of only obvious opening-wave behavior.
- Spawn rate and alive-cap ramp over time.
- Boss fights begin inside the existing run state instead of loading a separate boss scene.
- Ambient enemies can persist into boss fights, with boss-phase spawn throttling instead of a hard full stop.
- Parasites can spawn in grouped packs.
- Health pickups now drop from specialist and elite enemies at configured rates.

### Sandbox / debugging support

- Developer sandbox supports:
  - manual spawn buttons for specific enemies
  - boss spawn buttons
  - auto-spawn stop/start toggle
  - runtime pressure controls
  - asset showcase gallery
- The asset showcase is now grouped by category and is being moved toward using the same visual setup data as runtime spawns so size/readability checks are accurate.

## Immediate Implementation Focus

These are the active practical tracks implied by the current repo state:

1. Keep tightening visual parity between the developer asset showcase and real runtime presentation.
2. Continue tuning character art scale, enemy size, and boss readability based on in-scene comparisons.
3. Expand the continuous-run content set with additional enemies, bosses, biome art, and pickups without reintroducing hard scene boundaries.
4. Keep migrating duplicated runtime constants toward shared visual/config helpers so debug tools and gameplay stay aligned.
5. Close the remaining Unity verification gap by running compile/tests once the project-open lock is removed.

## Notes On Outdated Parts Below

Several sections later in this file describe older assumptions that are no longer true. In particular:

- there are now multiple playable characters
- there are now multiple bosses
- the game no longer uses a simple one-boss-one-scene progression model
- the runtime bootstrap has already evolved beyond the original prototype setup

Treat the rest of this file as baseline MVP history unless a section still matches current implementation.

## Goal

Build one complete playable run in the black mire that proves:

- the sailor is fun to move
- billboarded sprites read well in a perspective 3D world
- swarm pressure works
- at least one enemy forces prioritization
- progression inside a run changes the player build in meaningful ways

This plan is intentionally narrower than the full game vision. It focuses on the minimum functional components needed to ship a defensible first MVP.

## Current Implementation Context

This document started as the narrow MVP plan, but the repo has now moved into a more active combat and iteration pass. The current implementation state is:

- the runtime is still built scene-first through `SceneRuntimeBuilder` and `RuntimeCharacterCatalog`
- the project has moved beyond the original single-weapon MVP and now includes multiple player weapon runtimes and utility sandbox controls
- current iteration priority is combat readability, enemy spacing/pressure, sprite-driven presentation cleanup, and faster developer iteration

### Current combat / iteration direction

- low-level combat hit registration has been refactored to support unique-hit resolution across multi-collider targets
- weapon presentation is being separated from gameplay hit logic so combat can stay dependable even when art changes
- developer sandbox controls are being expanded to speed up tuning of weapons, enemies, bosses, and progression
- enemy presentation and prop dressing are being pushed away from placeholder/grid-like staging toward cleaner runtime art integration

### Current implemented combat set

Primary weapons and actives now in active runtime use include:

- `Harpoon Cast`
- `Anchor Chain`
- `Rot Lantern`
- `Bilge Spray`
- `Rot Beacon Bomb`
- `Floodline`
- `Tideburst`
- `Brine Surge` active

### Current enemy / boss roster in active runtime use

- `Mire Wretch`
- `Drowned Acolyte`
- `Mermaid`
- `Watcher Eye`
- `Parasite`
- `Deep Spawn`
- `Mire Colossus`
- `Monolith`
- `Drowned Admiral`

### Current sandbox / tuning support

The developer scene/runtime sandbox now supports:

- live weapon loadout editing
- live path upgrades
- manual enemy and boss spawning
- auto-spawn pressure controls
- character switching
- player invincibility toggle

### Current movement / crowding work

Enemy pressure is no longer only a spawn-count problem. Runtime work has started on body blocking and anti-stacking:

- enemies now use a gameplay-side body blocker layer instead of full physics collision
- normal player movement is blocked by enemy bodies
- dash remains the intended exception and can still pass through
- enemy movers are being resolved through the same body separation logic to reduce pile-ups and overlapping stacks

### Current follow-up risks

- Unity compile/play validation is still required after each major pass because this repo is often iterated in a live open editor session
- several recent systems are functionally implemented but still need tuning, especially crowd blocking feel, Floodline shove/width feel, and newer weapon upgrade balance
- visual alignment is now cleaner when gameplay leads, but bespoke asset swaps still need deliberate tuning rather than assuming 1:1 art-hit matching

## MVP Definition

The MVP is complete when a player can:

1. Start a run in the black mire.
2. Move and survive against escalating enemy waves.
3. Attack automatically with `Harpoon Cast`.
4. Use one active skill: `Brine Surge`.
5. Gain XP and level up.
6. Choose from a small pool of upgrades.
7. Encounter fodder, one specialist, one elite, and one boss.
8. Win or lose the run cleanly.
9. Return to a basic run summary screen.

## Functional Component Breakdown

## 1. Core Run Loop

This is the spine of the MVP.

### Required responsibilities

- Start a run
- Track run state
- Spawn enemies over time
- Detect player death
- Detect boss defeat or run clear
- Transition to run end

### Concrete deliverables

- `RunStateManager`
- `RunTimer`
- `RunResult` summary data
- Basic start / win / lose UI

### MVP acceptance

- A run can begin and end without manual scene intervention.
- No soft-locks if the player dies, clears the boss, or pauses.

## 2. Player Controller + Camera

This is already partially scaffolded and should be stabilized before adding content.

### Required responsibilities

- Move cleanly on the XZ plane
- Aim attacks consistently
- Keep camera readable under pressure
- Maintain good visual grounding for the player sprite

### Concrete deliverables

- Refined `PlayerMover`
- Finalized `FollowCameraRig`
- Stable perspective camera settings
- Player collision and hitbox tuning

### MVP acceptance

- Movement feels predictable in all eight directions.
- The player is never visually lost under normal combat load.
- Camera never clips or swings in a distracting way.

## 3. Combat Foundation

This is the second major pillar after movement.

### Required responsibilities

- Auto-fire primary weapon
- Projectile travel and hit detection
- Damage application
- Cooldown-based active skill
- Basic hit feedback

### Concrete deliverables

- `Harpoon Cast` finalized as the starting primary
- `Brine Surge` implemented as the first active
- Shared `Damageable` / `Health` behavior stabilized
- Basic hit flashes, impact sprites, or brief shake response

### MVP acceptance

- The player can kill enemies reliably.
- The active skill is useful under crowd pressure.
- Combat feedback is readable without final VFX polish.

## 4. Enemy Framework

The MVP does not need a huge roster. It needs distinct combat roles.

### Required enemy categories

- Fodder: `Mire Wretch`
- Specialist: one ranged or hazard-creating enemy
- Elite: one durable pressure enemy
- Boss: `The Mire Colossus`

### Concrete deliverables

- Shared enemy base setup
- Basic movement/attack state handling
- Telegraphs for specialist and boss actions
- Death behavior and cleanup

### Recommended MVP enemy set

- `Mire Wretch`: basic chase fodder
- `Drowned Acolyte`: ranged corruption projectile enemy
- `Deep Spawn`: elite bruiser that pushes through space
- `Mire Colossus`: boss with a small number of big readable attacks

### MVP acceptance

- The player can tell enemy roles apart immediately.
- Specialist and elite enemies interrupt autopilot behavior.
- Boss fight reads as a climax, not just a larger HP sponge.

## 5. Spawn Director

This is what turns isolated enemies into a bullet-heaven run.

### Required responsibilities

- Spawn enemies around the player
- Escalate intensity over time
- Mix fodder and higher-priority enemies
- Gate the boss appearance

### Concrete deliverables

- `SpawnDirector`
- Weighted spawn tables
- Intensity curve by run time
- Spawn safety rules to avoid unfair immediate overlaps

### MVP acceptance

- Early waves are survivable.
- Mid-run pressure ramps meaningfully.
- Boss spawn is deliberate and reliable.

## 6. Progression Inside A Run

This is what makes one run feel like a real bullet-heaven session instead of a sandbox.

### Required responsibilities

- XP drops or XP gain
- Level tracking
- Upgrade choice presentation
- Upgrade application

### Concrete deliverables

- `ExperienceController`
- `LevelUpPresenter`
- Small upgrade pool
- Upgrade application rules tied to weapon/player stats

### Recommended first upgrade pool

- +1 harpoon projectile
- increased harpoon damage
- faster attack rate
- wider pierce or chain behavior
- larger `Brine Surge`
- more max health

### MVP acceptance

- The player levels multiple times in one run.
- Upgrades visibly change the build.
- Choices are simple and quick to parse.

## 7. Corruption System

This is the main differentiator. MVP should include it, but in a trimmed form.

### Required responsibilities

- Track corruption value
- Trigger threshold changes
- Modify either player power or enemy pressure

### Recommended MVP scope

Implement only 3 thresholds:

- Threshold 1: slight player benefit
- Threshold 2: stronger player mutation plus higher spawn pressure
- Threshold 3: dangerous run-state escalation

### Concrete deliverables

- `CorruptionMeter` integrated into the run
- threshold events
- one visible gameplay mutation per threshold
- one visible downside per threshold

### MVP acceptance

- Corruption changes how the run feels.
- The player can notice both reward and risk.
- The system is meaningful even before deep content is added.

## 8. Presentation Layer

This should remain lean, but some UI is required for readability.

### Required HUD elements

- health
- corruption meter
- level / XP
- active skill cooldown
- simple run timer or wave indicator

### Required menus

- title or play entry point can be minimal
- pause
- run end summary

### MVP acceptance

- The player can understand health, corruption, and level state at a glance.
- Menus do not block gameplay flow or create confusion.

## 9. Data Layer

The MVP should be data-driven enough to avoid rewrites during content expansion.

### Required data types

- weapons
- enemies
- upgrades
- spawn waves or spawn tables

### Concrete deliverables

- `WeaponDefinition`
- `EnemyDefinition`
- `UpgradeDefinition`
- spawn config assets or simple scriptable tables

### MVP acceptance

- At least one new enemy or upgrade can be added mostly through data.

## 10. Scene / Prefab Conversion

Right now the runtime bootstrap is the fastest test path. MVP should gradually move from pure bootstrap to authored prefabs and scenes.

### Recommended transition

Phase 1:

- Keep runtime bootstrap for iteration speed.

Phase 2:

- Move player, enemies, and boss to prefabs.
- Keep only high-level scene setup or debug spawning in the bootstrap.

### MVP acceptance

- Core actors are prefab-based before content volume grows.

## Build Order

This is the recommended sequence.

### Phase 1: Stabilize the toy

1. Clean up the current playable scene bootstrap.
2. Finalize player movement, camera, and billboard readability.
3. Finalize harpoon combat against one enemy type.

### Phase 2: Make it a run

4. Add spawn director and wave escalation.
5. Add XP, leveling, and upgrade choice UI.
6. Add `Brine Surge`.

### Phase 3: Add threat diversity

7. Add one specialist enemy.
8. Add one elite enemy.
9. Tune crowd pressure and readability.

### Phase 4: Add identity

10. Integrate corruption thresholds into the run.
11. Add boss encounter and run clear logic.
12. Add basic end-of-run summary.

### Phase 5: Harden the MVP

13. Convert major actors to prefabs and data assets.
14. Replace temporary visuals and placeholder effects where they block readability.
15. Add smoke-test coverage through repeatable play sessions.

## Recommended First Engineering Milestones

These are the highest-value short-term milestones.

### Milestone A: Playable Combat Box

- Player movement
- Camera
- Harpoon attack
- Mire wretch enemies
- Basic damage

Success condition:

- 2 minutes of play feels stable.

### Milestone B: Survivors Loop

- Spawn escalation
- XP
- Level-up choices
- `Brine Surge`

Success condition:

- The player can survive, scale, and make build choices.

### Milestone C: Threat Layer

- Specialist enemy
- Elite enemy
- Corruption threshold 1 and 2

Success condition:

- The run is no longer just kiting fodder.

### Milestone D: Full MVP Run

- Boss
- Run end
- Summary

Success condition:

- One run has a real arc and ending.

## Explicit Deferrals

These should not block MVP.

- Meta progression hub
- Multiple playable characters
- Multiple biomes
- Complex inventory systems
- Narrative event chains
- Full animation sets
- Directional sprite sets
- Elaborate VFX polish
- Save/load beyond minimal settings or debug data

## Key Risks

## Risk 1: Readability collapse

Too many enemies plus painterly sprites can become mud.

Mitigation:

- limit enemy count while tuning
- keep enemy silhouettes distinct
- keep VFX restrained

## Risk 2: Content before systems

It is easy to start making enemies and upgrades before the run structure is stable.

Mitigation:

- finish spawn, XP, and upgrade loop before expanding roster

## Risk 3: Corruption too vague

If corruption is only a meter with no felt impact, it weakens the whole identity.

Mitigation:

- make the first three thresholds visibly change play

## Recommended Immediate Next Tasks

If building from the current repo state, the next implementation tasks should be:

1. Replace the current roaming mire setup with a real `SpawnDirector`.
2. Finalize player combat with a non-placeholder harpoon projectile prefab path.
3. Add XP drops and a simple level-up choice panel.
4. Implement `Brine Surge`.
5. Add one specialist enemy before touching the boss.
