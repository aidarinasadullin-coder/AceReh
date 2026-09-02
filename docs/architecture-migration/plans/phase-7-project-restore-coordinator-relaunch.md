# phase-7-project-restore-coordinator-relaunch - Work Plan

## TL;DR (For humans)

This shortened relaunch plan keeps Phase 7 focused on one thing: make project restore deterministic and safe, then prove that report/export and UI entrypoints consume the fresh session state instead of stale persisted DTO results. It preserves the approved architecture invariants from Phases 2-6, keeps `ProjectLoadOrchestrator` as the single restore boundary, and does not add a new recovery framework, archive-management subsystem, or legacy compatibility path. Every slice now carries an exact worker command and receipt path.

The work is split into 8 execution slices plus 4 final verification gates. The main risk is any remaining ambiguity about the exact current calculation/report source of truth; if the live code still cannot establish that cleanly, the worker must stop and surface that as an owner decision rather than inventing a new contract.

## Scope

### Authority and frozen-plan lifecycle

- Authoring candidate: `.omo/plans/phase-7-project-restore-coordinator-relaunch.md`.
- Active dossier authority: `docs/architecture-migration/AGENTS.md` and the latest `docs/architecture-migration/TASK_CONTEXT.md`.
- Planning approval authorizes plan writing only; execution still belongs to a separate worker session started by the user.
- Todo write-sets and commands below describe downstream worker execution, not this planning session.
- The plan stays on the approved Phase 2-6 foundation: no `.smc` format change, no `Version = "1.1"` change, no legacy compatibility expansion, no owner gate for private implementation choices, and no mutation of unrelated baseline artifacts.

### In scope

- Single restore boundary through `ProjectLoadOrchestrator` and the existing `ProjectSession` aggregate root.
- Validation before mutation for restore input.
- Deterministic canonical restore order for the four `ProjectSession` slices: Climate, Construction, Thermal, Hydraulics.
- Exactly-once application calculation after canonical restore, with published session-derived result state.
- Report/export consuming current session/calculation state, not stale saved DTO results.
- Project-open remaining read-only for global catalogs and templates.
- UI/DI adapters continuing to call the same restore boundary and refresh only after successful restore/calculation.
- Architecture evidence refresh only where needed to keep the dossier aligned with the live write-set.

### Must NOT have

- No new recovery-management framework, archive rotation subsystem, retry-policy framework, or recovery-metadata manifest hierarchy.
- No production-code implementation in this planning session.
- No test execution in this planning session.
- No `.smc` schema/persistence-format change, no legacy `.smc` compatibility expansion, and no relaxation of the approved Phase 2-6 semantics.
- No second restore service, no second calculation pass, no report recalculation path, no global catalog mutation on project open.
- No `TASK_CONTEXT.md` change unless a live blocker proves it is strictly required.

## Verification strategy

- Grounding is exploration-first: live code and tests define the current restore, calculation, report, and UI boundaries before any plan item is finalized.
- Each execution slice must have agent-executable happy/failure QA, with a concrete command or test filter and a receipt path; the downstream worker, not the planner, executes those commands.
- The main proof sequence is: restore contracts → restore characterization → calculation publication → report source-of-truth → UI/DI integration → architecture evidence → phase-wide gates.
- If the worker cannot prove the exact source of truth for a boundary from the live codebase, the correct stop condition is `OWNER_DECISION_REQUIRED`, not an invented fallback API.
- Before each focused test command, the worker must create the evidence directory and run `dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo`; only after a successful build may the listed `dotnet test ... --no-build` command run. Any focused test command that executes 0 tests is a failed verification even if the process exit code is 0.

## Execution strategy

One sequential lane. Read-only inspection and evidence gathering can be parallel where independent. The worker starts from the restore boundary, then moves outward only after the restore contract is stable. Any slice that would alter behavior outside the approved restore/report/UI boundary must stop for owner decision. Prometheus does not run product tests in this session; the worker creates the evidence directory, builds the test project, then runs the exact focused test command listed in each todo and rejects 0-test executions.

