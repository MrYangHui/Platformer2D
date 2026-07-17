# Fenny Whole-Frame Jitter Remediation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Eliminate stationary and running whole-frame jitter by enforcing a stable grounded body anchor, using one Idle frame, and replacing the current independent Run poses with a continuity-validated eight-frame cycle.

**Architecture:** Keep `PlayerFramePresentation2D` as a sprite-only runtime adapter. Bake all alignment into a versioned atlas: grounded X alignment uses the pelvis, grounded Y alignment uses the sole, and manifest-defined motion budgets reject discontinuous sequences before Unity imports them. Keep the accepted airborne frames unchanged.

**Tech Stack:** Python 3, Pillow, `unittest`, Unity 6.3, C#, NUnit, Unity Test Framework, Sprite Editor data providers, built-in image generation with local alpha post-processing.

## Global Constraints

- Execute inline in the current workspace; do not create or dispatch subagents.
- Keep the final cell size at 768×1024, PPU at 480, and Sprite pivot at `(0.5, 0)`.
- Use one Idle frame and eight Run frames plus Rising, Apex, and Falling.
- Run pelvis X total drift must be at most 8 px and adjacent drift at most 6 px.
- Run pelvis Y total range must be at most 32 px and adjacent change at most 18 px.
- Do not add per-frame Transform offsets or scales to runtime data.
- Do not modify Rigidbody2D, colliders, GroundProbe, PlayerMotor, camera, or level geometry.
- Preserve v006 and all legacy rig assets; write v007 outputs under new filenames.
- Do not update the production Profile until v007 passes automated budgets and visual preview review.

---

### Task 1: Separate Grounded Horizontal and Vertical Anchors

**Files:**
- Modify: `Tools/Art/tests/test_normalize_character_frames.py`
- Modify: `Tools/Art/normalize_character_frames.py`

**Interfaces:**
- Consumes: existing manifest frame fields `alignment`, `anchors.sole`, and `anchors.pelvis`.
- Produces: grounded `FrameResult` values with `pelvis_anchor.x == cell_width // 2` and `sole_anchor.y == sole_line`.

- [ ] **Step 1: Write the failing axis-separation test**

Add a synthetic grounded frame whose sole X is 24 and pelvis X is 32 in a 64 px cell, then assert:

```python
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
```

- [ ] **Step 2: Run the focused Python test and verify RED**

Run:

```powershell
& 'C:\Users\TUDOU\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' -m unittest Tools.Art.tests.test_normalize_character_frames.NormalizeCharacterFramesTests.test_grounded_frame_centers_pelvis_x_and_places_sole_y -v
```

Expected: FAIL because the current code centers `scaled_sole.x`, moving the pelvis away from X=32.

- [ ] **Step 3: Implement independent grounded offsets**

Replace the grounded alignment branch with independent axes:

```python
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
```

- [ ] **Step 4: Run all Python normalizer tests and verify GREEN**

Run the full module. Expected: all tests pass and no output atlas is written by the tests.

- [ ] **Step 5: Commit the axis fix**

```powershell
git add Tools/Art/normalize_character_frames.py Tools/Art/tests/test_normalize_character_frames.py
git commit -m "fix: stabilize grounded frame alignment"
```

### Task 2: Enforce Manifest Motion-Continuity Budgets

**Files:**
- Modify: `Tools/Art/tests/test_normalize_character_frames.py`
- Modify: `Tools/Art/normalize_character_frames.py`

**Interfaces:**
- Consumes: optional manifest array `motion_groups` containing `name`, `frames`, `max_pelvis_x_span`, `max_pelvis_x_step`, `max_pelvis_y_span`, and `max_pelvis_y_step`.
- Produces: `ValueError` with the group and metric when a normalized sequence exceeds its budget.

- [ ] **Step 1: Write failing rejection and acceptance tests**

Create a two-frame synthetic motion group. The rejection test sets the second frame pelvis-to-sole distance 20 px higher than the first and uses a Y-span budget of 8 px. The acceptance test uses identical pelvis anchors and budgets of 2 px. Both tests call `normalize()` through a manifest, not an internal helper.

```python
data["motion_groups"] = [{
    "name": "Run",
    "frames": ["Idle_00", "Idle_01"],
    "max_pelvis_x_span": 2,
    "max_pelvis_x_step": 2,
    "max_pelvis_y_span": 8,
    "max_pelvis_y_step": 8,
}]
```

