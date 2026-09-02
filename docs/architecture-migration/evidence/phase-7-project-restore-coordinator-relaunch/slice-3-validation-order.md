# Phase 7 Slice 3: Validation Before Mutation

Status: PASS
Date: 2026-08-31

## Boundary Evidence

- `ProjectLoadOrchestrator.RestoreModulesFromProjectAsync` builds Thermal and Hydraulics restore candidates before applying Climate or Construction snapshots.
- Thermal candidates are preflighted through `ProjectSessionThermalState.Restore`; rejected candidates return before any project-slice mutation.
- Hydraulics candidates are preflighted through `ProjectSessionHydraulicsState.Restore`; rejected candidates return before any project-slice mutation.
- Legacy-empty Thermal and Hydraulics DTOs retain canonical default behavior before preflight validation.
- Successful restore keeps the canonical order: Climate -> Construction -> Thermal -> Hydraulics, followed by deterministic finalization.

## Characterization Coverage

- Added `RestoreModulesFromProjectAsync_InvalidThermalInput_DoesNotMutatePriorClimateOrThermalSlices`.
- The test establishes prior Climate and Thermal snapshots, attempts to restore an invalid pipe spacing, and verifies both snapshots remain unchanged.
- Existing restore lifecycle characterization tests continue to cover successful canonical project loads, repeated loads without stale construction state, and early/late failure guard cleanup.

## Executed Gates

```text
dotnet build "tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj" -c Debug --nologo
Result: PASS, 0 warnings, 0 errors
```

```text
dotnet test "tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj" -c Debug --no-build --filter "FullyQualifiedName~ProjectLifecycleFlowCharacterizationTests|FullyQualifiedName~ProjectSessionTests|FullyQualifiedName~ProjectSessionThermalStateTests|FullyQualifiedName~ProjectSessionHydraulicsStateTests" --logger "trx;LogFileName=slice-3-validation-order.trx" --results-directory "docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/logs"
Result: PASS, 119 passed, 0 failed, 0 skipped, 119 total
```

TRX: `docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/logs/slice-3-validation-order.trx`

LSP diagnostics were unavailable because the harness rejected absolute paths as outside its request cwd; build and focused tests were used as the available source validation gates.

## Gate Decision

Slice 3 is PASS. Invalid Thermal and Hydraulics restore candidates are rejected before canonical project-slice mutation, legacy-empty DTO behavior remains compatible, and the focused validation suite passes without skipped tests.
