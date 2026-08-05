# phase-1-project-session-shell - Work Plan

## TL;DR (For humans)

- **What you'll get:** a minimal `ProjectSession` lifecycle aggregate that is the only writable owner of project identity, current file path, dirty state, and restore-in-progress state. Existing consumers continue through forwarding-only compatibility contracts while `CalculationContext` and all four module ownership slices remain untouched.
- **Why this approach:** it removes the split lifecycle ownership currently spanning `ProjectStateService` and `CalculationStateService` without turning `ProjectSession` into a command coordinator or migrating Climate, Construction, Thermal, or Hydraulics.
- **What it will NOT do:** no transactional restore, rollback, `.smc` schema/version-policy change, `.bak` recovery, formula/UI/package/SDK/installer/release change, module-state migration, `CalculationContext` facade/replacement, or unrelated cleanup.
- **Effort / risk:** architecture-sized, one sequential production lane. Highest risks are event-count drift, duplicate writable state during transition, partial-restore behavior drift, and accidental inclusion of protected dirty files.
- **Decisions:** characterization-first TDD; `ProjectSession` is sole lifecycle owner; existing interfaces are forwarding-only compatibility surfaces; current partial restore-failure semantics and currently accepted `.smc` corpus are preserved.

## Scope

### In scope

- Add a lifecycle-only `ProjectSession` contract and singleton implementation under `src/Services/Project/` (final names: `IProjectSession.cs` and `ProjectSession.cs`).
- Canonical lifecycle values: `ProjectNumber`, `ProjectObject`, `CurrentFilePath`, `IsDirty`, and `IsLoadProjectInProgress`.
- Preserve `IProjectInfoService`, `IProjectStateService`, and `IMarkDirtyService` as forwarding-only compatibility views over the same `ProjectSession` instance; remove mutable storage from `ProjectStateService` and retire the concrete service if no consumer still requires it.
- Make `CalculationStateService.IsLoadProjectInProgress` a forwarding compatibility view over `IProjectSession`, with no local backing field and no independent writer authority; retain calculation-state responsibilities and `SetPipeSpacing` source guard.
- Narrow rewiring of `MainWindow`, `MainViewModel`, `ResultsViewModel`, `ProjectLoadOrchestrator`, and DI while preserving current command ownership and call ordering.
- Characterize exact lifecycle, failure, event/subscription, refresh/recalculation, second-load, repeated-cycle, and persistence behavior before structural changes.
- Update the shared architecture model, six filtered views, widget, evidence receipts, and `TASK_CONTEXT.md` after code/test verification.

### Decision-complete `IProjectSession` contract

Create `src/Services/Project/IProjectSession.cs` with this exact public lifecycle-only shape (XML documentation may be added, but no additional property, command, module slice, file operation, dialog dependency, or orchestration method may be added in Phase 1):

```csharp
public interface IProjectSession : INotifyPropertyChanged
{
    string ProjectNumber { get; set; }
    string ProjectObject { get; set; }
    string? CurrentFilePath { get; set; }
    bool IsDirty { get; }
    bool IsLoadProjectInProgress { get; }

    void MarkDirty();
    void MarkClean();
    IDisposable BeginProjectRestore();
}
```

