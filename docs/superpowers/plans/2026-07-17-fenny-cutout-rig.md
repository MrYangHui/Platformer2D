# Fenny Cutout Rig Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace Fenny's inconsistent whole-frame Run animation with a stable rigid cutout rig that has continuous Idle, Run, and Airborne motion and a grounded sole line 0.1 world units below the collider bottom.

**Architecture:** A versioned 21-part transparent atlas feeds an idempotent editor builder. The builder slices semantic Sprites, creates a rigid joint hierarchy plus Animator assets, and installs a small runtime state driver on the existing Player prefab; gameplay physics remain untouched.

**Tech Stack:** Unity 6000.3.19f1, Unity 2D Animation 13.0.5, Unity Animator/AnimationClip, C#, NUnit/Unity Test Framework, built-in image generation, bundled Python 3 with Pillow, Git.

## Global Constraints

- Work inline in `F:\UnityProjects\Platformer2D`; do not create or dispatch subagents.
- Do not change `Rigidbody2D`, `CapsuleCollider2D`, `GroundProbe2D`, `PlayerMotor2D`, `PlayerMovementConfig`, input, camera, or level geometry.
- Keep every rendered body-part local scale exactly `(1, 1, 1)`; the visual root may use X `-1` only for left-facing mirroring.
- Use a grounded sole contact line of local Y `-1.0`; the collider bottom remains local Y `-0.9`.
- Preserve the approved Airborne pose: red-stocking leg raised with a deep bend; bare leg hanging with a slight bend.
- Keep the v003 frame sheets, v004 airborne Sprite, and v004 parts master as rollback/reference assets.
- Do not create a release.

---

## File Structure

- Create `Assets/Game/Art/Characters/Player/FennyGolden_RigParts_v005.png`: normalized 7-by-3, 21-part production atlas.
- Create `Tools/Art/normalize_fenny_rig_parts.py`: deterministic chroma-free atlas normalizer and cell validator.
- Create `Assets/Game/Scripts/Infrastructure/Editor/FennyRigBuilder.cs`: atlas import, slicing, rig prefab, clips, controller, and Player installation.
- Create `Assets/Game/Scripts/Presentation/Runtime/PlayerRigPresentation2D.cs`: runtime state/facing driver only.
- Create `Assets/Game/Prefabs/Player/FennyVisualRig.prefab`: generated rigid part/bone hierarchy.
- Create `Assets/Game/Animations/Player/Fenny_Idle.anim`: generated looped grounded idle.
- Create `Assets/Game/Animations/Player/Fenny_Run.anim`: generated 0.55-second run cycle.
- Create `Assets/Game/Animations/Player/Fenny_Airborne.anim`: generated airborne pose.
- Create `Assets/Game/Animations/Player/Fenny_Rig.controller`: generated state machine.
- Create `Assets/Game/Tests/EditMode/Infrastructure/FennyRigBuilderTests.cs`: source, rig, animation, and idempotence contracts.
- Modify `Assets/Game/Scripts/Infrastructure/Editor/ArtIntegrationConfigurator.cs`: delegate Player presentation construction to `FennyRigBuilder`.
- Modify `Assets/Game/Tests/EditMode/Infrastructure/ArtIntegrationConfiguratorTests.cs`: replace obsolete frame-offset assertions with rig integration and physics-preservation assertions.
- Modify `Assets/Game/Tests/PlayMode/VerticalSliceSmokeTests.cs`: verify rig state, mirroring, stable scale, airborne pose, and landing.
- Modify `Assets/Game/Prefabs/Player/Player.prefab`: generated rig instance and `PlayerRigPresentation2D` references.
- Keep `Assets/Game/Scripts/Presentation/Runtime/PlayerSpriteAnimator2D.cs` unreferenced as rollback code during user validation.

---

### Task 1: Lock the v005 art-source contract and generate the normalized parts atlas

**Files:**
- Create: `Assets/Game/Tests/EditMode/Infrastructure/FennyRigBuilderTests.cs`
- Create: `Tools/Art/normalize_fenny_rig_parts.py`
- Create: `Assets/Game/Art/Characters/Player/FennyGolden_RigParts_v005.png`
- Unity creates: `Assets/Game/Art/Characters/Player/FennyGolden_RigParts_v005.png.meta`

