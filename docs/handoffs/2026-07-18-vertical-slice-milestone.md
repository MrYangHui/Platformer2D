# Platformer2D 垂直切片里程碑交接

日期：2026-07-18

项目路径：`F:\UnityProjects\Platformer2D`

远程仓库：`https://github.com/MrYangHui/Platformer2D`

Unity：`6000.3.19f1`

## 1. 里程碑结论

当前项目已经从灰盒原型推进到可完整游玩的第一版垂直切片。玩家可以从正确入口进入关卡，完成移动和跳跃挑战，依次激活检查点、收集 3 个异常样本并到达终点；坠落会从最近检查点重生。

芬妮采用 v008 整帧动画管线。用户实机确认当前动画已经接近完美，不再继续为残余轻微抖动投入开发时间。后续只有在新增攻击、受击或其他状态需要新动作时，才重新评估角色帧体系。

本里程碑之前的运行时基线提交为 `48a7899`。该提交已推送至 `origin/main`，包含 v008 图集、增量播放相位、完整测试和 Windows 构建验证；本次收尾只增加文档并清理被忽略的本地中间文件。

## 2. 正确启动方式

- Unity Hub 打开：`F:\UnityProjects\Platformer2D`
- 入口场景：`Assets/Game/Scenes/00_Bootstrap.unity`
- Bootstrap 配置加载：
  - `Assets/Game/Scenes/10_Level_Prototype.unity`
  - `Assets/Game/Scenes/20_UI_Gameplay.unity`
- Windows 构建：`Builds/Windows/Platformer2D.exe`

三个场景都已启用在 `ProjectSettings/EditorBuildSettings.asset`。不要单独运行关卡场景来判断完整启动流程。

## 3. 操作与当前玩法

- 键盘：`A/D` 或左右方向键移动，`Space` 跳跃。
- 手柄：左摇杆/D-pad 移动，South Button 跳跃。
- 跳跃：土狼时间、跳跃预输入、可变跳高和空中控制。
- 关卡：3 个收集物、多个坠落区、检查点、终点和 HUD。
- 相机：Cinemachine 3 跟随、阻尼、前视和关卡边界约束。

当前没有攻击输入、生命值、敌人、伤害、战斗 UI 或对话系统。

## 4. 场景与模块架构

运行时使用模块化 MonoBehaviour、asmdef 和数据驱动 ScriptableObject，没有引入 ECS、全局可变单例或重型依赖注入。

| 模块 | 当前职责 |
|---|---|
| `Game.Core` | 场景加载计划、重生目标接口 |
| `Game.Player` | 输入、移动数学、地面检测、角色运动与重生目标 |
| `Game.Level` | 检查点、坠落区、终点、重生服务、关卡块 |
| `Game.Collectibles` | 收集物、会话计数和完成状态 |
| `Game.Presentation` | HUD、视差、角色帧 Profile、播放时钟与状态适配 |
| `Game.Infrastructure` | Bootstrap 和配置数据 |
| `Game.Infrastructure.Editor` | 项目配置器、美术导入器和 Windows Build 脚本 |

物理与表现保持分离：`PlayerMotor2D` 是移动状态权威，`PlayerFramePresentation2D` 只读取状态和速度并切换 Sprite，不逐帧修正 Transform 或 scale。

## 5. 芬妮 v008 表现管线

关键文件：

- 图集：`Assets/Game/Art/Characters/Player/FennyGolden_Frames_v008.png`
- 清单：`Tools/Art/Manifests/fenny_golden_v008.json`
- 离线归一化：`Tools/Art/normalize_character_frames.py`
- Unity 导入配置：`Assets/Game/Scripts/Infrastructure/Editor/FennyFrameConfigurator.cs`
- Profile：`Assets/Game/Config/Characters/FennyGoldenPresentation.asset`
- 运行时：
  - `Assets/Game/Scripts/Presentation/Runtime/FramePlaybackClock.cs`
  - `Assets/Game/Scripts/Presentation/Runtime/PlayerFramePresentation2D.cs`

