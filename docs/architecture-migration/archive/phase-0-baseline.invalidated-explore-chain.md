# INVALIDATED - Phase 0 Baseline and Reconciliation Plan

This draft is retained only as untrusted input. It MUST NOT be approved or
executed. Session metadata proved that the purported Metis, Prometheus,
Sisyphus-review, and Momus sessions all ran as `explore` agents. A task label is
not discipline-agent identity. The plan must be regenerated through real
`metis`, `plan`, and `momus` subagents under the primary Sisyphus session.

## Plan Metadata

| Field | Value |
|---|---|
| Phase | `phase-0-baseline` |
| Status | `INVALIDATED: discipline-agent chain was simulated by explore agents` |
| Repository | `D:\IA\ace v.2` |
| Historical audit root | `D:\IA\ace` |
| Plan path | `docs/architecture-migration/plans/phase-0-baseline.md` |
| Metis session | `ses_04d4d84dfffee72H6C2V2426z2` |
| Prometheus session | `ses_04d49ae1effePgsUqP0TUfeQRC` |
| Execution gate | `PROHIBITED: archived invalid draft` |

## Outcome

Execute a documentation-only baseline phase that re-verifies the historical
audit against the current `D:\IA\ace v.2` snapshot and creates a reproducible
architecture dossier. The dossier must let a later implementation plan move
state ownership into a composite `ProjectSession` without relying on stale
metrics, implicit runtime assumptions, or uncharacterized persistence and user
flows.

Phase 0 produces evidence, maps, inventories, target invariants, a
machine-readable architecture model, and a widget specification. It does not
implement `ProjectSession`, state slices, tests, a widget, or any production
refactor.

## Non-Negotiable Boundary

### Allowed writes during Phase 0 execution

- New or updated artifacts under `docs/architecture-migration/evidence/`.
- New or updated artifacts under `docs/architecture-migration/maps/`.
- The Phase 0 widget specification under
  `docs/architecture-migration/widget-spec.md`.
- `docs/architecture-migration/TASK_CONTEXT.md` updates required by the context
  update contract.
- This plan's progress/receipt section when execution is later authorized.

### Prohibited writes and actions

- No edits under `src/`, `tests/`, `data/`, `installer/`, `publish/`,
  `resources/`, `.opencode/`, or presentation directories.
- No edits to repository governance and historical inputs: root `AGENTS.md`,
  `README.md`, `docs/architecture-migration/AGENTS.md`,
  `docs/architecture-migration/architecture_audit.md`,
  `docs/architecture-migration/audit_metrics.json`, and
  `docs/architecture-migration/architecture_widget.html`. Reconciliation is
  written to `evidence/audit-reconciliation.md`; Phase 0 writes only the widget
  specification, never widget HTML.
- No edits to any path outside the explicit Phase 0 write allow-list above.
- No creation of `ProjectSession`, state slices, characterization tests,
  persistence fixtures, validators in production/test projects, or widget HTML.
- No changes to formulas, UI design, package versions, `.smc` schema or wire
  format, release artifacts, or generated publish output.
- No tool, SDK, workload, package, LSP, or Codegraph installation.
- No `git add`, `git commit`, `git push`, branch changes, stash, clean, reset,
  checkout, or revert.
- No deletion or normalization of pre-existing dirty or generated files.
- No use of the historical `audit_metrics.json` as current evidence.

`dotnet build` and `dotnet test` may update normal `bin/` and `obj/` outputs.
These are command side effects, not Phase 0 artifacts. Record their presence in
the pre-flight receipt and never delete or restore them as part of Phase 0.

## Known Starting Facts and Reconciliation Rules

- The historical audit was produced for `D:\IA\ace`; all its metrics and path
  claims are hypotheses for `D:\IA\ace v.2`.
- A fresh count observed 173 source `.cs` files when generated directories
  (`bin`, `obj`, and `win-x64`) are excluded. A raw recursive count can report
  241 by including generated files and is not an architecture metric.
- A fresh count observed 108 test `.cs` files. Execution must re-run and record
  this count rather than treating it as frozen truth.
- `ProjectSession` does not currently exist in production source.
- `src/Core/CalculationContext.cs` already represents part of the current
  shared calculation/state seam. It must be inventoried as current state, not
  silently equated with or assumed to become `ProjectSession`.
