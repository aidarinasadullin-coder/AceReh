---
description: Execute an owner-approved frozen architecture plan and stop for result acceptance.
---

Execute `$ARGUMENTS` only when `docs/architecture-migration/STATE.json` proves
the current phase and exact frozen plan identity. For a first start,
`stage=approved`, owner plan approval, and pending execution authorization are
required; this explicit invocation is the separate execution authorization.
For resume, require `stage=executing|verification` and already-approved
execution authorization, then continue at the first incomplete task without
recording that gate again. State is the sole active authority; do not blindly
rely on `/start-work` or stale Boulder state.

If an active frozen plan exists, run before routing:

```text
node docs/architecture-migration/workflow/validate-state.mjs validate --check-plan
```

Fail closed on any mismatch, stale identity, missing required receipt, scope
drift, or owner gate. On first start only, atomically set `stage=executing`, set
`ownerGates.executionAuthorization=approved`, set `nextAction` to the first
approved-plan task, and validate again. On resume, preserve the approved gate
and persisted stage. Do not edit stale plan/Boulder state to satisfy a hook;
report a Boulder mismatch and stop if an external hook blocks.

Execute the frozen write-set exactly, preserving the `ProjectSession`
aggregate root, explicit slices, single writable owner, adapter ViewModels,
service boundaries, derived Results, `.smc` compatibility, characterization
tests, and one sequential implementation lane. Classify changes as
control/docs-only, production/test, architecture artifacts, or user-visible.
Use a dirty baseline-relative delta. Reuse evidence unless covered inputs
changed; state validation and plan identity are always fresh. Assess all six
architecture views and widget/model inputs; refresh affected artifacts or
record why they remain unchanged.

LSP is attempted once per session for supported source extensions only. If its
effective workspace root or request cwd is outside the repo, record it once and
use compiler/tests; no Markdown LSP is claimed without a configured server.
Manual QA is mandatory once per frozen
write-set for each affected user-visible flow, not for docs/control-only work.

After implementation, verify three independent domains and issue one
consolidated receipt: (1) Conformance/Scope/Provenance, (2) Architecture/Code
Quality, (3) Executable QA/User Risk. A single reviewer cannot replace them.
The terminal receipt must contain exactly:

```text
REVIEW_ID: <id>
SUBJECT: <phase>@<plan-sha256>
RECEIPT: <consolidated receipt path or inline receipt>
VERDICT: APPROVE|REJECT|BLOCKED
REASON: <specific reason>
```

Missing or malformed receipt allows one same-session materialization/correction
retry only. A second failure is BLOCKED without rerunning executable evidence.
After all domains approve, set `stage=awaiting-owner-acceptance`, keep result
acceptance pending, validate, and STOP. Never mark completion or infer owner
acceptance.
