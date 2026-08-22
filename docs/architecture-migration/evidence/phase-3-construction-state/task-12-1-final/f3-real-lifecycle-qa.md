# Phase 3 Task 12.1 Final Wave F3 real lifecycle QA

Date: 2026-08-19

This is an independent hands-on QA receipt for Final Wave item F3. The
application surface is the executable WPF lifecycle integration harness over
the production DI graph. No UI-only evidence is used. No production source,
test source, plan, context, notepad, or existing receipt was edited.

## Environment and boundary

- Repository root: `D:/IA/ace v.2`
- HEAD: `e655735dfa66c00cf9c53be93d511eda8989e8bf`
- Branch: `master`
- Staged paths before receipt: `0`
- Test project: `tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj`
- Production project: `src/SnowMeltingCalculator.csproj`
- Test surface: NUnit/VSTest integration tests using
  `ServiceCollectionExtensions.AddApplicationServices()` and the real material
  catalog boundary.

## Fresh F3 commands

### Real lifecycle Release

```powershell
dotnet test "tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Release --no-build --filter "FullyQualifiedName~CanonicalDefaultConstructionLifecycleTests|FullyQualifiedName~MainViewModelTests.NewCalculation|FullyQualifiedName~ProjectLifecycleFlowCharacterizationTests|FullyQualifiedName~ProjectSessionConstructionStateTests|FullyQualifiedName~ConstructionMultiplicityCharacterizationTests|FullyQualifiedName~ConstructionViewModelTests|FullyQualifiedName~ConstructionViewModelEditorIntegrationTests|FullyQualifiedName~ConstructionServiceTemplateImportTests|FullyQualifiedName~MaterialImportTests|FullyQualifiedName~ResetOrchestrationTests|FullyQualifiedName~ProjectRoundTripTests|FullyQualifiedName~ResultsViewModelOpenProjectTests" --logger "trx;LogFileName=phase-3-task-12-1-f3-real-lifecycle-release.trx" --logger "console;verbosity=normal" --results-directory "tests\SnowMeltingCalculator.Tests\TestResults"
```

- Exit: `0`
- TRX aggregate: `203 total / 202 executed / 202 passed / 0 failed / 0 error / 0 timeout / 0 aborted / 0 aggregate notExecuted`
- Result-list non-passed identity: `NotExecuted | ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`
- This is the previously accepted absent external fixture identity. No new
  non-passed identity occurred.

### Canonical contracts Release

```powershell
dotnet test "tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Release --no-build --filter "FullyQualifiedName~CanonicalDefaultConstructionLifecycleTests|FullyQualifiedName~ProjectSessionConstructionStateTests|FullyQualifiedName~ConstructionStateLegacyStoreGuardTests|FullyQualifiedName~DiRegistrationTests" --logger "trx;LogFileName=phase-3-task-12-1-f3-canonical-contracts-release.trx" --logger "console;verbosity=normal" --results-directory "tests\SnowMeltingCalculator.Tests\TestResults"
```

- Exit: `0`
- TRX aggregate: `64 total / 64 executed / 64 passed / 0 failed / 0 error / 0 timeout / 0 aborted / 0 notExecuted`
- Result-list non-passed identities: none.

### Production builds

```powershell
dotnet build "src\SnowMeltingCalculator.csproj" -c Debug --nologo
dotnet build "src\SnowMeltingCalculator.csproj" -c Release --nologo
```

- Debug exit: `0`; `0 warnings / 0 errors`.
- The first Release attempt encountered a transient WPF markup compiler file
  lock on `SnowMeltingCalculator_MarkupCompile.cache`. It is retained as a
  superseded F3 attempt and is not treated as a green gate.
- Fresh isolated Release retry exit: `0`; `0 warnings / 0 errors`.

### Affected Debug

The affected Task 12 matrix was rerun with the canonical default lifecycle and
public NewCalculation tests included.

- Exit: `0`
- TRX aggregate: `320 total / 319 executed / 319 passed / 0 failed / 0 error / 0 timeout / 0 aborted / 0 aggregate notExecuted`
- Result-list non-passed identity: `NotExecuted | ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`
- No new failure or non-passed identity occurred.

### Full Release

