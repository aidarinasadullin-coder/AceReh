# Phase 2 Final Wave F3 - real lifecycle QA

Status: APPROVE

Date: 2026-08-13

## Lifecycle coverage

The F3 gate exercised the user-visible lifecycle through the Phase 2 targeted matrix and the full Release suite.

Covered behavior includes:

- Climate city/scalar/high-requirements/reset behavior and ViewModel adapter routes.
- Dirty state and logical mutation boundaries through ProjectSession-owned ClimateState.
- Downstream invalidation through ClimateData projection and CalculationContext publication.
- Save/reload and `.smc` round-trip compatibility.
- Results load/save/export projection behavior.
- Hydraulics/Thermal integration paths affected by Climate changes.
- Repeated load/reset and subscription hygiene through existing lifecycle tests.

## Commands

```powershell
dotnet test "tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Release --filter "FullyQualifiedName~Climate|FullyQualifiedName~ClimateToHydraulicsIntegrationTests|FullyQualifiedName~CalculationContextWriterAuthorityTests|FullyQualifiedName~DoubleCalculationPreventionTests|FullyQualifiedName~ProjectSession|FullyQualifiedName~ProjectLifecycle|FullyQualifiedName~ProjectRoundTrip|FullyQualifiedName~ResetOrchestration|FullyQualifiedName~ResultsStabilizationPhase1|FullyQualifiedName~ResultsViewModelOpenProject|FullyQualifiedName~CalculationContext|FullyQualifiedName~ThermalViewModelTests.ClimateDataChanged" --logger "trx;LogFileName=f3-targeted-release-atlas.trx" --results-directory "docs\architecture-migration\evidence\phase-2-climate-state"
```

Result: PASS, exit `0`; failed `0`, passed `329`, skipped `1`, total `330`, duration about `19 s`.

Receipt: `docs/architecture-migration/evidence/phase-2-climate-state/f3-targeted-release-atlas.trx`.

Skipped test: `ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`, the existing missing-fixture skip already documented in Task 11.

```powershell
dotnet test "tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Release --logger "trx;LogFileName=f3-full-release-atlas.trx" --results-directory "docs\architecture-migration\evidence\phase-2-climate-state"
```

Result: PASS, exit `0`; failed `0`, passed `1613`, skipped `1`, total `1614`, duration about `35 s`.

Receipt: `docs/architecture-migration/evidence/phase-2-climate-state/f3-full-release-atlas.trx`.

The console also listed two explicit baseline-regeneration tests as skipped and one existing Results fixture skip; the TRX counter reports one skipped test for this run. No failed tests remained.

## Verdict

The real lifecycle QA matrix and full Release suite passed. Values, dirty state, downstream invalidation, save/reload, and projection behavior are covered by executable tests and existing evidence.

VERDICT: APPROVE
