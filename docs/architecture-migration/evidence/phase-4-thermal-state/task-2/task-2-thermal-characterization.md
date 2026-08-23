# Task 2 — Thermal writers/subscribers/calculations/lifecycle/persistence characterization

Phase: `phase-4-thermal-state` · Todo 2 (frozen plan `docs/architecture-migration/plans/phase-4-thermal-state.md`, lines 366–374)
Base: branch `master`, HEAD `6a5a96f1763dd952c8d772ecd1d2536eb3b804cf` · Date: 2026-08-23
Verdict: **GREEN — all gates passed, zero production code changed.**

## 1. Deliverables

| Artifact | Path |
|---|---|
| Characterization suite (NEW, 41 cases) | `tests/SnowMeltingCalculator.Tests/Services/Project/ThermalMultiplicityCharacterizationTests.cs` |
| Negative-identity manifest (immutable for Todo 12/F3) | `docs/architecture-migration/evidence/phase-4-thermal-state/task-2/expected-negative-test-identities.json` |
| Determinism fix in existing suite (see §6) | `tests/SnowMeltingCalculator.Tests/Thermal/ThermalViewModelTests.cs` (9 methods, awaited idiom) |
| Task-owned allowed-hunks manifest (post-verify input) | `docs/architecture-migration/evidence/phase-4-thermal-state/task-2/allowed-hunks.json` |
| TRX + parsed identities | `task-2/TestResults/*.trx`, `task-2/trx-*.json` |
| Protected baseline receipts | `task-2/protected-pre.json`, `task-2/protected-post.json` |

## 2. Gate results (commands verbatim, PowerShell 5.1, repo root `D:\IA\3ace v.2`)

| Gate | Command (abbreviated paths) | Exit | Result |
|---|---|---|---|
| G0 preflight | `git rev-parse --show-toplevel` / `--branch` / HEAD; `verify-protected-baseline.ps1 -Baseline …task-1/baseline-manifest.json -AllowedHunks …todo-1-allowed-hunks.json … -Output …task-2/protected-pre.json` | 0 | root=`D:/IA/3ace v.2`, branch=master, HEAD=base; `drift=7 protected_mismatch_count=0` |
| G1 build | `dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Release --nologo` | 0 | 0 warnings, 0 errors |
| G2 happy QA | `dotnet test … -c Debug --filter "FullyQualifiedName~ThermalMultiplicityCharacterizationTests" --logger "trx;LogFileName=phase-4-characterization-debug.trx" --results-directory …task-2/TestResults` | 0 | **41/41 passed, failed=0** |
| G3 V3 upstream | `dotnet test … -c Release --no-build --filter "FullyQualifiedName~ClimateThermalInvalidationRegressionTests\|FullyQualifiedName~ConstructionThermalInvalidationRegressionTests" --logger "trx;LogFileName=phase-4-upstream-invalidation.trx" …` | 0 | **20/20 passed, failed=0** |
| G4 full Release | `dotnet test … -c Release --no-build --logger "trx;LogFileName=task-2-full-release.trx" …` — run **twice**, both green | 0 / 0 | **1780 total / 1777 passed / failed=0 / NotExecuted=3**; second run identical |
| Manifest reconcile | parse-trx on full-release + set comparison vs `expected-negative-test-identities.json` | — | groups 4/3/2, pairwise disjoint, non-empty, every identity `Passed` in full TRX |
| NotExecuted reconcile | parse-trx output | — | exactly `{CircuitsBaselineTests.RegenerateCircuitsBaseline, ThermalBaselineTests.RegenerateBaseline, ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile}` = Todo-1 baseline set |
| G5 post-verify | `verify-protected-baseline.ps1 -Baseline …task-1/baseline-manifest.json -AllowedHunks …task-2/allowed-hunks.json -EvidenceRoot … -Output …task-2/protected-post.json` | 0 | `drift=10 protected_mismatch_count=0 allowed_hunk_count=2` |

TRX SHA-256:

```
61E7E06AE07BE9DD136169764D0765B5E9F6FEB2FA0DAAB0528CCE05BA172CC1  TestResults/phase-4-characterization-debug.trx
CAA4B995FC727726241DC55425378B45A738027601B6B63F2BE8436ACFB24446  TestResults/phase-4-upstream-invalidation.trx
5D5390ED39223A4B9025F8102902429E6753DC59511DFB3F086246C6B9A589A3  TestResults/task-2-full-release.trx
9FB3920C664618ABDFF53F490B82324C340E8C21E4B5133C83398983F5A5F948  expected-negative-test-identities.json
0ABB26A7247DDF4D01C04DE86CA6B565C8B160BE41EF2D8FC3DBB767C62AFFA2  allowed-hunks.json
```

## 3. Scenario × counter matrix (41 executed cases)

