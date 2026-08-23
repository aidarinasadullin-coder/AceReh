# F1 — Conformance / Scope / Provenance Domain Receipt

REVIEW_ID: f1-conformance-phase-4-thermal-state
SUBJECT: phase-4-thermal-state@327B1288B8072E7A76D814F8439ECBC353F54F4CE59AB2B507595A08980B4C02
RECEIPT: inline (this document; supporting artifacts under `final/f1/`)
VERDICT: APPROVE
REASON: Every planned requirement has executable evidence; every changed path/hunk maps to a todo allow-list and DEC-T01..T08 scope; canonical/mirror plan identity, owner gates, staged-set preservation and six-view architecture fidelity verified; frozen release hashes byte-stable across the lane; workflow remains `executing` with no premature acceptance or next-phase authorization.

## 1. Gate matrix

| Check | Command (from repo root `D:\IA\3ace v.2`) | Exit | Result |
|---|---|---|---|
| V13-before | `pwsh -NoProfile -File <ev>/verify-frozen-release.ps1 -Manifest <ev>/frozen-release-sha256.json -Lane F1 -Moment Before` | 0 | artifacts=4, manifest sha `6D039FC7B84C84F389D2DB435B69C354323ACCAB6C62A16C0B8F75475B13BA72` |
| V0 state gate | `node docs/architecture-migration/workflow/validate-state.mjs validate --check-plan` | 0 | `valid=true`, `stage=executing` |
| V10 plan structure | `<ev>/verify-plan-structure.ps1 -Plan plans/phase-4-thermal-state.md -Output final/f1/plan-structure.json` | 0 | rows=18 ordered unique 1..14+F1..F4, `v11_first_todo=11`, errors=0 |
| Protected baseline | `<ev>/verify-protected-baseline.ps1 -Baseline <ev>/task-1/baseline-manifest.json -AllowedHunks <ev>/task-14/allowed-hunks.json -EvidenceRoot <ev> -Output final/f1/protected.json` | 0 | drift=79 paths, **protected_mismatch_count=0**, allowed_hunk_count=56 |
| V12-F1 model-v2 | `node docs/architecture-migration/widget/verify-widget.mjs --suite model-v2 … --output final/f1/model-v2.json` | 0 | PASS, 33 assertions / 21 mutations |
| V12-F1 runtime-v2 | same, `--suite runtime-v2` → `final/f1/runtime-v2.json` | 0 | PASS, 47 assertions / 20 mutations |
| Widget determinism | fresh SHA-256 of `docs/architecture-migration/architecture-widget.html` | — | `B4A0CF5412EBE2C7BF00ED8A80742F49A7041867D50908722F7644E224F6FC08` — equals Todo 14 recorded deterministic hash |
| Browser contract (six views) | Playwright MCP against loopback-served widget | 0 errors | see §4; screenshots `final/f1/browser/f1-phase-4-widget-<ID>.png` |
| V13-after | `-Lane F1 -Moment After` | 0 | four-hash set IDENTICAL to before |

## 2. Changed-path → allow-list provenance

The cumulative allowed-hunks manifest (`task-14/allowed-hunks.json`, 56 entries) enumerates the union of all todo write-sets. The protected verifier recomputed every tracked/untracked delta (79 drifted paths) and classified each as authorized — `protected_mismatch_count=0`. Granular mapping (todo granularity):