Expected rejection assertion:

```python
with self.assertRaisesRegex(ValueError, "Run.*pelvis Y span"):
    normalize(path)
```

- [ ] **Step 2: Run both focused tests and verify RED**

Expected: the rejection test fails because `normalize()` currently ignores `motion_groups`.

- [ ] **Step 3: Implement strict motion-group parsing and validation**

After all `FrameResult` objects are built, validate that every named frame exists, every budget is a finite non-negative number, and compute total span plus cyclic adjacent steps. Include the last-to-first step because Run loops.

Error messages must have this shape:

```text
Motion group 'Run' pelvis Y span is 89 px; maximum is 32 px
Motion group 'Run' pelvis Y step Run_04 -> Run_05 is 67 px; maximum is 18 px
```

- [ ] **Step 4: Run all Python tests and verify GREEN**

Expected: existing seven tests plus the new axis and motion-budget tests pass.

- [ ] **Step 5: Commit the budget gate**

```powershell
git add Tools/Art/normalize_character_frames.py Tools/Art/tests/test_normalize_character_frames.py
git commit -m "feat: reject discontinuous character frame sequences"
```

### Task 3: Make Single-Frame Idle a Supported Runtime Contract

**Files:**
- Modify: `Assets/Game/Tests/EditMode/Infrastructure/CharacterFramePipelineTests.cs`
- Modify: `Assets/Game/Tests/PlayMode/VerticalSliceSmokeTests.cs`
- Modify: `Assets/Game/Scripts/Presentation/Runtime/CharacterPresentationProfile.cs`
- Modify: `Assets/Game/Scripts/Infrastructure/Editor/FennyFrameConfigurator.cs`

**Interfaces:**
- Consumes: a Profile containing exactly one valid Idle Sprite.
- Produces: a stable stationary Sprite for any duration without changing the visual root.

- [ ] **Step 1: Write the failing Profile test**

Change `CreateValidProfile()` to assign one Idle Sprite and assert `TryValidate()` succeeds. Keep the Run minimum at eight.

```csharp
Assert.That(profile.IdleFrames.Count, Is.EqualTo(1));
Assert.That(profile.TryValidate(out string error), Is.True, error);
```

- [ ] **Step 2: Run focused Edit Mode and verify RED**

Run Unity Edit Mode filtered to `CharacterFramePipelineTests.ProfileRequiresEveryCoreStateAndPositiveTiming`.

Expected: FAIL with `Idle requires at least two complete frames.`

- [ ] **Step 3: Permit one complete Idle frame**

Change validation to reject only `idleFrames.Length < 1`, and update the error to `Idle requires at least one complete frame.` Do not special-case the runtime adapter; `ResolveLoopFrame()` already returns index zero for a one-frame list.

- [ ] **Step 4: Add a Play Mode stability assertion**

In `FennyFramesDriveRunAirborneAndPreserveFixedRoot`, stop the player, capture `renderer.sprite`, wait 0.6 seconds, and assert the same Sprite reference is still assigned:

```csharp
Sprite stationary = renderer.sprite;
yield return new WaitForSeconds(0.6f);
Assert.That(renderer.sprite, Is.SameAs(stationary));
```

- [ ] **Step 5: Configure only `Fenny_Idle_00` in the Profile**

Change the configurator assignment to:

```csharp
AssignFrames(serialized.FindProperty("idleFrames"), FrameNames.Take(1), sprites);
```

Update Edit Mode expectations from four Idle frames to one.

- [ ] **Step 6: Run focused Edit and Play Mode tests and verify GREEN**

Expected: Profile validation, configurator, and stationary stability tests pass.

- [ ] **Step 7: Commit the single-frame Idle contract**

```powershell
git add Assets/Game/Scripts/Presentation/Runtime/CharacterPresentationProfile.cs Assets/Game/Scripts/Infrastructure/Editor/FennyFrameConfigurator.cs Assets/Game/Tests/EditMode/Infrastructure/CharacterFramePipelineTests.cs Assets/Game/Tests/PlayMode/VerticalSliceSmokeTests.cs
git commit -m "fix: keep idle presentation visually stable"
```

### Task 4: Produce a Continuity-Controlled Run Candidate

