# Platformer2D Vertical Slice Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在一个全新的 Unity 6.3 LTS Universal 2D 项目中交付可启动、可移动跳跃、可收集、可坠落复活并可到达终点的 2～3 分钟灰盒切片。

**Architecture:** Bootstrap 以追加方式加载关卡和 UI；玩家输入、运动、关卡、收集与表现拆为独立程序集。静态玩法几何使用 Tilemap，独立平台和装饰使用 Prefab；所有玩法参数先由 ScriptableObject 和灰盒验证，美术随后替换。

**Tech Stack:** Unity 6.3 LTS、URP 2D Renderer、Input System、Cinemachine 3、2D Animation、2D Tilemap、NUnit/Unity Test Framework、C#。

## Global Constraints

- 新项目路径固定为 `F:\UnityProjects\Platformer2D`，目录名不得包含空格。
- 使用 Unity 6.3 LTS 最新可用补丁版和 Universal 2D 模板；不得从团结引擎工程原地升级。
- 首个平台为 Windows；输入资产同时包含键盘和手柄绑定。
- 不使用 ECS/DOTS、重型依赖注入、全局可写单例、对象名查找或万能 `GameManager`。
- 首版不实现战斗、移动平台、存档、多关卡、Addressables、Live2D 或最终美术。
- 运行时资源只进入 `Assets/Game/`；第三方资源进入 `Assets/ThirdParty/`；源图与生成记录进入项目根目录 `ArtSource/`。
- Git 仓库由用户在新项目创建后初始化；在用户完成初始化前不执行任何 commit。
- 每个任务完成后运行其指定测试；有 Git 仓库后按任务提交，禁止把多个独立任务压成一个提交。

---

## Planned File Map

```text
F:\UnityProjects\Platformer2D\
├── ArtSource\
│   ├── Generated\.gitkeep
│   ├── Provenance\.gitkeep
│   ├── References\.gitkeep
│   └── Working\.gitkeep
├── Assets\Game\
│   ├── Art\Characters\Player\
│   ├── Art\Environments\Backgrounds\
│   ├── Art\Environments\Terrain\
│   ├── Art\UI\
│   ├── Art\VFX\
│   ├── Data\Bootstrap\BootstrapSettings.asset
│   ├── Data\Player\PlayerMovementConfig.asset
│   ├── Data\RuntimeChannels\CollectibleCountChannel.asset
│   ├── Data\RuntimeChannels\LevelCompletedChannel.asset
│   ├── Input\Gameplay.inputactions
│   ├── Prefabs\Gameplay\Collectible_AnomalySample.prefab
│   ├── Prefabs\Gameplay\LevelEnd.prefab
│   ├── Prefabs\Gameplay\RespawnCheckpoint.prefab
│   ├── Prefabs\Player\Player.prefab
│   ├── Scenes\00_Bootstrap.unity
│   ├── Scenes\10_Level_Prototype.unity
│   ├── Scenes\20_UI_Gameplay.unity
│   ├── Scripts\Core\Runtime\IRespawnTarget.cs
│   ├── Scripts\Core\Runtime\SceneLoadPlan.cs
│   ├── Scripts\Core\Game.Core.asmdef
│   ├── Scripts\Infrastructure\Runtime\BootstrapSettings.cs
│   ├── Scripts\Infrastructure\Runtime\GameBootstrap.cs
│   ├── Scripts\Infrastructure\Editor\BuildScripts.cs
│   ├── Scripts\Infrastructure\Editor\Game.Infrastructure.Editor.asmdef
│   ├── Scripts\Infrastructure\Game.Infrastructure.asmdef
│   ├── Scripts\Player\Runtime\GroundProbe2D.cs
│   ├── Scripts\Player\Runtime\JumpIntentBuffer.cs
│   ├── Scripts\Player\Runtime\PlayerInputReader.cs
│   ├── Scripts\Player\Runtime\PlayerMotionState.cs
│   ├── Scripts\Player\Runtime\PlayerMotor2D.cs
│   ├── Scripts\Player\Runtime\PlayerMovementConfig.cs
│   ├── Scripts\Player\Runtime\PlayerMovementMath.cs
│   ├── Scripts\Player\Runtime\PlayerRespawnTarget.cs
│   ├── Scripts\Player\Game.Player.asmdef
│   ├── Scripts\Level\Runtime\Checkpoint2D.cs
│   ├── Scripts\Level\Runtime\FallKillZone2D.cs
│   ├── Scripts\Level\Runtime\LevelChunk2D.cs
│   ├── Scripts\Level\Runtime\LevelCompletedChannel.cs
│   ├── Scripts\Level\Runtime\LevelEnd2D.cs
│   ├── Scripts\Level\Runtime\RespawnPointStore.cs
│   ├── Scripts\Level\Runtime\RespawnService.cs
│   ├── Scripts\Level\Game.Level.asmdef
│   ├── Scripts\Collectibles\Runtime\Collectible2D.cs
│   ├── Scripts\Collectibles\Runtime\CollectibleCountChannel.cs
│   ├── Scripts\Collectibles\Runtime\LevelSessionController.cs
│   ├── Scripts\Collectibles\Runtime\LevelSessionState.cs
│   ├── Scripts\Collectibles\Game.Collectibles.asmdef
│   ├── Scripts\Presentation\Runtime\GameplayHud.cs
│   ├── Scripts\Presentation\Game.Presentation.asmdef
│   └── Tests\EditMode\...
├── docs\superpowers\specs\2026-07-16-platformer2d-vertical-slice-design.md
├── .gitattributes
└── .gitignore
```

---

### Task 1: Install Unity and create the clean Universal 2D project

**Files:**
- Create: `F:\UnityProjects\Platformer2D\ProjectSettings\ProjectVersion.txt`
- Create: `F:\UnityProjects\Platformer2D\Packages\manifest.json`

**Interfaces:**
- Consumes: Windows、联网权限、Unity 账号登录。
- Produces: 可由 Unity 6.3 LTS 打开的 Universal 2D 项目。

- [ ] **Step 1: Install Unity Hub because no Unity Hub or Unity Editor is currently installed**

Run in an elevated PowerShell only after approval:

```powershell
winget install --exact --id Unity.UnityHub --accept-package-agreements --accept-source-agreements
```

Expected: `Successfully installed` and `C:\Program Files\Unity Hub\Unity Hub.exe` exists.

- [ ] **Step 2: Install the current Unity 6.3 LTS patch in Hub**

Open Unity Hub, sign in, then choose `Installs > Install Editor > Official releases > Unity 6.3 LTS`. Install the newest `6000.3.*f1` patch with `Windows Build Support (IL2CPP)` and `Microsoft Visual Studio Community` unchecked if an IDE is already available.

