# AssetGen

`AssetGen` is the external pipeline scaffold for imported painterly sprite generation and Unity export.

## Purpose

Keep prompts, reference art, imported browser outputs, processed sprite files, and Unity exports reproducible outside Unity.

## Layout

- `inbox/`: freshly downloaded browser-generated images waiting to be sorted
- `reference/`: inspiration and target-style images
- `prompts/`: optional prompt text files or references
- `generated/`: raw model outputs
- `processed/`: cleaned outputs ready for export
- `manifests/`: JSON files describing each asset
- `specs/`: coded bitmap definitions for deterministic fallback placeholders
- `pipeline.py`: manifest validator, inbox importer, fallback renderer, and export helper

## Actual Workflow

1. Define an asset manifest.
2. Generate or download source art externally.
3. Save browser outputs into `inbox/`.
4. Import the selected image from `inbox/` into the manifest's `source_image` path, usually under `generated/`.
5. Process them into gameplay-ready sprites in `processed/`.
6. Export the final PNG into the Unity path defined by the manifest.

For runtime-loaded sprites in this project, that usually means `Assets/Resources/Sprites/...`, because the game loads many sprite textures through `Resources.Load`.

Typical commands:

```bash
python3 Tools/AssetGen/pipeline.py import-inbox deep_spawn.json deepspawn.png
python3 Tools/AssetGen/pipeline.py process deep_spawn.json --padding 12 --resize-to 128
python3 Tools/AssetGen/pipeline.py export deep_spawn.json
```

For deterministic fallback prototype art, you can also:

1. Define a sprite bitmap spec in `specs/`.
2. Run `pipeline.py render-spec ...`.
3. Export or copy the resulting PNG into the Unity output path defined in the manifest.

## Notes

- This scaffold does not call a model provider directly.
- The default expectation is external browser image generation plus local import/export tracking.
- `inbox/` is the drop location for new downloaded images before they are assigned to a manifest.
- Keep manifest `output_path` values stable so prefab wiring and runtime asset paths do not churn.
