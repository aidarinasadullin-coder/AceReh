# F2 — Architecture / Code Quality Receipt

- Write-set: `phase-5-hydraulics-state`
- Frozen plan SHA-256: `0D48A757DDD1B98384D131D76C7AF5220206260BF853FADF12BA859329FC8D38`
- HEAD audited: `b9866d3ad1be8cbe0649cef7e408ea309669a8b4`
- Method: independent source audit of current production and characterization-test sources, with baseline behavior comparison to `471c4f1` and correction history review for `f65e067`, `20e4285`, and `b9866d3`.
- Scope: F2 only. F3/F4 were not launched. `docs/architecture-migration/STATE.json` was read-only and remained unstaged; no production or test source was edited.

## Former F2 Finding: Resolved

The former finding was the dropped legacy global-to-row propagation. The corrected `CircuitsViewModel.MirrorSupplyInputs` path is now present at `src/ViewModels/Hydraulics/CircuitsViewModel.cs:1276-1297`. It assigns both `SupplySpacing_cm` and `SupplyHeatPercent` to every circuit row under `_isMirroringHydraulicsState`, then raises the two adapter properties.

The only call is the User-origin input branch at `CircuitsViewModel.cs:1317-1337`, after `ApplyGlobalInputs(..., HydraulicsMutationOrigin.User)` and only for the two supply properties. The lifecycle guard at `:1317-1321` returns before canonical mutation or mirroring when resetting, initializing, mirroring, or loading. Therefore ProjectLoad, SystemApply, and Reset do not invoke User-origin mirroring. No recursive `ApplyGlobalInputs` or User-origin event is created by the mirror because `_isMirroringHydraulicsState` is checked by the property handler.

Current characterization coverage directly checks:

- User edits mirror canonical global values and every circuit row: `tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/HydraulicsMultiplicityCharacterizationTests.cs:76-101`.
- Calculation receives `(8.0, 18.0)`, row power changes, and context results publish: `:103-118`.
- Save contains new global and per-circuit wire fields: `:120-147`.
- Lifecycle restore preserves loaded fixture identity and values, while SystemApply/reset produce no User origin and no dirty calls: `:149-190`. Corrections `20e4285` and `b9866d3` replaced the tautological assertion and aligned it with the loaded fixture.

This resolves the previous B finding without changing lifecycle semantics.

## A. Sole Writable Owners ST-016..ST-019 — PASS

- **ST-016 global inputs:** `ProjectSessionHydraulicsState.ApplyGlobalInputs` is the canonical mutation boundary (`src/Services/Project/ProjectSessionHydraulicsState.cs:48-56`); production callers are the guarded User path in `CircuitsViewModel.cs:1317-1329` and `CalculationStateService.ResetHydraulicsState` with `SystemApply` (`src/Services/Navigation/CalculationStateService.cs:129-134`).
- **ST-017 collectors/circuits:** `ReplaceCollectors` is the canonical state mutation (`ProjectSessionHydraulicsState.cs:58-63`). Adapter collection/property paths use User origin under lifecycle/mirror/calculation guards (`CircuitsViewModel.cs:1024,1094,1111,1132`). Completion captures immutable snapshots through `CaptureCanonicalCollectors` (`:858-874`) and coordinator completion (`src/Services/Project/HydraulicsStateCoordinator.cs:59-83`).
- **ST-018 results:** the approved production publication site is exactly `HydraulicsStateCoordinator.PublishHydraulics` (`HydraulicsStateCoordinator.cs:56-57`), preserving source literal `"CircuitsViewModel"`. The mapper and Results save consume the canonical snapshot (`src/Services/Project/HydraulicsPersistenceMapper.cs:15-85`; `src/ViewModels/Results/ResultsViewModel.cs` canonical save integration).
- **ST-019 status:** `BeginCalculation`, `FailCalculation`, and reset normalization route through the canonical slice (`CalculationStateService.cs:117-134`); coordinator wraps every calculation attempt in `try/finally` and resets status (`HydraulicsStateCoordinator.cs:59-83`). `ResetToDefaults` lifecycle/UserReset callers are in `ProjectLoadOrchestrator.cs:70-90` and `MainViewModel.cs:247`.

The slice’s `_snapshot` is assigned only by `ProjectSessionHydraulicsState.Commit` (`ProjectSessionHydraulicsState.cs:92-100`). No second writable canonical store was found.

## B. ViewModel Adapter Boundaries and Mirror Guards — PASS

