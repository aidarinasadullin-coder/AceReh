---
description: Execute an owner-approved frozen architecture plan and stop for result acceptance.
---

Execute `$ARGUMENTS` only after the owner has explicitly approved the frozen
plan with `/architecture-approve <phase>` and now explicitly authorizes
execution with this command. Use the frozen plan under
`docs/architecture-migration/plans/` and the dossier status/decision log; do not
rely on retired state files, scripts, or mandatory tracking mirrors.

Execute the frozen write-set exactly, preserving the `ProjectSession` aggregate
root, explicit slices, single writable owner, adapter ViewModels, service
boundaries, derived Results, `.smc` compatibility, characterization tests, and
one sequential implementation lane. Use a dirty baseline-relative delta.
Assess all six architecture views and widget/model inputs; refresh affected
artifacts or record why they remain unchanged. Manual QA is mandatory once per
frozen write-set for affected user-visible flows, not for docs/control-only
work.

After implementation, run three independent final-review domains and issue one
consolidated receipt: (1) Conformance/Scope/Provenance, (2) Architecture/Code
Quality, and (3) Executable QA/User Risk. STOP for explicit owner result
acceptance. Never mark completion or infer acceptance.

The terminal receipt must contain exactly:

```text
REVIEW_ID: <id>
SUBJECT: <phase>@<plan-sha256-or-recorded-plan-identity>
RECEIPT: <consolidated receipt path or inline receipt>
VERDICT: APPROVE|REJECT|BLOCKED
REASON: <specific reason>
```
