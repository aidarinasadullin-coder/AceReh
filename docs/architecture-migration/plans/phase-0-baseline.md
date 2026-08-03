# phase-0-baseline - Work Plan

## TL;DR (For humans)

**What you'll get:** A reproducible, evidence-backed baseline of the application's current architecture, behavior, state ownership, and project-file compatibility. It will provide one shared architecture model, six consistent views, a complete state inventory, documented test gaps, target constraints, and a specification for the future architecture widget.

**Why this approach:** The existing audit belongs to another working directory and the current repository already contains user changes. The plan therefore freezes the actual execution-time workspace first, treats historical material as hypotheses, and derives every architectural statement from current source, tests, commands, or explicitly degraded evidence.

**What it will NOT do:** It will not implement `ProjectSession`, alter product behavior, add tests, change `.smc` files, redesign the UI, update the existing widget, install tools, fix failures, or touch unrelated user changes.

**Effort:** XL
**Risk:** Medium - the phase is documentation-only, but completeness, dirty-worktree preservation, and cross-artifact consistency require strict controls.
**Decisions to sanity-check:** Legacy compatibility duration, transactional restore, `CalculationContext` disposition, future skill placement, C# LSP installation, and widget implementation remain deferred. Structural model validation may be marked `degraded`, but JSON parsing and deterministic integrity checks must still pass.

Your next move: execute this plan only in a separate worker session after all project-local review and approval gates are satisfied.

---

> TL;DR (machine): XL documentation-only baseline; medium risk; produces reproducible receipts, reconciliation, a canonical architecture model, six views, inventories, invariants, and a widget specification without product/test changes.

## Scope

### Must have

- Capture an execution-time workspace identity: canonical Git root, HEAD, branch/upstream, SDK/runtime, exact dirty path set, and path-level hashes sufficient to prove unrelated changes were preserved.
- Record every evidence receipt with `phase`, `snapshot_sha`, `source_basis`, UTC timestamps, working directory, exact commands, exit codes, status, raw-output path, and limitations.
- Reconcile every material claim used from `docs/architecture-migration/architecture_audit.md`, `audit_metrics.json`, the archived invalid plan, and the existing widget as `confirmed`, `changed`, `not-reproducible`, or `not-applicable` against current evidence.
- Define one JSON Schema Draft 2020-12 contract and one evidence-backed baseline model with stable IDs, typed edges, confidence, evidence, snapshots, state records, and ordered flows.
- Derive six separate views from that model: compile-time, DI/runtime, state ownership, reactive behavior, persistence, and user flow.
- Produce a completeness-driven inventory for project lifecycle/path/version, dirty/load guard, Climate, Construction/materials, Thermal, Hydraulics, Navigation, Results projections, export inputs, `CalculationContext`, and `CalculationStateService`.
- Inventory existing characterization assertions and gaps for new/load/second-load/edit/calculate/reset/save/reload/export/navigation flows, including exact event, calculator, Results-update, dirty-transition, and stale-state observations where evidence exists.
- Document `.smc` behavior separately at file I/O, serialization, model, orchestrator restore, UI-visible state, reactive/dirty, and save/backup boundaries.
- Define measurable target invariants for a composite `ProjectSession` without presenting target design as current implementation.
- Specify the future model-driven widget without changing `architecture_widget.html`.
- Run agent-executed final verification and stop at the project-local owner acceptance gate.

### Must NOT have (guardrails, anti-slop, scope boundaries)

- No edits under `src/`, `tests/`, `data/`, `installer/`, `publish/`, `resources/`, presentation directories, `.opencode/`, or any `.smc` path.
- No edits to root `AGENTS.md`, `docs/architecture-migration/AGENTS.md`, `architecture_audit.md`, `audit_metrics.json`, `architecture_widget.html`, owner source materials, `.omo/drafts/phase-0-baseline.md`, or this plan during execution.
- No creation of `ProjectSession`, state slices, tests, fixtures, production/test validators, HTML, JavaScript, CSS, package/configuration changes, or release artifacts.
- No installation or upgrade of SDKs, workloads, LSPs, Codegraph, schema validators, packages, or CLI tools.
- No fixes for build/test/analysis failures. Record a blocker with evidence and stop the dependent lane.
- No `git add`, commit, push, stash, reset, clean, checkout, restore, rebase, or broad file normalization during Phase 0.
- No use of historical metrics, `.omo/` evidence, `D:\IA\ace`, user scratch `.smc`, or the invalidated archive as current evidence without fresh verification.
- No byte-identity, compatibility-duration, transactional-restore, or ownership claim without direct evidence and owner authorization.
- No manual-click, visual-inspection, or owner-operated QA requirement.

### Exact execution write allow-list

Only these paths may be created or updated by the Phase 0 worker:

- `docs/architecture-migration/evidence/repository-snapshot.md`
- `docs/architecture-migration/evidence/environment.md`
- `docs/architecture-migration/evidence/build-baseline.md`
- `docs/architecture-migration/evidence/build-baseline.log`
- `docs/architecture-migration/evidence/test-baseline.md`
- `docs/architecture-migration/evidence/test-baseline.log`
- `docs/architecture-migration/evidence/test-results/phase-0.trx`
- `docs/architecture-migration/evidence/metrics-baseline.json`
- `docs/architecture-migration/evidence/codegraph-baseline.md`
- `docs/architecture-migration/evidence/persistence-fixtures.md`
- `docs/architecture-migration/evidence/user-flow-baseline.md`
- `docs/architecture-migration/evidence/audit-reconciliation.md`
- `docs/architecture-migration/evidence/model-validation.md`
- `docs/architecture-migration/evidence/dossier-gate.md`
- `docs/architecture-migration/evidence/final-verification-f1-plan-compliance.md`
- `docs/architecture-migration/evidence/final-verification-f2-dossier-quality.md`
- `docs/architecture-migration/evidence/final-verification-f3-runtime-qa.md`
- `docs/architecture-migration/evidence/test-results/phase-0-f3.trx`
- `docs/architecture-migration/evidence/final-verification-f4-scope-fidelity.md`
- `docs/architecture-migration/evidence/final-verification.md`
- `docs/architecture-migration/maps/architecture-model.schema.json`
- `docs/architecture-migration/maps/architecture-model.baseline.json`
- `docs/architecture-migration/maps/compile-time.md`
- `docs/architecture-migration/maps/di-runtime.md`
- `docs/architecture-migration/maps/state-ownership.md`
- `docs/architecture-migration/maps/reactive.md`
- `docs/architecture-migration/maps/persistence.md`
- `docs/architecture-migration/maps/user-flow.md`
- `docs/architecture-migration/maps/state-inventory.md`
- `docs/architecture-migration/maps/characterization-tests.md`
- `docs/architecture-migration/maps/persistence-compatibility.md`
- `docs/architecture-migration/maps/target-invariants.md`
- `docs/architecture-migration/widget-spec.md`
- `docs/architecture-migration/TASK_CONTEXT.md`

Normal `dotnet build`/`dotnet test` side effects under ignored `bin/` and `obj/` may occur. They are command side effects, not Phase 0 deliverables: record them, do not delete or restore them, and exclude them from source metrics.

## Verification strategy

> Zero human intervention - all verification is agent-executed.

- Test decision: TDD-oriented characterization planning with NUnit evidence inspection. Phase 0 reads existing assertions first and records missing future tests second; it creates or edits no tests.
- Source basis values:
  - `HEAD`: committed object at the captured SHA.
  - `working-tree`: current source including disclosed tracked modifications.
  - `HEAD-plus-approved-dossier`: HEAD plus allow-listed Phase 0 outputs, used only for final dossier consistency.
- Every Markdown receipt starts with YAML front matter containing `phase`, `snapshot_sha`, `source_basis`, `generated_at_utc`, `working_directory`, `commands`, `exit_code`, `status: pass|fail|degraded`, `raw_output`, and `limitations`.
- A `degraded` model validation is acceptable only when no installed Draft 2020-12 validator exists. JSON parsing, unique-ID checks, reference resolution, allowed enums, evidence-link existence, and six-view membership checks must still pass. Parse or structural failure is `fail`, not `degraded`.
- Every characterization capability is marked `covered`, `partial`, `missing`, or `blocked` and cites exact test symbols/assertions or says `no evidence supports it`.
- Every recalculation-sensitive flow distinguishes counters for `CalculationContext.ContextChanged`, `ICalculationStateService.StateChanged`, calculator invocation, Results projection update, and dirty-state transition. Unknown counts remain `unknown`.
- Evidence: Phase 0 artifacts live only at the exact allow-listed `docs/architecture-migration/evidence/` paths above. Worker orchestration evidence may additionally use its harness-provided `.omo/evidence/` attempt directory, but it is not architecture truth.

## Execution strategy

### Parallel execution waves

- **Wave 1 — freeze and collect:** Todo 1 runs first. After snapshot lock, Todos 2, 3, and 4 may run in parallel because they produce separate evidence artifacts and are read-only outside the allow-list.
- **Wave 2 — evidence model:** Todo 5 reconciles historical claims. Todos 6, 7, 8, and 9 may then run in parallel as partitioned research lanes, but only Todo 10 may write the schema/model files. Research lanes write only their assigned Markdown artifacts.
- **Wave 3 — synthesis:** Todo 10 integrates the canonical schema/model and verifies all IDs. Todo 11 writes target invariants and widget specification from the validated model contract.
- **Wave 4 — pre-verification gate:** Todo 12 performs dossier-wide consistency checks and dirty-worktree preservation checks, records `dossier-gate.md`, and moves `TASK_CONTEXT.md` only to `verification`. F1-F4 then run in parallel with separate result artifacts; F5 runs sequentially after all four, aggregates their verdicts into `final-verification.md`, updates `TASK_CONTEXT.md` to `awaiting-owner-acceptance` only when every lane approves, and stops.
- Central source, tests, DI, load/reset, persistence, Results, and module ViewModels are read-only in every wave. No two lanes write the same artifact.

### Dependency matrix

| Todo | Depends on | Blocks | Can parallelize with |
| --- | --- | --- | --- |
| 1 | Owner-authorized worker start | 2-12 | None |
| 2 | 1 | 5, 12 | 3, 4 |
| 3 | 1 | 5, 6, 10, 12 | 2, 4 |
| 4 | 1 | 5-10, 12 | 2, 3 |
| 5 | 2, 3, 4 | 6-11 | None |
| 6 | 5 | 10 | 7, 8, 9 |
| 7 | 5 | 10, 11 | 6, 8, 9 |
| 8 | 5 | 10, 11 | 6, 7, 9 |
| 9 | 5 | 10, 11 | 6, 7, 8 |
| 10 | 6, 7, 8, 9 | 11, 12 | None |
| 11 | 10 | 12 | None |
| 12 | 1-11 | F1-F4 | None |

## Todos

