# Slice 1 — LIM-P8-1 live verification and model record flip

Phase 11 (`phase-11-migration-tails-closure`), executed under
OWNER-PLAN-APPROVAL-PHASE-11 (in-session execution authorization). Write-set:
evidence + `maps/architecture-model.json` only. **Zero production edits.**

## Clause-by-clause live verification

| LIM-P8-1 clause (historical statement) | Live fact (2026-09-03) | Verdict |
|---|---|---|
| "Results still shares mutable `CircuitRow` objects with `CircuitsViewModel`" | `ResultsViewModel.UpdateCircuitsFilter` (`:1441-1466`) builds each display row as a new Results-owned copy via `HydraulicCircuitRowProjection.CreateRow(circuitSnapshot)` from canonical `HydraulicCircuitSnapshot`s and sets `DisplayMode` on the copy. Grep gate: `CircuitsViewModel` appears in `ResultsViewModel.cs` only in a doc comment (`:1412`). `CircuitsViewModel` has no back-reference to Results (it implements the Phase 9 `IReportCollectorDataSource` interface instead). | closed by Phase 9 |
| "rebuilds Specifications/EquipmentItems/SummaryCards via `HydraulicSummaryBuilder(CollectorData)`" | `HydraulicSummaryBuilder` public signatures: `BuildSummaryCards/BuildSpecifications/BuildEquipmentItems(IReadOnlyList<HydraulicCollectorSnapshot>?, …)` — the canonical snapshot type, not `CollectorData`. Call sites in `ResultsViewModel` (`:1475-1476`, `:1494-1495`, `:1508-1509`) pass `_projectSession.HydraulicsState.Snapshot.Collectors`. | closed by Phase 9 |
| "`UpdateCollectorSummary` reads the module selection" | `UpdateCollectorSummary` (`:1390+`) reads `_projectSession.HydraulicsState.Snapshot.Collectors` indexed by Results-owned `SelectedCollectorIndex`; the doc comment states the module VM selection is not read (Phase 9, ST-027). | closed by Phase 9 |

The Phase 9 terminal-plan-review receipt
(`evidence/phase-9-legacy-seams-cleanup/terminal-plan-review-receipt.md`)
names these exact clauses as Phase 9 in-scope; the closure was substantively
executed then, and only the model record was never flipped — a documentation
defect, corrected here, not a re-scope.

## Targeted suites (plan-exact command)

```
dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --nologo   → 0 warnings / 0 errors
dotnet test … --filter "FullyQualifiedName~ResultsOwnedCircuitProjectionTests|FullyQualifiedName~ResultsStabilizationPhase1BehaviorContractsTests|FullyQualifiedName~ResultsStabilizationPhase1ContractsTests|FullyQualifiedName~ResultsViewModelCollectorEquipmentItemsTests|FullyQualifiedName~HydraulicsMultiplicityCharacterizationTests" --logger "trx;LogFileName=slice-1-lim-p8-1.trx" --results-directory "docs/architecture-migration/evidence/phase-11-migration-tails-closure/logs"
```

Result: **50 passed / 0 failed / 0 skipped** (704 ms). The suites are
unmodified by this phase. TRX: `logs/slice-1-lim-p8-1.trx`.

## Model change

`maps/architecture-model.json`: `limitations` entry `LIM-P8-1` →
`status: "closed"` with the closure statement (live anchors + historical text
preserved inline); new evidence record `EV-P11-LIMP81` pointing at this
receipt. Post-edit validation: verifier `model-v2` suite → **PASS** (33
assertions / 21 mutations) on the updated model
(`model-v2-slice1.json`). Widget regeneration is deferred to Slice 4 per
plan.

No `OWNER_DECISION_REQUIRED` branch triggered: live code confirms the
closure on all three clauses.

**SLICE 1: PASS**
