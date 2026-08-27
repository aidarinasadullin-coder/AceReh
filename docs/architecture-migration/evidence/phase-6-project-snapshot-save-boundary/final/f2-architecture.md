# F2 — Architecture & Code-Quality Review (Fresh Final Wave)

- **REVIEW_ID:** F2
- **SUBJECT:** phase-6-project-snapshot-save-boundary
- **WAVE:** Fresh Final Verification Wave F2 (independent architecture reviewer, no codegraph dependency)
- **DATE:** 2026-08-26
- **REVIEWER ROLE:** Fresh independent reviewer. All Phase 6 production/test files were read directly before any test was run. This document replaces the prior informational `final/f2-architecture.md` (which was produced before Tasks 6-8 completed and is invalid for acceptance); it is a fresh verdict, not a reuse of that receipt.

## Scope

Independent architecture conformance review of the Phase 6 save-boundary implementation against the frozen plan `docs/architecture-migration/plans/phase-6-project-snapshot-save-boundary.md` (SHA-256 `C56E66D2733D65CC56190A7B95B4D87F5F032AA75E83339925CEB23C2E5A4E92`, verified unchanged by read-only `Get-FileHash` in Task 8).

Files reviewed directly (read, not via codegraph):

- `src/Services/Project/IProjectSaveService.cs`
- `src/Services/Project/ProjectSaveService.cs`
- `src/Services/Project/ProjectSnapshot.cs`
- `src/Services/Project/ProjectSnapshotFactory.cs`
- `src/Services/Project/ProjectPersistenceMapper.cs`
- `src/Services/Project/ProjectSnapshotPersistenceInputs.cs`
- `src/Services/Project/IProjectDisplayModeState.cs`
- `src/Services/Project/ProjectDisplayModeState.cs`
- `src/ViewModels/Results/ResultsViewModel.cs` (save-adapter slice, export/Markdown commands, `IsOperatingMode` ownership)
- `src/Configuration/ServiceCollectionExtensions.cs` (DI registration)
- `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectSaveServiceTests.cs`

Also inspected for claims: Task 3/5/6 evidence, Task 8 consolidated receipt, the six architecture maps (each for exactly one `## Phase 6 Save-Boundary Overlay`), and `architecture-model.json` (PN-P6-/PE-P6-/INV-P6-SAVE records).

## Build Gate (exact output, live rerun)

```
dotnet build src\SnowMeltingCalculator.csproj -c Debug --nologo
```

- Result: `Сборка успешно завершена.` (Build succeeded)
- Warnings: **0**
- Errors: **0**
- Duration: 00:00:00.81
- **BUILD_EXIT_CODE = 0**

The test project `tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj` was also built for the gates below: 0 warnings / 0 errors, exit 0.

## Test Gates (exact output, live rerun — not copied)

### Task 3 guard (immutable snapshot / ownership contracts)

```
dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~ProjectSnapshot" --nologo
```

- Passed: **26**
- Skipped: **0**
- Failed: **0**
- Total: **26**
- **TASK3_EXIT_CODE = 0**

Note: the historical Task 3 receipt recorded 24 passed; the live rerun shows 26 because the `ProjectSnapshot` substring filter now also matches two additional tests introduced in later Phase 6 tasks (e.g. `ProjectSnapshotFactoryTests`/`ProjectPersistenceMapperTests` methods whose FQN contains `ProjectSnapshot`). Zero failures either way; the higher count is transparently recorded, not hidden.

### Task 5 guard (save-boundary slice, combined F2 filter)

```
dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~ResultsViewModelOpenProjectTests|FullyQualifiedName~ProjectFileService|FullyQualifiedName~ProjectSaveService|FullyQualifiedName~ProjectSnapshot" --nologo
```

- Passed: **83**
- Skipped: **1**
- Failed: **0**
- Total: **84**
- **TASK5_EXIT_CODE = 0**

The single skipped test is `ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile` — a known baseline skip caused by the absent external legacy fixture `D:\IA\ace\Тест\тест 40.smc`. Skipped is distinguished from failed; 0 failed.

### Task 6 guard (persistence / compatibility / negative architecture guards)

