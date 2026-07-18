# Platformer2D 工作区清理审计

日期：2026-07-18

## 范围与原则

本审计覆盖 Git 忽略的本地生成物，以及 `Assets/Game` 中与芬妮旧表现方案有关的受跟踪文件。目标是减少无长期价值的中间文件，同时保证当前构建、v008 可复现性、历史来源和回退链不受破坏。

本轮没有删除任何受 Git 跟踪的文件。

## 本地生成物：清理前后

| 路径 | 清理前 | 清理后 | 决策 |
|---|---:|---:|---|
| `TestResults/` | 247 文件，26.10 MiB | 7 文件，4.09 MiB | 删除 240 个中间结果，保留最终 v008 证据与预览 |
| `Logs/` | 64 文件，3.67 MiB | 不存在 | 全部可再生，删除 |
| `.superpowers/` | 19 文件，218.7 KiB | 不存在 | 会话级 review/brief 中间文件，正式规范和计划已在 `docs/`，删除 |
| `Builds/` | 319 文件，230.69 MiB | 未变 | 保留最新 Windows Development Build |
| `Library/` | 43,328 文件，2,504.25 MiB | 未变 | 保留，避免无收益的完整重新导入 |
| `UserSettings/` | 4 文件，0.05 MiB | 未变 | 保留本机 Unity 设置 |
| `Temp/`、`obj/`、Python `__pycache__` | 0 文件 | 0 文件 | 无需处理 |

实际释放约 `25.89 MiB`，不包含本就不存在的缓存目录。

## 保留的最终证据

`TestResults/` 只保留：

- `FennyV008FinalEdit.log`
- `FennyV008FinalEdit.xml`
- `FennyV008FinalPlay.log`
- `FennyV008FinalPlay.xml`
- `FennyV008FinalBuild.log`
- `FennyFrames/FennyGolden_v008_ContactSheet.png`
- `FennyFrames/FennyRun_v008_GameScale.gif`

## 受跟踪资产引用审计

### 当前运行路径

- `Player.prefab`、`FennyGoldenPresentation.asset` 和 `FennyFrameConfigurator` 均引用 `FennyGolden_Frames_v008.png`。
- v008 的源输入由 `Tools/Art/Manifests/fenny_golden_v008.json` 直接按路径引用：
  - `FennyGolden_IdlePoses_Candidate_v003.png`
  - `FennyGolden_RunCycle_Candidate_v007.png`
  - `FennyGolden_AirbornePhases_Candidate_v006.png`
- 上述三张候选源图是确定性重建 v008 的必要输入，不能移出或删除。

### 旧 cutout rig 链

下列资产不在当前 Player 运行时层级中，但仍构成一个完整的实验/回退链：

- `FennyGolden_RigParts_v005.png`
- `FennyVisualRig.prefab`
- `Fenny_Rig.controller`
- `Fenny_Idle.anim`、`Fenny_Run.anim`、`Fenny_Airborne.anim`
- `FennyRigBuilder.cs`
- `PlayerRigPresentation2D.cs`
- `FennyRigBuilderTests.cs`

GUID 搜索确认 Animator 被 `FennyVisualRig.prefab` 引用，三个 AnimationClip 被 Animator 引用，RigParts atlas 被 Rig prefab 引用。路径搜索确认 Builder 和专用 EditMode 测试会直接加载并重建这些资源。因此单独删除任一资产会破坏测试和回退工具，本轮全部保留。

### 旧整帧图集与候选素材

- `FennyGolden_Frames_v006.png` 与 `FennyGolden_Frames_v007.png` 不再被当前 Profile/Prefab 引用，但仍是对应 manifest 的输出、历史设计证据和回退基线。
- `FennyGolden_PartsMaster_Candidate_v004.png` 当前只在历史 cutout rig 设计中出现，没有运行时引用；它与同一实验链一起延期处理。
- `PlayerSpriteAnimator2D.cs` 是早期整帧适配器。当前配置器会主动移除它，Edit/Play Mode 测试也显式要求 Player 不挂载该组件；为保持迁移与历史测试上下文，本轮保留。

## 延期清理建议

如果以后确定永远不再使用 cutout rig，应该单独建立“移除遗留角色表现链”设计和计划，一次性处理 Builder、测试、Prefab、Animator、AnimationClip、rig atlas、旧运行时类型和配置器迁移分支，并重新跑完整 Edit/Play/Build。不要在普通功能开发中零散删除。

旧 v006/v007 图集可以在确认不再需要仓库内回退后迁入带来源说明的归档目录；移动前需要评估 manifest 路径和 Unity GUID 的影响。

## 仓库卫生观察

- `.gitignore` 忽略整个 `/.superpowers/`，范围比当前需要更宽。本次已清空该目录；后续若要把其中内容作为正式资产提交，应先把规则缩小到明确的会话缓存子目录。
- `Builds/`、`TestResults/`、`Library/` 和 IDE 工程文件保持忽略状态，符合 Unity 工作区习惯。
- 历史 `docs/superpowers/specs/` 和 `docs/superpowers/plans/` 是设计决策记录，不属于垃圾文件。
