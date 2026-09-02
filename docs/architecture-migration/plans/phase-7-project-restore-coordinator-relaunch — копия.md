# phase-7-project-restore-coordinator-relaunch - Work Plan

## TL;DR (For humans)

Этот relaunch plan заменяет только execution plan для Phase 7 и не считает
первую dirty попытку успешной. Worker начнёт с физически чистой копии
owner-accepted Phase 6, зафиксирует baseline и будет последовательно строить
ViewModel-free restore boundary в существующем `ProjectLoadOrchestrator`.
Сначала будут зафиксированы mapping/representation contracts, named fixtures и
RED tests; затем validated candidates, ordered four-slice commit, DEC-003=C
rollback, exactly-once calculation/publication, catalog non-mutation, report
snapshot и UI/DI wiring. После обязательного intermediate gate обновятся maps,
model/widget и user-flow evidence; затем независимые F1-F4 и отдельный owner
result-acceptance stop. `.smc` wire format, DTO fields, `Version = "1.1"`,
формулы и legacy compatibility beyond DEC-002 не меняются. План approval не
является execution authorization.

## Scope

This `.omo/plans/phase-7-project-restore-coordinator-relaunch.md` is the mutable
primary candidate for this session. Before `/architecture-approve`, the owner
must explicitly designate this exact path and final SHA-256 as the Phase 7
relaunch plan identity; `/architecture-start` must execute those approved bytes.
No copy to `docs/architecture-migration/plans/` is implied, and no worker may
silently choose the old frozen plan or another mirror. If the owner workflow
cannot accept an `.omo` candidate, stop for an owner decision rather than
promoting or editing a canonical dossier file in this plan.

### Authority and hard stops

- Active authority: `docs/architecture-migration/AGENTS.md`, current
  `TASK_CONTEXT.md`, accepted Phase 6 evidence, and this new relaunch plan.
- Preserve unchanged as provenance: `docs/architecture-migration/plans/phase-7-project-restore-coordinator.md`,
  both Phase 7 archive analyses, `archive/STATE.json`, retired workflow scripts.
- The first dirty Phase 7 attempt is not repaired, attributed, or accepted.
- A clean physical Phase 6 copy is mandatory. Stop before production edits if a
  required Phase 7 path overlaps a pre-existing dirty path.
- This artifact is under `D:\IA\ace`; R0 must prove that
  `git rev-parse --show-toplevel` equals `D:\IA\ace` and that the active
  dossier/evidence belongs to the same root. `D:\IA\ace v.2` and its historical
  metrics are not an execution baseline; root mismatch is BLOCKED.
- Plan approval, `/architecture-start`, and result acceptance are separate
  owner decisions. No stage authorizes the next phase implicitly.

### In scope

- Current `.smc` persisted-input restore through the four canonical
  `ProjectSession` slices: Climate, Construction, Thermal, Hydraulics.
- Validate-first candidate mapping; one deterministic ordered commit; clean /
  default all-four rollback on unexpected commit or calculation failure.
- Exactly one application-level calculation after successful input restore and
  publication of fresh derived values into `ProjectSession`.
- `CalculationContext` remains the existing compatibility/read-projection seam
  with its approved writers.
- Project-open catalog non-mutation; immutable session-derived report snapshot;
  report export without recalculation or session mutation.
- ViewModels as UI adapters/projections; existing path, dirty, guard,
  cancellation and user-visible error semantics unless explicitly covered by
  the contracts below.

### Must NOT have

- No persisted calculated DTO field becomes current canonical result.
- No second hydraulics restore, no second application calculation, and no report
  recalculation.
- No catalog add/update/delete/import, template import, serializer/wire/schema
  change, `Version` change, formula redesign, broad Results migration, PB-002
  work, legacy compatibility beyond DEC-002, or AppData test-infrastructure
  cleanup.
- No concrete ViewModel dependency in restore/calculation/report services.
- No manual `STATE.json` edit, git staging/commit/reset/revert/clean/force-push,
  unrelated dirty cleanup, or modification of frozen old plans/archive analyses.

## Verification strategy

Tests-first: R1/R2 create RED tests and isolated seams before production restore
implementation. Every stage has focused agent-executable happy and failure QA,
an exact command or structural check, a receipt immediately after verification,
and one permitted next gate. Full Release is a later regression gate, never a
substitute for focused boundary tests. Use serial full Release as authoritative
unless process-global AppData is isolated; record the parallel settings race as
residual risk. Try LSP once for a supported C# file; if the effective workspace
root/request-cwd rejects it, record that exact limitation and use
`dotnet build/test`, without claiming clean LSP diagnostics.

Every receipt uses this one canonical schema, in this exact order:

```text
TODO/STAGE:
WRITE-SET:
FORBIDDEN-SET:
CHANGE-CLASS:
ACCEPTANCE:
COMMANDS:
RESULTS:
HAPPY-QA:
FAILURE-QA:
EXPECTED-RESULTS:
EVIDENCE:
RESIDUAL-RISK:
ROLLBACK:
NEXT-GATE:
```

There is no shorter receipt variant: every R0-R10 receipt uses the complete
schema above. `CHANGE-CLASS` is one or more of `control/docs-only`,
`production/test`, `architecture artifacts`, and `user-visible`; all path sets,
commands, results and evidence names are literal.

### Pre-execution review gate

Before R0, the complete plan must pass one terminal Momus review. The receipt
must contain exactly `REVIEW_ID`, `SUBJECT`, `RECEIPT`, `VERDICT` (`APPROVE`,
`REJECT`, or `BLOCKED`) and `REASON`, and bind the exact plan path, UTF-8 byte
count and SHA-256. Missing, malformed or mismatched receipt blocks owner
`/architecture-approve`; approval still does not authorize execution.

