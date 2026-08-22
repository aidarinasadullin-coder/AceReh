# Phase 3.1 Climate Thermal Invalidation on Project Load - Corrected Work Plan

## Context

### User Request Summary

Correct the blocked plan for `phase-3.1-climate-thermal-invalidation-on-project-load` without editing the repository. The narrow defect is that loading a project can publish a compatibility `ClimateData.DataChanged` event while restoring Climate state, causing `ThermalViewModel` to invalidate or clear a valid restored Thermal result.

The correction must distinguish user-command reset semantics from project-load/lifecycle reset semantics:

- `UserReset` is a user mutation origin.
- `ProjectLoadReset` is a project-load/lifecycle mutation origin.
- A changed `UserReset` publishes exactly one compatibility `DataChanged`, causes exactly one Thermal invalidation when a Thermal result exists, and marks the project dirty.
- A no-op `UserReset` produces no canonical completion/publication, no compatibility event, no Thermal invalidation, and no dirty transition.
- A changed `ProjectLoadReset` updates canonical Climate state, synchronizes the compatibility projection and `CalculationContext`, but produces zero compatibility `DataChanged`, zero Thermal invalidation, and zero dirty transition.
- The restored project load itself remains a non-user mutation and must not invalidate the Thermal result.

Phase 3 is completed and owner-accepted. Phase 3.1 is positioned after Phase 3 and before Phase 4. This corrected plan is planning authority only. It does not approve the plan, authorize implementation, start execution, or fix the defect.

Prior planning session `ses_fe1c68af3ffeqHWcXO6DjxI3ik` completed discovery but returned no usable terminal plan and is not a valid plan deliverable. Metis session `ses_fe1fff23fffefYNpPB6Ig7OEOb` correctly returned `BLOCKED` against the old plan because the shared `ClimateMutationOrigin.Reset` conflated user reset commands with lifecycle resets. Session `ses_02d0d549affeUPch83Lkox97qE` is Phase 2 provenance and must not be represented as a Phase 3.1 Momus review.

### Current Source Facts

Current inspection confirms four Climate reset call sites:

| Current call site | Current origin | Required classification | Required corrected origin |
|---|---|---|---|
| `ClimateViewModel.Reset()` | `Reset` | User-facing reset-to-defaults command | `UserReset` |
| `ClimateViewModel.ResetToCityData()` | `Reset` | User-facing reset-to-selected-city command | `UserReset` |
| `ProjectLoadOrchestrator.ResetModules()` | `Reset` | Pre-project-load lifecycle reset | `ProjectLoadReset` |
| `MainViewModel.PerformNewCalculationReset()` | `Reset` | New-project/lifecycle reset | `ProjectLoadReset` |

`ProjectLoadOrchestrator.RestoreModulesFromProjectAsync()` currently applies the saved Climate snapshot with `ClimateMutationOrigin.Load`. This remains project-load/lifecycle behavior and must produce no compatibility `DataChanged`, no Thermal invalidation, and no dirty transition.

`ProjectSessionClimateState.CompleteMutation(...)` currently applies the canonical snapshot to `ClimateData`, updates `CalculationContext`, raises canonical `Changed`, and marks dirty only for `User`. `ClimateData.ApplyProjection(...)` currently always raises `DataChanged`. The correction must retain canonical completion and projection synchronization while making compatibility publication origin-aware.

The existing characterization test `ClimateMultiplicity_Reset_EmitsOneExplicitCompatibilityUpdateWithoutDirtying` encodes the superseded ambiguous behavior. It must be corrected so a changed user reset expects one compatibility event and dirty state, while lifecycle reset tests independently expect zero compatibility events and zero dirty state.

### Decision Contract

The old ambiguous `ClimateMutationOrigin.Reset` policy is superseded. Implementation must define explicit `UserReset` and `ProjectLoadReset` origins and route every current reset call site according to the classification above.

| Origin and outcome | Canonical state mutation | Canonical `Changed` completion | Projection field copy | `CalculationContext` synchronization | Compatibility `DataChanged` | Thermal invalidation when result exists | Dirty |
|---|---:|---:|---:|---:|---:|---:|---:|
| Changed `User` | 1 | 1 | 1 | 1 | exactly 1 | exactly 1 | yes |
| No-op `User` | 0 | 0 | 0 | 0 | 0 | 0 | no |
| Changed `UserReset` | 1 | 1 | 1 | 1 | exactly 1 | exactly 1 | yes |
| No-op `UserReset` | 0 | 0 | 0 | 0 | 0 | 0 | no |
| Changed `ProjectLoadReset` | 1 | 1 | 1 | 1 | 0 | 0 | no |
| No-op `ProjectLoadReset` | 0 | 0 | 0 | 0 | 0 | 0 | no |
| Changed `Load` | 1 | 1 | 1 | 1 | 0 | 0 | no |
| No-op `Load` | 0 | 0 | 0 | 0 | 0 | 0 | no |
| Changed `Restore`, `SystemApply`, or `Initialization` | 1 | 1 | 1 | 1 | 0 | 0 | no |
| No-op or rejected mutation from any origin | 0 | 0 | 0 | 0 | 0 | 0 | no |

“Canonical `Changed` completion” means the existing canonical Climate completion event raised for an actual accepted state change. No-op or rejected mutations must not publish canonical completion.

Compatibility publication is not limited to `User`: both changed `User` and changed `UserReset` publish exactly one compatibility `DataChanged`. Project-load and lifecycle origins do not publish compatibility `DataChanged`.

Thermal invalidation remains existing subscriber behavior. The correction suppresses false lifecycle publication at the Climate mutation/projection source. It must not add subscriber guards, inspect `IsLoadProjectInProgress` in `ThermalViewModel`, or otherwise modify Thermal subscriber behavior.

### Scope

#### Exact Production Scope Ceiling

The implementation may modify only these six production files:

1. `src/Services/Project/ClimateMutationOrigin.cs`
2. `src/Services/Project/ProjectSessionClimateState.cs`
3. `src/Models/Climate/ClimateData.cs`
4. `src/Services/Project/ProjectLoadOrchestrator.cs`
5. `src/ViewModels/Climate/ClimateViewModel.cs`
6. `src/ViewModels/Shell/MainViewModel.cs`

