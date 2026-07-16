# Chunk 02 Traversal Fix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the one-way platform chain after the first checkpoint traversable without changing player movement parameters.

**Architecture:** Add a PlayMode regression test that reads the live player movement configuration and verifies every transition through Chunk 02's one-way chain against the resulting ballistic jump envelope. Then move only the three one-way platform instances to create a forgiving ascending route.

**Tech Stack:** Unity 6.3 LTS, C#, Unity Test Framework, 2D Physics.

## Global Constraints

- Keep `PlayerMovementConfig` unchanged so the approved movement feel remains intact.
- Keep the existing platform prefabs, colliders, visuals, chunk boundaries, checkpoints, and collectibles unchanged.
- Work inline in the current workspace; do not dispatch subagents.

---

### Task 1: Make the Chunk 02 one-way chain reachable

**Files:**
- Modify: `Assets/Game/Tests/PlayMode/LevelStructureTests.cs`
- Modify: `Assets/Game/Scenes/10_Level_Prototype.unity`

**Interfaces:**
- Consumes: `PlayerMovementConfig.MaxSpeed`, `JumpSpeed`, `GravityScale`, `FallGravityMultiplier`, `Physics2D.gravity`, and platform collider bounds.
- Produces: a regression test proving each ordered transition has sufficient vertical and horizontal jump range.

- [x] **Step 1: Write the failing test**

Load `10_Level_Prototype`, find the one-way platforms in `Chunk_02_Gaps`, locate the adjacent ground platforms, and compare each transition against the configured ballistic jump envelope. The current first transition must fail because it rises 3 units while the configured maximum rise is about 2.15 units.

- [x] **Step 2: Run the focused PlayMode test to verify it fails**

Run Unity Test Framework for `SnowbreakFan.Tests.PlayMode.LevelStructureTests.Chunk02OneWayChainFitsConfiguredJumpEnvelope`.

Expected: one assertion failure reporting that the first one-way platform exceeds the configured maximum rise.

- [x] **Step 3: Apply the minimal scene fix**

Keep the source and destination ground platforms unchanged. Move the three one-way platform instances under `Chunk_02_Gaps/Platforms` from local positions `(18, 3)`, `(25, 4.5)`, `(32, 6)` to `(18, 1.5)`, `(23, 3)`, `(28, 4.5)`.

- [x] **Step 4: Verify the focused test and complete suites**

Run the focused PlayMode test, then all EditMode and PlayMode tests. Expected: zero failures.

- [x] **Step 5: Build and inspect the resulting changes**

Build Windows x64 using `SnowbreakFan.Infrastructure.Editor.BuildScripts.BuildWindows64`, verify the executable exists, and ensure Git contains only the intended test, scene, and plan changes before committing.

## Self-Review Result

- Spec coverage: preserves the approved movement design and restores the existing requirement that the vertical slice be fully traversable.
- Placeholder scan: no deferred or ambiguous implementation step remains.
- Type consistency: all referenced configuration properties already exist on `PlayerMovementConfig`; the test assembly already references `Game.Player` and `Game.Level`.