- `ResultsViewModel`, `ProjectLoadOrchestrator`, `CircuitsViewModel`,
  `CalculationContext`, `CalculationStateService`, and the
  `ConstructionRepository` to `MaterialNotFoundException` type-level coupling
  require fresh source evidence.
- Existing tests and `.smc` fixtures are coverage candidates, not proof that
  every required characterization or compatibility behavior is covered.
- A green build proves compilation only. Runtime, reactive, persistence, and
  user-flow claims need their own evidence.

Every reconciled audit claim must be classified as `confirmed`, `changed`,
`not-reproducible`, or `not-applicable`, with current evidence and the exact
historical source claim. No hard threshold such as a minimum cycle count, node
count, edge count, fixture count, or test count may substitute for completeness
against the discovered current source.

## Required Artifacts

### Evidence receipts

| ID | Path | Required content |
|---|---|---|
| `EV-GIT` | `evidence/git-baseline.md` | root, HEAD SHA, branch/upstream, status porcelain, diff names/status, ignored/generated exclusions, timestamp |
| `EV-SDK` | `evidence/sdk-baseline.md` | `dotnet --info`, SDKs, runtimes, solution/project list, commands and exit codes |
| `EV-BUILD` | `evidence/build-baseline.md` | exact build command, UTC start/end, exit code, warning/error summary, full-output attachment/path |
| `EV-TEST` | `evidence/test-baseline.md` | exact test command, UTC start/end, exit code, passed/failed/skipped totals, full-output/TRX path |
| `EV-METRICS` | `evidence/metrics-baseline.json` | current SHA, exclusion rules, file/LOC/type/reference/cycle metrics, command/query provenance |
| `EV-CODEGRAPH` | `evidence/codegraph-baseline.md` | Codegraph availability/version if exposed, exact queries, results, fallback method and degraded flags |
| `EV-FIXTURES` | `evidence/persistence-fixtures.md` | fixture paths, sizes, SHA-256, detected version, readable/round-trip test coverage references |
| `EV-FLOWS` | `evidence/user-flow-baseline.md` | each required flow, current entry point, path through system, observed test/manual coverage, gap |
| `EV-RECONCILE` | `evidence/audit-reconciliation.md` | historical claim, current result, classification, evidence links, impact |

Each Markdown receipt begins with YAML front matter containing `phase`,
`snapshot_sha`, `generated_at_utc`, `working_directory`, `commands`, `exit_code`,
and `status` (`pass`, `fail`, or `degraded`). Raw command output must be captured
verbatim in a fenced block or named sibling file. Environment-dependent claims
must not be rewritten as repository invariants.

### Maps and inventories

| View/artifact | Path | Required content |
|---|---|---|
| Compile-time | `maps/compile-time.md` | solutions/projects, namespaces, types/interfaces, project/type references, SCCs, evidence per edge |
| DI/runtime | `maps/di-runtime.md` | registrations, lifetime, service/interface, constructor dependency, concrete VM injection, creation/resolve path |
| State ownership | `maps/state-ownership.md` | canonical owners, copies, writers/readers, target owners, dual-writable risks |
| Reactive | `maps/reactive.md` | events, `PropertyChanged`, subscriptions/unsubscriptions, commands, invalidation, recalculation, Results refresh, dirty effects |
| Persistence | `maps/persistence.md` | new/load/deserialize/version/validate/restore/save/backup paths and `.smc` fields |
| User flow | `maps/user-flow.md` | new, load, second load, four edit flows, calculate, reset/repeat, save/reload, exports, dirty/load guard/navigation |
| State inventory | `maps/state-inventory.md` | one row per significant state value/group using the mandatory columns below |
| Characterization inventory | `maps/characterization-tests.md` | existing tests, behavior assertions, gaps, proposed future test locations; no implementation |
| Persistence baseline | `maps/persistence-compatibility.md` | observed wire contract, versions, fixtures, compatibility evidence and unresolved commitments |
| Target invariants | `maps/target-invariants.md` | approved architectural constraints and measurable later-phase gates |
| Model schema | `maps/architecture-model.schema.json` | JSON Schema Draft 2020-12 contract |
| Baseline model | `maps/architecture-model.baseline.json` | evidence-backed nodes/edges/state/flows for the captured SHA |