This is a ceiling, not a required six-file write-set. Not every listed production file must change. The executor must make the smallest correct change. Any production path outside this exact ceiling requires an immediate stop, transition to `blocked`, and explicit owner amendment before further implementation.

#### Permitted Test and Evidence Scope

Focused new regression tests are permitted. Necessary updates to existing Climate characterization and integration tests are permitted, including correction of the superseded reset dirty expectation.

Expected test locations include:

- `tests/SnowMeltingCalculator.Tests/Services/Project/ClimateThermalInvalidationRegressionTests.cs`
- `tests/SnowMeltingCalculator.Tests/Services/Project/ClimateStateTests.cs`
- `tests/SnowMeltingCalculator.Tests/Climate/ClimateMultiplicityCharacterizationTests.cs`
- `tests/SnowMeltingCalculator.Tests/Climate/ClimateDataProjectionTests.cs`
- `tests/SnowMeltingCalculator.Tests/ViewModels/ResetOrchestrationTests.cs`
- `tests/SnowMeltingCalculator.Tests/ViewModels/MainViewModelTests.cs`
- `tests/SnowMeltingCalculator.Tests/ViewModels/ResultsViewModelOpenProjectTests.cs`
- Existing test helpers required to construct the real load graph

The executor may add or update only focused tests necessary to prove the contract. Test changes must not broaden production scope or replace concrete integration behavior with synthetic events.

Evidence and factual context updates are permitted after implementation gates pass:

- `docs/architecture-migration/evidence/phase-3.1-climate-thermal-invalidation-on-project-load/`
- `docs/architecture-migration/TASK_CONTEXT.md`
- The six architecture views and supporting shared architecture model only where factual behavior changed
- Generated architecture widget only when required by the migration dossier and only through the existing deterministic generation path
- Test logs and TRX files under the established evidence/results location

#### Read-Only Production References

- `src/ViewModels/Thermal/ThermalViewModel.cs`
- `src/ViewModels/Results/ResultsViewModel.cs`
- All production paths outside the six-file ceiling

`ThermalViewModel.cs` is strictly read-only. The defect must be corrected at the Climate publication source. Do not add subscriber guards, `IsLoadProjectInProgress` checks, suppression flags, direct event manipulation, or special project-load branches to `ThermalViewModel`.

#### Preserved Invariants

The implementation must preserve:

- `ProjectSession.ClimateState` as the sole writable canonical owner of project Climate values.
- `ClimateData` as a compatibility projection rather than a second writable owner.
- Copying of all projection fields:
  `SelectedCity`, `SelectedRegion`, `AirTemperature`, `ColdFiveDayTemperature`, `WindSpeed`, `Humidity`, `SnowfallIntensity`, and `Zone`.
- `CalculationContext` synchronization for every accepted changed Climate mutation, including project-load/lifecycle mutations.
- Existing canonical `Changed` completion semantics for accepted changed mutations.
- No canonical completion for no-op or rejected mutations.
- Existing `.smc` schema, versions, read/write compatibility, and project-load sequencing.
- Existing Climate formulas, city-selection behavior, high-requirements behavior, and validation.
- Existing UI/XAML, commands, navigation, DI registrations, packages, and public compatibility API unless a change is strictly necessary within the six-file ceiling.
- Existing Thermal behavior for genuine user-origin Climate changes.
- Unrelated dirty-worktree content and pre-existing changes byte-for-byte.

#### Prohibited Work

- No production file outside the exact six-file ceiling.
- No changes to `ThermalViewModel`.
- No subscriber-side suppression or `IsLoadProjectInProgress` guard.
- No second Climate owner, event bus, global suppression flag, or broad coordinator redesign.
- No `.smc` schema/version changes.
- No formula, UI, XAML, DI, package, installer, release, or unrelated architecture changes.
- No direct invocation of `ClimateData.RaiseDataChanged` from tests to simulate production behavior.
- No direct invocation of Thermal event handlers.
- No replacement of the real load graph with a self-contained fake for the primary regression.
- No manual QA requirement where executable tests can prove the behavior.
- No implementation before fresh exact-artifact reviews, owner `/architecture-approve`, and a later separate `/architecture-start`.
- No staging, commit, amend, push, reset, checkout, clean, or revert unless separately authorized.

#### Execution-Time Stop and Rollback Contract

After Task 6 authorizes execution, the executor must immediately stop all further edits and set the workflow to `blocked` if any of these conditions occurs:

- The approved behavior contract is confirmed infeasible within the current architecture.
- The exact Task 7 regression cannot produce a valid RED for the specified false Thermal invalidation.
- Any focused, affected, build, or full Release gate remains unresolved after investigation.
- A Task 7 protected preimage drifts or the final comparison finds any protected mismatch or overwritten pre-existing hunk.
- Correct implementation requires a production path outside the exact six-file ceiling.
- Continuing would require weakening, disabling, ignoring, or replacing an acceptance criterion, assertion, test scenario, or `NotExecuted` rule.

Before returning to the owner, preserve the exact failing commands, complete logs/TRX where applicable, observed versus expected behavior, current status/preimage comparison, and a factual blocker receipt. Do not improvise an alternate contract, expand scope, weaken assertions or acceptance criteria, add a silent workaround, or continue into a later task or final audit. Resume only after an explicit owner amendment or decision and any required plan/review/approval/start gates are satisfied.

Task 7 preimages define the rollback boundary. If rollback is required, restore only worker-owned Phase 3.1 changes to their captured Task 7 preimages. Never use `git reset`, `git checkout`, or `git clean`; never touch pre-existing hunks or unrelated dirty paths; and never delete diagnostic logs, RED receipts, or blocker evidence. If a safe worker-only restoration cannot be proven, leave the files untouched, record the blocker, and remain `blocked` for owner direction.

### Verification Commands

All commands run from repository root `D:\IA\ace v.2`.

The executor may refine a filter only when an actual current test identity differs, must record the exact replacement, and must not weaken scenario coverage.

#### RED Project-Load Regression

```powershell
dotnet test "tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Debug --filter "FullyQualifiedName~ClimateThermalInvalidationRegressionTests.ProjectLoad_DoesNotInvalidateRestoredThermalResult" --logger "trx;LogFileName=phase-3.1-red-project-load.trx" --results-directory "tests\SnowMeltingCalculator.Tests\TestResults"
```

