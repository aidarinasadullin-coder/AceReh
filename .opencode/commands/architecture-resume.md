---
description: Resume the architecture workflow from validated STATE.json without crossing owner gates.
---

Resume `$ARGUMENTS` from `docs/architecture-migration/STATE.json`, the sole
active authority. Normal recovery does not require a full `TASK_CONTEXT.md`
read; consult targeted history only for provenance, supersession, or recovery.

When an active frozen plan exists, run before routing:

```text
node docs/architecture-migration/workflow/validate-state.mjs validate --check-plan
```

Reconcile state, exact plan SHA/mirror, receipts, write-set, and dirty
baseline-relative delta. Any stale, missing, malformed, or contradictory
record fails closed. Never edit a stale plan or Boulder to satisfy a hook; if
an external hook blocks on Boulder mismatch, report it and stop.

Route without crossing gates:

- planning or no frozen plan: `/architecture-plan <phase>`;
- frozen plan awaiting owner approval: `/architecture-approve <phase>`;
- approved but not authorized: report the missing execution authorization;
- authorized incomplete work: `/architecture-start <phase>` in resume mode
  from the first incomplete task, preserving authorization and reusing valid
  evidence;
- awaiting result acceptance: report the receipt and stop unless the current
  owner message explicitly accepts or rejects the result; on explicit
  acceptance set `resultAcceptance=accepted`, `stage=completed`, clear pending
  gates, set `stop=true`, validate, and STOP; on rejection record `rejected`,
  set `stage=blocked` with the owner's reason, validate, and STOP;
- completed: report completion and STOP; never restart completed work;
- blocked: report the recorded blocker and only its safe, non-owner-gate
  recovery action.

Do not infer approval or authorization from conversation, session IDs, or old
Boulder state. Preserve characterization-first verification and the sequential
implementation lane. Evidence is rerun only when covered inputs changed;
validator and plan identity are always fresh. LSP has one attempt per session
on supported source extensions, with compiler/tests fallback when its effective
workspace root or request cwd is outside the repo; no Markdown LSP without a
configured server. Manual QA is once per frozen
write-set for affected user-visible flows, never docs/control-only.

Terminal receipts use exactly `REVIEW_ID`, `SUBJECT`, `RECEIPT`, `VERDICT
APPROVE|REJECT|BLOCKED`, and `REASON`. Missing or malformed receipt permits one
same-session correction/materialization retry; the second failure is BLOCKED
without executable-evidence rerun. Final verification remains one receipt
covering Conformance/Scope/Provenance, Architecture/Code Quality, and
Executable QA/User Risk.
