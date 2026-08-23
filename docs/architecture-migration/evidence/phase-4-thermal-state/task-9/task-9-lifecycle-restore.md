# Task 9 Receipt — Route lifecycle reset, project restore and fallback calculation through ThermalState

Plan: `docs/architecture-migration/plans/phase-4-thermal-state.md` (frozen), Todo 9.
Base: `6a5a96f1763dd952c8d772ecd1d2536eb3b804cf`, branch `master`. Write-set:
task-9/allowed-hunks.json = task-8's 29 entries + 7 new (36 total).

**STATUS: GREEN — RESOLVED-AMZ2.** Owner decision AMZ-2 (journal entry 2026-08-23 in
`TASK_CONTEXT.md`) authorized updating exactly the two pre-Todo-9 quirk pins in
`ThermalMultiplicityCharacterizationTests.cs` to DEC-T08 targets. Full Release suite:
**1924 parser total / 1921 passed / 0 failed / 3 NotExecuted (identities == baseline).**
All gates green.

## 1. Implemented changes

### Production
1. **NEW `src/Services/Project/ThermalPersistenceMapper.cs`** — pure restore-half mapper
   (DEC-T08). Save-half deferred to Todo 10 per plan.
   - `BuildInputsCandidate(ThermalProjectData?, IReadOnlyList<PipeType>)` → `ThermalInputsSnapshot`
     (mode/supply/ground verbatim; pipe resolved against standard catalog; spacing from DTO,
     legacy-missing ⇒ DTO initializer 200; null DTO ⇒ `ThermalInputsSnapshot.Default`).
   - `ResolveStandardPipe(PipeTypeProjectData? | ThermalPipeSnapshot?, pipes)` — structural match
     (`PipeType.Equals`: Name+Outer+Inner+Wall) ⇒ matching **standard definition** (Article/λ from
     catalog); unknown ⇒ first standard fallback; null ⇒ null.
   - `BuildSavedResult(ThermalResultProjectData?)` → `ThermalResultSnapshot?`; null/invalid ⇒ null
     (**invalid saved result never becomes canonical**). Exactly the eight wire fields
     (PowerUp/PowerDown/PowerTotal/Supply/Return/Mean/DeltaT/IsValid); runtime-only fields CLR-default.
   - `ToDomainResult(ThermalResultSnapshot)` for adapter publication.
   - Wire format / `ProjectData.cs` DTOs untouched (read-only).
2. **`src/Services/Project/ProjectLoadOrchestrator.cs`**
   - New `_thermalState` (= `session.ThermalState`) alongside climate/construction slices.
   - Thermal restore section rewritten: mapper builds candidate + saved result → canonical
     `Restore(candidate, saved)` (origin pinned `ProjectLoad` inside state). On atomic
     **rejection** (corrupt/out-of-range candidate): re-`Restore(ThermalInputsSnapshot.Default,
     saved)` — zero stale project-A values while a file's *valid* saved result is still honored
     (legacy observable preserved); absent/invalid saved ⇒ finalization falls back exactly once.
     Frozen validation NOT widened.
   - Compatibility surface kept: `SetPipeSpacing(candidate.PipeSpacing,
     "ProjectLoadOrchestrator.RestoreModules")` AFTER Restore ⇒ value-equal no-op (zero events),
     source-guard satisfied under load lease.
   - Adapter refresh from canonical candidate (mode/supply/ground/pipe/spacing) — under load guard
     these create no user mutations.
   - Finalization reads ONLY canonical state: `_thermalState.Snapshot.Result is { IsValid: true }`
     ⇒ one `LoadResult(ToDomainResult(...))` publish (calculator 0); else ONE
     `CalculateCommand.ExecuteAsync(null)` fallback. Hydraulics/circuit restore order unchanged.
   - `ResetModules()`: added canonical `_thermalState.ResetToDefaults(ProjectLoadReset)` before
     adapter `_thermalViewModel.Reset()` (non-user origin, no dirty from state class).