### Command contract

The worker uses exact commands recorded with literal filters and evidence names:

```text
dotnet build "src\SnowMeltingCalculator.csproj" -c Debug --nologo
dotnet build "src\SnowMeltingCalculator.csproj" -c Release --nologo
dotnet test "tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Debug --filter "FullyQualifiedName~RestoreValidationTests|FullyQualifiedName~RestoreCommitOrderTests|FullyQualifiedName~RestoreRollbackTests|FullyQualifiedName~RestoreCalculationMultiplicityTests|FullyQualifiedName~RestoreCatalogBoundaryTests|FullyQualifiedName~RestoreGuardLifecycleTests|FullyQualifiedName~ReportSessionSnapshotTests|FullyQualifiedName~ProjectLoadUserFlowTests" --logger "trx;LogFileName=focused-debug.trx" --results-directory "docs\architecture-migration\evidence\phase-7-project-restore-coordinator-relaunch\logs"
dotnet test "tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Release --filter "FullyQualifiedName~RestoreValidationTests|FullyQualifiedName~RestoreCommitOrderTests|FullyQualifiedName~RestoreRollbackTests|FullyQualifiedName~RestoreCalculationMultiplicityTests|FullyQualifiedName~RestoreCatalogBoundaryTests|FullyQualifiedName~RestoreGuardLifecycleTests|FullyQualifiedName~ReportSessionSnapshotTests|FullyQualifiedName~ProjectLoadUserFlowTests" --logger "trx;LogFileName=focused-release.trx" --results-directory "docs\architecture-migration\evidence\phase-7-project-restore-coordinator-relaunch\logs"
dotnet test "tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Release --no-build --logger "trx;LogFileName=full-release.trx" --results-directory "docs\architecture-migration\evidence\phase-7-project-restore-coordinator-relaunch\logs"
node "docs\architecture-migration\widget\model-contract.mjs" "docs\architecture-migration\maps\architecture-model.json"
node "docs\architecture-migration\widget\generate-widget.mjs" --check
```

R2 must replace placeholders with literal named filters and filenames; an
unresolved placeholder is not evidence.

## Execution strategy

One sequential production lane. Independent read-only inspection and fixture
preparation may run in parallel only when they do not edit central state, load,
DI or report surfaces. The worker must stop on missing/contradictory receipt,
dirty-path overlap, failed focused gate, scope drift, or unresolved owner
decision. Each stage names its exact write-set and forbidden paths below.

Every R0-R10 receipt must contain the exact fields `TODO/STAGE`, `WRITE-SET`,
`FORBIDDEN-SET`, `CHANGE-CLASS`, `ACCEPTANCE`, `COMMANDS`, `RESULTS`,
`HAPPY-QA`, `FAILURE-QA`, `EXPECTED-RESULTS`, `EVIDENCE`, `RESIDUAL-RISK`,
`ROLLBACK`, and `NEXT-GATE`. `COMMANDS` records the literal invocation, exit
code, working directory, and artifact path; `RESULTS` records named test
methods/assertions and counts; `EVIDENCE` lists the receipt, TRX, hash, or
trace files. A receipt missing any field, or containing a non-literal command,
is not PASS and cannot unlock the next stage.

## Todos

- [ ] 1. R0. Capture clean Phase 6 baseline and execution-start boundary

  **Goal:** Establish an attributable physical baseline before any Phase 7
  production/test edit; explicitly distinguish plan approval, execution
  authorization and future result acceptance.

  **CHANGE-CLASS:** control/docs-only. **Write-set:** Create only `docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/execution-start.md`,
  `baseline/porcelain.txt`, `baseline/head.txt`, `baseline/protected-paths.json`,
  `baseline/phase7-allow-list.json`, `baseline/hashes.txt`, `logs/r0-lifecycle.trx`,
  `logs/r0-roundtrip.trx`, `r0-baseline.md`, and read-only
  `scripts/check-phase7-allow-list.mjs` control script and read-only
  control status required by the authorized workflow. The script accepts exactly
  `--baseline docs\architecture-migration\evidence\phase-7-project-restore-coordinator-relaunch\baseline\head.txt --allow-list docs\architecture-migration\evidence\phase-7-project-restore-coordinator-relaunch\baseline\phase7-allow-list.json`, compares
  `git diff --name-only` against the JSON allow-list, prints
  `FORBIDDEN_PATHS=`, and `UNEXPECTED_DELETIONS=` counts. The JSON contains only
  `protectedPaths` and `allowedChangedPaths`; the checker does not require future
  files to exist and exits 0 only when `FORBIDDEN_PATHS=0` and
  `UNEXPECTED_DELETIONS=0`. No production/test/maps/
  widget edits.

  **Prerequisites/study:** Read `docs/architecture-migration/AGENTS.md`,
  `docs/architecture-migration/TASK_CONTEXT.md`,
  `docs/architecture-migration/evidence/phase-6-project-snapshot-save-boundary/owner-result-acceptance.md`,
  `docs/architecture-migration/plans/phase-7-project-restore-coordinator.md`,
  `docs/architecture-migration/archive/phase-7-restore-relaunch-plan-hardening.md`,
  and `docs/architecture-migration/archive/phase-7-technical-failure-analysis.md`; inspect
  `ProjectSession`, `ProjectLoadOrchestrator`, `ResultsViewModel`, report
  exporter and current git state. Capture repository root, worktree, branch,
  baseline HEAD, `git status --short`, `git diff --stat`, protected-path hashes.

  **Acceptance/QA:** `git rev-parse --show-toplevel` equals `D:\IA\ace`; baseline
  HEAD is written as one full SHA-1 line to `baseline/head.txt`; branch, worktree
  and dossier root are recorded. Clean physical Phase 6
  copy is proven, or stop. No required
  Phase 7 path overlaps a pre-existing dirty path. Current restore/report/catalog
  flow and nested lease are reproducible. Run `dotnet build
  "src\SnowMeltingCalculator.csproj" -c Debug --nologo`, then
  `dotnet test "tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Debug --no-build --filter "FullyQualifiedName~ProjectLifecycleFlowCharacterizationTests" --logger "trx;LogFileName=r0-lifecycle.trx" --results-directory "docs\architecture-migration\evidence\phase-7-project-restore-coordinator-relaunch\logs"` and the same command with filter `FullyQualifiedName~ProjectRoundTripTests` and log `r0-roundtrip.trx`; expected exit code 0 with all selected tests passing. Try LSP once and record request-cwd
  limitation if rejected.

  **Failure-QA:** Run the existing `ProjectLifecycleFlowCharacterizationTests`
  filter from the R0 command with its invalid-path case, and record the exact
  fully-qualified method name discovered before execution in `execution-start.md`;
  expected exit 0, typed user-visible failure, unchanged path/dirty state and
  `IsLoadProjectInProgress == false`. A missing invalid-path case or nonzero exit
  is BLOCKED rather than treated as a negative success.

  **Failure/evidence/gate:** Any root mismatch, baseline conflict or failed prerequisite is
  BLOCKED; evidence is `execution-start.md` and `baseline/*`; only remediation
  of R0 is allowed next. Forbidden: modifying old plan, archive, `STATE.json`,
  `TASK_CONTEXT.md`, production or tests.

