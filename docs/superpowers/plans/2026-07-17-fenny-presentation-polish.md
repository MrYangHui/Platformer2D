# Fenny Presentation Polish Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make Fenny visually contact the floor, remove crop-induced Run jitter, increase Run playback to 16 FPS, and replace the reused Run airborne pose with a dedicated natural-leg pose.

**Architecture:** Keep physics and gameplay untouched. `PlayerSpriteAnimator2D` owns presentation-only frame offsets on the existing `Visual` transform, while `ArtIntegrationConfigurator` imports and serializes a new airborne Sprite plus exact eight-frame Run corrections derived from the original 4×2 source grid.

**Tech Stack:** Unity 6000.3.19f1, C#, NUnit/Unity Test Framework, built-in image generation, bundled Python 3 with Pillow, Git.

## Global Constraints

- Work inline in the current repository; do not create or dispatch subagents.
- Do not change Rigidbody2D, CapsuleCollider2D, GroundProbe2D, PlayerMotor2D, PlayerMovementConfig, level geometry, camera behavior, or input.
- Player `Visual` baseline is exactly `(0, -0.95, 0)`.
- Run uses the existing eight Sprites at exactly `16` FPS; do not generate extra Run frames.
- The dedicated airborne pose keeps the red-stocking leg deeply bent and raised; the bare leg hangs naturally with only a slight knee bend.
- The airborne asset is versioned as `FennyGolden_Airborne_Candidate_v004.png`; do not overwrite Idle or Run sources.
- Invalid presentation setup disables only `PlayerSpriteAnimator2D` and emits one clear error.
- Do not create a release.

---

### Task 1: Lock the asset and prefab requirements with an EditMode RED test

**Files:**
- Modify: `Assets/Game/Tests/EditMode/Infrastructure/ArtIntegrationConfiguratorTests.cs`
- Test: `Assets/Game/Tests/EditMode/Infrastructure/ArtIntegrationConfiguratorTests.cs`

**Interfaces:**
- Consumes: Player prefab, current `PlayerSpriteAnimator2D`, airborne asset path.
- Produces: `FennyAirborneAndPrefabUseStablePresentationCalibration()` regression contract.

- [x] **Step 1: Add the failing configuration test**

Add these imports if absent:

```csharp
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools.Utils;
```

Add this test to `ArtIntegrationConfiguratorTests`:

```csharp
[Test]
public void FennyAirborneAndPrefabUseStablePresentationCalibration()
{
    const string airbornePath =
        "Assets/Game/Art/Characters/Player/FennyGolden_Airborne_Candidate_v004.png";
    const string playerPath = "Assets/Game/Prefabs/Player/Player.prefab";

    Sprite airborne = AssetDatabase.LoadAssetAtPath<Sprite>(airbornePath);
    Assert.That(airborne, Is.Not.Null);
    Assert.That(airborne.pivot.y, Is.LessThanOrEqualTo(0.01f));
    Assert.That(airborne.bounds.size.y, Is.EqualTo(1.91f).Within(0.03f));

    GameObject root = PrefabUtility.LoadPrefabContents(playerPath);
    try
    {
        Transform visual = root.transform.Find("Visual");
        Component animator = root.GetComponent("PlayerSpriteAnimator2D");
        Assert.That(animator, Is.Not.Null);
        SerializedObject serialized = new(animator);
        SerializedProperty offsets = serialized.FindProperty("runFrameOffsets");

        Assert.That(visual.localPosition, Is.EqualTo(new Vector3(0f, -0.95f, 0f)));
        Assert.That(serialized.FindProperty("runFramesPerSecond").floatValue, Is.EqualTo(16f));
        Assert.That(serialized.FindProperty("airborneFrame").objectReferenceValue, Is.SameAs(airborne));
        Assert.That(offsets.arraySize, Is.EqualTo(8));

        Vector2[] expected =
        {
            new(0.009259f, 0f),
            new(-0.046296f, 0f),
            new(-0.159722f, 0f),
            new(-0.328704f, 0f),
            new(-0.041667f, 0f),
            new(-0.106481f, 0f),
            new(-0.215278f, 0f),
            new(-0.349537f, 0f)
        };
        for (int index = 0; index < expected.Length; index++)
            Assert.That(offsets.GetArrayElementAtIndex(index).vector2Value,
                Is.EqualTo(expected[index]).Using(Vector2ComparerWithEqualsOperator.Instance));
    }
    finally
    {
        PrefabUtility.UnloadPrefabContents(root);
    }
}
```

