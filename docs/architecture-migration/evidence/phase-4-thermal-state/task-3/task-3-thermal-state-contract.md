# Task 3 — Immutable Thermal state contract, structural equality and direct state tests

Phase: `phase-4-thermal-state` · Todo 3 (frozen plan `docs/architecture-migration/plans/phase-4-thermal-state.md`, lines 376–384; binding contracts DEC-T01 lines 61–88, DEC-T02 lines 90–108)
Base: branch `master`, HEAD `6a5a96f1763dd952c8d772ecd1d2536eb3b804cf` · Date: 2026-08-23
Verdict: **GREEN — contract compiles 0 warnings/0 errors, 73/73 direct state tests pass, NO runtime consumers wired.**

## 1. Deliverables (write-set)

| Artifact | Path | Kind |
|---|---|---|
| Origin enum (closed, DEC-T02) | `src/Services/Project/ThermalMutationOrigin.cs` | NEW production |
| Snapshot value surface (5 types + phase enum) | `src/Services/Project/ThermalStateSnapshots.cs` | NEW production |
| Mutation vocabulary (status/edit/result/event args) | `src/Services/Project/ThermalMutationResult.cs` | NEW production |
| State contract interface | `src/Services/Project/IProjectSessionThermalState.cs` | NEW production |
| Canonical state implementation | `src/Services/Project/ProjectSessionThermalState.cs` | NEW production |
| Direct state tests (73 cases) | `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectSessionThermalStateTests.cs` | NEW test |
| Task evidence | `docs/architecture-migration/evidence/phase-4-thermal-state/task-3/` | NEW evidence |

`src/Services/Project/IProjectSession.cs` intentionally **untouched** (default per task instructions; `ThermalState` exposure on `IProjectSession`/`ProjectSession` is Todo 4). No ViewModel, service, CalculationContext, DI or csproj file was modified. Both csproj files verified SDK-style implicit compilation (no explicit `<Compile Include>` lists) before writing.

## 2. Implemented-type map vs DEC-T01/T02

