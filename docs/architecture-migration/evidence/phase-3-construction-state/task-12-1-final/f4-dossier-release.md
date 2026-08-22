# F4 dossier fidelity and release gate

Date: 2026-08-19

## Scope and source set

This is an independent read-only reconciliation of Task 12.1. No existing
dossier, plan, context, notepad, raw log/TRX, source, map, model, or widget file
was edited. The only created file is this receipt.

Reviewed:

- `docs/architecture-migration/evidence/phase-3-construction-state/task-12-1-canonical-default-construction-initialization.md`
- `.omo/notepads/phase-3-construction-state/learnings.md`
- `.omo/notepads/phase-3-construction-state/issues.md`
- `.omo/notepads/phase-3-construction-state/decisions.md` and `problems.md`
- `docs/architecture-migration/TASK_CONTEXT.md`
- `docs/architecture-migration/plans/phase-3-construction-state.md`
- `.omo/plans/phase-3-task-12-1-canonical-default-construction-initialization.md`
- `docs/architecture-migration/evidence/phase-3-construction-state/task-12-executable-gates.md`
- all Task 12.1 raw logs/TRX referenced by the receipt

## Receipt, plan, and context reconciliation

The Task 12.1 receipt records the approved two-file fixture correction:
`tests/SnowMeltingCalculator.Tests/Construction/ConstructionServiceTests.cs`
and
`tests/SnowMeltingCalculator.Tests/Services/Navigation/DialogServiceThreadAffinityTests.cs`.
The receipt identifies eight stale fixture failures in the first file and one
in the second, and states that both now share one session/state/initializer
graph while preserving custom material/template behavior. The active Task
12.1 plan records the same correction boundary and its Tasks 1-7 are checked.
The current worktree status confirms both files are modified; no claim is made
that unrelated dirty files are Task 12.1 changes.

The receipt, `learnings.md`, and the final `TASK_CONTEXT.md` entry agree on:

- focused Release reproduction: `9` passed, `0` failed;
- exact contracts: `117` passed and one accepted `NotExecuted` identity;
- affected Debug: `312` passed and the same accepted `NotExecuted` identity;
- full Release: `1711` passed and three accepted TRX `NotExecuted` identities;
- Debug and Release production builds: exit `0`, zero warnings/errors;
- no production source change for the fixture correction;
- Task 12.1 releases only parent Task 13; it does not accept Phase 3.

The active plan says Tasks 1-7 are checked and F1-F4 are unchecked. The parent
Phase 3 plan says Tasks 1-12 are checked, Task 13 is unchecked, and parent F1,
F2, F3, and F4 are unchecked. This F4 receipt is the expected evidence output;
the existing active/parent plan checkbox files were not edited by this review.

`TASK_CONTEXT.md` latest Phase 3 entry says workflow `executing`, Phase result
acceptance `pending`, only Task 13 released, and parent F1-F4 unstarted. No
stale current statement says Task 10 is last or Task 11 is next. Historical
entries are retained as history and are not current workflow claims.

## Independent TRX parse

The following was parsed from each XML `TestRun/Results/UnitTestResult` list and
from `TestRun/ResultSummary/Counters`, independently of console text:

| TRX | Result-list outcomes | TRX counters: total / executed / passed / failed / notExecuted |
|---|---|---|
| `tests/SnowMeltingCalculator.Tests/TestResults/phase-3-task-12-1-full-release-nine-repro.trx` | Passed 9 | `9 / 9 / 9 / 0 / 0` |
| `tests/SnowMeltingCalculator.Tests/TestResults/phase-3-task-12-1-contracts.trx` | Passed 117; NotExecuted 1 (`ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`) | `118 / 117 / 117 / 0 / 0` |
| `tests/SnowMeltingCalculator.Tests/TestResults/phase-3-task-12-1-affected-debug.trx` | Passed 312; NotExecuted 1 (`ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`) | `313 / 312 / 312 / 0 / 0` |
| `tests/SnowMeltingCalculator.Tests/TestResults/phase-3-task-12-1-full-release.trx` | Passed 1711; NotExecuted 3 (`RegenerateCircuitsBaseline`, `RegenerateBaseline`, `ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`) | `1714 / 1711 / 1711 / 0 / 0` |

