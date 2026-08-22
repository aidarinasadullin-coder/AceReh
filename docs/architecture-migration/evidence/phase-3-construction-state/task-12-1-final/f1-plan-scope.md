# F1 Plan Compliance and Protected-Worktree Audit

## Corrected authority comparison

- Governing plan: `.omo/plans/phase-3-task-12-1-canonical-default-construction-initialization.md`
- Correction-baseline SHA-256: `5BCEE8D2C450DFBDC7F05A044CD8DC7D1BB065F1678A825283AA18131BF12640`
- Current SHA-256: `A7EF926646BCD2AFA8B5C8F734F665475D5F75D20E7FD251DD5C7E2B38139599`
- Repository/HEAD: `D:/IA/ace v.2` / `e655735dfa66c00cf9c53be93d511eda8989e8bf`
- Raw evidence: `task-12-1-final/f1-plan-scope-raw.txt`

The plan hash drift is benign. Exhaustive substitution over its 11 top-level checkboxes reproduces the exact baseline hash by reverting only Tasks 1-7 from `[x]` to `[ ]`. There are zero non-checkbox text differences, and F1-F4 remain unchecked. Expected orchestrator checkbox progress is not a rejection reason.

## Disputed hunk classification

All three disputed paths contain technically necessary Task 12.1 adaptations rather than unrelated feature work:

1. `ResetOrchestrationTests.cs` updates two private fixture builders. It supplies canonical material lookup, one shared `ProjectSession`/`ConstructionState`, and `ConstructionDefaultStateInitializer` to `ConstructionViewModel` and `ProjectLoadOrchestrator`. The latter's Task 5 dependencies are runtime-required and throw when omitted. No test or assertion changed.
2. `ResultsStabilizationPhase1BehaviorContractsTests.cs` clears both layer collections before its existing single-layer PDF arrangement. The allow-listed shared ready fixture now loads canonical/default layers; clearing them preserves the original isolation and exact one-material assertion. No assertion changed.
3. `ResultsStabilizationPhase1ContractsTests.cs` passes the shared session and helper-created initializer to `MainViewModel`. Task 5 made both runtime-required, so this is constructor-only fixture repair. No test or assertion changed.

The focused Debug run covering `ResetOrchestrationTests`, the exact PDF behavior test, and `ResultsStabilizationPhase1ContractsTests` passed `19/19`, with zero failed or skipped tests.

## Scope ruling

The original path-authority ruling above is superseded by the dated owner overlay below. The technical hunk classification remains unchanged: all three fixture-only paths are causally attributable to Task 12.1 and behavior-preserving.

## Other guards

- No correction-baseline status record disappeared; the index remains empty.
- No forbidden production, maps, model, or widget status drift occurred.
- Save-time synchronization, direct VM dirty/context publication, duplicate recipe, and synthetic forbidden-path guards pass.
- Expected Task 12.1 receipt/evidence additions and checkbox-only plan changes were not rejected.

## Owner approval overlay

Owner decision dated `2026-08-19`: `вариант 1: узкое явное одобрение трёх fixture-файлов, без оверинжинеринга`.

The owner approves exactly these files and no other paths:

1. `tests/SnowMeltingCalculator.Tests/ViewModels/ResetOrchestrationTests.cs`
2. `tests/SnowMeltingCalculator.Tests/ViewModels/ResultsStabilizationPhase1BehaviorContractsTests.cs`
3. `tests/SnowMeltingCalculator.Tests/ViewModels/ResultsStabilizationPhase1ContractsTests.cs`

The approved scope is limited to the current classified hunks: shared `ProjectSession`/`ConstructionState`/initializer/material-catalog wiring and the two current collection `Clear()` calls for arrangement isolation. No helper factory, abstraction, wider refactor, assertion change, test-contract change, skip change, or production behavior change is authorized.

This is a dated narrow overlay on the original plan-scope ruling, not retroactive rewriting of the original plan. With the exact owner approval recorded above, and with no other existing finding blocking it, the current hunks satisfy F1. The prior `19/19` focused result, protected-worktree findings, source guards, and synthetic rejection result remain preserved.

VERDICT: APPROVE
