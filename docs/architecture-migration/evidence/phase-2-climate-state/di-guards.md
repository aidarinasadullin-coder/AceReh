# Phase 2 Climate State - Task 10 DI and single-owner guards

## Scope

Phase 2 Task 10 of `phase-2-climate-state`. The task proves that all Climate
lifecycle consumers resolve to the canonical `ProjectSession`-owned
`ProjectSessionClimateState`, with no independent DI registration and no
mutable adapter owner field.

This receipt is appended to the accepted Task 9 evidence chain
(`docs/architecture-migration/evidence/phase-2-climate-state/`); it does not
modify the Task 9 acceptance decision.

## Changed files

Test files only. No production DI / runtime sources were modified.

- `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectSessionTests.cs`
  - Replaced the weak `ClimateState_IsOwnedBySession` self-comparison with
    `ProjectSession_ClimateState_ReturnsStableCanonicalOwner`. The new test
    captures two property reads, asserts `Is.SameAs`, mutates through the
    first reference using `ApplyIndividualEdit(ClimateEditField.AirTemperature,
    -12.5, ClimateMutationOrigin.User)`, and verifies the second snapshot sees
    the change. The test would fail if a getter returned a new instance per
    read or if the mutation went into a disconnected store.
  - Added `using SnowMeltingCalculator.Models.Climate;` to bring
    `ClimateEdit`, `ClimateEditField`, `ClimateMutationOrigin` into scope.

- `tests/SnowMeltingCalculator.Tests/Configuration/DiRegistrationTests.cs`
  - Added `using System.Linq;`,
    `using SnowMeltingCalculator.Core;`,
    `using SnowMeltingCalculator.Models.Climate;`,
    `using SnowMeltingCalculator.Services.Project;`,
    `using SnowMeltingCalculator.Services.Results;`,
    `using SnowMeltingCalculator.ViewModels.Climate;`,
    `using SnowMeltingCalculator.ViewModels.Results;`.
  - Added `CreateApplicationServices()` helper that returns a fresh
    `ServiceCollection` after `AddApplicationServices()`.
  - Added `ClimateLifecycleDescriptors_HaveNoTransientSecondOwner`:
    asserts that no descriptor has
    `ServiceType == typeof(IProjectSessionClimateState)` or
    `ServiceType == typeof(ProjectSessionClimateState)`, and that
    `ProjectSession`, `IClimateData`, `CalculationContext`,
    `ClimateViewModel`, `ProjectLoadOrchestrator`, and `ResultsViewModel`
    are all registered as `ServiceLifetime.Singleton`.
  - Added `ClimateLifecycleConsumers_ObserveCanonicalProjectionChain`:
    builds a fresh provider, resolves `IProjectSession`,
    `ClimateViewModel`, `IClimateData`, `CalculationContext`,
    `ProjectLoadOrchestrator`, `ResultsViewModel`, and `IMarkDirtyService`,
    asserts every service resolves, asserts `IMarkDirtyService` is the same
    instance as `IProjectSession`, then mutates via
    `session.ClimateState.ApplyIndividualEdit(new ClimateEdit(
    ClimateEditField.AirTemperature, -12.5), ClimateMutationOrigin.User)`
    and asserts the canonical chain observes the change:
    result `IsChanged`/`IsValid`, `climateData.AirTemperature == -12.5`,
    `ReferenceEquals(calculationContext.Climate, climateData)`,
    `climateViewModel.AirTemperature == -12.5`, and
    `session.IsDirty == true`.

- `tests/SnowMeltingCalculator.Tests/Climate/ClimateStateLegacyStoreGuardTests.cs`
  - Extended `ClimateStateLegacyStoreGuard_CapturesExactCurrentWriterAndProjectionInventory`
    to read
    `src/Configuration/ServiceCollectionExtensions.cs` and
    `src/Services/Project/ProjectSession.cs`.
  - Asserted source-text prohibitions for any lifetime of
    `IProjectSessionClimateState` and the concrete
    `ProjectSessionClimateState` in DI: no `AddSingleton`,
    `AddTransient`, or `AddScoped` for either type.
  - Asserted that `ProjectSession` keeps
    `private readonly ProjectSessionClimateState _climateState;` as the
    only owner field.
  - Asserted that `ClimateViewModel`, `ProjectLoadOrchestrator`, and
    `ResultsViewModel` do not declare
    `private ProjectSessionClimateState` or
    `private readonly ProjectSessionClimateState`. The allowed forwarding
    field is `private readonly IProjectSessionClimateState _climateState;`
    in `ClimateViewModel`; the orchestrator and Results stay free of any
    concrete climate owner field.

## Verification

Approved Debug gate only (no Release suite, no full suite, no Task 11
broad gate). Atlas ran the gates after Task 10 implementation.

```text
dotnet build "src\SnowMeltingCalculator.csproj" -c Debug
```

PASS: 0 warnings, 0 errors.

```text
dotnet test "tests\SnowMeltingCalculator.Tests" \
  --filter "FullyQualifiedName~DiRegistrationTests|FullyQualifiedName~ProjectSession|FullyQualifiedName~ClimateStateLegacyStoreGuard" \
  -c Debug
```

PASS: failed 0, passed 40, skipped 0, total 40, duration 308 ms.

LSP diagnostics attempted on the changed C# files
(`tests/SnowMeltingCalculator.Tests/Services/Project/ProjectSessionTests.cs`,
`tests/SnowMeltingCalculator.Tests/Configuration/DiRegistrationTests.cs`,
`tests/SnowMeltingCalculator.Tests/Climate/ClimateStateLegacyStoreGuardTests.cs`)
and failed with the known C# harness message
`file path must be inside request cwd`; targeted
`dotnet build` / `dotnet test` are the executable correctness gates, as
recorded in earlier phase receipts.

## Production DI changes

None. `src/Configuration/ServiceCollectionExtensions.cs`,
`src/Services/Project/ProjectSession.cs`,
`src/ViewModels/Climate/ClimateViewModel.cs`,
`src/Services/Project/ProjectLoadOrchestrator.cs`, and
`src/ViewModels/Results/ResultsViewModel.cs` were intentionally not touched.
The guards above certify that the existing registrations already satisfy
the single-owner invariant.

## Out of scope

- Task 11 (`full user-flow and release gate`) was not started; this receipt
  explicitly records that.
- `.smc` wire format, UI design, formulas, packages, release artifacts,
  installer, Phase 1 docs/evidence, maps/model/widget, and
  `TASK_CONTEXT.md` plan checkboxes were not modified.
- No git stage/commit/reset/checkout/clean/sparse-checkout was performed.