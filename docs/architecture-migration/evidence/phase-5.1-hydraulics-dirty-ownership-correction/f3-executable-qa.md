# F3 Executable QA / User Risk

- Write-set: `phase-5.1-hydraulics-dirty-ownership-correction`
- Frozen plan SHA-256: `65999B8FF9008157ADAADC57D85138AF610829F70E57C6E9B0269F98C72EE46C`
- Verdict: `APPROVE`

## Evidence

- Debug build: PASS, `0 warnings / 0 errors`.
- Release build: PASS, `0 warnings / 0 errors`.
- Full Release suite after rerun: `1981 passed / 0 failed / 1 skipped`.
- WPF Release smoke run: process launched, main window was created and responsive, then closed cleanly.
- Initial transient failures were external file-locks (`settings.json`, generated WPF/build files); reruns passed after the running application/test processes ended.
- The one remaining skip was the known baseline fixture/baseline-regeneration skip.

## Runtime and widget checks

- `node docs/architecture-migration/widget/generate-widget.mjs --check`: `14/14 PASS`.
- `verify-widget.mjs --suite runtime-v2`: `47 assertions / 20 mutations / PASS`.

## Limitation

This receipt records executable QA and smoke validation. It does not claim a fresh interactive screenshot package for all five manual WPF flows; that limitation is covered by the F1/provenance blocker and prevents final phase acceptance.

REVIEW_ID: f3-phase5.1-executable
SUBJECT: phase-5.1-hydraulics-dirty-ownership-correction@65999B8FF9008157ADAADC57D85138AF610829F70E57C6E9B0269F98C72EE46C
VERDICT: APPROVE
REASON: Builds, focused/integration/full executable gates, WPF smoke, and widget verification passed; residual manual-flow evidence is explicitly limited.
