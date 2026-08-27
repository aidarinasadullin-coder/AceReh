# Task 6 — Persistence Fixtures and Guards

Date: 2026-08-26
Canonical plan: `docs/architecture-migration/plans/phase-6-project-snapshot-save-boundary.md`
Canonical plan SHA-256: `C56E66D2733D65CC56190A7B95B4D87F5F032AA75E83339925CEB23C2E5A4E92`

## Scope

This task ran the existing persistence compatibility, round-trip, Result API,
snapshot, save-boundary, and negative architecture guard tests. No production
code, DTO/schema, serializer, map, widget, plan, state, or tracked `.smc`
fixture was edited.

## Executable evidence

Command:

```text
dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~ProjectRoundTripTests|FullyQualifiedName~ProjectFileServiceResultTests|FullyQualifiedName~ProjectFileServiceMutationTests|FullyQualifiedName~ProjectFileServiceAtomicityTests|FullyQualifiedName~ProjectPersistenceMapperTests|FullyQualifiedName~ProjectSnapshotFactoryTests|FullyQualifiedName~ProjectSnapshotContractTests|FullyQualifiedName~ProjectSaveServiceTests|FullyQualifiedName~ResultsViewModelOpenProjectTests|FullyQualifiedName~ProjectSessionLegacyStoreGuardTests|FullyQualifiedName~ClimateStateLegacyStoreGuardTests|FullyQualifiedName~ConstructionStateLegacyStoreGuardTests|FullyQualifiedName~ThermalStateLegacyStoreGuardTests|FullyQualifiedName~HydraulicsStateLegacyStoreGuardTests|FullyQualifiedName~CalculationStateServiceGuardTests" --logger "trx;LogFileName=task-6-release.trx" --results-directory docs/architecture-migration/evidence/phase-6-project-snapshot-save-boundary
```

Result: `124 passed / 1 skipped / 0 failed / 125 total`.

TRX: `task-6-release.trx` in this directory.

The single skipped test was `ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`.
It is an explicit fixture-dependent skip because the test expects the legacy
path `D:\IA\ace\Тест\тест 40.smc`, which is absent in this worktree. It did not
fail and was not converted into a passing claim.

The Release test build completed successfully with no reported compiler
warnings or errors.

## Guard coverage

The required negative architecture conditions were exercised by passing tests:

- ViewModel/WPF dependency in the save service:
  `ProjectSaveServiceSource_RejectsViewModelAndWpfReferences`.
- `ProjectData` remains a persistence DTO rather than an architecture owner:
  `PublicProperties_ExcludeLifecycleRuntimeUiAndDateNames`,
  `PublicPropertyTypes_DoNotReferenceViewModelsOrWpf`, and
  `ProjectSnapshot_IsSealedWithoutWritableStateOrLifecycleMutators`.
- Concrete service/ViewModel boundary and canonical save path:
  `ProjectSaveServiceSource_RejectsViewModelAndWpfReferences` and
  `SaveToFileSourceSlice_RejectsSaveCurrentProject`.
- Duplicate snapshot store/owner and independent state ownership:
  `DiIndependentStateRegistration_GuardRequiresProjectSessionOwnership`,
  `DuplicateUpstreamSubscriber_GuardRequiresOneCoordinatorAttachPerSurface`,
  `ResultsNonCanonicalSave_GuardRequiresSessionSnapshotMapper`, and the
  snapshot mutability/contract guards.

## Fixture integrity

Baseline and post-run checks enumerated all `28` tracked `.smc` files, recorded
their sizes and SHA-256 hashes, and verified every fixture was present.
Post-run result: `28` tracked, `0` missing, `0` invalid hashes. The command
`git diff --name-only -- '*.smc'` returned no paths both before and after the
test run. The active `docs/architecture-migration/STATE.json` is absent as
expected; `docs/architecture-migration/archive/STATE.json` exists as
provenance-only archive state.

## Verdict

`TASK 6: PASS`

Acceptance checks passed: compatibility and guard tests had zero failures,
the required skip was identified, and all tracked fixtures remained
byte-for-byte unchanged. The external legacy fixture skip remains a residual
test-environment limitation for the named F5 smoke test only.

## Evidence correction and fresh rerun

The UTF-8-safe fixture evidence is now standalone in
`task-6-fixture-manifest.txt`. It records all `28` tracked `.smc` paths with
byte sizes and SHA-256 values, `MISSING_COUNT=0`, `HASH_INVALID_COUNT=0`, and
`SMC_DIFF_COUNT=0`. Git's default path quoting and console transport had made
the Cyrillic paths appear corrupted in an earlier textual capture; no fixture
was edited.

The required guard and negative-probe identities are listed in
`task-6-negative-probes.txt`. Existing source/ownership guards and invalid
file/result probes are recorded with their passing test identities. No
standalone process probe for an intentionally invalid architecture dependency
fixture exists; that absence is recorded as `STATUS=NOT_PRESENT` rather than
claimed as a nonzero result.

The first post-correction attempt of the exact Release command was blocked by
an unrelated WPF build-file lock (`MarkupCompile.cache`, then the Release
assembly). After no active `dotnet`, `MSBuild`, or `VBCSCompiler` process
remained, the same filter was rerun against the existing Release build with
`--no-build`; the fresh TRX is `task-6-release-correction.trx` and the result
was `124 passed / 1 skipped / 0 failed / 125 total`, exit code `0`. The skip is
`ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile` because
`D:\IA\ace\Тест\тест 40.smc` is absent.

Correction verdict remains:

`TASK 6: PASS`

## Evidence correction and fresh rerun

The UTF-8-safe fixture evidence is now standalone in
`task-6-fixture-manifest.txt`. It records all `28` tracked `.smc` paths with
byte sizes and SHA-256 values, `MISSING_COUNT=0`, `HASH_INVALID_COUNT=0`, and
`SMC_DIFF_COUNT=0`. Git's default path quoting and console transport had made
the Cyrillic paths appear corrupted in an earlier textual capture; no fixture
was edited.

The required guard and negative-probe identities are listed in
`task-6-negative-probes.txt`. Existing source/ownership guards and invalid
file/result probes are recorded with their passing test identities. No
standalone process probe for an intentionally invalid architecture dependency
fixture exists; that absence is recorded as `STATUS=NOT_PRESENT` rather than
claimed as a nonzero result.

The first post-correction attempt of the exact Release command was blocked by
an unrelated WPF build-file lock (`MarkupCompile.cache`, then the Release
assembly). After no active `dotnet`, `MSBuild`, or `VBCSCompiler` process
remained, the same filter was rerun against the existing Release build with
`--no-build`; the fresh TRX is `task-6-release-correction.trx` and the result
was `124 passed / 1 skipped / 0 failed / 125 total`, exit code `0`. The skip is
`ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile` because
`D:\IA\ace\Тест\тест 40.smc` is absent.

Correction verdict remains:

`TASK 6: PASS`