Counters per case: final values; dirty-INTENT (thermal-scoped `IMarkDirtyService`) vs observable `IsDirty` transitions (`ProjectSession.PropertyChanged`); `StateChanged(Thermal)` sequence+message; `PipeSpacingChanged`; `CalculationContext` publication order (`ThermalInputs`→`ThermalResult`); calculator invocations; Hydraulics summary computations (measured unit: one logical `CalculateAllCollectors()` = **2** `CalculateCollectorSummary` calls — collector summary + summary-card rebuild); Results refresh/`ProjectChanged`; subscription balance.

| # | Scenario (DEC) | Key frozen counters/outcomes |
|---|---|---|
| 1–4 | Mode/Supply/Ground/Pipe changed, result present (T03) | intent 1, transition 1, states `[NeedsRecalculation]` with exact RU message, result preserved, calculator 0, context 0, hydraulics Δ0 |
| 5–8 | Changed input, result absent (T03) ×4 params | intent 1, transition 1, states ∅, NeedsRecalculation=false, result null |
| 9 | No-op edits with result (T03) | all counters Δ0 |
| 10 | Second edit while dirty (T03) | intent 2 cumulative, transitions still 1 (idempotent MarkDirty), states `[NeedsRecalculation]` |
| 11 | Edits never invoke calculator/publish context (T03) | calculator 0, context ∅, result preserved |
| 12 | User reset (T03) | defaults+result cleared; intent Δ0, events 0; **legacy: session stays dirty, service status/spacing untouched** |
| 13 | Lifecycle ResetModules (T03/T04) | thermal silent: intent Δ0, states ∅, context ∅; **legacy: service spacing store untouched** |
| 14–15 | Climate user invalidation ±result (T04) | with result: cleared once, exact message, intent 0; without: silent |
| 16–17 | Construction user invalidation ±result (T04) | symmetric to climate |
| 18 | Valid calculate (T05) | context order `[TI,TR]`, calculator 1, states `[Calculating,Actual]`, no dirty, hydraulics Δ2 (=1 logical), messages cleared |
| 19 CF | Invalid input (T05) | calculator 0, context ∅, phase unchanged, result preserved, message = validator text |
| 20 CF | Calculator exception (T05) | `Ошибка расчёта: {msg}` exact, Result null, synthetic invalid TR published once, states `[Calculating,Actual]`, hydraulics 0 |
| 21 CF | Reentrant Calculate (T05) | TCS-gated: second ExecuteAsync no-op; calculator 1 total, single TI/TR pair, hydraulics Δ2 |
| 22 CF | Calculator-returned invalid result (T05) | stored canonically, published once, phase Actual, hydraulics 0 |
| 23 | Pipe structural equality (T08/T01) | Article/ThermalConductivity ignored, name case-insensitive, WallThickness significant |
| 24 | Spacing user edit fresh graph (T06) | event 1, circuit `spacing/10`=25.0, hydraulics 2 (=1 logical), intent 1, no recalc status w/o result |
| 25 | Spacing user edit with result (T06) | exact message, intent 1, result preserved |
| 26 | Service SetPipeSpacing changed/no-op (T06) | event 1 then 0; hydraulics 2 |
| 27 | Non-canonical direct writer (T06/QA) | `InvalidOperationException`, value unchanged, 0 events |
| 28 | Restore source under/out of guard (T06) | throws w/o lease; applies +1 event under lease |
| 29 | Restore valid saved result (T08) | calculator 0, states ∅, pipe SameAs standard[1], context `[TI,TR]`, clean, ProjectChanged 1 |
| 30 | Restore absent result (T08) | fallback calculator 1, valid fallback published, states `[Calculating,Actual]`, clean |
| 31 PF | Restore invalid saved result (T08) | calculator 1, saved 999 not final, canonical = fallback 555 valid |
| 32 PF | Unknown persisted pipe (T08) | falls back to first standard (SameAs StandardPipes[0]), calculator 0 |
| 33 | Null persisted pipe after lifecycle reset (T08) | pipe stays null, calculator 0, result restored |
| 34 | Missing legacy spacing via real persistence path (T08) | stripped-JSON DTO default 200 → restore emits exactly 1 spacing event (300→200) |
| 35 PF | Corrupt .smc open (real ProjectFileService) | dialog `Не удалось открыть проект: Ошибка десериализации…`, path not set, prior thermal state intact, calculator 0 |
| 36 | Second project load (T08) | inputs/spacing/pipe fully replaced by B; **characterized legacy defect: A's valid result survives when B has none (fallback skipped)** — see §5 |
| 37 | Repeated load/reset cycles (T08) | per-cycle deltas equal for states/context/spacing/dirty/ProjectChanged/calculator; hydraulics delta₂ = delta₁+2 (**stale-result surplus**, see §5); probe edit afterwards: 1 event, Δ2 hydraulics — zero subscription multiplication |
| 38 RF | Early restore failure (T08) | lease cleared, thermal defaults intact, calculator 0, clean |
| 39 RF | Late restore failure (T08) | lease cleared, thermal retains pre-failure defaults, clean |
| 40 QA | Synthetic direct writer | extra completion detected: exact-sequence assertion throws `AssertionException` on violating recording, passes canonical |
| 41 QA | Duplicate subscriber | doubled completion detected by harness; after unsubscribe contract holds again |

