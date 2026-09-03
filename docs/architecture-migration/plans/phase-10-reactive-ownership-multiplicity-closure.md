# phase-10-reactive-ownership-multiplicity-closure - Work Plan

## TL;DR (For humans)

This plan closes the last open architecture invariants left standing at Phase 9 acceptance. Its scope comes directly from the preserved open items recorded in the Phase 9 dossier: `INV-010` (every reactive subscription must have an explicit owner, lifetime, unsubscribe/disposal rule, and multiplicity expectation; downstream invalidation must consume completed logical changes, not ViewModel implementation details), the global closure of `INV-006` (exactly one writable canonical owner per value) and `INV-007` (ViewModels are WPF adapters only), which the dossier says is blocked only by the still-open `INV-010`, and the broader mutation-boundary portions of `INV-016` (distinguishable user vs system mutation paths, one identifiable logical-change completion boundary per user action). Four work areas, one sequential lane:

1. **Reactive census** — the reactive map (`RE-001..RE-014` plus per-phase overlay restatements) still records "unsubscribe not observed" on several edges and "unknown" in every runtime counter column (ContextChanged, StateChanged, calculator invocations, Results projection updates, dirty transitions). Several of its line references predate Phase 9 (Results and the orchestrator were reworked by slices 3-6). This slice re-grounds every edge against the live post-Phase-9 code and produces a complete subscription census of production code: every `+=` subscription to `CalculationContext.ContextChanged`, `ICalculationStateService.StateChanged`, `PipeSpacingChanged`, coordinator `Completion`/`CompleteChanged` events, adapter collection events, and `PropertyChanged`, each with owner, lifetime, unsubscribe rule, and multiplicity expectation.
2. **Multiplicity measurement** — subscription-lifecycle counting tests (the `INV-011` evidence style: repeated new/load/second-load/reset/repeated-reset cycles keep handler/event/calculator counts stable), proven sensitive by a RED probe (a deliberately injected duplicate subscription in test code must fail the harness). The measured counts fill the reactive map's "unknown" columns as receipt facts, never estimates.
3. **Consolidated mutation-boundary proofs** — one cross-slice acceptance suite proving, for every migrated slice (Climate, Construction, Thermal, Hydraulics, Results, Shell), that user-visible mutations cross public state/application boundaries, produce exactly one identifiable logical-change completion boundary (including actions that change multiple internal fields), and that load/reset/restore/system-apply paths are distinguishable and create no user dirty/history candidate. No undo/redo stacks are implemented — `INV-016` explicitly does not require them.
4. **Global closure and dossier finalization** — flip `INV-010`, then the global `INV-006`/`INV-007` statuses and the remaining `INV-016` portions, with a machine-checked writer inventory (the `INV-006` verification method) and adapter characterization evidence; refresh the reactive map (including its embedded structural QA, which today *requires* an "unknown" per row and must be adapted to require measured counts with provenance instead); record `EV-P10-*`; regenerate the widget deterministically; handle the verifier exemplar — `verify-widget.mjs` cites `INV-010` as the synthetic unverified exemplar, so after `INV-010` verifies the exemplar must re-point, and if no genuinely open invariant remains the slice stops with `OWNER_DECISION_REQUIRED`.

The plan preserves everything the owner accepted in Phases 1-9: `DEC-001 = A` (`CalculationContext` stays the downstream read-projection seam; no writer changes to `ST-020..ST-022`), the Phase 6 save boundary, the Phase 7 restore boundary (validation order, validate-first, exactly-once publication, rejected-restore preservation), the Phase 8 derived Results projection, the Phase 9 closed seams (Results-owned projections, `IProjectLoad*Adapter` decoupling, removed forwarding aliases, LIM-P8-2 decision B), and `.smc` wire compatibility. Explicitly out of scope: any invalidation-semantics redesign, any publication fan-out change beyond lifetime hygiene, `.smc` schema drift, Markdown removal, export/PDF/Excel/preview/print behavior change, manual WPF QA (RR-002 stays a recorded environment limitation), and the RR-004 external fixture. The main risk is that counting-driven "hygiene fixes" quietly change invalidation behavior; the plan answers that with a hygiene-only write-set, a named `OWNER_DECISION_REQUIRED` stop, and the frozen contract suites as the gate — not with trust.

## Scope

### Authority and frozen-plan lifecycle

