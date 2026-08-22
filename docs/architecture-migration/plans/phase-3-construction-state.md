# phase-3-construction-state - Work Plan

## TL;DR

Phase 3 переносит только project Construction inputs в `ProjectSession.ConstructionState`. После фазы этот state slice является единственным writable canonical owner для ordered layers above/below pipe, groundwater level и `HasLoads`; `ConstructionViewModel` остаётся WPF adapter, `Construction`/`IConstructionData` и `CalculationContext` становятся read/projection compatibility surfaces, а save/restore/reset проходят через state slice.

Это characterization-first migration. До ownership edits worker измеряет current multiplicity, writer surface, reset/load/template/import/editor semantics и subscription behavior. Каждый implementation task заканчивается компилируемым green boundary. Выполнение этого плана требует отдельного owner approval и затем отдельного `/architecture-start phase-3-construction-state`; запись/review плана не разрешает production edits.

## Scope

### In scope

- Current repository only: `D:\IA\ace v.2` at execution-time verified HEAD.
- Construction project-state ownership vertical slice:
  - `src/Services/Project/ProjectSession.cs`, `IProjectSession.cs` and new Construction state contract/implementation;
  - `src/Models/Construction/Construction.cs`, `Layer.cs` and `IConstructionData` only as required to remove writable ownership bypasses;
  - `src/ViewModels/Construction/ConstructionViewModel.cs` as adapter;
  - Construction seam in `ProjectLoadOrchestrator`, `ResultsViewModel`, `CalculationContext` and DI;
  - existing Construction service/repositories only where needed to convert template/file/project DTOs into canonical mutation inputs without redesigning them;
  - characterization, ownership, lifecycle, persistence, DI and downstream tests;
  - six maps, shared architecture model, generated widget, evidence and `TASK_CONTEXT.md` after green code gates.
- Preserve supported `.smc` v1.0/v1.1 semantics, including historical above-pipe ordering conversion, current below-pipe ordering, material-name resolution/fallback and current partial restore behavior.

### Out of scope / Must-NOT-Have

- No ThermalState, HydraulicsState or Results ownership migration.
- No formula, validation-range, groundwater threshold, UI/XAML/design, package/version, installer/publish or `.smc` schema/version change.
- No general redesign of `CalculationContext`, `ProjectLoadOrchestrator`, `ConstructionService`, Construction repositories, material/template editors or catalog persistence.
- No new `ConstructionService.CalculateR2` contract and no repository replacement.
- No Undo/Redo implementation, history stack, persisted command history or UI commands; only explicit mutation/origin/completion boundaries needed by a future recorder.
- No transactional project restore redesign. Preserve characterized partial-failure behavior unless evidence proves the current contract cannot be retained; then stop for owner decision.
- No `NotImplementedException`, placeholder/stub production paths, `[Ignore]`, Skip, weakened/deleted tests, guessed test counts, speculative compatibility shims or optional implementation tasks.
- No stage/commit/push/restore/reset/clean of unrelated dirty paths. Never hand-edit generated widget output or fabricate metrics from `D:\IA\ace`.

## Decision ledger and exact target contract

### DEC-C01 - Canonical values and projections

Canonical project Construction state consists of `GroundwaterLevel`, `HasLoads`, ordered `LayersAbovePipe` and ordered `LayersBelowPipe`. Every canonical layer snapshot contains `Id`, `MaterialId`, `MaterialName`, `Thickness`, `CalculatedLambda`, `IsLambdaOverridden`, `Position` and `Order`. `R1Total`, `R2Total`, `LambdaE`, total thicknesses and validation are derived projections, not independently writable state. Catalog lists, templates, selected layer/template, preview collections, validation display, loading flags, `HasUnsavedChanges` display and file-dialog paths remain adapter/catalog UI state.

### DEC-C02 - Identity and structural equality

