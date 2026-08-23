# Task 5 Blocker Analysis — Structural gap between DEC-T02 closed API and immutable Todo-2 characterization

Status: EXECUTION STOPPED per plan blocker protocol (plan line 194 / Todo 5 blocker clause). No Todo-5 production edits performed. Owner decision required.

## The contradiction (proven)

1. **Todo-2 characterization (immutable, G2 gate)** pins these observable facts after VM-driven own-input edits with result present AND after upstream invalidation (`ThermalMultiplicityCharacterizationTests`, assert helpers at lines ~1440-1475, ~384):
   - `ICalculationStateService.ThermalNeedsRecalculation == true`
   - `ICalculationStateService.ThermalValidationMessage == <exact Russian cause message>`
   - legacy `StateChanged` recorder == exactly `[NeedsRecalculation]` with that message
2. **DEC-T07 / Todo-5 requirement**: service must remove ALL Thermal backing fields (`_thermalNeedsRecalculation`, `_thermalIsCalculating`, `_thermalValidationMessage`, `_pipeSpacing`); getters read live from canonical `IProjectSessionThermalState`.
3. **DEC-T02 closed mutation set** cannot express "Phase=NeedsRecalculation + RecalculationMessage=msg with result PRESERVED" without knowing which field changed and its new value. The legacy writer receives ONLY a message string.
4. Pre-Todo-6 nothing populates canonical `Result` (VM not yet routed through state/coordinator), so every result-gated canonical mutation (`InvalidateFromClimate/Construction`, `ApplyInputEdit`) evaluates to NoChange → zero completions → characterization event-count assertions fail.
5. The characterization fixture constructs `new CalculationStateService(session)` (single-arg ctor, immutable test file) — injecting `CalculationContext`/calculator into the service to read results is impossible without editing immutable tests.
6. Exhaustive search over legal compositions (single mutations, sequences summing to one completion, message→origin mapping incl. InvalidateFrom*, SystemApply normalization, restore lease, internal compat session) yields ZERO designs satisfying all of: getters-read-canonical + counts-match + no-writable-store + immutable-tests-green. `SetThermalCalculating()`→BeginCalculation() and `ResetThermalState()`→ApplyInputs(SystemApply) ARE expressible; ONLY `SetThermalNeedsRecalculation(string)` is structurally inexpressible.

## Options

| Option | Essence | Plan conformance | Verdict |
|---|---|---|---|
| **A** | Owner-approved minimal amendment to Todo-3 contract: add ONE transitional mutation `ApplyNeedsRecalculation(string recalculationMessage, ThermalMutationOrigin origin)` to `IProjectSessionThermalState`/`ProjectSessionThermalState` (+ its Todo-3 tests): preserve inputs+result, set Phase=NeedsRecalculation + exact message, emit exactly one completion iff changed (idempotent-by-value). Service maps the seven exact legacy messages → origins (field msgs→User, Climate→ClimateInvalidation, Construction→ConstructionInvalidation) during the bridge window. | Keeps SOLE-WRITABLE-OWNER invariant fully intact (state stays the only store). Touches Todo-3-owned files = deviation from frozen todo boundaries; requires owner approval + documentation; Todo-11 guard must later prove no non-adapter production callers. | **RECOMMENDED** |
| B | Permit a transitional private status mirror inside CalculationStateService. | Violates DEC-T07 letter ("removes … writable store") and sole-owner spirit; guard suite (Todo 11) would flag it forever. | Reject |
| C | Re-sequence: execute Todo 6 (+7) before/merged with 5 so VM writes canonical directly and legacy writers disappear in one boundary. | Breaks frozen dependency graph 4→5→6; much larger blast radius; upstream rows still gap between 6 and 7 unless merged too. | Possible but riskier |
| D | Amend characterization expectations. | Forbidden ("never weaken tests"). | Reject |

## Consequence matrix if Option A approved

- Files touched beyond Todo-5 allow-list: `src/Services/Project/IProjectSessionThermalState.cs`, `src/Services/Project/ProjectSessionThermalState.cs`, `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectSessionThermalStateTests.cs` (new cases for the added mutation), each documented as owner-approved amendment AMZ-1 in task receipts.
- All other Todo-5 scope proceeds exactly as specified (fields removed, spacing full delegation, translations, Hydraulics untouched).

## RESOLUTION (2026-08-23, owner-approved merged boundary executed)

The owner approved the re-sequenced MERGED BOUNDARY AMZ-1 (Todos 5+6+7 as one green lane;
STATE.json nextAction). Execution outcome against this analysis:

- The merged boundary alone did NOT remove the need for Option A: the immutable QA-failure
  rows (`QaFailure_SyntheticDirectWriter_ViolationDetectedByMultiplicityAssertions`,
  `QaFailure_DuplicateSubscriber_ViolationDetectedByMultiplicityAssertions`) call
  `CalculationStateService.SetThermalNeedsRecalculation(string)` and pin its exact
  StateChanged multiplicity, and no closed DEC-T02 mutation can express
  "NeedsRecalculation + arbitrary message, result-preserving". **Option A was implemented
  exactly as specified**: one transitional mutation
  `ApplyNeedsRecalculation(string recalculationMessage, ThermalMutationOrigin origin)` on
  `IProjectSessionThermalState`/`ProjectSessionThermalState` — preserves inputs+result,
  sets Phase=NeedsRecalculation with the exact message, emits exactly one completion iff
  changed, idempotent-by-value. New cases for it live in the NEW lane-owned suite
  `ThermalStateCoordinatorTests` (Todo-3 test file left untouched). Legacy writer routing:
  needs-recalculation -> bridge mutation (User origin); SetThermalCalculating ->
  BeginCalculation(); ResetThermalState -> ApplyInputs(current, SystemApply).
- Additional structural findings resolved during execution (documented in
  task-6/task-567-merged-boundary.md section 10):
  1. User/lifecycle reset must NOT mutate canonical state (immutable stale-getter rows);
     reset is the canonical-silent adapter seam; canonical replacement happens only via
     CalculateAsync/LoadResult.
  2. Service `SetPipeSpacing` uses a status-flow-through origin so the compatibility writer
     never synthesizes StateChanged and never marks dirty (legacy-exact).
  3. VM spacing edits emit the canonical write first, then a compat echo
     `SetPipeSpacing(value,"ThermalViewModel")` that is a no-op in real composition and the
     event source for mocked-service integration fixtures.
  4. Coordinator LoadResult takes PipeSpacing from the canonical snapshot at finalize
     (orchestrator order SetPipeSpacing -> LoadResult; no-op spacing emits no echo).
  5. Three out-of-allow-list test files received minimal mechanical adaptations with
     assertion contracts preserved (GuardTests sample value into canonical range;
     ResultsStabilizationPhase1BehaviorContractsTests reflection path through
     `vm.Coordinator`; DiRegistrationTests CycleStateImplementation gained the new interface
     member as throw-only stub).

Final gates: G0-G9 all exit 0; full Release 1902 passed / 0 failed / 1903 total with
NotExecuted == the three baseline identities. Receipt:
`task-6/task-567-merged-boundary.md`.
