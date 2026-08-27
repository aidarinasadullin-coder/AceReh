# phase-6-project-snapshot-save-boundary - Work Plan

## TL;DR (For humans)

Создать одну immutable save boundary: `ProjectSession -> ProjectSnapshot -> ProjectPersistenceMapper -> ProjectData -> IProjectFileService -> .smc`. Текущая wire schema (`Version`, DTO names/fields, enum/string representation и serializer behavior) не меняется. `ProjectLoadOrchestrator`, restore ordering, calculations, exports и Markdown остаются вне scope. Реализация выполняется одной sequential lane после characterization baseline; параллельны только независимые read-only/tests/fixture/QA работы. План требует dual high-accuracy review и отдельного owner result acceptance.

## Scope

### In scope

- immutable `ProjectSnapshot` с `ProjectNumber`, `ProjectObject`, `IsOperatingMode`, четырьмя canonical state snapshots, custom materials и custom templates;
- snapshot assembler/factory, читающий canonical state из `ProjectSession` и явно разрешённые material/template persistence sources;
- pure `ProjectPersistenceMapper` (`ProjectSnapshot -> ProjectData`), включая перенос inline Climate mapping;
- отдельный application/persistence save service, если это необходимо для удаления canonical save responsibility из `ResultsViewModel`;
- минимальная DI registration;
- сохранение `ProjectFileService` как serializer/file-I/O boundary;
- characterization, mapper, round-trip, fixture, negative и architecture guard tests;
- six architecture maps, shared model, widget payload, evidence и append-only migration context через существующие gates;
- current save failure, successful save/clean transition, backup/temp semantics и legacy read compatibility.

### Explicitly out of scope

- полный `ProjectData -> ProjectSession` restore coordinator;
- изменение `ProjectLoadOrchestrator`, restore order, rollback или transactional restore;
- новая legacy compatibility policy;
- любые `.smc` schema/version/DTO/serializer changes;
- removal of unrelated legacy owners or full Results projection migration;
- `CalculationContext` redesign, formula/invalidation/dirty multiplicity changes;
- PDF, Excel, Preview, Print behavior changes;
- Markdown generation removal. Existing Markdown buttons, their UI and AutomationId remain. A future owner-approved phase may remove `.md` generation/write code, builders/renderers/services, DI registrations and tests while retaining buttons; button no-op behavior is a separate user-visible owner decision.
- button removal/rename, broad refactor, widget historical cleanup, STATE/workflow gate edits, git operations, or unrelated dirty-path edits.

## Verification strategy

Tests-after is permitted only after RED characterization is captured; every implementation task must add or update agent-executable tests in the same task. All happy and failure paths require exact commands and evidence files. No success claim may rely on grep or a subagent summary alone.

Mandatory gates: live state/plan validation; targeted characterization; snapshot/mapper unit tests; persistence fixtures; negative guards; affected integration tests; Debug build; Release build; full Release test gate; save/reload user-flow QA; six-view model/validator verification; deterministic widget generation/check; three-domain final review; one consolidated receipt.

## Execution strategy

Production files are changed sequentially in this order: baseline and allow-list, tests/fixtures, contracts, snapshot assembly/mapper, application save service and Results adapter, DI, architecture dossier/evidence, final gates. No second central slice starts before the previous one passes. Independent read-only inspections and isolated fixture/test preparation may run in parallel only when they do not touch `ProjectSession`, DI, `ResultsViewModel`, `ProjectLoadOrchestrator` or the save boundary.

## Workflow materialization and authorization gate

This `.omo` file is a planning artifact, not the active migration plan. Before
Task 1 can be released, the normal workflow must, under separate owner gates,
materialize the exact canonical path
`docs/architecture-migration/plans/phase-6-project-snapshot-save-boundary.md`,
make it byte-identical with this mirror, compute its SHA-256, validate it with
`node docs/architecture-migration/workflow/validate-state.mjs validate --check-plan`,
and record the new phase/plan identity in `STATE.json` through the штатный
workflow command. No worker may edit `STATE.json` manually. Plan approval,
execution authorization and result acceptance remain separate; this artifact
does not grant any of them. Until that gate completes, the only permitted work
is read-only inspection and plan review.

