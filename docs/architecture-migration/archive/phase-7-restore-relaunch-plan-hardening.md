# Phase 7 Restore Relaunch: Plan Hardening Analysis

Date: 2026-08-27
Status: analysis only
Purpose: identify why the first Phase 7 execution could not be closed and convert each failure mode into an explicit prevention rule for a new plan.

## Executive Summary

The first execution did not fail because the restore idea was impossible. It failed because the execution order allowed production changes, broad test changes, evidence collection, and architecture-status updates to drift apart. A green full test suite was available before the required Phase 7 acceptance evidence existed, while the report-source migration and final architecture artifacts were still incomplete.

The next run should not reuse the current dirty execution as its baseline. It should start from a clean, owner-accepted Phase 6 copy, preserve the reviewed Phase 7 plan as historical provenance, and execute a new relaunch plan with smaller vertical slices and blocking evidence gates.

## Observed Failure Modes

### 1. Baseline and live execution status diverged

`baseline.md` recorded execution facts and even stated that the task was the execution authorization, while `TASK_CONTEXT.md` still stated that Phase 7 had not started and execution authorization was pending.

Impact:

- provenance could not be reconciled without reconstructing session history;
- F1 could not reliably identify the active write-set or execution state;
- a later reviewer could not distinguish an approved plan from an executed plan.

Prevention rule for the new plan:

- the first execution action must create one execution-start receipt containing the exact plan path, plan identity, baseline HEAD, branch, worktree path, and owner authorization;
- the dossier status must be updated in the same controlled step;
- before every subsequent Todo, the worker must verify that the dossier and worktree agree;
- status disagreement is a blocking stop, not a documentation task deferred to the final wave.

Required evidence:

- `execution-start.md`;
- baseline HEAD and dirty-path manifest;
- exact frozen-plan identity;
- explicit distinction between plan approval, execution authorization, and result acceptance.

### 2. The dirty worktree was not isolated before implementation

The worktree already contained extensive changes from earlier phases, architecture tooling, evidence, and test files. Some paths listed as protected in the Phase 7 baseline were also modified in the live worktree.

Impact:

- Phase 7 changes could not be attributed cleanly;
- broad diff statistics became misleading;
- F1/F4 scope review would have to infer intent from a mixed write-set;
- test changes in pre-existing dirty files could not be treated as Phase 7-only evidence.

Prevention rule for the new plan:

- execution is authorized only from a clean Phase 6 copy or a formally recorded baseline with an immutable dirty-path manifest;
- no Phase 7 implementation is allowed while an allow-listed Phase 7 path overlaps a pre-existing dirty path;
- if a required file is already dirty, stop and create a new isolated worktree/copy rather than editing it;
- capture `git status --short`, `git diff --stat`, and baseline hashes before the first implementation edit.

Required evidence:

- `baseline/porcelain.txt`;
- `baseline/protected-paths.json`;
- `baseline/phase7-allow-list.json`;
- post-Todo write-set comparison.

### 3. Large Todos mixed implementation and proof

Todo 4 combined the coordinator, adapter wiring, ordered restore, calculation publication, rollback, guard semantics, dirty/path behavior, and failure QA. Todo 7 combined DI, UI, six maps, widget/model, manual QA, and release gates.

Impact:

- a production class could exist while its acceptance contract was still unproven;
- failures were discovered only after multiple architectural surfaces had changed;
- it became unclear which part had to be fixed before proceeding.

Prevention rule for the new plan:

- split each large Todo into one contract slice and one implementation slice;
- every slice ends with a receipt before the next slice begins;
- a Todo is not complete because its code compiles or because the full suite passes;
- acceptance criteria must map one-to-one to executable tests or explicit structural checks.

Recommended decomposition:

```text
R0 clean Phase 6 baseline and execution receipt
R1 RED characterization and dependency seams
R2 validated restore candidate contracts
R3 ordered commit and rollback contract tests
R4 coordinator implementation
R5 exactly-once calculation and derived-state publication
R6 catalog non-mutation boundary
R7 session-derived report snapshot
R8 UI/DI adapter wiring
R9 architecture maps and widget/model refresh
R10 final executable gates and F1-F4
```