图集包含 12 个 `768×1024`、PPU 480、底部中心 Pivot 的语义 Sprite：一个 Idle、八个 Run、Rising、Apex、Falling。v008 没有重新生成角色姿势，只对 v007 源像素进行确定性重定位；Run 的视觉核心 X span/step 为 `2/2 px`，骨盆 Y span/step 为 `12/8 px`。

最终图集 SHA-256：

```text
7A239664F93CCF1EF554CC6A3A50B67BF6A4A485755816E463165D9569DE9C46
```

## 6. 验证基线

2026-07-18 最终证据保留在 `TestResults/`：

- Python 美术工具：`19/19`。
- `TestResults/FennyV008FinalEdit.xml`：Edit Mode `52/52`。
- `TestResults/FennyV008FinalPlay.xml`：Play Mode `14/14`。
- `TestResults/FennyV008FinalBuild.log`：`Build Finished, Result: Success.`。
- 元数据审计：206 个 Assets 项、206 个 `.meta`，missing `0`、orphan `0`。

Unity 命令行测试不要使用 `-nographics`。启动器可能先于实际 Editor 进程返回，自动化脚本必须等待 Unity PID 真正退出后再读取 XML。

## 7. 清理结果与保留理由

本次清理删除 240 个旧 TestResults 文件（约 22.01 MiB）、整个旧 `Logs/`（约 3.67 MiB）以及 19 个 `.superpowers` 会话中间文件（约 218.7 KiB）。保留：

- 最新 Windows 构建（约 230.69 MiB）。
- 最终 v008 Edit/Play/Build 证据、接触表和固定根 GIF（7 个文件，约 4.09 MiB）。
- `Library`（约 2.5 GiB），因为删除只会触发完整重新导入，没有发现损坏证据。
- `UserSettings` 和 Unity 生成的 IDE 工程文件。
- 所有受 Git 跟踪的源素材、旧图集、设计文档和实验 rig 链。

完整分类证据见 `docs/audits/2026-07-18-workspace-cleanup-audit.md`。

## 8. 已知技术债

- `FennyRigBuilder`、`FennyVisualRig.prefab`、Animator 和 cutout rig 素材已经不是玩家运行时路径，但仍被专门的 EditMode 测试和回退生成流程引用。删除需要单独计划，不能只删 Prefab 或贴图。
- `PlayerSpriteAnimator2D` 与 `PlayerRigPresentation2D` 是旧表现适配器；当前配置器和测试显式确保它们不挂到 Player 上，但迁移/回退代码仍引用这些类型。
- v006/v007 图集当前不被 Player/Profile 引用，但对应 manifest、历史设计和回退证据仍存在。
- `FramePlaybackClock` 在正常游戏时长内稳定；若连续运行数年导致 phase 超过 Int32 范围，`CurrentIndex` 仍有理论上的负索引风险，优先级很低。
- `.gitignore` 当前忽略整个 `/.superpowers/`。本次已删除其中会话中间文件；以后如果需要提交该目录下的新材料，应先缩小忽略范围。

## 9. 下一对话：敌人与战斗系统

下一对话不得直接开始写代码。首先阅读本交接文档和现有玩家/关卡模块，然后与用户逐项确认：

1. 第一版战斗体验目标和芬妮使用的攻击形式。
2. 攻击输入、方向、节奏和是否允许空中攻击。
3. 第一种敌人的巡逻、发现、追击和攻击边界。
4. 玩家/敌人的生命、伤害、受击硬直、击退、无敌帧与死亡重生。
5. 第一版是否使用占位素材，以及攻击/受击动画的制作顺序。
6. 明确不进入首版的内容，例如连招、技能、闪避、远程敌人和复杂行为树。

确认后先形成战斗系统设计文档，再写实施计划，最后开始实现。剧情对话系统排在战斗最小闭环之后，除非用户在新对话中调整优先级。

建议新对话的首条任务：

```text
请先阅读 docs/handoffs/2026-07-18-vertical-slice-milestone.md，基于当前垂直切片与现有模块，和我逐项确认敌人与战斗系统的第一版范围。暂时不要实现代码；先比较方案、完成设计文档并等待我批准。
```