- [ ] 2. R1. Freeze contract and fixture matrix before implementation

  **Goal:** Define every persisted field mapping and representation boundary so
  the worker makes no implicit type/identity/lifecycle decision.

  **CHANGE-CLASS:** production/test. **Write-set:** Tests/fixture-only paths under
  `tests/SnowMeltingCalculator.Tests/Fixtures/`,
  `tests/SnowMeltingCalculator.Tests/RestoreFixtureFactoryTests.cs`,
  `docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/contracts/projectdata-mapping.md`,
  `logs/r1-fixtures.trx`, and `r1-contract-fixtures.md`. No production,
  maps, widget, DTO, serializer or `TASK_CONTEXT.md` writes.

  **Prerequisites/study:** R0 PASS receipt. Inspect `ProjectData`, all existing persistence
  mappers, four state interfaces/snapshots, `ProjectSession`, `CalculationContext`,
  material/template repositories, pipe types, report models, and current test
  helpers. The matrix must include every `ProjectData` field:
  `DTO field -> canonical target -> restore rule -> calculation rule`.

  The machine-checkable matrix is
  `docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/contracts/projectdata-mapping.md`.
  Its required columns are `DTO field`, `wire presence`, `representation level`,
  `canonical target`, `identity rule`, `null/default rule`, `validation
  boundary`, `calculation use`, `report use`, and `intentional omission reason`.

  **Required matrix decisions:** Document catalog/domain object, persistence
  DTO/snapshot, canonical `ProjectSession` snapshot, UI adapter projection and
  report snapshot levels. Name factories for each level; field-level equality
  helpers ignore runtime `Guid`, reference equality and UI identity. Record:
  `CalculatedLambda` preserved / `IsLambdaOverridden=false`; custom material
  project-local; unknown pipe invalid; null pipe invalid if required for fully
  calculable thermal input; built-in lookup read-only; restore order distinct
  from UI refresh order; one hydraulics restore; typed failure to existing UI
  boundary; clean/default rather than previous arbitrary snapshot.

  **Acceptance/QA:** First run `dotnet build "tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Debug --nologo`. Then named factory tests
  `RestoreFixtureFactoryTests.ValidCurrentFormatFixture_IsFullyCalculable` and
  `RestoreFixtureFactoryTests.NegativeFixturesChangeExactlyOneCondition` run
  with `dotnet test "tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Debug --filter "FullyQualifiedName~RestoreFixtureFactoryTests" --logger "trx;LogFileName=r1-fixtures.trx" --results-directory "docs\architecture-migration\evidence\phase-7-project-restore-coordinator-relaunch\logs"` and exit 0. Named factories exist for valid current-format complete
  calculable data, catalog/domain, persisted DTO, canonical, UI and report
  levels. No success restore test uses bare `new ProjectData()` except through
  the named valid factory. Each negative fixture changes exactly one condition
  and names its first validation boundary. Evidence records unresolved facts as
  STOP, never as fallback.

  **Failure/evidence/gate:** Matrix disagreement or missing field mapping is
  BLOCKED; receipt `r1-contract-fixtures.md`; only R1 correction next. Forbidden:
  production implementation, legacy fallback and changing old tests merely to
  make them green.

