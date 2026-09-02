# Architecture Migration Instructions

These instructions govern migration artifacts and related production or test
work. The active authority is the combination of the current plan under
`docs/architecture-migration/plans/` and the status and decision log in
`TASK_CONTEXT.md`. The workflow is linear and owner-controlled:

```text
primary Prometheus plan -> terminal Momus review -> /architecture-approve
-> /architecture-start -> sequential implementation -> three-domain final review
-> explicit owner result acceptance
```

The owner gates are explicit stops. Plan approval does not authorize execution;
`/architecture-start` is the separate execution authorization; final review
does not accept the result. Phase 6 and Phase 7 are completed and explicitly
accepted by the owner. The docs-only Phase 7.5 dossier refresh
(`phase-7.5-project-restore-coordinator-relaunch`) is completed as of
2026-09-03: Phase 7 overlays, corrected model evidence references, a
regenerated deterministic widget, the owner-adjudicated invariant statuses
(`INV-001`, `INV-011`, `INV-012`, `INV-013` verified/implemented) and the
owner-authorized verifier exemplar amendment are recorded in
`evidence/phase-7-project-restore-coordinator-relaunch/generation-hash-receipt.md`.
The next phase requires a separate planning workflow,
terminal review, owner plan approval, and separate execution authorization.
The archived `archive/STATE.json` and retired workflow scripts are provenance
only and are not active authority. A plan SHA may be recorded at freeze for
provenance, but no machine gate is required.

Read targeted history in `TASK_CONTEXT.md` for recovery, provenance, or
supersession. Append history only for a material decision, blocker,
supersession, or completed gate.

## Authority and gates
- A mutable draft becomes a frozen candidate only for terminal review.
- The primary Prometheus plan is reviewed by one terminal Momus review. The
  receipt records the reviewed plan identity and verdict.
- `/architecture-plan` stops after the terminal review at explicit owner plan
  approval. `/architecture-approve` records approval in the dossier only and
  does not authorize execution.
- `/architecture-start` is the explicit execution authorization. It uses the
  frozen plan and dossier, executes the production lane sequentially, and stops
  after final review for explicit owner result acceptance.
- Plan approval, execution authorization, and result acceptance are separate
  owner decisions. None is implied by another or by conversation history.
- Do not edit a frozen plan after review; a correction creates a new candidate
  and terminal review. Preserve the dirty baseline-relative delta.

## Environment-adaptive operation (non-OpenCode sessions)

Prometheus and Momus are OpenCode agent identities. When a session runs in
another environment (for example ZCode) where those named agents do not exist,
the workflow adapts as follows while every owner gate stays intact:

- The terminal plan review is performed by the acting agent itself, optionally
  cross-checked by one read-only independent subagent pass, and is recorded as
  a machine-readable receipt with the same five fields (`REVIEW_ID`,
  `SUBJECT`, `RECEIPT`, `VERDICT`, `REASON`) under the phase evidence
  directory.
- Plan approval, execution authorization, and result acceptance remain
  explicit owner statements in the session. None is implied by the review or
  by conversation context. Each completed gate is appended to
  `TASK_CONTEXT.md` as a dated entry.
- Evidence commands may be adapted to the local shell (for example Git Bash
  instead of cmd redirection), but build-before-test, focused filters, TRX
  logs, receipt paths, and the zero-test failure rule are preserved exactly.
- All other invariants (sequential lane, characterization-first, stop rules,
  write-set discipline) apply unchanged.

## Invariants
- `ProjectSession` is the aggregate root, with explicit climate, construction,
  thermal, and hydraulics slices.
- Each value has one writable canonical owner after its migration step.
- ViewModels are WPF adapters, not canonical state stores.
- Services do not depend on concrete ViewModels.
- Results is derived and does not own module inputs.
- Supported `.smc` behavior and wire compatibility remain intact unless a
  separately approved change says otherwise.
- Production implementation uses one sequential vertical-slice lane.
- Characterization-first and production verification are never weakened.

## Change classes and evidence
Classify every write-set as one or more of:

1. control/docs-only;
2. production/test;
3. architecture artifacts;
4. user-visible.

Rerun evidence only when a covered input changed. Control-only transitions
reuse technical evidence. Plan SHA may be recorded at freeze for provenance;
there is no machine-enforced identity or mirror gate.
Production or architecture changes preserve characterization tests,
persistence fixtures, invariant checks, and the sequential lane. They also
assess all six architecture views and widget/model inputs, refreshing affected
artifacts or recording why they remain unchanged.

Manual QA is mandatory once per frozen write-set for each affected
user-visible flow. It is not required for docs-only or control-only changes.
Final verification has three independent domains and one consolidated receipt:

1. Conformance / Scope / Provenance;
2. Architecture / Code Quality;
3. Executable QA / User Risk.

One reviewer cannot replace these domains. The receipt names the write-set,
evidence reused or rerun, and residual risks.

## Review contract
The primary Prometheus plan is followed by one terminal Momus plan review;
do not recreate the old multi-loop control chain. Minor correction may use one
combined review. Material or new architecture may use Metis only for genuine
ambiguity before the primary plan.
Terminal reviewer output is machine-readable and contains exactly these
fields:

```text
REVIEW_ID: <id>
SUBJECT: <frozen plan or result>
RECEIPT: <consolidated receipt path or inline receipt>
VERDICT: APPROVE|REJECT|BLOCKED
REASON: <specific reason>
```

Missing or malformed receipt permits one same-session correction retry only. A
second failure is `BLOCKED`; do not rerun executable evidence for that
formatting failure.

## Tooling and stop rules

- LSP is tried once per session and only for a supported source extension.
- If the LSP effective workspace root or request cwd is outside the repository,
  record it once and use compiler/tests. Do not claim Markdown LSP without a
  configured server.
- Use a dirty baseline-relative delta; never treat unrelated dirty paths as
  this task's evidence.
- Never begin a second central slice in parallel. Stop at every missing owner
  decision, scope drift, failed verification, or unresolved owner decision.
  After result acceptance, await a new explicit owner direction; never start
  the next phase implicitly.
- Do not commit, stage, reset, revert, clean, install tools, or overwrite
  unrelated dirty worktree files.
