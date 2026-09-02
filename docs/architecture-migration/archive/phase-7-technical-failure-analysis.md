# Phase 7 Technical Failure Analysis

Date: 2026-08-27
Status: analysis only
Purpose: record the concrete code, type, fixture, and contract mismatches that made the first Phase 7 implementation stumble. This is a technical input to the relaunch plan, not a process retrospective.

## Executive Summary

The first execution repeatedly stopped at boundaries where two representations described the same project value differently. The common pattern was:

```text
fixture or old test expectation
    -> legacy/domain object
    -> persistence DTO or canonical snapshot
    -> adapter projection
    -> assertion expecting another representation
```

The code often compiled, but the test or runtime contract still failed because the value had changed level, identity, ownership, or lifecycle meaning. The new plan must force each boundary to define its source type, target type, normalization rules, and test fixture factory before implementation begins.

## 1. `Concrete` Name vs `Material` Object vs Snapshot

### Symptom

A construction test expected a layer or material value such as `Concrete`, but the setup supplied or returned a `Material` domain object representing concrete. In another place the assertion read a material object while the restored layer exposed `MaterialName` or `MaterialId`.

### Why it happened

The same concept exists at multiple levels:

| Level | Representation |
|---|---|
| catalog/domain | `Material` object, with `Id`, `Name`, lambda limits and metadata |
| persistence | `LayerProjectData.MaterialName`, `MaterialLambda`, `CalculatedLambda` |
| canonical session | `ConstructionLayerSnapshot.MaterialId`, `MaterialName`, thickness and calculated lambda |
| UI adapter | a selected material object or collection entry |
| test assertion | sometimes a string (`"Concrete"`), sometimes a `Material` instance |

The old fixture was written for the domain/catalog path. The new restore contract consumes persisted layer data and creates a canonical snapshot. It does not return the same `Material` instance that the fixture created.

### Technical consequence

Reference equality and object-type equality are invalid at this boundary. A material can be semantically equal while being a different object, or a snapshot can intentionally expose only `MaterialId` and `MaterialName`.

### Relaunch requirement

Every fixture must declare which level it represents. Use separate named factories:

```text
CreateCatalogMaterial()
CreatePersistedLayer(materialName: "Concrete")
CreateCanonicalLayer(materialId, materialName)
CreateUiMaterialSelection()
```

Assertions must be explicit:

- persistence test: assert `MaterialName` and serialized lambda fields;
- restore test: assert `MaterialId`, `MaterialName`, thickness, position and order;
- catalog-boundary test: assert catalog contents/hash and mutation counters;
- adapter test: assert selected `Material` object only where adapter projection is the subject.

Do not use one fixture type for all four boundaries.

## 2. Persisted DTO Is Not Canonical Calculated State

### Symptom

Tests supplied `ThermalProjectData.Result` and expected the loaded result to remain available. The Phase 7 contract instead required a fresh calculation and stated that persisted calculated values must not become canonical current state.

### Why it happened

`ThermalProjectData` contains both persisted inputs and persisted result fields. The DTO shape is a wire-format compatibility shape, not an ownership declaration.

The correct mapping is:

```text
ThermalProjectData input fields
    -> ThermalInputsSnapshot
    -> ProjectSession.ThermalState
    -> exactly one calculation
    -> ProjectSession.ThermalState.Result
```

The saved `ThermalProjectData.Result` is at most compatibility/read data. It is not the result source for a current restore.

### Technical consequence

Old fixtures with a valid saved result caused tests to expect zero calculation or a result copied from the DTO. New tests needed a valid input fixture and a counting calculator returning a fresh result. When that mock was absent or returned an incomplete outcome, restore failed before the intended assertion.

### Relaunch requirement

Provide two explicit fixture families:

```text
CreatePersistedThermalInputsOnly()
CreatePersistedThermalDataWithStaleResult()
CreateFreshThermalCalculationResult()
```

The acceptance test must assert both:

- stale persisted result is ignored as canonical state;
- fresh calculation result is published exactly once.

