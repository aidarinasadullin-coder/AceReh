# phase-11-migration-tails-closure - Work Plan

## TL;DR (For humans)

Phase 10 closed the invariant program (18/18 invariants verified, owner
result acceptance recorded 2026-09-03). The owner then selected the next
scope: **the migration's own recorded tails** — nothing from the unstarted
audit P1/P2 program, no Markdown removal, no environment work. Three areas,
one sequential lane:

1. **LIM-P8-1 record correction** — the model's last "open" limitation says
   Results still shares mutable `CircuitRow` objects with `CircuitsViewModel`,
   rebuilds Specifications/EquipmentItems/SummaryCards via
   `HydraulicSummaryBuilder(CollectorData)`, and reads the module selection in
   `UpdateCollectorSummary`. Planning-time grounding shows all three clauses
   were **substantively closed by Phase 9** (Rows are projected per rebuild
   via `HydraulicCircuitRowProjection.CreateRow` from canonical
   `HydraulicCircuitSnapshot`s; the builder signature takes
   `IReadOnlyList<HydraulicCollectorSnapshot>`; the summary reads the
   canonical snapshot by Results-owned selection), but the limitation record
   was never flipped. This slice verifies the closure against live code and
   the Phase 9 receipts, and flips the record with evidence — or, if live
   code contradicts the closure, stops with `OWNER_DECISION_REQUIRED`
   instead of silently re-scoping.
2. **Sync-over-async removal in the save-catalog path** —
   `ProjectSnapshotPersistenceInputs.Templates` still calls
   `_templateRepository.GetAllAsync().GetAwaiter().GetResult()` (recorded
   Phase 6 residual risk: deadlock-prone on the UI thread, safe only on the
   cache-hit fast path). The save chain
   (`ResultsViewModel.SaveProjectCommand` → `IProjectSaveService.SaveAsync`
   → `IProjectSnapshotFactory.Create` → `IProjectSnapshotPersistenceInputs`)
   is made honestly async end-to-end. Same snapshot bytes, same `.smc` wire,
   zero behavior change — proven by the unmodified frozen contract suites.
3. **Dossier/model hygiene** — stale Phase 1-era `state-inventory.md` rows
   (ST-001..ST-005 still name the removed `ProjectStateService` as owner;
   ST-003 does not mention the Phase 6 `IProjectDisplayModeState`),
   `metadata.phase` still `phase-8` in the canonical model, and three dead
   `using SnowMeltingCalculator.ViewModels.Hydraulics;` directives in
   `src/Services/Hydraulics/` (the audit-era cycle is already dead at type
   level — `CircuitRow`/`CollectorData`/`CollectorSummary` live in
   `Models/Hydraulics`). Dated corrections plus deterministic widget
   regeneration.

Everything the owner accepted in Phases 1–10 is preserved: `DEC-001 = A`,
Phase 6 save boundary semantics, Phase 7 restore boundary, Phase 8 derived
Results, Phase 9 closed seams, Phase 10 reactive closure and B2 verifier
disposition, `.smc` wire compatibility, RR-002/RR-004 as recorded limitations.

## Scope

### Authority and frozen-plan lifecycle

- Authoring candidate:
  `docs/architecture-migration/plans/phase-11-migration-tails-closure.md`,
  authored directly in the canonical plans location. Terminal review and
  owner plan approval freeze this file; a `.omo/plans/` mirror, if created,
  is an operational execution ledger only, not a second authority.
- Active dossier authority: `docs/architecture-migration/AGENTS.md` and the
  latest `docs/architecture-migration/TASK_CONTEXT.md`.
- Planning approval authorizes plan writing only; execution still belongs to
  a separate worker session started by the owner's explicit
  `/architecture-start phase-11-migration-tails-closure`.