- `Layer.Id` is the stable identity for an existing layer across edit/reorder/snapshot operations. Add creates one new non-empty `Guid`; restore preserves a persisted ID when available and otherwise creates one once at the DTO-to-state boundary. Phase 3 does not add a new `.smc` field solely for layer IDs.
- Material resolution uses `Material.Id` inside live/catalog/template operations. Existing project restore compatibility continues to resolve persisted `MaterialName` as currently characterized, including fallback behavior; it must not silently reinterpret the `.smc` wire contract.
- Collection order is semantic. Above pipe: index/`Order` 0 is surface and increases toward pipe. Below pipe: index/`Order` 0 is nearest pipe and increases toward ground. After every structural mutation, `Order` equals collection index and `Position` matches the collection.
- Snapshot equality is explicit structural equality: scalars plus sequence equality of both layer collections in order, and field-by-field equality of every layer snapshot. It must not use default equality of `IReadOnlyList<T>` or mutable `Layer` reference equality. No-op detection uses this structural comparer.

### DEC-C03 - Mutation/origin/result/completion contract

The implementation may choose equivalent names, but production code and tests must provide this semantic shape before adapters are rewired:

```csharp
public enum ConstructionMutationOrigin
{
    User,
    Template,
    FileLoad,
    ProjectLoad,
    Reset,
    Restore,
    SystemApply,
    Initialization
}

public enum ConstructionMutationStatus
{
    Changed,
    NoChange,
    Rejected,
    Cancelled
}

public sealed record ConstructionLayerSnapshot(
    Guid Id,
    int MaterialId,
    string MaterialName,
    double Thickness,
    double CalculatedLambda,
    bool IsLambdaOverridden,
    LayerPosition Position,
    int Order);

public sealed record ConstructionStateSnapshot(
    double GroundwaterLevel,
    bool HasLoads,
    IReadOnlyList<ConstructionLayerSnapshot> LayersAbovePipe,
    IReadOnlyList<ConstructionLayerSnapshot> LayersBelowPipe);

public sealed record ConstructionMutationResult(
    ConstructionMutationStatus Status,
    ConstructionMutationOrigin Origin,
    ConstructionStateSnapshot Before,
    ConstructionStateSnapshot After,
    string? ErrorCode);

public interface IProjectSessionConstructionState
{
    ConstructionStateSnapshot Snapshot { get; }
    event EventHandler<ConstructionStateChangedEventArgs> Changed;

    ConstructionMutationResult Apply(ConstructionMutation mutation, ConstructionMutationOrigin origin);
    ConstructionMutationResult ApplySnapshot(ConstructionStateSnapshot snapshot, ConstructionMutationOrigin origin);
    ConstructionMutationResult ResetToDefaults(ConstructionDefaults defaults, ConstructionMutationOrigin origin);
}
```

`ConstructionMutation` is a closed, exhaustive command family or equivalent explicit methods for scalar edit, add, remove, edit/rebind and reorder layer. It is not a bag of nullable fields. `ConstructionDefaults` is produced by the existing default-construction policy/material catalog boundary; the canonical state does not access dialogs or repositories.

Required semantics:

1. Validate/normalize the complete candidate snapshot before replacing canonical state. A rejected mutation leaves `Snapshot` unchanged, emits no `Changed`, does not dirty, and does not update `CalculationContext`.
2. A structurally identical candidate returns `NoChange`, emits no `Changed`, does not dirty, and produces no downstream update.
3. A changed mutation atomically replaces the canonical snapshot and emits exactly one `Changed` completion containing origin, before/after and changed status. Internal layer/property/collection notifications are adapter details and cannot create extra logical completions.
4. `User` and a successfully applied `Template` are user-visible mutations and mark the project dirty exactly once per logical action. `FileLoad` is user initiated but preserves the current standalone-construction command semantics captured in Task 3; do not guess whether it changes project dirty state before characterization.
5. `ProjectLoad`, `Restore`, `Initialization` and lifecycle `Reset` are non-user origins and never create user dirty/history semantics. A user Reset command is represented as `User`, while `ProjectLoadOrchestrator.ResetModules()` uses `Reset`; they are not conflated.
6. `Cancelled` is returned only when an application boundary cancels before canonical apply, such as declining missing-material import. No canonical mutation/completion/dirty/context update occurs. Catalog imports/editor saves are external side effects and are not rolled back by state apply.
7. Exceptions from catalog/repository/dialog/application preparation occur before canonical apply whenever current behavior permits. If preparation fails, canonical state is unchanged. Existing partial project-load behavior outside this slice remains characterized rather than made transactional.
8. One successful logical completion drives at most one `Construction` read projection update and one `CalculationContext.UpdateConstruction` publication when valid. Invalid target behavior must be characterized first and then preserved without making validation output a second owner.

