---
phase: phase-1-project-session-shell
snapshot_sha: 021d4abd159aa71c4a19c7a6536851264e5a58ca
source_basis: accepted-phase-1-project-session-shell
generated_at_utc: 2026-08-04T00:00:00.0000000Z
working_directory: D:/IA/ace v.2
commands:
  - codegraph_codegraph_explore flows
  - targeted Read
  - PowerShell QA below
  - node docs/architecture-migration/widget/verify-widget.mjs --suite model-v2
  - node docs/architecture-migration/widget/verify-widget.mjs --suite runtime-v2
  - node docs/architecture-migration/widget/generate-widget.mjs --check
exit_code: 0
status: pass
raw_output: Reactive/action inventory updated for Phase 1 lifecycle shell. Exact module counters remain unknown.
limitations:
  - Source registration is not runtime multiplicity proof; exact module counters remain unknown.
  - Phase 1 verified only ProjectSession lifecycle events and restore guard semantics.
---

# Reactive Behavior View

| Edge ID | State IDs | Publisher | Subscriber | Subscription and unsubscribe/lifetime | Trigger/action | Effect | Evidence | Confidence | ContextChanged count | StateChanged count | Calculator invocation count | Results projection update count | Dirty transition count |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `RE-001` | `ST-021`,`ST-014` | CalculationContext.ContextChanged | Circuits handler | constructor subscription; unsubscribe not observed | ThermalInputs | notify hydraulic thermal properties | Circuits :728-730,1062-1082 | verified | unknown | unknown | unknown | unknown | unknown |
| `RE-002` | `ST-022`,`ST-014`,`ST-018` | CalculationContext.ContextChanged | Circuits handler | same subscription | valid ThermalResult | CalculateAllCollectors; invalid/null does not | Circuits :1068-1082 | verified | unknown | unknown | unknown | unknown | unknown |
| `RE-003` | `ST-020`,`ST-006`,`ST-018` | `ProjectSessionClimateState.CompleteMutation()` | `ClimateData.ApplyProjection`, `CalculationContext.UpdateClimate`, Circuits handler | canonical completion sequence | Climate user/load/reset/restore/system mutation that changes snapshot | one projection update then one context publication; Circuits receives one authoritative `CalculationContext.Climate` invalidation path without duplicate recalculation | `ProjectSessionClimateState.cs`; `ClimateData.cs`; `CalculationContext.cs`; `CircuitsViewModel.cs`; evidence `downstream-invalidation.md`, `multiplicity-characterization.md`, `affected-gates.md` | verified | 1 projection event for changed mutation | 1 `CalculationContext.Climate` publication | 1 Circuits recalculation path (two glycol reads for operating/design temperatures in guard test) | Results projection reads canonical snapshot | user origin marks dirty; load/reset/restore origins do not create user dirty semantics |

## Phase 2 ClimateState acceptance overlay

The accepted Climate reactive boundary is no longer `ClimateViewModel -> ClimateData -> CalculationContext`
as independent writable steps. A changed canonical Climate mutation completes in
`ProjectSessionClimateState.CompleteMutation()`, which first applies the `ClimateData` compatibility
projection and then publishes exactly one `CalculationContext.UpdateClimate(..., "Climate")`. The
downstream Circuits path consumes that single context publication. Task 9 evidence
`downstream-invalidation.md` records the duplicate-recalculation guard; Task 11 `affected-gates.md`
records the final targeted/full-suite acceptance counts.
| `RE-004` | `ST-015`,`ST-019` | ICalculationStateService.StateChanged | Circuits handler | constructor subscription; unsubscribe not observed | state change | IsCalculating notification | CalculationStateService :146-168; Circuits :1202-1206 | verified | unknown | unknown | unknown | unknown | unknown |

## Phase 3.1 Climate invalidation overlay (Task 11)

