# Fenny Golden runtime-art candidate QA

Date: 2026-07-17  
Generation path: built-in `image_gen`  
Transparency path: bundled `remove_chroma_key.py`; no CLI/model fallback

## Scope and reference roles

- `FennyGolden_SideMaster_v001.png`: primary identity, side-view silhouette, rendering and proportion reference.
- `FennyGolden_OfficialReference.png`: clothing-recognition reference only. It is not copied into the runtime candidate directory.
- `FrozenExclusionZone_v001.png`: environmental palette context only.
- Design target: non-pixel, roughly four-head-tall chibi, strict right-facing side view, golden curled twin-tails, black mechanical ornaments, orange-red/black/white outfit, lime accents, mechanical forearm guard, asymmetric legs, tactical boots, weapon stowed on the back.
- Pipeline target: Idle and Run are bone-animation pose references. They are not intended to replace the Unity 2D Animation rig with a conventional whole-frame spritesheet.

## Deliverables

### Keyed source and QA history

- `FennyGolden_PartsMaster_Keyed_v001.png`: first parts layout; retained for comparison.
- `FennyGolden_PartsMaster_Keyed_v002.png`: corrected parts-layout source used for the current runtime candidate.
- `FennyGolden_IdlePoses_Keyed_v001.png`: four-pose idle source.
- `FennyGolden_RunPoses_Keyed_v001.png`: eight-pose run source.
- `*_AlphaRejected_v001.png`: first matte pass, retained because the wide matte transition risked thinning red costume regions.
- `*_AlphaQA_v002.png`: second matte experiment, retained for QA only.
- `FennyGolden_PartsMaster_StructureRejected_v003.png`: transparent first parts layout, removed from the Unity candidate directory because several required pieces were missing or merged.

### Current Unity candidates

| File | Size | Mode | Transparent | Partial alpha | Opaque | Coverage | Alpha bbox |
|---|---:|---|---:|---:|---:|---:|---|
| `FennyGolden_PartsMaster_Candidate_v004.png` | 1536x1024 | RGBA | 1,173,288 | 35,386 | 364,190 | 25.40% | `(23,26)-(1478,996)` |
| `FennyGolden_IdlePoses_Candidate_v003.png` | 1672x941 | RGBA | 1,176,518 | 33,727 | 363,107 | 25.22% | `(149,67)-(1475,881)` |
| `FennyGolden_RunPoses_Candidate_v003.png` | 1536x1024 | RGBA | 1,290,654 | 34,221 | 247,989 | 17.94% | `(27,40)-(1420,974)` |

All four corner pixels are alpha 0 in all three current candidates. No magenta-like visible pixels remain after despill. Each PNG in `Assets/Game/Art/Characters/Player/` has a matching Unity `.meta` file.

Final removal parameters:

```text
--auto-key border --soft-matte --transparent-threshold 30 --opaque-threshold 96 --despill --edge-contract 1
```

Detected source keys were `#f403e9` (Parts v002), `#f703f4` (Idle), and `#f804ec` (Run).

## Visual QA and readiness

### Parts master v004

Passes:

- Identity, right-facing profile, palette, asymmetric legs, two boots, holster and stowed back weapon are preserved.
- Many elements are isolated with useful rectangular slicing space: head/profile, front hair options, individual curl sections, pelvis, both skirt panels, several limb candidates, both boots, holster and weapon.
- Whole-sheet alpha edge review shows no visible chroma-key halo; thin hair curls remain present after one-pixel edge contraction.

Limitations:

- This is a production **candidate**, not a directly bindable final rig master.
- The large rear-hair piece still contains both tails, while additional curl pieces are also present. An artist must choose/redraw a consistent non-duplicated hair hierarchy.
- Some arm candidates remain compound sleeve/forearm/hand shapes rather than clean upper-arm and forearm pairs.
- The torso candidate includes some lower orange garment structure; skirt and pelvis overlap must be manually redrawn for reliable deformation.
- Joint overlap lengths were generated visually and are not guaranteed to survive extreme elbow/knee rotations.
- Fine curl tips, fingers, black stocking edges and boot straps need inspection at intended pixels-per-unit before final slicing.

Directly sliceable now: individual reference/candidate pieces for manual assembly tests and silhouette evaluation.  
Not ready without manual art work: final Sprite Skin mesh, bone weights, deformable elbow/knee joints, production hair hierarchy.

