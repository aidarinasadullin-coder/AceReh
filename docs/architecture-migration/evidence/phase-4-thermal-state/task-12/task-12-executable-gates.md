# Task 12 — Executable Gates Receipt

Status: **GREEN WITH ONE FINDING** (negative-category additive drift — see FINDINGS; all executable gates exit 0; finding is manifest-closed-set drift, not a test/build failure).

Todo 12 of frozen plan `docs/architecture-migration/plans/phase-4-thermal-state.md` (plan SHA-256 `327B1288B8072E7A76D814F8439ECBC353F54F4CE59AB2B507595A08980B4C02`, verified equal to `STATE.json`). No production, test-source, map, model, widget or dossier edits. No git operations.

## Environment

- Root `D:\IA\3ace v.2` · branch `master` · HEAD `6a5a96f1763dd952c8d772ecd1d2536eb3b804cf` (= execution base) · final dirty rows 61.
- SDK: .NET 8.0.418 (`dotnet --version`). Shell pwsh 7 (`pwsh -NoProfile -File`) for all scripts.

## New Todo 12-owned artifacts

- Scripts (evidence root): `assert-trx-identities.ps1`, `verify-frozen-release.ps1`, `verify-final-receipts.ps1` — UTF-8 no BOM, parser-clean.
- `frozen-release-sha256.json` + re-hash proof `task-12/freeze-proof.json`.
- Fixtures + matrix: `task-12/fixtures/**`, `task-12/fixtures/fixture-matrix.json`.
- Runs/logs/parses: `task-12/TestResults/*.trx` (8), `task-12/logs/*.log` (10), `task-12/trx-v{2..6}.json`, `task-12/trx-all.json`, `task-12/v1-builds.json`, `task-12/v2-v5-runs.json`, `task-12/negative-lanes.json`, `task-12/{calculation,persistence,restore}-failure-identities.json`, `task-12/allowed-hunks.json`, `task-12/protected-{pre,post}.json`.

## Gate table

| Gate | Command (abridged) | Exit | Result |
|---|---|---|---|
| Fixture matrix A×12 | assert-trx-identities happy CF/PF/RF + 9 reject branches | 0×3 / nz×9 | ALLPASS |
| Fixture matrix B×11 | verify-frozen-release happy F4/After (+receipt written) + 10 rejects | 0 / nz×10 | ALLPASS |
| Fixture matrix C×12 | verify-final-receipts happy F1-F3 + 11 rejects | 0 / nz×11 | ALLPASS |
| G0 protected-pre | verify-protected-baseline -Baseline task-1/baseline-manifest.json -AllowedHunks task-12/allowed-hunks.json | 0 | drift 59, mismatch **0**, allowed hunks 42 |
| V1 product Debug | dotnet build src/SnowMeltingCalculator.csproj -c Debug --nologo | 0 | 1.0 s, 0 warnings, 0 errors |
| V1 product Release | … -c Release --nologo | 0 | 1.0 s, 0 w / 0 e |
| V1 tests Release | dotnet build tests/SnowMeltingCalculator.Tests/… -c Release --nologo | 0 | 1.2 s, 0 w / 0 e |
| V1 tests Debug | … -c Debug --nologo | 0 | 4.4 s, 0 w / 0 e |
| V2 focused | dotnet test -c Release --no-build --filter "…ProjectSessionThermalStateTests\|ThermalMultiplicityCharacterizationTests\|ThermalViewModelTests\|CalculationStateServiceTests\|DiRegistrationTests" → task-12-v2-focused.trx | 0 | 203/203/0/NE0, 1.8 s |
| V3 upstream | filter "…ClimateThermalInvalidationRegressionTests\|ConstructionThermalInvalidationRegressionTests" → task-12-v3-upstream-invalidation.trx | 0 | 21/21/0/NE0, 1.3 s |
| V4 hydraulics | filter "…ThermalToHydraulicsIntegrationTests\|PipeSpacingSynchronizationTests\|DoubleCalculationPreventionTests\|CalculationContextInvalidationTests\|CalculationContextWriterAuthorityTests" → task-12-v4-hydraulics-consumer.trx | 0 | 59/59/0/NE0, 1.4 s |
| V5 persistence | filter "…ProjectRoundTripTests\|ResultsViewModelOpenProjectTests\|ProjectLifecycleFlowCharacterizationTests\|ThermalPersistenceMapperTests" → task-12-v5-persistence.trx | 0 | 77 total /76 passed /0 failed /NE1 (known identity), 10.7 s |
| V6 full Release | no filter → task-12-v6-full-release.trx | 0 | **1946 total /1943 passed /0 failed /NotExecuted 3**, 31.6 s |
| v6a CalculationFailure lane | dotnet test --filter "TestCategory=CalculationFailure" → task-12-v6a-calculation-failure.trx; assert-trx-identities → calculation-failure-identities.json | 0 / 0 | strict equality **4/4 Passed** |
| v6b PersistenceFailure lane | … "TestCategory=PersistenceFailure" → task-12-v6b-persistence-failure.trx; assert ×1 | 0 / **3** | TRX total 6 = manifest 3 present+Passed **+ 3 unexpected** ⇒ FINDING-1 |
| v6c RestoreFailure lane | … "TestCategory=RestoreFailure" → task-12-v6c-restore-failure.trx; assert ×1 | 0 / **3** | TRX total 3 = manifest 2 present+Passed **+ 1 unexpected** ⇒ FINDING-1 |
| Freeze | frozen-release-sha256.json written; immediate re-hash of all four artifacts | 0 | all four equal=true; plan sha == STATE.json sha |
| G4 protected-post | same verifier, task-12 manifest | 0 | drift 60, mismatch **0**, allowed hunks 43 |