Verification:

```powershell
$editor = Get-ChildItem 'C:\Program Files\Unity\Hub\Editor\6000.3.*\Editor\Unity.exe' |
  Sort-Object { $_.VersionInfo.FileVersionRaw } -Descending |
  Select-Object -First 1 -ExpandProperty FullName
& $editor -version
```

Expected: output starts with `6000.3.`.

- [ ] **Step 3: Create the project from the Universal 2D template**

In Hub choose `Projects > New project`, select the installed 6.3 editor, select `Universal 2D`, set Project name to `Platformer2D`, Location to `F:\UnityProjects`, disable Unity Cloud connection, and select `Create project`.

Expected: `F:\UnityProjects\Platformer2D\Assets`, `Packages`, and `ProjectSettings` exist; the project opens without a render-pipeline error.

- [ ] **Step 4: Verify the template and record the editor**

```powershell
Get-Content 'F:\UnityProjects\Platformer2D\ProjectSettings\ProjectVersion.txt'
Select-String -Path 'F:\UnityProjects\Platformer2D\Packages\manifest.json' -Pattern 'com.unity.render-pipelines.universal'
```

Expected: editor version starts with `6000.3.` and URP appears once in the manifest.

- [ ] **Step 5: Hand control to the user for Git initialization**

The user initializes the repository inside `F:\UnityProjects\Platformer2D`. Do not run `git init` on their behalf. Resume only after this succeeds:

```powershell
git -C 'F:\UnityProjects\Platformer2D' status --short
```

Expected: command succeeds without `not a git repository`.

---

### Task 2: Establish packages, project settings, folders, and assembly boundaries

**Files:**
- Create: `F:\UnityProjects\Platformer2D\.gitignore`
- Create: `F:\UnityProjects\Platformer2D\.gitattributes`
- Create: all directories in Planned File Map
- Create: six runtime `.asmdef` files and matching test `.asmdef` files
- Copy: approved design spec into `F:\UnityProjects\Platformer2D\docs\superpowers\specs\`

**Interfaces:**
- Consumes: Universal 2D project from Task 1.
- Produces: stable source layout and one-way assembly dependency graph.

- [ ] **Step 1: Install only the required packages**

In `Window > Package Manager > Unity Registry`, install Input System, Cinemachine, 2D Animation, 2D Tilemap Editor, 2D Tilemap Extras, and Test Framework. Keep the versions resolved as compatible by the installed 6000.3 editor; do not add preview packages.

In `Edit > Project Settings > Player > Other Settings`, set Active Input Handling to `Input System Package (New)` and restart the Editor when prompted.

Expected: Console has no package resolution error and the Package Manager lists Cinemachine `3.x`.

- [ ] **Step 2: Configure serialization and metadata**

Set `Edit > Project Settings > Editor > Version Control Mode` to `Visible Meta Files` and `Asset Serialization Mode` to `Force Text`.

Use this `.gitattributes`:

```gitattributes
* text=auto
*.cs text eol=lf
*.asmdef text eol=lf
*.json text eol=lf
*.md text eol=lf
*.unity merge=unityyamlmerge eol=lf
*.prefab merge=unityyamlmerge eol=lf
*.asset merge=unityyamlmerge eol=lf
*.png binary
*.psd binary
*.wav binary
*.ogg binary
```

Use this `.gitignore`; it deliberately does not ignore `.meta` files:

```gitignore
/[Ll]ibrary/
/[Tt]emp/
/[Oo]bj/
/[Ll]ogs/
/[Uu]ser[Ss]ettings/
/[Bb]uilds/
/TestResults/
*.csproj
*.sln
*.suo
*.tmp
*.user
*.userprefs
*.pidb
*.booproj
*.svd
*.pdb
*.mdb
*.opendb
*.VC.db
.vs/
.idea/
.vscode/
sysinfo.txt
```

- [ ] **Step 3: Create the runtime assembly definitions**

Create the following names and references:

```json
// Assets/Game/Scripts/Core/Game.Core.asmdef
{"name":"Game.Core","rootNamespace":"SnowbreakFan.Core","references":[],"autoReferenced":true}

// Assets/Game/Scripts/Player/Game.Player.asmdef
{"name":"Game.Player","rootNamespace":"SnowbreakFan.Player","references":["Game.Core","Unity.InputSystem"],"autoReferenced":true}

// Assets/Game/Scripts/Level/Game.Level.asmdef
{"name":"Game.Level","rootNamespace":"SnowbreakFan.Level","references":["Game.Core"],"autoReferenced":true}

// Assets/Game/Scripts/Collectibles/Game.Collectibles.asmdef
{"name":"Game.Collectibles","rootNamespace":"SnowbreakFan.Collectibles","references":["Game.Core"],"autoReferenced":true}

// Assets/Game/Scripts/Presentation/Game.Presentation.asmdef
{"name":"Game.Presentation","rootNamespace":"SnowbreakFan.Presentation","references":["Game.Player","Game.Level","Game.Collectibles","Unity.TextMeshPro"],"autoReferenced":true}

// Assets/Game/Scripts/Infrastructure/Game.Infrastructure.asmdef
{"name":"Game.Infrastructure","rootNamespace":"SnowbreakFan.Infrastructure","references":["Game.Core"],"autoReferenced":true}

