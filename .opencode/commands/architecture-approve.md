---
description: Record owner approval of a validated frozen architecture plan without executing it.
---

Approve `$ARGUMENTS` only from `docs/architecture-migration/STATE.json`, the
sole active authority. Do not read full `TASK_CONTEXT.md` during normal flow;
use targeted history only to resolve provenance or supersession.

Before routing, if an active frozen plan exists, run:

```text
node docs/architecture-migration/workflow/validate-state.mjs validate --check-plan
```

Fail closed unless state proves the requested phase, `stage` is
`awaiting-owner-approval`, the exact frozen plan SHA and mirror match, planning
review is approved, and plan approval is still pending. The
terminal receipt must contain exactly `REVIEW_ID`, `SUBJECT`, `RECEIPT`,
`VERDICT` (`APPROVE`, `REJECT`, or `BLOCKED`), and `REASON`. A missing or
malformed receipt gets one same-session correction/materialization retry only;
the second failure is BLOCKED, with no executable-evidence rerun.

This explicit command invocation records only plan approval. Atomically set
`stage=approved`, `ownerGates.planApproval=approved`, and `nextAction` to
`/architecture-start <phase>`; keep execution authorization and result
acceptance pending, validate state again, and STOP. Do not infer execution
authorization from review, prior intent, or a matching SHA. Preserve the dirty
baseline-relative delta. Do not execute work, edit production/tests, rewrite
history, or alter Boulder to satisfy a hook.
