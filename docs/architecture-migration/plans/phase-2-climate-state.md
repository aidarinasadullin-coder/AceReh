# phase-2-climate-state - Work Plan

## TL;DR (For humans)

Этот план готовит выполнение Phase 2 архитектурной миграции Climate-state: `ProjectSession.ClimateState` станет единственным writable owner для проектных Climate-значений, а `ClimateViewModel`, `IClimateData`/`ClimateData`, `CalculationContext`, restore/save/export paths станут adapter/projection/compatibility surfaces без второго владения состоянием.

Почему так: текущая реализация держит writable Climate state в `ClimateViewModel`, затем копирует его в settable singleton `ClimateData` и downstream `CalculationContext`. Это нарушает целевой invariant `INV-002`: Climate values SHALL have one writable canonical owner in `ProjectSession.ClimateState`.

Что план НЕ делает: не начинает реализацию, не меняет `.smc` schema/version, формулы, UI design, packages, transactional restore, Undo/Redo, Results/Construction/Thermal/Hydraulics ownership или общий redesign `CalculationContext`.

Статус approval: пользователь одобрил только **создание этого планового артефакта**. Сам Phase 2 execution требует отдельного owner gate и отдельного worker session, например `$start-work phase-2-climate-state` / соответствующий architecture-start workflow.

## Scope

### In scope

- Current repository only: `D:\IA\ace v.2`.
- Climate project-state ownership vertical slice:
  - `src/ViewModels/Climate/ClimateViewModel.cs`;
  - `src/Models/Climate/ClimateData.cs` and `IClimateData` compatibility surface;
  - `src/Core/CalculationContext.cs` Climate projection seam only;
  - `src/Services/Project/ProjectSession.cs` and related project-session interfaces/adapters;
  - `src/Services/Project/ProjectLoadOrchestrator.cs` Climate restore/reset path;
  - `src/ViewModels/Results/ResultsViewModel.cs` Climate save/load/export projection reads;
  - DI registrations in `src/Configuration/ServiceCollectionExtensions.cs`.
- Characterization-first tests for current and target Climate behavior.
- `.smc` v1.0/v1.1 Climate compatibility without schema/version change.
- Architecture dossier updates after implementation gates:
  - six maps under `docs/architecture-migration/maps/`;
  - `architecture-model.json` generated through canonical scripts;
  - `architecture-widget.html` generated through canonical scripts;
  - `TASK_CONTEXT.md` and phase evidence receipts.

### Out of scope / Must-NOT-Have

- No migration of Construction, Thermal, Hydraulics, Results ownership.
- No general `CalculationContext` redesign or replacement.
- No `.smc` schema/version change and no byte-identical round-trip requirement unless current tests already require it; semantic round-trip is the default.
- No formulas change, validation-range change, city-search ranking change, UI redesign, package/version change, installer/publish change.
- No transactional restore/rollback redesign; current partial-load semantics must be characterized and preserved unless owner opens a separate phase.
- No Undo/Redo implementation, history stacks, persisted command history, snapshots, or UI commands. Phase 2 only preserves future compatibility through explicit mutation/completion boundaries.
- No edits to protected unrelated dirty paths, user `.smc` corpus under `Тест/`, presentations, historical audit inputs, archived drafts, generated widget/model by hand, build/publish artifacts, or unrelated OMO evidence.

### ClimateState contract to implement

The worker must encode this contract in production code and tests before rewiring consumers:

```csharp
public enum ClimateMutationOrigin
{
    User,
    Load,
    Reset,
    Restore,
    SystemApply,
    Initialization
}

public sealed record ClimateStateSnapshot(
    string SelectedCity,
    string SelectedRegion,
    double AirTemperature,
    double ColdFiveDayTemperature,
    double WindSpeed,
    double Humidity,
    double SnowfallIntensity,
    ClimateZone Zone,
    bool IsHighRequirements,
    bool IsCitySelected,
    bool HasUserModifications);

public interface IProjectSessionClimateState
{
    ClimateStateSnapshot Snapshot { get; }
    event EventHandler<ClimateStateChangedEventArgs> Changed;

    ClimateMutationResult ApplyCitySelection(CityInfo? city, bool isHighRequirements, ClimateMutationOrigin origin);
    ClimateMutationResult ApplyIndividualEdit(ClimateEdit edit, ClimateMutationOrigin origin);
    ClimateMutationResult ApplyProjectSnapshot(ClimateProjectData data, CityInfo? city, ClimateMutationOrigin origin);
    ClimateMutationResult ResetToDefaults(ClimateMutationOrigin origin);
    ClimateMutationResult ResetToCityData(ClimateMutationOrigin origin);
}
```

Implementation details may differ only if tests and dossier record an equivalent explicit API. Required semantics:

- One writable project-state owner: `ProjectSession.ClimateState` or a nested object owned by `ProjectSession`.
- UI-only state remains in `ClimateViewModel`: search query, popup state, filtered collections, selected suggestion index, loading indicator, validation display text, and recent-city UI collections unless a test proves they are persisted project state.
- `ClimateData` cannot remain an externally writable owner. It must become immutable snapshot/read projection, forwarding-only compatibility object, or internally mutable implementation unreachable from consumers except through the canonical owner.
- `CalculationContext` remains a downstream projection/invalidation seam. It must not own canonical Climate state.
- Authoritative sequence for each logical mutation:
  1. canonical state mutation or no-op detection;
  2. one logical completion result/event with origin;
  3. one compatibility projection update when state changed;
  4. one downstream invalidation publication through the documented seam;
  5. downstream reactions.
- No-op semantics must be tested for same city, same scalar value, repeated reset, identical load, and repeated projection update.

## Verification strategy

- TDD / characterization-first. The worker must add RED characterization tests for current behavior before changing ownership.
- Exact multiplicity values must be measured by tests or counters. Do not invent expected counts from static reading.
- Every behavior test must include a happy path and at least one failure/no-op/edge path.
- QA evidence must be agent-executable: `dotnet build`, `dotnet test` filters, generated receipts, hash/status checks, and generated model/widget verification.
- Dirty-worktree safety is a first-class gate: before edits, save a binary-safe protected status receipt and after edits prove no protected unrelated path changed.
- Architecture model/widget must be generated through `docs/architecture-migration/widget/generate-widget.mjs` and verified through `verify-widget.mjs`; never hand-edit generated outputs.

## Execution strategy

Recommended dependency chain:

1. Boundary and inventory baseline.
2. Characterization tests for current Climate logical actions and persistence.
3. Canonical `ClimateState` contract and single-owner guards.
4. Adapter/projection rewiring.
5. Restore/save/export/downstream compatibility rewiring.
6. Lifecycle, failure, subscription, and `.smc` compatibility gates.
7. Architecture dossier/model/widget refresh.
8. Final verification wave.

Use small commits after green gates; never stage or revert unrelated user changes.

## Todos

- [x] 1. Protected baseline: Capture repository identity and dirty boundary before edits - expect binary-safe receipts and protected path allow-list
  - References: `AGENTS.md`; `docs/architecture-migration/AGENTS.md`; `docs/architecture-migration/TASK_CONTEXT.md`; previous baseline HEAD `65959ee4a0e5b5308753c01994930367b53b94dc`.
  - Action: Run read-only git commands from `D:\IA\ace v.2`: `git rev-parse --show-toplevel`, `git rev-parse HEAD`, `git status --porcelain=v1 -z --branch`, `git diff --name-only -z`, `git diff --cached --name-only -z`.
  - Acceptance: Evidence under `docs/architecture-migration/evidence/phase-2-climate-state/` records git root, HEAD, branch/upstream, full NUL-safe dirty set, staged set, Climate-relevant dirty subset, and protected unrelated paths.
  - Happy QA: Re-run status after receipt creation; only allow-listed evidence files and later Phase 2 files may differ.
  - Failure QA: If HEAD moved or protected dirty set changed unexpectedly, stop and record blocker in `TASK_CONTEXT.md`; do not edit production/tests.
  - Commit: `test(architecture): capture phase 2 climate baseline`

