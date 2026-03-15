---
name: dagon-assetgen
description: Use this skill when generating, rendering, processing, or exporting Dagon art assets, especially sprite specs, asset manifests, placeholder PNGs, and Unity imports through Tools/AssetGen and Assets/Art/Sprites.
---

# Dagon AssetGen

## Overview

Use this skill for the Dagon art pipeline. The primary path is imported painterly sprites generated externally and tracked by manifest. Coded bitmap specs are only a fallback placeholder path.

## Workflow

1. Read [Docs/AssetWorkflow.md](/Users/jakeferrigan/Echo Rift/Docs/AssetWorkflow.md).
2. Decide whether the task is:
   - browser-generated sprite import from `Tools/AssetGen/inbox/`
   - manifest validation/export from `Tools/AssetGen/manifests/`
   - fallback placeholder art from `Tools/AssetGen/specs/`
   - pipeline extension in `Tools/AssetGen/pipeline.py`
3. Prefer imported painterly sprites for character and monster art.
4. Keep manifest ids and Unity output paths stable so prefab wiring does not churn.
5. Run the pipeline commands rather than describing them abstractly when the user wants assets generated.

## Default Commands

Validate manifests:

```bash
python3 Tools/AssetGen/pipeline.py validate
```

Import from inbox:

```bash
python3 Tools/AssetGen/pipeline.py import-inbox <manifest-name>.json <downloaded-file>.png
python3 Tools/AssetGen/pipeline.py import-inbox <manifest-name>.json <downloaded-file>.png --processed
```

Render a coded sprite spec:

```bash
python3 Tools/AssetGen/pipeline.py render-spec <spec-name>.json
```

Export to Unity:

```bash
python3 Tools/AssetGen/pipeline.py export <manifest-name>.json
```

## When To Read References

- Read `references/asset-rules.md` when creating or revising manifests/specs.

## Boundaries

- Do not assume browser-generated images will be production-ready animation sheets.
- Treat coded specs as fallback placeholders, not the primary art path.
- Favor transparent backgrounds, limited palettes, and bold silhouettes.