Changed `User` and `UserReset` completions apply the projection, publish
compatibility `DataChanged`, update `CalculationContext`, and mark dirty once.
Changed `ProjectLoadReset`, `Load`, `Restore`, `SystemApply`, and
`Initialization` synchronize projection/context without compatibility publication
or user dirty semantics. Task 9 focused Debug and Release each passed `76/76`;
Task 10 affected/full Release gates passed with zero failures. Exact counters are
receipt facts, not inferred from subscription declarations.
| `RE-005` | `ST-015` | StateChanged | Thermal handler | constructor subscription; unsubscribe not observed | state change | handler body not observed | Thermal :279-280 | verified | unknown | unknown | unknown | unknown | unknown |
| `RE-006` | `ST-013` | PipeSpacingChanged | Circuits | constructor subscription; unsubscribe not observed | guarded spacing change | handler effects partial | CalculationStateService :120-139; Circuits :724-726 | verified | unknown | unknown | unknown | unknown | unknown |
| `RE-007` | `ST-013` | PipeSpacingChanged | Thermal/Construction | constructor subscriptions; unsubscribe not observed | guarded spacing change | projection effects not fully observed | Thermal :282-283; Construction :246-247 | verified | unknown | unknown | unknown | unknown | unknown |
| `RE-008` | `ST-016`,`ST-017`,`ST-004`,`ST-018` | HydraulicInputData/Collectors | Circuits handlers | old InputData explicitly unsubscribed; collection unsubscribe not observed | input/collection edit | dirty, propagation, calculate | Circuits :732-739,1113-1180 | verified | unknown | unknown | unknown | unknown | unknown |
| `RE-009` | `ST-008`,`ST-009`,`ST-010`,`ST-011`,`ST-004` | `ProjectSessionConstructionState.CompleteChanged` | `CurrentProjection`, CalculationContext, adapter and dirty owner | singleton state/adapter; repeated lifecycle hygiene covered | changed canonical mutation | refresh projection; valid User/Template publishes once; raise one Changed; origin-aware dirty | Tasks 10-12.1; pre-Task 13 correction | verified | at most 1 valid user/template publication | 1 canonical Changed | one Thermal invalidation after correction | Results/save reads canonical snapshot | 1 for changed User/Template; 0 lifecycle/no-op/rejected |

## Phase 3 Construction completion overlay

`CompleteChanged` updates `ConstructionStateProjection` before downstream
publication. Valid `User` and `Template` changes publish once through
`CalculationContext.UpdateConstruction`; lifecycle origins update canonical
state and the adapter without user dirty semantics or downstream publication;
no-op, rejected and cancelled mutations publish nothing. The pre-Task 13
correction proves Thermal consumes this path. The separate Climate ProjectLoad
indicator defect remains open and is not attributed to `RE-009`.
| `RE-010` | `ST-020`,`ST-024`,`ST-004`,`ST-006`,`ST-008`,`ST-012`,`ST-016`,`ST-017` | MainViewModel.PerformNewCalculationReset | context, Results, four module VMs | direct command path; runtime multiplicity unknown | new calculation | context reset, Results reset, clean, module resets, clean | MainViewModel.cs:178-225 | verified | unknown | unknown | unknown | unknown | unknown |
| `RE-011` | `ST-020`,`ST-024`,`ST-005`,`ST-023`,`ST-002`,`ST-004` | Results load/apply | Results/orchestrator/modules | repeat reload lifetime not proven; one load path source observed | load/reload | Results reset, modules reset, clean, guarded restore, RefreshAll, path/clean | Results :778-825,1573-1607 | verified | unknown | unknown | unknown | unknown | unknown |
| `RE-012` | `ST-020`,`ST-006`,`ST-008`,`ST-012`,`ST-016`,`ST-017`,`ST-023`,`ST-024` | ProjectLoadOrchestrator.ResetModules | context and four module VMs | **one statically observed call site from ResultsViewModel; runtime invocation multiplicity unknown** | load reset | context reset before four VM resets; restore order after entry unknown | Orchestrator :56-70; Results :813-819 | verified | unknown | unknown | unknown | unknown | unknown |
| `RE-013` | `ST-001`,`ST-002`,`ST-004`,`ST-023` | Results SaveProject/SaveAs/SaveToFile | file service/project state | command/action lifetime not applicable | save: current path or SaveAs; success writes path only for SaveAs and MarkClean | DTO snapshot, temp/bak/move file write; clean on success | Results :730-756,945-968; ProjectFileService :115-163 | verified | unknown | unknown | unknown | unknown | unknown |
| `RE-014` | `ST-024`,`ST-025`,`ST-026`,`ST-027`,`ST-023` | Results export/preview/print commands | PDF/report/export services | command/action lifetime not applicable | PDF, markdown, Excel, preview, print | RefreshAll before export input generation; builds projection/snapshot | Results :590-724,832-940,1493-1505 | verified | unknown | unknown | unknown | unknown | unknown |