## Decision ledger

| Decision | Binding rule |
|---|---|
| Snapshot type | Separate immutable `ProjectSnapshot`; no second writable state store. |
| Lifecycle metadata | Include only `ProjectNumber`, `ProjectObject`, `IsOperatingMode`; exclude path, dirty, load guard, restore lease, WPF/transient UI. |
| Module source | `ProjectSession.ClimateState.Snapshot`, `ConstructionState.Snapshot`, `ThermalState.Snapshot`, `HydraulicsState.Snapshot`. |
| Custom data | Include custom materials and custom templates from explicitly approved persistence inputs, not module canonical state. |
| DTO mapping | Pure deterministic `ProjectSnapshot -> ProjectData`; preserve existing DTO shape and `Version = "1.1"`. |
| File service | Remains serializer/I/O only; no ViewModel dependency and no state construction. |
| Restore | Not migrated. Existing restore behavior is a regression constraint only. |
| Dirty | Save success invokes existing clean transition exactly once as characterized; save failure does not clean. |
| Markdown | Separate future change; buttons remain. |

## Exact contracts

### `ProjectSnapshot`

Add it under the existing project/persistence model namespace selected by live conventions. It must expose get-only properties for lifecycle metadata, `ClimateStateSnapshot`, `ConstructionStateSnapshot`, `ThermalStateSnapshot`, `HydraulicsStateSnapshot`, immutable custom-material records and immutable custom-template records. Constructor/factory must defensively copy collections and reject null required values. It must not expose setters, mutation methods, runtime lifecycle values or ViewModels.

`CreatedDate` and `ModifiedDate` are explicit save-operation inputs, not hidden runtime state. Preserve the current contract: carry the existing `ProjectData.CreatedDate` when available, assign `ModifiedDate = DateTime.Now` for each save attempt, and assign `CreatedDate = DateTime.Now` only when the prior value is `DateTime.MinValue`. The assembler/service may carry these values as immutable snapshot metadata only when they are passed explicitly; it must not derive them from `CurrentFilePath`, `IsDirty` or UI state. Tests must prove first-save and subsequent-save semantics.

### Snapshot assembler

Expose an interface such as `IProjectSnapshotFactory`/`IProjectSnapshotAssembler` only after live naming inspection confirms the repository convention. It receives `IProjectSession` plus interfaces for explicitly allowed material/template persistence sources. It reads each canonical snapshot once per assembly, captures all save inputs into one immutable object, and never reads module ViewModels for module state. Template access may use a narrowly scoped repository/application input; it must not make `ProjectSnapshot` depend on WPF types.

### Persistence mapper

Expose a pure mapper/service with no `ResultsViewModel`, `ProjectLoadOrchestrator`, `ProjectFileService` or WPF dependency. It maps every existing `ProjectData` field, including dates supplied by the existing save orchestration, custom materials/templates, Climate, Construction, Thermal and Hydraulics. It reuses existing pure module mappers and preserves null/list/order/enum semantics. `ProjectData` remains a transport DTO, never a canonical owner.

### Save service and error semantics

If required by live dependency analysis, add an application service that accepts a file path and assembled snapshot, maps it, delegates exactly once to `IProjectFileService.SaveProjectResultAsync`, and returns the existing operation result/boolean contract expected by UI. It must preserve extension normalization, temp/backup/move/cleanup, error display responsibility, cancellation behavior and exception-to-failure conversion. `ProjectFileService` must not build snapshots or know ViewModels.

### Dirty semantics

Keep `ProjectSession`/`IProjectStateService` as lifecycle owner. On successful file persistence, retain the current clean transition and file-path update behavior. On failed result or exception, preserve dirty state and current user-visible error behavior. No new dirty event, recalculation, invalidation or restore event may be introduced.

