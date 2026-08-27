# Phase 6 Task 2 Characterization

Date: 2026-08-25
Repository: `D:\IA\3ace v.2`
Plan: `phase-6-project-snapshot-save-boundary`
Plan SHA-256: `C56E66D2733D65CC56190A7B95B4D87F5F032AA75E83339925CEB23C2E5A4E92`

## Gate and Scope

Task 1 baseline is green with the documented fixture-hash follow-up. Task 2
therefore characterizes the existing save boundary before any production
snapshot, mapper, save-service, or DI implementation changes.

The observed save path remains:

`ResultsViewModel.SaveProject` -> `SaveToFile` -> `SaveCurrentProject` ->
`IProjectFileService.SaveProjectResultAsync` -> `.smc`

The only source test path changed for this task is the already-dirty
`tests/SnowMeltingCalculator.Tests/ViewModels/ResultsViewModelOpenProjectTests.cs`.
The pre-existing constructor-wiring changes in that file are protected baseline
content; the Task 2 additions are the two tests named below. No production file,
restore path, serializer, calculation, export, or Markdown behavior was changed.

## Characterization Matrix

| Requirement | Existing coverage | Task 2 assertion |
|---|---|---|
| New/populated project and all four module projections | `ProjectRoundTripTests`; `ResultsViewModelOpenProjectTests.ProjectRoundTrip_LiveMutationsAreSavedLoadedAndExportedWithoutResultsCalculation` | Existing values, custom construction material, thermal result, and hydraulic collector/circuit data remain asserted |
| Custom materials/templates | `ProjectLifecycleFlowCharacterizationTests` restore failure/lifecycle cases and existing Results load coverage | Persistence inputs remain in the existing load/save graph; no new production source is introduced in Task 2 |
| Saved thermal results | `SaveCurrentProject_PersistsThermalStateSnapshot_NotThermalViewModelMirror`; `LoadProjectData_KpiReflectSavedThermalResult_WithoutCityReselection`; `LoadProjectData_SecondLoadWithoutSavedResult_ReplacesAllThermalStaleValues` | Canonical saved result and no-calculation behavior remain locked |
| Two-collector summaries | `ProjectRoundTrip_TwoCollectors_PreservesPerCollectorSummaries`; `ResultsViewModel_LoadProject_TwoCollectors_RestoresIndependentSummaryCards` | Independent collector summary values remain asserted |
| Second load | `ProjectRoundTrip_FieldCompleteRoundTrip_SecondLoadReplacesProjectA`; `LoadProjectData_SecondLoadWithoutSavedResult_ReplacesAllThermalStaleValues` | Stale project state replacement and clean load semantics remain asserted |
| Save success | New `SaveProject_Success_StampsDatesAndClearsDirtyOnce` | Exactly one `SaveProjectResultAsync` call; `CreatedDate` and `ModifiedDate` are stamped; exactly one dirty-to-clean transition; no error dialog |
| Save failure | New `SaveProject_Failure_PreservesDirtyStateAndShowsError` | Exactly one save call; dirty state remains true; no clean transition; existing error dialog is shown once |
| File service failure/atomicity | `ProjectFileServiceResultTests`; `ProjectFileServiceAtomicityTests`; `ProjectFileServiceMutationTests` | Existing result, `.bak`, `.tmp`, extension and date-preservation semantics remain green |
| Legacy and wire compatibility | `ProjectRoundTripTests`; `ProjectFileService_RoundTripPreservesSchemaVersionAndJsonShape` | Existing `Version`, DTO property names, enum and null-shape assertions remain green |

## New Assertions

### Successful save

`SaveProject_Success_StampsDatesAndClearsDirtyOnce` starts from a dirty project
with an existing path and an injected successful `SaveProjectResultAsync`.
It captures the DTO sent to the file service and observes `IProjectStateService`
property changes. The test proves:

- one save call for the current path;
- `CreatedDate` is assigned during the first save;
- `ModifiedDate` is assigned during the save;
- the final state is clean;
- exactly one `IsDirty: true -> false` transition occurs; and
- no save error is shown.

### Failed save

`SaveProject_Failure_PreservesDirtyStateAndShowsError` injects a failed
`OperationResult<object?>` from `SaveProjectResultAsync`. It proves:

- one save call for the current path;
- dirty state remains true;
- no dirty-to-clean transition occurs; and
- the existing localized save error is shown exactly once.

## Commands and Results

All commands were run from `D:\IA\3ace v.2`; `--no-build` uses the already
successful Debug/Release project builds recorded in the baseline and prior
Task 2 execution.

| Command | Exit/result |
|---|---|
| `dotnet test --configuration Debug --no-build --filter "FullyQualifiedName~ResultsViewModelOpenProjectTests|FullyQualifiedName~ProjectRoundTripTests|FullyQualifiedName~ProjectFileService" --logger "console;verbosity=normal"` | `0`; 64 discovered/total, 63 passed, 1 skipped, 0 failed |
| `dotnet test --configuration Release --no-build --filter "FullyQualifiedName~ResultsViewModelOpenProjectTests|FullyQualifiedName~ProjectRoundTripTests|FullyQualifiedName~ProjectFileService" --logger "console;verbosity=normal"` | `0`; 64 discovered/total, 63 passed, 1 skipped, 0 failed |
| `dotnet build src\SnowMeltingCalculator.csproj -c Debug --nologo` | `0`; 0 warnings, 0 errors, recorded in Task 1 baseline |

The targeted suite contains 64 test cases: 39 from
`ResultsViewModelOpenProjectTests`, 12 from `ProjectRoundTripTests`, and 13
from the `ProjectFileService`-matching tests. Both configurations produced the
same result.

## Accepted Skip

The single skipped test is:

`ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`

It remains skipped because the existing external smoke fixture
`D:\IA\ace\Тест\тест 40.smc` is absent. The test itself was not weakened or
changed by Task 2. This skip is retained as a skip, not counted as a pass; the
complete tracked `.smc` corpus still requires the byte-safe re-enumeration
specified for Task 6.

## Task 2 Decision

`CHARACTERIZATION: PASS`

Current save success/failure behavior, dirty multiplicity, date stamping,
canonical module save projections, round-trip coverage, file-service failure
semantics, and wire-shape guards are green in both Debug and Release targeted
runs. Production boundary extraction is released to the next sequential task.

LSP was attempted once for the supported C# paths but the repository request
was rejected by the environment with `LSP file path must be inside request cwd`.
Compiler and executable test evidence above is the applicable replacement; no
Markdown LSP result is claimed.
