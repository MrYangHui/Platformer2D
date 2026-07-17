# Fenny Run Stability Finalization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Produce a v008 whole-frame Fenny atlas whose perceptual upper-body core stays stable, and make run playback advance incrementally so changing movement speed cannot reinterpret or jump the accumulated animation phase.

**Architecture:** Extend the offline Python normalizer with an optional `visual_core` anchor used only for baked X placement, while pelvis Y continues to carry the reduced run bob curve. Add a small deterministic C# `FramePlaybackClock` and make `PlayerFramePresentation2D` advance it by `deltaTime × currentFPS`; no runtime per-frame Transform correction is introduced.

**Tech Stack:** Python 3 + Pillow + unittest, Unity 6.3/C#, Unity Test Framework, versioned PNG/JSON assets, Git.

## Global Constraints

- Keep the complete-frame SpriteRenderer architecture, one-frame Idle, accepted Rising/Apex/Falling pixels, Player physics, movement tuning, camera, and level content unchanged.
- Do not add run frames, restore the cutout rig, or add runtime per-frame position/scale correction.
- Reuse `FennyGolden_RunCycle_Candidate_v007.png`; do not invoke image generation unless deterministic v008 placement fails the approved continuity metrics.
- New atlas path is `Assets/Game/Art/Characters/Player/FennyGolden_Frames_v008.png`; v006/v007 assets remain present for rollback.
- Run visual-core X span and cyclic step are each at most `4 px`.
- Run pelvis Y destinations are exactly `440, 432, 436, 444, 440, 432, 436, 444`, giving span `12 px` and maximum cyclic step `8 px`.
- Runtime speed changes affect only future phase increments; Run entry presents frame `00` before advancing.
- Direct work on `main` and push to `origin/main` follow the user's previously approved repository workflow; do not create a PR, release, or remote repository.

---

### Task 1: Visual-core-aware offline normalization

**Files:**
- Modify: `Tools/Art/normalize_character_frames.py`
- Modify: `Tools/Art/tests/test_normalize_character_frames.py`

**Interfaces:**
- Consumes: optional frame field `anchors.visual_core: [number, number]`, optional `destination_visual_core_x: int`, and either the legacy `max_pelvis_*` motion budget or the new generic motion budget.
- Produces: `FrameResult.visual_core_anchor: tuple[int, int] | None`; generic group fields `x_anchor`, `y_anchor`, `max_x_span`, `max_x_step`, `max_y_span`, `max_y_step` with legacy compatibility.

- [ ] **Step 1: Write failing visual-core alignment tests**

Add tests that mutate the synthetic `Apex` frame as follows and assert X uses the visual core while Y still uses the pelvis:

```python
def test_airborne_frame_can_align_visual_core_x_and_pelvis_y(self) -> None:
    data = json.loads(self.manifest_path.read_text(encoding="utf-8"))
    frame = data["frames"][2]
    frame["anchors"]["visual_core"] = [38, 82]
    frame["destination_visual_core_x"] = 30
    path = self.root / "visual-core-alignment.json"
    path.write_text(json.dumps(data, indent=2), encoding="utf-8")

    result = normalize(path).frames["Apex"]

    self.assertEqual(result.visual_core_anchor[0], 30)
    self.assertEqual(result.pelvis_anchor[1], 58)

def test_visual_core_destination_requires_visual_core_anchor(self) -> None:
    data = json.loads(self.manifest_path.read_text(encoding="utf-8"))
    data["frames"][2]["destination_visual_core_x"] = 30
    path = self.root / "missing-visual-core.json"
    path.write_text(json.dumps(data, indent=2), encoding="utf-8")

    with self.assertRaisesRegex(ValueError, "Apex.*visual_core"):
        normalize(path)
```

- [ ] **Step 2: Run the focused tests and confirm RED**

Run:

```powershell
& 'C:\Users\TUDOU\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' -m unittest `
  Tools.Art.tests.test_normalize_character_frames.NormalizeCharacterFramesTests.test_airborne_frame_can_align_visual_core_x_and_pelvis_y `
  Tools.Art.tests.test_normalize_character_frames.NormalizeCharacterFramesTests.test_visual_core_destination_requires_visual_core_anchor -v
```

Expected: failure because `FrameResult` has no `visual_core_anchor` and the normalizer ignores `destination_visual_core_x`.

- [ ] **Step 3: Implement optional visual-core placement**

Extend `FrameResult`:

