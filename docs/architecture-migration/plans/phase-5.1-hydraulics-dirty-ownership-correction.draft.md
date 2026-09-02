# phase-5.1-hydraulics-dirty-ownership-correction - Mutable Draft

## Status

This is a mutable planning draft only. It is not a frozen plan, does not
authorize implementation, and must not modify `STATE.json` or reopen the
accepted Phase 5 plan.

Proposed phase identity:

```text
phase-5.1-hydraulics-dirty-ownership-correction
```

Parent phase:

```text
phase-5-hydraulics-state
```

Current baseline validation on 2026-08-25:

- `STATE.json` validates with `--check-plan`.
- Parent Phase 5 is `completed` with accepted result and `stop=true`.
- Working tree is clean at draft creation time.
- No execution authorization exists for this correction phase.

## Problem

`CircuitsViewModel` still owns an `IMarkDirtyService` dependency and calls
`MarkDirty()` directly from `SetInputData()` for the four global hydraulics
inputs. The same user mutation is first sent through
`ProjectSessionHydraulicsState.ApplyGlobalInputs(...,
HydraulicsMutationOrigin.User)`. For a changed valid snapshot,
`ProjectSessionHydraulicsState.Commit()` already performs the centralized dirty
intent.

The current path is therefore:

```text
CircuitsViewModel -> ApplyGlobalInputs(User)
    -> ProjectSessionHydraulicsState.Commit() -> MarkDirty()
CircuitsViewModel -> MarkDirty()
```

`ProjectSession.MarkDirty()` currently suppresses a second lifecycle
notification when already dirty, but this masks two architectural owners and
does not make the duplicate intent correct for arbitrary test doubles or
future dirty implementations.

The ViewModel also calls its direct dirty service without inspecting the
mutation result. Consequently, a rejected or no-op state mutation can still
produce a dirty intent from the adapter.

## Objective

Restore the Phase 5 ownership contract with the smallest possible correction:

- `ProjectSessionHydraulicsState` remains the sole owner of dirty intent for
  canonical hydraulics user mutations.
- `CircuitsViewModel` remains a WPF adapter and does not depend on the dirty
  lifecycle mechanism for global hydraulics inputs.
- Valid changed `User` mutations produce exactly one dirty intent.
- `NoChange`, `Rejected`, and lifecycle-origin mutations produce zero dirty
  intents.
- Calculation, event, reset, restore, and `.smc` observable behavior remains
  unchanged except for the correction of dirty-intent ownership.

## Scope Classification

Expected write-set classes:

1. `production/test` - remove the redundant dependency and calls; add or align
   characterization coverage.
2. `architecture artifacts` - update affected evidence and six-view model
   inputs if the final plan determines that the accepted Phase 5 dossier needs
   a correction note or refreshed ownership edges.
3. `user-visible` - potentially affected only at dirty-state semantics for
   rejected/no-op input attempts; the plan must explicitly decide and test this
   boundary.

No control-plane transition is included in the implementation write-set.

## In Scope

- `src/ViewModels/Hydraulics/CircuitsViewModel.cs`
  - remove the `_markDirtyService` field;
  - remove the `IMarkDirtyService` constructor parameter and null check;
  - remove the two direct `MarkDirty()` calls in `SetInputData()`;
  - preserve the existing `ApplyGlobalInputs(..., User)` call and calculation
    branching.
- All current production and test constructor call sites affected by that
  signature change, identified from live source during planning.
- Characterization tests for dirty-intent multiplicity and mutation result
  semantics, preferably at the narrowest existing hydraulics/state seam.
- Targeted verification of valid changed, no-op, rejected, initialization,
  reset, project-load, and adapter-mirroring paths.
- A correction evidence receipt and any narrowly affected architecture-map or
  model/widget input refresh required by the final plan.

## Explicitly Out of Scope

- Reopening or editing the accepted Phase 5 plan or its final receipt.
- Changing `ProjectSessionHydraulicsState.Commit()` dirty policy unless live
  evidence proves the current accepted contract is impossible to satisfy.
- Changing `IMarkDirtyService`, `ProjectSession.MarkDirty()`, or lifecycle
  ownership outside the hydraulics adapter seam.
- Changes to calculation formulas, `HydraulicsStateCoordinator`, persistence,
  `.smc` wire format, Results projection, reset/load routing, or thermal/climate
  synchronization.
- Broad refactoring, constructor redesign beyond the redundant parameter, or
  compatibility shims.