- [ ] 3. R2. Add RED characterization tests and isolated test seams

  **Goal:** Prove the new boundary is absent/fails for the intended contract,
  rather than because fixtures are malformed or DI uses another runtime shape.

  **CHANGE-CLASS:** production/test. **Write-set:** New or explicitly allow-listed tests and helpers only, including
  `RestoreValidationTests`, `RestoreCommitOrderTests`, `RestoreRollbackTests`,
  `RestoreCalculationMultiplicityTests`, `RestoreCatalogBoundaryTests`,
  `RestoreGuardLifecycleTests`, `RestoreCandidateTests`,
  `RestoreCatalogStructuralGuardTests`, `ReportSessionSnapshotTests`, and
  `ProjectLoadUserFlowTests`; fixture files from R1, all named test files, and
  `logs/r2-red-characterization.trx`, `logs/r2-green-regression.trx`. No production edits. Exact new
  test files are `RestoreCandidateTests.cs` and
  `RestoreCatalogStructuralGuardTests.cs` under the test project.

  **Prerequisites/study:** `ProjectRoundTripTests`,
  `ResultsViewModelOpenProjectTests`, `ProjectLifecycleFlowCharacterizationTests`,
  `DiRegistrationTests`, existing state/guard tests, report tests and
  `ResultsViewModelTestHelpers`.

  **Acceptance/QA:** First run `dotnet build "tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Debug --nologo`. Then add counting calculator, climate/construction/thermal/
  hydraulics commit failure seams, calculation failure seam, report snapshot spy,
  catalog repository spies and before/after SHA-256 harness. RED tests cover
  incomplete input, invalid enum/value, missing material, four individual commit
  failures, calculation failure, nested lease, stale calculated DTO fields,
  second hydraulics restore, deterministic order, exactly-once calculation,
  UI refresh-after-success and failure translation. The required user-flow
  methods are exactly `ProjectLoadUserFlowTests.NewProject_IsEmptyAndClean`,
  `ProjectLoadUserFlowTests.OpenCurrentFormatProject_RestoresAndPublishes`,
  `ProjectLoadUserFlowTests.SecondOpen_ReplacesAllFourSlices`,
  `ProjectLoadUserFlowTests.EditCalculateReset_SaveReloadExport`, and
  `ProjectLoadUserFlowTests.InvalidInput_CommitFailure_CalculationFailure_AndCancellation_PreserveBoundary`.
  Run
  `dotnet test "tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Debug --filter "FullyQualifiedName~RestoreValidationTests|FullyQualifiedName~RestoreCommitOrderTests|FullyQualifiedName~RestoreRollbackTests|FullyQualifiedName~RestoreCalculationMultiplicityTests|FullyQualifiedName~RestoreCatalogBoundaryTests|FullyQualifiedName~RestoreGuardLifecycleTests|FullyQualifiedName~ReportSessionSnapshotTests|FullyQualifiedName~ProjectLoadUserFlowTests" --logger "trx;LogFileName=r2-red-characterization.trx" --results-directory "docs\architecture-migration\evidence\phase-7-project-restore-coordinator-relaunch\logs"`
  with `r2-red-characterization.trx`; expected nonzero exit with every intended
  RED assertion failing for its named contract reason, and no setup/fixture
  exception. Record each test name and failure message. Only tests in the exact
  namespace `SnowMeltingCalculator.Tests.RestoreRedProbeTests` are expected
  negative. After implementation, the exact GREEN command is the same filter
  with `FullyQualifiedName!~RestoreRedProbeTests` and log
  `r2-green-regression.trx`; it must exit 0 and every former RED contract must
  have a passing non-probe assertion.

  **Failure/evidence/gate:** A malformed fixture or wrong DI shape is a failed
  stage, not a valid RED result. Receipt `r2-red-characterization.md`; only R2
  correction next. Forbidden: production implementation, broad suite as proof,
  modifying frozen plan or archived analysis.

- [ ] 4. R3. Implement validated persisted-input candidates and mapping completeness

  **Goal:** Convert current-format `ProjectData` into immutable, fully validated
  candidates for all four slices before any canonical mutation.

  **CHANGE-CLASS:** production/test. **Write-set:** Only
  `src/Services/Project/ProjectLoadOrchestrator.cs`,
  `src/Services/Project/ProjectPersistenceMapper.cs`,
  `src/Services/Project/ConstructionPersistenceMapper.cs`,
  `src/Services/Project/ThermalPersistenceMapper.cs`,
  `src/Services/Project/HydraulicsPersistenceMapper.cs`,
  `tests/SnowMeltingCalculator.Tests/RestoreCandidateTests.cs`,
  `logs/r3-candidates.trx`, and `r3-candidates.md`, plus the named R1/R2
  test files. No new production file may be added in R3 without an owner-approved
  plan revision. No report/UI/DI/maps/widget changes.

  **Prerequisites/study:** R2 PASS receipt. Inspect `ProjectData`, `ProjectPersistenceMapper`,
  `ConstructionPersistenceMapper`, `ThermalPersistenceMapper`,
  `HydraulicsPersistenceMapper`, all state snapshots, validators, material and
  pipe APIs. Confirm live `null` pipe validity before implementation; if it is
  not calculable, encode validation failure and test it.

  **Acceptance/QA:** Every persisted input has explicit mapping or intentional
  omission; all validation completes before mutation; calculated DTO result
  fields are not current canonical inputs. Run
  `dotnet test "tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Debug --filter "FullyQualifiedName~RestoreCandidateTests" --logger "trx;LogFileName=r3-candidates.trx" --results-directory "docs\architecture-migration\evidence\phase-7-project-restore-coordinator-relaunch\logs"`; expected exit 0 and named assertions for all DTO
  fields, built-in/custom/missing material,
  enum/range, unknown/null pipe and lambda semantics match R1. The receipt names
  each assertion and its expected semantic value; no candidate test may use
  runtime identity.

  **Failure/evidence/gate:** Any incomplete mapping or compatibility fallback is
  BLOCKED. Receipt `r3-candidates.md`; only R3 correction next. Forbidden:
  catalog mutation, calculation, ViewModel reference, serializer/wire changes.