// Assets/Game/Scripts/Infrastructure/Editor/Game.Infrastructure.Editor.asmdef
{"name":"Game.Infrastructure.Editor","rootNamespace":"SnowbreakFan.Infrastructure.Editor","references":["Game.Infrastructure"],"includePlatforms":["Editor"],"autoReferenced":true}
```

Create one EditMode test assembly per tested module. For example, the Player test assembly is:

```json
{"name":"Game.Player.Tests.EditMode","rootNamespace":"SnowbreakFan.Player.Tests","references":["Game.Player"],"includePlatforms":["Editor"],"optionalUnityReferences":["TestAssemblies"],"autoReferenced":false}
```

Create the Core, Level, and Collectibles variants by changing both `name`/`rootNamespace` and the single runtime reference to `Game.Core`, `Game.Level`, and `Game.Collectibles` respectively. Create one PlayMode test assembly at `Assets/Game/Tests/PlayMode/Game.Tests.PlayMode.asmdef`:

```json
{"name":"Game.Tests.PlayMode","rootNamespace":"SnowbreakFan.Tests.PlayMode","references":["Game.Infrastructure","Game.Player","Game.Level","Game.Collectibles","Game.Presentation"],"optionalUnityReferences":["TestAssemblies"],"autoReferenced":false}
```

- [ ] **Step 4: Copy the approved spec and create ArtSource keep files**

```powershell
Copy-Item -LiteralPath 'F:\unity\project\My project\docs\superpowers\specs\2026-07-16-platformer2d-vertical-slice-design.md' -Destination 'F:\UnityProjects\Platformer2D\docs\superpowers\specs\2026-07-16-platformer2d-vertical-slice-design.md'
```

Create empty `.gitkeep` files in the four `ArtSource` children. Do not copy generated previews into runtime `Assets`.

- [ ] **Step 5: Verify compilation and commit**

Expected: Unity finishes domain reload with zero compiler errors.

```powershell
git -C 'F:\UnityProjects\Platformer2D' add .
git -C 'F:\UnityProjects\Platformer2D' commit -m "chore: scaffold Unity 6.3 project"
```

---

### Task 3: Build and test the additive Bootstrap scene loader

**Files:**
- Create: `Assets/Game/Scripts/Core/Runtime/SceneLoadPlan.cs`
- Create: `Assets/Game/Scripts/Infrastructure/Runtime/BootstrapSettings.cs`
- Create: `Assets/Game/Scripts/Infrastructure/Runtime/GameBootstrap.cs`
- Test: `Assets/Game/Tests/EditMode/Core/SceneLoadPlanTests.cs`
- Create: `Assets/Game/Scenes/00_Bootstrap.unity`

**Interfaces:**
- Consumes: scene names `10_Level_Prototype` and `20_UI_Gameplay`.
- Produces: `SceneLoadPlan.Create(string, string) -> IReadOnlyList<string>` and Bootstrap additive loading.

- [ ] **Step 1: Write the failing scene-plan tests**

```csharp
using System;
using NUnit.Framework;
using SnowbreakFan.Core;

public sealed class SceneLoadPlanTests
{
    [Test]
    public void Create_ReturnsLevelThenUi()
    {
        CollectionAssert.AreEqual(
            new[] { "10_Level_Prototype", "20_UI_Gameplay" },
            SceneLoadPlan.Create("10_Level_Prototype", "20_UI_Gameplay"));
    }

    [TestCase("", "20_UI_Gameplay")]
    [TestCase("10_Level_Prototype", "")]
    [TestCase("Same", "Same")]
    public void Create_RejectsInvalidNames(string level, string ui)
    {
        Assert.Throws<ArgumentException>(() => SceneLoadPlan.Create(level, ui));
    }
}
```

- [ ] **Step 2: Run the test and verify the expected failure**

Run EditMode tests from Test Runner.

Expected: compile failure because `SceneLoadPlan` does not exist.

- [ ] **Step 3: Implement the scene plan and loader**

```csharp
// SceneLoadPlan.cs
using System;
using System.Collections.Generic;

namespace SnowbreakFan.Core
{
    public static class SceneLoadPlan
    {
        public static IReadOnlyList<string> Create(string levelScene, string uiScene)
        {
            if (string.IsNullOrWhiteSpace(levelScene))
                throw new ArgumentException("Level scene name is required.", nameof(levelScene));
            if (string.IsNullOrWhiteSpace(uiScene))
                throw new ArgumentException("UI scene name is required.", nameof(uiScene));
            if (string.Equals(levelScene, uiScene, StringComparison.Ordinal))
                throw new ArgumentException("Level and UI scenes must be different.");
            return new[] { levelScene, uiScene };
        }
    }
}
```

```csharp
// BootstrapSettings.cs
using System.Collections.Generic;
using SnowbreakFan.Core;
using UnityEngine;

namespace SnowbreakFan.Infrastructure
{
    [CreateAssetMenu(menuName = "Game/Bootstrap Settings")]
    public sealed class BootstrapSettings : ScriptableObject
    {
        [SerializeField] private string levelScene = "10_Level_Prototype";
        [SerializeField] private string uiScene = "20_UI_Gameplay";
        public IReadOnlyList<string> BuildLoadPlan() => SceneLoadPlan.Create(levelScene, uiScene);
    }
}
```

```csharp
// GameBootstrap.cs
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SnowbreakFan.Infrastructure
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private BootstrapSettings settings;
        private static bool activeInstance;
        private bool ownsActiveFlag;

        private void Awake()
        {
            if (activeInstance) { Destroy(gameObject); return; }
            activeInstance = true;
            ownsActiveFlag = true;
            DontDestroyOnLoad(gameObject);
        }

        private IEnumerator Start()
        {
            if (settings == null)
            {
                Debug.LogError("GameBootstrap requires BootstrapSettings.", this);
                yield break;
            }

            foreach (string sceneName in settings.BuildLoadPlan())
            {
                if (SceneUtility.GetBuildIndexByScenePath($"Assets/Game/Scenes/{sceneName}.unity") < 0)
                {
                    Debug.LogError($"Scene is not enabled in Build Profiles: {sceneName}", this);
                    yield break;
                }

                if (!SceneManager.GetSceneByName(sceneName).isLoaded)
                    yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            }
        }

        private void OnDestroy() { if (ownsActiveFlag) activeInstance = false; }
    }
}
```

- [ ] **Step 4: Create and wire the Bootstrap scene**

Create `BootstrapSettings.asset`; create `00_Bootstrap` containing one `Bootstrap` GameObject with `GameBootstrap`; assign the asset. Create empty `10_Level_Prototype` and `20_UI_Gameplay` scenes. Add all three to Build Profiles in that order.

- [ ] **Step 5: Run tests and smoke-test loading**

Expected: EditMode tests pass. Playing `00_Bootstrap` leaves three loaded scenes in the Hierarchy and produces no duplicate-load warning.

- [ ] **Step 6: Commit**

```powershell
git -C 'F:\UnityProjects\Platformer2D' add Assets/Game
git -C 'F:\UnityProjects\Platformer2D' commit -m "feat: add additive bootstrap flow"
```

---

### Task 4: Implement test-driven player movement and input buffering

**Files:**
- Create: Player runtime files listed in Planned File Map, excluding `PlayerRespawnTarget.cs`
- Test: `Assets/Game/Tests/EditMode/Player/JumpIntentBufferTests.cs`
- Test: `Assets/Game/Tests/EditMode/Player/PlayerMovementMathTests.cs`
- Create: `Assets/Game/Input/Gameplay.inputactions`
- Create: `Assets/Game/Data/Player/PlayerMovementConfig.asset`
- Create: `Assets/Game/Prefabs/Player/Player.prefab`

**Interfaces:**
- Consumes: Input Actions named `Gameplay/Move` and `Gameplay/Jump`.
- Produces: `PlayerMotor2D.State`, `PlayerMotor2D.ResetMotion()`, and a stable Rigidbody2D player root.

- [ ] **Step 1: Write failing timing and horizontal-motion tests**

```csharp
using NUnit.Framework;
using SnowbreakFan.Player;

