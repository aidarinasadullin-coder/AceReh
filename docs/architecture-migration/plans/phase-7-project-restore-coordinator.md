# phase-7-project-restore-coordinator - Work Plan

## TL;DR (For humans)

Ввести единую ViewModel-free restore boundary для текущего `.smc`: валидировать persisted inputs, подготовить все четыре canonical `ProjectSession` slices, выполнить ровно один application-level расчёт, опубликовать derived state в `ProjectSession`, построить актуальный report snapshot из `ProjectSession` и обновить UI projections. Открытие проекта не импортирует и не изменяет глобальные materials/templates catalogs. Wire schema `ProjectData` и `Version = "1.1"` сохраняются; старые `.smc` вне обязательств DEC-002. План рассчитан на последовательную реализацию с characterization-first тестами и финальной F1-F4 проверкой.

## Scope

### Prerequisites

- Phase 6 result acceptance is a prerequisite. Verify `TASK_CONTEXT.md:2893-2920`
  and `docs/architecture-migration/evidence/phase-6-project-snapshot-save-boundary/owner-result-acceptance.md` before Todo 1; the stale `AGENTS.md:16-19` note is superseded by the later authoritative decision-log entry in this worktree.
- The existing restore lease implementation must pass a nested-scope regression
  test before Todo 4. If the test fails, make the minimal lease correction and
  update the evidence; do not proceed with the coordinator on an unresolved
  guard defect.

### In scope

- validate-first restore coordinator с одним ordered commit согласно DEC-003=C;
- восстановление только persisted inputs из `ProjectData` в четыре canonical `ProjectSession` slices;
- exactly-once recalculation после восстановления inputs и публикация calculated state в `ProjectSession`;
- сохранение `CalculationContext` как compatibility/read projection и существующих Thermal/Hydraulics writers согласно DEC-001;
- удаление project-open mutation/import для global materials/templates catalogs;
- ViewModel-free application boundary; ViewModels остаются UI adapters/projections;
- report snapshot/data builder, читающий актуальное состояние `ProjectSession`, включая inputs, derived values и formulas;
- guard, path, dirty и user-visible error semantics;
- characterization, unit, integration, negative dependency and report-source tests;
- affected six architecture maps, shared model/widget payload, evidence receipts and append-only migration context.

### Explicitly out of scope / Must NOT have

- восстановление calculated values из `ProjectData` как canonical current state;
- второй расчёт при открытии проекта или повторное вычисление в report export;
- импорт, обновление, overwrite или rollback global materials/templates catalogs при открытии `.smc`;
- использование templates как restore data; templates остаются input-time UI tools;
- concrete ViewModel dependencies в restore/calculation coordinator;
- изменение formulas, calculation algorithms, wire DTO names/fields, serializer format или `Version`;
- legacy compatibility branches for old `.smc`;
- full Results derived-projection migration, legacy owner removal, DI cleanup beyond required registrations;
- PB-002 root-cause fix, Markdown removal, PDF/Excel/Preview/Print redesign;
- manual edits to `STATE.json`, unrelated dirty paths, git operations or broad refactoring.

## Verification strategy

Tests-after is allowed only after a RED characterization baseline. Every implementation todo includes tests and agent-executable happy/failure QA in the same task. Use exact `dotnet test` filters, Debug/Release builds, architecture guard scripts and recorded evidence files. No completion claim may rely on grep or a subagent summary alone. Manual WPF QA is required for the affected open-project/report flow; the external legacy fixture is not required under DEC-002 and must be recorded as residual risk.

Mandatory gates: baseline, current-format round-trip, invalid-input and commit-failure tests, exactly-once calculation assertions, catalog non-mutation assertions, report-source assertions, Debug build, Release build, full Release test gate, six-map/model/widget verification, and F1-F4 final verification.

## Execution strategy

Production changes run in one sequential vertical-slice lane: baseline and allow-list, characterization tests, restore contracts/candidates, ordered commit and calculation, Results/UI adapter, report snapshot migration, DI, architecture artifacts, then final gates. Independent read-only inspection and isolated fixture preparation may run in parallel only when they do not modify the central restore/report surfaces. The worker must stop on a failed prerequisite, scope drift, missing owner decision or protected dirty-path conflict.

## Todos