## Exact write-set

The executor must first confirm each path exists or record the intended new path. Expected paths are:

- `src/Models/Project/ProjectSnapshot.cs` or live-equivalent snapshot model path;
- `src/Services/Project/IProjectSnapshotFactory.cs` and implementation, or live-equivalent;
- `src/Services/Project/ProjectPersistenceMapper.cs` or live-equivalent;
- `src/Services/Project/ClimatePersistenceMapper.cs` only if needed to remove inline mapping;
- `src/Services/Project/IProjectSaveService.cs` and implementation only if needed;
- `src/ViewModels/Results/ResultsViewModel.cs` minimal adapter/orchestration change;
- `src/Configuration/ServiceCollectionExtensions.cs` minimal registration;
- existing module mapper files only for pure extraction/contract reuse, never restore redesign;
- targeted existing/new test files under `tests/SnowMeltingCalculator.Tests/`;
- persistence fixtures/evidence under `docs/architecture-migration/evidence/phase-6-project-snapshot-save-boundary/`;
- affected `docs/architecture-migration/maps/{compile-time,di-runtime,state-ownership,reactive,persistence,user-flow}.md`, `docs/architecture-migration/maps/architecture-model.json`, `docs/architecture-migration/maps/architecture-model.schema.json`, `docs/architecture-migration/maps/architecture-model.widget.schema.json`, `docs/architecture-migration/widget/{model-contract.mjs,generate-widget.mjs,verify-widget.mjs,architecture-widget.mjs}` and phase-6 widget evidence;
- append-only permitted section in `docs/architecture-migration/TASK_CONTEXT.md` only after all gates; never `STATE.json`.

No other file may be changed without a factual blocker receipt and owner decision.

## Todos

- [ ] 1. Capture live baseline and protected dirty-path boundary

  **References:** `docs/architecture-migration/STATE.json`; root and migration `AGENTS.md`; `src/ViewModels/Results/ResultsViewModel.cs`; `src/Services/Project/ProjectFileService.cs`; `docs/architecture-migration/maps/persistence.md`; current git status.

  **Purpose:** establish exact current save behavior, test counts, hashes, warnings, fixture inventory and protected paths before edits.

  **Prerequisite:** none; `STATE.json` must still be phase-5.1 completed/stop=true.

  **Allowed changes:** only new phase-6 baseline receipt under `docs/architecture-migration/evidence/phase-6-project-snapshot-save-boundary/` and `.omo` execution evidence; no production/test changes.

  **Forbidden:** modifying `STATE.json`, workflow stage/pendingGates/stop, protected dirty files, or historical claims.

  **Expected tests/QA:** run targeted current save/round-trip tests and Debug build; record exact pass/fail/skips, dirty status and SHA values.

  **Acceptance:** baseline reproduces current save path and semantics; all protected paths are allow-listed; any baseline failure blocks Task 2.

  **Evidence:** `baseline.md`, command logs, status binary, hashes.

  **Rollback boundary:** delete only newly created phase-6 baseline artifacts if the task is abandoned; restore no existing file.

- [ ] 2. Add characterization tests for current save and user-visible semantics

  **References:** `tests/SnowMeltingCalculator.Tests/Project/ProjectRoundTripTests.cs`; `tests/SnowMeltingCalculator.Tests/ViewModels/ResultsViewModelOpenProjectTests.cs`; `ProjectFileServiceResultTests.cs`, `ProjectFileServiceAtomicityTests.cs`, `ProjectFileServiceMutationTests.cs`; `ResultsViewModel.cs` save/load/export methods.

  **Purpose:** lock new/populated project, all four modules, custom materials/templates, saved thermal results, two-collector summaries, second load, save failure and dirty transition before boundary extraction.

  **Prerequisite:** Task 1 green baseline.

  **Allowed changes:** tests and isolated fixtures only; test seams may use existing interfaces.

  **Forbidden:** changing production behavior to make tests pass; altering restore ordering, formulas, export or Markdown behavior.

  **Expected tests/QA:** Debug and Release targeted filters; include happy save/reload and injected file failure with dirty-state assertions.

  **Acceptance:** tests assert values plus event/recalculation/dirty multiplicity where characterized; all baseline rows are green or a factual blocker is recorded.

  **Evidence:** `task-2-characterization.md`, TRX/logs, fixture manifest.

  **Rollback boundary:** revert only Task-2 test/fixture additions; retain baseline receipt.

