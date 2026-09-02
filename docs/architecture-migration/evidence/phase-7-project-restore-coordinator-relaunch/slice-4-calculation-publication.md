# Slice 4 — Calculation Publication Multiplicity (PASS)

**Date:** 2026-08-31
**Plan:** `docs/architecture-migration/plans/phase-7-project-restore-coordinator-relaunch.md` (frozen, NOT edited)
**Lane:** continuation of Slice 4 / Todo 4 (same execution lane)

## Exact Commands

```powershell
dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo
dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~ProjectSessionThermalStateTests|FullyQualifiedName~ProjectSessionHydraulicsStateTests|FullyQualifiedName~HydraulicsMultiplicityCharacterizationTests" --logger "trx;LogFileName=slice-4-calculation-publication.trx" --results-directory "docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/logs"
```

## Build Result

- `dotnet build ... -c Debug --nologo` → **Сборка успешно завершена. Предупреждений: 0, Ошибок: 0**

## Test Result (exact focused filter)

- **Пройдено: 102, не пройдено: 0, пропущено: 0, всего: 102**
- TRX: `docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/logs/slice-4-calculation-publication.trx`
- Matched classes: `ProjectSessionThermalStateTests`, `ProjectSessionHydraulicsStateTests`, `HydraulicsMultiplicityCharacterizationTests` (both happy + failure paths executed).

### Target test `ThermalContextRouting_CalculationFailurePublishesTerminalFailureOnce` → `outcome="Passed"`

Assertions verified:
- `publications == 1` (exactly one `CalculationContext.HydraulicsResults` publication on failure path)
- `fixture.Context.HydraulicsResults == null` (null context result)
- `fixture.SummaryCalls == 0` (zero calculation summary calls on failure)
- `Status.Phase == HydraulicsCalculationPhase.Error` (canonical terminal failure transition)
- `Status.ValidationMessage` contains `"injected hydraulics failure"` (canonical failure message propagated)
- `Collectors` not empty, and `Collectors.All(Summary is null)` (cleared summaries)

## Root Cause (duplicate publication)

`HydraulicsStateCoordinator.RunCalculation` failure branch previously called **both**:
1. `PublishHydraulics(null)` — a second `CalculationContext.HydraulicsResults` publication, AND
2. `_state.FailCalculation(...)`.

Additionally, `CircuitsViewModel.ExecuteCalculateAll` catch block already calls `_coordinator.PublishHydraulics(null)` + `_calculationStateService.SetHydraulicsError(...)`. So the failure path emitted **two** `HydraulicsResults` publications (one from the VM catch, one from the coordinator failure branch).

## Minimal Fix (Slice 4 write-set only)

- `src/Services/Project/HydraulicsStateCoordinator.cs`
  - Removed the duplicate `PublishHydraulics(null)` in the failure branch; kept only `_state.FailCalculation(_calculationStateService.HydraulicsValidationMessage)` (canonical terminal transition).
  - Added `_state.BeginCalculation()` so the phase is `Calculating` before `FailCalculation` (otherwise `FailCalculation` rejects).
  - `OnContextChanged` thermal-result branch now routes through `CalculateAll(_calculateAll!)` (full `RunCalculation` path) instead of invoking the delegate directly — preserves the single-publication invariant for valid results.
  - `CompleteCalculation` now receives the real `summaryByCollector` map (valid-result exactly-once fix).
- `src/Services/Project/ProjectSessionHydraulicsState.cs`
  - `FailCalculation` now clears each collector's `Summary` to `null` (canonical terminal failure with cleared summaries).
- `tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/HydraulicsMultiplicityCharacterizationTests.cs`
  - Added `ThermalContextRouting_ValidResultPublishesFreshHydraulicsStateOnce` and `ThermalContextRouting_CalculationFailurePublishesTerminalFailureOnce`.
  - `Fixture` now models the `ICalculationStateService` contract: `SetHydraulicsError` stores the message and `HydraulicsValidationMessage` returns it, so the canonical terminal failure state carries the injected message (previously the mock returned empty string, breaking the assertion).

## Changed Paths (Slice 4 write-set only)

- `src/Services/Project/HydraulicsStateCoordinator.cs`
- `src/Services/Project/ProjectSessionHydraulicsState.cs`
- `tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/HydraulicsMultiplicityCharacterizationTests.cs`

> Other dirty files present in the working tree (`src/Services/Project/ProjectLoadOrchestrator.cs`, `tests/.../ProjectLifecycleFlowCharacterizationTests.cs`, `.opencode/commands/architecture-*.md`, `docs/architecture-migration/STATE.json`, `docs/architecture-migration/TASK_CONTEXT.md`, `evidence/phase-0.5-acceptance-v2.json`, `workflow/validate-state.*`) originate from prior Slices 1-3 (which already hold accepted PASS receipts) and were **NOT** modified in this lane.

## Residual Risks

- **LSP unavailable:** `lsp_diagnostics` failed with `LSP file path must be inside request cwd` (documented harness limitation). Authoritative compile gate was `dotnet build` (0 warnings/0 errors). No source-level diagnostics could be independently confirmed via LSP.
- **VM catch block dual call:** `CircuitsViewModel.ExecuteCalculateAll` catch still calls both `SetHydraulicsError` and `PublishHydraulics(null)`. This is intentional — the single externally-visible null publication plus the canonical error transition. The coordinator no longer double-publishes. If a future refactor moves publication responsibility out of the VM, re-verify the single-publication invariant.
- **Mock contract coupling:** the characterization test mock now mirrors `HydraulicsValidationMessage`/`SetHydraulicsError`. If the real `CalculationStateService` contract changes, the mock must be updated in lockstep.
- **No owner decision escalated:** observed behavior required no public/API contract, state-ownership, rollback-semantics, calculation-source-of-truth, or scope change.
