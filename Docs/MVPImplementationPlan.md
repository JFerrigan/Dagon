# Dagon - MVP Implementation Plan

## Goal

Build one complete playable run in the black mire that proves:

- the sailor is fun to move
- billboarded sprites read well in a perspective 3D world
- swarm pressure works
- at least one enemy forces prioritization
- progression inside a run changes the player build in meaningful ways

This plan is intentionally narrower than the full game vision. It focuses on the minimum functional components needed to ship a defensible first MVP.

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