### Idle poses v003

- Exactly four complete right-facing poses are present.
- Foot baseline varies only about two pixels across the sheet (`y=879..881`).
- Identity, outfit, hair ornaments, asymmetric legs and stowed weapon remain consistent.
- Motion is deliberately subtle and suitable for tuning torso breathing, pelvis settle, twin-tail lag and skirt secondary motion on a bone rig.
- Can be sliced directly as pose reference or temporary placeholder frames; should not be shipped as the final Idle implementation.

### Run poses v003

- Exactly eight complete right-facing poses are present in a 4x2 reading order.
- Top-row contact baseline varies about seven pixels (`y=474..481`); bottom row varies about six (`y=968..974`).
- Contact, compression, passing and elevated phases are visually distinct, with opposite-leg phases and natural arm counter-swing.
- Identity, asymmetric legs and stowed weapon remain consistent.
- Some elevated poses are more airborne and stylized than a strict mechanical run chart. Use them to tune the bone loop and select Sprite Swap accents, not as a literal ready-to-play eight-frame export.
- Can be sliced directly as animation reference or temporary placeholder frames; final gameplay Run should be authored on the 2D bone rig.

## Final prompt set

### Corrected parts master

```text
Use case: identity-preserve
Asset type: corrected Unity 2D Animation production character parts master, keyed cutout source
Input images: Image 1 is the prior parts-sheet draft to improve; Image 2 is the primary character identity and strict right-facing side-view style anchor; Image 3 is clothing recognition support only.
Primary request: Correct only the anatomical part separation of Image 1. Preserve its exact character identity, palette, line art, scale and rendering. Produce exactly 23 isolated rigging pieces; never combine an upper arm with a forearm, never combine a torso with a skirt, and never omit either boot.
Required pieces: 1 head/face; 2 rear hair mass; 3 front bangs/face-frame hair; 4 near twin-tail upper segment; 5 near twin-tail lower curl; 6 far twin-tail upper segment; 7 far twin-tail lower curl; 8 torso; 9 pelvis; 10 front white skirt panel; 11 rear orange skirt panel; 12 near upper arm; 13 near forearm plus hand and mechanical guard; 14 far upper arm; 15 far forearm plus hand; 16 near thigh; 17 near shin; 18 near boot; 19 far thigh with asymmetric stocking; 20 far shin with asymmetric stocking; 21 far boot; 22 hip holster; 23 back-mounted weapon.
Layout: arrange these 23 pieces in a spacious regular technical grid, four rows with invisible cells, one cell may remain empty. No two pieces touch or overlap. Every part has a complete silhouette. Extend joint ends under neighboring pieces with generous hidden overlap allowance at shoulders, elbows, hips, knees, ankles, hair joints and skirt pivots. Both boots must be complete and separate. Maintain consistent source scale and strict right-facing side anatomy.
Scene/backdrop: perfectly flat solid #ff00ff chroma-key background, exact uniform color, no shadows, no gradient, no texture, no floor, no reflection, no lighting variation.
Style/medium: polished non-pixel 2D anime game production art, approximately four-head-tall chibi proportions when assembled, crisp dark contour and restrained cel shading matching Images 1 and 2.
Constraints: exactly 23 isolated pieces; no assembled full character; no compound full limbs; no duplicated or missing parts; no extra weapons; no pose frames; no labels, text, numbers, guides, borders, checkerboard, watermark, scenery, cast shadows, glow or effects; do not use #ff00ff in character pieces; keep the weapon in stowed form; preserve golden curls, black mechanical ornaments, orange-red/black/white clothing and lime-green accents.
```

### Idle pose reference

