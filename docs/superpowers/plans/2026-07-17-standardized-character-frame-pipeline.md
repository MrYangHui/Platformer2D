# Standardized Character Frame Pipeline Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. This project must be executed inline; do not create or dispatch subagents.

**Goal:** Replace Fenny's rigid cutout puppet with a stable whole-frame Idle/Run/Rising/Apex/Falling presentation and establish a reusable profile-driven pipeline for future playable characters.

**Architecture:** A deterministic offline normalizer packs complete character poses into fixed `512 x 1024` cells with one canonical character scale, a sole line at pixel `48`, and fixed bottom-center pivots. Unity imports the 15 semantic sprites, builds a `CharacterPresentationProfile`, and installs one `PlayerFramePresentation2D` plus one SpriteRenderer on Player; gameplay physics remain untouched and non-deforming attachments stay optional.

**Tech Stack:** Unity 6000.3.19f1, C#, Unity Test Framework/NUnit, Python 3, Pillow, NumPy, JSON manifests, built-in image generation, Git.

## Global Constraints

- Execute in `F:\UnityProjects\Platformer2D` without subagents or a separate worktree.
- Do not modify `Rigidbody2D`, `CapsuleCollider2D`, `GroundProbe2D`, `PlayerMotor2D`, movement configuration, input, camera, checkpoints, collectibles, UI, or level geometry.
- Final frame cells are exactly `512 x 1024`; atlas layout is 4 columns by 4 rows; cell 15 remains transparent and unsliced.
- Final sprites use `480` PPU, Full Rect meshes, bilinear filtering, no mipmaps, alpha transparency, and a fixed normalized pivot `(0.5, 0)`.
- The sole baseline is pixel `48`; Fenny's visual root Y is `-1.1`, producing a sole line at world Y `-1.0`, which is `0.1` below the collider bottom.
- Final semantic order is `Idle_00..03`, `Run_00..07`, `Rising`, `Apex`, `Falling`; exactly 15 sprites are imported.
- Runtime frame changes may not modify Transform position or scale. Facing uses `SpriteRenderer.flipX` only.
- Keep v003/v004 whole-frame sources and all cutout-rig assets until user Play Mode acceptance. Do not delete them in this plan.
- Git commits are local milestones. Push `main` only after full verification; do not create a release.

---

## File Map

- `Tools/Art/normalize_character_frames.py`: generic manifest loader, grid extractor, anchor-based normalizer, proportion validator, atlas writer, and contact-sheet renderer.
- `Tools/Art/tests/test_normalize_character_frames.py`: synthetic-image unit tests for scale, anchor placement, rejection, and deterministic output.
- `Tools/Art/Manifests/fenny_golden_v006.json`: source-sheet grids, semantic frame order, measured anchors, canonical scale, and output paths.
- `Assets/Game/Art/Characters/Player/FennyGolden_AirbornePhases_Candidate_v006.png`: complete-character Rising/Apex/Falling source strip.
- `Assets/Game/Art/Characters/Player/FennyGolden_Frames_v006.png`: normalized 4-by-4 runtime atlas.
- `Assets/Game/Scripts/Presentation/Runtime/CharacterPresentationProfile.cs`: reusable immutable-at-runtime character presentation data.
- `Assets/Game/Scripts/Presentation/Runtime/PlayerFramePresentation2D.cs`: profile-driven state/facing/frame selector.
- `Assets/Game/Scripts/Infrastructure/Editor/FennyFrameConfigurator.cs`: atlas importer, Sprite slicer, profile asset creator, Player prefab installer, and validation preview entry point.
- `Assets/Game/Config/Characters/FennyGoldenPresentation.asset`: generated Fenny profile.
- `Assets/Game/Tests/EditMode/Infrastructure/CharacterFramePipelineTests.cs`: atlas/profile/configurator/idempotence contracts.
- `Assets/Game/Tests/EditMode/Infrastructure/ArtIntegrationConfiguratorTests.cs`: Player whole-frame integration and physics preservation.
- `Assets/Game/Tests/PlayMode/VerticalSliceSmokeTests.cs`: actual scene state, facing, scale, and physics regression.
- `Assets/Game/Scripts/Infrastructure/Editor/ArtIntegrationConfigurator.cs`: delegate Player art setup to `FennyFrameConfigurator` instead of `FennyRigBuilder`.
- `Assets/Game/Prefabs/Player/Player.prefab`: generated whole-frame Player presentation.

