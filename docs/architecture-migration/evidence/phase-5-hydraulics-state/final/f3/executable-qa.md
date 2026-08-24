# F3 — Executable QA / User Risk Receipt

- Write-set: `phase-5-hydraulics-state`
- Frozen plan SHA-256: `0D48A757DDD1B98384D131D76C7AF5220206260BF853FADF12BA859329FC8D38`
- Method: one fresh sequential test command executed in this turn; UI QA harness availability and prior T13 evidence assessed for interactive coverage of the two global supply fields (`SupplySpacing_cm`, `SupplyHeatPercent`). `STATE.json` and all source files left untouched (read-only).
- Scope: F3 only. F4 was not launched; no STATE transition; no source edits.

## Fresh Executable Command (this turn)

```text
dotnet test tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj --no-restore --no-build --filter FullyQualifiedName~HydraulicsMultiplicityCharacterizationTests --logger "trx;LogFileName=f3-targeted.trx"
```

- Result: `Пройден! : не пройдено 0, пройдено 18, пропущено 0, всего 18, длительность 209 ms`
- Exit code: `0`
- TRX artifact: `docs/architecture-migration/evidence/phase-5-hydraulics-state/final/f3/f3-targeted.trx`
- TRX SHA-256: `4D5C1278EFD1D4E6DF679A7A03F4164D6537CFDA1E033194E12AD8FEB202AD2C`
- TRX bytes: `27661`

## UI QA Harness Assessment

- Harness script: `docs/architecture-migration/evidence/phase-5-hydraulics-state/ui-qa/run-hydraulics-flows.ps1`
- Environment probe (this turn): `pwshPresent=true`, `[Environment]::UserInteractive=true`, frozen exe present (`src\bin\Release\net8.0-windows\win-x64\SnowMeltingCalculator.exe`, SHA-256 `1CCC3BED807943973024E7452DA52C1891E74D83A7B017B1029E1B6A4527332B`).
- Prior T13 UI QA evidence: `docs/architecture-migration/evidence/phase-5-hydraulics-state/ui-qa/observations.json` — 9 PASS steps (S1-S9), but it did NOT cover the global supply fields.
- Selector registry of the harness (lines 228-238) contains `HydraulicsSupplyHeatPercent` but **no `HydraulicsSupplySpacing` selector**; a repo-wide grep for `SupplySpacing` across the `ui-qa` directory returned zero matches.

## Interactive Exercise of Global Supply Fields (explicit, required)

- `SupplySpacing_cm`: **NOT exercised interactively.** The UI QA harness has no `HydraulicsSupplySpacing` selector and never edits or reads this global supply input; prior T13 evidence contains no `SupplySpacing` interaction.
- `SupplyHeatPercent`: **NOT exercised interactively.** The harness only *reads* `HydraulicsSupplyHeatPercent` in prior T13 (S3 observed `10`, S7 observed `15`, S8 observed `10`); it never performs an interactive edit of this field. No fresh interactive edit was performed in this turn because the harness does not drive an edit of it and fabricating such evidence is forbidden.

Because at least one of the two required global supply fields (`SupplySpacing_cm`) was never exercised interactively, and `SupplyHeatPercent` was only observed (not edited) interactively, the F3 verdict cannot be APPROVE per the explicit gate rule.

## Machine-readable F3 block

```json
{
  "f3": {
    "writeSet": "phase-5-hydraulics-state",
    "planSha256": "0D48A757DDD1B98384D131D76C7AF5220206260BF853FADF12BA859329FC8D38",
    "freshCommand": {
      "command": "dotnet test tests\\SnowMeltingCalculator.Tests\\SnowMeltingCalculator.Tests.csproj --no-restore --no-build --filter FullyQualifiedName~HydraulicsMultiplicityCharacterizationTests --logger \"trx;LogFileName=f3-targeted.trx\"",
      "passed": 18,
      "failed": 0,
      "skipped": 0,
      "total": 18,
      "exitCode": 0,
      "durationMs": 209
    },
    "artifacts": [
      {
        "path": "docs/architecture-migration/evidence/phase-5-hydraulics-state/final/f3/f3-targeted.trx",
        "sha256": "4D5C1278EFD1D4E6DF679A7A03F4164D6537CFDA1E033194E12AD8FEB202AD2C",
        "bytes": 27661
      }
    ],
    "uiHarness": {
      "available": true,
      "pwshPresent": true,
      "userInteractive": true,
      "exePresent": true,
      "exeSha256": "1CCC3BED807943973024E7452DA52C1891E74D83A7B017B1029E1B6A4527332B",
      "supplySpacingCmSelectorPresent": false,
      "supplyHeatPercentSelectorPresent": true,
      "supplyHeatPercentEditedInteractively": false
    },
    "interactiveExercise": {
      "SupplySpacing_cm": "NOT_EXERCISED",
      "SupplyHeatPercent": "READ_ONLY_NOT_EDITED"
    },
    "commit": "none"
  }
}
```

## Verification and Residual Risk

- Fresh characterization command: `18 passed / 0 failed / 0 skipped`, exit `0`; TRX hashed and stored under `final/f3/`.
- UI harness is technically available in this environment, but its selector set does not cover `SupplySpacing_cm` and only reads `SupplyHeatPercent`; therefore the global supply fields are not exercised interactively. No UI interaction evidence was fabricated.
- Residual risk: F3 executable QA for the global supply fields (`SupplySpacing_cm` edit, `SupplyHeatPercent` edit) remains unverified through the prescribed interactive harness. This blocks F3 acceptance until a harness step that interactively edits both fields is added and executed, or an owner-approved alternative is supplied.
- No commit was made (BLOCKED → no control commit, per task rules). `STATE.json` and source files are unchanged.

REVIEW_ID: f3-phase5-executable
SUBJECT: phase-5-hydraulics-state@0D48A757DDD1B98384D131D76C7AF5220206260BF853FADF12BA859329FC8D38
RECEIPT: docs/architecture-migration/evidence/phase-5-hydraulics-state/final/f3/executable-qa.md
VERDICT: BLOCKED
REASON: Fresh characterization command passed 18/18 (exit 0, TRX hashed), but the prescribed UI QA harness does not contain a SupplySpacing_cm selector and only reads (never interactively edits) SupplyHeatPercent; therefore neither global supply field was exercised interactively, which mandates BLOCKED per the F3 gate rule. No commit was made.