public sealed class JumpIntentBufferTests
{
    [Test]
    public void ConsumesBufferedPressInsideCoyoteWindow()
    {
        var buffer = new JumpIntentBuffer();
        buffer.MarkGrounded(1.00f);
        buffer.PressJump(1.05f);
        Assert.That(buffer.TryConsume(1.08f, 0.10f, 0.12f), Is.True);
        Assert.That(buffer.TryConsume(1.08f, 0.10f, 0.12f), Is.False);
    }

    [Test]
    public void RejectsExpiredPress()
    {
        var buffer = new JumpIntentBuffer();
        buffer.MarkGrounded(1.00f);
        buffer.PressJump(1.01f);
        Assert.That(buffer.TryConsume(1.20f, 0.10f, 0.12f), Is.False);
    }
}

public sealed class PlayerMovementMathTests
{
    [Test]
    public void HorizontalVelocity_AcceleratesTowardTarget()
    {
        Assert.That(PlayerMovementMath.HorizontalVelocity(0f, 1f, 6f, 20f, 0.1f), Is.EqualTo(2f).Within(0.001f));
    }

    [Test]
    public void HorizontalVelocity_NeverExceedsMaxSpeed()
    {
        Assert.That(PlayerMovementMath.HorizontalVelocity(5.8f, 1f, 6f, 20f, 0.1f), Is.EqualTo(6f).Within(0.001f));
    }
}
```

- [ ] **Step 2: Run tests and confirm missing-type failures**

Expected: compile failure naming `JumpIntentBuffer` and `PlayerMovementMath`.

- [ ] **Step 3: Implement pure movement logic**

```csharp
// JumpIntentBuffer.cs
namespace SnowbreakFan.Player
{
    public sealed class JumpIntentBuffer
    {
        private float groundedAt = float.NegativeInfinity;
        private float pressedAt = float.NegativeInfinity;
        private bool consumed = true;

        public void MarkGrounded(float now) => groundedAt = now;
        public void PressJump(float now) { pressedAt = now; consumed = false; }

        public bool TryConsume(float now, float coyoteTime, float bufferTime)
        {
            if (consumed || now - groundedAt > coyoteTime || now - pressedAt > bufferTime)
                return false;
            consumed = true;
            return true;
        }

        public void Reset() { groundedAt = pressedAt = float.NegativeInfinity; consumed = true; }
    }
}
```

```csharp
// PlayerMovementMath.cs
using UnityEngine;

namespace SnowbreakFan.Player
{
    public static class PlayerMovementMath
    {
        public static float HorizontalVelocity(float current, float input, float maxSpeed, float acceleration, float deltaTime)
            => Mathf.MoveTowards(current, Mathf.Clamp(input, -1f, 1f) * maxSpeed, acceleration * deltaTime);
    }
}
```

- [ ] **Step 4: Implement configuration, input, ground probe, and motor**

```csharp
// PlayerMovementConfig.cs
using UnityEngine;

namespace SnowbreakFan.Player
{
    [CreateAssetMenu(menuName = "Game/Player Movement Config")]
    public sealed class PlayerMovementConfig : ScriptableObject
    {
        [field: SerializeField, Min(0f)] public float MaxSpeed { get; private set; } = 6f;
        [field: SerializeField, Min(0f)] public float GroundAcceleration { get; private set; } = 55f;
        [field: SerializeField, Min(0f)] public float GroundDeceleration { get; private set; } = 70f;
        [field: SerializeField, Min(0f)] public float AirAcceleration { get; private set; } = 30f;
        [field: SerializeField, Min(0f)] public float AirDeceleration { get; private set; } = 20f;
        [field: SerializeField, Min(0f)] public float JumpSpeed { get; private set; } = 13f;
        [field: SerializeField, Min(0f)] public float GravityScale { get; private set; } = 4f;
        [field: SerializeField, Min(1f)] public float FallGravityMultiplier { get; private set; } = 1.5f;
        [field: SerializeField, Range(0f, 1f)] public float JumpCutMultiplier { get; private set; } = 0.5f;
        [field: SerializeField, Min(0f)] public float CoyoteTime { get; private set; } = 0.10f;
        [field: SerializeField, Min(0f)] public float JumpBufferTime { get; private set; } = 0.12f;
    }
}
```

```csharp
// PlayerInputReader.cs
using UnityEngine;
using UnityEngine.InputSystem;

namespace SnowbreakFan.Player
{
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionReference move;
        [SerializeField] private InputActionReference jump;
        private bool pressed;
        private bool released;
        public float MoveX => move.action.ReadValue<Vector2>().x;

        private void OnEnable()
        {
            move.action.Enable(); jump.action.Enable();
            jump.action.performed += OnJumpPerformed;
            jump.action.canceled += OnJumpCanceled;
        }

        private void OnDisable()
        {
            jump.action.performed -= OnJumpPerformed;
            jump.action.canceled -= OnJumpCanceled;
            move.action.Disable(); jump.action.Disable();
        }

        private void OnJumpPerformed(InputAction.CallbackContext _) => pressed = true;
        private void OnJumpCanceled(InputAction.CallbackContext _) => released = true;
        public bool ConsumePressed() { bool value = pressed; pressed = false; return value; }
        public bool ConsumeReleased() { bool value = released; released = false; return value; }
    }
}
```

```csharp
// GroundProbe2D.cs
using UnityEngine;

namespace SnowbreakFan.Player
{
    public sealed class GroundProbe2D : MonoBehaviour
    {
        [SerializeField] private Vector2 size = new(0.55f, 0.12f);
        [SerializeField] private LayerMask groundMask;
        public bool IsGrounded => Physics2D.OverlapBox(transform.position, size, 0f, groundMask) != null;
        private void OnDrawGizmosSelected() { Gizmos.color = Color.cyan; Gizmos.DrawWireCube(transform.position, size); }
    }
}
```

```csharp
// PlayerMotionState.cs
namespace SnowbreakFan.Player { public enum PlayerMotionState { Grounded, Rising, Falling } }
```

```csharp
// PlayerMotor2D.cs
using System;
using UnityEngine;