---

### Task 1: Build the deterministic frame normalizer

**Files:**
- Create: `Tools/Art/normalize_character_frames.py`
- Create: `Tools/Art/tests/test_normalize_character_frames.py`

**Interfaces:**
- Consumes: JSON manifest with `cell_size`, `atlas_columns`, `sole_line`, `canonical_head_pelvis`, `sources`, and semantic `frames`.
- Produces: `normalize(manifest_path: Path) -> NormalizationResult`, `build_contact_sheet(result, output_path: Path) -> None`, and CLI exit code `0` only for valid deterministic output.

- [ ] **Step 1: Write failing synthetic-image tests**

Create four opaque test figures in temporary 64-by-128 source cells and assert exact output contracts:

```python
class NormalizeCharacterFramesTests(unittest.TestCase):
    def test_grounded_frame_places_sole_on_manifest_baseline(self):
        result = normalize(self.manifest_path)
        frame = result.frames["Idle_00"]
        self.assertEqual(frame.sole_anchor[1], 12)

    def test_airborne_frame_aligns_pelvis_without_runtime_offset(self):
        result = normalize(self.manifest_path)
        self.assertEqual(result.frames["Apex"].pelvis_anchor, (32, 58))

    def test_rejects_final_head_pelvis_error_over_three_percent(self):
        manifest = self.write_manifest(head_pelvis_error=0.04)
        with self.assertRaisesRegex(ValueError, "HeadPelvisRatio"):
            normalize(manifest)

    def test_repeated_normalization_is_byte_identical(self):
        first = normalize(self.manifest_path).atlas.tobytes()
        second = normalize(self.manifest_path).atlas.tobytes()
        self.assertEqual(first, second)
```

- [ ] **Step 2: Run the Python tests and verify RED**

```powershell
& 'C:\Users\TUDOU\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' `
  -m unittest discover -s 'Tools/Art/tests' -p 'test_normalize_character_frames.py' -v
```

Expected: import failure for `normalize_character_frames` or missing `normalize`.

- [ ] **Step 3: Implement manifest parsing and normalization**

Implement these exact dataclasses and rules:

```python
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

def normalize(manifest_path: Path) -> NormalizationResult:
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    # Extract declared grid cells, resample to the canonical anatomical
    # distance, place grounded frames by SoleAnchor and airborne frames by
    # PelvisAnchor, validate final ratios, and pack a transparent 4x4 atlas.
```

Use `Image.Resampling.LANCZOS`, integer-rounded placement, alpha compositing, and stable row-major ordering. Reject missing/duplicate names, missing alpha, anchors outside their source cell, overflow outside the destination cell, and final head-pelvis deviation greater than `0.03`.

- [ ] **Step 4: Implement CLI and contact sheet**

```python
def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("manifest", type=Path)
    parser.add_argument("--contact-sheet", type=Path)
    args = parser.parse_args()
    result = normalize(args.manifest)
    data = json.loads(args.manifest.read_text(encoding="utf-8"))
    output = Path(data["output_atlas"])
    output.parent.mkdir(parents=True, exist_ok=True)
    result.atlas.save(output, optimize=True)
    if args.contact_sheet:
        build_contact_sheet(result, args.contact_sheet)
    return 0