### DEC-C04 - Template, import and editor boundary

- Template preview/selection is UI-only and never mutates canonical state.
- Applying a resolvable template prepares a full candidate snapshot and commits it once with `Template`; its many layer changes produce one completion/dirty transition.
- Missing-material prompt `No`, missing snapshot, import exception or template construction exception leaves canonical state unchanged. Successful material import refreshes catalogs, re-resolves the template, then performs one canonical apply. The global material import remains an external catalog side effect even if later state apply fails; evidence must state this rather than claim rollback.
- Material/template editor cancellation (`false` or `null`) and catalog refresh semantics are locked by characterization. Refresh/rebind must preserve layer IDs, order, thickness and lambda override semantics and must not create a logical project mutation unless project layer material data structurally changes.
- Project material/template imports remain orchestration prerequisites in `ProjectLoadOrchestrator`/Construction application boundary; they do not become responsibilities of `ProjectSessionConstructionState`.

### DEC-C05 - Reset, load and persistence boundary

- Lifecycle reset preserves the currently characterized default layer recipe, preserves groundwater where current behavior does, resets `HasLoads`, and is non-user. Initialization and explicit UI reset are tested separately because current `Initialize()` invokes reset while `ResetToDefault` delegates to the same method.
- Project restore converts `ConstructionProjectData` plus the project version and material catalog into one normalized snapshot, including v1.0 above-order conversion and current lambda override behavior, then applies once with `ProjectLoad`/`Restore`.
- Save reads only `ProjectSession.ConstructionState.Snapshot` (or a pure DTO mapper over it), never a mutable ViewModel cache. The same persisted fields/version behavior and semantic round-trip remain intact.
- Standalone construction JSON load/save remains supported through DTO/model mappers. It must not reintroduce a writable `Construction` owner; exact dirty/success-message semantics are preserved from characterization.

## Execution and recovery discipline

- One sequential implementation lane. Read-only investigation and independent verification may run in parallel; no two workers edit `ProjectSession`, Construction state, ViewModel, load/reset, Results or DI concurrently.
- Before every task, compare binary-safe current status against Task 1 baseline. The task write-set is its explicit allow-list plus its new evidence directory only. A pre-existing dirty allow-listed file requires a byte/hash receipt of the user delta before editing and a post-task proof that unrelated hunks remain.
- Every task starts from the previous green boundary, runs its targeted gate and records a receipt. If it fails, edit only that task's allow-list or stop. Never use Git rollback on the shared dirty tree; recover by applying a minimal inverse patch to task-owned hunks or by restoring a task-created file from its recorded preimage.
- If live characterization contradicts DEC-C01..C05 in a way that changes observable semantics, identity, persistence or scope, stop with `Stage = blocked`, record evidence/options in `TASK_CONTEXT.md`, and obtain owner decision. Do not improvise.

## Todos