namespace SnowbreakFan.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerMotor2D : MonoBehaviour
    {
        [SerializeField] private PlayerMovementConfig config;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private GroundProbe2D groundProbe;
        private readonly JumpIntentBuffer jumpBuffer = new();
        private Rigidbody2D body;
        private bool cutJump;
        public PlayerMotionState State { get; private set; }
        public event Action<PlayerMotionState> StateChanged;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            if (config == null || input == null || groundProbe == null)
            {
                Debug.LogError("PlayerMotor2D is missing required references.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (input.ConsumePressed()) jumpBuffer.PressJump(Time.time);
            if (input.ConsumeReleased()) cutJump = true;
        }

        private void FixedUpdate()
        {
            bool grounded = groundProbe.IsGrounded;
            if (grounded) jumpBuffer.MarkGrounded(Time.time);

            Vector2 velocity = body.linearVelocity;
            bool hasInput = Mathf.Abs(input.MoveX) > 0.01f;
            float acceleration = grounded
                ? (hasInput ? config.GroundAcceleration : config.GroundDeceleration)
                : (hasInput ? config.AirAcceleration : config.AirDeceleration);
            velocity.x = PlayerMovementMath.HorizontalVelocity(velocity.x, input.MoveX, config.MaxSpeed, acceleration, Time.fixedDeltaTime);

            if (jumpBuffer.TryConsume(Time.time, config.CoyoteTime, config.JumpBufferTime))
                velocity.y = config.JumpSpeed;
            if (cutJump && velocity.y > 0f)
                velocity.y *= config.JumpCutMultiplier;
            cutJump = false;

            body.gravityScale = config.GravityScale * (velocity.y < 0f ? config.FallGravityMultiplier : 1f);
            body.linearVelocity = velocity;
            SetState(grounded && velocity.y <= 0.01f ? PlayerMotionState.Grounded : velocity.y > 0f ? PlayerMotionState.Rising : PlayerMotionState.Falling);
        }

        public void ResetMotion() { jumpBuffer.Reset(); body.linearVelocity = Vector2.zero; body.gravityScale = config.GravityScale; }
        private void SetState(PlayerMotionState next) { if (State == next) return; State = next; StateChanged?.Invoke(next); }
    }
}
```

- [ ] **Step 5: Create actions and player prefab**

Create `Gameplay.inputactions` with map `Gameplay`: `Move` as Value/Vector2, bindings A/D 2D composite, arrow-key 2D composite, gamepad left stick, gamepad dpad; `Jump` as Button, bindings Space and gamepad south button.

Create Player prefab root on `Player` layer with Rigidbody2D (Dynamic, Freeze Rotation Z, Interpolate), CapsuleCollider2D, `PlayerInputReader`, `PlayerMotor2D`, and `PlayerRespawnTarget` added later. Add child `GroundProbe` just below the capsule and child `CameraTarget` at the torso. Use a simple orange rectangle SpriteRenderer as placeholder visual.

- [ ] **Step 6: Run tests and commit**

Expected: all movement EditMode tests pass; in a temporary floor scene the player accelerates, stops, jumps higher when held, jumps lower when released, and can jump briefly after leaving an edge.

```powershell
git -C 'F:\UnityProjects\Platformer2D' add Assets/Game
git -C 'F:\UnityProjects\Platformer2D' commit -m "feat: implement responsive player movement"
```

---

### Task 5: Add checkpoint and fall-respawn flow

**Files:**
- Create: `Assets/Game/Scripts/Core/Runtime/IRespawnTarget.cs`
- Create: Level respawn files from Planned File Map
- Create: `Assets/Game/Scripts/Player/Runtime/PlayerRespawnTarget.cs`
- Test: `Assets/Game/Tests/EditMode/Level/RespawnPointStoreTests.cs`

**Interfaces:**
- Consumes: `PlayerMotor2D.ResetMotion()`.
- Produces: `RespawnService.SetCheckpoint(Vector2)` and `RespawnService.Respawn()`.

- [ ] **Step 1: Write the failing store tests**

```csharp
using NUnit.Framework;
using SnowbreakFan.Level;
using UnityEngine;

public sealed class RespawnPointStoreTests
{
    [Test]
    public void UsesDefaultUntilCheckpointIsSet()
    {
        var store = new RespawnPointStore(new Vector2(2f, 3f));
        Assert.That(store.Current, Is.EqualTo(new Vector2(2f, 3f)));
        store.Set(new Vector2(10f, 4f));
        Assert.That(store.Current, Is.EqualTo(new Vector2(10f, 4f)));
    }
}
```

- [ ] **Step 2: Implement the store, contract, and components**

```csharp
// IRespawnTarget.cs
using UnityEngine;
namespace SnowbreakFan.Core { public interface IRespawnTarget { void RespawnAt(Vector2 position); } }

// RespawnPointStore.cs
using UnityEngine;
namespace SnowbreakFan.Level
{
    public sealed class RespawnPointStore
    {
        public RespawnPointStore(Vector2 defaultPoint) => Current = defaultPoint;
        public Vector2 Current { get; private set; }
        public void Set(Vector2 point) => Current = point;
    }
}
```

```csharp
// RespawnService.cs
using SnowbreakFan.Core;
using UnityEngine;

namespace SnowbreakFan.Level
{
    public sealed class RespawnService : MonoBehaviour
    {
        [SerializeField] private Transform defaultSpawn;
        [SerializeField] private MonoBehaviour targetBehaviour;
        private RespawnPointStore store;
        private IRespawnTarget Target => targetBehaviour as IRespawnTarget;
        private void Awake()
        {
            if (defaultSpawn == null || Target == null) { Debug.LogError("RespawnService is not configured.", this); enabled = false; return; }
            store = new RespawnPointStore(defaultSpawn.position);
        }
        public void SetCheckpoint(Vector2 point) => store.Set(point);
        public void Respawn() => Target.RespawnAt(store.Current);
    }
}
```

```csharp
// PlayerRespawnTarget.cs
using SnowbreakFan.Core;
using UnityEngine;

namespace SnowbreakFan.Player
{
public sealed class PlayerRespawnTarget : MonoBehaviour, IRespawnTarget
    {
        [SerializeField] private PlayerMotor2D motor;
        public void RespawnAt(Vector2 position) { transform.position = position; motor.ResetMotion(); }
    }
}
```

```csharp
// Checkpoint2D.cs
using UnityEngine;

namespace SnowbreakFan.Level
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class Checkpoint2D : MonoBehaviour
    {
        [SerializeField] private RespawnService respawnService;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private LayerMask playerMask;
        private bool activated;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (activated || (playerMask.value & (1 << other.gameObject.layer)) == 0) return;
            if (respawnService == null || spawnPoint == null) { Debug.LogError("Checkpoint2D is not configured.", this); return; }
            activated = true;
            respawnService.SetCheckpoint(spawnPoint.position);
        }
    }
}
```

```csharp
// FallKillZone2D.cs
using UnityEngine;

