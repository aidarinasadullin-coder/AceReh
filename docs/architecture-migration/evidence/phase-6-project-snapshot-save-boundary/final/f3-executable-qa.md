# Phase 6 Final Verification Wave F3 — Executable QA / User Risk (FRESH)

## Audit identity

- Review: `F3`
- Subject: `phase-6-project-snapshot-save-boundary`
- Domain: Executable QA / User Risk (third of three independent final-verification domains)
- Mode: read-only execution gates; no production/test/plan/STATE/maps/widget/fixtures edits; no stage/commit/reset/revert/clean/install
- Result acceptance: remains a separate owner decision and is not inferred here
- All commands ran from `D:\IA\3ace v.2`. Totals are captured fresh from this run, not copied from prior receipts.
- This receipt supersedes the prior `final/f3-executable-qa.md` (created before Tasks 6-8 and invalidated for phase acceptance per the Phase 6 plan-identity exception). It is produced as part of the fresh phase-wide F1-F4 wave after Tasks 1-8.

## Commands and exit codes

| # | Command | Exit | Passed | Skipped | Failed | Total | Notes |
|---|---|---:|---:|---:|---:|---:|---|
| 1 | `dotnet build src\SnowMeltingCalculator.csproj -c Debug --nologo` | 0 | — | — | — | — | 0 warnings / 0 errors |
| 2 | `dotnet build src\SnowMeltingCalculator.csproj -c Release --nologo` | 0 | — | — | — | — | 0 warnings / 0 errors |
| 3 | `dotnet test --configuration Debug --no-build` | 0 | 2017 | 1 | 0 | 2018 | — |
| 4 | `dotnet test --configuration Release --no-build` | 0 | 2017 | 1 | 0 | 2018 | — |
| 5 | `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~ProjectRoundTripTests|FullyQualifiedName~ProjectFileServiceResultTests|FullyQualifiedName~ProjectFileServiceMutationTests|FullyQualifiedName~ProjectFileServiceAtomicityTests|FullyQualifiedName~ProjectPersistenceMapperTests|FullyQualifiedName~ProjectSnapshotFactoryTests|FullyQualifiedName~ProjectSnapshotContractTests|FullyQualifiedName~ProjectSaveServiceTests|FullyQualifiedName~ResultsViewModelOpenProjectTests|FullyQualifiedName~ProjectSessionLegacyStoreGuardTests|FullyQualifiedName~ClimateStateLegacyStoreGuardTests|FullyQualifiedName~ConstructionStateLegacyStoreGuardTests|FullyQualifiedName~ThermalStateLegacyStoreGuardTests|FullyQualifiedName~HydraulicsStateLegacyStoreGuardTests|FullyQualifiedName~CalculationStateServiceGuardTests"` | 0 | 124 | 1 | 0 | 125 | comprehensive save/persistence/guard filter |

The single skipped test in **every** run is
`ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`. It is an
explicit fixture-dependent skip: the test expects the external legacy path
`D:\IA\ace\Тест\тест 40.smc`, which is absent in this worktree. It did **not**
fail and was **not** converted into a passing claim. Skipped is distinguished
from passed and from failed: **0 failed** across all five gates.

`--no-build` usage justification: the Task 6 evidence records a transient WPF
build-output lock that blocked one exact Release build attempt; the same filter
was rerun with `--no-build` against the existing Release build. Here both
Release and Debug builds were re-executed fresh (gates 1-2, exit 0, 0
warnings/0 errors) immediately before the `--no-build` test gates, so the
`--no-build` runs execute against freshly compiled binaries. This matches the
documented build/output-lock workaround and is the only place `--no-build` is
used.

## Build evidence

Both Debug and Release builds of `src\SnowMeltingCalculator.csproj` completed
with `Сборка успешно завершена. Предупреждений: 0, Ошибок: 0` and exit code 0
(Debug ~1.23 s, Release ~1.29 s). The `--no-build` test gates therefore executed
against freshly compiled binaries.

## Targeted save/project flow coverage (gate 5, all passed)