## 3. `IThermalCalculator` Mock Contract Was Incomplete

### Symptom

Restore tests reached calculation with invalid or missing thermal inputs, or the mock calculator returned a default/null result. The test then failed in restore even though the test was intended to check construction, path, or UI behavior.

### Why it happened

The new coordinator validates thermal input fields before calculation. Existing fixtures had historically relied on ViewModel defaults, prior setup, or a saved result. The coordinator no longer receives those implicit side effects.

### Technical consequence

A fixture that was valid for the old ViewModel-driven load was invalid for the ViewModel-free coordinator. The failure surfaced as a calculation error, not at fixture construction, making the failing test appear unrelated.

### Relaunch requirement

Create one shared `CreateValidRestoreProjectData()` factory with explicit:

- `SelectedMode`;
- `SupplyTemperature`;
- `GroundTemperature`;
- `PipeSpacing`;
- `SelectedPipe`;
- climate data accepted by `IClimateDataService`;
- at least one resolvable construction material;
- valid hydraulics inputs;
- a configured calculator mock returning a non-null fresh result.

Negative tests must mutate exactly one invalid field and state which validation boundary they target.

## 4. `MaterialSnapshot` vs `Material` Catalog Semantics

### Symptom

A custom material existed in `ProjectData.CustomMaterials`, but the restore path either attempted to import it into the global catalog or tests expected the catalog to contain it after open. The new contract requires the custom material to be used as persisted project input without mutating the global catalog.

### Why it happened

`MaterialSnapshot` is a portable persistence record. `Material` is a mutable/catalog domain object. The old path treated project custom materials as an import operation. Phase 7 changed the boundary to a non-mutating restore lookup.

### Technical consequence

The same material name can be present in:

- global catalog;
- project custom material snapshots;
- restored layer snapshot.

Name lookup precedence and missing-material behavior therefore matter. A test that only checked a final material name did not prove whether the catalog was mutated or whether the persisted custom record was used.

### Relaunch requirement

Test three separate outcomes:

1. built-in name resolves to the existing catalog material without mutation;
2. custom persisted name resolves to the project snapshot and does not enter the global catalog;
3. missing name fails before canonical commit and leaves clean/default state.

Use repository spies and before/after file hashes. Do not assert only the layer name.

## 5. `ConstructionLayerSnapshot` Identity Is Newly Generated

### Symptom

Round-trip or restore assertions compared layer object identity or expected persisted IDs to remain unchanged. The restore implementation creates a new `Guid` for each canonical layer and normalizes order.

### Why it happened

The wire DTO does not persist the canonical snapshot `Guid`. The canonical layer identity is runtime state; persistence identity is represented by material name/ID, position, order, and values.

### Technical consequence

Reference or `Guid` equality is not a valid current-format restore assertion. Equality must be field-based over the persisted contract plus the documented normalization.

### Relaunch requirement

Define equality helpers before writing restore tests:

```text
AssertPersistedLayerEquivalent(dto, snapshot)
AssertCanonicalLayerEquivalent(expected, actual)
```

These helpers must ignore runtime-only `Guid` values and explicitly check `MaterialId`, `MaterialName`, thickness, position, order, lambda and override semantics.

## 6. `IsLambdaOverridden` Was Not the Same as `CalculatedLambda`

### Symptom

Fixtures supplied a saved `CalculatedLambda` and an override flag. Restore preserved the calculated numeric value but intentionally cleared the override flag so future groundwater changes can recalculate lambda. Assertions expecting both old fields unchanged failed.

### Why it happened

The numeric value and the ownership/control flag have different lifecycle semantics. One is a persisted denormalized value; the other controls future recalculation behavior.

### Technical consequence

Testing the two fields as one bundle encoded the old behavior and contradicted the new restore rule.

### Relaunch requirement

Add a dedicated contract table:

| Field | Restore rule |
|---|---|
| `CalculatedLambda` | preserve persisted numeric value where required by wire behavior |
| `IsLambdaOverridden` | reset according to the approved restore semantics |
| future groundwater edit | recalculate when override is cleared |