- [ ] 5. R4. Implement ordered four-slice commit, rollback and guard contract

  **Goal:** Commit candidates once in a fixed order and guarantee DEC-003=C
  clean/default state after unexpected commit failure.

  **CHANGE-CLASS:** production/test. **Write-set:** Only
  `src/Services/Project/ProjectLoadOrchestrator.cs`, existing canonical state API
  files listed in `baseline/phase7-allow-list.json`, and named R2 commit/rollback/
  guard test files. No new production file may be added in R4 without plan
  revision. No calculation publication,
  catalog, report, UI, DI or architecture artifact edits.

  **Prerequisites/study:** R3 candidates with PASS receipt; `ProjectSession` and four slice
  interfaces; `BeginProjectRestore`; existing reset APIs and origins. Use the
  fixed order Climate -> Construction -> Thermal -> Hydraulics; this is not a
  worker-discovered choice.

  **Acceptance/QA:** Success proves exactly four ordered commits. For ordinary
  rollback, all four resets must succeed and all four slices must be clean/default.
  For a reset exception, the separate `PARTIAL_RESET_FAILURE` contract applies:
  attempt all four resets once in Climate -> Construction -> Thermal ->
  Hydraulics order, collect every exception, mark restore failed, do not claim
  clean/default for a slice whose reset threw, and release the guard in
  `finally`; record `PARTIAL_RESET_FAILURE` and stop the gate. No retry or
  previous-snapshot fallback is permitted. Run
  `dotnet test "tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Debug --filter "FullyQualifiedName~RestoreCommitOrderTests|FullyQualifiedName~RestoreRollbackTests|FullyQualifiedName~RestoreGuardLifecycleTests" --logger "trx;LogFileName=r4-atomic-restore.trx" --results-directory "docs\architecture-migration\evidence\phase-7-project-restore-coordinator-relaunch\logs"`; expected exit 0. Each of the
  four commit failures proves all four slices clean/default, derived/status
  state reset, path/dirty semantics, guard released. Rollback attempts all four
  resets even if one reset fails, aggregates diagnostics, never reports success,
  and releases guard. Nested restore lease disposes to `IsLoadProjectInProgress
  == false`; no second hydraulics restore. Re-run the exact R4 filter in Debug
  and Release using `r4-atomic-restore-debug.trx` and
  `r4-atomic-restore-release.trx`; both exit 0 and order/failure spies contain
  the expected four commit events and four reset attempts.

  **Failure/evidence/gate:** Mixed state, missing reset attempt or held guard is
  BLOCKED. Receipt `r4-atomic-restore.md`; only R4 correction next. Forbidden:
  restoring the previous arbitrary snapshot, calculation or UI refresh.

- [ ] 6. R5. Complete exactly-once calculation and ProjectSession publication

  **Goal:** Calculate once only after successful input commit and publish fresh
  derived/status values to `ProjectSession`, preserving DEC-001=A.

  **CHANGE-CLASS:** production/test. **Write-set:** Only
  `src/Services/Project/ProjectLoadOrchestrator.cs`, `src/Services/Project/ProjectSession.cs`
  if required by the existing four-slice API, and the named calculation/publication
  test files; no new coordinator type or production file. No catalog/report/UI/maps.

  **Prerequisites/study:** R4, `ThermalStateCoordinator`,
  `HydraulicsStateCoordinator`, `CalculationContext`, calculator contracts and
  four state snapshots. Verify no unapproved `CalculationContext` writer.

  **Acceptance/QA:** Run
  `dotnet test "tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Debug --filter "FullyQualifiedName~RestoreCalculationMultiplicityTests" --logger "trx;LogFileName=r5-calculation-publication.trx" --results-directory "docs\architecture-migration\evidence\phase-7-project-restore-coordinator-relaunch\logs"`; expected exit 0. Counting double proves one application-level calculation,
  after all four commits; fresh thermal/hydraulic derived values are published
  into session; stale calculated DTO fields are ignored; no second hydraulics
  restore and no second calculation. Calculation failure produces clean/default
  four-slice state, failure result, released guard and preserved UI boundary.
  Re-run the exact R5 filter in Release with
  `r5-calculation-publication-release.trx`; expected exit 0, counter exactly 1,
  publication after the four commit events, and no second hydraulics restore.

  **Failure/evidence/gate:** Any pre-commit calculation, duplicate call or stale
  result publication is BLOCKED. Receipt `r5-calculation-publication.md`; only
  R5 correction next. Forbidden: formula/algorithm redesign or Context removal.

- [ ] 7. R6. Prove project-open catalog non-mutation boundary

  **Goal:** Ensure real project-open consumes project-local persisted material
  snapshots and never mutates global materials/templates catalogs.

  **CHANGE-CLASS:** production/test. **Write-set:** Only
  `src/Services/Project/ProjectLoadOrchestrator.cs`,
  `src/Services/Project/ConstructionPersistenceMapper.cs`, the named
  catalog-boundary/user-flow test files,
  `tests/SnowMeltingCalculator.Tests/RestoreCatalogStructuralGuardTests.cs`,
  `tests/SnowMeltingCalculator.Tests/ProjectCatalogHashHarness.cs`,
  `logs/r6-catalog-boundary.trx`, and `r6-structural-scan.txt`; no catalog repository
  CRUD file, report/UI/maps/widget change, or new production file.

  **Prerequisites/study:** real `LoadProjectFromPathAsync` entrypoint,
  `ProjectLoadOrchestrator`, `IConstructionService`, material/template
  repositories, `data/materials_db.json`, template persistence location and R3
  material rules.

  **Acceptance/QA:** Run
  `dotnet test "tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Debug --filter "FullyQualifiedName~RestoreCatalogBoundaryTests|FullyQualifiedName~ProjectLoadUserFlowTests" --logger "trx;LogFileName=r6-catalog-boundary.trx" --results-directory "docs\architecture-migration\evidence\phase-7-project-restore-coordinator-relaunch\logs"`; expected exit 0. Real open with built-in and custom material records shows
  zero add/update/delete/import calls; `materials_db.json` and resolved template
  persistence file are byte/SHA-256 identical before/after. Missing material
  fails before canonical commit; duplicate/custom catalog conditions do not
  mutate files. Structural scan covers all restore paths for import/add/update/
  delete and no sync-over-async template call. Run the exact
  `RestoreCatalogStructuralGuardTests` filter from R2; it must exit 0 and report
  zero restore-path catalog mutation calls and zero sync-over-async calls in
  `r6-structural-scan.txt`. No broad text grep is an acceptance substitute.
  The command above exits 0
  and the named tests report zero mutation calls and identical hashes.

  **Failure/evidence/gate:** Any mutation counter or hash change is BLOCKED.
  Receipt `r6-catalog-boundary.md`; only R6 correction next. Forbidden: changing
  catalog repository CRUD semantics or adding compatibility import.

