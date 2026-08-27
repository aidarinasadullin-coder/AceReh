# Phase 6 Task 3 Evidence Receipt — Immutable Snapshot and Ownership Guard Contracts

- Date: 2026-08-25
- Phase: `phase-6-project-snapshot-save-boundary`
- Task: 3 — Define immutable snapshot and ownership guard contracts
- Frozen plan SHA-256: `C56E66D2733D65CC56190A7B95B4D87F5F032AA75E83339925CEB23C2E5A4E92`
- Change class: production/test (focused tests) + architecture artifacts (evidence/notepad)

## Scope boundary

The production contract `src/Services/Project/ProjectSnapshot.cs` was created and
independently Debug-build verified in an earlier subtask of this execution lane.
THIS subtask added only the Task 3 tests, this receipt, and phase-6 notepad
entries. The production contract was not modified here, and no unrelated
production, DI, mapper, restore, export, map, widget, plan, or `STATE.json`
path was touched.

## Exact write-set (dirty baseline-relative delta)

1. `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectSnapshotContractTests.cs` — NEW, 21 focused NUnit tests.
2. `docs/architecture-migration/evidence/phase-6-project-snapshot-save-boundary/task-3-snapshot-contract.md` — NEW, this receipt.
3. `.omo/notepads/phase-6-project-snapshot-save-boundary/learnings.md` — NEW, append-only notepad.
4. `.omo/notepads/phase-6-project-snapshot-save-boundary/decisions.md` — NEW, append-only notepad.

## Contract decisions verified by executable tests

- Null rejection: every required constructor input (`projectNumber`,
  `projectObject`, four module snapshots, both collections) throws
  `ArgumentNullException` with the exact `ParamName`; null collection elements
  throw `ArgumentException` naming the offending collection parameter.
- Defensive-copy isolation: mutating caller-owned source lists after
  construction does not change `ProjectSnapshot.CustomMaterials`,
  `CustomTemplates`, nested `ProjectTemplateRecord.LayersAbovePipe`,
  `LayersBelowPipe`, or nested template `MaterialSnapshots`; escaping lists are
  read-only wrappers (`ICollection<T>.Add` throws `NotSupportedException`).
- Property shape: reflection over all four contract types
  (`ProjectSnapshot`, `ProjectCustomMaterialRecord`, `ProjectTemplateRecord`,
  `ProjectTemplateLayerRecord`) proves every public instance property has no
  setter (`CanWrite == false`).
- Canonical module typing: `ClimateStateSnapshot`, `ConstructionStateSnapshot`,
  `ThermalStateSnapshot`, `HydraulicsStateSnapshot` are used exactly, by type.
- Runtime/UI/date exclusion: no property name matches `CurrentFilePath`,
  `FilePath`, `Dirty`, `LoadProjectInProgress`, `Restore`, `CreatedDate`,
  `ModifiedDate`. The property graph references no
  `SnowMeltingCalculator.ViewModels` and no `System.Windows` namespace.
- Ownership guard (scoped strictly to the new contract): `ProjectSnapshot` is
  `sealed`, declares no events, has only init-only instance fields, exposes no
  public lifecycle mutator methods, and implements no interface. No claims are
  made about unrelated legacy state owners.
- Value round-trip: identity/mode scalars are stored as provided and the four
  module snapshots are stored by reference as provided; empty collections are
  accepted and produce empty lists.

### Dates note

Dates are intentionally excluded from `ProjectSnapshot`.
`CreatedDate`/`ModifiedDate` remain explicit save-operation inputs for the
later save tasks (Task 4 assembly / Task 5 persistence). No date semantics are
asserted or invented in Task 3 beyond provable absence from the snapshot.

## Commands and results (actual output)

1. `dotnet test --configuration Debug --filter "FullyQualifiedName~ProjectSnapshot" --nologo`
   - First attempt: RED at compile — CS1739 named-argument case mismatches
     (`HasLoads:`/`IsBuiltIn:`/`IsOperatingMode:` vs lower camelCase ctor
     parameters) in the new focused test only. Corrected in-place; no assertion
     was weakened or removed.
   - Final result: **exit code 0**;
     `Пройдено! : не пройдено 0, пройдено 24, пропущено 0, всего 24,
     длительность 35 ms` — i.e. **24 passed / 0 failed / 0 skipped**.
   - Inventory: the new fixture contributes exactly 21 tests; the remaining 3
     matches are pre-existing `ClimateStateTests` methods whose FQNs contain
     `ApplyProjectSnapshot` (`ApplyProjectSnapshot_Load_ChangesStateDoesNotMarkDirty`,
     `ApplyProjectSnapshot_SameData_IsNoOp`,
     `ApplyProjectSnapshot_User_ChangesStateAndMarksDirty`). All 24 passed.
2. `dotnet build src\SnowMeltingCalculator.csproj -c Debug --nologo`
   - Result: **exit code 0**; `Сборка успешно завершена. Предупреждений: 0,
     Ошибок: 0`.

## LSP status

`lsp_diagnostics` was attempted once on the changed C# test file and returned
the exact harness error `LSP file path must be inside request cwd`. This is the
known environment limitation already recorded in the dossier; C# correctness
gates therefore remain the direct `dotnet build`/`dotnet test` commands above.

## Residual risks / caveats

- The filtered test run intentionally includes 3 pre-existing
  `ApplyProjectSnapshot_*` climate tests because the vstest substring filter
  cannot exclude them; they passed and are counted transparently above.
- Task 4 (snapshot assembly/mapper) remains blocked until the orchestrator
  independently verifies this write-set.

## Verdict

CONTRACTS: PASS

Both executable gates pass after the recorded in-task correction; the initial
RED compile attempt is retained above as honest history, not hidden.
