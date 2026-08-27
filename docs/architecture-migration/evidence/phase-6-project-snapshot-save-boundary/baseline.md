# Phase 6 Task 1 Baseline

Date: 2026-08-25
Repository: `D:\IA\3ace v.2`
Branch: `master`
Upstream: `origin/master`

## Authority and Plan Identity

- Canonical plan: `docs/architecture-migration/plans/phase-6-project-snapshot-save-boundary.md`
- Mirror: `.omo/plans/phase-6-project-snapshot-save-boundary.md`
- Canonical plan bytes: `29455`
- Mirror bytes: `29455`
- Canonical SHA-256: `C56E66D2733D65CC56190A7B95B4D87F5F032AA75E83339925CEB23C2E5A4E92`
- Mirror SHA-256: `C56E66D2733D65CC56190A7B95B4D87F5F032AA75E83339925CEB23C2E5A4E92`
- Plan/mirror identity: equal
- Terminal planning receipt: `docs/architecture-migration/evidence/phase-6-project-snapshot-save-boundary/terminal-plan-review-receipt.md`
- Receipt fields: `REVIEW_ID`, `SUBJECT`, `RECEIPT`, `VERDICT`, `REASON` present
- Receipt verdict: `APPROVE`
- Execution authorization: explicitly granted by `/architecture-start phase-6-project-snapshot-save-boundary`
- Result acceptance: pending owner decision; not inferred from this baseline

## Retired Control Plane

The following checks were run before execution. The active machine control plane is not restored:

| Path/check | Result |
|---|---|
| `docs/architecture-migration/STATE.json` | absent |
| `docs/architecture-migration/workflow/validate-state.mjs` | absent |
| `docs/architecture-migration/workflow/validate-state.test.mjs` | absent |
| `docs/architecture-migration/workflow/register-state.mjs` | absent |
| `docs/architecture-migration/workflow/` entries | `0` |
| `docs/architecture-migration/archive/STATE.json` | present, provenance-only |

The historical `validate-state.mjs validate --check-plan` command is not an executable gate because the workflow was retired and the file is absent.

## Save Boundary Observed Before Changes

The current save path is:

`ResultsViewModel.SaveProject` -> `SaveToFile` -> `SaveCurrentProject` -> `IProjectFileService.SaveProjectResultAsync` -> `.smc`

Observed current semantics:

- `SaveProject` uses `SaveProjectAs` when `CurrentFilePath` is empty; otherwise it calls `SaveToFile(CurrentFilePath)`.
- `SaveToFile` builds `ProjectData` through `SaveCurrentProject`.
- Each save sets `ModifiedDate = DateTime.Now`.
- `CreatedDate` is set to `DateTime.Now` only when it is `default`.
- Successful persistence calls the existing clean transition once through `_projectStateService.MarkClean()`.
- Failure returns `false`, reports the existing error/status, and does not call `MarkClean()`.
- `ProjectFileService` remains the serializer/file-I/O boundary: it normalizes `.smc`, writes a same-volume `.tmp`, backs up an existing file to `.bak`, and moves the temporary file into place.

## Commands and Results

All commands were run from the repository root. Exit code `0` means success.

| Command | Exit/result |
|---|---|
| `$env:GIT_MASTER='1'; git rev-parse --show-toplevel` | `0`, `D:/IA/3ace v.2` |
| `$env:GIT_MASTER='1'; git branch --show-current` | `0`, `master` |
| `$env:GIT_MASTER='1'; git rev-parse --abbrev-ref '@{upstream}'` | `0`, `origin/master` |
| `$env:GIT_MASTER='1'; git status --porcelain=v1` | `0`, 37 dirty paths |
| `dotnet build src\SnowMeltingCalculator.csproj -c Debug --nologo` | `0`, 0 warnings, 0 errors |
| `dotnet test --configuration Debug --no-build --filter "FullyQualifiedName~ProjectRoundTripTests"` | `0`, 12 passed, 0 skipped, 0 failed |
| `dotnet test --configuration Debug --no-build --filter "FullyQualifiedName~ResultsViewModelOpenProjectTests"` | `0`, 37 passed, 1 skipped, 0 failed |
| `dotnet test --configuration Debug --no-build --filter "FullyQualifiedName~ProjectFileService"` | `0`, 13 passed, 0 skipped, 0 failed |