Expected pre-fix result: the named test executes and fails because loading a project causes Climate compatibility publication to invalidate a non-null valid restored Thermal result, set `ThermalNeedsRecalculation`, or publish a Thermal recalculation state event. A compile failure, fixture failure, absent restored result, unexecuted test, or unrelated assertion failure is not an accepted RED.

#### Characterization and Focused Contract Gate

```powershell
dotnet test "tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Debug --filter "FullyQualifiedName~ClimateThermalInvalidationRegressionTests|FullyQualifiedName~ClimateStateTests|FullyQualifiedName~ClimateMultiplicityCharacterizationTests|FullyQualifiedName~ClimateDataProjectionTests|FullyQualifiedName~ResetOrchestrationTests|FullyQualifiedName~MainViewModelTests" --logger "trx;LogFileName=phase-3.1-focused-debug.trx" --results-directory "tests\SnowMeltingCalculator.Tests\TestResults"
```

```powershell
dotnet test "tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Release --filter "FullyQualifiedName~ClimateThermalInvalidationRegressionTests|FullyQualifiedName~ClimateStateTests|FullyQualifiedName~ClimateMultiplicityCharacterizationTests|FullyQualifiedName~ClimateDataProjectionTests|FullyQualifiedName~ResetOrchestrationTests|FullyQualifiedName~MainViewModelTests" --logger "trx;LogFileName=phase-3.1-focused-release.trx" --results-directory "tests\SnowMeltingCalculator.Tests\TestResults"
```

#### Affected Integration Gate

```powershell
dotnet test "tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Release --filter "FullyQualifiedName~Climate|FullyQualifiedName~ThermalViewModelTests|FullyQualifiedName~CalculationStateServiceTests|FullyQualifiedName~ResultsViewModelOpenProjectTests|FullyQualifiedName~ProjectLifecycleFlowCharacterizationTests|FullyQualifiedName~ResetOrchestrationTests|FullyQualifiedName~MainViewModelTests" --logger "trx;LogFileName=phase-3.1-affected-release.trx" --results-directory "tests\SnowMeltingCalculator.Tests\TestResults"
```

#### Build Gates

```powershell
dotnet build "src\SnowMeltingCalculator.csproj" -c Debug --nologo
```

```powershell
dotnet build "src\SnowMeltingCalculator.csproj" -c Release --nologo
```

#### Full Release Gate

```powershell
dotnet test "tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Release --logger "trx;LogFileName=phase-3.1-full-release.trx" --results-directory "tests\SnowMeltingCalculator.Tests\TestResults"
```

Every TRX must be reconciled for total, executed, passed, failed, skipped, and `NotExecuted` identities. No new failure or new `NotExecuted` result is acceptable. Existing accepted `NotExecuted` identities must be compared factually with the execution-time baseline and listed, not assumed.

## Task Dependency Graph

| Task | Depends On | Dependents | Reason |
|---|---|---|---|
| Task 1 | None | Task 2 | The corrected terminal artifact must exist before byte-identical import and hashing. |
| Task 2 | Task 1 | Task 3 | Reviews must target the exact imported canonical artifact and byte-identical `.omo` mirror. |
| Task 3 | Task 2 | Task 4 | Fresh Sisyphus review must assess the exact imported SHA before Momus. |
| Task 4 | Task 3 | Task 5 | Fresh Momus review must assess the same exact SHA after Sisyphus passes. |
| Task 5 | Task 4 | Task 6 | Owner plan approval is allowed only after both fresh planning reviews approve the exact artifact. |
| Task 6 | Task 5 | Task 7 | Implementation may begin only after a later, separate `/architecture-start` authorization. |
| Task 7 | Task 6 | Task 8 | Dirty-worktree protection and executable RED characterization must precede production edits. |
| Task 8 | Task 7 | Task 9 | The minimal origin/publication implementation may begin only after the expected RED is proven. |
| Task 9 | Task 8 | Task 10 | Call-site routing and behavior tests depend on the explicit origin contract and publication mechanism. |
| Task 10 | Task 9 | Task 11 | Affected and full gates are meaningful only after focused behavior is green. |
| Task 11 | Task 10 | F1-F4 | Evidence and architecture context must describe verified behavior and include final protected-worktree comparison. |
| F1 | Task 11 | Task 12 | Compliance can be audited only against the completed implementation/evidence write-set. |
| F2 | Task 11 | Task 12 | Code quality can be audited only after the production diff is final. |
| F3 | Task 11 | Task 12 | Independent QA can run only after all planned executable gates exist and pass. |
| F4 | Task 11 | Task 12 | Scope and dirty-worktree fidelity require final artifacts and preimage comparison. |
| Task 12 | F1-F4 | None | Owner acceptance is allowed only after all four final reviews return APPROVE on the stable final write-set. |

Tasks 1-6 are planning, review, and owner-control gates. They grant no implementation authority except that Task 6, when separately invoked by the owner after Task 5, authorizes Task 7 to begin. Tasks 7-11, F1-F4, and Task 12 remain unstarted in this plan.

## Parallel Execution Graph

The implementation lane is intentionally sequential. Climate canonical state, publication policy, call-site routing, tests, and evidence must not be edited concurrently.

```text
Wave 1:
└── Task 1: Produce corrected terminal plan artifact

Wave 2:
└── Task 2: Import byte-identically, mirror, hash, and validate control-plane state

Wave 3:
└── Task 3: Fresh exact-SHA Sisyphus review

Wave 4:
└── Task 4: Fresh exact-SHA Momus review

Wave 5:
└── Task 5: Owner /architecture-approve checkpoint

Wave 6:
└── Task 6: Separate owner /architecture-start checkpoint

Wave 7:
└── Task 7: Protected baseline and RED characterization

Wave 8:
└── Task 8: Implement explicit origin/publication contract

Wave 9:
└── Task 9: Route all reset call sites and complete focused behavior tests

Wave 10:
└── Task 10: Build, affected integration, and full Release gates

Wave 11:
└── Task 11: Evidence, six-view context, and final protected comparison

Wave 12, after Task 11:
├── F1: Plan-compliance audit
├── F2: Code-quality and contract audit
├── F3: Independent executable QA audit
└── F4: Scope, dirty-worktree, and migration-fidelity audit

Wave 13, after F1-F4 all return APPROVE:
└── Task 12: Owner result acceptance checkpoint
```