- [x] **Step 2: Run the focused EditMode test and verify RED**

Run Unity without `-quit`:

```powershell
& 'F:\unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode `
  -projectPath 'F:\UnityProjects\Platformer2D' -runTests -testPlatform EditMode `
  -testFilter 'SnowbreakFan.Infrastructure.Tests.ArtIntegrationConfiguratorTests.FennyAirborneAndPrefabUseStablePresentationCalibration' `
  -testResults 'F:\UnityProjects\Platformer2D\TestResults\FennyPolishTask1Red.xml' `
  -logFile 'F:\UnityProjects\Platformer2D\Temp\FennyPolishTask1Red.log'
```

Poll the Unity process, then inspect the XML. Expected: one failure because the v004 airborne Sprite does not exist.

---

### Task 2: Generate and prepare the dedicated airborne Sprite

**Files:**
- Create: `Assets/Game/Art/Characters/Player/FennyGolden_Airborne_Candidate_v004.png`
- Unity creates: `Assets/Game/Art/Characters/Player/FennyGolden_Airborne_Candidate_v004.png.meta`

**Interfaces:**
- Consumes: Idle and Run candidate sheets as identity/style references.
- Produces: one cropped alpha PNG containing the dedicated right-facing airborne pose.

- [x] **Step 1: Generate the chroma-key source with the built-in image tool**

Call built-in image generation with these reference paths:

```text
Assets/Game/Art/Characters/Player/FennyGolden_IdlePoses_Candidate_v003.png
Assets/Game/Art/Characters/Player/FennyGolden_RunPoses_Candidate_v003.png
```

Use this prompt:

```text
Use case: stylized-concept
Asset type: 2D side-view platformer airborne character Sprite
Primary request: Create one new right-facing airborne pose for the same Fenny character shown in the reference sheets.
Input images: Image 1 is the identity, proportions, outfit, hair, and rendering reference; Image 2 is the motion and side-view reference.
Subject: Preserve the same face, golden curled twin-tail hair, black mechanical hair ornaments, orange-red/black/white outfit, neon-green accents, mechanical forearm, weapon backpack, asymmetric legs, and tactical boots. Keep the red-stocking leg deeply bent and raised forward/up. Make the bare leg hang naturally downward with only a slight knee bend and relaxed downward toe. Torso leans forward slightly without curling into a ball.
Style/medium: clean non-pixel anime game Sprite matching the reference rendering and outline treatment.
Composition/framing: full body, strict side view facing right, centered, generous padding, no cropping.
Scene/backdrop: perfectly flat solid #ff00ff chroma-key background for removal.
Constraints: one character only; one pose only; preserve costume asymmetry and equipment; crisp separated silhouette.
Avoid: no floor, no shadow, no contact shadow, no effects, no text, no watermark, no extra weapons, no duplicated limbs, no #ff00ff in the character.
```

Save the tool result to:

```text
Temp/ImageGen/FennyGolden_Airborne_Candidate_v004_chroma.png
```

- [x] **Step 2: Remove chroma key and crop to the alpha bounds**

Use the bundled Python runtime:

```text
C:\Users\TUDOU\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe
```

Run:

```powershell
& 'C:\Users\TUDOU\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' `
  'C:\Users\TUDOU\.codex\skills\.system\imagegen\scripts\remove_chroma_key.py' `
  --input 'Temp/ImageGen/FennyGolden_Airborne_Candidate_v004_chroma.png' `
  --out 'Temp/ImageGen/FennyGolden_Airborne_Candidate_v004_alpha.png' `
  --auto-key border --soft-matte --transparent-threshold 12 `
  --opaque-threshold 220 --despill --force
