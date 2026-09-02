---
description: Record owner approval of a frozen architecture plan without executing it.
---

Approve `$ARGUMENTS` using the frozen plan under
`docs/architecture-migration/plans/` and the latest status and decision context
in `docs/architecture-migration/TASK_CONTEXT.md`. Confirm the requested phase,
the terminal Momus receipt, and the reviewed plan identity recorded for
provenance.

This explicit owner command records plan approval in the dossier only. It does
not authorize execution, modify production or tests, or infer authorization
from review, prior intent, session IDs, or plan identity. STOP after recording
approval. The next explicit gate is `/architecture-start <phase>`.
