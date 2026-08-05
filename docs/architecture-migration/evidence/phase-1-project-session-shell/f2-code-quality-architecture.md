# F2 Code Quality and Architecture Review - phase-1-project-session-shell

## Scope

Independent code-quality and architecture audit of the Phase 1 `ProjectSession`
lifecycle shell. Review canonical ownership, adapter statelessness, DI lifetime,
event semantics, error handling, constructor graph, and maintainability. Explicitly
inspect for dual stores, god-object growth, module ownership migration, concrete-VM
service dependencies, and `CalculationContext` drift.

## Basis

- Repository root: `D:/IA/ace v.2` (verified by `git rev-parse --show-toplevel`).
- Active plan: `docs/architecture-migration/plans/phase-1-project-session-shell.md`.
- Active plan SHA-256: `011594E3AB70787CCD0D49893458F70125C143EB3BD74545680712EA6AED1948`.
- F1 plan-compliance receipt: `docs/architecture-migration/evidence/phase-1-project-session-shell/f1-plan-compliance.md` (APPROVE).

## Source-inspection claims and evidence

### 1. `ProjectSession` owns only lifecycle identity/path/dirty/load-guard and restore depth/lease

Verified by reading `src/Services/Project/ProjectSession.cs`:

- Private fields: `_projectNumber`, `_projectObject`, `_currentFilePath`, `_isDirty`,
  `_isLoadProjectInProgress`, `_restoreDepth`.
- Public API (`IProjectSession`): `ProjectNumber`, `ProjectObject`, `CurrentFilePath`,
  `IsDirty`, `IsLoadProjectInProgress`, `MarkDirty()`, `MarkClean()`,
  `BeginProjectRestore()`.
- No climate, construction, thermal, hydraulics, results, calculation, export,
  dialog, persistence DTO, or command members.
- `ProjectSession` implements `IProjectSession`, `IProjectStateService`, and
  `IMarkDirtyService` directly, satisfying the preferred minimum in the plan.

### 2. No shared `_currentLease`; nested leases are distinct and idempotent

Verified by reading `src/Services/Project/ProjectSession.cs` lines 83-178:

- No `_currentLease` field exists.
- `BeginProjectRestore()` returns `new ProjectRestoreLease(this)` for every
  successful begin.
- `ProjectRestoreLease` is a private sealed class with its own `_disposed` flag
  and uses `Interlocked.Exchange(ref _disposed, 1)` to ensure `EndRestore()` is
  called at most once.
- Regression tests in `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectSessionTests.cs`
  cover dispose-inner-then-outer, dispose-outer-then-inner, and throwing exit
  subscribers while keeping leases idempotent.

### 3. `ProjectStateService` has no mutable lifecycle backing fields

Verified by reading `src/Services/Results/ProjectStateService.cs`:

- Single instance field: `private readonly IProjectSession _session`.
- All properties/methods forward to `_session`.
- `ProjectSessionLegacyStoreGuardTests.ProjectStateService_HasNoMutableLifecycleBackingFields`
  is GREEN.

### 4. `CalculationStateService` has only the allowed compatibility lease reference

Verified by reading `src/Services/Navigation/CalculationStateService.cs`:

- Lifecycle-related fields: `private readonly IProjectSession _projectSession`
  and `private IDisposable? _restoreLease`.
- No local `bool` or `int` guard/depth copy.
- `IsLoadProjectInProgress` getter delegates to `_projectSession.IsLoadProjectInProgress`.
- Compatibility setter acquires one lease if absent and disposes/clears it on
  `false`, with idempotent repeated true/false calls.
- `ProjectSessionLegacyStoreGuardTests.CalculationStateService_HasNoLocalRestoreGuardBackingField`
  is GREEN.

### 5. DI maps lifecycle aliases to the same singleton canonical session

Verified by reading `src/Configuration/ServiceCollectionExtensions.cs` lines 172-176:

```csharp
services.AddSingleton<ProjectSession>();
services.AddSingleton<IProjectSession>(sp => sp.GetRequiredService<ProjectSession>());
services.AddSingleton<IProjectInfoService>(sp => sp.GetRequiredService<ProjectSession>());
services.AddSingleton<IProjectStateService>(sp => sp.GetRequiredService<ProjectSession>());
services.AddSingleton<IMarkDirtyService>(sp => sp.GetRequiredService<ProjectSession>());
```

- `ProjectSessionTests.DependencyInjection_ResolvesSameCanonicalInstance_ForAllLifecycleInterfaces`
  and `DependencyInjection_LifecycleConsumersShareCanonicalSession` are GREEN.
