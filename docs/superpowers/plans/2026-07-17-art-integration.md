# First Level Art Integration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Integrate the approved frozen-zone backgrounds, platform skins, and temporary Fenny Idle/Run frame animation into the complete first-level vertical slice without changing physics or level geometry.

**Architecture:** Configure existing textures and platform prefabs through one repeatable editor configurator. Add two focused presentation components: `ParallaxLayer2D` owns three-segment camera-relative background looping, while `PlayerSpriteAnimator2D` translates existing motor state and Rigidbody velocity into temporary whole-frame sprites. Existing gameplay components remain the authority for movement, collision, checkpoints, collectibles, and completion.

**Tech Stack:** Unity 6.3 LTS, C#, URP 2D Renderer, Unity Test Framework, SpriteRenderer, UnityEditor prefab/scene APIs.

## Global Constraints

- Execute inline in the current main workspace; do not dispatch subagents.
- Do not modify `PlayerMovementConfig`, Rigidbody2D physics values, player collider dimensions, platform collider dimensions, or any level transform.
- Runtime code uses explicit serialized references and never searches gameplay objects by name.
- Platform renderer bounds must match collider bounds within `0.01` world units.
- Only runtime assets below `Assets/Game/Art/` may be referenced by the scene and prefabs.
- Fenny uses temporary Idle/Run whole-frame animation; do not create a Sprite Skin or claim the parts master is production-rig ready.
- No remote push or release occurs until all tests and the Windows x64 build pass.

---

### Task 1: Configure texture geometry and platform prefabs

**Files:**
- Create: `Assets/Game/Tests/EditMode/Infrastructure/ArtIntegrationConfiguratorTests.cs`
- Create: `Assets/Game/Scripts/Infrastructure/Editor/ArtIntegrationConfigurator.cs`
- Modify: `Assets/Game/Art/Characters/Player/FennyGolden_IdlePoses_Candidate_v003.png.meta`
- Modify: `Assets/Game/Art/Characters/Player/FennyGolden_RunPoses_Candidate_v003.png.meta`
- Modify: `Assets/Game/Art/Environments/Backgrounds/Background_Sky_v001.png.meta`
- Modify: `Assets/Game/Art/Environments/Backgrounds/Background_Far_v001.png.meta`
- Modify: `Assets/Game/Art/Environments/Backgrounds/Background_Mid_v001.png.meta`
- Modify: `Assets/Game/Art/Environments/Backgrounds/Background_Near_v001.png.meta`
- Modify: `Assets/Game/Art/Environments/Terrain/Platform_Short_v001.png.meta`
- Modify: `Assets/Game/Art/Environments/Terrain/Platform_Medium_v001.png.meta`
- Modify: `Assets/Game/Prefabs/Gameplay/Platform_Short.prefab`
- Modify: `Assets/Game/Prefabs/Gameplay/Platform_Medium.prefab`
- Modify: `Assets/Game/Prefabs/Gameplay/Platform_Long.prefab`

**Interfaces:**
- Consumes: the ten committed PNG candidates and the three existing platform prefabs.
- Produces: `ArtIntegrationConfigurator.Configure()` and platform prefabs whose visuals use the expected candidate Sprites.

- [x] **Step 1: Write the failing platform/import test**

Create `ArtIntegrationConfiguratorTests.cs` with tests that load each prefab through `PrefabUtility.LoadPrefabContents`, then assert:

```csharp
[TestCase("Platform_Short", "Platform_Short_v001.png", 3f)]
[TestCase("Platform_Medium", "Platform_Medium_v001.png", 6f)]
[TestCase("Platform_Long", "Platform_Long_v001.png", 12f)]
public void PlatformPrefabUsesCandidateSpriteAndPreservesColliderSize(
    string prefabName, string textureName, float expectedWidth)
{
    string prefabPath = $"Assets/Game/Prefabs/Gameplay/{prefabName}.prefab";
    GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
    try
    {
        BoxCollider2D collider = root.GetComponent<BoxCollider2D>();
        SpriteRenderer renderer = root.GetComponentInChildren<SpriteRenderer>();
        Assert.That(AssetDatabase.GetAssetPath(renderer.sprite), Does.EndWith(textureName));
        Assert.That(renderer.size, Is.EqualTo(new Vector2(expectedWidth, 1f)));
        Assert.That(renderer.color, Is.EqualTo(Color.white));
        Assert.That(renderer.sortingLayerName, Is.EqualTo("Terrain"));
    }
    finally { PrefabUtility.UnloadPrefabContents(root); }
}

[Test]
public void FennyFrameSheetsUseBottomCenterPivotsAndComparableWorldHeight()
{
    Sprite[] idle = LoadSprites("Assets/Game/Art/Characters/Player/FennyGolden_IdlePoses_Candidate_v003.png");
    Sprite[] run = LoadSprites("Assets/Game/Art/Characters/Player/FennyGolden_RunPoses_Candidate_v003.png");
    Assert.That(idle, Has.Length.EqualTo(4));
    Assert.That(run, Has.Length.EqualTo(8));
    Assert.That(idle.All(sprite => sprite.pivot.y <= 0.01f), Is.True);
    Assert.That(run.All(sprite => sprite.pivot.y <= 0.01f), Is.True);
    Assert.That(idle.Average(sprite => sprite.bounds.size.y), Is.EqualTo(1.91f).Within(0.08f));
    Assert.That(run.Average(sprite => sprite.bounds.size.y), Is.EqualTo(1.91f).Within(0.08f));
}
```