- [ ] 1. Freeze the execution-time repository and dirty-worktree boundary
  What to do / Must NOT do: From `D:\IA\ace v.2`, capture canonical root, HEAD, branch, upstream/ahead-behind, worktree list, `git status --porcelain=v1 --untracked-files=all`, tracked diff names/status, SDK version, UTC timestamp, and the exact pre-existing dirty set in `evidence/repository-snapshot.md`. For every dirty tracked file record status, HEAD blob ID where applicable, current SHA-256 (or `deleted`); recursively enumerate and SHA-256 every untracked file under the repository except ignored `.git/`, `.codegraph/`, `.omo/`, `bin/`, `obj/`, and other explicitly listed generated directories. Record excluded roots as `unhashed-excluded` with reason. Record that `docs/architecture-migration/` is pre-existing untracked owner content before Phase 0. Must not modify, stage, normalize, or hash files by opening them for write.
  Parallelization: Wave 1 | Blocked by: owner-authorized worker start | Blocks: 2-12
  References (executor has NO interview context - be exhaustive): `AGENTS.md:3-14`; `docs/architecture-migration/AGENTS.md:8-16,87-95`; `docs/architecture-migration/TASK_CONTEXT.md:7-16`; `.omo/drafts/phase-0-baseline.md` Findings and Scope OUT.
  Acceptance criteria (agent-executable): PowerShell invokes `git rev-parse --show-toplevel`, `git rev-parse HEAD`, `git branch --show-current`, `git status --porcelain=v1 --untracked-files=all`, and `git diff --name-status`; parsed root equals `D:/IA/ace v.2`, HEAD is 40 hex characters, every dirty status line maps to one ledger entry, every hashed present file's SHA-256 re-computes identically, and no allow-listed Phase 0 output is mislabeled as pre-existing unless it existed at start.
  QA scenarios (name the exact tool + invocation): Happy — PowerShell re-runs the commands and compares normalized status/path/hash records, writing results into `evidence/repository-snapshot.md`. Failure — simulate parser input containing a deleted file, a Cyrillic path, an absent upstream, and a directory-valued untracked entry; parser must retain status/path safely or mark the receipt `fail` without touching the worktree. Evidence `docs/architecture-migration/evidence/repository-snapshot.md`.
  Commit: N | Phase 0 prohibits Git mutations; owner-authorized documentation commit is post-acceptance only.

- [ ] 2. Capture SDK, solution, build, and test/TRX baseline receipts
  What to do / Must NOT do: Record `dotnet --info`, installed SDKs/runtimes, absence/presence of `global.json`, solution projects, then run a deterministic Debug baseline: `dotnet build "SnowMeltingCalculator.sln" -c Debug --nologo --no-incremental` with output redirected to `evidence/build-baseline.log`; only if build succeeds, run `dotnet test "tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj" -c Debug --no-build --nologo --logger "trx;LogFileName=phase-0.trx" --results-directory "docs/architecture-migration/evidence/test-results"` with output redirected to `evidence/test-baseline.log`. Record exact exit codes and parsed totals. Do not restore explicitly, install SDKs, fix failures, or claim behavior preservation from a green run.
  Parallelization: Wave 1 | Blocked by: 1 | Blocks: 5, 12
  References: `SnowMeltingCalculator.sln`; `src/SnowMeltingCalculator.csproj`; `tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj`; `docs/architecture-migration/AGENTS.md:58-70`; `docs/architecture-migration/TASK_CONTEXT.md:130-140`.
  Acceptance criteria: `environment.md`, `build-baseline.md`, and `test-baseline.md` contain the common receipt fields; build/test log paths exist; exit codes in receipts equal process exit codes; when test runs, TRX counters equal log totals. A non-zero build or test marks the receipt `fail`, creates no source fix, and blocks acceptance.
  QA scenarios: Happy — PowerShell parses the generated TRX XML and asserts `total = passed + failed + skipped/other` using the TRX counters. Failure — if build exits non-zero, assert test command is not launched, the build receipt is `fail`, dependent completion is blocked, and the original dirty ledger still matches. Evidence `docs/architecture-migration/evidence/environment.md`, `build-baseline.*`, `test-baseline.*`, `test-results/phase-0.trx`.
  Commit: N | No commit during Phase 0.

- [ ] 3. Recompute filtered source metrics with reproducible provenance
  What to do / Must NOT do: Produce `evidence/metrics-baseline.json` from the execution-time working tree, explicitly separating tracked/raw/filtered counts and excluding `bin`, `obj`, `.git`, `.codegraph`, `.omo`, `publish`, installer output, generated runtime folders, and generated C# files. Record command/script text or Codegraph query provenance for file count, physical/nonblank LOC, declared types, project/namespace/type references, and SCC/cycles. If a sound SCC method is unavailable, mark cycle metrics `degraded` or `not-reproducible`; never infer cycles from `using` counts.
  Parallelization: Wave 1 | Blocked by: 1 | Blocks: 5, 6, 10, 12
  References: `docs/architecture-migration/architecture_audit.md:3-11,17-35,39-83`; `docs/architecture-migration/audit_metrics.json`; `docs/architecture-migration/TASK_CONTEXT.md:92-105`; `docs/architecture-migration/AGENTS.md:89`.
  Acceptance criteria: JSON parses; includes `snapshot_sha`, `source_basis: working-tree`, exclusions, commands/queries, tool versions, raw and filtered scopes, value/status per metric, and no absolute source path rooted at historical `D:\IA\ace`. Every numeric claim used later points to a metric ID.
  QA scenarios: Happy — PowerShell `ConvertFrom-Json` validates structure and a second independent count confirms filtered `.cs` paths contain no excluded segment. Failure — inject one `obj/` file into a test list and assert the filter excludes it; if SCC computation is absent, output status is `degraded`, not a fabricated number. Evidence `docs/architecture-migration/evidence/metrics-baseline.json`.
  Commit: N | No commit during Phase 0.