The plan must require one restore assertion and one post-restore mutation assertion.

## 7. Pipe DTO vs `PipeType` vs `ThermalPipeSnapshot`

### Symptom

Thermal tests supplied or expected a `PipeType`, while persistence contains `PipeTypeProjectData` and canonical state contains `ThermalPipeSnapshot`. Matching by object identity or by non-wire fields caused failures.

### Why it happened

`PipeType.Equals` compares the wire-relevant structural fields (`Name`, outer diameter, inner diameter, wall thickness), while other domain fields such as article or conductivity are not serialized in the same contract.

### Technical consequence

A persisted pipe is reconstructed and resolved against `PipeType.StandardPipes`; it is not the same instance that may have existed in a ViewModel fixture. An unavailable pipe can follow the frozen fallback rule.

### Relaunch requirement

Define explicit pipe fixture and assertion helpers:

- DTO structural equality;
- canonical snapshot structural equality;
- resolved standard-pipe equality;
- unavailable-pipe/fallback behavior.

Do not compare UI-selected object references in coordinator tests.

## 8. Null DTO Defaults Hid Invalid Fixtures

### Symptom

Tests using `new ProjectData()` appeared structurally valid because DTO properties have default objects, but the data was not calculable or did not contain a resolvable city/material/pipe.

### Why it happened

Object construction defaults satisfy CLR nullability but not application validity. The restore coordinator validates at the application boundary.

### Technical consequence

Tests failed deep inside restore with messages about city, material, thermal calculation, or hydraulics, obscuring the original invalid fixture.

### Relaunch requirement

Ban bare `new ProjectData()` in success-path restore tests. Require named factories:

```text
CreateValidCurrentProjectData()
CreateInvalidClimateProjectData()
CreateMissingMaterialProjectData()
CreateInvalidThermalProjectData()
```

Each negative fixture must identify its first expected failure boundary.

## 9. Restore Order and Adapter Refresh Order Were Confused

### Symptom

Tests expected ViewModel properties to be updated during each module mutation, while the new contract requires canonical session commit first and adapter projection only after successful calculation/restore completion.

### Why it happened

The old orchestrator interleaved ViewModel mutation, catalog reload, adapter refresh, and calculation. The new boundary separates canonical state from UI projection.

### Technical consequence

Intermediate adapter observations became invalid. An assertion made between climate commit and thermal calculation could see an intentionally incomplete UI projection.

### Relaunch requirement

Test two levels separately:

- coordinator: ordered canonical slice mutations and calculation;
- UI entrypoint: projection refresh after success, no projection commit on failure.

Use event/order spies rather than asserting transient ViewModel state.

## 10. Hydraulics Restore Was Previously Performed Twice

### Symptom

Existing round-trip tests depended on a second hydraulics restore that reapplied saved results after the fallback calculation. The new contract requires one ordered canonical restore and a fresh calculation path.

### Why it happened

The old orchestrator first restored hydraulics inputs/results, then performed finalization and restored hydraulics again to retain the file result.

### Technical consequence

Tests that counted restore operations or expected saved hydraulic results to win over current calculation conflicted with the new exactly-once boundary.

### Relaunch requirement

Add an explicit before/after contract:

- four canonical input commits occur once in deterministic order;
- one application-level calculation occurs after commit;
- no second hydraulics restore is allowed;
- derived current results come from the calculation publication path.

## 11. Report Builder Input Type Stayed at the Wrong Boundary

### Symptom

Report tests passed a `ProjectData` DTO and validated its persisted calculated values, while Phase 7 required report data from the current session snapshot.

### Why it happened

The report API still had the old signature and its builders were coupled to DTO fields. The restore migration changed ownership without completing the report migration.

### Technical consequence

Stale saved result fields continued to be visible in reports. A green report test proved only DTO rendering, not current-session reporting.

### Relaunch requirement