`LoadSprites` returns `AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(sprite => sprite.name).ToArray()`.

- [x] **Step 2: Run the focused EditMode fixture and verify RED**

Run Unity with `-runTests -testPlatform EditMode -testFilter SnowbreakFan.Infrastructure.Tests.ArtIntegrationConfiguratorTests`.

Expected: platform tests report built-in sprite paths and orange/blue tint; Fenny pivot/height test reports center pivots and mismatched world heights.

- [x] **Step 3: Apply minimal importer configuration**

Edit the committed metadata so:

- Idle sheet `spritePixelsToUnits` is `425`; all four entries use `alignment: 7` and `pivot: {x: 0.5, y: 0}`.
- Run sheet `spritePixelsToUnits` is `216`; all eight entries use `alignment: 7` and `pivot: {x: 0.5, y: 0}`.
- All four backgrounds use `spriteMode: 1`, `spritePixelsToUnits: 100`, center pivot, no mipmaps, and alpha transparency where present.
- Short platform keeps one Sprite rect `{x: 669, y: 215, width: 831, height: 277}`.
- Medium platform keeps one Sprite rect `{x: 273, y: 181, width: 1632, height: 272}`.
- Long platform keeps its existing `{x: 34, y: 514, width: 1307, height: 102}` rect.

Create `ArtIntegrationConfigurator.Configure()` with constants for the three prefab and texture paths. For each platform it must load prefab contents, load the first Sprite from the texture, assign `renderer.sprite`, set `renderer.color = Color.white`, `renderer.drawMode = SpriteDrawMode.Sliced`, `renderer.size = collider.size`, reset the Visual transform to identity, set sorting layer `Terrain`, save, and unload the prefab.

- [x] **Step 4: Run the editor configurator and verify GREEN**

Run Unity batch mode with `-executeMethod SnowbreakFan.Infrastructure.Editor.ArtIntegrationConfigurator.Configure`, then rerun the focused fixture.

Expected: all platform/import tests pass and existing `EveryPlatformVisualMatchesItsCollisionBounds` remains green.

- [x] **Step 5: Commit the importer and platform milestone**

```bash
git add Assets/Game/Art Assets/Game/Prefabs/Gameplay Assets/Game/Scripts/Infrastructure/Editor/ArtIntegrationConfigurator.cs Assets/Game/Tests/EditMode/Infrastructure/ArtIntegrationConfiguratorTests.cs
git commit -m "art: integrate first level platform skins"
```

---

### Task 2: Create the four-layer background hierarchy

**Files:**
- Create: `Assets/Game/Scripts/Presentation/Runtime/ParallaxLayer2D.cs`
- Modify: `Assets/Game/Scripts/Infrastructure/Editor/Game.Infrastructure.Editor.asmdef`
- Modify: `Assets/Game/Scripts/Infrastructure/Editor/ArtIntegrationConfigurator.cs`
- Modify: `Assets/Game/Scenes/10_Level_Prototype.unity`
- Modify: `Assets/Game/Tests/PlayMode/LevelStructureTests.cs`

**Interfaces:**
- Consumes: explicit Main Camera Transform, four background Sprites, sorting layers, and the level scene.
- Produces: `EnvironmentVisuals` with four `ParallaxLayer2D` children, each containing exactly three SpriteRenderer segments.

- [x] **Step 1: Write the failing scene-structure test**