namespace SnowbreakFan.Level
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class FallKillZone2D : MonoBehaviour
    {
        [SerializeField] private RespawnService respawnService;
        [SerializeField] private LayerMask playerMask;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if ((playerMask.value & (1 << other.gameObject.layer)) == 0) return;
            if (respawnService == null) { Debug.LogError("FallKillZone2D is not configured.", this); return; }
            respawnService.Respawn();
        }
    }
}
```

Both trigger colliders must have `Is Trigger` enabled. Neither component uses tags or object names.

- [ ] **Step 3: Create prefabs and test**

Create `RespawnCheckpoint.prefab` with trigger collider and visible cyan placeholder. Create a full-width kill-zone trigger below the test floor. Assign Player layer masks and RespawnService references.

Expected: store test passes; entering checkpoint changes respawn position; falling resets player position and velocity without reloading the scene.

- [ ] **Step 4: Commit**

```powershell
git -C 'F:\UnityProjects\Platformer2D' add Assets/Game
git -C 'F:\UnityProjects\Platformer2D' commit -m "feat: add checkpoint respawn flow"
```

---

### Task 6: Add collectible session state, completion channel, and UI

**Files:**
- Create: Collectible runtime files from Planned File Map
- Create: `Assets/Game/Scripts/Level/Runtime/LevelCompletedChannel.cs`
- Create: `Assets/Game/Scripts/Level/Runtime/LevelEnd2D.cs`
- Create: `Assets/Game/Scripts/Presentation/Runtime/GameplayHud.cs`
- Test: `Assets/Game/Tests/EditMode/Collectibles/LevelSessionStateTests.cs`
- Create: collectible and end prefabs

**Interfaces:**
- Produces: `LevelSessionController.TryCollect(string) -> bool`, count channel `(collected,total)`, and completion event.

- [ ] **Step 1: Write failing session tests**

```csharp
using NUnit.Framework;
using SnowbreakFan.Collectibles;

public sealed class LevelSessionStateTests
{
    [Test]
    public void UniqueIdsCountOnce()
    {
        var state = new LevelSessionState(3);
        Assert.That(state.TryCollect("sample-a"), Is.True);
        Assert.That(state.TryCollect("sample-a"), Is.False);
        Assert.That(state.Collected, Is.EqualTo(1));
        Assert.That(state.IsComplete, Is.False);
    }

    [Test]
    public void AllSamplesCompleteCollectionGoal()
    {
        var state = new LevelSessionState(2);
        state.TryCollect("a"); state.TryCollect("b");
        Assert.That(state.IsComplete, Is.True);
    }
}
```

- [ ] **Step 2: Implement state and runtime channels**

```csharp
// LevelSessionState.cs
using System;
using System.Collections.Generic;

namespace SnowbreakFan.Collectibles
{
    public sealed class LevelSessionState
    {
        private readonly HashSet<string> ids = new();
        public LevelSessionState(int total) { if (total < 1) throw new ArgumentOutOfRangeException(nameof(total)); Total = total; }
        public int Total { get; }
        public int Collected => ids.Count;
        public bool IsComplete => Collected >= Total;
        public bool TryCollect(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Collectible id is required.", nameof(id));
            return ids.Add(id);
        }
    }
}
```

```csharp
// CollectibleCountChannel.cs
using System;
using UnityEngine;

namespace SnowbreakFan.Collectibles
{
    [CreateAssetMenu(menuName = "Game/Channels/Collectible Count")]
    public sealed class CollectibleCountChannel : ScriptableObject
    {
        public event Action<int, int> Changed;
        public int Current { get; private set; }
        public int Total { get; private set; }
        public void Publish(int current, int total) { Current = current; Total = total; Changed?.Invoke(current, total); }
    }
}
```

```csharp
// LevelSessionController.cs
using UnityEngine;

namespace SnowbreakFan.Collectibles
{
    public sealed class LevelSessionController : MonoBehaviour
    {
        [SerializeField, Min(1)] private int totalSamples = 3;
        [SerializeField] private CollectibleCountChannel channel;
        private LevelSessionState state;

        private void Awake()
        {
            if (channel == null) { Debug.LogError("LevelSessionController requires a count channel.", this); enabled = false; return; }
            state = new LevelSessionState(totalSamples);
            channel.Publish(0, totalSamples);
        }

        public bool TryCollect(string id)
        {
            if (state == null || !state.TryCollect(id)) return false;
            channel.Publish(state.Collected, state.Total);
            return true;
        }
    }
}
```

```csharp
// Collectible2D.cs
using UnityEngine;

namespace SnowbreakFan.Collectibles
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class Collectible2D : MonoBehaviour
    {
        [SerializeField] private string collectibleId;
        [SerializeField] private LevelSessionController session;
        [SerializeField] private GameObject visual;
        private Collider2D triggerCollider;

        private void Awake() => triggerCollider = GetComponent<Collider2D>();

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.layer != LayerMask.NameToLayer("Player")) return;
            if (session == null) { Debug.LogError("Collectible2D requires a LevelSessionController.", this); return; }
            if (!session.TryCollect(collectibleId)) return;
            triggerCollider.enabled = false;
            if (visual != null) visual.SetActive(false);
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(collectibleId)) Debug.LogError("Collectible2D needs a unique id.", this);
        }
    }
}
```

```csharp
// LevelCompletedChannel.cs
using System;
using UnityEngine;
namespace SnowbreakFan.Level
{
    [CreateAssetMenu(menuName = "Game/Channels/Level Completed")]
    public sealed class LevelCompletedChannel : ScriptableObject
    {
        public event Action Raised;
        public void Raise() => Raised?.Invoke();
    }
}
```

`LevelEnd2D` uses a Player LayerMask and raises the channel once. `GameplayHud` subscribes in `OnEnable`, unsubscribes in `OnDisable`, renders `样本 {current}/{total}` in a TMP text, and enables a `关卡完成` panel on completion.

```csharp
// LevelEnd2D.cs
using UnityEngine;

namespace SnowbreakFan.Level
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class LevelEnd2D : MonoBehaviour
    {
        [SerializeField] private LevelCompletedChannel channel;
        [SerializeField] private LayerMask playerMask;
        private bool completed;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (completed || (playerMask.value & (1 << other.gameObject.layer)) == 0) return;
            if (channel == null) { Debug.LogError("LevelEnd2D requires a completion channel.", this); return; }
            completed = true;
            channel.Raise();
        }
    }
}
```

```csharp
// GameplayHud.cs
using SnowbreakFan.Collectibles;
using SnowbreakFan.Level;
using TMPro;
using UnityEngine;

