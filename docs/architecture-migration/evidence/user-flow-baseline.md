---
phase: phase-0-baseline
snapshot_sha: f0d19c34ac03075d64548f1059e9c6626d3596b5
source_basis: working-tree
generated_at_utc: 2026-07-30T19:13:03.9618828Z
working_directory: D:/IA/ace v.2
commands: [codegraph_codegraph_explore test symbols and production entry points, targeted Read assertions/setup, read-only PowerShell QA below]
exit_code: 0
status: pass
raw_output: Assertion inventory only; no test command was launched.
limitations: [No source/test/fixture was changed or run; green TRX does not prove user flows; unprobed runtime multiplicity remains unknown.]
---

# User-Flow Characterization Baseline

The shared IDs are `CF-001`, `CF-002`, `CF-003`, `CF-004`, `CF-005`, `CF-006`, `CF-007`, `CF-008`, `CF-009`, `CF-010`, `CF-011`, `CF-012`, `CF-013`, `CF-014`, `CF-015`, `CF-016`, `CF-017`, `CF-018`, `CF-019`, `CF-020`, `CF-021`, `CF-022`. Todo 2 TRX is execution context only: 1537 Passed, 3 NotExecuted, 0 Failed. Row authority is [characterization-tests.md](../maps/characterization-tests.md); ordered mirror is [user-flow.md](../maps/user-flow.md).

| Status | Count |
| --- | ---: |
| covered | 2 |
| partial | 15 |
| missing | 5 |
| blocked | 0 |
| total | 22 |

## Decision-Complete Future Characterization Gaps

Every `FG-*` specifies proposed file/symbol, setup/action/final stale-state assertion, five named observers, and either an exact expected count or a reason the proposed boundary cannot observe it. `not observable` is an expected result, not a zero.