- [x] 1. Protected baseline: Capture execution-time repository, tools and dirty boundary - expect NUL-safe recovery evidence before edits
  - Depends on: owner plan approval and separate `/architecture-start phase-3-construction-state`.
  - Allow-list: new files only under `docs/architecture-migration/evidence/phase-3-construction-state/`; factual workflow rows in `TASK_CONTEXT.md`.
  - Action: Record Git root/HEAD/branch/upstream, `dotnet --info`, binary `git status --porcelain=v1 -z --branch`, binary unstaged/staged name sets and hashes. Derive Construction-relevant dirty paths and protected set without decoding NUL streams through PowerShell text pipelines.
  - Acceptance/QA: baseline and immediate post-capture sets are symmetric after excluding only Task 1 evidence; staged set remains unchanged; raw receipts and parser commands are reproducible.
  - Failure/recovery: HEAD move, staged drift or protected-path drift blocks Task 2. Delete/recreate only Task 1-created evidence via patch; never reset/clean/checkout.
  - Commit guidance: `test(architecture): capture phase 3 construction baseline`.

- [x] 2. Writer and subscriber inventory: Guard every Construction owner/bypass path - expect exact current and target ownership surfaces
  - Depends on: Task 1.
  - Allow-list: new/updated Construction ownership guard tests; Phase 3 evidence.
  - References: `ConstructionViewModel`, mutable `Construction`/`Layer`, `ProjectLoadOrchestrator`, `ResultsViewModel`, `CalculationContext`, DI and Construction editor/import/service/repository call sites.
  - Action: Add deterministic guard tests that distinguish writers, projections, catalog writers and readers. Inventory direct scalar setters, mutable collection operations, layer property setters, `SyncToModel`/`SyncFromModel`/`CopyConstructionData`, model methods, restore/reset, template/file load, save and context publication. Inventory collection/item/model subscriptions and their detach paths.
  - Acceptance/QA: guard captures current bypass list and includes negative fixtures proving a new direct ViewModel/model write and missing unsubscribe are detected.
  - Failure/recovery: incomplete inventory blocks characterization; revert only task-owned test/evidence hunks by inverse patch.
  - Commit guidance: `test(construction): characterize writable ownership surface`.

- [x] 3. Behavioral characterization: Lock logical actions, failure/no-op and subscription multiplicity - expect measured current contracts, never guessed counts
  - Depends on: Task 2.
  - Allow-list: Construction tests, project lifecycle/reset/round-trip tests only where directly affected; Phase 3 evidence.
  - Action: Counter-based tests cover scalar groundwater/loads, add above/below, remove, layer thickness/material/manual-lambda edit, reorder if reachable, same-value/no-op, template preview/apply, missing-material yes/no/no-snapshot/import failure, standalone load/save null/failure/success, editor true/false/null refresh, initialization, UI reset, lifecycle reset, project load, second load and repeated reset/load.
  - Acceptance/QA: each case records final ordered values/IDs, `Order`, dirty calls/transitions, VM/model/completion candidates, validation, context updates, downstream Thermal/Results effects and collection/item subscription counts. Tests explicitly establish current `FileLoad`, reset groundwater, lambda override, catalog refresh/rebind and partial-failure semantics.
  - Failure/recovery: uncertain or contradictory semantics become a blocking owner decision before Task 4; no expected count is inferred from source alone.
  - Commit guidance: `test(construction): lock logical action multiplicity`.

- [x] 4. ConstructionState foundation: Implement structural snapshots and canonical mutation contract - expect one ProjectSession-owned writable state
  - Depends on: Tasks 1-3 green and no unresolved owner decision.
  - Allow-list: `src/Services/Project/IProjectSession.cs`, `ProjectSession.cs`, new Construction state files in the same project service area, direct state unit tests; Phase 3 evidence.
  - Action: Implement DEC-C01..C03, structural comparer, candidate validation/normalization, closed mutation API, origins/status/result/event and ProjectSession exposure. Do not wire ViewModel yet.
  - Acceptance/QA: unit tests prove field-complete round-trip, ordered structural equality, same sequence no-op, reordered sequence change, stable layer IDs, add/remove/edit/reorder, rejected atomicity, exactly one event for changed mutation and zero for no-op/rejected.
  - Failure/recovery: no stubs or dual-owner completion claim. If contract cannot represent characterized behavior, stop; inverse-patch only Task 4 hunks.
  - Commit guidance: `feat(project): add canonical construction state owner`.