- Authoring candidate: `docs/architecture-migration/plans/phase-10-reactive-ownership-multiplicity-closure.md`, authored directly in the canonical plans location. Terminal review and owner plan approval freeze this file; a `.omo/plans/` mirror, if created later, is an operational execution ledger only and is not a second authority.
- Active dossier authority: `docs/architecture-migration/AGENTS.md` and the latest `docs/architecture-migration/TASK_CONTEXT.md`.
- Planning approval authorizes plan writing only; execution still belongs to a separate worker session started by the user.
- Todo write-sets and commands below describe downstream worker execution, not this planning session.
- Baseline: the tracked worktree is clean at commit `e9e45c4` (phase-9 dossier: plan, terminal review, owner gates, evidence, model `INV-008` verified, verifier exemplar `INV-010`, widget); the only untracked paths are unrelated `docs/workspace/*` presentation files and stay protected. The dirty baseline-relative delta discipline applies to a clean tracked baseline; every slice receipt must record its exact write-set.
- The Phase 9 dossier is internally consistent (post-acceptance verifier exemplar amendment + model consistency fix recorded in `evidence/phase-9-legacy-seams-cleanup/verifier-exemplar-amendment.md`); this plan builds on those final hashes and supersedes them only through its own Slice 7 records.

### In scope

- A complete production-code reactive census, grounded on the live post-Phase-9 tree: every subscription site for `CalculationContext.ContextChanged`, `ICalculationStateService.StateChanged`, `PipeSpacingChanged`, coordinator completion events (`ThermalStateCoordinator`, `HydraulicsStateCoordinator`, `ProjectSessionConstructionState.CompleteChanged`, `ProjectSessionClimateState` completion), hydraulics adapter input/collection events, and `PropertyChanged` subscriptions — each recorded with owner, lifetime class (singleton application-lifetime vs per-view teardown), unsubscribe rule, and multiplicity expectation, mapped to the `RE-001..RE-014` edges (overlay restatements deduplicated against live code).
- Measured runtime counters for every reactive edge — ContextChanged publications, StateChanged publications, calculator invocations, Results projection updates, dirty transitions — as receipt facts from executable counting harnesses, replacing the "unknown" columns; per-edge unsubscribe/lifetime verdicts replacing "unsubscribe not observed".
- New subscription-lifecycle tests, fixed by this plan as `ReactiveSubscriptionLifecycleTests`, exercising repeated new/load/second-load/reset/repeated-reset cycles and asserting stable handler/event/calculator/dirty counts; the suite is proven sensitive by a RED probe (a test-only duplicate subscription makes the harness fail; the RED run is recorded), mirroring the `ApplicationServiceViewModelDecouplingTests` RED-then-GREEN precedent.
- Hygiene-only fixes for leaks the harness proves (a missing unsubscribe that measurably multiplies handlers across lifecycle cycles); singleton application-lifetime subscriptions are *justified in the census*, not "fixed". If a proven leak cannot be fixed without touching publication or invalidation semantics, the slice stops with `OWNER_DECISION_REQUIRED`.
- Consolidated `INV-016` mutation-boundary acceptance tests, fixed by this plan as `MutationBoundaryConsolidationTests`, covering all migrated slices: user mutation → public state/application boundary → exactly one logical-change completion boundary (including a multi-field single action, e.g. a Thermal mode change touching mode + temperatures + spacing in one commit); load/reset/restore/system-apply origins produce no user dirty/history candidate; no proof requires interception of ViewModel setters, commands, or internals; no undo/redo stacks, persistence, or UI commands are implemented.
- Global closure evidence and dossier finalization: `INV-010` → verified/implemented with the counting-harness receipts; `INV-006` → verified via the machine-checked writer inventory (a script/grep proof that no canonical value has two writable owners across `ST-001..ST-027`, with `DEC-001 = A` read-projection sites documented as non-owning); `INV-007` → verified via the constructor-contract inspection and the adapter characterization suites already in the tree, scoped honestly to the migrated write-set; the remaining `INV-016` portions → verified with the consolidated suite; reactive map counters and structural QA adapted; `EV-P10-*` model records; deterministic widget regeneration; verifier exemplar disposition (owner-authorized re-point, or the explicit no-open-invariant decision); one dated `TASK_CONTEXT.md` entry per gate.

### Must NOT have

- No change to `CalculationContext` writers or the `ST-020..ST-022` seam disposition (`DEC-001 = A` intact); no new production writer into `CalculationContext`; no change to what publishes or when a publication happens — only *how many handlers remain subscribed* may be fixed, and only where the harness proves multiplication.
- No invalidation-semantics redesign: no new events, no event-splitting, no re-routing of downstream consumers between compat surfaces and canonical completions, no change to origin classification (`User`/`Template`/`Load`/`ProjectLoadReset`/`Restore`/`SystemApply`/`Initialization`/`UserReset`/`NoChange`/`Rejected`).
- No `.smc` schema/persistence-format change, no `ProjectData` wire-shape change, no Markdown removal, no export/PDF/Excel/preview/print behavior change.
- No redesign of the Phase 7 restore boundary or the Phase 6 save boundary; the Phase 8 derived Results projection and the Phase 9 closed seams (Results-owned `CircuitRow` projections, `IProjectLoad*Adapter`, `IReport*Source`, alias removal, LIM-P8-2 decision B import-less restore) are preserved as accepted results, re-proven — not reworked.
- No undo/redo implementation: no history stacks, no snapshots for history, no UI commands — `INV-016` explicitly does not require them.
- No manual-QA claims: RR-002 (headless environment, no manual WPF button/dialog/print QA) remains a recorded environment limitation; this plan adds none and pretends none.
- No global-status claim beyond the migrated write-set: `INV-006`/`INV-007` flips are scoped to `ST-001..ST-027` with the scope statement recorded verbatim in the dossier.
- No production-code implementation in this planning session; no test execution in this planning session.
- No `TASK_CONTEXT.md` change during execution except the dated dossier entries named in Slice 7.