The six maps are filters over `architecture-model.baseline.json`, not separately
authored sources of truth. Each documented edge includes its `kind`, source and
target IDs, current source location or command evidence, confidence, and
applicable views.

## State Inventory Contract

Every significant value or cohesive value group must have these columns:

| Column | Meaning |
|---|---|
| `State` | Stable state ID and human-readable value/group |
| `Current canonical owner` | Current writable authority; use `unresolved` only with evidence and a gap |
| `Copies / projections` | UI copies, context copies, caches, DTOs, derived projections |
| `Writers` | All methods/commands/services able to mutate it |
| `Readers` | All material consumers |
| `Reactive effects` | events, invalidation, recalculation, Results/dirty updates |
| `Persistence` | DTO/JSON field/version or `not persisted` with reason |
| `Target owner` | `ProjectSession` lifecycle/meta or an explicit state slice/derived projection |
| `Migration status` | `legacy`, `seam`, `migrated`, `legacy removed`, or `verified` |
| `Evidence` | source locations and receipt IDs |

Inventory coverage is completeness-based. At minimum inspect project identity,
path/version/dirty/load guard, climate, construction/layers/materials, thermal
inputs/results, hydraulics/circuits/collectors, navigation, Results projections,
export inputs, `CalculationContext`, and `CalculationStateService`. Do not set an
arbitrary row minimum. Any observed dual writable path must be marked as a risk,
not normalized into a single owner without evidence.

## Characterization-Test Inventory and Gap Rules

Inventory all relevant existing tests by file and test method. For each required
behavior record assertions already made, event/recalculation counts checked,
stale-state checks, dirty/load-guard checks, fixture used, and gaps. Required
behavior categories are:

1. New project and cold start.
2. Current and legacy `.smc` load.
3. Second project load after a first load.
4. Climate input mutation.
5. Construction input/layer/material mutation.
6. Thermal input mutation.
7. Hydraulics/circuit input mutation.
8. Invalidation and exact recalculation/Results update counts.
9. Reset, repeated reset, repeated load, and subscription multiplication.
10. Save and reload with semantic value preservation.
11. Summary, PDF, and every currently supported export flow.
12. Dirty state, load guard, and navigation.

Do not infer coverage from a filename. A behavior is covered only when assertions
prove it. Record uncovered behaviors as proposed future characterization tests,
including intended test file, setup, action, observations, and expected counts,
but do not add or edit tests in Phase 0.

## Persistence Compatibility Baseline

The baseline must derive the current `.smc` contract from models, serializer
options, load/save services, version handling, validators, backup/atomic-save
behavior, fixtures, and tests. For every persisted field record JSON name, CLR
type/nullability/default, containing DTO, introduced/detected version, load
fallback, save behavior, and owning state entry.

Compatibility is semantic unless the current implementation/tests explicitly
promise byte identity. Do not require byte-for-byte JSON round trips without
evidence because ordering, whitespace, and serializer defaults can differ.
Capture fixture SHA-256 for provenance; never modify or regenerate fixtures.

The baseline must distinguish:

- Current observed read behavior.
- Current observed write format.
- Existing tested compatibility guarantees.
- Gaps and unsupported/corrupt input behavior.
- Proposed future guarantees requiring owner approval.

No new compatibility period, schema version, migration rule, or transactional
restore guarantee becomes approved merely by documenting it in Phase 0.

## Target Invariants

`maps/target-invariants.md` must define measurable future constraints:

1. `ProjectSession` is the aggregate root for current-project lifecycle,
   identity, dirty state, restore guard, and explicit slices.
2. `ClimateState`, `ConstructionState`, `ThermalState`, and `HydraulicsState`
   are explicit components; the aggregate is not a flat property bag.
3. Every migrated value has exactly one writable canonical owner at phase exit.
4. `CalculationContext` disposition is decided from evidence; no assumed rename,
   wrapper, or deletion is embedded in Phase 0.
5. ViewModels are WPF adapters, not shared canonical stores.
6. Application services do not depend on concrete ViewModels.
7. Results is a derived projection and owns no module input.
8. Reactive edges have explicit publishers, subscribers, unsubscribe lifetime,
   invalidation semantics, and at-most-once recalculation expectations.