Only F1-F4 may execute in parallel because they are read-only final audits over a stable implementation. If any final audit requests edits, all final audits are invalidated for acceptance and must be rerun against the corrected final write-set.

Critical path:

```text
Task 1 -> Task 2 -> Task 3 -> Task 4 -> Task 5 -> Task 6
-> Task 7 -> Task 8 -> Task 9 -> Task 10 -> Task 11 -> F1-F4 -> Task 12
```

Estimated parallel speedup is intentionally negligible before the final wave. Correctness and protected dirty-worktree safety take precedence over speculative concurrency.

## Tasks

### Task 1: Establish the Corrected Primary Plan Artifact

**Description**: Use this complete terminal Markdown as the recovery primary Plan Mode authority. Do not claim that session `ses_fe1c68af3ffeqHWcXO6DjxI3ik` supplied a valid deliverable. Confirm that the plan contains the explicit `UserReset` and `ProjectLoadReset` contract, exact six-file production ceiling, call-site classification, sequential characterization-first TDD tasks, fresh review gates, separate owner gates, F1-F4, and no implementation authorization.

**Delegation Recommendation**:
- Category: `writing` - this is a decision-complete architecture-plan artifact, not implementation.
- Skills: [`ulw-plan`] - ensures the imported artifact remains decision-complete and execution-ready without reopening settled owner choices.

**Skills Evaluation**:
- INCLUDED `ulw-plan`: directly applicable to producing the decision-complete plan.
- OMITTED `agent-browser`, `playwright`, `frontend`, `visual-qa`, `ultimate-browsing`: no browser or UI work.
- OMITTED `ast-grep`, `programming`, `refactor`, `simplify`, `remove-ai-slops`, `debugging`, `lsp-setup`: no source edit or runtime debugging in this task.
- OMITTED `cartography`, `codemap`, `init-deep`: the repository and migration dossier are already mapped.
- OMITTED `clonedeps`: no dependency internals are involved.
- OMITTED `coding-agent-sessions`: the relevant prior-session facts are already supplied and no transcript reconstruction is required.
- OMITTED `customize-opencode`: this is not OpenCode configuration.
- OMITTED `deepwork`, `start-work`, `worktrees`: implementation is prohibited.
- OMITTED `git-master`: no git operation is authorized.
- OMITTED `reflect`: no workflow retrospective is requested.
- OMITTED `release-smoke-test`: unrelated product/release domain.
- OMITTED `review-work`: formal reviews occur in Tasks 3 and 4.
- OMITTED `security-research`, `security-review`, `ulw-research`: no security or broad research request.

**Depends On**: None.

**Acceptance Criteria**:
- Complete Markdown exists from title through success criteria.
- It is internally consistent with all owner decisions.
- It does not imply approval, execution authorization, or implementation completion.
- It explicitly rejects the old shared `Reset` publication policy.
- It does not cite Phase 2 Momus provenance as Phase 3.1 review evidence.

### Task 2: Import, Mirror, Hash, and Validate the Exact Plan

**Description**: After the planning-only caller is separately authorized to import artifacts, write this terminal plan byte-for-byte to:

- `docs/architecture-migration/plans/phase-3.1-climate-thermal-invalidation-on-project-load.md`
- `.omo/plans/phase-3.1-climate-thermal-invalidation-on-project-load.md`

Verify byte identity, compute SHA-256, and record the exact hash as the candidate review identity. Update control-plane planning state factually to identify the artifact as corrected and awaiting fresh reviews. Do not modify production code or tests.

**Delegation Recommendation**:
- Category: `quick` - bounded two-path byte-identical import and hash verification.
- Skills: [`git-master`] - only if status/diff inspection is needed; no stage, commit, or history mutation is allowed.

**Skills Evaluation**:
- INCLUDED `git-master`: supports non-destructive status/diff inspection while respecting the dirty worktree.
- OMITTED `programming`, `ast-grep`, `refactor`, `simplify`, `remove-ai-slops`, `debugging`, `lsp-setup`: no production source work.
- OMITTED `ulw-plan`: Task 1 already finalized plan content; Task 2 must not rewrite it.
- OMITTED all browser/UI skills: no browser or visual work.
- OMITTED `cartography`, `codemap`, `init-deep`, `clonedeps`, `coding-agent-sessions`, `customize-opencode`, `deepwork`, `reflect`, `release-smoke-test`, `review-work`, `security-research`, `security-review`, `start-work`, `ultimate-browsing`, `ulw-research`, `worktrees`: domains do not overlap or would imply implementation.

**Depends On**: Task 1.

**Acceptance Criteria**:
- Canonical and `.omo` paths are byte-identical.
- Both paths have one identical SHA-256.
- The imported bytes match this terminal artifact exactly.
- No production/test file changes occur.
- Workflow remains planning-only and does not advance past review gates.
- Any import discrepancy blocks reviews until corrected and rehashed.

### Task 3: Run a Fresh Exact-SHA Sisyphus Planning Review

**Description**: Run a fresh Sisyphus planning review against the exact imported canonical bytes and SHA from Task 2. Review narrow scope, explicit origin semantics, call-site classification, TDD sequencing, executable QA, protected dirty-worktree handling, and owner gates. This review must not reuse or infer approval from prior sessions.

**Delegation Recommendation**:
- Category: `unspecified-high` - requires rigorous architecture-plan and executable-contract review.
- Skills: [`review-work`] - use its review discipline conceptually for plan compliance, without launching implementation QA or changing files.

**Skills Evaluation**:
- INCLUDED `review-work`: relevant to structured goal/constraint and quality verification.
- OMITTED `programming`, `debugging`, `refactor`, `simplify`, `remove-ai-slops`: no implementation exists to edit.
- OMITTED `git-master`: only exact read-only hash/status confirmation is needed and may be performed by the orchestrator.
- OMITTED `ulw-plan`: the artifact is under review, not being replanned unless findings require return to Task 1.
- OMITTED all remaining skills: no browser, UI, security, dependency, session-reconstruction, release, configuration, codemap, or execution work.

**Depends On**: Task 2.

**Acceptance Criteria**:
- Review names the exact canonical path, byte count, and SHA-256.
- Review is fresh and specific to Phase 3.1.
- Verdict is PASS with no unresolved finding.
- Any finding returns workflow to plan correction and requires a new import, SHA, and fresh review.
- PASS does not authorize owner approval or implementation.