## Verification strategy

- Grounding is exploration-first: the worker re-verifies every inventory claim below against live code before editing. All file/line references in this plan are planning-time observations from the Phase 9 dossier maps (several reactive-map line anchors predate the Phase 9 reworks of `ResultsViewModel` and `ProjectLoadOrchestrator` and are expected to have drifted); they are not frozen truths.
- Each execution slice has agent-executable happy/failure QA with a concrete command or test filter and a receipt path; the downstream worker, not the planner, executes those commands. Build-before-test is mandatory: `dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo` before any `dotnet test --no-build`; any focused command executing 0 tests is a failed verification even if the exit code is 0.
- Characterization is never weakened: the Phase 2-9 frozen contracts (per-origin completion semantics, exactly-once publication, result-zeroing without recalculation, projection multiplicity, fresh-vs-stale sentinel, rejected-restore preservation, dirty ownership) stay in force. Counting harnesses add assertions; they never replace the existing per-slice receipts.
- The RED probe (Slice 3) makes the counting harness trustworthy: a harness that cannot fail on an injected duplicate subscription proves nothing. The RED run is recorded in the receipt exactly like the Phase 9 static-test RED runs (`slice-5-static-test-RED/GREEN.trx` precedent).
- The verifier exemplar (Slice 7) follows the owner-authorization gate of the Phase 7.5 (`INV-001` → `INV-008`) and Phase 9 (`INV-008` → `INV-010`) precedents. Because this phase may leave *no* genuinely open invariant, the slice presents the owner a concrete choice (see Slice 7) and stops with `OWNER_DECISION_REQUIRED` until the owner records it in-session or at plan approval.
- Evidence reuse: Phase 2-9 receipts are the frozen-contract baseline this phase re-proves against; they are never rewritten. The reactive map is updated by dated Phase 10 overlay + counter-column fills, preserving the historical overlay sections.

## Execution strategy

One sequential lane. Read-only inspection and evidence gathering can be parallel where independent. The worker locks the reactive baseline and census first, converts unknowns into measured facts with a sensitivity-proven harness, fixes only proven leaks under a hygiene-only write-set, consolidates the `INV-016` proofs, and closes with the full regression and the dossier/global-closure refresh. Any slice that would alter behavior outside lifetime hygiene must stop for owner decision. Prometheus does not run product tests in this session; the worker creates the evidence directory, builds, runs the exact commands, and rejects 0-test executions.

## Todos