Add `PrototypeContainsConfiguredFourLayerBackground` to `LevelStructureTests` without referencing the not-yet-created component type:

```csharp
[UnityTest]
public IEnumerator PrototypeContainsConfiguredFourLayerBackground()
{
    yield return SceneManager.LoadSceneAsync("10_Level_Prototype", LoadSceneMode.Single);
    GameObject root = GameObject.Find("EnvironmentVisuals");
    Assert.That(root, Is.Not.Null);
    string[] names = { "Background_Sky", "Background_Far", "Background_Mid", "Background_Near" };
    foreach (string name in names)
    {
        Transform layer = root.transform.Find(name);
        Assert.That(layer, Is.Not.Null, name);
        Assert.That(layer.GetComponent("ParallaxLayer2D"), Is.Not.Null, name);
        Assert.That(layer.GetComponentsInChildren<SpriteRenderer>(), Has.Length.EqualTo(3), name);
        Assert.That(layer.GetComponentsInChildren<Collider2D>(), Is.Empty, name);
    }
}
```

- [x] **Step 2: Run the focused PlayMode test and verify RED**

Expected: `EnvironmentVisuals` is null.

- [x] **Step 3: Add the minimal component and scene configuration**

Create `ParallaxLayer2D` with serialized fields `cameraTransform`, `segments`, `horizontalFollow`, `verticalFollow`, `tileWidth`, and `overlap`. Its initial `LateUpdate` may only validate fields; movement is implemented in Task 3.

Extend the editor asmdef references with `Game.Presentation` and `Game.Player`. Extend `ArtIntegrationConfigurator.Configure()` to open `10_Level_Prototype`, recreate `EnvironmentVisuals`, find the Main Camera during editor configuration, and create four layers with these settings:

| Layer | Scale | H Follow | V Follow | Sorting | Order | Color |
|---|---:|---:|---:|---|---:|---|
| Sky | 1.5 | 1.00 | 1.00 | BackgroundFar | -100 | white |
| Far | 2.0 | 0.88 | 0.85 | BackgroundFar | 0 | `(0.78,0.86,0.95,0.95)` |
| Mid | 2.0 | 0.68 | 0.65 | BackgroundMid | 0 | `(0.72,0.80,0.88,0.88)` |
| Near | 1.8 | 0.45 | 0.45 | BackgroundNear | 0 | `(0.48,0.60,0.72,0.52)` |

Each segment receives the same layer Sprite, local positions `(-tileWidth,0)`, `(0,0)`, `(tileWidth,0)`, no collider, and the layer scale shown above. Serialize the camera and segment references explicitly and save the scene.

- [x] **Step 4: Run configurator and verify structure GREEN**

Run the configurator, then the focused PlayMode test. Expected: all four layers exist with three segments and no colliders.

---

### Task 3: Implement and verify parallax loop behavior

**Files:**
- Modify: `Assets/Game/Scripts/Presentation/Runtime/ParallaxLayer2D.cs`
- Modify: `Assets/Game/Tests/PlayMode/LevelStructureTests.cs`

**Interfaces:**
- Consumes: camera world position, layer origin, follow factors, three segment transforms, tile width, overlap.
- Produces: stable layer-root follow and three segment centers surrounding the camera in layer-local space.

- [x] **Step 1: Write the failing movement/coverage test**

After the component type exists, add a UnityTest that loads the scene, selects `Background_Far`, records its root and segment positions, moves Main Camera `80` units in X, yields one frame, then asserts:

```csharp
Assert.That(layer.transform.position.x, Is.EqualTo(originX + 80f * 0.88f).Within(0.05f));
float cameraLocalX = layer.transform.InverseTransformPoint(camera.transform.position).x;
float[] centers = layer.GetComponentsInChildren<SpriteRenderer>()
    .Select(renderer => renderer.transform.localPosition.x)
    .OrderBy(value => value)
    .ToArray();
Assert.That(centers[0], Is.LessThan(cameraLocalX));
Assert.That(centers[2], Is.GreaterThan(cameraLocalX));
```

Expected RED: the minimal component leaves the layer and segments unchanged.

- [x] **Step 2: Implement the minimal LateUpdate loop**

`Awake` validates all references, stores `origin = transform.position`, and stores each segment's original local Y/Z. `LateUpdate` must:

```csharp
Vector3 cameraPosition = cameraTransform.position;
transform.position = new Vector3(
    origin.x + (cameraPosition.x - origin.x) * horizontalFollow,
    origin.y + (cameraPosition.y - origin.y) * verticalFollow,
    origin.z);

float stride = Mathf.Max(0.01f, tileWidth - overlap);
float cameraLocalX = transform.InverseTransformPoint(cameraPosition).x;
float center = Mathf.Round(cameraLocalX / stride) * stride;
for (int i = 0; i < segments.Length; i++)
{
    Vector3 local = segments[i].transform.localPosition;
    local.x = center + (i - 1) * stride;
    segments[i].transform.localPosition = local;
}
```

Validation requires exactly three non-null segments, a non-null camera, and positive tile width. Invalid configuration logs one error and disables only this component.

- [x] **Step 3: Verify parallax GREEN and full level fixture**

Run the focused movement test, then the full `LevelStructureTests`. Expected: all prior route and collision tests remain green.

- [x] **Step 4: Commit the environment milestone**

```bash
git add Assets/Game/Scripts/Presentation Assets/Game/Scripts/Infrastructure/Editor Assets/Game/Scenes/10_Level_Prototype.unity Assets/Game/Tests/PlayMode/LevelStructureTests.cs
git commit -m "feat: add looping frozen-zone parallax"
```

---

### Task 4: Attach the temporary Fenny presentation adapter

**Files:**
- Create: `Assets/Game/Scripts/Presentation/Runtime/PlayerSpriteAnimator2D.cs`
- Modify: `Assets/Game/Scripts/Infrastructure/Editor/ArtIntegrationConfigurator.cs`
- Modify: `Assets/Game/Prefabs/Player/Player.prefab`
- Modify: `Assets/Game/Tests/PlayMode/VerticalSliceSmokeTests.cs`

**Interfaces:**
- Consumes: Player Visual SpriteRenderer, Rigidbody2D, PlayerMotor2D, four Idle Sprites, eight Run Sprites, and one airborne Sprite.
- Produces: configured player prefab with a presentation-only adapter and no physics changes.

- [ ] **Step 1: Write the failing player-art configuration test**

Add a test that loads the level and asserts by component name so it compiles before the type exists:

```csharp
[UnityTest]
public IEnumerator PlayerUsesConfiguredFennyPresentationWithoutPhysicsChanges()
{
    yield return SceneManager.LoadSceneAsync("10_Level_Prototype", LoadSceneMode.Single);
    PlayerMotor2D motor = Object.FindFirstObjectByType<PlayerMotor2D>();
    Rigidbody2D body = motor.GetComponent<Rigidbody2D>();
    CapsuleCollider2D collider = motor.GetComponent<CapsuleCollider2D>();
    SpriteRenderer renderer = motor.GetComponentInChildren<SpriteRenderer>();
    Assert.That(motor.GetComponent("PlayerSpriteAnimator2D"), Is.Not.Null);
    Assert.That(renderer.sprite.name, Does.StartWith("FennyGolden_IdlePoses"));
    Assert.That(renderer.sortingLayerName, Is.EqualTo("Player"));
    Assert.That(body.gravityScale, Is.EqualTo(4f));
    Assert.That(collider.size, Is.EqualTo(new Vector2(0.8f, 1.8f)));
}
```

Expected RED: adapter is missing and the renderer uses the built-in placeholder sprite.

- [ ] **Step 2: Add the minimal adapter and configure the prefab**

Create `PlayerSpriteAnimator2D` with serialized fields:

```csharp
[SerializeField] private SpriteRenderer targetRenderer;
[SerializeField] private Rigidbody2D body;
[SerializeField] private PlayerMotor2D motor;
[SerializeField] private Sprite[] idleFrames;
[SerializeField] private Sprite[] runFrames;
[SerializeField] private Sprite airborneFrame;
[SerializeField] private float idleFramesPerSecond = 4f;
[SerializeField] private float runFramesPerSecond = 12f;
[SerializeField] private float movementThreshold = 0.1f;
```

At this step `Awake` only validates references and assigns `idleFrames[0]`; animation behavior is added after its failing test in Task 5.

Extend the editor configurator to load the Player prefab, reset `Visual` scale to one, set local position `(0,-0.9,0)`, assign Idle frame 0, white tint, Simple draw mode, Player sorting layer, add/configure `PlayerSpriteAnimator2D` on the root, and serialize the four Idle and eight Run frames in name order. Set `airborneFrame` to Run frame index 3. Save the prefab without changing Rigidbody2D, CapsuleCollider2D, GroundProbe, PlayerMotor2D, or PlayerMovementConfig.