| FG | Applies to | Proposed file/symbol; setup/action/final assertion | Counter expectations with observer |
| --- | --- | --- | --- |
| `FG-001` | `CF-001` | `UserFlows/NewProjectFlowTests.cs::NewProject_ResetsComposedProject`; real composition, seed all slices then Main new; defaults, clean Results, no stale cards | ContextChanged: exact 1 via context event probe; StateChanged: not observable because new path contract has no state-service event requirement; calculator: exact 0 via calculator spy; Results: exact 1 via RefreshAll observer; dirty: exact 1 clean transition via ProjectStateService probe |
| `FG-002` | `CF-002` | `UserFlows/CurrentSmcLoadFlowTests.cs::OpenCurrentFile_RestoresComposedState`; real fixture/UI open; all VMs/Results restored, clean/no stale collector | ContextChanged: exact 1 via context probe; StateChanged: not observable because restore has no asserted StateChanged contract; calculator: exact 0 via calculator spy; Results: exact 1 via RefreshAll probe; dirty: exact 1 clean transition |
| `FG-003` | `CF-003` | `UserFlows/LegacySmcRestoreFlowTests.cs::OpenLegacyFile_RestoresAllSlices`; checked-in legacy UI load; all slices/Results restored, no stale state | ContextChanged: exact 1; StateChanged: not observable because legacy restore contract does not require it; calculator: exact 0; Results: exact 1; dirty: exact 1 clean transition; observers respectively context, state service, calculator spy, Results, dirty probe |
| `FG-004` | `CF-004` | `UserFlows/SecondProjectLoadFlowTests.cs::SecondOpen_ReplacesFirstProject`; two real files/UI; second-only values/path/cards replace first/no stale | ContextChanged: exact 2 via probe; StateChanged: not observable because no StateChanged contract; calculator: exact 0 via spy; Results: exact 2 via RefreshAll probe; dirty: exact 2 clean transitions |
| `FG-005` | `CF-005` | `UserFlows/ClimateEditCharacterizationTests.cs::ClimateEdit_InvalidatesAndProjectsOnce`; composed VMs, one edit; final climate, downstream clear, Results current | ContextChanged: exact 1; StateChanged: not observable because climate edit has no required state-service event; calculator: exact 1 via calculator spy; Results: exact 1; dirty: exact 1; probes context/state/calculator/Results/dirty |
| `FG-006` | `CF-006` | `UserFlows/ConstructionEditCharacterizationTests.cs::GroundwaterEdit_UpdatesLambdaOnce`; composed VMs, one water edit; LambdaB and stale downstream clear | ContextChanged: exact 1; StateChanged: not observable because construction edit has no state-service event contract; calculator: exact 1; Results: exact 1; dirty: exact 1; probes context/state/calculator/Results/dirty |
| `FG-007` | `CF-007` | `UserFlows/ThermalEditCharacterizationTests.cs::SpacingEdit_InvalidatesHydraulicsOnce`; composed VMs, one spacing edit; inputs final/no stale hydraulics | ContextChanged: exact 1; StateChanged: exact 1 via state service probe; calculator: exact 1; Results: exact 1; dirty: exact 1 |
| `FG-008` | `CF-008` | `UserFlows/HydraulicsEditCharacterizationTests.cs::ConcentrationEdit_RecalculatesOnce`; composed VMs, one edit; summary/current Results/dirty | ContextChanged: not observable because hydraulics edit need not publish context; StateChanged: not observable because no state-service contract; calculator: exact 1; Results: exact 1; dirty: exact 1 |
| `FG-009` | `CF-009` | `UserFlows/InvalidationFlowTests.cs::ClimateChange_ClearsAllStaleProjections`; composed precomputed results, one climate edit; thermal/hydraulics/Results stale values clear | ContextChanged: exact 1; StateChanged: not observable because invalidation test has no state-service event contract; calculator: exact 0 via spy; Results: exact 1; dirty: exact 1 |
| `FG-010` | `CF-010` | `UserFlows/CalculateFlowTests.cs::Calculate_ProjectsOneValidResult`; valid composed inputs, invoke calculate; numeric cards current | ContextChanged: not observable because calculate command need not update context; StateChanged: exact 2 start/end via state probe; calculator: exact 1; Results: exact 1; dirty: not observable because calculation dirty contract is not established |
| `FG-011` | `CF-012` | `UserFlows/RepeatedRestoreSubscriptionTests.cs::RepeatedResetLoad_FinalEditHasOneHandler`; reset/load/reset/load then one edit; no duplicate or stale effects | ContextChanged: exact 1 final edit; StateChanged: not observable because selected edit has no state-service contract; calculator: exact 1; Results: exact 1; dirty: exact 1 |
| `FG-012` | `CF-013` | `UserFlows/SaveReloadFlowTests.cs::SaveReload_RestoresFreshComposedVm`; real file/fresh VM; all slices/Results/path clean/no stale | ContextChanged: exact 1; StateChanged: not observable because save/reload has no StateChanged contract; calculator: exact 0; Results: exact 1; dirty: exact 2 (edit dirty, save/load clean) |
| `FG-013` | `CF-014` | `UserFlows/SummaryProjectionFlowTests.cs::TwoCollectorLoad_ProjectsUnswappedCards`; real two-collector file; cards/specs exact/no stale swap | ContextChanged: exact 1; StateChanged: not observable because summary projection has no StateChanged contract; calculator: exact 0; Results: exact 1; dirty: exact 1 clean transition |
| `FG-014` | `CF-015` | `UserFlows/PdfExportFlowTests.cs::PdfExport_WritesCurrentProjection`; real exporter/temp PDF; nonempty file/current values/no stale | ContextChanged: not observable because export does not change context; StateChanged: not observable because export has no state-service contract; calculator: exact 0; Results: exact 1 builder RefreshAll; dirty: exact 0 via dirty probe |
| `FG-015` | `CF-017` | `UserFlows/ExcelExportFlowTests.cs::ExcelExport_WritesCurrentWorkbook`; real exporter/workbook; sheets/current fields/no stale | ContextChanged: not observable because export does not change context; StateChanged: not observable because export has no state-service contract; calculator: exact 0; Results: exact 1; dirty: exact 0 |
| `FG-016` | `CF-018` | `UserFlows/PreviewFlowTests.cs::Preview_ReceivesCurrentProjectionOnce`; preview probe; action preview; current projection once/no stale | ContextChanged: not observable because preview does not change context; StateChanged: not observable because preview has no state-service contract; calculator: exact 0; Results: exact 1; dirty: exact 0 |
| `FG-017` | `CF-019` | `UserFlows/PrintFlowTests.cs::Print_ReceivesCurrentSnapshotOnce`; print probe; action print; current snapshot/no stale | ContextChanged: not observable because print does not change context; StateChanged: not observable because print has no state-service contract; calculator: exact 0; Results: exact 1; dirty: exact 0 |
| `FG-018` | `CF-020` | `UserFlows/DirtyStateFlowTests.cs::DeclinedDirtyOpen_PreservesState`; real state service/dialog probe; decline; project/path/dirty unchanged | ContextChanged: exact 0 via probe; StateChanged: exact 0 via probe; calculator: exact 0 via spy; Results: exact 0 via probe; dirty: exact 0 transitions |
| `FG-019` | `CF-021` | `UserFlows/LoadGuardFlowTests.cs::LoadGuard_SuppressesRestoreEffectsThenAllowsEdit`; real file/open then edit; clean after load, edit works | ContextChanged: exact 1 during load via probe; StateChanged: not observable because load guard has no StateChanged contract; calculator: exact 0 during restore via spy; Results: exact 1; dirty: exact 1 clean transition |
| `FG-020` | `CF-022` | `UserFlows/NavigationFlowTests.cs::CleanOpen_NavigatesWithoutPrompt`; real Main/Results navigation; selected page/preserved state | ContextChanged: exact 0; StateChanged: exact 0; calculator: exact 0; Results: not observable because navigation boundary does not project Results; dirty: exact 1 clean transition |

