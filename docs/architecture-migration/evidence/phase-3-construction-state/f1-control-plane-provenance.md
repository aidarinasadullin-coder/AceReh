# Phase 3 Final Verification F1: Control-Plane Provenance Receipt

Audit date: `2026-08-19`

## Purpose

This receipt records the provenance of the three metadata-attribution findings
that caused the parent F1 plan-compliance review to return `VERDICT: REJECT`
(see `f1-plan-compliance.md`, preserved unchanged as historical evidence). It
classifies each finding as an exact-hash-bound control-plane or owner-directed
artifact, not as Phase 3 Tasks 1-13 implementation output. It does not broaden
any allow-list and does not waive future drift.

## Scope

This receipt is documentation only. It does not modify the ledger, boulder,
either Phase 3.1 plan, parent plans, the F1 receipt, code, tests, maps, widget,
or any other evidence. It does not claim `F1 APPROVE`, does not mark any Final
Wave checkbox, does not transition workflow, and does not authorize or start
Phase 3.1.

## 1. `.omo/start-work/ledger.jsonl` is append-only relative to HEAD

The ledger is a JSONL event log. Its first two rows are the original rows
present at the Task 1 baseline and are an exact prefix of the current file:

1. `start-work` for `fix-calculation-context-writers` (work_id
   `fix-calculation-context-writers-fe474f20`, session
   `opencode:ses_099a26dfaffe767cPSgowj9MN1`, timestamp `2026-07-17T17:39:05.188Z`).
2. `task-1-completed` for the same work_id, timestamp `2026-07-17T19:47:00.000Z`.

Seven rows follow, all for the owner-started Task 9 recovery work
(`phase-3-task-9-recovery-20260815`, session
`opencode:atlas-current-phase-3-task-9-recovery`):

- `start-work` (row 3) - owner command `$start-work .omo/plans/phase-3-task-9-recovery.md`.
- `task-completed` Todo 1 baseline (row 4).
- `task-completed` Todo 2 RED characterization (row 5).
- `task-completed` Todo 3 collection recovery (row 6).
- `task-completed` Todo 4 property recovery (row 7).
- `task-completed` Todo 5 helper session normalization (row 8).
- `task-completed` Todo 6 executor gates and scope audit (row 9).

These seven rows connect to the owner-started Task 9 recovery evidence receipts
under `docs/architecture-migration/evidence/phase-3-construction-state/`
(`task-9-recovery.md`, `task-9-failure-investigation-handoff.md`,
`task-9-persistence-standalone.md`). The ledger is therefore append-only
relative to HEAD: the two original rows are an exact prefix and the seven Task 9
recovery rows were appended. This is control-plane orchestration provenance, not
Phase 3 Tasks 1-13 implementation output.

## 2. `.omo/boulder.json` is control-plane orchestration state

- Schema version: `2`.
- Work IDs and statuses:
  - `phase-3-task-9-recovery-20260815` - `status: completed` (owner-started Task
    9 recovery; six `task_sessions` all `completed`).
  - `phase-3-construction-state` - `status: active` (current Phase 3
    orchestration; `task_sessions` empty).
- Active plan: `D:\IA\ace v.2\.omo\plans\phase-3-construction-state.md`
  (plan_name `phase-3-construction-state`), matching the top-level
  `active_plan` field.
- This file is control-plane orchestration state, not Phase 3 Tasks 1-13
  implementation output.

## 3. Phase 3.1 plans are byte-identical, owner-directed, queued, unapproved, unstarted

- Canonical: `docs/architecture-migration/plans/phase-3.1-climate-thermal-invalidation-on-project-load.md`.
- Tracking mirror: `.omo/plans/phase-3.1-climate-thermal-invalidation-on-project-load.md`.
- Both are byte-identical (same byte size and SHA-256, recorded in the
  verification section below).
- Phase 3.1 is an explicitly owner-directed queued successor positioned after
  Phase 3 owner acceptance and before Phase 4. It is not approved, not
  authorized, not started, and not completed. It is not Phase 3 Tasks 1-13
  implementation output.

## Classification

All three findings are exact-hash-bound control-plane or owner-directed
artifacts. Recording their provenance does not broaden any Phase 3 task
allow-list and does not waive future drift: arbitrary future metadata drift
remains rejectable by the same strict F1 rule.

## Workflow state (unchanged)

- `Stage = final-verification`.
- Phase result acceptance: `pending`.
- Historical F1 verdict: `REJECT`, pending independent re-review.
- F2-F4: `pending`.
- Phase 3.1: queued, unapproved, unstarted.

## Verification

Exact independently verified values (raw binary bytes):

- `.omo/start-work/ledger.jsonl` current: `11214` bytes, SHA-256
  `DD4918C4389CF897B602D3498845BF2796EA25C44113AA6C7A6197583EA6A3CC`.
- `.omo/start-work/ledger.jsonl` HEAD raw blob: `2162` bytes, SHA-256
  `F2D5D65C66AB69E5B085E7A646F2B51B9D9A01FDDDD2DD1956C11431405F4882`.
- Binary-safe prefix check: `current.startswith(head) = true`; appended tail is
  `9052` bytes (`11214 - 2162`).
- The earlier line-decoded PowerShell reconstruction was not authoritative.
  The authoritative comparison is the raw Python `subprocess` byte stream
  (`git show HEAD:.omo/start-work/ledger.jsonl` and the current file read as
  bytes), which confirms the two original rows are an exact binary prefix and
  the seven Task 9 recovery rows were appended.
- `.omo/boulder.json`: `4263` bytes, SHA-256
  `CB49B561ABD1BEE68818247D89975BB05ABC35ACB2FAF0963163E3E84EA81862`.
- Canonical Phase 3.1 plan
  (`docs/architecture-migration/plans/phase-3.1-climate-thermal-invalidation-on-project-load.md`):
  `19199` bytes, SHA-256
  `BE7A3091C4E4A1B05DD3052F0414458C1EE43228267049DCD71A2A217CFD4380`.
- Tracking Phase 3.1 plan
  (`.omo/plans/phase-3.1-climate-thermal-invalidation-on-project-load.md`):
  same `19199` bytes and same SHA-256
  `BE7A3091C4E4A1B05DD3052F0414458C1EE43228267049DCD71A2A217CFD4380`; byte
  identity `true`.
- Scoped `git diff --check` on the three changed files: exit `0`.

This receipt supplies remediation evidence only. It does not claim `F1 APPROVE`
and does not mark any Final Wave checkbox.
