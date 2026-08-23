# Todo 8 Context and Hydraulics Receipt

Status: GREEN. Todo 8 publishes the Thermal projection through the approved
`CalculationContext` seam while preserving Hydraulics and pipe-spacing counts.

## Scope

- Production Thermal projection writer: `ThermalStateCoordinator` (1).
- Approved context seam: `CalculationContext` (excluded from the writer
  inventory because it owns the projection API, not application ownership).
- Hydraulics consumer: `CircuitsViewModel.OnCalculationContextChanged`.
- Hydraulics owner and formulas: unchanged.
- Spacing consumer: `CircuitsViewModel.OnPipeSpacingChanged`.

## Gate Results

| Gate | Command/evidence | Result |
|---|---|---|
| G0 | `verify-protected-baseline.ps1` with cumulative `task-8/allowed-hunks.json` | exit 0; protected mismatches 0; allowed hunks 29 |
| G2 / V4 | Focused Release filter for Thermal-to-Hydraulics, spacing, double-calculation, context invalidation, and writer authority tests | exit 0; 59 passed; 0 failed; 0 not executed |
| G3 | Full Release `dotnet test --no-build` plus `parse-trx.ps1` | exit 0; parser 1909 total, 1906 passed, 0 failed, 3 not executed |
| G3 arithmetic | `task-8/arithmetic.json` | passed; full failed 0; NotExecuted count remains baseline 3; focused suite 59/59 |
| G4 | `verify-protected-baseline.ps1` post-run | exit 0; protected mismatches 0; allowed hunks 29 |

## Behavioral Counts

- Thermal input notification-only publication: no Hydraulics calculation.
- Valid Thermal result publication: one logical Hydraulics calculation.
- Invalid or null Thermal result: zero Hydraulics calculations.
- Own-source `CircuitsViewModel` context publication: zero recursive
  Hydraulics calculations.
- Changed spacing: one compatibility spacing event and one consumer update.
- No-op spacing: zero compatibility events and zero calculations.
- Production writer guard: exactly `ThermalStateCoordinator`.

## Evidence

- `allowed-hunks.json`: cumulative 29-entry manifest.
- `protected-pre.json` and `protected-post.json`: symmetric protected-baseline
  checks.
- `trx-hydraulics-consumer.json`: focused V4 parser output.
- `trx-full-release.json`: full Release parser output.
- `arithmetic.json`: G3 arithmetic reconciliation.

Residual risk: the full Release parser reports three known baseline
`NotExecuted` identities; no new failed or not-executed identity was introduced.