- [ ] 1. Reactive surface (`src/Services/**`, `src/ViewModels/**`, `src/Views/**`): lock the reactive baseline and the full subscription census - expect every production subscription inventoried with owner, lifetime, unsubscribe rule, and multiplicity expectation

  **Goal:** Freeze the current reactive behavior on the clean baseline and record the complete census this phase will measure and close.

  **Write-set / change class:** tests/evidence only at this stage. No production edits in this slice.

  **References:** `docs/architecture-migration/maps/reactive.md` (edges `RE-001..RE-014` and per-phase overlays; expected-stale anchors: Circuits `:728-730,1062-1082,1202-1206,724-726,1024-1319`, `CalculationStateService.cs:146-168,226-235`, `ThermalViewModel.cs:266-267,438-460`, `ConstructionViewModel.cs:258`, `MainViewModel.cs:178-225`, Results `:730-756,778-825,945-968,1573-1607`, Orchestrator `:56-70,132-155,171-173` — the worker re-grounds every anchor against the live post-Phase-9 tree); `src/ViewModels/Hydraulics/CircuitsViewModel.cs`; `src/ViewModels/Thermal/ThermalViewModel.cs`; `src/ViewModels/Construction/ConstructionViewModel.cs`; `src/ViewModels/Climate/ClimateViewModel.cs`; `src/ViewModels/Shell/MainViewModel.cs`; `src/ViewModels/Results/ResultsViewModel.cs`; `src/Core/CalculationContext.cs`; `src/Services/Navigation/CalculationStateService.cs`; `src/Services/Project/{ThermalStateCoordinator,HydraulicsStateCoordinator,ProjectSessionConstructionState,ProjectSessionClimateState}.cs`; Phase 9 receipts under `docs/architecture-migration/evidence/phase-9-legacy-seams-cleanup/` (the reworked Results/orchestrator/adapter surfaces).

  **Acceptance:** The receipt contains: (a) every production subscription site found by search (`+=` on the event surfaces named above) with file/line, subscriber instance, publisher, and the mapped `RE-` edge ID (or `NEW` if the census finds an edge the map lacks — such a find stops the lane as `OWNER_DECISION_REQUIRED` only if it changes the phase boundary, otherwise it joins the map in Slice 7); (b) a per-edge row: owner, lifetime class (application-lifetime singleton vs per-view), unsubscribe rule (explicit `-=` / `Dispose` / none-by-design), multiplicity expectation (expected handler count after repeated cycles); (c) the current counter column state from the map, marked as pre-measurement baseline; (d) the stabilization suites pass unmodified on the baseline.

  **Happy QA:** The existing lifecycle, stabilization, and multiplicity suites pass unchanged, proving the baseline is the accepted Phase 9 state.

  **Failure QA:** A subscription site that cannot be classified (no identifiable owner or lifetime) stops the slice with the gap recorded; a silently dropped site discovered later in the lane is a blocker.

  **Commands / receipt:** `mkdir -p docs/architecture-migration/evidence/phase-10-reactive-ownership-multiplicity-closure/logs`; `dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo`; `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~ProjectLifecycleFlowCharacterizationTests|FullyQualifiedName~ResetOrchestrationTests|FullyQualifiedName~ThermalMultiplicityCharacterizationTests|FullyQualifiedName~HydraulicsMultiplicityCharacterizationTests" --logger "trx;LogFileName=slice-1-reactive-census.trx" --results-directory "docs/architecture-migration/evidence/phase-10-reactive-ownership-multiplicity-closure/logs"`; receipt `docs/architecture-migration/evidence/phase-10-reactive-ownership-multiplicity-closure/slice-1-reactive-census.md`.

  **Next gate / Commit:** only Todo 2 after PASS. Expected receipt is a reactive census with the per-edge ownership table.

- [ ] 2. Counting harness design freeze and baseline counts: record pre-fix counters per edge - expect measured numbers for every "unknown" column on the accepted baseline

  **Goal:** Turn the harness from design into executable evidence: run the counting instrumentation (counting harnesses already exist for the coordinator dirty-intent path from Phase 9; this slice generalizes the approach) over the frozen repeated-cycle scenarios and record the baseline counters per `RE-` edge.

  **Write-set / change class:** tests/evidence only. Harness code lands in the test project only; no production edits in this slice.

  **References:** Slice 1 census receipt; `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectLifecycleFlowCharacterizationTests.cs`; `tests/SnowMeltingCalculator.Tests/ViewModels/ResetOrchestrationTests.cs`; Phase 7 slice-7 repeated-cycle contract (`docs/architecture-migration/plans/phase-7-project-restore-coordinator-relaunch.md`); `docs/architecture-migration/maps/reactive.md` overlay rows `RE-003`, `RE-009` (the only edges with non-unknown measured columns today).

  **Acceptance:** The receipt records per-edge measured counts for the frozen scenarios (new calculation; load; second load; reset; repeated reset) with at least: ContextChanged publications, StateChanged publications, calculator invocations, Results projection updates, dirty transitions; every count is stable across two consecutive identical runs; counts consistent with the characterized exactly-once contracts (one publication per completed logical change) are annotated as such; any count that contradicts a frozen contract stops the lane as `OWNER_DECISION_REQUIRED`.

  **Happy QA:** Two consecutive harness runs produce identical numbers (determinism proof).

  **Failure QA:** A count that differs between identical runs, or a harness that observes fewer surfaces than the Slice 1 census lists, fails the slice; quietly dropping an unobservable edge from the receipt is a blocker.

  **Commands / receipt:** `mkdir -p docs/architecture-migration/evidence/phase-10-reactive-ownership-multiplicity-closure/logs`; `dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo`; `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~ReactiveSubscriptionLifecycleTests" --logger "trx;LogFileName=slice-2-baseline-counts.trx" --results-directory "docs/architecture-migration/evidence/phase-10-reactive-ownership-multiplicity-closure/logs"`; receipt `docs/architecture-migration/evidence/phase-10-reactive-ownership-multiplicity-closure/slice-2-baseline-counts.md`.

  **Next gate / Commit:** only Todo 3 after PASS. Expected receipt is a baseline-counts note with per-edge tables. (If the harness must exist before its suite can run, the worker creates `ReactiveSubscriptionLifecycleTests` in this slice as test-only code; the name is fixed by this plan and reused in Slices 3-4.)