- **Initial state:** `ProjectNumber == string.Empty`, `ProjectObject == string.Empty`, `CurrentFilePath == null`, `IsDirty == false`, and `IsLoadProjectInProgress == false`.
- **Null policy:** `ProjectNumber` and `ProjectObject` are non-null. Assigning `null` (including via null-forgiving/reflection/legacy runtime code) throws `ArgumentNullException`, does not mutate state, and raises no event. Empty and whitespace strings remain accepted to preserve current caller behavior; there is no trimming or normalization. `CurrentFilePath == null` means an unsaved/new project; non-null values, including empty/whitespace strings, are stored verbatim in Phase 1 because path validation remains with existing callers.
- **Equality/idempotency:** string setters compare with `StringComparison.Ordinal`. Assigning an equal value is a no-op. `MarkDirty()` changes only `false → true`; `MarkClean()` changes only `true → false`; repeated calls are no-ops. No lifecycle method implicitly changes identity, path, or another lifecycle value.
- **`PropertyChanged` semantics:** one synchronous event is raised after each real mutation, with exactly `nameof(ProjectNumber)`, `nameof(ProjectObject)`, `nameof(CurrentFilePath)`, `nameof(IsDirty)`, or `nameof(IsLoadProjectInProgress)`. Equal assignments, repeated dirty/clean calls, rejected null assignments, nested restore entries after the first, and non-final restore-scope disposal raise zero events. Handlers observe the already-updated value. For identity/path/dirty changes, subscriber exceptions propagate and do not roll back the completed mutation. Restore transitions use the stronger exception-safety rules in the next bullet.
- **Restore-guard ownership:** `BeginProjectRestore()` is the only canonical mutation API for `IsLoadProjectInProgress`. It increments a private `int` nesting depth owned only by `ProjectSession` and returns a private idempotent `IDisposable` lease. Depth `0 → 1` sets the property true and raises exactly one event. If that entry event throws, `BeginProjectRestore()` must restore depth to `0` and the property to false before rethrowing; it returns no lease and emits no compensating event. Each successfully returned lease may be disposed multiple times, but decrements depth at most once. Non-final disposal leaves the property true and raises no event; final depth `1 → 0` sets it false and raises exactly one event. On final disposal, mark the lease disposed and set depth/property to the final state before raising; if the exit event throws, the exception propagates but the guard remains false and a later disposal is still a no-op. A lease is bound to the creating `ProjectSession` and cannot affect another instance. Before incrementing, depth `int.MaxValue` throws `InvalidOperationException` without mutation/event; underflow is impossible through the public API. Callers must use `using`/`finally`, so synchronous or asynchronous restore-body exceptions still dispose the lease and clear the guard when the outermost scope exits.
- **Compatibility setter rule:** the existing writable `ICalculationStateService.IsLoadProjectInProgress` remains only as a temporary compatibility surface. `CalculationStateService` may hold one private compatibility **lease reference**, but no bool/depth copy: setting `true` acquires one lease only if that compatibility lease is absent; repeated `true` is a no-op; setting `false` disposes/clears only that compatibility lease; repeated `false` is a no-op. Production restore flow must migrate to `using var restoreScope = _projectSession.BeginProjectRestore()` and must not toggle the compatibility setter. The lease reference is not canonical state; all reads delegate to `IProjectSession.IsLoadProjectInProgress`.
- **Legacy interface rule:** `IProjectInfoService`, `IProjectStateService`, and `IMarkDirtyService` expose the same `ProjectSession` state/methods. Preferred implementation is for `ProjectSession` to implement them directly. Any retained adapter may contain only an `IProjectSession` reference (and, for `CalculationStateService`, the single compatibility lease described above), never copied identity/path/dirty/guard values.

### Out of scope / Must-NOT-Have

- No writable `ProjectSession` Climate, Construction, Thermal, Hydraulics, results, calculation, export, dialog, persistence DTO, or command slices.
- No forwarding from `ProjectSession` into `CalculationContext.Update*`; no `CalculationContext` replacement, facade, constructor change, event/reset semantic change, or new subscription.
- No migration of `NewCalculation`, Open/Save/SaveAs, close handling, module reset/restore, calculation, report, or export orchestration into `ProjectSession`.
- No snapshot, compensation, transaction, rollback, persistent-import undo, `.bak` fallback, schema dispatch, unsupported-version rejection, JSON property rename/order policy, or new `.smc` field.
- No package, SDK, target framework, formula, UI design, installer, publish, presentation, or release artifact change.
- Do not stage, reset, revert, clean, overwrite, or absorb any pre-existing dirty path. Treat live status at execution start as protected baseline, including `src/SnowMeltingCalculator.csproj`, `.gitignore`, `docs/architecture-migration/TASK_CONTEXT.md`, installer/publish/presentation artifacts, and untracked owner/tooling evidence.

## Verification strategy