```

The contact sheet labels semantic names and renders the sole baseline for grounded frames and pelvis crosshair for airborne frames.

- [ ] **Step 5: Run Python tests and verify GREEN**

Run the Step 2 command. Expected: all four tests pass.

- [ ] **Step 6: Commit the normalizer**

```powershell
git add Tools/Art/normalize_character_frames.py Tools/Art/tests/test_normalize_character_frames.py
git commit -m "feat: normalize whole-frame character art"
```

---

### Task 2: Produce and visually validate Fenny's normalized v006 atlas

**Files:**
- Create: `Assets/Game/Art/Characters/Player/FennyGolden_AirbornePhases_Candidate_v006.png`
- Create: `Tools/Art/Manifests/fenny_golden_v006.json`
- Create: `Assets/Game/Art/Characters/Player/FennyGolden_Frames_v006.png`
- Verification output: `TestResults/FennyFrames/FennyGolden_v006_ContactSheet.png`

**Interfaces:**
- Consumes: v003 Idle, v003 Run, v004 Airborne, Fenny identity references, and Task 1 CLI.
- Produces: 15 coherent whole-character frames in the exact semantic order required by the Unity configurator.

- [ ] **Step 1: Generate one three-pose airborne source strip**

Use built-in image generation in generation mode with v003 Idle, v003 Run, and v004 Airborne as references. Use this exact prompt:

```text
Create a transparent-background production sprite strip for the same Fenny character shown in the references, strict side view facing right, exactly three separate full-body poses arranged left-to-right with equal character scale and no overlap: Rising, Apex, Falling. Preserve her face, golden twin-tail hair hardware, black/orange/white outfit, green accents, asymmetric legs, boots, holster, gloves, and backpack weapon. Rising: torso slightly forward, red-stocking leg bent upward, bare leg relaxed downward. Apex: compact but balanced, red-stocking leg bent, bare leg naturally hanging with a slight knee bend. Falling: torso upright, both legs extending downward with the bare leg leading slightly. Keep full coherent silhouettes; do not split limbs, expose joints, crop any body part, add labels, shadows, floor, effects, text, or extra objects. Match the painted anime finish and line weight of the references. Canvas 1536x1024, transparent RGBA.
```

Reject any result with disconnected anatomy, a different face, missing equipment, cropped boots/hair, or visibly different head/torso scale.

- [ ] **Step 2: Author the manifest**

Use `512 x 1024` destination cells, `sole_line = 48`, `atlas_columns = 4`, `canonical_head_pelvis = 430`, `airborne_pelvis_target = [256,505]`, the three source grids `(4x1, 4x2, 3x1)`, and this frame order:

```json
[
  "Idle_00", "Idle_01", "Idle_02", "Idle_03",
  "Run_00", "Run_01", "Run_02", "Run_03",
  "Run_04", "Run_05", "Run_06", "Run_07",
  "Rising", "Apex", "Falling"
]
```

Record integer `head`, `pelvis`, and `sole` anchors for every source cell. Grounded placement uses sole; airborne placement uses pelvis. Head/pelvis values drive final proportion validation.

- [ ] **Step 3: Generate atlas and contact sheet**

```powershell
& 'C:\Users\TUDOU\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' `
  'Tools/Art/normalize_character_frames.py' `
  'Tools/Art/Manifests/fenny_golden_v006.json' `
  --contact-sheet 'TestResults/FennyFrames/FennyGolden_v006_ContactSheet.png'
```

Expected: exit `0`, atlas `2048 x 4096`, fifteen populated cells, transparent cell 15, and no ratio rejection.

- [ ] **Step 4: Inspect source-size and 25% contact sheets**

Require stable head/torso size, coherent silhouettes, common grounded sole line with visible pelvis bob, readable Rising/Apex/Falling progression, and no joint seams at game-size reduction. Replace only failing source poses and rerun.

- [ ] **Step 5: Test and commit accepted art**

```powershell
& 'C:\Users\TUDOU\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' `
  -m unittest discover -s 'Tools/Art/tests' -p 'test_normalize_character_frames.py' -v
git add Assets/Game/Art/Characters/Player/FennyGolden_AirbornePhases_Candidate_v006.png `
  Assets/Game/Art/Characters/Player/FennyGolden_Frames_v006.png `
  Tools/Art/Manifests/fenny_golden_v006.json
