# F2 — Architecture / Code Quality Domain Receipt

REVIEW_ID: f2-architecture-phase-4-thermal-state
SUBJECT: phase-4-thermal-state@327B1288B8072E7A76D814F8439ECBC353F54F4CE59AB2B507595A08980B4C02
RECEIPT: inline (this document)
VERDICT: APPROVE
REASON: All four V12-F2 Release suites green against the frozen binaries with zero Failed and zero NotExecuted (focused 211, upstream 21, hydraulics 59, negative 8 — exactly the eight synthetic guard categories); every TRX reconciled against its intended non-empty identity set with zero out-of-set identities; all nine architecture inspection items proven at symbol level with file:line citations (immutable snapshots, exhaustive 9-member origins, one ProjectSession state instance, zero legacy writable stores, one upstream subscriber, context single-writer, exact consumer semantics, no Hydraulics/Results ownership migration) and the only deviations found are the owner-approved AMZ-1/AMZ-2/AMZ-3 records; frozen manifest sha `6D039FC7B84C84F389D2DB435B69C354323ACCAB6C62A16C0B8F75475B13BA72` and all four artifact hashes byte-stable before/after the lane.

## 1. Gate matrix

All commands from repo root `D:\IA\3ace v.2` (path contains a space; evidence scripts via `pwsh -NoProfile -File`; test hosts run strictly sequentially; `-c Release --no-build` only — no rebuilds).

| Check | Command | Exit | Result |
|---|---|---|---|
| V13-before | `pwsh -NoProfile -File docs/architecture-migration/evidence/phase-4-thermal-state/verify-frozen-release.ps1 -Manifest docs/architecture-migration/evidence/phase-4-thermal-state/frozen-release-sha256.json -Lane F2 -Moment Before` | 0 | artifacts=4, manifest sha `6D039FC7…BA72`, receipt `final/F2/frozen-hashes-before.json` |
| V12-F2 a | `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~ProjectSessionThermalStateTests\|FullyQualifiedName~ThermalMultiplicityCharacterizationTests\|FullyQualifiedName~ThermalStateLegacyStoreGuardTests\|FullyQualifiedName~ThermalViewModelTests\|FullyQualifiedName~CalculationStateServiceTests\|FullyQualifiedName~DiRegistrationTests" --results-directory …/final/f2/TestResults --logger "trx;LogFileName=f2-focused.trx"` | 0 | failed=0, passed=211, skipped=0 |
| V12-F2 b | same host, filter `"FullyQualifiedName~ClimateThermalInvalidationRegressionTests\|FullyQualifiedName~ConstructionThermalInvalidationRegressionTests"`, `f2-upstream.trx` | 0 | failed=0, passed=21, skipped=0 |
| V12-F2 c | same host, filter `"FullyQualifiedName~ThermalToHydraulicsIntegrationTests\|FullyQualifiedName~PipeSpacingSynchronizationTests\|FullyQualifiedName~DoubleCalculationPreventionTests\|FullyQualifiedName~CalculationContextInvalidationTests\|FullyQualifiedName~CalculationContextWriterAuthorityTests"`, `f2-hydraulics.trx` | 0 | failed=0, passed=59, skipped=0 |
| V12-F2 d | same host, filter `"FullyQualifiedName~ThermalStateLegacyStoreGuardTests&TestCategory=NegativeFixture"`, `f2-negative.trx` | 0 | failed=0, passed=8, skipped=0 |
| TRX parse ×4 | `pwsh -NoProfile -File <ev>/parse-trx.ps1 -InputFile <ev>/final/f2/TestResults/f2-<name>.trx -Output <ev>/final/f2/trx-<name>.json` for focused/upstream/hydraulics/negative | 0 each | UTF-8 no-BOM JSON, duplicate-free identities |
| V13-after | `-Lane F2 -Moment After` | 0 | receipt `final/F2/frozen-hashes-after.json` |

## 2. Per-lane TRX totals