- Baseline: the post-Phase-10-acceptance tree. The tracked worktree carries
  the accepted Phase 10 documentation set as uncommitted changes
  (`TASK_CONTEXT.md`, `maps/{reactive,target-invariants}.md`,
  `maps/architecture-model.json`, `widget/verify-widget.mjs`,
  `architecture-widget.html`) plus untracked phase-10 evidence, the two
  Phase 10 test files, and unrelated protected `docs/workspace/*` items.
  Every slice receipt records its exact write-set against this baseline.
  Superseded-at-planning identities recorded here for provenance:
  `architecture-model.json` SHA-256
  `B59122F86F6169A25CD97CA9F0CF78F1FBB87FE4DBB9AAD4F1DD9DB01BFD871B`;
  `architecture-widget.html` 15998126 bytes, SHA-256
  `A13952963729398C7EDF885D83B4B2D1F806E4516337A75D31C6A05574C6289C`;
  frozen Phase 10 plan SHA-256
  `D8F893B20AA468D10ED42C275A3FC1D951A3354409E37CDF06B3412F411135B7`
  (unchanged by this phase).
- The dirty baseline-relative delta discipline applies; no commits are
  implied by this plan.

### In scope

- Verification and record flip of `LIM-P8-1` (model `limitations` array +
  `EV-P11-*` evidence record), grounded in live code and Phase 9 receipts.
- Async save-catalog path: `IProjectSnapshotPersistenceInputs` and
  `IProjectSnapshotFactory` become async at the catalog step
  (`GetTemplatesAsync()`, `CreateAsync`), `ProjectSnapshotPersistenceInputs`
  and `ProjectSnapshotFactory` implement them, `ProjectSaveService` awaits
  the factory. Production tests updated only where they reference the changed
  member signatures; behavior contracts untouched.
- One added characterization test pinning that a save on the warm-catalog
  path produces a byte-identical `ProjectData` payload to the pre-change
  implementation (hash pin against the mapped DTO, not the file).
- Dated `state-inventory.md` overlay: historical ST-001..ST-005 first-block
  rows marked as Phase 1 historical with pointers to the canonical Phase 4+
  addendum rows and the Phase 10 writer inventory; ST-003 corrected (live
  owner `IProjectDisplayModeState`/`ProjectDisplayModeState`, Results
  read-through verified with file:line); ST-023 `ProjectData` row annotated
  as the unchanged wire DTO.
- `metadata` refresh in `maps/architecture-model.json`
  (`phase` → `phase-11-migration-tails-closure`, refreshed `snapshot_sha`,
  `source_basis`), `LIM-P8-1` flip from Slice 1, `EV-P11-*` records;
  deterministic widget regeneration; both verifier suites and
  `generate-widget.mjs --check` pass; `git diff --check` clean.
- Removal of the three dead
  `using SnowMeltingCalculator.ViewModels.Hydraulics;` directives in
  `src/Services/Hydraulics/{CircuitsValidator,CollectorTypeSelector,ICollectorTypeSelector}.cs`
  (compile-proven cosmetics).
- One dated `TASK_CONTEXT.md` entry per slice.

### Must NOT have

- No change to what a save produces: identical `ProjectSnapshot` content,
  identical `ProjectPersistenceMapper` output, identical `ProjectData` wire
  shape (`Version = "1.1"`), identical file-write semantics. The async
  refactor changes **when** the catalog is fetched, not **what**.
- No change to `ProjectSession`, the four canonical slices, the Phase 7
  restore boundary, or the Phase 10 reactive closure.
- No change to `DEC-001 = A` (`CalculationContext` untouched; its
  `ST-020..ST-022` projection seam stays as accepted).
- No Markdown removal, no export/PDF/Excel/preview/print behavior change, no
  UI change, no undo/redo implementation.
- No audit-P1/P2 program work (repository cycle, VM→repositories,
  `AppSettings`, `GlycolDataService` split, code-behind refactors, README) —
  explicitly out of scope until the owner directs it.
- No `CalculationContext` writer change, no invalidation/publication
  semantics change, no manual-QA claims, no RR-002/RR-004 closure claims.
- No rewriting of historical map rows or receipts: corrections are dated
  overlays/annotations; history is preserved.
- No production edits outside the named write-set; no test weakening; the
  frozen contract suites
  (`ProjectSnapshotContractTests`, `ProjectPersistenceMapperTests`,
  `ProjectFileService*Tests`, `ResultsStabilizationPhase1*`,
  `ResultsOwnedCircuitProjectionTests`, lifecycle/multiplicity suites) pass
  unmodified.

## Verification strategy

- Grounding is exploration-first: the worker re-verifies every inventory
  claim below against live code before editing. All file/line references are
  planning-time observations, not frozen truths.
