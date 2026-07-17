# 芬妮跑步动画稳定性收尾设计

日期：2026-07-18  
状态：等待用户书面复核

## 1. 目标与范围

本轮只解决当前整帧跑步动画仍可感知的抖动，并把同一稳定性合同留给后续角色。保留现有完整角色帧架构、单帧 Idle、已确认的跳跃帧、玩家碰撞体、移动参数和关卡内容。

不增加跑步帧数，不恢复切片骨骼，不在运行时写入逐帧位置或缩放补偿，也不同时调整相机、Pixel Perfect 或纹理 mipmap。若本轮完成后只剩移动边缘微闪，再把纹理缩小采样作为独立问题处理。

## 2. 根因证据

### 2.1 素材中的视觉核心横向漂移

v007 的骨盆锚点已经固定，但玩家最敏感的头部与躯干并未连续。最终 `768×1024` 单元格的只读量化结果显示：

- `Run_01 → Run_02`：躯干横移 `-42.4 px`，头部横移 `-53.3 px`；
- `Run_05 → Run_06`：躯干横移 `-34.1 px`，头部横移 `-48.9 px`；
- `Run_07 → Run_00`：躯干横移 `+31.4 px`，头部横移 `+31.1 px`；
- 各帧 alpha 面积只在均值附近约 `±4.7%`，因此剩余问题不是整体缩放；
- 对上述过渡做刚性平移补偿后，头部 IoU 可提高到 `0.905–0.927`，躯干 IoU 可提高到 `0.802–0.894`，证明主要缺陷是上身核心 placement，而非轮廓身份变化。

纵向方面，现有骨盆目标曲线为 `440, 424, 432, 448` 循环，总范围 `24 px`；躯干相对骨盆的纵向跨度只有 `5.3 px`。因此纵向残差主要来自人为 bob 曲线，不需要重新生成角色姿势。

### 2.2 变速时运行时重算历史动画相位

`PlayerFramePresentation2D` 当前使用 `floor(stateElapsed × currentFramesPerSecond)` 选择帧。由于 `currentFramesPerSecond` 随当前速度变化，减速会用新的 FPS 重新解释全部历史时间。例如运行一秒后从速度 `6` 降到约 `4.6`，逻辑帧可能从 `Run_00` 直接跳到 `Run_04`；其他相位还会出现倒退或重复。

该问题独立于素材，即使 v008 的每张图完全对齐，正常加速、刹车和反向仍可能制造跳帧。

### 2.3 已排除的主因

- 当前播放器在 `Awake` 后只切换 `SpriteRenderer.sprite` 与 `flipX`，不逐帧修改 Visual Transform；
- 所有 Sprite 使用相同 `768×1024` rect、PPU `480` 与 `(0.5, 0)` pivot；
- Rigidbody2D 已启用 Interpolate；
- Player 只挂载整帧播放器，旧骨骼与旧 Sprite animator 未接入；
- 相机阻尼和无 Pixel Perfect 可能造成连续运动中的采样微闪，但不能解释严格跟随 Run 换帧的头/躯干横跳。

## 3. 方案比较与决定

### 方案 A：烘焙视觉核心对齐，同时修复增量相位（采用）

复用 v007 的八张完整跑姿，不重新生成身份和动作。离线清单为每帧增加可感知的 `visual_core` 锚点，横轴按该锚点烘焙对齐，纵轴继续使用解剖骨盆曲线但缩小 bob 幅度。运行时改为逐帧累计相位。

优点是同时修复已确认的两个根因，不破坏现有造型与跳跃，不引入运行时 Transform 补偿，并形成后续角色可复用的视觉核心合同。

### 方案 B：只平移 v007 PNG，不修改管线

短期文件最少，但调整值无法被清单解释或自动验证；后续角色会再次依赖人工目测，且运行时变速跳帧仍存在，因此拒绝。

### 方案 C：重新生成或补更多跑步帧

当前问题是核心 placement 和播放相位，而不是帧数不足。重新生成会重新引入身份、比例和轮廓漂移，补帧还会放大相位重算的跳跃，因此本轮拒绝。

## 4. 素材与归一化合同

### 4.1 v008 版本化资产

- 保留 `FennyGolden_RunCycle_Candidate_v007.png` 作为原始姿势来源；
- 新建 `Tools/Art/Manifests/fenny_golden_v008.json`；
- 新建 `Assets/Game/Art/Characters/Player/FennyGolden_Frames_v008.png`；
- v006、v007 与旧骨骼资源继续保留，便于回退；
- Idle 与 Rising/Apex/Falling 的像素内容不变，只重新打包进 v008 图集。

### 4.2 视觉核心锚点

Run 帧在现有 `sole`、`pelvis`、`head` 之外增加 `visual_core`。该点表示头部与躯干共同形成的感知中心，而不是脚步或完整 alpha 包围盒中心。

