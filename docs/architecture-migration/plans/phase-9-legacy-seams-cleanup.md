# phase-9-legacy-seams-cleanup - Work Plan

## TL;DR (For humans)

This plan closes the legacy seams that earlier phases had to leave open, and it takes its scope directly from the named Phase 9 debts recorded at Phase 8 acceptance. Four work areas, one sequential lane:

1. **Results ↔ Circuits shared objects** — `ResultsViewModel` still holds the staged Phase 8 fallback: shared mutable `CircuitRow` objects with `CircuitsViewModel` (Results writes `circuit.DisplayMode` into module objects), `UpdateCollectorSummary` reading the VM's selection, and `HydraulicSummaryBuilder.Build*(IEnumerable<CollectorData>)` taking the module model as input. This slice reconstructs Results-owned projection objects from canonical `HydraulicsState` snapshots, covers `ST-026`/`ST-027`, and closes the Results clause of `INV-016`.
2. **INV-008** — `ProjectLoadOrchestrator` still depends on four concrete module ViewModels. This slice re-grounds the dependency, moves the orchestrator to state/application boundaries, and adds the static architecture test named by the invariant. Phase 7 restore contracts (validation order, validate-first, exactly-once publication, rejected-restore preservation) are re-proven, not redesigned.
3. **Legacy forwarding aliases** — `IProjectStateService` / `IProjectInfoService` / `IMarkDirtyService` and the legacy `ProjectStateService` class are removed; consumers re-target `IProjectSession`; dead `IMarkDirtyService` constructor parameters in the migrated module ViewModels go away; dirty semantics stay byte-for-byte characterized.
4. **LIM-P8-2** — the pre-existing import-removal baseline anomaly (5 failing characterization tests: `LoadProjectDataAsync_{Early,Late}RestoreFailure_*` ×4, `ProjectData_Load_ImportsCustomMaterialsBeforeLayers`; the `ImportProjectMaterialsAsync`/`ImportProjectTemplatesAsync` calls were removed from `ProjectLoadOrchestrator` by an unattributed pre-existing delta and landed in the baseline commit). The owner picks between reinstating the import inside the restore boundary and re-pinning the tests; the slice implements the recorded choice and returns the full suite to green.

The plan preserves the approved Phase 2-8 foundation: `DEC-001 = A` (`CalculationContext` stays the downstream read-projection seam; no writer changes to `ST-020..ST-022`), the Phase 7 restore boundary, the Phase 6 save boundary, the Phase 8 derived Results projection, and `.smc` wire compatibility. Explicitly out of scope: global closure of the unknown reactive counters (`INV-010`), Markdown removal, and any export/PDF/Excel/preview/print behavior change. The main risk is Slice 5 (orchestrator decoupling) silently changing restore behavior; the plan answers that with a frozen-contract re-proof suite and hard stop rules, not with trust.

## Scope

### Authority and frozen-plan lifecycle

- Authoring candidate: `docs/architecture-migration/plans/phase-9-legacy-seams-cleanup.md`, authored directly in the canonical plans location. Terminal review and owner plan approval freeze this file; a `.omo/plans/` mirror, if created later, is an operational execution ledger only and is not a second authority.
- Active dossier authority: `docs/architecture-migration/AGENTS.md` and the latest `docs/architecture-migration/TASK_CONTEXT.md`.
- Planning approval authorizes plan writing only; execution still belongs to a separate worker session started by the user.
- Todo write-sets and commands below describe downstream worker execution, not this planning session.
- Baseline: the tracked worktree is clean at commit `3a077c7` (phase-8 dossier); the only untracked paths are unrelated `docs/workspace/*` presentation files and stay protected. The dirty baseline-relative delta discipline therefore applies to a clean tracked baseline; every slice receipt must still record its exact write-set.

### In scope