All suites ran sequentially in separate invocations (no concurrent test hosts). parse-trx was invoked per TRX (`trx-v2..v6.json`); a single `-InputDirectory` pass is impossible by design because V2–V5 filtered sets are subsets of V6 and the parser rejects cross-file duplicate identities; `task-12/trx-all.json` aggregates the five authoritative parser outputs plus the reconciliation block.

## Reconciliation vs Todo 11 baseline (1946/1943/0/3)

- V6 totals arithmetic: 1943 passed + 0 failed + 3 NotExecuted = 1946 ✓
- Delta vs Task 11 full Release: **0 new tests, 0 new failures, 0 new NotExecuted identities** ✓
- V6 NotExecuted identities (exact): `RegenerateCircuitsBaseline`, `RegenerateBaseline`, `ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile` ✓ (V5 carries the ResultsViewModel one as its NE1.)
- Warning/error identity delta vs baseline: none (all builds 0/0).

## TRX SHA-256 (uppercase)

| TRX | SHA-256 |
|---|---|
| task-12-v2-focused.trx | 46E4D34A13FE9355C4C02C5D8D8330E92D201841E7919B0187258CA4F11AE310 |
| task-12-v3-upstream-invalidation.trx | B0FACB0C5D6BC28958537AA1F47F23F0B99350084E5DC8863BB5C40F113F34ED |
| task-12-v4-hydraulics-consumer.trx | B8D00084C2C5FF2A76405C0ADFF58124BCBA914F81EC4BEF1279603C7264F648 |
| task-12-v5-persistence.trx | 6E0E2DA3DC97387D860C9F3B719B1BE2C250115FAC1E6779EC817BCFF326871A |
| task-12-v6-full-release.trx | 0FC18718E6912E52ED1ADC46C3DCEC5E0B1F8E247DF7A71A8A2CA96971D45EF8 |
| task-12-v6a-calculation-failure.trx | D30EE455E1942A6EDBEF5D1137D9A7E2FA14DB1C476063B5126E483B3475BD39 |
| task-12-v6b-persistence-failure.trx | 648D253F3A989F10F31856B8DC3823260861679DE7D7197777D0AF05C8A27A7A |
| task-12-v6c-restore-failure.trx | 0C6F8E0FC06329D4D5DEBF193A20182FD9FB2735AC2530EB90B4EC2C6857090D |

## Frozen release manifest (`frozen-release-sha256.json`, self SHA-256 `6D039FC7B84C84F389D2DB435B69C354323ACCAB6C62A16C0B8F75475B13BA72`)

| Key | Path | SHA-256 |
|---|---|---|
| executable | src/bin/Release/net8.0-windows/win-x64/SnowMeltingCalculator.exe | BE36766AF72900F8734B6BADD4EF014C6E0FC689EB459B62651EB2CFF3C6335D |
| productDll | src/bin/Release/net8.0-windows/win-x64/SnowMeltingCalculator.dll | E03F335273A1EDFE6706C37828F941992EFF064DE73B91A0345C5CD1E489F5B9 |
| testDll | tests/SnowMeltingCalculator.Tests/bin/Release/net8.0-windows/SnowMeltingCalculator.Tests.dll | E6B451F520BB25AFE543484458861D54EEA1E6729D680A75456DABED3D013D4C |
| plan | docs/architecture-migration/plans/phase-4-thermal-state.md | 327B1288B8072E7A76D814F8439ECBC353F54F4CE59AB2B507595A08980B4C02 |