- **Method:** characterization-first TDD. Every behavioral seam receives a failing test before its production change. Preserve existing assertions; add exact counts and field-level postconditions rather than replacing behavior tests with source grep.
- **Test project:** `tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj` (`net8.0-windows`, NUnit/Moq).
- **Targeted lane:** `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --filter "FullyQualifiedName~ProjectSession|FullyQualifiedName~ProjectStateServiceTests|FullyQualifiedName~CalculationStateServiceGuardTests"` — exit 0.
- **Affected lifecycle lane:** `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --filter "FullyQualifiedName~MainViewModelTests|FullyQualifiedName~ResetOrchestrationTests|FullyQualifiedName~ResultsViewModelOpenProjectTests|FullyQualifiedName~CircuitsViewModelEventLeakTests|FullyQualifiedName~ResultsStabilizationPhase1|FullyQualifiedName~DoubleCalculationPreventionTests"` — exit 0.
- **Persistence lane:** `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --filter "FullyQualifiedName~ProjectRoundTripTests|FullyQualifiedName~ProjectFileServiceResultTests|FullyQualifiedName~ProjectFileServiceAtomicityTests|FullyQualifiedName~ProjectFileServiceMutationTests"` — exit 0.
- **Build/full gate:** `dotnet build src/SnowMeltingCalculator.csproj -c Debug`, `dotnet build src/SnowMeltingCalculator.csproj -c Release`, and `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Release` — each exit 0, with logs under `docs/architecture-migration/evidence/phase-1-project-session-shell/`.
- **Runtime/user-flow gate:** agent-executed new → edit → save/new decisions; load v1.0 → load v1.1 → edit; failed parse; injected module-restore failure; repeated load/reset. Capture assertions/logs without requiring human clicks by using test doubles and the existing WPF/NUnit STA harness.
- A green build is not sufficient. Completion requires tests, negative architecture checks, evidence receipts, regenerated maps/widget, dirty-scope audit, and all four final verifiers.

## Execution strategy

- **Mandatory authorization gates:** this planning session edits only `.omo/plans/phase-1-project-session-shell.md` and stops. The owner must next approve the repository phase plan with `/architecture-approve phase-1-project-session-shell`. Approval records the gate but does not execute anything. Only a later, separate, explicit `/architecture-start phase-1-project-session-shell` may begin implementation. `$start-work`, a generic worker session, PR creation, or shipping flags are not valid substitutes and must not bypass either architecture gate.
- **Wave 1 — Baseline and RED characterization:** tasks 1-3 can proceed only on non-overlapping test/evidence files; production remains untouched.
- **Wave 2 — Canonical owner and compatibility:** tasks 4-6 are sequential. Never leave a completed task with two writable lifecycle stores; temporary compile-broken/red-test states are allowed only inside the same task/commit.
- **Wave 3 — Narrow integration and regression:** tasks 7-8 are sequential after DI identity is proven.
- **Wave 4 — Architecture evidence and dossier:** tasks 9-10 follow all production/test gates.
- **Dependency chain:** 1 → {2,3} → 4 → 5 → 6 → 7 → 8 → 9 → 10 → {F1,F2,F3,F4}.
- Before every edit, compare the target path against the task-1 dirty manifest. If it was already dirty, preserve the original diff and produce a path-specific before/after patch receipt; if safe separation is impossible, stop and request owner direction rather than overwrite.

## Todos

- [x] 1. Capture the live repository and behavior baseline
  - **References:** `AGENTS.md`; `docs/architecture-migration/AGENTS.md:6-15,58-70,87-95`; `docs/architecture-migration/TASK_CONTEXT.md`; current anchor observed during planning `021d4abd159aa71c4a19c7a6536851264e5a58ca` (informational only; re-verify live).
  - **Work:** record canonical root, branch/upstream, full HEAD, ahead/behind, complete NUL-safe dirty inventory, relevant fixture hashes, existing targeted/full test outcomes, and exact protected-path manifest in `docs/architecture-migration/evidence/phase-1-project-session-shell/baseline.md` plus raw logs. Do not mutate/stage repository state while collecting.
  - **Acceptance:** evidence names every modified/deleted/untracked path; distinguishes pre-existing changes from Phase 1; records command, cwd, timestamp, exit code, and artifact hash; no production/docs changes other than the new evidence directory.
  - **QA — happy:** run Git identity/status and baseline build/test commands; expect internally consistent root/HEAD/status and executable logs.
  - **QA — failure:** if root differs, tests do not actually execute, fixture is absent, or a target file is already dirty in a way that cannot be preserved, mark baseline `BLOCKED` and stop before edits.
  - **Evidence:** `docs/architecture-migration/evidence/phase-1-project-session-shell/baseline.md`, `baseline-git-status.bin`, `baseline-tests.trx` or equivalent full console log.
  - **Commit:** `test(architecture): capture phase 1 lifecycle baseline` — include only new Phase 1 evidence and tests; never include pre-existing dirty paths.

