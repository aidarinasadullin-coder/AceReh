# Phase 2 Task 8 — Persistence and Results projection

**Date:** 2026-08-11
**Scope:** Read/write Climate snapshots from `ProjectSession.ClimateState`; preserve the existing `.smc` wire-format semantic contract.

## Changed files

- `src/ViewModels/Results/ResultsViewModel.cs`
  - `SaveCurrentProject()` now builds `ClimateProjectData` from `_projectSession.ClimateState.Snapshot`.
  - No Climate DTO field names, types, count, or version marker were changed.
- `tests/SnowMeltingCalculator.Tests/Climate/ClimateStateLegacyStoreGuardTests.cs`
  - Source-text guard now extracts `SaveCurrentProject()` method body and asserts the canonical snapshot mapping.
  - Negative assertions forbid VM-based persistence reads (`SelectedCity = _climateViewModel`, `AirTemperature = _climateViewModel`, etc.) inside the save method.
- `tests/SnowMeltingCalculator.Tests/ViewModels/ResultsViewModelOpenProjectTests.cs`
  - Added `SaveCurrentProject_PersistsClimateStateSnapshot_NotClimateViewModelMirror` proving saved DTO follows `ClimateState` even when the `ClimateViewModel` mirror differs.
  - Updated `ProjectRoundTrip_CitySurvivesRealSaveLoad` and `ProjectRoundTrip_LiveMutationsAreSavedLoadedAndExportedWithoutResultsCalculation` to mutate the canonical session-backed `ClimateState` before save/load.
  - Added optional `IProjectSession` overload to `CreateClimateViewModelWithCity` so tests can use a session-backed adapter.
- `tests/SnowMeltingCalculator.Tests/Project/ProjectRoundTripTests.cs`
  - Added `SaveThenLoad_ClimateFields_RoundTrip` proving v1.1 Climate DTO round-trip through `ProjectFileService`.
  - Added `using SnowMeltingCalculator.Models.Climate;` to resolve `ClimateZone`.

## Verification commands

```powershell
dotnet build "src\SnowMeltingCalculator.csproj" -c Debug
```

Result:

```text
Сборка успешно завершена.
    Предупреждений: 0
    Ошибок: 0
```

```powershell
dotnet test tests\SnowMeltingCalculator.Tests --filter "FullyQualifiedName~ProjectRoundTripTests|FullyQualifiedName~ResultsViewModelOpenProjectTests|FullyQualifiedName~ClimateStateLegacyStoreGuard" -c Debug
```

Result:

```text
Пройден!   : не пройдено     0, пройдено    41, пропущено     1, всего    42, длительность 9 s.
```

The single skipped test (`ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`) is the existing F5 fixture guard that ignores when the owner `Тест/` fixture is absent.

## Source scan for forbidden persistence reads

Scanned `src/ViewModels/Results/ResultsViewModel.cs` for `_climateViewModel` reads of Climate persistence fields:

```text
Lines 991-996   LoadClimateData()        — read-only Results projection, not persistence
Line 1442       CheckDataReadiness()     — UI readiness check, not persistence
Line 1834       HasUnsavedData()         — obsolete helper, not persistence
SaveCurrentProject() method body contains zero `_climateViewModel` Climate reads.
```

## Wire-format statement

The persisted Climate DTO remains exactly the existing eight-field set:

- `SelectedCity` (string)
- `Region` (string)
- `AirTemperature` (double)
- `WindSpeed` (double)
- `Humidity` (double)
- `SnowfallIntensity` (double)
- `SelectedZone` (`ClimateZone`)
- `IsHighRequirements` (bool)

No new persisted fields (e.g. `ColdFiveDayTemperature`, UI search/query/recent state, validation display) were added. The `.smc` version marker, JSON property names, and enum serialization policy are unchanged. Compatibility is isolated at the persistence boundary: `.smc DTO <-> ClimateState snapshot`.

## Architecture invariants

- `ProjectSession.ClimateState` remains the only canonical writable runtime owner of Climate values.
- `ClimateViewModel` remains a read-only adapter / UI mirror.
- Results save does not reintroduce a second writable owner or legacy mutation path.
