# Phase 2 Task 3: Climate multiplicity characterization

Date: 2026-08-06

## Scope

This receipt locks the observable legacy `ClimateViewModel` compatibility
sequence before Task 4. The probes subscribe to `ClimateViewModel.DataChanged`,
concrete `ClimateData.DataChanged`, and `CalculationContext.ContextChanged`, and
count `IMarkDirtyService.MarkDirty()` with a Moq callback. All counts are per
logical action after setup counters are cleared.

## Observed counts

| Logical action | Final state observed | MarkDirty calls | ClimateData.DataChanged | ClimateViewModel.DataChanged | CalculationContext.ContextChanged |
|---|---|---:|---:|---:|---:|
| Select Moscow | city=Москва, air=-15, city projection=Москва | 3 | 3 | 3 | 3 |
| Edit air temperature to -20 | air=-20 in VM and projection | 1 | 1 | 1 | 1 |
| Toggle high requirements on | zone=Zone_M20_Plus, air=-20 | 2 | 2 | 2 | 2 |
| Reset after edits | city=null, projection city empty, air=-15 | 0 | 1 | 1 | 1 |
| Reset to Moscow data after scalar edits | air=-15, wind=4.5, humidity=85 | 3 | 4 | 4 | 4 |
| Same-value air edit (-15 to -15) | air remains -15 | 0 | 0 | 0 | 0 |
| Same `CityInfo` instance selection | projection city remains Москва | 0 | 0 | 0 | 0 |
| Load city list | two filtered cities, LoadCalls=1 | 0 | 0 | 0 | 0 |
| Second list load | two filtered cities, LoadCalls=2 | 0 | 0 | 0 | 0 |
| First reset from defaults | defaults retained | 0 | 1 | 1 | 1 |
| Repeated reset from defaults | defaults retained | 0 | 1 | 1 | 1 |

The test names are filtered by `ClimateMultiplicity` and encode these values as
explicit assertions. Legacy nested updates are intentional characterization,
not target behavior: selecting a city runs scalar property handlers before the
selected-city completion; toggling high requirements runs the air-temperature
handler before its own completion; `ResetToCityData` runs restored scalar
handlers plus its explicit completion.

## Downstream boundary

The narrow VM probe does not construct `ThermalViewModel`, `CircuitsViewModel`,
or `ResultsViewModel`, so it cannot honestly report thermal invalidation,
circuits recalculation, or Results refresh counts. Current observable routing is
the counted `ClimateData.DataChanged` and `CalculationContext.ContextChanged`
compatibility boundary. Existing relevant coverage remains:

- `ThermalViewModelTests.ClimateDataChanged_ClearsResult` for thermal invalidation.
- `ClimateToHydraulicsIntegrationTests` and `DoubleCalculationPreventionTests`
  for circuits reactions and duplicate-calculation behavior.
- `ProjectLifecycleFlowCharacterizationTests`, `ResetOrchestrationTests`, and
  `ResultsViewModelOpenProjectTests` for project-load/reset/results orchestration.

Task 4+ must either preserve these legacy counts where compatibility requires it
or deliberately update this receipt and the corresponding assertions when the
new canonical completion boundary removes duplicate legacy notifications.

## Commands and results

```powershell
dotnet test tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj --filter "FullyQualifiedName~ClimateMultiplicity"
```

Result: PASS, 9 passed, 0 failed, 0 skipped.

## Independent verifier re-check

Atlas re-read the new characterization test, this receipt, and the Phase 2
notepad entry, then re-ran Task 3 gates:

```text
lsp_diagnostics tests/SnowMeltingCalculator.Tests/Climate/ClimateMultiplicityCharacterizationTests.cs
LSP file path must be inside request cwd: D:\IA\ace v.2\tests\SnowMeltingCalculator.Tests\Climate\ClimateMultiplicityCharacterizationTests.cs

dotnet test tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj --filter "FullyQualifiedName~ClimateMultiplicity"
Passed: 9, Failed: 0, Skipped: 0

dotnet test tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj --filter "FullyQualifiedName~ClimateMultiplicity|FullyQualifiedName~ClimateStateLegacyStoreGuard|FullyQualifiedName~ClimateViewModelTests"
Passed: 33, Failed: 0, Skipped: 0

dotnet test tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj --filter "FullyQualifiedName~Climate|FullyQualifiedName~ClimateToHydraulicsIntegrationTests|FullyQualifiedName~DoubleCalculationPreventionTests|FullyQualifiedName~CalculationContext|FullyQualifiedName~ThermalViewModelTests.ClimateDataChanged"
Passed: 203, Failed: 0, Skipped: 0

$env:GIT_MASTER='1'; git diff --check -- "tests/SnowMeltingCalculator.Tests/Climate/ClimateMultiplicityCharacterizationTests.cs" "docs/architecture-migration/evidence/phase-2-climate-state/multiplicity-characterization.md" ".omo/notepads/phase-2-climate-state/learnings.md" "docs/architecture-migration/plans/phase-2-climate-state.md" "docs/architecture-migration/TASK_CONTEXT.md"
No diff-check errors; Git reported only the existing LF-to-CRLF warning for TASK_CONTEXT.md.
```

The verifier also scanned Task 3 test/evidence/notepad files for TODO/FIXME/HACK,
placeholder markers, and trivial assertion markers; no matches were found.

## Status

PASS for the Task 3 multiplicity characterization suite and independent
verifier re-check. No production files, state ownership, or Task 4 API were
changed.