| TRX | Total | Passed | Failed | NotExecuted | Identity reconciliation |
|---|---|---|---|---|---|
| f2-focused.trx | 211 | 211 | 0 | 0 | exactly the six intended fixture classes (`DiRegistrationTests`, `CalculationStateServiceTests`, `ProjectSessionThermalStateTests`, `ThermalMultiplicityCharacterizationTests`, `ThermalStateLegacyStoreGuardTests`, `ThermalViewModelTests`); census of parsed identities shows **0 outside-intended** rows (parameterized rows resolve to `ProjectSessionThermalStateTests`) |
| f2-upstream.trx | 21 | 21 | 0 | 0 | exactly 2 classes: `ClimateThermalInvalidationRegressionTests`, `ConstructionThermalInvalidationRegressionTests` |
| f2-hydraulics.trx | 59 | 59 | 0 | 0 | exactly 5 classes: `CalculationContextInvalidationTests`, `CalculationContextWriterAuthorityTests`, `DoubleCalculationPreventionTests`, `PipeSpacingSynchronizationTests`, `ThermalToHydraulicsIntegrationTests` |
| f2-negative.trx | 8 | 8 | 0 | 0 | exactly the 8 guard categories executed (list in §3.6), single class `ThermalStateLegacyStoreGuardTests` |

Parsed identity sets: `final/f2/trx-focused.json`, `trx-upstream.json`, `trx-hydraulics.json`, `trx-negative.json`. Raw TRX retained under `final/f2/TestResults/`.

## 3. Architecture inspection findings (symbol-level)

### 3.1 Immutable snapshots — defensive ingress + read-only egress
`src/Services/Project/ThermalStateSnapshots.cs`
- Ingress `FromPipeType` :57–71 — mutable domain `PipeType` copied field-by-field into immutable `ThermalPipeSnapshot`; null-safe.
- Ingress `FromResult` :229–257 — full-surface copy of mutable `ThermalCalculationResult` into immutable `ThermalResultSnapshot`.
- Defensive list ingress: ctor :222–223 `Array.AsReadOnly(validationErrors?.ToArray() ?? Array.Empty<string>())` — later mutation of the source array cannot leak in.
- Egress: backing field :138 `private readonly ReadOnlyCollection<string> _validationErrors`; property :179 exposes it only as `IReadOnlyList<string>` — a `(string[])` cast is rejected at runtime (`ReadOnlyCollection<T>` reports `IsReadOnly`/non-array), pinned by guard test `SnapshotMutability_GuardDefensivelyCopiesEscapingMutableValues` (executed in f2-negative).
- Outbound egress `ToPipeType` :77–88 always returns a fresh mutable instance; the snapshot never shares its internals.

### 3.2 Exhaustive origins/status — closed enum + switch coverage
`src/Services/Project/ThermalMutationOrigin.cs:8–36` — closed 9-member enum: `User`, `UserReset`, `ProjectLoadReset`, `ProjectLoad`, `ClimateInvalidation`, `ConstructionInvalidation`, `Calculation`, `Initialization`, `SystemApply`. Test reference `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectSessionThermalStateTests.cs`: :585–597 asserts `Enum.GetNames(typeof(ThermalMutationOrigin))` equals the exact 9-name array (closed set); :600–614 `EveryOrigin_FlowsThroughChangedMutation_ResultAndEventCarryIt` over `[ValueSource(AllOrigins)]` (:616); :618–627 `OriginSwitchExpression_CoversEveryMemberExhaustively` proves 9 distinct labels through the exhaustive switch expression :629–641 whose default arm throws.

### 3.3 ONE ProjectSession state instance — reference-identical exposure
`src/Services/Project/ProjectSession.cs`: field :26 `private readonly ProjectSessionThermalState _thermalState;`; ctor :37–42 creates exactly one instance (:41); exposure :35 `public IProjectSessionThermalState ThermalState => _thermalState` returns the same reference on every access. The coordinator contract requires this identity (`src/Services/Project/ThermalStateCoordinator.cs:58–59`: «состояние должно быть reference-identical с `IProjectSession.ThermalState`»).

### 3.4 ZERO legacy writable stores
- `src/Services/Navigation/CalculationStateService.cs:29–38` — complete private field block: `_hydraulicsIsCalculating`, `_hydraulicsValidationMessage`, `_projectSession`, `_thermalChangedHandler`, `_restoreLease`. No `_thermal*` state store and no `_pipeSpacing` backing field exists; header :8–13 documents removal of all Thermal backing stores. Canonical getters read the live snapshot: `ThermalNeedsRecalculation` :72–73, `ThermalIsCalculating` :76–77, `ThermalValidationMessage` :80–81.
- `src/ViewModels/Thermal/ThermalViewModel.cs:26–30` — fields are adapter dependencies plus `_isResetting` only; no thermal status/recalc backing fields. Derived status properties delegate: `RecalcMessage` :197 → service getter, `NeedsRecalculation` :202 → service getter (which reads the canonical snapshot). Input `[ObservableProperty]`s are UI echo surfaces whose change handlers route edits to the coordinator (:109–166), never acting as a second canonical store.