- [x] 5. Construction projection boundary: Make mutable Construction/Layer a non-owning compatibility projection - expect no externally writable second canonical store
  - Depends on: Task 4.
  - Allow-list: Construction/Layer/IConstructionData model files only as necessary, state projection adapter, direct model/state/thermal compatibility tests; Phase 3 evidence.
  - Action: Build fresh read projection objects or an internally updated forwarding projection from each successful completion. Restrict production mutation entrypoints so external consumers cannot treat `Construction`, its collections or `Layer` objects as canonical writable state. Preserve formulas and `IConstructionData` read behavior.
  - Acceptance/QA: `R1Total`, `R2Total`, `LambdaE`, material-around-pipe and ordering match snapshots; guard fails on a new production bypass; no formulas change.
  - Failure/recovery: if a required consumer cannot use a read projection without broad Thermal redesign, stop and record the exact caller; do not expand Phase 3.
  - Commit guidance: `refactor(construction): make construction model a projection`.

- [x] 6. ConstructionViewModel adapter: Route all project mutations through ConstructionState - expect stable bindings without canonical backing collections
  - Depends on: Task 5.
  - Allow-list: `ConstructionViewModel.cs`, direct Construction ViewModel tests; minimal constructor test helpers; Phase 3 evidence.
  - Action: Preserve public WPF binding/command shape while exposing adapter collections/properties from snapshots and routing scalar/layer mutations to the canonical API. Move logical orchestration, normalization and completion out of VM; retain dialogs, selection, previews, validation text, loading and catalog UI locally. Subscribe once to state completion and detach/replace collection/item subscriptions deterministically.
  - Acceptance/QA: VM no longer calls `IMarkDirtyService.MarkDirty`, mutates canonical `Construction`/`Layer` or invokes `CalculationContext.UpdateConstruction` directly. One UI action yields one canonical completion; adapter refresh causes no recursion/completion; no-op none.
  - Failure/recovery: XAML/API compatibility failure is fixed inside allow-list without UI redesign. Restore only task-owned hunks by inverse patch.
  - Commit guidance: `refactor(construction): make view model a state adapter`.

- [x] 7. Templates, catalogs and editors: Prepare external effects then apply one state mutation - expect exact cancellation/failure semantics and preserved identities
  - Depends on: Task 6.
  - Allow-list: Construction VM application-boundary methods, minimal Construction service mapper code if proven necessary, Construction template/material/editor integration tests; no repository redesign; Phase 3 evidence.
  - Action: Implement DEC-C04. Template conversion and material resolution produce a complete candidate before state apply. Refresh/rebind preserves layer IDs/order and uses explicit state mutation only if structural project data changes.
  - Acceptance/QA: successful template apply is one `Template` completion/dirty action; preview and cancelled/failed apply produce zero; successful import then apply produces one state completion while catalog side effect is separately asserted; editor true/false/null and missing-material paths match Task 3.
  - Failure/recovery: external catalog writes are never claimed rolled back. If current editor result semantics conflict, preserve characterized behavior and update adapter tests, not state ownership.
  - Commit guidance: `refactor(construction): route templates through state boundary`.

- [x] 8. Reset and project restore: Apply one normalized Construction snapshot through lifecycle origins - expect no direct VM writes or stale project A state
  - Depends on: Task 7.
  - Allow-list: Construction seam in `ProjectLoadOrchestrator.cs`, reset orchestration caller only if required, project lifecycle/reset tests; Phase 3 evidence.
  - Action: Move default recipe preparation and project DTO/version/material mapping to an application mapper/coordinator, then call state once. Preserve v1.0/v1.1 above ordering, below ordering, fallback resolution, lambda override and partial failure contracts measured in Task 3.
  - Acceptance/QA: project load/reset are non-user, do not dirty, second load replaces all Construction values, repeated cycles do not multiply subscriptions/completions/context updates, restore guard clears on failure and current partial-state contract remains.
  - Failure/recovery: any required `.smc` semantic or transaction change blocks Task 9 and returns to owner.
  - Commit guidance: `refactor(project): restore construction through session state`.

