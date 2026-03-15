# Dagon - Project Status / Context Handoff

## Purpose

Read this file first after a context reset.

It summarizes:

- what the game is
- what has been implemented
- what is currently using real art versus placeholder art
- what still needs work
- what the next recommended steps are

## Project Identity

- Title: `Dagon`
- Genre: isometric-style bullet heaven
- Rendering: 2D billboard sprites in a true 3D world
- Camera: fixed perspective, narrow FOV
- Tone: maritime Lovecraftian horror, black mire, corruption, rot, sea-floor nightmare
- Protagonist: sailor with harpoon

## Current Design Anchors

- opening biome is the black mire
- player starts as a stranded sailor
- core weapon is `Harpoon Cast`
- active skill is `Brine Surge`
- corruption is a central mechanic, not just flavor
- core enemy layers are:
  - `Mire Wretch` fodder
  - `Drowned Acolyte` ranged specialist
  - `Deep Spawn` elite bruiser
  - `Mire Colossus` boss

## Current Repo Docs

These are the main planning docs:

- [Game plan](/Users/jakeferrigan/Echo Rift/Docs/GamePlan.md)
- [Technical plan](/Users/jakeferrigan/Echo Rift/Docs/TechnicalPlan.md)
- [Prototype setup](/Users/jakeferrigan/Echo Rift/Docs/PrototypeSetup.md)
- [Asset workflow](/Users/jakeferrigan/Echo Rift/Docs/AssetWorkflow.md)
- [MVP implementation plan](/Users/jakeferrigan/Echo Rift/Docs/MVPImplementationPlan.md)
- [Next implementation phase](/Users/jakeferrigan/Echo Rift/Docs/NextImplementationPhase.md)

This file is the condensed handoff. The others contain more detail.

## Current Git Setup

A Unity-friendly `.gitignore` has been added at:

- [/.gitignore](/Users/jakeferrigan/Echo Rift/.gitignore)

Before committing from Unity, editor settings should be:

- `Version Control: Visible Meta Files`
- `Asset Serialization: Force Text`

Generated directories like `Library/`, `Temp/`, `Logs/`, and `UserSettings/` should not be committed.

## Current Runtime State

The project currently relies on runtime-built stage scenes.

Primary bootstrap:

- [PrototypeSceneBootstrap](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Bootstrap/PrototypeSceneBootstrap.cs)

Current scene entry points:

- [BlackMire.unity](/Users/jakeferrigan/Echo Rift/Assets/Scenes/BlackMire.unity)
- [MireColossusBoss.unity](/Users/jakeferrigan/Echo Rift/Assets/Scenes/MireColossusBoss.unity)
- [SampleScene.unity](/Users/jakeferrigan/Echo Rift/Assets/Scenes/SampleScene.unity) still works as a legacy prototype entry scene

What happens on Play in the gameplay stages:

- creates the player
- creates the camera and follow rig
- creates the black mire ground
- scatters harpoon props
- starts the spawn director
- starts HUD and run-state logic

This is fast for iteration, but still prototype architecture.

## What Is Implemented

## Core

- movement on XZ plane
- perspective camera follow
- billboard sprite rendering
- health/damage model
- corruption meter

Key scripts:

- [PlayerMover](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Gameplay/PlayerMover.cs)
- [FollowCameraRig](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Core/FollowCameraRig.cs)
- [BillboardSprite](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Rendering/BillboardSprite.cs)
- [Health](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Core/Health.cs)
- [CorruptionMeter](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Core/CorruptionMeter.cs)

## Combat

- auto-fire harpoon weapon
- runtime projectile factory
- `Brine Surge` active skill
- contact damage

Key scripts:

- [HarpoonLauncher](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Gameplay/HarpoonLauncher.cs)
- [HarpoonProjectile](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Gameplay/HarpoonProjectile.cs)
- [RuntimeHarpoonProjectileFactory](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Gameplay/RuntimeHarpoonProjectileFactory.cs)
- [ProjectileBillboardVisual](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Rendering/ProjectileBillboardVisual.cs)
- [BrineSurgeAbility](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Gameplay/BrineSurgeAbility.cs)

## Enemies

Implemented roles:

- `Mire Wretch`
- `Drowned Acolyte`
- `Deep Spawn`
- `Mire Colossus`

Key scripts:

- [SpawnDirector](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Gameplay/SpawnDirector.cs)
- [MireWanderer](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Gameplay/MireWanderer.cs)
- [DrownedAcolyteShooter](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Gameplay/DrownedAcolyteShooter.cs)
- [DeepSpawnBruiser](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Gameplay/DeepSpawnBruiser.cs)
- [MireColossusController](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Gameplay/MireColossusController.cs)

## Progression

- XP rewards
- visible XP pickup objects
- level-ups
- upgrade choice UI
- corruption threshold effects

Key scripts:

- [ExperienceController](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Gameplay/ExperienceController.cs)
- [ExperiencePickup](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Gameplay/ExperiencePickup.cs)
- [ExperienceHud](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Gameplay/ExperienceHud.cs)
- [EnemyDeathRewards](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Gameplay/EnemyDeathRewards.cs)
- [CorruptionRuntimeEffects](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Gameplay/CorruptionRuntimeEffects.cs)