- [ ] 3. Define immutable snapshot and ownership guard contracts

  **References:** `src/Services/Project/IProjectSession.cs`; four `IProjectSession*State` contracts and snapshot types; `ProjectSession.cs`; `ProjectData.cs`; owner decisions above; state-ownership map.

  **Purpose:** make the aggregate-to-persistence boundary explicit without introducing a writable duplicate.

  **Prerequisite:** Tasks 1-2.

  **Allowed changes:** snapshot contracts, focused unit/guard tests, compile-time dependency assertions.

  **Forbidden:** restore changes, ProjectData setters/ownership migration, runtime-only metadata, ViewModel references, mutable collection leakage.

  **Expected tests/QA:** run `dotnet test --configuration Debug --filter "FullyQualifiedName~ProjectSnapshot"`; assert constructor null rejection, defensive-copy immutability, date first-save/subsequent-save behavior, canonical reads from `ProjectSession`, and absence of a second writable snapshot owner. Expected result: focused tests pass and deliberately invalid ViewModel dependency fixtures exit nonzero.

  **Acceptance:** snapshot contains all and only approved save inputs; no setters or mutable aliases; ProjectData remains DTO; tests fail if a ViewModel is used as module source.

  **Evidence:** `task-3-snapshot-contract.md`, guard output and dependency report.

  **Rollback boundary:** remove only new contract/test files; do not alter existing state slices.

- [ ] 4. Implement snapshot assembly and pure ProjectData mapping

  **References:** `ConstructionPersistenceMapper.cs`; `ThermalPersistenceMapper.cs`; `HydraulicsPersistenceMapper.cs`; inline climate mapping in `ResultsViewModel.SaveCurrentProject`; `MaterialSnapshot.cs`; `ConstructionTemplate.cs`; `ProjectData.cs`.

  **Purpose:** assemble one immutable snapshot and map it deterministically to the unchanged DTO graph.

  **Prerequisite:** Task 3.

  **Allowed changes:** new assembler/mapper, minimal climate extraction, mapper unit tests and fixture assertions.

  **Forbidden:** schema/version/field changes, restore mapper changes, formulas, material/template behavior changes, ViewModel reads for module slices.

  **Expected tests/QA:** run `dotnet test --configuration Debug --filter "FullyQualifiedName~ProjectSnapshot|FullyQualifiedName~ProjectPersistenceMapper"`; serialize mapped `ProjectData` with live `ProjectFileService` options and compare property names/enum strings against baseline fixtures; assert all module/lifecycle/custom-data/results/date cases and null/invalid input failures. Expected result: no added, removed or renamed wire fields.

  **Acceptance:** same canonical input produces equal DTO graph; serialized JSON preserves field names and values; mapper has no UI/load/file dependency; all mapper tests pass.

  **Evidence:** `task-4-snapshot-mapper.md`, serialized golden/fixture comparison and test logs.

  **Rollback boundary:** revert only new mapper/assembler and tests; preserve characterization baseline.