- [ ] 1. Capture restore/report baseline and protected dirty-path boundary

  **References:** `docs/architecture-migration/AGENTS.md`; `docs/architecture-migration/TASK_CONTEXT.md`; `src/Services/Project/ProjectLoadOrchestrator.cs`; `src/ViewModels/Results/ResultsViewModel.cs`; `src/Services/Project/ProjectSession.cs`; `src/Services/Reports/Calculation/CalculationReportExportService.cs`; current git status; existing project/load/report tests.

  **Purpose:** record the actual current load ordering, ViewModel dependencies, calculation invocations, catalog mutations, report source, dirty/path/guard behavior, test inventory and protected unrelated changes before production edits.

  **Acceptance:** baseline receipt identifies every current restore mutation and report input; records `IProjectSession.BeginProjectRestore()` nested-scope behavior and the current `ProjectSession.ProjectRestoreLease` implementation; current-format fixture and failure behavior are reproducible; dirty paths are allow-listed; any baseline failure is recorded and blocks Todo 2.

  **QA:** happy: run targeted existing load/round-trip/report tests and Debug build, record passing commands and artifacts in `docs/architecture-migration/evidence/phase-7-project-restore-coordinator/baseline.md`. Failure: inject/observe invalid project or file failure and record existing user-visible error, guard and dirty state in the same receipt.

  **Commit:** baseline/evidence-only change; no product or test behavior changes.

- [ ] 2. Add characterization tests for current load, calculation, catalog and report behavior

  **References:** `tests/SnowMeltingCalculator.Tests/Project/ProjectRoundTripTests.cs`; `tests/SnowMeltingCalculator.Tests/ViewModels/ResultsViewModelOpenProjectTests.cs`; `src/Services/Project/ProjectLoadOrchestrator.cs`; `src/ViewModels/Results/ResultsViewModel.cs`; `src/Repositories/Construction/MaterialRepository.cs`; `src/Repositories/Construction/ConstructionTemplateRepository.cs`; calculation/report tests and fixtures.

  **Purpose:** lock current behavior and expose the intended regression contract before replacing the ViewModel-driven path.

  **Acceptance:** tests cover all four slices, current `.smc` inputs, one load, repeated load, invalid payload, calculation call count, guard nesting/finalization, dirty/path behavior, report output fields/formulas, and catalog state before/after load. The guard test explicitly opens outer and inner `IProjectSession.BeginProjectRestore()` scopes, disposes both exactly once, and asserts `IsLoadProjectInProgress == false`; tests are RED for the new canonical-source assertions or explicitly document an existing gap.

  **QA:** happy: run focused Debug and Release filters and capture TRX/logs under `docs/architecture-migration/evidence/phase-7-project-restore-coordinator/task-2-characterization/`. Failure: use malformed or incomplete current-format data and a throwing calculation/catalog seam; assert no silent success, no mixed canonical state and preserved error semantics.

  **Commit:** characterization tests and isolated fixtures only.

- [ ] 3. Define validated persisted-input candidates and atomic ProjectSession restore contract

  **References:** `src/Models/Project/ProjectData.cs`; `src/Services/Project/ProjectPersistenceMapper.cs`; `src/Services/Project/ConstructionPersistenceMapper.cs`; `src/Services/Project/ThermalPersistenceMapper.cs`; `src/Services/Project/HydraulicsPersistenceMapper.cs`; `src/Services/Project/ProjectSession.cs`; `src/Services/Project/ProjectSessionConstructionState.cs`; four state interfaces/snapshots; `docs/architecture-migration/maps/architecture-model.json`; `docs/architecture-migration/maps/target-invariants.md`; `TASK_CONTEXT.md:2922-2939`.

  **Purpose:** create immutable validated candidates for persisted inputs and make the four-slice commit boundary explicit without restoring calculated state from the DTO.

  **Acceptance:** every current `ProjectData` input field has an explicit mapping or documented intentional omission; candidates are fully validated before canonical mutation; commit order is deterministic; the coordinator captures the pre-restore canonical snapshots, commits through the four existing slice mutation APIs in the documented order, and on any commit failure invokes the defined all-four-slice reset/rollback path before returning failure; failure leaves clean/default state with no mixed partial state; no catalog repository is called; calculated DTO fields are ignored as current state.

  **QA:** happy: unit-test complete valid candidate construction and equality against `ProjectSession` input snapshots. Failure: null, invalid enum, missing layer/material reference and commit exception tests fail before partial mutation or leave clean/default state; use a `ProjectSession` state snapshot before/after to prove rollback; dependency guard fails if a candidate/coordinator references a concrete ViewModel; a spy `IMaterialRepository`/template repository records zero mutating calls.

  **Commit:** restore candidate/contract code plus focused tests.

