# Phase 6 Task 4 — snapshot assembly and pure ProjectData mapping

## Scope and write-set

Task 4 implementation is limited to:

- `src/Services/Project/IProjectSnapshotPersistenceInputs.cs`
- `src/Services/Project/IProjectSnapshotFactory.cs`
- `src/Services/Project/ProjectSnapshotFactory.cs`
- `src/Services/Project/ProjectSaveDates.cs`
- `src/Services/Project/ProjectPersistenceMapper.cs`
- `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectSnapshotFactoryTests.cs`
- `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectPersistenceMapperTests.cs`
- this receipt and append-only Phase 6 notepad entries

No Results orchestration, `ProjectFileService`, `ProjectData` definitions,
restore code, serializer options, DI registration, plan files, fixtures or
protected unrelated paths were changed.

## Implementation evidence

`ProjectSnapshotFactory` reads `IProjectSession.ProjectNumber`,
`ProjectObject`, `ClimateState.Snapshot`, `ConstructionState.Snapshot`,
`ThermalState.Snapshot` and `HydraulicsState.Snapshot` once per assembly. The
factory receives custom materials/templates and operating mode only through
`IProjectSnapshotPersistenceInputs`; it does not reference a ViewModel. Custom
materials are filtered by `IsBuiltIn`, custom templates preserve `Id`, ordered
layers and material portability snapshots, and missing portability catalog
records are omitted exactly as in the legacy save path.

`ProjectPersistenceMapper` is static and pure. It accepts only
`ProjectSnapshot`, explicit `ProjectSaveDates` and `IMaterialRepository`, maps
the inline Climate DTO, delegates Construction/Thermal/Hydraulics to their
existing pure mappers, sets `Version = "1.1"`, and maps the full existing DTO
graph without dates in `ProjectSnapshot`.

Date policy is explicit and deterministic: a non-`DateTime.MinValue` prior
created date is carried forward; otherwise `CreatedDate` is `Now`; every save
attempt gets `ModifiedDate = Now`.

## Commands and results

```text
dotnet test --configuration Debug --filter "FullyQualifiedName~ProjectSnapshot|FullyQualifiedName~ProjectPersistenceMapper" --nologo
31 passed / 0 failed / 0 skipped / 31 total

dotnet build src\SnowMeltingCalculator.csproj -c Debug --nologo
0 warnings / 0 errors
```

The focused test count includes the pre-existing 24 `ProjectSnapshot` contract
tests and the new factory/mapper tests. The new compatibility test serializes
the mapped DTO with the live `ProjectFileService` options equivalent
(`camelCase`, `WhenWritingNull`, `JsonStringEnumConverter(camelCase)`) and
asserts representative top-level names, `Version`, and enum string values.
No serializer or DTO field was modified.

`lsp_diagnostics` was attempted once on each changed C# production/test file;
the environment returned the known limitation:
`LSP file path must be inside request cwd`. Compiler and focused test gates are
therefore the correctness evidence, consistent with the migration dossier.

## Residual risks

- The factory contract is intentionally not wired into save orchestration;
  that is the separately frozen Task 5 boundary.
- `IsOperatingMode` remains an explicit narrow persistence input until Task 5
  supplies the production adapter; no ViewModel dependency was introduced.
- Full fixture corpus and user-flow gates remain later Phase 6 tasks.

MAPPER: PASS