**Files:**
- Create: `Assets/Game/Art/Characters/Player/FennyGolden_RunCycle_Candidate_v007.png`
- Create: `Assets/Game/Art/Characters/Player/FennyGolden_RunCycle_Candidate_v007.png.meta`
- Reference: `Assets/Game/Art/Characters/Player/FennyGolden_RunPoses_Candidate_v003.png`
- Reference: `Assets/Game/Art/Characters/Player/FennyGolden_IdlePoses_Candidate_v003.png`

**Interfaces:**
- Consumes: current full-character identity and costume references.
- Produces: a transparent 4×2 source sheet with eight ordered Run phases and no text, grid lines, shadows, or background pixels.

- [ ] **Step 1: Inspect both local reference images at original resolution**

Use `view_image` before the edit call. Treat Idle as the identity/proportion reference and Run v003 as pose-order reference only.

- [ ] **Step 2: Generate one combined candidate with the built-in image tool**

Use both local files as references and this production prompt:

```text
Use case: stylized-concept
Asset type: 2D side-view game character run-cycle sprite sheet
Primary request: redraw Fenny as one coherent eight-frame run cycle ordered contact, down, passing, up, then the mirrored-leg contact, down, passing, up phases.
Input images: Idle reference locks identity, costume, proportions, head size, backpack and rendering style; Run reference provides only the intended side-view running vocabulary.
Composition: exact 4 columns by 2 rows, one complete character per equal cell, facing right, generous separation, no cropping.
Motion constraint: the pelvis remains on one common vertical guide with only a small smooth symmetric bob; torso length and head size remain identical; adjacent frames are true in-betweens; first and second half are phase-paired.
Backdrop: perfectly flat solid #FF00FF chroma-key, no shadows, gradients, texture, grid, labels, watermark or text.
Avoid: independent unrelated poses, camera zoom changes, body scale changes, identity drift, extra limbs, detached parts, foot duplication, perspective rotation, green costume changes.
```

- [ ] **Step 3: Remove chroma key and normalize source dimensions**

Use the installed `remove_chroma_key.py` with border auto-key, soft matte, thresholds 12/220, despill, and one-pixel edge contraction if needed. Crop only uniform outer padding, preserve the complete characters, and resample the final sheet to a 4×2 grid whose dimensions are divisible by four and two. Save only the accepted transparent candidate at the v007 path.

- [ ] **Step 4: Inspect the candidate at source and game scale**

Reject and regenerate if any frame changes identity, head size, torso length, costume, backpack, viewing angle, or if the pelvis trajectory visibly jumps. One targeted regeneration is allowed per diagnosed defect; do not stack unrelated prompt changes.

- [ ] **Step 5: Commit the accepted source candidate**

```powershell
git add Assets/Game/Art/Characters/Player/FennyGolden_RunCycle_Candidate_v007.png Assets/Game/Art/Characters/Player/FennyGolden_RunCycle_Candidate_v007.png.meta
git commit -m "art: add continuity-controlled Fenny run cycle"
```

### Task 5: Build and Integrate the v007 Atlas

**Files:**
- Create: `Tools/Art/Manifests/fenny_golden_v007.json`
- Create: `Assets/Game/Art/Characters/Player/FennyGolden_Frames_v007.png`
- Create: `Assets/Game/Art/Characters/Player/FennyGolden_Frames_v007.png.meta`
- Modify: `Assets/Game/Scripts/Infrastructure/Editor/FennyFrameConfigurator.cs`
- Modify: `Assets/Game/Tests/EditMode/Infrastructure/CharacterFramePipelineTests.cs`
- Modify: `Assets/Game/Config/Characters/FennyGoldenPresentation.asset`
- Modify: `Assets/Game/Prefabs/Player/Player.prefab`

**Interfaces:**
- Consumes: one accepted Idle frame from v003, eight Run frames from v007, and three accepted airborne frames from v006.
- Produces: a 12-Sprite v007 atlas and a Profile containing 1 Idle, 8 Run, Rising, Apex, and Falling.

- [ ] **Step 1: Author the v007 manifest with measured anchors**

Use 768×1024 destination cells, four columns, three rows, canonical head-pelvis distance 430, and sole line 48. Record integer Sole/Pelvis/Head coordinates from each source cell. Add this mandatory budget block:

```json
"motion_groups": [
  {
    "name": "Run",
    "frames": ["Run_00", "Run_01", "Run_02", "Run_03", "Run_04", "Run_05", "Run_06", "Run_07"],
    "max_pelvis_x_span": 8,
    "max_pelvis_x_step": 6,
    "max_pelvis_y_span": 32,
    "max_pelvis_y_step": 18
  }
]
```

The frame order is `Idle_00`, `Run_00`–`Run_07`, `Rising`, `Apex`, `Falling`.

For v007, align all Run frames through explicit pelvis destinations `[440, 424, 432, 448, 440, 424, 432, 448]` at X=384. This preserves the two flight phases instead of forcing every boot onto the ground, yields a 24 px Y span and a 16 px maximum cyclic step, and keeps all corrections baked into the atlas.

- [ ] **Step 2: Run normalization and require a clean budget pass**

Run the normalizer with a contact-sheet output under `TestResults/FennyFrames/`. If any budget fails, return to Task 4 and replace the candidate; do not relax the approved values.

- [ ] **Step 3: Change Unity atlas constants and semantic names**

Set `AtlasPath` to v007, `AtlasHeight` to 3072, and `FrameNames` to the 12-frame order. Keep PPU, pivot, Full Rect, no mipmaps, 4096 max size, and uncompressed import.

- [ ] **Step 4: Update Edit Mode atlas expectations before running the configurator**

Expect v007, exactly 12 Sprites, one Idle frame, eight Run frames, and unchanged airborne semantic names.

- [ ] **Step 5: Run focused Edit Mode and verify RED, then configure assets**

The focused configurator test must first fail because the Profile still references v006. Run `FennyFrameConfigurator.Configure()` through the focused test or Unity execute method, then rerun and require GREEN.

- [ ] **Step 6: Generate visual QA artifacts**

Create a contact sheet plus an animated Run preview at actual camera scale. The preview must loop for at least three cycles and include a fixed crosshair at the Player root so whole-body drift is visible.

- [ ] **Step 7: Commit v007 integration**

```powershell
git add Tools/Art/Manifests/fenny_golden_v007.json Assets/Game/Art/Characters/Player/FennyGolden_Frames_v007.png Assets/Game/Art/Characters/Player/FennyGolden_Frames_v007.png.meta Assets/Game/Scripts/Infrastructure/Editor/FennyFrameConfigurator.cs Assets/Game/Tests/EditMode/Infrastructure/CharacterFramePipelineTests.cs Assets/Game/Config/Characters/FennyGoldenPresentation.asset Assets/Game/Prefabs/Player/Player.prefab
git commit -m "feat: install continuity-validated Fenny frames"
```

### Task 6: Full Regression, Build, and Delivery

**Files:**
- Verify: `Assets/Game/`
- Verify: `ProjectSettings/`
- Output: `Builds/Windows/Platformer2D.exe`
- Output: `TestResults/FennyFrames/`

**Interfaces:**
- Consumes: the integrated v007 Profile and Player Prefab.
- Produces: passing Python, Edit Mode, Play Mode, asset audit, and Windows x64 Development Build evidence.

- [ ] **Step 1: Run all Python art-tool tests**

Expected: all normalizer tests pass, including axis separation and motion budgets.

- [ ] **Step 2: Run complete Unity Edit Mode**

Expected: zero failures; restore only known Unity serialization noise with `apply_patch`.

- [ ] **Step 3: Run complete Unity Play Mode**

Expected: zero failures, including stationary Sprite stability and fixed visual-root assertions.

- [ ] **Step 4: Audit assets and diffs**

Require zero missing `.meta`, zero orphan `.meta`, no per-frame transform fields, `git diff --check` clean, and no unrelated project-setting changes.

- [ ] **Step 5: Build Windows x64 Development**

Run `SnowbreakFan.Infrastructure.Editor.BuildScripts.BuildWindows64`. Require `Build Finished, Result: Success.` and a new `Builds/Windows/Platformer2D.exe`.

- [ ] **Step 6: Commit any verification-only test adjustments**

Only commit intentional source/test/asset changes; exclude `TestResults`, generated caches, Unity project-setting noise, and build outputs already ignored by Git.

- [ ] **Step 7: Push only after local verification is clean**

Push `main` to `origin` without creating a remote repository, PR, Release, or publication. Confirm `origin/main...main` is `0 0` and the working tree is clean.