- [x] 2. Current writer inventory: Add automated guard for every Climate writable path - expect exact current bypass list before migration
  - References: `src/ViewModels/Climate/ClimateViewModel.cs`; `src/Models/Climate/ClimateData.cs`; `src/Core/CalculationContext.cs`; `src/Services/Project/ProjectLoadOrchestrator.cs`; `src/ViewModels/Results/ResultsViewModel.cs`; `src/Configuration/ServiceCollectionExtensions.cs`.
  - Action: Add tests or analyzer-style reflection/AST guards that enumerate writable Climate fields/properties and direct setter call sites in production.
  - Acceptance: Guard identifies current writers: `ClimateViewModel` property setters/partials, `ClimateViewModel.SyncToClimateData()`, concrete `ClimateData` setters, `CalculationContext.UpdateClimate()`, `ProjectLoadOrchestrator.RestoreModulesFromProjectAsync()`, reset paths, and `ResultsViewModel` snapshot reads. It distinguishes writers from readers/projections.
  - Happy QA: `dotnet test tests\SnowMeltingCalculator.Tests --filter "FullyQualifiedName~ClimateStateLegacyStoreGuard|FullyQualifiedName~ClimateViewModelTests"` shows the characterization guard failing before implementation or explicitly capturing current legacy state.
  - Failure QA: Add a negative fixture/test proving a new direct setter in `ResultsViewModel` or `ProjectLoadOrchestrator` would fail the guard.
  - Commit: `test(climate): characterize writable climate ownership surface`

- [x] 3. Characterization counts: Lock current logical action multiplicity before rewiring - expect measured counts for dirty/event/context/downstream effects
  - References: `tests/SnowMeltingCalculator.Tests/Climate/ClimateViewModelTests.cs`; `tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/DoubleCalculationPreventionTests.cs`; `tests/SnowMeltingCalculator.Tests/Thermal/ThermalViewModelTests.cs`; `tests/SnowMeltingCalculator.Tests/Core/CalculationContextTests.cs`.
  - Action: Add counter-based characterization tests for city selection, scalar edit, high-requirements toggle, reset, reset-to-city-data, no-op edit, same-city selection, load, second load, and repeated load/reset.
  - Acceptance: Each test records final state, origin if available, logical completion count, `MarkDirty` transition/call count, `ClimateData.DataChanged` count, `ClimateViewModel.DataChanged` count if still present, `CalculationContext.UpdateClimate`/`ContextChanged` count, downstream thermal invalidation, circuits recalculation count, and Results refresh count where relevant.
  - Happy QA: Tests pass against current behavior after expected observed counts are encoded from empirical runs.
  - Failure QA: Include no-op tests proving unchanged scalar/city/repeated reset do not create unintended extra compatibility/downstream updates, or explicitly document current failing behavior as a migration target.
  - Commit: `test(climate): lock logical action multiplicity`

- [x] 4. ClimateState API: Implement canonical ProjectSession-owned Climate state contract - expect one explicit mutation/completion/origin boundary
  - References: `src/Services/Project/ProjectSession.cs`; `src/Services/Project/IProjectSession.cs`; Phase 1 `ProjectSession` lifecycle pattern; contract section in this plan.
  - Action: Add `IProjectSessionClimateState`/implementation or equivalent nested `ProjectSession.ClimateState`, `ClimateStateSnapshot`, mutation origin/result/event types, equality/no-op behavior, and validation/error reporting boundaries.
  - Acceptance: Tests prove all project Climate fields in the contract have one canonical owner, origin is included in completion results/events, no-op mutations return no-change results, and `User` origin is the only origin that creates user-dirty semantics unless explicitly documented.
  - Happy QA: `dotnet test tests\SnowMeltingCalculator.Tests --filter "FullyQualifiedName~ClimateStateTests|FullyQualifiedName~ProjectSessionTests"` passes.
  - Failure QA: Invalid Climate scalar input returns/throws the documented error without partial canonical mutation; repeated identical mutation returns no-change and emits no extra downstream completion.
  - Commit: `feat(project): add canonical climate state owner`

