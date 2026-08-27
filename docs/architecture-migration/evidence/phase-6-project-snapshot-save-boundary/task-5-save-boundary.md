# Phase 6 Task 5 — save boundary test and source guards

## Scope and write-set

Task 5 test/evidence slice is limited to:

- `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectSaveServiceTests.cs` (new)
- `docs/architecture-migration/evidence/phase-6-project-snapshot-save-boundary/task-5-save-boundary.md` (this receipt)
- append-only Phase 6 notepad entries (`learnings.md`, `decisions.md`)

No production code, DTO/schema, serializer, load/restore/export/Markdown/calculation
code, fixtures, maps, widget, STATE, or protected unrelated paths were changed.
The production `ProjectSaveService` was already implemented and build-green; this
slice adds the missing characterization test and the source guards only.

## Implementation evidence

The new test exercises the existing production `ProjectSaveService` through its
live signatures (unchanged):

- `ProjectSaveService(IProjectSnapshotFactory, IMaterialRepository, IProjectFileService)`
- `SaveAsync(IProjectSession, string, ProjectSaveDates, CancellationToken) -> Task<OperationResult<object?>>`
- `IProjectSnapshotFactory.Create(IProjectSession) -> ProjectSnapshot`
- `IProjectFileService.SaveProjectResultAsync(string, ProjectData, CancellationToken) -> Task<OperationResult<object?>>`

Behavioral coverage (Moq, `Mock<IProjectSession>` pass-through, `Mock<IMaterialRepository>` Loose):

- **Success mapping** — captures the `ProjectData` DTO produced by
  `ProjectPersistenceMapper` and asserts the six mapped fields:
  `Version == "1.1"`, `ProjectNumber`, `ProjectObject`,
  `CreatedDate` (from `ProjectSaveDates.CreatedDate`),
  `ModifiedDate` (from `ProjectSaveDates.ModifiedDate`), `IsOperatingMode`.
- **Exactly one call each** — `Verify(f => f.Create(...), Times.Once)` and
  `Verify(s => s.SaveProjectResultAsync(...), Times.Once)`.
- **Failed result unchanged** — a failed `OperationResult<object?>` returned by
  the file service is returned unchanged (reference identity `Is.SameAs`).
- **Exception propagation** — a file-service throw propagates
  (`Throws.TypeOf<InvalidOperationException>`); the boundary performs no catch.
- **Cancellation token passthrough** — the `CancellationToken` supplied to
  `SaveAsync` reaches `SaveProjectResultAsync` unchanged (captured in callback,
  `Is.EqualTo`).

Source guards (no production change):

- `src/Services/Project/ProjectSaveService.cs` contains no `ViewModel`,
  `System.Windows`, `DependencyObject`, or `DependencyProperty` references.
- The `SaveToFile` slice of `src/ViewModels/Results/ResultsViewModel.cs`
  (from `private async Task<bool> SaveToFile` up to the `SaveLegacyFileAsync`
  boundary) contains `_projectSaveService.SaveAsync` and does NOT contain
  `SaveCurrentProject`, proving the new boundary is wired and the legacy path
  is not used in that slice.

The snapshot is built with the live `ProjectSnapshot` constructor (9 parameters:
`projectNumber`, `projectObject`, `isOperatingMode`, four state snapshots,
custom materials, custom templates) using `ClimateStateSnapshot`,
`ConstructionStateSnapshot`, `ThermalStateSnapshot.Default`,
`HydraulicsStateSnapshot.Default`, and empty material/template collections,
mirroring the Task 4 factory-test pattern. With empty construction layers the
mapper makes no `IMaterialRepository` calls, so the Loose mock is sufficient.
(The earlier draft of this receipt said "10 parameters"; the live constructor
declared in `src/Services/Project/ProjectSnapshot.cs` has exactly nine.)

## Commands and results

```text
dotnet test --configuration Debug --filter "FullyQualifiedName~ResultsViewModelOpenProjectTests|FullyQualifiedName~ProjectFileService|FullyQualifiedName~ProjectSaveService|FullyQualifiedName~ProjectSnapshot" --nologo
83 passed / 1 skipped / 0 failed / 84 total

dotnet build src\SnowMeltingCalculator.csproj -c Debug --nologo
0 warnings / 0 errors
```

`lsp_diagnostics` was attempted once on the new test file; the environment
returned the known limitation: `LSP file path must be inside request cwd`.
Compiler and focused test gates are therefore the correctness evidence,
consistent with the migration dossier.

### Fresh re-verification (2026-08-26)

Both gates were re-run in the finishing session against the on-disk artifact;
the totals above were confirmed by rerun, not copied:

```text
dotnet test --configuration Debug --filter "FullyQualifiedName~ResultsViewModelOpenProjectTests|FullyQualifiedName~ProjectFileService|FullyQualifiedName~ProjectSaveService|FullyQualifiedName~ProjectSnapshot" --nologo
не пройдено 0, пройдено 83, пропущено 1, всего 84

dotnet build src\SnowMeltingCalculator.csproj -c Debug --nologo
Сборка успешно завершена. Предупреждений: 0, Ошибок: 0
```

The test file was additionally checked symbol-by-symbol against live source
(`ProjectSnapshot` 9-parameter ctor, `ClimateStateSnapshot` 11-parameter ctor
with `ClimateZone.Zone_M15`, `ConstructionStateSnapshot(double, bool,
IReadOnlyList<ConstructionLayerSnapshot>, IReadOnlyList<ConstructionLayerSnapshot>)`,
`ThermalStateSnapshot.Default`, `HydraulicsStateSnapshot.Default`,
`ProjectSaveDates(CreatedDate, ModifiedDate)`, `OperationResult<T>.Success/
Failure` in `SnowMeltingCalculator.Core.Results`, `IMaterialRepository` in
`SnowMeltingCalculator.Repositories.Construction`, `ProjectData` in
`SnowMeltingCalculator.Models.Project`) and uses NUnit constraint syntax only.
No further edits to the test file were required.

## Residual risks

- The new test is characterization of the already-green production boundary; no
  production behavior changed.
- Full fixture corpus and user-flow gates remain later Phase 6 tasks.

SAVE BOUNDARY: PASS