- Results-owned hydraulic projection objects: `Results.Circuits` rebuilt from `IProjectSession.HydraulicsState` collector snapshots, `DisplayMode` writes landing on Results-owned copies, and a negative probe proving Results no longer mutates module objects.
- `UpdateCollectorSummary` selection re-sourcing so no projection read touches `_circuitsViewModel`; selection stays UI state owned by the Results layer, initialized from canonical collectors.
- `HydraulicSummaryBuilder` input canonicalization (collector snapshots instead of `CollectorData`; the builder is used only by Results, so the refactor is contained) with byte-identical summary-card output against the frozen fixtures.
- Removal of the last concrete module-ViewModel reference (`CircuitsViewModel`) from `ResultsViewModel` construction, fields, `AddResultsModule` wiring, and `ResultsViewModelTestHelpers`.
- `ProjectLoadOrchestrator` decoupling from `ClimateViewModel`, `ConstructionViewModel`, `ThermalViewModel`, `CircuitsViewModel` through canonical state/application boundaries, plus the static architecture test that rejects application-service constructors referencing concrete ViewModel types (`INV-008` verification method from `maps/target-invariants.md`).
- Removal of the legacy forwarding aliases `IProjectStateService`, `IProjectInfoService`, `IMarkDirtyService` and re-targeting of their consumers (`MainWindow.xaml.cs`, `MainViewModel` dirty/file-path PropertyChanged subscription, `ResultsViewModel`, DI forwarding registrations in `ServiceCollectionExtensions.cs:201-203`); removal of the dead `IMarkDirtyService` parameters in `ClimateViewModel`, `ConstructionViewModel`, `ThermalViewModel`; explicit disposition of the legacy `ProjectStateService` class (its remaining live consumers are test seams).
- LIM-P8-2 resolution: one recorded owner decision (reinstate import vs re-pin tests) and the implementation of that decision, returning the full suite to zero failures except the known external-fixture skip (RR-004).
- Architecture evidence refresh for the closed seams: `ST-026`/`ST-027` → covered, `INV-008` → verified with static-test evidence, `INV-016` Results clause closed, honest `INV-006`/`INV-007` progress notes (global closure still blocked by `INV-010`), `EV-P9-*` model records, deterministic widget regeneration, and the owner-authorized verifier exemplar amendment (`INV-008` → next genuinely open invariant, `INV-010`) in `widget/verify-widget.mjs` following the Phase 7.5 precedent.

### Must NOT have

- No change to `CalculationContext` writers or the `ST-020..ST-022` seam disposition (`DEC-001 = A` intact); no new production writer into `CalculationContext`.
- No `INV-010` work beyond the verifier exemplar re-point: no subscription-framework change, no global reactive-counter closure, no new invalidation semantics.
- No `.smc` schema/persistence-format change, no `ProjectData` wire-shape change, no Markdown removal, no export/PDF/Excel/preview/print behavior change.
- No redesign of the Phase 7 restore boundary: validation order (Climate → Construction → Thermal → Hydraulics), validate-first semantics (`DEC-003 = C`), exactly-once calculation publication, and rejected-restore preservation are re-proven, not reworked. If decoupling cannot proceed without changing restore behavior, the slice stops with `OWNER_DECISION_REQUIRED`.
- No removal of canonical module state or of VM adapter state that is legitimately UI-local; the target is shared *seams*, not adapter existence.
- No new canonical store, no second restore boundary, no second save boundary.
- No production-code implementation in this planning session; no test execution in this planning session.
- No `TASK_CONTEXT.md` change during execution except the dated dossier entries named in Slice 8.

## Verification strategy

- Grounding is exploration-first: the worker re-verifies every inventory claim below against live code before editing (file/line references in this plan are planning-time observations from commit `3a077c7`, not frozen truths).
- Each execution slice has agent-executable happy/failure QA with a concrete command or test filter and a receipt path; the downstream worker, not the planner, executes those commands. Build-before-test is mandatory: `dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo` before any `dotnet test --no-build`; any focused command executing 0 tests is a failed verification even if the exit code is 0.
- Characterization is never weakened: the Phase 8 frozen contracts (result-zeroing without recalculation, projection multiplicity, fresh-vs-stale sentinel, rejected-restore preservation) stay in force. Where a test pins a seam this phase removes, the equivalent boundary assertion replaces it and the replacement is recorded in the slice receipt. The 5 LIM-P8-2 tests may be changed only through the recorded owner decision, never silently.
- The static architecture test (Slice 5) is the executable `INV-008` proof: it must fail on the pre-slice code (RED run recorded) and pass after; a test that never observed the violation proves nothing.
- LIM-P8-2 presents the owner a concrete two-option choice with the characterized consequences of each; the slice stops at `OWNER_DECISION_REQUIRED` until the owner records the choice. If the owner answered at plan-approval time, the recorded answer is reused.
- Evidence reuse: Phase 7 restore receipts and Phase 8 projection receipts are the frozen-contract baseline this phase re-proves against; they are never rewritten.

## Execution strategy

One sequential lane. Read-only inspection and evidence gathering can be parallel where independent. The worker locks the baseline and seam inventory first, resolves LIM-P8-2 to restore a fully green regression baseline, then removes the shared seams in increasing blast radius (Results-owned objects → builder/selection/DI → orchestrator → aliases), and closes with the multiplicity/regression wave and the dossier refresh. Any slice that would alter behavior outside the named boundaries must stop for owner decision. Prometheus does not run product tests in this session; the worker creates the evidence directory, builds, runs the exact commands, and rejects 0-test executions.

## Todos