- [ ] 4. Capture Codegraph provenance and source-evidence coverage
  What to do / Must NOT do: Query Codegraph first for `ProjectLoadOrchestrator`, `ResultsViewModel`, `CalculationContext`, `CalculationStateService`, `ProjectStateService`, `ProjectData`, DI registrations, reactive handlers, persistence flows, and navigation/export entry points. Record exact query text, index/staleness banners, returned files/symbols/call paths, confidence, and any fallback in `evidence/codegraph-baseline.md`. If unindexed or stale, do not initialize/update it; use targeted source reads for facts while marking graph-completeness claims unavailable/degraded.
  Parallelization: Wave 1 | Blocked by: 1 | Blocks: 5-10, 12
  References: `docs/architecture-migration/TASK_CONTEXT.md:92-105`; `src/Services/Project/ProjectLoadOrchestrator.cs:38-53`; `src/ViewModels/Results/ResultsViewModel.cs:478-520`; `src/Core/CalculationContext.cs`; `src/Configuration/ServiceCollectionExtensions.cs`.
  Acceptance criteria: Receipt lists each required architecture area with methodology `codegraph|targeted-read`, status `verified|derived|degraded`, and current source evidence; no stale Codegraph content supports a current claim without a direct current-file read.
  QA scenarios: Happy — independently read one representative source edge from each of the six views and match endpoints/kind to the receipt. Failure — treat an index stale banner as input and assert graph-completeness status becomes `degraded` and the plan does not invoke `codegraph init`. Evidence `docs/architecture-migration/evidence/codegraph-baseline.md`.
  Commit: N | No commit during Phase 0.

- [ ] 5. Reconcile all material historical architecture claims
  What to do / Must NOT do: Write `evidence/audit-reconciliation.md` mapping every material claim used by `architecture_audit.md`, `audit_metrics.json`, `architecture_widget.html`, owner source materials, and the invalidated archived plan to current evidence and one classification: `confirmed`, `changed`, `not-reproducible`, or `not-applicable`. Include historical text/path, current result, snapshot/source basis, evidence IDs, confidence, and migration impact. Historical artifacts remain unchanged.
  Parallelization: Wave 2 prerequisite | Blocked by: 2, 3, 4 | Blocks: 6-11
  References: `docs/architecture-migration/architecture_audit.md`; `docs/architecture-migration/audit_metrics.json`; `docs/architecture-migration/architecture_widget.html`; `docs/architecture-migration/archive/phase-0-baseline.invalidated-explore-chain.md:1-7`; `docs/architecture-migration/TASK_CONTEXT.md:7-16,92-105`.
  Acceptance criteria: Every historical metric or architectural claim referenced by any later map has exactly one reconciliation row; no `confirmed` row lacks a current evidence ID; old absolute paths never appear as current paths; counts cited by the widget/audit are explicitly reconciled.
  QA scenarios: Happy — a script extracts numeric/backticked headline claims from the audit and verifies each has a reconciliation key, followed by semantic read review against current evidence. Failure — provide a claim with no reproducible method and assert it is `not-reproducible`, never omitted or guessed. Evidence `docs/architecture-migration/evidence/audit-reconciliation.md`.
  Commit: N | No commit during Phase 0.

- [ ] 6. Build compile-time and DI/runtime research views
  What to do / Must NOT do: Document current projects, namespaces, types/interfaces, project/type references, repository-to-service type coupling, DI registrations, interfaces, lifetimes, constructor dependencies, resolve/create paths, concrete ViewModel injection, and unresolved dynamic paths. Write provisional model IDs and the two maps, but do not write canonical schema/model files; Todo 10 integrates them. Compile-time edges and runtime resolution edges must remain distinct.
  Parallelization: Wave 2 | Blocked by: 5 | Blocks: 10 | Can parallelize with: 7, 8, 9
  References: `SnowMeltingCalculator.sln`; `src/SnowMeltingCalculator.csproj`; `tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj`; `src/Configuration/ServiceCollectionExtensions.cs`; `src/Repositories/Construction/ConstructionRepository.cs`; `src/Services/Construction/MaterialNotFoundException.cs`; `src/Services/Project/ProjectLoadOrchestrator.cs:38-53`; `src/ViewModels/Results/ResultsViewModel.cs:478-520`; `tests/SnowMeltingCalculator.Tests/Configuration/DiRegistrationTests.cs`.
  Acceptance criteria: `compile-time.md` and `di-runtime.md` declare their filter, source basis, stable provisional IDs, evidence/confidence per edge, and unresolved gaps; every DI registration/lifetime used in claims is cross-checked with constructor dependencies; type-only coupling is not labeled runtime invocation.
  QA scenarios: Happy — parse all documented edge IDs and verify no duplicate; spot-check `ProjectLoadOrchestrator -> four ViewModels` and `ConstructionRepository -> MaterialNotFoundException`. Failure — present a `using` directive without constructor/call/registration evidence and assert it can appear only in compile-time, not DI/runtime. Evidence the two map files plus `evidence/codegraph-baseline.md`.
  Commit: N | No commit during Phase 0.