- [ ] 3. `ReactiveSubscriptionLifecycleTests` (new test class) with RED probe: prove the harness detects multiplication, then prove the baseline is stable - expect RED probe fail recorded, then stable-counts GREEN

  **Goal:** Make the counting assertions binding: a deliberately injected duplicate subscription (test code only) must fail the harness (recorded RED run); without the injection the suite proves stable handler/event/calculator/dirty counts across repeated new/load/second-load/reset/repeated-reset cycles — the executable heart of `INV-010`.

  **Write-set / change class:** tests only. No production edits in this slice.

  **References:** Slice 1 census; Slice 2 baseline counts; the Phase 9 RED-then-GREEN precedent (`docs/architecture-migration/evidence/phase-9-legacy-seams-cleanup/` slice-5 receipts, `ApplicationServiceViewModelDecouplingTests`); `docs/architecture-migration/maps/target-invariants.md` row `INV-010` (verification method: subscription-lifecycle tests with stable counts plus one identifiable completion boundary per logical user action).

  **Acceptance:** (a) The RED probe run (duplicate handler injected in test scaffolding) is recorded as a failing TRX before the GREEN run; (b) the GREEN run asserts, for every edge with a multiplicity expectation from the census: identical handler count before/after each lifecycle cycle, exactly-once publication per completed logical change, one recalculation path per valid Thermal publication (the two-glycol-read guard precedent), zero dirty transitions from load/reset/restore origins; (c) per-slice completion-boundary assertions: one identifiable completion boundary drives downstream invalidation for one logical user action, including at least one multi-field single action per slice (Climate snapshot change; Construction layer edit; Thermal mode change; Hydraulics collector edit).

  **Happy QA:** The suite passes with stable counts matching the Slice 2 baseline.

  **Failure QA:** A GREEN run on code the RED probe just failed, a baseline count drift without a recorded production change, or a missing RED record are blockers; weakening an assertion to reach stability is a blocker.

  **Commands / receipt:** `mkdir -p docs/architecture-migration/evidence/phase-10-reactive-ownership-multiplicity-closure/logs`; `dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo`; `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~ReactiveSubscriptionLifecycleTests" --logger "trx;LogFileName=slice-3-lifecycle-RED.trx" --results-directory "docs/architecture-migration/evidence/phase-10-reactive-ownership-multiplicity-closure/logs"` (RED probe; failing run recorded); fix the probe, rerun: same filter, `slice-3-lifecycle-GREEN.trx`; receipt `docs/architecture-migration/evidence/phase-10-reactive-ownership-multiplicity-closure/slice-3-lifecycle-tests.md`.

  **Next gate / Commit:** only Todo 4 after PASS. Expected receipt is a lifecycle-tests note with both TRX files and the probe description.

- [ ] 4. Production subscription surfaces named by the census as leaking: hygiene-only unsubscribe fixes - expect proven leaks closed with no publication/invalidation semantics change, or a recorded no-op

  **Goal:** Close exactly the leaks the harness proved: subscriptions whose handlers measurably multiply across lifecycle cycles because teardown never unsubscribes. This slice is intentionally allowed to be a no-op: if the census and harness show every edge is either application-lifetime-by-design or already correctly unsubscribed, the receipt records that and the lane continues.

  **Write-set / change class:** production/test. The write-set is exactly the subscription sites the census names as leaking, plus the tests pinning the fix. Unsubscribe/Dispose wiring only.

  **References:** Slice 1 census (a)-(b) rows; Slice 3 GREEN counts; `src/ViewModels/Hydraulics/CircuitsViewModel.cs`; `src/ViewModels/Thermal/ThermalViewModel.cs`; `src/Services/Navigation/CalculationStateService.cs`; `src/Services/Project/ProjectRestoreAdapters.cs` (adapter-lifetime subscriptions introduced by Phase 9 slice 5/6); `docs/architecture-migration/maps/reactive.md` rows recording "unsubscribe not observed" (`RE-001`, `RE-002`, `RE-004..RE-007`, `RE-010..RE-012` as re-grounded).

  **Acceptance:** After the fixes, the lifecycle suite holds stable counts across doubled cycle repetitions (the harness runs more cycles than Slice 3 to prove the fix, not just the baseline); every fixed site has a census-row update (unsubscribe rule filled); every application-lifetime subscription without a fix has an explicit by-design justification line in the receipt; no publication order, no origin classification, no event payload changed (proven by the unmodified frozen contract suites passing).

  **Happy QA:** The frozen Phase 2-9 contract suites (lifecycle, climate-thermal invalidation regression, multiplicity, stabilization) pass unmodified alongside the strengthened lifecycle suite.

  **Failure QA:** Any change to when or what an event publishes, any new event surface, any origin reclassification, or a "fix" for a subscription the harness did not prove to multiply is a blocker and stops the lane as `OWNER_DECISION_REQUIRED` if the worker believes semantics must change.

  **Commands / receipt:** `mkdir -p docs/architecture-migration/evidence/phase-10-reactive-ownership-multiplicity-closure/logs`; `dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo`; `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~ReactiveSubscriptionLifecycleTests|FullyQualifiedName~ProjectLifecycleFlowCharacterizationTests|FullyQualifiedName~ClimateThermalInvalidationRegressionTests|FullyQualifiedName~ThermalMultiplicityCharacterizationTests|FullyQualifiedName~HydraulicsMultiplicityCharacterizationTests" --logger "trx;LogFileName=slice-4-leak-hygiene.trx" --results-directory "docs/architecture-migration/evidence/phase-10-reactive-ownership-multiplicity-closure/logs"`; receipt `docs/architecture-migration/evidence/phase-10-reactive-ownership-multiplicity-closure/slice-4-leak-hygiene.md`.

  **Next gate / Commit:** only Todo 5 after PASS. Expected receipt is a leak-hygiene note (or a justified no-op note).