- [ ] 1. `src/ViewModels/Results/ResultsViewModel.cs`, `src/Services/Project/ProjectLoadOrchestrator.cs`, alias surfaces, and the stabilization suites: lock the legacy-seam baseline and the full consumption inventory - expect current seam behavior frozen before any production change

  **Goal:** Freeze the current shared-seam behavior on the clean baseline and record the complete inventory this phase will remove.

  **Write-set / change class:** tests/evidence only at this stage. No production edits in this slice.

  **References:** `src/ViewModels/Results/ResultsViewModel.cs` (staged residual sites: `_circuitsViewModel` field `:42`, ctor `:509/:530`, `UpdateCollectorSummary` `:1399-1422`, module-object `DisplayMode` write `:1429`, builder calls `:1445/:1464/:1478`, Reset ordering comment `:1597-1599`, `_markDirtyService.MarkDirty()` `:72/:92`); `src/Services/Project/ProjectLoadOrchestrator.cs` (four concrete VM fields `:27-30`, ctor `:44-57`); `src/Services/Results/{IProjectStateService,IProjectInfoService,IMarkDirtyService,ProjectStateService}.cs`; `src/Services/Project/ProjectSession.cs:16` (`ProjectSession : IProjectSession, IProjectStateService, IMarkDirtyService`); `src/Configuration/ServiceCollectionExtensions.cs:83,201-203`; `src/MainWindow.xaml.cs:35`; `src/ViewModels/Shell/MainViewModel.cs:29,51,182-183`; `docs/architecture-migration/evidence/phase-8-results-derived-projection/{slice-4-thermal-hydraulics-resourcing.md,slice-6-module-vm-decoupling.md,final-f3-executable-qa.md}`.

  **Acceptance:** The receipt contains four inventories: (a) every `_circuitsViewModel` read/write site in `ResultsViewModel` with the canonical replacement candidate per site; (b) every alias-member consumption site (`IProjectStateService`, `IProjectInfoService`, `IMarkDirtyService`, legacy `ProjectStateService`) across production and tests, separating live production consumers from dead parameters and test seams; (c) every concrete-ViewModel member the orchestrator actually touches, with the state/application boundary candidate per member; (d) the LIM-P8-2 cluster re-run on the clean baseline showing exactly the 5 pre-existing failures plus the RR-004 skip. The stabilization and collector suites pass unmodified on the baseline.

  **Happy QA:** The existing Results stabilization, collector-equipment, and lifecycle suites pass unchanged, proving the baseline is green apart from the known LIM-P8-2 cluster.

  **Failure QA:** Any previously unrecorded shared-seam site (a Results write into module objects, an alias consumer not in the inventory, an orchestrator VM member) discovered during grounding is added to the receipt and, if it changes the phase boundary, stops the lane as `OWNER_DECISION_REQUIRED`.

  **Commands / receipt:** `mkdir -p docs/architecture-migration/evidence/phase-9-legacy-seams-cleanup/logs`; `dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo`; `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~ResultsStabilizationPhase1BehaviorContractsTests|FullyQualifiedName~ResultsStabilizationPhase1ContractsTests|FullyQualifiedName~ResultsViewModelCollectorEquipmentItemsTests|FullyQualifiedName~ProjectLifecycleFlowCharacterizationTests" --logger "trx;LogFileName=slice-1-legacy-seam-baseline.trx" --results-directory "docs/architecture-migration/evidence/phase-9-legacy-seams-cleanup/logs"`; receipt `docs/architecture-migration/evidence/phase-9-legacy-seams-cleanup/slice-1-legacy-seam-baseline.md`.

  **Next gate / Commit:** only Todo 2 after PASS. Expected receipt is a legacy-seam baseline note with the four inventories.