- [ ] 4. Implement the ViewModel-free restore coordinator and exactly-once calculation publication

  **References:** `src/Services/Project/ProjectLoadOrchestrator.cs`; `src/Services/Project/ProjectSession.cs`; `src/Services/Project/IProjectSession.cs:BeginProjectRestore`; `src/Services/Project/ThermalStateCoordinator.cs`; `src/Services/Project/HydraulicsStateCoordinator.cs`; `src/Core/CalculationContext.cs`; `src/Services/Project/ProjectSession*State.cs`; `src/ViewModels/Results/ResultsViewModel.cs:LoadProjectDataAsync`; existing calculation command/service contracts.

  **Purpose:** keep `ResultsViewModel.LoadProjectDataAsync` as the UI entrypoint and make `ProjectLoadOrchestrator` a thin application adapter/delegator (or the renamed coordinator itself), with no ViewModel-owned restore mutation: validate inputs, ordered commit to `ProjectSession`, execute exactly one calculation, publish current thermal/hydraulics derived state through existing coordinators, then expose completion to the UI adapter. `LoadProjectDataAsync` owns the outer lease/final UI projection and clean/path finalization; the coordinator owns module restore and calculation ordering.

  **Acceptance:** restore coordinator has no concrete ViewModel dependency; it uses the existing guard lease; all four slices are restored as one ordered operation; calculation runs once only after inputs commit; current derived values live in `ProjectSession`; successful load ends with correct path and clean state; failure invokes the same defined rollback/reset boundary for all four slices, ends with clean/default canonical state, releases the guard and preserves the user-visible failure.

  **QA:** happy: run a valid current-format round-trip with a counting calculation-service decorator/test double and assert `CallCount == 1`, all inputs/derived values, guard false, path set and expected dirty transition. Failure: make validation, commit or calculation throw; assert no mixed state, no second calculation, guard false in `finally`, catalogs unchanged and failure result/message. Capture under `docs/architecture-migration/evidence/phase-7-project-restore-coordinator/task-4-restore-coordinator/`.

  **Commit:** coordinator, minimal load adapter and tests as one sequential vertical slice.

- [ ] 5. Remove project-open catalog mutation and preserve catalog/template boundaries

  **References:** `src/Services/Project/ProjectLoadOrchestrator.cs`; `src/Repositories/Construction/MaterialRepository.cs`; `src/Repositories/Construction/ConstructionTemplateRepository.cs`; `data/materials_db.json`; application template persistence location; `ProjectSnapshotPersistenceInputs` and current template access seams; construction state/layer restore code.

  **Purpose:** ensure `.smc` open consumes expanded construction inputs and never treats global catalogs as project-owned restore targets.

  **Acceptance:** no restore path calls catalog add/update/delete/import; construction layers restore their persisted input values deterministically without silently changing them by name lookup; templates are not required for restore; no sync-over-async template call remains on the UI restore path; a spy repository observes zero mutating calls; SHA-256/byte snapshots of `data/materials_db.json` and the resolved templates persistence file are identical before and after project open.

  **QA:** happy: open a project whose layers reference existing and custom persisted material input and compare session inputs and repository spy counters plus before/after SHA-256 hashes. Failure: duplicate/missing catalog entries and unavailable template repository must not mutate catalogs or deadlock; restore either uses persisted layer inputs or returns a controlled failure with clean/default session. Evidence: `docs/architecture-migration/evidence/phase-7-project-restore-coordinator/task-5-catalog-boundary/`.

  **Commit:** catalog-boundary production changes and regression tests.

- [ ] 6. Migrate calculation report input to a current ProjectSession report snapshot

  **References:** `src/Services/Reports/Calculation/CalculationReportExportService.cs`; `src/Services/Reports/Calculation/ICalculationReportDataBuilder.cs`; `src/Services/Reports/Calculation/CalculationReportDataBuilder.cs`; `src/Services/Reports/Calculation/Builders/ClimateSectionBuilder.cs`; `src/Services/Reports/Calculation/Builders/ConstructionSectionBuilder.cs`; thermal/hydraulics report builders; `src/Models/Project/ProjectData.cs`; `src/Services/Project/ProjectSession.cs`; Results report commands.

  **Purpose:** make the report a projection of the current aggregate after calculation, not a projection of stale/saved calculated fields in `ProjectData`.

  **Acceptance:** an immutable report snapshot is assembled from `ProjectSession` after successful calculation and contains the current four slices, inputs, derived values and formula-relevant data; builders/exporter consume that snapshot or an equivalent session-derived contract; report export does not recalculate, mutate session or read saved calculated DTO values; existing Markdown file/error/cancellation behavior remains intact.

  **QA:** happy: inject a report builder spy and a `ProjectData` containing deliberately stale calculated fields, mutate/recalculate `ProjectSession`, then export; assert the report contains post-calculation session values/formulas, builder receives the session-derived snapshot, and calculation call count remains unchanged. Failure: null/invalid snapshot, missing derived state, cancellation and write failure return existing failure behavior without mutation. Run targeted report tests and capture under `docs/architecture-migration/evidence/phase-7-project-restore-coordinator/task-6-report-snapshot/`.

  **Commit:** report snapshot contract, adapters/builders, Results wiring and tests.

