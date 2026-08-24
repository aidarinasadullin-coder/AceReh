# F2 — Architecture / Code Quality receipt (independent source audit)

- Write-set: **phase-5-hydraulics-state** (frozen plan, SHA-256 `0D48A757DDD1B98384D131D76C7AF5220206260BF853FADF12BA859329FC8D38`)
- Repo HEAD under review: `dc9b8c7c952d1bcffdc5378c4d02e675e94f248d`
- Method: independent audit of actual production/test sources (not receipts). Every claim below was re-derived from source with repo-wide sweeps; receipts were spot-checked against code. Baseline comparisons against pre-phase-5 `471c4f1` via `git show`/`git diff`.
- Worktree at review time: exactly one dirty path (`docs/architecture-migration/STATE.json`, owner-gate artifact) — untouched, unstaged.
- Correction context honored (not re-litigated): owner-adjudicated deviations in `task-9/divergence-notes.md` (dirty-authority transfer; FIX B unconditional per-attempt status termination; auto-recalc dirty-churn elimination; DI construction-cycle fix) and `task-6/correction-notes.md` (shared-session fixtures; save-projection test rewritten as behavioral round-trip). Code was verified to match those documented designs — it does.

## A. Sole writable owner per value ST-016..ST-019 — PASS

Sweep used: regex over all of `src/` for `ApplyGlobalInputs|ReplaceCollectors|BeginCalculation|CompleteCalculation|FailCalculation|ResetToDefaults|.Restore(` and `UpdateHydraulics|PublishHydraulics`. The backing field `_snapshot` is assigned in exactly one place: `ProjectSessionHydraulicsState.Commit` (`src/Services/Project/ProjectSessionHydraulicsState.cs:96`).

**ST-016 Global inputs.** Only mutation entry: `ApplyGlobalInputs(candidate, origin)` (`ProjectSessionHydraulicsState.cs:48`). Production call sites, exhaustive:
- `src/ViewModels/Hydraulics/CircuitsViewModel.cs:1300-1306` — origin `User`, guarded:
  ```csharp
  if (_isResetting || _isInitializing || _isMirroringHydraulicsState ||
      _calculationStateService.IsLoadProjectInProgress)
  { return; }
  _hydraulicsState.ApplyGlobalInputs(new HydraulicGlobalInputsSnapshot(...), HydraulicsMutationOrigin.User);
  ```
- `src/Services/Navigation/CalculationStateService.cs:131-133` — `ResetHydraulicsState()` re-applies current GlobalInputs with origin `SystemApply` (status normalization precedent, per plan contract).
No other path mutates global inputs. Validation/rejection centralized (`Validate`, state :104-112).

**ST-017 Collectors/circuits.** Only mutation entry: `ReplaceCollectors(collectors, origin)` (`state :58`). Production call sites, exhaustive — all four in the adapter, all origin `User`, all guarded by `_isInitializing/_isResetting/_isMirroringHydraulicsState/_isCalculating`: `CircuitsViewModel.cs:1024, 1094, 1111, 1132`.
`CaptureCanonicalCollectors` (`CircuitsViewModel.cs:858-874`) is the **sole producer** of collector snapshots from VM state: it is a read-only `Select` projection into new snapshot objects (no writes to `Collectors`/rows), wired once via `_coordinator.Connect(..., CaptureCanonicalCollectors, ...)` (:929-935) and consumed by the coordinator's completion (`HydraulicsStateCoordinator.cs:77`). The only other snapshot producers are creation defaults inside the VM (:339-347, :391-398) and the restore mapper from DTOs (`HydraulicsPersistenceMapper.BuildCollectorSnapshot`) — both outside the user-mutation path by design.