- [ ] 7. Build the completeness-driven state ownership and reactive inventories
  What to do / Must NOT do: Inventory every writable field/property/collection participating in new/load/reset/edit/calculate/save/reload/export, every value in the four target slices, and every value referenced by `ResultsViewModel`, `ProjectLoadOrchestrator`, `ProjectFileService`, `CalculationContext`, and `ICalculationStateService`. Each row includes stable state ID, current canonical owner, copies/projections, all writers/readers, reactive effects, persistence, target owner, migration status, evidence, and coverage status; unknown facts are explicit. Build `state-ownership.md` and `reactive.md` from the same provisional IDs, including publishers/subscribers/unsubscribers, commands, invalidation, calculator and Results updates, dirty transitions, load guards, reset, and multiplicity-sensitive paths.
  Parallelization: Wave 2 | Blocked by: 5 | Blocks: 10, 11 | Can parallelize with: 6, 8, 9
  References: `docs/architecture-migration/TASK_CONTEXT.md:54-90`; `src/Core/CalculationContext.cs`; `src/Services/Navigation/CalculationStateService.cs`; `src/Services/Results/ProjectStateService.cs`; `src/Services/Project/ProjectLoadOrchestrator.cs`; `src/ViewModels/Climate/ClimateViewModel.cs`; `src/ViewModels/Construction/ConstructionViewModel.cs`; `src/ViewModels/Thermal/ThermalViewModel.cs`; `src/ViewModels/Hydraulics/CircuitsViewModel.cs`; `src/ViewModels/Results/ResultsViewModel.cs`.
  Acceptance criteria: All mandatory columns exist and are non-empty or explicit `unknown/not observed`; required domains are represented; every inventory row has test coverage status; dual writable paths are risks, not normalized away; reactive edges identify publisher, subscriber, unsubscribe/lifetime, effect, evidence, and counter status.
  QA scenarios: Happy — a parser validates unique state IDs/mandatory columns and samples lifecycle plus one input and one derived value per module against current source. Failure — remove an evidence/coverage/migration field from a copied row and assert structural QA fails; an unproven exact event count remains `unknown`. Evidence `maps/state-inventory.md`, `state-ownership.md`, `reactive.md`.
  Commit: N | No commit during Phase 0.

- [ ] 8. Inventory assertion-backed characterization coverage and user flows
  What to do / Must NOT do: Write `maps/characterization-tests.md`, `maps/user-flow.md`, and `evidence/user-flow-baseline.md`. Cover cold/new, current and legacy `.smc` load, second load, each of four edit domains, invalidation, calculation, reset/repeated reset/load, save/reload, summary, PDF, all supported exports, dirty/load guard, and navigation. Each capability records test path/symbol/type, setup boundary, real-vs-mock persistence, asserted final values/events/calculations/Results/dirty/stale behavior, status, and exact future test gap (proposed file/setup/action/expected counters) without implementing it.
  Parallelization: Wave 2 | Blocked by: 5 | Blocks: 10, 11 | Can parallelize with: 6, 7, 9
  References: `docs/architecture-migration/TASK_CONTEXT.md:54-70`; `tests/SnowMeltingCalculator.Tests/Project/ProjectRoundTripTests.cs`; `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectFileServiceAtomicityTests.cs`; `tests/SnowMeltingCalculator.Tests/Core/CalculationContextInvalidationTests.cs`; `tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/DoubleCalculationPreventionTests.cs`; `tests/SnowMeltingCalculator.Tests/ViewModels/ResetOrchestrationTests.cs`; `tests/SnowMeltingCalculator.Tests/ViewModels/Hydraulics/CircuitsViewModelEventLeakTests.cs`; `tests/SnowMeltingCalculator.Tests/ViewModels/ResultsStabilizationPhase1ContractsTests.cs`; `tests/SnowMeltingCalculator.Tests/ViewModels/ResultsStabilizationPhase1BehaviorContractsTests.cs`; `tests/SnowMeltingCalculator.Tests/ViewModels/ResultsViewModelOpenProjectTests.cs`; export/report test directories.
  Acceptance criteria: Every required capability has exactly one `covered|partial|missing|blocked` outcome and evidence/test symbol or `no evidence supports it`; filenames alone earn no credit; the known real-file second-load, repeated reset/load subscription, full legacy restore, and four-domain exact-counter gaps are explicit unless fresh assertions disprove them.
  QA scenarios: Happy — script checks the capability matrix has all required categories and statuses, then semantic review verifies assertions in a sample from each category. Failure — supply a filename with no relevant assertion and assert status cannot be `covered`. Evidence `maps/characterization-tests.md`, `maps/user-flow.md`, `evidence/user-flow-baseline.md`.
  Commit: N | No commit during Phase 0.