## Todos

- [ ] 1. `src/Services/Project/ProjectSession.cs` and `src/Services/Project/ProjectLoadOrchestrator.cs`: lock the canonical restore entrypoint and four-slice ownership - expect a single restore boundary with deterministic guard semantics

  **Goal:** Keep restore ownership centralized in `ProjectLoadOrchestrator` and `ProjectSession`, with the four canonical slices and restore guard behavior explicitly preserved.

  **Write-set / change class:** production/test. Expected future writes are limited to the existing restore boundary and the tests that characterize it. No new coordinator type, no recovery subsystem, no unrelated service split.

  **References:** `src/Services/Project/ProjectLoadOrchestrator.cs`; `src/Services/Project/ProjectSession.cs`; `src/Services/Project/IProjectSession.cs`; `docs/architecture-migration/plans/phase-6-project-snapshot-save-boundary.md`.

  **Acceptance:** The worker can point to one restore entrypoint, one restore guard, and the four canonical slices. Any ambiguity about a second restore boundary or hidden writer ownership stops the slice.

  **Happy QA:** Characterization proves `BeginProjectRestore()` is the guard boundary and the load path enters through `ProjectLoadOrchestrator`.

  **Failure QA:** A probe for a second restore boundary, second restore coordinator, or direct bypass of the session guard must fail and be reported as a blocker.

  **Commands / receipt:** `mkdir docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/logs 2>nul`; `dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo`; `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~ProjectSessionTests|FullyQualifiedName~ProjectSessionLegacyStoreGuardTests|FullyQualifiedName~ProjectLifecycleFlowCharacterizationTests" --logger "trx;LogFileName=slice-1-restore-boundary.trx" --results-directory "docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/logs"`; receipt `docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/slice-1-restore-boundary.md`.

  **Next gate / Commit:** only Todo 2 after PASS. No commit in planning mode; expected receipt is a restore-boundary characterization note.

- [ ] 2. `src/Services/Project/ProjectFileService.cs` and `src/ViewModels/Results/ResultsViewModel.cs`: prove project-open input shape and UI handoff - expect load to accept only validated project data and hand off to the same restore path

  **Goal:** Establish the exact file-load boundary, the error-handling path, and the UI handoff that triggers restore.

  **Write-set / change class:** tests/evidence only at this stage. No production edits unless the live code proves a contract gap that already exists in scope.

  **References:** `src/Services/Project/ProjectFileService.cs`; `src/ViewModels/Results/ResultsViewModel.cs`; `src/Services/Project/IProjectFileService.cs`.

  **Acceptance:** The worker can show that project load returns a typed result, that UI error handling stays on the existing dialog boundary, and that successful load flows into the same restore path used elsewhere.

  **Happy QA:** Characterize successful and failed `LoadProjectResultAsync` paths and the `LoadProjectFromPathAsync` UI handoff.

  **Failure QA:** Invalid file, missing file, or deserialization failure must stop before any restore mutation and surface the existing error boundary only.

  **Commands / receipt:** `mkdir docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/logs 2>nul`; `dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo`; `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~ProjectFileServiceResultTests|FullyQualifiedName~ProjectFileServiceMutationTests|FullyQualifiedName~ResultsViewModelOpenProjectTests" --logger "trx;LogFileName=slice-2-load-boundary.trx" --results-directory "docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/logs"`; receipt `docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/slice-2-load-boundary.md`.

  **Next gate / Commit:** only Todo 3 after PASS. Expected receipt is a load-boundary characterization note.