- [x] 5. Projection hardening: Convert ClimateData/IClimateData to non-owning compatibility projection - expect no externally writable ClimateData owner
  - References: `src/Models/Climate/ClimateData.cs`; `src/ViewModels/Thermal/ThermalViewModel.cs`; `src/Services/Thermal/ThermalCalculator.cs`; `src/ViewModels/Hydraulics/CircuitsViewModel.cs`; `src/Core/CalculationContext.cs`.
  - Action: Replace public writable `ClimateData` ownership with immutable snapshot/read projection, forwarding object, or internal mutable adapter updated only by canonical ClimateState completion.
  - Acceptance: Production consumers can read `IClimateData` values and receive `DataChanged` compatibility notifications, but no production code outside the approved projection updater can set Climate values on `ClimateData`.
  - Happy QA: Existing `ClimateViewModelTests`, `ThermalViewModelTests::ClimateDataChanged_ClearsResult`, and Hydraulics Climate integration tests pass after updates.
  - Failure QA: Guard test fails if a production caller casts `IClimateData` to concrete writable `ClimateData` and sets a property.
  - Commit: `refactor(climate): make climate data a projection`

- [x] 6. ClimateViewModel adapter: Rewire UI mutations through ClimateState - expect ViewModel has no canonical backing store
  - References: `src/ViewModels/Climate/ClimateViewModel.cs`; `src/Views/Climate/ClimateView.xaml`; `src/Controls/Climate/CityAutoCompleteBox.xaml.cs`; `tests/SnowMeltingCalculator.Tests/Climate/ClimateViewModelTests.cs`.
  - Action: Keep WPF binding surface stable while routing project-state mutations through `ProjectSession.ClimateState`. UI-only search/filter/popup/loading/validation display state remains local.
  - Acceptance: `ClimateViewModel` no longer calls `IMarkDirtyService.MarkDirty()` or `CalculationContext.UpdateClimate()` directly. It applies city selection and individual edits through canonical mutation methods and mirrors snapshots to observable properties without causing recursive duplicate completions.
  - Happy QA: Climate UI unit tests pass for city auto-fill, table 1.6 formulas, high requirements, validation, reset, reset-to-city-data, and sync/projection expectations.
  - Failure QA: Same-value UI setter and repeated reset do not emit duplicate canonical completion or downstream updates.
  - Commit: `refactor(climate): make climate view model an adapter`

- [x] 7. Restore and reset routes: Move ProjectLoadOrchestrator/MainViewModel climate writes to canonical boundaries - expect load/reset origins and partial-failure semantics preserved
  - References: `src/Services/Project/ProjectLoadOrchestrator.cs`; `src/ViewModels/Shell/MainViewModel.cs`; `src/ViewModels/Results/ResultsViewModel.cs`; `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectLifecycleFlowCharacterizationTests.cs`; `tests/SnowMeltingCalculator.Tests/ViewModels/ResetOrchestrationTests.cs`.
  - Action: Replace direct `ClimateViewModel` restore assignments and explicit `SyncToClimateData()`/`HasUserModifications=false` bypasses with canonical `ApplyProjectSnapshot(..., Load/Restore)` and `ResetToDefaults(Reset)` calls.
  - Acceptance: Load keeps existing non-transactional partial-failure contract, restore guard clears on exceptions, load/restore does not mark project dirty, second load replaces Climate values and projections with no stale project A values.
  - Happy QA: `dotnet test tests\SnowMeltingCalculator.Tests --filter "FullyQualifiedName~ProjectLifecycleFlowCharacterizationTests|FullyQualifiedName~ResetOrchestrationTests|FullyQualifiedName~ResultsViewModelOpenProjectTests"` passes.
  - Failure QA: Inject early/late restore failures and verify guard=false, documented partial state, dirty state unchanged per existing contract, and no duplicate subscriptions.
  - Commit: `refactor(project): route climate restore through session state`

