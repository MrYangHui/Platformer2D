# Platformer2D Vertical Slice Milestone Closeout Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Document the current vertical-slice milestone, audit and safely prune local generated files, then create a clean handoff into a new combat-design conversation.

**Architecture:** Documentation is split by audience: root quick start, detailed engineering handoff, and evidence-based cleanup audit. Cleanup is restricted to ignored and reproducible local outputs; tracked Unity assets are audited but preserved unless a separate removal plan is approved.

**Tech Stack:** Markdown, Git, PowerShell, Unity 6000.3.19f1 project metadata.

## Global Constraints

- Do not change gameplay, scenes, prefabs, runtime scripts, packages, project settings, or art pixels.
- Do not delete tracked assets or historical design/plan documents.
- Preserve the latest Windows build and the final v008 verification/preview artifacts.
- Verify absolute paths before every recursive cleanup.
- Push directly to `origin/main`; do not create a PR, release, or remote repository.

---

### Task 1: Write milestone-facing documentation

**Files:**
- Create: `README.md`
- Create: `docs/handoffs/2026-07-18-vertical-slice-milestone.md`

**Interfaces:**
- Consumes: current build settings, input actions, module layout, v008 design/report evidence.
- Produces: the canonical starting context for users and the next Codex conversation.

- [ ] **Step 1:** Write `README.md` with exact Unity version, entry scene, controls, current features, test commands, build command and documentation links.
- [ ] **Step 2:** Write the milestone handoff with current architecture, scene flow, art pipeline, verification counts, known debt, cleanup result and an explicit combat-design handoff prompt.
- [ ] **Step 3:** Check every referenced local path with `Test-Path` or `rg --files` and remove any stale statement.

### Task 2: Audit references and clean local generated outputs

**Files:**
- Create: `docs/audits/2026-07-18-workspace-cleanup-audit.md`
- Delete only selected ignored files under: `Logs/`, `TestResults/`, `.superpowers/`, `Tools/Art/**/__pycache__/`

**Interfaces:**
- Consumes: `.gitignore`, Git tracked file list, serialized GUID references and filesystem size inventory.
- Produces: an auditable retain/delete/defer decision for each cleanup category.

- [ ] **Step 1:** Record sizes and file counts for ignored build, cache, log and result directories.
- [ ] **Step 2:** Use `rg` and GUID/name searches to classify the old cutout rig, Animator, v006/v007 atlases and source candidates as referenced, reproducibility inputs or deferred historical assets.
- [ ] **Step 3:** Delete only obsolete ignored logs, intermediate XMLs/previews, session-level `.superpowers` artifacts and Python caches after resolving each absolute target under the workspace.
- [ ] **Step 4:** Re-inventory retained outputs and write the exact findings and cleanup result into the audit document.

### Task 3: Verify, commit, push and hand off

**Files:**
- Verify: all new Markdown documents and the unchanged Git-tracked project.

**Interfaces:**
- Consumes: Task 1 and Task 2 documents plus the existing final test/build evidence.
- Produces: a synchronized milestone commit and a new user-owned combat-design conversation.

- [ ] **Step 1:** Run `git diff --check`, Markdown placeholder scans, path checks, metadata pairing audit and `git status --short`.
- [ ] **Step 2:** Confirm `FennyV008FinalEdit.xml` is `52/52`, `FennyV008FinalPlay.xml` is `14/14`, the final build log reports success and `Builds/Windows/Platformer2D.exe` exists.
- [ ] **Step 3:** Commit only the spec, plan, README, milestone handoff and cleanup audit.
- [ ] **Step 4:** Push `main` to `origin/main`, then require `git rev-list --left-right --count origin/main...main` to return `0 0` and a clean worktree.
- [ ] **Step 5:** Create a new Codex conversation that first reads `docs/handoffs/2026-07-18-vertical-slice-milestone.md`, asks the user to confirm combat scope, and does not implement before design approval.