Introduce and test an explicit `ProjectSessionReportSnapshot` or equivalent immutable contract before changing the exporter. The test must:

1. load DTO with deliberately stale calculated fields;
2. calculate/mutate the session to different values;
3. export without recalculation;
4. assert builder input and rendered values come from session;
5. assert session remains unchanged.

## 12. Mocked Service Shape vs Real DI Shape

### Symptom

Unit fixtures constructed `ProjectLoadOrchestrator` with optional or legacy dependencies, while production DI supplied the new coordinator. A test could therefore exercise the old fallback path or fail because the coordinator was null.

### Why it happened

The adapter was made compatible with old constructors while the real architecture required a single configured coordinator. This created two testable runtime shapes.

### Technical consequence

Passing unit tests did not prove that the DI graph used the same restore path. Failures appeared only in integration/open-project tests.

### Relaunch requirement

Add a DI smoke test that resolves the real `ResultsViewModel` and restore services, then assert:

- one coordinator instance is resolved;
- the orchestrator delegates to it;
- no fallback restore path is selected;
- no concrete ViewModel is injected into the coordinator.

Avoid optional production dependencies unless they are explicitly part of the approved contract.

## 13. Rollback Reset Used a Different Construction Path

### Symptom

Failure tests expected the pre-restore snapshot or a clean/default state, but rollback used `ConstructionDefaultStateInitializer` with current groundwater and different origin semantics. The resulting state was not the same representation as the test's expected snapshot.

### Why it happened

The plan's DEC-003 contract selected clean/default rollback rather than reversible restoration of arbitrary prior state. The implementation and tests were not aligned on which one was authoritative.

### Technical consequence

A test could incorrectly report rollback failure when the implementation intentionally discarded the old project and returned to defaults, or could miss mixed state if it checked only one slice.

### Relaunch requirement

Choose and state one rollback contract before implementation:

```text
failure -> all four canonical slices clean/default
```

Then assert all four slices, status/result fields, guard state, path and dirty semantics. Do not compare only the slice where the injected failure occurred.

## 14. User-Visible Error vs Internal Failure Result

### Symptom

Coordinator returned `ProjectRestoreResult.Failure(message)`, while the existing UI path expected an exception to propagate and display a dialog. Tests alternated between checking a returned failure and expecting a thrown exception.

### Why it happened

The new coordinator introduced a result object, but the surrounding adapter contract still had exception-based failure semantics.

### Technical consequence

The failure was technically recorded but not necessarily surfaced through the existing user-visible path. A test could pass at service level while UI behavior remained wrong.

### Relaunch requirement

Define the error boundary explicitly:

- coordinator returns a typed failure or throws, but not an accidental mixture;
- adapter translates that result to the existing UI error contract;
- tests cover both service result and real entrypoint behavior.

## 15. Global AppData Made Test Results Order-Dependent

### Symptom

The full serial Release suite passed, while parallel execution failed in `MainViewModelTests.TearDown` when tests concurrently manipulated `%APPDATA%\SnowMeltingCalculator\settings.json`.

### Why it happened

The test fixture uses process-global filesystem state. This is independent of the restore domain but affects confidence in full-suite evidence.

### Technical consequence

Parallel failures looked like Phase 7 regressions although the serial authoritative run was green.

### Relaunch requirement

Declare the authoritative command and environment before execution. If isolation is not part of scope, use one worker and record the parallel race as residual risk. Never mix parallel failure output into a product failure verdict without reproduction in the authoritative mode.

## 16. Confirmed Execution Evidence From Historical TRX Runs

The preceding sections describe the contract classes. The historical TRX files
confirm that these were observed failures, not only inferred risks. The focused
Phase 7 run `phase7-focused.trx` contained `139 passed`, `31 failed`, and `0
skipped`. Representative failures are grouped below by the boundary they
actually exercised.

### Invalid success fixtures reached restore validation

The following tests failed with the same restore validation message:

```text
OperatingMode must be a defined value.; SupplyTemperature must be between 20 and 90.
```