- [ ] 5. Move save orchestration to the application boundary and retain Results adapter behavior

  **References:** `ResultsViewModel.cs:SaveProject,SaveProjectAs,SaveToFile,SaveCurrentProject`; `IProjectFileService`; `ProjectFileService`; `IProjectStateService`; `ServiceCollectionExtensions.AddResultsModule`.

  **Purpose:** remove direct canonical module-state construction from Results while preserving UI commands, path handling, status/error messages and dirty transition.

  **Prerequisite:** Task 4.

  **Allowed changes:** minimal save service interface/implementation, Results constructor/fields/method calls, DI registration, focused tests.

  **Forbidden:** full Results ViewModel dependency cleanup, export/PDF/Excel/Preview/Print redesign, Markdown removal, load/restore edits, ProjectFileService conceptual changes.

  **Expected tests/QA:** run `dotnet test --configuration Debug --filter "FullyQualifiedName~ResultsViewModelOpenProjectTests|FullyQualifiedName~ProjectFileService"`; assert no Results module-state reads or ProjectFileService ViewModel references; inject success/failure and assert exactly one save call, one clean transition only on success, existing error/status, extension and cancellation semantics. Expected result: valid suite passes and invalid dependency fixture exits nonzero.

  **Acceptance:** `ResultsViewModel` does not read module ViewModels for Climate/Construction/Thermal/Hydraulics save data; no service depends on concrete ViewModels; ProjectFileService receives only ProjectData; all existing save commands remain available.

  **Evidence:** `task-5-save-boundary.md`, dependency/guard output, targeted TRX.

  **Rollback boundary:** revert only save-boundary service/Results/DI changes from this task; do not touch load path.

- [ ] 6. Run persistence fixtures and compatibility/negative guard suite

  **References:** `tests/SnowMeltingCalculator.Tests/Project/ProjectRoundTripTests.cs`; all tracked `.smc` fixtures; `maps/persistence-compatibility.md`; `ProjectFileService*Tests`; architecture guard tests.

  **Purpose:** prove wire compatibility and ownership constraints against the live fixture corpus.

  **Prerequisite:** Task 5.

  **Allowed changes:** phase-6 fixture copies/manifests, tests and evidence only.

  **Forbidden:** serializer compatibility shims, fixture rewriting to hide failures, legacy policy changes, restore modifications.

  **Expected tests/QA:** run `dotnet test --configuration Release --filter "FullyQualifiedName~ProjectRoundTripTests|FullyQualifiedName~ResultsViewModelOpenProjectTests|FullyQualifiedName~ProjectFileService"`; enumerate fixtures with `git ls-files '*.smc'`, hash each, round-trip through Result API, compare required DTO paths, and execute negative guards for VM source, ProjectData ownership, ProjectFileService VM dependency and duplicate snapshot store. Expected result: every fixture is accounted for and invalid fixtures are rejected.

  **Acceptance:** every live fixture either round-trips with unchanged schema/semantics or produces an explicit blocker; no accepted skip is silently reclassified.

  **Evidence:** `task-6-persistence-fixtures-and-guards.md`, fixture hashes, TRX and negative-probe logs.

  **Rollback boundary:** remove only phase-6 fixture/evidence additions; never modify legacy fixtures.

- [ ] 7. Refresh six architecture views, shared model and widget evidence

  **References:** `docs/architecture-migration/maps/{compile-time,di-runtime,state-ownership,reactive,persistence,user-flow}.md`; `architecture-model.json`; schema/contract; `widget/model-contract.mjs`; `widget/generate-widget.mjs`; `widget/verify-widget.mjs`; Phase 5 evidence patterns.

  **Purpose:** represent the new boundary and only its proven edges in all six views.

  **Prerequisite:** Tasks 3-6 green.

  **Allowed changes:** affected map rows, model nodes/edges/invariants, phase-6 evidence, deterministic generated widget artifacts.

  **Forbidden:** historical widget cleanup, invented Target status, unrelated invariant corrections, changes to schema contract beyond required proven IDs.

  **Expected tests/QA:** run `node docs/architecture-migration/widget/verify-widget.mjs --suite model-v2`, `node docs/architecture-migration/widget/verify-widget.mjs --suite runtime-v2`, and `node docs/architecture-migration/widget/generate-widget.mjs --check`; generate twice from `docs/architecture-migration/maps/architecture-model.json`, hash `docs/architecture-migration/architecture-widget.html`, and run invalid-ID/missing-edge fixtures expecting nonzero exits. Expected result: valid suites/check pass, hashes match, six views exist, invalid fixtures reject.

  **Acceptance:** maps show `ProjectSession -> ProjectSnapshot -> mapper -> ProjectData -> file service`; lifecycle runtime-only values are excluded; no restore migration or Markdown removal appears as completed.

  **Evidence:** `task-7-architecture-dossier-refresh.md`, model/runtime JSON, widget hash and validator logs.

  **Rollback boundary:** revert only phase-6 map/model/widget/evidence paths; preserve production/tests.