- [x] 8. Persistence and Results projection: Read/write Climate snapshots from ClimateState - expect unchanged .smc semantic contract
  - References: `src/ViewModels/Results/ResultsViewModel.cs`; `src/Models/Project/ProjectData.cs`; `src/Services/Project/ProjectFileService.cs`; `src/Services/Reports/Calculation/Builders/ClimateSectionBuilder.cs`; `tests/SnowMeltingCalculator.Tests/Project/ProjectRoundTripTests.cs`; `tests/SnowMeltingCalculator.Tests/ViewModels/ResultsViewModelOpenProjectTests.cs`.
  - Action: Rewire `SaveCurrentProject()`, `LoadClimateData()`, PDF/Markdown projection sources, and report builders as needed so persisted/read values come from `ClimateState` snapshot or an explicitly derived DTO.
  - Acceptance: `ClimateProjectData` still has the same eight persisted fields and version marker behavior. `ColdFiveDayTemperature`, UI search state, filtered collections, validation display, and recent-city UI state are not newly persisted.
  - Happy QA: Add/keep tests for load v1.0, load v1.1, save v1.1, save/reload semantic round-trip, missing/default Climate fields, second-load replacement, and export projection.
  - Failure QA: Corrupt/invalid fixture fails through existing `ProjectFileService` failure path without partial writes to disk and without changing schema/version.
  - Commit: `refactor(results): snapshot climate from session state`

- [x] 9. Downstream invalidation: Establish one authoritative compatibility update sequence - expect no duplicate Thermal/Circuits recalculation
  - References: `src/Core/CalculationContext.cs`; `src/ViewModels/Thermal/ThermalViewModel.cs`; `src/ViewModels/Hydraulics/CircuitsViewModel.cs`; `tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/ClimateToHydraulicsIntegrationTests.cs`; `tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/DoubleCalculationPreventionTests.cs`; `tests/SnowMeltingCalculator.Tests/Thermal/ThermalViewModelTests.cs`.
  - Action: Route canonical Climate completion to exactly one projection update and exactly one downstream invalidation publication. Remove or suppress legacy duplicate `SyncToClimateData`/VM event paths.
  - Acceptance: One user logical change produces the target measured number of thermal invalidations and circuits recalculations; no-op produces none; load/reset origins produce the documented compatibility updates without user dirty semantics.
  - Happy QA: Hydraulics/Thermal Climate integration filters pass and exact-count tests remain green.
  - Failure QA: Add a test where both legacy and canonical events would fire; assert downstream receives only the authoritative sequence.
  - Commit: `refactor(climate): unify downstream invalidation path`

- [x] 10. DI and single-owner guards: Verify instance identity and absence of transient second owners - expect all lifecycle consumers share canonical ClimateState
  - References: `src/Configuration/ServiceCollectionExtensions.cs`; `tests/SnowMeltingCalculator.Tests/Configuration/DiRegistrationTests.cs`; `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectSessionTests.cs`; `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectSessionLegacyStoreGuardTests.cs`.
  - Action: Update DI registrations and add tests proving `ProjectSession`, `IProjectSession`, `IMarkDirtyService`, ClimateState, `ClimateViewModel`, `IClimateData`, `CalculationContext`, `ProjectLoadOrchestrator`, and `ResultsViewModel` observe the same canonical Climate state where applicable.
  - Acceptance: No transient or duplicate Climate owner is registered; no circular dependency is introduced; legacy adapters are forwarding/read-only.
  - Happy QA: `dotnet test tests\SnowMeltingCalculator.Tests --filter "FullyQualifiedName~DiRegistrationTests|FullyQualifiedName~ProjectSession|FullyQualifiedName~ClimateStateLegacyStoreGuard"` passes.
  - Failure QA: Reflection/DI guard fails if a second mutable Climate backing field is added to adapter services.
  - Commit: `test(di): guard climate state singleton ownership`

