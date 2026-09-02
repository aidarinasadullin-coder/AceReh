# Phase 7.5 dossier refresh - execution plan

## Класс изменения
control/docs-only

## Цель
Выполнить docs-only correction для принятого Phase 7 dossier state, не трогая production code, tests и исторические артефакты. План фиксирует только будущую исполнительную работу и стопается на owner plan approval; ` /architecture-start ` не входит в этот этап.

## Allow-list
- `docs/architecture-migration/AGENTS.md`
- `docs/architecture-migration/maps/architecture-model.json`
- `docs/architecture-migration/maps/compile-time.md`
- `docs/architecture-migration/maps/di-runtime.md`
- `docs/architecture-migration/maps/state-ownership.md`
- `docs/architecture-migration/maps/reactive.md`
- `docs/architecture-migration/maps/persistence.md`
- `docs/architecture-migration/maps/user-flow.md`
- `docs/architecture-migration/maps/state-inventory.md`
- `docs/architecture-migration/maps/target-invariants.md`
- `docs/architecture-migration/maps/characterization-tests.md`
- `docs/architecture-migration/maps/persistence-compatibility.md`
- `docs/architecture-migration/architecture-widget.html`
- `docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/generation-hash-receipt.md`

## Do-not-change list
- `docs/architecture-migration/maps/architecture-model.baseline.json`
- `docs/architecture-migration/TASK_CONTEXT.md`
- `docs/architecture-migration/plans/phase-7-project-restore-coordinator-relaunch.md`
- all historical evidence snapshots for Phase 0.5 through Phase 6
- production code
- tests

## Execution steps
1. Update `docs/architecture-migration/AGENTS.md` so Phase 7 wording matches the accepted dossier state and still preserves the explicit separation between plan approval and execution authorization.
2. Refresh `docs/architecture-migration/maps/architecture-model.json` to reflect current Phase 7 provenance, status, and evidence references.
3. Patch only the Phase 7-related current overlay maps named in the allow-list so their claims align with the accepted dossier refresh.
4. Regenerate `docs/architecture-migration/architecture-widget.html` from the refreshed model and overlays.
5. Write `docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/generation-hash-receipt.md` with the deterministic generation inputs, output hash, and command/script identity.
6. Verify the final diff stays within the allow-list and that the generated widget is reproducible from the recorded inputs.

## Required evidence and validation
- confirm the change set is limited to the allow-list above;
- confirm the widget regenerates deterministically from the updated docs model;
- confirm the receipt records source inputs, generation command or script, and final hash;
- confirm no production code, tests, baseline history, or frozen plan artifacts changed;
- confirm the plan stops at owner plan approval and does not imply ` /architecture-start `.

## Stop rule
Stop after owner plan approval. Do not execute the write-set in this session.
