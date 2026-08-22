---
description: Create and review a frozen architecture migration plan, then stop for owner approval.
---

Plan `$ARGUMENTS` using `docs/architecture-migration/STATE.json` as the sole
active authority. Do not use full `TASK_CONTEXT.md` during normal flow. This
command is planning-only and must not execute production work.

## Gate and routing

Run, before routing, when an active frozen plan exists:

```text
node docs/architecture-migration/workflow/validate-state.mjs validate --check-plan
```

Fail closed on invalid state, plan identity, SHA, mirror, phase, or owner
gate. Confirm the requested phase against state and inspect only targeted
history for recovery or supersession. Preserve the dirty baseline-relative
delta.

## Workflow

1. Classify the change: control/docs-only, production/test, architecture
   artifacts, or user-visible. Reuse evidence unless a covered input changed.
2. Create one mutable draft with scope, dependencies, invariants, write-set,
   rollback, evidence, and risk-based QA. Keep the sequential implementation
   lane and characterization-first verification.
3. Materialize and hash the frozen candidate before terminal review. For minor
   correction, use one combined review. For material or new
   architecture, invoke Metis only for genuine ambiguity, then one terminal
   plan critic. No per-edit hash ceremony and no multi-loop reviewer chain.
4. Apply at most one same-session materialization/correction retry if the
   terminal receipt is missing or malformed. A second failure is BLOCKED and
   does not rerun executable evidence.
5. Bind the approving planning receipt to `<phase>@<plan-sha256>`. Do not edit
   frozen bytes after approval; a correction creates a newly hashed candidate
   and new terminal review. Write the exact plan and planning-receipt identities
   to state, set `stage=awaiting-owner-approval`, keep all owner gates pending,
   then validate state and plan again.

The terminal critic must return exactly:

```text
REVIEW_ID: <id>
SUBJECT: <frozen plan>
RECEIPT: <receipt path or inline receipt>
VERDICT: APPROVE|REJECT|BLOCKED
REASON: <specific reason>
```

Do not infer plan approval, execution authorization, or result acceptance.
Record the reviewed plan and stop with the next command
`/architecture-approve <phase>`. Do not edit production, tests, history, or
Boulder in this command; state, the canonical/mirror plan artifacts, and the
identity-bound planning receipt are the only allowed writes.