```python
@dataclass(frozen=True)
class FrameResult:
    name: str
    image: Image.Image
    sole_anchor: tuple[int, int]
    pelvis_anchor: tuple[int, int]
    head_anchor: tuple[int, int]
    visual_core_anchor: tuple[int, int] | None = None
```

When reading each frame, validate `anchors.visual_core` only when present, scale it with `_scaled_anchor`, and for `airborne` frames calculate axes independently:

```python
destination = _integer_pair(
    frame_data.get("destination_anchor", [cell_width // 2, cell_height // 2]),
    f"Frame '{name}' destination_anchor",
)
destination_visual_core_x = frame_data.get("destination_visual_core_x")
if destination_visual_core_x is None:
    offset_x = destination[0] - scaled_pelvis[0]
else:
    if scaled_visual_core is None:
        raise ValueError(
            f"Frame '{name}' destination_visual_core_x requires a visual_core anchor"
        )
    if isinstance(destination_visual_core_x, bool) or not isinstance(
        destination_visual_core_x, int
    ):
        raise ValueError(f"Frame '{name}' destination_visual_core_x must be an integer")
    offset_x = destination_visual_core_x - scaled_visual_core[0]
offset = (offset_x, destination[1] - scaled_pelvis[1])
```

Translate the optional anchor into `final_visual_core` and store it in `FrameResult`.

- [ ] **Step 4: Write failing generic motion-budget tests**

Add two visual cores to the synthetic Idle frames and a group using:

```python
{
    "name": "RunCore",
    "frames": ["Idle_00", "Idle_01"],
    "x_anchor": "visual_core",
    "y_anchor": "pelvis",
    "max_x_span": 2,
    "max_x_step": 2,
    "max_y_span": 2,
    "max_y_step": 2,
}
```

The accepted case aligns both visual cores within `2 px`. The rejected case places the second visual core more than `2 px` away and expects an error matching `RunCore.*visual_core X span`. Keep `test_motion_group_accepts_sequence_inside_budget` unchanged to prove the legacy schema remains supported.

- [ ] **Step 5: Run the generic budget tests and confirm RED**

Run the two new tests plus `test_motion_group_accepts_sequence_inside_budget`. Expected: the two new tests fail because only legacy pelvis budgets are recognized, while the legacy test passes.

- [ ] **Step 6: Implement generic anchor budgets with legacy fallback**

If a group contains any of `x_anchor`, `y_anchor`, or `max_x_span`, require the complete generic schema. Accept `x_anchor` values `pelvis` and `visual_core`; accept `y_anchor` value `pelvis`. Resolve each frame's selected anchor, reject missing visual cores, and reuse the current cyclic span/step loop with labels such as `visual_core X`. If the new keys are absent, execute the existing `max_pelvis_*` behavior unchanged.

- [ ] **Step 7: Run all Python tests and confirm GREEN**

Run:

```powershell
& 'C:\Users\TUDOU\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' `
  -m unittest Tools.Art.tests.test_normalize_character_frames -v
```

Expected: all tests pass, including new alignment/generic-budget tests and every legacy test.

---

### Task 2: Incremental run playback phase

**Files:**
- Create: `Assets/Game/Scripts/Presentation/Runtime/FramePlaybackClock.cs`
- Create: `Assets/Game/Scripts/Presentation/Runtime/FramePlaybackClock.cs.meta`
- Modify: `Assets/Game/Scripts/Presentation/Runtime/PlayerFramePresentation2D.cs`
- Modify: `Assets/Game/Tests/EditMode/Infrastructure/CharacterFramePipelineTests.cs`

**Interfaces:**
- Produces: public sealed `FramePlaybackClock` with `Reset()`, `Advance(float deltaTime, float framesPerSecond)`, and `CurrentIndex(int frameCount)`.
- Consumed by: `PlayerFramePresentation2D`; phase is reset on state entry and incremented only on later Updates.

- [ ] **Step 1: Write failing clock tests**

Add tests:

```csharp
[Test]
public void RunPhaseDoesNotReinterpretHistoryWhenSpeedDrops()
{
    FramePlaybackClock clock = new();
    for (int index = 0; index < 60; index++)
        clock.Advance(1f / 60f, 16f);
    int before = clock.CurrentIndex(8);

    clock.Advance(1f / 60f, 12.266667f);
    int after = clock.CurrentIndex(8);

    Assert.That(before, Is.EqualTo(0));
    Assert.That(after, Is.AnyOf(0, 1));
}