- [ ] 3. `src/Services/Project/ProjectLoadOrchestrator.cs` and restore-related tests: enforce validation-before-mutation and canonical four-slice order - expect restore to reject bad input before any slice write

  **Goal:** Implement or confirm restore input validation before mutation, then preserve the fixed Climate → Construction → Thermal → Hydraulics order.

  **Write-set / change class:** production/test. This is the first slice that may require production changes if live code does not already satisfy the boundary.

  **References:** `src/Services/Project/ProjectLoadOrchestrator.cs`; `src/Services/Project/ProjectSession.cs`; existing restore characterization tests under `tests/SnowMeltingCalculator.Tests/Services/Project/`.

  **Acceptance:** Invalid restore input fails before any canonical slice mutation. Valid restore reaches the four slices in the fixed order and does not add a new data path or write a stale DTO back into the session.

  **Happy QA:** A focused restore test proves the order and the pre-mutation validation boundary.

  **Failure QA:** One-invalid-field probes fail early; any slice write observed before validation is a blocker.

  **Commands / receipt:** `mkdir docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/logs 2>nul`; `dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo`; `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~ProjectLifecycleFlowCharacterizationTests|FullyQualifiedName~ProjectSessionTests|FullyQualifiedName~ProjectSessionThermalStateTests|FullyQualifiedName~ProjectSessionHydraulicsStateTests" --logger "trx;LogFileName=slice-3-validation-order.trx" --results-directory "docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/logs"`; receipt `docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/slice-3-validation-order.md`.

  **Next gate / Commit:** only Todo 4 after PASS. Expected receipt is a restore-validation/order characterization note.

- [ ] 4. `src/Services/Project/ProjectSession*.cs` and calculation tests: publish exactly one calculation after canonical restore - expect fresh session-derived results and no second calculation path

  **Goal:** Keep the restore flow to a single post-commit calculation and ensure the published calculation state comes from the session, not from stale persisted results.

  **Write-set / change class:** production/test. Potential production writes stay inside the existing session/state boundary.

  **References:** `src/Services/Project/ProjectSessionThermalState.cs`; `src/Services/Project/ProjectSessionHydraulicsState.cs`; `src/Services/Project/ThermalStateCoordinator.cs`; `src/Services/Project/IHydraulicsStateCoordinator.cs`; current calculation characterization tests.

  **Acceptance:** Exactly one calculation occurs after canonical restore, and the resulting thermal/hydraulic session state is what downstream code sees.

  **Happy QA:** Calculation multiplicity tests show one calculation call and one publication step.

  **Failure QA:** A calculation failure must not create a second restore or second calculation attempt; the existing failure boundary must remain the terminal outcome.

  **Commands / receipt:** `mkdir docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/logs 2>nul`; `dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo`; `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~ProjectSessionThermalStateTests|FullyQualifiedName~ProjectSessionHydraulicsStateTests|FullyQualifiedName~HydraulicsMultiplicityCharacterizationTests" --logger "trx;LogFileName=slice-4-calculation-publication.trx" --results-directory "docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/logs"`; receipt `docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/slice-4-calculation-publication.md`.

  **Next gate / Commit:** only Todo 5 after PASS. Expected receipt is a calculation-publication note.

