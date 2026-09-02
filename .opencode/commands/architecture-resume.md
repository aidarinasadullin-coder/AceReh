---
description: Resume the architecture workflow from the dossier and current plan without crossing owner gates.
---

Resume `$ARGUMENTS` from the current plan under
`docs/architecture-migration/plans/` and the latest status/decision context in
`docs/architecture-migration/TASK_CONTEXT.md`. Consult targeted history only
for provenance, supersession, or recovery.

Route without crossing owner gates:

- no current decision-complete plan: `/architecture-plan <phase>`;
- terminal Momus review complete but owner approval absent:
  `/architecture-approve <phase>`;
- owner plan approval recorded but execution authorization absent: report that
  `/architecture-start <phase>` is required;
- explicitly authorized incomplete work: `/architecture-start <phase>` in
  resume mode from the first incomplete task, preserving the sequential lane;
- final three-domain review complete: report the receipt and stop for explicit
  owner result acceptance;
- current owner explicitly accepts the result: record it in the dossier and
  STOP;
- blocked: report the recorded blocker and only its safe recovery action.

Never infer approval, authorization, or acceptance from conversation history,
session IDs, hashes, or retired control-plane artifacts. Preserve
characterization-first verification, six-view/evidence requirements, dirty
baseline safety, and sequential implementation. Rerun evidence only when a
covered input changed. Do not restart completed work or cross an owner gate.