**Interfaces:**
- Consumes: v003 Idle/Run, v004 Airborne, and v004 PartsMaster visual references.
- Produces: one RGBA `1792x1152` atlas containing exactly 21 occupied `256x384` cells in semantic order.

- [ ] **Step 1: Write the failing source-asset test**

Create `FennyRigBuilderTests.cs` with this first contract:

```csharp
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SnowbreakFan.Infrastructure.Tests
{
    public sealed class FennyRigBuilderTests
    {
        private const string PartsPath =
            "Assets/Game/Art/Characters/Player/FennyGolden_RigParts_v005.png";

        [Test]
        public void RigPartsMasterIsNormalizedTransparentAtlas()
        {
            Assert.That(File.Exists(PartsPath), Is.True);
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(PartsPath);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(PartsPath);
            Assert.That(texture, Is.Not.Null);
            Assert.That(texture.width, Is.EqualTo(1792));
            Assert.That(texture.height, Is.EqualTo(1152));
            Assert.That(importer.alphaIsTransparency, Is.True);
        }
    }
}
```

- [ ] **Step 2: Run the focused EditMode test and verify RED**

Run Unity without `-quit`:

```powershell
& 'F:\unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode `
  -projectPath 'F:\UnityProjects\Platformer2D' -runTests -testPlatform EditMode `
  -testFilter 'SnowbreakFan.Infrastructure.Tests.FennyRigBuilderTests.RigPartsMasterIsNormalizedTransparentAtlas' `
  -testResults 'F:\UnityProjects\Platformer2D\TestResults\FennyRigArtRed.xml' `
  -logFile 'F:\UnityProjects\Platformer2D\Temp\FennyRigArtRed.log'
```

Expected: one failure because `FennyGolden_RigParts_v005.png` does not exist.

- [ ] **Step 3: Generate the source plate with the image tool**

Use the built-in image generator with the four existing Fenny reference paths and this prompt:

```text
Use case: stylized-concept
Asset type: production 2D rigid-cutout character parts atlas for a side-view platformer
Primary request: Draw exactly twenty-one isolated parts for the same Fenny character in the references, all from one strict right-facing side-view camera and at one consistent anatomical scale. Arrange them in exactly seven columns and three rows on a perfectly flat solid #ff00ff background. Keep every cell separated with generous empty space and never overlap neighboring cells.
Row 1, left to right: head with front hair; rear hair mass; near ponytail upper segment; near ponytail lower segment; far ponytail upper segment; far ponytail lower segment; torso.
Row 2, left to right: pelvis/waist; front skirt panel; rear skirt panel; backpack and weapon; near upper arm; near forearm with hand; far upper arm.
Row 3, left to right: far forearm with hand; red-stocking thigh; red-stocking shin; red-stocking boot; bare thigh; bare shin; bare boot.
Identity: preserve the same face, golden curled twin tails, black mechanical hair ornaments, orange-red/black/white outfit, neon-green accents, mechanical forearm details, weapon backpack, asymmetric red-stocking and bare legs, and tactical boots.
Construction: limb pieces are straight neutral pieces with circular overlap hidden beneath adjacent joints; split cleanly at shoulder, elbow, hip, knee, ankle, and ponytail bend. Preserve painted outlines and surface detail. Parts must be true relative sizes, not icon-sized samples.
Constraints: exactly 21 parts, exactly one part per cell, one consistent side view and lighting, no assembled character, no pose variants, no labels, no grid lines, no shadows, no floor, no text, no watermark, no extra objects, no duplicated parts, and no #ff00ff inside the painted parts.
```

Save the raw result to `Temp/ImageGen/FennyGolden_RigParts_v005_chroma.png`.

- [ ] **Step 4: Normalize the atlas deterministically**

Create `Tools/Art/normalize_fenny_rig_parts.py` with a `PART_NAMES` tuple in the exact row-major order above. The script must:

1. open the chroma-removed RGBA input;
2. resize it to `1792x1152` with Lanczos filtering;
3. split it into `256x384` cells;
4. reject a cell if its alpha bounds are empty or touch a cell edge;
5. reject visible pixels whose RGB distance from magenta is below 24;
6. save the verified atlas to the requested output path.

Expose this exact command interface:

```python
def main(input_path: str, output_path: str) -> None:
    """Normalize and validate the 21-cell Fenny rig atlas."""
```

Remove the chroma key first with:

```powershell
& 'C:\Users\TUDOU\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' `
  'C:\Users\TUDOU\.codex\skills\.system\imagegen\scripts\remove_chroma_key.py' `
  --input 'Temp/ImageGen/FennyGolden_RigParts_v005_chroma.png' `
  --out 'Temp/ImageGen/FennyGolden_RigParts_v005_alpha.png' `
  --auto-key border --soft-matte --transparent-threshold 12 `
  --opaque-threshold 220 --despill --force
```

Then run:

```powershell
& 'C:\Users\TUDOU\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' `
  'Tools/Art/normalize_fenny_rig_parts.py' `
  'Temp/ImageGen/FennyGolden_RigParts_v005_alpha.png' `
  'Assets/Game/Art/Characters/Player/FennyGolden_RigParts_v005.png'
```

- [ ] **Step 5: Visually inspect the atlas and rerun the focused test GREEN**

Inspect the final PNG at original resolution. Require all 21 cells, consistent strict-side-view rendering, no merged cells, correct asymmetric legs, intact alpha, and no magenta fringe. Re-run the Step 2 command to `TestResults/FennyRigArtGreen.xml`; expected one passed test.

- [ ] **Step 6: Commit the accepted art-source milestone**

```powershell
git add -- 'Assets/Game/Art/Characters/Player/FennyGolden_RigParts_v005.png' `
  'Assets/Game/Art/Characters/Player/FennyGolden_RigParts_v005.png.meta' `
  'Assets/Game/Tests/EditMode/Infrastructure/FennyRigBuilderTests.cs' `
  'Tools/Art/normalize_fenny_rig_parts.py'