The comprehensive targeted filter (gate 5) exercises the save-boundary surface
through existing automated tests. Each required flow maps to concrete passing
tests inside that 124/1/0/125 run:

| Required flow | Covering passing tests (within gate 5) |
|---|---|
| New / populated project | `ProjectRoundTrip_LiveMutationsAreSavedLoadedAndExportedWithoutResultsCalculation`, `ProjectRoundTrip_TwoCollectors_PreservesPerCollectorSummaries`, `ProjectRoundTrip_FieldCompleteRoundTrip_SecondLoadReplacesProjectA` |
| Save happy path / success | `SaveProject_Success_StampsDatesAndClearsDirtyOnce`; `ProjectSaveServiceTests` success mapping (Version `"1.1"`, path forwarded, `Times.Once`) |
| Save failure | `SaveProject_Failure_PreservesDirtyStateAndShowsError`; `ProjectSaveServiceTests` failed-result-returned-unchanged + exception-propagation; `SaveProjectAsync_IsAtomic_OriginalIntactOnWriteFailure` |
| Save / reload | `ProjectRoundTrip_LiveMutationsAreSavedLoadedAndExportedWithoutResultsCalculation`; `ProjectFileService_RoundTripPreservesSchemaVersionAndJsonShape`; `ResultsViewModel_LoadProject_TwoCollectors_RestoresIndependentSummaryCards` |
| Second-load compatibility | `ProjectRoundTrip_FieldCompleteRoundTrip_SecondLoadReplacesProjectA`; `LoadProjectData_SecondLoadWithoutSavedResult_ReplacesAllThermalStaleValues` |
| Dirty transition | `SaveProject_Success_StampsDatesAndClearsDirtyOnce` (exactly one `IsDirty: true -> false` transition); `SaveProject_Failure_PreservesDirtyStateAndShowsError` (dirty preserved, no clean transition) |
| Status / error behavior | `SaveProject_Failure_PreservesDirtyStateAndShowsError` (existing localized save error shown exactly once; no save error on success) |
| `.bak` / `.tmp` semantics | `SaveProjectAsync_CreatesBackup_BakExistsAfterSave` (`.bak` holds previous version after second save); `SaveProjectAsync_TempFileCleanedUpOnFailure` (no leftover `.tmp` after failure); `SaveProjectAsync_IsAtomic_OriginalIntactOnWriteFailure` |
| Saved results | `SaveCurrentProject_PersistsThermalStateSnapshot_NotThermalViewModelMirror`; `LoadProjectData_KpiReflectSavedThermalResult_WithoutCityReselection` |
| Two-collector summaries | `ProjectRoundTrip_TwoCollectors_PreservesPerCollectorSummaries`; `ResultsViewModel_LoadProject_TwoCollectors_RestoresIndependentSummaryCards` |

Negative / architecture guards also inside gate 5 (passed): VM/WPF dependency
rejection in the save service (`ProjectSaveServiceSource_RejectsViewModelAndWpfReferences`),
`ProjectData` DTO-boundary guards, duplicate-snapshot-store / independent-state
ownership guards, and `SaveToFileSourceSlice_RejectsSaveCurrentProject`.

## Smoke-check: PDF / Excel / Preview / Print / Markdown commands

Read-only source scan of `src/ViewModels/Results/ResultsViewModel.cs` confirms
all five command families remain present and wired (no removal during Phase 6),
each decorated with `[RelayCommand]`:

- `ExportPdf()` (line 613) → `IPdfExportService.ExportResultsToPdfAsync`
- `ExportExcel()` (line 739)
- `PreviewPdf()` (line 855) → uses `_projectFileService.GetPreviewPdfPath()` + PDF export
- `PrintPdf()` (line 905) → preview path + `_dialogService.ShowPrintDialog()`
- `ExportOperatingMarkdownReport()` (line 664) / `ExportDesignColdMarkdownReport()` (line 678) / `ExportMarkdownReportAsync()` (line 688)