- [ ] 9. Establish the layered `.smc` persistence and fixture compatibility baseline
  What to do / Must NOT do: Inventory checked-in fixtures with size, SHA-256, detected version, and coverage. Map every persisted `ProjectData` field to JSON name, CLR type/nullability/default, DTO, observed version, load fallback, save behavior, owning state ID, and evidence. Separate file existence/read, serialization/deserialization, validation, module reset, module restore, context/event propagation, dirty/load guard, Results projection, write/temp/backup/replace, and reload. Distinguish observed current/legacy behavior, tested guarantees, unsupported/corrupt behavior, and owner-deferred future policy. Never modify/regenerate fixtures or call semantic preservation byte identity.
  Parallelization: Wave 2 | Blocked by: 5 | Blocks: 10, 11 | Can parallelize with: 6, 7, 8
  References: `src/Models/Project/ProjectData.cs`; `src/Services/Project/ProjectFileService.cs:17-190`; `src/Services/Project/ProjectLoadOrchestrator.cs`; `src/ViewModels/Results/ResultsViewModel.cs:1573-1817`; `tests/SnowMeltingCalculator.Tests/Fixtures/v1-sample.smc`; `tests/SnowMeltingCalculator.Tests/Project/ProjectRoundTripTests.cs`; `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectFileServiceResultTests.cs`; `ProjectFileServiceAtomicityTests.cs`; `ProjectFileServiceMutationTests.cs`; `ResultsViewModelOpenProjectTests.cs`.
  Acceptance criteria: Fixture receipt hashes re-compute; every serialized DTO property has a matrix row; current/legacy columns are separate for every boundary; missing/corrupt/temp/backup/original-preservation/semantic-round-trip statuses are explicit; byte identity, compatibility duration, and transactional in-memory restore are `not established` unless directly proven.
  QA scenarios: Happy — PowerShell parses fixture JSON without writing it, computes hash, and cross-checks model properties and serializer options. Failure — corrupt JSON is analyzed only through existing tests/source evidence and marked unsupported/failure behavior; no fixture mutation occurs. Evidence `evidence/persistence-fixtures.md`, `maps/persistence.md`, `maps/persistence-compatibility.md`.
  Commit: N | No commit during Phase 0.

- [ ] 10. Integrate and structurally validate the canonical architecture schema and baseline model
  What to do / Must NOT do: Write `architecture-model.schema.json` using Draft 2020-12 and integrate verified outputs from Todos 6-9 into `architecture-model.baseline.json`. Schema requires `meta`, stable `nodes`, typed `edges`, state entries, ordered flows, snapshots (`baseline/current/target`), evidence references, confidence (`verified|derived|degraded`), and view membership. Edge kinds include compile reference, DI registration/resolution, state read/write, event publish/subscribe/unsubscribe, invalidation, recalculation, persistence read/write/transform/validate/backup, user action, navigation, and derived projection. Regenerate/check all six Markdown maps as model filters; research maps do not override the model.
  Parallelization: Wave 3 | Blocked by: 6, 7, 8, 9 | Blocks: 11, 12
  References: `docs/architecture-migration/AGENTS.md:29-42`; `docs/architecture-migration/TASK_CONTEXT.md:37-52,72-90`; all Todo 6-9 artifacts.
  Acceptance criteria: PowerShell `ConvertFrom-Json` parses both JSON files; deterministic structural validation confirms required fields, unique IDs, resolved references, allowed enums, existing evidence links, ordered flow steps, and view membership; each Markdown map contains only model IDs in its filter; no edge lacks evidence/confidence. Record validation as `full`, `degraded`, or `failed` with tool availability; structural failure always fails.
  QA scenarios: Happy — run installed validator if already available, then always run structural/reference checks and compare model-filter ID sets to all six maps. Failure — temporary in-memory test data with duplicate/orphan IDs, invalid edge kind, or absent evidence must be rejected without modifying canonical files; absent full validator yields `degraded`, not `pass/full`. Evidence `maps/architecture-model.schema.json`, `maps/architecture-model.baseline.json`, `evidence/model-validation.md`.
  Commit: N | No commit during Phase 0.

- [ ] 11. Define measurable target invariants and a model-driven widget specification
  What to do / Must NOT do: Write `maps/target-invariants.md` with invariant ID, normative statement, affected views, current evidence, later verification method, status (`verified|unverified|deferred`), and blocker. Cover composite `ProjectSession` lifecycle plus four slices, one writable owner per migrated value, ViewModels as WPF adapters, no concrete ViewModel dependencies in application services, Results as derived projection, explicit reactive lifetime/multiplicity, no stale state/subscription multiplication, `.smc` wire preservation, and sequential vertical slices. Keep `CalculationContext`, compatibility duration, transactional restore, skill placement, LSP, and widget implementation as classified deferred decisions. Write `widget-spec.md` for one model input; Baseline/Current/Target/Diff; six combinable filters; evidence drill-down; status/risk/search/legend; added/removed/changed/unresolved/invariant violation; keyboard/focus/screen-reader/reduced-motion; responsive/offline/empty/error/stale-model behavior; deterministic acceptance matrix. Do not edit HTML/CSS/JS or redesign visuals.
  Parallelization: Wave 3 | Blocked by: 10 | Blocks: 12
  References: `docs/architecture-migration/AGENTS.md:44-57`; `docs/architecture-migration/TASK_CONTEXT.md:18-35,245-255`; `docs/architecture-migration/architecture_widget.html` as historical input only; `maps/architecture-model.schema.json`; `maps/architecture-model.baseline.json`.
  Acceptance criteria: Every migration invariant has a row and is not falsely `verified` when current code violates it; deferred decisions state `record-only|blocking-for-target|out-of-scope` and owner/next phase; widget matrix covers 4 snapshot modes × 6 views plus combined filters, invalid input, stale input, evidence navigation, accessibility, and narrow viewport without implementation artifacts.
  QA scenarios: Happy — script verifies unique invariant IDs, model references, all mode/view matrix entries, and required error/accessibility states; semantic review compares invariants to both AGENTS files. Failure — a proposed target edge rendered as current or an invariant without evidence/status causes failure. Evidence `maps/target-invariants.md`, `docs/architecture-migration/widget-spec.md`.
  Commit: N | No commit during Phase 0.