git commit -m "art: add normalized Fenny rig parts"
```

---

### Task 2: Slice semantic Sprites and build the static rigid rig

**Files:**
- Modify: `Assets/Game/Tests/EditMode/Infrastructure/FennyRigBuilderTests.cs`
- Create: `Assets/Game/Scripts/Infrastructure/Editor/FennyRigBuilder.cs`
- Create: `Assets/Game/Prefabs/Player/FennyVisualRig.prefab`
- Modify: `Assets/Game/Art/Characters/Player/FennyGolden_RigParts_v005.png.meta`

**Interfaces:**
- Produces: `public static class FennyRigBuilder`, `public static void Build()`, `public static readonly string[] RequiredPartNames`, and a prefab whose root is `FennyVisualRig`.

- [ ] **Step 1: Add the failing semantic-rig test**

Add a test that calls `FennyRigBuilder.Build()`, loads all Sprite sub-assets, and requires this exact ordered set:

```csharp
string[] expected =
{
    "Head", "RearHair", "NearPonyUpper", "NearPonyLower",
    "FarPonyUpper", "FarPonyLower", "Torso", "Pelvis",
    "FrontSkirt", "RearSkirt", "Backpack", "NearUpperArm",
    "NearForearmHand", "FarUpperArm", "FarForearmHand",
    "RedThigh", "RedShin", "RedBoot", "BareThigh",
    "BareShin", "BareBoot"
};
```

Load `Assets/Game/Prefabs/Player/FennyVisualRig.prefab` and require the joint paths:

```text
Hip/Chest/Neck
Hip/NearThigh/NearShin/NearBoot
Hip/FarThigh/FarShin/FarBoot
Hip/Chest/NearUpperArm/NearForearm
Hip/Chest/FarUpperArm/FarForearm
Hip/Chest/Neck/NearPonyUpper/NearPonyLower
Hip/Chest/Neck/FarPonyUpper/FarPonyLower
```

Also assert root local position zero, all child local scales one, 21 SpriteRenderers, sorting layer `Player`, and the lowest grounded sole at Y `-1.0 +/- 0.02`.

- [ ] **Step 2: Run the focused test and verify RED**

Run `FennyRigBuilderTests` to `TestResults/FennyRigStaticRed.xml`. Expected: compile failure or failed assertion because `FennyRigBuilder` and the prefab do not exist.

- [ ] **Step 3: Implement deterministic atlas slicing**

Create `FennyRigBuilder.cs` with constants for the atlas, rig prefab, animation folder, Player prefab, `CellWidth = 256`, `CellHeight = 384`, `Columns = 7`, and `PixelsPerUnit = 512f`.

`ConfigureAtlas()` sets Sprite Multiple mode, `512` pixels per unit, bilinear filtering, no mipmaps, alpha transparency, FullRect meshes, and 21 `SpriteRect` entries through Unity's Sprite Editor Data Provider API. Exact alpha-edge pivots provide stable joint placement while the compact hierarchy spacing deliberately overlaps rigid limb pieces to conceal sockets. Use row-major cell coordinates and semantic names. Assign joint-oriented pivots:

- bottom-center `(0.5, 0.08)` for Head, Torso, and Pelvis;
- top-center `(0.5, 0.92)` for ponytail segments, skirts, arms, thighs, shins, and boots;
- center `(0.5, 0.5)` for RearHair and Backpack.

Use `importer.spritesheet = metadata; importer.SaveAndReimport();` and verify all 21 named Sprites load exactly once.

- [ ] **Step 4: Build the static hierarchy at scale one**

`BuildRigPrefab()` creates the hierarchy from Step 1 with these neutral joint anchors in world-local units:

```csharp
Hip             = new Vector2(0.00f, -0.15f);
Chest           = new Vector2(0.00f,  0.28f);
Neck            = new Vector2(0.02f,  0.26f);
NearThigh       = new Vector2(0.04f,  0.00f);
NearShin        = new Vector2(0.00f, -0.38f);
NearBoot        = new Vector2(0.00f, -0.37f);
FarThigh        = new Vector2(-0.04f, 0.00f);
FarShin         = new Vector2(0.00f, -0.38f);
FarBoot         = new Vector2(0.00f, -0.37f);
NearUpperArm    = new Vector2(0.03f,  0.20f);
NearForearm     = new Vector2(0.00f, -0.28f);
FarUpperArm     = new Vector2(-0.03f, 0.20f);
FarForearm      = new Vector2(0.00f, -0.28f);
```

Attach one SpriteRenderer for each semantic part, use explicit sorting orders `0-40`, and never set a child scale to anything except `Vector3.one`. Position boot renderers so both neutral sole bounds end at local Y `-1.0`; adjust renderer child position rather than bone scale.

- [ ] **Step 5: Run the builder and inspect the assembled neutral pose**

Run:

```powershell
& 'F:\unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -quit `
  -projectPath 'F:\UnityProjects\Platformer2D' `
  -executeMethod SnowbreakFan.Infrastructure.Editor.FennyRigBuilder.Build `
  -logFile 'F:\UnityProjects\Platformer2D\Temp\FennyRigStaticBuild.log'
```

Inspect the prefab or a rendered preview. Require a coherent 1.9-unit-tall side-view silhouette, concealed joints, correct costume layering, and both soles at the contact line. If an art part is malformed, repair/regenerate the atlas before authoring animation.

- [ ] **Step 6: Rerun the static-rig tests GREEN and commit**

Expected: all `FennyRigBuilderTests` pass and running `Build()` twice leaves exactly one of every joint and renderer.

```powershell
git add -- 'Assets/Game/Art/Characters/Player/FennyGolden_RigParts_v005.png.meta' `
  'Assets/Game/Scripts/Infrastructure/Editor/FennyRigBuilder.cs' `
  'Assets/Game/Prefabs/Player/FennyVisualRig.prefab' `
  'Assets/Game/Prefabs/Player/FennyVisualRig.prefab.meta' `
  'Assets/Game/Tests/EditMode/Infrastructure/FennyRigBuilderTests.cs'