git commit -m "art: add normalized Fenny frame atlas"
```

---

### Task 3: Add the reusable character presentation profile

**Files:**
- Create: `Assets/Game/Scripts/Presentation/Runtime/CharacterPresentationProfile.cs`
- Create: `Assets/Game/Tests/EditMode/Infrastructure/CharacterFramePipelineTests.cs`

**Interfaces:**
- Produces: `CharacterPresentationProfile` with public read-only properties and `bool TryValidate(out string error)`.
- Consumes later: imported semantic sprites from Task 2.

- [ ] **Step 1: Write failing profile contracts**

```csharp
[Test]
public void ProfileRequiresEveryCoreStateAndPositiveTiming()
{
    CharacterPresentationProfile profile = CreateValidProfile();
    Assert.That(profile.TryValidate(out string error), Is.True, error);
    SerializedObject serialized = new(profile);
    serialized.FindProperty("fallingFrame").objectReferenceValue = null;
    serialized.ApplyModifiedPropertiesWithoutUndo();
    Assert.That(profile.TryValidate(out error), Is.False);
    Assert.That(error, Does.Contain("Falling"));
}

[Test]
public void ProfileHasNoPerFrameTransformCorrectionFields()
{
    string[] forbidden = { "runFrameOffsets", "frameScales", "airborneOffset" };
    Assert.That(forbidden.Any(name =>
        typeof(CharacterPresentationProfile).GetField(
            name, BindingFlags.Instance | BindingFlags.NonPublic) != null), Is.False);
}
```

- [ ] **Step 2: Run focused EditMode tests and verify RED**

Filter `SnowbreakFan.Infrastructure.Tests.CharacterFramePipelineTests`. Expected: compile/test failure because `CharacterPresentationProfile` does not exist.

- [ ] **Step 3: Implement `CharacterPresentationProfile`**

Create exactly these serialized fields:

```csharp
[CreateAssetMenu(menuName = "Snowbreak Fan/Character Presentation Profile")]
public sealed class CharacterPresentationProfile : ScriptableObject
{
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private Sprite[] runFrames;
    [SerializeField] private Sprite risingFrame;
    [SerializeField] private Sprite apexFrame;
    [SerializeField] private Sprite fallingFrame;
    [SerializeField] private Sprite fallbackFrame;
    [SerializeField] private float idleFramesPerSecond = 4f;
    [SerializeField] private float runFramesPerSecond = 16f;
    [SerializeField] private float movementThreshold = 0.1f;
    [SerializeField] private float referenceRunSpeed = 6f;
    [SerializeField] private float apexVelocityThreshold = 0.75f;
    [SerializeField] private Vector3 visualRootLocalPosition = new(0f, -1.1f, 0f);
    [SerializeField] private float visualScale = 1f;
}
```

Expose read-only properties. `TryValidate` requires at least two non-null Idle frames, at least eight non-null Run frames, all three airborne frames, a fallback, positive FPS/reference speed/scale, non-negative thresholds, and a finite root position.

- [ ] **Step 4: Run focused tests and verify GREEN**

Expected: all profile tests pass without unexpected logs.

- [ ] **Step 5: Commit the profile API**

```powershell
git add Assets/Game/Scripts/Presentation/Runtime/CharacterPresentationProfile.cs `
  Assets/Game/Tests/EditMode/Infrastructure/CharacterFramePipelineTests.cs
git commit -m "feat: define character presentation profiles"
```

---

### Task 4: Implement the stable whole-frame runtime adapter

**Files:**
- Create: `Assets/Game/Scripts/Presentation/Runtime/PlayerFramePresentation2D.cs`
- Modify: `Assets/Game/Tests/EditMode/Infrastructure/CharacterFramePipelineTests.cs`
- Modify: `Assets/Game/Tests/PlayMode/VerticalSliceSmokeTests.cs`

