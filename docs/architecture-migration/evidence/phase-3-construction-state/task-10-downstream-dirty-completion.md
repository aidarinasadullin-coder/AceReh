# Phase 3 Task 10 downstream and dirty completion

Date: 2026-08-16

## Verdict

`PASS` - Task 10 is accepted by executable verification after the owner
authorized `src/ViewModels/Construction/ConstructionViewModel.cs` as a narrow
production exception.

One changed, valid user-visible Construction action now completes through
`ProjectSessionConstructionState.CompleteChanged(...)`, refreshes one immutable
`ConstructionStateProjection`, updates `CalculationContext` at most once, and
applies dirty semantics from `ConstructionMutationOrigin`.

Task 11 was not started. Phase 3 remains executing and is not completed or
owner-accepted.

## Behavior and implementation

- `User` and `Template` changed completions mark dirty and publish one valid
  projection through `CalculationContext.UpdateConstruction(...)`.
- `ProjectLoad`, `Reset`, `Restore`, `SystemApply`, and `Initialization`
  completions neither publish downstream nor create user dirty state.
- `NoChange` and `Rejected` results complete without publication or dirty state.
- A changed invalid user snapshot marks dirty but does not replace the valid
  downstream Construction projection.
- Standalone `FileLoad` retains its characterized downstream publication
  semantics without being classified as a user-dirty origin.
- Direct ViewModel dirty calls and direct ViewModel context publication were
  removed. Scalar, collection, layer, and template actions now identify their
  canonical `User` or `Template` origin.
- Collection and material cascades are coalesced into one canonical completion;
  adapter order and subscription reconciliation remain intact.
- Validation no longer mutates live adapter layers while constructing its
  temporary model. The now-unused legacy `SyncLayerCollection` path was removed.
  The public VM `DataChanged` compatibility surface and its private publisher
  seam remain to avoid an API break, but no Construction action calls that seam.
- Task 9 project save remains canonical snapshot mapping through
  `ConstructionPersistenceMapper`; no save-time `SyncToCanonicalState()` call
  was introduced and the `.smc` schema/version was unchanged.

`ConstructionMutationStatus.Cancelled` remains a contract value, but the current
Construction mutation API has no operation that produces it. No synthetic
production cancellation path was added; the contract continues to guarantee
that `Changed` is the only status entering authoritative completion.

## RED evidence

The Task 10 behavior filter first compiled after a test-only concrete-state cast
and then failed for the intended legacy boundary:

- `11 failed / 23 passed / 0 skipped / 34 total`;
- failures showed zero canonical context publications, legacy multiplicity,
  direct ViewModel dirty/context writers, and missing invalid-projection
  semantics.

Historical allow-list discovery and owner-resolution context remains at
`docs/architecture-migration/evidence/phase-3-construction-state/task-10-allowlist-blocker.md`.

## Final commands and results

### Task 10 targeted matrix

```powershell
dotnet test "tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Debug --nologo --filter "FullyQualifiedName~ProjectSessionConstructionStateTests.ApplySnapshot_ValidUserChange_PublishesFreshProjectionExactlyOnceAndMarksDirtyOnce|FullyQualifiedName~ProjectSessionConstructionStateTests.ApplySnapshot_NoChangeOrRejected_PublishesNothingAndDoesNotDirty|FullyQualifiedName~ProjectSessionConstructionStateTests.ApplySnapshot_ValidLifecycleChange_PublishesNothingWithoutUserDirty|FullyQualifiedName~ProjectSessionConstructionStateTests.ApplySnapshot_InvalidUserChange_MarksDirtyButDoesNotPublish|FullyQualifiedName~ConstructionMultiplicityCharacterizationTests|FullyQualifiedName~ConstructionStateLegacyStoreGuardTests" --logger "trx;LogFileName=phase-3-task-10-final-targeted.trx"
```

Result: exit `0`; `34 passed / 0 failed / 0 skipped / 34 total`.

### Task 9 direct recovery regression

The exact eleven-test Task 9 recovery filter passed:
`11 passed / 0 failed / 0 skipped / 11 total`.

Raw artifact:
`tests/SnowMeltingCalculator.Tests/TestResults/phase-3-task-10-final-task-9-direct.trx`.

### Task 9 focused persistence regression

The exact six-test persistence filter passed:
`6 passed / 0 failed / 0 skipped / 6 total`.

Raw artifact:
`tests/SnowMeltingCalculator.Tests/TestResults/phase-3-task-10-final-task-9-focused.trx`.

### Directly affected integration matrix

```powershell
dotnet test "tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Debug --nologo --filter "FullyQualifiedName~ResultsViewModelOpenProjectTests|FullyQualifiedName~ProjectRoundTripTests|FullyQualifiedName~ConstructionRepositoryTests|FullyQualifiedName~ConstructionServiceTests|FullyQualifiedName~ConstructionViewModelTests|FullyQualifiedName~ProjectLifecycleFlowCharacterizationTests|FullyQualifiedName~ThermalViewModelTests" --logger "trx;LogFileName=phase-3-task-10-affected-green.trx"
```

Result: exit `0`; `173 passed / 0 failed / 1 skipped / 174 total`.
The exact known skip remains
`ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`.

### Debug build

```powershell
dotnet build "src\SnowMeltingCalculator.csproj" -c Debug --nologo
```

Result: exit `0`; warnings `0`; errors `0`.

### LSP diagnostics

Diagnostics were attempted on every changed C# source/test file and reproduced
the known harness/root mismatch:

```text
LSP file path must be inside request cwd: D:\IA\ace v.2\...
```

The fresh `dotnet test` and `dotnet build` results are the authoritative C#
verification.

## Scope audit

- Production changes are limited to the Construction state/projection/session
  seam and the owner-authorized `ConstructionViewModel.cs` exception.
- Directly affected tests were updated for Task 10 origin and publication
  contracts; no failing behavior assertion was removed.
- No formula, XAML/public binding surface, package, `.smc` schema/version,
  Thermal/Results redesign, installer, or release artifact was changed.
- Task 11 and later Phase 3 tasks were not started.
