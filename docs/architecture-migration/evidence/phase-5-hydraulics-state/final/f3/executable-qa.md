# F3 - Executable QA / User Risk Receipt

- Write-set: `phase-5-hydraulics-state`
- Frozen plan SHA-256: `0D48A757DDD1B98384D131D76C7AF5220206260BF853FADF12BA859329FC8D38`
- Scope: F3 only. F4 was not launched. Production source, tests and `STATE.json` were not edited.
- Fresh harness fix: `Select-Sidebar` now rebinds to the live main window (`Refresh-MainWindow`), activates the sidebar `ListItem` via `InvokePattern` when supported and falls back to `SelectionItemPattern.Select()`, then verifies the view-specific selector live (`Wait-True` on the expected AutomationId) before returning. The Results navigation target was corrected to the real `ResultsThermalPower` AutomationId (the prior `ResultsHydraulicPower` id does not exist in `ResultsView.xaml`); Hydraulics target remains `HydraulicsPipeSpacing`. Strict S3 Results cards and S4f downstream power assertions are unchanged.

## Fresh Complete Run

The valid complete run is the fresh `out-f3-fix` run from 2026-08-25, consolidated here under `final/f3/` with its observations, failure branch receipt, process logs and nine screenshots. S1 fixture hashes and executable SHA are recorded in `observations.json`; the source fixture SHA is the canonical `E1D02BC006D1EE15304EC3BEC41DE5F68D756BD27324D87A2A0E3D264119397A`.

- Steps: `S1 PASS, S2 PASS, S3 PASS, S4 PASS, S5 PASS, S6 PASS, S7 PASS, S8 PASS, F PASS`
- Exit code: `0`. Executable SHA-256 unchanged before/after every launch (`ECB0AE84B760700C76024F4D5312D6440FE8D4D540775534CB89F57EE4AB164E`).
- Both navigation targets verified live: `Select-Sidebar 'Гидравлический расчёт'` (verified `HydraulicsPipeSpacing`) and `Select-Sidebar 'Результаты'` (verified `ResultsThermalPower`) are exercised in S3, S5, S6, S7 and S8; every Results summary-card assertion passed.
- Selector resolutions: nonzero for all required selectors, including `HydraulicsPipeSpacing`, `HydraulicsSupplyTemperature`, `HydraulicsReturnTemperature`, `HydraulicsSupplySpacing`, `HydraulicsSupplyHeatPercent` and `HydraulicsCalculateButton`.
- Screenshots: `9` fresh PNGs, each recorded with dimensions, byte count and SHA-256.
- Process assertions: every launched process exited `0`; stderr was free of crash patterns; executable SHA was unchanged.
- Strict assertions retained: S3 Results summary cards (`Длина труб`, `Мощность`, `Расход`) and S4f recalculated power at supply spacing/heat `12/10` and `12/15`, plus reverted power.

## Diagnosis

The prior blocker was a UIA/WPF navigation timing defect, not an executable-path or stale-process mismatch: `Select-Sidebar` called `SelectionItemPattern.Select()` on a cached/stale `ListItem` and then only `Start-Sleep 900ms` without confirming the target view had materialized, so the main window stayed pre-navigation and downstream selectors timed out at S3. The fix rebinds to the live owned window, drives activation through `InvokePattern` (fallback `SelectionItemPattern`), and waits for the view-specific AutomationId to appear before returning. With the corrected `ResultsThermalPower` id, both sidebar targets now navigate deterministically; a single fresh full run reached S3 and executed all remaining steps green. The harness does not weaken assertions or substitute screenshots for semantic checks.

## Current F3 Verdict

```text
REVIEW_ID: f3-phase5-executable
SUBJECT: phase-5-hydraulics-state@0D48A757DDD1B98384D131D76C7AF5220206260BF853FADF12BA859329FC8D38
RECEIPT: docs/architecture-migration/evidence/phase-5-hydraulics-state/final/f3/executable-qa.md
VERDICT: APPROVE
REASON: A fresh complete run is green with real selector resolutions and fresh screenshots; the Select-Sidebar activation fix (InvokePattern with SelectionItemPattern fallback, live view-specific selector verification, corrected ResultsThermalPower target) makes both navigation targets deterministic. All S1-S8 + F steps PASS, exit 0, executable SHA unchanged.
```