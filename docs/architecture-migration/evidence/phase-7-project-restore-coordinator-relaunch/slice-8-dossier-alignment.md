# Phase 7 Slice 8: Dossier Alignment (PASS)

**Date:** 2026-09-01
**Plan:** `docs/architecture-migration/plans/phase-7-project-restore-coordinator-relaunch.md` (frozen, NOT edited)
**Todo:** 8, align the Phase 7 dossier with the verified live write-set from Todos 1-7

## Scope

This is a documentation and evidence alignment slice. It records the live
restore, report, and UI boundary established by Todos 1-7. No production code,
tests, dependencies, persistence format, recovery subsystem, or second restore
path was introduced by this slice.

## Aligned Statements

1. The application has one project restore boundary: the singleton
   `ProjectLoadOrchestrator`, reached from `ResultsViewModel.LoadProjectDataAsync`
   under the `ProjectSession.BeginProjectRestore()` lease. The orchestrator
   applies the four session-owned canonical slices.
2. `ResultsViewModel` is the UI adapter handoff for the restored canonical
   session state. Its refresh and report handoff occur only after a successful
   restore; a rejected restore does not perform an extra successful refresh,
   leaves the prior UI values intact, and releases the restore guard.
3. Report and PDF values are derived from the fresh session/current projection,
   not from a stale saved-result sentinel. A fallback calculation supplies the
   fresh values once when the persisted result is invalid.
4. Project open does not import project-local catalog records into global
   catalogs. Invalid restore candidates are rejected before the authorized
   canonical mutation boundary, while existing `.smc` compatibility behavior
   remains in scope.

## Evidence-to-Statement Trace

| Changed dossier statement | Supporting Todo 1-7 receipt or test result |
| --- | --- |
| One singleton orchestrator restore boundary and four session-owned slices | Slice 1 receipt, `slice-1-restore-boundary.md`, Boundary Evidence and Negative Probe; Slice 7 receipt, `slice-7-di-ui-alignment.md`, `DiRegistrationTests.ResultsViewModel_RestorePath_UsesTheSingletonOrchestratorWithCanonicalSessionSlices` |
| Rejected restore preserves prior UI, performs no extra successful refresh, and releases the guard | Slice 7 receipt, `slice-7-di-ui-alignment.md`, `LoadProjectData_SecondInvalidProjectPreservesPriorUiAndReleasesRestoreGuard`, including KPI, `ProjectChanged`, guard, and dirty assertions |
| Fresh session/current projection replaces stale saved-result data for UI and PDF | Slice 5 receipt, `slice-5-report-source-of-truth.md`, `Build_UsesCurrentProjection_WhenPersistedDtoHasStaleSentinel`; Slice 7 receipt, `slice-7-di-ui-alignment.md`, `LoadProjectData_InvalidSavedResultPublishesFreshUiAndPdfValuesOnce` |
| Validation precedes canonical mutation and legacy-empty behavior remains compatible | Slice 3 receipt, `slice-3-validation-order.md`, invalid Thermal/Hydraulics preflight evidence and focused test; Slice 6 receipt, `slice-6-catalog-boundary.md`, thermal restore regression checks |
| Project-local catalog records remain read-only on open | Slice 6 receipt, `slice-6-catalog-boundary.md`, `OpenProject_WithCustomCatalogRecords_LeavesGlobalCatalogReadOnly` and invalid-record counterpart |

## Map and Widget Decision

No map or widget refresh was required. The inspected `di-runtime.md`,
`state-ownership.md`, `persistence.md`, and `user-flow.md` already represent
the same canonical `ProjectSession` ownership and existing restore boundary.
The Slice 7 write-set added executable coverage and confirmed the existing
wiring; it did not change production architecture, ownership, or widget/model
inputs. The slice therefore adds no map or widget edits.

## Reused Verification

The technical evidence is reused from the PASS receipts for Todos 1-7. This
docs-only slice does not rerun product tests. The required artifact checks for
this receipt are:

```text
node docs/architecture-migration/widget/verify-widget.mjs
git diff --check
```

## Gate Decision

Slice 8 is PASS when the artifact checks above pass. The Phase 7 dossier now
maps each changed statement to an existing Todo 1-7 receipt or test result,
without claiming a new recovery framework, restore boundary, or user-visible
production change.
