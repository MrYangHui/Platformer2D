# Platformer2D 项目交接说明

## 新工程

- 路径：`F:\UnityProjects\Platformer2D`
- Unity：Unity 6.3 LTS（6000.3.19f1）
- 模板：Universal 2D（URP 2D Renderer）
- 当前旧 Demo：`F:\unity\project\My project`，只作为历史参考，不在其上继续重构。

## 已确认的产品方向

- 非商业《尘白禁区》同人游戏，用于个人学习和同好分享。
- 2D 平台跳跃，非像素、偏 Q 版；首名角色为芬妮·戈尔登。
- 角色比例采用已经确认的约 3.7–4 头身版本，正常移动时武器收纳在背部或枪套。
- 第一阶段交付 2–3 分钟可玩的垂直切片：横向为主、少量纵向区域、3 个收集物、终点、坠落重生。
- 移动手感偏精准平台跳跃：土狼时间、跳跃预输入、可变跳高、空中控制。
- Windows 优先，后续再考虑移动端。

## 技术与架构结论

- 模块化 MonoBehaviour + 数据驱动 ScriptableObject，使用 asmdef 隔离模块。
- 不采用 ECS/DOTS、重型依赖注入、全局可变单例或按对象名称查找依赖。
- 场景规划：`00_Bootstrap`、`10_Level_Prototype`、`20_UI_Gameplay`。
- 模块规划：Core、Player、Level、Collectibles、Presentation、Infrastructure。
- 关卡使用 Tilemap + Prefab 混合搭建。
- 相机使用 Cinemachine 3：Dead Zone、阻尼/前视、Confiner2D、独立稳定的相机跟随目标。
- 动画采用骨骼动画为主、关键姿势换图与特效为辅的混合方案。

## 美术方向结论

- 环境：冻结都市异常封锁区，阴天冷色日光，轻科幻、动画化简化但保留可信材质与空间层次。
- 背景拆分：Sky、Far、Mid、Near，加独立雾层，便于视差与长关卡相机移动。
- 原始参考、生成图和来源记录放在项目根目录 `ArtSource/`；只有运行时整理完成的资源进入 `Assets/Game/Art/`。
- 官方参考只用于识别角色与风格研究，不把官方壁纸或拆包资源直接打进可发布版本。

## 后续扩展方向

- 长关卡与水平/垂直相机移动。
- 多关卡加载与切换。
- 战斗：普攻、技能、闪避等。
- Live2D + 对话框式剧情演出。

## 已有详细文档

- `docs/superpowers/specs/2026-07-16-platformer2d-vertical-slice-design.md`
- `docs/superpowers/plans/2026-07-16-platformer2d-vertical-slice-implementation.md`

## 新任务的第一步

1. 验证新工程可由 Unity 正常打开并完成首次导入。
2. 初始化 Git，加入适用于 Unity 的 `.gitignore`。
3. 检查 Universal 2D/URP、Input System、Cinemachine 与测试框架版本。
4. 按实施计划从生产级目录、asmdef 和 Bootstrap 场景开始建设。