**Interfaces:**
- Consumes: `CharacterPresentationProfile`, `SpriteRenderer`, `Transform visualRoot`, `Rigidbody2D`, and `PlayerMotor2D`.
- Produces: whole-frame Idle/Run/Rising/Apex/Falling selection and persistent facing without per-frame Transform writes.

- [ ] **Step 1: Replace rig-specific PlayMode expectations with failing frame expectations**

Require `PlayerFramePresentation2D`, child `Visual`, exactly one SpriteRenderer, no Animator, no `PlayerRigPresentation2D`, and no `FennyVisualRig`. Add this state/facing sequence:

```csharp
body.linearVelocity = new Vector2(4f, 0f);
SetMotorState(motor, PlayerMotionState.Grounded);
yield return null;
Assert.That(renderer.sprite.name, Does.StartWith("Fenny_Run_"));
Assert.That(renderer.flipX, Is.False);
Assert.That(visual.localPosition, Is.EqualTo(new Vector3(0f, -1.1f, 0f)));
Assert.That(visual.localScale, Is.EqualTo(Vector3.one));

body.linearVelocity = new Vector2(-4f, 0f);
yield return null;
Assert.That(renderer.flipX, Is.True);

SetMotorState(motor, PlayerMotionState.Rising);
body.linearVelocity = new Vector2(0f, 0.2f);
yield return null;
Assert.That(renderer.sprite.name, Is.EqualTo("Fenny_Apex"));
```

- [ ] **Step 2: Run focused PlayMode and verify RED**

Filter `SnowbreakFan.Tests.PlayMode.VerticalSliceSmokeTests`. Expected: missing new adapter and current rig hierarchy failures.

- [ ] **Step 3: Implement `PlayerFramePresentation2D`**

Use internal states `Idle`, `Run`, `Rising`, `Apex`, `Falling`. `Awake` validates references/profile, sets the root position/one-time uniform scale and fallback Sprite. `Update` uses:

```csharp
float horizontalSpeed = body.linearVelocity.x;
if (Mathf.Abs(horizontalSpeed) > profile.MovementThreshold)
    facingRight = horizontalSpeed > 0f;
targetRenderer.flipX = !facingRight;

PresentationState state = ResolveState();
if (state != currentState)
{
    elapsed = 0f;
    currentState = state;
}
elapsed += Time.deltaTime;
targetRenderer.sprite = ResolveFrame(state, elapsed) ?? profile.FallbackFrame;
```

`ResolveState` chooses Grounded Idle/Run, then Apex when `abs(linearVelocity.y) <= ApexVelocityThreshold`, then Rising for `PlayerMotionState.Rising`, otherwise Falling. Run FPS multiplier is `Mathf.Clamp(abs(horizontalSpeed) / ReferenceRunSpeed, 0.75f, 1.35f)`. No method assigns root position or scale after `Awake`.

- [ ] **Step 4: Add an EditMode reflection guard**

Require exactly these serialized adapter fields: `profile`, `targetRenderer`, `visualRoot`, `body`, and `motor`. Reject per-frame offset/scale fields.

- [ ] **Step 5: Compile and commit the runtime API**

Focused EditMode tests must pass. Scene PlayMode remains red until Task 5 installs the component.

```powershell
git add Assets/Game/Scripts/Presentation/Runtime/PlayerFramePresentation2D.cs `
  Assets/Game/Tests/EditMode/Infrastructure/CharacterFramePipelineTests.cs `
  Assets/Game/Tests/PlayMode/VerticalSliceSmokeTests.cs
git commit -m "feat: drive standardized character frames"
```

---

### Task 5: Import the atlas, create Fenny's profile, and migrate Player

**Files:**
- Create: `Assets/Game/Scripts/Infrastructure/Editor/FennyFrameConfigurator.cs`
- Create generated: `Assets/Game/Config/Characters/FennyGoldenPresentation.asset`
- Modify generated: `Assets/Game/Art/Characters/Player/FennyGolden_Frames_v006.png.meta`
- Modify: `Assets/Game/Scripts/Infrastructure/Editor/ArtIntegrationConfigurator.cs`
- Modify: `Assets/Game/Tests/EditMode/Infrastructure/CharacterFramePipelineTests.cs`
- Modify: `Assets/Game/Tests/EditMode/Infrastructure/ArtIntegrationConfiguratorTests.cs`
- Modify generated: `Assets/Game/Prefabs/Player/Player.prefab`

