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

## Phase 2 ClimateState persistence overlay

`PP-006` and `PP-013..PP-020` retain the existing `.smc` Climate wire fields and version behavior.
Phase 2 changes the live source/restore boundary, not the serialized schema: `ResultsViewModel.SaveCurrentProject()`
maps `ClimateProjectData` from `_projectSession.ClimateState.Snapshot`, while `ProjectLoadOrchestrator`
applies loaded Climate DTO values through `IProjectSessionClimateState.ApplyProjectSnapshot` / reset paths with
non-user `ClimateMutationOrigin`. `ClimateViewModel` is an adapter mirror, and `ClimateData`/`IClimateData`
remains a compatibility projection updated by canonical completion. Evidence: `persistence-results.md`,
`restore-reset-routing.md`, `climate-data-projection.md`, and Task 11 `affected-gates.md`.

Task 11 accepted counts for this persistence boundary are targeted Release TRX
`total 330 / executed 329 / passed 329 / failed 0` and full Release rerun TRX
`total 1616 / executed 1613 / passed 1613 / failed 0`; the existing missing-fixture skip remains documented.

## Phase 3.1 Climate invalidation overlay (Task 11)

Persistence is verified unchanged: `.smc` Climate fields, version behavior, DTO
shape, and restore format were not modified. Lifecycle publication suppression
changes reactive invalidation only; it adds no persistence field or schema
migration. Load continues to apply DTO values to canonical `ProjectSession.ClimateState`.

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
| `PN-06` | snapshot assembly | `ST-001`,`ST-003`,`ST-006`,`ST-008`-`ST-018`,`ST-023` | `ResultsViewModel.SaveCurrentProject`; `ConstructionPersistenceMapper.ToProjectData(ProjectSession.ConstructionState.Snapshot, ...)` | verified | current |

## Phase 3 ConstructionState persistence overlay

The `.smc` schema and save literal `Version = "1.1"` are unchanged. Project
save reads `ProjectSession.ConstructionState.Snapshot` and maps it with
`ConstructionPersistenceMapper`; it does not read writable adapter collections.
Restore normalizes `ConstructionProjectData` into one canonical snapshot while
preserving v1.0 above-pipe reversal, current below-pipe order, material fallback
and lambda override semantics. Reset uses the canonical seven-layer initializer.
Evidence: Tasks 8-12.1 and the accepted pre-Task 13 correction.
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

Normal UI open calls `ApplyLoadedProjectAsync(filePath, data)`: after its dirty confirmation it calls `ResultsViewModel.Reset()`, then `_projectLoadOrchestrator.ResetModules()`, marks clean, calls `LoadProjectDataAsync(data)`, assigns file path, and marks clean again ([ResultsViewModel.cs](/D:/IA/3ace%20v.2/src/ViewModels/Results/ResultsViewModel.cs:798)). Direct `LoadProjectDataAsync(data)` enters `using var restoreScope = _projectSession.BeginProjectRestore()`, restores identity, calls `RestoreModulesFromProjectAsync`, calls one `RefreshAll`, raises `ProjectChanged`, marks clean, and clears the guard when the outer lease exits; it does **not** call either reset boundary ([ResultsViewModel.cs](/D:/IA/3ace%20v.2/src/ViewModels/Results/ResultsViewModel.cs:1573)).

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

## Phase 4 ThermalState persistence overlay (Task 14)

The `.smc` schema, Thermal DTO fields, exact 8-field result wire contract and
save literal `Version = "1.1"` are unchanged. Phase 4 changes only the live
source/restore boundary for the Thermal slice:

- **Save** reads exclusively the canonical snapshot:
  `data.ThermalData = ThermalPersistenceMapper.BuildThermalProjectData(_projectSession.ThermalState.Snapshot)`
  (`ResultsViewModel.cs:1701-1706`); it never reads `ThermalViewModel` caches or
  service state. The mapper is pure and emits exactly the eight persisted result
  fields (`PowerUp`, `PowerDown`, `PowerTotal`, `SupplyTemperature`,
  `ReturnTemperature`, `MeanTemperature`, `DeltaT`, `IsValid`;
  `ThermalPersistenceMapper.cs:79-98`); runtime-only snapshot fields, status,
  messages and origins are not persisted.
