# Platformer2D Vertical Slice Milestone Closeout Design

日期：2026-07-18

## 目标

把当前可通关的 2D 平台跳跃垂直切片冻结为一个可复现、可交接的里程碑，并在进入敌人与战斗系统设计前清理无长期价值的本地生成物。此次收尾不修改游戏行为、关卡、角色素材或运行时配置。

## 文档交付

- 在仓库根目录创建 `README.md`，提供项目定位、Unity 版本、正确入口、操作、已完成功能、测试和构建命令。
- 新建 `docs/handoffs/2026-07-18-vertical-slice-milestone.md`，记录当前架构、角色 v008 表现管线、验证基线、已知技术债和下一对话的起点。
- 新建 `docs/audits/2026-07-18-workspace-cleanup-audit.md`，记录磁盘生成物、旧实验资产、引用证据、实际清理项和保留理由。
- 保留 7 月 16 日交接文档及全部设计/实施计划作为历史记录，不覆盖或删除。

## 清理策略

清理分为三类：

1. **立即删除**：可重新生成且不承载项目状态的缓存、旧测试日志和过期预览；删除前记录路径与体积。
2. **保留**：当前 Windows 构建、v008 最终测试/构建证据、v008 接触表与固定根预览、Unity `Library` 和 `UserSettings`（避免无意义的重新导入和本机设置丢失）。
3. **延期处理**：受 Git 跟踪的旧 cutout rig、Animator、旧 atlas 与候选源图。本轮只做引用与用途审计；只要仍被测试、生成工具、清单或回退流程引用，就不删除。

所有递归删除必须使用已验证的工作区绝对路径，且不得删除 `Assets`、`Packages`、`ProjectSettings`、`Tools`、`ArtSource` 或 `docs` 下的跟踪内容。

## 里程碑事实基线

- Unity：`6000.3.19f1`。
- 正确入口：`Assets/Game/Scenes/00_Bootstrap.unity`；Bootstrap 以 Additive 方式加载关卡和 UI。
- 玩家输入：键盘 A/D 或方向键左右移动，Space 跳跃；手柄左摇杆/D-pad 移动，South Button 跳跃。
- 当前主循环：移动与跳跃、坠落重生、检查点、3 个收集物、终点、HUD、Cinemachine 相机、环境贴图和 Fenny v008 整帧动画。
- 验证基线：Python `19/19`、Unity Edit Mode `52/52`、Play Mode `14/14`、Windows x64 Development Build 成功。
- Git 基线：`main` 与 `origin/main` 在提交 `48a7899` 同步，工作区干净。

## 下一阶段边界

下一对话只讨论敌人与战斗系统的需求、方案和最小闭环，不在本次收尾中预设具体攻击键、敌人 AI、伤害公式、动画或美术方案。新对话必须先阅读新的里程碑交接文档，再通过逐项确认形成战斗设计文档和实施计划。

## 完成标准

- 三份新文档内容互相一致，路径和命令可从仓库直接验证。
- 清理审计明确列出已删除、保留和延期项；没有删除受跟踪项目资产。
- `git diff --check` 通过，Git 工作区在提交和推送后干净。
- `origin/main...main` 返回 `0 0`。
- 已创建新的 Codex 对话，其首条任务要求阅读里程碑交接文档，并在实施前与用户确认战斗系统细节。
