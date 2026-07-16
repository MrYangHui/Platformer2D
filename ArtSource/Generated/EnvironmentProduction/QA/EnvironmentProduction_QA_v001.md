# Environment Production QA v001

Date: 2026-07-17

Every final PNG was inspected with `view_image` at original detail and measured with Pillow. The keyed sources remain in `Sources/`.

| Candidate | Size | Alpha | Nonzero coverage | Alpha bounds | Corner alpha TL/TR/BL/BR | Magenta-like opaque pixels | Intended use |
| --- | ---: | --- | ---: | --- | --- | ---: | --- |
| Background_Sky_v001.png | 1672×940 | Opaque RGBA | 100.00% | full canvas | 255/255/255/255 | 0 | Base sky and cloud layer |
| Background_Far_v001.png | 1928×816 | RGBA | 23.91% | (0,310)–(1928,816) | 0/0/255/255 | 0 | Far skyline and lockdown towers |
| Background_Mid_v001.png | 1871×841 | RGBA | 29.62% | (0,295)–(1871,756) | 0/0/0/0 | 0 | Buildings, road ruins, barriers, bus |
| Background_Near_v001.png | 1672×941 | RGBA | 15.70% | (0,395)–(1669,855) | 0/0/0/0 | 0 | Sparse steel frames and pipes |
| Platform_Short_v001.png | 2172×724 | RGBA | 29.94% | (151,233)–(2017,508) | 0/0/0/0 | 0 | 3-world-unit platform skin candidate |
| Platform_Medium_v001.png | 2179×722 | RGBA | 31.71% | (109,270)–(2069,540) | 0/0/0/0 | 0 | 6-world-unit platform skin candidate |
| Platform_Long_v001.png | 1374×1145 | RGBA | 7.88% | (35,530)–(1340,630) | 0/0/0/0 | 0 | 12-world-unit platform skin candidate |

## Visual conclusions

- PASS: Palette, cold daylight, simplified industrial materials, and restrained detail align with the frozen exclusion-zone reference.
- PASS: Far/Mid/Near have clean transparency, transparent top corners, no visible magenta fringe, and no magenta-like opaque residue.
- PASS: Mid and Near keep large open areas and do not introduce bright high-frequency noise behind the player.
- PASS: All platform candidates have a strong bright snow top edge, darker structural underside, orthographic side view, and no cast shadow.
- PASS: No text, logo, UI, watermark, character, or extracted third-party material is visible.

## Known limitations and integration decisions

- The generated platform canvases deliberately retain transparent padding. Unity integration should use sprite trimming or custom sprite rects before assigning a fixed PPU; do not use the full canvas bounds as gameplay geometry.
- The three generated platform paintings are visually related but not a mathematically identical modular kit. For first-pass prefab skins this is acceptable; a later production pass may derive shared left/fill/right modules if seamless runtime stretching is required.
- Background_Far is intentionally bottom-anchored across the full width, so its bottom corners are opaque; its top corners are transparent as required for compositing.
- Background images are candidate parallax strips, not guaranteed seamless tiles. Overlap, fog masking, or alternating variants should be evaluated during scene integration.
- Near contains top beams that could be mistaken for walkable surfaces if placed too close to gameplay silhouettes. Keep it behind playable geometry with reduced contrast and parallax offset.
- No scene, prefab, importer PPU, slicing, collider, code, test, Git commit, or push was changed by this task.