- [ ] 2. `src/Services/Project/ProjectLoadOrchestrator.cs` (import step) or the LIM-P8-2 test cluster, per recorded owner decision: resolve the import-removal baseline anomaly - expect the full suite green except the known external-fixture skip

  **Goal:** Record one owner decision for LIM-P8-2 and implement it, restoring a fully green regression baseline for the rest of the phase.

  **Write-set / change class:** production/test. The write-set is exactly one of: (a) `ProjectLoadOrchestrator.cs` plus restore/invalidation tests if the owner chooses reinstatement; (b) the 5 named tests if the owner chooses re-pinning. No other production edits in this slice.

  **References:** `docs/architecture-migration/evidence/phase-8-results-derived-projection/final-f3-executable-qa.md` (LIM-P8-2 provenance); `git show 80579e8:src/Services/Project/ProjectLoadOrchestrator.cs` lines 111/118 (`ImportProjectMaterialsAsync(data.CustomMaterials)`, `ImportProjectTemplatesAsync(data.CustomTemplates)`); `tests/SnowMeltingCalculator.Tests/Construction/ConstructionServiceTests.cs:973` (`ProjectData_Load_ImportsCustomMaterialsBeforeLayers`); the 4 restore-failure tests `LoadProjectDataAsync_{Early,Late}RestoreFailure_*`: two in `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectLifecycleFlowCharacterizationTests.cs` (`:281`, `:325`) and two in `tests/SnowMeltingCalculator.Tests/Services/Project/ThermalMultiplicityCharacterizationTests.cs` (`:1278`, `:1312` — the worker re-grounds this file attribution before editing); `DEC-003 = C` in `TASK_CONTEXT.md` (external catalog imports are not transactionally reversible).

  **Decision contract:** Option A — reinstate the import calls inside the restore boundary, placed only after full module validation and before the ordered canonical commit (validate-first preserved; the receipt records that a failure after import leaves imported catalog rows in place, per DEC-003), with the 4 restore-failure tests re-grounded to the reinstated semantics. Option B — accept the import-less restore as the new characterized behavior, re-pin the 5 tests, and record the user-visible consequence (projects with custom materials no longer re-import them on load) as an owner-approved behavior change. The slice stops with `OWNER_DECISION_REQUIRED` until the owner records the choice in-session or at plan approval.

  **Acceptance:** The owner decision is recorded verbatim in the receipt with its option letter; the chosen branch is implemented; the 5 tests pass; a full-suite run reports 0 failed tests except the RR-004 external-fixture skip.

  **Happy QA:** The named 5-test cluster plus the lifecycle suite pass after the chosen branch.

  **Failure QA:** Any attempt to change the unchosen branch "while nearby", to widen the write-set beyond the named files, or to fix the failures without a recorded owner decision is a blocker.

  **Commands / receipt:** `mkdir -p docs/architecture-migration/evidence/phase-9-legacy-seams-cleanup/logs`; `dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo`; `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~ProjectLifecycleFlowCharacterizationTests|FullyQualifiedName~ThermalMultiplicityCharacterizationTests|FullyQualifiedName~ProjectData_Load_ImportsCustomMaterialsBeforeLayers" --logger "trx;LogFileName=slice-2-lim-p8-2-resolution.trx" --results-directory "docs/architecture-migration/evidence/phase-9-legacy-seams-cleanup/logs"`; full suite: `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --no-build --logger "trx;LogFileName=slice-2-full-suite.trx" --results-directory "docs/architecture-migration/evidence/phase-9-legacy-seams-cleanup/logs"`; receipt `docs/architecture-migration/evidence/phase-9-legacy-seams-cleanup/slice-2-lim-p8-2-resolution.md`.

  **Next gate / Commit:** only Todo 3 after PASS. Expected receipt is the decision record plus the green-suite proof.

- [ ] 3. `src/ViewModels/Results/ResultsViewModel.cs` (`UpdateCircuitsFilter`, Circuits projection) and Results tests: rebuild Results-owned circuit projection objects from canonical snapshots - expect no shared mutable object with `CircuitsViewModel`

  **Goal:** Replace the shared `CircuitRow` objects with Results-owned copies reconstructed from `IProjectSession.HydraulicsState` collector snapshots, so `circuit.DisplayMode` writes (Results `:1429`) mutate only Results-owned objects.

  **Write-set / change class:** production/test. Writes stay inside `ResultsViewModel`, its projection types, and Results tests.

  **References:** `src/ViewModels/Results/ResultsViewModel.cs`; `src/Services/Project/ProjectSessionHydraulicsState.cs`; `src/Models/Hydraulics/CircuitRow.cs` (incl. `TotalLength` `:218`); `docs/architecture-migration/maps/state-inventory.md` row `ST-026`; `docs/architecture-migration/evidence/phase-8-results-derived-projection/slice-4-thermal-hydraulics-resourcing.md`.

  **Acceptance:** `Results.Circuits` holds objects no other component references; the module VM's collection objects are provably untouched after projection rebuild (negative probe comparing instance identity or deep state before/after); all displayed circuit values match the frozen Phase 8 baseline in every characterized scenario; the KPI chain (`CalculateSystemVolume` Σ `CircuitLength + SupplyLength`) is unchanged.

  **Happy QA:** Stabilization and collector-equipment suites prove identical projection output and the negative probe passes.

  **Failure QA:** A projection rebuild that mutates any object reachable from `CircuitsViewModel`, or that changes a displayed value against the frozen baseline, is a blocker.

  **Commands / receipt:** `mkdir -p docs/architecture-migration/evidence/phase-9-legacy-seams-cleanup/logs`; `dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo`; `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~ResultsStabilizationPhase1BehaviorContractsTests|FullyQualifiedName~ResultsViewModelCollectorEquipmentItemsTests|FullyQualifiedName~ProjectSessionHydraulicsStateTests" --logger "trx;LogFileName=slice-3-results-owned-circuits.trx" --results-directory "docs/architecture-migration/evidence/phase-9-legacy-seams-cleanup/logs"`; receipt `docs/architecture-migration/evidence/phase-9-legacy-seams-cleanup/slice-3-results-owned-circuits.md`.

  **Next gate / Commit:** only Todo 4 after PASS. Expected receipt is a Results-owned-circuits note with the negative-probe proof.