3. **`src/ViewModels/Shell/MainViewModel.cs`** — only the sanctioned reset-call wiring:
   `_thermalState` field + constructor assignment (mirrors existing `_climateState`/
   `_constructionState` pattern) and `PerformNewCalculationReset()` now calls
   `_thermalState.ResetToDefaults(ThermalMutationOrigin.ProjectLoadReset)` before
   `_thermalViewModel.Reset()`.

### Second-load stale-result fix (recorded Todo 2 divergence)
Old path set VM fields directly and finalized on `_thermalViewModel.Result` — project A's result
object survived a B-load without saved result and was re-published (fallback skipped, hydraulics
surplus). Now every load performs a canonical four-component replacement (`Restore` covers
inputs/result/status atomically); finalization consults canonical state only. Proven by
`RestoreModulesFromProjectAsync_SecondLoadWithoutSavedResult_ReplacesAllThermalStaleValues`,
`LoadProjectData_SecondLoadWithoutSavedResult_ReplacesAllThermalStaleValues` (public boundary,
ends clean), and multiplicity rows.

## 2. DEC-T08 matrix coverage

| Row | Outcome | Evidence |
|---|---|---|
| valid saved result → calculator 0 / invalidation 0 | PASS | `ProjectLoad_DoesNotInvalidateRestoredThermalResult`; `LoadProjectData_KpiReflectSavedThermalResult_...`; `SecondLoadWithoutSavedResult_...` stage-A sanity (777 published, 0 calc) |
| absent → calculator 1 + one successful fallback publish | PASS | `ProjectLoadWithoutSavedThermalResult_CalculatesOnceWithoutClimateInvalidation`; `LoadProjectData_MissingOrInvalidThermalResult_...`; lifecycle/results second-load rows |
| invalid saved → calculator 1, invalid not final | PASS | mapper `BuildSavedResult_InvalidResult_IsNotCanonical`; `MissingOrInvalidThermalResult_...` (invalid branch) |
| pipe match → matching standard definition | PASS | `BuildInputsCandidate_PersistedPipeMatchesStandard_...` (Article/λ from catalog); `ProjectRoundTrip_PipeSelectionRestored` |
| pipe unknown → first standard fallback | PASS | `BuildInputsCandidate_PersistedPipeUnknown_...`; `ResolveStandardPipe_FromSnapshot_...` |
| pipe null → stays null after lifecycle reset | PASS | `BuildInputsCandidate_PersistedPipeNull_...`; second-load rows assert `Inputs.Pipe == null` |
| missing legacy spacing → 200 | PASS | `BuildInputsCandidate_MissingLegacySpacing_DefaultsTo200` |
| second load → zero stale values | PASS (prod) / **BLOCKED pin** | two new zero-stale rows green; contradicted pin `SecondProjectLoad_...UntilTodo9` — see blocker §5 |
| repeated load/reset → no multiplication | PASS (prod) / **BLOCKED pin** | `RepeatedResetAndLoadCycles_DoNotMultiply...` (steady-state completions/calcs equal, 1 calc/cycle); `RepeatedResetAndLoad_DoesNotMultiplyClimateOrThermalEvents`; contradicted pin `LifecycleResetModules_IsSilentForThermalAndDoesNotDirty` |
| restore exception → lease clears, characterized partial state | PASS | `[RestoreFailure] RestoreModulesFromProjectAsync_ThermalBoundaryException_...`; existing Early/Late failure rows green |

## 3. Gates