- Build-before-test is mandatory:
  `dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo`
  before any `dotnet test --no-build`; any focused command executing 0 tests
  is a failed verification even if the exit code is 0.
- The async refactor is proven behavior-neutral by the unmodified frozen
  suites plus one added hash-pin characterization test; the LIM-P8-1 flip is
  proven by live-code facts and existing Phase 9 suites, not by new
  production code.
- Evidence reuse: Phase 6–10 receipts are the frozen baseline re-proven
  against; never rewritten.

## Execution strategy

One sequential lane. Slices 1→2→3→4 in order; read-only verification may run
ahead. Any discovery that contradicts this plan's grounding (a live shared
`CircuitRow`, a save-behavior delta, a schema/validation failure after the
metadata refresh) stops the lane with `OWNER_DECISION_REQUIRED` before any
further edit. Prometheus does not run product tests in this planning session;
the worker creates the evidence directory, builds, runs the exact commands,
and rejects 0-test executions.

## Todos

- [ ] 1. LIM-P8-1 live verification and model record flip - expect the limitation record flipped to closed with live-code evidence, or an OWNER_DECISION_REQUIRED stop if live code contradicts closure

  **Goal:** Turn the last "open" limitation into an evidence-backed closed
  record without touching production code.

  **Write-set / change class:** evidence + model JSON only. No production
  edits in this slice.

  **References:** model `limitations` entry `LIM-P8-1`
  (`maps/architecture-model.json`); Phase 9 receipts
  `docs/architecture-migration/evidence/phase-9-legacy-seams-cleanup/`
  (terminal-plan-review-receipt names the LIM-P8-1 clauses as Phase 9
  in-scope); live anchors to re-verify:
  `src/ViewModels/Results/ResultsViewModel.cs` — `UpdateCircuitsFilter`
  (~:1441, builds rows via `HydraulicCircuitRowProjection.CreateRow` from
  canonical snapshots), `UpdateCollectorSummary` (~:1390, canonical snapshot
  by Results-owned selection), `UpdateCollectorsList` (~:1332, canonical
  snapshot); `src/Services/Results/HydraulicSummaryBuilder.cs`
  (`IReadOnlyList<HydraulicCollectorSnapshot>` signatures);
  `src/Services/Results/HydraulicCircuitRowProjection.cs`; grep gate:
  no `CircuitsViewModel` reference in `ResultsViewModel.cs` outside comments.

  **Acceptance:** The receipt records (a) the three LIM-P8-1 clauses checked
  one-by-one against live anchors and Phase 9 evidence; (b) the grep/compile
  facts; (c) targeted suites green:
  `FullyQualifiedName~ResultsOwnedCircuitProjectionTests|FullyQualifiedName~ResultsStabilizationPhase1BehaviorContractsTests|FullyQualifiedName~ResultsStabilizationPhase1ContractsTests|FullyQualifiedName~ResultsViewModelCollectorEquipmentItemsTests|FullyQualifiedName~HydraulicsMultiplicityCharacterizationTests`;
  (d) the model `limitations` `LIM-P8-1` entry flipped to closed with an
  `EV-P11-LIMP81` evidence record pointing at the receipt; (e) verifier
  model-v2 suite passes on the updated model (widget regeneration deferred to
  Slice 4). If any clause is NOT closed live, the slice stops with
  `OWNER_DECISION_REQUIRED` and records the finding — no silent re-scope.

  **Happy QA:** The five targeted suites pass unmodified; the grep fact is
  clean; the model validates.

  **Failure QA:** A live contradiction between the limitation text and code
  that this slice cannot resolve as a record correction stops the lane;
  flipping the record without the evidence facts is a blocker.

  **Commands / receipt:** `mkdir -p docs/architecture-migration/evidence/phase-11-migration-tails-closure/logs`;
  `dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo`;
  `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~ResultsOwnedCircuitProjectionTests|FullyQualifiedName~ResultsStabilizationPhase1BehaviorContractsTests|FullyQualifiedName~ResultsStabilizationPhase1ContractsTests|FullyQualifiedName~ResultsViewModelCollectorEquipmentItemsTests|FullyQualifiedName~HydraulicsMultiplicityCharacterizationTests" --logger "trx;LogFileName=slice-1-lim-p8-1.trx" --results-directory "docs/architecture-migration/evidence/phase-11-migration-tails-closure/logs"`;
  receipt `docs/architecture-migration/evidence/phase-11-migration-tails-closure/slice-1-lim-p8-1-closure.md`.

  **Next gate / Commit:** only Todo 2 after PASS.

