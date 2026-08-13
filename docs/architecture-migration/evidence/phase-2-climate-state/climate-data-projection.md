# Phase 2 Task 5 — ClimateData Projection Hardening Evidence

## Scope

Convert `ClimateData`/`IClimateData` to a non-owning compatibility projection.
`ProjectSession.ClimateState` (Task 4) remains the sole writable canonical owner of
project Climate values. Task 6 (ClimateViewModel adapter rewiring through
`ProjectSession.ClimateState`) was **not started**.

## Files changed by this task

| Path | Change |
|---|---|
| `src/Models/Climate/ClimateData.cs` | Concrete properties changed from `public set` to `internal set`; added `[assembly: InternalsVisibleTo("SnowMeltingCalculator.Tests")]`; added approved `ApplyProjection(IClimateData source, bool isValid = true)` seam; `RaiseDataChanged` kept public for compatibility with a doc comment restricting use to the projection updater. |
| `src/ViewModels/Climate/ClimateViewModel.cs` | `SyncToClimateData()` now calls `data.ApplyProjection(GetClimateData(), IsValid)` instead of eight direct concrete property assignments; still calls `_calculationContext.UpdateClimate(_climateData, "Climate")` once. |
| `tests/SnowMeltingCalculator.Tests/Climate/ClimateStateLegacyStoreGuardTests.cs` | Updated writer inventory: asserts no public writable `ClimateData` setters, asserts `ApplyProjection` is the only internal mutation seam, asserts `SyncToClimateData` invokes it, adds failure QA for direct concrete/cast assignments. |
| `tests/SnowMeltingCalculator.Tests/Climate/ClimateDataProjectionTests.cs` | New narrow tests proving `IClimateData` is read-only, `ClimateData` properties are not publicly settable, `ApplyProjection` updates values and raises `DataChanged` exactly once, forwards `isValid`, and rejects `null`. |

## Commands and results

### Build

```powershell
dotnet build "D:\IA\ace v.2\src\SnowMeltingCalculator.csproj" -c Debug
```

Result: `0 error(s), 0 warning(s)` — build succeeded.

```powershell
dotnet build "D:\IA\ace v.2\tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Debug
```

Result: `0 error(s), 0 warning(s)` — test project build succeeded.

### Task 5 happy QA

```powershell
dotnet test "D:\IA\ace v.2\tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Debug --filter "FullyQualifiedName~ClimateViewModelTests|FullyQualifiedName~ThermalViewModelTests.ClimateDataChanged|FullyQualifiedName~ClimateToHydraulicsIntegrationTests"
```

Result: `Passed! - Failed: 0, Passed: 40, Skipped: 0, Total: 40`

### Projection/guard/multiplicity QA

```powershell
dotnet test "D:\IA\ace v.2\tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Debug --filter "FullyQualifiedName~ClimateStateLegacyStoreGuard|FullyQualifiedName~ClimateDataProjection|FullyQualifiedName~ClimateMultiplicity"
```

Result: `Passed! - Failed: 0, Passed: 17, Skipped: 0, Total: 17`

### Broader climate/session/thermal/context regression gate

```powershell
dotnet test "D:\IA\ace v.2\tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Debug --filter "FullyQualifiedName~Climate|FullyQualifiedName~ProjectSession|FullyQualifiedName~ThermalViewModelTests|FullyQualifiedName~CalculationContext"
```

Result: `Passed! - Failed: 0, Passed: 274, Skipped: 0, Total: 274`

### Full release suite

```powershell
dotnet test "D:\IA\ace v.2\tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Release
```

Result: `Passed! - Failed: 0, Passed: 1606, Skipped: 1, Total: 1607`

The single skipped test (`ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`) is a pre-existing skip unrelated to Task 5.

## LSP status

C# LSP diagnostics could not be run for changed files. The harness returned the
known error for each file:

```
LSP file path must be inside request cwd: D:\IA\ace v.2\src\Models\Climate\ClimateData.cs
```

As recorded in `TASK_CONTEXT.md`, the executable correctness gate is `dotnet build`/`dotnet test`, both of which passed.

## Scope confirmation

- No edits to `ProjectLoadOrchestrator`, `ResultsViewModel`, report builders, DI registrations, architecture maps, generated widget/model, Phase 1 docs, `.smc` files, formulas, UI/XAML/design, packages, or installer/publish/build artifacts.
- No edits to `CalculationContext` or downstream invalidation routing.
- `ClimateViewModel` still owns its writable UI backing fields and still calls `CalculationContext.UpdateClimate`; only the direct concrete `ClimateData` assignments were replaced by the approved projection updater.
- Task 3 multiplicity counts remain unchanged (verified by the still-passing `ClimateMultiplicityCharacterizationTests`).
- Task 6 (ClimateViewModel adapter rewiring through `ProjectSession.ClimateState`) **not started**.