9. Reset/load operations do not multiply subscriptions or retain stale state.
10. Save/load boundaries map state slices to the existing `ProjectData` wire
    contract without incidental schema change.
11. Existing supported `.smc` behavior remains compatible until a separately
    approved persistence migration changes it.
12. Production migration runs as one sequential vertical-slice lane; only
    independent research, tests/fixtures, and QA may run in parallel.
13. Each later structural phase updates model, maps, widget, evidence, tests,
    build receipt, and affected user-flow receipt before completion.

Target invariants are design constraints, not authorization to implement their
suggested structures.

## Machine-Readable Architecture Model Specification

The Draft 2020-12 schema must define:

- `meta`: model version, phase, repository root, baseline SHA, generated time,
  tool/query provenance, and status.
- `nodes`: stable ID, label, kind, source path/symbol, architectural layer,
  current/target status, and evidence references.
- `edges`: stable ID, source, target, kind, applicable views, source evidence,
  confidence (`verified`, `derived`, `degraded`), and notes.
- `state`: the inventory fields defined above using node references.
- `flows`: ordered steps with action, entry point, participating nodes/edges,
  observable outcome, coverage references, and gaps.
- `snapshots`: `baseline`, `current`, and `target` membership/status so the
  widget can compute a diff without a second truth source.

Required edge kinds include compile reference, DI registration/resolution,
state read/write, event publish/subscribe/unsubscribe, invalidation,
recalculation, persistence read/write/transform/validate/backup, user action,
navigation, and derived projection.

Validate JSON syntax with `ConvertFrom-Json` in Windows PowerShell 5.1. If no
installed Draft 2020-12 validator is available, run a dossier-local read-only
validation script that checks required fields, IDs, references, edge kinds,
view memberships, and evidence links. Do not claim full JSON Schema validation
when only structural validation was performed, and do not install a validator.

## Widget Specification

Create `docs/architecture-migration/widget-spec.md`; do not modify or generate
HTML in Phase 0. The specification requires:

- One shared architecture-model input, with no hand-maintained duplicate graph.
- Snapshot modes `Baseline`, `Current`, `Target`, and `Diff`.
- View filters `Compile-time`, `DI/runtime`, `State`, `Reactive`, `Persistence`,
  and `User flow`, with combinations allowed where useful.
- `Baseline` displays the accepted Phase 0 snapshot.
- `Current` displays the newest model snapshot; it equals Baseline immediately
  after Phase 0 and diverges only after later accepted updates.
- `Target` displays target nodes/edges/invariants explicitly marked proposed or
  owner-approved; it never presents proposed design as current fact.
- `Diff` compares selected snapshots and distinguishes added, removed, changed,
  unresolved, and invariant violations.
- Evidence drill-down for every visible edge/state/flow.
- Search, legend, risk/status filters, keyboard operation, focus states,
  screen-reader labels, reduced-motion behavior, responsive desktop/mobile
  layouts, empty/error states, and local/offline operation.
- Deterministic rendering from the model and a documented stale-model warning.
- Acceptance scenarios for all 4 snapshot modes, all 6 views, combined filters,
  evidence navigation, invalid model input, accessibility, and narrow viewport.

The existing `architecture_widget.html` is historical presentation input only
and remains unchanged during Phase 0.

## Execution Tasks

- [ ] 1. Capture repository snapshot and exclusions
  - Write `EV-GIT` using `git rev-parse --show-toplevel`, `git rev-parse HEAD`,
    `git branch --show-current`, upstream lookup, `git status --porcelain=v1
    --untracked-files=all`, and `git diff --name-status`.
  - Record generated-directory exclusions and every pre-existing dirty path.
  - Acceptance: root is `D:/IA/ace v.2`; receipt makes no claim that the tree is
    clean; no path outside the dossier changes because of this task.
  - QA: repeat status and compare the non-dossier path set byte-for-byte.