**ST-018 Results projection.** `CalculationContext.UpdateHydraulics` production call sites — **exactly one**, repo-wide:
```
src/Services/Project/HydraulicsStateCoordinator.cs:56-57
public void PublishHydraulics(List<CollectorSummary>? summaries) =>
    _calculationContext.UpdateHydraulics(summaries, "CircuitsViewModel");
```
Sweep proof: matches for `UpdateHydraulics(` in `src/` are only the declaration (`src/Core/CalculationContext.cs:211`), the coordinator site above, and `MainViewModel.UpdateHydraulicsBadge` (:289,:325 — a different symbol; the identifier does not match `UpdateHydraulics(`). The adapter's glycol-failure path routes through the coordinator too (`CircuitsViewModel.cs:571 _coordinator.PublishHydraulics(null);`). Guard category `ContextUnapprovedWriter` independently pins the writer set to `{HydraulicsStateCoordinator}` (and thermal to `{ThermalStateCoordinator}`). Source literal `"CircuitsViewModel"` preserved verbatim per plan.

**ST-019 Status.** Writers, exhaustive:
- `BeginCalculation` ← `CalculationStateService.cs:119`, whose only caller is `HydraulicsStateCoordinator.RunCalculation:61`;
- `CompleteCalculation` ← coordinator :77 only;
- `FailCalculation` ← `CalculationStateService.cs:125` (`SetHydraulicsError`), only caller `CircuitsViewModel.cs:570` (glycol lookup failure); rejection-guarded when no calculation is active (state :78-79);
- `ResetToDefaults` ← `ProjectLoadOrchestrator.cs:89` (`ProjectLoadReset`) and `MainViewModel.cs:247` (`UserReset`);
- status normalization ← `ApplyGlobalInputs(..., SystemApply)` (`CalculationStateService.ResetHydraulicsState :129-134`).
Unconditional per-attempt termination verified (owner-adjudicated FIX B): `RunCalculation` wraps the attempt in `try/finally { _calculationStateService.ResetHydraulicsState(); }` (`HydraulicsStateCoordinator.cs:59-83`) — exactly one reset per attempt, success or failure, including early-exit paths.

## B. ViewModels-as-adapters — structure PASS; diff-breadth judgment FAIL (one undocumented semantic delta)

**Structure (all sub-checks pass):**
- No canonical store of its own: writable fields are UI mirrors (`ObservableCollection<CollectorData> Collectors`, summary cards, `HydraulicInputData InputData`, selection/mode props). Canonical writes go only through `_hydraulicsState` (5 sites listed under A).
- Ctor requires `IHydraulicsStateCoordinator` + `IProjectSession` (`CircuitsViewModel.cs:898-907`); the slice reference is taken from the session (`:917 _hydraulicsState = (...projectSession).HydraulicsState;`).
- User edits route `ApplyGlobalInputs`/`ReplaceCollectors(origin User)` under the four guard flags (sites cited under A).
- Lifecycle mirror is pull-only: `OnHydraulicsStateChanged` reacts solely to origin `ProjectLoad` (:876-892) and mirrors under `_isMirroringHydraulicsState`; `ApplyLifecycleSnapshotToAdapter` (:717-793) performs no canonical writes ("The caller owns the canonical mutation").
- Upstream subscriptions (`ContextChanged`, `PipeSpacingChanged`, `StateChanged`) absent from the VM (guard counts them = 0) and present exactly once in the coordinator (:31-33).

**Diff breadth vs baseline `471c4f1` (+287/-148, confirmed via `git diff --numstat`):** the overwhelming majority is mechanical ownership transfer — subscriptions/handlers removed (`OnCalculationContextChanged`, `OnPipeSpacingChanged`, `OnCalculationStateChanged`, `PublishHydraulicsSummaries`), adapter machinery added (`ApplyLifecycleSnapshotToAdapter`, `ToSnapshot`×2, `ToDomainResult`, `CaptureCanonicalCollectors`, `MirrorPipeSpacing`, guard flags), `_markDirtyService.MarkDirty()` replaced by `ReplaceCollectors(User)` in the four collection/property handlers, `Calculate` delegating to `_coordinator.Calculate/CalculateAll`. This is ownership-transfer-shaped.

**However, one hunk is NOT ownership-transfer-only.** The legacy input handler propagated global supply inputs into every circuit row before recalculating; the migrated handler dropped that propagation:

Baseline `471c4f1` (verbatim, removed hunks of the `SetInputData` handler):
```csharp
if (e.PropertyName == nameof(HydraulicInputData.SupplySpacing_cm))
{
    _markDirtyService.MarkDirty();
    OnPropertyChanged(nameof(SupplySpacing_cm));
    foreach (var collector in Collectors)
    {
        foreach (var circuit in collector.Circuits)
        {
            circuit.SupplySpacing_cm = InputData.SupplySpacing_cm;   // <-- propagation
        }
    }
    Calculate();
}
else if (e.PropertyName == nameof(HydraulicInputData.SupplyHeatPercent))
{ /* same propagation pattern: circuit.SupplyHeatPercent = InputData.SupplyHeatPercent; */ ... }
```

Current handler (`CircuitsViewModel.cs:1292-1320`): publishes `ApplyGlobalInputs(..., User)` then `MarkDirty + Calculate()` — **no propagation to circuit rows**. A repo-wide sweep confirms no remaining assignment of row supply values from globals anywhere in `src/` (only creation defaults `CircuitsViewModel.cs:345,397` and the lifecycle mirror :730,:766).

Consequence chain (all citations current code):
1. The two globals remain user-editable TextBoxes bound to `InputData` (`src/Views/Hydraulics/CircuitsView.xaml:306`, `:320`).
2. Hydraulic power depends on the per-row values: `CircuitsCalculator.CalculateCircuitPower` (`src/Services/Hydraulics/CircuitsCalculator.cs:34-37`):
   ```csharp
   double supplyLengthPerArea = circuit.SupplyLength / (100.0 / circuit.SupplySpacing_cm);
   double supplyHeatFactor = circuit.SupplyHeatPercent / 100.0;
   double power = (lengthPerArea + supplyLengthPerArea * supplyHeatFactor) * (q_up + q_down);
   ```
3. Therefore, after this phase, editing global «Шаг подводки» / «Полезное тепло от подводок» no longer changes calculation results, the per-circuit display cards (`CircuitsView.xaml:567`, `:575` — display-only TextBlocks), nor the saved per-circuit wire fields (`HydraulicsPersistenceMapper.cs:78-79` reads stale row values while `:23-24` writes the new globals). At baseline the same edit changed all three.
4. Not adjudicated anywhere: `task-9/divergence-notes.md` documents exactly four deviations (dirty transfer, FIX B, auto-recalc churn, DI deadlock) — none covers this; `TASK_CONTEXT.md` journal and phase commit bodies are silent on it.
5. Not covered by any gate: the characterization fixture pins identical multiplicity on both sides (`GlobalInputEdit_UsesCurrentDirtyAndCalculationMultiplicity("SupplySpacing_cm",2,2)` — legacy emitted 2 mock dirty calls from the handler, migrated emits 1 slice-raised + 1 VM-raised; `HydraulicsMultiplicityCharacterizationTests.cs:36-73` asserts counts only, never values); round-trip/open-project tests do not exercise live global edits; UI QA asserts field values only (`ui-qa/run-hydraulics-flows.ps1:1022-1023,1270-1271,1315-1316`).

This violates the frozen plan's Must-NOT-Have #1 («Никаких изменений наблюдаемого поведение... тексты сообщений — без изменений»; item reads «Никаких изменений наблюдаемого поведения») via an undocumented semantic change, and it also leaves canonical `GlobalInputs.SupplySpacingCm/SupplyHeatPercent` computationally inert (persistence-only), creating a latent divergence between ST-016 globals and captured ST-017 rows. Item B therefore fails its breadth criterion. Remediation is narrow and explicit: owner adjudicates either (a) restore the legacy propagation semantics under the existing mirror guards before publishing `ReplaceCollectors`, or (b) accept the new semantics as a documented deviation with characterization coverage pinning it; then rerun the affected focused gates.

## C. Services do not depend on concrete VMs — PASS (no new dependencies)