- [x] 9. Persistence and standalone files: Read/write canonical snapshots without wire changes - expect semantic round-trip compatibility
  - Depends on: Task 8.
  - Allow-list: Construction save projection in `ResultsViewModel.cs`, pure DTO mapper files, standalone Construction command mapper if needed, project/Construction repository and round-trip tests only when contract requires; no repository redesign; Phase 3 evidence.
  - Action: Save `.smc` Construction data/custom material/template references from state snapshot plus existing catalogs. Route standalone JSON load through state; route save from snapshot projection. Preserve existing DTO fields, versions, messages and failure behavior.
  - Acceptance/QA: v1.0/v1.1 load, save/reload semantic round-trip, order, lambda override, material fallback/custom imports, missing/corrupt files and second-load replacement pass. `ResultsViewModel` no longer reads writable VM state for Construction.
  - Failure/recovery: schema/version or broad Results redesign is forbidden and blocks continuation.
  - Commit guidance: `refactor(results): snapshot construction from session state`.

- [x] 10. Downstream and dirty completion: Publish one authoritative projection sequence - expect one invalidation per changed logical action
  - Depends on: Task 9.
  - Allow-list: Construction-only seam in `CalculationContext.cs`, projection coordinator/state files, directly affected Thermal/Results/Construction integration tests; Phase 3 evidence.
  - Action: On one changed completion, apply origin-aware dirty semantics, refresh one read projection and publish through existing context seam at most once when valid. Remove/suppress legacy VM/model event publications. Do not redesign Thermal/Results.
  - Acceptance/QA: measured action matrix meets target multiplicity; no-op/rejected/cancelled publish none; lifecycle origins do not dirty; invalid-state behavior matches characterization; duplicate legacy+canonical event test proves one authoritative path.
  - Failure/recovery: if downstream requires multi-slice redesign, stop and retain narrow compatibility adapter.
  - Commit guidance: `refactor(construction): unify completion and invalidation`.

- [x] 11. DI and ownership guards: Bind every lifecycle consumer to one ConstructionState - expect no duplicate owner or circular lifetime
  - Depends on: Task 10.
  - Allow-list: `ServiceCollectionExtensions.cs`, DI/ProjectSession/Construction ownership tests; Phase 3 evidence.
  - Action: Register/expose state through singleton `ProjectSession`; update constructors/test helpers. Guard against mutable Construction backing fields/direct writers in adapters and services.
  - Acceptance/QA: ProjectSession, interface, VM, load orchestrator, Results and context projection observe the same state; catalog services remain independent; no transient second state/cycle.
  - Failure/recovery: do not add service locator or duplicate fallback owner to break a cycle; stop and document graph if narrow constructor change cannot solve it.
  - Commit guidance: `test(di): guard construction state ownership`.

- [x] 12. Affected executable gates: Build and run Construction/lifecycle/persistence matrix - expect green before dossier edits
  - Depends on: Tasks 1-11.
  - Allow-list: new raw logs/TRX/receipts under Phase 3 evidence; test-only correction files only for failures caused by Phase 3 and after recording root cause.
  - Action: Run Debug and Release builds; targeted Construction, ProjectSession, lifecycle/reset, Results open/save, round-trip, CalculationContext and directly affected Thermal tests; then full Release suite. Discover filters from live tests rather than copying guessed totals.
  - Acceptance/QA: exit 0, zero new warnings/errors, targeted/full suites have no new failures; raw commands, SDK, counters/skips and hashes recorded. Known pre-existing failures/skips require baseline evidence and cannot be waived silently.
  - Failure/recovery: dossier remains unchanged except blocker context/evidence until gates pass.
  - Commit guidance: `test(architecture): verify phase 3 construction gates`.