### Task 4: Run a Fresh Exact-SHA Momus Planning Review

**Description**: After Task 3 passes, run a fresh Momus review against the identical canonical bytes and SHA. Do not cite `ses_02d0d549affeUPch83Lkox97qE` as Phase 3.1 provenance. Require terminal `[OKAY]` or the repository’s equivalent unambiguous approval verdict.

**Delegation Recommendation**:
- Category: `ultrabrain` - this is the final adversarial consistency review of a subtle origin/publication contract.
- Skills: [`review-work`] - supports constraint verification and risk-focused review.

**Skills Evaluation**:
- INCLUDED `review-work`: applicable to final plan consistency and risk review.
- OMITTED `coding-agent-sessions`: provenance is already settled; this task creates fresh provenance rather than reconstructing old sessions.
- OMITTED `ulw-plan`: use only if Momus returns findings and the plan must return to Task 1.
- OMITTED all implementation, browser, security, repository-mapping, dependency, release, and execution skills because this is a read-only plan review.

**Depends On**: Task 3.

**Acceptance Criteria**:
- Review targets the same path, bytes, and SHA as Task 3.
- Reviewer identity and session ID are captured factually.
- Terminal verdict is `[OKAY]` or equivalent with no unresolved finding.
- A rejection returns to correction; reviews must restart against the replacement SHA.
- The workflow stops at `awaiting-owner-approval` only after Tasks 3 and 4 both approve the exact artifact.

### Task 5: Stop for Exact-SHA Owner Plan Approval

**Description**: Present the exact reviewed SHA, Sisyphus PASS, and fresh Momus approval to the owner. Wait for explicit `/architecture-approve phase-3.1-climate-thermal-invalidation-on-project-load` or the repository’s exact approval invocation. Approval changes the plan state to `approved` only.

**Delegation Recommendation**:
- Category: `quick` - bounded control-plane owner checkpoint.
- Skills: [] - this is an owner decision, not delegated engineering.

**Skills Evaluation**:
- OMITTED every skill: no agent may substitute for owner approval.

**Depends On**: Task 4.

**Acceptance Criteria**:
- Owner approval references the exact reviewed SHA.
- Workflow becomes `approved`.
- No production, test, or evidence implementation begins.
- Approval is explicitly recorded as plan approval, not execution authorization.

### Task 6: Stop for Separate Owner Execution Authorization

**Description**: In a later separate owner action, require `/architecture-start phase-3.1-climate-thermal-invalidation-on-project-load`. Revalidate the exact approved SHA, fresh review provenance, current phase, dirty-worktree boundary, and `Stage = approved` before transitioning to `executing`.

**Delegation Recommendation**:
- Category: `quick` - bounded control-plane authorization check.
- Skills: [`start-work`, `git-master`] - only after the owner explicitly invokes the start gate, to enforce plan-bound execution and read-only worktree verification.

**Skills Evaluation**:
- INCLUDED `start-work`: applicable only after explicit owner execution authorization.
- INCLUDED `git-master`: read-only root, status, and diff validation at the start boundary.
- OMITTED `worktrees`: shared dirty-worktree policy is authoritative unless the owner separately changes it.
- OMITTED all other skills: implementation skills belong to later tasks and must not be preloaded before authorization.

**Depends On**: Task 5.

**Acceptance Criteria**:
- `/architecture-start` is a separate owner action after approval.
- The approved plan SHA is unchanged and reviews still apply to it.
- Protected dirty-worktree capture begins only after authorization.
- Any SHA drift, missing review, wrong stage, or conflicting dirty change blocks execution.
- No implementation is authorized by this plan response itself.

### Task 7: Capture the Protected Boundary and Write RED Characterization Tests

**Description**: After Task 6 only, capture the execution-time repository root, HEAD, branch/upstream, and binary-safe dirty-worktree manifest. Record SHA-256/preimages for every existing path that may be touched, including all dirty files within the six-file ceiling and existing test/evidence paths. Then add or update characterization tests before production edits.

Characterization must prove:

- The real project-load graph currently falsely invalidates a valid restored Thermal result.
- Changed user reset-to-defaults is a user action.
- Changed reset-to-city-data is a user action.
- Pre-project-load reset is lifecycle behavior.
- New-calculation reset is lifecycle behavior.
- No-op user reset has zero completion/publication/invalidation/dirty.
- Changed user reset has one canonical completion, one projection/context synchronization, exactly one compatibility event, exactly one Thermal invalidation when a result exists, and dirty.
- Changed project-load reset has one canonical completion, one projection/context synchronization, zero compatibility events, zero Thermal invalidation, and zero dirty.
- Repeated reset/load paths do not multiply subscriptions or events.
- Project load without a saved Thermal result retains existing calculate-or-restore behavior without introducing a false extra invalidation.

Run the exact RED command before any production edit. If valid RED cannot be obtained, apply the execution-time stop and rollback contract; do not proceed to Task 8.

**Delegation Recommendation**:
- Category: `deep` - requires one coherent deliverable: executable RED characterization over a concrete WPF/MVVM dependency graph.
- Skills: [`programming`, `debugging`, `git-master`] - C# TDD, root-cause-quality RED validation, and dirty-worktree protection.

**Skills Evaluation**:
- INCLUDED `programming`: mandatory for C# test work and TDD.
- INCLUDED `debugging`: applicable because the test must reproduce the real runtime defect and distinguish accepted RED from fixture failure.
- INCLUDED `git-master`: non-destructive protected-status and preimage inspection.
- OMITTED `ast-grep`: textual/current call-site inventory is small and already known; use only if a structural inventory becomes necessary.
- OMITTED `refactor`, `simplify`, `remove-ai-slops`: characterization precedes implementation cleanup.
- OMITTED browser/UI skills: executable integration tests, not manual UI QA, are authoritative.
- OMITTED `deepwork`, `start-work`, `worktrees`: orchestration/start gate already occurred; one sequential lane is required.
- OMITTED all other skills: unrelated domains.

**Depends On**: Task 6.