```powershell
dotnet test "tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Release --no-build --logger "trx;LogFileName=phase-3-task-12-1-f3-full-release.trx" --logger "console;verbosity=normal" --results-directory "tests\SnowMeltingCalculator.Tests\TestResults"
```

- Exit: `0`
- TRX aggregate: `1714 total / 1711 executed / 1711 passed / 0 failed / 0 error / 0 timeout / 0 aborted / 0 aggregate notExecuted`
- Result-list `NotExecuted` identities:
  - `ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`
  - `RegenerateBaseline`
  - `RegenerateCircuitsBaseline`
- These are exactly the three accepted identities from Task 12.1. There are no
  new skips, failures, errors, timeouts, or aborted results.

VSTest records ignored/explicit tests differently in aggregate counters and in
the result list. This receipt preserves both exact XML representations rather
than normalizing them by assumption.

## Lifecycle assertions

All identities below were explicitly found with `outcome=Passed` in the fresh
real-lifecycle TRX and again in the full Release TRX where applicable.

1. Catalog load and initialization:
   `ColdStartup_DefaultUiExistsButCanonicalConstructionIsInitialized` resolves
   the production DI graph, executes the real Construction initialize command,
   and asserts the adapter, canonical `IProjectSessionConstructionState.Snapshot`,
   and `CurrentProjection`. The canonical recipe is one above-pipe layer and six
   below-pipe layers, with ordered material IDs `5` and `5,6,10,13,2,2`, correct
   `Order`, non-empty unique layer IDs, groundwater `2.0`, `HasLoads=false`, and
   clean session state.
2. Immediate save and round-trip:
   `ColdStartup_ImmediateSavePersistsAndRoundTripsCanonicalDefaultConstruction`
   calls `ResultsViewModel.SaveCurrentProject()`, writes through the real
   `ProjectFileService`, reloads the file, and compares the complete serialized
   project semantics. It asserts schema `1.1`, exactly seven ordered layers,
   groundwater, loads, R1, R2, LambdaE, material names, thickness, calculated
   lambda, override flags, and ordered DTO parity with the canonical snapshot.
3. Immediate Thermal calculation:
   `ColdStartup_ImmediateThermalUsesCanonicalDefaultProjection` uses the valid
   conditions `-5 C`, `1 m/s`, `0.5 mm/h`, and `60 C` supply. The command result
   matches a direct `ThermalCalculator` control fed by the same canonical
   projection for DeltaT, return temperature, and PowerDown within `1e-10`; all
   three outputs are finite and both R1/R2 are positive.
4. Construction mutation and projection:
   `ApplySnapshot_ValidUserChange_PublishesFreshProjectionExactlyOnceAndMarksDirtyOnce`,
   `Projection_IsRefreshedAtomicallyAfterMutation_NeverLagsSnapshot`, and
   `ConstructionCanonicalMutation_RefreshesThermalProjectionAndCalculationContext`
   passed. The mutation boundary refreshes the projection atomically, publishes
   one valid completion/context update, and marks dirty once.
5. NewCalculation, save, and calculate readiness:
   `NewCalculation_ReplacesEditedConstructionWithCanonicalDefaultsAndStaysClean`
   starts from a custom canonical snapshot containing a stale `333 mm` layer,
   runs the public command, and asserts one `Reset` origin, canonical default
   snapshot, adapter parity, removal of the stale layer ID, clean state, zero
   lifecycle Construction context publications, and an immediate schema `1.1`
   seven-layer save. The same fresh suite also passes immediate Thermal against
   the reset/default canonical projection.
6. Project load twice and stale-state removal:
   `RestoreModulesFromProjectAsync_Twice_ReplacesConstructionWithoutStaleFirstProjectValues`
   asserts exactly two `ProjectLoad` origins and project B's canonical/adapter
   values after A then B, with only B's one above-pipe layer remaining.
   `LoadProjectDataAsync_TwiceOnSingletonGraph_ReplacesIdentityWithoutStaleState`
   asserts project B identity and a clean session after the second load.
7. Stable multiplicity:
   `RepeatedInitializationAndReset_DoNotMultiplySubscriptionsOrDownstreamPublication`,
   `RepeatedAddThenReset_ProducesSamePerCycleMarkDirtyCount_AcrossThreeCycles`,
   and `RepeatedResetCycles_DoNotDuplicateCircuitsEventSubscriptions` passed.
   Repeated lifecycle cycles do not increase completion, invalidation, dirty, or
   event-handler counts.