git commit -m "feat: build Fenny cutout rig"
```

---

### Task 3: Add the runtime presentation driver

**Files:**
- Create: `Assets/Game/Scripts/Presentation/Runtime/PlayerRigPresentation2D.cs`
- Modify: `Assets/Game/Tests/PlayMode/VerticalSliceSmokeTests.cs`

**Interfaces:**
- Produces: `PlayerRigPresentation2D` with serialized `Animator animator`, `Transform visualRoot`, `Rigidbody2D body`, `PlayerMotor2D motor`, `float movementThreshold = 0.1f`, and `float referenceRunSpeed = 6f`.

- [ ] **Step 1: Replace the old PlayMode expectations with failing rig-driver assertions**

Update `PlayerUsesConfiguredFennyPresentationWithoutPhysicsChanges()` to require `PlayerRigPresentation2D`, a child Animator, 21 renderers on sorting layer `Player`, gravity 4, and collider size `(0.8, 1.8)`.

Update the movement test to set velocity to `+4`, `-4`, and zero and assert:

```csharp
Assert.That(animator.GetBool("IsRunning"), Is.True);
Assert.That(visualRoot.localScale.x, Is.EqualTo(1f));
Assert.That(visualRoot.localScale.x, Is.EqualTo(-1f));
Assert.That(animator.GetBool("IsRunning"), Is.False);
```

Require every descendant except `visualRoot` to retain local scale `Vector3.one` throughout the test.

- [ ] **Step 2: Run the focused PlayMode fixture and verify RED**

Run `VerticalSliceSmokeTests` to `TestResults/FennyRigDriverRed.xml`. Expected: failure because the Player prefab still uses `PlayerSpriteAnimator2D`.

- [ ] **Step 3: Implement the minimal driver**

Create `PlayerRigPresentation2D` with Animator hashes for `IsRunning`, `IsAirborne`, and `RunSpeed`. `Awake()` validates all references and positive thresholds; invalid setup logs one error and disables only itself.

`Update()` must:

```csharp
float speed = Mathf.Abs(body.linearVelocity.x);
if (speed > movementThreshold)
    facingRight = body.linearVelocity.x > 0f;

bool airborne = motor.State != PlayerMotionState.Grounded;
animator.SetBool(IsAirborneHash, airborne);
animator.SetBool(IsRunningHash, !airborne && speed > movementThreshold);
animator.SetFloat(RunSpeedHash, Mathf.Max(0.5f, speed / referenceRunSpeed));
visualRoot.localScale = new Vector3(facingRight ? 1f : -1f, 1f, 1f);
```

Do not write Player position, velocity, collider, motor state, or any descendant part scale.

- [ ] **Step 4: Compile and commit the driver milestone**

Run EditMode compilation/tests; the PlayMode test remains red until the generated controller and prefab integration tasks.

```powershell
git add -- 'Assets/Game/Scripts/Presentation/Runtime/PlayerRigPresentation2D.cs' `
  'Assets/Game/Scripts/Presentation/Runtime/PlayerRigPresentation2D.cs.meta' `
  'Assets/Game/Tests/PlayMode/VerticalSliceSmokeTests.cs'