- [ ] 2. Capture SDK, solution, build, and test receipts
  - Run `dotnet --info`, `dotnet --list-sdks`, `dotnet --list-runtimes`, and
    `dotnet sln "SnowMeltingCalculator.sln" list` from the repository root.
  - Run `dotnet build "SnowMeltingCalculator.sln" -c Debug --nologo
    --no-incremental` and then `dotnet test
    "tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj"
    -c Debug --no-build --nologo --logger "trx;LogFileName=phase-0.trx"
    --results-directory
    "docs/architecture-migration/evidence/test-results"`.
  - Acceptance: exact versions, commands, outputs, exit codes and test totals are
    recorded. A failing build/test is a baseline finding and blocks Phase 0
    completion; it does not authorize a code fix.
  - QA: receipt totals agree with command output/TRX and status preserves all
    pre-existing non-dossier changes.

- [ ] 3. Recompute source metrics and reconcile the historical audit
  - Count source/test files with explicit exclusion of `bin`, `obj`, generated
    publish/runtime directories, and generated C# files.
  - Recompute relevant LOC, type/reference, namespace dependency, and SCC/cycle
    metrics with commands/query definitions stored in `EV-METRICS`.
  - Write `EV-RECONCILE`, including every material audit claim used by the old
    widget/audit and a `confirmed|changed|not-reproducible|not-applicable` result.
  - Acceptance: no metric is copied solely from historical files; raw and
    filtered file counts are labeled separately; all current claims reference
    the captured SHA.
  - QA: independently spot-check key files and verify the raw 241-style count is
    not presented as source architecture scope.

- [ ] 4. Capture Codegraph evidence with a declared degraded fallback
  - Use available Codegraph operations for symbols and relationships; record
    exact queries and results in `EV-CODEGRAPH`.
  - If unavailable or incomplete, mark affected sections `degraded` and use
    repository `Glob`, `Grep`, and targeted source reads. Do not fabricate a
    Tarjan result from `using` counts and do not install tools.
  - Acceptance: compile, DI, state, reactive, persistence, and user-flow inputs
    each identify their evidence method and confidence.
  - QA: spot-check representative edges against exact current source lines.

- [ ] 5. Build the compile-time and DI/runtime views
  - Populate the shared model first, then render `maps/compile-time.md` and
    `maps/di-runtime.md` as filters.
  - Include project/type dependencies, registrations/lifetimes, constructor
    resolution, concrete ViewModel dependencies, and type-only coupling.
  - Acceptance: every edge has kind, endpoints, source evidence, confidence,
    and view membership; DI runtime claims are not inferred from `using` alone.
  - QA: reconcile registrations with constructors and report unresolved runtime
    creation paths as gaps.

- [ ] 6. Build the state inventory and state-ownership view
  - Trace all mandatory state domains and populate every inventory column.
  - Represent copies, writers, readers, effects, persistence, target owner and
    migration status without assuming current ownership from naming alone.
  - Acceptance: every discovered significant state value/group is classified;
    unresolved and dual-writable entries are explicit; target owner follows the
    composite aggregate invariants.
  - QA: sample at least one input and one derived value from each module plus
    project lifecycle/meta and verify all writers/readers against source.

- [ ] 7. Build the reactive view
  - Trace publishers, subscriptions, unsubscriptions, commands, invalidation,
    recalculation, Results refresh, dirty updates, reset/load suppression, and
    repeated-load/reset lifetime behavior.
  - Acceptance: sequence and multiplicity-sensitive paths are represented, and
    missing unsubscribe/count evidence becomes a characterization gap.
  - QA: walk climate-to-results and thermal-to-hydraulics paths end-to-end from
    source evidence without treating a compile edge as a reactive edge.

- [ ] 8. Build the persistence view and compatibility baseline
  - Trace `.smc` read/version/deserialize/transform/validate/restore and
    save/serialize/atomic replace/backup paths.
  - Inventory fixtures with SHA-256 and map persisted fields to state entries.
  - Acceptance: observed read/write behavior and guaranteed compatibility are
    separated; semantic round trip is not called byte-identical without proof;
    transactional restore remains an open owner decision if not current.
  - QA: cross-check models, serializer options, services, fixtures and tests;
    record corrupt/legacy/default behavior gaps without modifying fixtures.

