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

## Intended Workflow

1. Define an asset manifest.
2. Generate or download source art externally.
3. Save browser outputs into `inbox/`.
4. Copy selected images into `generated/`.
5. Process them into gameplay-ready sprites in `processed/`.
6. Export the final PNG into Unity's `Assets/Art/Sprites/` tree.

For deterministic fallback prototype art, you can also:

1. Define a sprite bitmap spec in `specs/`.
2. Run `pipeline.py render-spec ...`.
3. Export or copy the resulting PNG into Unity.

## Notes

- This scaffold does not call a model provider directly.
- The default expectation is external browser image generation plus local import/export tracking.