- [ ] 8. R6-A. Pass mandatory intermediate restore acceptance gate

  **Goal:** Independently review live write-set and R1-R6 receipts before report,
  UI, maps or widget work begins.

  **CHANGE-CLASS:** control/docs-only. **Write-set:** Create only
  `r6-a-restore-acceptance-gate.md`, `r6-a-write-set-check.txt`, and supporting
  read-only comparison output; no production/test/docs architecture edits.

  **Prerequisites/study:** R0-R6 receipts, live `git status --short`, diff/stat,
  allow-list, exact changed files, focused logs/TRX and current dossier status.

  **Acceptance/QA:** Every prior receipt exists, says PASS, matches live files,
  and proves validation-before-mutation, four ordered commits, all-four rollback,
  guard release, one calculation, fresh publication, no second hydraulics restore
  and catalog counter/hash zero. Full Release is explicitly not accepted as a
  substitute. Read the literal baseline commit from
  `docs\architecture-migration\evidence\phase-7-project-restore-coordinator-relaunch\baseline\head.txt`
  and run `git diff --name-only (Get-Content
  docs\architecture-migration\evidence\phase-7-project-restore-coordinator-relaunch\baseline\head.txt)`
  plus `node
  docs\architecture-migration\evidence\phase-7-project-restore-coordinator-relaunch\scripts\check-phase7-allow-list.mjs
  --baseline docs\architecture-migration\evidence\phase-7-project-restore-coordinator-relaunch\baseline\head.txt
  --allow-list docs\architecture-migration\evidence\phase-7-project-restore-coordinator-relaunch\baseline\phase7-allow-list.json`;
  record stdout as `r6-a-write-set-check.txt`; expected
  zero missing receipts, zero mismatches and exit 0.

  **Failure/evidence/gate:** Missing/contradictory receipt or dirty overlap is
  BLOCKED; only the named failed R0-R6 stage may be remediated. Receipt
  `r6-a-restore-acceptance-gate.md`. Forbidden: report/UI/maps/widget work.

- [ ] 9. R7. Build immutable ProjectSession-derived report snapshot and export path

  **Goal:** Make report input an immutable current session-derived snapshot and
  ensure export never recalculates or mutates session.

  **CHANGE-CLASS:** production/test,user-visible. **Write-set:** Only
  `src/Services/Reports/Calculation/CalculationReportDataBuilder.cs`,
  `src/Services/Reports/Calculation/CalculationReportExportService.cs`,
  `src/ViewModels/Results/ResultsViewModel.cs`, existing report model/interface
  files named by R0, and the named report test files; no restore commit/catalog/
  maps/widget changes.

  **Prerequisites/study:** R6-A PASS; `CalculationReportDataBuilder`, interface,
  six section builders, `CalculationReportExportService`, report models,
  `ResultsViewModel` export methods and existing Markdown/cancellation/error
  tests.

  **Acceptance/QA:** Run
  `dotnet test "tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Debug --filter "FullyQualifiedName~ReportSessionSnapshotTests|FullyQualifiedName~CalculationReportExportServiceTests" --logger "trx;LogFileName=r7-report-snapshot.trx" --results-directory "docs\architecture-migration\evidence\phase-7-project-restore-coordinator-relaunch\logs"`; expected exit 0. Snapshot contains current four slices, derived values and
  formula-relevant data; stale `ProjectData` calculated fields are not read.
  Spy receives the session snapshot; export call count remains unchanged and
  session snapshot before/after is equal. Cancellation/write failure preserves
  existing behavior. The R7 command exits 0; the stale-DTO assertion, deep
  immutability assertion, unchanged calculation counter and unchanged session
  snapshot are all PASS in `r7-report-snapshot.trx`.

  **Failure/evidence/gate:** Any DTO calculated-field source, recalc or mutation
  is BLOCKED. Receipt `r7-report-snapshot.md`; only R7 correction next. Forbidden:
  PDF/Excel/Preview/Print redesign or broad Results migration.

