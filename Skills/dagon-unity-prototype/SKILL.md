---
name: dagon-unity-prototype
description: Use this skill when working on the Dagon Unity vertical slice in this repository, especially for perspective-camera prototype work, billboarded sprites in a 3D world, scene wiring, prefabs, or gameplay scaffolding under Assets/ and Docs/PrototypeSetup.md.
---

# Dagon Unity Prototype

## Overview

Use this skill for implementation tasks in the Unity project. It keeps work aligned to the current prototype direction: perspective camera, true 3D gameplay plane, and 2D billboard sprites.

## Workflow

1. Read [Docs/PrototypeSetup.md](/Users/jakeferrigan/Echo Rift/Docs/PrototypeSetup.md) if the task touches scene setup or prefab wiring.
2. Check the relevant runtime scripts under `Assets/Scripts/`.
3. Preserve the current prototype assumptions:
   - perspective camera
   - gameplay on the XZ plane
   - billboarded world-space sprites
   - one-slice-first scope
4. Prefer extending existing components before introducing new systems.
5. If a scene edit is requested, avoid fragile YAML surgery unless there is no cleaner option.

## Current Prototype Components

- `Assets/Scripts/Core/FollowCameraRig.cs`
- `Assets/Scripts/Rendering/BillboardSprite.cs`
- `Assets/Scripts/Gameplay/PlayerMover.cs`
- `Assets/Scripts/Gameplay/HarpoonLauncher.cs`
- `Assets/Scripts/Gameplay/HarpoonProjectile.cs`
- `Assets/Scripts/Gameplay/SimpleEnemyChaser.cs`
- `Assets/Scripts/Core/Health.cs`
- `Assets/Scripts/Core/CorruptionMeter.cs`

## When To Read References

- Read `references/prototype-notes.md` for constraints and preferred next steps.

## Boundaries

- Do not expand into a full production architecture unless the user asks.
- Default to placeholder art and simple prefabs until feel and readability are proven.
- If a technical decision conflicts with pixel readability, prefer readability.
