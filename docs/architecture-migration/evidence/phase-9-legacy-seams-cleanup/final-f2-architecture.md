# Final F2 — Code-Boundary and Architecture Check

Дата: 2026-09-03.

REVIEW_ID: F2-P9-ARCHITECTURE
SUBJECT: Phase 9 executed result — live code boundary audit
RECEIPT: this file; consolidated in `final-f4-consolidated-stop.md`
VERDICT: APPROVE
REASON:

1. **INV-008 boundary on live code**: `ProjectLoadOrchestrator` fields/ctor
   (`:23-26`, ctor) hold only `IProjectLoad*Adapter` interfaces; zero concrete
   ViewModel types in the file's signatures. `ResultsPdfDataBuilder` ctor takes
   `IReportConstructionLayerSource`/`IReportCollectorDataSource`. The static
   guard `ApplicationServiceViewModelDecouplingTests` scans every concrete
   class under `SnowMeltingCalculator.Services.*` — GREEN in the full
   regression, with a recorded RED run against genuinely violating code.
2. **No shared mutable seams**: `Results.Circuits` rows are built by
   `HydraulicCircuitRowProjection.CreateRow` from canonical snapshots;
   `UpdateCircuitsFilter`/`UpdateCollectorSummary`/`RebuildHydraulicSummaryCards`/
   `UpdateCollectorSpecifications`/`UpdateCollectorEquipmentItems` read only
   `IProjectSession.HydraulicsState.Snapshot.Collectors`; ownership negative
   probes (`ResultsOwnedCircuitProjectionTests`) prove no instance sharing and
   no module-object mutation. `ResultsViewModel` contains zero concrete
   module-ViewModel code references (grep: 3 comment-only matches).
3. **Alias removal**: `IProjectStateService`/`IProjectInfoService` files
   deleted; grep over `src/` shows zero code references; `ProjectSession :
   IProjectSession, IMarkDirtyService` (internal seam only); consumers
   (`MainWindow`, `MainViewModel`, `ResultsViewModel`) on `IProjectSession`.
   DI: only `IProjectSession` + internal `IMarkDirtyService → ProjectSession`;
   no forwarding registrations.
4. **DI graph**: interface bindings `:222-227` resolve the same singleton
   module adapters; `DiRegistrationTests` and the full composition green.
5. **No hidden canonical writes**: Results mutation targets are Results-owned
   collections only; coordinator writer uniqueness untouched (`DEC-001 = A`);
   the restore path mutates only through session slice boundaries
   (`ApplySnapshot`/`Restore`/`ResetToDefaults`), Phase 7 contracts re-proven
   by the green lifecycle/multiplicity suites.

Residual risks (recorded, non-gating): `IMarkDirtyService` survives as the
internal session dirty seam (slice-6 deviation); the adapter-mirror duplication
between `HydraulicCircuitRowProjection.ToDomainResult` and
`CircuitsViewModel.ToDomainResult` is pinned by tests on both sides and named
in slice-3 as cleanup debt; headless manual WPF QA gap (RR-002).
