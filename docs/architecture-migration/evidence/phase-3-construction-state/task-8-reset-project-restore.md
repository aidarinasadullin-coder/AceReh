# Phase 3 Task 8 - Reset and project restore through ConstructionState

Date: 2026-08-14

## Scope

Task 8 acceptance covered only lifecycle reset/project restore Construction seams:

- `src/Services/Project/ProjectLoadOrchestrator.cs`
- `src/ViewModels/Construction/ConstructionViewModel.cs`
- focused lifecycle/reset/Construction tests under `tests/SnowMeltingCalculator.Tests/`

Task 9 persistence/standalone-file work was not started.

## Implementation summary

- `ProjectLoadOrchestrator` now uses `ProjectSession.ConstructionState` when a session is supplied.
- Lifecycle reset applies one canonical `ConstructionMutationOrigin.Reset` snapshot, then refreshes the adapter from the canonical snapshot.
- Project restore maps `ConstructionProjectData` into one normalized snapshot and applies it with `ConstructionMutationOrigin.ProjectLoad`, then refreshes the adapter.
- Legacy/null-session fallback remains for existing callers without `IProjectSessionConstructionState`.
- `ConstructionViewModel.ApplyLifecycleSnapshotToAdapter(...)` refreshes adapter collections/scalars under `_isRefreshing`, avoiding recursive canonical writes.
- Focused tests assert one lifecycle origin, adapter refresh, non-user lifecycle behavior, second project load replacement, order normalization, lambda override/material behavior, repeated reset subscription hygiene and restore-guard failure behavior.

## Manual inspection notes

- Reset/project restore changes are limited to the Construction lifecycle seam and focused tests.
- No `.smc` schema/version change was introduced by Task 8.
- No Task 9 save/standalone persistence code path was started.
- LSP diagnostics could not be used in this environment because the harness rejects repository paths with `LSP file path must be inside request cwd`; C# correctness was verified with `dotnet test` and `dotnet build`.

## Verification

### Targeted Task 8 suite

Command:

```powershell
dotnet test tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj -c Debug --nologo --filter "FullyQualifiedName~ProjectLifecycleFlowCharacterizationTests|FullyQualifiedName~ResetOrchestrationTests|FullyQualifiedName~ResultsViewModelOpenProjectTests|FullyQualifiedName~ConstructionMultiplicityCharacterizationTests|FullyQualifiedName~ConstructionViewModelTests"
```

Result:

```text
Пройден!   : не пройдено     0, пройдено    99, пропущено     1, всего   100, длительность 9 s. - SnowMeltingCalculator.Tests.dll (net8.0)
```

### Debug build

Command:

```powershell
dotnet build src\SnowMeltingCalculator.csproj -c Debug --nologo
```

Result:

```text
Сборка успешно завершена.
    Предупреждений: 0
    Ошибок: 0
```

## Acceptance decision

Task 8 accepted. Required gates passed and no blocker requiring `.smc` semantic, transaction, or Task 9 scope change was found.