```

Crop the non-transparent bounds with 12 pixels of padding and save the project asset:

```powershell
@'
from pathlib import Path
from PIL import Image

source = Path(r"Temp/ImageGen/FennyGolden_Airborne_Candidate_v004_alpha.png")
target = Path(r"Assets/Game/Art/Characters/Player/FennyGolden_Airborne_Candidate_v004.png")
image = Image.open(source).convert("RGBA")
bounds = image.getchannel("A").getbbox()
if bounds is None:
    raise SystemExit("airborne source contains no opaque pixels")
left, top, right, bottom = bounds
padding = 12
left = max(0, left - padding)
top = max(0, top - padding)
right = min(image.width, right + padding)
bottom = min(image.height, bottom + padding)
target.parent.mkdir(parents=True, exist_ok=True)
image.crop((left, top, right, bottom)).save(target)
'@ | & 'C:\Users\TUDOU\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' -
```

- [x] **Step 3: Inspect the result before integration**

Use `view_image` on the final project PNG. Verify the red-stocking leg is deeply bent, the bare leg hangs naturally with a slight bend, identity/outfit are preserved, all limbs are singular, corners are transparent, and no magenta fringe is visible. If only a thin fringe remains, repeat chroma removal once with `--edge-contract 1`; do not switch to CLI/native transparency without user confirmation.

---

### Task 3: Configure the airborne import and stable prefab calibration

**Files:**
- Modify: `Assets/Game/Scripts/Presentation/Runtime/PlayerSpriteAnimator2D.cs`
- Modify: `Assets/Game/Scripts/Infrastructure/Editor/ArtIntegrationConfigurator.cs`
- Modify: `Assets/Game/Prefabs/Player/Player.prefab`
- Modify: `Assets/Game/Art/Characters/Player/FennyGolden_Airborne_Candidate_v004.png.meta`
- Test: `Assets/Game/Tests/EditMode/Infrastructure/ArtIntegrationConfiguratorTests.cs`

**Interfaces:**
- Consumes: v004 airborne PNG and eight existing Run frames.
- Produces: serialized `visualRoot`, `baseVisualLocalPosition`, `runFrameOffsets`, and `airborneOffset` fields.

- [x] **Step 1: Add the presentation calibration fields and validation**

Add these serialized fields to `PlayerSpriteAnimator2D`:

```csharp
[SerializeField] private Transform visualRoot;
[SerializeField] private Vector3 baseVisualLocalPosition = new(0f, -0.95f, 0f);
[SerializeField] private Vector2[] runFrameOffsets;
[SerializeField] private Vector2 airborneOffset;
```

Extend `Awake` validation with:

```csharp
visualRoot == null || runFrameOffsets == null ||
runFrameOffsets.Length != runFrames.Length
```

Keep behavior unchanged in this task except assigning:

```csharp
visualRoot.localPosition = baseVisualLocalPosition;
```

- [x] **Step 2: Add deterministic airborne import configuration**

Add to `ArtIntegrationConfigurator`:

```csharp
private const string FennyAirbornePath =
    "Assets/Game/Art/Characters/Player/FennyGolden_Airborne_Candidate_v004.png";