**Acceptance Criteria**:
- Protected baseline is binary-safe and includes pre-existing dirty/untracked paths.
- Production files remain unchanged during RED.
- The named project-load test executes and fails only on false Thermal invalidation.
- Characterization tests express the owner-decided reset contract, including corrected dirty semantics.
- The superseded “reset without dirty” expectation is explicitly replaced for user reset and retained only for lifecycle reset.
- Any failure to obtain valid RED triggers the execution-time stop and rollback contract and blocks Task 8.

### Task 8: Implement the Explicit Origin and Publication Contract

**Description**: Make the smallest production change inside the six-file ceiling to define `UserReset` and `ProjectLoadReset` and apply origin-aware compatibility publication.

Expected responsibilities:

- `ClimateMutationOrigin.cs` defines distinct `UserReset` and `ProjectLoadReset`; the ambiguous old `Reset` must not remain as the policy-bearing origin. Remove or replace it where needed rather than retaining ambiguous behavior.
- `ProjectSessionClimateState.cs` remains the authoritative completion boundary. It decides dirty semantics and whether compatibility notification is permitted by origin.
- `ClimateData.cs` continues copying all eight projection fields. It gains the minimum explicit mechanism needed to synchronize fields without necessarily raising `DataChanged`.
- Changed `User` and changed `UserReset` publish exactly one compatibility event.
- Changed `User` and changed `UserReset` mark dirty.
- Changed `Load`, `ProjectLoadReset`, `Restore`, `SystemApply`, and `Initialization` synchronize canonical/projection/context state but publish no compatibility event and do not dirty.
- No-op/rejected paths publish nothing and do not dirty.

Do not add a Thermal guard or rely on `IsLoadProjectInProgress`.

**Delegation Recommendation**:
- Category: `deep` - one goal and deliverable: implement the minimal origin-aware Climate completion boundary.
- Skills: [`programming`, `debugging`] - strict C# change with RED-to-GREEN validation.

**Skills Evaluation**:
- INCLUDED `programming`: mandatory for C# implementation and tests.
- INCLUDED `debugging`: ensures the minimal fix addresses the proven source publication path.
- OMITTED `refactor`, `simplify`, `remove-ai-slops`: broad cleanup is outside scope.
- OMITTED `ast-grep`: use only if exact structural caller inventory cannot otherwise be completed.
- OMITTED `git-master`: no git mutation; protected comparison is performed in Tasks 7 and 11.
- OMITTED all browser/UI, security, architecture-mapping, dependency, release, and orchestration skills.

**Depends On**: Task 7.

**Acceptance Criteria**:
- RED project-load regression turns GREEN.
- All eight projection fields remain synchronized.
- `CalculationContext` remains synchronized for changed lifecycle mutations.
- Canonical `Changed` remains exactly once for accepted changed mutations.
- Changed `UserReset` publishes exactly one compatibility event and dirties.
- Changed `ProjectLoadReset` publishes zero compatibility events and does not dirty.
- No-op `UserReset` produces zero completion, projection/context synchronization, compatibility event, Thermal invalidation, and dirty transition.
- No production path outside the six-file ceiling changes.
- `ThermalViewModel.cs` remains byte-identical to its protected preimage.

### Task 9: Route Every Reset Call Site and Complete Focused Behavior Tests

**Description**: Route all current reset call sites according to the approved classification and lock their behavior with focused tests.

Required routing:

- `ClimateViewModel.Reset()` -> `UserReset`.
- `ClimateViewModel.ResetToCityData()` -> `UserReset`.
- `ProjectLoadOrchestrator.ResetModules()` -> `ProjectLoadReset`.
- `MainViewModel.PerformNewCalculationReset()` -> `ProjectLoadReset`.
- `ProjectLoadOrchestrator.RestoreModulesFromProjectAsync()` remains `Load` unless current implementation requires an equivalently explicit project-load origin within the approved enum contract; it must remain non-user and silent at the compatibility event boundary.

Tests must exercise public application entrypoints where available. Direct state tests supplement but do not replace concrete graph tests.

Run the focused Debug and Release commands.

**Delegation Recommendation**:
- Category: `deep` - one deliverable: complete routing plus focused behavior proof across all classified call sites.
- Skills: [`programming`, `debugging`] - C# changes and exact event/dirty/invalidation assertions.

**Skills Evaluation**:
- INCLUDED `programming`: mandatory for C# production/test work.
- INCLUDED `debugging`: relevant to exact event multiplicity and lifecycle behavior.
- OMITTED `refactor`, `simplify`, `remove-ai-slops`: no broad cleanup.
- OMITTED browser/UI and `visual-qa`: tests can prove behavior without manual UI checks.
- OMITTED `git-master`: no stage/commit operation; final comparison occurs later.
- OMITTED all remaining skills: unrelated.

**Depends On**: Task 8.

**Acceptance Criteria**:
- Every current reset call site is classified and routed exactly as specified.
- Changed user reset-to-defaults and reset-to-city-data each produce exactly one compatibility event, one Thermal invalidation when a result exists, and dirty.
- No-op user reset produces no canonical completion/publication, compatibility event, Thermal invalidation, or dirty.
- Pre-load and new-calculation lifecycle resets synchronize state while producing zero compatibility event, zero Thermal invalidation, and zero dirty.
- Project load preserves a valid restored Thermal result and leaves `ThermalNeedsRecalculation == false`.
- A genuine user Climate edit after load still invalidates exactly once and dirties.
- Repeated loads/resets do not duplicate subscriptions or events.
- Focused Debug and Release gates pass with zero failures and no unexpected `NotExecuted`.

### Task 10: Run Build, Affected Integration, and Full Release Gates

**Description**: Run both production builds, the affected Release integration filter, and the full Release suite. Reconcile every TRX counter and every `NotExecuted` identity. Do not substitute manual QA for executable tests.

**Delegation Recommendation**:
- Category: `unspecified-high` - high-effort execution and reconciliation of multiple .NET gates.
- Skills: [`programming`, `debugging`] - interpret compiler/test failures without weakening assertions.

**Skills Evaluation**:
- INCLUDED `programming`: required for .NET build/test gate discipline.
- INCLUDED `debugging`: required if an affected regression appears.
- OMITTED `release-smoke-test`: this is not an oh-my-opencode-slim release artifact.
- OMITTED browser/UI skills: executable tests are sufficient for this behavioral defect.
- OMITTED `git-master`: no git mutation is authorized.
- OMITTED all remaining skills: unrelated.

**Depends On**: Task 9.

