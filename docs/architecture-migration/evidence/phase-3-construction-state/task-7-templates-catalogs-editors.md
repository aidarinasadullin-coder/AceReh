# Phase 3 Task 7: templates, catalogs and editors

Date: 2026-08-14

## Root cause and red test

`ConstructionViewModel.ApplyTemplateCore` mutated the VM layer collections and
`HasLoads` first, then shadow-wrote their final values through
`SyncStateFromCollections(ConstructionMutationOrigin.SystemApply)`. Existing
value tests did not distinguish that completion from the required DEC-C04
`Template` completion.

`ApplyTemplate_Success_EmitsExactlyOneCanonicalTemplateCompletion` uses a real
`ProjectSessionConstructionState`, subscribes to `Changed`, and applies a
resolvable template through the generated async command. Before the production
fix it failed with expected `Template`, actual `SystemApply`.

## Fix

`ApplyTemplateCore` now completes external template resolution first, creates a
full `ConstructionStateSnapshot` from the prepared `ConstructionModel` while
preserving layer IDs and current project groundwater, and calls
`ApplySnapshot(candidate, ConstructionMutationOrigin.Template)` once. It then
updates the existing VM adapter collections and properties under `_isSyncing`,
without broadening `OnConstructionStateChanged` or emitting collection
shadow-writes.

The successful-template dirty characterization changed from 6 to the measured
3 calls: one canonical `Template` completion, the retained legacy `HasLoads`
completion, and the retained final legacy completion. Task 10 remains the owner
of downstream dirty/context cleanup.

Missing-material preview, cancel and failure paths still prepare or fail before
the canonical apply and therefore do not mutate canonical state. Catalog import
remains an external effect and is not rolled back if a later retry fails.

## Changed files

- `src/ViewModels/Construction/ConstructionViewModel.cs`
- `tests/SnowMeltingCalculator.Tests/Construction/ConstructionViewModelTests.cs`
- `tests/SnowMeltingCalculator.Tests/Construction/ConstructionMultiplicityCharacterizationTests.cs`
- `docs/architecture-migration/evidence/phase-3-construction-state/task-7-templates-catalogs-editors.md`

## Verification

- Focused red run before fix: exit `1`; 1 failed; actual origin `SystemApply`.
- Focused run after fix: exit `0`; 1 passed, 0 failed, 0 skipped.
- Required targeted Task 7 suite: exit `0`; 80 passed, 0 failed, 0 skipped.
- `dotnet build src\SnowMeltingCalculator.csproj -c Debug --nologo`: exit `0`;
  0 warnings, 0 errors.
- C# LSP diagnostics were unavailable because the harness incorrectly resolved
  paths against `C:\Users\Admin`; `dotnet test` and `dotnet build` are the
  executable correctness gates.