### 4. Tests-after did not protect the new boundary

The full Release suite reached `2020 passed, 0 failed, 1 skipped`, but the new coordinator had no sufficiently independent covering test set. The full suite therefore demonstrated regression compatibility, not complete Phase 7 acceptance.

Impact:

- the green suite could be mistaken for proof of the new architecture;
- validation, commit failure, rollback, and report-source requirements remained under-tested;
- failures in the new boundary were hidden behind broad ViewModel fixtures.

Prevention rule for the new plan:

- create RED tests before production implementation for every new contract;
- use dedicated test seams for the coordinator and report source;
- require named test classes for validation, commit order, rollback, exactly-once calculation, catalog boundary, guard lifecycle, and report snapshot;
- the full suite is a later regression gate and cannot substitute for these tests.

Required test groups:

```text
RestoreValidationTests
RestoreCommitOrderTests
RestoreRollbackTests
RestoreCalculationMultiplicityTests
RestoreCatalogBoundaryTests
RestoreGuardLifecycleTests
ReportSessionSnapshotTests
ProjectLoadUserFlowTests
```

### 5. The coordinator contract was under-specified at the test seam

`ProjectRestoreCoordinator` was added and wired, but its central method was not covered by a complete independent test matrix. The implementation also used concrete dependencies and a fallback/reset shape that made the acceptance contract harder to prove.

Impact:

- no direct proof of the exact ordered mutation sequence;
- no direct proof that calculation occurs only after all four input commits;
- no direct proof that each failure point ends in the required canonical state;
- no direct proof of behavior when rollback itself encounters an error.

Prevention rule for the new plan:

- freeze the coordinator interface and dependency seam before implementation;
- inject all external reads and calculation operations needed by tests;
- define the failure result and rollback semantics before writing production code;
- test the sequence with spies/counters, not only final state assertions;
- do not hide contract gaps behind optional concrete dependencies or compatibility fallbacks.

Required acceptance matrix:

| Boundary | Required proof |
|---|---|
| null/incomplete input | fails before canonical mutation |
| invalid enum/value | fails before canonical mutation |
| missing material | fails before canonical mutation |
| climate commit failure | clean/default result and guard release |
| construction commit failure | clean/default result and guard release |
| thermal commit failure | clean/default result and guard release |
| hydraulics commit failure | clean/default result and guard release |
| calculation failure | no second calculation, clean/default result |
| success | four commits, one calculation, derived result in session |

### 6. Report migration was left behind the restore migration

The restore path was treated as nearly complete while `CalculationReportDataBuilder` and `CalculationReportExportService` still consumed `ProjectData`. The report therefore remained capable of reading stale persisted calculated fields.

Impact:

- Todo 6 was a known production gap at the time the implementation was being considered complete;
- F2 could not verify that `ProjectSession` was the current report source;
- F3 could not verify fresh calculation followed by correct report export.

Prevention rule for the new plan:

- define the report-source contract before coordinator implementation is considered complete;
- create a stale-DTO test that fails against the baseline implementation;
- implement the immutable session-derived report snapshot before final UI wiring;
- forbid final-gate execution while any report builder/exporter production path still accepts persisted calculated DTO values as its canonical source.

Required acceptance:

- mutate or recalculate the session after loading a DTO with deliberately stale result fields;
- export without another calculation;
- assert that the builder receives session-derived values and formulas;
- assert that export does not mutate session state.

### 7. Catalog non-mutation was asserted too narrowly

The new coordinator avoided the old import calls in its direct path and a test asserted zero repository mutation calls. However, the plan required a broader open-flow proof, including byte/hash equality of catalog files and template persistence.

Impact:

- direct coordinator behavior was not equivalent to full user-visible project-open behavior;
- no durable proof existed that the materials/templates files remained unchanged;
- legacy orchestration dependencies remained a possible alternate path.

Prevention rule for the new plan:

- test the real project-open entrypoint, not only the coordinator in isolation;
- snapshot materials and templates persistence files before and after open;
- assert both zero mutating repository calls and identical SHA-256 values;
- structurally scan all restore paths for import/add/update/delete calls;
- make missing/duplicate/custom material behavior explicit before implementation.

### 8. Evidence was deferred until the end

Only `baseline.md` and the plan approval receipt existed in the Phase 7 evidence directory. Required Todo 2, Todo 4, Todo 5, Todo 6, Todo 7, and F1-F4 receipts were absent.

Impact:

- final review would have had to reconstruct evidence;
- a passing command without a receipt could not be tied to a specific acceptance criterion;
- the phase could not be closed even where technical behavior appeared green.

Prevention rule for the new plan:

- evidence is a completion artifact of each Todo, not a final documentation task;
- the worker must stop after each Todo until its receipt exists and names exact commands, outputs, files, and residual risks;
- no later Todo may start while the previous receipt is missing or verdict is not PASS.

Receipt template:

```text
TODO: <id and title>
WRITE-SET: <exact files>
ACCEPTANCE: PASS|FAIL|BLOCKED
COMMANDS: <exact commands>
RESULTS: <counts and exit codes>
EVIDENCE: <paths to logs/TRX/hashes>
RESIDUAL-RISK: <explicit none or list>
NEXT-GATE: <the only permitted next step>
```

### 9. The plan had no explicit intermediate gate after restore work

The plan allowed execution to proceed from the restore/candidate work into report and final wiring even though earlier Todo acceptance had not been formally proven.

Impact:

- the workstream advanced with unresolved proof gaps;
- later failures accumulated on top of earlier uncertainty;
- F1-F4 were effectively asked to discover missing Todo-level evidence.

Prevention rule for the new plan:

- add a mandatory `R6-A` intermediate gate after restore, rollback, calculation, and catalog boundary;
- the gate must independently review Todo receipts R1-R6 and the live write-set;
- if any receipt is missing, the only permitted action is remediation of that Todo;
- report/UI/maps work cannot begin at this gate unless the restore contract is fully evidenced.

### 10. Parallel test execution exposed shared global state

Parallel full-suite runs failed in `MainViewModelTests.TearDown` because tests shared `%APPDATA%\SnowMeltingCalculator\settings.json`. The serial run passed.

Impact:

- parallel results were not reliable;
- the final QA story required a special execution mode;
- this could be confused with a product regression.

Prevention rule for the new plan:

- record the serial command as the authoritative full-suite command unless test isolation is fixed;
- add an explicit residual-risk receipt for shared settings state;
- do not spend Phase 7 scope on broad test infrastructure cleanup unless separately authorized;
- if parallel execution is used, isolate `APPDATA` per test process before treating the result as evidence.

### 11. LSP diagnostics were unavailable through the effective workspace path

The LSP server was installed, but the diagnostics harness selected a workspace root outside the repository and returned a request-cwd error.

Impact:

- LSP could not be used as a correctness gate;
- claims of clean diagnostics would be unsupported.

Prevention rule for the new plan:

- try LSP once for supported source files;
- record the harness failure and exact reason;
- use `dotnet build` and `dotnet test` as the C# correctness gates;
- never claim diagnostics passed when the request was rejected by workspace configuration.

### 12. Frozen-plan status handling was ambiguous

The plan was reviewed and frozen, but its Todo checkboxes remained unchecked while execution facts existed elsewhere. Editing the frozen plan after review would violate the dossier rule, yet leaving status only in scattered evidence made progress hard to inspect.

Prevention rule for the new plan:

- preserve the original reviewed plan unchanged as historical provenance;
- create a new reviewed relaunch plan with explicit status semantics;
- use append-only execution receipts and dossier entries for completion state;
- define whether checkboxes are immutable planning placeholders or executor status fields before execution starts;
- never infer completion from a green test count alone.

## Why F1-F4 Could Not Close the First Run

### F1: Conformance, scope, and provenance

Blocked by the mixed dirty worktree, missing per-Todo receipts, and disagreement between `TASK_CONTEXT.md` and live execution evidence.

### F2: Architecture and code quality

