# Phase 7 R0 Baseline Receipt

## TODO/STAGE

- Stage: `R0` preflight and baseline evidence.
- Final status: `BLOCKED`.
- Scope: establish the mandatory preconditions for Phase 7 Project Restore Coordinator relaunch.
- No R1-R10 work was started.

## WRITE-SET

The canonical R0 write-set permits only Phase 7 evidence artifacts under:

`docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/`

This receipt is the only additional file created by this continuation. No production or test source was changed.

## FORBIDDEN-SET

- Production source, tests, plans, `TASK_CONTEXT.md`, architecture maps, widgets, state files, and approval receipts.
- Existing checker, logs, and evidence artifacts.
- Destructive cleanup, deletion, overwrite, or regeneration of pre-existing files.
- Baseline manifests, because the mandatory build prerequisite failed before a valid baseline could be established.

## CHANGE-CLASS

Evidence-only R0 receipt. This is not an implementation change and does not claim Phase 7 conformance.

## ACCEPTANCE

R0 requires all of the following before R1-R10 may begin:

1. Required-path collision eligibility is clear.
2. The mandatory Debug build succeeds.
3. Lifecycle and round-trip evidence is available and preserved.
4. The allow-list checker and plan/owner approvals are preserved.

The build prerequisite was not satisfied, so R0 cannot be accepted as `PASS`.

## COMMANDS

Repository identity and evidence inspection:

```text
git rev-parse --show-toplevel
git branch --show-current
git rev-parse HEAD
git status --short --untracked-files=all
```

Mandatory Debug build:

```text
dotnet build "tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Debug --nologo
```

Previously executed R0 QA commands produced the preserved TRX artifacts:

- lifecycle suite: `logs/r0-lifecycle.trx`
- round-trip suite: `logs/r0-roundtrip.trx`

## RESULTS

- Root: `D:/IA/ace`
- Branch: `master`
- HEAD: `80579e803843d95bb60198907dac192f8a4e90f2`
- Canonical plan: `docs/architecture-migration/plans/phase-7-project-restore-coordinator-relaunch.md`
- Plan size: `65608` bytes.
- Plan SHA-256: `1135F95CBA913499904BF655F5BE08F92F45B02CAFAFB171D40F6BF7F51C88D5`.
- Required R0 path collisions at initial eligibility check: `0`.
- Debug build: `FAILED`.
- Exact build failure: `Невозможно создать файл, так как он уже существует.`
- R0 result: `BLOCKED`.

## HAPPY-QA

The preserved executable evidence completed successfully:

- Lifecycle QA: `12 passed / 0 failed`; result preserved in `logs/r0-lifecycle.trx`.
- Round-trip QA: `12 passed / 0 failed`; result preserved in `logs/r0-roundtrip.trx`.
- Terminal plan review receipt contains `VERDICT: APPROVE`.
- Owner plan approval receipt contains `VERDICT: APPROVE`.
- The existing allow-list checker was preserved unchanged.

## FAILURE-QA

The mandatory command

```text
dotnet build "tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj" -c Debug --nologo
```

failed with:

```text
Невозможно создать файл, так как он уже существует.
```

Because the mandatory Debug build failed, the baseline gate is blocked. The existing evidence was not overwritten, and no destructive retry or cleanup was performed.

## EXPECTED-RESULTS

- Expected R0: successful Debug build plus valid preserved QA and approval evidence.
- Observed R0: build prerequisite failed; status is `BLOCKED`.
- Expected next action under the frozen plan: resolve the external build/output collision, then restart R0 from a clean, explicitly authorized checkpoint.

## EVIDENCE

Preserved Phase 7 artifacts and their SHA-256 values at receipt creation:

- `scripts/check-phase7-allow-list.mjs`: `72FFB18B59451A1CD269661994205D1A0DDBCB8F2DEC7429F882B3194A025C5D`
- `logs/r0-lifecycle.trx`: `385911D7D41803B896359D0CB1F66D2010624B42284EB3905E7C69E8EFF4F56D`
- `logs/r0-roundtrip.trx`: `A8CD19D5013C93EF40E4298FA3404C5D42D7A12D45EFED01C36EF8DDFB20DC3A`
- `terminal-plan-review-receipt.md`: `ECD7A3D48F901507D8544DD4810D5C39F44BE30CE6A9A68904D67897C6021489`
- `owner-plan-approval.md`: `DC8CB036963482CC2FEEFB52668A5708E6C68A45DE91491D3720AFB91EAA5130`

No `r0-baseline` manifests were created.

## RESIDUAL-RISK

The underlying file/output collision remains unresolved. The successful lifecycle and round-trip suites do not compensate for the failed mandatory Debug build. Phase 7 implementation and final review therefore remain unevaluated.

## ROLLBACK

This receipt is evidence-only. Rollback means removing only this newly created receipt after explicit authorization. Do not remove or modify the pre-existing checker, TRX logs, approvals, plan, or unrelated worktree changes.

## NEXT-GATE

`BLOCKED`: stop Phase 7 execution. R1-R10 and final review cannot start from this receipt. A future attempt must resolve the build collision, rerun the complete mandatory R0 preflight under the frozen plan, and obtain a new R0 decision without overwriting this evidence.
