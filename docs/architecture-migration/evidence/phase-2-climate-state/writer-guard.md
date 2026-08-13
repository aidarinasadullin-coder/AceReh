# Phase 2 Task 2: Climate Legacy Writer Guard

Date: 2026-08-06

## Scope

This receipt captures the current legacy Climate writable surface before any
ownership migration. No production source under `src/` was changed. The guard
is intentionally a source-text inventory: it asserts exact known legacy
writers, so an unexpected direct setter fails until the inventory and migration
decision are deliberately updated.

## Guard Artifact

`tests/SnowMeltingCalculator.Tests/Climate/ClimateStateLegacyStoreGuardTests.cs`

The guard contains two tests with `ClimateStateLegacyStoreGuard` in their names:

1. Captures the exact current writer and projection inventory.
2. Uses source fixtures for forbidden direct setters in `ResultsViewModel` and
   `ProjectLoadOrchestrator`; the detector reports those setters, proving that a
   new direct write would fail the projection-only assertion.

## Exact Current Inventory

| Location | Classification | Exact current surface |
|---|---|---|
| `src/ViewModels/Climate/ClimateViewModel.cs` | Legacy writable UI store | Observable backing fields: `_selectedCity`, `_airTemperature`, `_coldFiveDayTemperature`, `_windSpeed`, `_humidity`, `_snowfallIntensity`, `_selectedZone`, `_isHighRequirements`, `_hasUserModifications`. Mutation boundaries: `Reset`, `ResetToCityData`, `SetClimateParameters`, `OnSelectedCityChanged`, `OnIsHighRequirementsChanged`, scalar partial handlers, and `SyncToClimateData`. |
| `ClimateViewModel.Reset()` and `ProjectLoadOrchestrator.ResetModules()` | Reset writer path | `Reset()` assigns Climate VM values and calls `SyncToClimateData`; `ResetModules()` calls `_climateViewModel.Reset()`. |
| `ClimateViewModel.SyncToClimateData()` | Legacy projection writer | Direct concrete `ClimateData` assignments in exact execution order: `SelectedCity`, `SelectedRegion`, `AirTemperature`, `WindSpeed`, `Humidity`, `SnowfallIntensity`, `Zone`, `ColdFiveDayTemperature`; then `RaiseDataChanged` and `CalculationContext.UpdateClimate`. |
| `src/Models/Climate/ClimateData.cs` | Concrete writable compatibility store | Exact public settable properties: `SelectedCity`, `SelectedRegion`, `AirTemperature`, `ColdFiveDayTemperature`, `WindSpeed`, `Humidity`, `SnowfallIntensity`, `Zone`. |
| `src/Core/CalculationContext.cs` | Downstream update seam | `UpdateClimate(IClimateData climate, string source = "Climate")` assigns `Climate = climate` and invokes `OnContextChanged`. |
| `src/Services/Project/ProjectLoadOrchestrator.cs` | Restore bypass writer | `RestoreModulesFromProjectAsync()` writes `_climateViewModel.SelectedCity`, `SearchQuery`, `AirTemperature`, `WindSpeed`, `Humidity`, `SnowfallIntensity`, `SelectedZone`, `IsHighRequirements`, then `SelectedCity` and `HasUserModifications`; its `finally` calls `SyncToClimateData`. |
| `src/ViewModels/Results/ResultsViewModel.cs` | Projection/read site, not writer | `SaveCurrentProject()` reads Climate VM properties into `ClimateProjectData`: selected city/region, air temperature, wind speed, humidity, snowfall intensity, and selected zone. The guard asserts zero direct `_climateViewModel.<property> =` writes. |

## Commands and Results

```text
$env:GIT_MASTER='1'; git rev-parse --show-toplevel
D:/IA/ace v.2

dotnet test tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj --filter "FullyQualifiedName~ClimateStateLegacyStoreGuard"
Passed: 2, Failed: 0, Skipped: 0
```

The first guard run exposed and corrected inventory ordering plus an equality
comparison false-positive in the detector. The final assertion list now matches
the actual source exactly; no production code was changed.

## Independent Verifier Re-check

Atlas re-read the guard test, this receipt, and the Phase 2 notepad entry, then
re-ran the Task 2 gates:

```text
lsp_diagnostics tests/SnowMeltingCalculator.Tests/Climate/ClimateStateLegacyStoreGuardTests.cs
LSP file path must be inside request cwd: D:\IA\ace v.2\tests\SnowMeltingCalculator.Tests\Climate\ClimateStateLegacyStoreGuardTests.cs

dotnet test tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj --filter "FullyQualifiedName~ClimateStateLegacyStoreGuard"
Passed: 2, Failed: 0, Skipped: 0

dotnet test tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj --filter "FullyQualifiedName~ClimateStateLegacyStoreGuard|FullyQualifiedName~ClimateViewModelTests"
Passed: 24, Failed: 0, Skipped: 0

$env:GIT_MASTER='1'; git diff --check -- "tests/SnowMeltingCalculator.Tests/Climate/ClimateStateLegacyStoreGuardTests.cs" "docs/architecture-migration/evidence/phase-2-climate-state/writer-guard.md" ".omo/notepads/phase-2-climate-state/learnings.md" "docs/architecture-migration/TASK_CONTEXT.md" "docs/architecture-migration/plans/phase-2-climate-state.md"
No diff-check errors; Git reported only the existing LF-to-CRLF warning for TASK_CONTEXT.md.
```

The verifier also scanned the Task 2 guard/evidence/control files for TODO/FIXME/
HACK/placeholder markers and found no Task 2 blocker. The only match was an
unrelated historical `Assert.Ignore` mention in `TASK_CONTEXT.md`.

## Tooling Caveat

`lsp_diagnostics` could not run because the harness rejected the absolute file
path with `LSP file path must be inside request cwd`. This is consistent with the
known workspace-root harness limitation recorded in `TASK_CONTEXT.md`; the
targeted `dotnet test` command is the C# correctness gate for this task.

## Status

**PASS**: the current legacy writer/bypass surface is explicitly captured before
migration, the negative fixture proves the forbidden-caller detector works, and
the independent verifier re-check passed.
