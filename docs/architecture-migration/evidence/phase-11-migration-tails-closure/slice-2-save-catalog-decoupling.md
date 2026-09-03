# Slice 2 — Catalogs decoupled from the save wire (DEC-006, owner-directed re-scope)

Phase 11 (`phase-11-migration-tails-closure`). This slice **stopped once** at
a grounding contradiction and was re-scoped by an explicit owner decision
(DEC-006, recorded in `TASK_CONTEXT.md`), then executed.

## The stop and the decision

The frozen plan asserted "No `ResultsViewModel` change" while switching
`IProjectSnapshotPersistenceInputs.Templates` to async. Live code refuted the
premise: `ResultsViewModel.SaveCurrentProject()` (`:1730`) also read
`Templates`, with ~25+ test call sites around the sync public API. Per the
plan's own contract the lane stopped (`OWNER_DECISION_REQUIRED`); presented
with the options, the owner chose the architecturally stronger path
(**DEC-006**): catalogs live only globally — complete LIM-P8-2 decision B —
and the two never-read wire members are removed from `ProjectData`
entirely (sub-variant (a)). Old tests asserting the embedding are outdated
and were updated/removed (owner-blessed).

## Production write-set (exact)

- `src/Models/Project/ProjectData.cs` — removed `CustomMaterials` and
  `CustomTemplates` (write-only members: the import-less restore never read
  them back; verified no consumers).
- `src/Services/Project/ProjectSnapshot.cs` — removed
  `ProjectCustomMaterialRecord`, `ProjectTemplateLayerRecord`,
  `ProjectTemplateRecord`, the two snapshot members and the ctor args.
- `src/Services/Project/ProjectPersistenceMapper.cs` — removed the two wire
  fills and their helpers; `Version` stays `"1.1"` (removed members are
  optional JSON collections with zero consumers; a bump would orphan the 28
  tracked hashed `.smc` fixtures — DEC-002 covers format freedom).
- `src/Services/Project/IProjectSnapshotPersistenceInputs.cs` /
  `ProjectSnapshotPersistenceInputs.cs` — the seam reduces to
  `IsOperatingMode`; the sync-over-async `Templates` getter
  (`GetAllAsync().GetAwaiter().GetResult()`) **is deleted with its code path**.
- `src/Services/Project/ProjectSnapshotFactory.cs` — no catalog reads.
- `src/ViewModels/Results/ResultsViewModel.cs` — removed both catalog fills in
  `SaveCurrentProject` and the now-dead `_persistenceInputs` field + optional
  ctor parameter (verified unused; DI unaffected — interface binding kept).

The frozen plan's clause "No `.smc` schema/persistence-format change, no
`ProjectData` wire-shape change" is superseded for exactly these two members
by DEC-006 (recorded). Old files carrying `custom_materials`/
`custom_templates` JSON keep loading: default `System.Text.Json`
deserialization ignores unknown members — pinned by a re-pinned test.

## Gates

- `grep -rn "GetAwaiter().GetResult()" src --include="*.cs"` (excl. obj): the
  save-chain instance is **gone**. One **pre-existing, unrelated** instance
  remains at `src/Repositories/Hydraulics/CollectorRepository.cs:99` (hydraulics
  collector repository; outside the save path and this phase's scope) —
  recorded and forwarded to the audit-P1/P2 backlog.
- `grep -rn "CustomMaterials|CustomTemplates" src --include="*.cs"`: **0**
  (code and comments).
- New hash-pin characterization test
  (`ProjectSnapshotFactoryTests.Create_ToProjectData_HashPinStaysStable_AcrossCatalogEmbeddingRemoval`)
  pins the SHA-256 of the full serialized `ProjectData` produced by
  factory+mapper on a fixed fixture:
  `FBD2010C0C8BF0F1552BE48F4CFAFF30A35ACFA57CA42D7DA0F39A2729B1B7B5`
  (captured on the post-change build; any snapshot/mapper drift fails the
  pin).

## Suites (plan-exact filter) and test updates

Build 0 warnings / 0 errors. Result: **37 passed / 0 failed / 0 skipped**.
TRX: `logs/slice-2-save-catalog-decoupling.trx`.

Updated/removed as outdated (owner-blessed under DEC-006):
`ProjectSnapshotFactoryTests` (rewritten: read-once semantics without
catalogs + the pin), `ProjectSnapshotContractTests` (rewritten: catalog
records gone; DEC-006 absence guards added), `ProjectPersistenceMapperTests`
(DEC-006 wire-absence assertions replace the embedding round-trip),
`ProjectSaveServiceTests` (snapshot helper signature), `ConstructionServiceTests`
(two embedding round-trip tests removed; LIM-P8-2 load test and old-file
deserialization re-pinned), `ProjectLifecycleFlowCharacterizationTests` /
`ThermalMultiplicityCharacterizationTests` (dead catalog assignments dropped),
`ResultsViewModelOpenProjectTests` (two catalog tests re-pinned into one
DEC-006 guard). Frozen contract suites for mapper/file-service semantics pass
unmodified.

**SLICE 2: PASS (re-scoped by DEC-006)**