CF=`[Category("CalculationFailure")]` ×4 · PF=`[Category("PersistenceFailure")]` ×3 · RF=`[Category("RestoreFailure")]` ×2.

## 4. Negative-identity manifest (closed after Todo 2)

`expected-negative-test-identities.json` (UTF-8 no BOM), FQ prefix `SnowMeltingCalculator.Tests.Services.Project.ThermalMultiplicityCharacterizationTests.`:

- `CalculationFailure` (4): Calculate_InvalidInput_ZeroCalculatorZeroContextPhaseUnchanged; Calculate_Exception_SetsExactErrorMessageNullResultAndInvalidContextPublication; Calculate_ReentrantWhileCalculating_PerformsNoSecondCalculatorHit; Calculate_CalculatorReturnedInvalidResult_StoredCanonicallyPublishedOnceZeroHydraulics
- `PersistenceFailure` (3): Restore_InvalidSavedResult_CalculatorOnceInvalidResultNotFinalCanonical; Restore_UnknownPersistedPipe_FallsBackToFirstStandardPipe; LoadCorruptProjectFile_ShowsErrorKeepsPriorProjectAndThermalStateUntouched
- `RestoreFailure` (2): LoadProjectDataAsync_EarlyRestoreFailure_ClearsLeasePreservesPartialThermalDefaults; LoadProjectDataAsync_LateRestoreFailure_ClearsLeaseThermalRetainsPreFailureDefaults

Groups non-empty, pairwise disjoint, no duplicates; every identity discoverable and `Passed` in the full-release TRX (Todo 12/F3 filter reconciliation will match exactly).

## 5. Characterized divergences & notes for later todos (not blockers; behavior frozen as-is)

1. **Second-load stale result** (`SecondProjectLoad_ReplacesProjectAInputsButKeepsStaleResultUntilTodo9`): when project B has no saved Thermal result, orchestrator finalization takes the `LoadResult` branch on project A's still-populated `Result` — A's result survives into B and the fallback calculation is skipped (calculator 0). DEC-T08 requires zero stale values; **Todo 9 must eliminate this** while keeping every other count on this page. Same root cause produces the characterized `+2` hydraulics surplus in cycle 2 of `RepeatedLoadResetCycles`.
2. **Reset does not touch service-side stores**: user reset and lifecycle reset leave `_thermalNeedsRecalculation/_thermalValidationMessage/_pipeSpacing` unchanged (ST-013/ST-015 seams). Frozen as current behavior.
3. **One logical `CalculateAllCollectors()` = 2 `CalculateCollectorSummary` invocations** (collector summary + summary-card rebuild). All downstream counts use this unit.
4. No contradiction with DEC-T01..T08 preservation clauses was found beyond items 1–2 above; exact Russian cause messages, restore order/fallbacks, pipe equality semantics and Phase 3.1 load-silence all match the plan's preservation requirements.

## 6. Justified edit to existing test file (allow-list line 368)

`ThermalViewModelTests.Validate_*` (9 methods) used `CalculateCommand.Execute(null)` + immediate asserts. Empirically (probe: `isCalculating=True` right after `Execute` returned) `AsyncRelayCommand.Execute` does not wait for completion — the pattern is inherently racy and lost deterministically once any preceding suite section exercises the real downstream chain (valid ThermalResult publications heat the thread pool between command start and assert). Fix: converted these methods to `await CalculateCommand.ExecuteAsync(null)` — the same proven deterministic idiom used across green suites (e.g. `ConstructionThermalInvalidationRegressionTests`). Assertion contracts unchanged; nothing weakened/skipped; failure pre-fix reproduced cheaply via pair filter and disappeared post-fix.

## 7. Production writer/subscriber inventory

Full inventory with file:line bindings and DEC mapping is embedded as the header comment of `ThermalMultiplicityCharacterizationTests.cs` (W1–W6 writers, S1 subscribers; ST-012..ST-015, ST-021..ST-022 coverage). Summary: ThermalViewModel property handlers/Calculate/Reset/LoadResult/upstream handlers/ctor subscriptions; CalculationStateService thermal+spacing stores and writer-authority guard; CalculationContext UpdateThermal*/UpdateClimate/UpdateConstruction/Reset; ProjectLoadOrchestrator restore inputs/pipe/result/finalization + ResetModules; ResultsViewModel save projection + load lease + open error boundary; MainViewModel.PerformNewCalculationReset; CircuitsViewModel consumer subscriptions (StateChanged/PipeSpacingChanged/ContextChanged).

## 8. Worktree confirmation

`git status --porcelain` contains ONLY: the new allow-listed test file, the modified allow-listed `ThermalViewModelTests.cs`, and additions under `docs/architecture-migration/evidence/phase-4-thermal-state/task-2/` (+ pre-existing Todo-1 evidence/scripts already present at gate G0). Zero production (`src/**`) changes; no git staging/commit/reset performed.
