---
description: Create and review a frozen architecture migration plan, then stop for owner approval.
---

Plan `$ARGUMENTS` using the current plan under
`docs/architecture-migration/plans/` and the status/decision log in
`docs/architecture-migration/TASK_CONTEXT.md`. This command is planning-only
and must not execute production work.

## Workflow

1. Read the relevant dossier context and classify the change as control/docs-only,
   production/test, architecture artifacts, or user-visible. Preserve the
   dirty baseline-relative delta.
2. Have the primary Prometheus planning lane create one decision-complete plan
   with scope, dependencies, invariants, write-set, rollback, evidence, and
   risk-based QA. Keep characterization-first verification and one sequential
   production lane.
3. Send the frozen candidate to one terminal Momus review. For genuine
   ambiguity only, use Metis before the primary plan. A plan SHA may be recorded
   at freeze for provenance; no mirror or machine gate is required.
4. Permit at most one same-session correction retry for a missing or malformed
   terminal receipt. A second failure is BLOCKED and does not rerun executable
   evidence.
5. Record the reviewed plan and receipt in the dossier, preserve frozen bytes,
   and STOP for `/architecture-approve <phase>`.

The terminal critic must return exactly:

```text
REVIEW_ID: <id>
SUBJECT: <frozen plan>
RECEIPT: <receipt path or inline receipt>
VERDICT: APPROVE|REJECT|BLOCKED
REASON: <specific reason>
```

Do not infer plan approval, execution authorization, or result acceptance. Do
not edit production, tests, or unrelated history in this command.