[Test]
public void PlaybackClockTraversesThreeCyclesInOrder()
{
    FramePlaybackClock clock = new();
    List<int> changed = new() { clock.CurrentIndex(8) };
    int previous = changed[0];
    for (int tick = 0; tick < 180; tick++)
    {
        clock.Advance(1f / 120f, 16f);
        int current = clock.CurrentIndex(8);
        if (current == previous)
            continue;
        changed.Add(current);
        previous = current;
    }
    Assert.That(changed.Take(25), Is.EqualTo(
        Enumerable.Range(0, 24).Select(index => index % 8).Append(0)));
}

[Test]
public void PlaybackClockResetReturnsToFrameZero()
{
    FramePlaybackClock clock = new();
    clock.Advance(0.25f, 16f);
    Assert.That(clock.CurrentIndex(8), Is.EqualTo(4));
    clock.Reset();
    Assert.That(clock.CurrentIndex(8), Is.Zero);
}
```

- [ ] **Step 2: Run focused Edit Mode tests and confirm RED**

Run Unity Edit Mode filtered to `CharacterFramePipelineTests`. Expected: compile failure because `FramePlaybackClock` does not exist.

- [ ] **Step 3: Implement `FramePlaybackClock`**

Use a `double phase` so long sessions do not lose practical precision. `Advance` rejects negative/non-finite delta and non-positive/non-finite FPS with `ArgumentOutOfRangeException`; `CurrentIndex` rejects `frameCount <= 0` and returns `(int)Math.Floor(phase) % frameCount`; `Reset` assigns zero.

- [ ] **Step 4: Replace elapsed-time reinterpretation in the adapter**

Replace `elapsed` with one clock instance. On a state change, reset the clock and resolve the frame immediately at index zero. When the state did not change, calculate the current Run rate using the existing `0.75–1.35` multiplier, advance by `Time.deltaTime × rate`, then resolve the index. Single-frame airborne states continue to return their fixed sprites. Keep all existing Transform writes confined to `Awake`.

- [ ] **Step 5: Run focused Edit Mode tests and confirm GREEN**

Run the same filtered fixture. Expected: the new phase tests and all existing presentation tests pass.

---

### Task 3: Generate and audit the v008 atlas

**Files:**
- Create: `Tools/Art/Manifests/fenny_golden_v008.json`
- Create: `Assets/Game/Art/Characters/Player/FennyGolden_Frames_v008.png`
- Create: `Assets/Game/Art/Characters/Player/FennyGolden_Frames_v008.png.meta`
- Modify: `Assets/Game/Tests/EditMode/Infrastructure/CharacterFramePipelineTests.cs`

**Interfaces:**
- Consumes: Task 1 visual-core placement and generic motion-budget schema.
- Produces: twelve semantic cells in v008 order: `Idle_00`, `Run_00..07`, `Rising`, `Apex`, `Falling`.

- [ ] **Step 1: Create the v008 manifest without generating new poses**

Copy v007 as the starting manifest, change only the output path, Run visual-core data, Run Y destinations, and motion group schema. Use these Run visual-core source anchors, derived from the approved torso/head perceptual center measurements:

```text
Run_00 [277,284]
Run_01 [219,271]
Run_02 [165,281]
Run_03 [129,281]
Run_04 [265,384]
Run_05 [211,378]
Run_06 [158,377]
Run_07 [126,375]
```

Set `destination_visual_core_x` to `380` on all eight Run frames. Set pelvis destination Y values to `440,432,436,444,440,432,436,444`; preserve each destination X only as the anatomical fallback value `384`. Use generic budgets `x_anchor=visual_core`, `y_anchor=pelvis`, `max_x_span=4`, `max_x_step=4`, `max_y_span=12`, `max_y_step=8`.

- [ ] **Step 2: Normalize v008 and render the contact sheet**

Run:

```powershell
& 'C:\Users\TUDOU\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' `
  Tools/Art/normalize_character_frames.py `
  Tools/Art/Manifests/fenny_golden_v008.json `
  --contact-sheet TestResults/FennyFrames/FennyGolden_v008_ContactSheet.png
```

Expected: output PNG is `3072×3072`, twelve cells are present, and normalization accepts all budgets.

- [ ] **Step 3: Add a v008 asset contract test before Unity integration**