- [x] 2. Add lifecycle-owner and compatibility contract tests in RED
  - **References:** `src/Services/Results/ProjectStateService.cs`; `IProjectStateService.cs`; `IProjectInfoService.cs`; `IMarkDirtyService.cs`; `src/Configuration/ServiceCollectionExtensions.cs:172-175`; `tests/SnowMeltingCalculator.Tests/Services/Results/ProjectStateServiceTests.cs`.
  - **Work:** add `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectSessionTests.cs` and DI contract tests. Specify initial values; idempotent `MarkDirty`/`MarkClean`; `PropertyChanged` exactly once per real transition and never for equal assignments; identity/path notification behavior; all legacy interfaces and `IProjectSession` observing one canonical instance; and reflection/behavior proof that compatibility adapters contain no mutable lifecycle backing fields.
  - **Acceptance:** tests fail before `ProjectSession` exists; contract explicitly forbids cached/duplicated lifecycle values; `ProjectNumber`, `ProjectObject`, `CurrentFilePath`, `IsDirty`, and restore guard each have one writable owner.
  - **QA — happy:** run the targeted lane and preserve the expected RED failures tied only to missing shell/forwarding semantics.
  - **QA — failure:** add adversarial tests that resolve each interface separately, mutate through every writable compatibility entry point, and fail if any view diverges or emits duplicate events.
  - **Evidence:** `docs/architecture-migration/evidence/phase-1-project-session-shell/tdd-owner-red.md` with command/output and expected failing test names.
  - **Commit:** combine RED tests with task 4's GREEN implementation if repository policy disallows intentionally failing commits; otherwise `test(project): characterize ProjectSession ownership`.

- [x] 3. Add lifecycle-flow, repeated-cycle, and failure characterization in RED
  - **References:** `src/ViewModels/Shell/MainViewModel.cs:165-225`; `src/ViewModels/Results/ResultsViewModel.cs:730-826,945-977,1510-1608`; `src/Services/Project/ProjectLoadOrchestrator.cs:60-232`; `src/Core/CalculationContext.cs:131-230`; existing `MainViewModelTests.cs`, `ResetOrchestrationTests.cs`, `ResultsViewModelOpenProjectTests.cs`, `CircuitsViewModelEventLeakTests.cs`, `DoubleCalculationPreventionTests.cs`.
  - **Work:** add or extend tests for: load A then B on one singleton graph; reset/load repeated at least three cycles; one edit after load; dirty Yes/No/Cancel plus save-result failure for new and close flows; parse failure; injected restore exceptions at representative early and late boundaries; guard false after all exits; exact pre-existing partial state, path, dirty state, `PropertyChanged`, `ContextChanged`, `RefreshAll`, `ProjectChanged`, subscription, and thermal/hydraulic calculation counts. Characterize—do not improve—partial restore semantics.
  - **Acceptance:** tests record field-level old/new values after failure, including values already mutated and values retained from the prior project; prove no rollback; prove no duplicate handler/recalculation after repeated cycles; avoid brittle assertions on unrelated WPF notifications.
  - **QA — happy:** current implementation passes behavior-preservation characterization that already exists and new baseline tests whose semantics are current; shell-specific expectations remain RED.
  - **QA — failure:** injected restore throw must propagate/currently surface as observed, leave the characterized partial state, keep `CurrentFilePath` at its observed value, preserve observed `IsDirty`, and always clear the load guard; a test must fail if rollback is accidentally introduced.
  - **Evidence:** `docs/architecture-migration/evidence/phase-1-project-session-shell/tdd-flows-red.md` and named test output.
  - **Commit:** combine with the corresponding GREEN integration tasks when necessary; otherwise `test(project): lock lifecycle flow semantics`.

