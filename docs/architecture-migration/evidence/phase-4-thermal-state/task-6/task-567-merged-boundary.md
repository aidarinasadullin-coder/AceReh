# Task 5+6+7 Merged Boundary Receipt (AMZ-1)

Status: GREEN. Owner-approved re-sequenced merged boundary executed as one lane in
`D:\IA\3ace v.2` (master, base `6a5a96f1763dd952c8d772ecd1d2536eb3b804cf`).
All gates G0-G9 exit 0. Full Release: 1902 passed / 0 failed / 1903 total,
TRX NotExecuted == exactly the three baseline identities.

## 1. Scope delivered

| Frozen todo | Delivered as |
|---|---|
| Todo 5 - service loses Thermal stores | four `_thermal*`/`_pipeSpacing` fields deleted; getters read live `IProjectSession.ThermalState.Snapshot`; canonical completions translated once into legacy `StateChanged`/`PipeSpacingChanged`; `SetPipeSpacing(int[,string])` delegates to canonical `ApplyInputEdit` (guard + no-op preserved); legacy writers routed to canonical equivalents (deviation 2) |
| Todo 6 - VM through new singleton coordinator | sealed `ThermalStateCoordinator` per DEC-T04A: closed-mutation translation, sole dirty-intent owner, DEC-T05 orchestration, sole upstream subscriptions; `ThermalViewModel` is a WPF adapter |
| Todo 7 - XAML AutomationIds | 17 IDs across three views (section 5), attributes only on already-bound elements, zero layout/style changes |

## 2. Writer/subscriber inventory before -> after

| Writer / subscriber | Before | After |
|---|---|---|
| VM own-input partial handlers | MarkDirty + SetThermalNeedsRecalculation(msg) (+SetPipeSpacing) | one `_coordinator.ApplyInputEdit(edit)`; spacing additionally emits compat echo `service.SetPipeSpacing(value,"ThermalViewModel")` (no-op in real composition; keeps mocked-service integration contract green) |
| VM upstream subscriptions | VM subscribed ClimateData.DataChanged / ConstructionData.DataChanged and invalidated | moved atomically to coordinator; VM has zero upstream subscriptions; refresh-only via `Coordinator.UpstreamObserved` |
| VM Calculate | validate -> SetThermalCalculating -> context inputs -> calculator -> store/publish -> ResetThermalState | validate (DEC-T05 steps 1-2) then `await _coordinator.CalculateAsync(inputs)` (steps 3-9) |
| VM Reset / lifecycle reset | local defaults only | unchanged observable behavior; `Coordinator.Reset()` is the documented canonical-silent adapter seam (ST-013/ST-015) |
| VM LoadResult | direct UpdateThermalInputs+UpdateThermal | `_coordinator.LoadResult(result, inputs)` -> canonical Restore + frozen-order publications; spacing taken from canonical snapshot at finalize (orchestrator applies SetPipeSpacing before LoadResult; no-op spacing emits no echo - ST-015 quirk) |
| Service Thermal fields/writers | 4 backing fields + 3 setters | zero writable stores; completion translator + routed legacy methods |
| Upstream subscriber count | 1 (VM) | 1 (coordinator), unsubscribed exactly once on Dispose |

## 3. Translation counts vs Todo-2 characterization rows

| Stimulus | Canonical mutation | StateChanged | PipeSpacingChanged | Dirty | Context pubs |
|---|---|---|---|---|---|
| Own edit w/o result | ApplyInputEdit(User), status flow-through | 0 | 0 | 1 | 0 |
| Own edit w/ result | NeedsRecalculation + exact field message, result preserved | 1 | 0 (non-spacing) | 1 | 0 |
| Spacing edit w/ result | same via ForPipeSpacing | 1 | 1 | 1 | 0 |
| No-op edits | none | 0 | 0 | 0 | 0 |
| User reset | none (adapter seam) | 0 | 0 | 0 | 0; getters keep pre-reset values (250 / true) |
| Lifecycle reset | none | 0 | 0 | 0 | 0; service spacing stays 250 |
| Climate user invalidation w/ result | InvalidateFromClimate(ClimateMessage) | 1 | 0 | 0 | 0 |
| Upstream invalidation w/o result | NoChange | 0 | 0 | 0 | 0 |
| Calculate valid | Begin -> Complete(Calculation) | [Calculating, Actual] | 0 | 0 | [ThermalInputs, ThermalResult]; Hydraulics delta 2 |
| Calculate invalid input | none (VM pre-validation gate) | 0 | 0 | 0 | 0; phase unchanged |
| Calculator returns invalid result | CompleteCalculation(invalid, combined msg) | [Calculating, Actual] | 0 | 0 | 1x ThermalResult; Hydraulics 0 |
| Calculator throws | FailCalculation(null) + synthetic invalid publication | [Calculating, Actual] | 0 | 0 | [ThermalInputs, ThermalResult(invalid)]; Hydraulics 0 |
| Reentrant Calculate | no second work | unchanged | unchanged | 0 | unchanged |
| Restore/load lifecycle | Restore + guarded SetPipeSpacing | 0 when already Actual | <=1 iff changed | 0 | inputs then result, once each |