private static readonly Vector2[] RunFrameOffsets =
{
    new(0.009259f, 0f),
    new(-0.046296f, 0f),
    new(-0.159722f, 0f),
    new(-0.328704f, 0f),
    new(-0.041667f, 0f),
    new(-0.106481f, 0f),
    new(-0.215278f, 0f),
    new(-0.349537f, 0f)
};
```

Call `ConfigureAirborneImporter()` immediately after the initial synchronous refresh and before `ConfigurePlayer()`:

```csharp
private static void ConfigureAirborneImporter()
{
    Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(FennyAirbornePath);
    TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(FennyAirbornePath);
    importer.textureType = TextureImporterType.Sprite;
    importer.spriteImportMode = SpriteImportMode.Single;
    importer.spritePixelsPerUnit = texture.height / 1.91f;
    importer.mipmapEnabled = false;
    importer.alphaIsTransparency = true;
    importer.filterMode = FilterMode.Bilinear;

    TextureImporterSettings settings = new();
    importer.ReadTextureSettings(settings);
    settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
    settings.spritePivot = new Vector2(0.5f, 0f);
    settings.spriteMeshType = SpriteMeshType.FullRect;
    importer.SetTextureSettings(settings);
    importer.SaveAndReimport();
}
```

- [x] **Step 3: Serialize the new calibration into the Player prefab**

In `ConfigurePlayer()` load:

```csharp
Sprite airborne = AssetDatabase.LoadAssetAtPath<Sprite>(FennyAirbornePath);
```

Set the Visual transform and serialized adapter properties:

```csharp
visual.localPosition = new Vector3(0f, -0.95f, 0f);
serialized.FindProperty("visualRoot").objectReferenceValue = visual;
serialized.FindProperty("baseVisualLocalPosition").vector3Value = visual.localPosition;
AssignVectors(serialized.FindProperty("runFrameOffsets"), RunFrameOffsets);
serialized.FindProperty("airborneOffset").vector2Value = Vector2.zero;
serialized.FindProperty("airborneFrame").objectReferenceValue = airborne;
serialized.FindProperty("runFramesPerSecond").floatValue = 16f;
```

Add:

```csharp
private static void AssignVectors(SerializedProperty property, Vector2[] values)
{
    property.arraySize = values.Length;
    for (int index = 0; index < values.Length; index++)
        property.GetArrayElementAtIndex(index).vector2Value = values[index];
}
```

- [x] **Step 4: Run the configurator and verify the focused EditMode test GREEN**

Run:

```powershell
& 'F:\unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -quit `
  -projectPath 'F:\UnityProjects\Platformer2D' `
  -executeMethod SnowbreakFan.Infrastructure.Editor.ArtIntegrationConfigurator.Configure `
  -logFile 'F:\UnityProjects\Platformer2D\Temp\FennyPolishConfigure.log'
```

Then rerun the Task 1 focused test to `TestResults/FennyPolishTask3Green.xml`. Expected: one passed test, airborne height `1.91 ± 0.03`, eight exact offsets, 16 FPS, and Visual Y `-0.95`.

---

### Task 4: Apply mirrored per-frame corrections at runtime

**Files:**
- Modify: `Assets/Game/Tests/PlayMode/VerticalSliceSmokeTests.cs`
- Modify: `Assets/Game/Scripts/Presentation/Runtime/PlayerSpriteAnimator2D.cs`

**Interfaces:**
- Consumes: current frame index, last facing direction, serialized offsets, Player motion state.
- Produces: `ApplyFrame(Sprite sprite, Vector2 offset)` presentation behavior.

- [x] **Step 1: Extend the PlayMode test to verify RED**

In `FennyPresentationCyclesRunAndPreservesFacingAtIdle()`, after the existing rightward Run wait add:

Add this import if absent:

```csharp
using UnityEngine.TestTools.Utils;
```

```csharp
Assert.That(renderer.transform.localPosition.x, Is.LessThan(-0.1f));
```

After switching to negative velocity and yielding one frame add:

```csharp
Assert.That(renderer.transform.localPosition.x, Is.GreaterThan(0.1f));
```

After returning to Idle add:

```csharp
Assert.That(renderer.transform.localPosition,
    Is.EqualTo(new Vector3(0f, -0.95f, 0f))
        .Using(Vector3ComparerWithEqualsOperator.Instance));
```

Finally force the motor presentation state and assert the dedicated frame:

```csharp
typeof(PlayerMotor2D).GetProperty(nameof(PlayerMotor2D.State))
    .GetSetMethod(true)
    .Invoke(motor, new object[] { PlayerMotionState.Rising });
