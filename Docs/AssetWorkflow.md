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
