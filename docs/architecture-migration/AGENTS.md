# Architecture Migration Instructions

These instructions govern migration artifacts and related production or test
work. `STATE.json` is the sole active authority for stage, plan identity,
owner gates, evidence freshness, and next action. Validate a frozen plan before
routing:

```text
node docs/architecture-migration/workflow/validate-state.mjs validate --check-plan
```

Normal startup reads this file and `STATE.json`, not full `TASK_CONTEXT.md`.
Read targeted history only for recovery, provenance, or supersession. Append
history only for a material decision, blocker, supersession, or completed gate.

## Authority and gates
- A mutable draft becomes a frozen candidate only for terminal review.
- A frozen candidate is materialized at the exact plan path and exact SHA
  recorded in `STATE.json`; mirrors must match that identity.
- Planning and final receipts use separate state paths and bind `SUBJECT` to
  `<phase>@<plan-sha256>` before their gates can advance.
- Hash only at freeze, import, and validation boundaries, not after each edit.
- Plan approval, execution authorization, and result acceptance are separate
  owner gates. None is implied by another or by conversation history. Only the
  matching explicit owner command or acceptance statement records each gate.
- A missing, stale, or contradictory state, plan, receipt, or SHA fails closed.
- Do not edit a stale plan or Boulder state to satisfy a hook. If an external
  hook blocks progress on a Boulder mismatch, report the mismatch and stop.

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

Rerun evidence only when a covered input changed. State and plan identity
checks are always fresh; control-only transitions reuse technical evidence.
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
Minor correction may use one combined review. Material or new architecture
uses Metis only for genuine ambiguity, followed by one terminal plan critic;
do not recreate the old Metis -> Prometheus -> Sisyphus -> Momus loop.
Terminal reviewer output is machine-readable and contains exactly these
fields:

```text
REVIEW_ID: <id>
SUBJECT: <frozen plan or result>
RECEIPT: <consolidated receipt path or inline receipt>
VERDICT: APPROVE|REJECT|BLOCKED
REASON: <specific reason>
```

Missing or malformed receipt permits one same-session materialization or
correction retry only. A second failure is `BLOCKED`; do not rerun executable
evidence for that formatting failure.

## Tooling and stop rules

- LSP is tried once per session and only for a supported source extension.
- If the LSP effective workspace root or request cwd is outside the repository,
  record it once and use compiler/tests. Do not claim Markdown LSP without a
  configured server.
- Use a dirty baseline-relative delta; never treat unrelated dirty paths as
  this task's evidence.
- Never begin a second central slice in parallel. Stop on a missing owner gate,
  invalid identity, stale state, scope drift, failed verification, or unresolved
  owner decision. After result acceptance set `completed`, set `stop=true`, and
  await a new explicit owner direction; never start the next phase implicitly.
- Do not commit, stage, reset, revert, clean, install tools, or overwrite
  unrelated dirty worktree files.