- [ ] 5. `MutationBoundaryConsolidationTests` (new test class) + existing per-slice suites: consolidate the INV-016 mutation-boundary proofs across all migrated slices - expect user vs system paths distinguishable with one completion boundary per logical action

  **Goal:** Prove the remaining `INV-016` portions in one place: for every migrated slice, user-visible mutations cross public state/application mutation boundaries and produce exactly one identifiable logical-change completion boundary; load/reset/restore/system-apply paths are distinguishable from user paths and create no user dirty/history candidate; multiple internal field changes are observable as one user action; no assertion requires interception of ViewModel setters, commands, or internals.

  **Write-set / change class:** tests only. No production edits in this slice.

  **References:** `docs/architecture-migration/maps/target-invariants.md` row `INV-016` (full text and verification method) and the per-phase overlays in `TASK_CONTEXT.md` (Phase 2 climate completion, Phase 3 construction completion, Phase 3.1 invalidation, Phase 4 thermal coordinator, Phase 5 hydraulics coordinator, Phase 8 Results derived consumption, Phase 9 closed seams); existing per-slice suites: `tests/SnowMeltingCalculator.Tests/Services/Project/{ProjectLifecycleFlowCharacterizationTests,ThermalMultiplicityCharacterizationTests,ClimateThermalInvalidationRegressionTests}.cs`; `tests/SnowMeltingCalculator.Tests/ViewModels/ResetOrchestrationTests.cs`; the Phase 9 dirty-ownership counting harnesses.

  **Acceptance:** (a) Each of the six slices (Climate, Construction, Thermal, Hydraulics, Results, Shell/Save) has at least one consolidated test proving: user edit → public boundary → one completion → downstream effect, with multi-field single-action coverage for at least one Climate, one Construction, and one Thermal scenario; (b) each lifecycle origin (load, reset, restore, system apply) is asserted to produce zero user-dirty transitions and zero user-history candidates; (c) no test accesses ViewModel internals (the suite compiles against public surfaces only — recorded as a grep/compile fact); (d) no undo/redo stack, snapshot, or command is introduced; the receipt states the invariant's "future recorder" hook remains a boundary property, not an implementation.

  **Happy QA:** The consolidated suite plus all per-slice suites pass together.

  **Failure QA:** A consolidated test that only re-states an existing per-slice assertion without the cross-slice distinguishability proof fails review; any test reaching into ViewModel internals is a blocker.

  **Commands / receipt:** `mkdir -p docs/architecture-migration/evidence/phase-10-reactive-ownership-multiplicity-closure/logs`; `dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo`; `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~MutationBoundaryConsolidationTests|FullyQualifiedName~ProjectLifecycleFlowCharacterizationTests|FullyQualifiedName~ResultsStabilizationPhase1BehaviorContractsTests|FullyQualifiedName~ProjectSaveServiceTests" --logger "trx;LogFileName=slice-5-mutation-boundaries.trx" --results-directory "docs/architecture-migration/evidence/phase-10-reactive-ownership-multiplicity-closure/logs"`; receipt `docs/architecture-migration/evidence/phase-10-reactive-ownership-multiplicity-closure/slice-5-mutation-boundaries.md`.

  **Next gate / Commit:** only Todo 6 after PASS. Expected receipt is a mutation-boundary consolidation note mapped slice-by-slice.