Skipped test: `ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`. It remains skipped by the existing test condition and was not changed by Task 1; it is not reclassified as a pass.

## Protected Dirty Boundary

The pre-existing dirty baseline contains 37 paths. They are protected and are not part of the Phase 6 Task 1 write-set. The only new path created by this task is:

- `docs/architecture-migration/evidence/phase-6-project-snapshot-save-boundary/baseline.md`

The pre-existing phase evidence directory and unrelated production/test changes remain untouched. No file was staged, committed, reset, reverted, cleaned, or overwritten.

The complete baseline-relative status allow-list, captured with
`git status --porcelain=v1`, is recorded below. Directory entries use Git's
aggregate `??` record; files created inside an already allow-listed evidence
directory remain within that directory's boundary.

```text
 M .opencode/commands/architecture-approve.md
 M .opencode/commands/architecture-draft.md
 M .opencode/commands/architecture-plan.md
 M .opencode/commands/architecture-resume.md
 M .opencode/commands/architecture-start.md
 M docs/architecture-migration/AGENTS.md
 D docs/architecture-migration/STATE.json
 M docs/architecture-migration/TASK_CONTEXT.md
 M docs/architecture-migration/evidence/phase-0.5-acceptance-v2.json
 D docs/architecture-migration/workflow/validate-state.mjs
 D docs/architecture-migration/workflow/validate-state.test.mjs
 M src/ViewModels/Hydraulics/CircuitsViewModel.cs
 M tests/SnowMeltingCalculator.Tests/Construction/ConstructionServiceTests.cs
 M tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/CalculationContextWriterAuthorityTests.cs
 M tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/CircuitsViewModelColdStartTests.cs
 M tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/ClimateToHydraulicsIntegrationTests.cs
 M tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/DoubleCalculationPreventionTests.cs
 M tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/GlycolAutoRecalculationTests.cs
 M tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/HydraulicsMultiplicityCharacterizationTests.cs
 M tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/PipeSpacingSynchronizationTests.cs
 M tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/ThermalToHydraulicsIntegrationTests.cs
 M tests/SnowMeltingCalculator.Tests/Services/Navigation/DialogServiceThreadAffinityTests.cs
 M tests/SnowMeltingCalculator.Tests/Services/Project/ClimateThermalInvalidationRegressionTests.cs
 M tests/SnowMeltingCalculator.Tests/Services/Project/ProjectLifecycleFlowCharacterizationTests.cs
 M tests/SnowMeltingCalculator.Tests/Services/Project/ThermalMultiplicityCharacterizationTests.cs
 M tests/SnowMeltingCalculator.Tests/ViewModels/Hydraulics/CircuitsViewModelEventLeakTests.cs
 M tests/SnowMeltingCalculator.Tests/ViewModels/Hydraulics/CircuitsViewModelTests.cs
 M tests/SnowMeltingCalculator.Tests/ViewModels/MainViewModelTests.cs
 M tests/SnowMeltingCalculator.Tests/ViewModels/ResetOrchestrationTests.cs
 M tests/SnowMeltingCalculator.Tests/ViewModels/ResultsViewModelOpenProjectTests.cs
 M tests/SnowMeltingCalculator.Tests/ViewModels/ResultsViewModelTestHelpers.cs
?? docs/architecture-migration/archive/STATE.json
?? docs/architecture-migration/evidence/phase-5.1-hydraulics-dirty-ownership-correction/
?? docs/architecture-migration/plans/phase-5.1-hydraulics-dirty-ownership-correction.draft.md
?? docs/architecture-migration/plans/phase-5.1-hydraulics-dirty-ownership-correction.md
?? docs/architecture-migration/evidence/phase-6-project-snapshot-save-boundary/
?? docs/architecture-migration/plans/phase-6-project-snapshot-save-boundary.md
```

The existing dirty preimage for
`tests/SnowMeltingCalculator.Tests/ViewModels/ResultsViewModelOpenProjectTests.cs`
is the Phase 5.1 protected hash
`C7CE2E5BDF50C5CCB020AFC5AC15C65AAF897CBCD97ADF9A4EA4F43DD17ACD3C`.
Task 2 is allowed to append characterization tests to that already-dirty path;
the prior constructor-wiring changes in the same file remain protected and
are not attributed to Phase 6.