- [ ] 9. Build characterization and user-flow inventories
  - Inventory test methods and map assertions to all 12 behavior categories.
  - Populate `EV-FLOWS` and `maps/user-flow.md` from model flow records.
  - Acceptance: new/load/second-load/four edits/calculate/reset/save-reload/all
    exports/dirty guard/navigation have evidence or explicit gaps; event and
    recalculation count coverage is stated precisely.
  - QA: test names alone do not count as coverage; verify assertions and setup.

- [ ] 10. Define target invariants and proposed owner decisions
  - Write `maps/target-invariants.md` using the invariant section above.
  - Record the disposition of existing seams, compatibility commitment,
    transactional restore target, and skill/tool policy as owner decisions, not
    silent defaults.
  - Acceptance: invariants are measurable and do not prescribe an unverified
    `CalculationContext` conversion or authorize implementation.
  - QA: check every invariant against both AGENTS files and identify its later
    phase gate.

- [ ] 11. Complete and validate the shared architecture model
  - Write schema and baseline model from the evidence-backed maps/inventories.
  - Run JSON syntax and reference/evidence/view integrity checks without tool
    installation; record exact validation depth.
  - Acceptance: six maps can be derived as model filters; all references resolve;
    no edge lacks evidence/confidence; degraded claims remain marked.
  - QA: regenerate/check each view membership from model data and compare with
    the Markdown maps.

- [ ] 12. Write and review the widget specification
  - Create `widget-spec.md` with all snapshot/view, evidence, diff,
    accessibility, responsive, offline, error and acceptance requirements.
  - Acceptance: Baseline/Current/Target/Diff and all six view filters are fully
    specified; no HTML/widget implementation is created or edited.
  - QA: exercise the written acceptance matrix against the model fields and
    confirm each interaction has required data and an empty/error behavior.

- [ ] 13. Run final dossier QA and update persistent context
  - Validate artifact existence, cross-links, SHA consistency, model integrity,
    map/filter consistency, inventory completeness, and prohibited-path status.
  - Re-run build/test only if the baseline commands or source snapshot changed;
    otherwise verify the existing receipts still match the captured SHA.
  - Update `TASK_CONTEXT.md` with Phase 0 execution results, evidence links,
    accepted owner decisions, unresolved blockers, next phase/action, and dated
    decision log. Do not cross any later owner acceptance gate.
  - Acceptance: all completion criteria below pass and non-dossier dirty paths
    remain preserved.
  - QA: a second reviewer follows the Final Verification Wave.

## Dependencies and Parallel Read-Only Lanes

| Work | Depends on | Parallel allowance |
|---|---|---|
| Task 1 | Owner execution approval | Runs first and freezes snapshot |
| Task 2 | Task 1 | Sequential build then test; may run beside Tasks 3-4 after snapshot |
| Task 3 | Task 1 | May run beside Tasks 2 and 4 |
| Task 4 | Task 1 | May run beside Tasks 2 and 3 |
| Task 5 | Tasks 3-4 | May run beside Tasks 6-9, separate artifact files |
| Task 6 | Tasks 3-4 | May run beside Tasks 5, 7-9; state model IDs coordinated centrally |
| Task 7 | Tasks 4 and 6 seed | May run beside Tasks 8-9 |
| Task 8 | Tasks 3-4 and state IDs | May run beside Tasks 7 and 9 |
| Task 9 | Tasks 2, 4 | May run beside Tasks 7-8 |
| Task 10 | Tasks 6-9 | Sequential synthesis |
| Task 11 | Tasks 5-10 | Sequential model integration |
| Task 12 | Task 11 model contract | Separate specification artifact |
| Task 13 | Tasks 1-12 | Final sequential gate |

Parallel lanes are read-only with respect to source, test, DI, persistence,
load/reset and Results files. Writers are partitioned by dossier artifact; only
the model integrator writes schema/model files. No lane migrates state or edits
fixtures/tests.

## Completion Criteria

Phase 0 execution is complete only when:

1. Git, SDK, build, test, metrics and Codegraph/degraded-fallback receipts are
   reproducible and tied to one captured SHA.
2. Build and test baselines pass. Failures are documented blockers, not silently
   fixed within Phase 0.
3. Historical audit claims used by the migration have current classifications
   and evidence; no stale metric is presented as current.