- [x] 4. Introduce the minimal canonical `IProjectSession` and `ProjectSession`
  - **References:** this plan's `Decision-complete IProjectSession contract`; `src/Services/Results/ProjectStateService.cs`; `src/Services/Results/IProjectStateService.cs`; `src/Services/Results/IProjectInfoService.cs`; `src/Services/Results/IMarkDirtyService.cs`; `docs/architecture-migration/maps/target-invariants.md` (`ProjectSession` and single-writer invariants).
  - **Work:** create `src/Services/Project/IProjectSession.cs` and `ProjectSession.cs` exactly as specified by the embedded contract. Implement the five lifecycle values, three mutation/transition methods, ordinal equality, null rejection, exact `PropertyChanged` behavior, and checked nested exception-safe restore leases. Do not infer API shape from another planning artifact and do not add convenience commands or dependencies.
  - **Acceptance:** task-2 owner tests turn GREEN; the public API matches the embedded contract exactly; one class stores all five lifecycle values and restore depth; no application service gains a concrete ViewModel dependency; files expose no file IO, dialog, module, command, coordinator, `CalculationContext`, service-locator, or dependency-bag member.
  - **QA — happy:** construct directly; verify exact defaults; mutate every property/state once; nest two restore scopes; dispose inner then outer; assert exact values and one event per real outer transition.
  - **QA — failure:** null identity assignments throw without mutation/event; equal/repeated transitions emit nothing; double-dispose is harmless; an exception inside nested `using` scopes leaves guard false after outer disposal; a throwing entry-event subscriber leaves depth 0/guard false and returns no lease; a throwing final-exit subscriber still leaves depth 0/guard false and the lease idempotently disposed. Use private-state reflection only in the unit test to set depth to `int.MaxValue`; `BeginProjectRestore()` must then throw `InvalidOperationException` without mutation/event or public test seam.
  - **Evidence:** `docs/architecture-migration/evidence/phase-1-project-session-shell/project-session-contract.md` and targeted GREEN log.
  - **Commit:** `feat(project): add canonical ProjectSession lifecycle owner`.

- [x] 5. Convert legacy project-state contracts to forwarding-only compatibility surfaces
  - **References:** `src/Services/Results/IProjectInfoService.cs`; `IProjectStateService.cs`; `IMarkDirtyService.cs`; `ProjectStateService.cs`; all references identified by LSP/Codegraph, especially `MainWindow.xaml.cs`, `MainViewModel.cs`, `ResultsViewModel.cs`, module dirty-service consumers, and tests.
  - **Work:** make legacy contracts forward to the same `ProjectSession` instance. Preferred minimum: `ProjectSession` implements the legacy interfaces directly and `ProjectStateService` is removed after references are migrated; if a concrete compatibility adapter must remain, it holds only an `IProjectSession` reference and zero lifecycle backing fields. Preserve public semantics and names required by callers.
  - **Acceptance:** all mutation through `IProjectStateService`/`IMarkDirtyService` changes `IProjectSession` immediately; all interfaces resolve singleton-identical canonical state; no `_isDirty`, path, identity, or guard duplicate storage exists outside `ProjectSession`; existing `ProjectStateServiceTests` are migrated/retained as compatibility tests, not deleted.
  - **QA — happy:** mutate through each legacy interface and observe exact values/events through `IProjectSession` and other aliases.
  - **QA — failure:** reflection/behavior test fails for any compatibility type containing duplicate lifecycle fields or for any DI resolution yielding divergent state.
  - **Evidence:** `docs/architecture-migration/evidence/phase-1-project-session-shell/compatibility-adapters.md` and targeted test log.
  - **Commit:** `refactor(project): route legacy state contracts through ProjectSession`.