- [ ] 10. R8. Wire DI and UI adapters after successful restore/calculation only

  **Goal:** Prove application DI resolves the same real ViewModel-free restore
  shape tested in isolation, while UI projections remain adapters.

  **CHANGE-CLASS:** production/test,user-visible. **Write-set:** Only the exact
  existing DI registration file discovered in R0 and recorded in
  `baseline/phase7-allow-list.json`, `src/ViewModels/Results/ResultsViewModel.cs`,
  `src/Services/Project/ProjectLoadOrchestrator.cs`, and the named DI/UI test
  files; no new service type, maps/widget or serializer changes.

  **Prerequisites/study:** R6-A and R7 PASS; existing DI registrations,
  Results constructor/call paths, `LoadProjectDataAsync`, `RefreshAll`, path,
  dirty, cancellation and error boundary tests.

  **Acceptance/QA:** Run
  `dotnet test "tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Debug --filter "FullyQualifiedName~DiRegistrationTests|FullyQualifiedName~ProjectLoadUserFlowTests" --logger "trx;LogFileName=r8-ui-di.trx" --results-directory "docs\architecture-migration\evidence\phase-7-project-restore-coordinator-relaunch\logs"`; expected exit 0. Real DI resolves one `ProjectLoadOrchestrator` shape; no
  concrete ViewModel is injected into restore/calculation/report services; no
  fallback restore path exists. UI projections refresh only after successful
  canonical restore and calculation; failure reaches existing user-visible
  error boundary; guard releases on every failure. The R8 command exits 0 and
  its named tests prove the real DI graph, successful projection ordering,
  second-load replacement, dirty/path/cancel/error behavior and released guard.

  **Failure/evidence/gate:** DI mismatch, early refresh or altered user semantics
  is BLOCKED. Receipt `r8-ui-di.md`; only R8 correction next. Forbidden: broad
  ViewModel owner removal or unrelated UI redesign.

- [ ] 11. R9. Refresh six architecture maps, model/widget and user-flow evidence

  **Goal:** Reconcile architecture artifacts with the live accepted write-set,
  then prove the real user flow.

  **CHANGE-CLASS:** architecture artifacts,user-visible. **Write-set:** Only the
  six named map files listed in `baseline/phase7-allow-list.json`, supporting
  map files already listed there, active v2 `architecture-model.json`, active
  widget generator/validator inputs and canonical hyphen `architecture-widget.html`, plus new
  Phase 7 evidence and receipt. Never edit underscore historical widget, v1
  schema, baseline model, archive or old plan.

  **Prerequisites/study:** R8 PASS; active v2 schema/model, six maps, widget
  generator/model-contract/verify scripts, widget spec, Phase 6 accepted
  artifacts. Reconcile compile-time, DI/runtime, state ownership, reactive,
  persistence and user-flow views including report/catalog/guard edges.

  **Acceptance/QA:** Run `node
  docs\architecture-migration\widget\model-contract.mjs
  docs\architecture-migration\maps\architecture-model.json` and `node
  docs\architecture-migration\widget\generate-widget.mjs --check`; each exits
  0, and two generator passes have identical SHA-256. Run `dotnet build
  tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj -c Release
  --nologo`, then `dotnet test
  tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj -c Release
  --no-build --filter "FullyQualifiedName~ProjectLoadUserFlowTests" --logger
  "trx;LogFileName=r9-user-flow.trx" --results-directory
  docs\architecture-migration\evidence\phase-7-project-restore-coordinator-relaunch\logs`;
  record each new/open/second-open/edit/
  calculate/reset/save-reload/export and validation/calculation/cancellation
  failure assertion. No WPF flow is PASS without a screenshot or executable
  trace; unavailable automation is a limitation and blocks user-visible
  acceptance.

  **Failure/evidence/gate:** stale model, wrong widget filename, generated drift
  or missing user-flow proof is BLOCKED. Receipt `r9-artifacts-gates.md`; only R9
  correction next. Forbidden: archived artifacts, STATE.json or unrelated maps.

- [ ] 12. R10. Run F1-F4 verification and stop for owner result acceptance

  **Goal:** Independently verify conformance/provenance, architecture quality,
  executable QA/user risk and scope fidelity; never self-accept the result.

  **CHANGE-CLASS:** control/docs-only,user-visible. **Write-set:** New `evidence/phase-7-project-restore-coordinator-relaunch/final/`
  F1-F4 receipts, consolidated receipt and pending `owner-result-acceptance.md`;
  no product/test/map/widget edits during review.

  **Prerequisites/study:** R0-R9 PASS receipts and live diff. F1 compares every
  Must-NOT-Have and wire/schema/version rule; F2 checks canonical ownership,
  VM-free dependencies, DEC-001 writers, ordered/exactly-once structure and
  catalog path; F3 runs focused suites, Debug/Release builds, serial full
  Release, and the `ProjectLoadUserFlowTests` executable trace for
  current-format open/second-open/report/cancellation/failure flows; F4
  reconciles scope and residual risks.

  **Acceptance/QA:** F1 runs the R0-created allow-list checker with the literal
  baseline and allow-list paths plus `git diff --check`; expected zero forbidden
  paths and exit 0. F2 runs the exact R2, R5 and R6 test filters, the literal
  model-contract and widget-check commands, and PowerShell `Select-String`
  source guards over `src\Services\Project`, `src\Services\Reports`,
  `src\ViewModels`, and DI registration files for `ProjectSession`,
  `CalculationContext`, `BeginProjectRestore`, `ProjectLoadOrchestrator`,
  `Add|Update|Delete|Import`, and ViewModel constructor references; every
  forbidden-match count is zero and every positive ownership assertion is
  recorded in `final/f2-architecture.md`. F3 runs the literal Debug/Release
  builds, exact R2-R8 focused filters, serial full Release command and the
  exact R9 `ProjectLoadUserFlowTests` command; commands exit 0 except explicitly recorded
  expected-negative probes. F1/F2/F3 independently PASS as the three governance
  domains required by `AGENTS.md`; F4 is a mandatory scope/residual-risk
  reconciliation section in the consolidated receipt, not a fourth governance
  domain. All four records name exact write-set, commands, logs/TRX/hashes and
  residual risk. Serial full Release is authoritative; parallel AppData race is
  recorded, not fixed. `owner-result-acceptance.md` may contain only PENDING
  metadata until a separate owner action records acceptance or rejection.

  **Failure/evidence/gate:** Any failed or missing domain blocks acceptance and
  permits only correction of the identified evidence/implementation stage. No
  claim of Phase 7 completion without explicit owner result acceptance.
  Forbidden: editing old plan, STATE.json, archive, git operations or starting
  another phase.

