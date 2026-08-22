# Pre-Task 13 Construction -> Thermal Invalidation Correction

## Baseline boundary

- Captured: 2026-08-19 before Task 1 edits.
- Git root: `D:/IA/ace v.2`.
- HEAD: `e655735dfa66c00cf9c53be93d511eda8989e8bf`.
- Branch/upstream: `master` / `origin/master`.
- NUL-safe `git status --porcelain=v1 -z --branch`: `16999` bytes, `255` NUL-terminated records, SHA-256 `794128B2F5CCD9AA5D1D3867A77A32173020D0FD2BD11FC8B096D14F70887054`.
- Staged set: empty.
- Source plan preimage: present, Git blob `fc1a639c93b8ddf379711527cf6308e598af7201`.
- Repository plan target preimage: absent.
- Regression test preimage: absent.
- Correction receipt preimage: absent.
- Production preimage `src/Services/Project/ProjectSessionConstructionState.cs`: `16144` bytes, SHA-256 `E6B8F40877574BD6C15E524250A7C0D1920F3A51DA77847F952C70F16BF10F08`, pre-existing status `??`.
- `TASK_CONTEXT.md` preimage: `201438` bytes, SHA-256 `773CA6D4B4FE7D5D9740A5F4387EBE25BE730D205F8E954773531022C1DE48FC`, pre-existing status `M`.
- Protected user state is not staged, restored, reset, cleaned, or overwritten by this lane.

## TDD RED

Command:

```text
dotnet test "tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Release --filter "FullyQualifiedName~ConstructionThermalInvalidationRegressionTests.UserMutation_WithExistingResult_InvalidatesThermalOnce" --logger "trx;LogFileName=pre-task-13-construction-thermal-red.trx" --results-directory "tests\SnowMeltingCalculator.Tests\TestResults"
```

- The initial run was not accepted as RED because the new fixture did not compile: `CS0103` for `ModuleState`. Only the new test file was corrected by importing its live `SnowMeltingCalculator.Models.Enums` namespace.
- Accepted RED run exit: nonzero.
- TRX counters: total `1`, executed `1`, passed `0`, failed `1`, notExecuted `0`; outcome `Failed`.
- Failing test: `UserMutation_WithExistingResult_InvalidatesThermalOnce`.
- Observed behavioral assertions after mutation status/origin had already passed as `Changed` / `User`:
  - `ThermalViewModel.Result` remained non-null.
  - `CalculationStateService.ThermalNeedsRecalculation` remained `false`.
  - the collected Thermal state sequence was empty instead of one `ModuleState.NeedsRecalculation`.
- Raw evidence: `tests/SnowMeltingCalculator.Tests/TestResults/pre-task-13-construction-thermal-red.trx`, `6660` bytes, SHA-256 `9025E8CB1DE4034F7DB2DD1AC7C51275DE3E47B931FCD802E27B8B8E10A5E273`.
- This is the intended end-to-end notification failure: the accepted canonical mutation updates the real stable `CurrentProjection`, but `ThermalViewModel` receives no `DataChanged` event. The fixture does not raise an event, replace the projection with `ConstructionData`, mock the state-service invalidation call, or invoke the private handler.

## Task 1 scope

- Production fix is intentionally not implemented.
- No synthetic `RaiseDataChanged`, replacement `ConstructionData`, or private-handler invocation is used.
- Task 13 dossier/maps/model/widget remain untouched.

## Task 1 verdict

`RED CONFIRMED`. Production correction is intentionally deferred to Todo 2.

## Task 2 production correction and contract matrix

- Production change: `src/Services/Project/ProjectSessionConstructionState.cs:421` contains exactly one executable `_projection.RaiseDataChanged();` call, inside the existing `_projection.IsValid && PublishesDownstream(origin)` branch.
- The notification remains after `_projection.Update(newSnapshot)` and immediately before `_calculationContext?.UpdateConstruction(_projection, "ConstructionState")`; no other production logic was changed.
- The new regression file constructs `ConstructionStateSnapshot` instances through the live four-argument constructor and imports `System.IO` for the source-inventory guard. No synthetic projection event, `ConstructionData`, mock invalidation service, or private handler invocation is used.
- Exact command:

```text
dotnet test "tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Release --filter "FullyQualifiedName~ConstructionThermalInvalidationRegressionTests|FullyQualifiedName~ProjectSessionConstructionStateTests|FullyQualifiedName~ConstructionStateLegacyStoreGuardTests|FullyQualifiedName~CanonicalDefaultConstructionLifecycleTests|FullyQualifiedName~DiRegistrationTests|FullyQualifiedName~ThermalViewModelTests|FullyQualifiedName~CalculationStateServiceTests" --logger "trx;LogFileName=pre-task-13-construction-thermal-contracts.trx" --results-directory "tests\SnowMeltingCalculator.Tests\TestResults"
```

- Exit code: `0`.
- TRX: `tests/SnowMeltingCalculator.Tests/TestResults/pre-task-13-construction-thermal-contracts.trx`, `182448` bytes, SHA-256 `4EC550029145346124F8BB1D653CBE877F48720C3B804AD13EC153C76C43641E`.
- TRX counters: total `136`, executed `136`, passed `136`, failed `0`, error `0`, timeout `0`, aborted `0`, inconclusive `0`, notExecuted `0`, notRunnable `0`.
- Outcome reconciliation: `Passed=136`; no non-passed results and no new `NotExecuted` identities.
- Regression contract identities all passed: `UserMutation_WithExistingResult_InvalidatesThermalOnce` (three parameterized cases), `TemplateMutation_WithExistingResult_InvalidatesThermalOnce`, `UserMutation_WithoutExistingResult_PublishesProjectionButDoesNotInvalidateThermal`, `NoChange_WithExistingResult_IsSilent`, `Rejected_WithExistingResult_IsSilent`, `LifecycleMutation_WithExistingResult_IsSilent` (three lifecycle origins), and `Cancelled_IsNotReachableFromCurrentProductionMutationPaths`.

## Task 3 successful recalculation

- `SuccessfulRecalculation_AfterInvalidation_ReturnsThermalToActual` uses the
  same real session/projection graph and the production `CalculateCommand` with
  the real `ThermalCalculator` and validators.
- The accepted Construction mutation emits one `NeedsRecalculation`; the
  successful calculation creates a non-null result, clears
  `ThermalNeedsRecalculation`, and produces the exact Thermal state sequence
  `NeedsRecalculation`, `Calculating`, `Actual`.
- An isolated exploratory run produced
  `pre-task-13-construction-thermal-recalculation-release.trx`. This is an
  intermediate artifact, not one of the three required final test gates. It
  contains `1 total / 1 executed / 1 passed / 0 failed / 0 notExecuted`.

## Task 3 automated gates

### Production builds

- Debug command: `dotnet build "src\SnowMeltingCalculator.csproj" -c Debug --nologo`.
- Debug result: exit `0`, warnings `0`, errors `0`.
- Release command: `dotnet build "src\SnowMeltingCalculator.csproj" -c Release --nologo`.
- Release result: exit `0`, warnings `0`, errors `0`.
- The Debug test assembly was also built before the required `--no-build`
  affected gate: exit `0`, warnings `0`, errors `0`.

### Focused Release

- Filter: `FullyQualifiedName~ConstructionThermalInvalidationRegressionTests|FullyQualifiedName~ProjectSessionConstructionStateTests|FullyQualifiedName~ConstructionStateLegacyStoreGuardTests|FullyQualifiedName~CanonicalDefaultConstructionLifecycleTests|FullyQualifiedName~DiRegistrationTests|FullyQualifiedName~ThermalViewModelTests|FullyQualifiedName~CalculationStateServiceTests`.
- Exit: `0`.
- Console and TRX: `137 total / 137 executed / 137 passed / 0 failed / 0 notExecuted`.
- Result-list `NotExecuted` identities: none.

### Affected Debug

- The filter preserves the complete live Task 12 affected matrix and adds the
  current Task 12.1 lifecycle class plus the correction regression and
  calculation-state classes.
- Exit: `0`.
- Console: `351 total / 350 passed / 0 failed / 1 skipped`.
- TRX counters: `351 total / 350 executed / 350 passed / 0 failed / 0 notExecuted`.
- Result-list `NotExecuted` identity:
  `ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`.
- This is the existing accepted absent external-fixture skip; no new identity
  occurred.

### Full Release

- Command: `dotnet test "tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Release --no-build --logger "trx;LogFileName=pre-task-13-construction-thermal-full-release.trx" --results-directory "tests\SnowMeltingCalculator.Tests\TestResults"`.
- Exit: `0`.
- Console: `1724 total / 1723 passed / 0 failed / 1 skipped`.
- TRX counters: `1726 total / 1723 executed / 1723 passed / 0 failed / 0 notExecuted`.
- Result-list `NotExecuted` identities are exactly the accepted baseline set:
  `RegenerateCircuitsBaseline`, `RegenerateBaseline`, and
  `ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`.
