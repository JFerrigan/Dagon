#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import sys
from dataclasses import dataclass
from pathlib import Path
import struct
import zlib


ROOT = Path(__file__).resolve().parents[2]
MANIFEST_DIR = ROOT / "Tools" / "AssetGen" / "manifests"
SPEC_DIR = ROOT / "Tools" / "AssetGen" / "specs"
INBOX_DIR = ROOT / "Tools" / "AssetGen" / "inbox"


@dataclass
class AssetManifest:
    asset_id: str
    asset_type: str
    prompt: str
    output_path: str
    style: str | None = None
    source_image: str | None = None
    processed_image: str | None = None
    size: list[int] | None = None
    palette_notes: str | None = None
    orientation: str | None = None
    seed: int | None = None
    provider: str | None = None
    notes: str | None = None

    @classmethod
    def from_dict(cls, data: dict) -> "AssetManifest":
        required = ("asset_id", "asset_type", "prompt", "output_path")
        missing = [key for key in required if key not in data]
        if missing:
            raise ValueError(f"missing required keys: {', '.join(missing)}")

        return cls(
            asset_id=data["asset_id"],
            asset_type=data["asset_type"],
            prompt=data["prompt"],
            output_path=data["output_path"],
            style=data.get("style"),
            source_image=data.get("source_image"),
            processed_image=data.get("processed_image"),
            size=data.get("size"),
            palette_notes=data.get("palette_notes"),
            orientation=data.get("orientation"),
            seed=data.get("seed"),
            provider=data.get("provider"),
            notes=data.get("notes"),
        )


def load_manifest(path: Path) -> AssetManifest:
    with path.open("r", encoding="utf-8") as handle:
        data = json.load(handle)
    return AssetManifest.from_dict(data)


def iter_manifest_paths() -> list[Path]:
    return sorted(MANIFEST_DIR.glob("*.json"))


def validate_manifests() -> int:
    manifest_paths = iter_manifest_paths()
    if not manifest_paths:
        print("No manifests found.")
        return 0

    failures = 0
    for path in manifest_paths:
        try:
            manifest = load_manifest(path)
            print(f"OK  {path.relative_to(ROOT)} -> {manifest.asset_id}")
        except Exception as exc:  # pragma: no cover - CLI reporting path
            failures += 1
            print(f"ERR {path.relative_to(ROOT)} -> {exc}")

    return 1 if failures else 0


def export_asset(manifest_name: str) -> int:
    manifest_path = MANIFEST_DIR / manifest_name
    if not manifest_path.exists():
        print(f"Manifest not found: {manifest_path.relative_to(ROOT)}")
        return 1

    manifest = load_manifest(manifest_path)
    source_rel = manifest.processed_image or manifest.source_image
    if not source_rel:
        print(f"Manifest {manifest.asset_id} does not define source_image or processed_image.")
        return 1

    source_path = ROOT / source_rel
    output_path = ROOT / manifest.output_path

    if not source_path.exists():
        print(f"Source image not found: {source_path.relative_to(ROOT)}")
        return 1

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_bytes(source_path.read_bytes())
    print(f"Exported {manifest.asset_id} -> {output_path.relative_to(ROOT)}")
    return 0


def import_from_inbox(manifest_name: str, inbox_filename: str, mark_processed: bool) -> int:
    manifest_path = MANIFEST_DIR / manifest_name
    if not manifest_path.exists():
        print(f"Manifest not found: {manifest_path.relative_to(ROOT)}")
        return 1

    manifest = load_manifest(manifest_path)
    inbox_path = INBOX_DIR / inbox_filename
    if not inbox_path.exists():
        print(f"Inbox image not found: {inbox_path.relative_to(ROOT)}")
        return 1

    destination_rel = manifest.processed_image if mark_processed and manifest.processed_image else manifest.source_image
    if not destination_rel:
        print(f"Manifest {manifest.asset_id} does not define a destination image path for this import mode.")
        return 1

    destination_path = ROOT / destination_rel
    destination_path.parent.mkdir(parents=True, exist_ok=True)
    destination_path.write_bytes(inbox_path.read_bytes())
    print(f"Imported {inbox_path.relative_to(ROOT)} -> {destination_path.relative_to(ROOT)}")
    return 0


