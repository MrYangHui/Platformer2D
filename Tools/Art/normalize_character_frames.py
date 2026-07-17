from __future__ import annotations

import argparse
import json
import math
from dataclasses import dataclass
from pathlib import Path
from typing import Any

from PIL import Image, ImageDraw


MAX_HEAD_PELVIS_ERROR = 0.03


@dataclass(frozen=True)
class FrameResult:
    name: str
    image: Image.Image
    sole_anchor: tuple[int, int]
    pelvis_anchor: tuple[int, int]
    head_anchor: tuple[int, int]


@dataclass(frozen=True)
class NormalizationResult:
    atlas: Image.Image
    frames: dict[str, FrameResult]


def _pair(value: Any, label: str) -> tuple[float, float]:
    if not isinstance(value, list) or len(value) != 2:
        raise ValueError(f"{label} must contain exactly two numbers")
    try:
        return float(value[0]), float(value[1])
    except (TypeError, ValueError) as exception:
        raise ValueError(f"{label} must contain exactly two numbers") from exception


def _integer_pair(value: Any, label: str) -> tuple[int, int]:
    first, second = _pair(value, label)
    if not first.is_integer() or not second.is_integer():
        raise ValueError(f"{label} must contain integers")
    return int(first), int(second)


def _resolve_path(manifest_path: Path, raw_path: Any, label: str) -> Path:
    if not isinstance(raw_path, str) or not raw_path:
        raise ValueError(f"{label} must be a non-empty path")
    path = Path(raw_path)
    return path if path.is_absolute() else manifest_path.parent / path


def _load_source(
    manifest_path: Path,
    source_name: str,
    source_data: Any,
) -> tuple[Image.Image, tuple[int, int], int, float | None, bool]:
    if not isinstance(source_data, dict):
        raise ValueError(f"Source '{source_name}' must be an object")
    path = _resolve_path(manifest_path, source_data.get("path"), f"Source '{source_name}' path")
    if not path.is_file():
        raise ValueError(f"Source '{source_name}' does not exist: {path}")
    with Image.open(path) as opened:
        if "A" not in opened.getbands():
            raise ValueError(f"Source '{source_name}' is missing alpha")
        image = opened.convert("RGBA")
    cell_size = _integer_pair(source_data.get("cell_size"), f"Source '{source_name}' cell_size")
    columns = source_data.get("columns")
    if not isinstance(columns, int) or columns <= 0:
        raise ValueError(f"Source '{source_name}' columns must be a positive integer")
    if image.width % cell_size[0] != 0 or image.height % cell_size[1] != 0:
        raise ValueError(f"Source '{source_name}' dimensions do not match its cell_size")
    if image.width // cell_size[0] != columns:
        raise ValueError(f"Source '{source_name}' columns do not match image width")
    expected = source_data.get("expected_head_pelvis")
    if expected is not None and (not isinstance(expected, (int, float)) or expected <= 0):
        raise ValueError(f"Source '{source_name}' expected_head_pelvis must be positive")
    keep_largest = source_data.get("keep_largest_component", False)
    if not isinstance(keep_largest, bool):
        raise ValueError(f"Source '{source_name}' keep_largest_component must be boolean")
    return image, cell_size, columns, float(expected) if expected is not None else None, keep_largest


def _keep_largest_alpha_component(image: Image.Image) -> Image.Image:
    width, height = image.size
    alpha = image.getchannel("A").tobytes()
    visited = bytearray(width * height)
    largest: list[int] = []
    for start, value in enumerate(alpha):
        if value == 0 or visited[start]:
            continue
        component: list[int] = []
        stack = [start]
        visited[start] = 1
        while stack:
            index = stack.pop()
            component.append(index)
            x = index % width
            y = index // width
            for next_y in range(max(0, y - 1), min(height, y + 2)):
                row_start = next_y * width
                for next_x in range(max(0, x - 1), min(width, x + 2)):
                    neighbor = row_start + next_x
                    if not visited[neighbor] and alpha[neighbor] != 0:
                        visited[neighbor] = 1
                        stack.append(neighbor)
        if len(component) > len(largest):
            largest = component
    if not largest:
        return image
    filtered_alpha = bytearray(width * height)
    for index in largest:
        filtered_alpha[index] = alpha[index]
    filtered = image.copy()
    filtered.putalpha(Image.frombytes("L", image.size, bytes(filtered_alpha)))
    return filtered


def _validate_anchor(
    name: str,
    anchor_name: str,
    value: Any,
    source_size: tuple[int, int],
) -> tuple[float, float]:
    anchor = _pair(value, f"Frame '{name}' {anchor_name} anchor")
    if not (0 <= anchor[0] <= source_size[0] and 0 <= anchor[1] <= source_size[1]):
        raise ValueError(f"Frame '{name}' {anchor_name} anchor is outside its source cell")
    return anchor


