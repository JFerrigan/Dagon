# Dagon Asset Workflow

This project now supports two art paths:

1. Imported painterly sprites generated externally, tracked by manifest
2. Coded bitmap specs as a fallback-only placeholder path

## Primary Path: Imported Painterly Sprites

The expected workflow is:

1. Generate sprite candidates in ChatGPT or another external tool.
2. Download the images into `Tools/AssetGen/inbox/`.
3. Keep inspiration or target references in `Tools/AssetGen/reference/`.
4. Copy selected images into `generated/` or `processed/` through the manifest.
5. Export the chosen processed image into Unity.

This keeps browser-generated art reproducible without making Unity itself responsible for generation.

## Reusable GPT Prompt Template

Use this as the default paste-ready prompt when generating new Black Mire assets in ChatGPT or a similar tool.

```text
I’m generating game art for a dark maritime horror bullet-heaven called Dagon.

Create a [ASSET TYPE] for the Black Mire biome with this theme:
- tone: rotting maritime Lovecraftian horror
- setting: black marsh, drowned ruins, sea rot, swamp fog, old fishing debris
- mood: eerie, corrupted, wet, ancient, ominous
- visual direction: painterly, readable at gameplay scale, with a 16-bit inspired visual language
- style target: retro 16-bit-era readability with clean shape design, but still atmospheric and painterly
- silhouette: clear and distinct, easy to identify instantly in combat
- palette: sickly greens, swamp blacks, muddy browns, pale bone, algae teal, muted decay tones
- avoid: bright heroic fantasy colors, clean sci-fi shapes, ornate high-fantasy decoration, overly busy details, blurry forms

Asset target:
- asset name: [ASSET NAME]
- asset role: [ENEMY / WEAPON / PROP / EFFECT / HUD ICON / BOSS]
- gameplay purpose: [WHAT THE PLAYER SHOULD READ IMMEDIATELY]
- shape language: [JAGGED / HEAVY / SPINDLY / ORGANIC / ROTTED / BARBED / ETC]
- size impression: [SMALL / MEDIUM / LARGE / MASSIVE]
- facing/framing: [SIDE / 3-QUARTER / TOP-DOWN-ISH / ISOLATED ON TRANSPARENT OR PLAIN BACKGROUND]
- composition: centered, clean outer silhouette, minimal wasted space
- important features to emphasize: [LIST 3-5]
- important features to avoid: [LIST 3-5]

Technical constraints:
- preserve strong readability at small in-game size
- no perspective-heavy background scene
- subject only, or subject with minimal grounded base if needed
- keep edges clean for sprite extraction
- do not distort proportions just to fill space
- preserve natural aspect ratio
- output should work well as a billboarded sprite in a 3D world

Return:
1. a short art direction summary
2. 3 prompt variants:
   - safe/readable gameplay version
   - moodier horror version
   - exaggerated iconic version
3. a short negative prompt
```

Default intent is `16-bit inspired painterly gameplay art`, not strict low-resolution sprite-sheet output. If a specific asset needs hard `16-bit pixel art` constraints, request that explicitly in the prompt instead of assuming it here.

## Ground Tile Prompting Notes

Current ground-tile manifests use these conventions:

- `asset_type`: `ground_tile`
- `orientation`: `top_down_tile`
- `size`: `256 x 256`
- seamless edges are required
- square composition
- no strong central subject
- low-to-medium contrast so tiles repeat cleanly in gameplay

For biome tile generation prompts, explicitly include:

- `256x256`
- `top-down`
- `seamless`
- `square composition`
- `readable from gameplay camera height`

Do not rely on the general asset template alone for tiles. Tile prompts should directly describe the image to generate and should not ask the model to return prompt variants or summaries when the goal is immediate image generation.

## Reference-Image Animation Prompt Template

Use this when you already have a base enemy sprite and want ChatGPT to generate additional action frames that stay on-model.

Best practice is to generate `one frame at a time`, not a full sprite sheet first. That gives much better control over consistency, framing, and pose readability.

```text
I am providing a base reference image of an in-game enemy sprite for a dark maritime horror bullet-heaven.

Use the provided image as the strict visual source of truth for:
- character identity
- proportions
- silhouette
- costume/body features
- rendering style
- palette family
- sprite framing

Generate a new sprite frame of the SAME character in a different action pose.

Requirements:
- preserve the exact character design from the reference
- preserve natural aspect ratio
- do not stretch, squash, skew, or redesign the character
- keep the sprite centered and isolated
- no background scene
- no environment props unless explicitly requested
- maintain strong gameplay readability at small size
- 16-bit inspired sprite art look
- dark maritime Lovecraftian horror tone
- painterly-retro hybrid rendering is fine, but keep forms clean and readable
- output should work as a billboarded sprite in a 3D game
- keep consistent framing and scale relative to the base image
- do not crop off important limbs or silhouette features
- transparent or plain empty background preferred

Action target:
- animation set: [IDLE / MOVE / WINDUP / ATTACK / RECOVER / HURT / DEATH]
- specific frame goal: [DESCRIBE THE EXACT MOMENT]
- motion direction: [SUBTLE / LEFT-LEAN / RIGHT-LEAN / FORWARD / RECOIL / TWIST]
- intensity: [LOW / MEDIUM / HIGH]
- silhouette priority: [WHAT MUST STILL READ CLEARLY]

Keep continuity with the source image:
- same character, not a reinterpretation
- same head/body proportion
- same weapon/appendage placement unless changed by the action
- same visual scale and camera framing
- same mood and material rendering

Avoid:
- redesigning the face or body
- adding extra limbs, accessories, or costume details
- dramatic perspective distortion
- blurry motion-smear
- background art
- exaggerated posing that breaks in-game readability

Return:
- one clean sprite frame only
```

