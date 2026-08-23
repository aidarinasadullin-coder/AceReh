# F3 — Executable QA / User Risk Domain Receipt

REVIEW_ID: f3-executable-phase-4-thermal-state
SUBJECT: phase-4-thermal-state@327B1288B8072E7A76D814F8439ECBC353F54F4CE59AB2B507595A08980B4C02
RECEIPT: inline (this document)
VERDICT: APPROVE
REASON: All executable/user-risk gates green against the frozen binaries: full Release suite 1946 total / 1943 passed / 0 failed / NotExecuted == exactly the three baseline identities; strict negative-category equality CF=4/PF=6/RF=3 (AMZ-3 manifest); UI QA harness ten steps + failure branch PASS (87+11 assertions, 0 failed); all five plan-line-519 failure probes reject correctly; V13 before/after four-hash sets byte-identical. The single initially-blocking finding — plan line 289's directory parse being unsatisfiable because category-lane TRX subsets necessarily overlap `f3-full-release.trx` under the Todo-1 cross-file duplicate rule — was resolved by owner decision AMZ-4 (2026-08-23, journaled in TASK_CONTEXT.md): `parse-trx.ps1` directory mode now deduplicates benign cross-file overlaps whose outcomes agree while still rejecting conflicting outcomes, within-file duplicates, zero-test and malformed inputs (fixture-proven under `final/f3/amz4/`). The mandated artifact `final/f3/trx-identities.json` exists: merged totals 1946 total / 1943 passed / 0 failed / 3 notExecuted across all four files. Original REJECT analysis preserved verbatim in §5 for provenance.

## 1. Gate matrix

All commands from repo root `D:\IA\3ace v.2`, sequential hosts only, no rebuilds (`--no-build` only), no git operations. `<ev>` = `docs/architecture-migration/evidence/phase-4-thermal-state`.