- VSTest console and TRX aggregate counters represent explicit/ignored results
  differently, as in the earlier Task 12 receipts. Both observed forms are
  preserved; there are no failed, error, timeout, or aborted results.

## Task 3 artifact manifest

| Artifact | Bytes | SHA-256 |
|---|---:|---|
| `pre-task-13-construction-thermal-build-debug.log` | 842 | `9F629A1851BD52A3299EA75295DD716C050AF66FE6020199F0E3DFF6582919E0` |
| `pre-task-13-construction-thermal-build-release.log` | 846 | `FF78CF4C70284118C1A819B349AB8BFB933FE01B90D91CE37AEA8C664B9C702E` |
| `pre-task-13-construction-thermal-focused-release.trx` | 183831 | `CD51BD4875A232024D5BF33B322B5B790F68692A53B9D737128DEC997A46DE1C` |
| `pre-task-13-construction-thermal-focused-release.log` | 1520 | `4DE7A6E0F96AEBBEFA9ECB027C34A4C473488FFA6EA820A40B1631D659F16362` |
| `pre-task-13-construction-thermal-affected-debug.trx` | 469141 | `A8F5AE197B3882A9F00CFB5DA506D1A6B184FBD126A3499AB4387876A8F53189` |
| `pre-task-13-construction-thermal-affected-debug.log` | 1702 | `B21EA87C2571AFE5E1EFF39942552B34B1D4D1A127D35210A152B6C2DB84A28C` |
| `pre-task-13-construction-thermal-full-release.trx` | 2269506 | `165167E350F1433018A81C70DB955D57A9DFA017323F077FB58F545E719EFC1C` |
| `pre-task-13-construction-thermal-full-release.log` | 2274 | `6C8BAF70F94D35EA95453CB5B3FD5AFF93A1F4438EA2E89D5458F1CEA6C55484` |
| `pre-task-13-construction-thermal-recalculation-release.trx` | 3057 | `9FA4D8518251D0535DFF1E2E7491C8AB6FEF97C64AB5FA661EC0C9E8444EC65D` |

## Task 3 DoneClaim

- Production correction remains exactly one executable
  `_projection.RaiseDataChanged()` immediately before
  `_calculationContext?.UpdateConstruction(...)` in the existing valid
  downstream branch.
- Focused Release, affected Debug, both production builds, and full Release are
  green. TRX result identities contain no unaccepted `NotExecuted` entry.
- The staged set remains empty. The repository was already extensively dirty;
  this lane did not restore, reset, clean, stage, commit, or overwrite unrelated
  paths.
- C# LSP diagnostics could not run because the harness returned
  `LSP file path must be inside request cwd` for both changed C# paths. The
  successful Debug/Release compilation and executable tests are authoritative.
- Per the owner constraint for Todo 3, WPF was not launched and
  `docs/architecture-migration/TASK_CONTEXT.md` was not edited. Task 13 remains
  blocked pending owner manual QA and the separate Todo 4 context transition.

`AUTOMATED VERDICT: PASS`. Ready for owner manual WPF QA; no overall correction
`VERDICT: PASS` is claimed yet.

## Owner manual report

- The owner supplied the following exact wording: “все работает как надо. меняю толщину, материал, шаблон, УГВ - индикация появляется.”
- This report records the Thermal indicator appearing after each of the four named Construction changes. It does not identify an executable, configuration, build, or launch path.

### Observed manual matrix

| Construction change | Owner-reported observation | Manual result |
|---|---|---|
| Thickness | Thermal indicator appears | PASS, observed |
| Material | Thermal indicator appears | PASS, observed |
| Template | Thermal indicator appears | PASS, observed |
| Groundwater level, УГВ | Thermal indicator appears | PASS, observed |

- УГВ is the observed groundwater-level change. It is distinct from the plan's calculated-lambda/manual-override scenario; lambda/override was not reported.
- The owner did not report the indicator returning off after recalculation. No explicit recalculation-off observation was supplied.
- No explicit startup/new-project observation was supplied.
- No explicit project-load observation was supplied.
- No explicit lambda/override observation was supplied.
- No executable or configuration path was supplied.

## Manual correction status

`MANUAL QA PARTIAL`. The owner-reported positive invalidation observation covers all four listed Construction changes, thickness, material, template, and УГВ. The required observations for lambda/override, indicator-off after successful recalculation, startup/new-project state, and project-load state are absent, so this receipt does not claim overall correction `PASS` and does not claim Task 13 or Phase 3 completion.