```
dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~ProjectRoundTripTests|FullyQualifiedName~ProjectFileServiceResultTests|FullyQualifiedName~ProjectFileServiceMutationTests|FullyQualifiedName~ProjectFileServiceAtomicityTests|FullyQualifiedName~ProjectPersistenceMapperTests|FullyQualifiedName~ProjectSnapshotFactoryTests|FullyQualifiedName~ProjectSnapshotContractTests|FullyQualifiedName~ProjectSaveServiceTests|FullyQualifiedName~ResultsViewModelOpenProjectTests|FullyQualifiedName~ProjectSessionLegacyStoreGuardTests|FullyQualifiedName~ClimateStateLegacyStoreGuardTests|FullyQualifiedName~ConstructionStateLegacyStoreGuardTests|FullyQualifiedName~ThermalStateLegacyStoreGuardTests|FullyQualifiedName~HydraulicsStateLegacyStoreGuardTests|FullyQualifiedName~CalculationStateServiceGuardTests" --nologo
```

- Passed: **124**
- Skipped: **1**
- Failed: **0**
- Total: **125**
- **TASK6_EXIT_CODE = 0**

The single skip is the same external-fixture test `ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`. This matches the Task 6 Release evidence (124/1/0/125) when run in Debug; 0 failed.

## LSP Diagnostic Probe

`lsp_diagnostics` on `src/Services/Project/ProjectSaveService.cs` returned the known harness error:

```
LSP file path must be inside request cwd: D:\IA\3ace v.2\src\Services\Project\ProjectSaveService.cs
```

This is the documented environment limitation (recorded in the migration dossier and prior learnings). It is NOT a code defect. The compiler (`dotnet build`, 0 warnings/0 errors, exit 0) and the three focused test runs (all exit 0, 0 failed) remain the authoritative C# correctness gates, and all pass.

## Architecture Invariant Inspection (source read directly)

### 1. No concrete ViewModel / WPF dependencies in services — PASS

`using` directives per service file (verified by direct read):

- `ProjectSaveService.cs`: `System`, `System.Threading`, `System.Threading.Tasks`, `SnowMeltingCalculator.Core.Results`, `SnowMeltingCalculator.Repositories.Construction`. No `ViewModel`, no `System.Windows`, no `DependencyObject`/`DependencyProperty`.
- `ProjectSnapshot.cs`: `SnowMeltingCalculator.Models.Construction` only.
- `ProjectSnapshotFactory.cs`: `System`, `System.Linq`, `SnowMeltingCalculator.Models.Construction`.
- `ProjectPersistenceMapper.cs`: `System`, `System.Linq`, `SnowMeltingCalculator.Models.Project`, `SnowMeltingCalculator.Repositories.Construction`.
- `ProjectSnapshotPersistenceInputs.cs`: `System`, `System.Collections.Generic`, `System.Linq`, `SnowMeltingCalculator.Models.Construction`, `SnowMeltingCalculator.Repositories.Construction`.
- `IProjectDisplayModeState.cs` / `ProjectDisplayModeState.cs`: no external `using` directives.

The test file `ProjectSaveServiceTests.cs` references "ViewModel" only as a substring inside production-source-guard assertions (it reads `ResultsViewModel.cs` text and asserts the `SaveToFile` slice delegates to `_projectSaveService.SaveAsync` and does NOT call `SaveCurrentProject`). This is a test-time string check, not a compile-time dependency of the service. The service layer is ViewModel-free and WPF-free.

### 2. Immutable snapshot — PASS

`ProjectSnapshot` is `sealed` with get-only public properties on all four contract types (`ProjectSnapshot`, `ProjectCustomMaterialRecord`, `ProjectTemplateRecord`, `ProjectTemplateLayerRecord`). `CustomMaterials`/`CustomTemplates` are wrapped in `Array.AsReadOnly(...)` defensive read-only copies; nested template layer/material lists are also `IReadOnlyList<T>` copied via `CopyValidated`. Required inputs throw `ArgumentNullException` with exact param names. The snapshot deliberately excludes paths, dirty flags, restore guards, dates, and transient UI/service state (per its XML doc). This matches the Task 3 contract-test findings (immutable by construction; live Task 3 filter: 26 passed / 0 failed).

### 3. Exactly one snapshot / one map / one file call — PASS