DI registration in `src/Configuration/ServiceCollectionExtensions.cs` still binds
the backing services: `IPdfExportService` → `PdfExportService` (line 206),
`ICalculationReportMarkdownRenderer` → `CalculationReportMarkdownRenderer` (line 208),
and `IProjectFileService` → `ProjectFileService` (line 210). No evidence of
removal or breakage of these commands or their services.

This is a **source-level smoke-check only**. No WPF button was clicked and no
dialog/rendering was executed (headless environment — see residual risk below).

## Unavailable WPF manual flow and residual user risk

This is a WPF application. The environment is headless; **no actual WPF manual
UI flow was executed**: clicking the Save/Open/Export/Preview/Print toolbar
buttons, the file/dialog interactions, the print dialog, and the live rendering
of PDF/Excel/Preview are not runnable here. The automated tests cover the logic
and persistence boundary (save success/failure, reload, second-load, `.bak`/`.tmp`
atomicity, path forwarding, dirty transition, saved results, two-collector
summaries) but do **not** exercise the real WPF command bindings, dialogs, or
visual output.

Explicit residual evidence (recorded honestly, not as gate failures):

1. **External fixture skip** — `ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`
   is skipped because `D:\IA\ace\Тест\тест 40.smc` is absent in this worktree.
   This is the one known external fixture skip; it is an explicit skip, not a
   pass, not a failure.
2. **Absent standalone invalid-process probe** — the plan's separate standalone
   process probe for an intentionally invalid architecture-dependency fixture
   does not exist; per Task 6 evidence it is recorded as `STATUS=NOT_PRESENT`
   rather than fabricated as a nonzero result. Available negative coverage is the
   passing guard/ownership tests listed above.
3. **Headless WPF manual QA gap** — end-to-end WPF click-through (button →
   command → dialog → save/export/print) and visual output are unverified in
   this environment. This is a manual-QA gap, not an executable-gate failure.
4. **`ProjectSnapshotPersistenceInputs.Templates` sync-over-async** — documented
   in Task 5/7 as deadlock-prone on the UI thread, safe only on the cache-hit
   fast path; non-gating, carried forward as a known residual risk.

These are disclosed as residual user risk and remain for owner result acceptance
/ a manual WPF pass; they do not invalidate the automated executable evidence
collected above.

## Decision

All mandatory executable evidence was produced fresh and is green: both builds 0
warnings / 0 errors (exit 0); full Debug `--no-build` 2017 passed / 1 skipped /
0 failed / 2018 (exit 0); full Release `--no-build` 2017 passed / 1 skipped / 0
failed / 2018 (exit 0); comprehensive targeted Release `--no-build` 124 passed /
1 skipped / 0 failed / 125 (exit 0). The PDF/Excel/Preview/Print/Markdown command
smoke-check passed at source/DI level. No command failed and no mandatory
evidence is missing. The required flows (new/populated, save success, save
failure, save/reload, second-load, dirty transition, status/error, `.bak`/`.tmp`,
saved results, two-collector summaries) are all exercised by passing automated
tests. The only open items are the headless-unavailable WPF manual flows and the
external fixture skip, both disclosed as residual risk rather than gate failures.

REVIEW_ID: F3
SUBJECT: phase-6-project-snapshot-save-boundary
RECEIPT: docs/architecture-migration/evidence/phase-6-project-snapshot-save-boundary/final/f3-executable-qa.md
VERDICT: APPROVE
REASON: Fresh Debug+Release builds (0 warnings/0 errors, exit 0), full Debug/Release no-build runs (2017 passed/1 skipped/0 failed/2018 total, exit 0 each), and comprehensive targeted Release no-build filter (124 passed/1 skipped/0 failed/125 total, exit 0) all pass; all required save flows (new/populated, success, failure, reload, second-load, dirty transition, status/error, .bak/.tmp, saved results, two-collector summaries) are exercised by passing automated tests; PDF/Excel/Preview/Print/Markdown commands confirmed present and DI-wired. Residual WPF manual UI flows and the external `D:\IA\ace\Тест\тест 40.smc` fixture skip are headless-unavailable and disclosed as user risk, not as gate failures; no mandatory evidence is missing.
