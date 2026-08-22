# Phase 3 Task 12.1 canonical default Construction initialization

Date: 2026-08-19

Task 12.1 closes the cold-start/reset gap where the Construction adapter could
show defaults while `ProjectSession.ConstructionState` remained empty. One
application-boundary initializer now owns the seven-layer recipe, and startup,
NewCalculation, and project pre-load reset use that initializer over the same
session state. This receipt records the final Task 7 gates after repairing stale
test fixture seams; it releases only parent Task 13.

## Environment

- Repository root: `D:/IA/ace v.2`
- HEAD: `e655735dfa66c00cf9c53be93d511eda8989e8bf`
- Branch: `master`, upstream `origin/master`
- Staged paths: `0`
- OS: Windows 10.0.19045, `win-x64`
- .NET SDK: `8.0.418` (`5854a779c1`)
- MSBuild: `17.11.48+02bf66295`
- Runtime: `.NET 8.0.24`, x64
- `global.json`: absent
- Environment log:
  `tests/SnowMeltingCalculator.Tests/TestResults/phase-3-task-12-1-dotnet-info.log`

## Fixture correction

The first full Release attempt exposed nine stale fixture failures rather than
production regressions. Eight were in `ConstructionServiceTests`; one was in
`DialogServiceThreadAffinityTests`. Their private helpers now share one
`ProjectSession`, its `ConstructionState`, one repository-backed
`ConstructionDefaultStateInitializer`, and a session-backed
`CalculationStateService` across `ConstructionViewModel`, `ResultsViewModel`,
and `ProjectLoadOrchestrator`.

Custom material and template behavior remains intact. Synthetic catalogs are
supplemented with missing canonical IDs instead of being replaced, and
`GetMaterialById` resolves both custom payload materials and required default
materials. No assertion was weakened or skipped, and no production source was
edited during this fixture correction.

The focused Release reproduction selected the exact nine prior failures:

```powershell
dotnet test "tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Release --filter "FullyQualifiedName~ProjectData_LayerOrder_RoundTrip_PreservesLambdaE|FullyQualifiedName~ProjectData_Load_v1_0_MigratesAbovePipeOrder|FullyQualifiedName~ProjectData_Load_ReindexesOrder|FullyQualifiedName~ResultsViewModel_SaveProject_UsesDialogServiceNotProjectFileServiceForPath|FullyQualifiedName~ProjectData_Load_ImportsCustomMaterialsBeforeLayers|FullyQualifiedName~ProjectRoundTrip_CustomTemplateSurvives|FullyQualifiedName~ProjectData_Save_v1_1_SetsVersion|FullyQualifiedName~ProjectData_CustomTemplates_RoundTrip|FullyQualifiedName~ProjectData_CustomMaterials_RoundTrip" --logger "trx;LogFileName=phase-3-task-12-1-full-release-nine-repro.trx" --results-directory "tests\SnowMeltingCalculator.Tests\TestResults"
```

- Exit: `0`
- TRX: `9 total / 9 executed / 9 passed / 0 failed / 0 notExecuted`
- Non-passed identities: none

## Contract and build gates

### Exact contracts

The exact Task 6 filter from the approved correction plan was rerun in Debug.

- Exit: `0`
- Console: `117 passed / 1 skipped / 0 failed / 118 total`
- TRX counters: `118 total / 117 executed / 117 passed / 0 failed /
  0 notExecuted`
- Result-list `NotExecuted` identity:
  `ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`
- This is the accepted absent external-fixture skip from the prior receipts.

### Debug build

```powershell
dotnet build "src\SnowMeltingCalculator.csproj" -c Debug --nologo
```

- Exit: `0`
- Warnings: `0`
- Errors: `0`

### Release build

```powershell
dotnet build "src\SnowMeltingCalculator.csproj" -c Release --nologo
```

- Exit: `0`
- Warnings: `0`
- Errors: `0`

The focused Release test and exact Debug contracts command also compiled the
test project in their respective configurations after the final fixture edits.

## Affected Debug gate

The exact live-discovered Task 12 filter from
`task-12-executable-gates.md` was rerun with
`CanonicalDefaultConstructionLifecycleTests` added, using `--no-build`.

- Exit: `0`
- Console: `312 passed / 1 skipped / 0 failed / 313 total`
- TRX counters: `313 total / 312 executed / 312 passed / 0 failed /
  0 notExecuted`
- Result-list `NotExecuted` identity:
  `ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`
- No new failure or skip identity occurred.

## Full Release gate

```powershell
dotnet test "tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Release --no-build --logger "trx;LogFileName=phase-3-task-12-1-full-release.trx" --results-directory "tests\SnowMeltingCalculator.Tests\TestResults"
```

