from __future__ import annotations

import json
import sys
import tempfile
import unittest
from pathlib import Path

from PIL import Image, ImageDraw


ART_TOOLS = Path(__file__).resolve().parents[1]
if str(ART_TOOLS) not in sys.path:
    sys.path.insert(0, str(ART_TOOLS))

from normalize_character_frames import build_contact_sheet, normalize


class NormalizeCharacterFramesTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp_dir = tempfile.TemporaryDirectory()
        self.root = Path(self.temp_dir.name)
        self.source_path = self.root / "synthetic.png"
        self.manifest_path = self.write_manifest()

    def tearDown(self) -> None:
        self.temp_dir.cleanup()

    def write_manifest(self, head_pelvis_error: float = 0.0) -> Path:
        cell_width, cell_height = 64, 128
        sheet = Image.new("RGBA", (cell_width * 4, cell_height), (0, 0, 0, 0))
        draw = ImageDraw.Draw(sheet)
        for index, color in enumerate(
            ((245, 180, 60, 255), (70, 190, 230, 255), (230, 90, 120, 255), (120, 210, 100, 255))
        ):
            left = index * cell_width + 22
            draw.rectangle((left, 18, left + 19, 112), fill=color)
        sheet.save(self.source_path)

        canonical = 50
        measured = canonical * (1.0 + head_pelvis_error)
        manifest = {
            "cell_size": [cell_width, cell_height],
            "atlas_columns": 4,
            "sole_line": 12,
            "canonical_head_pelvis": canonical,
            "output_atlas": str(self.root / "atlas.png"),
            "sources": {
                "synthetic": {
                    "path": str(self.source_path),
                    "cell_size": [cell_width, cell_height],
                    "columns": 4,
                }
            },
            "frames": [
                self.frame("Idle_00", 0, "grounded", measured),
                self.frame("Idle_01", 1, "grounded", measured),
                self.frame("Apex", 2, "airborne", measured, [32, 58]),
                self.frame("Falling", 3, "airborne", measured, [32, 58]),
            ],
        }
        path = self.root / f"manifest-{head_pelvis_error:.3f}.json"
        path.write_text(json.dumps(manifest, indent=2), encoding="utf-8")
        return path

    @staticmethod
    def frame(
        name: str,
        column: int,
        alignment: str,
        head_pelvis_distance: float,
        destination_anchor: list[int] | None = None,
    ) -> dict[str, object]:
        pelvis_y = 66
        frame: dict[str, object] = {
            "name": name,
            "source": "synthetic",
            "cell": [column, 0],
            "alignment": alignment,
            "anchors": {
                "sole": [32, 16],
                "pelvis": [32, pelvis_y],
                "head": [32, pelvis_y + head_pelvis_distance],
            },
        }
        if destination_anchor is not None:
            frame["destination_anchor"] = destination_anchor
        return frame

    def test_grounded_frame_places_sole_on_manifest_baseline(self) -> None:
        result = normalize(self.manifest_path)
        frame = result.frames["Idle_00"]
        self.assertEqual(frame.sole_anchor[1], 12)

    def test_airborne_frame_aligns_pelvis_without_runtime_offset(self) -> None:
        result = normalize(self.manifest_path)
        self.assertEqual(result.frames["Apex"].pelvis_anchor, (32, 58))

    def test_rejects_final_head_pelvis_error_over_three_percent(self) -> None:
        manifest = self.write_manifest(head_pelvis_error=0.04)
        with self.assertRaisesRegex(ValueError, "HeadPelvisRatio"):
            normalize(manifest)

    def test_repeated_normalization_is_byte_identical(self) -> None:
        first = normalize(self.manifest_path).atlas.tobytes()
        second = normalize(self.manifest_path).atlas.tobytes()
        self.assertEqual(first, second)

    def test_contact_sheet_is_written_with_rgba_content(self) -> None:
        output = self.root / "contact.png"
        build_contact_sheet(normalize(self.manifest_path), output)
        with Image.open(output) as contact:
            self.assertEqual(contact.mode, "RGBA")
            self.assertGreater(contact.getbbox()[2], 0)


if __name__ == "__main__":
    unittest.main()