Update the Edit Mode fixture constants to v008 and assert twelve sprites, PPU `480`, pivot `(384,0)`, one Idle, eight ordered Run sprites, and the expected jump sprite names. Before the configurator points at v008 this test must fail because v008 is not yet imported/configured.

- [ ] **Step 4: Render a fixed-root game-scale GIF and audit visual metrics**

Reuse the existing preview convention: `1280×720`, ortho-equivalent game scale, root crosshair fixed, three cycles at 16 FPS. Save `TestResults/FennyFrames/FennyRun_v008_GameScale.gif`. Compute the same torso/head regions used for the diagnosis and require the three previously severe transitions to meet torso IoU `≥0.80`, head IoU `≥0.85`; report visual-core and pelvis coordinate sequences.

---

### Task 4: Integrate v008 into Unity

**Files:**
- Modify: `Assets/Game/Scripts/Infrastructure/Editor/FennyFrameConfigurator.cs`
- Modify: `Assets/Game/Tests/EditMode/Infrastructure/CharacterFramePipelineTests.cs`
- Modify: `Assets/Game/Config/Characters/FennyGoldenPresentation.asset`
- Modify: `Assets/Game/Prefabs/Player/Player.prefab`
- Modify: `Assets/Game/Art/Characters/Player/FennyGolden_Frames_v008.png.meta`

**Interfaces:**
- Consumes: Task 3 v008 atlas.
- Produces: Player/profile references to v008 with one Idle, eight Run frames, and unchanged jump states/physics.

- [ ] **Step 1: Point the configurator at v008**

Change only `AtlasPath` from v007 to v008. Preserve cell dimensions, atlas dimensions, PPU `480`, bilinear filtering, uncompressed import, full-rect meshes, semantic names, one Idle, eight Run frames, 16 base FPS, thresholds, and visual root `-1.1`.

- [ ] **Step 2: Run the configurator in Unity batch mode**

```powershell
& 'F:\unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -quit -projectPath 'F:\UnityProjects\Platformer2D' `
  -executeMethod SnowbreakFan.Infrastructure.Editor.FennyFrameConfigurator.Configure `
  -logFile 'F:\UnityProjects\Platformer2D\TestResults\FennyV008Configure.log'
```

Expected: the v008 importer has twelve semantic sprites and the profile/Player prefab reference v008.

- [ ] **Step 3: Run focused Edit and Play Mode presentation tests**

Run `CharacterFramePipelineTests` and `VerticalSliceSmokeTests`. Expected: v008 configuration, incremental phase, single-frame Idle, fixed Visual Transform, run/facing, and airborne assertions all pass.

---

### Task 5: Full verification, review, commit, and push

**Files:**
- Verify all files changed by Tasks 1–4.
- Update: `docs/superpowers/plans/2026-07-18-fenny-run-stability-finalization.md` checkbox state only if useful; do not add unrelated documentation.

**Interfaces:**
- Produces: fresh test/build evidence, clean worktree, reviewed commits, synchronized `origin/main`.

- [ ] **Step 1: Run full Python and Unity suites**

Run the full Python module, full Unity Edit Mode, and full Unity Play Mode. Save results under `TestResults/FennyV008Final*.xml` and logs under `TestResults/FennyV008Final*.log`. Require zero failures.

- [ ] **Step 2: Audit Unity metadata**

Enumerate tracked assets and require no missing `.meta` files and no orphan `.meta` files. Run `git diff --check`.

- [ ] **Step 3: Build Windows x64 Development**

```powershell
& 'F:\unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -quit -projectPath 'F:\UnityProjects\Platformer2D' `
  -executeMethod SnowbreakFan.Infrastructure.Editor.BuildScripts.BuildWindows64 `
  -buildTarget StandaloneWindows64 `
  -logFile 'F:\UnityProjects\Platformer2D\TestResults\FennyV008FinalBuild.log'
```

Require `Build Finished, Result: Success.` and `Builds/Windows/Platformer2D.exe`.

- [ ] **Step 4: Review scope and restore only Unity-generated noise**

Inspect every diff. Preserve user work and all intended v008 changes. Restore only known unrelated Unity serialization noise after verifying its exact diff; never use `git reset --hard` or blanket checkout.

- [ ] **Step 5: Commit intentionally and push**

Create focused commits for the normalizer, playback clock, v008 art/integration, and any final test/doc adjustment. Push `main` to `origin/main`. Verify `git rev-list --left-right --count origin/main...main` returns `0 0` and `git status --short` is empty.