- [x] 11. Full affected gates: Run build and climate/lifecycle/persistence test matrix - expect green with documented existing skips only
  - References: all modified production/test files; test filters in draft evidence; `docs/architecture-migration/AGENTS.md` quality gates.
  - Action: Run targeted and broad gates:
    - `dotnet build src/SnowMeltingCalculator.csproj -c Debug`
    - `dotnet build src/SnowMeltingCalculator.csproj -c Release`
    - `dotnet test tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj -c Release --filter "FullyQualifiedName~Climate|FullyQualifiedName~ClimateToHydraulicsIntegrationTests|FullyQualifiedName~CalculationContextWriterAuthorityTests|FullyQualifiedName~DoubleCalculationPreventionTests|FullyQualifiedName~ProjectSession|FullyQualifiedName~ProjectLifecycle|FullyQualifiedName~ProjectRoundTrip|FullyQualifiedName~ResetOrchestration|FullyQualifiedName~ResultsStabilizationPhase1|FullyQualifiedName~ResultsViewModelOpenProject|FullyQualifiedName~CalculationContext|FullyQualifiedName~ThermalViewModelTests.ClimateDataChanged"`
    - full Release test suite when targeted gates pass.
  - Acceptance: Debug/Release builds pass; targeted Climate/lifecycle/persistence gates pass; full suite passes or records only pre-existing documented skips/failures with evidence.
  - Happy QA: Store raw logs/trx under `docs/architecture-migration/evidence/phase-2-climate-state/`.
  - Failure QA: Any new failure blocks dossier updates; record blocker and do not claim Phase 2 complete.
  - Commit: `test(architecture): verify phase 2 climate gates`

- [x] 12. Architecture dossier refresh: Update six maps and regenerate model/widget after code gates - expect INV-002 verified and generated artifacts reproducible
  - References: `docs/architecture-migration/maps/compile-time.md`; `di-runtime.md`; `state-ownership.md`; `reactive.md`; `persistence.md`; `user-flow.md`; `state-inventory.md`; `characterization-tests.md`; `persistence-compatibility.md`; `target-invariants.md`; `docs/architecture-migration/widget/*.mjs`; `docs/architecture-migration/TASK_CONTEXT.md`.
  - Action: Update Climate records `ST-006`, `ST-007`, `INV-002`, `CF-005`, `RE-003`, `PP-006`, `PP-013..PP-020`, compile-time/DI nodes and edges, characterization coverage, evidence links, and task context. Regenerate generated model/widget through canonical scripts only.
  - Acceptance: `node docs/architecture-migration/widget/verify-widget.mjs --suite model-v2 --schema docs/architecture-migration/maps/architecture-model.widget.schema.json --model docs/architecture-migration/maps/architecture-model.json --output docs/architecture-migration/evidence/phase-2-climate-state/model-v2-recheck.json` passes; `runtime-v2` passes; `node docs/architecture-migration/widget/generate-widget.mjs --check` passes.
  - Happy QA: Evidence receipts include SHA-256 of updated model/widget and link every changed Climate edge to tests/evidence.
  - Failure QA: If generated check fails, fix source maps/model inputs; do not hand-edit generated HTML/JSON to make checks pass.
  - Commit: `docs(architecture): record phase 2 climate ownership`

## Final verification wave

