# Phase 1 Task 9 — Full gates and scope/single-owner invariant audit

## Scope

Run final gates and inspect source to confirm Phase 1 stayed within its narrow
shell boundary: one lifecycle store, no module slices in `ProjectSession`,
unchanged `CalculationContext`, no new application-service → concrete-ViewModel
edges, and no protected dirty files were overwritten.

## Architecture audit findings

### One lifecycle store

- `src/Services/Project/ProjectSession.cs` contains only lifecycle fields:
  `_projectNumber`, `_projectObject`, `_currentFilePath`, `_isDirty`,
  `_isLoadProjectInProgress`, `_restoreDepth`, `_currentLease`.
- `src/Services/Results/ProjectStateService.cs` contains a single
  `IProjectSession _session` field and forwards all reads/writes/events.
- `src/Services/Navigation/CalculationStateService.cs` no longer has a local
  `IsLoadProjectInProgress` backing field; it holds one `_restoreLease`
  compatibility reference and delegates to `_projectSession`.

### No module slices in ProjectSession

`IProjectSession`/`ProjectSession` expose only:

- `ProjectNumber`, `ProjectObject`, `CurrentFilePath`, `IsDirty`,
  `IsLoadProjectInProgress`, `MarkDirty()`, `MarkClean()`,
  `BeginProjectRestore()`.

There are no climate, construction, thermal, hydraulics, results, calculation,
export, dialog, persistence DTO, or command members.

### CalculationContext unchanged

```bash
git diff -- src/Core/CalculationContext.cs
```

Result: no output (`CalculationContext.cs` is unchanged).

### No new service → concrete ViewModel edges

The only application-service files that reference concrete ViewModels are
pre-existing architectural debt explicitly outside Phase 1 scope:

- `src/Services/Project/ProjectLoadOrchestrator.cs`
- `src/Services/Results/ResultsPdfDataBuilder.cs`
- `src/Services/Results/HydraulicSummaryBuilder.cs`
- `src/Services/Hydraulics/CollectorTypeSelector.cs`
- `src/Services/Hydraulics/CircuitsValidator.cs`

Phase 1 did not add any new service with a concrete ViewModel constructor
parameter or field.

### Dirty-worktree fidelity

Phase 1 write-set is limited to:

Production code:
- `src/Services/Project/IProjectSession.cs` (new)
- `src/Services/Project/ProjectSession.cs` (new)
- `src/Configuration/ServiceCollectionExtensions.cs`
- `src/Services/Navigation/CalculationStateService.cs`
- `src/Services/Results/ProjectStateService.cs`
- `src/ViewModels/Results/ResultsViewModel.cs`

Tests:
- `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectSessionTests.cs` (new)
- `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectSessionLegacyStoreGuardTests.cs` (new)
- `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectLifecycleFlowCharacterizationTests.cs` (new)
- `tests/SnowMeltingCalculator.Tests/ViewModels/ResultsViewModelTestHelpers.cs`
- `tests/SnowMeltingCalculator.Tests/ViewModels/ResultsViewModelOpenProjectTests.cs`
- `tests/SnowMeltingCalculator.Tests/ViewModels/ResetOrchestrationTests.cs`
- `tests/SnowMeltingCalculator.Tests/ViewModels/MainViewModelTests.cs`
- `tests/SnowMeltingCalculator.Tests/Services/Navigation/DialogServiceThreadAffinityTests.cs`
- `tests/SnowMeltingCalculator.Tests/Construction/ConstructionServiceTests.cs`

Dossier:
- `docs/architecture-migration/AGENTS.md` (new)
- `docs/architecture-migration/plans/phase-1-project-session-shell.md` (new)
- `docs/architecture-migration/evidence/phase-1-project-session-shell/` directory (new evidence files)
- `docs/architecture-migration/TASK_CONTEXT.md`

No formulas, UI/XAML, packages, installer, publish artifacts, `.smc` schema, or
user files were modified by Phase 1. Pre-existing dirty paths remain untouched.

## Commands and results

### Final full Release gate

```bash
dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Release
```

Result:

```text
Пройден!   : не пройдено     0, пройдено  1565, пропущено     1, всего  1566
```

### Debug/Release builds

Both `dotnet build src/SnowMeltingCalculator.csproj -c Debug` and `-c Release`
exit with 0 warnings and 0 errors.

### Architecture invariant probes

- `ProjectSessionLegacyStoreGuardTests.ProjectStateService_HasNoMutableLifecycleBackingFields` — GREEN.
- `ProjectSessionLegacyStoreGuardTests.CalculationStateService_HasNoLocalRestoreGuardBackingField` — GREEN.
- `ProjectSessionTests.DependencyInjection_LifecycleConsumersShareCanonicalSession` — GREEN.

## Next step

Task 10: update the shared architecture dossier and generated widget. Update the
six filtered views (`compile-time`, `di-runtime`, `state-ownership`, `reactive`,
`persistence`, `user-flow`) and `TASK_CONTEXT.md` to record Phase 1 completion,
remaining limitations, evidence links, and the next owner gate.