| Gate | Command (repo root, pwsh) | Exit | Result |
|---|---|---|---|
| V0 state/plan | `node docs/architecture-migration/workflow/validate-state.mjs validate --check-plan` | 0 | valid=true, executing |
| G0 protected-pre | `verify-protected-baseline.ps1 -Baseline …task-1/baseline-manifest.json -AllowedHunks …task-9/allowed-hunks.json -EvidenceRoot … -Output …task-9/protected-pre.json` | 0 | mismatch 0, allowed 36 |
| G1 Debug build | `dotnet build src/SnowMeltingCalculator.csproj -c Debug --nologo` | 0 | 0 warn / 0 err |
| G1 Release build | `dotnet build src/SnowMeltingCalculator.csproj -c Release --nologo` | 0 | 0 warn / 0 err |
| G1 test Release build | `dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Release --nologo` | 0 | 0 warn / 0 err |
| G2 filtered lifecycle | `dotnet test … -c Release --no-build --filter "FullyQualifiedName~ProjectLifecycleFlowCharacterizationTests\|FullyQualifiedName~ClimateThermalInvalidationRegressionTests\|FullyQualifiedName~ResultsViewModelOpenProjectTests" --logger "trx;LogFileName=phase-4-lifecycle.trx" --results-directory …task-9/TestResults` | 0 | 54 total / 53 passed / 0 failed / 1 NotExecuted |
| G3 full Release | `dotnet test … -c Release --no-build --logger "trx;LogFileName=phase-4-full-release.trx" --results-directory …task-9/TestResults` | 0 | 1924 parser total / 1921 passed / **0 failed** / 3 NotExecuted (== baseline identities) |
| G4 protected-post | same verifier, `-Output …task-9/protected-post.json` | 0 | mismatch 0, allowed 36 |

TRX SHA-256:
- `phase-4-lifecycle.trx`: `C881C189A0CC42180CD68624A70D1A4BD2822F22C06586EE835D71D2A1AE5BA1`
- `phase-4-full-release.trx` (final, post-AMZ2): `66E5A2E5582866D4642AA0B911FF2146FA238EC2F23169168D212874C9E73054`

Arithmetic vs baseline (1909/1906/0/3): +15 new tests (mapper 10, lifecycle 3, climate 1,
results 1) ⇒ parser total 1924, passed 1921 = 1906+15, failed 0, NotExecuted identities
identical to baseline (`RegenerateBaseline`, `RegenerateCircuitsBaseline`,
`ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`). Details:
`task-9/arithmetic.json`.

## 4. Post-mortems (first G3 run had 9 failures)

1. **Production defect (fixed)**: corrupt-candidate rejection left prior project's valid result
   canonical; finalization re-published it and skipped fallback (broke
   `LoadProjectData_MissingOrInvalidThermalResult_…`, calculator expected 1 got 0). Fix: reject
   ⇒ `Restore(Default-inputs, saved)` (shape G) — zero stale, valid saved results of
   invalid-input files still honored, fallback-once preserved. This also restored the
   CollectorEquipmentItems ×5 + RefreshAll ×2 cluster (their fixtures load ready-data with
   out-of-range supply=0 and pin saved-result publication).
2. **Sanctioned pin update (allow-listed)**:
   `ResultsStabilizationPhase1BehaviorContractsTests.ProjectLoadOrchestrator_PreservesLoadOnlyThermalFallbackBoundary`
   pinned the old VM-field finalization source string; updated to the new canonical boundary
   string with a Todo 9 referencing comment. The fallback invocation pin is unchanged.
3. **Test-expectation corrections (my new orchestrator-direct rows)**: removed direct
   `session.IsDirty` assertions; adapter-level collector-clear transiently marks dirty
   (pre-existing baseline behavior masked by caller-side `MarkClean()` in
   `ResultsViewModel.LoadProjectDataAsync` and the Climate fixture convention). Cleanliness is
   pinned at the public boundary by the passing Results-flow second-load row.

## 5. RESOLVED-AMZ2 — former blocker: untouchable-file pins contradicted Todo 9 acceptance

Two characterization rows in
`tests/SnowMeltingCalculator.Tests/Services/Project/ThermalMultiplicityCharacterizationTests.cs`
pinned pre-Todo-9 behavior that the frozen plan's Todo 9 action/acceptance explicitly replaces;
no production shape could satisfy both sides:

1. `SecondProjectLoad_ReplacesProjectAInputsButKeepsStaleResultUntilTodo9` (was line 1147)
   pinned project A's valid result SURVIVING a second load without saved result (777 kept,
   calculator 0) — mutually exclusive with DEC-T08 «second load»: zero stale values, calculator 1.
   The test's own name declared its temporality («UntilTodo9»).
2. `LifecycleResetModules_IsSilentForThermalAndDoesNotDirty` (was line 390) pinned thermal
   silence of `ResetModules` including service spacing store staying 250 — mutually exclusive
   with canonical defaults (spacing 200) under sole canonical ownership.

**Owner decision AMZ-2** (journal entry 2026-08-23 appended to `TASK_CONTEXT.md`) granted the
two-row update; executed exactly as scoped:

1. Renamed to `SecondProjectLoad_ReplacesAllThermalState_CalculatesFallbackOnce`; now pins:
   project-B inputs fully replace A's, fresh fallback result (555.0) replaces A's saved result,
   calculator invoked exactly once, canonical status `Actual`, session clean. Comment:
   `// AMZ-2 (2026-08-23): row updated from pre-Todo-9 quirk pin to DEC-T08 second-load target.`
2. Kept name `LifecycleResetModules_IsSilentForThermalAndDoesNotDirty` (silence-for-dirty is
   still true); updated thermal assertions: exactly one canonical completion at
   `ThermalState.Changed`, canonical + service-getter spacing == 200 after reset, zero dirty
   intents/transitions, legacy surface (`Recorder.ThermalStates`/context publications) silent,
   zero hydraulics delta, zero calculator invocations. Same AMZ-2 comment.

Supporting production change (allow-listed file, required to make row 2's preserved-silence
assertions true while keeping canonical ownership): `CalculationStateService.OnThermalStateChanged`
now suppresses the legacy-surface translation (`StateChanged`/`PipeSpacingChanged`) for mutations
with origin `ProjectLoadReset` only — lifecycle reset keeps its frozen observable silence while
the canonical completion remains visible on `ThermalState.Changed`. Load-path origins
(`ProjectLoad`) and user/calculation origins are untouched.

No other row of `ThermalMultiplicityCharacterizationTests.cs` was changed (verified by full-suite
green run incl. `RepeatedLoadResetCycles_...`, `Restore_AbsentSavedResult_...` and all other rows).

## 6. Files changed (worker-owned)

- NEW `src/Services/Project/ThermalPersistenceMapper.cs`
- `src/Services/Project/ProjectLoadOrchestrator.cs`
- `src/ViewModels/Shell/MainViewModel.cs` (reset call wiring only)
- NEW `tests/SnowMeltingCalculator.Tests/Services/Project/ThermalPersistenceMapperTests.cs`
- `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectLifecycleFlowCharacterizationTests.cs`
- `tests/SnowMeltingCalculator.Tests/Services/Project/ClimateThermalInvalidationRegressionTests.cs`
- `tests/SnowMeltingCalculator.Tests/ViewModels/ResultsViewModelOpenProjectTests.cs`
- `tests/SnowMeltingCalculator.Tests/ViewModels/ResultsStabilizationPhase1BehaviorContractsTests.cs`
  (source-pin row update — carried over from task-8 allow-list)
- `tests/SnowMeltingCalculator.Tests/Services/Project/ThermalMultiplicityCharacterizationTests.cs`
  (EXACTLY two rows per owner decision AMZ-2, journal 2026-08-23; see §5)
- `src/Services/Navigation/CalculationStateService.cs`
  (ProjectLoadReset translation suppression — allow-listed file; see §5)
- Evidence: `task-9/{allowed-hunks.json, protected-pre.json, protected-post.json,
  trx-lifecycle.json, trx-full-release.json, arithmetic.json, TestResults/*.trx, this receipt}`

No git operations performed. HEAD unchanged: `6a5a96f1763dd952c8d772ecd1d2536eb3b804cf`.