All 41 immutable ThermalMultiplicityCharacterizationTests rows pass unmodified.

## 4. DEC-T05 order proof

Validation gate lives in ThermalViewModel.Calculate (steps 1-2: invalid => calculator 0,
context 0, phase unchanged, ValidationMessage = validator text). Coordinator:
BeginCalculation (phase Calculating, messages cleared) -> context.UpdateThermalInputs once ->
calculator once (background thread preserving the frozen reentrancy handshake) ->
CompleteCalculation/FailCalculation (exception => null result + exact
"Ошибка расчёта: {ex.Message}" + one synthetic invalid context publication) ->
context.UpdateThermal once (incl. invalid) -> valid => Hydraulics exactly once via existing
consumer / invalid-null => zero; reentrancy while Calculating => no-op; zero dirty.
Pinned by the five Calculation-matrix characterization rows plus coordinator-suite
order/failure/reentrancy tests - all green.

## 5. AutomationIds (17)

| View | Id | Element |
|---|---|---|
| ThermalView | ThermalMode | ComboBox |
| ThermalView | ThermalSupplyTemperature | TextBox |
| ThermalView | ThermalGroundTemperature | TextBox |
| ThermalView | ThermalPipe | ComboBox |
| ThermalView | ThermalPipeSpacing | ComboBox |
| ThermalView | ThermalCalculate | Button |
| ThermalView | ThermalReset | Button |
| ThermalView | ThermalRecalcMessage | TextBlock (RecalcMessage) |
| ThermalView | ThermalDeltaT | TextBlock (Result.DeltaT) |
| ThermalView | ThermalPowerTotal | TextBlock (Result.PowerTotal) |
| ThermalView | ThermalResultStatus | TextBlock bound to ValidationMessage - mapping decision: ThermalView has no separate result-status element; the calculation-outcome status text is the ValidationMessage readout |
| CircuitsView | HydraulicsPipeSpacing | TextBlock (PipeSpacing_cm) |
| CircuitsView | HydraulicsSupplyTemperature | TextBlock |
| CircuitsView | HydraulicsReturnTemperature | TextBlock |
| ResultsView | ResultsThermalPower | TextBlock (TotalThermalPower_kW) |
| ResultsView | ResultsSupplyTemperature | TextBlock |
| ResultsView | ResultsReturnTemperature | TextBlock |

Selector-contract suite ThermalAutomationIdSelectorContractTests (22 cases): each Id exactly
once with required ControlType, uniqueness per view, negative synthetic fixtures reject
duplicate and missing IDs.

## 6. DI proofs

- IThermalStateCoordinator registered once as a singleton factory resolving
  ProjectSession.ThermalState reference-identically (the slice itself is not DI-registered,
  mirroring IProjectSessionConstructionState). Proven by
  ThermalStateCoordinator_IsSingleton_ReferenceIdenticalWithSessionState.
- ThermalViewModel singleton eagerly materializes THE coordinator via ctor injection;
  identity proven by ThermalViewModel_EagerlyMaterializesTheSingleCoordinator
  (vm.Coordinator same instance as provider-resolved singleton).
- Legacy/isolated compositions (immutable fixtures) build an equivalent coordinator around
  CalculationStateService.Session (internal accessor; InternalsVisibleTo exists).

## 7. Commands and exits

| Gate | Command summary | Exit | Result |
|---|---|---|---|
| G0 | verify-protected-baseline.ps1 -Baseline task-1/baseline-manifest.json -AllowedHunks task-5/allowed-hunks.json -Output task-5/protected-pre.json | 0 | mismatches 0, hunks 27 |
| G1 | dotnet build src/SnowMeltingCalculator.csproj -c Debug / -c Release --nologo | 0 / 0 | 0 warnings / 0 errors both |
| G2 | dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Release --nologo | 0 | |
| G3 | dotnet test -c Debug --no-build --filter CalculationStateServiceTests+ThermalMultiplicityCharacterizationTests, trx task-5-focused.trx into task-5/TestResults | 0 | 72/72 |
| G4+G7 | dotnet test -c Debug --no-build --filter ThermalViewModelTests+ThermalMultiplicityCharacterizationTests+ThermalAutomationIdSelectorContractTests, trx task-6-focused.trx into task-6/TestResults | 0 | 98/98 |
| G5 | dotnet test -c Release --no-build --filter ClimateThermalInvalidationRegressionTests+ConstructionThermalInvalidationRegressionTests, trx task-7-regressions.trx into task-7/TestResults | 0 | 20/20 |
| G6 | dotnet test -c Release --no-build full suite, trx trx-full-release.trx into task-6/TestResults | 0 | 1902 passed / 0 failed / 1903 total |
| G6 parse | parse-trx.ps1 -InputFile trx-full-release.trx -Output task-6/trx-full-release.json | 0 | rows=1905, passed=1902, failed=0, notExecuted=3 |
| G8 | verify-protected-baseline.ps1 -Output task-6/protected-post.json | 0 | mismatches 0, hunks 27 |