`CircuitsViewModel` obtains the slice from `IProjectSession` in its constructor (`CircuitsViewModel.cs:898-918`), delegates canonical mutations, and retains only WPF mirrors. Lifecycle projection is pull-only through `ApplyLifecycleSnapshotToAdapter` (`:717-793`) and guarded mirror methods. Upstream subscriptions are absent from the VM and owned once by `HydraulicsStateCoordinator` (`HydraulicsStateCoordinator.cs:23-34`). The corrected User-only supply mirror is documented above and is covered by value, calculation, persistence, and lifecycle negative checks.

## C. No New Service → Concrete-VM Dependencies — PASS

New hydraulics state/coordinator/mapper services contain no concrete ViewModel dependency. Existing `ProjectLoadOrchestrator` and Results PDF dependencies remain pre-existing architectural debt and were not expanded by this correction. The coordinator’s `"CircuitsViewModel"` is the frozen event-source payload literal, not a type dependency.

## D. Snapshot Immutability — PASS

Snapshot types in `src/Services/Project/HydraulicsStateSnapshots.cs` expose get-only properties, structural equality, and read-only copied collections. `ReplaceCollectors`, `CompleteCalculation`, and `Restore` copy collection boundaries (`ProjectSessionHydraulicsState.cs:58-63,68-74,83-88`). Adapter capture and lifecycle projection create fresh objects. The guard runtime probe rejects collection mutation.

## E. Guard Suite Honesty — PASS, 8/8

`tests/SnowMeltingCalculator.Tests/Services/Project/HydraulicsStateLegacyStoreGuardTests.cs` covers VmWritableStore, ServiceHydraulicsStore, OrchestratorDirectAssign, ResultsNonCanonicalSave, ContextUnapprovedWriter, SnapshotMutability, DuplicateUpstreamSubscriber, and DiIndependentStateRegistration. Each category includes a real violating fixture fed to the same predicate and a behavioral/source assertion; none is tautological. Focused execution passed all 8 guard tests.

## F. DI Factory and Reference Equality — PASS

`src/Configuration/ServiceCollectionExtensions.cs:148-151` resolves the coordinator against the session’s slice, without separate slice registration. The explicit `ProjectSession` factory at `:196-199` avoids the documented construction cycle. `DiRegistrationTests.cs:258-288` and guard category 8 verify singleton/reference identity and zero independent state registrations.

## G. Naming, Documentation, and Dead Code — PASS with non-blocking notes

Naming and mutation vocabulary follow accepted Thermal precedents (`HydraulicsMutationStatus`, `HydraulicsMutationResult`, snapshot/origin/coordinator names). The correction is narrow and preserves existing APIs. Pre-existing debts remain outside this audit: `CalculationContext._hydraulicsResults`, ST-005 duplicated load flag, and legacy service→VM dependencies. `AutoSelectCollectorType` remains pre-existing dead code. The mapper and some new public types have less XML documentation than the Thermal precedent; this is a documentation-quality debt, not a correctness blocker. Existing VM-side dirty calls are idempotent and preserve observed counts.

## Verification and Residual Risk

- Focused Debug run after rebuilding the test assembly: `51 passed / 0 failed / 0 skipped`, including 18 characterization tests, 8 guard tests, and DI checks.
- Debug production build: `0 warnings / 0 errors`.
- A stale Release `--no-build` assembly initially executed the superseded pre-correction lifecycle assertion and failed; rebuilding Release was blocked by an unrelated transient `CS2012` file lock. The fresh Debug build/test run executed current sources successfully. Recorded fresh phase evidence remains `18/18` characterization, full Release `1981 passed / 0 failed / 1 accepted skip`, and build `0 warnings / 0 errors` as supplied for this control audit.
- LSP cwd limitation is known and non-blocking: the harness may select `C:\Users\Admin` instead of this repository; compiler/tests are the correctness gate.
- No new blocking residual risk found. Documentation debt and pre-existing service→VM coupling remain noted but do not fail A-G.

REVIEW_ID: f2-phase5-architecture
SUBJECT: phase-5-hydraulics-state@0D48A757DDD1B98384D131D76C7AF5220206260BF853FADF12BA859329FC8D38
RECEIPT: docs/architecture-migration/evidence/phase-5-hydraulics-state/final/f2/architecture-quality.md
VERDICT: APPROVE
REASON: Corrected User-only MirrorSupplyInputs restores global SupplySpacing_cm/SupplyHeatPercent into every circuit row, calculator inputs, and persisted global/per-circuit wire fields; lifecycle guards prevent User-origin mirroring, and independent A-G source audit plus 51/51 focused checks pass.
