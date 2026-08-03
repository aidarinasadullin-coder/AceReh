---
phase: phase-0-baseline
snapshot_sha: f0d19c34ac03075d64548f1059e9c6626d3596b5
source_basis: working-tree
generated_at_utc: 2026-07-30T20:19:23.9695290Z
working_directory: D:/IA/ace v.2
commands:
  - codegraph_codegraph_explore ProjectData ProjectFileService ProjectLoadOrchestrator ResultsViewModel persistence
  - targeted Read ProjectFileService.cs ProjectLoadOrchestrator.cs ResultsViewModel.cs and cited tests
exit_code: 0
status: pass
raw_output: Source-backed layered persistence map and read-only QA.
limitations:
  - Atomic replacement, crash safety, semantic whole-project round trip, and transactional restore are not established.
---

# Persistence Map

## Scope and stable nodes

| ID | Node/boundary | State IDs | Evidence | Confidence | Status |
| --- | --- | --- | --- | --- | --- |
| `PN-01` | file path/existence/read Result boundary | `ST-002`,`ST-023` | `ProjectFileService.cs:167-190` | verified | current |
| `PN-02` | JSON serializer/deserializer options | `ST-023` | `ProjectFileService.cs:19-28,50,103,125,177` | verified | current |
| `PN-03` | version observation and legacy layer ordering | `ST-009`,`ST-010`,`ST-023` | `ProjectLoadOrchestrator.cs:335-350` | verified | current |
| `PN-04` | module restore coordinator | `ST-006`-`ST-022` | `ProjectLoadOrchestrator.cs:76-232` | verified | current |
| `PN-05` | load guard, refresh, event, clean finalization | `ST-001`,`ST-003`-`ST-005`,`ST-024`-`ST-027` | `ResultsViewModel.cs:1573-1607` | verified | current |
| `PN-06` | snapshot assembly | `ST-001`,`ST-003`,`ST-006`,`ST-008`-`ST-018`,`ST-023` | `ResultsViewModel.cs:1613-1817` | verified | current |
| `PN-07` | Result save temp/backup/move/catch-cleanup | `ST-002`,`ST-023` | `ProjectFileService.cs:115-163` | verified | current |
| `PN-08` | normal UI open boundary/reset sequence | `ST-001`,`ST-005`,`ST-006`-`ST-022` | `ResultsViewModel.cs:798-825` | verified | current |
| `PN-09` | explicit save/export snapshot entry | `ST-001`,`ST-003`,`ST-006`,`ST-008`-`ST-018`,`ST-023` | `ResultsViewModel.cs:945-957,1613-1817` | verified | current |

## Typed edges

| Edge ID | From -> To | Kind | Evidence | Confidence | Status |
| --- | --- | --- | --- | --- | --- |
| `PE-01` | `PN-01 -> PN-02` | persistence-read | `ProjectFileService.cs:171-184` | verified | current |
| `PE-02` | `PN-02 -> PN-03` | persistence-transform | `ProjectLoadOrchestrator.cs:335-350` | verified | current |
| `PE-03` | `PN-03 -> PN-04` | persistence-restore | `ProjectLoadOrchestrator.cs:80-232` | verified | current |
| `PE-04` | `PN-04 -> PN-05` | restore-finalization-sequence | `ResultsViewModel.cs:1593-1606` | verified | current |
| `PE-05` | `PN-09 -> PN-06` | user-action/save-entry | `ResultsViewModel.cs:945-957,1613-1817` | verified | current |
| `PE-06` | `PN-06 -> PN-07` | persistence-write | `ProjectFileService.cs:115-163` | verified | current |
| `PE-07` | `PN-01 -> PN-08` | user-action/load-boundary | `ResultsViewModel.cs:798-825` | verified | current |
| `PE-08` | `PN-08 -> PN-05` | reset-before-restore | `ResultsViewModel.cs:813-821` | verified | current |

## Exact entry boundaries and save sequence

Normal UI open calls `ApplyLoadedProjectAsync(filePath, data)`: after its dirty confirmation it calls `ResultsViewModel.Reset()`, then `_projectLoadOrchestrator.ResetModules()`, marks clean, calls `LoadProjectDataAsync(data)`, assigns file path, and marks clean again ([ResultsViewModel.cs](/D:/IA/ace%20v.2/src/ViewModels/Results/ResultsViewModel.cs:798)). Direct `LoadProjectDataAsync(data)` sets the load guard, restores identity, calls `RestoreModulesFromProjectAsync`, calls one `RefreshAll`, raises `ProjectChanged`, marks clean, and clears the guard; it does **not** call either reset boundary ([ResultsViewModel.cs](/D:/IA/ace%20v.2/src/ViewModels/Results/ResultsViewModel.cs:1573)).

