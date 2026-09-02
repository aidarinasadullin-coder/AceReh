---
description: Produce a decision-complete mutable architecture plan draft.
agent: prometheus
---

Draft `$ARGUMENTS` from the current plan context in
`docs/architecture-migration/TASK_CONTEXT.md` and targeted dossier evidence.
Do not edit repository files or execute the phase.

Produce one mutable Markdown draft, not a hash for each edit. It must be
decision-complete and classify the write-set as control/docs-only,
production/test, architecture artifacts, or user-visible. Preserve the
`ProjectSession` aggregate root, explicit slices, one writable owner, adapter
ViewModels, service boundaries, derived Results, `.smc` compatibility,
characterization-first verification, and the sequential implementation lane.

Include exact paths, dependencies, scope and rollback, dirty baseline-relative
delta handling, evidence inputs and freshness, three final verification
domains, six-view/widget-model impact (refresh or justified unchanged), and
risk-based manual QA. Manual QA is required once per frozen write-set for
affected user-visible flows and is omitted for docs/control-only work. A plan
SHA may be recorded at freeze for provenance; no machine identity or mirror gate
is required.

For genuine ambiguity only, request Metis input. Otherwise route the primary
Prometheus plan to one terminal Momus critic. The critic's terminal output must
contain exactly:

```text
REVIEW_ID: <id>
SUBJECT: <candidate plan>
RECEIPT: <receipt path or inline receipt>
VERDICT: APPROVE|REJECT|BLOCKED
REASON: <specific reason>
```

Return the complete draft for the owner-controlled freeze/review step. Do not
claim that a file, approval, execution authorization, or acceptance was
recorded.