def _scaled_anchor(
    anchor: tuple[float, float],
    crop_box: tuple[int, int, int, int],
    source_height: int,
    scale: float,
) -> tuple[int, int]:
    crop_left, _, _, crop_bottom = crop_box
    crop_bottom_from_bottom = source_height - crop_bottom
    return (
        round((anchor[0] - crop_left) * scale),
        round((anchor[1] - crop_bottom_from_bottom) * scale),
    )


def _translate(anchor: tuple[int, int], offset: tuple[int, int]) -> tuple[int, int]:
    return anchor[0] + offset[0], anchor[1] + offset[1]


def normalize(manifest_path: Path) -> NormalizationResult:
    manifest_path = Path(manifest_path)
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    if not isinstance(manifest, dict):
        raise ValueError("Manifest root must be an object")

    cell_width, cell_height = _integer_pair(manifest.get("cell_size"), "cell_size")
    if cell_width <= 0 or cell_height <= 0:
        raise ValueError("cell_size values must be positive")
    atlas_columns = manifest.get("atlas_columns")
    if not isinstance(atlas_columns, int) or atlas_columns <= 0:
        raise ValueError("atlas_columns must be a positive integer")
    atlas_rows = manifest.get("atlas_rows", atlas_columns)
    if not isinstance(atlas_rows, int) or atlas_rows <= 0:
        raise ValueError("atlas_rows must be a positive integer")
    sole_line = manifest.get("sole_line")
    if not isinstance(sole_line, int) or not 0 <= sole_line <= cell_height:
        raise ValueError("sole_line must be an integer inside the destination cell")
    canonical = manifest.get("canonical_head_pelvis")
    if not isinstance(canonical, (int, float)) or canonical <= 0:
        raise ValueError("canonical_head_pelvis must be positive")

    sources_data = manifest.get("sources")
    if not isinstance(sources_data, dict) or not sources_data:
        raise ValueError("sources must be a non-empty object")
    sources = {
        name: _load_source(manifest_path, name, data)
        for name, data in sources_data.items()
    }

    frames_data = manifest.get("frames")
    if not isinstance(frames_data, list) or not frames_data:
        raise ValueError("frames must be a non-empty array")
    if len(frames_data) > atlas_columns * atlas_rows:
        raise ValueError("frames exceed atlas capacity")

    atlas = Image.new(
        "RGBA",
        (cell_width * atlas_columns, cell_height * atlas_rows),
        (0, 0, 0, 0),
    )
    results: dict[str, FrameResult] = {}

    for index, frame_data in enumerate(frames_data):
        if not isinstance(frame_data, dict):
            raise ValueError(f"Frame at index {index} must be an object")
        name = frame_data.get("name")
        if not isinstance(name, str) or not name:
            raise ValueError(f"Frame at index {index} is missing a name")
        if name in results:
            raise ValueError(f"Duplicate frame name: {name}")

        source_name = frame_data.get("source")
        if source_name not in sources:
            raise ValueError(f"Frame '{name}' references a missing source")
        (
            source,
            (source_width, source_height),
            source_columns,
            source_reference,
            keep_largest_component,
        ) = sources[source_name]
        column, row = _integer_pair(frame_data.get("cell"), f"Frame '{name}' cell")
        source_rows = source.height // source_height
        if not 0 <= column < source_columns or not 0 <= row < source_rows:
            raise ValueError(f"Frame '{name}' cell is outside its source")

        anchors = frame_data.get("anchors")
        if not isinstance(anchors, dict):
            raise ValueError(f"Frame '{name}' is missing anchors")
        sole = _validate_anchor(name, "sole", anchors.get("sole"), (source_width, source_height))
        pelvis = _validate_anchor(name, "pelvis", anchors.get("pelvis"), (source_width, source_height))
        head = _validate_anchor(name, "head", anchors.get("head"), (source_width, source_height))
        measured = math.dist(head, pelvis)
        expected_source_distance = source_reference or float(canonical)
        error = abs(measured - expected_source_distance) / expected_source_distance
        if error > MAX_HEAD_PELVIS_ERROR:
            raise ValueError(
                f"HeadPelvisRatio for frame '{name}' differs from source reference by "
                f"{error:.3%}; maximum is 3%"
            )

        source_box = (
            column * source_width,
            row * source_height,
            (column + 1) * source_width,
            (row + 1) * source_height,
        )
        source_cell = source.crop(source_box)
        if keep_largest_component:
            source_cell = _keep_largest_alpha_component(source_cell)
        alpha_box = source_cell.getchannel("A").getbbox()
        if alpha_box is None:
            raise ValueError(f"Frame '{name}' has no opaque pixels")
        cropped = source_cell.crop(alpha_box)
        scale = float(canonical) / measured
        scaled_size = (
            max(1, round(cropped.width * scale)),
            max(1, round(cropped.height * scale)),
        )
        scaled = cropped.resize(scaled_size, Image.Resampling.LANCZOS)
        scaled_sole = _scaled_anchor(sole, alpha_box, source_height, scale)
        scaled_pelvis = _scaled_anchor(pelvis, alpha_box, source_height, scale)
        scaled_head = _scaled_anchor(head, alpha_box, source_height, scale)

        alignment = frame_data.get("alignment")
        if alignment == "grounded":
            offset = (
                cell_width // 2 - scaled_pelvis[0],
                sole_line - scaled_sole[1],
            )
        elif alignment == "airborne":
            destination = _integer_pair(
                frame_data.get("destination_anchor", [cell_width // 2, cell_height // 2]),
                f"Frame '{name}' destination_anchor",
            )
            offset = (
                destination[0] - scaled_pelvis[0],
                destination[1] - scaled_pelvis[1],
            )
        else:
            raise ValueError(f"Frame '{name}' alignment must be 'grounded' or 'airborne'")
        paste_left = offset[0]
        paste_top = cell_height - (offset[1] + scaled.height)
        if (
            paste_left < 0
            or paste_top < 0
            or paste_left + scaled.width > cell_width
            or paste_top + scaled.height > cell_height
        ):
            raise ValueError(f"Frame '{name}' overflows the destination cell")

        output = Image.new("RGBA", (cell_width, cell_height), (0, 0, 0, 0))
        output.alpha_composite(scaled, (paste_left, paste_top))
        final_sole = _translate(scaled_sole, offset)
        final_pelvis = _translate(scaled_pelvis, offset)
        final_head = _translate(scaled_head, offset)
        final_distance = math.dist(final_head, final_pelvis)
        final_error = abs(final_distance - canonical) / canonical
        if final_error > MAX_HEAD_PELVIS_ERROR:
            raise ValueError(
                f"HeadPelvisRatio for frame '{name}' is {final_error:.3%} after normalization"
            )

        atlas_left = (index % atlas_columns) * cell_width
        atlas_top = (index // atlas_columns) * cell_height
        atlas.alpha_composite(output, (atlas_left, atlas_top))
        results[name] = FrameResult(
            name=name,
            image=output,
            sole_anchor=final_sole,
            pelvis_anchor=final_pelvis,
            head_anchor=final_head,
        )

    return NormalizationResult(atlas=atlas, frames=results)


def build_contact_sheet(result: NormalizationResult, output_path: Path) -> None:
    if not result.frames:
        raise ValueError("Cannot build a contact sheet without frames")
    cell_width, cell_height = next(iter(result.frames.values())).image.size
    columns = min(4, len(result.frames))
    rows = math.ceil(len(result.frames) / columns)
    label_height = 18
    sheet = Image.new(
        "RGBA",
        (columns * cell_width, rows * (cell_height + label_height)),
        (18, 23, 32, 255),
    )
    draw = ImageDraw.Draw(sheet)
    for index, frame in enumerate(result.frames.values()):
        left = (index % columns) * cell_width
        top = (index // columns) * (cell_height + label_height)
        sheet.alpha_composite(frame.image, (left, top + label_height))
        draw.text((left + 3, top + 3), frame.name, fill=(245, 245, 245, 255))
        if frame.name in {"Rising", "Apex", "Falling"}:
            x = left + frame.pelvis_anchor[0]
            y = top + label_height + cell_height - frame.pelvis_anchor[1]
            draw.line((x - 5, y, x + 5, y), fill=(80, 255, 160, 255), width=1)
            draw.line((x, y - 5, x, y + 5), fill=(80, 255, 160, 255), width=1)
        else:
            y = top + label_height + cell_height - frame.sole_anchor[1]
            draw.line((left, y, left + cell_width - 1, y), fill=(255, 90, 90, 255), width=1)
    output_path = Path(output_path)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(output_path, optimize=True)


def main() -> int:
    parser = argparse.ArgumentParser(description="Normalize whole-frame character art")
    parser.add_argument("manifest", type=Path)
    parser.add_argument("--contact-sheet", type=Path)
    args = parser.parse_args()
    result = normalize(args.manifest)
    data = json.loads(args.manifest.read_text(encoding="utf-8"))
    output = _resolve_path(args.manifest, data.get("output_atlas"), "output_atlas")
    output.parent.mkdir(parents=True, exist_ok=True)
    result.atlas.save(output, optimize=True)
    if args.contact_sheet:
        build_contact_sheet(result, args.contact_sheet)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