- [x] F1. Plan compliance audit: Compare implementation against this plan and allow-list - expect every todo satisfied and no scope creep
  - References: `.omo/plans/phase-2-climate-state.md`; `docs/architecture-migration/evidence/phase-2-climate-state/`; git status receipts.
  - Acceptance: Independent reviewer confirms all implementation tasks have evidence, all Must-NOT-Have constraints hold, only allow-listed paths changed, generated artifacts were produced by scripts, and no manual-only verification is used.
  - QA: Run `git status --porcelain=v1 -z`, compare with baseline protected set, inspect evidence index, and record `f1-plan-compliance.md`.

- [x] F2. Code quality and single-owner audit: Verify ClimateState ownership invariant in source - expect no writable bypasses
  - References: modified source and tests; `INV-002`; guard tests.
  - Acceptance: No production path writes canonical Climate outside `ProjectSession.ClimateState`; adapters/projections are read-only or forwarding-only; no broad `CalculationContext`/Results/other-slice redesign occurred.
  - QA: Run guard tests, affected builds, and source inspection; record `f2-code-quality-architecture.md`.

- [x] F3. Real lifecycle QA: Exercise user flows through tests/automation - expect values, dirty state, downstream invalidation, save/reload, and exports correct
  - References: Climate UI/viewmodel tests, Results load/save/export tests, Hydraulics/Thermal integration tests, `.smc` fixtures.
  - Acceptance: City selection, individual edit, reset, load, second load, save/reload, PDF/Markdown projection, and downstream recalculation behave per characterized target counts.
  - QA: Run targeted matrix and full Release suite; record raw logs/trx and `f3-real-lifecycle-qa.md`.

- [x] F4. Architecture dossier fidelity: Verify six maps/model/widget/TASK_CONTEXT match code and evidence - expect reproducible generated artifacts
  - References: all architecture maps, model/widget scripts, generated outputs, `TASK_CONTEXT.md`.
  - Acceptance: Model-v2 and runtime-v2 verification pass; `generate-widget.mjs --check` passes; changed Climate records cite current evidence; historical audit inputs are not used as current metrics.
  - QA: Record verifier JSON outputs, hashes, and `f4-scope-fidelity-dirty-worktree.md`.

## Commit strategy

Use atomic commits after each green wave. Suggested sequence:

1. `test(architecture): capture phase 2 climate baseline`
2. `test(climate): characterize writable climate ownership surface`
3. `test(climate): lock logical action multiplicity`
4. `feat(project): add canonical climate state owner`
5. `refactor(climate): make climate data a projection`
6. `refactor(climate): make climate view model an adapter`
7. `refactor(project): route climate restore through session state`
8. `refactor(results): snapshot climate from session state`
9. `refactor(climate): unify downstream invalidation path`
10. `test(di): guard climate state singleton ownership`
11. `test(architecture): verify phase 2 climate gates`
12. `docs(architecture): record phase 2 climate ownership`

Do not commit, stage, restore, clean, or revert unrelated existing dirty paths. If protected dirty paths change unexpectedly, stop and record the blocker.

## Success criteria

- `ProjectSession.ClimateState` or equivalent ProjectSession-owned object is the only writable canonical owner of project Climate values.
- `ClimateViewModel` is a WPF adapter with UI-only local state and no direct dirty/downstream ownership.
- `IClimateData`/`ClimateData` is read-only/forwarding/immutable projection, not a second owner.
- `CalculationContext` remains a narrow downstream compatibility projection/invalidation seam.
- `ProjectLoadOrchestrator`, reset paths, save/reload/export, Thermal, Hydraulics, and Results projections use the canonical state or documented projection boundaries.
- `.smc` Climate semantic contract v1.0/v1.1 is preserved; no schema/version change.
- Logical action boundaries, origins, dirty transitions, projection updates, downstream invalidation, no-op behavior, repeated load/reset, partial failure, and subscription hygiene are tested with agent-executable commands.
- Six architecture maps, target invariants, generated model/widget, evidence receipts, and `TASK_CONTEXT.md` reflect the completed Phase 2 state.
- Final verification wave F1-F4 approves before any completion claim.