### 3.5 ONE upstream subscriber (+ disposal path); VM has zero
`src/Services/Project/ThermalStateCoordinator.cs`: ctor :84–92 attaches the application's sole upstream subscriptions — `_climateDataImpl.DataChanged += _climateUpstreamHandler` (:89) and `_constructionData.DataChanged += _constructionUpstreamHandler` (:92), commented as the atomic DEC-T04A move from the VM. Disposal path `Dispose` :244 detaches both (:254 climate, :257 construction). Upstream handlers translate exactly once to `InvalidateFromClimate` :264 / `InvalidateFromConstruction` :274. VM census: grep of `src/ViewModels/Thermal/ThermalViewModel.cs` finds **zero** `DataChanged` subscriptions — `IClimateData` appears only as constructor pass-through into isolated coordinator composition (:229, :293, :299).

### 3.6 Context single-writer
Codegraph blast radius: `CalculationContext.UpdateThermalInputs` (`src/Core/CalculationContext.cs:192`) has 16 production callers, **all** inside `src/Services/Project/ThermalStateCoordinator.cs` (call sites :147, :239; sibling `UpdateThermal` calls :166, :187, :240). No ViewModel/service writes Thermal context directly. Enforced by guard `ContextUnapprovedWriter_GuardAllowsOnlyCoordinatorProductionWriter` (manifest `tests/SnowMeltingCalculator.Tests/Services/Project/ThermalStateLegacyStoreGuardTests.cs:27`) — executed green in f2-negative.trx.

The eight executed guard categories (f2-negative.trx): `ContextUnapprovedWriter_GuardAllowsOnlyCoordinatorProductionWriter`, `DiIndependentStateRegistration_GuardRejectsIndependentDescriptorsAndInstances`, `DuplicateUpstreamSubscriber_GuardRequiresOneCoordinatorAttachPerSurface`, `OrchestratorDirectAssign_GuardRequiresRestoreBeforeAdapterProjection`, `ResultsNonCanonicalSave_GuardRequiresCanonicalMapperInput`, `ServiceThermalStore_GuardRejectsThermalAndSpacingBackingFields`, `SnapshotMutability_GuardDefensivelyCopiesEscapingMutableValues`, `VmWritableStore_GuardRejectsThermalStatusBackingFields`.

### 3.7 Consumer semantics preserved (CircuitsViewModel)
`src/ViewModels/Hydraulics/CircuitsViewModel.cs`
- Inputs are notification-only projections from `CalculationContext.ThermalInputs/ThermalResult` (:156–286); no writable thermal stores.
- `OnCalculationContextChanged` :1062–1088: self-source recursion guard :1065–1066 (own `UpdateHydraulics` publications ignored); `ThermalInputs` → `NotifyThermalPropertiesChanged()` only, no recalculation (:1070–1072); `ThermalResult` → notify, then **valid result → exactly one `CalculateAllCollectors()`** (:1078–1081), invalid/null → zero recalculations (fallback shown instead).
- Spacing propagation `OnPipeSpacingChanged` :1093–1106: mm→cm conversion `spacing / 10.0` (:1095), circuit fan-out, one recalculation; no recursion because the resulting hydraulics publication carries source `"CircuitsViewModel"` and is filtered by the :1065 guard.

### 3.8 NO Hydraulics/Results ownership migration
`src/ViewModels/Results/ResultsViewModel.cs` remains a projection: display values read the canonical slice `_projectSession.ThermalState.Snapshot` (:1036–1044; inner diameter :1172–1174). Save path composes persistence data exclusively from the canonical snapshot via the isolated wire mapper: `data.ThermalData = ThermalPersistenceMapper.BuildThermalProjectData(_projectSession.ThermalState.Snapshot)` (:1705–1706). No Results-owned thermal inputs exist anywhere in the file.

### 3.9 AMZ-accepted deviations (NOT violations)