- Exit: `0`
- Console: `1711 passed / 1 skipped / 0 failed / 1712 total`
- TRX counters: `1714 total / 1711 executed / 1711 passed / 0 failed /
  0 notExecuted`
- Result-list `NotExecuted` identities:
  - `RegenerateCircuitsBaseline`
  - `RegenerateBaseline`
  - `ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`
- The two regeneration tests are pre-existing explicit baseline generators;
  the Results test is the accepted absent external-fixture skip.

As in earlier receipts, VSTest console totals and TRX aggregate counters do not
count explicit/ignored results identically. The result list above is recorded
without guessing a normalized value. There are no failed, error, timeout, or
aborted results.

## Behavioral and source guards

- Canonical lifecycle tests prove the exact ordered `1 + 6` recipe exists in
  the adapter, canonical snapshot, persistence DTO, and thermal-facing
  projection immediately after startup and reset.
- Immediate save retains `.smc` version `1.1`; immediate Thermal calculation
  consumes the same canonical projection and matches its control calculation.
- Missing required catalog IDs fail before state/adapter mutation.
- `ConstructionViewModel.SyncToCanonicalState()` has no save/Results caller.
- `ConstructionViewModel` contains no direct `MarkDirty(` or
  `UpdateConstruction(` call.
- `BuildResetConstructionSnapshot` and `AddDefaultLayer` no longer exist in
  production source; the reset recipe is centralized in
  `ConstructionDefaultStateInitializer`.
- `new ProjectSessionConstructionState(` remains only in `ProjectSession` and
  the legacy-compatible ViewModel fallback; production DI uses the session
  state.
- Parent Phase 3 Task 13 and F1-F4 remain unchecked. No map, shared model,
  widget source, or generated widget was edited for Task 12.1.

## Protected worktree conclusion

- HEAD remains `e655735dfa66c00cf9c53be93d511eda8989e8bf`.
- The staged stream remains empty.
- Task 1 preimages classify the expected Task 12.1 production/test changes;
  unrelated baseline entries checked during the final gate remain unchanged.
- The final fixture correction changes only the two owner-approved test files.
- No commit, stage, push, checkout, reset, restore, clean, or stash operation
  was performed.
- C# LSP diagnostics again failed at the external harness boundary with
  `LSP file path must be inside request cwd`; successful Debug/Release
  compilation and test execution are authoritative.

## Artifact manifest

| Artifact | Bytes | SHA-256 |
|---|---:|---|
| `phase-3-task-12-1-dotnet-info.log` | 2134 | `1F09C4A36F8B3ACDF461B3748E8DC83F589323766AC81377FAF49FCB84D79E3F` |
| `phase-3-task-12-1-full-release-nine-repro.trx` | 13359 | `C85ADE162AEE19E94F32DCB1988E4B6FACBF6A1FBA56AD1B6272FCAF40016D59` |
| `phase-3-task-12-1-full-release-nine-repro.log` | 2694 | `C4212A42B9F298D2A431E8DB39907B72659325BA4510C599B8A0FDD65EA15DCD` |
| `phase-3-task-12-1-contracts.trx` | 162328 | `79E29CA134C00E721FB94D8846D163EDE9D2A097A96EE5311813608A9017BB47` |
| `phase-3-task-12-1-contracts.log` | 2818 | `8C20209B3A47903B9BFF740BA19A3578DDD8C0A09889B35B6E5D47DD8DF81AC6` |
| `phase-3-task-12-1-build-debug.log` | 842 | `414332E67541043F0CB5B5EDDDD787A35E4F3B7FD63C8ADBCD287077EB9CF139` |
| `phase-3-task-12-1-build-release.log` | 846 | `84C28149E22BA15F9D70DA4275B306B4C3BC47394BBBBDFA7EA54E707FE3312E` |
| `phase-3-task-12-1-affected-debug.trx` | 419025 | `5685B8A954D4C045EDEA1BCF35078076C174EBAA1AC5EB48D0DE2A0369DD9F0F` |
| `phase-3-task-12-1-affected-debug.log` | 2840 | `39F5E52C26BD49867B32E9C9CF53743479CDE52C5E7E08A19E8AEB7268400701` |
| `phase-3-task-12-1-full-release.trx` | 2253099 | `CC1B43027046E7D5EA08B86FEC248F288EF023790A5D92D99F0579D840951009` |
| `phase-3-task-12-1-full-release.log` | 2594 | `F9FE6643494436DAB5706245E7EAC765181D759FB1FE935EA968DE178E91CDA5` |

Task 12.1 is GREEN. This releases only parent Task 13 architecture dossier
refresh. Phase 3 remains executing, its result acceptance remains pending, and
the parent Final Verification Wave has not started.

VERDICT: PASS