## Structural QA Record

```powershell
$p='D:/IA/ace v.2/docs/architecture-migration/maps';$i=Get-Content -Raw "$p/state-inventory.md";$o=Get-Content -Raw "$p/state-ownership.md";$r=Get-Content -Raw "$p/reactive.md";$get={param($t)@([regex]::Matches($t,'(?m)^\| `(ST-\d{3})` \|')|%{$_.Groups[1].Value})};$a=@(& $get $i);$b=@(& $get $o);if(($a|select -Unique).Count-ne$a.Count -or ($b|select -Unique).Count-ne$b.Count -or (Compare-Object ($a|sort) ($b|sort))){throw 'state IDs'};foreach($x in @($i-split"`n"|?{$_-match'^\| `ST-'})){if((@($x-split'\|').Count-2)-ne12 -or $x-notmatch'\| (legacy|seam|migrated|legacy removed|verified) \|' -or $x-notmatch'\| (covered|partial|missing|blocked) \|$'){throw 'inventory columns/enums'}};foreach($token in 'ProjectNumber','ProjectObject','CurrentFilePath','IsOperatingMode','SelectedCity','ColdFiveDayTemperature','HasUserModifications','SearchQuery','SelectedMode','SupplyTemperature','GroundTemperature','SelectedPipe','HydraulicsResults','ThermalInputs','HydraulicSummaryCards','PerformNewCalculationReset','SaveToFile','RefreshAll'){if(($i+$r)-notmatch$token){throw "missing $token"}};$e=@([regex]::Matches($r,'(?m)^\| `(RE-\d{3})` \|')|%{$_.Groups[1].Value});if(($e|select -Unique).Count-ne$e.Count){throw 'edge IDs'};foreach($x in @($r-split"`n"|?{$_-match'^\| `RE-'})){if((@($x-split'\|').Count-2)-ne14 -or $x-notmatch'unknown'){throw 'reactive columns/counters'};foreach($id in @([regex]::Matches($x,'ST-\d{3}')|% Value)){if($a-notcontains$id){throw 'orphan'}}};$bad='| `ST-999` | a | b | c | d | e | f | g | h |  |  |  |';if($bad-notmatch'\|  \|'){throw 'negative missing field'};[pscustomobject]@{inventory_rows=$a.Count;ownership_rows=$b.Count;reactive_edges=$e.Count;required_flows='new/load/reset/edit/calculate/save/reload/export';negative_missing_field='failed as expected';unproven_counters='unknown';result='pass'}|fl
```

Observed output:

```text
inventory_rows : 27
ownership_rows : 27
reactive_edges : 14
required_flows : new/load/reset/edit/calculate/save/reload/export
negative_missing_field : failed as expected
unproven_counters : unknown
result : pass
```

## Phase 1 ProjectSession lifecycle shell overlay

Lifecycle events now originate from `ProjectSession` (`INotifyPropertyChanged`):

| Edge ID | State IDs | Publisher | Subscriber | Subscription | Trigger | Effect | Evidence | Confidence |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `RE-P1-001` | `ST-001`,`ST-002`,`ST-004` | `ProjectSession.PropertyChanged` | `ResultsViewModel` / `MainWindow` | constructor subscription | `ProjectNumber`, `ProjectObject`, `CurrentFilePath`, `IsDirty` mutations | UI title/status update; dirty prompt decisions | `ProjectSession.cs`; `ProjectLifecycleFlowCharacterizationTests.cs`; `lifecycle-user-flows.md` | verified |
| `RE-P1-002` | `ST-005` | `ProjectSession.BeginProjectRestore` / lease disposal | `ICalculationStateService.StateChanged` | compatibility delegate | `IsLoadProjectInProgress` true/false | guards recalculation during restore | `ProjectSession.cs`; `CalculationStateService.cs`; `restore-guard.md` | verified |

`PropertyChanged` is raised exactly once per real lifecycle mutation and never for
idempotent assignments, equal values, or nested restore scopes. Module-level
reactive edges (`RE-001` through `RE-014`) are unchanged by Phase 1.