- **Restore** goes through the canonical `Restore`
  (`ProjectLoadOrchestrator.cs:127-155`): `BuildInputsCandidate` +
  `BuildSavedResult` construct candidates from the DTO; a rejected
  (out-of-range/corrupt) candidate falls back atomically to canonical defaults
  while preserving a valid saved file result; pipe resolution keeps the frozen
  structural-match-else-first-standard fallback (`ResolveStandardPipe`,
  `ThermalPersistenceMapper.cs:126-175`). Finalization publishes the restored
  valid result once via the adapter or performs exactly one fallback calculation
  (`ProjectLoadOrchestrator.cs:208-228`).
- **Second-load zero-stale (DEC-T08/AMZ-2):** `Restore` atomically replaces all
  components (inputs/result/status) of the previous project; the two
  characterization rows that pinned pre-Todo-9 stale behavior were updated to
  DEC-T08 target semantics under owner-approved AMZ-2
  (`task-9/task-9-lifecycle-restore.md` §5).
- **Compatibility rows:** `PP-008` and `PP-035..PP-052` in
  [persistence-compatibility.md](persistence-compatibility.md) retain their JSON
  names, CLR types and classifications; their save/restore evidence now cites
  the mapper and canonical state.

Executable evidence: `task-10/task-10-persistence-results.md` (V5 persistence
lane + full Release), `task-12/task-12-executable-gates.md` (frozen full Release
1946 total / 1943 passed / 0 failed / 3 accepted NotExecuted), and the UI QA
save/reload/second-load/unknown-pipe steps in
`task-13/task-13-user-flow-qa.md`. Byte identity, compatibility duration and
crash atomicity remain deferred exactly as before.

## Phase 5 HydraulicsState persistence overlay (Task 14)

The `.smc` schema, Hydraulics DTO fields and save literal `Version = "1.1"` are unchanged. Phase 5
changes only the live source/restore boundary for the Hydraulics slice:

- **Save** reads exclusively the canonical snapshot:
  `data.HydraulicsData = HydraulicsPersistenceMapper.BuildHydraulicsProjectData(_projectSession.HydraulicsState.Snapshot)`
  (`ResultsViewModel.cs:1711-1712`); it never reads `CircuitsViewModel` collections or service
  state. The mapper is pure: global inputs plus collectors/circuits/results/summary snapshots map
  one-to-one onto the existing wire fields (`HydraulicsPersistenceMapper.cs:15-118`); runtime-only
  status and origins are not persisted.
- **Restore** goes through canonical `Restore(origin=ProjectLoad)` only
  (`ProjectSessionHydraulicsState.cs:83-88`; any other origin is rejected atomically with no change
  event): `BuildRestoreCandidate(data.HydraulicsData)` normalizes the DTO (missing nested results
  reconstructed from flat circuit fields, legacy FlowRegime fallbacks preserved,
  `HydraulicsPersistenceMapper.cs:29-47,120-208`), the slice commits under `ProjectLoad`, and the
  adapter mirrors read-only (`ProjectLoadOrchestrator.cs:171-173`). The restore is re-applied after
  thermal finalization so a valid persisted project remains a lossless round-trip
  (`ProjectLoadOrchestrator.cs:197-201`).
- **Wire compat:** proven by the unmodified `ProjectRoundTripTests` plus the serialized
  round-trip characterization asserting all eight hydraulics wire groups with value checks
  (`glycolType`, concentration, spacings, per-circuit results, summaries, `version=1.1`;
  `task-6/correction-notes.md`). Second load replaces the whole project with zero stale values.

Executable evidence: `task-12/arithmetic.json` (full Release 1976 passed / 0 failed / 3 accepted
NotExecuted identities) and the UI QA save/reload/second-load/unknown-pipe steps in
`ui-qa/observations.json`. Byte identity, compatibility duration and crash atomicity remain deferred
exactly as before.
