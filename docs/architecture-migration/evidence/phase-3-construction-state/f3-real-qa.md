# Phase 3 Final Verification F3: Real QA — Mandatory Scenario Execution

Receipt date: `2026-08-20`

## Scope

This is the real-QA gate for Phase 3 Final Verification Wave F3. It verifies
that the mandatory lifecycle/standalone scenarios execute and pass against the
current source tree. Per the bounded-closure instruction, this receipt is
produced by **running existing test filters only** — no new production code and
no new tests were created; this is a run-and-compare exercise.

The prior F3 rejection was for *missing* mandatory coverage:
standalone corrupt/load/save/import failure and field-complete
round-trip/second-load. Those test surfaces were added in the prior correction
session (Todo 1). This receipt confirms they now exist, execute, and pass, and
compares the resulting counters.

## Test run 1 — mandatory F3 scenario filter (Debug)

Command:

```powershell
dotnet test tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj -c Debug --nologo `
  --filter "FullyQualifiedName~StandaloneLoadConstruction|FullyQualifiedName~StandaloneSaveConstruction|FullyQualifiedName~ProjectRoundTrip_FieldCompleteRoundTrip|FullyQualifiedName~ProjectRoundTrip_CitySurvivesRealSaveLoad|FullyQualifiedName~ProjectRoundTrip_PreservesHasLoads|FullyQualifiedName~ProjectRoundTrip_DoesNotMarkDirtyOnLoad|FullyQualifiedName~ProjectRoundTrip_LiveMutationsAreSavedLoadedAndExportedWithoutResultsCalculation|FullyQualifiedName~SaveProjectResultAsync_OnIoFailure|FullyQualifiedName~LoadProjectResultAsync_OnMissingFile|FullyQualifiedName~LoadProjectResultAsync_OnCorruptJson|FullyQualifiedName~SaveAndLoadProjectResult_RoundTripsCollectorAndCircuitResults" `
  --logger "trx;LogFileName=f3-real-qa.trx"
```

TRX: `tests/SnowMeltingCalculator.Tests/TestResults/f3-real-qa.trx`

Result: **passed 15, failed 0, skipped 0, total 15**.

Executed mandatory scenarios (verified by name from the TRX):

| # | Test | Mandatory scenario |
| --- | --- | --- |
| 1 | `StandaloneLoadConstruction_CorruptJson_PreservesCanonicalSnapshotAndPublishesNoCompletion` | standalone **corrupt** |
| 2 | `StandaloneLoadConstruction_LoadFailure_PreservesCanonicalSnapshotAndPublishesNoCompletion` | standalone **load failure** |
| 3 | `StandaloneLoadConstruction_RepositoryReturnsNull_IsSilentNoOp` | standalone load failure (null) |
| 4 | `StandaloneLoadConstruction_ImportFailure_ThroughRealServicePreservesCanonicalState` | standalone **import failure** |
| 5 | `StandaloneSaveConstruction_Success_DoesNotCallMarkDirtyAndClearsHasUnsavedChanges` | standalone **save** |
| 6 | `StandaloneSaveConstruction_SaveFailure_PreservesCanonicalSnapshotDirtyAndCompletionState` | standalone **save failure** |
| 7 | `ProjectRoundTrip_FieldCompleteRoundTrip_SecondLoadReplacesProjectA` | **field-complete round-trip + second-load** |
| 8 | `ProjectRoundTrip_CitySurvivesRealSaveLoad` | round-trip |
| 9 | `ProjectRoundTrip_PreservesHasLoads` | round-trip |
| 10 | `ProjectRoundTrip_DoesNotMarkDirtyOnLoad` | round-trip |
| 11 | `ProjectRoundTrip_LiveMutationsAreSavedLoadedAndExportedWithoutResultsCalculation` | round-trip |
| 12 | `SaveProjectResultAsync_OnIoFailure_ReturnsFailureWithMessage` | file-service save failure |
| 13 | `LoadProjectResultAsync_OnMissingFile_ReturnsFailureWithFileNotFound` | file-service load failure |
| 14 | `LoadProjectResultAsync_OnCorruptJson_ReturnsFailureWithDeserializationError` | file-service **corrupt** |
| 15 | `SaveAndLoadProjectResult_RoundTripsCollectorAndCircuitResults` | round-trip |

All 15 passed. The previously-missing mandatory coverage (standalone
corrupt/load/save/import failure and field-complete round-trip/second-load) is
now present and green.

## Test run 2 — dedicated round-trip class (Debug)

Command:

```powershell
dotnet test tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj -c Debug --nologo `
  --filter "FullyQualifiedName~ProjectRoundTripTests" `
  --logger "trx;LogFileName=f3-roundtrip-class.trx"
```

TRX: `tests/SnowMeltingCalculator.Tests/TestResults/f3-roundtrip-class.trx`

Result: **passed 9, failed 0, skipped 0, total 9** (the `ProjectRoundTripTests`
class, excluding its `SetUp` fixture). This confirms the broader round-trip
suite — including `Load_v1_Fixture_PreservesCanonicalFields`,
`SaveThenLoad_NewProject_RoundTripsFields`, `FullProject_RoundTrip_PreservesAllCircuitResultDetails`,
and `ProjectRoundTrip_TwoCollectors_PreservesPerCollectorSummaries` — is green.

## Counter comparison

- Prior F3 state: REJECTED — mandatory standalone failure/import and
  field-complete round-trip coverage absent (0 executable tests for those
  scenarios).
- Current F3 state: the same mandatory scenarios now have **15 executable,
  passing tests** (0 failed, 0 skipped) plus a green 9-test round-trip class.
- No new production code or new tests were introduced by this receipt; only
  existing filters were executed.

## Conclusion

The mandatory F3 real-QA scenarios execute and pass against the current tree.
The standalone corrupt/load/save/import failure surface and the field-complete
round-trip/second-load surface are both covered and green. Counters are
consistent with the corrected coverage: 15/15 (focused) and 9/9 (round-trip
class), zero failures, zero unexpected skips.

VERDICT: APPROVE