- [ ] 7. Wire the boundary, refresh UI projections, update architecture evidence and run release gates

  **References:** `src/Configuration/ServiceCollectionExtensions.cs`; `src/ViewModels/Results/ResultsViewModel.cs`; affected load/report command paths; `docs/architecture-migration/maps/{compile-time,di-runtime,state-ownership,reactive,persistence,user-flow}.md`; `docs/architecture-migration/maps/architecture-model.json`; widget schemas/generators/verifier; `docs/architecture-migration/TASK_CONTEXT.md` append-only section; all Phase 7 evidence paths.

  **Purpose:** complete DI and UI projection wiring, prove architecture invariants, refresh the six-map dossier/widget, and collect release evidence without broad unrelated cleanup.

  **Acceptance:** DI resolves the coordinator and report snapshot path; `ResultsViewModel.LoadProjectDataAsync` remains the UI entrypoint while `ProjectLoadOrchestrator` delegates without concrete ViewModel ownership; Results remains an adapter and does not own canonical restore/calculation/report data; UI projections refresh only after successful session commit/calculation; all scope-out guards pass; maps/model/widget describe the new edges and ownership; Debug, Release and full Release tests pass.

  **QA:** happy: run valid open-project then report export through the real WPF command path and capture manual evidence/screenshots or equivalent executable trace. Failure: invalid project, calculation failure, canceled export and catalog conflict each leave guard/session/catalog/error state as specified. Run exact build/test/widget commands and store logs under `docs/architecture-migration/evidence/phase-7-project-restore-coordinator/task-7-gates/`.

  **Commit:** DI/UI/evidence/map/widget updates only after Todos 1-6 pass; append migration context only through the documented gate, never edit `STATE.json` manually.

## Final verification wave

- [ ] F1. Conformance and provenance audit

  Compare the final diff to every in-scope and Must-NOT-Have rule, verify only allow-listed paths changed, confirm current-format wire schema is unchanged, and reconcile all evidence with the live repository and migration context. Store `f1-conformance.md`.

- [ ] F2. Architecture and code-quality review

  Verify `ProjectSession` remains the only canonical owner, restore/report services have no concrete ViewModel dependency, `CalculationContext` writer rules remain intact, exactly-once calculation is structurally enforced, and no catalog mutation path remains. Store `f2-architecture.md`.

- [ ] F3. Executable QA and user-risk verification

  Run full Release tests, Debug/Release builds, current-format open/save/reload, report export, invalid payload, calculation failure, cancellation, catalog non-mutation and guard/dirty/path scenarios through exact commands. Perform the affected WPF open/report manual flow and store `f3-executable-qa.md` with logs/screenshots.

- [ ] F4. Scope fidelity and residual-risk review

  Confirm no legacy compatibility promise, PB-002 fix, Markdown removal, broad Results projection migration or unrelated dirty-path change entered the write-set. Record skipped legacy fixture/manual limitations and all residual risks in `f4-scope.md`.

  The final wave must also produce one consolidated receipt, in addition to any
  per-domain notes. It must name the exact write-set, reused and rerun evidence,
  residual risks, and independent F1/F2/F3 domain verdicts; one reviewer may not
  substitute for the three required domains.

## Commit strategy

Use sequential commits aligned to Todos 1-7. Keep characterization tests separate from production changes where repository convention permits, but every production todo must include its tests. Do not squash or rewrite unrelated user changes. Before any commit, verify the dirty baseline-relative allow-list. No commit or staging is performed by the planner; execution belongs to the worker session.

## Success criteria

- Current `.smc` inputs reopen into all four canonical `ProjectSession` slices.
- Open performs exactly one calculation after input restore and stores current derived state in `ProjectSession`.
- No calculated DTO field becomes canonical current state.
- Report output reflects the current `ProjectSession` snapshot and does not recalculate.
- Opening a project does not mutate materials/templates catalogs.
- Invalid/failed restore leaves no mixed partial canonical state and always releases the restore guard.
- Existing path, dirty, cancellation and user-visible error contracts remain characterized and passing.
- Debug/Release/full Release gates, architecture maps/widget checks and F1-F4 all pass with evidence.