| # | Check | Command (abridged) | Exit | Duration | Result |
|---|---|---|---|---|---|
| 1 | V13-before | `pwsh -NoProfile -File <ev>/verify-frozen-release.ps1 -Manifest <ev>/frozen-release-sha256.json -Lane F3 -Moment Before` | **0** | ~2 s | artifacts=4, manifest sha `6D039FC7…B13BA72` → `final/f3/frozen-hashes-before.json` |
| 2a | Full Release suite | `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Release --no-build --logger "trx;LogFileName=f3-full-release.trx" --results-directory <ev>/final/f3/TestResults` | **0** | 31 s | failed 0, passed 1943, total 1946 (TRX) |
| 2b | CalculationFailure lane | same + `--filter "TestCategory=CalculationFailure"` → `f3-calculation-failure.trx` | **0** | 1 s | 4 total / 4 passed / 0 failed |
| 2c | PersistenceFailure lane | same + `--filter "TestCategory=PersistenceFailure"` → `f3-persistence-failure.trx` | **0** | 1 s | 6 total / 6 passed / 0 failed |
| 2d | RestoreFailure lane | same + `--filter "TestCategory=RestoreFailure"` → `f3-restore-failure.trx` | **0** | 1 s | 3 total / 3 passed / 0 failed |
| 3a | Reconcile CF | `assert-trx-identities.ps1 -InputFile …f3-calculation-failure.trx -ExpectedManifest <ev>/task-2/expected-negative-test-identities.json -ExpectedGroup CalculationFailure -Output final/f3/calculation-failure-identities.json` | **0** | <1 s | expected=4 matched=4 status=ok |
| 3b | Reconcile PF | same, PersistenceFailure → `final/f3/persistence-failure-identities.json` | **0** | <1 s | expected=6 matched=6 status=ok |
| 3c | Reconcile RF | same, RestoreFailure → `final/f3/restore-failure-identities.json` | **0** | <1 s | expected=3 matched=3 status=ok |
| 4 | Directory parse | `parse-trx.ps1 -InputDirectory <ev>/final/f3/TestResults -Output final/f3/trx-identities.json` | **0** ✓ | <1 s | post-AMZ-4 rerun: merged totals 1946/1943/0/3 across four files (category-subset overlap deduplicated) → `final/f3/trx-identities.json` written — see §5 |
| 5a | Fixtures | `prepare-ui-fixtures.ps1 -Source tests/SnowMeltingCalculator.Tests/Fixtures/v1-sample.smc -OutputDirectory <ev>/final/f3/fixtures` | **0** | ~1 s | a=`E1D02BC0…`, b=`FBE377AB…`, u=`D7BA538E…` + `fixture-manifest.json` |
| 5b | UI QA harness | `run-wpf-ui-qa.ps1 -Executable src/bin/Release/net8.0-windows/win-x64/SnowMeltingCalculator.exe -ExpectedExecutableSha256File <ev>/frozen-release-sha256.json -ProjectA …/fixtures/project-a.smc -ProjectB …/fixtures/project-b.smc -InvalidProject …/fixtures/unknown-pipe.smc -OutputDirectory <ev>/final/f3/ui-qa` | **0** | 37 s | PASS — ten steps + failure branch; see §7 |
| 6 | V13-after | `-Lane F3 -Moment After` | **0** | ~2 s | four-hash set IDENTICAL to before (§9) |
| — | Probes A–E (plan line 519) | see `probes/probe-log.md` | 3/3/3/3/**1** (all reject) | <10 s | every probe rejects without touching source fixture or frozen build |

## 2. Full-suite totals vs expectation

| Metric | Expected | Observed (TRX `f3-full-release.trx`) | Verdict |
|---|---|---|---|
| Total | ≈1946 | **1946** | MATCH |
| Passed | 1943 | **1943** | MATCH |
| Failed | 0 | **0** | MATCH |
| NotExecuted count | 3 | **3** | MATCH |

NotExecuted identities (exact, from per-file parse `probes/trx-perfile/f3-full-release.json`):

1. `SnowMeltingCalculator.Tests.RefactorBaseline.CircuitsBaselineTests.RegenerateCircuitsBaseline`
2. `SnowMeltingCalculator.Tests.RefactorBaseline.ThermalBaselineTests.RegenerateBaseline`
3. `SnowMeltingCalculator.Tests.ViewModels.ResultsViewModelOpenProjectTests.ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`

This set equals the required baseline identity set EXACTLY — no NotExecuted drift. (Console summary prints total 1944/skipped 1 because VSTest counts NUnit explicit work differently; the TRX records 1946 `UnitTestResult` rows with 3 `NotExecuted` — the authoritative form used here.)

## 3. Negative-lane reconciliation (strict identity equality)

Expected-manifest file: `<ev>/task-2/expected-negative-test-identities.json` (AMZ-3-authoritative closed set CF=4 / PF=6 / RF=3, pairwise disjoint). Verifier: `assert-trx-identities.ps1` — strict SET equality, rejects non-Passed outcomes, absent/unexpected/duplicate identities, empty groups.

| Group | Expected (manifest) | Actual (TRX) | Strict equality | Unexpected identities | Duplicates | Non-Passed | Output artifact |
|---|---|---|---|---|---|---|---|
| CalculationFailure | 4 | 4 | **TRUE** (exit 0, matched=4) | 0 | 0 | 0 | `final/f3/calculation-failure-identities.json` |
| PersistenceFailure | 6 | 6 | **TRUE** (exit 0, matched=6) | 0 | 0 | 0 | `final/f3/persistence-failure-identities.json` |
| RestoreFailure | 3 | 3 | **TRUE** (exit 0, matched=3) | 0 | 0 | 0 | `final/f3/restore-failure-identities.json` |

Reconciled identities (verbatim from the output JSONs):

- **CalculationFailure (4):** `…ThermalMultiplicityCharacterizationTests.Calculate_CalculatorReturnedInvalidResult_StoredCanonicallyPublishedOnceZeroHydraulics`; `…Calculate_Exception_SetsExactErrorMessageNullResultAndInvalidContextPublication`; `…Calculate_InvalidInput_ZeroCalculatorZeroContextPhaseUnchanged`; `…Calculate_ReentrantWhileCalculating_PerformsNoSecondCalculatorHit`
- **PersistenceFailure (6):** `…ThermalMultiplicityCharacterizationTests.LoadCorruptProjectFile_ShowsErrorKeepsPriorProjectAndThermalStateUntouched`; `…Restore_InvalidSavedResult_CalculatorOnceInvalidResultNotFinalCanonical`; `…Restore_UnknownPersistedPipe_FallsBackToFirstStandardPipe`; `…ResultsViewModelOpenProjectTests.PersistenceFailure_FailedFileOperation_PreservesErrorStateWithoutSchemaDrift`; `…PersistenceFailure_MissingOrCorruptSavedResult_FallbackOnce_InvalidNeverCanonical`; `…PersistenceFailure_UnknownPipe_FallsBackToFirstStandard_NoSchemaDrift`
- **RestoreFailure (3):** `…ProjectLifecycleFlowCharacterizationTests.RestoreModulesFromProjectAsync_ThermalBoundaryException_ClearsLeaseAndPreservesPartialState`; `…ThermalMultiplicityCharacterizationTests.LoadProjectDataAsync_EarlyRestoreFailure_ClearsLeasePreservesPartialThermalDefaults`; `…LoadProjectDataAsync_LateRestoreFailure_ClearsLeaseThermalRetainsPreFailureDefaults`

## 4. Subset/disjointness proofs (supplementary, `probes/trx-perfile/` + `probes/category-only-identities.json`)

- Every category-lane identity exists in the full-suite TRX with outcome `Passed`: missing=0, non-Passed=0 for CF(4), PF(6), RF(3). Hence the step-4 collision set is exactly those 13 identities — nothing else duplicates.
- Cross-category intersections: CF∩PF=0, CF∩RF=0, PF∩RF=0.
- `parse-trx.ps1 -InputDirectory` over ONLY the three category TRX copies (`probes/category-only/`): **exit 0**, 13 unique identities, all Passed — proving the tool works and the sole obstruction to the step-4 command is the full-suite overlap inherent to subset lanes sharing one results directory.

## 5. FINDING-1 (RESOLVED-AMZ4) — plan line 289 directory parse (original analysis preserved verbatim)

- Command executed verbatim from repo root; original exit **3**; post-correction rerun exit **0** (merged totals 1946/1943/0/3 across four files, category-subset overlap deduplicated).
- Rejection: duplicate identity across files — first collision `SnowMeltingCalculator.Tests.Services.Project.ThermalMultiplicityCharacterizationTests.Calculate_CalculatorReturnedInvalidResult_StoredCanonicallyPublishedOnceZeroHydraulics` (present in both `f3-calculation-failure.trx` and `f3-full-release.trx`).
- Root cause: `parse-trx.ps1` (Todo 1) shares one `$seen` identity set across all `.trx` files of `-InputDirectory` and fails closed on any repeat ("rejects missing input, zero tests, duplicate identities and malformed XML" — restated at plan line 320). Plan lines 282–285 write four TRX files into one directory where the three filtered lanes are subsets of the full suite, so line 289 can never exit 0. The frozen plan is self-contradictory on this step.
- Consequence: the planned artifact `final/f3/trx-identities.json` was not created (the verifier writes output only on success). This is the single missing artifact and the sole basis of this REJECT.
- Not a product signal: zero test failures, zero NotExecuted drift, zero unexpected identities anywhere; the frozen binaries are identical before/after (§9).
- Minimal correction shape (for the owner/planner; NOT executed by this lane at REJECT time): amend plan line 289 to parse the three category TRX files individually or as a category-only directory (demonstrated exit 0), or extend `parse-trx.ps1` with an explicit dedup-aware directory mode; then rerun the complete F1→F4 chain per "Any correction invalidates all prior F1-F4 receipts and reruns the complete sequential final chain."
- Resolution (owner decision **AMZ-4**, 2026-08-23, journaled in TASK_CONTEXT.md): the second correction shape was adopted in its minimal form — `parse-trx.ps1` gained an AMZ-4 dedup-aware directory mode (cross-file overlap whose outcomes AGREE is counted once; conflicting outcomes for one identity, within-file duplicates, zero-test/malformed inputs still fail closed; proven by fixtures under `final/f3/amz4/fixtures`: benign overlap exit 0/deduped, outcome conflict exit 3, within-file duplicate exit 3, single-file regression totals unchanged). The plan-line-289 command was re-executed verbatim and now exits 0, writing `final/f3/trx-identities.json` (merged totals 1946/1943/0/3 across four files). The full-chain rerun was explicitly scoped down by the same decision: F1/F2 lanes never exercise directory-parse and the frozen manifest covers exe/productDll/testDll/plan only, so an evidence-script edit cannot drift those hashes; F1/F2 receipts stand.

## 6. Plan-line-519 failure probes (all reject; `probes/probe-log.md`, builder `probes/build-probes.ps1`)

| Probe | Artifact | Exit | Rejection class |
|---|---|---|---|
| Zero-test TRX | `probes/trx-zero-test.trx` | 3 | `zero tests … (empty TRX)` |
| Unexpected identity | `probes/trx-unexpected.trx` | 3 | `unexpected identities … (1): SnowMeltingCalculator.Tests.Probes.UnexpectedIdentityProbe` |
| Duplicate identity | `probes/trx-duplicate.trx` | 3 | `duplicate test identity '…Calculate_Exception_SetsExactErrorMessageNullResultAndInvalidContextPublication'` |
| Corrupted expected selector | `probes/corrupted-expected-manifest.json` (one CF identity removed) | 3 | removed identity becomes `unexpected` |
| Corrupted copied unknown-pipe expectation | `probes/fixture-corrupt/unknown-pipe.smc` (+ manifest, sibling fixtures) | 1 | harness step 1 SHA mismatch (`expected D7BA538E…, observed 339E37F5…`) BEFORE any process launch |

No source fixture, frozen binary, or canonical task artifact was touched by any probe.

## 7. Isolated UI QA flow (agent-operated real user flows)

The UI-QA harness (`run-wpf-ui-qa.ps1`) exited **0**, result **PASS** — ten steps + failure branch green. Raw evidence: `final/f3/ui-qa/observations.json` (result=PASS, 10 steps, **87 assertions, 0 failed**), `final/f3/ui-qa/failure-observations.json` (**11 assertions, 0 failed**), harness-generated `final/f3/ui-qa/task-13-user-flow-qa.md`. Assertion totals are identical to the implementation-time task-13 run (87+11).

Process records (exe SHA `BE36766A…335D` validated before AND after every launch, all matches; stdout/stderr logs present and clean for every run):

| Run tag | Project | PID | Exit | stderr |
|---|---|---|---|---|
| a-edit-save | project-a.smc | 8580 | 0 | clean (no crash patterns) |
| a-relaunch | project-a.smc | 7368 | 0 | clean |
| b-load-reset | project-b.smc | 25148 | 0 | clean |
| unknown-pipe | unknown-pipe.smc | 4800 | 0 | clean |

Ten-step summary (assertions → artifacts):

| Step | Scope | Assertions | Artifacts |
|---|---|---|---|
| 1 | fixture-manifest + 3 input SHAs | 3 | `fixtures/fixture-manifest.json` |
| 2 | launch Project A, clean title | 2 | `ui-qa/run-a-edit-save-*.log` |
| 3 | Thermal baseline Melting/50/10/S20/250/261.0/15.0; recalc+status absent | 9 | — |
| 4 | mode→AntiIcing EXACT oracle, supply→65 EXACT oracle, ground 15, pipe S25, spacing 300, prior result retained ×4, 11-ID registry unique/enabled | 23 | `ui-qa/01-edit.png` |
| 5 | Calculate → recalc absent, result ≠ baseline | 2 | `ui-qa/02-calculate.png` |
| 6 | Hydraulics projections (spacing 30 cm, supply 65, return numeric) + Results projections (power 5.2 > 0, supply 65, return numeric) | 6 | `ui-qa/03-hydraulics.png`, `ui-qa/04-results.png` |
| 7 | save: file SHA/timestamp advance, dirty marker clears, WM_CLOSE exit 0, relaunch restores AntiIcing/65/15/S25/300/step-5 power | 15 | `ui-qa/run-a-relaunch-*.log` |
| 8 | Project B load: 55.0/5.0/150/S17, no project-A result carried | 6 | `ui-qa/05-load-2.png` |
| 9 | new-calculation reset on clean B: DEC-T01 defaults, bare title | 11 | `ui-qa/06-reset.png` |
| 10 | failure branch: fallback S17, invalid-zero fallback result + characterized physics-validation status, restore guard cleared via supply-edit EXACT oracle, save advances file, dirty clears, clean close | 10 (+11 in failure-observations.json) | `ui-qa/07-unknown-pipe.png`, `ui-qa/failure-observations.json` |

Screenshot inventory (`final/f3/ui-qa/`, all 900×700):

| File | Bytes |
|---|---|
| 01-edit.png | 55 252 |
| 02-calculate.png | 56 137 |
| 03-hydraulics.png | 92 952 |
| 04-results.png | 100 797 |
| 05-load-2.png | 54 326 |
| 06-reset.png | 50 610 |
| 07-unknown-pipe.png | 54 392 |

## 8. User-visible values (baseline / edit / save / reload / second-load / reset)

| Flow point | Value | Observed |
|---|---|---|
| Baseline (Project A = v1-sample) | mode / supply / ground / pipe / spacing | Melting / 50.0 / 10.0 / RAUTHERM S 20x2,0 / 250 mm |
| Baseline saved result | PowerTotal / ΔT | 261.0 / 15.0 |
| Edit | mode / supply / ground / pipe / spacing | AntiIcing / 65.0 / 15.0 / RAUTHERM S 25x2,3 / 300 mm; exact recalculation oracles shown; prior result 261.0 retained through every edit |
| Calculate | PowerTotal | 0.0 (≠ baseline 261.0); recalc message absent |
| Save (Ctrl+S-equivalent menu command) | project-a.smc SHA | `E1D02BC0…` → `5DA9B1E0E71B3B694560F0F4913BB6BEDC820FB6436F35EBFC363F457F7B6F84`; title dirty marker cleared |
| Reload (relaunch A) | restored state | AntiIcing / 65.0 / 15.0 / S25x2,3 / 300 mm / power == step-5 value (0.0); no recalc message |
| Second load (Project B) | supply / ground / spacing / pipe / stale-A result | 55.0 / 5.0 / 150 mm / RAUTHERM S 17x2,0 / none (PowerTotal 0.0 ≠ 261.0) |
| Reset («Создать новый расчёт») | title / mode / supply / ground / pipe / spacing / result | bare app title / Melting / 50.0 / 10.0 / none / 200 mm / absent |
| Unknown-pipe failure branch | fallback pipe / result / status / guard | RAUTHERM S 17x2,0 / invalid-zero published / characterized physics-validation status / cleared via supply edit (EXACT oracle) then save advanced `D7BA538E…` → `DED9CADF…`-class advance with dirty clear |

## 9. Frozen release binding and before/after equality

Manifest sha256 `6D039FC7B84C84F389D2DB435B69C354323ACCAB6C62A16C0B8F75475B13BA72`. Four artifacts (echoing `final/f1/conformance-scope-provenance.md` §5 binding):

| Key | Path | SHA-256 |
|---|---|---|
| executable | src/bin/Release/net8.0-windows/win-x64/SnowMeltingCalculator.exe | BE36766AF72900F8734B6BADD4EF014C6E0FC689EB459B62651EB2CFF3C6335D |
| productDll | src/bin/Release/net8.0-windows/win-x64/SnowMeltingCalculator.dll | E03F335273A1EDFE6706C37828F941992EFF064DE73B91A0345C5CD1E489F5B9 |
| testDll | tests/SnowMeltingCalculator.Tests/bin/Release/net8.0-windows/SnowMeltingCalculator.Tests.dll | E6B451F520BB25AFE543484458861D54EEA1E6729D680A75456DABED3D013D4C |
| plan | docs/architecture-migration/plans/phase-4-thermal-state.md | 327B1288B8072E7A76D814F8439ECBC353F54F4CE59AB2B507595A08980B4C02 |

Before/after equality statement: `final/f3/frozen-hashes-before.json` and `final/f3/frozen-hashes-after.json` contain the identical manifest sha256 and byte-identical key|resolvedPath|sha256 triples for all four artifacts (verified field-by-field: executable/productDll/testDll/plan all equal=True). The frozen release was not rebuilt, rewritten, or mutated anywhere in this lane.

## 10. Residual risks

- ~~Blocking~~ **RESOLVED (AMZ-4):** plan line 289 directory-parse now exits 0 after the owner-approved parser-semantics correction (`final/f3/trx-identities.json` present, merged totals 1946/1943/0/3 across four files); see §5 and the AMZ-4 addendum below.
- UI QA keystroke substitution (^s/^n → «Файл»-menu Invoke of the SAME bound commands) remains environment-specific, inherited from task-13; observables unchanged.
- HydraulicsPipeSpacing is centimetres (thermal mm / 10, `CircuitsViewModel.PipeSpacing_cm`); code-faithful value asserted.
- Unknown-pipe fallback publishes an INVALID zero result with physics-validation status for the fixture inputs (supply 55 / ground 5) — characterized behavior, asserted as presence + exact recorded status.
- `fixtures/project-a.smc` and `fixtures/unknown-pipe.smc` were intentionally mutated by the plan-mandated saves inside the harness run (originals preserved as `*.bak` beside them); rerun `prepare-ui-fixtures.ps1` before any subsequent harness invocation.
- Write-set discipline: `git status` delta outside `final/(f1|F2|F3)` unchanged (80 == 80 pre-existing entries); this lane wrote only under `final/f3/`.

Domain verdict: **APPROVE** — the sole original obstruction (plan-line-289 impossibility, §5) was resolved under owner decision AMZ-4 with fixture-proven parser semantics; every executable/user-risk requirement of the frozen plan is evidenced green above against the unchanged frozen release.

## 11. AMZ-4 addendum (2026-08-23)

Owner decision AMZ-4 (journaled in `TASK_CONTEXT.md`): the frozen plan's line-289 directory reconciliation was internally unsatisfiable — lines 282–285 write `f3-full-release.trx` and its three category extracts into one TestResults directory while the Todo-1 parser contract rejected any cross-file identity overlap. Correction executed by the orchestrator:

| Change | Detail |
|---|---|
| Parser semantics | `parse-trx.ps1` directory mode now deduplicates cross-file identities whose outcomes AGREE; conflicting outcomes for one identity, within-file duplicates, zero-test/malformed inputs still fail closed |
| Fixture proofs | `final/f3/amz4/fixtures` + `dir-a/dir-b/dir-c`: benign overlap exit 0 (deduped, total=2 from 3 rows), outcome conflict exit 3, within-file duplicate exit 3; single-file regression on real `phase-4-full-release.trx` unchanged (1946/1943/0/3) |
| Line-289 rerun | verbatim command re-executed post-fix: exit 0, wrote `final/f3/trx-identities.json`, merged totals **1946/1943/0/3** across four files |
| Scope | F1/F2 lanes never exercise directory-parse; the frozen manifest covers exe/productDll/testDll/plan only — an evidence-script edit cannot drift those hashes; F1/F2 receipts stand |

Post-correction freshness proof: V13-after re-affirmed after the edit (see run log below); frozen four-hash set unchanged.

## 12. Post-AMZ-4 V13-after re-affirmation

Re-affirmation command — `pwsh -NoProfile -File <ev>/verify-frozen-release.ps1 -Manifest <ev>/frozen-release-sha256.json -Lane F3 -Moment After` — executed after the parser edit and the directory-parse rerun; exit 0 with the identical manifest sha256 `6D039FC7B84C84F389D2DB435B69C354323ACCAB6C62A16C0B8F75475B13BA72` and byte-identical four-artifact hash triples (see §9).
