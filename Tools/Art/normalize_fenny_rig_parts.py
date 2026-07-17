"""Normalize and validate the production Fenny rigid-rig atlas."""

from __future__ import annotations

import sys
from collections import deque
from pathlib import Path

import numpy as np
from PIL import Image


PART_NAMES = (
    "Head",
    "RearHair",
    "NearPonyUpper",
    "NearPonyLower",
    "FarPonyUpper",
    "FarPonyLower",
    "Torso",
    "Pelvis",
    "FrontSkirt",
    "RearSkirt",
    "Backpack",
    "NearUpperArm",
    "NearForearmHand",
    "FarUpperArm",
    "FarForearmHand",
    "RedThigh",
    "RedShin",
    "RedBoot",
    "BareThigh",
    "BareShin",
    "BareBoot",
)

ATLAS_SIZE = (1792, 1152)
CELL_SIZE = (256, 384)
VISIBLE_ALPHA = 8
EDGE_MARGIN = 2
MAGENTA_DISTANCE = 24 * 24
DOWNSAMPLE = 4


def _validate_cell(image: Image.Image, index: int, name: str) -> None:
    cell_width, cell_height = CELL_SIZE
    column = index % 7
    row = index // 7
    box = (
        column * cell_width,
        row * cell_height,
        (column + 1) * cell_width,
        (row + 1) * cell_height,
    )
    cell = image.crop(box)
    alpha = cell.getchannel("A")
    bounds = alpha.point(lambda value: 255 if value > VISIBLE_ALPHA else 0).getbbox()
    if bounds is None:
        raise ValueError(f"{name} cell contains no visible pixels")

    left, top, right, bottom = bounds
    if (
        left <= EDGE_MARGIN
        or top <= EDGE_MARGIN
        or right >= cell_width - EDGE_MARGIN
        or bottom >= cell_height - EDGE_MARGIN
    ):
        raise ValueError(f"{name} cell touches its boundary: {bounds}")

    pixels = np.asarray(cell, dtype=np.int32)
    visible = pixels[:, :, 3] > VISIBLE_ALPHA
    distance = (
        (pixels[:, :, 0] - 255) ** 2
        + pixels[:, :, 1] ** 2
        + (pixels[:, :, 2] - 255) ** 2
    )
    if np.any(visible & (distance < MAGENTA_DISTANCE)):
        raise ValueError(f"{name} contains visible chroma-key pixels")


def _dilate(mask: np.ndarray, iterations: int) -> np.ndarray:
    result = mask.copy()
    for _ in range(iterations):
        padded = np.pad(result, 1, mode="constant", constant_values=False)
        result = np.zeros_like(result)
        for offset_y in range(3):
            for offset_x in range(3):
                result |= padded[
                    offset_y : offset_y + result.shape[0],
                    offset_x : offset_x + result.shape[1],
                ]
    return result


def _component_bounds(image: Image.Image) -> list[tuple[int, int, int, int]]:
    alpha = np.asarray(image.getchannel("A")) > VISIBLE_ALPHA
    height = alpha.shape[0] // DOWNSAMPLE
    width = alpha.shape[1] // DOWNSAMPLE
    pooled = alpha[: height * DOWNSAMPLE, : width * DOWNSAMPLE]
    pooled = pooled.reshape(height, DOWNSAMPLE, width, DOWNSAMPLE).any(axis=(1, 3))
    pooled = _dilate(pooled, 2)

    visited = np.zeros_like(pooled)
    components: list[tuple[int, int, int, int, int]] = []
    for start_y, start_x in zip(*np.nonzero(pooled & ~visited)):
        if visited[start_y, start_x]:
            continue
        queue = deque([(int(start_y), int(start_x))])
        visited[start_y, start_x] = True
        min_x = max_x = int(start_x)
        min_y = max_y = int(start_y)
        area = 0

        while queue:
            y, x = queue.popleft()
            area += 1
            min_x = min(min_x, x)
            max_x = max(max_x, x)
            min_y = min(min_y, y)
            max_y = max(max_y, y)
            for delta_y, delta_x in ((-1, 0), (1, 0), (0, -1), (0, 1)):
                next_y = y + delta_y
                next_x = x + delta_x
                if (
                    0 <= next_y < height
                    and 0 <= next_x < width
                    and pooled[next_y, next_x]
                    and not visited[next_y, next_x]
                ):
                    visited[next_y, next_x] = True
                    queue.append((next_y, next_x))

        if area >= 12:
            components.append((area, min_x, min_y, max_x + 1, max_y + 1))

    if len(components) != len(PART_NAMES):
        sizes = sorted((component[0] for component in components), reverse=True)
        raise ValueError(
            f"expected 21 isolated parts, found {len(components)}; areas={sizes}"
        )

    bounds = [
        (
            max(0, min_x * DOWNSAMPLE + 2 * DOWNSAMPLE),
            max(0, min_y * DOWNSAMPLE + 2 * DOWNSAMPLE),
            min(image.width, max_x * DOWNSAMPLE - 2 * DOWNSAMPLE),
            min(image.height, max_y * DOWNSAMPLE - 2 * DOWNSAMPLE),
        )
        for _, min_x, min_y, max_x, max_y in components
    ]

    ordered: list[tuple[int, int, int, int]] = []
    remaining = bounds.copy()
    for _ in range(3):
        row = sorted(remaining, key=lambda box: (box[1] + box[3]) * 0.5)[:7]
        for box in row:
            remaining.remove(box)
        ordered.extend(sorted(row, key=lambda box: (box[0] + box[2]) * 0.5))
    return ordered


def _pack_parts(source: Image.Image) -> Image.Image:
    atlas = Image.new("RGBA", ATLAS_SIZE, (0, 0, 0, 0))
    source_cell_width = source.width / 7
    source_cell_height = source.height / 3
    scale = min(
        (CELL_SIZE[0] - 16) / source_cell_width,
        (CELL_SIZE[1] - 16) / source_cell_height,
    )

    for index, bounds in enumerate(_component_bounds(source)):
        part = source.crop(bounds)
        width = max(1, round(part.width * scale))
        height = max(1, round(part.height * scale))
        part = part.resize((width, height), Image.Resampling.LANCZOS)

        cell_x = (index % 7) * CELL_SIZE[0]
        cell_y = (index // 7) * CELL_SIZE[1]
        x = cell_x + (CELL_SIZE[0] - width) // 2
        y = cell_y + (CELL_SIZE[1] - height) // 2
        atlas.alpha_composite(part, (x, y))
    return atlas


def main(input_path: str, output_path: str) -> None:
    """Normalize and validate the 21-cell Fenny rig atlas."""
    source = Image.open(input_path).convert("RGBA")
    atlas = _pack_parts(source)

    for index, name in enumerate(PART_NAMES):
        _validate_cell(atlas, index, name)

    target = Path(output_path)
    target.parent.mkdir(parents=True, exist_ok=True)
    atlas.save(target)


if __name__ == "__main__":
    if len(sys.argv) != 3:
        raise SystemExit(
            "usage: normalize_fenny_rig_parts.py INPUT_RGBA OUTPUT_ATLAS"
        )
    main(sys.argv[1], sys.argv[2])
