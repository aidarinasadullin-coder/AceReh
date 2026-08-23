# Todo 10 — Complete Thermal persistence mapping; Results save/read canonical projections

Phase 4 `phase-4-thermal-state`, frozen plan (Todo 10, plan lines 446-454).
Base: `master` @ `6a5a96f1763dd952c8d772ecd1d2536eb3b804cf` (verified before work;
unchanged after — no git ops performed). Evidence owner: `task-10/`.

## Write-set (allow-list delta vs task-9)

`task-10/allowed-hunks.json` = task-9's 36 entries + 2 new
(`src/ViewModels/Results/ResultsViewModel.cs`,
`tests/SnowMeltingCalculator.Tests/Project/ProjectRoundTripTests.cs`), deduped →
38 entries. `ThermalPersistenceMapper.cs`, `ThermalPersistenceMapperTests.cs`,
`ResultsViewModelOpenProjectTests.cs` were already allow-listed by task-9.
DTO definitions (`ProjectData.cs`) read-only — untouched. Note: the execution
brief said "task-9's 37"; the actual task-9 manifest contains 36 entries — both
counts refer to the same file, preserved verbatim.

Files changed in this todo:
- `src/Services/Project/ThermalPersistenceMapper.cs` — SAVE half added:
  `BuildThermalProjectData(ThermalStateSnapshot)` (+ private
  `BuildResultProjectData`); restore half untouched.
- `src/ViewModels/Results/ResultsViewModel.cs` — save reads ONLY
  `_projectSession.ThermalState.Snapshot` via mapper; `LoadThermalData` inputs
  (pipe/mode/supply-context fields/spacing/surface) read canonical snapshot;
  `CalculateSystemVolume` pipe from canonical snapshot.
- `tests/.../ThermalPersistenceMapperTests.cs` — 5 new save-half rows incl. the
  production wire-contract property-set proof.
- `tests/.../ProjectRoundTripTests.cs` — 3 canonical round-trip rows.
- `tests/.../ResultsViewModelOpenProjectTests.cs` — canonical-mirror row,
  zero-calculation export row, 3 `[Category("PersistenceFailure")]` rows.

## Wire-field table (DEC-T08 exact contract)

| DTO field | Source (canonical snapshot) | Persisted |
|---|---|---|
| `thermalData.selectedMode` | `Inputs.Mode` | yes |
| `thermalData.supplyTemperature` | `Inputs.SupplyTemperature` | yes |
| `thermalData.groundTemperature` | `Inputs.GroundTemperature` | yes |
| `thermalData.selectedPipe.name` | `Inputs.Pipe.Name` | yes (null omitted) |
| `thermalData.selectedPipe.outerDiameter` | `Inputs.Pipe.OuterDiameter` | yes |
| `thermalData.selectedPipe.innerDiameter` | `Inputs.Pipe.InnerDiameter` | yes |
| `thermalData.selectedPipe.wallThickness` | `Inputs.Pipe.WallThickness` | yes |
| `thermalData.pipeSpacing` | `Inputs.PipeSpacing` | yes |
| `thermalData.result.powerUp/powerDown/powerTotal` | `Result.PowerUp/PowerDown/PowerTotal` | yes |
| `thermalData.result.supplyTemperature/returnTemperature/meanTemperature/deltaT` | `Result.*` | yes |
| `thermalData.result.isValid` | `Result.IsValid` | yes (8-property result contract) |
| status / messages / origins / `Article` / `ThermalConductivity` / runtime-only result fields (`Alpha`, `MeltingHeat`, `RadiationHeat`, `ConvectionHeat`, `ExcessTemperature`, `RFb`, `RD`, `ParameterM`, `EfficiencyEtaR`, `MassFlowRate`, `VolumeFlowRate`, `ValidationErrors`) | — | **never persisted** |

## Property-set proof (production wire-contract)

`BuildThermalProjectData_SerializedPropertySet_IsExactWireContract`
(`ThermalPersistenceMapperTests`) serializes the mapped DTO with the SAME options
as the production save path (`WriteIndented` + `CamelCase` +
`WhenWritingNull` + `JsonStringEnumConverter(camelCase)` — identical to
`ProjectFileService._jsonOptions`) and asserts exact property-name sets:
root `{selectedMode, supplyTemperature, groundTemperature, selectedPipe,
pipeSpacing, result}` (null members omitted when absent), `selectedPipe`
`{name, outerDiameter, innerDiameter, wallThickness}`, `result` exactly the
8-property set. 19 forbidden names (`article`, `thermalConductivity`,
`status`, `phase`, messages, origin, `validationErrors`, runtime-only fields)
asserted absent. File-level drift proof: `ThermalRoundTrip_CanonicalSaveLoad_…`
parses the real `.smc` written by `ProjectFileService.SaveProjectAsync` and
asserts the same sets plus `"version": "1.1"` unchanged.

## Round-trip matrix (all green)

v1.0 fixture load; v1.0/v1.1 canonical save→load semantic equality through both
mapper halves; default-session save (no pipe/result, spacing 200); missing
legacy spacing → 200; pipe match/fallback/null; valid saved result published
with calculator 0; absent/invalid result → exactly one fallback calculation,
invalid never canonical; second load replaces all project-A Thermal values;
save/reload semantic equality of inputs (structural equality) and 8-field
result; VM-mirror divergence never leaks into `.smc`; save/export trigger zero
calculator calls.