- [ ] 4. `src/ViewModels/Results/ResultsViewModel.cs`, `HydraulicSummaryBuilder`, `src/Configuration/ServiceCollectionExtensions.cs`, and Results test helpers: re-source summary-builder input and selection, and remove the last module-ViewModel reference - expect a `ResultsViewModel` with no `CircuitsViewModel` and byte-identical cards

  **Goal:** Canonicalize the `HydraulicSummaryBuilder.Build*(IEnumerable<CollectorData>)` input to collector snapshots, re-source the `UpdateCollectorSummary` selection read to Results-owned state, and remove the `_circuitsViewModel` field, constructor parameter, DI wiring, and test-helper seeding.

  **Write-set / change class:** production/test.

  **References:** `src/ViewModels/Results/ResultsViewModel.cs` (`:1399-1478`, `:1597-1599`); `src/Services/Results/HydraulicSummaryBuilder.cs`; `src/Models/Hydraulics/CollectorData.cs`; `src/Configuration/ServiceCollectionExtensions.cs` (`AddResultsModule`); `tests/SnowMeltingCalculator.Tests/ViewModels/ResultsViewModelTestHelpers.cs`; `tests/SnowMeltingCalculator.Tests/Configuration/DiRegistrationTests.cs`; `docs/architecture-migration/maps/state-inventory.md` rows `ST-026`/`ST-027`.

  **Acceptance:** Summary cards, specifications, and equipment items are byte-identical against the frozen Phase 8 fixtures (including the collector-seeding scenarios `RefreshAll_ProjectsCollectorCircuitSpecificationsEquipmentCardsAndKpi` and `ResultsPdfDataBuilder_AfterInputMutation_*`); collector selection derives from Results-owned state initialized from canonical collectors with no `_circuitsViewModel` read; the Reset() ordering behavior at `:1597-1599` (cards cleared before rebuild; VM reset ordering no longer relevant) is preserved or the equivalent contract is re-pinned with the replacement recorded; `ResultsViewModel` holds no concrete module-ViewModel reference and the DI graph resolves.

  **Happy QA:** Collector-equipment, stabilization, DI-registration, and save-service suites pass with the decoupled constructor.

  **Failure QA:** A changed card value, a selection flip caused by VM-only state, or any remaining concrete `CircuitsViewModel` reference (static probe) is a blocker.

  **Commands / receipt:** `mkdir -p docs/architecture-migration/evidence/phase-9-legacy-seams-cleanup/logs`; `dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo`; `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~ResultsViewModelCollectorEquipmentItemsTests|FullyQualifiedName~ResultsStabilizationPhase1ContractsTests|FullyQualifiedName~ResultsStabilizationPhase1BehaviorContractsTests|FullyQualifiedName~DiRegistrationTests|FullyQualifiedName~ProjectSaveServiceTests" --logger "trx;LogFileName=slice-4-builder-selection-decoupling.trx" --results-directory "docs/architecture-migration/evidence/phase-9-legacy-seams-cleanup/logs"`; receipt `docs/architecture-migration/evidence/phase-9-legacy-seams-cleanup/slice-4-builder-selection-decoupling.md`.

  **Next gate / Commit:** only Todo 5 after PASS. Expected receipt is a builder/selection decoupling note.

