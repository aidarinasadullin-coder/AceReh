---
description: Produce a decision-complete mutable architecture plan draft.
agent: prometheus
---

Draft `$ARGUMENTS` from `docs/architecture-migration/STATE.json`, which is the
sole active authority. Normal flow does not require a full
`TASK_CONTEXT.md` read; use targeted history only for recovery, provenance, or
supersession. Do not edit repository files or execute the phase.

Run the state validator before accepting a frozen-plan context:

```text
node docs/architecture-migration/workflow/validate-state.mjs validate --check-plan
```

Produce one mutable Markdown draft, not a hash for each edit. It must be
decision-complete and classify the write-set as control/docs-only,
production/test, architecture artifacts, or user-visible. Preserve the
`ProjectSession` aggregate root, explicit slices, one writable owner, adapter
ViewModels, service boundaries, derived Results, `.smc` compatibility,
characterization-first verification, and the sequential implementation lane.

Include exact paths, dependencies, scope and rollback, dirty baseline-relative
delta handling, evidence inputs and freshness, three final verification
domains, six-view/widget-model impact (refresh or justified unchanged), and
risk-based manual QA. Manual QA is required once per frozen
write-set for affected user-visible flows and is omitted for docs/control-only
work. State validator and plan identity checks are always fresh; technical
evidence is rerun only when covered inputs change.

For genuine ambiguity only, request Metis input. Otherwise proceed to one
terminal plan critic. The critic's terminal output must contain exactly:

```text
REVIEW_ID: <id>
SUBJECT: <candidate plan>
RECEIPT: <receipt path or inline receipt>
VERDICT: APPROVE|REJECT|BLOCKED
REASON: <specific reason>
```

Freeze and hash the candidate before terminal review; bind its receipt subject
to `<phase>@<plan-sha256>`. If correction changes bytes, create a new identity
and review. If materialization yields a missing or malformed receipt, allow
one retry in this session only; a second failure is BLOCKED without rerunning
executable evidence. Return the complete draft for the owner-controlled
freeze/import step. Do not claim that a file, approval, execution
authorization, or acceptance was recorded.
