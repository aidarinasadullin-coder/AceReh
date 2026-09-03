# Slice 5 — `MutationBoundaryConsolidationTests`: consolidated INV-016 mutation-boundary proofs

Phase 10 (`phase-10-reactive-ownership-multiplicity-closure`). Write-set:
test-only — new class
`tests/SnowMeltingCalculator.Tests/Services/Project/MutationBoundaryConsolidationTests.cs`
(name fixed by the plan), reusing the Phase 10 harness fixture
(`ReactiveSubscriptionLifecycleTests.ReactiveGraph`, extended only with an
accessor to the already-present `IProjectFileService` mock). No production
edits.

## Slice-by-slice consolidation (public surfaces only, no reflection in test bodies)

| Slice | User-visible action (public boundary) | Completion boundary proof | Multi-field single-action proof |
|---|---|---|---|
| Climate | `ClimateViewModel.SelectedCity = city` → `ProjectSessionClimateState.ApplyCitySelection(…, User)` | exactly **1** `Changed` completion, origin `User` on public args; exactly 1 `CalculationContext.Climate` publication; exactly 1 dirty transition | city + cold-five-day + derived air temperature change in ONE snapshot commit |
| Construction | `AddLayerAbovePipeCommand` → `SyncStateFromCollections(User)` | exactly **1** `CompleteChanged` completion (`After.LayersAbovePipe = Before+1`); exactly 1 `CalculationContext.Construction` publication (`RE-009`); 1 dirty | layer + order re-index + λ in one commit |
| Thermal | `ProjectSessionThermalState.ApplyInputs(…, User)` (state boundary) and `ThermalViewModel.SupplyTemperature` (adapter boundary) | multi-field candidate = exactly **1** completion carrying mode+supply+ground+spacing changes; adapter edit = exactly 1 `User` completion | mode + 2 temperatures + spacing = 4 internal fields, one commit |
| Hydraulics | circuit-length edit on an adapter collector → `ReplaceCollectors(…, User)` | exactly **1** `User`-origin completion (measured: the only origin observed is `User`); exactly 1 new dirty transition | — (single logical field edit) |
| Results | `RefreshAll()` (derived projection rebuild) | rebuild reads canonical snapshots (`ResultsVm.PipeSpacing/OperatingMode` == canonical Thermal inputs, Phase 8 derived projection); **0** canonical completions, **0** dirty during rebuild; exactly 1 observable summary-cards rebuild per `RefreshAll` | — |
| Shell/Save | `SaveProjectCommand` (canonical dirty project, mocked file service success) | exactly **1** clean transition (Phase 6 save boundary); 0 new dirty beyond the deliberate pre-save dirty; project clean after save | — |

Lifecycle/system origins (load, reset, restore, system apply) are asserted
**distinguishable and dirty-free** in
`LoadResetRestoreAndSystemApply_AreDistinguishableAndCreateNoUserDirty`:
origins visible on public completion args (`ClimateMutationOrigin.Load` etc.),
zero user-dirty transitions across the whole traffic, same-value
`SystemApply` completion is a NoChange (quiet). The "future recorder" hook of
`INV-016` remains this origin-on-public-args boundary property; no history
stack, snapshot store, or UI command is introduced anywhere.

Measured fact recorded for the receipt: a hydraulics user circuit edit emits
exactly one canonical completion and its origin is `User` (console output
`[phase-10 consolidation] hydraulics origins: User`, retained in the TRX).

## No-ViewModel-internals fact

The suite compiles against public production surfaces only; no production
private member is accessed (the only `BindingFlags` use in the file is the
test-support `AppSettings` singleton reset copied from `MainViewModelTests`;
production types are constructed and driven exclusively through public
constructors, properties, and commands). Grep fact: no `GetField`/
`GetValue(` target inside this suite points at production code.

## Commands (plan-exact) and results

```
dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo        # 0 warnings / 0 errors
dotnet test ... --filter "FullyQualifiedName~MutationBoundaryConsolidationTests|FullyQualifiedName~ProjectLifecycleFlowCharacterizationTests|FullyQualifiedName~ResultsStabilizationPhase1BehaviorContractsTests|FullyQualifiedName~ProjectSaveServiceTests" --logger "trx;LogFileName=slice-5-mutation-boundaries.trx" --results-directory "docs/architecture-migration/evidence/phase-10-reactive-ownership-multiplicity-closure/logs"
```

Plan-exact filter result: **36 passed / 0 failed / 0 skipped**.
The consolidated suite also passes together with the full Phase 10 harness
(`MutationBoundaryConsolidationTests|ReactiveSubscriptionLifecycleTests` →
18 passed / 0 failed, same TRX path, recorded in the same run file family).
TRX: `logs/slice-5-mutation-boundaries.trx`.

**SLICE 5: PASS**
