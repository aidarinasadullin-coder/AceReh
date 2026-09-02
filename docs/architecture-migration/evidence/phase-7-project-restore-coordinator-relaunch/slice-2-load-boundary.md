# Phase 7 Slice 2: Load Boundary

Status: PASS
Date: 2026-08-31

## Boundary Evidence

- `src/Services/Project/IProjectFileService.cs` exposes `LoadProjectResultAsync` as the typed file-load boundary returning `OperationResult<ProjectData>`.
- `src/Services/Project/ProjectFileService.cs` returns failure for missing files, null deserialization, and read/deserialization exceptions without invoking project restore or mutating session state.
- `src/ViewModels/Results/ResultsViewModel.cs`, `LoadProjectFromPathAsync`, keeps the existing UI error boundary: failed or null-valued results call `_dialogService.ShowError(...)` and return before `ApplyLoadedProjectAsync`.
- Successful path is `LoadProjectFromPathAsync` -> `ApplyLoadedProjectAsync` -> `LoadProjectDataAsync`; the latter owns the same `BeginProjectRestore` and `ProjectLoadOrchestrator.RestoreModulesFromProjectAsync` path proven in Slice 1.
- Dirty-project confirmation remains in `ApplyLoadedProjectAsync`, after file parsing has succeeded and before project mutation.

## Characterization Coverage

Existing tests were sufficient; no production or test source was changed.

- `ProjectFileServiceResultTests`: missing-file failure, corrupt JSON failure, successful typed load/save, and `.smc` round-trip preservation.
- `ProjectFileServiceMutationTests`: load/save do not mutate `ModifiedDate`.
- `ResultsViewModelOpenProjectTests`: clean and dirty open flows, dirty-user-decline path, successful load handoff, and restored project data.

## Executed Gates

Build was completed before the focused `--no-build` test command:

```text
dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo
Result: PASS, 0 warnings, 0 errors
```

```text
dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~ProjectFileServiceResultTests|FullyQualifiedName~ProjectFileServiceMutationTests|FullyQualifiedName~ResultsViewModelOpenProjectTests" --logger "trx;LogFileName=slice-2-load-boundary.trx" --results-directory "docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/logs"
Result: PASS, 46 passed, 0 failed, 1 skipped, 47 total
```

The one skipped test is the pre-existing accepted baseline test `ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`; it was not caused by this slice and is retained unchanged.

TRX: `docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/logs/slice-2-load-boundary.trx`

## Gate Decision

Slice 2 is PASS. Valid file data reaches the established restore boundary; missing, invalid, or deserialization-failed input stops at the existing dialog boundary before restore mutation. No scope expansion or production change was required.