Confirmed test names included:

- `ResultsViewModel_LoadProjectData_RestoresCityAndClimateParameters`;
- `LoadProjectFromPathAsync_WhenSuccess_LoadsDataAndSetsCurrentFilePath`;
- `OpenProject_WhenDirty_ShowsReplacePrompt`;
- `OpenProject_WhenClean_DoesNotShowPrompt`;
- `ProjectData_Load_v1_0_MigratesAbovePipeOrder`;
- `ProjectData_Load_ImportsCustomMaterialsBeforeLayers`;
- `ResultsViewModel_LoadProjectData_SyncsClimateToSingletonData`.

These tests were aimed at climate, file-path, prompt, legacy-order, or catalog
behavior, but their payloads did not satisfy the thermal restore contract. This
is direct evidence that a broad UI test can fail before reaching its intended
assertion. The relaunch must construct a valid complete payload first, then
mutate exactly one field for a negative case.

### Saved-result semantics and calculation multiplicity disagreed

The run contains several direct contradictions between old assertions and the
fresh-calculation contract:

- `LoadProjectData_SecondLoadWithoutSavedResult_ReplacesAllThermalStaleValues`
  expected persisted `PowerTotal = 777`, but the restored state contained
  `42.5`;
- `Restore_ValidSavedResult_CalculatorZeroResultSurvivesLoadClean` expected
  `PowerTotal = 777` and zero calculator calls, but received `PowerTotal = 555`,
  one calculator call, two thermal state notifications, and four context
  publications instead of two;
- `Restore_AbsentSavedResult_FallbackCalculatesExactlyOnce` expected two context
  publications, but received four;
- `SecondProjectLoad_ReplacesAllThermalState_CalculatesFallbackOnce` expected
  one calculator invocation, but received two;
- `PersistenceFailure_UnknownPipe_FallsBackToFirstStandard_NoSchemaDrift` and
  `Restore_UnknownPersistedPipe_FallsBackToFirstStandardPipe` expected the
  saved result or no calculation, while the actual path calculated and exposed
  a fresh result (`42.5` or `555`, depending on fixture).

The evidence identifies two separate contracts that had been mixed: persisted
calculated values may be present in the DTO, while current canonical result
publication is produced by the restore calculation path. The relaunch must
assert the fresh result value, invocation count, state notifications, and
context publications independently.

### Mapping and identity assertions were representation-incompatible

The following observed values confirm the identity problems described above:

- `ProjectRoundTrip_PreservesLambdaValueButResetsOverrideFlag` received
  `IsLambdaOverridden = true` although the restore contract expected `false`;
- `ProjectRoundTrip_LambdaUpdatesWhenGroundwaterLevelChanges` and its
  `AfterOverride` variant expected `1.6`, but received the persisted `1.5`;
- `ProjectRoundTrip_FieldCompleteRoundTrip_SecondLoadReplacesProjectA` expected
  material IDs `[5, 1]` and `[2, 5]`, while the restored snapshots contained
  `5` in the mismatching positions; the same test also observed persisted
  override flags instead of reset flags;
- `ProjectRoundTrip_CustomTemplateSurvives` expected the custom template in the
  adapter collection, but it was absent;
- `ProjectRoundTrip_LiveMutationsAreSavedLoadedAndExportedWithoutResultsCalculation`
  failed because `Live construction material` was not found in either the
  catalog or project custom-material snapshots;
- `Restore_NullPersistedPipe_PipeRemainsNullAfterLifecycleReset` expected a
  null pipe and persisted result `777` with zero calculations, but received the
  first standard pipe, result `555`, and one calculation.

These are not interchangeable assertion failures. They cover runtime-generated
layer identity, material lookup precedence, pipe fallback, catalog/template
import policy, lambda ownership, and saved-result semantics. Each needs its own
field-level equality helper and fixture factory.

### Failure boundary and UI error semantics were inconsistent

