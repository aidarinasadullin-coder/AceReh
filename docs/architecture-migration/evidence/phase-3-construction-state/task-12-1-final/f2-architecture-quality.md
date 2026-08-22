# F2 Architecture and Code-Quality Review

Date: 2026-08-19
Repository: `D:/IA/ace v.2`
Scope: independent read-only review of Task 12.1 initializer, DI identity,
lifecycle entrypoints, atomic failure behavior, projection identity, and the
owner-approved fixture changes.

## Fresh executable evidence

### Release build

Command:

```powershell
dotnet build src\SnowMeltingCalculator.csproj -c Release --nologo
```

- Exit: `0`
- Warnings: `0`
- Errors: `0`
- Console log: `tests/SnowMeltingCalculator.Tests/TestResults/phase-3-f2-release-build.log`

### Exact Task 6 contracts filter

Command:

```powershell
dotnet test tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~CanonicalDefaultConstructionLifecycleTests|FullyQualifiedName~ResultsViewModelOpenProjectTests|FullyQualifiedName~DiRegistrationTests|FullyQualifiedName~ConstructionStateLegacyStoreGuardTests|FullyQualifiedName~ConstructionMultiplicityCharacterizationTests|FullyQualifiedName~ProjectSessionConstructionStateTests" --logger "trx;LogFileName=phase-3-f2-contracts.trx" --results-directory tests\SnowMeltingCalculator.Tests\TestResults
```

- Exit: `0`
- Console: `117 passed / 1 skipped / 0 failed / 118 total`
- TRX: `118 total / 117 executed / 117 passed / 0 failed / 0 notExecuted`
- Skipped identity: `ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`
- Skip matches the already accepted absent external-fixture case in the Task 12.1 receipt.
- TRX: `tests/SnowMeltingCalculator.Tests/TestResults/phase-3-f2-contracts.trx`
- Log: `tests/SnowMeltingCalculator.Tests/TestResults/phase-3-f2-contracts.log`

### Missing-material atomic rejection probe

Command selected the two required missing-material tests:

```powershell
dotnet test tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj -c Release --filter "FullyQualifiedName~CanonicalDefaultConstructionLifecycleTests.MissingRequiredDefaultMaterial_DoesNotPartiallyResetStateOrAdapter|FullyQualifiedName~CanonicalDefaultConstructionLifecycleTests.Initializer_MissingOneOrSeveralRequiredMaterials_ThrowsBeforeApply" --logger "trx;LogFileName=phase-3-f2-missing-material-atomic.trx" --results-directory tests\SnowMeltingCalculator.Tests\TestResults
```

- Exit: `0`
- TRX: `7 total / 7 executed / 7 passed / 0 failed / 0 notExecuted`
- The selected atomicity assertions passed: required IDs are resolved before
  `ResetToDefaults`, and missing material failure leaves canonical state,
  adapter, dirty state, and context publication unchanged.
- TRX: `tests/SnowMeltingCalculator.Tests/TestResults/phase-3-f2-missing-material-atomic.trx`
- Log: `tests/SnowMeltingCalculator.Tests/TestResults/phase-3-f2-missing-material-atomic.log`

## Architecture review

### Initializer and canonical ownership

`src/Services/Project/ConstructionDefaultStateInitializer.cs:9-60` owns the
seven-layer recipe, resolves IDs `2, 5, 6, 10, 13`, validates all materials
before mutation, creates fresh layer IDs, and calls
`IProjectSessionConstructionState.ResetToDefaults(...)` once. The state object
does not hold a repository or dialog/ViewModel dependency. This agrees with the
atomic probe and the Task 10/11 receipts.

`src/Services/Project/ProjectSessionConstructionState.cs:25-32` creates the
single `CurrentProjection` from the canonical snapshot. The state mutation
surface owns dirty/context publication; the initializer only supplies the
recipe and origin.

### DI identity and lifecycle entrypoints

`src/Configuration/ServiceCollectionExtensions.cs:94-101,175-178` registers
`IProjectSessionConstructionState` as the `ProjectSession.ConstructionState`
alias, registers `ConstructionDefaultStateInitializer`, and maps
`IConstructionData` to `state.CurrentProjection`. The existing
`DiRegistrationTests` in the fresh contracts run passed the state/session and
projection reference-identity assertions.

The three lifecycle entrypoints use the same initializer/state:

- `src/ViewModels/Construction/ConstructionViewModel.cs:278-285` uses
  `Initialization` after catalog refresh.
- `src/ViewModels/Shell/MainViewModel.cs:225-241` uses `Reset` before the
  destructive NewCalculation reset sequence.
- `src/Services/Project/ProjectLoadOrchestrator.cs:70-81` uses `Reset` before
  clearing the other modules and explicitly applies `result.After` to the
  adapter.

