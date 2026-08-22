# Phase 3 Task 9 - Persistence and Standalone Files

**Date:** 2026-08-14
**Phase:** phase-3-construction-state
**Task:** 9. Persistence and standalone files: Read/write canonical snapshots without wire changes - expect semantic round-trip compatibility
**Status:** ACCEPTED

## Summary

Project `.smc` save now reads Construction data from the canonical
`ProjectSession.ConstructionState.Snapshot` via a pure DTO mapper
(`ConstructionPersistenceMapper`), not from the writable
`ConstructionViewModel` cache. The `.smc` wire format (DTO fields, schema,
version) is unchanged. Standalone Construction JSON load/save remains
compatible without code changes because its repository wire format is
independent of the project save path.

## Changed Files

1. `src/Services/Project/ConstructionPersistenceMapper.cs` (NEW)
   - Pure mapper: `ConstructionStateSnapshot` -> `ConstructionProjectData`.
   - Derives R1/R2/LambdaE through `ConstructionStateProjection` (no formula
     duplication). Resolves `MaterialLambda` from `IMaterialRepository` by
     `MaterialId` to preserve the denormalized value written by the legacy
     save path. No wire schema/version change.

2. `src/ViewModels/Results/ResultsViewModel.cs` (MODIFIED)
   - `SaveCurrentProject()`: replaced inline `_constructionViewModel` read
     block with `ConstructionPersistenceMapper.ToProjectData(
     _projectSession.ConstructionState.Snapshot, _materialRepository)`.
   - No other method changed; no DTO/schema/version change.

3. `tests/SnowMeltingCalculator.Tests/ViewModels/ResultsViewModelTestHelpers.cs` (MODIFIED)
   - `CreateResultsViewModel`: `ConstructionViewModel` now wired to
     `projectStateService.Session.ConstructionState`.
   - `CreateConstructionViewModel()`: added overload
     `CreateConstructionViewModel(IProjectSession?)` that forwards
     `projectSession?.ConstructionState` to the VM constructor.

4. `tests/SnowMeltingCalculator.Tests/ViewModels/ResultsViewModelOpenProjectTests.cs` (MODIFIED)
   - All `CreateConstructionViewModel()` call sites updated to pass
     `_projectStateService.Session` so VM and session share one
     `ConstructionState`.
   - `CreateConstructionViewModel(CalculationStateService, IMarkDirtyService)`:
     added optional `IProjectSession?` parameter; call sites updated.
   - `CreateInitializedConstructionViewModelAsync`: changed from `static` to
     instance; passes `_projectStateService.Session`.
   - Both `CreateViewModel` overloads: `ProjectLoadOrchestrator` now receives
     `_projectStateService.Session`.
   - NEW test: `SaveCurrentProject_PersistsConstructionStateSnapshot_NotConstructionViewModelMirror`
     - Constructs a canonical Construction snapshot different from VM mirror,
       calls `SaveCurrentProject()`, asserts saved `ConstructionData` uses
       canonical snapshot values (groundwater, HasLoads, layer
       material/thickness/lambda/override/order).
   - NEW test: `ProjectRoundTrip_ConstructionSaveFromCanonicalState_RoundTripsThroughJson`
     - Mutates VM (shadow-writes to canonical state), saves, serializes
       through JSON, deserializes, asserts semantic round-trip of version,
       groundwater, layer thickness/lambda/override.

## .smc Wire Compatibility Statement

No `ProjectData`, `ConstructionProjectData`, or `LayerProjectData` schema
fields were added, removed, renamed, or retyped. `ProjectData.Version`
remains `"1.1"`. The mapper produces the same DTO shape as the legacy inline
code: `R1`, `R2`, `LambdaE`, `GroundwaterLevel`, `HasLoads`, and `Layers`
with `Position`, `MaterialName`, `MaterialLambda`, `Thickness`,
`CalculatedLambda`, `IsLambdaOverridden`, `Order`. `CalculatedR` is left at
its default (as in the legacy code, which never set it either).

## Standalone Construction JSON Compatibility

Standalone Construction JSON load/save (`ConstructionRepository.SaveConstructionAsync`
/ `LoadConstructionAsync`) operates on `ConstructionModel` (the writable
model), not on `ProjectSession.ConstructionState`. Its wire format (Version
`1.1`, `layers_above_pipe`, `layers_below_pipe`, `material_snapshots`,
v1.0 above-order conversion) is independent of the project `.smc` save path.
No standalone code change is required for Task 9. Existing
`ConstructionRepositoryTests` cover standalone load/save/round-trip
compatibility and passed in the targeted suite.

## Gates

### Targeted Tests

Command:
```
dotnet test tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~ResultsViewModelOpenProjectTests|FullyQualifiedName~ProjectRoundTripTests|FullyQualifiedName~ConstructionRepositoryTests|FullyQualifiedName~ConstructionServiceTests|FullyQualifiedName~ConstructionViewModelTests|FullyQualifiedName~ProjectLifecycleFlowCharacterizationTests" --logger "trx;LogFileName=phase-3-task-9-targeted.trx"
```

Result: **PASSED**
- Total: 126
- Passed: 125
- Failed: 0
- Skipped: 1 (pre-existing `ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`)

### Debug Build

Command:
```
dotnet build src\SnowMeltingCalculator.csproj -c Debug --nologo
```

Result: **PASSED**
- Errors: 0
- Warnings: 4 (pre-existing CS8601 nullable warnings in `ConstructionViewModel.cs` lines 1389/1409, not in Task 9 changed files)
- Duration: 00:00:05.51

### LSP Diagnostics

Attempted `lsp_diagnostics` on:
- `D:\IA\ace v.2\src\Services\Project\ConstructionPersistenceMapper.cs`
- `D:\IA\ace v.2\src\ViewModels\Results\ResultsViewModel.cs`

Result: **FAILED** - known harness cwd/root mismatch.
Error: `LSP file path must be inside request cwd: D:\IA\ace v.2\...`

This is the same known issue recorded in prior Phase 2/3 evidence: the
harness picks workspace root `C:\Users\Admin` instead of `D:\IA\ace v.2`,
so C# correctness gates rely on `dotnet build` and `dotnet test` which both
passed.

## Test Coverage for Task 9 Contract

- `SaveCurrentProject_PersistsConstructionStateSnapshot_NotConstructionViewModelMirror`:
  proves save reads canonical `ProjectSession.ConstructionState.Snapshot`,
  not stale VM cache. Would fail if `SaveCurrentProject()` reverted to
  reading `_constructionViewModel` values.
- `ProjectRoundTrip_ConstructionSaveFromCanonicalState_RoundTripsThroughJson`:
  proves semantic round-trip: VM mutation -> canonical state -> save ->
  JSON serialize/deserialize -> assert values preserved.
- `ProjectRoundTrip_PreservesGroundwaterLevel` / `_PreservesHasLoads` /
  `_PreservesLambdaValueButResetsOverrideFlag` /
  `_LambdaUpdatesWhenGroundwaterLevelChanges_AfterOverride` /
  `_LambdaUpdatesWhenGroundwaterLevelChanges`: existing round-trip tests,
  now wired through canonical state, all pass.
- `ProjectRoundTrip_LiveMutationsAreSavedLoadedAndExportedWithoutResultsCalculation`:
  end-to-end save/load/export with live VM mutations, all pass.
- `ConstructionRepositoryTests`: standalone JSON load/save/round-trip
  compatibility, all pass.