- [ ] 8. Update append-only migration context and assemble phase receipt

  **References:** `TASK_CONTEXT.md`; `STATE.json` (read-only); all phase-6 evidence; plan identity.

  **Purpose:** record factual Phase 6 result, partial/deferred IDs and provenance without changing active workflow authority.

  **Prerequisite:** Tasks 1-7 and final verification wave prerequisites.

  **Allowed changes:** append-only context entry and phase evidence receipt; `.omo` receipt artifacts.

  **Forbidden:** STATE/workflow mutation, claiming owner acceptance, claiming Phase 7+ IDs complete, overwriting history.

  **Expected tests/QA:** before canonical import, run a read-only path/hash check proving `.omo` is not falsely active; after the owner-authorized workflow import, run `node docs/architecture-migration/workflow/validate-state.mjs validate --check-plan`; verify receipt references existing artifacts/current hashes. Expected result: no manual STATE edit and missing import remains a blocked gate.

  **Acceptance:** receipt separates closed, partial and deferred identifiers; includes Markdown decision, wire compatibility, dirty semantics, residual risks and exact write-set.

  **Evidence:** `phase-6-consolidated-receipt.md`.

  **Rollback boundary:** remove only newly appended phase-6 context/evidence line if rejected; do not rewrite prior history.

## Identifier disposition

### Phase 6 closure claims, only if evidence passes

- `INV-001` partial: save boundary reads canonical session snapshots, but aggregate-wide ownership cleanup remains later.
- `INV-006`, `INV-007`, `INV-009`, `INV-012`: only the save-boundary portions proven by guards.
- `INV-014` partial: sequential persistence boundary only; not restore.
- lifecycle `ST-001..ST-005`: persisted metadata inclusion/exclusion and clean/path semantics only; runtime guard/dirty ownership remains existing behavior.
- `ST-006..ST-019`: save projections for four slices only.
- `CF-013`, `CF-014`: save and save-failure flows; `CF-015` only if snapshot-derived export input is demonstrably touched.
- `CF-020`, `CF-021`: failure preserves dirty; successful save transitions clean.
- `PN-*`, `PP-*`, `SMC-*`: only save-side nodes/edges and fixture rows.
- `EV-P2`, `EV-P3`, `EV-P4`, `EV-P5`: reused or superseded with phase-6 evidence, never rewritten.

### Partial/deferred; do not mark complete

Full restore coordinator, transactional restore, restore order, Results derived projection, calculation redesign, broad legacy-owner removal, export behavior changes, Markdown removal, and any IDs whose definition includes these remain Phase 7+.

## Final verification wave

- [ ] F1. Conformance, scope, provenance and state/plan identity audit

  **Expected tests/QA:** run `git status --porcelain=v1`, compare protected baseline-relative paths, hash `STATE.json` before/after, run state validation only after authorized canonical import, and run a script rejecting DTO/version/serializer changes and forbidden restore/Markdown/export paths. Expected result: allow-list only, unchanged STATE, valid identity, no scope drift. Evidence: `final/f1-conformance.md`.

- [ ] F2. Architecture and code-quality review

  **Expected tests/QA:** run `dotnet build src\SnowMeltingCalculator.csproj -c Debug --nologo` and all guard filters from Tasks 3/5/6; inspect compiler/dependency output for forbidden ViewModel references. Expected result: zero new warnings/errors, guards pass, no second writable owner. Evidence: `final/f2-architecture.md`.

