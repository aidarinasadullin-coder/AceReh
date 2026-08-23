# Task 11 — Ownership Guards Receipt

Status: GREEN. Guard-only todo executed per plan lines 456-464 with zero production edits.

## Environment
Root `D:\IA\3ace v.2` · branch `master` · HEAD base `6a5a96f1763dd952c8d772ecd1d2536eb3b804cf` · AMZ-1/AMZ-2 journal entries in force.

## Guard suite (`ThermalStateLegacyStoreGuardTests`, 8 categories, all `[Category("NegativeFixture")]`)

| Guard | Rejects |
|---|---|
| VmWritableStore_GuardRejectsThermalStatusBackingFields | Thermal status/message writable fields in `ThermalViewModel` |
| ServiceThermalStore_GuardRejectsThermalAndSpacingBackingFields | `_thermal*`/`_pipeSpacing` stores in `CalculationStateService` |
| OrchestratorDirectAssign_GuardRequiresRestoreBeforeAdapterProjection | direct thermal assignment in `ProjectLoadOrchestrator` bypassing canonical `Restore` |
| ResultsNonCanonicalSave_GuardRequiresCanonicalMapperInput | `ResultsViewModel` save path not reading canonical snapshot via mapper |
| ContextUnapprovedWriter_GuardAllowsOnlyCoordinatorProductionWriter | any `UpdateThermal*` production writer except `ThermalStateCoordinator` |
| SnapshotMutability_GuardDefensivelyCopiesEscapingMutableValues | mutable references escaping `ThermalStateSnapshots` |
| DuplicateUpstreamSubscriber_GuardRequiresOneCoordinatorAttachPerSurface | more than one Climate/Construction upstream subscriber attach |
| DiIndependentStateRegistration_GuardRejectsIndependentDescriptorsAndInstances | independent `IProjectSessionThermalState` DI registration / second instance |

Mechanism: source-scan guards over `src/**` (repo root resolved via solution-file walk, following the Todo 8 WriterAuthority style) + in-memory behavioral probes (defensive-copy escape, DI descriptor enumeration). Synthetic violating inputs fed directly to guard predicates prove each rejection branch (QA-failure contract).

AMZ-expected set encoded (not violations): `ApplyNeedsRecalculation` on the state with exactly one production caller (`CalculationStateService` compat route, AMZ-1); legacy interface writers routed to canonical with zero production callers; ProjectLoadReset translation suppression in the service (AMZ-2).

## Gates

| Gate | Exit | Result |
|---|---|---|
| G0 protected-pre (39 hunks) | 0 | mismatch 0 |
| V1 product Debug+Release / tests Release builds | 0/0/0 | 0 warnings, 0 errors |
| V2 focused (`phase-4-focused.trx`) | 0 | 203/203, failed 0 |
| V11 defined this todo (`phase-4-legacy-store-guards.trx`) | 0 | 8/8, failed 0 |
| Plan structure verifier (`plan-structure.json`) | 0 | valid=true, `v11_first_todo=11`, rows 18 |
| Full Release (`phase-4-full-release.trx`, clean single-host rerun) | 0 | **1946 total / 1943 passed / 0 failed / NotExecuted == 3 exact baseline identities** |
| Protected-post | 0 | drift 55, mismatch 0, allowed hunks 39 |

## Arithmetic vs Todo 10 (1937/1934/0/3)
+9 tests (8 guards + 1 DI case) ⇒ 1946 total; 1943 = 1934+9; NotExecuted unchanged.

## Incident record (environmental, resolved)
First full-suite run (15:48) showed 4 `AppSettingsTests.Save_*` failures — all `IOException: settings.json being used by another process` in `%APPDATA%`, caused by concurrent test hosts during agent iteration. A clean single-host rerun is fully green with identical NotExecuted identities; no production or test change was needed. TRX above is the clean run.

## Files
NEW: `tests/SnowMeltingCalculator.Tests/Services/Project/ThermalStateLegacyStoreGuardTests.cs`; modified: `DiRegistrationTests.cs` (+1 DI-identity case). Evidence: `task-11/{allowed-hunks.json(39), plan-structure.json, protected-pre/post.json, trx-full-release.json, TestResults/*.trx}`. No production edits (guard-only rule honored). No git operations.