**Acceptance Criteria**:
- Debug build exits `0` with zero warnings and zero errors.
- Release build exits `0` with zero warnings and zero errors.
- Focused and affected tests have zero failures.
- Full Release suite has zero new failures and zero new `NotExecuted`.
- Existing accepted `NotExecuted` identities are listed and reconciled against the execution-time baseline.
- No test is disabled, weakened, ignored, or replaced with manual-only evidence.
- Any unresolved failure triggers the execution-time stop and rollback contract and blocks evidence finalization.

### Task 11: Update Factual Evidence and Compare Protected Preimages

**Description**: After all automated gates pass, create a factual Phase 3.1 evidence receipt. Update migration context and the affected architecture views to record explicit reset origins, canonical completion, compatibility projection behavior, reactive invalidation, persistence compatibility, and user-flow results. Regenerate the widget only through the existing deterministic path if the dossier requires it.

At minimum, review and factually update the six separate architecture views where behavior changed:

1. Compile-time.
2. DI/runtime.
3. State ownership.
4. Reactive behavior.
5. Persistence.
6. User flow.

Do not collapse the views into one graph. Do not invent changes in views that remain unaffected; record “verified unchanged” where appropriate.

Perform binary-safe final status and preimage comparison against Task 7. Distinguish worker-owned deltas from all pre-existing dirty content.

Any protected mismatch, overwritten pre-existing hunk, outside-ceiling production path, or unsupported claim triggers the execution-time stop and rollback contract before F1-F4.

**Delegation Recommendation**:
- Category: `writing` - evidence and architecture-context synchronization after verified implementation.
- Skills: [`cartography`, `git-master`] - maintain the six architecture views and perform non-destructive protected-worktree comparison.

**Skills Evaluation**:
- INCLUDED `cartography`: directly relevant to factual updates across the six established architecture views.
- INCLUDED `git-master`: read-only status/diff/preimage comparison.
- OMITTED `codemap`, `init-deep`: no new repository-wide mapping is needed.
- OMITTED `programming`, `debugging`: implementation and executable verification are already complete.
- OMITTED `frontend`, `visual-qa`, `playwright`, `agent-browser`: widget regeneration is deterministic documentation work; no UI redesign is authorized.
- OMITTED all remaining skills: unrelated.

**Depends On**: Task 10.

**Acceptance Criteria**:
- Evidence lists exact commands, exit codes, TRX counters, and artifact paths.
- Context records `UserReset` and `ProjectLoadReset` semantics without rewriting history silently.
- Canonical Climate ownership and compatibility projection roles remain accurate.
- `.smc`, formulas, UI, DI, and unrelated architecture are recorded as preserved.
- Worker-owned production paths are a subset of the exact six-file ceiling.
- Not every production file is required to change.
- Every pre-existing protected path and unrelated hunk matches its captured preimage.
- Any outside-ceiling production path, overwritten dirty hunk, or unsupported claim blocks final verification.

### F1: Final Plan Compliance Audit

**Description**: Audit the final implementation and evidence against the exact reviewed plan SHA. Verify the six-file production ceiling, all call-site classifications, characterization-first RED-to-GREEN chain, owner gates, and artifact coverage.

**Delegation Recommendation**:
- Category: `unspecified-high` - independent compliance review.
- Skills: [`review-work`] - structured goal and constraint verification.

**Skills Evaluation**:
- INCLUDED `review-work`: directly applicable to final compliance.
- OMITTED implementation skills: F1 is read-only.
- OMITTED all unrelated browser, security, dependency, release, mapping, and execution skills.

**Depends On**: Task 11.

**Acceptance Criteria**:
- Terminal verdict is `APPROVE`.
- No scope drift, missing criterion, or unsupported waiver exists.
- F1 remains unchecked until executed after implementation.

### F2: Final Code Quality and Contract Audit

**Description**: Inspect the final production diff and caller inventory. Confirm minimal origin-aware publication, preserved projection copying/context synchronization, exact no-op behavior, no duplicate canonical owner, and no subscriber-side suppression.

**Delegation Recommendation**:
- Category: `ultrabrain` - subtle event, origin, and state-ownership audit.
- Skills: [`review-work`, `programming`] - code-quality review with C# contract awareness.

**Skills Evaluation**:
- INCLUDED `review-work`: independent quality review.
- INCLUDED `programming`: necessary to evaluate C# state/event semantics.
- OMITTED `refactor`, `simplify`, `remove-ai-slops`: F2 must report findings, not broaden edits.
- OMITTED all unrelated skills.

**Depends On**: Task 11.

**Acceptance Criteria**:
- Terminal verdict is `APPROVE`.
- `ThermalViewModel.cs` is unchanged.
- No `IsLoadProjectInProgress` or subscriber guard is introduced.
- F2 remains unchecked until executed after implementation.

### F3: Final Independent Executable QA Audit

**Description**: Independently rerun the exact focused, affected, build, and full Release commands. Verify the RED-to-GREEN receipt, real project-load graph, restored-result preservation, user-reset invalidation, lifecycle silence, repeated-operation multiplicity, and TRX reconciliation.

**Delegation Recommendation**:
- Category: `unspecified-high` - independent hands-on QA execution.
- Skills: [`programming`, `debugging`] - run and interpret .NET test/build behavior.

**Skills Evaluation**:
- INCLUDED `programming`: required for .NET verification.
- INCLUDED `debugging`: required to distinguish genuine failures from harness defects.
- OMITTED browser/UI skills: executable integration coverage is authoritative.
- OMITTED all unrelated skills.

**Depends On**: Task 11.

**Acceptance Criteria**:
- Terminal verdict is `APPROVE`.
- All commands and counters agree with evidence.
- No misleading success output or unexpected `NotExecuted` exists.
- F3 remains unchecked until executed after implementation.

### F4: Final Scope, Dirty-Worktree, and Migration-Fidelity Audit

**Description**: Compare final status and preimages against the protected baseline. Verify that only approved worker-owned paths changed, all unrelated dirty content is preserved, evidence is factual, and the six architecture views remain coherent.

**Delegation Recommendation**:
- Category: `unspecified-high` - independent scope and provenance audit.
- Skills: [`git-master`, `cartography`, `review-work`] - protected diff analysis, six-view fidelity, and final constraint review.

