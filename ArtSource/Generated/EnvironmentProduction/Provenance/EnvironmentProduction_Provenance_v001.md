# Environment Production Provenance v001

- Generation date: 2026-07-17
- Generator: built-in `image_gen` tool (one independent call per asset)
- Style reference: `ArtSource/Generated/ArtDirection/FrozenExclusionZone_v001.png`
- Reference role: style, palette, material, and mood reference only; not an edit target
- Copyright policy: original generated assets; no official wallpaper, extracted game asset, third-party fan art, logo, or trademark was requested for inclusion
- Transparent workflow: flat `#ff00ff` keyed source, followed by `remove_chroma_key.py --auto-key border --soft-matte --transparent-threshold 12 --opaque-threshold 220 --despill`
- Removal runtime: bundled Codex Python with Pillow 12.2.0
- Sky workflow: opaque generation; no chroma-key removal

## Built-in output mapping

| Asset | Built-in generated output | Preserved project source |
| --- | --- | --- |
| Background_Sky_v001 | `exec-572dd782-c19d-4b4b-86f1-60f55994418f.png` | `Sources/Background_Sky_v001_source.png` |
| Background_Far_v001 | `exec-2ec64493-6cd7-41bb-b224-10de8b2ee652.png` | `Sources/Background_Far_v001_keyed.png` |
| Background_Mid_v001 | `exec-997278f6-9fa9-479f-aad1-aa76160b2cc1.png` | `Sources/Background_Mid_v001_keyed.png` |
| Background_Near_v001 | `exec-e82b8519-73e7-4122-aff7-4513af245bc6.png` | `Sources/Background_Near_v001_keyed.png` |
| Platform_Short_v001 | `exec-e6ccf50e-e3ad-4780-9558-3c0dd3b9f7ce.png` | `Sources/Platform_Short_v001_keyed.png` |
| Platform_Medium_v001 | `exec-735f6a2b-481a-417d-bdd3-8a38c27d31d9.png` | `Sources/Platform_Medium_v001_keyed.png` |
| Platform_Long_v001 | `exec-51f19561-1809-4d0d-81d6-dc1d4898e84b.png` | `Sources/Platform_Long_v001_keyed.png` |

All final candidate PNGs are under `Assets/Game/Art/Environments/Backgrounds/` or `Assets/Game/Art/Environments/Terrain/`. No scene integration was performed.