- [ ] 5. `src/Services/Project/ProjectLoadOrchestrator.cs`, `src/Configuration/ServiceCollectionExtensions.cs`, and a new static architecture test: decouple the orchestrator from concrete ViewModels - expect INV-008 satisfied with Phase 7 restore contracts re-proven

  **Goal:** Remove the four concrete module-ViewModel dependencies (`ClimateViewModel`, `ConstructionViewModel`, `ThermalViewModel`, `CircuitsViewModel`; fields `:27-30`, ctor `:44-57`) from `ProjectLoadOrchestrator` and satisfy `INV-008` ("Application services SHALL NOT depend on concrete ViewModels") without changing any Phase 7 restore contract.

  **Write-set / change class:** production/test. The preferred design keeps the orchestrator consuming only `ProjectSession` slices and existing application mutation boundaries, and moves view-side effects (for example `SearchQuery` reset `:82`, adapter lifecycle application `:83`) to the adapter/shell layer driven by the restore outcome; if the live code cannot establish that without behavior change, the fallback is an abstraction interface owned by the application layer (not by the ViewModels), and neither branch may alter validation order, validate-first semantics, exactly-once publication, or rejected-restore preservation. The chosen design, the per-member routing table from Slice 1 inventory (c), and any `OWNER_DECISION_REQUIRED` stop are recorded in the receipt.

  **References:** `src/Services/Project/ProjectLoadOrchestrator.cs`; `src/Services/Project/{ProjectSession.cs,ProjectSessionClimateState.cs,ProjectSessionConstructionState.cs,ThermalStateCoordinator.cs,HydraulicsStateCoordinator.cs}`; `src/Configuration/ServiceCollectionExtensions.cs`; `docs/architecture-migration/maps/target-invariants.md` row `INV-008` (verification method: static architecture test on application-service constructors) and rows `CTE-005..CTE-008`, `DRE-032..DRE-035`, `DRN-016`; `docs/architecture-migration/plans/phase-7-project-restore-coordinator-relaunch.md` (frozen restore contracts); Phase 7 slice receipts under `docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/`.

  **Acceptance:** (a) A new static architecture test, fixed by this plan as `ApplicationServiceViewModelDecouplingTests` (the worker uses exactly this name so the slice filter matches), scans application-service constructors for concrete ViewModel types, is demonstrated RED on the pre-slice orchestrator (recorded run) and GREEN after; (b) all four VM constructor parameters are gone and the DI graph resolves; (c) the Phase 7 contract suites pass unmodified: validation order, validate-first failure path, exactly-once calculation publication, rejected-restore preservation, second-load clean replace; (d) the compile-time map claims `CTE-005..CTE-008`/`DRE-032..DRE-035` closure only after the static test confirms no other application service carries a concrete VM dependency.

  **Happy QA:** Restore/lifecycle, thermal and hydraulics multiplicity, and DI suites pass with the decoupled orchestrator.

  **Failure QA:** A restore-order change, a second calculation/publication, a rejected restore that mutates or refreshes projection state, or a static test that never observed the violation is a blocker; inventing a new canonical store to avoid the interface is a blocker.

  **Commands / receipt:** `mkdir -p docs/architecture-migration/evidence/phase-9-legacy-seams-cleanup/logs`; `dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo`; `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~ProjectLifecycleFlowCharacterizationTests|FullyQualifiedName~ThermalMultiplicityCharacterizationTests|FullyQualifiedName~HydraulicsMultiplicityCharacterizationTests|FullyQualifiedName~DiRegistrationTests|FullyQualifiedName~ApplicationServiceViewModelDecouplingTests" --logger "trx;LogFileName=slice-5-orchestrator-decoupling.trx" --results-directory "docs/architecture-migration/evidence/phase-9-legacy-seams-cleanup/logs"`; receipt `docs/architecture-migration/evidence/phase-9-legacy-seams-cleanup/slice-5-orchestrator-decoupling.md`.

  **Next gate / Commit:** only Todo 6 after PASS. Expected receipt is an orchestrator-decoupling note with the RED/GREEN static-test proof.