**Skills Evaluation**:
- INCLUDED `git-master`: non-destructive final path/preimage comparison.
- INCLUDED `cartography`: architecture-view fidelity.
- INCLUDED `review-work`: final constraint verification.
- OMITTED all implementation, browser, security, dependency, and release skills.

**Depends On**: Task 11.

**Acceptance Criteria**:
- Terminal verdict is `APPROVE`.
- Protected preimages and unrelated hunks are unchanged.
- Production write-set is a subset of the exact six-file ceiling.
- Evidence and context do not overclaim approval, authorization, or completion.
- F4 remains unchecked until executed after implementation.

### Task 12: Owner Result Acceptance Checkpoint

**Description**: After F1, F2, F3, and F4 each return `APPROVE` on the stable final write-set, stop for an explicit owner result decision. The owner must inspect the four review receipts and the technical evidence, then explicitly accept or reject the Phase 3.1 implementation result. No agent may infer acceptance from technical verification or from four approving reviews.

**Delegation Recommendation**:
- Category: `quick` - bounded owner result checkpoint after the final review wave.
- Skills: [] - this is an owner decision and cannot be delegated.

**Skills Evaluation**:
- OMITTED every skill: no agent may substitute for explicit owner result acceptance, and this checkpoint does not authorize additional implementation, plan mutation, or Phase 4 execution.

**Depends On**: F1, F2, F3, and F4 all returning `APPROVE` on the stable final write-set.

**Acceptance Criteria**:
- The owner explicitly inspects the F1-F4 receipts and technical evidence and explicitly accepts or rejects the Phase 3.1 implementation result.
- After F1-F4 `APPROVE`, workflow is updated only to `awaiting-owner-acceptance`.
- Only an explicit owner acceptance may move Phase 3.1 to `completed` and unblock Phase 4 planning.
- If the owner is absent, workflow remains `awaiting-owner-acceptance`; if the owner rejects, workflow transitions to `blocked` with the requested corrections. Phase 4 remains blocked in either case.
- Technical F1-F4 approval alone is insufficient for final completion.
- Task 12 does not authorize additional implementation, plan mutation, or Phase 4 execution.
- Task 12 remains unchecked until the owner provides the explicit result decision.

## Commit Strategy

No commit, staging, push, amend, rebase, branch operation, reset, checkout, clean, or revert is authorized by this plan.

If the owner later separately requests a commit after Tasks 7-11 and F1-F4 all approve:

1. Reinspect `git status`, the complete intended diff, and recent repository commit style.
2. Stage only worker-owned Phase 3.1 paths, never unrelated pre-existing dirty content.
3. Prefer one atomic implementation commit containing the explicit reset-origin/publication correction, focused regression tests, and factual migration evidence because they form one inseparable behavioral change.
4. If repository policy requires evidence to remain separate, use at most two atomic commits:
   `fix: distinguish climate user and lifecycle resets`, followed by
   `docs: record phase 3.1 verification evidence`.
5. Do not commit unless the owner explicitly authorizes it after reviewing the final write-set.
6. Never push unless separately and explicitly authorized.

## Success Criteria

- The corrected plan is imported byte-identically to canonical and `.omo` mirror paths and bound to one exact SHA-256.
- Fresh Sisyphus and fresh Momus reviews approve that exact artifact.
- Workflow stops at `awaiting-owner-approval` after successful reviews.
- A separate owner `/architecture-approve` approves only the exact reviewed plan SHA.
- A later separate owner `/architecture-start phase-3.1-climate-thermal-invalidation-on-project-load` is required before any implementation.
- The old ambiguous `ClimateMutationOrigin.Reset` publication policy is replaced by explicit `UserReset` and `ProjectLoadReset` semantics.
- `ClimateViewModel.Reset()` and `ResetToCityData()` are classified as `UserReset`.
- `ProjectLoadOrchestrator.ResetModules()` and `MainViewModel.PerformNewCalculationReset()` are classified as `ProjectLoadReset`.
- Changed `UserReset` produces exactly one canonical completion, one compatibility projection/context synchronization, exactly one compatibility `DataChanged`, exactly one Thermal invalidation when a result exists, and dirty state.
- No-op `UserReset` produces no canonical completion/publication, no compatibility event, no Thermal invalidation, and no dirty transition.
- Changed `ProjectLoadReset` produces canonical completion and projection/context synchronization, but zero compatibility `DataChanged`, zero Thermal invalidation, and zero dirty transition.
- Loading a project with a valid saved Thermal result preserves the restored result and leaves `ThermalNeedsRecalculation == false`.
- A genuine user Climate edit after load still clears or invalidates the Thermal result exactly once and marks the project dirty.
- All eight Climate projection fields and `CalculationContext` remain synchronized.
- Canonical Climate ownership, `.smc` compatibility, formulas, UI, DI, and unrelated architecture remain unchanged.
- `src/ViewModels/Thermal/ThermalViewModel.cs` remains read-only and unchanged.
- The production write-set is a subset of the exact six-file ceiling; not all six files are required to change.
- Any required production path outside the ceiling causes immediate stop and owner amendment.
- Characterization-first RED-to-GREEN evidence, focused tests, affected integration tests, Debug/Release builds, and full Release tests pass with exact TRX reconciliation.
- Protected dirty-worktree preimages and unrelated changes remain intact.
- Confirmed infeasibility, invalid RED, unresolved test/build failure, protected drift or preimage mismatch, required production scope expansion, or required acceptance/assertion weakening immediately stops edits, sets the workflow to `blocked`, preserves exact blocker evidence, and returns the decision to the owner.
- Any required rollback restores only worker-owned Phase 3.1 changes to Task 7 captured preimages without `git reset`, `git checkout`, `git clean`, modification of pre-existing hunks, or deletion of diagnostic/blocker evidence.
- F1-F4 each return `APPROVE`; none is pre-marked complete.
- Task 12 is a separate final checkpoint after all F1-F4 approvals: the owner must explicitly accept the Phase 3.1 implementation result after inspecting the review receipts and technical evidence. Technical F1-F4 approval alone is insufficient; only that explicit acceptance may move Phase 3.1 to `completed` and unblock Phase 4 planning, while absent or rejected owner action keeps Phase 4 blocked.
- No commit, staging, or push occurs without separate owner authorization.
- Phase 4 remains blocked until Phase 3.1 technical verification and separate owner result acceptance are complete.
