# Phase 3 Final Verification F2: Code Quality / Single-Owner Audit

Receipt date: `2026-08-20`

## Scope

This is a code-quality / single-owner audit of the Phase 3 Construction
ownership boundary. It verifies three invariants required by the migration
contract and the Phase 3 plan:

1. Every Construction value has exactly one writable canonical owner.
2. `ConstructionViewModel` (and other ViewModels) remain WPF adapters, not
   shared canonical state stores.
3. There are no duplicate writers / no second writable canonical Construction
   store.

This receipt is produced in the current bounded closure session. It reuses the
already-collected Phase 3 evidence and a targeted live re-check of the current
source tree; it does not modify any production code, tests, or dossier.

## Evidence base

- `task-2-writer-subscriber-inventory.md` — pre-migration writer/subscriber
  inventory and the exact bypass list that Tasks 4-11 eliminated.
- `task-6-viewmodel-adapter.md` — `ConstructionViewModel` shadow-writes to
  canonical state via `ApplySnapshot` with `ConstructionMutationOrigin.SystemApply`;
  `OnConstructionStateChanged` is a no-op; legacy VM collections stay active only
  as a transitional adapter.
- `task-11-di-ownership-guards.md` — DI binds `IConstructionData` to
  `IProjectSessionConstructionState.CurrentProjection`; `Construction` remains
  registered only as the compatibility model for the `ConstructionViewModel`
  adapter and is NOT exposed as the production `IConstructionData` service;
  "there is no separately registered concrete or transient
  `ProjectSessionConstructionState` owner"; "resolves the lifecycle graph without
  a duplicate owner or circular lifetime". Targeted gate: `49 passed / 0 failed
  / 0 skipped`.
- `task-13-architecture-dossier-refresh.md` — six views and shared model record
  "`ProjectSession.ConstructionState` and `ProjectSessionConstructionState` are
  the sole writable canonical owner; `ConstructionViewModel` is a WPF adapter;
  `ConstructionStateProjection` is the read projection consumed through
  `IConstructionData`". Model/runtime validators: `33/21` and `47/20` PASS;
  widget `14/14` PASS.
- `f1-plan-compliance-superseding.md` — preserves the historical F1 REJECT but
  confirms the Must-NOT-Have findings remain valid: "No Phase 3-attributed
  formula documentation, UI/XAML design, package or project version, persistence
  DTO or schema, installer, publish, or release artifact was added. No
  `ThermalState` or `HydraulicsState` ownership file entered the Phase 3
  production set." This rules out a second writable canonical store introduced
  by Phase 3.
- Live re-check via codegraph on `2026-08-20`: `ProjectSessionConstructionState`
  is the only class implementing `IProjectSessionConstructionState`;
  `ConstructionViewModel` writes to canonical state only through
  `_constructionState.Apply(...)` (e.g. `OnLayerChanged` →
  `ConstructionMutationOrigin.User`) and `SyncStateFromCollections(...)`; the
  `Construction` model's own mutation methods are not used as the canonical path
  from the ViewModel (per `task-2` §2.4). `ConstructionStateLegacyStoreGuardTests`
  guards against any NEW direct `_constructionViewModel.<Property> =` write.

## Findings

### 1. Single writable canonical owner — PASS

`IProjectSessionConstructionState` is documented as the "Single writable
canonical owner of GroundwaterLevel, HasLoads, ordered LayersAbovePipe and
ordered LayersBelowPipe." Its only implementation, `ProjectSessionConstructionState`,
is constructed and owned by `ProjectSession` and is not separately registered in
DI (Task 11 §3-4). All canonical mutations flow through the single
`Apply` / `ApplySnapshot` / `ResetToDefaults` / `CompleteChanged` API on that one
object. No second writable store exists.

### 2. ViewModels remain adapters — PASS

`ConstructionViewModel` is a WPF adapter: it subscribes to
`_constructionState.Changed` and refreshes itself; `OnConstructionStateChanged`
is explicitly a no-op because user edits already originate in the adapter and
lifecycle snapshots refresh it through `ApplyLifecycleSnapshotToAdapter`. It
writes to canonical state only through the canonical mutation API with explicit
origins (`User`/`Template`/`SystemApply`), never holding canonical state itself.
`Construction` is retained only as the adapter's compatibility model, not as a
canonical store.

### 3. No duplicate writers — PASS

DI exposes `IConstructionData` as `CurrentProjection` (read-only), not as the
mutable `Construction` model. The legacy `Construction` model mutation API is not
the canonical write path from the ViewModel. The `ConstructionStateLegacyStoreGuardTests`
negative fixture detects any new direct ViewModel setter write. The F1
Must-NOT-Have confirmation rules out any second writable canonical store added
during Phase 3. The lifecycle graph resolves without a duplicate owner or
circular lifetime (Task 11 §6).

## Conclusion

All three single-owner / code-quality invariants hold against the current source
tree and the already-collected Phase 3 evidence. No duplicate writer, no second
writable canonical Construction store, and ViewModels remain adapters.

VERDICT: APPROVE