- [ ] 5. `src/Services/Reports/Calculation/CalculationReportDataBuilder.cs` and `src/Services/Reports/Calculation/CalculationReportExportService.cs`: make report/export consume current session state - expect no recalculation and no stale DTO read

  **Goal:** Keep report generation tied to current session/calculation state and preserve the existing export boundary.

  **Write-set / change class:** production/test. Evidence-only is allowed only for already-proven secondary export mechanics; the central fresh-vs-stale source-of-truth proof must be backed by an executable assertion.

  **References:** `src/Services/Reports/Calculation/CalculationReportDataBuilder.cs`; `src/Services/Reports/Calculation/CalculationReportExportService.cs`; report builder/export tests under `tests/SnowMeltingCalculator.Tests/Services/Reports/Calculation/`.

  **Acceptance:** The report path uses the fresh session state that exists after restore and calculation, and the export path does not recompute or mutate session data. The worker must add or identify a sentinel assertion where persisted DTO data intentionally differs from current session/calculation state after restore; report/export output must contain the session-derived value and must not contain the stale persisted DTO sentinel.

  **Happy QA:** Report snapshot and export tests prove the data source is current session state with a positive fresh-vs-stale sentinel that would fail if the report builder read saved DTO results.

  **Failure QA:** A stale persisted DTO sentinel must not appear in generated report/export output. Mutating the underlying source after snapshot or forcing export cancellation must not corrupt the existing report boundary.

  **Commands / receipt:** `mkdir docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/logs 2>nul`; `dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo`; `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~CalculationReportDataBuilderTests|FullyQualifiedName~CalculationReportExportServiceTests|FullyQualifiedName~CalculationReportInventoryTests|FullyQualifiedName~CalculationReportWarningTests" --logger "trx;LogFileName=slice-5-report-source-of-truth.trx" --results-directory "docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/logs"`; receipt `docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/slice-5-report-source-of-truth.md`.

  **Next gate / Commit:** only Todo 6 after PASS. Expected receipt is a report-source-of-truth note.

- [ ] 6. `src/Services/Project/ProjectLoadOrchestrator.cs` and catalog-related tests: preserve read-only catalog behavior on project open - expect no global material/template mutation

  **Goal:** Confirm that project-open remains read-only for global catalogs and that custom material state stays project-local.

  **Write-set / change class:** production/test. No catalog CRUD or template-import feature work.

  **References:** `src/Services/Project/ProjectLoadOrchestrator.cs`; `src/Services/Project/ConstructionPersistenceMapper.cs`; catalog/open-project tests and any existing catalog-hash probes.

  **Acceptance:** A successful open changes only project-local state; global catalog data remains byte-stable and no CRUD path is introduced.

  **Happy QA:** Open-project and catalog-boundary tests demonstrate unchanged global catalog hashes or equivalent read-only evidence.

  **Failure QA:** Any mutation of the global catalog during open is an immediate blocker.

  **Commands / receipt:** `mkdir docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/logs 2>nul`; `dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo`; `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~ProjectSaveServiceTests|FullyQualifiedName~ProjectPersistenceMapperTests|FullyQualifiedName~ProjectFileServiceResultTests|FullyQualifiedName~ResultsViewModelOpenProjectTests" --logger "trx;LogFileName=slice-6-catalog-boundary.trx" --results-directory "docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/logs"`; receipt `docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/slice-6-catalog-boundary.md`.

  **Next gate / Commit:** only Todo 7 after PASS. Expected receipt is a catalog-boundary note.

- [ ] 7. `src/Configuration/ServiceCollectionExtensions.cs`, `src/ViewModels/Results/ResultsViewModel.cs`, and architecture maps: keep DI/UI adapters aligned with the live restore path - expect adapters to refresh only after successful restore

  **Goal:** Keep the application wiring consistent with the restore boundary and avoid introducing a second service path or pre-success UI refresh.

  **Write-set / change class:** production/test/architecture evidence.

  **References:** `src/Configuration/ServiceCollectionExtensions.cs`; `src/ViewModels/Results/ResultsViewModel.cs`; `docs/architecture-migration/maps/*.md`; `docs/architecture-migration/architecture-widget.html` if the live write-set requires refresh.

  **Acceptance:** DI resolves the same restore service shape used in tests, UI refreshes happen only after successful restore/calculation, and the architecture artifacts match the live boundary. UI-facing assertions must preserve the same fresh-vs-stale invariant from Slice 5: post-open UI/report state is derived from restored session/calculation state, not stale saved DTO result fields.

  **Happy QA:** DI and user-flow tests prove the adapters call the same restore path, refresh only after success, and expose session-derived post-restore state when saved DTO result fields contain a stale sentinel.

  **Failure QA:** A real failure injected through the wired graph must preserve the existing user-visible boundary and release the guard; stale saved DTO sentinel data must not surface through the UI/report handoff.

  **Commands / receipt:** `mkdir docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/logs 2>nul`; `dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo`; `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~DiRegistrationTests|FullyQualifiedName~ResultsViewModelOpenProjectTests|FullyQualifiedName~ProjectSessionTests" --logger "trx;LogFileName=slice-7-di-ui-alignment.trx" --results-directory "docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/logs"`; receipt `docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/slice-7-di-ui-alignment.md`.

  **Next gate / Commit:** only Todo 8 after PASS. Expected receipt is a DI/UI alignment note.