## Owner scope decision and superseding manual acceptance

- 2026-08-19 owner decision: Todo 4 is complete for the Construction correction and the parent Final Verification Wave F1-F4 may proceed. This decision supersedes the earlier project-load criterion for this Construction correction without deleting the historical `MANUAL QA PARTIAL` record above.
- Owner-confirmed Construction observations: thickness, material, template, groundwater level (УГВ), and calculated lambda/manual override changes each cause the Thermal indicator after an existing result; successful recalculation produces the result and turns the indicator off. Startup/new-project behavior was also confirmed without a Construction correction failure.
- After loading a project, an indicator can appear, but its exact message is `Климатические данные изменились. Требуется пересчёт.`. That wording originates from the Climate path (`ThermalViewModel.OnClimateDataChanged`), not the Construction notification path. The project-load indicator is therefore classified as a separate open Climate ProjectLoad invalidation defect, is not a Construction correction failure, and is not claimed fixed or included in the Construction production change.
- Construction manual acceptance is `PASS` for the owner-approved scope. Task 13 and Phase 3 are not complete; the separate Climate defect remains open.

`VERDICT: PASS`

## Final Wave F1 plan and allow-list compliance audit

- Audited: `2026-08-19T18:26:50+05:00` against the Task 1 baseline recorded above and the exact correction allow-list.
- Production proof passes: the current source contains one `_projection.RaiseDataChanged();` call. Removing exactly that 48-byte line reconstructs the recorded production preimage exactly: `16144` bytes, SHA-256 `E6B8F40877574BD6C15E524250A7C0D1920F3A51DA77847F952C70F16BF10F08`. The call is after `_projection.Update(newSnapshot)`, inside the unchanged `_projection.IsValid && PublishesDownstream(origin)` branch, and immediately before `_calculationContext?.UpdateConstruction(...)`.
- The staged set is empty. Correction source, regression test, receipt/context, and correction-prefixed raw evidence are identifiable. No Task 13 evidence artifact exists.
- Dirty downstream consumer, DI, UI, installer, publish, maps/model/widget, schema, package, and release paths were already present when this F1 audit started and are not correction-owned artifacts. Without the retained baseline stream their individual pre-correction status cannot be proven, so F1 does not attribute them to this lane or claim that exact baseline preservation was established.
- Allow-list violation: `.omo/plans/pre-task-13-construction-thermal-invalidation-correction.md` changed after its recorded baseline blob `fc1a639c93b8ddf379711527cf6308e598af7201` to blob `40477b7761a3b8431349226ab729e310d4a965e6` by checking Todos 1-4. The source plan is not in the correction write allow-list.
- Allow-list violation: `docs/architecture-migration/plans/pre-task-13-construction-thermal-invalidation-correction.md` was absent at baseline but is now present with blob `fc1a639c93b8ddf379711527cf6308e598af7201`. This repository plan path is not in the correction write allow-list.
- Baseline/final status cannot be reconciled as an exact set: the receipt preserves only the baseline stream size/count/hash (`16999` bytes, `255` records, SHA-256 `794128...`) and no retained NUL-safe baseline artifact was found. Current porcelain has `256` records. The two out-of-scope plan changes are independently proven, and absence of lost or overwritten unrelated records cannot be proven from a hash alone.
- The project-load message `Климатические данные изменились. Требуется пересчёт.` remains a separate known open Climate ProjectLoad defect. It is not classified as a Construction scope failure and is not a reason for this F1 rejection.

`F1 VERDICT: REJECT` - the executable correction is exactly the required one-call change and Task 13 scope was not entered, but exact allow-list compliance fails on two correction-owned plan paths and the retained baseline evidence is insufficient for exact baseline/final status-set reconciliation.

## Final Wave F3 executable and manual evidence audit