- [ ] 6. Full-suite regression: prove no drift from the accepted Phase 9 state beyond the named hygiene fixes - expect 0 failed except the known external-fixture skip

  **Goal:** Prove the whole application is behaviorally where Phase 9 acceptance left it, plus only the proven leak fixes and new tests: full suite green, `.smc` fixtures untouched, dirty/save/report fixtures unchanged.

  **Write-set / change class:** tests/evidence only.

  **References:** Phase 9 full-regression receipt (`evidence/phase-9-legacy-seams-cleanup/` slice-7, 2032 passed / 0 failed / 1 skip); `docs/architecture-migration/plans/phase-7-project-restore-coordinator-relaunch.md` slice-7 repeated-cycle contract.

  **Acceptance:** Full suite: 0 failed, exactly 1 known skip (RR-004 external fixture `D:\IA\ace\Тест\тест 40.smc`, recorded as skip, never as pass); test-count delta vs Phase 9 equals the tests this plan added (RED-probe scaffolding excluded from the final tree); no `.smc` fixture changed (`git diff --name-only -- '*.smc'` empty); the repeated-cycle counts match Slices 2-4.

  **Happy QA:** The full-suite TRX plus the unchanged-fixture proofs.

  **Failure QA:** Any new failure, any `.smc` fixture diff, or an unexplained test-count delta is a blocker.

  **Commands / receipt:** `mkdir -p docs/architecture-migration/evidence/phase-10-reactive-ownership-multiplicity-closure/logs`; `dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo`; `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --no-build --logger "trx;LogFileName=slice-6-full-regression.trx" --results-directory "docs/architecture-migration/evidence/phase-10-reactive-ownership-multiplicity-closure/logs"`; `git diff --name-only -- '*.smc'`; receipt `docs/architecture-migration/evidence/phase-10-reactive-ownership-multiplicity-closure/slice-6-full-regression.md`.

  **Next gate / Commit:** only Todo 7 after PASS. Expected receipt is a full-regression note.

- [ ] 7. `docs/architecture-migration/` maps, model, widget, verifier, and `TASK_CONTEXT.md`: global closure and dossier finalization - expect INV-010/INV-016 flipped with measured evidence, INV-006/INV-007 flipped with the machine-checked writer inventory, counters filled, widget regenerated, exemplar disposition owner-authorized

  **Goal:** Finalize the dossier so it describes the closed reactive and mutation-boundary surfaces: reactive map counter columns filled with Slice 2-4 measured facts (dated Phase 10 overlay; historical overlays preserved), the embedded reactive structural QA adapted (it currently *requires* an `unknown` in every `RE-` row; it must be changed to require measured counts with receipt provenance, and the adaptation recorded in the QA section), `INV-010` → verified/implemented with the lifecycle-suite evidence, `INV-016` → verified/implemented with the consolidated suite, `INV-006`/`INV-007` → verified via a machine-checked writer inventory across `ST-001..ST-027` (script/grep proof of single writable owners; `DEC-001 = A` read-projection sites documented as non-owning) plus the adapter-characterization suites, `EV-P10-*` model records, deterministic widget regeneration, and the verifier exemplar disposition.

  **Write-set / change class:** architecture artifacts plus one dated `TASK_CONTEXT.md` entry.

  **References:** `docs/architecture-migration/maps/{reactive,state-ownership,state-inventory,target-invariants,compile-time,di-runtime,user-flow,characterization-tests}.md`; `docs/architecture-migration/maps/architecture-model.json`; `docs/architecture-migration/widget/verify-widget.mjs` (exemplar `INV-010` in the `changed-unverified` and `added-survivor-unverified` synthetic scenarios — after the `INV-010` flip these assertions depend on a status that no longer exists); `docs/architecture-migration/architecture-widget.html`; `evidence/phase-9-legacy-seams-cleanup/verifier-exemplar-amendment.md` (the Phase 7.5-pattern gate and the hash-supersession record); Slices 1-6 receipts.

  **Decision contract (verifier exemplar):** Option A — re-point the exemplar to the next genuinely open invariant and execute the amendment (owner authorization required, same gate as Phase 7.5/9). Option B — if no invariant remains genuinely open, record the owner's chosen disposition among: keep a permanently-open synthetic placeholder ID; amend the verifier to source the exemplar scenarios from an injected synthetic status instead of a live model row; or retire the two exemplar assertions with the owner accepting the reduced verifier surface. The slice stops with `OWNER_DECISION_REQUIRED` until the owner records the choice in-session or at plan approval. No suite may be left failing while the decision is pending.

  **Acceptance:** The reactive map shows measured counts with provenance on every `RE-` row and the adapted structural QA passes; the model records the four flips with `EV-P10-*` and the honest scope statement for the global `INV-006`/`INV-007` verdicts (migrated write-set `ST-001..ST-027`); the machine-checked writer inventory output is stored in the receipt; `node docs/architecture-migration/widget/verify-widget.mjs` passes both suites, `generate-widget.mjs --check` passes, and the widget is reproducible; the dated `TASK_CONTEXT.md` entry records the Phase 10 execution result, the leak-hygiene outcome (fixes or justified no-op), and the exemplar disposition; RR-002 and RR-004 are re-stated as preserved limitations, not closed.

  **Happy QA:** Verifier suites and widget-generation check pass; dossier statements map one-to-one to slice receipts; the writer-inventory script output is reproducible.

  **Failure QA:** Any global-status claim without the writer-inventory receipt, any RR-002 closure claim, any `CalculationContext` writer change, or an unauthorized exemplar edit is a blocker; a dossier flip contradicted by a slice receipt is a blocker.

  **Commands / receipt:** `mkdir -p docs/architecture-migration/evidence/phase-10-reactive-ownership-multiplicity-closure`; `node docs/architecture-migration/widget/verify-widget.mjs`; `node docs/architecture-migration/widget/generate-widget.mjs --check`; `git diff --check`; worker content-review of the architecture/evidence diff mapping each changed dossier statement to slice receipts; receipt `docs/architecture-migration/evidence/phase-10-reactive-ownership-multiplicity-closure/slice-7-dossier-global-closure.md`.

  **Next gate / Commit:** only the Final verification wave after PASS. Expected receipt is a dossier global-closure note with the writer-inventory output and the exemplar decision record.