body.linearVelocity = new Vector2(0f, 5f);
yield return null;
Assert.That(renderer.sprite.name, Does.StartWith("FennyGolden_Airborne_Candidate_v004"));
```

Run only this PlayMode test to `TestResults/FennyPolishTask4Red.xml`. Expected: failure because the current component never applies Run frame offsets.

- [x] **Step 2: Implement the minimal frame application helper**

Add:

```csharp
private void ApplyFrame(Sprite sprite, Vector2 offset)
{
    targetRenderer.sprite = sprite;
    float offsetX = facingRight ? offset.x : -offset.x;
    visualRoot.localPosition = baseVisualLocalPosition +
        new Vector3(offsetX, offset.y, 0f);
}
```

Replace direct Sprite assignments:

```csharp
ApplyFrame(airborneFrame, airborneOffset);
```

and:

```csharp
ApplyFrame(frames[frameIndex], running ? runFrameOffsets[frameIndex] : Vector2.zero);
```

In `Awake`, replace the initial direct assignment with:

```csharp
ApplyFrame(idleFrames[0], Vector2.zero);
```

- [x] **Step 3: Verify runtime GREEN and presentation regression**

Rerun the focused PlayMode test to `TestResults/FennyPolishTask4Green.xml`, then run the entire `VerticalSliceSmokeTests` fixture. Expected: dedicated airborne frame, mirrored correction, Idle reset, physics assertions, and all fixture tests pass.

- [x] **Step 4: Commit the character polish milestone**

```powershell
git add -- 'Assets/Game/Art/Characters/Player/FennyGolden_Airborne_Candidate_v004.png' `
  'Assets/Game/Art/Characters/Player/FennyGolden_Airborne_Candidate_v004.png.meta' `
  'Assets/Game/Prefabs/Player/Player.prefab' `
  'Assets/Game/Scripts/Presentation/Runtime/PlayerSpriteAnimator2D.cs' `
  'Assets/Game/Scripts/Infrastructure/Editor/ArtIntegrationConfigurator.cs' `
  'Assets/Game/Tests/EditMode/Infrastructure/ArtIntegrationConfiguratorTests.cs' `
  'Assets/Game/Tests/PlayMode/VerticalSliceSmokeTests.cs'
git commit -m "fix: polish Fenny presentation"
```

---

### Task 5: Full verification, build, and delivery

**Files:**
- Modify only if a scoped verification failure requires correction.

**Interfaces:**
- Consumes: complete presentation polish implementation.
- Produces: verified test XML, Windows Development Build, clean Git state, synchronized remote main.

- [ ] **Step 1: Run the complete automated suites**

Run all EditMode tests to `TestResults/FennyPolishFinalEditMode.xml` and all PlayMode tests to `TestResults/FennyPolishFinalPlayMode.xml`. Parse both `<test-run>` elements and require `failed="0"`.

- [ ] **Step 2: Audit asset/meta pairing**

Run the existing PowerShell audit over `Assets`. Expected:

```text
MISSING_META=0
ORPHAN_META=0
```

- [ ] **Step 3: Build Windows x64**

```powershell
& 'F:\unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -quit `
  -projectPath 'F:\UnityProjects\Platformer2D' `
  -executeMethod SnowbreakFan.Infrastructure.Editor.BuildScripts.BuildWindows64 `
  -logFile 'F:\UnityProjects\Platformer2D\Temp\FennyPolishFinalBuild.log'
```

Expected log: `Build Finished, Result: Success.` Restore only the known Unity build-time serialization changes in DefaultVolumeProfile, UniversalRP, UniversalRenderPipelineGlobalSettings, ProjectSettings, and UnityConnectSettings with `apply_patch`.

- [ ] **Step 4: Final diff and push**

Confirm `git diff --check` for non-Unity YAML, a clean worktree, and `HEAD` containing the design, plan, and implementation commits. Push `main` to `origin`; do not create a release.

## Self-Review Result

- Spec coverage: floor contact, eight-frame correction, mirrored facing, 16 FPS, dedicated airborne pose, error isolation, physics preservation, automated verification, manual visual acceptance, build, and delivery are all mapped.
- Placeholder scan: no TBD, TODO, deferred implementation marker, undefined output path, or unspecified test command remains.
- Type consistency: `visualRoot`, `baseVisualLocalPosition`, `runFrameOffsets`, `airborneOffset`, `RunFrameOffsets`, `AssignVectors`, and `ApplyFrame` are identical across tests, configurator, runtime code, and commands.
