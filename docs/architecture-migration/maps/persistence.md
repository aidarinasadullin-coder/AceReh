---
phase: phase-1-project-session-shell
snapshot_sha: 021d4abd159aa71c4a19c7a6536851264e5a58ca
source_basis: accepted-phase-1-project-session-shell
generated_at_utc: 2026-08-04T00:00:00.0000000Z
working_directory: D:/IA/ace v.2
commands:
  - codegraph_codegraph_explore ProjectData ProjectFileService ProjectLoadOrchestrator ResultsViewModel persistence
  - targeted Read ProjectFileService.cs ProjectLoadOrchestrator.cs ResultsViewModel.cs and cited tests
  - node docs/architecture-migration/widget/verify-widget.mjs --suite model-v2
  - node docs/architecture-migration/widget/verify-widget.mjs --suite runtime-v2
  - node docs/architecture-migration/widget/generate-widget.mjs --check
exit_code: 0
status: pass
raw_output: Source-backed layered persistence map updated for Phase 1 lifecycle shell.
limitations:
  - Atomic replacement, crash safety, semantic whole-project round trip, and transactional restore are not established.
  - Phase 1 preserved all `.smc` serializer, DTO, and version behavior; no schema or fixture change occurred.
---

# Persistence Map

## Phase 1 ProjectSession Shell Boundary

`ProjectSession` changes only the canonical in-memory lifecycle owner. No
persistence edge, DTO, schema, formula, package, or `.smc` wire-format edge was
changed. The existing `ResultsViewModel.LoadProjectDataAsync` path enters a
`ProjectSession` restore lease before it restores `ProjectData` identity and
delegates module restore to `ProjectLoadOrchestrator`.

| Boundary | Edge kind | Current evidence | Phase 1 result |
| --- | --- | --- | --- |
| `ProjectData.ProjectNumber` / `ProjectObject` to lifecycle owner | restore assignment | `ResultsViewModel.LoadProjectDataAsync`; `ProjectSession` | identity reaches the canonical session under its restore lease |
| `CurrentFilePath` | process lifecycle state | `ProjectSession`; save/open flow tests | remains non-persisted and updates through the existing lifecycle flow |
| dirty / restore guard | process lifecycle state | `ProjectSession`; `CalculationStateService` compatibility lease | remains non-persisted and adds no schema field |
| module data | existing DTO/module restore | `ProjectLoadOrchestrator`, `ProjectData` | owners and ordered partial restore semantics are unchanged |

The 18-passing persistence lane preserves accepted v1.0/v1.1 fixture behavior.
Transactional in-memory restore remains deferred: a module failure can leave the
characterized partial state, while the outer session lease clears the guard.

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

Normal UI open calls `ApplyLoadedProjectAsync(filePath, data)`: after its dirty confirmation it calls `ResultsViewModel.Reset()`, then `_projectLoadOrchestrator.ResetModules()`, marks clean, calls `LoadProjectDataAsync(data)`, assigns file path, and marks clean again ([ResultsViewModel.cs](/D:/IA/ace%20v.2/src/ViewModels/Results/ResultsViewModel.cs:798)). Direct `LoadProjectDataAsync(data)` enters `using var restoreScope = _projectSession.BeginProjectRestore()`, restores identity, calls `RestoreModulesFromProjectAsync`, calls one `RefreshAll`, raises `ProjectChanged`, marks clean, and clears the guard when the outer lease exits; it does **not** call either reset boundary ([ResultsViewModel.cs](/D:/IA/ace%20v.2/src/ViewModels/Results/ResultsViewModel.cs:1573)).

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

## Phase 1 ProjectSession lifecycle shell overlay

Phase 1 did **not** change persistence nodes, edges, DTOs, serializers, `.smc`
schema, fixture acceptance, or version policy. The lifecycle values that flow
into `ProjectData` (`ProjectNumber`, `ProjectObject`) and the save path
(`CurrentFilePath`) are now canonicalized in `ProjectSession`, but the
read/write boundaries (`PN-01`, `PN-07`) and the restore coordinator
(`PN-04`/`PN-08`) consume/produce the same values through forwarding adapters.

Verified preservation:

- v1.0 and v1.1 fixture corpus still loads through the production path.
- Save/reload round-trips preserve the same JSON shape and current fields.
- Restore guard is now owned by `ProjectSession.BeginProjectRestore()`; the guard
effect on `PN-05` and `PN-08` is unchanged.

Evidence: `docs/architecture-migration/evidence/phase-1-project-session-shell/lifecycle-user-flows.md`, `final-gates.md`.