- [ ] 2. Async save-catalog path: remove sync-over-async from the Phase 6 save boundary - expect identical snapshot bytes with an honestly async chain

  **Goal:** Make the save chain async end-to-end so no production code calls
  `GetAwaiter().GetResult()`; eliminate the recorded Phase 6 deadlock risk
  without changing what a save produces.

  **Write-set / change class:** production + tests. Production:
  `src/Services/Project/IProjectSnapshotPersistenceInputs.cs` (property
  `Templates` → `Task<IReadOnlyList<ConstructionTemplate>> GetTemplatesAsync()`),
  `src/Services/Project/ProjectSnapshotPersistenceInputs.cs` (async
  implementation), `src/Services/Project/IProjectSnapshotFactory.cs`
  (`Create` → `Task<ProjectSnapshot> CreateAsync(IProjectSession)`),
  `src/Services/Project/ProjectSnapshotFactory.cs`, and the single call site
  in `src/Services/Project/ProjectSaveService.cs` (`await
  _snapshotFactory.CreateAsync(...)` inside the existing async `SaveAsync`).
  No DI registration change (interface bindings unchanged); no
  `ResultsViewModel` change (already awaits `SaveAsync`). Tests: update only
  the member-signature references in
  `tests/SnowMeltingCalculator.Tests/Services/Project/{ProjectSnapshotFactoryTests,ProjectSaveServiceTests}.cs`;
  add one hash-pin characterization test (same `IProjectSession` fixture
  maps to the same `ProjectData` DTO as the pre-change contract — assert on
  `ProjectPersistenceMapper` output identity against a pinned literal or a
  golden serialization, following the existing contract-test style).

  **References:** `src/Services/Project/{ProjectSnapshotPersistenceInputs,ProjectSnapshotFactory,ProjectSaveService}.cs`;
  `src/Repositories/Construction/ConstructionTemplateRepository.cs`
  (`GetAllAsync` = `EnsureLoadedAsync` + cached `_templates`);
  `src/Repositories/Construction/MaterialRepository.cs:140` (sync
  `GetAllMaterials()` precedent — unchanged); frozen contract suites:
  `ProjectSnapshotContractTests`, `ProjectPersistenceMapperTests`,
  `ProjectFileService*Tests`; Phase 6 receipts
  (`evidence/phase-6-project-snapshot-save-boundary/phase-6-consolidated-receipt.md`
  records the residual risk this slice closes).

  **Acceptance:** (a) `grep -rn "GetAwaiter().GetResult()" src --include="*.cs"`
  returns zero matches; (b) targeted suites green:
  `FullyQualifiedName~ProjectSnapshotFactoryTests|FullyQualifiedName~ProjectSaveServiceTests|FullyQualifiedName~ProjectSnapshotContractTests|FullyQualifiedName~ProjectPersistenceMapperTests|FullyQualifiedName~ProjectFileServiceAtomicityTests|FullyQualifiedName~ProjectFileServiceMutationTests|FullyQualifiedName~ProjectFileServiceResultTests`;
  (c) the frozen contract suites pass **unmodified** (contract/mapper/file
  tests untouched by the diff); (d) the hash-pin test passes; (e) no wire,
  DTO, or file-write semantics change (the suites prove it).

  **Happy QA:** Build 0 warnings / 0 errors; all targeted suites green.

  **Failure QA:** Any `ProjectData`/`ProjectSnapshot` shape change, any
  file-write semantics change, or a weakening of a frozen contract test is a
  blocker; a cold-cache save hanging in tests (the old deadlock path) is a
  blocker.

  **Commands / receipt:** `mkdir -p docs/architecture-migration/evidence/phase-11-migration-tails-closure/logs`;
  `dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo`;
  `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~ProjectSnapshotFactoryTests|FullyQualifiedName~ProjectSaveServiceTests|FullyQualifiedName~ProjectSnapshotContractTests|FullyQualifiedName~ProjectPersistenceMapperTests|FullyQualifiedName~ProjectFileServiceAtomicityTests|FullyQualifiedName~ProjectFileServiceMutationTests|FullyQualifiedName~ProjectFileServiceResultTests" --logger "trx;LogFileName=slice-2-async-save-path.trx" --results-directory "docs/architecture-migration/evidence/phase-11-migration-tails-closure/logs"`;
  `grep -rn "GetAwaiter().GetResult()" src --include="*.cs" | grep -v obj` (expect empty);
  receipt `docs/architecture-migration/evidence/phase-11-migration-tails-closure/slice-2-async-save-path.md`.

  **Next gate / Commit:** only Todo 3 after PASS.