Task 10 origin/multiplicity characterization and Task 11 identity guards were
included in the fresh contract run and passed. No repository dependency was
introduced into canonical state, and no application service newly depends on a
concrete ViewModel; the existing orchestrator/ViewModel calls are adapter
orchestration boundaries.

### Groundwater, IDs, adapter refresh, and fixture scope

The initializer preserves the current canonical groundwater for reset and uses
the adapter's startup value for initialization. `CreateLayer` assigns fresh
non-empty GUIDs and preserves the ordered recipe. Lifecycle callers explicitly
apply the returned snapshot, so adapter refresh does not require a second state
mutation. The atomic probe confirms failure occurs before any reset.

The two owner-approved fixture diffs only provide the shared session state and
repository-backed initializer to stale direct-construction helpers; they do not
weaken assertions or change production semantics. The fresh contracts and
atomic tests passed with those fixtures.

## Superseding constructor-remediation review

This section supersedes the prior F2 rejection. Current source at
`src/ViewModels/Construction/ConstructionViewModel.cs:219-248` requires
non-null `IProjectSessionConstructionState constructionState` and
`ConstructionDefaultStateInitializer defaultStateInitializer`. Lines 245-248
apply explicit null guards and assign those instances directly. There are no
nullable/defaulted parameters and no ViewModel-local construction of either
dependency.

Strict current-source guards produced:

- nullable `IProjectSessionConstructionState?` in `ConstructionViewModel`: `0`;
- nullable `ConstructionDefaultStateInitializer?` in `ConstructionViewModel`: `0`;
- `new ProjectSessionConstructionState(...)` in `ConstructionViewModel`: `0`;
- `new ConstructionDefaultStateInitializer(...)` in `ConstructionViewModel`: `0`;
- production files directly constructing `ConstructionViewModel`: `0`;
- production files constructing `ProjectSessionConstructionState`: `1`, solely
  `ProjectSession`, the intended aggregate owner.

Production DI remains unchanged: `IProjectSessionConstructionState` aliases
`ProjectSession.ConstructionState`, the initializer is a singleton resolved over
that alias, and `IConstructionData` resolves the same state's stable
`CurrentProjection`. The fresh DI contracts below prove reference identity.

The six remediation fixture files preserve their existing assertions and custom
material behavior. Their direct-construction helpers now pass one fixture
session's `ConstructionState` and create the initializer with that exact state
and the same material repository supplied to the ViewModel. No assertion was
removed, weakened, ignored, or replaced. Custom fixture materials remain in
their catalogs and required defaults are supplemented only where lifecycle
initialization needs them.

The former reachable second-owner branch no longer exists. The exact plan
prohibition on an optional production fallback is now satisfied, so the prior
F2 rejection is fully resolved.

## Fresh remediation executable evidence

- Debug production build:
  `tests/SnowMeltingCalculator.Tests/TestResults/phase-3-task-12-1-f2-remediation-build-debug.log`,
  exit `0`, `0` warnings, `0` errors.
- Release production build:
  `tests/SnowMeltingCalculator.Tests/TestResults/phase-3-task-12-1-f2-remediation-build-release.log`,
  exit `0`, `0` warnings, `0` errors.
- Focused Debug suite:
  `tests/SnowMeltingCalculator.Tests/TestResults/phase-3-task-12-1-f2-remediation-focused-debug-final.trx`;
  parsed TRX is `99 total / 99 executed / 99 passed / 0 failed / 0 NotExecuted`.
- Exact Debug contracts:
  `tests/SnowMeltingCalculator.Tests/TestResults/phase-3-task-12-1-f2-remediation-contracts-debug.trx`;
  parsed result list is
  `118 total / 117 executed / 117 passed / 0 failed / 1 NotExecuted`.
- Exact Release contracts:
  `tests/SnowMeltingCalculator.Tests/TestResults/phase-3-task-12-1-f2-remediation-contracts-release.trx`;
  parsed result list is
  `118 total / 117 passed / 0 failed / 1 NotExecuted`. The sole non-passed
  identity remains
  `ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`, the
  accepted absent external fixture.

The prior `7/7` missing-material atomic probe remains applicable and no fresh
source or executable evidence contradicts it. Task 10 origin/multiplicity and
Task 11 `CurrentProjection` identity are included in the fresh exact contracts
and remain green. The separate F1 allow-list blocker is outside F2 architecture
scope and is not treated as an F2 failure.

## LSP note

Diagnostics were attempted for each relevant changed C# file. The external
harness rejected every request at its boundary with:

`LSP file path must be inside request cwd: D:\IA\ace v.2\...`

This limitation persists after remediation. Debug/Release compilation and the
fresh focused/contracts suites are authoritative.

## Verdict

The initializer, DI alias, lifecycle origin routing, atomic failure behavior,
groundwater/ID policy, adapter refresh, Task 10 multiplicity, and Task 11
projection identity remain supported. Current source and fresh executable
evidence prove the nullable fallback and second ViewModel-owned state were
removed without weakening fixture behavior.

VERDICT: APPROVE