Blocked by the incomplete report-source migration, insufficient independent coordinator coverage, unresolved legacy orchestration surface, and lack of structural proof for the full boundary.

### F3: Executable QA and user risk

Blocked by missing Phase 7 manual open/report evidence, missing report snapshot verification, incomplete failure-path evidence, and the parallel shared-settings race.

### F4: Scope fidelity and residual risk

Blocked by inability to separate pre-existing modifications from Phase 7 changes and by missing explicit residual-risk documentation.

## Required Changes to the Relaunch Plan

The new plan should include these mandatory controls:

1. Start from a clean Phase 6 copy and record immutable baseline identity.
2. Preserve the original Phase 7 plan unchanged; create a new relaunch plan with a new identity.
3. Add an execution-start receipt and update dossier status before production edits.
4. Build RED characterization tests and test seams before implementation.
5. Separate candidate validation, ordered commit, rollback, calculation publication, catalog boundary, report snapshot, UI wiring, and artifacts into distinct slices.
6. Add a blocking intermediate gate after restore/calc/catalog work and before report/UI work.
7. Require one receipt per slice, produced immediately after verification.
8. Treat report source as a blocking architectural contract, not a late cleanup task.
9. Require real entrypoint catalog hash tests and report stale-DTO tests.
10. Require explicit sequence/counter assertions for all four slices and one calculation.
11. Define failure semantics for validation, each commit, calculation, cancellation, and rollback failure.
12. Make serial full Release testing authoritative unless process-global state is isolated.
13. Record LSP harness limitations once and use compiler/tests without unsupported claims.
14. Add a final write-set reconciliation before F1-F4.
15. Make F1-F4 independent review domains with one consolidated receipt and a separate owner result-acceptance stop.

## Recommended Relaunch Sequence

```text
R0  clean Phase 6 baseline, plan identity, execution authorization
    -> receipt: execution-start.md

R1  RED characterization and isolated dependency/test seams
    -> receipt: r1-characterization.md

R2  validated persisted-input candidates and mapping completeness
    -> receipt: r2-candidates.md

R3  ordered four-slice commit, rollback, and guard tests
    -> receipt: r3-atomic-restore.md

R4  coordinator implementation, exactly-once calculation, session publication
    -> receipt: r4-coordinator.md

R5  full project-open catalog boundary and file-hash proof
    -> receipt: r5-catalog-boundary.md

R6  intermediate restore acceptance gate
    -> receipt: r6-restore-gate.md

R7  immutable ProjectSession-derived report snapshot and export path
    -> receipt: r7-report-snapshot.md

R8  DI and UI adapter wiring with successful-load-only projection refresh
    -> receipt: r8-ui-di.md

R9  six maps, model/widget, user-flow evidence, and release gates
    -> receipt: r9-artifacts-gates.md

R10 F1 conformance, F2 architecture, F3 executable QA, F4 scope fidelity
    -> consolidated receipt
    -> explicit owner result acceptance
```

## Scope Exclusions for the Relaunch

The following should remain explicitly out of scope unless separately approved:

- legacy `.smc` compatibility beyond the current format decision;
- broad Results derived-projection migration;
- PB-002 root-cause work;
- Markdown/PDF/Excel/Preview/Print redesign;
- unrelated dirty worktree cleanup;
- general test-runner or AppData isolation refactoring;
- manual edits to archived state files or retired workflow scripts.

## Recommended Acceptance Language

The relaunch plan should state:

> A Todo is complete only when its production/test write-set, acceptance tests, exact verification commands, evidence receipt, and residual-risk statement all exist and agree with the live repository. A green broader suite does not close a missing Todo receipt. Any baseline/status/write-set contradiction blocks the next Todo. Final F1-F4 review begins only after every implementation Todo and the intermediate restore gate are PASS.

## Final Recommendation

Do not repair the first dirty execution in place and do not mark its old frozen plan complete. Preserve it as historical analysis, restart from the physical Phase 6 copy, create a new relaunch plan based on this document, obtain a new terminal review and owner approval, then execute the smaller gated sequence above.