- [x] 6. Move restore-guard storage to `ProjectSession` without changing calculation semantics
  - **References:** `src/Services/Navigation/ICalculationStateService.cs:100-113`; `CalculationStateService.cs:117,128-132`; `ResultsViewModel.LoadProjectDataAsync`; Thermal/Circuits guard readers; `CalculationStateServiceGuardTests.cs`.
  - **Work:** inject `IProjectSession` into `CalculationStateService`; implement the embedded compatibility-setter lease rule with no local bool/depth field. On compatibility `false`, copy the adapter-held lease to a local, clear the field first, then dispose it so an exit-event exception cannot retain a stale lease. Preserve `SetPipeSpacing` canonical-source checks and event behavior. Replace the production true/false toggle in `ResultsViewModel.LoadProjectDataAsync` with `using var restoreScope = _projectSession.BeginProjectRestore()` spanning the same restore-body boundary; remove the old guard assignment/reset while retaining any unrelated `finally` work. Existing module readers may temporarily read via `ICalculationStateService`, but that read delegates to the canonical session.
  - **Acceptance:** only `ProjectSession` stores guard bool/depth; successful and failed outer restores each produce exactly false → true → false; nested scopes produce no intermediate false/event; compatibility true/true/false/false is idempotent and uses only one adapter-held lease; `CalculationStateServiceGuardTests` remain green; noncanonical pipe-spacing writers still fail exactly as before; no module ViewModel ownership or local `_isResetting`/`_isLoadingProject` flag is removed.
  - **QA — happy:** successful load observes one guard entry/exit and unchanged pipe-spacing synchronization.
  - **QA — failure:** injected restore exception clears canonical and compatibility guard views; test fails if either view remains true or if source validation is bypassed.
  - **Evidence:** `docs/architecture-migration/evidence/phase-1-project-session-shell/restore-guard.md` and targeted guard/flow logs.
  - **Commit:** `refactor(project): centralize restore guard in ProjectSession`.

- [x] 7. Register one singleton lifecycle graph and rewire only existing consumers
  - **References:** `src/Configuration/ServiceCollectionExtensions.cs:149,161,172-187`; constructors of `MainWindow`, `MainViewModel`, `ResultsViewModel`, `ProjectLoadOrchestrator`, and `CalculationStateService`.
  - **Work:** register `ProjectSession` once as singleton and map `IProjectSession`, `IProjectInfoService`, `IProjectStateService`, and `IMarkDirtyService` to it (or to stateless forwarding adapters bound to it). Adjust constructors only where required to consume the canonical lifecycle owner. Keep `CalculationContext`, module VMs, `ProjectLoadOrchestrator`, `ResultsViewModel`, and `MainViewModel` singleton lifetimes unchanged.
  - **Acceptance:** DI tests prove one canonical object/state across every alias and consumer; no circular dependency; no transient/scoped lifecycle adapter; no constructor gains module-state ownership or orchestration responsibilities; Debug build passes.
  - **QA — happy:** build a real `ServiceProvider`, resolve every alias and shell/root consumer, mutate once, and observe one event/state transition everywhere.
  - **QA — failure:** resolution graph test fails on duplicate registration, circular dependency, divergent adapter state, or a missing interface mapping.
  - **Evidence:** `docs/architecture-migration/evidence/phase-1-project-session-shell/di-runtime.md`, DI test log, and build log.
  - **Commit:** `refactor(di): bind lifecycle services to ProjectSession`.