- [x] 13. Architecture dossier refresh: Update six views over one shared model and regenerate widget - expect current code/evidence fidelity
  - Depends on: Task 12 green.
  - Allow-list: six map files, state inventory, characterization/persistence/invariant maps, `architecture-model.json` and schema inputs only as canonical workflow requires, widget generated artifact via script, Phase 3 evidence and `TASK_CONTEXT.md`.
  - Action: Update compile-time, DI/runtime, state ownership, reactive, persistence and user-flow views with typed evidence edges; mark Construction single owner, adapters, origins/completion, writer removal, persistence compatibility and tests. Update one shared architecture model and regenerate/check widget through canonical scripts only.
  - Acceptance/QA: model-v2, runtime-v2 and `generate-widget.mjs --check` pass; two generation passes are byte-identical; hashes/evidence links are recorded; historical audit metrics are not reused.
  - Failure/recovery: fix source model/map input, never generated HTML by hand. After technical F1-F4, set workflow only to `awaiting-owner-acceptance`.
  - Commit guidance: `docs(architecture): record phase 3 construction ownership`.

## Final verification wave

- [ ] F1. Plan compliance and protected-scope audit: Compare exact implementation to tasks, decisions and baseline - expect no scope creep or lost user deltas
  - References: this exact SHA-bound plan, Task 1 binaries, all Phase 3 receipts and final NUL-safe Git status.
  - QA/acceptance: independent reviewer maps every changed path to one allow-list/task, checks Must-NOT-Have rules, compares protected status symmetrically, confirms every task/evidence exists and records `f1-plan-compliance.md`. Any unexplained path or user-hunk drift is `REJECT`.

- [ ] F2. Architecture/code-quality audit: Prove single owner, structural equality and one completion - expect no mutable bypasses
  - Executable QA:
    1. Run `dotnet build src\SnowMeltingCalculator.csproj -c Release`; expect exit `0` and no new warning/error relative to Task 12 receipt.
    2. Run `dotnet test tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~ConstructionStateTests|FullyQualifiedName~ConstructionStateLegacyStoreGuardTests|FullyQualifiedName~ConstructionMultiplicityCharacterizationTests|FullyQualifiedName~DiRegistrationTests|FullyQualifiedName~ProjectSessionTests" --logger "trx;LogFileName=phase-3-f2.trx"`; these exact test-class names must be created/updated by Tasks 2-4/11. Expect exit `0`, zero failed tests, and only baseline-documented skips.
    3. Inspect the guard's emitted writer/subscriber inventory and final production diff. Assert zero direct canonical writes outside `IProjectSessionConstructionState` implementation/application mapper, zero `IMarkDirtyService`/`CalculationContext.UpdateConstruction` calls in `ConstructionViewModel`, zero externally mutable projection backing stores, and no changed ThermalState/HydraulicsState ownership files. Record each assertion and file reference in `f2-code-quality-architecture.md`.
  - Acceptance: ordered structural comparer tests include equal independent lists, changed field and reordered list; completion tests assert one event for changed and zero for no-op/rejected/cancelled; catalog side effects remain separate. Any failed assertion is `REJECT`.

- [ ] F3. Lifecycle and persistence QA: Exercise real logical actions and failure matrix - expect correct values, order, identity, dirty, invalidation and round-trip
  - Executable QA:
    1. Run `dotnet test tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~ConstructionMultiplicityCharacterizationTests|FullyQualifiedName~ConstructionViewModelTests|FullyQualifiedName~ConstructionViewModelEditorIntegrationTests|FullyQualifiedName~ConstructionServiceTemplateImportTests|FullyQualifiedName~MaterialImportTests|FullyQualifiedName~ProjectLifecycleFlowCharacterizationTests|FullyQualifiedName~ResetOrchestrationTests|FullyQualifiedName~ProjectRoundTripTests|FullyQualifiedName~ResultsViewModelOpenProjectTests" --logger "trx;LogFileName=phase-3-f3-targeted.trx"`; expect exit `0`, zero failed tests and only baseline-documented skips.
    2. The targeted tests must execute and assert the Task 3 matrix: scalar/layer add-remove-edit-reorder/no-op; template preview/success/missing-material yes/no/no-snapshot/import failure; editor true/false/null; standalone load/save success/null/corrupt/failure; initialization; user reset; lifecycle reset; project load; second load; repeated load/reset. For each, assert ordered snapshot/IDs/`Order`, mutation status/origin/completion count, dirty count, context publication count and subscription count against the Task 3 measured target receipt.
    3. Run `dotnet test tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj -c Release --no-build --logger "trx;LogFileName=phase-3-f3-full.trx"`; expect exit `0`, zero new failures and only baseline-documented skips. Parse both TRX files and record total/executed/passed/failed/skipped counters without hard-coding them in advance.
    4. Re-open the saved fixture through the automated round-trip test and assert semantic equality of groundwater, loads, both ordered layer sequences, material names/IDs where represented, thickness, lambda and override flags; assert project A values are absent after second load.
  - Acceptance: all scenarios pass, changed logical actions have exactly one completion, no-op/rejected/cancelled have zero, lifecycle origins do not dirty, and repeated cycles do not increase handler/invalidation counts. Store raw logs/TRX and `f3-real-lifecycle-qa.md`; any mismatch is `REJECT`.

