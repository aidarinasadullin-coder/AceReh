---
title: "OpenCode session, regression, failure, and tooling analysis"
labels: [control/docs-only, navigation/provenance-only, non-authoritative]
status: "evidence-backed analysis"
created: "2026-08-26"
---

# OpenCode session, regression, failure, and tooling analysis

## Authority and scope

This is a non-authoritative, read-only analysis of OpenCode session history for
`D:\IA\3ace v.2`. It does not change production code, tests, frozen plans,
architecture maps, the model, the widget, product-bug receipts, or OpenCode
configuration. It grants no execution authorization and does not close any
product bug.

The analysis covers the local OpenCode SQLite store available on 2026-08-26,
with emphasis on the current worktree and on sessions that contain explicit
test, tool, failure, blocker, or regression evidence. A session-level search
hit is a candidate, not proof of a failure; findings below require a concrete
result, status, or limitation in the transcript.

## Reproducible inventory

- Store: `C:\Users\Admin\.local\share\opencode\opencode.db`.
- Project directory filter: `D:/IA/3ace v.2`.
- Sessions: `339` total, consisting of `16` root sessions and `323` child or
  subagent sessions.
- Messages: `9,966`; raw parts: `45,754`.
- Observed range: `2026-08-21 19:02:06` through `2026-08-26 12:42:15`.
- The latest bounded snapshot was captured at `2026-08-26T12:38:54.783Z`; it
  contained `339` sessions, `16` roots, `323` children, `45,754` parts, and
  `9,966` messages. The database continued to receive this audit session after
  that snapshot, so later rows are not silently mixed into the counts above.
- The built-in session listing and bundled finder did not return this project;
  the inventory therefore used a read-only SQLite fallback. The `opencode`
  CLI was unavailable in the execution environment.
- Parent/child relationships were read from `session.parent_id`; raw evidence
  was read from `part.data`.

The inventory is an audit denominator, not a claim that all 339 sessions are
independent implementation attempts. Many are delegated research, review,
retry, continuation, or duplicate subagent sessions.

## Confirmed findings

### SA-001: Intentional RED runs were sometimes recorded as failures

Session `ses_057d34091ffesjrbF9EpaqMKFs` explicitly describes a failing-first
test task. Its contract required intentional failures before production edits;
the recorded full run was `10 failed, 1520 passed, 1 skipped`. The same session
records the targeted run as `8 failed, 2 passed` and identifies the expected
pre-fix stale Results behavior.

This is not a product regression by itself. It is a workflow classification
risk: aggregate searches for `failed` can incorrectly count TDD RED evidence as
an unresolved defect unless task intent and the final follow-up run are read.

Confidence: high; the task contract and the recorded test results are in the
same transcript.

### SA-002: Real regressions were exposed, fixed, and re-verified

The historical regression cycle includes a recorded failing gate of
`328 passed, 1 failed, 1 skipped` in `ses_ff62e48f8001f7d5Ecw5XDW8HR`, followed
by a rerun in `ses_ff69980d8001isatdxHJfMJvCU` reporting `1613 passed, 1 skipped,
0 failed`. The latter also records successful Debug/Release builds.

The evidence supports a resolved regression cycle, not a currently open
failure. The transcript search does not by itself establish that every
intermediate failure had the same root cause; the finding is limited to the
observed red-to-green verification sequence.

Confidence: medium-high; the counts and rerun are explicit, while the exact
test name/root cause is not fully reproduced in this dossier.

### SA-003: API connectivity can terminate a delegated session

In the background research chain associated with
`ses_056b09580ffe1kkW4eU2FCHt8C`, one attempt is recorded as:
`Cannot connect to API: Connect Timeout Error`, with IPv6 endpoint addresses
and a 10-second timeout. The same chain records a fallback model retry that
completed.

This is an infrastructure/model-route failure, not evidence of a repository
failure. The observed recovery mechanism is model fallback and continuation,
but the initial failed attempt still consumes time and can make a parent task
appear interrupted if the handoff is not persisted.

Confidence: high for the timeout; medium for its impact on the parent because
the parent/child continuation semantics vary by invocation.

### SA-004: LSP request-root mismatch is a repeatable tooling limitation

The session evidence records diagnostics failing with
`LSP file path must be inside request cwd: D:\...`, while the repository
context records that `csharp-ls 0.16.0` is installed and that the external LSP
harness selects `C:\Users\Admin` as its workspace root. The project therefore
has a configured C# server, but the diagnostics request boundary is wrong.

This explains why compiler and test commands are the correctness gates for
production C# work. It is not evidence that the C# code itself failed to
compile.

Confidence: high; the error text and the configured-server/current-root facts
are explicit.

### SA-005: Workflow blockers were often technical-control blockers, not code
  regressions

The history contains repeated `BLOCKED` states for missing receipts, malformed
or incomplete evidence, stale phase status, unavailable harnesses, and
owner-gated decisions. Examples include `ses_050fe247affe34fOfL8Qa5Kamr`,
`ses_fd346e9ad001TnWft5qPy7p61p`, and `ses_ff62e48f8001f7d5Ecw5XDW8HR`.