## Final verification wave

- [ ] F1. Conformance and provenance audit

  Run `git diff --check`, `git status --short`, and `node docs\architecture-migration\evidence\phase-7-project-restore-coordinator-relaunch\scripts\check-phase7-allow-list.mjs --baseline docs\architecture-migration\evidence\phase-7-project-restore-coordinator-relaunch\baseline\head.txt --allow-list docs\architecture-migration\evidence\phase-7-project-restore-coordinator-relaunch\baseline\phase7-allow-list.json`;
  expected no forbidden path and exit 0. Compare SHA-256 of the old frozen plan
  and both archives; inspect current `.smc` DTO fields and `Version = "1.1"`;
  produce `final/f1-conformance.md` with PASS only when every comparison passes.

- [ ] F2. Architecture and code-quality audit

  Run the exact R2 GREEN, R5 and R6 filters from the Todos, plus the literal
  model-contract and widget-check commands; expected every assertion PASS and
  exit 0. Additionally run the literal guard command
  `$files = Get-ChildItem src\Services\Project,src\Services\Reports,src\ViewModels -Filter *.cs -Recurse; $forbidden = $files | Select-String -Pattern 'new\s+ResultsViewModel|ResultsViewModel\s+\w+|\.\b(Add|Update|Delete|Import)(Async)?\s*\('; $required = @('ProjectSession','ProjectLoadOrchestrator','BeginProjectRestore','CalculationContext'); $missing = @($required | Where-Object { -not ($files | Select-String -SimpleMatch $_) }); if ($forbidden -or $missing.Count -gt 0) { [pscustomobject]@{ FORBIDDEN_MATCHES = @($forbidden).Count; MISSING_REQUIRED_SYMBOLS = $missing.Count } | Set-Content docs\architecture-migration\evidence\phase-7-project-restore-coordinator-relaunch\final-f2-source-guards.txt; exit 1 } else { 'FORBIDDEN_MATCHES=0'; 'MISSING_REQUIRED_SYMBOLS=0' | Set-Content docs\architecture-migration\evidence\phase-7-project-restore-coordinator-relaunch\final-f2-source-guards.txt; exit 0 }` and record its exact output. Inspect DI descriptors and
  restore/report dependency graph for `ProjectSession`/four-slice ownership,
  ViewModel-free services, DEC-001 writers, deterministic commit, exactly-once
  calculation, no second hydraulics restore and no catalog mutation; produce
  `final/f2-architecture.md`.

- [ ] F3. Executable QA and user-risk audit

  Run the literal focused filters recorded in R2-R8, both build commands, and
  `dotnet test "tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Release --no-build --logger "trx;LogFileName=full-release.trx" --results-directory "docs\architecture-migration\evidence\phase-7-project-restore-coordinator-relaunch\logs"`; expected exit 0 for positive suites and exactly the documented exit for
  expected-negative RED probes. Exercise the five exact `ProjectLoadUserFlowTests`
  methods named in R2 for current-format open/second-open/
  save-reload/report export, invalid input, four commit failures, calculation
  failure, cancellation, dirty/path/error and nested guard flows; perform
  executable user-flow trace with those exact method names and TRX evidence; produce
  `final/f3-executable-qa.md`.

- [ ] F4. Scope fidelity and residual-risk audit

  Confirm exclusions (legacy beyond DEC-002, PB-002, broad Results, redesigns,
  AppData cleanup, serializer/formula changes) and document all limitations,
  including parallel settings race and any LSP cwd rejection; append this
  section to `final/f4-consolidated-receipt.md`, which is the single consolidated
  receipt naming F1/F2/F3 domain verdicts. F4 has no separate governance-domain
  verdict. Run `node docs\architecture-migration\evidence\phase-7-project-restore-coordinator-relaunch\scripts\check-phase7-allow-list.mjs --baseline docs\architecture-migration\evidence\phase-7-project-restore-coordinator-relaunch\baseline\head.txt --allow-list docs\architecture-migration\evidence\phase-7-project-restore-coordinator-relaunch\baseline\phase7-allow-list.json` plus `git diff --check`; expected zero out-of-scope paths.
  expected zero out-of-scope paths. Record the parallel AppData race and LSP
  cwd limitation as residual risk when applicable.

## Commit strategy

The worker performs no Git mutation under any authorization. The worker may use
only read-only `git diff`, `git status`, `git rev-parse`, and `git diff --check`
for evidence; never stage, commit, reset, revert, clean or rewrite unrelated
paths. Keep R1/R2 characterization distinguishable from production
stages. Do not stage, commit, reset, revert, clean or rewrite unrelated dirty
paths. Receipts are created immediately after each stage's focused verification;
green compilation or broad tests never substitute for a receipt.

## Success criteria

- Current-format `.smc` persisted inputs restore into the four canonical slices
  through one ViewModel-free application boundary.
- Full validation precedes mutation; commit order is deterministic; every commit
  and calculation failure yields clean/default all-four state and released guard.
- Exactly one application calculation publishes fresh derived values to session;
  stale calculated DTO values are never current canonical results; no second
  hydraulics restore occurs.
- Global materials/templates repositories and persistence files remain unchanged
  on real project open; custom materials remain project-local.
- Reports consume an immutable current `ProjectSession` snapshot and export
  without recalculation or session mutation.
- Existing user-visible path, dirty, guard, cancellation and error behavior is
  preserved and proven through the real entrypoint.
- R0-R10 receipts, active six maps/model/widget evidence and independent F1-F4
  receipts all PASS; then and only then the worker stops for explicit owner
  result acceptance.