4. All six views exist as filters over the validated shared model.
5. State inventory is complete for the discovered significant state surface and
   includes owners, copies, writers, readers, effects, persistence, target
   owners and migration status.
6. Characterization inventory distinguishes asserted coverage from gaps and
   proposes, but does not implement, missing tests.
7. Persistence baseline records current wire/read/write/fixture behavior without
   inventing byte identity or future compatibility guarantees.
8. Target invariants preserve all migration invariants and explicitly handle
   current seams without creating a `ProjectSession` god object.
9. Schema/model JSON parses, required references resolve, evidence links exist,
   and validation depth is accurately reported.
10. Widget specification covers 4 snapshot modes, 6 filters, diff semantics,
    evidence drill-down, accessibility, responsiveness, offline/error behavior,
    and deterministic model-driven rendering.
11. `git status` shows no Phase 0 edits outside the allowed dossier paths and no
    pre-existing user change was reverted, staged, normalized, or overwritten.
12. `TASK_CONTEXT.md` contains execution receipts and the correct next gate.

## Rollback Boundary

Phase 0 has no production rollback because it makes no production/test changes.
Its rollback unit is the set of newly created or Phase-0-updated dossier
artifacts listed in this plan. Never use Git-wide destructive commands to roll
back the dossier, because the repository is dirty and the dossier itself began
as user work. Any rollback must be owner-approved, path-specific, and preserve
pre-existing dossier content and all unrelated changes.

Generated `bin/obj` changes caused by build/test are not deleted or reverted by
Phase 0. They are recorded in the baseline/status evidence and left untouched.

## Material Owner Decisions at Approval

Approval of this plan must explicitly accept or revise these proposed decisions:

| ID | Decision | Proposed Phase 0 treatment |
|---|---|---|
| `OD-01` | Required read-compatibility duration for old `.smc` files | Record current observed/tested support only; make no broader commitment until separately approved |
| `OD-02` | Transactional in-memory restore after one slice fails | Baseline current behavior; defer target guarantee to the snapshot/restore phase |
| `OD-03` | Relationship between `CalculationContext` and future `ProjectSession` | Treat as an evidence-backed later design fork; do not assume rename, facade or removal in Phase 0 |
| `OD-04` | Future OpenCode skill location | Out of Phase 0 execution; retain as an open workflow decision |
| `OD-05` | C# LSP/tool installation | Not allowed in Phase 0; use available Codegraph and declared degraded fallback |
| `OD-06` | Widget implementation timing | Phase 0 produces specification only; implementation needs a later approved plan |

Owner approval authorizes execution of this documentation-only Phase 0 plan. It
does not approve any particular later `ProjectSession` implementation, schema
change, tool installation, widget implementation, commit, or production edit.

## Final Verification Wave

- [ ] F1. Governance and scope audit
  - Re-read both `AGENTS.md` files and `TASK_CONTEXT.md`; compare every changed
    path with the allowed-write list.
  - Pass: only approved dossier artifacts changed and no prohibited action was
    taken.

- [ ] F2. Evidence reproducibility audit
  - Re-run or independently sample every receipt command from the captured SHA;
    verify timestamp, exclusions, exit code and raw output linkage.
  - Pass: no stale historical metric or unlabelled environment assumption.

- [ ] F3. Architecture consistency audit
  - Validate model JSON, IDs, references, edge kinds, evidence links, view
    memberships and state/flow cross-references; compare six maps to filters.
  - Pass: one shared truth model, six complete views, no unsupported edge.

- [ ] F4. Behavior and persistence audit
  - Cross-check characterization gaps, user flows, fixture hashes, DTO fields,
    serializer behavior and compatibility claims against current source/tests.
  - Pass: coverage is assertion-backed and future guarantees remain explicit
    owner decisions.

- [ ] F5. Build/test and dirty-worktree audit
  - Confirm baseline build/test pass and compare non-dossier status paths with
    the pre-flight receipt.
  - Pass: no regression receipt is red and unrelated user changes are preserved.

- [ ] F6. Context and owner-gate audit
  - Verify `TASK_CONTEXT.md` has all Phase 0 receipts, decisions, blockers and
    the next command, and does not infer later approval/acceptance.
  - Pass: execution stops at the recorded owner gate.