- [ ] 12. Run the pre-verification dossier gate and preserve the dirty worktree
  What to do / Must NOT do: Run all artifact existence, receipt metadata, SHA consistency, link integrity, schema/model, six-filter, inventory completeness, characterization matrix, persistence matrix, target invariant, and widget-spec checks. Compare current non-dossier dirty status and hashes against Todo 1. Record this pre-verification result only in `evidence/dossier-gate.md`. Update the required factual sections of `TASK_CONTEXT.md` and set workflow stage to `verification`; do not set `awaiting-owner-acceptance`, claim `completed`, authorize Phase 1, or cross the owner gate. If any required component is blocked/fails, record it in `dossier-gate.md` and context and do not launch F1-F4.
  Parallelization: Wave 4 | Blocked by: 1-11 | Blocks: F1-F4
  References: `docs/architecture-migration/AGENTS.md:58-85`; `docs/architecture-migration/TASK_CONTEXT.md:130-168,170-236`; Todo 1 snapshot and every allow-listed artifact.
  Acceptance criteria: All required pre-verification artifacts exist; all receipts bind to the same snapshot SHA and disclose source basis; structural validation passes; only allow-listed dossier paths are Phase 0 changes; each pre-existing non-dossier path retains its original status/hash; build/test are green or Phase 0 is blocked; context sections are internally consistent and workflow stage is exactly `verification`; no final-verification artifact is written by this task.
  QA scenarios: Happy — PowerShell reruns status/hash comparison and all structural checks, records exact commands/assertions/exit codes in `evidence/dossier-gate.md`, and parses `TASK_CONTEXT.md` to assert stage `verification`. Failure — alter only an in-memory expected hash or model reference and assert the verifier reports the exact path/ID, marks the dossier gate `blocked`, does not launch F1-F4, and does not touch user files. Evidence `docs/architecture-migration/evidence/dossier-gate.md`, updated `TASK_CONTEXT.md`.
  Commit: N | No commit during Phase 0; stop for owner acceptance.

## Final verification wave

> F1-F4 run in parallel after all implementation todos and write separate immutable result artifacts. F5 runs sequentially only after F1-F4 terminate, aggregates their receipts, and may advance to owner acceptance only when all four verdicts are `APPROVE`.

- [ ] F1. Plan compliance audit
  Agent-executable QA scenario: Use PowerShell from `D:\IA\ace v.2` to parse the column-zero Todo rows in this plan, enumerate the exact write allow-list, read `repository-snapshot.md` and `dossier-gate.md`, run `git status --porcelain=v1 --untracked-files=all`, and recompute every pre-existing non-dossier hash. Inputs/actions: compare each Todo's required artifact and acceptance evidence with the filesystem; compare every changed path with the allow-list; compare current dirty statuses/hashes with the Todo 1 ledger; verify both AGENTS files and owner gates were cited and no prohibited Git command is recorded in receipts. Assertions: all 12 implementation todos have matching evidence, every Phase 0-created/updated path is allow-listed, every pre-existing non-dossier path preserves status/hash, no forbidden path or owner gate changed, and workflow stage entering final verification is `verification`. APPROVE only if every assertion passes and no required todo is `blocked`; otherwise REJECT with exact todo/path/hash/gate mismatches. Write only `docs/architecture-migration/evidence/final-verification-f1-plan-compliance.md` with commands, exit codes, assertions, mismatches, and terminal `verdict: APPROVE|REJECT`.

- [ ] F2. Code quality review
  Agent-executable QA scenario: Use PowerShell `ConvertFrom-Json` plus the deterministic structural validator recorded in `model-validation.md`; then independently parse all six Markdown maps and both inventory matrices. Inputs/actions: load `architecture-model.schema.json`, `architecture-model.baseline.json`, all six maps, `state-inventory.md`, `characterization-tests.md`, `persistence-compatibility.md`, reconciliation, and evidence receipts; compute unique node/edge/state/flow IDs, resolve every reference/evidence path, compare each map ID set with its model view membership, and scan current claims for required confidence/source basis. Assertions: JSON parses, IDs are unique, references/evidence resolve, edge kinds/status enums are allowed, no edge lacks confidence/evidence, maps introduce no non-model IDs, mandatory inventory columns are populated or explicit unknowns, historical/degraded claims are honestly labeled, and no duplicate source of truth diverges from the model. APPROVE only if all structural assertions pass and semantic review finds zero unsupported current claims; otherwise REJECT with exact ID/path/claim defects. Write only `docs/architecture-migration/evidence/final-verification-f2-dossier-quality.md` with commands, assertion counts, defects, and terminal verdict.

- [ ] F3. Real manual QA
  Agent-executable QA scenario: Use `dotnet` and PowerShell only; rerun `dotnet build "SnowMeltingCalculator.sln" -c Debug --nologo --no-incremental`, then on success rerun `dotnet test "tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj" -c Debug --no-build --nologo --logger "trx;LogFileName=phase-0-f3.trx" --results-directory "docs/architecture-migration/evidence/test-results"`, parse the allow-listed `phase-0-f3.trx`, and rerun canonical model structural validation. Inputs/actions: compare build/test exit codes and TRX totals with baseline receipts; map executed test names to the documented characterization capability matrix; verify no source/test file status/hash changed after commands. Assertions: build exit 0, test exit 0, TRX has zero failed tests and internally consistent totals, structural model validation passes, documented covered capabilities cite tests present in the run, and all pre-existing non-dossier/source/test hashes remain preserved. APPROVE only if every assertion passes; otherwise REJECT with command, exit code, failing test/capability/model/hash details. Write only `docs/architecture-migration/evidence/final-verification-f3-runtime-qa.md` and the allow-listed `docs/architecture-migration/evidence/test-results/phase-0-f3.trx`; do not delete or overwrite the baseline `phase-0.trx`.