- [ ] 8. `docs/architecture-migration/` evidence and phase gate outputs: refresh the Phase 7 dossier only as needed - expect evidence that matches the live write-set and no scope creep

  **Goal:** Update the architecture dossier/evidence so it describes the accepted live boundary, not the older overgrown relaunch narrative.

  **Write-set / change class:** architecture evidence only.

  **References:** the Phase 7 plan, the relevant maps, `TASK_CONTEXT.md`, and the phase-specific evidence directory.

  **Acceptance:** The evidence points to the actual live contract, the plan no longer claims the removed recovery framework, and the resulting dossier is consistent with the approved scope.

  **Happy QA:** Evidence artifacts reference the same live boundaries and invariants established in Todos 1-7, including the build-before-test receipts and the fresh-vs-stale sentinel proof from report/UI verification.

  **Failure QA:** Any attempt to expand scope beyond the approved restore/report/UI boundary must be rejected and recorded as out of scope.

  **Commands / receipt:** `mkdir docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch 2>nul`; `node docs/architecture-migration/widget/verify-widget.mjs`; `git diff --check`; worker content-review of the architecture/evidence diff mapping each changed dossier statement to Todos 1-7 evidence; receipt `docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/slice-8-dossier-alignment.md`.

  **Next gate / Commit:** only Final verification wave after PASS. Expected receipt is a dossier-alignment note.

## Final verification wave

- [ ] F1. Scope, provenance and invariant check - expect the plan to preserve the approved Phases 2-6 contracts and the current Phase 7 boundary

  Verify canonical plan identity, scope, must-not-have rules, and the preserved invariants from the approved earlier phases. Confirm the plan still rejects `.smc` format drift, recovery-framework creep, and any new owner gate for private implementation details.

- [ ] F2. Code-boundary and architecture check - expect one restore boundary, one calculation publication path, and one report source of truth

  Independently inspect the live source paths for restore, calculation, report, and UI wiring. Confirm the worker can point to the exact current source of truth for each boundary and that no hidden second path exists.

- [ ] F3. Executable QA check - expect every slice to have agent-executed happy/failure coverage with named commands and receipts

  Verify that each slice lists a concrete command or test filter, an explicit build-before-test step where focused tests run with `--no-build`, an explicit receipt path, and at least one happy and one failure assertion. Do not re-run product tests in this planning session; this wave audits the worker-facing commands and receipts recorded in Todos 1-8. Any command plan that could pass with 0 matching tests fails this gate.

- [ ] F4. Consolidated stop check - expect one final receipt set and then a stop for owner acceptance

  Consolidate the three review domains, confirm the plan is still within scope, and stop without execution. The result is only a plan handoff; any later execution still requires the separate worker session started by the user.

## Commit strategy

The planner does not stage, commit, or run product code. Only `.omo` plan artifacts are edited in this session. Execution, if approved later, belongs to the separate worker session.

## Success criteria

- The plan is shortened to 8 execution slices plus 4 final verification gates.
- The restore boundary is explicit, singular, and tied to the current live code paths.
- Validation-before-mutation, canonical restore order, exactly-once calculation, fresh publication, report source-of-truth, and catalog non-mutation are all preserved.
- The plan contains no recovery-management subsystem, no legacy-format expansion, and no scope drift beyond the approved Phase 7 boundary.
