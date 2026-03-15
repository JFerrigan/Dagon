# Dagon - Next Implementation Phase

## Phase Goal

Move from a fast runtime prototype to a stable MVP-alpha foundation.

Right now the project has:

- player movement
- auto-fire combat
- active skill
- enemy spawning
- fodder, specialist, elite, and boss layers
- XP, upgrades, corruption, and run end flow

The next big phase is not "add lots more features." It is:

1. replace remaining placeholder content
2. convert core runtime objects into reusable prefabs/data
3. stabilize tuning and readability
4. remove prototype-only brittleness

## Phase Name

Recommended label:

`MVP Alpha Hardening`

## Primary Outcomes

By the end of this phase, the project should have:

- real art for all core enemy roles
- more stable actor setup using prefabs
- cleaner data-driven content configuration
- less runtime-only scene construction
- better combat readability and balance
- a boss encounter that feels deliberate rather than provisional

## Workstreams

## 1. Content Replacement

This is the highest-visibility work.

### Replace placeholder visuals for:

- `Drowned Acolyte`
- `Deep Spawn`
- `Mire Colossus`
- `XP pickup`
- `Brine Surge` effect

### Optional high-value replacements:

- additional mire props
- HUD icons
- better ground dressing sprites

### Acceptance

- Every core enemy role has distinct art.
- The boss no longer feels like a code-first placeholder.
- Combat role recognition is faster.

## 2. Prefab Conversion

Right now major actors are still created procedurally. That was correct for speed, but it should not remain the long-term structure.

### Convert these into prefabs:

- Player
- Harpoon projectile
- Orb projectile
- Mire Wretch
- Drowned Acolyte
- Deep Spawn
- Mire Colossus
- XP pickup

### Keep runtime bootstrap only for:

- debug scene assembly
- fallback test scene creation
- quick prototyping hooks

### Acceptance

- Core actors can be edited and tuned without rewriting bootstrap code.
- Runtime code references prefab assets or spawn definitions instead of hand-building everything.

## 3. Data Layer Expansion

The project already has the start of a data layer. This phase should make it real.

### Expand data definitions for:

- enemy stats
- enemy spawn weights
- boss stats
- upgrade pools
- corruption thresholds
- run pacing

### Concrete result

- fewer hardcoded values in bootstrap/runtime scripts
- easier balancing without code edits

### Acceptance

- At least spawn pacing, enemy health/damage, and upgrade values can be changed mostly in data.

## 4. Run Tuning

The core loop exists, but it now needs tuning.

### Tune:

- early-game spawn pressure
- elite frequency
- boss timer
- XP gain rate
- upgrade pacing
- corruption gain rate
- active skill cooldown

### Acceptance

- the run has a clear early, mid, and late game
- boss arrival feels earned rather than arbitrary
- upgrades happen often enough to feel like a bullet-heaven run

## 5. Readability Pass

Painterly sprites are good for tone but risky for gameplay clarity.

### Focus areas:

- projectile visibility
- enemy silhouette distinction
- boss readability under crowd pressure
- UI legibility
- prop density versus gameplay clarity

### Acceptance

- the player can identify threats quickly
- props improve atmosphere without obscuring gameplay
- projectiles remain legible at combat speed

## 6. Scene Transition

You do not need a full authored production scene yet, but you should start reducing reliance on one giant runtime bootstrap.

### Recommended approach:

Phase A:

- keep `PrototypeSceneBootstrap` as the fast test scene path

Phase B:

- create one authored gameplay scene
- place ground, lighting, camera rig, and prop anchors in-editor
- let runtime systems handle only dynamic spawn/run logic

### Acceptance

- static world composition is no longer fully code-built
- runtime code focuses on gameplay, not map assembly

## Recommended Build Order

1. Swap in real `Drowned Acolyte` art
2. Swap in real `Deep Spawn` art
3. Replace boss visual and tune boss scale/behavior
4. Add real XP pickup art and Brine Surge VFX
5. Convert projectiles and enemies into prefabs
6. Move spawn stats and pacing to data assets
7. Tune run pacing and corruption
8. Build an authored gameplay scene

## Biggest Decision

The main decision for this phase is:

`Do we keep prioritizing prototype speed, or do we start investing in structure?`

Recommendation:

Start investing in structure now.

Not with a giant rewrite. Just enough to:

- stop hardcoding actor assembly
- stop tying content changes to code changes
- make balancing faster

That is the right move before adding more enemy families, additional abilities, or meta progression.

## Recommended Immediate Next Task

The best next concrete step is:

`Replace the Drowned Acolyte placeholder with the real sprite and use that as the start of the prefab/content replacement pass.`

Why:

- it improves visual clarity immediately
- it is low-risk
- it starts the transition away from color-tinted placeholder enemies
- it gives a clean path into prefab work