- [x] 8. Preserve lifecycle orchestration, partial-failure semantics, and `.smc` compatibility through the new owner
  - **References:** `MainWindow.xaml.cs:168-211`; `MainViewModel.cs:178-225`; `ResultsViewModel.cs:730-826,945-977,1510-1608`; `ProjectLoadOrchestrator.cs:60-232`; `ProjectFileService.cs`; `ProjectData.cs`; fixture `tests/SnowMeltingCalculator.Tests/Fixtures/v1-sample.smc`; repository v1.1 fixtures catalogued in `docs/architecture-migration/evidence/persistence-fixtures.md`.
  - **Work:** replace lifecycle reads/writes with the canonical session/compatibility views while preserving call order, dirty prompts, save-failure behavior, reset-before-normal-open, direct-load behavior, `RefreshAll`/`ProjectChanged` ordering, and forward-only restore. Do not move command bodies or module reset/restore logic. Extend persistence tests so the same production path loads the assertion-backed v1.0 fixture and at least one catalogued v1.1 fixture without changing serializers or DTOs.
  - **Acceptance:** task-3 tests turn GREEN; new/open/save/close observable behavior and exact counts match baseline; early/late injected restore failures match field-level partial-state characterization and do not rollback; v1.0/v1.1 accepted corpus still loads; save/reload preserves schema shape and current round-trip fields; no new version rejection or `.bak` fallback exists.
  - **QA — happy:** load A, load B, edit once, save/reload; expect B-only lifecycle identity/path, clean after load/save, dirty after one edit, no stale Results, and exact one-event/recalculation assertions from tests.
  - **QA — failure:** corrupt input leaves pre-load state untouched; module restore failure leaves exactly characterized partial state and guard false; save-result failure keeps dirty true and prevents destructive new/close continuation.
  - **Evidence:** `docs/architecture-migration/evidence/phase-1-project-session-shell/lifecycle-user-flows.md`, `persistence-compatibility.md`, and targeted/affected/persistence logs.
  - **Commit:** `refactor(project): route lifecycle flows through ProjectSession`.

- [x] 9. Run full gates and prove scope/single-owner invariants
  - **References:** `docs/architecture-migration/AGENTS.md:44-70,87-95`; task-1 protected manifest; all Phase 1 tests and production diffs.
  - **Work:** run Debug/Release builds, targeted/affected/persistence/full Release tests, and agent-executed lifecycle flows. Use Codegraph/LSP/reference inventory plus source-aware inspection to prove one lifecycle store, unchanged `CalculationContext` contract, no module slices in `ProjectSession`, no new application-service → concrete-ViewModel edge, and no prohibited file changes.
  - **Acceptance:** all commands exit 0 and actually execute nonzero relevant tests; no skipped required fixture test; test counts and hashes recorded; diff contains only allow-listed Phase 1 paths plus dossier/evidence updates; every pre-existing dirty path remains byte/diff-preserved except owner-authorized additive `TASK_CONTEXT.md` updates.
  - **QA — happy:** verifier independently matches test results, live source, evidence hashes, and architecture invariants.
  - **QA — failure:** deliberately search for duplicate lifecycle backing fields, `ProjectSession` module members, `CalculationContext` edits, rollback/version logic, concrete-VM service dependencies, skipped tests, or protected-path drift; any hit blocks completion until explained and removed.
  - **Evidence:** `docs/architecture-migration/evidence/phase-1-project-session-shell/final-gates.md`, raw build/test logs, source-invariant report, and final dirty-manifest comparison.
  - **Commit:** `test(architecture): verify ProjectSession phase gates`.

- [x] 10. Update the shared architecture dossier and generated widget after verification
  - **References:** `docs/architecture-migration/maps/architecture-model.json`; six views `compile-time.md`, `di-runtime.md`, `state-ownership.md`, `reactive.md`, `persistence.md`, `user-flow.md`; `characterization-tests.md`; `persistence-compatibility.md`; `target-invariants.md`; `docs/architecture-migration/widget/generate-widget.mjs`; `architecture-widget.html`; `TASK_CONTEXT.md`; context update contract in `docs/architecture-migration/AGENTS.md:72-85`.
  - **Work:** update the shared model first, then regenerate all six filtered views and widget from it. Mark lifecycle current owner as `ProjectSession`, record compatibility aliases, unchanged `CalculationContext`/module owners, preserved partial restore semantics, exact coverage/evidence links, accepted fixtures, remaining limitations/deferred transactional restore, and Phase 1 status. Correct the known `ST-021` reset/ThermalInputs doc-vs-source drift without changing behavior. Append—never silently rewrite—`TASK_CONTEXT.md` decisions/status/open questions/next action/journal.
  - **Acceptance:** model validates; each documented edge identifies kind and current evidence; all six views agree with the model; widget generation/tests succeed and no hand-edited generated drift remains; `TASK_CONTEXT.md` preserves prior history and points to receipts; architecture widget labels ProjectSession implemented only after gates pass.
  - **QA — happy:** run the repository's existing widget/model validation and generation commands discovered from package scripts/README; compare generated outputs and verify all evidence references resolve.
  - **QA — failure:** stale or unresolved IDs, disagreement among views, missing evidence, hand-edited widget output, or a claim of transactional restore/module ownership causes validation failure and blocks handoff.
  - **Evidence:** `docs/architecture-migration/evidence/phase-1-project-session-shell/dossier-update.md`, model/widget validation logs, and generated artifact hashes.
  - **Commit:** `docs(architecture): record ProjectSession shell completion`.