- [ ] 6. Alias surfaces (`src/Services/Results/*`, `src/MainWindow.xaml.cs`, `src/ViewModels/**`, `src/Configuration/ServiceCollectionExtensions.cs`) and dirty-semantics tests: remove the legacy forwarding aliases - expect dirty/save/load semantics unchanged and no alias symbol left

  **Goal:** Remove `IProjectStateService`, `IProjectInfoService`, `IMarkDirtyService`, and the legacy `ProjectStateService` class from production; re-target consumers to `IProjectSession` (or narrower session surfaces); delete the dead `IMarkDirtyService` parameters in `ClimateViewModel`, `ConstructionViewModel`, `ThermalViewModel`; record the explicit disposition of `ProjectStateService` (remaining consumers are test seams — the class either moves to test support or its test call sites migrate to `ProjectSession`).

  **Write-set / change class:** production/test.

  **References:** `src/Services/Results/{IProjectStateService.cs,IProjectInfoService.cs,IMarkDirtyService.cs,ProjectStateService.cs}`; `src/Services/Project/ProjectSession.cs:16,41,50-69` (`ProjectNumber`/`ProjectObject` members live on the session); `src/Services/Project/ProjectSessionClimateState.cs:17,35` (optional `IMarkDirtyService` climate-state dirty param) and `src/Services/Project/ProjectSessionHydraulicsState.cs:41,44` plus the `ProjectSession` ctor `IMarkDirtyService? hydraulicsDirtyService` param (hydraulics-dirty param) — both replaced by session-internal dirty paths; `src/MainWindow.xaml.cs:35,55`; `src/ViewModels/Shell/MainViewModel.cs:29,51,182-183` (dirty/file-path PropertyChanged subscription — re-proven against the session's property-raise behavior); `src/ViewModels/Results/ResultsViewModel.cs:29-31,72,92,501` (dirty calls re-target the session boundary with byte-identical dirty semantics); `src/Configuration/ServiceCollectionExtensions.cs:83,201-203`; `tests/SnowMeltingCalculator.Tests/Services/Navigation/DialogServiceThreadAffinityTests.cs:82`, `tests/SnowMeltingCalculator.Tests/Services/Project/ClimateThermalInvalidationRegressionTests.cs:418`, `tests/SnowMeltingCalculator.Tests/Construction/ConstructionServiceTests.cs:62` (test seams); `docs/architecture-migration/maps/state-ownership.md` alias notes.

  **Acceptance:** No production symbol references the removed aliases (worker grep proof in the receipt); `MainViewModel` dirty/file-path reactions fire identically on save/failure/load scenarios; Results dirty-raising at `:72`/`:92` keeps the exact characterized dirty transitions (save success cleans once, failure preserves dirty); legacy `ProjectStateService` disposition is recorded and its production DI registrations are gone; the DI graph resolves and the module VMs construct without the dead parameters.

  **Happy QA:** Reset orchestration, climate-thermal invalidation regression, dialog-service affinity, DI, and stabilization-contract suites pass.

  **Failure QA:** A dirty-state semantics change, a `MainViewModel` reaction that no longer fires (stale-subscription regression), or any production reference to a removed alias is a blocker.

  **Commands / receipt:** `mkdir -p docs/architecture-migration/evidence/phase-9-legacy-seams-cleanup/logs`; `dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo`; `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~ResetOrchestrationTests|FullyQualifiedName~ClimateThermalInvalidationRegressionTests|FullyQualifiedName~DialogServiceThreadAffinityTests|FullyQualifiedName~DiRegistrationTests|FullyQualifiedName~ResultsStabilizationPhase1ContractsTests" --logger "trx;LogFileName=slice-6-alias-removal.trx" --results-directory "docs/architecture-migration/evidence/phase-9-legacy-seams-cleanup/logs"`; receipt `docs/architecture-migration/evidence/phase-9-legacy-seams-cleanup/slice-6-alias-removal.md`.

  **Next gate / Commit:** only Todo 7 after PASS. Expected receipt is an alias-removal note with the grep proof and dirty-semantics re-pin.

- [ ] 7. Full-suite regression and repeated-cycle characterization: prove no multiplied subscriptions, no dirty drift, and unchanged save/report fixtures - expect 0 failed except the known external-fixture skip

  **Goal:** Prove the closed seams did not shift behavior: repeated new/load/second-load/reset cycles keep stable handler/event/calculator counts (`INV-011` evidence style), dirty transitions match the frozen contracts, save and report-export outputs match the frozen fixtures, and the whole suite is green.

  **Write-set / change class:** tests/evidence only (a test addition is allowed only if a seam-removal contract needs an explicit regression pin, recorded in the receipt).

  **References:** `tests/SnowMeltingCalculator.Tests/ViewModels/ResetOrchestrationTests.cs`; `tests/SnowMeltingCalculator.Tests/ViewModels/ResultsViewModelOpenProjectTests.cs`; `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectLifecycleFlowCharacterizationTests.cs`; `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectSaveServiceTests.cs`; `docs/architecture-migration/plans/phase-7-project-restore-coordinator-relaunch.md` slice 7 (repeated-cycle contract).

  **Acceptance:** Full suite: 0 failed, 1 known skip (RR-004 external fixture); repeated-cycle counts identical to the Phase 7/8 characterized expectations; the fresh-vs-stale sentinel and projection multiplicity contracts still hold; no `.smc` fixture changed (`git diff --name-only -- '*.smc'` empty).

  **Happy QA:** The full-suite TRX plus the repeated-cycle receipts.

  **Failure QA:** Any new failure, any multiplied handler/calculator count, or any `.smc` fixture diff is a blocker; the RR-004 skip is recorded as a skip, never as a pass.

  **Commands / receipt:** `mkdir -p docs/architecture-migration/evidence/phase-9-legacy-seams-cleanup/logs`; `dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo`; `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --no-build --logger "trx;LogFileName=slice-7-full-regression.trx" --results-directory "docs/architecture-migration/evidence/phase-9-legacy-seams-cleanup/logs"`; `git diff --name-only -- '*.smc'`; receipt `docs/architecture-migration/evidence/phase-9-legacy-seams-cleanup/slice-7-full-regression.md`.

  **Next gate / Commit:** only Todo 8 after PASS. Expected receipt is a full-regression note.

- [ ] 8. `docs/architecture-migration/` maps, model, widget, verifier, and `TASK_CONTEXT.md`: refresh the Phase 9 dossier only as needed - expect evidence that matches the live write-set and no scope creep

  **Goal:** Update the architecture dossier so it describes the closed legacy seams: `INV-008` verified with static-test evidence, `ST-026`/`ST-027` covered, the `INV-016` Results clause closed, honest `INV-006`/`INV-007` progress notes (global closure remains blocked by the still-open `INV-010`), the LIM-P8-2 resolution recorded, `EV-P9-*` model evidence, and the verifier exemplar amendment.

  **Write-set / change class:** architecture artifacts plus one dated `TASK_CONTEXT.md` entry.

  **References:** `docs/architecture-migration/maps/{state-ownership,state-inventory,target-invariants,compile-time,di-runtime,reactive,user-flow,characterization-tests}.md`; `docs/architecture-migration/maps/architecture-model.json`; `docs/architecture-migration/widget/verify-widget.mjs` (exemplar `INV-008` at lines 33-34 must move to the next genuinely open invariant, `INV-010`, mirroring the owner-authorized Phase 7.5 amendment pattern); `docs/architecture-migration/architecture-widget.html`; the Phase 9 evidence directory; `docs/architecture-migration/TASK_CONTEXT.md`.

  **Acceptance:** The maps record the removed seams exactly as implemented; the verifier exemplar amendment is explicitly authorized by the owner before it lands (same gate as the Phase 7.5 `INV-001` → `INV-008` amendment; if the owner has not authorized it by this slice, the slice stops with `OWNER_DECISION_REQUIRED`); `node docs/architecture-migration/widget/verify-widget.mjs` passes both suites, `generate-widget.mjs --check` passes, and the widget is reproducible; the dated `TASK_CONTEXT.md` entry records the Phase 9 execution result, the LIM-P8-2 decision, and the exemplar amendment.

  **Happy QA:** Verifier suites and widget-generation check pass; dossier statements map one-to-one to slice receipts.

  **Failure QA:** Any claim of `INV-010` closure, `CalculationContext` disposition change, Markdown removal, or export-behavior change is rejected and recorded as out of scope; an unauthorized exemplar edit is a blocker.

  **Commands / receipt:** `mkdir -p docs/architecture-migration/evidence/phase-9-legacy-seams-cleanup`; `node docs/architecture-migration/widget/verify-widget.mjs`; `node docs/architecture-migration/widget/generate-widget.mjs --check` (or the dossier's established check invocation); `git diff --check`; worker content-review of the architecture/evidence diff mapping each changed dossier statement to slice receipts; receipt `docs/architecture-migration/evidence/phase-9-legacy-seams-cleanup/slice-8-dossier-alignment.md`.

  **Next gate / Commit:** only the Final verification wave after PASS. Expected receipt is a dossier-alignment note.

## Final verification wave

- [ ] F1. Scope, provenance and invariant check - expect the plan to preserve the approved Phases 2-8 contracts and the current Phase 9 boundary

  Verify canonical plan identity, scope, must-not-have rules, and the preserved invariants from the approved earlier phases. Confirm the plan still rejects `CalculationContext` writer changes (`DEC-001 = A`), `INV-010` scope creep, `.smc` format drift, Markdown/export work, and any restore-boundary redesign beyond the named decoupling.

- [ ] F2. Code-boundary and architecture check - expect no shared mutable seam, no concrete ViewModel dependency in application services, and no alias symbol left

  Independently inspect the live source for the Results-owned projection objects, the orchestrator constructor surface, the alias removal, and the DI graph. Confirm the static architecture test exists and is wired into the suite, and that no hidden module-ViewModel path or canonical write-back remains.

- [ ] F3. Executable QA check - expect every slice to have agent-executed happy/failure coverage with named commands and receipts

  Verify that each slice lists a concrete command or test filter, an explicit build-before-test step where focused tests run with `--no-build`, an explicit receipt path, and at least one happy and one failure assertion. Do not re-run product tests in this planning session; this wave audits the worker-facing commands and receipts recorded in Todos 1-8. Any command plan that could pass with 0 matching tests fails this gate.

- [ ] F4. Consolidated stop check - expect one final receipt set and then a stop for owner acceptance

  Consolidate the three review domains, confirm the plan is still within scope, and stop without execution. The result is only a plan handoff; any later execution still requires the separate worker session started by the user.

## Commit strategy

The planner does not stage, commit, or run product code. Only the Phase 9 plan artifact and its terminal-review receipt are authored in this session. Execution, if approved later, belongs to the separate worker session; parallel owner-side commits to the clean baseline do not conflict with planning artifacts because every Phase 9 artifact is a new file and the only shared file touched during execution (`TASK_CONTEXT.md`) is append-only in Slice 8.

## Success criteria

- The plan contains 8 execution slices plus 4 final verification gates, each with concrete commands, receipts, and happy/failure QA.
- `ResultsViewModel` holds no concrete module-ViewModel reference, owns its circuit projection objects and selection state, and the summary builder consumes canonical snapshots with byte-identical output; `ST-026`/`ST-027` and the `INV-016` Results clause are covered by evidence.
- `ProjectLoadOrchestrator` depends on no concrete ViewModel, a static architecture test proves the `INV-008` boundary, and every Phase 7 restore contract is re-proven, not redesigned.
- The legacy forwarding aliases and the legacy `ProjectStateService` production registrations are removed with dirty/save/load semantics unchanged, and the LIM-P8-2 decision is recorded and implemented with a green full suite (RR-004 skip excepted).
- The dossier refresh flips only the invariants the evidence supports (`INV-008`; honest partial notes for `INV-006`/`INV-007`), records `EV-P9-*`, regenerates the widget deterministically, and amends the verifier exemplar only with explicit owner authorization.
- The plan contains no `INV-010` closure work, no `CalculationContext` disposition change, no `.smc`/export/Markdown work, and no scope drift beyond the approved Phase 9 boundary.