## Final verification wave

- [ ] F1. Scope, provenance and invariant check - expect the plan to preserve the approved Phases 1-9 contracts and the current Phase 10 boundary

  Verify canonical plan identity, scope, must-not-have rules, and the preserved invariants from the approved earlier phases. Confirm the plan still rejects `CalculationContext` writer changes (`DEC-001 = A`), publication/invalidation semantics changes, `.smc` format drift, Markdown/export work, restore/save boundary redesign, and undo/redo implementation.

- [ ] F2. Code-boundary and architecture check - expect every subscription owned, every counter measured, and no unproven global claim

  Independently inspect the live source against the census: every subscription site has an owner/lifetime/unsubscribe/multiplicity row; the leak fixes match the harness-proven list; the writer-inventory receipt supports the global `INV-006`/`INV-007` flips; the reactive map counters trace to TRX facts. Confirm the counting suite and the consolidated mutation-boundary suite are wired into the project and their names match this plan exactly.

- [ ] F3. Executable QA check - expect every slice to have agent-executed happy/failure coverage with named commands and receipts

  Verify that each slice lists a concrete command or test filter, an explicit build-before-test step where focused tests run with `--no-build`, an explicit receipt path, and at least one happy and one failure assertion. Do not re-run product tests in this planning session; this wave audits the worker-facing commands and receipts recorded in Todos 1-7. Any command plan that could pass with 0 matching tests fails this gate; the RED-probe record is mandatory for the counting suite's credibility.

- [ ] F4. Consolidated stop check - expect one final receipt set and then a stop for owner acceptance

  Consolidate the three review domains, confirm the plan is still within scope, and stop without execution. The result is only a plan handoff; any later execution still requires the separate worker session started by the user.

## Commit strategy

The planner does not stage, commit, or run product code. Only the Phase 10 plan artifact and its terminal-review receipt are authored in this session. Execution, if approved later, belongs to the separate worker session; parallel owner-side commits to the clean baseline do not conflict with planning artifacts because every Phase 10 artifact is a new file and the only shared file touched during execution (`TASK_CONTEXT.md`) is append-only in Slice 7.

## Success criteria

- The plan contains 7 execution slices plus 4 final verification gates, each with concrete commands, receipts, and happy/failure QA.
- Every production reactive subscription has a recorded owner, lifetime, unsubscribe rule, and multiplicity expectation; every reactive-map counter column holds a measured fact with TRX provenance; no "unknown" remains in the reactive behavior view.
- The counting harness is proven sensitive (recorded RED probe) and proves stable handler/event/calculator/dirty counts across repeated lifecycle cycles; any leak fix is hygiene-only and harness-proven; frozen Phase 2-9 contracts pass unmodified.
- The consolidated mutation-boundary suite proves user vs system paths, single completion boundaries, and multi-field single actions across all migrated slices, with no ViewModel-interception requirement and no undo/redo implementation.
- `INV-010` and the remaining `INV-016` portions flip to verified/implemented on executable evidence; the global `INV-006`/`INV-007` flips rest on a machine-checked writer inventory scoped to `ST-001..ST-027`; the reactive map's structural QA is adapted and passes; the widget regenerates deterministically and both verifier suites pass.
- The verifier exemplar disposition is an explicit, recorded owner decision; RR-002/RR-004 and `DEC-001 = A` are preserved honestly; the full suite is green with exactly the known RR-004 skip.
- The plan contains no `CalculationContext` writer work, no invalidation-semantics redesign, no `.smc`/export/Markdown work, no manual-QA claims, and no scope drift beyond the approved Phase 10 boundary.