| Deviation | Symbol-level evidence | Status |
|---|---|---|
| AMZ-1 transitional mutation `ApplyNeedsRecalculation` on canonical state | Declared `IProjectSessionThermalState.cs:99`; implemented `ProjectSessionThermalState.cs:172–195` (preserves inputs+result, status-only change, idempotent by value) | EXACTLY ONE production caller: `CalculationStateService.cs:88` (compat route `SetThermalNeedsRecalculation`). Two immutable-test references: `ThermalStateCoordinatorTests.cs:160–165`. Guard manifest row `ThermalStateLegacyStoreGuardTests.cs:27`; DI stub throws `NotSupportedException` (`DiRegistrationTests.cs:486`). Grep census across repo confirms no other production callers |
| Legacy interface writers routed to canonical | `SetThermalNeedsRecalculation` → `ApplyNeedsRecalculation` (`CalculationStateService.cs:84–89`); `SetThermalCalculating` → `BeginCalculation` (:92–95); `ResetThermalState` → `ApplyInputs(SystemApply)` (:98–103); `SetPipeSpacing(spacing, source)` → `ApplyInputEdit(ThermalInputEdit.ForPipeSpacing(...))` with non-canonical-source guard throw (:180–193, guard at :185) | Zero bypass writers remain |
| ProjectLoadReset translation suppression in service | `OnThermalStateChanged` early-return for `Origin == ProjectLoadReset` (`CalculationStateService.cs:224–231`) keeps legacy `StateChanged`/`PipeSpacingChanged` silent while canonical completion still fires | Owner-approved (Todo 9 / AMZ-2 journal) |
| AMZ-2 two characterization pin rows | `ThermalMultiplicityCharacterizationTests.cs:399` and `:1157` — rows updated from pre-Todo-9 quirk pins to DEC-T08 target semantics | Owner-approved, journaled |
| AMZ-3 extended negative manifest CF=4/PF=6/RF=3 | `task-2/expected-negative-test-identities.json`: CalculationFailure = 4 identities (:16–19), PersistenceFailure = 6 (:3–8), RestoreFailure = 3 (:11–13) | Owner-approved, journaled |

## 4. Frozen release binding (echo)

Manifest sha256 `6D039FC7B84C84F389D2DB435B69C354323ACCAB6C62A16C0B8F75475B13BA72`; four artifacts:

| Key | Path | SHA-256 |
|---|---|---|
| executable | src/bin/Release/net8.0-windows/win-x64/SnowMeltingCalculator.exe | BE36766AF72900F8734B6BADD4EF014C6E0FC689EB459B62651EB2CFF3C6335D |
| productDll | src/bin/Release/net8.0-windows/win-x64/SnowMeltingCalculator.dll | E03F335273A1EDFE6706C37828F941992EFF064DE73B91A0345C5CD1E489F5B9 |
| testDll | tests/SnowMeltingCalculator.Tests/bin/Release/net8.0-windows/SnowMeltingCalculator.Tests.dll | E6B451F520BB25AFE543484458861D54EEA1E6729D680A75456DABED3D013D4C |
| plan | docs/architecture-migration/plans/phase-4-thermal-state.md | 327B1288B8072E7A76D814F8439ECBC353F54F4CE59AB2B507595A08980B4C02 |

## 5. Before/after equality statement

`final/F2/frozen-hashes-before.json` vs `final/F2/frozen-hashes-after.json`: manifest sha256 equal (`True`), and all four `key|resolvedPath|sha256` triples identical ignoring the `moment` field (`Compare-Object` diff empty). The frozen write-set was byte-stable across the entire F2 lane — no rebuild, no production/test edit, no git operation occurred.

## 6. Residual risks (accepted, journaled)

- AMZ-1 transitional `ApplyNeedsRecalculation` remains on the canonical interface with exactly one production caller; Todo 11 guards pin the caller set and will fail on any addition.
- `SetPipeSpacing(spacing, source)` remains a temporary legacy write surface routed to canonical edits; guarded to canonical sources only (:185).
- LSP unavailable for this workspace path (known recorded limitation); correctness gated by compiler/suites per migration instructions.
- UI QA keystroke substitution noted in F1 remains environment-specific; not re-exercised in this lane (out of F2 scope).

Domain verdict: APPROVE. Downstream lane F3 may proceed against the identical frozen write-set; any correction invalidates this chain and reruns F1→F4.