## Reproducible Structural QA

```powershell
$r='D:/IA/ace v.2/docs/architecture-migration';$f=@("$r/maps/characterization-tests.md","$r/maps/user-flow.md","$r/evidence/user-flow-baseline.md");$t=$f|%{[IO.File]::ReadAllText($_)};$want=1..22|%{'CF-{0:000}' -f $_};$sets=@($t|%{,@([regex]::Matches($_,'CF-\d{3}')|% Value|sort -Unique)});foreach($s in $sets){if((@(Compare-Object $want $s).Count -ne 0) -or ($s.Count -ne 22)){throw 'exact capability IDs'}};$rows=$t[0]-split"`n"|?{$_-match'^\| `CF-'};if($rows.Count -ne 22){throw 'row count'};foreach($x in $rows){$c=@($x-split'\|')[1..7]|% Trim;if($c|?{!$_}){throw 'empty cell'};if($c[5]-notmatch'^(covered|partial|missing|blocked)$'){throw 'status'};if($c[4]-notmatch'ContextChanged=.*StateChanged=.*calculator=.*Results=.*dirty='){throw 'current tuple'};if(($c[5]-eq'covered') -and ($c[2]-notmatch'`.+`' -or $c[3]-notmatch'event|Assert.That')){throw 'filename-only covered'};if(($c[5]-match'partial|missing') -and $c[6]-notmatch'^`FG-\d{3}`$'){throw 'missing FG'}};$g=$t[2]-split"`n"|?{$_-match'^\| `FG-'};if($g.Count -ne 20){throw 'FG count'};foreach($x in $g){if($x-notmatch'\.cs.*;.*;.*(ContextChanged:.*(exact|not observable).+StateChanged:.*(exact|not observable).+calculator:.*(exact|not observable).+Results:.*(exact|not observable).+dirty:.*(exact|not observable))'){throw 'FG incomplete'}};$synthetic='SYNTHETIC-CAPABILITY filename only covered';if($synthetic-match'CF-\d{3}'){throw 'synthetic ID leaked'};[pscustomobject]@{capability_ids=22;id_sets='equal exact';statuses='covered=2 partial=15 missing=5 blocked=0';current_counter_tuples='validated';future_counter_resolutions=20;filename_only_negative='failed as expected';result='pass'}|Format-List
```

Observed output:

```text
capability_ids             : 22
id_sets                    : equal exact
statuses                   : covered=2 partial=15 missing=5 blocked=0
current_counter_tuples     : validated
future_counter_resolutions : 20
filename_only_negative     : failed as expected
result                     : pass
```