## Fixture Inventory

The tracked `.smc` corpus was enumerated with `git -c core.quotePath=false ls-files --stage '*.smc'`. The readable fixture files were hashed with SHA-256 during baseline capture:

| Fixture | Bytes | SHA-256 |
|---|---:|---|
| `docs/architecture-migration/evidence/phase-4-thermal-state/final/F3/fixtures/project-a.smc` | 4813 | `5DA9B1E0E71B3B694560F0F4913BB6BEDC820FB6436F35EBFC363F457F7B6F84` |
| `docs/architecture-migration/evidence/phase-4-thermal-state/final/F3/fixtures/project-b.smc` | 3385 | `FBE377ABAB8A5D3A47086E23A5E4FFFA68B95EAEEE569DEE459CEB0235940882` |
| `docs/architecture-migration/evidence/phase-4-thermal-state/final/F3/fixtures/unknown-pipe.smc` | 4503 | `D6B580D0664208D0F92906C8EF28700A6A59C216FC244246A0EA922608DAB6B6` |
| `docs/architecture-migration/evidence/phase-4-thermal-state/final/F3/probes/fixture-corrupt/project-a.smc` | 3626 | `E1D02BC006D1EE15304EC3BEC41DE5F68D756BD27324D87D2A0E3D264119397A` |
| `docs/architecture-migration/evidence/phase-4-thermal-state/final/F3/probes/fixture-corrupt/project-b.smc` | 3385 | `FBE377ABAB8A5D3A47086E23A5E4FFFA68B95EAEEE569DEE459CEB0235940882` |
| `docs/architecture-migration/evidence/phase-4-thermal-state/final/F3/probes/fixture-corrupt/unknown-pipe.smc` | 3408 | `339E37F5AD33C1AE6555FEE9D661A6743FE2C051A256420450945C8CE81AEF42` |
| `docs/architecture-migration/evidence/phase-4-thermal-state/task-13/fixtures/project-a.smc` | 4812 | `69E083B6AD68F2A491AA72A617C71B104A3799C840B80AE08F6704C97A5C43C4` |
| `docs/architecture-migration/evidence/phase-4-thermal-state/task-13/fixtures/project-b.smc` | 3385 | `FBE377ABAB8A5D3A47086E23A5E4FFFA68B95EAEEE569DEE459CEB0235940882` |
| `docs/architecture-migration/evidence/phase-4-thermal-state/task-13/fixtures/unknown-pipe.smc` | 4503 | `DED9CADF2A5595748F5D2B27544F4A74BABC2E6A8071A8A58A8913EC46B2E505` |
| `docs/architecture-migration/evidence/phase-5-hydraulics-state/ui-qa/fixtures/project-a.smc` | 3626 | `E1D02BC006D1EE15304EC3BEC41DE5F68D756BD27324D87D2A0E3D264119397A` |
| `docs/architecture-migration/evidence/phase-5-hydraulics-state/ui-qa/fixtures/project-b.smc` | 4638 | `8A8A1C9767BF266B90E95D8CCC33FF663736CC69D37D3F5571D5B1B15510BF1A` |
| `docs/architecture-migration/evidence/phase-5-hydraulics-state/ui-qa/fixtures/unknown-pipe.smc` | 4663 | `FA0EDAAC71AEAC82AB84A3C119DFA2D1228D61C84B530EB47DE6836210ADB614` |
| `tests/SnowMeltingCalculator.Tests/Fixtures/v1-sample.smc` | 3626 | `E1D02BC006D1EE15304EC3BEC41DE5F68D756BD27324D87A2A0E3D264119397A` |

Additional tracked fixtures under the pre-existing Cyrillic `Тест/` directory were accounted for by Git blob IDs in the command output. Their path names were not repeated here because the PowerShell output encoding rendered them as escaped byte sequences; this is an environment/output concern, not a changed fixture. Task 6 must re-enumerate and hash the complete corpus using a byte-safe command before compatibility acceptance.

## Task 1 Decision

`BASELINE: PASS WITH FIXTURE-HASH FOLLOW-UP`

The save baseline, build, and targeted tests are green. Oracle terminal review is `VERDICT: APPROVE`. Task 2 characterization may start sequentially. Complete byte-safe full fixture hashing remains mandatory in Task 6 and is not silently treated as complete by this baseline.