**Interfaces:**
- Produces: `FennyFrameConfigurator.Configure()`, `ConfigureAtlas()`, `CreateOrUpdateProfile()`, `InstallOnPlayer()`, and `RenderContactPreview(string outputPath)`.
- Consumes: exact semantic frame names and runtime APIs from Tasks 2-4.

- [ ] **Step 1: Write failing asset and Player contracts**

```csharp
Assert.That(sprites, Has.Count.EqualTo(15));
Assert.That(sprites.All(sprite => sprite.rect.size == new Vector2(512f, 1024f)), Is.True);
Assert.That(sprites.All(sprite => sprite.pixelsPerUnit == 480f), Is.True);
Assert.That(sprites.All(sprite => sprite.pivot == new Vector2(256f, 0f)), Is.True);

Transform visual = player.transform.Find("Visual");
Assert.That(player.transform.Find("FennyVisualRig"), Is.Null);
Assert.That(player.GetComponent("PlayerRigPresentation2D"), Is.Null);
Assert.That(player.GetComponent("PlayerFramePresentation2D"), Is.Not.Null);
Assert.That(visual.GetComponentsInChildren<SpriteRenderer>(), Has.Length.EqualTo(1));
Assert.That(visual.localPosition, Is.EqualTo(new Vector3(0f, -1.1f, 0f)));
Assert.That(player.GetComponent<CapsuleCollider2D>().size,
    Is.EqualTo(new Vector2(0.8f, 1.8f)));
```

Also require profile arrays `4/8`, Rising/Apex/Falling names, FPS `4/16`, reference speed `6`, and root Y `-1.1`.

- [ ] **Step 2: Run focused EditMode and verify RED**

Expected: missing importer/profile/configurator and current rig still installed.

- [ ] **Step 3: Implement deterministic atlas slicing**

Use Unity Sprite Editor Data Provider APIs. Configure Multiple Sprite mode, PPU `480`, alpha transparency, bilinear filter, no mipmaps, Full Rect, fixed pivot `(0.5,0)`, and exactly these row-major names:

```csharp
private static readonly string[] FrameNames =
{
    "Fenny_Idle_00", "Fenny_Idle_01", "Fenny_Idle_02", "Fenny_Idle_03",
    "Fenny_Run_00", "Fenny_Run_01", "Fenny_Run_02", "Fenny_Run_03",
    "Fenny_Run_04", "Fenny_Run_05", "Fenny_Run_06", "Fenny_Run_07",
    "Fenny_Rising", "Fenny_Apex", "Fenny_Falling"
};
```

Do not slice the transparent final cell.

- [ ] **Step 4: Create/update the profile idempotently**

Create `Assets/Game/Config/Characters` if missing. Load or create the profile, assign semantic sprites, exact Task 3 values, and fallback `Fenny_Idle_00`.

- [ ] **Step 5: Migrate Player idempotently**

Remove `PlayerRigPresentation2D`, `PlayerSpriteAnimator2D`, and child `FennyVisualRig`. Reuse/create child `Visual` with one Player-layer SpriteRenderer, local position `(0,-1.1,0)`, identity rotation/scale, and initial Idle sprite. Add/reuse `PlayerFramePresentation2D` and serialize profile, renderer, visual root, body, and motor. Preserve all other components/children.

- [ ] **Step 6: Switch the art integration entry point**

Replace `FennyRigBuilder.Build()` in `ArtIntegrationConfigurator.Configure()` with `FennyFrameConfigurator.Configure()`. Leave platform/background configuration unchanged.

- [ ] **Step 7: Execute configurator and verify GREEN**