## Final verification wave

- [x] F1. Plan compliance audit
  - Independently map every implemented diff hunk to tasks 1-10, confirm all acceptance/QA/evidence clauses, and reject any unplanned feature or missing receipt.
  - **Approval condition:** unconditional APPROVE with exact plan path, commit range, and evidence hashes; otherwise return blocking findings.

- [x] F2. Code quality and architecture review
  - Review canonical ownership, adapter statelessness, DI lifetime, event semantics, error handling, constructor graph, and maintainability; explicitly inspect for dual stores, god-object growth, module ownership migration, concrete-VM service dependencies, and `CalculationContext` drift.
  - **Approval condition:** unconditional APPROVE and no high/medium correctness or architecture finding.

- [x] F3. Real agent-executed lifecycle QA
  - Execute new/dirty decisions, save failure, v1.0 then v1.1 second load, repeated reset/load, one post-load edit, corrupt file, and injected early/late restore failure using the real test harness and production DI graph; verify exact final state/events/recalculations.
  - **Approval condition:** all named scenarios pass with reproducible logs and no skipped required case.

- [x] F4. Scope fidelity and dirty-worktree audit
  - Compare baseline/final NUL-safe manifests and hashes; confirm no protected user change was overwritten, staged, reverted, or bundled; confirm no forbidden formula/UI/package/SDK/installer/release/persistence/module-slice change.
  - **Approval condition:** unconditional APPROVE; any unexplained protected-path or out-of-scope drift blocks completion.

## Commit strategy

- No commit or implementation action is authorized by this plan artifact alone. The owner must first run `/architecture-approve phase-1-project-session-shell`; implementation may begin only after a later explicit `/architecture-start phase-1-project-session-shell`.
- Use atomic commits aligned to the todo commit lines; where strict TDD would create a deliberately red commit, combine that RED test with its smallest GREEN implementation while preserving the red-run evidence receipt.
- Never use broad `git add .`; stage explicit allow-listed paths only after comparing against the task-1 protected manifest.
- Do not amend, squash, rebase, push, or create a PR unless the separately started worker workflow and owner authorization request it.
- Suggested sequence: baseline/tests → canonical shell → compatibility owner → restore guard → DI → lifecycle integration → verification → dossier/widget.

## Success criteria

- `ProjectSession` is the sole writable canonical owner of identity, path, dirty, and restore guard; legacy surfaces cannot diverge or store copies.
- Existing new/open/save/close/reset behavior, event/recalculation multiplicity, current partial restore-failure semantics, and supported `.smc` behavior remain characterized and green.
- `CalculationContext` and Climate/Construction/Thermal/Hydraulics ownership and reactive contracts remain unchanged.
- No transactional rollback, schema/version-policy change, `.bak` recovery, god object, new coordinator, formula/UI/package/SDK/installer/release change, or protected dirty-path overwrite exists.
- Debug/Release builds, targeted/affected/persistence/full Release tests, runtime flow QA, architecture invariant checks, six-view/model/widget validation, and final dirty audit all pass with reproducible evidence.
- `TASK_CONTEXT.md` and the shared architecture dossier accurately record Phase 1 implementation, remaining limitations, receipts, and the next owner gate.
- F1-F4 all return unconditional approval; only then may the architecture execution session report Phase 1 ready for owner acceptance. This plan stops before approval: the next permitted action is `/architecture-approve phase-1-project-session-shell`, and implementation requires a later separate explicit `/architecture-start phase-1-project-session-shell`. No `$start-work`, generic worker, PR, or ship workflow may bypass these gates.