Publish: exe already existed after the V1 Release build (csproj sets `RuntimeIdentifier=win-x64`); **no publish command executed**. Immediate re-hash equality: all four `true` (`task-12/freeze-proof.json`).

## Fixture matrix summary (35 cases, `fixtures/fixture-matrix.json`, allPass=true)

Happy paths exit 0: A1/A2/A3 (CF/PF/RF strict equality on synthetic valid TRX), B1 (valid 4-key manifest, F4/After receipt written under sandbox), C1 (three synthetic APPROVE receipts + before/after/cross-lane/artifact-hash reconciliation).
Reject branches exit nonzero (30): non-Passed outcome; absent expected; unexpected extra; cross-group leak; duplicate identity; zero-test TRX; empty manifest group; missing input; bad group name; manifest extra key; missing key; lowercase sha; hash mismatch; missing file; path escape (`..`); duplicate resolved path; non-regular file (directory); bad Lane; bad Moment; missing receipt; missing REASON field; duplicate VERDICT field; wrong SUBJECT; VERDICT=REJECT; VERDICT=BLOCKED; altered artifact hash; cross-lane drift; before≠after drift; extra machine field (`SUMMARY:`); missing frozen-hashes-before.json.

## FINDINGS

### FINDING-1 — RESOLVED-AMZ3 (owner decision 2026-08-23, journal TASK_CONTEXT.md; manifest extended CF=4/PF=6/RF=3; post-correction strict equality exit 0 for all three lanes)

The immutable Todo 2 closed-set manifest `task-2/expected-negative-test-identities.json` (CF=4, PF=3, RF=2) is **additively stale** versus the current suite's negative categories:

- PersistenceFailure: 3 unmanifested identities, all in `tests/SnowMeltingCalculator.Tests/ViewModels/ResultsViewModelOpenProjectTests.cs` — `[Category("PersistenceFailure")]` at lines **2758** (`PersistenceFailure_FailedFileOperation_PreservesErrorStateWithoutSchemaDrift`), **2845** (`PersistenceFailure_MissingOrCorruptSavedResult_FallbackOnce_InvalidNeverCanonical`), **2916** (`PersistenceFailure_UnknownPipe_FallsBackToFirstStandard_NoSchemaDrift`).
- RestoreFailure: 1 unmanifested identity in `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectLifecycleFlowCharacterizationTests.cs` — `[NUnit.Framework.Category("RestoreFailure")]` at line **478** (`RestoreModulesFromProjectAsync_ThermalBoundaryException_ClearsLeaseAndPreservesPartialState`, Todo 9 allow-listed file).

Proof of additive-only drift: in both lanes every manifest identity is present exactly once with outcome Passed (the assert script checks absence BEFORE unexpectedness; no absent error fired), all lane tests Passed (v6b 6/6, v6c 3/3), and V6 full-suite failed=0. CalculationFailure is clean (4/4 exact).

Impact: plan F3 requires each category TRX to equal its manifest group with "no duplicate/unexpected identity"; as-is, F3's PF/RF lanes will REJECT. Per Todo 12 spec the manifest was NOT edited silently; drift is reported as a finding.

Owner options:
- **(a) Recommended:** authorized narrow correction lane extending the Todo 2 manifest with exactly these 4 identities (they are legitimate characterized negative-category tests; keeps F3 filters unchanged).
- (b) Re-scope F3/V12-F3 filters to thermal-scoped negative fixtures (plan-text change; weaker coverage).

## Deviations from instruction letter

1. Directory-level single parse replaced by per-TRX parses + aggregated `trx-all.json` (parser's cross-file duplicate rejection makes the literal single invocation impossible for overlapping suites; plan wording "Parse every TRX by exact identity" honored).
2. Category lanes added beyond the original V1-V6 list per reconciled instruction (v6a/v6b/v6c) to exercise `assert-trx-identities` against real suite output; produced FINDING-1.

No other deviations. No `catalog/v*` writes; no shared test-project TestResults writes; protected baseline symmetric (pre/post mismatch 0).
