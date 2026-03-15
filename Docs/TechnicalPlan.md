# Dagon - Technical Plan

## Goal

Build a Unity 3D game that renders pixel-art sprites as billboarded actors in a true 3D world, while using a code-driven content pipeline for AI-assisted asset generation.

This document focuses on what to build first and how to keep the art pipeline reproducible.

## High-Level Recommendation

Use this split:

- Unity handles gameplay, rendering, import, animation playback, prefabs, and data.
- An external tooling layer handles pixel-art generation, prompt/version tracking, bitmap post-processing, and export into Unity-ready files.

Do not try to generate art inside the running game. That will complicate development, create dependency problems, and make asset iteration harder.

## Recommended Project Shape

### Unity Side

Recommended folders to add under `Assets/`:

- `Assets/Art/Sprites`
- `Assets/Art/Materials`
- `Assets/Art/Animations`
- `Assets/Prefabs`
- `Assets/Scenes`
- `Assets/Scripts/Core`
- `Assets/Scripts/Gameplay`
- `Assets/Scripts/Rendering`
- `Assets/Scripts/UI`
- `Assets/Data`

### External Tooling Side

Recommended repo folders outside Unity's asset tree:

- `Tools/AssetGen`
- `Tools/AssetGen/prompts`
- `Tools/AssetGen/generated`
- `Tools/AssetGen/processed`
- `Tools/AssetGen/manifests`

This keeps source prompts and generation metadata separate from imported game assets.

## Core Unity Technical Approach

### World Model

- Use a normal 3D Unity scene.
- Keep gameplay on a flat XZ plane.
- Use 3D transforms for movement, projectiles, spawning, and camera framing.
- Render characters and enemies with sprites on quads or `SpriteRenderer` objects that always face the camera.

### Camera

Recommended first implementation:

- Fixed isometric angle
- Perspective camera
- Narrow enough field of view to keep the scene readable
- Stable fixed framing with restrained follow behavior

Current project decision:

Use perspective, not orthographic. This better supports the desired 3D depth feel, but it increases the need for early readability testing.

### Billboard Sprites

Each actor should have:

- Root gameplay object in 3D world space
- Child visual object holding sprite renderer or quad
- Billboard script that aligns visuals to the camera

Important rule:

Do not put gameplay logic on the billboard visual child. Keep gameplay and rendering separated.

### Sorting / Depth

This is one of the first real technical risks.

Recommended approach:

- Use world position for gameplay
- Use sprite sorting layers for coarse categories
- Use y-offset or z-depth driven sorting only for visuals if needed
- Keep large enemies and props on deliberate sort groups

You will need to test crowd readability early. Dense bullet-heaven scenes expose sorting problems fast.

## Minimum Gameplay Systems

These are the first systems worth building.

### 1. Player Controller

- Isometric movement on XZ plane
- Input from the new Input System
- Optional dash later, not required for first pass

### 2. Camera Rig

- Smooth follow
- Fixed rotation
- Stable framing under swarm pressure

### 3. Billboard Presenter

- Makes actor visuals face the camera
- Supports flipping or directional sprite variants later

### 4. Enemy Spawn Director

- Spawns enemies in waves or pressure bands around the player
- Supports fodder, specialist, elite, and boss encounters

### 5. Weapon / Projectile System

- Auto-fire attacks
- Active ability cooldowns
- Projectile movement and impact handling

### 6. Health / Damage / Status

- Shared for player and enemies
- Handles effects like Soaked, Bleed, Corrupted

### 7. Upgrade System

- Level-up reward choices
- Data-driven upgrade definitions

### 8. Corruption System

- Run-level meter
- Threshold events
- Gameplay mutation hooks

## Data Architecture Recommendation

Use `ScriptableObject` assets for static game data.

Good candidates:

- Weapons
- Enemies
- Upgrades
- Status effects
- Biomes
- Loot / reward tables

This is much cleaner than hardcoding everything into monolithic scripts and will help once content expands.

## AI-Assisted Pixel Art Pipeline

## Recommendation

Use a hybrid pipeline:

1. Generate source art externally with code.
2. Save source outputs and metadata to disk.
3. Post-process outputs into clean limited-palette PNG files.
4. Copy or export final files into `Assets/Art/Sprites`.
5. Let Unity import them with consistent settings.

This gives you repeatability and version control instead of one-off asset generation.

## Why External Tooling Is Better

- Easier to script and automate
- Easier to swap providers or prompts
- Easier to keep prompt history and generation manifests
- Easier to batch-process images
- Unity stays focused on the game rather than image generation

## Best Asset Workflow

Each asset should have three stages:

### 1. Source Definition

Store a structured manifest for each asset:

- asset id
- asset type
- prompt
- negative prompt if used
- target size
- palette notes
- pose / orientation notes
- generation seed
- model or provider

JSON is fine for this.

### 2. Generated Source Image

Store raw generated outputs in `Tools/AssetGen/generated`.

Do not import these directly into Unity if they need cleanup.

### 3. Processed Game Asset

Run cleanup scripts to:

- crop
- remove background
- reduce palette
- resize to intended sprite size
- add transparent padding
- split sheets if needed

Then export the final PNG into Unity's art folder.

## Suggested Generation Strategy

AI image generation is usually weak at strict pixel-art consistency if left uncontrolled. To make it usable, constrain the problem.

Recommended strategy:

- Generate concept references first if needed
- Generate small isolated subjects on plain backgrounds
- Post-process aggressively
- Standardize palettes
- Standardize camera angle and pose framing

For actual in-game sprites, a coded post-processing step is important.

## What To Generate With AI

Best candidates:

- Character portraits
- Enemy concept sheets
- Prop concepts
- Tile or decal concepts
- Large boss concept art
- Atmospheric UI art

Possible but harder:

- Final in-game 32x32 sprites
- Consistent animation frames

For real production, a practical compromise is:

- AI for ideation and base frames
- code + cleanup for pixel conversion
- manual touch-up only when necessary

## Recommended Tooling Design

Build a small external generator app in either Python or Node.

Recommendation:

Use Python if the pipeline will do image cleanup and palette work.

Why:

- Strong image tooling
- Easy batch processing
- Good for manifests and file transforms

### Suggested Tool Responsibilities

- Read asset manifests
- Generate source images via provider API or local placeholders
- Save prompt and seed metadata
- Run post-processing
- Export final PNGs into Unity
- Optionally generate sprite sheets and import manifests

## Suggested Tool Output Example

For one enemy:

- `Tools/AssetGen/manifests/mire_wretch.json`
- `Tools/AssetGen/generated/mire_wretch_seed_01.png`
- `Tools/AssetGen/processed/mire_wretch.png`
- `Assets/Art/Sprites/Enemies/mire_wretch.png`

## Unity Import Rules For Pixel Art

Every sprite import should be consistent.

Recommended defaults:

- Texture Type: `Sprite (2D and UI)`
- Filter Mode: `Point (no filter)`
- Compression: disabled or minimal
- Mip Maps: off
- Pixels Per Unit: fixed project-wide value
- Mesh Type: Full Rect unless a tight mesh is useful

Choose one `Pixels Per Unit` standard early. A common approach is to map 32 pixels to 1 world unit or 0.5 world units, but the exact value depends on your desired scale.

## Animation Recommendation

Do not start with fully directional sprite sets.

For MVP:

- Idle
- Move
- Attack
- Hit
- Death

All using billboarded front-facing or 3/4-facing sprites.

Directional variants can come later if the visual payoff is worth the workload.

## Short-Term Build Order

This is the most practical technical sequence.

1. Create clean project folders.
2. Build a test scene with camera, ground plane, and one billboarded sprite actor.
3. Implement player movement in isometric space.
4. Implement one auto-attack and one enemy.
5. Verify readability, sorting, and scale.
6. Build the external asset-generation tool skeleton.
7. Define the first few asset manifests.
8. Import the first generated sprites and wire them into prefabs.

## Suggested First Technical Deliverables

Concrete near-term tasks:

- A `BillboardSprite` Unity component
- A `PlayerMover` component using the Input System
- A `FollowCameraRig`
- One simple enemy prefab
- One harpoon projectile
- A `Tools/AssetGen` scaffold with manifest format and export directories

## Biggest Risks

### Risk 1: Readability

Small sprites, heavy VFX, and swarm density can become unreadable fast.

Mitigation:

- Test with placeholder art early
- Keep silhouettes bold
- Limit noisy effects

### Risk 2: AI Asset Inconsistency

Generated assets may vary wildly in shape, palette, and style.

Mitigation:

- Use structured manifests
- Use fixed palette constraints
- Use the same framing language
- Post-process consistently

### Risk 3: Animation Cost

Pixel animation multiplies workload quickly.

Mitigation:

- Start with minimal states
- Reuse effects and overlays
- Prefer strong silhouettes over many frames

## Recommendation For You Right Now

Do not begin by trying to build the entire game.

Build one vertical slice:

- sailor sprite
- black mire ground
- isometric movement
- harpoon attack
- mire wretch enemy
- corruption meter stub

If that slice feels good, the rest of the project becomes much clearer.