`ProjectSaveService.SaveAsync` (lines 35–48) performs, in order:
- exactly one `_snapshotFactory.Create(projectSession)` (line 44),
- exactly one `ProjectPersistenceMapper.ToProjectData(snapshot, dates, _materialRepository)` (line 45),
- exactly one `await _fileService.SaveProjectResultAsync(filePath, data, cancellationToken)` (line 47), forwarding the token.

The behavioral test `SaveAsync_WithValidSession_MapsSnapshotFieldsAndCallsServicesExactlyOnce` verifies `Times.Once` on both the factory and the file service, and that the path and cancellation token are forwarded unchanged. Confirmed by source and test.

### 4. DTO-only file service boundary — PASS

`ProjectPersistenceMapper.ToProjectData` is a `static` pure mapper that produces the existing `ProjectData` wire DTO (Version `"1.1"`, preserving the current DTO graph) and delegates the three existing module mappers (`ConstructionPersistenceMapper`, `ThermalPersistenceMapper`, `HydraulicsPersistenceMapper`). The save service never touches serialization, extension normalization, or I/O — those stay in `IProjectFileService`. The boundary is DTO-only.

### 5. No second writable owner — PASS

`IsOperatingMode` has exactly one writable canonical owner: `ResultsViewModel` (observable property; toggle ~line 555, reset ~1562, load ~1625). `IProjectSession` / `IProjectStateService` expose no display-mode member. The persisted mode is supplied by `IProjectDisplayModeState` (`ProjectDisplayModeState`, a plain `sealed` class, default `IsOperatingMode = true`), which `ResultsViewModel` writes once in its constructor (`_displayModeState.IsOperatingMode = IsOperatingMode`, line 525) and on change (`OnIsOperatingModeChanged`, line 564); `ProjectSnapshotPersistenceInputs` reads it (`IsOperatingMode => _displayModeState.IsOperatingMode`). The save service (`ProjectSaveService`) depends only on `IProjectSnapshotFactory`, `IMaterialRepository`, `IProjectFileService` — it holds no writable copy of the snapshot or mode and is not injected with `IProjectDisplayModeState` or `IProjectSnapshotPersistenceInputs`. No second writable owner of the snapshot/mode exists in the save boundary.

### 6. Preserved export / Markdown commands (no forbidden redesign) — PASS

In `ResultsViewModel.cs` the following commands remain present and intact (verified by direct read):
- `ExportPdf()` (line 613)
- `ExportOperatingMarkdownReport()` (line 664) → `ExportMarkdownReportAsync(Operating)`
- `ExportDesignColdMarkdownReport()` (line 678) → `ExportMarkdownReportAsync(DesignCold)`
- `ExportMarkdownReportAsync(...)` (line 688)
- `ExportExcel()` (line 739)
- `PreviewPdf()` (line 855)
- `PrintPdf()` (line 905)

`SaveCurrentProject()` (line 1657) is preserved for report/export compatibility (used by `ExportMarkdownReportAsync` and the legacy `SaveLegacyFileAsync` fallback). No export/Markdown command was removed or weakened. The `SaveToFile` slice (lines 968–1012) delegates to `_projectSaveService.SaveAsync` (line 975) when the service is present and falls back to `SaveLegacyFileAsync` only when it is null; the source-guard test confirms the slice contains `_projectSaveService.SaveAsync` and does NOT contain `SaveCurrentProject`. No restore migration, Markdown removal, or export redesign was performed (those remain deferred to Phase 7+ per the consolidated receipt).

## DI Wiring (`ServiceCollectionExtensions.cs`)

`AddResultsModule` registers, in order:
- `IProjectFileService` → `ProjectFileService` (210)
- `IProjectDisplayModeState` → `ProjectDisplayModeState` (211)
- `IProjectSnapshotPersistenceInputs` → `ProjectSnapshotPersistenceInputs` (212)
- `IProjectSnapshotFactory` → `ProjectSnapshotFactory` (213)
- `IProjectSaveService` → `ProjectSaveService` (214)
- `ResultsViewModel` → singleton (221)

`ResultsViewModel` receives `IProjectSaveService?` and `IProjectDisplayModeState?` as optional ctor parameters (lines ~501–502), defaulting to `null` with a legacy fallback path in `SaveToFile` when the save service is absent. The chain resolves cleanly (build + all three test gates pass).

## Six-view / model claims