- [ ] F3. Executable QA and user-risk review

  **Expected tests/QA:** run Debug/Release builds, targeted filters and `dotnet test --configuration Release --no-build`; execute new/populated/failure/save-reload/second-load flows recording status, file, `.bak`/`.tmp`, dirty, saved results and two-collector summaries; smoke-check PDF/Excel/Preview/Print and Markdown buttons remain present. Expected result: all commands pass with no new warnings/errors and user-flow assertions pass. Evidence: `final/f3-executable-qa.md`.

- [ ] F4. Six-view/widget deterministic verification and consolidated receipt

  **Expected tests/QA:** run both widget validator suites, negative fixtures, two deterministic generations and `generate-widget.mjs --check`; parse payload for exactly six views and all Phase-6 evidence links. Produce consolidated receipt only after F1-F3 approve with plan SHA/write-set/risks. Expected result: deterministic hash and complete evidence linkage. Evidence: `final/f4-consolidated-receipt.md`.

## Stop rules

Immediately stop edits, preserve factual blocker receipt, and do not continue if: characterization baseline differs; snapshot cannot be assembled from canonical state; a second writable owner appears; `.smc` wire contract must change; restore behavior must change; custom materials/templates cannot be preserved; event/dirty/recalculation multiplicity changes; Results remains canonical writer; scope drifts into Markdown/restore/CalculationContext/exports; any build/test/guard gate fails; STATE/plan/receipt/SHA is stale or contradictory; protected dirty-path mismatch occurs; or the plan is proven infeasible. Do not weaken criteria, add fallback shims, or proceed to the next task.

## Verification command catalog

```powershell
node docs/architecture-migration/workflow/validate-state.mjs validate --check-plan
dotnet build src\SnowMeltingCalculator.csproj -c Debug --nologo
dotnet build src\SnowMeltingCalculator.csproj -c Release --nologo
dotnet test --configuration Debug --no-build
dotnet test --configuration Release --no-build
dotnet test --configuration Release --filter "FullyQualifiedName~ProjectRoundTripTests"
dotnet test --configuration Release --filter "FullyQualifiedName~ResultsViewModelOpenProjectTests"
dotnet test --configuration Release --filter "FullyQualifiedName~ProjectFileService"
node docs/architecture-migration/widget/verify-widget.mjs --suite model-v2
node docs/architecture-migration/widget/verify-widget.mjs --suite runtime-v2
node docs/architecture-migration/widget/generate-widget.mjs --check
```

The executor must replace filters with the exact live test names discovered in Task 1 and record every exit code, total, executed, passed, failed, skipped and NotExecuted identity.

## Rollback boundary

Rollback is task-local and baseline-relative. Remove/revert only Phase 6 allow-listed production/test/map/evidence changes, never reset/clean/revert the repository globally, never overwrite protected dirty paths, never alter `STATE.json`, and never modify `.smc` fixtures to conceal a regression. If a central task fails, stop at that boundary and retain RED evidence.

## Success criteria

- One immutable `ProjectSnapshot` is the only save snapshot boundary and has no runtime-only or UI state.
- Save data for all four modules is read from canonical `ProjectSession` snapshots.
- Materials/templates are preserved from approved persistence sources.
- `ResultsViewModel` no longer constructs canonical module state for save.
- `ProjectFileService` remains ViewModel-free serializer/I/O boundary.
- `ProjectData` remains DTO-only.
- Existing `.smc` wire compatibility, save failure semantics, clean transition, and user-visible save/reload behavior are preserved.
- Targeted/affected/full Release tests, Debug/Release builds, guards, fixtures, user-flow QA and widget gates pass with no new warnings/errors.
- Six views, model, widget and evidence are current and deterministic.
- Final receipt has three approved domains plus widget verification and owner result acceptance remains a separate gate.

## Commit strategy

The worker must not commit unless separately authorized by the owner/workflow. If commits are authorized later, use one sequential commit per completed task or an equivalent atomic vertical slice, with tests and evidence in the same commit; never stage unrelated dirty paths.