namespace SnowbreakFan.Presentation
{
    public sealed class GameplayHud : MonoBehaviour
    {
        [SerializeField] private CollectibleCountChannel countChannel;
        [SerializeField] private LevelCompletedChannel completedChannel;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private GameObject completedPanel;

        private void OnEnable()
        {
            countChannel.Changed += RenderCount;
            completedChannel.Raised += ShowCompleted;
            RenderCount(countChannel.Current, countChannel.Total);
            completedPanel.SetActive(false);
        }

        private void OnDisable()
        {
            countChannel.Changed -= RenderCount;
            completedChannel.Raised -= ShowCompleted;
        }

        private void RenderCount(int current, int total) => countText.text = $"样本 {current}/{total}";
        private void ShowCompleted() => completedPanel.SetActive(true);
    }
}
```

- [ ] **Step 3: Create three unique sample prefabs and UI scene**

Create one reusable cyan-diamond collectible prefab and place three instances with IDs `sample-01`, `sample-02`, `sample-03`. Create `20_UI_Gameplay` with Screen Space Overlay Canvas, count text at upper-left, and disabled completion panel centered.

- [ ] **Step 4: Run tests and commit**

Expected: state tests pass; a sample counts once; UI updates from the shared channel even though UI and level are separate additive scenes; endpoint shows completion panel once.

```powershell
git -C 'F:\UnityProjects\Platformer2D' add Assets/Game
git -C 'F:\UnityProjects\Platformer2D' commit -m "feat: add collectible level loop"
```

---

### Task 7: Build the hybrid Tilemap/Prefab prototype level and validate chunks

**Files:**
- Create: `Assets/Game/Scripts/Level/Runtime/LevelChunk2D.cs`
- Modify: `Assets/Game/Scenes/10_Level_Prototype.unity`
- Create: placeholder Terrain Tile Palette and static platform prefabs

**Interfaces:**
- Consumes: Player prefab, RespawnService, samples, endpoint.
- Produces: five validated Level Chunk roots and a traversable 2～3 minute route.

- [ ] **Step 1: Implement LevelChunk2D validation**

```csharp
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SnowbreakFan.Level
{
    public sealed class LevelChunk2D : MonoBehaviour
    {
        [SerializeField] private string chunkId;
        [SerializeField] private Tilemap gameplayTilemap;
        [SerializeField] private Tilemap terrainArtTilemap;
        [SerializeField] private Collider2D cameraBoundary;
        [SerializeField] private Transform defaultSpawn;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(chunkId)) Debug.LogError("LevelChunk2D needs a chunk id.", this);
            if (gameplayTilemap == null) Debug.LogError($"{chunkId}: missing gameplay Tilemap.", this);
            if (terrainArtTilemap == null) Debug.LogError($"{chunkId}: missing terrain art Tilemap.", this);
            if (cameraBoundary == null) Debug.LogError($"{chunkId}: missing camera boundary.", this);
            if (defaultSpawn == null) Debug.LogError($"{chunkId}: missing default spawn.", this);
        }
    }
}
```

- [ ] **Step 2: Configure physics and sorting layers**

Create Physics 2D layers `Player`, `Ground`, `OneWayPlatform`, `GameplayTrigger`; allow Player collisions with Ground and OneWayPlatform, and trigger overlap with GameplayTrigger. Create Sorting Layers in this order: `BackgroundFar`, `BackgroundMid`, `BackgroundNear`, `Terrain`, `Gameplay`, `Player`, `Foreground`, `UIWorld`.

For each Gameplay Tilemap use TilemapCollider2D + CompositeCollider2D + Static Rigidbody2D, with composite operation enabled. Terrain Art Tilemaps have no colliders.

- [ ] **Step 3: Build five chunk roots**

Create `Chunk_01_Tutorial`, `Chunk_02_Gaps`, `Chunk_03_Vertical`, `Chunk_04_Recovery`, and `Chunk_05_Final`. Each contains `Grid/Gameplay`, `Grid/TerrainArt`, `Platforms`, `Decorations`, `ParallaxAnchors`, `CameraBoundary`, and `DefaultSpawn`.

Greybox route requirements:

- Chunk 01: flat start, one short jump, sample-01, checkpoint.
- Chunk 02: three gaps of increasing width, one single-direction platform chain.
- Chunk 03: at least six platforms producing 10～14 world units of vertical gain, sample-02 midway, checkpoint at top.
- Chunk 04: descending route and a safe recovery floor, sample-03 visible before the final challenge.
- Chunk 05: mixed horizontal/vertical jumps, endpoint on stable ground.

Total horizontal extent starts at 280 world units. A first-time traversal measurement below 120 seconds requires extending Chunk 02 and Chunk 05 in 20-unit modules; a measurement over 180 seconds requires removing the last 20-unit module from Chunk 05. Record three first-time traversal runs and keep the median within 120～180 seconds.

- [ ] **Step 4: Validate collisions and commit**

Expected: no collider seam stops horizontal movement; every gap is recoverable or falls into kill zone; no decoration has a gameplay collider; all chunk validation messages are clear.

```powershell
git -C 'F:\UnityProjects\Platformer2D' add Assets/Game
git -C 'F:\UnityProjects\Platformer2D' commit -m "feat: build hybrid greybox level"
```

---

### Task 8: Configure Cinemachine 3 for long-level traversal

**Files:**
- Modify: `Assets/Game/Scenes/10_Level_Prototype.unity`
- Modify: `Assets/Game/Prefabs/Player/Player.prefab`

**Interfaces:**
- Consumes: Player `CameraTarget` child and chunk camera boundaries.
- Produces: dead-zone follow, horizontal look-ahead, vertical damping, and confinement.

- [ ] **Step 1: Create the Cinemachine camera**

In `10_Level_Prototype`, create `GameObject > Cinemachine > 2D Camera`. Set Tracking Target to Player/CameraTarget, Lens Projection to Orthographic, and initial Orthographic Size to `6.5`.

Use Position Composer with Screen Position `(0.50, 0.55)`, Dead Zone Size `(0.20, 0.15)`, Damping X `0.20`, Damping Y `0.35`, Lookahead Time `0.15`, and Lookahead Smoothing `0.10`. Disable inherited rotation.

- [ ] **Step 2: Add confinement**

Add Cinemachine Confiner 2D and assign a CompositeCollider2D boundary enclosing the complete prototype route. Chunk-specific switching remains a future extension; the first slice uses one combined boundary while each chunk still stores its own authoring boundary.

- [ ] **Step 3: Verify camera behavior**

Expected:

- Tiny ground movements inside the dead zone do not move the camera.
- Sustained right movement shows more space in front of the player without snapping.
- Short hops cause little vertical motion; the vertical section follows smoothly.
- Falling never shows outside the level boundary.
- Player visual bob does not affect the target because CameraTarget is under the stable physics root, not under the Sprite/Animator child.

- [ ] **Step 4: Commit**

```powershell
git -C 'F:\UnityProjects\Platformer2D' add Assets/Game
git -C 'F:\UnityProjects\Platformer2D' commit -m "feat: configure long-level camera"
```

---

### Task 9: Add automated build entry point and run final verification

**Files:**
- Create: `Assets/Game/Scripts/Infrastructure/Editor/BuildScripts.cs`
- Create: `Assets/Game/Tests/PlayMode/VerticalSliceSmokeTests.cs`
- Create: `Builds/Windows/Platformer2D.exe` during verification only
- Create: `TestResults/EditMode.xml` and `TestResults/PlayMode.xml` during verification only

**Interfaces:**
- Consumes: enabled scenes in Build Profiles.
- Produces: repeatable Windows x64 development build.

- [ ] **Step 1: Implement the build entry point**

```csharp
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace SnowbreakFan.Infrastructure.Editor
{
    public static class BuildScripts
    {
        public static void BuildWindows64()
        {
            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length != 3) throw new InvalidOperationException($"Expected 3 enabled scenes, found {scenes.Length}.");

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = "Builds/Windows/Platformer2D.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException($"Windows build failed: {report.summary.result}");
        }
    }
}
```

- [ ] **Step 2: Add a PlayMode additive-scene smoke test**

```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SnowbreakFan.Tests.PlayMode
{
    public sealed class VerticalSliceSmokeTests
    {
        [UnityTest]
        public IEnumerator BootstrapLoadsLevelAndUiExactlyOnce()
        {
            yield return SceneManager.LoadSceneAsync("00_Bootstrap", LoadSceneMode.Single);

            float timeout = 10f;
            while (timeout > 0f &&
                   (!SceneManager.GetSceneByName("10_Level_Prototype").isLoaded ||
                    !SceneManager.GetSceneByName("20_UI_Gameplay").isLoaded))
            {
                timeout -= UnityEngine.Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.That(SceneManager.GetSceneByName("10_Level_Prototype").isLoaded, Is.True);
            Assert.That(SceneManager.GetSceneByName("20_UI_Gameplay").isLoaded, Is.True);
            Assert.That(SceneManager.sceneCount, Is.EqualTo(3));
        }
    }
}
```

Expected in Test Runner: test passes and scene count equals three.

- [ ] **Step 3: Run EditMode and PlayMode tests from the command line**

```powershell
$editor = Get-ChildItem 'C:\Program Files\Unity\Hub\Editor\6000.3.*\Editor\Unity.exe' |
  Sort-Object { $_.VersionInfo.FileVersionRaw } -Descending |
  Select-Object -First 1 -ExpandProperty FullName
& $editor -batchmode -quit -projectPath 'F:\UnityProjects\Platformer2D' -runTests -testPlatform EditMode -testResults 'F:\UnityProjects\Platformer2D\TestResults\EditMode.xml' -logFile 'F:\UnityProjects\Platformer2D\Logs\EditModeTests.log'
& $editor -batchmode -quit -projectPath 'F:\UnityProjects\Platformer2D' -runTests -testPlatform PlayMode -testResults 'F:\UnityProjects\Platformer2D\TestResults\PlayMode.xml' -logFile 'F:\UnityProjects\Platformer2D\Logs\PlayModeTests.log'
```

Expected: both processes exit `0`; XML results contain zero failures.

- [ ] **Step 4: Build Windows x64**

```powershell
& $editor -batchmode -quit -projectPath 'F:\UnityProjects\Platformer2D' -executeMethod SnowbreakFan.Infrastructure.Editor.BuildScripts.BuildWindows64 -buildTarget StandaloneWindows64 -logFile 'F:\UnityProjects\Platformer2D\Logs\WindowsBuild.log'
```

Expected: exit `0` and `Builds\Windows\Platformer2D.exe` exists.

- [ ] **Step 5: Perform the acceptance run**

Launch the build and verify: Bootstrap loads once; A/D and arrows move; Space gives variable-height jump; controller move/jump work; coyote time and buffering are perceptible; three samples count once; checkpoint and fall respawn work; camera stays confined; endpoint shows completion; median first-time run is 120～180 seconds; player log contains no exception or repeating warning.

- [ ] **Step 6: Commit source and verification configuration, not build artifacts**

```powershell
git -C 'F:\UnityProjects\Platformer2D' add Assets Packages ProjectSettings docs .gitignore .gitattributes
git -C 'F:\UnityProjects\Platformer2D' commit -m "test: verify playable vertical slice"
git -C 'F:\UnityProjects\Platformer2D' status --short
```

Expected: clean status except explicitly ignored `Builds`, `Logs`, and `TestResults`.

---

## Self-Review Result

- Spec coverage: project creation, packages, assemblies, Bootstrap, input, responsive movement, respawn, collectibles, endpoint, mixed Tilemap/Prefab level, long-level camera, testing and Windows build all map to Tasks 1～9.
- Deferred scope is preserved: no combat, final art, Live2D, Addressables, mobile UI, moving platforms or multi-level flow is implemented.
- Type consistency: `PlayerMotor2D.ResetMotion()` is consumed only by `PlayerRespawnTarget`; `SceneLoadPlan.Create` matches its tests and Bootstrap; collectible and completion events cross additive scenes through ScriptableObject channels.
- Placeholder scan: no unresolved placeholder or undefined implementation step remains. Package patch versions are intentionally resolved by Unity 6.3 because verified package versions depend on the installed LTS patch.

## Official References

- Unity 6.3 LTS release and support: https://unity.com/blog/unity-6-3-lts-is-now-available
- Universal 2D template creates URP with a preconfigured 2D Renderer: https://docs.unity3d.com/ja/current/Manual/urp/creating-a-new-project-with-urp.html
- Unity command-line arguments and `-createProject`: https://docs.unity3d.com/cn/current/Manual/EditorCommandLineArguments.html
- Command-line Windows builds: https://docs.unity3d.com/kr/current/Manual/build-command-line.html
- Cinemachine package information: https://docs.unity3d.com/ja/current/Manual/com.unity.cinemachine.html