def parse_rgba(hex_value: str) -> tuple[int, int, int, int]:
    if len(hex_value) != 8:
        raise ValueError(f"palette color must be 8 hex chars, got {hex_value!r}")
    return tuple(int(hex_value[i : i + 2], 16) for i in range(0, 8, 2))


def png_chunk(tag: bytes, data: bytes) -> bytes:
    return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)


def write_rgba_png(path: Path, width: int, height: int, pixels: list[tuple[int, int, int, int]]) -> None:
    raw = bytearray()
    for y in range(height):
        raw.append(0)
        row_start = y * width
        for x in range(width):
            raw.extend(pixels[row_start + x])

    ihdr = struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0)
    payload = b"".join(
        [
            b"\x89PNG\r\n\x1a\n",
            png_chunk(b"IHDR", ihdr),
            png_chunk(b"IDAT", zlib.compress(bytes(raw), level=9)),
            png_chunk(b"IEND", b""),
        ]
    )
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(payload)


def paeth_predictor(a: int, b: int, c: int) -> int:
    p = a + b - c
    pa = abs(p - a)
    pb = abs(p - b)
    pc = abs(p - c)
    if pa <= pb and pa <= pc:
        return a
    if pb <= pc:
        return b
    return c


def read_rgba_png(path: Path) -> tuple[int, int, list[tuple[int, int, int, int]]]:
    data = path.read_bytes()
    if data[:8] != b"\x89PNG\r\n\x1a\n":
        raise ValueError(f"not a PNG file: {path}")

    offset = 8
    width = None
    height = None
    color_type = None
    bit_depth = None
    compressed = bytearray()

    while offset < len(data):
        length = struct.unpack(">I", data[offset : offset + 4])[0]
        offset += 4
        chunk_type = data[offset : offset + 4]
        offset += 4
        chunk_data = data[offset : offset + length]
        offset += length + 4

        if chunk_type == b"IHDR":
            width, height, bit_depth, color_type, compression, flt, interlace = struct.unpack(">IIBBBBB", chunk_data)
            if bit_depth != 8 or color_type not in (2, 6) or compression != 0 or flt != 0 or interlace != 0:
                raise ValueError(f"unsupported PNG format in {path.name}: bit_depth={bit_depth}, color_type={color_type}, interlace={interlace}")
        elif chunk_type == b"IDAT":
            compressed.extend(chunk_data)
        elif chunk_type == b"IEND":
            break

    if width is None or height is None:
        raise ValueError(f"missing IHDR in {path}")

    raw = zlib.decompress(bytes(compressed))
    channel_count = 4 if color_type == 6 else 3
    stride = width * channel_count
    pixels: list[tuple[int, int, int, int]] = []
    previous = bytearray(stride)
    cursor = 0

    for _ in range(height):
        filter_type = raw[cursor]
        cursor += 1
        row = bytearray(raw[cursor : cursor + stride])
        cursor += stride

        if filter_type == 1:
            for i in range(stride):
                left = row[i - channel_count] if i >= channel_count else 0
                row[i] = (row[i] + left) & 0xFF
        elif filter_type == 2:
            for i in range(stride):
                row[i] = (row[i] + previous[i]) & 0xFF
        elif filter_type == 3:
            for i in range(stride):
                left = row[i - channel_count] if i >= channel_count else 0
                up = previous[i]
                row[i] = (row[i] + ((left + up) // 2)) & 0xFF
        elif filter_type == 4:
            for i in range(stride):
                left = row[i - channel_count] if i >= channel_count else 0
                up = previous[i]
                up_left = previous[i - channel_count] if i >= channel_count else 0
                row[i] = (row[i] + paeth_predictor(left, up, up_left)) & 0xFF
        elif filter_type != 0:
            raise ValueError(f"unsupported PNG filter {filter_type} in {path.name}")

        previous = row
        if channel_count == 4:
            for i in range(0, stride, 4):
                pixels.append((row[i], row[i + 1], row[i + 2], row[i + 3]))
        else:
            for i in range(0, stride, 3):
                pixels.append((row[i], row[i + 1], row[i + 2], 255))

    return width, height, pixels


def crop_pixels_to_alpha(width: int, height: int, pixels: list[tuple[int, int, int, int]], alpha_threshold: int) -> tuple[int, int, list[tuple[int, int, int, int]]]:
    min_x = width
    min_y = height
    max_x = -1
    max_y = -1

    for y in range(height):
        row_start = y * width
        for x in range(width):
            if pixels[row_start + x][3] > alpha_threshold:
                min_x = min(min_x, x)
                min_y = min(min_y, y)
                max_x = max(max_x, x)
                max_y = max(max_y, y)

    if max_x < min_x or max_y < min_y:
        return width, height, pixels

    cropped_width = max_x - min_x + 1
    cropped_height = max_y - min_y + 1
    cropped_pixels: list[tuple[int, int, int, int]] = []
    for y in range(min_y, max_y + 1):
        row_start = y * width
        cropped_pixels.extend(pixels[row_start + min_x : row_start + max_x + 1])

    return cropped_width, cropped_height, cropped_pixels


def pad_pixels(width: int, height: int, pixels: list[tuple[int, int, int, int]], padding: int) -> tuple[int, int, list[tuple[int, int, int, int]]]:
    if padding <= 0:
        return width, height, pixels

    padded_width = width + (padding * 2)
    padded_height = height + (padding * 2)
    transparent = (0, 0, 0, 0)
    padded_pixels = [transparent] * (padded_width * padded_height)

    for y in range(height):
        source_start = y * width
        dest_start = (y + padding) * padded_width + padding
        padded_pixels[dest_start : dest_start + width] = pixels[source_start : source_start + width]

    return padded_width, padded_height, padded_pixels


def resize_pixels_nearest(width: int, height: int, pixels: list[tuple[int, int, int, int]], target_width: int, target_height: int) -> list[tuple[int, int, int, int]]:
    resized: list[tuple[int, int, int, int]] = []
    for y in range(target_height):
        source_y = min(height - 1, (y * height) // target_height)
        for x in range(target_width):
            source_x = min(width - 1, (x * width) // target_width)
            resized.append(pixels[source_y * width + source_x])
    return resized


def render_spec(spec_name: str, output_rel: str | None) -> int:
    spec_path = SPEC_DIR / spec_name
    if not spec_path.exists():
        print(f"Spec not found: {spec_path.relative_to(ROOT)}")
        return 1

    with spec_path.open("r", encoding="utf-8") as handle:
        spec = json.load(handle)

    width = int(spec["width"])
    height = int(spec["height"])
    scale = int(spec.get("scale", 1))
    palette = {key: parse_rgba(value) for key, value in spec["palette"].items()}
    rows = spec["rows"]

    if len(rows) != height:
        print(f"Spec {spec_name} height mismatch: expected {height}, got {len(rows)} rows")
        return 1

    base_pixels: list[tuple[int, int, int, int]] = []
    for row in rows:
        if len(row) != width:
            print(f"Spec {spec_name} width mismatch: expected {width}, got {len(row)}")
            return 1
        for symbol in row:
            if symbol not in palette:
                print(f"Spec {spec_name} uses missing palette symbol {symbol!r}")
                return 1
            base_pixels.append(palette[symbol])

    scaled_width = width * scale
    scaled_height = height * scale
    scaled_pixels: list[tuple[int, int, int, int]] = []
    for y in range(height):
        row = base_pixels[y * width : (y + 1) * width]
        scaled_row = []
        for pixel in row:
            scaled_row.extend([pixel] * scale)
        for _ in range(scale):
            scaled_pixels.extend(scaled_row)

    output_path = ROOT / output_rel if output_rel else ROOT / "Tools" / "AssetGen" / "processed" / f"{spec_path.stem}.png"
    write_rgba_png(output_path, scaled_width, scaled_height, scaled_pixels)
    print(f"Rendered {spec_path.relative_to(ROOT)} -> {output_path.relative_to(ROOT)}")
    return 0


def process_asset(manifest_name: str, crop_alpha: bool, alpha_threshold: int, padding: int, resize_to: int | None) -> int:
    manifest_path = MANIFEST_DIR / manifest_name
    if not manifest_path.exists():
        print(f"Manifest not found: {manifest_path.relative_to(ROOT)}")
        return 1

    manifest = load_manifest(manifest_path)
    if not manifest.source_image or not manifest.processed_image:
        print(f"Manifest {manifest.asset_id} must define source_image and processed_image.")
        return 1

    source_path = ROOT / manifest.source_image
    output_path = ROOT / manifest.processed_image
    if not source_path.exists():
        print(f"Source image not found: {source_path.relative_to(ROOT)}")
        return 1

    width, height, pixels = read_rgba_png(source_path)
    original_width, original_height = width, height

    if crop_alpha:
        width, height, pixels = crop_pixels_to_alpha(width, height, pixels, alpha_threshold)

    width, height, pixels = pad_pixels(width, height, pixels, padding)

    if resize_to is not None:
        pixels = resize_pixels_nearest(width, height, pixels, resize_to, resize_to)
        width = resize_to
        height = resize_to

    write_rgba_png(output_path, width, height, pixels)
    print(
        f"Processed {manifest.asset_id}: "
        f"{original_width}x{original_height} -> {width}x{height} "
        f"at {output_path.relative_to(ROOT)}"
    )
    return 0


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description="Validate and export asset manifests.")
    subparsers = parser.add_subparsers(dest="command", required=True)

    subparsers.add_parser("validate", help="Validate all manifests.")

    export_parser = subparsers.add_parser("export", help="Export one processed asset into Unity.")
    export_parser.add_argument("manifest_name", help="Manifest filename, e.g. sailor_idle_front.json")

    import_parser = subparsers.add_parser("import-inbox", help="Copy a browser-generated image from inbox into a manifest source or processed path.")
    import_parser.add_argument("manifest_name", help="Manifest filename, e.g. sailor_idle_front.json")
    import_parser.add_argument("inbox_filename", help="Filename inside Tools/AssetGen/inbox/")
    import_parser.add_argument(
        "--processed",
        action="store_true",
        help="Import into the manifest processed_image path instead of source_image",
    )

    render_parser = subparsers.add_parser("render-spec", help="Render a sprite PNG from a coded bitmap spec.")
    render_parser.add_argument("spec_name", help="Spec filename, e.g. sailor_idle_front.json")
    render_parser.add_argument("--output", dest="output_rel", help="Optional repo-relative output path")

    process_parser = subparsers.add_parser("process", help="Crop and optionally resize a source sprite into the processed manifest path.")
    process_parser.add_argument("manifest_name", help="Manifest filename, e.g. sailor_idle_front.json")
    process_parser.add_argument("--no-crop-alpha", action="store_true", help="Do not crop transparent bounds before writing output.")
    process_parser.add_argument("--alpha-threshold", type=int, default=0, help="Alpha threshold for crop detection. Default: 0")
    process_parser.add_argument("--padding", type=int, default=0, help="Transparent padding to add after crop. Default: 0")
    process_parser.add_argument("--resize-to", type=int, help="Optional square output size in pixels, e.g. 128")

    args = parser.parse_args(argv)

    if args.command == "validate":
        return validate_manifests()

    if args.command == "export":
        return export_asset(args.manifest_name)

    if args.command == "import-inbox":
        return import_from_inbox(args.manifest_name, args.inbox_filename, args.processed)

    if args.command == "render-spec":
        return render_spec(args.spec_name, args.output_rel)

    if args.command == "process":
        return process_asset(
            args.manifest_name,
            crop_alpha=not args.no_crop_alpha,
            alpha_threshold=args.alpha_threshold,
            padding=args.padding,
            resize_to=args.resize_to,
        )

    parser.error("unknown command")
    return 2


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