Both save APIs serialize the supplied snapshot, name temp with `Path.ChangeExtension(filePath, ".tmp")`, write that temp, conditionally copy an existing destination to `filePath + ".bak"`, execute `File.Move(tempPath, filePath, overwrite: true)`, and on any caught exception attempt to delete the same `.tmp` path. `File.Move` is not a reload edge. A reload is only a separate user/test initiated load call (`ProjectRoundTripTests.SaveThenLoad_NewProject_RoundTripsFields`); no automatic save-to-reload behavior is documented.

## API-specific boundary matrix

| Boundary | Current | Legacy | Corrupt/unsupported | Tested | Deferred |
| --- | --- | --- | --- | --- | --- |
| obsolete load `LoadProjectAsync` | exact provided path; `ProjectData?` | v1 fixture accepted | `null`, debug detail only | `ProjectRoundTripTests` | validation policy not established |
| Result load `LoadProjectResultAsync` | exact provided path; `OperationResult<ProjectData>` | no version dispatch except later ordering | missing: `Файл не найден: {path}`; null: fixed deserialization text; exception: text plus exception | `ProjectFileServiceResultTests` | compatibility duration not established |
| obsolete save `SaveProjectAsync` | appends `.smc` case-insensitively; `bool` | same serializer | `false`; debug detail | `ProjectRoundTripTests`, `ProjectFileServiceAtomicityTests`, `ProjectFileServiceMutationTests` | narrow backup/cleanup assertions only |
| Result save `SaveProjectResultAsync` | appends `.smc`; success null payload | same serializer | `Failure(ex.Message, ex)` | `ProjectFileServiceResultTests` | source-equivalent sequence only; obsolete atomicity/backup tests not transferred |
| cancellation | save write receives token; catch converts exception | same | Result load read receives token; catch returns failure | no cancellation-specific test found | cancellation semantics beyond source not established |
| restore | ordered assignments/final thermal behavior | pre-1.1 above layers reverse | failed deserialize never enters restore | selected results/open tests | transactional restore not established |
| temp/backup/move | temp -> conditional `.bak` -> overwrite move -> catch cleanup | same | catch attempts cleanup | obsolete tests only | atomic/crash safety not established |
| reload | separate open/load invocation only | same | n/a | explicit SaveThenLoad test call | no automatic save reload |

## Read-only QA

```powershell
$m=Get-Content -Raw 'docs/architecture-migration/maps/persistence.md'
$nodeSection=$m.Substring($m.IndexOf('## Scope and stable nodes'),$m.IndexOf('## Typed edges')-$m.IndexOf('## Scope and stable nodes'))
$edgeSection=$m.Substring($m.IndexOf('## Typed edges'),$m.IndexOf('## Exact entry boundaries')-$m.IndexOf('## Typed edges'))
$nodes=[regex]::Matches($nodeSection,'\| `PN-\d{2}` \|')|ForEach-Object Value
$edges=[regex]::Matches($edgeSection,'\| `PE-\d{2}` \|')|ForEach-Object Value
if($nodes.Count -ne 9 -or ($nodes|Sort-Object -Unique).Count -ne 9){throw 'PN IDs'}
if($edges.Count -ne 8 -or ($edges|Sort-Object -Unique).Count -ne 8){throw 'PE IDs'}
if($edgeSection -match 'persistence-reload'){throw 'automatic reload edge'}
if($m -notmatch 'Reset\(\).*ResetModules\(\).*LoadProjectDataAsync'){throw 'normal-open-reset-order'}
if($m -notmatch 'does \*\*not\*\* call either reset boundary'){throw 'direct-load-distinction'}
'PASS nodes=9; edges=8; boundaries=8; automatic-reload=absent'
```

Observed output (exit `0`): `PASS nodes=9; edges=8; boundaries=8; automatic-reload=absent`.

## DoneClaim

**DoneClaim PERSIST-MAP-03:** 9 nodes, 8 typed edges, and 8 API-aware boundaries distinguish normal UI open/reset from direct restore, explicit save/export entry from load finalization, Result APIs from obsolete bool/null APIs, and save from separately initiated reload. The observed save sequence is exactly temp write, conditional backup copy, overwrite move, then catch cleanup on exception. Atomicity, crash safety, byte identity, compatibility duration, semantic whole-project round trip, and transactional in-memory restore are not established.