Grep of `src/Services/**` for concrete `*ViewModel` references yields code-level dependencies ONLY in files that already had them at baseline (verified via `git show 471c4f1:...`):
- `src/Services/Results/ResultsPdfDataBuilder.cs:20-21,29-35` — `ConstructionViewModel`, `CircuitsViewModel` fields/ctor params: byte-for-byte the same dependency shape at `471c4f1` (pre-existing documented debt, «этап C2»). Unchanged by phase 5 (F1 confirmed empty diff for this file).
- `src/Services/Project/ProjectLoadOrchestrator.cs:27-30,43-47` — four concrete VMs: identical set at `471c4f1` («этап C1» debt). Phase 5 reduced its direct-write behavior (canonical Restore) without adding VM surface.
- `src/Services/Hydraulics/ICollectorTypeSelector.cs` / `ICircuitsValidator.cs` (+impls) reference `ViewModels.Hydraulics` model types (`CollectorData`/`CircuitRow`): pre-existing (baseline ctor already injected both).
All other matches are comments/doc strings or reflection-by-name (`EditorDialogService.cs:15-16`, string constants only).
New phase-5 services (`IHydraulicsStateCoordinator`, `HydraulicsStateCoordinator`, `IProjectSessionHydraulicsState`, `ProjectSessionHydraulicsState`, `HydraulicsStateSnapshots`, `HydraulicsPersistenceMapper`) reference zero ViewModel types. The `"CircuitsViewModel"` string in the coordinator is the frozen event-payload source literal mandated by the plan, not a type dependency. No service→VM wiring was added.

## D. Snapshot immutability — PASS

`src/Services/Project/HydraulicsStateSnapshots.cs`: all seven snapshot types are `sealed` with get-only properties and structural `Equals`/`GetHashCode` (field-by-field; collection equality via `SequenceEqual`). Collection boundaries defensively copied:
```csharp
// HydraulicCollectorSnapshot ctor (:150)
Circuits = Array.AsReadOnly((circuits ?? Array.Empty<HydraulicCircuitSnapshot>()).ToArray());
// HydraulicsStateSnapshot ctor (:184)
Collectors = Array.AsReadOnly((collectors ?? Array.Empty<HydraulicCollectorSnapshot>()).ToArray());
```
Entry boundaries copy again: `ReplaceCollectors` (`state :61 ToArray()`), `CompleteCalculation` rebuilds collectors through the copying ctor (`state :72`), `Restore` re-wraps (`state :87`). Mirror path builds fresh objects: `ApplyLifecycleSnapshotToAdapter` constructs new `CollectorData`/`CircuitRow` per snapshot element (`CircuitsViewModel.cs:733-778`); `CaptureCanonicalCollectors` produces brand-new snapshots. Runtime probe: casting `Default.Collectors` to `IList<T>.Add` throws `NotSupportedException` (guard test :123-124). No shared mutable collection escapes the slice.

## E. Guard suite honesty — PASS (8/8 categories, real negative self-checks)

File: `tests/SnowMeltingCalculator.Tests/Services/Project/HydraulicsStateLegacyStoreGuardTests.cs`. Each category combines a production source-scan/in-memory predicate with a genuine violating input fed to that same predicate (detection asserted), plus a behavioral probe. Representative self-check assertion per category:

1. **VmWritableStore** (:19-38) — feeds `"_inputData.GlycolConcentration = value;"` into the production predicate: `Assert.That(RejectsVmWritableStore("_inputData.GlycolConcentration = value;"), Is.True);` (:28); behavioral probe asserts `origin == User` after `ApplyGlobalInputs` (:31-37).
2. **ServiceHydraulicsStore** (:40-54) — `Assert.That(RejectsServiceStore("private bool _hydraulicsIsCalculating;"), Is.True);` (:47); probe: `session.HydraulicsState.BeginCalculation(); Assert.That(service.HydraulicsIsCalculating, Is.True);` (:52-53).
3. **OrchestratorDirectAssign** (:56-66) — `Assert.That(FindDirectHydraulicsAssignments("_circuitsViewModel.InputData = data;"), Is.EqualTo(new[] { "InputData" }));` (:64); production scan must be empty while `_hydraulicsState.Restore(` must be present (:62-63).
4. **ResultsNonCanonicalSave** (:68-80) — `Assert.That(RejectsNonCanonicalSave("var snapshot = _circuitsViewModel.BuildCanonicalSnapshot();"), Is.True);` (:78); save method body must contain mapper + session snapshot and no `_circuitsViewModel.{BuildCanonicalSnapshot|InputData|Collectors}` (:75-77).
5. **ContextUnapprovedWriter** (:82-105) — synthetic writer detected: `Assert.That(FindUnapprovedWriterFiles(new[] { ("Synthetic.cs", "context.UpdateHydraulics(items);") }), Is.EqualTo(new[] { "Synthetic.cs" }));` (:95); production writer sets pinned to exactly `{HydraulicsStateCoordinator}` / `{ThermalStateCoordinator}` (:94,:104).
6. **SnapshotMutability** (:107-125) — `Assert.That(RejectsMutableSnapshot("public List<HydraulicCollectorSnapshot> Collectors { get; set; }"), Is.True);` (:120); runtime mutability probe throws `NotSupportedException` (:123-124).
7. **DuplicateUpstreamSubscriber** (:127-147) — `Assert.That(RejectsDuplicateSubscriber("if (coordinator == null) context.ContextChanged += handler;"), Is.True);` (:138); behavioral: full-DI provider proves VM's `_coordinator` field `Is.SameAs` the resolved coordinator singleton (:141-146).
8. **DiIndependentStateRegistration** (:149-162) — `Assert.That(RejectsIndependentDiRegistration("services.AddSingleton<IProjectSessionHydraulicsState>();"), Is.True);` (:156); behavioral: descriptor count zero (:155) and `provider.GetRequiredService<IProjectSession>().HydraulicsState` `Is.SameAs(session.HydraulicsState)` (:159-161).