| Write-set | Paths |
|---|---|
| task-1 (baseline infra) | capture-baseline.ps1, verify-protected-baseline.ps1, verify-plan-structure.ps1, parse-trx.ps1, task-1/* evidence |
| task-2 (characterization) | ThermalMultiplicityCharacterizationTests.cs, expected-negative-test-identities.json, ThermalViewModelTests.cs (drive mechanism), task-2/* |
| task-3 (state contract) | IProjectSessionThermalState.cs, ProjectSessionThermalState.cs, ThermalStateSnapshots.cs, ThermalMutationOrigin.cs, ThermalMutationResult.cs, ProjectSessionThermalStateTests.cs, task-3/* |
| task-4 (session/DI) | IProjectSession.cs, ProjectSession.cs, DiRegistrationTests.cs, task-4/* |
| merged 5+6+7 (AMZ-1) | CalculationStateService.cs + interface, ServiceCollectionExtensions.cs, ThermalViewModel.cs, IThermalStateCoordinator.cs, ThermalStateCoordinator.cs, ThermalView/CircuitsView/ResultsView.xaml, coordinator+selector-contract tests, service-guard adaptations, task-5/6/7/* |
| task-8 (projection authority) | CalculationContextWriterAuthorityTests.cs, PipeSpacingSynchronizationTests.cs, task-8/* |
| task-9 (+AMZ-2) | ThermalPersistenceMapper.cs, ProjectLoadOrchestrator.cs, MainViewModel.cs, ThermalPersistenceMapperTests.cs, lifecycle/climate/results test updates, two characterization pin rows, task-9/* |
| task-10 (persistence) | ResultsViewModel.cs thermal seams, ProjectRoundTripTests.cs, task-10/* |
| task-11 (guards) | ThermalStateLegacyStoreGuardTests.cs, DiRegistrationTests.cs additions, task-11/* |
| task-12 (executable gates) | assert-trx-identities.ps1, verify-frozen-release.ps1, verify-final-receipts.ps1, frozen-release-sha256.json, task-12/* |
| task-13 (UI QA) | prepare-ui-fixtures.ps1, run-wpf-ui-qa.ps1, task-13/* |
| task-14 (dossier) | ten maps/*.md, architecture-model.json, architecture-widget.html (generated only), task-14/* |
| control plane (workflow-owned) | STATE.json transitions, TASK_CONTEXT.md journal (AMZ-1/2/3) |

No unexplained path exists. No `D:\IA\ace` (v1) metrics referenced anywhere in the refreshed dossier.

## 3. Owner-gate and identity conformance

- `planApproval=approved` via `/architecture-approve phase-4-thermal-state` (explicit owner command).
- `executionAuthorization=approved` via `/architecture-start phase-4-thermal-state` (explicit owner command).
- `resultAcceptance=pending` — untouched; workflow `stage=executing`; no awaiting-acceptance transition and no Phase 5 authorization exists anywhere in artifacts (verified by V0 and dossier inspection).
- Canonical plan and `.omo` mirror are byte-identical at the STATE-bound SHA (V0 `--check-plan`).
- Documented owner-directed deviations: AMZ-1 (merged boundary + transitional canonical mutation, single production caller), AMZ-2 (two characterization pins updated to DEC-T08 target semantics), AMZ-3 (negative-manifest extension CF=4/PF=6/RF=3). All three carry journal entries and lane receipts.

## 4. Six-view browser contract (this lane)

Served the canonical widget over loopback HTTP from the verified repository root (Playwright MCP blocks `file:`); buttons are multi-select toggles, all active by default; asserted end-state per view after one click: `aria-pressed="true"`.

| View | aria-pressed | state-kind (non-empty, non-error) | rows > 0 | Screenshot (final/f1/browser/) |
|---|---|---|---|---|
| compile-time | true | Строки доступны | 65 | f1-phase-4-widget-compile-time.png |
| di-runtime | true | Строки доступны | 171 | f1-phase-4-widget-di-runtime.png |
| state-ownership | true | Строки доступны | 199 | f1-phase-4-widget-state-ownership.png |
| reactive | true | Строки доступны | 213 | f1-phase-4-widget-reactive.png |
| persistence | true | Строки доступны | 233 | f1-phase-4-widget-persistence.png |
| user-flow | true | Строки доступны | 256 | f1-phase-4-widget-user-flow.png |

Console across the whole session: 0 errors / 0 warnings. Page closed cleanly; loopback server shut down.

## 5. Frozen release binding

Manifest sha256 `6D039FC7B84C84F389D2DB435B69C354323ACCAB6C62A16C0B8F75475B13BA72`; four artifacts (before == after):

| Key | Path | SHA-256 |
|---|---|---|
| executable | src/bin/Release/net8.0-windows/win-x64/SnowMeltingCalculator.exe | BE36766AF72900F8734B6BADD4EF014C6E0FC689EB459B62651EB2CFF3C6335D |
| productDll | src/bin/Release/net8.0-windows/win-x64/SnowMeltingCalculator.dll | E03F335273A1EDFE6706C37828F941992EFF064DE73B91A0345C5CD1E489F5B9 |
| testDll | tests/SnowMeltingCalculator.Tests/bin/Release/net8.0-windows/SnowMeltingCalculator.Tests.dll | E6B451F520BB25AFE543484458861D54EEA1E6729D680A75456DABED3D013D4C |
| plan | docs/architecture-migration/plans/phase-4-thermal-state.md | 327B1288B8072E7A76D814F8439ECBC353F54F4CE59AB2B507595A08980B4C02 |

## 6. Residual risks (accepted, journaled)

- AMZ-1 transitional mutation `ApplyNeedsRecalculation` retained on the canonical state with exactly one production caller (compat route); Todo 11 guards enforce zero additional callers.
- UI QA keystroke substitution (^s/^n → File-menu Invoke) is environment-specific; observables unchanged (task-13 receipt §deviations).
- LSP unavailable for this workspace path (known recorded limitation); correctness gated by compiler/suites per migration instructions.

Domain verdict: APPROVE. Downstream lanes (F2, F3) may proceed against the identical frozen write-set; any correction invalidates this chain and reruns F1→F4.