- [ ] 3. Full-suite regression after the production change - expect 0 failed except the known RR-004 skip, delta exactly this plan's added tests

  **Goal:** Prove the whole application is behaviorally where Phase 10
  acceptance left it, plus only the async-catalog refactor and the new
  characterization test.

  **Write-set / change class:** tests/evidence only.

  **References:** Phase 10 full-regression receipt (2050 passed / 0 failed /
  1 skip); Slice 2 write-set.

  **Acceptance:** 0 failed; exactly 1 known RR-004 external-fixture skip
  (`D:\IA\ace\Тест\тест 40.smc`, recorded as skip, never as pass); test-count
  delta vs Phase 10 equals exactly the tests this plan added (the one
  hash-pin characterization test; signature-only updates in the two touched
  test files add none); `git diff --name-only -- '*.smc'` empty; no
  unrelated test drift.

  **Happy QA:** The full-suite TRX plus the empty `.smc` diff.

  **Failure QA:** Any new failure, any `.smc` fixture diff, or an
  unexplained test-count delta is a blocker.

  **Commands / receipt:** `mkdir -p docs/architecture-migration/evidence/phase-11-migration-tails-closure/logs`;
  `dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo`;
  `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --no-build --logger "trx;LogFileName=slice-3-full-regression.trx" --results-directory "docs/architecture-migration/evidence/phase-11-migration-tails-closure/logs"`;
  `git diff --name-only -- '*.smc'`; receipt
  `docs/architecture-migration/evidence/phase-11-migration-tails-closure/slice-3-full-regression.md`.

  **Next gate / Commit:** only Todo 4 after PASS.

- [ ] 4. Dossier/model hygiene and deterministic widget regeneration - expect corrected maps metadata, removed dead usings, regenerated widget, all gates green

  **Goal:** Align the dossier with the live, accepted architecture: dated
  `state-inventory.md` overlay, refreshed model metadata, dead-using removal,
  and the deterministic widget/verifier cycle.

  **Write-set / change class:** architecture artifacts + tiny production
  cosmetics + one dated `TASK_CONTEXT.md` entry (plus the per-slice entries
  from Slices 1–3 if not yet appended).

  **References:** `docs/architecture-migration/maps/state-inventory.md`
  (historical ST-001..ST-005 first-block rows naming `ProjectStateService`;
  Phase 4+ addendum rows; live anchors: `MainViewModel.cs:78` session
  subscription, `ProjectDisplayModeState.cs`, `ResultsViewModel.cs:304-312`
  read-through); `maps/architecture-model.json` `metadata`;
  `src/Services/Hydraulics/{CircuitsValidator,CollectorTypeSelector,ICollectorTypeSelector}.cs`
  dead usings; Phase 10 verifier/widget gates as the passing baseline.

  **Acceptance:** (a) a dated Phase 11 overlay in `state-inventory.md`
  marks the ST-001..ST-005 first block as Phase 1 historical (canonical
  ownership per the Phase 4+ addendum rows and the Phase 10 writer
  inventory), corrects ST-003 (live owner `IProjectDisplayModeState` /
  `ProjectDisplayModeState`, Results read-through with file:line), and
  annotates ST-023 as the unchanged wire DTO — history preserved, no row
  rewrites; (b) `metadata.phase` = `phase-11-migration-tails-closure`,
  `snapshot_sha` refreshed, `source_basis` updated, and `EV-P11-*` evidence
  records resolve; (c) the three dead usings are removed;
  `grep -rn "using SnowMeltingCalculator.ViewModels" src/Services --include="*.cs"`
  returns zero matches; (d) `node docs/architecture-migration/widget/verify-widget.mjs`
  passes both suites, `node docs/architecture-migration/widget/generate-widget.mjs --check`
  passes, two sequential generations are byte-identical; (e)
  `git diff --check` clean; (f) the dated `TASK_CONTEXT.md` entry records
  the slice results; (g) RR-002 and RR-004 re-stated as preserved
  limitations, not closed.

  **Happy QA:** Verifier suites and widget-generation check pass; the
  metadata/limitations changes validate against the widget schema.

  **Failure QA:** Any schema/validation failure after the metadata refresh,
  an orphaned evidence reference, a history-rewriting diff on the inventory
  rows, or a non-deterministic widget is a blocker.

  **Commands / receipt:** `mkdir -p docs/architecture-migration/evidence/phase-11-migration-tails-closure`;
  `node docs/architecture-migration/widget/verify-widget.mjs` (both suites,
  exact CLI as in Phase 10 slice 7); `node docs/architecture-migration/widget/generate-widget.mjs --check`;
  `node docs/architecture-migration/widget/generate-widget.mjs` (twice for
  the determinism proof); `git diff --check`; receipt
  `docs/architecture-migration/evidence/phase-11-migration-tails-closure/slice-4-dossier-hygiene.md`.

  **Next gate / Commit:** only the Final verification wave after PASS.