- [ ] F4. Scope fidelity
  Agent-executable QA scenario: Use PowerShell path enumeration plus read-only semantic inspection of this plan, `TASK_CONTEXT.md`, all final maps/evidence, `widget-spec.md`, both AGENTS files, and `git diff --name-status`/`git status --porcelain=v1 --untracked-files=all`. Inputs/actions: check each Must-have requirement against a concrete artifact/section; check every Must-NOT requirement against changed paths and artifact content; verify six distinct views, all required user-flow categories, layered persistence boundaries, target/deferred distinction, widget specification-only boundary, rollback instructions, and owner acceptance stop. Assertions: no requested component is absent or silently deferred; no `ProjectSession`, tests, fixture/schema-wire, UI/widget implementation, package/config/release change, or guessed owner policy was introduced; `architecture_widget.html` and forbidden paths retain baseline status/hash; target facts are not represented as current; next workflow action is owner acceptance, not Phase 1 execution. APPROVE only if every scope-in item is evidenced and every scope-out assertion holds; otherwise REJECT with exact missing requirement or scope-leak path/statement. Write only `docs/architecture-migration/evidence/final-verification-f4-scope-fidelity.md` with the coverage matrix, violations, and terminal verdict.

- [ ] F5. Aggregate independent verification receipts and advance the owner gate
  Agent-executable QA scenario: Run sequentially after F1-F4 have terminated. Use PowerShell to open exactly the four allow-listed F1-F4 result artifacts, verify each is a regular file produced for the same `snapshot_sha`, parse its terminal `verdict`, and reject missing, malformed, stale, duplicate, or non-`APPROVE` receipts. Inputs/actions: collect each lane's commands, exit codes, assertion totals, defects, and verdict; confirm no lane wrote another lane's artifact; rerun final changed-path/hash comparison; write the consolidated matrix to `docs/architecture-migration/evidence/final-verification.md`; update `TASK_CONTEXT.md` only after aggregation. Assertions: four distinct receipts exist, all bind to the current snapshot, all four verdicts equal `APPROVE`, no unresolved blocking defect exists, and dirty-worktree preservation still passes. APPROVE/advance to `awaiting-owner-acceptance` only when all assertions pass; otherwise write aggregate `verdict: REJECT`, keep workflow at `verification` or `blocked`, list every failed/missing lane, and do not cross the owner gate. This task alone writes `final-verification.md` and performs the final workflow-stage update.

## Commit strategy

- Phase 0 execution performs no Git commit, staging, push, or branch operation because the repository begins dirty and the migration dossier is pre-existing untracked owner work.
- After F1-F4 approve and the owner explicitly accepts the Phase 0 result, a separate owner-authorized Git task may create one documentation-only atomic commit containing only the approved dossier allow-list. Recommended message: `docs(architecture): establish phase 0 baseline dossier`.
- That later commit must exclude all pre-existing non-dossier changes, generated `bin/obj`, `.omo/`, `.codegraph/`, product/test files, `.smc`, installer/publish artifacts, and presentations. If clean path-selective staging cannot be proven, do not commit.
- Rollback is path-specific and owner-approved: remove only files recorded as `phase0_created`, or restore only allow-listed files from their Todo 1 pre-execution hashes. Never use broad Git reset/clean/checkout/restore, never delete build side effects, and never touch pre-existing dirty files. Record rollback paths and resulting hashes/status.

## Success criteria

- One execution-time snapshot identifies HEAD and the complete dirty working-tree boundary; every unrelated path is demonstrably preserved.
- Build and full test baseline pass. A failure is documented and blocks acceptance rather than being fixed inside Phase 0.
- Current metrics and architectural claims are reproducible, source-basis labeled, and never copied from `D:\IA\ace`.
- Every material historical claim used by the migration has exactly one reconciliation classification with current evidence.
- The Draft 2020-12 schema and baseline model parse and pass deterministic integrity checks; full validator absence is honestly `degraded`.
- Six separate Markdown views are filters over the same canonical model and introduce no independent IDs or unsupported edges.
- State inventory covers all required domains and every row has owner/copies/writers/readers/effects/persistence/target/status/evidence/test-coverage data or an explicit unknown.
- Characterization inventory covers every required user-flow capability with assertion-backed `covered|partial|missing|blocked` status and exact counter/stale/dirty gaps.
- Persistence baseline separates file, JSON, model, restore, reactive, projection, and save/backup boundaries for current and legacy files without overclaiming support.
- Target invariants are measurable, preserve the composite `ProjectSession` boundary, and keep unresolved owner decisions deferred.
- Widget specification covers all required modes, views, diffs, evidence, accessibility, responsive/offline/error/stale behavior without changing the existing widget.
- F1-F4 each produce a separate result artifact with agent-executable commands/assertions and `APPROVE`; F5 verifies and aggregates all four into `final-verification.md`; `TASK_CONTEXT.md` points to every current receipt/artifact and workflow stops at `awaiting-owner-acceptance`.
- No Phase 0 implementation, production/test/config/release change, Git mutation, tool installation, or owner-gate crossing occurred.