- Audited: 2026-08-19 18:26 +05:00.
- Raw artifact manifest reconciled byte-for-byte by size and SHA-256: `pre-task-13-construction-thermal-build-debug.log`, `pre-task-13-construction-thermal-build-release.log`, `pre-task-13-construction-thermal-focused-release.log` / `.trx`, `pre-task-13-construction-thermal-affected-debug.log` / `.trx`, and `pre-task-13-construction-thermal-full-release.log` / `.trx` all match the Task 3 manifest above.
- Debug and Release production build logs each report successful completion with `0` warnings and `0` errors; the receipt records exit `0` for both commands. Focused Release is `137 total / 137 executed / 137 passed / 0 failed / 0 NotExecuted`. Affected Debug is `351 total / 350 executed / 350 passed / 0 failed` with exactly one result-list `NotExecuted`, the accepted absent external-fixture identity `ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`. Full Release is `1726 total / 1723 executed / 1723 passed / 0 failed` with exactly `RegenerateCircuitsBaseline`, `RegenerateBaseline`, and `ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile` as result-list `NotExecuted` identities. The console/TRX aggregate-count distinction is preserved and introduces no failure or additional skip.
- Owner manual Construction acceptance is `PASS` for thickness, material, template, groundwater level (УГВ), and calculated lambda/manual override invalidation, plus successful recalculation producing a result and turning the indicator off; startup/new-project behavior is accepted. The project-load message `Климатические данные изменились. Требуется пересчёт.` remains a separate open Climate ProjectLoad defect and does not fail this Construction-scoped audit.
- Residual evidence limitation: the UTF-16 build/test logs preserve successful command output but do not embed a separate shell exit-code marker, so exit `0` is supported by the contemporaneous receipt rather than independently encoded in each raw log. The original owner report did not name the executable, launch command, or configuration; the later explicit owner scope decision accepts the complete Construction matrix and releases F1-F4, but does not retroactively add those launch details. These provenance limitations are recorded without claiming the Climate defect fixed, Task 13 complete, or Phase 3 complete.

`F3 VERDICT: APPROVE`

## Final Wave F2 reactive contract audit

- Audited: `2026-08-19 18:25:14 +05:00` against the live `D:\IA\ace v.2` worktree.
- Canonical path: `ProjectSessionConstructionState.CompleteChanged` updates the stable `CurrentProjection`, then its valid downstream branch calls `_projection.RaiseDataChanged()` exactly once before `CalculationContext.UpdateConstruction`. Repository source has one `RaiseDataChanged()` call site; `ConstructionStateProjection.Update()` only replaces `_snapshot` and remains silent.
- Real graph: `ConstructionThermalInvalidationRegressionTests.CreateFixture` constructs one real `ProjectSession`, initializes its canonical `ConstructionState`, injects that same `CurrentProjection` into concrete `ThermalViewModel` and `ThermalValidator`, and uses concrete `CalculationStateService`, `CalculationContext`, `ThermalCalculator`, and validators. The test contains no synthetic event, replacement `ConstructionData`, private-handler call, or mocked invalidation service.
- Positive multiplicity: User `Material`, `Thickness`, and `CalculatedLambda`/override rows plus the multi-field `Template` row assert one projection notification, one context publication in `projection -> context` order, and exactly one Thermal `ModuleState.NeedsRecalculation`; all identities passed in contract TRX `136/136` and focused TRX `137/137`.
- Silent contracts: the no-result row asserts one projection/context publication but zero Thermal state changes and a false recalculation flag; `NoChange`, `Rejected`, and changed `Initialization`, `ProjectLoad`, and `Reset` rows assert zero projection/context/Thermal events while preserving the prior result. Every named identity passed in both inspected TRX artifacts.
- Cancelled is represented truthfully as currently unreachable: the source-inventory test finds no production construction of a `ConstructionMutationResult` with `ConstructionMutationStatus.Cancelled`; live source contains only the derived `IsCancelled` status reader, so no fake mutation is claimed to reach `CompleteChanged`.
- Recalculation: `SuccessfulRecalculation_AfterInvalidation_ReturnsThermalToActual` passed in the focused `137/137` TRX and isolated `1/1` TRX; its real `CalculateCommand` path proves non-null result, recalculation flag off, and exact Thermal sequence `NeedsRecalculation -> Calculating -> Actual`.
- Subscription audit: `ThermalViewModel` subscribes to `_constructionData.DataChanged += OnConstructionDataChanged`; no `CalculationContext.ContextChanged` subscription exists. `OnConstructionDataChanged` clears an existing result and invokes `SetThermalNeedsRecalculation` once; without a result it emits no Thermal state change.
- The known project-load message `Климатические данные изменились. Требуется пересчёт.` remains separate Climate-path evidence and is not counted against this Construction contract. This verdict does not claim Task 13 or Phase 3 completion.

`F2 VERDICT: APPROVE`

## Final Verification Wave F4