| Contract row | Implementation | Proof |
|---|---|---|
| `ThermalStateSnapshot{Inputs, Result?, Status}` | `ThermalStateSnapshot` (sealed, IEquatable, compositional equality) | `FreshState_HasExactContractDefaults`, `StateSnapshot_Equality_DetectsEveryComponentChange` |
| `ThermalInputsSnapshot{Mode, SupplyTemperature, GroundTemperature, Pipe?, PipeSpacing}` | `ThermalInputsSnapshot` + static `Default` | `InputsSnapshot_Equality_DetectsEveryFieldChange` (all 5 fields) |
| `ThermalStatusSnapshot{Phase, RecalculationMessage, ValidationMessage}` | `ThermalStatusSnapshot` + static `Default` | `StatusSnapshot_Equality_DetectsEveryFieldChange` (all 3 fields) |
| `Phase: Actual \| NeedsRecalculation \| Calculating` | `ThermalCalculationPhase` enum (exactly 3 members) | lifecycle tests |
| `ThermalPipeSnapshot{Name, Article, OuterDiameter, InnerDiameter, WallThickness, ThermalConductivity}` | `ThermalPipeSnapshot` (6-field ordinal structural equality; `FromPipeType`/`ToPipeType` ingress/egress copies) | `PipeSnapshot_Equality_DetectsEveryFieldChange` (all 6 fields) |
| `ThermalResultSnapshot` = exhaustive `ThermalCalculationResult.cs:153-193` value surface (19 scalars + `IsValid` + ordered immutable `ValidationErrors`) | `ThermalResultSnapshot` (19 scalars + bool + `ReadOnlyCollection<string>` defensive copy) | `ResultSnapshot_Equality_DetectsEveryScalarFieldChange` (19/19 asserted), `ResultSnapshot_Equality_ValidationErrorsAreOrderedAndContentSignificant` |
| Reference equality forbidden as identity | explicit `Equals` everywhere; two structurally equal pipes/results equal while `Not.SameAs` | `StructuralSnapshots_ReferenceEqualityIsNotIdentity` |
| No mutable `PipeType`/arrays/writable backing refs escape | snapshots hold only scalars/strings/read-only wrappers; ingress copies via `FromPipeType`/`FromResult`; egress `ValidationErrors` is `ReadOnlyCollection` (cast to `string[]` throws, `IList` mutations throw) | `[DefensiveCopy]` ×5 |
| Origin enum EXACTLY: User, UserReset, ProjectLoadReset, ProjectLoad, ClimateInvalidation, ConstructionInvalidation, Calculation, Initialization, SystemApply | `ThermalMutationOrigin` (same order) | `OriginEnum_HasExactlyTheClosedDecT02MemberSetInOrder` |
| Mutation status `Changed \| NoChange \| Rejected` (no Cancelled) | `ThermalMutationStatus` | multiplicity tests |
| Result carries status + origin + before + after | `ThermalMutationResult{Status, Origin, Before, After, Errors}` | `ChangedMutation_EmitsExactlyOneCanonicalCompletion_CarryingOriginBeforeAfter` |
| API names exactly: ApplyInputs/ApplyInputEdit/ResetToDefaults/BeginCalculation/CompleteCalculation/FailCalculation/Restore/InvalidateFromClimate/InvalidateFromConstruction + Snapshot + completion event | `IProjectSessionThermalState` (event `Changed` carries `ThermalMutationResult` via `ThermalStateChangedEventArgs.Mutation`) | whole suite |
| `Restore(inputs, savedResult, ProjectLoad)` origin binding | `Restore(inputs, savedResult)` binds `ThermalMutationOrigin.ProjectLoad` internally | `Restore_BindsProjectLoadOrigin_ReplacesFullStateAndNormalizesStatus` |
| Candidate validation before atomic replacement; invalid → Rejected + zero events + before==after | `ValidateInputs` (ValidationConstants ranges, inclusive) → `Reject` returns `Before`/`After` as the SAME instance | `[RejectedCandidate]` ×11 |
| Changed → exactly one completion AFTER replacement; NoChange/Rejected → zero | single `Commit` raises `Changed` once | multiplicity tests |

## 3. Exact defaults (DEC-T01, verified against `ThermalViewModel.Reset()` lines 378–395)

| Field | Default |
|---|---|
| `Inputs.Mode` | `OperatingMode.Melting` |
| `Inputs.SupplyTemperature` | `50.0` |
| `Inputs.GroundTemperature` | `10.0` |
| `Inputs.Pipe` | `null` |
| `Inputs.PipeSpacing` | `200` |
| `Result` | `null` |
| `Status.Phase` | `Actual` |
| `Status.RecalculationMessage` | `""` |
| `Status.ValidationMessage` | `""` |

## 4. Origin × scenario coverage matrix (all 9 origins exercised)

| Origin | Scenarios exercised (test) | Outcome |
|---|---|---|
| `User` | per-field edit with result → exact RU cause message + result preserved + NeedsRecalculation (5 parameterized cases); edit without result → no synthesized message; whole-candidate apply → first-changed-field message; invalid candidate → Rejected; flow-through origin | ✓ |
| `UserReset` | `ResetToDefaults` clears result/status to defaults; lifecycle normalization loop; flow-through | ✓ |
| `ProjectLoadReset` | `ResetToDefaults` restores exact defaults incl. result clear; lifecycle loop; flow-through | ✓ |
| `ProjectLoad` | `Restore` binds origin internally (result + event carry it); lifecycle loop; flow-through | ✓ |
| `ClimateInvalidation` | `InvalidateFromClimate` with result → clear once + NeedsRecalculation once + exact message; without result → zero effect; second call → NoChange; flow-through | ✓ |
| `ConstructionInvalidation` | symmetric to climate; flow-through | ✓ |
| `Calculation` | `BeginCalculation` (phase transition + message clearing; reentrant second call NoChange); `CompleteCalculation` (canonical result + Actual + recalc message cleared + validation message set; identical repeat NoChange); `FailCalculation` (± compatibilityInvalidResult, exact message, null result variant); full sequence; flow-through | ✓ |
| `Initialization` | `ApplyInputs` normalization to Actual + empty messages, result preserved; flow-through | ✓ |
| `SystemApply` | same normalization; non-synthesis edit test; flow-through | ✓ |

