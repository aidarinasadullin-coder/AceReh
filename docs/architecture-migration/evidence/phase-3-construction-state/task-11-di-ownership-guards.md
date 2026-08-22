# Task 11: DI and ownership guards

## 1. Scope

Task 11 binds production lifecycle and thermal-facing consumers to the single
Construction state owned by singleton `ProjectSession`. The change is limited to
DI ownership/projection wiring and executable ownership guards. It does not alter
Construction or Thermal formulas, lifecycle mutation semantics, persistence,
UI, schema, packages, maps, widget, or Task 12/13 artifacts.

## 2. Changed files

- `src/Configuration/ServiceCollectionExtensions.cs` now binds
  `IConstructionData` to `IProjectSessionConstructionState.CurrentProjection`
  instead of mutable `Construction`.
- `tests/SnowMeltingCalculator.Tests/Configuration/DiRegistrationTests.cs` adds
  construction lifecycle descriptor, consumer identity, canonical projection,
  and behavior tests.
- Supporting canonical projection contract/state files expose the stable
  `CurrentProjection` consumed by DI while retaining the Task 10 completion
  semantics.
- This receipt records the Task 11 implementation and verification boundary.

## 3. DI ownership conclusions

- `IProjectSessionConstructionState` resolves from singleton
  `ProjectSession.ConstructionState`; there is no separately registered concrete
  or transient `ProjectSessionConstructionState` owner.
- `IProjectSession`, `IProjectStateService`, and `IMarkDirtyService` resolve to
  the same singleton `ProjectSession` instance.
- Thermal-facing `IConstructionData` resolves to
  `IProjectSessionConstructionState.CurrentProjection`, so it observes the
  canonical Construction state rather than the mutable adapter model.
- `Construction` remains registered only as compatibility model for the
  `ConstructionViewModel` adapter. It is not exposed as the production
  `IConstructionData` service and therefore is not the thermal canonical owner.
- The provider resolves `ConstructionViewModel`, `ProjectLoadOrchestrator`, and
  `ResultsViewModel` without a circular lifetime or service locator.
- A canonical user mutation refreshes the stable thermal projection and makes
  `CalculationContext.Construction` reference that same projection.

## 4. Residual grep classification

- `new ProjectSessionConstructionState` in `ProjectSession` is expected: the
  aggregate root constructs and owns its one Construction state slice.
- The optional `ConstructionViewModel` fallback remains a test/helper
  compatibility path. Production DI supplies the registered canonical
  `IProjectSessionConstructionState`, so the fallback is not reached by the
  application provider.
- The optional `ProjectLoadOrchestrator` session fallback remains a legacy
  test/helper compatibility path. Production DI supplies singleton
  `IProjectSession`; application resolution therefore uses
  `ProjectSession.ConstructionState`.
- `AddSingleton<Construction>` is retained for the WPF adapter compatibility
  model only. `AddSingleton<IConstructionData>` no longer maps to it and instead
  maps to `CurrentProjection`.

## 5. Verification commands/results

Targeted Task 11 ownership gate:

```powershell
dotnet test tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj -c Debug --filter "FullyQualifiedName~DiRegistrationTests|FullyQualifiedName~ProjectSessionConstructionStateTests|FullyQualifiedName~ConstructionStateLegacyStoreGuardTests" --logger "trx;LogFileName=phase-3-task-11-di-guards.trx"
```

Result: `49 passed / 0 failed / 0 skipped`.

TRX: `tests/SnowMeltingCalculator.Tests/TestResults/phase-3-task-11-di-guards.trx`.

Production Debug build:

```powershell
dotnet build "src\SnowMeltingCalculator.csproj" -c Debug --nologo
```

Result: `0 warnings / 0 errors`.

`lsp_diagnostics` failed for the two changed C# files with the known C# harness
cwd/workspace-root mismatch. The successful targeted `dotnet test` and Debug
`dotnet build` are the authoritative compile and behavior gates.

## 6. Thermal false alarm note

The reported thermal deltaT anomaly was closed as a wrong Debug artifact launch.
Task 11 made no Thermal formula or Thermal calculation code changes.

Conclusion: production DI has one canonical Construction owner, exposes its
stable read-only projection to thermal consumers, preserves the mutable
`Construction` object only as a non-canonical ViewModel adapter model, and
resolves the lifecycle graph without a duplicate owner or circular lifetime.
