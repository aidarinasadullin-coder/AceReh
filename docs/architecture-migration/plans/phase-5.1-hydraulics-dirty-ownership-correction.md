# phase-5.1-hydraulics-dirty-ownership-correction

## Status and Boundary

This is the frozen plan candidate for a narrow correction wave after the
accepted `phase-5-hydraulics-state`. It does not reopen the parent phase and
does not authorize execution or result acceptance.

The active workflow authority remains `docs/architecture-migration/STATE.json`.
The implementer must not edit `STATE.json`, the accepted Phase 5 plan, or its
final receipt. Execution must capture a fresh baseline-relative dirty delta;
the draft-time worktree observation is not an execution baseline.

## Objective

Remove duplicate dirty ownership from `CircuitsViewModel` while preserving the
Phase 5 canonical contract:

```text
Changed + HydraulicsMutationOrigin.User -> exactly one dirty intent
NoChange                                  -> zero dirty intents
Rejected                                  -> zero dirty intents
Changed + non-User origin                 -> zero dirty intents
```

`ProjectSessionHydraulicsState.Commit()` remains unchanged and is the sole
canonical owner of hydraulics dirty intent. `CircuitsViewModel` remains a WPF
adapter, not a lifecycle-state owner.

## In Scope

Production write-set is limited to:

- `src/ViewModels/Hydraulics/CircuitsViewModel.cs`
  - remove `_markDirtyService`;
  - remove the `IMarkDirtyService markDirtyService` constructor parameter,
    assignment, and null-check;
  - remove both direct `_markDirtyService.MarkDirty()` calls in `SetInputData()`;
  - preserve `ApplyGlobalInputs(..., HydraulicsMutationOrigin.User)`, guards,
    mirror behavior, `Calculate()`, and `CalculateAllCollectors()`.

Required dependent test/constructor edits are limited to removing the redundant
constructor argument from every affected direct construction and helper. The
existing production registration in
`src/Configuration/ServiceCollectionExtensions.cs` remains unchanged:
`AddSingleton<CircuitsViewModel>()` must resolve the shorter constructor.

New Phase 5.1 evidence, focused characterization/guard coverage, and the
narrowly affected architecture ownership/reactive/compile-time/DI assessment
are in scope.

## Out of Scope

Do not change `STATE.json`, the accepted Phase 5 plan/receipt,
`ProjectSessionHydraulicsState.Commit()`, `ProjectSession.MarkDirty()`, or the
`IMarkDirtyService` contract. Do not change `HydraulicInputData` notification
or validation behavior, formulas, `HydraulicsStateCoordinator`, persistence,
`.smc` wire format, Results projection, reset/load routing, thermal/climate
synchronization, or unrelated dirty paths. Do not add compatibility overloads,
fallbacks, new abstractions, or broad refactors.

`Rejected -> zero dirty` is an intentional correction under the Phase 5 state
contract. Do not add input rollback or validation redesign in this phase.

## Required Baseline and Characterization

Execution starts by recording the live Git root, protected modified/untracked
paths, exact plan identity, and baseline test/build status. Protected paths are
baseline-relative and must not be reset, cleaned, reverted, or overwritten.

Before production correction, add or align RED characterization at the
narrowest existing hydraulics/state seam. It must establish:

- changed valid `User` mutation: one canonical dirty intent;
- equal candidate: `NoChange`, zero dirty intents, zero state changed event;
- invalid candidate: `Rejected`, zero dirty intents, zero state changed event;
- changed lifecycle/non-User mutation: zero dirty intents;
- supply inputs retain `Calculate()` routing;
- glycol inputs retain `CalculateAllCollectors()` routing.

Do not assert two `ProjectSession.PropertyChanged` events: `MarkDirty()` is
idempotent. The test must distinguish canonical dirty intent from lifecycle
notification multiplicity.

## Implementation Sequence

Use one sequential lane:

1. Capture fresh baseline and protected delta.
2. Add or align the RED characterization above.
3. Apply the minimal `CircuitsViewModel` correction.
4. Update all affected constructor/helper call sites without a shim.
5. Run targeted hydraulics/state, adapter, multiplicity, and guard tests.
6. Run affected hydraulics/lifecycle integration tests.
7. Run Debug and Release builds and the applicable full test gate.
8. Reconcile reused Phase 5 evidence against the fresh write-set.
9. Assess six views and run focused dirty-state manual QA.
10. Produce independent F1/F2/F3 verification receipts and one consolidated
    F4 receipt.

## Verification Scope

Use `dotnet test` against the existing test project; do not invent a second
test harness. The focused command is:

```powershell
dotnet test tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj -c Debug --no-restore --filter "FullyQualifiedName~ProjectSessionHydraulicsStateTests|FullyQualifiedName~HydraulicsMultiplicityCharacterizationTests|FullyQualifiedName~CircuitsViewModelTests|FullyQualifiedName~CircuitsViewModelEventLeakTests|FullyQualifiedName~ResetOrchestrationTests|FullyQualifiedName~HydraulicsStateLegacyStoreGuardTests"
```

Expected result: exit code `0`, no failed tests, and every selected test
identity executed. Record the generated TRX/log output under the Phase 5.1
evidence directory. The affected integration command is:

```powershell
dotnet test tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj -c Debug --no-restore --filter "FullyQualifiedName~GlycolAutoRecalculationTests|FullyQualifiedName~PipeSpacingSynchronizationTests|FullyQualifiedName~DoubleCalculationPreventionTests|FullyQualifiedName~ClimateToHydraulicsIntegrationTests|FullyQualifiedName~ThermalToHydraulicsIntegrationTests|FullyQualifiedName~CircuitsViewModelColdStartTests|FullyQualifiedName~CalculationContextWriterAuthorityTests|FullyQualifiedName~ProjectLifecycleFlowCharacterizationTests|FullyQualifiedName~ResultsViewModelOpenProjectTests"
```

Expected result: exit code `0`, no new failures, and no unexpected
NotExecuted/skipped identities. If a named fixture or test class is absent,
stop and revise the frozen plan instead of silently broadening the filter.

The guard must prove that `CircuitsViewModel` has no `IMarkDirtyService` field,
constructor parameter, or direct `MarkDirty()` call, while canonical state
still owns the User dirty intent. All direct constructor call sites must
compile; no alternate constructor may be added.

Build commands are:

```powershell
dotnet build src\SnowMeltingCalculator.csproj -c Debug --nologo
dotnet build src\SnowMeltingCalculator.csproj -c Release --nologo
```

Expected result for each: exit code `0`, zero warnings, zero errors. The full
gate is:

```powershell
dotnet test tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj -c Release --no-build --no-restore
```

Expected result: exit code `0`; record total passed/failed/skipped and every
NotExecuted identity, then reconcile them against the fresh baseline. Record
exact commands, exit codes, counts, and baseline-relative differences. Do not
report reused Phase 5 counts as new Phase 5.1 results.

## Evidence Reuse and Six Views

Reuse Phase 5 evidence only for behavior outside this write-set, explicitly
labeling it as reused. Persistence, `.smc`, Results, formulas, coordinator,
reset/load, and thermal/climate behavior are expected unchanged and require an
honest unchanged/reused rationale rather than unrelated regeneration.

Assess all six views:

- compile-time: removed constructor dependency and affected references;
- DI/runtime: unchanged singleton registration and identity;
- state ownership: one canonical dirty owner;
- reactive: dirty and calculation/event multiplicity;
- persistence: unchanged mapper and `.smc` evidence boundary;
- user flow: valid edit, rejected/no-op attempt, load, reset, save.

Refresh only affected map/model/widget inputs and correction evidence. Do not
regenerate unrelated artifacts.

## Manual QA

Manual QA is required because dirty state is user-visible. Launch the WPF
application with the repository's normal Debug startup command:

```powershell
dotnet run --project src\SnowMeltingCalculator.csproj -c Debug --no-restore
```

Use the existing operator/browser/UI harness recorded by Phase 5 evidence when
available; otherwise perform the same actions in the running WPF window and
capture screenshots plus the application/test log in the Phase 5.1 evidence
directory. The operator must record the action, visible result, and dirty
transition count for every step.

Run this focused flow:

1. load a valid project: expected project data appears and the dirty indicator
   is clean after load;
2. edit supply spacing/heat: expected exactly one dirty transition and the
   existing selected-collector `Calculate()` result;
3. edit glycol type/concentration: expected exactly one dirty transition and
   the existing all-collector `CalculateAllCollectors()` result;
4. attempt rejected invalid inputs: expected no additional dirty transition,
   no canonical `Changed` event, and no new calculation beyond the existing
   characterized routing;
5. load/reset/save/reload: expected load/reset boundaries remain clean, save
   clears dirty state, reload preserves values/results, and no duplicate
   subscription behavior appears.

Expected manual-QA result: all five flows pass, no application exception or
unexpected console/log error occurs, and screenshots/logs are attached to the
receipt. If the WPF application cannot be launched in the execution boundary,
stop with a blocked User Risk domain; do not mark manual QA passed by proxy.

Reuse prior UI evidence for untouched behavior only; record fresh evidence for
the dirty-state correction.

## Stop Rules and Rollback

Stop without scope expansion if a non-state mutation needs the ViewModel dirty
dependency, if `Commit()` must change, if rollback/validation redesign appears
necessary, if persistence/Results/formulas/coordinator/lifecycle routing is
affected, if a compatibility shim is required, or if protected baseline and
new failures cannot be separated. Preserve RED evidence and return to owner
review.

Rollback is limited to `CircuitsViewModel.cs`, affected constructor/test call
sites, Phase 5.1 tests, and Phase 5.1 evidence. Never use reset, clean, revert,
or overwrite unrelated paths.

## Final Verification Domains

F1 Conformance / Scope / Provenance: run
`git status --short`, compare the final status to the Todo 1 protected baseline,
and run `node docs/architecture-migration/workflow/validate-state.mjs validate --check-plan`.
Expected result: only allowed Phase 5.1 paths changed, accepted Phase 5
artifacts and `STATE.json` are untouched, validator exit `0`, and reused/new
evidence is labeled honestly.

F2 Architecture / Code Quality: run the Phase 5.1 guard tests from the focused
`dotnet test` command and inspect the compiler/build output. Expected result:
`CircuitsViewModel` has no `IMarkDirtyService` field/parameter/direct call,
canonical state retains the User dirty call, DI registration remains the same,
and no forbidden production path is changed. Record the guard TRX and a
baseline-relative diff summary.

F3 Executable QA / User Risk: run the two focused `dotnet test` commands, both
`dotnet build` commands, the Release full gate, and the WPF manual-QA command
above. Expected result: all commands exit `0`, exact dirty/call-routing tests
pass, and all five manual flows pass with screenshots/logs.

F4 consolidated receipt: verify that F1, F2, and F3 receipts exist and that
their command outputs agree. Expected result: one receipt names the write-set,
fresh baseline, reused Phase 5 evidence, new Phase 5.1 evidence, exact dirty
counts, residual risks, and manual-QA result.

## Owner Gates

Plan materialization was authorized separately by the owner. The following
remain separate and mandatory:

1. exact canonical/mirror byte identity and SHA-256 freeze;
2. terminal plan review with machine-readable `REVIEW_ID`, `SUBJECT`,
   `RECEIPT`, `VERDICT`, and `REASON`;
3. explicit owner approval of this exact frozen plan and SHA;
4. separate execution authorization;
5. independent verification and separate owner result acceptance.

Plan approval does not authorize execution. Execution authorization does not
imply result acceptance. After result acceptance, stop and await new owner
direction.
