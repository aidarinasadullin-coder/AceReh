# F3 QA-failure probe log (plan line 519)

All probes built and executed from repo root `D:\IA\3ace v.2` on 2026-08-24 by the
F3 executor lane. Builder: `build-probes.ps1` (this directory). No source fixture,
frozen binary, or canonical task artifact was touched; every probe artifact lives
under `final/f3/probes/`.

| # | Probe | Command (abridged) | Exit | Rejection observed |
|---|---|---|---|---|
| A | zero-test TRX | `assert-trx-identities.ps1 -InputFile probes/trx-zero-test.trx -ExpectedManifest <ev>/task-2/expected-negative-test-identities.json -ExpectedGroup CalculationFailure -Output probes/probe-a-zero-test.json` | 3 | `zero tests in '…probes/trx-zero-test.trx' (empty TRX)` |
| B | unexpected identity | same verifier, `-InputFile probes/trx-unexpected.trx` | 3 | `unexpected identities not in manifest group 'CalculationFailure' (1): SnowMeltingCalculator.Tests.Probes.UnexpectedIdentityProbe` |
| C | duplicate identity | same verifier, `-InputFile probes/trx-duplicate.trx` | 3 | `duplicate test identity '…ThermalMultiplicityCharacterizationTests.Calculate_Exception_SetsExactErrorMessageNullResultAndInvalidContextPublication' in '…probes/trx-duplicate.trx'` |
| D | corrupted expected selector | same verifier, `-InputFile <ev>/final/f3/TestResults/f3-calculation-failure.trx -ExpectedManifest probes/corrupted-expected-manifest.json` | 3 | `unexpected identities not in manifest group 'CalculationFailure' (1): …Calculate_CalculatorReturnedInvalidResult_StoredCanonicallyPublishedOnceZeroHydraulics` (the identity removed from the corrupted manifest copy is now "unexpected") |
| E | corrupted copied unknown-pipe expectation | `run-wpf-ui-qa.ps1 … -ProjectA probes/fixture-corrupt/project-a.smc -ProjectB probes/fixture-corrupt/project-b.smc -InvalidProject probes/fixture-corrupt/unknown-pipe.smc -OutputDirectory probes/ui-qa-corrupt` | 1 | step 1, before any process launch: `assertion FAILED [fixture unknown-pipe.smc SHA matches manifest]: expected <D7BA538E14C8C9AC33556540705EECA6C10E8F223BB0DA837463B584F1AB1532>, observed <339E37F5AD33C1AE6555FEE9D661A6743FE2C051A256420450945C8CE81AEF42>` |

Every probe rejected. Rejected verifier runs write no `-Output` JSON by design;
this log plus the builder script are the persisted probe evidence.

Supplementary structural evidence (same directory):

- `trx-perfile/*.json` — per-file `parse-trx.ps1` outputs for all four F3 TRX files
  (full: total=1946 passed=1943 failed=0 notExecuted=3; CF 4/4; PF 6/6; RF 3/3).
- `category-only/` + `category-only-identities.json` — directory parse over ONLY the
  three category TRX copies: exit 0, 13 unique identities, all Passed → the three
  failure categories are pairwise disjoint.