git commit -m "feat: drive Fenny rig presentation"
```

---

### Task 4: Generate continuous Idle, Run, and Airborne animation assets

**Files:**
- Modify: `Assets/Game/Scripts/Infrastructure/Editor/FennyRigBuilder.cs`
- Modify: `Assets/Game/Tests/EditMode/Infrastructure/FennyRigBuilderTests.cs`
- Create: `Assets/Game/Animations/Player/Fenny_Idle.anim`
- Create: `Assets/Game/Animations/Player/Fenny_Run.anim`
- Create: `Assets/Game/Animations/Player/Fenny_Airborne.anim`
- Create: `Assets/Game/Animations/Player/Fenny_Rig.controller`

**Interfaces:**
- Produces: controller parameters `IsRunning: bool`, `IsAirborne: bool`, `RunSpeed: float`; states `Idle`, `Run`, `Airborne`.

- [ ] **Step 1: Add failing clip/controller tests**

Load all four generated assets. Assert Idle and Run loop, Run length `0.55 +/- 0.01`, controller parameter types, and state names. Inspect every `EditorCurveBinding`; fail if `propertyName` contains `m_LocalScale`. Require Run curves on Hip position, both thigh/shin rotations, both arm rotations, and both ponytail rotations.

- [ ] **Step 2: Run focused EditMode tests and verify RED**

Expected: missing animation/controller assets.

- [ ] **Step 3: Generate Idle and Airborne clips**

Add helper methods:

```csharp
private static AnimationClip CreateIdleClip();
private static AnimationClip CreateRunClip();
private static AnimationClip CreateAirborneClip();
private static void SetRotationCurve(AnimationClip clip, string path, params Keyframe[] keys);
private static void SetPositionCurve(AnimationClip clip, string path, char axis, params Keyframe[] keys);
```

Idle length is 2 seconds. Keep Hip Y at `-0.15`, animate Chest rotation between `-1` and `1` degrees, Head counter-rotation between `1` and `-1` degrees, and ponytails within 3 degrees.

Airborne is a one-second non-looping hold. Set torso lean to `-8` degrees, red thigh/shin to a deeply folded raised pose, bare thigh/shin to a downward pose with a slight knee bend, and ponytails/backpack trailing backward. It must use no scale curves.

- [ ] **Step 4: Generate the 0.55-second Run clip**

Use nine keys including the repeated loop endpoint at normalized phases `0`, `.125`, `.25`, `.375`, `.5`, `.625`, `.75`, `.875`, and `1`. Author contact/down/passing/up poses for each side with the opposite side offset by half a cycle. Use these bounds:

- Hip Y bob: `-0.15` to `-0.20` only.
- Chest lean: `-6` to `-10` degrees.
- Thigh rotations: within `-42` to `42` degrees.
- Knee rotations: within `0` to `72` degrees.
- Upper-arm counter-swing: within `-30` to `30` degrees.
- Forearm bend: within `20` to `65` degrees.
- Ponytail follow-through: within `-8` to `12` degrees.

Use smooth tangents for transform motion and constant tangents only for SpriteRenderer sorting-order swaps at phases `0` and `.5`. Do not animate horizontal root translation.

- [ ] **Step 5: Generate the Animator Controller**

Create the parameters and state transitions:

- Idle -> Run: `IsRunning == true`, duration `0.08`.
- Run -> Idle: `IsRunning == false`, duration `0.08`.
- Idle/Run -> Airborne: `IsAirborne == true`, duration `0.05`.
- Airborne -> Idle: `IsAirborne == false` and `IsRunning == false`, duration `0.06`.
- Airborne -> Run: `IsAirborne == false` and `IsRunning == true`, duration `0.06`.

Set the Run state's speed parameter to `RunSpeed`; leave Idle and Airborne at speed 1.

- [ ] **Step 6: Run animation tests GREEN and inspect sampled poses**

Sample the rig prefab at Idle time 0, Run times `0`, `.1375`, `.275`, `.4125`, Airborne time 0, and left-facing Run. Inspect screenshots/previews for stable anatomy, correct limb crossover, no joint gaps, and no whole-body scale pulse.

- [ ] **Step 7: Commit the animation milestone**

```powershell
git add -- 'Assets/Game/Scripts/Infrastructure/Editor/FennyRigBuilder.cs' `
  'Assets/Game/Tests/EditMode/Infrastructure/FennyRigBuilderTests.cs' `
  'Assets/Game/Animations/Player'