8. Atomic missing-material failure:
   `MissingRequiredDefaultMaterial_DoesNotPartiallyResetStateOrAdapter` passed on
   the production DI graph. It expects `InvalidOperationException` and proves
   canonical snapshot, adapter scalars, both adapter layer sequences, and event
   count remain unchanged. All six
   `Initializer_MissingOneOrSeveralRequiredMaterials_ThrowsBeforeApply` cases
   passed for missing IDs `2`, `5`, `6`, `10`, `13`, and `2,6,13`; each verifies
   the exception identifies every missing material and canonical state receives
   zero mutation events.
9. Rejected candidate atomicity:
   `ApplySnapshot_DuplicateLayerIds_IsRejected_AndCanonicalStateUnchanged`
   passed with `Rejected`, `DuplicateLayerId`, unchanged canonical state, zero
   completion events, and zero dirty calls.

## Artifact manifest

| Artifact | Bytes | SHA-256 |
|---|---:|---|
| `phase-3-task-12-1-f3-real-lifecycle-release.log` | 37184 | `67668901517EAD2C341960370C8A51A876DB3160A62ACEB04D125DC71F912408` |
| `phase-3-task-12-1-f3-real-lifecycle-release.trx` | 276299 | `177ABAE1C695EEAB49B2C0FCD7ED1F77BB9A23D3A7B1E35132D2FF2800A8F833` |
| `phase-3-task-12-1-f3-canonical-contracts-release.log` | 13500 | `992CC6DD4EABE9D90642E366576A59A7A299D4DC6F884FE198FCC16075F1DD7E` |
| `phase-3-task-12-1-f3-canonical-contracts-release.trx` | 88917 | `D60CAE8ABEFAB109D8A3C52FB6BB55F20671A1C1E3412D6A979DC0B2CEDD8E4E` |
| `phase-3-task-12-1-f3-build-debug.log` | 842 | `1C17FFA54A2E59490F50729E9D05BCD59D36D26D391DB27071AC6E5F50D1A9C5` |
| `phase-3-task-12-1-f3-build-release.log` (superseded lock attempt) | 2226 | `0666953A6C7DEB3CD3D9BD50B6E38B2EFA80CBDFD243DD55F4AA8DF18F1A1485` |
| `phase-3-task-12-1-f3-build-release-retry-1.log` | 846 | `97F35CFFA79D56AC4E04DD037F0B6E88FE15E5A302A7E02922D1BF0A4A8BC2FB` |
| `phase-3-task-12-1-f3-affected-debug.log` | 54952 | `DF0DC6AC9C29B38CF388631B95ACB6CBE872D1A4B1E7BD019614F832A3F0A4DD` |
| `phase-3-task-12-1-f3-affected-debug.trx` | 428269 | `9B9DC47EC2665340BE40FA5EFF8163E1450DC1C2E50E6DB85907A352454C74DD` |
| `phase-3-task-12-1-f3-full-release.log` | 259458 | `39DBD4B98137AD1E5E04884583AC3B4196A900B30A29D03AA76950FEBC5BC86E` |
| `phase-3-task-12-1-f3-full-release.trx` | 2253099 | `1E6A398ABE3DBD34D4FCFB3D1676C9F6E740D650F57C5914FEFDD1526647007E` |

The `.log` artifacts are under
`docs/architecture-migration/evidence/phase-3-construction-state/`; the TRX
artifacts are under `tests/SnowMeltingCalculator.Tests/TestResults/`. All names
are fresh and F3-specific.

## Conclusion

The fresh executable evidence proves the production-like DI lifecycle from
catalog initialization through immediate save and Thermal calculation,
canonical mutation, NewCalculation reset, save/calculate readiness, and two
project loads. Canonical snapshots and projections are asserted directly, not
inferred from ViewModel collections. Ordering, identity, clean state, stale
layer removal, stable event multiplicity, schema `1.1`, seven-layer persistence,
finite Thermal/control parity, and atomic missing-material failure all pass.

VERDICT: APPROVE