- [ ] F4. Dossier fidelity and workflow gate: Verify six views/shared model/widget/context - expect reproducible artifacts and owner checkpoint only
  - Executable QA:
    1. Run `node docs/architecture-migration/widget/verify-widget.mjs --suite model-v2 --schema docs/architecture-migration/maps/architecture-model.widget.schema.json --model docs/architecture-migration/maps/architecture-model.json --output docs/architecture-migration/evidence/phase-3-construction-state/f4-model-v2.json`; expect exit `0` and a passing receipt.
    2. Run `node docs/architecture-migration/widget/verify-widget.mjs --suite runtime-v2 --schema docs/architecture-migration/maps/architecture-model.widget.schema.json --model docs/architecture-migration/maps/architecture-model.json --output docs/architecture-migration/evidence/phase-3-construction-state/f4-runtime-v2.json`; expect exit `0` and a passing receipt.
    3. Run `node docs/architecture-migration/widget/generate-widget.mjs`, hash `docs/architecture-migration/architecture-widget.html`, run the same generation command again, and assert byte-identical SHA-256. Then run `node docs/architecture-migration/widget/generate-widget.mjs --check`; expect exit `0`.
    4. Inspect all six map filters and the shared model: every changed Construction node/edge/invariant must cite current source plus Phase 3 test/evidence; no stale `D:\IA\ace` metric may appear. Run final NUL-safe Git status comparison against Task 1 and assert zero protected removed/added/status-changed paths.
    5. Verify `TASK_CONTEXT.md` says `Stage = awaiting-owner-acceptance`, `Phase result acceptance = pending`, and does not say Phase 3 `completed` or authorize Phase 4.
  - Acceptance: all commands exit `0`, generated hashes match, six-view/shared-model and protected-worktree assertions pass. Record commands, exits, hashes and assertions in `f4-scope-fidelity-dirty-worktree.md`. Do not mark `completed` without later explicit owner acceptance.

## Atomic commit guidance only

If and only if execution is separately authorized, use small green commits corresponding to Tasks 1-13, pairing each implementation with its direct tests. Before every commit inspect staged diff and stage only the task allow-list. Do not commit during planning/review, do not amend or push without explicit owner request, and never include unrelated dirty paths.

## Completion and owner gates

Technical Phase 3 success requires all Tasks 1-13 and F1-F4 green, `ProjectSession.ConstructionState` as sole writable owner, preserved Construction formulas/UI/`.smc` behavior, clean protected-boundary comparison and reproducible dossier/widget. Technical success transitions only to `awaiting-owner-acceptance`. A separate explicit owner statement accepting the Phase 3 result is required for `completed`.

Before execution there are two distinct earlier gates: (1) owner approves the exact reviewed plan SHA, moving to `approved`; (2) owner separately invokes `/architecture-start phase-3-construction-state`, moving to `executing`. Neither this planning artifact, Sisyphus review nor Momus review crosses either gate.
