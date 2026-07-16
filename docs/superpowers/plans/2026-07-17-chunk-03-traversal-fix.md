# Remaining Route Traversal Fix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the complete route from Chunk 03 through the end of Chunk 05 traversable without changing the approved player movement parameters.

**Architecture:** Extend the existing PlayMode geometry tests to validate every ordered transition against the live `PlayerMovementConfig`. Re-space and lower the affected platform chains while keeping samples, checkpoints, the recovery route, and the level endpoint supported.

**Tech Stack:** Unity 6.3 LTS, C#, Unity Test Framework, 2D Physics.

## Global Constraints

- Keep `PlayerMovementConfig` unchanged.
- Preserve Chunk 03's vertical gain between 10 and 14 world units.
- Preserve platform visuals, colliders, prefabs, samples, checkpoints, and chunk boundaries.
- Work in the current main workspace as explicitly approved by the user.

---

### Task 1: Validate and repair the Chunk 03 vertical route

**Files:**
- Modify: `Assets/Game/Tests/PlayMode/LevelStructureTests.cs`
- Modify: `Assets/Game/Scenes/10_Level_Prototype.unity`

**Interfaces:**
- Consumes: `PlayerMovementConfig.MaxSpeed`, `JumpSpeed`, `GravityScale`, `FallGravityMultiplier`, `Physics2D.gravity`, and ordered platform collider bounds.
- Produces: `Chunk03VerticalRouteFitsConfiguredJumpEnvelope`, proving all six ascending transitions fit the configured jump envelope with horizontal reserve.

- [x] **Step 1: Write the failing route test**

Load `10_Level_Prototype`, sort all seven `Chunk_03_Vertical/Platforms` colliders by X, and calculate each transition's height delta and horizontal edge gap. Assert that height does not exceed the configured maximum rise and that the gap does not exceed 85% of the configured ballistic horizontal range.

- [x] **Step 2: Run the focused test and verify RED**

The transition from world X `127` to `135` failed because it needed `3.5` horizontal units while the safe configured range was about `2.06`.

- [x] **Step 3: Apply the complete-chain repair**

Keep the starting long platform at local `(6, 0)`. Move the remaining platforms to `(15, 1.75)`, `(21, 3.5)`, `(27, 5.25)`, `(33, 7)`, `(39, 8.75)`, and `(50, 10.5)`. Move `AnomalySample_02` from world `(143, 8)` to `(139, 7.25)`. Move `RespawnCheckpoint_VerticalTop` from local `(52, 14)` to `(50, 12.5)`.

- [x] **Step 4: Verify focused GREEN**

The focused Chunk 03 reachability test passes, while preserving a `10.5`-unit vertical gain.

### Task 2: Validate and repair the remaining Chunk 04 and Chunk 05 routes

- [x] **Step 1: Add failing tests for main, recovery, rejoin, and final routes**

Add `Chunk04MainAndRecoveryRoutesFitConfiguredJumpEnvelope` and `Chunk05FinalRouteFitsConfiguredJumpEnvelope`, reusing one transition-envelope helper so every route is judged by the same movement configuration.

- [x] **Step 2: Verify RED and identify the actual blockers**

Chunk 04's main route had a `4`-unit gap above its `3.82`-unit safe range. Chunk 05 had a `3`-unit ascent above the configured `2.15`-unit maximum rise. The old checkpoint assertion also encoded an absolute height instead of the required support relationship.

- [x] **Step 3: Apply geometry-only repairs**

Re-space Chunk 04's main and recovery routes so all gaps and the recovery rejoin fit the jump envelope. Reshape Chunk 05 into a gradual rise and descent, while retaining support under the end trigger. Keep the player configuration unchanged. Validate the high checkpoint at `1.5` units above its supporting platform surface rather than against a brittle absolute Y coordinate.

- [x] **Step 4: Verify all route tests GREEN**

Run the complete `LevelStructureTests` fixture and confirm all route, chunk extent, collectible, and checkpoint assertions pass.

### Task 3: Full verification and delivery

- [x] **Step 1: Run full regression**

Run all EditMode and PlayMode tests and confirm zero failures.

- [x] **Step 2: Build and inspect**

Build Windows x64 using `SnowbreakFan.Infrastructure.Editor.BuildScripts.BuildWindows64`, restore Unity's unrelated build-time serialization changes, and inspect Git so only the intended plan, test, scene, and separately reviewed art candidate files remain.

- [x] **Step 3: Commit and push**

Commit the verified route repair and reviewed art candidates intentionally, then push `main` to the configured GitHub origin. Do not create a release or publish a build.

## Self-Review Result

- Spec coverage: the route retains a vertical challenge while every tested transition stays inside the configured movement envelope.
- Placeholder scan: no deferred route implementation detail remains.
- Type consistency: all route tests use the same movement configuration and ballistic formula already proven by the Chunk 02 regression.