Exhaustiveness locks: `OriginEnum_HasExactlyTheClosedDecT02MemberSetInOrder` (names+order), `OriginSwitchExpression_CoversEveryMemberExhaustively` (9 distinct labels), `EveryOrigin_FlowsThroughChangedMutation_ResultAndEventCarryIt` (ValueSource over all 9).

Exact Russian cause messages (character-exact from `ThermalViewModel.cs` lines 117/135/150/165/180; upstream texts 451/466 passed as parameters): «Режим работы изменён. Требуется пересчёт.», «Температура подачи изменена. Требуется пересчёт.», «Температура грунта изменена. Требуется пересчёт.», «Тип трубы изменён. Требуется пересчёт.», «Шаг укладки изменён. Требуется пересчёт.», «Климатические данные изменены. Требуется пересчёт.», «Данные конструкции изменены. Требуется пересчёт.»

## 5. State-level semantic decisions (documented, within DEC-T01..T04)

1. **No wiring**: no `IMarkDirtyService`, no `CalculationContext`, no compatibility events — class is a pure state machine (Todos 4–8 add wiring).
2. **Validation ranges** from `ValidationConstants` (Supply [20,90], Ground [-10,30], Spacing [50,500], inclusive; NaN rejected; undefined `OperatingMode` rejected). Boundary values accepted (test).
3. **`ApplyInputs(User)` with result** uses the first-changed-field message in canonical order Mode→Supply→Ground→Pipe→Spacing — reuses only characterized strings, no invented text.
4. **Lifecycle origins** (UserReset, ProjectLoadReset, ProjectLoad, Initialization, SystemApply) normalize status to Actual + empty messages on input application; input application never clears the result (only `ResetToDefaults` does).
5. **`Restore` validates candidates** (canonical-inputs invariant; corrupt saved data rejected atomically — `[RejectedCandidate]` case).
6. **`CompleteCalculation`/`FailCalculation` do not re-validate** `calculatedInputs` (calculation-record semantics; caller validated pre-calculation).
7. **Upstream invalidation without result**: zero effect including message (characterized Todo-2 rows 14–17); with result: result cleared once, NeedsRecalculation once, `ValidationMessage` untouched.
8. **Double equality is exact** (`double.Equals`, NaN-pair-safe) — every observable change must be detectable; no epsilon.
9. **Snapshot pipe equality is strict 6-field ordinal** (DEC-T01 snapshot contract), deliberately distinct from domain `PipeType.Equals` (case-insensitive, 4 fields) which later todos use for persisted-pipe matching.
10. **`ThermalInputEdit`** typed single-field edit (static factories `ForMode/ForSupplyTemperature/ForGroundTemperature/ForPipe/ForPipeSpacing`).

## 6. Gate results (commands verbatim, PowerShell 5.1 console; verifier scripts require `pwsh` 7 — `powershell.exe` 5.1 fails exit 4 on `ProcessStartInfo.ArgumentList`)