git commit -m "feat: animate Fenny cutout rig"
```

---

### Task 5: Install the rig on Player and retire whole-frame playback from the prefab

**Files:**
- Modify: `Assets/Game/Scripts/Infrastructure/Editor/FennyRigBuilder.cs`
- Modify: `Assets/Game/Scripts/Infrastructure/Editor/ArtIntegrationConfigurator.cs`
- Modify: `Assets/Game/Tests/EditMode/Infrastructure/ArtIntegrationConfiguratorTests.cs`
- Modify: `Assets/Game/Tests/PlayMode/VerticalSliceSmokeTests.cs`
- Modify: `Assets/Game/Prefabs/Player/Player.prefab`

**Interfaces:**
- Consumes: generated rig prefab/controller and `PlayerRigPresentation2D`.
- Produces: Player prefab with exactly one `FennyVisualRig` child, one `PlayerRigPresentation2D`, and no attached `PlayerSpriteAnimator2D`.

- [ ] **Step 1: Add failing integration and physics-preservation assertions**

Replace `FennyAirborneAndPrefabUseStablePresentationCalibration()` with `FennyRigIsInstalledWithoutPhysicsChanges()`. Require:

- Player has one `PlayerRigPresentation2D` and no attached `PlayerSpriteAnimator2D`;
- child `FennyVisualRig` has local position zero and Animator controller `Fenny_Rig.controller`;
- Rigidbody2D gravity scale 4;
- CapsuleCollider2D size `(0.8, 1.8)` and offset zero;
- GroundProbe local position `(0, -0.94, 0)` and size `(0.55, 0.12)`;
- `PlayerMovementConfig` retains MaxSpeed 6, JumpSpeed 13, GravityScale 4.

- [ ] **Step 2: Implement idempotent Player installation**

`InstallOnPlayer()` loads prefab contents, destroys the legacy `Visual` child only after the new rig prefab/controller load successfully, removes the attached `PlayerSpriteAnimator2D`, instantiates exactly one `FennyVisualRig`, and assigns all serialized references on `PlayerRigPresentation2D`. Save and unload in `finally`.

Update `ArtIntegrationConfigurator.Configure()` to call `FennyRigBuilder.Build()` after the synchronous asset refresh and remove its legacy Fenny importer/player configuration constants and methods.

- [ ] **Step 3: Run the configurator and focused tests GREEN**

Run the configurator, focused EditMode rig tests, and `VerticalSliceSmokeTests`. Expected: state parameters change, facing flips only the rig root, all rendered part scales stay one, Airborne uses the rig pose, and landing restores grounded state/contact.

- [ ] **Step 4: Commit integration**

```powershell
git add -- 'Assets/Game/Scripts/Infrastructure/Editor/FennyRigBuilder.cs' `
  'Assets/Game/Scripts/Infrastructure/Editor/ArtIntegrationConfigurator.cs' `
  'Assets/Game/Tests/EditMode/Infrastructure/ArtIntegrationConfiguratorTests.cs' `
  'Assets/Game/Tests/PlayMode/VerticalSliceSmokeTests.cs' `
  'Assets/Game/Prefabs/Player/Player.prefab'
git commit -m "feat: install Fenny cutout presentation"
```

---

### Task 6: Full verification, visual handoff, and delivery

**Files:**
- Create verification outputs only under `TestResults`, `Temp`, and the existing Windows build output.
- Modify scoped source only if a verification failure demonstrates a rig defect.

**Interfaces:**
- Produces: verified suites, clean asset/meta audit, successful Windows build, and user-facing Play Mode validation target.

- [ ] **Step 1: Run complete EditMode and PlayMode suites**

Write results to `TestResults/FennyRigFinalEditMode.xml` and `TestResults/FennyRigFinalPlayMode.xml`. Poll the Unity process, then require both `<test-run>` elements to report `failed="0"`.

- [ ] **Step 2: Audit assets and scale curves**

Require `MISSING_META=0`, `ORPHAN_META=0`, no animation curve path containing `m_LocalScale`, exactly 21 rig SpriteRenderers, and no attached `PlayerSpriteAnimator2D` on Player.

- [ ] **Step 3: Build Windows x64 Development**

```powershell
& 'F:\unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe' -batchmode -quit `
  -projectPath 'F:\UnityProjects\Platformer2D' `
  -executeMethod SnowbreakFan.Infrastructure.Editor.BuildScripts.BuildWindows64 `
  -logFile 'F:\UnityProjects\Platformer2D\Temp\FennyRigFinalBuild.log'
```

Require `Build Finished, Result: Success.` Restore only known Unity build-time serialization noise with `apply_patch`.

- [ ] **Step 4: Final Play Mode visual checklist**

Verify in the correct bootstrap/level entry:

- soles visibly overlap the raised platform top without the legs appearing buried;
- Idle has no hovering or body scaling;
- a full right/left Run loop has stable head/torso size and natural arm-leg opposition;
- planted feet do not jump vertically;
- jump retains the approved asymmetric leg intent;
- landing contains no root snap.

- [ ] **Step 5: Final diff, commit any verification record, and push**

Run `git diff --check`, confirm a clean worktree, and push `main` to the configured origin. Do not create a release.

## Self-Review Result

- Spec coverage: new art, semantic parts, rigid hierarchy, stable scale, natural three-state motion, limb crossover, contact overlap, runtime isolation, idempotent generation, rollback assets, automated tests, visual QA, and Windows build are all mapped to tasks.
- Placeholder scan: the plan contains no deferred marker or unspecified implementation step.
- Type consistency: `FennyRigBuilder.Build`, `RequiredPartNames`, `PlayerRigPresentation2D`, the three Animator parameters, state names, asset paths, 21 semantic part names, and contact-line values are consistent throughout.
