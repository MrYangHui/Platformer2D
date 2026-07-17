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
                    "expected_head_pelvis": canonical,
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

    def test_grounded_frame_centers_pelvis_x_and_places_sole_y(self) -> None:
        data = json.loads(self.manifest_path.read_text(encoding="utf-8"))
        data["frames"][0]["anchors"]["sole"] = [24, 16]
        data["frames"][0]["anchors"]["pelvis"] = [32, 66]
        data["frames"][0]["anchors"]["head"] = [32, 116]
        path = self.root / "split-grounded-anchors.json"
        path.write_text(json.dumps(data, indent=2), encoding="utf-8")

        frame = normalize(path).frames["Idle_00"]

        self.assertEqual(frame.pelvis_anchor[0], 32)
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

    def test_source_reference_scale_normalizes_to_canonical_distance(self) -> None:
        data = json.loads(self.manifest_path.read_text(encoding="utf-8"))
        data["cell_size"] = [128, 256]
        data["sole_line"] = 24
        data["sources"]["synthetic"]["expected_head_pelvis"] = 25
        for frame in data["frames"]:
            frame["anchors"]["pelvis"] = [32, 50]
            frame["anchors"]["head"] = [32, 75]
            if frame["alignment"] == "airborne":
                frame["destination_anchor"] = [64, 116]
        path = self.root / "half-scale-source.json"
        path.write_text(json.dumps(data, indent=2), encoding="utf-8")

        frame = normalize(path).frames["Idle_00"]

        self.assertEqual(frame.head_anchor[1] - frame.pelvis_anchor[1], 50)

    def test_optional_largest_component_filter_removes_grid_leakage(self) -> None:
        with Image.open(self.source_path) as opened:
            sheet = opened.convert("RGBA")
        ImageDraw.Draw(sheet).rectangle((2, 100, 4, 102), fill=(255, 40, 20, 255))
        sheet.save(self.source_path)
        data = json.loads(self.manifest_path.read_text(encoding="utf-8"))
        data["sources"]["synthetic"]["keep_largest_component"] = True
        path = self.root / "filtered-components.json"
        path.write_text(json.dumps(data, indent=2), encoding="utf-8")

        alpha = normalize(path).frames["Idle_00"].image.getchannel("A")

        self.assertEqual(alpha.getpixel((4, 105)), 0)

    def test_motion_group_rejects_pelvis_y_span_over_budget(self) -> None:
        data = json.loads(self.manifest_path.read_text(encoding="utf-8"))
        data["frames"][0]["anchors"]["pelvis"] = [32, 50]
        data["frames"][0]["anchors"]["head"] = [32, 100]
        data["frames"][1]["anchors"]["pelvis"] = [32, 70]
        data["frames"][1]["anchors"]["head"] = [32, 120]
        data["motion_groups"] = [self.motion_group(y_span=8, y_step=8)]
        path = self.root / "rejected-motion-group.json"
        path.write_text(json.dumps(data, indent=2), encoding="utf-8")

        with self.assertRaisesRegex(ValueError, "Run.*pelvis Y span.*Idle_00.*Idle_01"):
            normalize(path)

    def test_motion_group_accepts_sequence_inside_budget(self) -> None:
        data = json.loads(self.manifest_path.read_text(encoding="utf-8"))
        data["motion_groups"] = [self.motion_group(y_span=2, y_step=2)]
        path = self.root / "accepted-motion-group.json"
        path.write_text(json.dumps(data, indent=2), encoding="utf-8")

        result = normalize(path)

        self.assertIn("Idle_00", result.frames)
        self.assertIn("Idle_01", result.frames)

    def test_motion_groups_rejects_explicit_null(self) -> None:
        data = json.loads(self.manifest_path.read_text(encoding="utf-8"))
        data["motion_groups"] = None
        path = self.root / "null-motion-groups.json"
        path.write_text(json.dumps(data, indent=2), encoding="utf-8")

        with self.assertRaisesRegex(ValueError, "motion_groups must be an array"):
            normalize(path)

    def test_motion_group_rejects_unrepresentable_budget(self) -> None:
        data = json.loads(self.manifest_path.read_text(encoding="utf-8"))
        group = self.motion_group(y_span=2, y_step=2)
        group["max_pelvis_x_span"] = 10**400
        data["motion_groups"] = [group]
        path = self.root / "unrepresentable-motion-budget.json"
        path.write_text(json.dumps(data, indent=2), encoding="utf-8")

        with self.assertRaisesRegex(ValueError, "non-negative finite number"):
            normalize(path)

    def test_motion_groups_reject_duplicate_names(self) -> None:
        data = json.loads(self.manifest_path.read_text(encoding="utf-8"))
        group = self.motion_group(y_span=2, y_step=2)
        data["motion_groups"] = [group, dict(group)]
        path = self.root / "duplicate-motion-groups.json"
        path.write_text(json.dumps(data, indent=2), encoding="utf-8")

        with self.assertRaisesRegex(ValueError, "Duplicate motion group name: Run"):
            normalize(path)

    def test_motion_group_rejects_duplicate_frame_names(self) -> None:
        data = json.loads(self.manifest_path.read_text(encoding="utf-8"))
        group = self.motion_group(y_span=2, y_step=2)
        group["frames"] = ["Idle_00", "Idle_00"]
        data["motion_groups"] = [group]
        path = self.root / "duplicate-motion-frames.json"
        path.write_text(json.dumps(data, indent=2), encoding="utf-8")

        with self.assertRaisesRegex(ValueError, "Run.*duplicate frame names"):
            normalize(path)

    @staticmethod
    def motion_group(y_span: int, y_step: int) -> dict[str, object]:
        return {
            "name": "Run",
            "frames": ["Idle_00", "Idle_01"],
            "max_pelvis_x_span": 2,
            "max_pelvis_x_step": 2,
            "max_pelvis_y_span": y_span,
            "max_pelvis_y_step": y_step,
        }

    def test_contact_sheet_is_written_with_rgba_content(self) -> None:
        output = self.root / "contact.png"
        build_contact_sheet(normalize(self.manifest_path), output)
        with Image.open(output) as contact:
            self.assertEqual(contact.mode, "RGBA")
            self.assertGreater(contact.getbbox()[2], 0)


if __name__ == "__main__":
    unittest.main()