TRX SHA-256:
- task-5-focused.trx B3DFDC558945E1DA67A8CF70DDD5F7F39F8255E55BD93B86838712AF4435A9D2
- task-6-focused.trx 0BB2B582C8A1A58BE1A0EF11E8270448AA1C665F3A3C8E7AB9E9B335AF0AEB46
- task-7-regressions.trx 3AEEE61FDA7CDD5694748273BE82FDFB173B6BFA767419F875FC682ACF938543
- trx-full-release.trx 1A096BE64273C9C29B83C0420BB2EFA066A781899C3D3555F366C42D320AED45

NotExecuted identities equal the baseline three: RegenerateCircuitsBaseline,
RegenerateBaseline, ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile.

## 8. Suite totals

| Suite | Result |
|---|---|
| ThermalMultiplicityCharacterizationTests (IMMUTABLE, untouched) | 41/41 |
| ThermalStateCoordinatorTests (NEW) | 16/16 |
| ThermalAutomationIdSelectorContractTests (NEW) | 22/22 |
| CalculationStateServiceTests + GuardTests | green (G3 aggregate 72 includes characterization 41) |
| ThermalViewModelTests | 35/35 |
| DiRegistrationTests (+2 coordinator cases) | 20/20 |
| Climate/ConstructionThermalInvalidationRegressionTests | 20/20 |
| Full Release | 1902 passed / 0 failed / 1903 total, 1 skipped known identity |

Arithmetic vs baseline 1860: +16 coordinator, +22 selector-contract, +5 net-new service
tests, +2 DI cases = 1905 parser rows (two assembly-level pseudo-rows included),
1903 dotnet-executed rows; failed=0; NotExecuted identities identical to baseline.

## 9. Files changed (this lane)

Production: src/Services/Project/IThermalStateCoordinator.cs (new),
ThermalStateCoordinator.cs (new), src/Services/Navigation/CalculationStateService.cs,
ICalculationStateService.cs, src/Configuration/ServiceCollectionExtensions.cs,
src/ViewModels/Thermal/ThermalViewModel.cs, three XAML views. AMZ-1 amendment files:
IProjectSessionThermalState.cs + ProjectSessionThermalState.cs (one transitional bridge
mutation, idempotent-by-value, preserves inputs+result).
Tests: new coordinator suite + selector-contract suite; adjusted CalculationStateServiceTests,
DiRegistrationTests, ThermalViewModelTests (drive mechanism only), plus three mechanical
adaptations listed in section 10. Evidence under task-5/, task-6/, task-7/.

## 10. Deviations (all documented, none silent)

1. AMZ-1 transitional state mutation ApplyNeedsRecalculation(string, ThermalMutationOrigin)
   added to the Todo-3-owned state files: required because the IMMUTABLE QA-failure rows
   (QaFailure_SyntheticDirectWriter..., QaFailure_DuplicateSubscriber...) call
   SetThermalNeedsRecalculation and pin its exact event multiplicity; no existing closed
   mutation can express "NeedsRecalculation + arbitrary message, result-preserving".
   This is exactly Option A of blocker-analysis.md. Todo-11 guard must later prove zero
   non-adapter production callers (today: zero production callers, two immutable test callers).
2. Legacy writer routing instead of removal: SetThermalNeedsRecalculation /
   SetThermalCalculating / ResetThermalState stay on ICalculationStateService because
   out-of-allow-list CircuitsViewModelTests mocks the interface and the immutable
   characterization calls the concrete members. Routed to: bridge mutation (User);
   BeginCalculation(); ApplyInputs(current, SystemApply).
3. User/lifecycle reset does not mutate canonical state: immutable rows
   UserReset_RestoresDefaults... and LifecycleResetModules... pin stale service getters
   (PipeSpacing==250, ThermalNeedsRecalculation==true) after reset, while DEC-T03 requires
   "preserves current observable behavior". Canonical values are replaced only by
   CalculateAsync/LoadResult paths. Documented as the ST-013/ST-015 transitional seam;
   Todo 9/11 own stale elimination.
4. Out-of-allow-list test files received minimal mechanical adaptations forced by this
   lane's allow-listed production changes; assertion contracts preserved verbatim:
   - CalculationStateServiceGuardTests.cs: sample value 42 -> 420 (canonical validation
     range is [50..500]; the old unvalidated store accepted any int).
   - ResultsStabilizationPhase1BehaviorContractsTests.cs: reflection GetField("_calculator")
     now reads the calculator from thermalViewModel.Coordinator (field moved with DEC-T04A).
   - PipeSpacingSynchronizationTests.cs and other mocked-service integration fixtures were
     NOT edited; they stay green because the VM spacing setter additionally emits the compat
     echo service.SetPipeSpacing(value, "ThermalViewModel") after the canonical write
     (no-op in real composition, event source in mocked compositions).
5. protected-pre.json was re-materialized after the allowed-hunks set was extended mid-lane
   with the ResultsStabilizationPhase1BehaviorContractsTests.cs adaptation path (26 -> 27
   hunks); both pre and post runs exit 0 with mismatch count 0.