## Run Flow

- run timer
- boss countdown
- boss spawn
- win/lose summary
- restart on `R`

Key script:

- [RunStateManager](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Gameplay/RunStateManager.cs)

## Props / Dressing

- black mire plane
- scattered harpoon ground props

Key script:

- [MirePropScatterer](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Bootstrap/MirePropScatterer.cs)

## Asset Pipeline

Asset tool root:

- [Tools/AssetGen](/Users/jakeferrigan/Echo Rift/Tools/AssetGen)

Current pipeline supports:

- browser-generated image inbox
- generated / processed / manifest workflow
- crop/pad/resize processing
- export into Unity paths
- fallback bitmap-spec rendering

Key file:

- [pipeline.py](/Users/jakeferrigan/Echo Rift/Tools/AssetGen/pipeline.py)

## Current Runtime Art

These assets are in active runtime use:

- sailor: [sailor_idle_front.png](/Users/jakeferrigan/Echo Rift/Assets/Resources/Sprites/Characters/sailor_idle_front.png)
- mire wretch: [mire_wretch.png](/Users/jakeferrigan/Echo Rift/Assets/Resources/Sprites/Enemies/mire_wretch.png)
- drowned acolyte: [drowned_acolyte.png](/Users/jakeferrigan/Echo Rift/Assets/Resources/Sprites/Enemies/drowned_acolyte.png)
- boss: [mire_colossus.png](/Users/jakeferrigan/Echo Rift/Assets/Resources/Sprites/Bosses/mire_colossus.png)
- deep spawn: [deep_spawn.png](/Users/jakeferrigan/Echo Rift/Assets/Resources/Sprites/Enemies/deep_spawn.png)
- harpoon projectile: [harpoon_projectile.png](/Users/jakeferrigan/Echo Rift/Assets/Resources/Sprites/Weapons/harpoon_projectile.png)
- ground prop: [harpoon_ground_prop.png](/Users/jakeferrigan/Echo Rift/Assets/Resources/Sprites/Props/harpoon_ground_prop.png)

## Important Asset Notes

Canonical harpoon projectile manifest:

- [harpoon_projectile.json](/Users/jakeferrigan/Echo Rift/Tools/AssetGen/manifests/harpoon_projectile.json)

Important note recorded there:

- the right side of the projectile sprite is the tip
- projectile art should be compact and horizontally oriented
- projectile sprites need relatively few pixels so they stay readable when scaled down in runtime

## What Is Still Placeholder Or Incomplete

## Art Gaps

Still missing or placeholder:

- `XP pickup` uses a temporary reused visual
- no dedicated HUD icons yet
- no additional black mire prop set beyond the harpoon ground prop

## Current Structure Progress

- `Deep Spawn` now has a dedicated runtime sprite and the first enemy prefab resource at [DeepSpawn.prefab](/Users/jakeferrigan/Echo Rift/Assets/Resources/Prefabs/Enemies/DeepSpawn.prefab)
- `SpawnDirector` still runtime-builds most enemies, but elites now have an initial prefab-backed spawn path

## Structure Gaps

Still prototype-heavy:

- enemies are still runtime-built, not prefab-driven
- player is still runtime-built
- projectiles are still runtime-built
- scene composition is mostly code-driven
- balance values are still heavily hardcoded in scripts

## Tuning Gaps

Needs iteration:

- enemy spawn pacing
- elite frequency
- boss timer and boss behavior difficulty
- XP gain rate
- corruption pacing
- UI readability under longer runs
- relative visual scale of enemies and props

## Important Caveat

Unity compile/runtime validation has been done by the user in-editor, not by automated testing from this environment.

This means:

- the prototype is working enough to iterate on
- but every new code slice should still be tested in the editor after changes

## Recommended Next Major Phase

The recommended next phase is:

`MVP Alpha Hardening`

Meaning:

- replace remaining placeholder visuals
- convert core actors to prefabs
- move spawn/tuning values into data
- build a more authored gameplay scene
- reduce runtime bootstrap complexity

See:

- [Next implementation phase](/Users/jakeferrigan/Echo Rift/Docs/NextImplementationPhase.md)

## Recommended Immediate Next Tasks

In priority order:

1. Replace `Deep Spawn` placeholder art with real art.
2. Add dedicated XP pickup art.
3. Add dedicated `Brine Surge` VFX art.
4. Convert enemy and projectile setup toward prefabs.
5. Move spawn pacing and core enemy stats into data assets.
6. Build one authored gameplay scene instead of relying entirely on runtime construction.

## If Starting Fresh After Context Reset

Start from this sequence:

1. Read this file.
2. Read [Docs/NextImplementationPhase.md](/Users/jakeferrigan/Echo Rift/Docs/NextImplementationPhase.md).
3. Check [PrototypeSceneBootstrap](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Bootstrap/PrototypeSceneBootstrap.cs) to understand how the current scene is assembled.
4. Check [SpawnDirector](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Gameplay/SpawnDirector.cs) and [RunStateManager](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Gameplay/RunStateManager.cs) to understand the current run loop.
5. Test in Unity before making broad architectural changes.
