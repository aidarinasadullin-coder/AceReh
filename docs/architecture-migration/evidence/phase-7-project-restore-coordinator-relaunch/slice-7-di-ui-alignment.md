# Slice 7 - DI/UI Alignment (PASS)

**Date:** 2026-09-01
**Plan:** `docs/architecture-migration/plans/phase-7-project-restore-coordinator-relaunch.md` (frozen, NOT edited)
**Todo:** 7 - keep DI/UI adapters aligned with the live restore path, with refresh only after successful restore

## Exact Commands

```powershell
dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo
dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~DiRegistrationTests|FullyQualifiedName~ResultsViewModelOpenProjectTests|FullyQualifiedName~ProjectSessionTests" --logger "trx;LogFileName=slice-7-di-ui-alignment.trx" --results-directory "docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/logs"
```

## Build Result

- Build succeeded.
- Warnings: **0**.
- Errors: **0**.

## Test Result

- Passed: **94**.
- Failed: **0**.
- Skipped: **1**.
- Total: **95**.
- TRX: `docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/logs/slice-7-di-ui-alignment.trx`
- The skipped test is the known fixture-gated `ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`.

## Contracts Verified

### DI restore boundary

`DiRegistrationTests.ResultsViewModel_RestorePath_UsesTheSingletonOrchestratorWithCanonicalSessionSlices` verifies exactly one `ProjectLoadOrchestrator` registration, the DI-wired `ResultsViewModel` uses that singleton, and the orchestrator fields reference the same session-owned Climate, Construction, Thermal, and Hydraulics slices.

### Rejected restore lifecycle

`LoadProjectData_SecondInvalidProjectPreservesPriorUiAndReleasesRestoreGuard` loads valid project A, rejects invalid thermal project B, and verifies A's KPI values remain (`TotalPowerDensity == 100`, `SupplyTemperature == 45`), `ProjectChanged` remains exactly one, `Session.IsLoadProjectInProgress == false`, and dirty remains false.

### Fresh UI/report handoff

`LoadProjectData_InvalidSavedResultPublishesFreshUiAndPdfValuesOnce` supplies a stale invalid `PowerTotal == 999999`, injects a calculator returning `PowerTotal == 333` and `SupplyTemperature == 55`, and verifies both Results UI and `ResultsPdfDataBuilder.Build(viewModel)` expose the fresh values. The calculator invocation count is exactly one.

## Architecture-Map Rationale

The affected DI-runtime, state-ownership, reactive, persistence, and user-flow views remain aligned with the existing canonical model: `ProjectSession` owns the four slices, `ProjectLoadOrchestrator` is the single restore boundary, and `ResultsViewModel.RefreshAll()` runs only after a successful restore. No production map/widget refresh is required because this slice adds executable coverage for already-established wiring and does not change production architecture or ownership.

## Changed Paths

- `tests/SnowMeltingCalculator.Tests/Configuration/DiRegistrationTests.cs` (existing Slice 7 DI assertion preserved and compiled)
- `tests/SnowMeltingCalculator.Tests/ViewModels/ResultsViewModelOpenProjectTests.cs` (two narrow UI/report restore assertions added)
- `docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/logs/slice-7-di-ui-alignment.trx`
- `docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/slice-7-di-ui-alignment.md`
- `.omo/notepads/phase-7-project-restore-coordinator-relaunch/learnings.md`

## LSP Limitation

`lsp_diagnostics` was attempted once for each modified C# file and failed with the harness error `LSP file path must be inside request cwd`. Per repository guidance, compiler and test gates are authoritative for this limitation.

## Scope

The frozen plan was not edited. No production code, dependencies, `.smc` files, catalog mutation path, second restore service, calculation pass, commit, staging, reset, revert, or clean operation was introduced.
