# Platformer2D

个人学习与同好分享用途的 2D 平台跳跃同人项目。当前里程碑是一段可从起点通关到终点的 Windows 垂直切片，已完成基础移动、关卡循环、环境美术与芬妮整帧角色表现。

## 开发环境

- Unity `6000.3.19f1`
- Universal Render Pipeline / Universal 2D Renderer
- Input System `1.19.0`
- Cinemachine `3.1.2`
- Unity Test Framework `1.6.0`
- 首要目标平台：Windows x64

## 运行项目

1. 用 Unity Hub 打开仓库根目录。
2. 打开 `Assets/Game/Scenes/00_Bootstrap.unity`。
3. 进入 Play Mode。Bootstrap 会以 Additive 方式加载 `10_Level_Prototype` 和 `20_UI_Gameplay`。

不要直接把旧示例工程或其他场景当作入口。Windows 构建位于 `Builds/Windows/Platformer2D.exe`。

## 操作

| 操作 | 键盘 | 手柄 |
|---|---|---|
| 左右移动 | `A` / `D` 或左右方向键 | 左摇杆或 D-pad |
| 跳跃 | `Space` | South Button |

跳跃支持土狼时间、跳跃预输入和可变跳高。

## 当前功能

- 可完整通过的原型关卡与 Cinemachine 跟随相机。
- 三个异常样本收集物、HUD 计数和终点判定。
- 坠落死亡、检查点激活和最近检查点重生。
- 冰封都市背景视差、平台贴图与可见碰撞一致性。
- 芬妮 v008 整帧表现：Idle、8 帧 Run、Rising、Apex、Falling。
- Windows x64 Development Build 和 Edit/Play Mode 自动化测试。

当前没有敌人、战斗系统或剧情对话系统。下一阶段优先讨论敌人与战斗的最小闭环，具体设计尚未确定。

## 验证

Python 美术归一化测试：

```powershell
$Python = 'python'
& $Python -m unittest discover -s Tools/Art/tests -v
```

Unity Edit Mode：

```powershell
$UnityExe = 'F:\unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe'
$ProjectRoot = (Resolve-Path '.').Path
& $UnityExe -batchmode -projectPath $ProjectRoot `
  -runTests -testPlatform EditMode `
  -testResults (Join-Path $ProjectRoot 'TestResults/EditMode.xml') `
  -logFile (Join-Path $ProjectRoot 'TestResults/EditMode.log')
```

Unity Play Mode 使用同一命令，将 `EditMode` 改为 `PlayMode`。不要添加 `-nographics`；当前 Unity 版本在一个遗留 rig 预览测试中会因此触发原生崩溃。

Windows x64 Development Build：

```powershell
$UnityExe = 'F:\unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe'
$ProjectRoot = (Resolve-Path '.').Path
& $UnityExe -batchmode -quit -projectPath $ProjectRoot `
  -executeMethod SnowbreakFan.Infrastructure.Editor.BuildScripts.BuildWindows64 `
  -buildTarget StandaloneWindows64 `
  -logFile (Join-Path $ProjectRoot 'TestResults/WindowsBuild.log')
```

当前里程碑验证基线：Python `19/19`、Edit Mode `52/52`、Play Mode `14/14`，Windows 构建成功。

## 文档

- [当前里程碑交接](docs/handoffs/2026-07-18-vertical-slice-milestone.md)
- [工作区清理审计](docs/audits/2026-07-18-workspace-cleanup-audit.md)
- [初始垂直切片设计](docs/superpowers/specs/2026-07-16-platformer2d-vertical-slice-design.md)
- [角色 Run 稳定化设计](docs/superpowers/specs/2026-07-18-fenny-run-stability-finalization-design.md)
- [美术来源说明](ArtSource/Provenance/README.md)

`docs/superpowers/specs/` 与 `docs/superpowers/plans/` 保留历史设计决策和实施记录，不应当作待执行任务清单直接重跑。