- Each of the six architecture maps (`compile-time.md`, `di-runtime.md`, `reactive.md`, `persistence.md`, `state-ownership.md`, `user-flow.md`) contains exactly one `## Phase 6 Save-Boundary Overlay` (grep-confirmed: one occurrence per file). The overlays describe only observed baseline/current save facts and explicitly do NOT claim restore migration, Markdown/export completion, calculation completion, or broad ownership cleanup.
- `architecture-model.json` contains the verified save-boundary records `PN-P6-SNAPSHOT`, `PN-P6-MAPPER`, `PN-P6-DATA`, `PN-P6-SERVICE`, `PE-P6-SESSION-SNAPSHOT`, `PE-P6-SNAPSHOT-MAPPER`, `PE-P6-MAPPER-DATA`, and `INV-P6-SAVE`, each with `status: observed`, `confidence: verified`. The model hash recorded in the Task 8 receipt (`554C3E171A6AEF42AA92ED2E88E24BFA9DD7D6B69E9DD91F7D6D216F734A52BF`) is unchanged.

## Residual Risk (recorded honestly, not a gate failure)

- **Sync-over-async `Templates`**: `ProjectSnapshotPersistenceInputs.Templates` (lines 32–33) uses `_templateRepository.GetAllAsync().GetAwaiter().GetResult()`. On a UI thread this can deadlock if the async path ever blocks; in the current design it is safe only on the already-loaded cache-hit fast path (the repository surface is async-only, so a synchronous `Templates { get; }` adapter has no clean alternative without injecting a ViewModel or inventing state, both forbidden by the slice guardrails). Documented, non-gating.
- **External fixture skip**: the single skipped test in every run (`ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`) depends on the absent external legacy fixture `D:\IA\ace\Тест\тест 40.smc`. Recorded as an explicit skip, not a pass; 0 failed.
- **NOT_PRESENT standalone negative process probe**: no standalone process probe for an intentionally invalid architecture-dependency fixture exists; this absence is recorded as `STATUS=NOT_PRESENT` (honest absence, not a fabricated nonzero result), per Task 6/Task 8 evidence.
- **Headless manual QA**: this is a WPF app and the environment is headless, so no actual WPF manual UI flow (button clicks, dialogs, print/preview rendering) was executed. The "dirty" flag interaction with save is characterized by tests but not asserted by a live UI flow. These are manual-QA gaps, not gate failures.

## Verdict

All six architecture invariants hold under direct source inspection. The Debug build is clean (0 warnings / 0 errors, exit 0; test project also 0/0). All three live targeted gates pass: Task 3 (26 passed / 0 skipped / 0 failed / 26 total, exit 0), Task 5 (83 passed / 1 skipped / 0 failed / 84 total, exit 0), Task 6 (124 passed / 1 skipped / 0 failed / 125 total, exit 0). The only LSP probe result is the known cwd harness limitation, not a code defect. The four residual risks (sync-over-async `Templates`, external fixture skip, `NOT_PRESENT` standalone negative probe, headless manual QA) are documented and do not break any gate. No forbidden restore/Markdown/export redesign was found; export/Markdown commands and `SaveCurrentProject` are preserved.

REVIEW_ID: F2
SUBJECT: phase-6-project-snapshot-save-boundary
RECEIPT: docs/architecture-migration/evidence/phase-6-project-snapshot-save-boundary/final/f2-architecture.md
VERDICT: APPROVE
REASON: Fresh independent source inspection confirms all six save-boundary architecture invariants (no VM/WPF deps in services, immutable snapshot, exactly one snapshot/map/file call, DTO-only file boundary, single writable owner, preserved export/Markdown commands with no forbidden redesign). Debug build 0 warnings/0 errors (exit 0); three live targeted gates all green — Task 3: 26 passed/0 skipped/0 failed/26 total, Task 5: 83 passed/1 skipped/0 failed/84 total, Task 6: 124 passed/1 skipped/0 failed/125 total (all exit 0; the single skip is the known external `D:\IA\ace\Тест\тест 40.smc` fixture). Only LSP probe result is the known cwd harness error, not a defect. Residuals (sync-over-async Templates, external fixture skip, NOT_PRESENT standalone negative probe, headless manual QA) are recorded but do not fail any gate.