| Gate | Command (abbreviated) | Exit | Result |
|---|---|---|---|
| G0 preflight | `git rev-parse --show-toplevel` / branch / HEAD; `pwsh verify-protected-baseline.ps1 -Baseline …task-1/baseline-manifest.json -AllowedHunks …task-3/allowed-hunks.json -EvidenceRoot docs/architecture-migration/evidence -Output …task-3/protected-pre.json` | 0 | root=`D:/IA/3ace v.2`, branch=master, HEAD=base; `drift=11 protected_mismatch_count=0 allowed_hunk_count=2` (2 = inherited task-2 test files) |
| G1 build product | `dotnet build src/SnowMeltingCalculator.csproj -c Debug --nologo` then `-c Release` | 0 / 0 | **0 warnings, 0 errors** both configs |
| G2 build tests | `dotnet build tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Release --nologo` | 0 | **0 warnings, 0 errors** |
| G3 happy QA | `dotnet test … -c Debug --filter "FullyQualifiedName~ProjectSessionThermalStateTests" --logger "trx;LogFileName=phase-4-state-debug.trx" --results-directory …task-3/TestResults` + `parse-trx.ps1` | 0 / 0 | **73/73 passed, failed=0, NotExecuted=0** → `trx-state-debug.json` |
| G4 failure filters | same suite `-c Debug --filter "FullyQualifiedName~ProjectSessionThermalStateTests&TestCategory=DefensiveCopy\|FullyQualifiedName~ProjectSessionThermalStateTests&TestCategory=RejectedCandidate"` + parse | 0 / 0 | **16 ran (>0), 16/16 passed** → `trx-state-negative.json` |
| G5 full Release | rebuild tests Release (0w/0e) then `dotnet test … -c Release --no-build --logger "trx;LogFileName=task-3-full-release.trx" …` + parse | 0 / 0 | **1853 total / 1850 passed / failed=0 / NotExecuted=3**; identities = exactly the Todo-1 baseline set (`RegenerateCircuitsBaseline`, `RegenerateBaseline`, `ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`); arithmetic reconcile: 1780 (Todo 2) + 73 (new) = 1853, 1777 + 73 = 1850 → `trx-full-release.json` |
| G6 post-verify | `pwsh verify-protected-baseline.ps1 … -AllowedHunks …task-3/allowed-hunks.json -Output …task-3/protected-post.json` | 0 | `drift=17 protected_mismatch_count=0 allowed_hunk_count=8` (2 inherited + 6 task-3 files) |

TRX SHA-256:

```
F22DAB962EE85E4E7E685CBCA57A0802C2D6F766252B4497F0094B42DC0D4A2C  TestResults/phase-4-state-debug.trx
000A0B5EEADB8C9921036915D95AB1F7801BD4A6206F7861EFBB4F35F7071725  TestResults/phase-4-state-negative.trx
C6586C950E7036690C60A47FAFFA8E48BD007125C09AEF5560F3A57F3EB50626  TestResults/task-3-full-release.trx
```

## 7. Test counts

| Suite | Total | Passed | Failed | NotExecuted |
|---|---|---|---|---|
| `ProjectSessionThermalStateTests` (Debug, happy) | 73 | 73 | 0 | 0 |
| — of which `[Category("DefensiveCopy")]` | 5 | 5 | 0 | 0 |
| — of which `[Category("RejectedCandidate")]` | 11 | 11 | 0 | 0 |
| — uncategoryed structural/semantic | 57 | 57 | 0 | 0 |
| Failure-filter run (G4) | 16 | 16 | 0 | 0 |
| Full suite (Release) | 1853 | 1850 | 0 | 3 (baseline identities) |

## 8. Worktree confirmation

`git status --porcelain` contains ONLY: the six new task-3 files (5 production + 1 test), additions under `docs/architecture-migration/evidence/phase-4-thermal-state/` (task-1/2/3 evidence + Todo-1 scripts), the two inherited task-2 test-file deltas, and the two pre-existing dirty control files (`docs/architecture-migration/STATE.json`, `docs/architecture-migration/plans/phase-4-thermal-state.md`) whose hashes match the Todo-1 baseline manifest (no drift, untouched by this task). Zero out-of-allow-list diffs; no git staging/commit/reset performed.
