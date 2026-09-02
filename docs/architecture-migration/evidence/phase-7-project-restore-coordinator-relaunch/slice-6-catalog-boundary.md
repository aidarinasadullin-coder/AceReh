# Slice 6 — Catalog Boundary on Project Open (PASS)

**Date:** 2026-08-31
**Plan:** `docs/architecture-migration/plans/phase-7-project-restore-coordinator-relaunch.md` (frozen, NOT edited)
**Lane:** continuation of Slice 6 / Todo 6 (same execution lane)

## Exact Commands

```powershell
dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo
dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~ProjectSaveServiceTests|FullyQualifiedName~ProjectPersistenceMapperTests|FullyQualifiedName~ProjectFileServiceResultTests|FullyQualifiedName~ResultsViewModelOpenProjectTests" --logger "trx;LogFileName=slice-6-catalog-boundary.trx" --results-directory "docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/logs"
dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~LoadProjectData_MissingOrInvalidThermalResult_UsesLoadOnlyFallbackAndRefreshDoesNotRecalculate|FullyQualifiedName~ProjectRoundTrip_PipeSelectionRestored|FullyQualifiedName~RestoreModulesFromProjectAsync_InvalidThermalInput_DoesNotMutatePriorClimateOrThermalSlices|FullyQualifiedName~OpenProject_WithCustomCatalogRecords_LeavesGlobalCatalogReadOnly|FullyQualifiedName~OpenProject_WithInvalidCustomCatalogRecords_DoesNotMutateGlobalCatalog" --logger "trx;LogFileName=slice-6-regression-check.trx" --results-directory "docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/logs"
```

## Build Result

- `dotnet build ... -c Debug --nologo` → **Build succeeded. Warnings: 0, Errors: 0**

## Test Result (exact focused filter)

- **Passed: 57, Failed: 0, Skipped: 1, Total: 58**
- TRX: `docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/logs/slice-6-catalog-boundary.trx`
- Matched classes: `ProjectSaveServiceTests`, `ProjectPersistenceMapperTests`, `ProjectFileServiceResultTests`, `ResultsViewModelOpenProjectTests`.
- One test was skipped by the harness fixture gate: `ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`.

## Regression Check (open-project / restore preservation)

Additional exact verification run:

- `LoadProjectData_MissingOrInvalidThermalResult_UsesLoadOnlyFallbackAndRefreshDoesNotRecalculate` → **Passed**
- `ProjectRoundTrip_PipeSelectionRestored` → **Passed**
- `RestoreModulesFromProjectAsync_InvalidThermalInput_DoesNotMutatePriorClimateOrThermalSlices` → **Passed**
- `OpenProject_WithCustomCatalogRecords_LeavesGlobalCatalogReadOnly` → **Passed**
- `OpenProject_WithInvalidCustomCatalogRecords_DoesNotMutateGlobalCatalog` → **Passed**

## Contracts Verified

### Open-project flow still restores thermal UI state

The project-open path continues to restore the thermal inputs/result path used by existing open-project tests:

- `LoadProjectData_MissingOrInvalidThermalResult_UsesLoadOnlyFallbackAndRefreshDoesNotRecalculate` keeps the expected `TotalPowerDensity == 333`, `SupplyTemperature == 55`, and non-recalculating refresh semantics.
- `ProjectRoundTrip_PipeSelectionRestored` keeps the expected pipe selection restored in the thermal ViewModel.

### Catalogs remain read-only on open

The open-project tests verify the read-only catalog boundary by asserting that:

- `ImportProjectMaterialsAsync(...)` is never called;
- `ImportProjectTemplatesAsync(...)` is never called;
- `ImportMissingMaterialAsync(...)` is never called.

This preserves the no-import behavior while allowing project-local custom records to remain in the loaded DTO.

### Invalid thermal restore still does not mutate prior slices

`RestoreModulesFromProjectAsync_InvalidThermalInput_DoesNotMutatePriorClimateOrThermalSlices` passed, confirming the narrowed thermal preflight still blocks the invalid explicit-out-of-range candidate without mutating earlier canonical slices.

## Changed Paths

- `src/Services/Project/ProjectLoadOrchestrator.cs`
- `tests/SnowMeltingCalculator.Tests/ViewModels/ResultsViewModelOpenProjectTests.cs`
- `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectLifecycleFlowCharacterizationTests.cs`
- `docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/slice-6-catalog-boundary.md`
- `docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/logs/slice-6-catalog-boundary.trx`

## Residual Risks

- **LSP unavailable:** `lsp_diagnostics` repeatedly failed with cwd/path resolution errors in this harness (`LSP file path must be inside request cwd` / `Working directory does not exist`). The authoritative compile gate was the successful `dotnet build` above.
- **Skipped fixture-based smoke test:** one test in the exact focused filter is skipped by a known fixture gate, so the evidence here relies on the rest of the exact filter plus the separate regression-check run.
- **No catalog mutation path introduced:** no production CRUD/import path was added; the receipt relies on the verified `Times.Never` assertions in the open-project tests.

## Notes

- The open-project catalog boundary remains project-local: custom materials/templates stay in `ProjectData` and are not imported into global catalogs during open.
- The thermal preflight was narrowed so the legacy open-project shape continues to restore user-facing thermal state without breaking the invalid-thermal characterization.