`LoadProjectDataAsync_EarlyRestoreFailure_ClearsLeasePreservesPartialThermalDefaults`
and `LoadProjectDataAsync_LateRestoreFailure_ClearsLeaseThermalRetainsPreFailureDefaults`
expected an `InvalidOperationException`, but no exception was observed in the
historical run. In contrast, the current coordinator returns a typed
`ProjectRestoreResult.Failure`, while `ProjectLoadOrchestrator` translates that
result into an exception for the existing `ResultsViewModel` path. This confirms
that service-level and UI-entrypoint tests were asserting different error
contracts. The relaunch must test the result boundary and the real entrypoint
translation separately, including lease release and all-four-slice rollback.

### Dirty and event multiplicity changed during correction attempts

The historical hydraulics characterization runs also recorded exact
multiplicity drift. In `task-9-h5-final-2.trx`, four global-input cases expected
two dirty calls and received one. In `task-9-h5-final-4.trx`, the same four cases
expected two and received three. The affected inputs were
`GlycolType`, `SupplySpacing_cm`, `SupplyHeatPercent`, and
`GlycolConcentration`. This is evidence of competing writers/subscribers, not
just a value mismatch. The relaunch must reset counters before each logical
action and define whether dirty/calculation counts are per setter, per
canonical mutation, or per user action.

### What the evidence does and does not prove

The TRX evidence proves the historical assertions and received values above.
It does not by itself prove that every expected value was the correct final
contract; some assertions explicitly encoded superseded behavior, especially
saved-result retention, null/unknown pipe handling, custom catalog import, and
override preservation. Those rows must therefore remain historical RED evidence
until the relaunch contract matrix chooses the authoritative behavior and adds a
new focused assertion for it.

## Technical Relaunch Checklist

Before implementation:

- define domain, DTO, canonical snapshot, adapter, and report fixture factories;
- define field-level mapping tables for all four slices;
- define identity/equality rules for materials, layers, and pipes;
- define stale calculated DTO behavior;
- define rollback state and error boundary;
- define the real DI graph and test seam;
- define valid success fixtures independently of `new ProjectData()` defaults.

Before each production slice:

- add a RED test at the exact boundary;
- prove the test fails for the intended reason;
- record the expected type/value/order contract;
- implement the smallest change;
- run the focused test and inspect the actual received value/type;
- write a receipt before proceeding.

Before final gates:

- run a full current-format round-trip;
- run stale-result report export;
- run missing material, invalid thermal, commit failure, calculation failure and cancellation cases;
- prove catalog hashes and mutation counters;
- prove four-slice rollback and guard release;
- run the real DI/UI entrypoint;
- use serial full Release as authoritative unless global state is isolated.

## Recommended Plan Amendment

The replacement Phase 7 plan should add an explicit pre-implementation task named `Contract and Fixture Matrix`. Its acceptance criteria should require:

1. a table for every persisted field and its canonical target;
2. a fixture factory for each representation level;
3. equality helpers that do not compare invalid object identity;
4. a counting calculator and failure seams;
5. a report snapshot seam;
6. a catalog spy/hash harness;
7. one RED test for every failure class;
8. an intermediate review confirming that the tests fail for contract reasons, not malformed setup.

The replacement plan should then execute in this order:

```text
Contract/fixture matrix
-> RED boundary tests
-> candidate mappings
-> ordered commit/rollback
-> coordinator/calculation
-> catalog boundary
-> report snapshot
-> DI/UI projection
-> maps and evidence
-> F1-F4
```

## Final Conclusion

The recurring technical problem was not simply that tests were missing. It was that tests, fixtures, and production code disagreed about which representation was authoritative at each boundary. `Concrete` is a useful example: a string name, a catalog `Material`, a persisted `MaterialSnapshot`, and a canonical `ConstructionLayerSnapshot` are related values, but they are not interchangeable objects.

The relaunch plan must make those distinctions executable before implementation. If every boundary has a named fixture, mapping table, equality rule, failure seam, and focused RED test, the implementation can proceed without discovering representation mismatches late in broad ViewModel tests.