归一化器增加可选 `destination_visual_core_x`：

- 若帧提供 `visual_core` 与 `destination_visual_core_x`，X 轴使用视觉核心对齐；
- Y 轴仍由 `destination_anchor.y - scaledPelvis.y` 决定；
- 未提供视觉核心的 Idle 与跳跃帧保持现有行为；
- 对齐结果只写入最终图集，运行时 Profile 不增加逐帧 offset 字段。

Run 的八个视觉核心使用同一 X 目标。骨盆 Y 曲线从 `440, 424, 432, 448` 收敛为 `440, 432, 436, 444`，下半周期重复同一曲线，使总范围从 `24 px` 降到 `12 px`，最大相邻变化从 `16 px` 降到 `8 px`。

### 4.3 通用序列预算

`motion_groups` 支持通用锚点预算：

- `x_anchor` 可选择 `pelvis` 或 `visual_core`；
- `y_anchor` 本轮固定为 `pelvis`；
- 新字段 `max_x_span`、`max_x_step`、`max_y_span`、`max_y_step` 对选定锚点生效；
- 旧清单的 `max_pelvis_*` 字段继续兼容，避免破坏 v006/v007 与现有测试。

v008 Run 的自动门禁为：视觉核心 X 总范围不超过 `4 px`、相邻及循环步长不超过 `4 px`；骨盆 Y 总范围不超过 `12 px`、相邻及循环步长不超过 `8 px`。

## 5. 增量相位播放器

新增一个只负责循环相位的 `FramePlaybackClock`：

- 状态进入时 `Reset()`，当前索引立即为 `0`；
- 后续每个渲染帧执行 `phase += deltaTime × currentFPS`；
- 帧索引为 `floor(phase) % frameCount`；
- 改变速度只影响本次及未来的相位增量，不重新解释历史相位；
- 状态切换仍重置序列，Idle 仍只有一帧；
- 不改变方向判断、移动速度、Rigidbody、碰撞体或跳跃状态选择。

本轮不同时加入速度阈值滞回。若后续能稳定复现 `0.1` 附近的 Run/Idle 抖动，再以独立失败测试处理，避免把尚未证实的外力问题混入本轮。

## 6. 测试顺序

### 6.1 先失败的工具测试

1. 带 `visual_core` 的帧在 X 轴按视觉核心对齐，而 Y 轴仍按骨盆目标对齐；
2. 视觉核心 X 超出通用预算时，报告动作组、帧名、实际跨度/步长与上限；
3. 旧版 pelvis-only motion group 仍可正常验证；
4. v008 清单输出的核心与骨盆曲线满足本设计预算；
5. 重复归一化产生字节一致的图集。

### 6.2 先失败的播放器测试

1. `RunPhaseDoesNotReinterpretHistoryWhenSpeedDrops`：先按 16 FPS 累积，再降到约 12 FPS，帧序只能保持或前进，不能从 `00` 跳到 `04`；
2. `RunEntryAlwaysPresentsFrame00BeforeAdvancing`：进入 Run 的第一帧必为 `Run_00`；
3. 恒速至少三圈只允许 `00→01→…→07→00`；
4. 正常加速、刹车和反向轨迹中，在测试步长足够小时没有倒退或跨帧；
5. 所有换帧期间 Visual position、rotation、scale 保持不变。

### 6.3 完整回归

- Python 资产工具全部测试；
- Unity Edit Mode 全部测试；
- Unity Play Mode 全部测试；
- `.meta` 完整性审计；
- Windows x64 Development Build；
- 固定根节点的游戏实际缩放 GIF 与接触表。

## 7. 完成标准

- Idle 五秒内 Sprite 与 Transform 完全不变；
- v008 Run 视觉核心 X 总范围 `≤4 px`，相邻与 `07→00` 步长 `≤4 px`；
- Run 骨盆 Y 总范围 `≤12 px`，相邻与循环步长 `≤8 px`；
- 三个原严重过渡的躯干 IoU `≥0.80`、头部 IoU `≥0.85`；
- 在有界测试步长下，连续十圈零倒退、零跨帧，唯一循环接缝为 `07→00`；
- 起跑首先显示 `Run_00`，正常减速不会重算历史相位；
- 运行时仍无逐帧 Transform/scale correction；
- 自动化测试、预览和 Windows Build 全部通过；
- 用户在 Play Mode 或新 EXE 中确认持续跑动、刹车和循环接缝已达到可接受水平。

## 8. 回退策略

Player 在 v008 通过全部门禁前继续引用 v007。v008 使用新的文件名、manifest 与 Unity GUID，不覆盖既有资源。若视觉核心标注或预览不合格，只修订 v008；运行时相位修复可以独立测试和回退，不依赖 v008 图集。