The records also show recovery to `executing` or later green verification in
some chains. Therefore a `BLOCKED` token must be classified by its reason and
subsequent state transition; it must not be counted as a product defect or a
terminal session crash without that context.

Confidence: medium-high; the state reasons are explicit, but this dossier does
not assign a complete count to every blocker subtype.

### SA-006: Session history is vulnerable to inefficient continuation patterns

The inventory shows `323` child sessions under `16` roots. The transcripts
contain repeated retries, continuation prompts, compaction recovery, and
subagent duplications. This is observable orchestration overhead, but the
available store does not expose a reliable cost or wall-time metric for every
child. No numerical efficiency claim is made here.

Confidence: high for the topology; low for any unmeasured token/cost impact.

### SA-007: Confirmed operation errors were child-session events, not root failures

The bounded snapshot contains `133` final error-tail candidates. This is a
candidate count, not a failure count: `109` are `MessageAbortedError` tails,
`18` are inherited `Tool execution aborted` tails, `4` are API errors, and `2`
are direct tool errors. The direct/API operation-error set is therefore `6`
child sessions, not `6` failed root workflows.

The four API errors were model/provider or connectivity failures: regional
model availability, connect timeout, DNS resolution failure, and an unavailable
provider endpoint. The two direct tool errors were an LSP request-cwd mismatch
and an exact-text edit mismatch. All six occurred below a root that has a later
`step-finish`, except the edit error below the currently active audit root;
there is no confirmed root-level terminal operation error in this snapshot.

The root `ses_fc8338cf5ffeTFFu7wKDV7ftrn` has only its initial prompt followed
by `MessageAbortedError`, with no tool result, command result, or provider error.
It is therefore an ambiguous interruption, not evidence of a product or tool
failure. The current root `ses_fc7375e05ffeORw9ZfGOfWCO79` is still marked
`running` and must not be classified as failed.

Confidence: high for the observed tails and parent/root relationships; low for
any claim about the user's intent behind the ambiguous root abort.

## Regression and failure taxonomy

| Class | Evidence rule | Current interpretation |
|---|---|---|
| Intentional RED | Task explicitly requires failing-first tests and later green work | Do not count as unresolved regression |
| Product regression | A non-intentional gate fails, then a focused fix and rerun are recorded | Track by test/root cause; current sampled cycle resolved |
| Tool/runtime failure | API timeout, missing CLI, LSP cwd mismatch, harness limitation | Improve routing or use documented fallback; not a product defect |
| Workflow/control blocker | Missing receipt, owner gate, stale state, malformed evidence | Preserve stop semantics; improve recovery/reporting |
| Duplicate/continuation overhead | Child session repeats the same research or recovery context | Candidate efficiency issue; requires cost-aware measurement |
| Unproven claim | Search hit without a concrete command result or final state | Exclude from findings |

## Recommendations for a later owner checkpoint

These are recommendations only and do not authorize tool or configuration
changes.

1. Add a repository-scoped session audit command that queries the SQLite store
   by normalized path and emits root, parent, agent, start/end, terminal state,
   and child count. Keep the direct SQLite fallback documented while the CLI
   does not discover this project.
2. Make failure records structured at the tool boundary: command, exit code,
   timeout, retry number, session ID, and whether the attempt was intentional
   RED. This would prevent keyword-only searches from conflating test design
   with regressions.
3. Fix or explicitly configure the LSP request cwd to the repository root. If
   that cannot be changed, keep the one-attempt rule and surface the known
   fallback to `dotnet build/test` automatically in the workflow receipt.
4. Use one bounded executor per regression cycle and reuse its continuation
   session for retries. Keep research, implementation, and verification
   sessions distinct so duplicate child trees do not obscure the terminal
   result.
5. Persist a compact phase checkpoint containing current task, last verified
   command, exact failure, next action, and owner gate. This reduces loss of
   state across compaction, API fallback, and manual interruption.
6. Measure efficiency only after adding stable fields for elapsed time, retry
   count, child count, and terminal outcome. Do not optimize from raw session
   counts alone.

## Limitations and non-findings

- The audit did not prove a root cause for the `40.1 -> 39.9 kW` symptom.
- The audit did not prove a true session-level failure percentage. OpenCode does
  not store one authoritative terminal outcome for a root and the error tails
  include propagated child aborts.
- It did not calculate token cost, model cost, or true wall-clock efficiency;
  those metrics are not reliably present in the inspected records.
- It did not treat every `failed`, `blocked`, or `timeout` keyword as a failure.
- The built-in session index and CLI discovery path were unavailable for this
  worktree, so the SQLite fallback is part of the provenance and should be
  rerun if the store schema changes.
- External web/tool research was not used as product evidence in this dossier.

## Exact write-set

This dossier is a `control/docs-only` and `navigation/provenance-only` write.
The accompanying context pointer and backlog navigation entry are append-only
or navigational updates. No production/test/architecture artifact was changed.

Snapshot: `2026-08-26T12:38:54.783Z`

STATUS: PASS