- Changes to `STATE.json`, owner gates, frozen-plan hashes, or execution state
  by the implementer.

## Required Planning Investigation

The separate Prometheus planning session must inspect the live worktree and
confirm, rather than assume:

1. Every `CircuitsViewModel` constructor call site, including DI and tests.
2. Every remaining `IMarkDirtyService` use in the ViewModel and whether any
   non-global-input path relies on it.
3. The exact `HydraulicInputData` notification behavior and validation path.
4. `ProjectSessionHydraulicsState.Commit()` result/event/dirty ordering.
5. Existing hydraulics dirty, multiplicity, integration, and guard tests.
6. Current Phase 5 evidence and whether the correction changes any accepted
   behavioral count or only removes a hidden duplicate intent.
7. Baseline-relative dirty paths before any proposed implementation edit.

If this investigation finds that `IMarkDirtyService` is intentionally needed
for a mutation not routed through `HydraulicsState`, the plan must stop and
record that finding instead of removing the dependency.

## Behavioral Contract To Freeze

The final plan must state executable assertions for at least:

| Scenario | State result | Dirty intent |
|---|---|---:|
| Valid changed global input with `User` origin | `Changed` | 1 |
| Same global input value | `NoChange` or no adapter notification | 0 |
| Rejected global input | `Rejected` | 0 |
| `Initialization`, `ProjectLoad`, reset, or adapter mirror | lifecycle result | 0 |

The plan must separately preserve existing calculation behavior:

- supply spacing/heat changes continue through `Calculate()`;
- glycol type/concentration changes continue through
  `CalculateAllCollectors()`;
- existing `Changed` event and `PropertyChanged` multiplicity remain within
  the accepted Phase 5 contract.

## Characterization-First Sequence

The frozen plan must use one sequential implementation lane:

1. Capture protected baseline and baseline-relative delta.
2. Add or align a RED characterization for exact dirty-intent behavior.
3. Make the minimal production correction.
4. Update only affected constructor fixtures/call sites.
5. Run targeted hydraulics/state tests.
6. Run affected integration and guard suites.
7. Run build and applicable full test gate.
8. Run architecture invariant/evidence checks.
9. Perform required manual QA if the final write-set changes an affected
   user-visible dirty-state flow.

No failing test may be deleted or weakened. Pre-existing failures must be
separated from correction failures using the protected baseline.

## Verification Gates

The frozen plan must define exact commands and paths for:

- state and plan identity validation;
- targeted hydraulics/state tests;
- affected integration tests;
- guard-suite tests;
- Debug/Release build as applicable;
- applicable full phase test gate;
- architecture model/widget validation or an explicit unchanged rationale;
- final evidence receipts.

Final verification must cover three independent domains:

1. Conformance / Scope / Provenance;
2. Architecture / Code Quality;
3. Executable QA / User Risk.

The final consolidated receipt must name the correction write-set, reused and
rerun evidence, exact dirty multiplicity, residual risks, and manual-QA result
or justified omission.

## Six-View and Widget Impact

The frozen plan must assess all six views:

- compile-time: constructor dependency and references;
- DI/runtime: construction graph and identity;
- state ownership: one dirty owner for hydraulics user inputs;
- reactive: dirty intent and event multiplicity;
- persistence: expected unchanged `.smc` path, with evidence;
- user flow: edit/invalid input/dirty indication behavior.

Refresh affected model/widget/evidence artifacts, or record a precise reason
why an artifact remains unchanged. Do not regenerate unrelated artifacts.

## Rollback Boundary

Rollback is limited to the correction write-set identified by the frozen plan:
the adapter dependency/call-site correction, its characterization tests, and
correction evidence. Do not reset, clean, revert, or overwrite unrelated dirty
paths. If the correction cannot satisfy the frozen contract, stop, preserve
the failing evidence, and return to owner review without scope expansion.

## Owner Gates

The following are separate and mandatory:

1. Prometheus produces the decision-complete mutable/frozen plan candidate.
2. The candidate is materialized at its exact canonical and mirror paths and
   receives a SHA-256 identity.
3. Terminal plan review returns the required machine-readable verdict.
4. Owner explicitly approves the exact frozen plan and SHA.
5. Owner separately authorizes execution of this correction phase.
6. After implementation and independent verification, owner separately accepts
   the result.

This draft requests planning only. It does not imply approval, execution
authorization, or result acceptance.