### Short Action Variants

`Idle`

```text
Using the provided base sprite as strict reference, generate an idle frame of the same character. Keep the pose calm, readable, centered, and gameplay-clean. Add only subtle life: slight sway, breathing, cloth drift, or eerie tension. Preserve exact proportions, silhouette, scale, palette, and aspect ratio. No redesign, no background, 16-bit inspired dark maritime horror sprite.
```

`Move`

```text
Using the provided base sprite as strict reference, generate a movement frame of the same character. Show a clear locomotion step or glide pose while preserving exact design, proportions, scale, framing, and silhouette readability. Keep it centered, isolated, no background, no perspective distortion, 16-bit inspired dark maritime horror sprite.
```

`Windup`

```text
Using the provided base sprite as strict reference, generate a windup frame of the same character preparing an attack. The pose should clearly communicate anticipation and intent, not impact. Preserve exact character design, proportions, framing, scale, silhouette, and aspect ratio. No background. Strong gameplay readability. 16-bit inspired dark maritime horror sprite.
```

`Attack`

```text
Using the provided base sprite as strict reference, generate an attack frame of the same character at the moment of release. The pose should clearly show the action peak while staying readable at small gameplay scale. Preserve exact design, proportions, framing, and aspect ratio. No redesign, no background, 16-bit inspired dark maritime horror sprite.
```

`Recover`

```text
Using the provided base sprite as strict reference, generate a recovery frame of the same character immediately after an attack. Show recoil, follow-through, or brief vulnerability while preserving exact design, proportions, framing, scale, and silhouette. No background, no extra effects, 16-bit inspired dark maritime horror sprite.
```

`Hurt`

```text
Using the provided base sprite as strict reference, generate a hurt reaction frame of the same character. Show a brief hit reaction without deforming or redesigning the body. Preserve exact design, proportions, scale, framing, and aspect ratio. Keep the silhouette readable and centered. No background. 16-bit inspired dark maritime horror sprite.
```

`Death`

```text
Using the provided base sprite as strict reference, generate a death frame of the same character. Show collapse, unraveling, or corruption while preserving the core design language of the source sprite. Keep framing consistent, preserve aspect ratio, and avoid excessive gore or chaotic detail that hurts readability. No background. 16-bit inspired dark maritime horror sprite.
```

### Recommended Animation Set Structure

For enemy frame generation, default to small sets like:

- `idle`
- `move`
- `windup`
- `attack`
- `recover`
- optional `hurt`
- optional `death`

Example naming:

- `acolyte_move_01`
- `acolyte_move_02`
- `acolyte_windup_01`
- `acolyte_attack_01`

Keep every frame on the same apparent scale, pivot, framing, and aspect ratio so flipbook playback does not wobble.

## Commands

Validate manifests:

```bash
python3 Tools/AssetGen/pipeline.py validate
```

Import a downloaded browser image from `inbox/` into a manifest path:

```bash
python3 Tools/AssetGen/pipeline.py import-inbox sailor_idle_front.json sailor_candidate_01.png
python3 Tools/AssetGen/pipeline.py import-inbox mire_wretch.json mire_candidate_02.png --processed
```

Process a source image into a gameplay-ready processed sprite:

```bash
python3 Tools/AssetGen/pipeline.py process sailor_idle_front.json --padding 12 --resize-to 128
python3 Tools/AssetGen/pipeline.py process mire_wretch.json --padding 12 --resize-to 128
```

Export a processed image into Unity using a manifest:

```bash
python3 Tools/AssetGen/pipeline.py export sailor_idle_front.json
python3 Tools/AssetGen/pipeline.py export mire_wretch.json
```

## Fallback Path: Coded Specs

Use this only when you need fast deterministic placeholders.

Render a placeholder sprite from a coded spec:

```bash
python3 Tools/AssetGen/pipeline.py render-spec sailor_idle_front.json
python3 Tools/AssetGen/pipeline.py render-spec mire_wretch.json
```

## Recommended Near-Term Workflow

1. Keep one manifest per production-facing asset.
2. Generate several painterly sprite candidates in the browser.
3. Save them into `Tools/AssetGen/inbox/`.
4. Import the chosen file into `generated/`.
5. Run `process` to crop and size it into `processed/`.
6. Export the processed version into Unity.
6. Keep manifest ids and Unity output paths stable so prefab wiring does not churn.

For projectile assets, record directionality explicitly in the manifest notes.

Example:

- `right side of sprite is the tip`
- `horizontal side view`
- `compact footprint for small runtime scaling`

## Important Constraint

Do not rely on browser image generation to produce production-ready animation sheets consistently at the start.

Use AI first for:

- portraits
- single-character sprite poses
- monster key art
- props
- atmosphere references

Use coded specs only when you need an immediate gameplay placeholder.
