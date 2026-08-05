# Architecture Migration Agent Instructions

These instructions apply to every artifact in this directory and to any
production-code work performed as part of this architecture migration.

## Required Start

Before analysis, planning, delegation, or edits:

1. Read `TASK_CONTEXT.md` in full.
2. Verify the workspace with `git rev-parse --show-toplevel`.
3. Inspect the current dirty worktree without reverting or staging unrelated
   user changes.
4. Re-verify relevant claims against current source. The supplied audit was
   created for `D:\IA\ace`, not `D:\IA\ace v.2`.

## Source of Truth

- `TASK_CONTEXT.md` stores current status, decisions, open questions, next
  action, and the decision log.
- `architecture_audit.md` is historical input, not automatically current.
- `architecture_widget.html` is a presentation artifact, not the sole source
  of architectural truth.
- `maps/` stores the six detailed architecture views and state inventory.
- `evidence/` stores reproducible build, test, graph, and user-flow receipts.
- `plans/` stores approved phase plans and rollback boundaries.
- `archive/` stores superseded baselines and generated widget versions.

## Required Architecture Views

Maintain these six views as separate filters over one shared architecture
model:

1. Compile-time.
2. DI/runtime.
3. State ownership.
4. Reactive behavior.
5. Persistence.
6. User flow.

Do not collapse these into one dependency graph. Every documented edge must
identify its kind and current source evidence.

## Migration Invariants

- `ProjectSession` is the aggregate root of the current project, not a flat god
  object.
- Climate, construction, thermal, and hydraulics have explicit state slices.
- Every value has one writable canonical owner after its migration phase.
- ViewModels are WPF adapters, not shared canonical state stores.
- Application services do not depend on concrete ViewModels.
- Results is a derived projection and does not own module inputs.
- Existing supported `.smc` behavior and wire format remain compatible unless
  a separately approved migration changes them.
- Production migration proceeds as one sequential implementation lane by
  vertical slice.

## Change Gates

Do not begin ownership migration until the baseline dossier, state inventory,
characterization tests, persistence fixtures, and target invariants are ready.

After every structural change:

1. Run targeted and affected integration tests.
2. Validate the relevant architecture invariants.
3. Update the shared architecture model, maps, widget, and evidence.
4. Run `dotnet build` and the required phase test gate.
5. Exercise the affected user flow.
6. Update `TASK_CONTEXT.md` before handing off or ending the task.

## Context Update Contract

After every material finding, decision, or completed phase, update at least:

- `Подтверждённые текущие наблюдения` when facts change;
- `Текущий статус`;
- `Принятые решения`;
- `Открытые вопросы`;
- `Следующее действие`;
- `Журнал решений`;
- links to generated evidence and the current widget when available.

Do not silently rewrite prior decisions. Record the date, the new decision,
and why it supersedes the previous one.

## Prohibited Actions

- Do not copy stale metrics into current artifacts.
- Do not treat a green build as proof of preserved runtime behavior.
- Do not leave two writable canonical state stores as a completed phase.
- Do not migrate multiple central state slices in parallel.
- Do not change formulas, UI design, package versions, persistence schema, or
  release artifacts as incidental architecture cleanup.
- Do not commit, revert, stage, or overwrite unrelated dirty worktree files.
