# Slice 5 — Report Source of Truth (PASS)

**Date:** 2026-08-31
**Plan:** `docs/architecture-migration/plans/phase-7-project-restore-coordinator-relaunch.md` (frozen, NOT edited)
**Lane:** continuation of Slice 5 / Todo 5 (same execution lane)

## Exact Commands

```powershell
dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo
dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~CalculationReportDataBuilderTests|FullyQualifiedName~CalculationReportExportServiceTests|FullyQualifiedName~CalculationReportInventoryTests|FullyQualifiedName~CalculationReportWarningTests" --logger "trx;LogFileName=slice-5-report-source-of-truth.trx" --results-directory "docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/logs"
```

## Build Result

- `dotnet build ... -c Debug --nologo` -> **Build succeeded. Warnings: 0, Errors: 0**

## Test Result

- **Passed: 42, Failed: 0, Skipped: 0, Total: 42**
- TRX: `docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/logs/slice-5-report-source-of-truth.trx`
- Matched classes: `CalculationReportDataBuilderTests`, `CalculationReportExportServiceTests`, `CalculationReportInventoryTests`, `CalculationReportWarningTests`.

## Contracts Verified

### Current projection wins over stale persisted DTO

`Build_UsesCurrentProjection_WhenPersistedDtoHasStaleSentinel` passes. A separate persisted-shaped DTO contains an `OperatingResult.Power` sentinel of `999999.0`; the builder receives the current projection and emits `1200.0`, proving report data is sourced from the supplied current state rather than a stale persisted value.

### Export is single-pass and non-mutating

`ExportReportAsync_BuildsAndRendersOnce_WithoutMutatingProject` passes. The test verifies:

- exactly one builder invocation;
- exactly one renderer invocation;
- project identity remains unchanged;
- thermal result remains null;
- hydraulics collectors remain empty.

Existing focused tests also retain cancellation-before-build, invalid/null input handling, directory creation, explicit mode propagation, UTF-8 output, warning behavior, and report inventory/API guards.

## Changed Paths

- `tests/SnowMeltingCalculator.Tests/Services/Reports/Calculation/CalculationReportDataBuilderTests.cs`
- `tests/SnowMeltingCalculator.Tests/Services/Reports/Calculation/CalculationReportExportServiceTests.cs`
- `.omo/notepads/phase-7-project-restore-coordinator-relaunch/learnings.md`
- `docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/slice-5-report-source-of-truth.md`
- `docs/architecture-migration/evidence/phase-7-project-restore-coordinator-relaunch/logs/slice-5-report-source-of-truth.trx`

Production files in the Slice 5 allow-list were reviewed and did not require modification because the existing caller-to-report `ProjectData` boundary already carries the current projection.

## Residual Risks

- **LSP unavailable:** `lsp_diagnostics` failed with `LSP file path must be inside request cwd`; a relative-path retry resolved outside the repository. The authoritative compile gate is the successful `dotnet build` above.
- **Projection responsibility:** callers must continue supplying the current `ProjectData` projection. A future caller that passes an old persisted DTO would violate the verified boundary and needs its own characterization test.
- **No manual UI QA:** this slice changes report tests and confirms an existing service boundary; no user-visible production implementation changed.
- **No owner decision escalated:** no public/API contract, persistence/schema, state ownership, rollback-semantics, or scope change was required.