```powershell
& 'F:\unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -quit `
  -projectPath 'F:\UnityProjects\Platformer2D' `
  -executeMethod SnowbreakFan.Infrastructure.Editor.FennyFrameConfigurator.Configure `
  -logFile 'F:\UnityProjects\Platformer2D\TestResults\FennyFrameConfigure.log'
```

Run focused `CharacterFramePipelineTests`, `ArtIntegrationConfiguratorTests`, and `VerticalSliceSmokeTests`. Require all pass with no presentation errors.

- [ ] **Step 8: Verify idempotence and commit migration**

Run Configure twice in one EditMode test and compare atlas/profile/Player dependency hashes.

```powershell
git add Assets/Game/Art/Characters/Player/FennyGolden_Frames_v006.png.meta `
  Assets/Game/Config/Characters `
  Assets/Game/Scripts/Infrastructure/Editor/FennyFrameConfigurator.cs `
  Assets/Game/Scripts/Infrastructure/Editor/ArtIntegrationConfigurator.cs `
  Assets/Game/Tests/EditMode/Infrastructure/CharacterFramePipelineTests.cs `
  Assets/Game/Tests/EditMode/Infrastructure/ArtIntegrationConfiguratorTests.cs `
  Assets/Game/Tests/PlayMode/VerticalSliceSmokeTests.cs `
  Assets/Game/Prefabs/Player/Player.prefab
git commit -m "feat: install standardized Fenny frames"
```

---

### Task 6: Full verification, visual handoff, and Git delivery

**Files:**
- Verification outputs only under `TestResults`, `Temp`, and `Builds/Windows`.
- Modify scoped source only when verification proves a defect.

**Interfaces:**
- Produces: verified frame pipeline, clean asset/meta audit, successful Windows build, and user Play Mode validation target.

- [ ] **Step 1: Render final contact and scene-scale previews**

Produce `TestResults/FennyFrames/FennyFinalContact.png` plus Idle, Run contact/passing, Rising, Apex, and Falling previews at Player world scale. Require coherent silhouettes and fixed root/scale.

- [ ] **Step 2: Run complete EditMode and PlayMode suites**

Write `TestResults/FennyFramesFinalEditMode.xml` and `TestResults/FennyFramesFinalPlayMode.xml`. Launch Unity without `-quit`, poll, and require both `<test-run>` elements to report `failed="0"`.

- [ ] **Step 3: Audit runtime assets and metadata**

Require `MISSING_META=0`, `ORPHAN_META=0`, fifteen v006 sprites, one Player SpriteRenderer, no Player Animator/rig drivers/rig child, no per-frame Transform corrections, and unchanged physics/movement data.

- [ ] **Step 4: Build Windows x64 Development**

```powershell
& 'F:\unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -quit `
  -projectPath 'F:\UnityProjects\Platformer2D' `
  -executeMethod SnowbreakFan.Infrastructure.Editor.BuildScripts.BuildWindows64 `
  -logFile 'F:\UnityProjects\Platformer2D\TestResults\FennyFramesFinalBuild.log'
```

Require `Build Finished, Result: Success.` Restore only known Unity serialization noise with `apply_patch`.

- [ ] **Step 5: Final diff and push**

Require `git diff --check`, clean worktree after any final scoped commit, and remote equality:

```powershell
git push origin main
git rev-list --left-right --count origin/main...main
```

Expected: `0 0`. Do not create a release.

## Self-Review Result

- Spec coverage: fixed canvas, canonical scale, three anchors, ratio rejection, deterministic atlas, semantic sprites, reusable profile, shared adapter, fixed Transform, flip-only facing, Fenny migration, new-character workflow, fallback behavior, physics isolation, visual QA, full tests, meta audit, Windows build, and Git delivery all map to explicit tasks.
- Placeholder scan: no deferred marker, unspecified error handling, or generic test step remains.
- Type consistency: `CharacterPresentationProfile`, `PlayerFramePresentation2D`, `FennyFrameConfigurator`, serialized fields, semantic names, cell/PPU/baseline values, profile values, paths, and tests are consistent.