## PersistenceFailure coverage (V5 `[Category=PersistenceFailure]`)

- `PersistenceFailure_UnknownPipe_FallsBackToFirstStandard_NoSchemaDrift` —
  frozen fallback to first standard pipe, valid file result published with
  calculator 0, re-save keeps exact property set (no schema drift).
- `PersistenceFailure_MissingOrCorruptSavedResult_FallbackOnce_InvalidNeverCanonical`
  — null and corrupt (`IsValid=false`, 999) results: exactly one fallback calc,
  fallback persisted, invalid value never canonical, project stays clean.
- `PersistenceFailure_FailedFileOperation_PreservesErrorStateWithoutSchemaDrift`
  — failed save op (missing target dir) → `IsSuccess=false`, no file, canonical
  state byte-equal to pre-failure snapshot, subsequent successful save has no
  schema drift.

## Gate table

| Gate | Command (summary) | Exit | Result |
|---|---|---|---|
| G0 protected-pre | `verify-protected-baseline.ps1 -Baseline …task-1/baseline-manifest.json -AllowedHunks …task-10/allowed-hunks.json -Output …task-10/protected-pre.json` | 0 | mismatch 0, allowed 38 |
| G1 build Debug | `dotnet build src/SnowMeltingCalculator.csproj -c Debug --nologo` | 0 | 0 warnings / 0 errors |
| G1 build Release | same, `-c Release` | 0 | 0 warnings / 0 errors |
| G1 tests Release build | `dotnet build tests/SnowMeltingCalculator.Tests.csproj -c Release --nologo` | 0 | 0 warnings / 0 errors |
| G2 V5 filter | `dotnet test … -c Release --no-build --filter "FullyQualifiedName~ProjectRoundTripTests\|FullyQualifiedName~ResultsViewModelOpenProjectTests\|FullyQualifiedName~ProjectLifecycleFlowCharacterizationTests\|FullyQualifiedName~ThermalPersistenceMapperTests" --logger "trx;LogFileName=phase-4-persistence.trx" --results-directory …task-10/TestResults` | 0 | failed=0, passed=76, NotExecuted=1 (F5 fixture skip), total=77 |
| G3 full Release | `dotnet test … -c Release --no-build --logger "trx;LogFileName=phase-4-full-release.trx" --results-directory …task-10/TestResults` | 0 | failed=0, passed=1934, total=1937 |
| G3 parse | `parse-trx.ps1 -InputFile …phase-4-full-release.trx -Output …task-10/trx-full-release.json` | 0 | total=1937 passed=1934 failed=0 notExecuted=3 |
| G3 parse V5 | `parse-trx.ps1 -InputFile …phase-4-persistence.trx -Output …task-10/trx-persistence.json` | 0 | total=77 passed=76 failed=0 notExecuted=1 |
| G4 protected-post | same verifier as G0, `-Output …task-10/protected-post.json` | 0 | mismatch 0, allowed 38 |

Arithmetic vs baseline (1924 rows / 1921 passed / 0 failed / NotExecuted==3):
1937 = 1924 + 13 new Todo-10 tests; 1934 = 1921 + 13; failed 0 = 0;
NotExecuted identities exactly {RegenerateCircuitsBaseline, RegenerateBaseline,
ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile} — no new
or removed identity. All 13 new tests Passed (identity list verified in parsed
TRX).

TRX SHA-256:
- `phase-4-persistence.trx` = `F26B8253A2EBD21658DCD2CAA19FCC70CF5481C6C7523A3FA99A13CA7637A583`
- `phase-4-full-release.trx` = `1EDDC61FCBF158D0510537D3D7678CE2AFECFCEF303773F94BBD7763C24E0F64`

## Read-seam disposition (recorded decision)

`LoadThermalData` inputs and `CalculateSystemVolume` pipe now read the canonical
snapshot (service-cache read `_calculationStateService.PipeSpacing` removed).
The last-result KPI/readiness projection intentionally keeps reading the adapter
surface `_thermalViewModel.Result`: frozen characterization
`ResultsStabilizationPhase1BehaviorContractsTests.RefreshAll_WhenSourceResultIsCleared_ZerosOutputAndMarksNotReady`
(protected file, outside this todo's allow-list) fixes that clearing the adapter
result zeroes Results KPIs without recalculation; switching that source would
contradict frozen observable behavior and require a blocker. Save is unaffected:
it reads only the canonical snapshot (proven by
`SaveCurrentProject_PersistsThermalStateSnapshot_NotThermalViewModelMirror`).

## Fixture corrections during G2 (first V5 run had 2 failures, both test-side)

1. New failure rows initially reused helper DTOs with supply/ground = 0 (< frozen
   `MinSupplyTemperature` 20) → frozen atomic rejection path fired instead of the
   intended restore/fallback paths. Fixed by setting valid temperatures (45/5).
   Production behavior confirmed frozen-correct, not modified.
2. Zero-calculation row asserted KPIs after bare canonical `Restore` without
   publishing to the adapter projection (production publishes via orchestrator
   `LoadResult`). Fixed by seeding the adapter like existing fixtures.

## Deviations

- None from plan scope. No git operations; working tree contains only cumulative
  allowed hunks (Todos 1-10). `ThermalMultiplicityCharacterizationTests.cs`
  untouched. `ProjectData.Version`, JSON naming/options, DTO definitions
  unchanged. PDF/report/export behavior preserved (zero-calc proof green).