- Timestamp: 2026-08-19.
- Receipt status: `VERDICT: PASS` is the final Construction correction status. It does not claim that the separate Climate ProjectLoad defect is fixed.
- `TASK_CONTEXT.md` status: `Current phase = phase-3-construction-state`; `Stage = executing`; `Phase result acceptance = pending for Phase 3`; Task 13 remains not started; parent F1-F4 remain unstarted before this wave.
- Release boundary: the only next action released by this correction is parent Task 13 architecture dossier refresh. This verdict does not claim Phase 3 or Task 13 completion.
- Scope audit: no Task 13 dossier, map, state-model, widget, formula, schema, UI, package, or release artifact was changed by this correction lane. The receipt is the only file modified by this F4 documentation action; the pre-existing dirty worktree remains outside scope.
- Open defect boundary: the project-load indicator message `Климатические данные изменились. Требуется пересчёт.` remains a separate open Climate ProjectLoad invalidation defect, not a Construction correction failure.

`F4 VERDICT: APPROVE`

## Final Wave F1 superseding owner-scope reassessment

- Reassessed: `2026-08-19T18:35:32+05:00`. This section preserves the historical F1 `REJECT` above and supersedes its scope classification after the owner's explicit instruction to mark Todo 4 complete and proceed through F1-F4.
- Production scope remains exact: `ProjectSessionConstructionState.CompleteChanged(...)` contains one `_projection.RaiseDataChanged();` after `_projection.Update(newSnapshot)`, inside the unchanged `_projection.IsValid && PublishesDownstream(origin)` branch, immediately before `_calculationContext?.UpdateConstruction(...)`. Removing that one 48-byte line still reconstructs the recorded `16144`-byte production preimage with SHA-256 `E6B8F40877574BD6C15E524250A7C0D1920F3A51DA77847F952C70F16BF10F08`.
- The only correction-owned product/test paths are that one-call production correction and `tests/SnowMeltingCalculator.Tests/Services/Project/ConstructionThermalInvalidationRegressionTests.cs`. Correction receipt/context updates and correction-prefixed raw `.trx`/logs are within the evidence allow-list. No additional correction-owned product path was found.
- Workflow paths are authorized: checking Todos 1-4 in `.omo/plans/pre-task-13-construction-thermal-invalidation-correction.md` is required workflow tracking under the clarified owner instruction. `docs/architecture-migration/plans/pre-task-13-construction-thermal-invalidation-correction.md` is the byte-identical pre-implementation repository-facing plan import at blob `fc1a639c93b8ddf379711527cf6308e598af7201`; the `.omo` source differs only by the four authorized completed checkboxes and has blob `40477b7761a3b8431349226ab729e310d4a965e6`. Neither path is product scope or a Task 13 dossier refresh.
- Task 13 scope remains absent: no Task 13 evidence artifact exists, and no map, state model, widget, schema, UI, package, installer, publish, release, downstream consumer, DI, formula, or persistence path is attributed to this correction lane.
- The staged set is empty. No positive evidence of a removed path, reverted hunk, overwritten unrelated record, staged content, or other unexplained correction-owned drift was found.
- Residual evidence limitation remains: the original NUL-safe baseline stream itself was not retained, so exact unrelated-record preservation cannot be independently reconstructed from its recorded size/count/hash alone. This limits proof strength but is not positive evidence that data or a dirty hunk was lost, and it does not identify any substantive correction-scope violation.
- The known project-load message `Климатические данные изменились. Требуется пересчёт.` remains a separate open Climate ProjectLoad defect, not a Construction correction failure and not a reason to reject F1.

`F1 VERDICT: APPROVE` - under the explicit owner scope clarification, both previously rejected plan paths are authorized workflow artifacts; the one-call production requirement, regression-test boundary, empty staged set, and zero Task 13 scope hold, with no real unexplained correction-owned drift found.

## Final Wave aggregate closeout

- 2026-08-19: all four Construction correction Final Wave reviewers returned terminal approval: superseding F1 APPROVE (the historical F1 REJECT above is preserved, not erased), F2 APPROVE, F3 APPROVE, and F4 APPROVE. Correction Todos 1-4 and F1-F4 are complete. This closes only the pre-Task 13 Construction correction: Phase 3 remains `executing` with result acceptance pending, Task 13 dossier refresh is the next Construction action and is not started, and the separate Climate ProjectLoad invalidation defect (`Климатические данные изменились. Требуется пересчёт.`) remains open and is not fixed.

`FINAL WAVE AGGREGATE: F1 APPROVE | F2 APPROVE | F3 APPROVE | F4 APPROVE`