- `ResultsViewModel` and `CalculationStateService` each receive the same
  `IProjectSession` instance.

### 6. `ResultsViewModel` uses `BeginProjectRestore()` without migrating module ownership

Verified by reading `src/ViewModels/Results/ResultsViewModel.cs` line 1580:

```csharp
using var restoreScope = _projectSession.BeginProjectRestore();
```

- `LoadProjectDataAsync` wraps the restore body in a canonical session lease.
- No command/module reset/restore orchestration logic moved into `ProjectSession`.
- `ProjectLoadOrchestrator.RestoreModulesFromProjectAsync(data)` remains the
  module-restore coordinator.

### 7. `CalculationContext` is unchanged

Verified by command:

```bash
$env:GIT_MASTER='1'; git diff HEAD -- src/Core/CalculationContext.cs
```

Result: no output (`CalculationContext.cs` is unchanged).

### 8. No application service gained concrete ViewModel dependencies

Verified by grep over `src/Services/` for concrete `*ViewModel` constructor
parameters or fields. The only application-service files with concrete VM
references are pre-existing debt explicitly outside Phase 1 scope:

- `src/Services/Project/ProjectLoadOrchestrator.cs`
- `src/Services/Results/ResultsPdfDataBuilder.cs`
- `src/Services/Results/HydraulicSummaryBuilder.cs`
- `src/Services/Hydraulics/CollectorTypeSelector.cs`
- `src/Services/Hydraulics/CircuitsValidator.cs`

`ResultsViewModel` itself takes concrete module ViewModels, but `ResultsViewModel`
is a ViewModel, not an application service. Phase 1 did not introduce any new
service-to-concrete-ViewModel edge.

## Commands and results

```bash
$env:GIT_MASTER='1'; git rev-parse --show-toplevel
```

Result: `D:/IA/ace v.2`.

```bash
$env:GIT_MASTER='1'; git diff HEAD -- src/Core/CalculationContext.cs
```

Result: no output.

```bash
dotnet build src/SnowMeltingCalculator.csproj -c Debug
```

Result: 0 warnings, 0 errors.

```bash
dotnet build src/SnowMeltingCalculator.csproj -c Release
```

Result: 0 warnings, 0 errors.

```bash
dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --filter "FullyQualifiedName~ProjectSessionTests|FullyQualifiedName~ProjectStateServiceTests|FullyQualifiedName~ProjectSessionLegacyStoreGuardTests|FullyQualifiedName~CalculationStateServiceGuardTests|FullyQualifiedName~ProjectLifecycleFlowCharacterizationTests"
```

Result: 49 passed, 0 failed, 0 skipped.

```bash
dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --filter "FullyQualifiedName~MainViewModelTests|FullyQualifiedName~ResetOrchestrationTests|FullyQualifiedName~ResultsViewModelOpenProjectTests|FullyQualifiedName~CircuitsViewModelEventLeakTests|FullyQualifiedName~ResultsStabilizationPhase1|FullyQualifiedName~DoubleCalculationPreventionTests"
```

Result: 100 passed, 0 failed, 1 pre-existing skipped.

```bash
dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --filter "FullyQualifiedName~ProjectRoundTripTests|FullyQualifiedName~ProjectFileServiceResultTests|FullyQualifiedName~ProjectFileServiceAtomicityTests|FullyQualifiedName~ProjectFileServiceMutationTests"
```

Result: 18 passed, 0 failed, 0 skipped.

```bash
dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Release
```

Result: 1568 passed, 0 failed, 1 pre-existing skipped.

## Findings

- **APPROVE basis:** All Phase 1 architecture invariants are satisfied. The
  lifecycle shell is narrow, stateless adapters forward to the canonical session,
  DI maps all aliases to one singleton, `CalculationContext` is untouched, and no
  module ownership migrated.
- **Pre-existing debt (not Phase 1 blockers):**
  - `ProjectLoadOrchestrator`, `ResultsPdfDataBuilder`, `HydraulicSummaryBuilder`,
    `CollectorTypeSelector`, and `CircuitsValidator` already depend on concrete
    ViewModels.
  - `ResultsViewModel` has 15 constructor parameters and depends on four concrete
    module ViewModels plus concrete service classes (`ProjectLoadOrchestrator`,
    `ResultsPdfDataBuilder`, `HydraulicSummaryBuilder`). This is outside the
    Phase 1 shell scope.
  - `ProjectStateService()` and `CalculationStateService()` parameterless
    constructors create fresh `ProjectSession` instances as test/direct-use seams;
    production DI uses the singleton path.
- **No high/medium correctness or architecture finding introduced by Phase 1.**

VERDICT: APPROVE