None of these are tautological: each negative fixture is a distinct violating string processed by the exact predicate applied to production sources, and each category adds an independent behavioral probe.

## F. DI composition sanity — PASS

- Construction cycle avoided by explicit factory (`src/Configuration/ServiceCollectionExtensions.cs:196-199`):
  ```csharp
  services.AddSingleton(sp => new ProjectSession(
      sp.GetRequiredService<IClimateData>(),
      sp.GetRequiredService<CalculationContext>(),
      hydraulicsDirtyService: null));
  ```
  with the deadlock rationale in-source (:191-195) matching `task-9/divergence-notes.md` («DI construction-cycle deadlock fixed»); `null` is canonical because the slice falls back to the session itself (`ProjectSessionHydraulicsState` receives `hydraulicsDirtyService ?? this`, `ProjectSession.cs:46`).
- Coordinator singleton bound to the session slice instance (`ServiceCollectionExtensions.cs:148-151`): factory resolves `sp.GetRequiredService<ProjectSession>().HydraulicsState` — no separate slice registration anywhere.
- Reference-equality proofs: `tests/SnowMeltingCalculator.Tests/Configuration/DiRegistrationTests.cs:258-274` (`Is.SameAs` through concrete + `IProjectSession` aliases) and `:280-288` (`GetServices<IProjectSessionHydraulicsState>()` empty, `GetService<ProjectSessionHydraulicsState>()` null); independently re-proven by guard category 8.
- No service→VM wiring added (see C).

## G. Code quality judgment — PASS with notes

- **Naming consistency:** new surface mirrors the accepted phase-4 thermal vocabulary exactly — `HydraulicsMutationStatus { Changed, NoChange, Rejected }` + `HydraulicsMutationResult` parallels `ThermalMutationStatus`/`ThermalMutationResult` (`src/Services/Project/ThermalMutationResult.cs:13,83`); snapshot/origin/coordinator naming parallel thermal precedents. Consistent.
- **XML docs:** plan contract required the persistence mapper to carry an XML-doc fixing the full wire-set («аналог ThermalPersistenceMapper :16-37»). `ThermalPersistenceMapper` documents every wire field group (:16-37); `HydraulicsPersistenceMapper` carries only a 4-line class summary (`HydraulicsPersistenceMapper.cs:8-12`) without the field-by-field enumeration. Likewise the new public surface (`IProjectSessionHydraulicsState`, `ProjectSessionHydraulicsState`, `IHydraulicsStateCoordinator`, snapshot classes) has no XML docs, unlike neighboring thermal files. Documentation gap vs plan text — non-behavioral, cheap to fix, does not affect ownership or correctness.
- **Redundant dirty calls:** `CircuitsViewModel.cs:1311` and `:1317` still call `_markDirtyService.MarkDirty()` immediately after `ApplyGlobalInputs(..., User)` — which already raised dirty through the canonical slice (`state :97`). Idempotent at the aggregate root (`ProjectSession.MarkDirty` early-returns, `ProjectSession.cs:85-94`), so observable dirty semantics are unchanged, but it contradicts the divergence-note wording that dirty intent "no longer originates in CircuitsViewModel" for the InputData path specifically. Cleanup candidate; not a defect.
- **Dead code introduced by this phase:** none found. (`AutoSelectCollectorType()` at `CircuitsViewModel.cs:1225` is uncalled but verifiably dead at baseline `471c4f1` too — pre-existing.)
- **Pre-existing debts out of scope (noted, not charged):** dead `CalculationContext._hydraulicsResults` (`src/Core/CalculationContext.cs:122`, explicitly deferred by plan Q1 decision); service→VM debts of C1/C2 extractions (see C); ST-005 duplicate `IsLoadProjectInProgress`.