```text
Use case: identity-preserve
Asset type: Unity 2D Animation idle-cycle pose reference sheet, not final frame animation
Input images: Image 1 is the primary character identity, exact side-view silhouette and rendering reference; Image 2 is clothing recognition support only; Image 3 is environment palette context only.
Primary request: Create a four-pose idle-cycle reference sheet for Fenny Golden, used by an animator to tune a bone-rigged Unity 2D Animation loop. Show the same character exactly four times in a single horizontal row, in chronological cycle order, without panels or labels.
Character invariants in every pose: non-pixel polished anime game art, approximately four-head-tall chibi proportions, strict orthographic side view facing right, same character scale and body proportions, same golden curled twin-tails and black mechanical hair ornaments, orange-red black-and-white outfit, fluorescent lime-green accents, mechanical forearm guard, asymmetric leg costume, tactical short boots, weapon securely stowed vertically on the back. Preserve identity and costume from Image 1. Do not redraw or redesign between poses.
Pose sequence: 1 neutral relaxed standing contact; 2 subtle inhale/up pose with torso rising and twin-tails lagging slightly; 3 neutral passing pose; 4 subtle exhale/down pose with knees and torso settling and curls overshooting slightly. Both feet remain planted; no walking, no waving, no weapon draw. Motion amplitude small and game-readable.
Scene/backdrop: perfectly flat solid #ff00ff chroma-key background for later local removal, exact uniform color only, with no shadows, gradients, texture, floor, reflection, or lighting variation.
Style/medium: clean crisp non-pixel 2D anime gameplay sprite reference, restrained cel shading, dark readable contour, matching Image 1.
Composition/framing: landscape sheet; four equal invisible cells; each full body completely visible with generous padding; identical foot-contact baseline, identical head-to-foot scale and consistent horizontal center within each cell.
Constraints: exactly four complete figures; face right; no perspective or 3/4 view; no cropped hair, weapon, hands or boots; no extra character; no duplicated/missing limbs; no detached pieces; no labels, text, numbers, arrows, borders, ground line, watermark, scenery, effects, cast shadow or glow; do not use #ff00ff in character; prioritize a stable silhouette and consistent identity over exaggerated motion.
```

### Run pose reference

```text
Use case: identity-preserve
Asset type: Unity 2D Animation run-cycle key-pose reference sheet, not final frame animation
Input images: Image 1 is the primary character identity, exact side-view silhouette and rendering reference; Image 2 is clothing recognition support only; Image 3 is environment palette context only.
Primary request: Create an eight-pose run-cycle reference sheet for Fenny Golden, for an animator tuning a bone-rigged Unity 2D Animation loop and selecting occasional Sprite Swap key poses. Show exactly eight full-body poses in chronological order in a clean 4-column by 2-row layout, no panels and no labels.
Character invariants in every pose: non-pixel polished anime game art, approximately four-head-tall chibi proportions suitable for gameplay, strict orthographic side view facing right, identical character scale and body proportions, golden curled twin-tails with black mechanical hair ornaments, orange-red black-and-white outfit with fluorescent lime-green accents, mechanical forearm guard, asymmetric left/right leg costume, tactical short boots, weapon firmly stowed on the back. Preserve identity and costume from Image 1. Do not redraw or redesign between poses.
Pose sequence in reading order: 1 near-leg forward contact; 2 recoil/down with weight accepted; 3 passing with rear foot under body; 4 high/up before toe-off; 5 far-leg forward opposite contact; 6 opposite recoil/down; 7 opposite passing; 8 opposite high/up. Use a practical energetic but controlled platformer run, natural opposite arm swing, torso leaning slightly forward. Weapon stays secured. Twin-tail curls lag and overlap naturally but do not obscure the face or feet.
Scene/backdrop: perfectly flat solid #ff00ff chroma-key background for later local removal; exact uniform color with no shadows, gradients, texture, floor, reflection, or lighting variation.
Style/medium: clean crisp non-pixel 2D anime gameplay sprite reference with dark readable contour and restrained cel shading, matching Image 1.
Composition/framing: landscape sheet, eight equal invisible cells in 4x2; full character and all hair/weapon visible in every cell; generous padding; identical foot-contact baseline within each row; identical head-to-foot scale and centered registration in all cells.
Constraints: exactly eight complete figures; all face right; no perspective or 3/4 views; no crop; no extra characters; no duplicated or missing limbs; no weapon draw; no floating or detached costume pieces; no labels, text, numbers, arrows, borders, ground line, watermark, scenery, speed lines, effects, cast shadows or glow; do not use #ff00ff in character; preserve asymmetric leg costume through the full cycle; prioritize correct readable run phases and identity consistency.
```

## Integration boundary

No Player prefab, Animator, Sprite Library, Sprite Skin, scene, test, project setting or Git state was modified by this character-art task. Runtime integration must be reviewed separately after manual slicing/cleanup.