- [ ] **Step 3: Run configurator and verify configuration GREEN**

Run the configurator, then the focused player configuration test. Expected: Fenny is assigned and all physics assertions remain unchanged.

---

### Task 5: Implement Idle, Run, airborne hold, and facing

**Files:**
- Modify: `Assets/Game/Scripts/Presentation/Runtime/PlayerSpriteAnimator2D.cs`
- Modify: `Assets/Game/Tests/PlayMode/VerticalSliceSmokeTests.cs`

**Interfaces:**
- Consumes: `Time.deltaTime`, `body.linearVelocity.x`, and `motor.State`.
- Produces: deterministic frame cycling, stable airborne hold, and SpriteRenderer horizontal flip.

- [ ] **Step 1: Write the failing runtime animation test**

Load the scene, obtain the player adapter by concrete type, disable `PlayerMotor2D`, set horizontal velocity to `4`, wait `0.2` seconds, and assert the Sprite changed to a Run frame. Then set velocity to `-4`, yield a frame and assert `flipX` is true. Finally set velocity to zero, wait `0.3` seconds and assert the Sprite name starts with `FennyGolden_IdlePoses`.

Expected RED: the minimal adapter remains on Idle frame 0 and never flips.

- [ ] **Step 2: Implement minimal animation state**

Add `elapsed`, `wasRunning`, and `facingRight` fields. `Update` must:

1. Update `facingRight` only when `abs(velocityX) > movementThreshold` and set `targetRenderer.flipX = !facingRight`.
2. If `motor.State` is Rising or Falling, assign `airborneFrame`, reset `elapsed`, and return.
3. Select Run only when grounded and above the movement threshold; otherwise select Idle.
4. Reset elapsed on Idle/Run transition; increment elapsed by `Time.deltaTime`.
5. Assign `frames[Mathf.FloorToInt(elapsed * fps) % frames.Length]`.

The component validates non-empty frame arrays and positive FPS in `Awake`; invalid setup logs one error and disables only itself.

- [ ] **Step 3: Verify animation GREEN and presentation regression**

Run the focused runtime animation test and all PlayMode tests. Expected: temporary Idle/Run/facing works and gameplay tests remain green.

- [ ] **Step 4: Commit the character milestone**

```bash
git add Assets/Game/Scripts/Presentation Assets/Game/Scripts/Infrastructure/Editor/ArtIntegrationConfigurator.cs Assets/Game/Prefabs/Player/Player.prefab Assets/Game/Tests/PlayMode/VerticalSliceSmokeTests.cs
git commit -m "feat: add temporary Fenny sprite animation"
```

---

### Task 6: Full verification, visual inspection, build, and delivery

**Files:**
- Modify only if verification exposes a scoped defect in files already listed above.

**Interfaces:**
- Consumes: complete integrated project.
- Produces: verified Windows x64 build and synchronized Git history.

- [ ] **Step 1: Run full automated verification**

Run all EditMode tests, all PlayMode tests, and parse the generated XML. Expected: zero failures. Run an Assets meta pairing audit and expect `ORPHAN_META=0`, `MISSING_META=0`.

- [ ] **Step 2: Build Windows x64**

Run `SnowbreakFan.Infrastructure.Editor.BuildScripts.BuildWindows64`. Expected log: `Build Finished, Result: Success.` Restore only Unity's known unrelated build-time serialization changes using `apply_patch`.

- [ ] **Step 3: Inspect the integrated frame**

Launch the correct prototype entry in Play Mode or use a Development Build screenshot. Verify: no gray exposure, platform art/collision alignment, Fenny foot baseline, Idle/Run/facing changes, background ordering, and Near-layer gameplay readability. If GUI automation is unavailable, report that manual visual verification remains for the user while retaining automated structural evidence.

- [ ] **Step 4: Final diff and push**

Run `git status`, `git diff --check` for non-Unity-generated text, and confirm only intended integration files remain. Commit any final scoped correction, push `main` to `origin`, and do not create a release.

## Self-Review Result

- Spec coverage: platform skins, four background layers, looping parallax, temporary Idle/Run/facing, error isolation, automated regression, build, and manual visual acceptance are all mapped to tasks.
- Placeholder scan: no placeholder markers, deferred implementation instruction, or undefined signature remains.
- Type consistency: `ParallaxLayer2D`, `PlayerSpriteAnimator2D`, and `ArtIntegrationConfigurator.Configure()` names are identical across production, tests, configuration, and commands.