The aggregate TRX `notExecuted=0` values are recorded literally. They are not
normalized to the result-list identities. The receipt's console forms remain
separate: `117 passed / 1 skipped / 118 total`, `312 passed / 1 skipped /
313 total`, and `1711 passed / 1 skipped / 1712 total`. No failed, error,
timeout, or aborted result was found.

## Artifact path and hash reconciliation

All 11 receipt manifest entries were checked at their actual paths under
`tests/SnowMeltingCalculator.Tests/TestResults`. Every byte count and SHA-256
matched the receipt manifest:

```text
phase-3-task-12-1-dotnet-info.log                 2134    1F09C4A36F8B3ACDF461B3748E8DC83F589323766AC81377FAF49FCB84D79E3F
phase-3-task-12-1-full-release-nine-repro.trx    13359    C85ADE162AEE19E94F32DCB1988E4B6FACBF6A1FBA56AD1B6272FCAF40016D59
phase-3-task-12-1-full-release-nine-repro.log     2694    C4212A42B9F298D2A431E8DB39907B72659325BA4510C599B8A0FDD65EA15DCD
phase-3-task-12-1-contracts.trx                 162328    79E29CA134C00E721FB94D8846D163EDE9D2A097A96EE5311813608A9017BB47
phase-3-task-12-1-contracts.log                   2818    8C20209B3A47903B9BFF740BA19A3578DDD8C0A09889B35B6E5D47DD8DF81AC6
phase-3-task-12-1-build-debug.log                  842    414332E67541043F0CB5B5EDDDD787A35E4F3B7FD63C8ADBCD287077EB9CF139
phase-3-task-12-1-build-release.log                846    84C28149E22BA15F9D70DA4275B306B4C3BC47394BBBBDFA7EA54E707FE3312E
phase-3-task-12-1-affected-debug.trx           419025    5685B8A954D4C045EDEA1BCF35078076C174EBAA1AC5EB48D0DE2A0369DD9F0F
phase-3-task-12-1-affected-debug.log              2840    39F5E52C26BD49867B32E9C9CF53743479CDE52C5E7E08A19E8AEB7268400701
phase-3-task-12-1-full-release.trx             2253099    CC1B43027046E7D5EA08B86FEC248F288EF023790A5D92D99F0579D840951009
phase-3-task-12-1-full-release.log                2594    F9FE6643494436DAB5706245E7EAC765181D759FB1FE935EA968DE178E91CDA5
```

## Protected dossier and workflow checks

`git status --porcelain` and `git diff --name-status` show no tracked change in
the six map files, `maps/architecture-model.json`,
`widget/architecture-widget.mjs`, `widget/model-contract.mjs`,
`widget/generate-widget.mjs`, or generated
`docs/architecture-migration/architecture-widget.html`. Observed current
hashes for the shared model and generated widget are respectively
`BED5C535731D6036970664E9E6533C70617C250B533F02FD4C0BCEDEAF0737CC` and
`A8B12B29D931AB4555F2F20F6FA0036702CB08E48BBBC587A4188FB03E840549`.
The parent plan is present as an untracked pre-existing dossier artifact; its
Task 13 and F1-F4 checkboxes remain unchecked. No map/model/widget drift is
attributable to Task 12.1.

The receipt's statements about no stage/commit/push/checkout/reset/restore/
clean/stash operation, no Task 13 work, and no Final Wave start agree with the
reviewed notepads, context, and plan state. The expected next action is parent
Task 13 architecture dossier refresh, not Phase 3 acceptance or Phase 4.

## Fidelity mutation probe

An in-memory copy of the independently parsed expected counter/status record was
mutated only in memory: affected `passed` changed `312 -> 311`, and full
Release `notExecuted` changed `0 -> 2`. The same exact-field fidelity predicate
returned:

```text
baseline_accept=True
mutated_accept=False
mutation_rejected=True
```

No source, receipt, plan, TRX, or status file was written by the probe.

## Release decision

All current Task 12.1 dossier claims checked here have matching raw evidence,
matching artifact paths/hashes, and consistent status/next-action records. The
console/TRX counter representation is reconciled without inventing normalized
counts. This approves the release of parent Task 13 only. It does not mark
Task 13 started, does not start parent F1-F4, and does not accept Phase 3.

## Superseding post-remediation reconciliation

Date: 2026-08-19

This section supersedes the preceding release decision after the F2 constructor
remediation and its approval. It does not alter the Task 12.1 receipt, plans,
notepads, context, source, maps, model, widget, or raw artifacts.

### Current reviewer status

- F1 remains `REJECT` and pending only exact owner approval for these three
  necessary fixture-only paths: `ResetOrchestrationTests.cs`,
  `ResultsStabilizationPhase1BehaviorContractsTests.cs`, and
  `ResultsStabilizationPhase1ContractsTests.cs`. No owner approval for those
  paths is present in the reviewed evidence.
- F2 is approved. Its receipt now ends `VERDICT: APPROVE` after the nullable
  production fallback and reachable second-owner branch were removed. The
  current raw remediation artifacts use the `phase-3-task-12-1-f2-remediation-*`
  names. Their final focused TRX is `99/99` passed, and the final Debug and
  Release contract TRX files are `117` passed plus the one accepted
  `NotExecuted` identity, with zero failures. The older `f2-rerun-*` path names
  mentioned inside the F2 receipt are absent; the existing remediation-named
  artifacts are the raw evidence actually present on disk.
- F3 has receipt evidence ending `VERDICT: APPROVE`. Its fresh lifecycle TRX
  records `202` passed plus the accepted
  `ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`
  `NotExecuted` result, and its full Release TRX records `1711` passed plus the
  three accepted `NotExecuted` identities. This is a receipt status, not an
  assertion that the active plan checkbox has been updated.
- F4 is not approved because F1 remains unapproved. The active Task 12.1 plan
  has F1 unchecked, F2 checked because its receipt ends `APPROVE`, F3 unchecked,
  and F4 unchecked. These are checkbox-only workflow records and no unrelated
  plan text change is attributed to this review.

### Current workflow and protected artifacts

The Task 12.1 receipt, notepads, and `TASK_CONTEXT.md` agree that Task 12.1 is
the child Final Wave and releases only parent Task 13. The parent Phase 3 Final
Wave is distinct and remains unaccepted. In the parent plan, Task 13 and parent
F1 through F4 remain unchecked. `TASK_CONTEXT.md` remains `Stage = executing`
with phase result acceptance `pending`. No map, shared model, widget source, or
generated widget drift is attributable to Task 12.1.

Independent raw checks found these current protected hashes:

- `maps/architecture-model.json`: `BED5C535731D6036970664E9E6533C70617C250B533F02FD4C0BCEDEAF0737CC`
- `architecture-widget.html`: `A8B12B29D931AB4555F2F20F6FA0036702CB08E48BBBC587A4188FB03E840549`

All cited Task 12.1 and F3 TRX paths exist and their counters were parsed from
both result lists and `ResultSummary/Counters`. The four principal Task 12.1
TRX counters remain exactly `9/9/9/0/0`, `118/117/117/0/0`,
`313/312/312/0/0`, and `1714/1711/1711/0/0` for total, executed, passed,
failed, and aggregate `notExecuted`, respectively. Console and result-list
representations remain separate and unnormalized.

### Repeated mutation rejection probe

The copied expected status record was mutated in memory only, changing affected
Debug `passed` from `312` to `311` and full Release `notExecuted` from `0` to
`2`. The exact-field predicate returned:

```text
baseline_accept=True
mutated_accept=False
mutation_rejected=True
```

The probe wrote no source, receipt, plan, TRX, or status file.