## Final verification wave

- [ ] F1. Scope, provenance and invariant check - expect the plan to preserve the approved Phases 1-10 contracts and the current Phase 11 boundary

  Verify canonical plan identity, scope, must-not-have rules, and preserved
  invariants. Confirm the plan still rejects `CalculationContext` writer
  changes (`DEC-001 = A`), save/restore semantics changes, `.smc` format
  drift, Markdown/export work, audit-P1/P2 scope absorption, and undo/redo.

- [ ] F2. Code-boundary and architecture check - expect the save chain honestly async, the LIM-P8-1 flip evidence-backed, and the dossier aligned

  Independently inspect the live source: the async chain
  (`ProjectSaveService` → `CreateAsync` → `GetTemplatesAsync`) has no
  sync-over-async; the model `LIM-P8-1` record and `EV-P11-*` trace to slice
  receipts; the state-inventory overlay matches live anchors; no
  `using SnowMeltingCalculator.ViewModels` remains under `src/Services`.

- [ ] F3. Executable QA check - expect every slice to have agent-executed happy/failure coverage with named commands and receipts

  Verify each slice lists a concrete command or test filter, an explicit
  build-before-test step where focused tests run with `--no-build`, an
  explicit receipt path, and at least one happy and one failure assertion.
  Any command plan that could pass with 0 matching tests fails this gate.

- [ ] F4. Consolidated stop check - expect one final receipt set and then a stop for owner acceptance

  Consolidate the three review domains, confirm the phase is still within
  scope, and stop for explicit owner result acceptance. Never mark
  completion or infer acceptance.

## Commit strategy

The planner does not stage, commit, or run product code. Only the Phase 11
plan artifact and its terminal-review receipt are authored in this session.
Execution belongs to the separate worker session authorized by the owner's
`/architecture-start phase-11-migration-tails-closure`.

## Success criteria

- The plan contains 4 execution slices plus 4 final verification gates, each
  with concrete commands, receipts, and happy/failure QA.
- `LIM-P8-1` is either flipped to closed on live-code evidence or the lane
  stopped with `OWNER_DECISION_REQUIRED` — never silently re-scoped.
- The save chain contains no sync-over-async; snapshot bytes, `.smc` wire,
  and file-write semantics are proven unchanged by unmodified frozen suites
  plus one hash-pin characterization test.
- The dossier reflects live architecture: dated state-inventory overlay,
  refreshed model metadata with `EV-P11-*`, three dead usings removed,
  deterministic widget regeneration with both verifier suites and
  `--check` passing.
- The full suite is green with exactly the known RR-004 skip; the
  test-count delta equals this plan's additions.
- The plan contains no audit-P1/P2 work, no `CalculationContext` writer
  work, no Markdown/export work, no manual-QA claims, and no scope drift
  beyond the approved Phase 11 boundary.