## Residual risks

1. **R-B1 (verdict-driving):** undocumented removal of global→per-circuit propagation of `SupplySpacing_cm`/`SupplyHeatPercent` (detail and full evidence chain under B). Observable behavior change in a live-edit flow; contradicts frozen Must-NOT-Have #1; invisible to every executed gate. Requires owner adjudication (restore semantics or document deviation + pinning coverage) before result acceptance.
2. **R-G1:** missing full wire-set XML doc on `HydraulicsPersistenceMapper` and XML docs on the new public surface (plan-text deviation, documentation-only).
3. **R-G2:** redundant VM-side `MarkDirty()` calls on the InputData path (idempotent; wording drift vs divergence notes).
4. **R-C1:** pre-existing service→VM dependencies (`ResultsPdfDataBuilder`, `ProjectLoadOrchestrator`, hydraulics validator/selector interfaces) remain and now include adapter calls (`ApplyLifecycleSnapshotToAdapter`) — unchanged class of debt, correctly out of scope, should stay on the lifecycle-cleanup radar.
5. **R-A1:** `GlobalInputs.SupplySpacingCm/SupplyHeatPercent` are currently persistence-only values (consequence of R-B1); if R-B1 is resolved by restoring propagation, sole-owner semantics become fully end-to-end.

## Verdict basis

A, C, D, E, F pass on direct source evidence; G passes with documentation notes. B fails its explicit breadth criterion: the diff contains one hunk beyond ownership transfer whose effect is an undocumented observable behavior change against the frozen plan. Per the review contract, APPROVE requires all checklist items to pass.

REVIEW_ID: f2-phase5-architecture
SUBJECT: phase-5-hydraulics-state@0D48A757DDD1B98384D131D76C7AF5220206260BF853FADF12BA859329FC8D38
RECEIPT: docs/architecture-migration/evidence/phase-5-hydraulics-state/final/f2/architecture-quality.md
VERDICT: REJECT
REASON: Checklist item B fails: versus pre-phase-5 baseline 471c4f1, the CircuitsViewModel input handler dropped the legacy propagation of global SupplySpacing_cm/SupplyHeatPercent into circuit rows (baseline handler assigned circuit.SupplySpacing_cm/InputData.SupplyHeatPercent to every row before Calculate; current CircuitsViewModel.cs:1292-1320 publishes ApplyGlobalInputs without propagation, and no src/ code assigns rows from globals). Since CircuitsCalculator.CalculateCircuitPower (CircuitsCalculator.cs:34-37) consumes the per-row values, editing the user-editable global fields (CircuitsView.xaml:306,320) no longer changes hydraulic results, per-circuit display (xaml:567,575), or saved per-circuit wire fields (HydraulicsPersistenceMapper.cs:78-79) — an undocumented observable behavior change violating the frozen Must-NOT-Have #1, outside the four owner-adjudicated deviations in task-9/divergence-notes.md and uncovered by characterization (count-only pins), round-trip tests, and UI QA (field-value-only assertions). All other items pass: sole writable owner ST-016..ST-019 verified by exhaustive sweeps (exactly one UpdateHydraulics production site, HydraulicsStateCoordinator.cs:57; unconditional per-attempt status termination :80-83); adapter structure, snapshot immutability, honest 8/8 guard self-checks, DI factory/reference-equality proofs, and no new service→VM dependencies (baseline-verified pre-existing only).