### Superseding verdict

F2 remediation and approval are supported by current raw evidence. F3 approval
is represented only because its receipt supplies approval evidence. F1 remains
pending for exact owner authorization of the three fixture-only paths. Task 13
must remain unchecked and blocked while F1 is unapproved, and Phase 3 remains
executing and not accepted. No release of Task 13 or parent Final Wave approval
is claimed.

VERDICT: REJECT

## Superseding post-F1-overlay reconciliation

Date: 2026-08-19

This section supersedes the preceding post-remediation rejection. The current
F1 receipt and raw F1 evidence both end `VERDICT: APPROVE` after the dated
owner overlay. The owner decision dated `2026-08-19` approves exactly these
three fixture files:

1. `tests/SnowMeltingCalculator.Tests/ViewModels/ResetOrchestrationTests.cs`
2. `tests/SnowMeltingCalculator.Tests/ViewModels/ResultsStabilizationPhase1BehaviorContractsTests.cs`
3. `tests/SnowMeltingCalculator.Tests/ViewModels/ResultsStabilizationPhase1ContractsTests.cs`

The overlay is limited to the current shared
`ProjectSession`/`ConstructionState`/initializer/material-catalog wiring and
the two current collection `Clear()` calls for arrangement isolation. It does
not authorize helper factories, abstractions, wider refactoring, assertion
changes, test-contract changes, skip changes, or production behavior changes.
No such expansion is claimed.

F1, F2, and F3 each end `VERDICT: APPROVE`. F2 cites only existing
post-remediation artifacts: `phase-3-task-12-1-f2-remediation-build-debug.log`,
`phase-3-task-12-1-f2-remediation-build-release.log`,
`phase-3-task-12-1-f2-remediation-focused-debug-final.trx`,
`phase-3-task-12-1-f2-remediation-contracts-debug.trx`, and
`phase-3-task-12-1-f2-remediation-contracts-release.trx`. Their verified
results are focused `99/99` passed; Debug contracts `117` passed plus one
known `NotExecuted`; Release contracts `117` passed plus one known
`NotExecuted`; and clean Debug/Release builds with exit `0`, zero warnings,
and zero errors. The known contract identity is
`ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`.

F3's existing executable evidence remains: lifecycle `202/202` passed plus
the known `NotExecuted` identity; canonical contracts `64/64` passed; affected
Debug `319/319` passed plus the known `NotExecuted` identity; and full Release
`1711/1711` passed with exactly these three accepted result-list
`NotExecuted` identities:

- `ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`
- `RegenerateBaseline`
- `RegenerateCircuitsBaseline`

The active Task 12.1 plan had Tasks 1-7 and F1-F3 checked and F4 unchecked at
review time. The F4 checkbox remains the sole unchecked Task 12.1 Final Wave
item in that plan. The parent Phase 3 plan still has Task 13 and parent F1-F4
unchecked. Task 13 is not executed or checked; this F4 approval releases it as
the sole next implementation task only. The parent Phase 3 Final Wave remains
unstarted.

The Task 12.1 receipt, notepads, and `TASK_CONTEXT.md` remain consistent:
Phase 3 is `executing`, phase result acceptance is `pending`, and Phase 3 is
not owner-accepted or complete. No map, shared model, widget source, or
generated widget drift is attributed to Task 12.1. Protected hashes remain:

- `maps/architecture-model.json`:
  `BED5C535731D6036970664E9E6533C70617C250B533F02FD4C0BCEDEAF0737CC`
- `architecture-widget.html`:
  `A8B12B29D931AB4555F2F20F6FA0036702CB08E48BBBC587A4188FB03E840549`

The copied-counter/status mutation rejection probe was rerun without writing
repository files and returned exactly:

```text
baseline_accept=True
mutated_accept=False
mutation_rejected=True
```

All cited F1/F2/F3 artifacts and TRX paths exist. No discrepancy remains that
blocks the Task 12.1 F4 release gate.

VERDICT: APPROVE
