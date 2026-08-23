# phase-4-thermal-state - Work Plan

## TL;DR (For humans)

Phase 4 переносит только Thermal project state в `ProjectSession.ThermalState`. После выполнения `ProjectSession.ThermalState` будет единственным writable owner для Thermal inputs, pipe spacing, последнего производного Thermal result и Thermal status; `ThermalViewModel` станет WPF adapter, `CalculationStateService` — compatibility adapter, `CalculationContext` — downstream projection bus, а Hydraulics и Results останутся потребителями.

Работа выполняется characterization-first/TDD в одной последовательной central lane. Сначала фиксируются текущие значения, события, dirty transitions, calculation counts, restore/fallback и subscription multiplicity; затем вводится immutable state contract и по одному переводятся adapters, invalidation, lifecycle и persistence. Каждая задача заканчивается компилируемой green boundary. Поддерживаемый `.smc` v1.0/v1.1 wire contract, Thermal formulas, validation ranges и UI сохраняются.

План намеренно НЕ переносит ownership Hydraulics или Results, не меняет `.smc` schema/version, не перестраивает формулы/интерфейс и не выполняет широкую переделку `CalculationContext`. Технический результат проходит четыре независимые final-verification области и только затем ожидает отдельной owner acceptance. Запись или review этого плана не разрешает product-code edits; execution требует отдельного architecture execution gate и worker session.

- **Effort:** architecture-scale, 14 последовательных implementation todos + 4 последовательных final-verification tasks (три обязательных независимых domain verdicts и один безусловный consolidated receipt).
- **Основной риск:** размножение events/calculations при замене owner поверх чистой закоммиченной базы `master`; риск ограничивается NUL-safe clean baseline, per-todo allow-lists, exact multiplicity tests и запретом параллельных central edits.
- **Зафиксированные решения:** полный четырёхкомпонентный Thermal slice; characterization-first/TDD; own-input edit сохраняет last result и ставит `NeedsRecalculation`, upstream user invalidation очищает result; lifecycle restore не создаёт ложную invalidation; status входит в Thermal state; Hydraulics/Results остаются consumers.

### Planning status and routing gate

Этот plan artifact является owner-directed **frozen re-plan candidate**: repository workflow materialized его байт-идентично в canonical path `docs/architecture-migration/plans/phase-4-thermal-state.md` и в этот `.omo` mirror до terminal review и записывает его bytes/SHA-256 identity в `STATE.json` при переходе в `awaiting-owner-approval`; это не active architecture execution plan и не retry/approval отклонённого SHA `4311E0A9BC9CCF7B678C1A3E99EAC99B9D935E9A9199C48B3A4CA80B28C8943A`. Rejected receipt `docs/architecture-migration/evidence/phase-4-thermal-state/planning-consolidated-receipt-amendment-1-rejected.md` остаётся historical rejection; superseded identities (`C238E2876B43F8D606F24DF9682CAFD41F9B71C6C2DDDF965814C99EEC8AD451`, `198A63E690146089F9184504B2455F28C9D2FC636237F900E05B47F328D5BBF7`) и Todo 1 amendment evidence из retired task-owned worktree являются planning input, не executable proof. Никакая строка ниже не исполняется: execution требует отдельного `/architecture-approve phase-4-thermal-state` и затем отдельного `/architecture-start phase-4-thermal-state`; worker не запускает Todo 1/2 и не редактирует canonical plan/`STATE.json`/Boulder вручную. Для этого нового planning cycle `terminalRetryCount=0`; ровно один terminal plan critic выносит `APPROVE|REJECT|BLOCKED`, и только terminal `APPROVE` переводит stage в `awaiting-owner-approval` с planning approved, всеми owner gates/final domains pending, `nextAction=/architecture-approve phase-4-thermal-state`, `stop=true`, `blocker=null`; `REJECT|BLOCKED` оставляет stage blocked. Plan approval и `/architecture-start phase-4-thermal-state` остаются отдельными gates. This 2026-08-22 owner-directed revision re-bases execution onto the current clean `master` of `D:\IA\3ace v.2` via the main-checkout sequential lane (Phases 1–3.1 precedent); the superseded task-owned `.slim` worktree design (branch base `e655735dfa66c00cf9c53be93d511eda8989e8bf`, lacking Phase 3/3.1 code) and its stale `D:\IA\ace v.2` paths are retired as provenance, and the three session-dependency correction hunks are already present at `master` (commit `0ed4ef2`).

## Scope

### In scope

- Current repository only: `D:\IA\3ace v.2`, с execution-time verified Git root/branch/HEAD и fresh clean baseline at the current `master` tip.
- `ProjectSession` Thermal slice:
  - `src/Services/Project/IProjectSession.cs` и `ProjectSession.cs`;
  - новые Thermal state contract/implementation/snapshot/mapper files под `src/Services/Project/`;
  - ровно один runtime state instance, принадлежащий `ProjectSession`.
- Thermal adapter and application boundary:
  - `src/ViewModels/Thermal/ThermalViewModel.cs`;
  - `src/Services/Navigation/ICalculationStateService.cs` и `CalculationStateService.cs` только для Thermal status/spacing compatibility;
  - Thermal validators/calculator interfaces только как read-only contracts, если fresh implementation не докажет узкую необходимую signature adaptation.
- Reactive/downstream seams:
  - Thermal-only projection methods в `src/Core/CalculationContext.cs`;
  - Thermal/spacing consumer seams в `src/ViewModels/Hydraulics/CircuitsViewModel.cs`;
  - Climate/Construction completion subscriptions только для переноса единственного Thermal invalidation subscriber.
- Lifecycle and persistence:
  - Thermal seams в `src/Services/Project/ProjectLoadOrchestrator.cs`;
  - Thermal save/read projection в `src/ViewModels/Results/ResultsViewModel.cs`;
  - существующие `ThermalProjectData`, `PipeTypeProjectData`, `ThermalResultProjectData` как неизменяемые wire DTO contracts;
  - pure internal `ThermalPersistenceMapper`.
- DI and tests:
  - `src/Configuration/ServiceCollectionExtensions.cs` только для identity/lifetime wiring;
  - Thermal state, multiplicity, legacy-writer, VM, calculation-state, upstream invalidation, Hydraulics consumer, lifecycle, persistence, Results и DI tests;
  - evidence под `docs/architecture-migration/evidence/phase-4-thermal-state/`.
- Architecture dossier после green code gates: все шесть maps, supporting inventories/invariants, shared model, generated widget, exact evidence links и workflow state transition.

### Out of scope / Must-NOT-Have

- No `HydraulicsState` creation, Hydraulics ownership migration, collector/circuit formula redesign or broad `CircuitsViewModel` refactor.
- No Results ownership migration; `ResultsViewModel` numeric caches remain derived projections for this phase.
- No Thermal formula, coefficient, validation-range, error-copy, UI/XAML/design or command-surface redesign.
- No `.smc` JSON property, naming, version, serializer-option or migration change; no persistence of status/messages/origins/runtime-only pipe fields.
- No broad `CalculationContext` redesign or removal; only the Thermal projection/writer boundary needed for sole ownership.
- No transactional project restore redesign. Preserve the characterized partial-failure boundary; if impossible, stop for owner decision.
- No package/tool/SDK installation, service locator, second Thermal state singleton, writable fallback owner, placeholder/stub, `NotImplementedException`, `[Ignore]`, Skip, weakened/deleted tests or guessed counters.
- No hand-edit of `architecture-widget.html`; generate it only from canonical model/map inputs.
- No stage/commit/reset/revert/checkout/restore/clean/push of unrelated working-tree paths. Commit lines below are guidance for an explicitly authorized isolated worker only.

### Exact target contract

#### DEC-T01 — Canonical ownership and immutable snapshots

`IProjectSession` exposes `IProjectSessionThermalState ThermalState { get; }`; `ProjectSession` creates exactly one instance. The state is not independently registered in DI. Canonical shape:

```text
ThermalStateSnapshot
- Inputs: ThermalInputsSnapshot
- Result: ThermalResultSnapshot?
- Status: ThermalStatusSnapshot

ThermalInputsSnapshot
- OperatingMode Mode
- double SupplyTemperature
- double GroundTemperature
- ThermalPipeSnapshot? Pipe
- int PipeSpacing

ThermalStatusSnapshot
- Phase: Actual | NeedsRecalculation | Calculating
- RecalculationMessage: string
- ValidationMessage: string
```

`ThermalPipeSnapshot` contains `Name`, `Article`, `OuterDiameter`, `InnerDiameter`, `WallThickness`, `ThermalConductivity`. `ThermalResultSnapshot` exhaustively contains the current `src/Models/Thermal/ThermalCalculationResult.cs:153-193` value surface: `Alpha`, `PowerUp`, `PowerDown`, `PowerTotal`, `MeltingHeat`, `RadiationHeat`, `ConvectionHeat`, `ExcessTemperature`, `MeanTemperature`, `SupplyTemperature`, `ReturnTemperature`, `DeltaT`, `RFb`, `RD`, `ParameterM`, `EfficiencyEtaR`, `MassFlowRate`, `VolumeFlowRate`, `IsValid`, and an immutable/defensively copied ordered `ValidationErrors`. `ResultChanged`, `RaiseResultChanged()` and `ToString()` are behavior surfaces, not snapshot fields. Returned snapshots never share mutable `PipeType`, mutable result arrays/collections or writable backing references. Equality is field-by-field structural equality (including ordered error strings); pipe/result reference equality is forbidden.

Defaults are exact current reset defaults: `Mode=OperatingMode.Melting`, `SupplyTemperature=50.0`, `GroundTemperature=10.0`, `Pipe=null`, `PipeSpacing=200`, `Result=null`, status `Actual`, both messages empty. `LambdaE` remains a derived Construction projection and is added only when building calculator `ThermalInputs`; it is not Thermal-owned state.

The result is the **canonical last derived Thermal result**, not user input. It has no general setter. The only writers are calculation completion/failure, project restore, upstream invalidation and lifecycle reset.

#### DEC-T02 — Mutation/origin/completion API

Use a closed origin enum: `User`, `UserReset`, `ProjectLoadReset`, `ProjectLoad`, `ClimateInvalidation`, `ConstructionInvalidation`, `Calculation`, `Initialization`, `SystemApply`. Never conflate user reset and lifecycle reset.

Public semantic boundaries (equivalent names are allowed only if tests prove the exact contract):

```text
ApplyInputs(candidate, origin)
ApplyInputEdit(edit, origin)
ResetToDefaults(origin)
BeginCalculation()
CompleteCalculation(calculatedInputs, result, validationMessage)
FailCalculation(calculatedInputs, validationMessage, compatibilityInvalidResult?)
Restore(inputs, savedResult, ProjectLoad)
InvalidateFromClimate(message)
InvalidateFromConstruction(message)
```

Mutation status is `Changed | NoChange | Rejected`; do not add `Cancelled` without a real Thermal pre-apply cancellation scenario. Every result carries status, origin, before and after snapshots. Candidate validation/normalization completes before atomic replacement. One changed logical mutation emits exactly one canonical completion after replacement; no-op/rejected emits zero canonical completion, dirty intent, compatibility event, context publication and downstream calculation.

#### DEC-T03 — Own input edit, dirty and status semantics

- Changed `User` input replaces canonical inputs once and issues exactly one `MarkDirty()` intent per logical action. Observable `IsDirty`/`PropertyChanged(IsDirty)` transitions are `1` only when the session was clean and `0` when it was already dirty, because `ProjectSession.MarkDirty()` is idempotent; characterization and acceptance assert intent count separately from transition count.
- If a result exists, own input edit preserves the last result, transitions status to `NeedsRecalculation`, uses the exact current Russian cause message, and publishes one compatibility Thermal state event.
- If no result exists, input edit does not synthesize a recalculation event.
- Input edits never invoke the calculator and do not publish new `CalculationContext.ThermalInputs` until Calculate/restore completion.
- User Thermal Reset preserves current observable behavior: resets defaults/result/status and does **not** mark dirty. Changing this is a separately approved product behavior change and is forbidden here.
- `ProjectLoadReset`, `ProjectLoad`, initialization/system apply, calculation completion and upstream invalidation create no additional dirty; dirty belongs only to the originating user input or upstream user action.

#### DEC-T04 — Upstream invalidation and Phase 3.1 preservation

- Genuine changed user Climate/Construction completion clears canonical Thermal result once and sets `NeedsRecalculation` once only if a result existed.
- No-op/rejected upstream action produces zero Thermal effects.
- Lifecycle Climate/Construction `Load`, `ProjectLoadReset`, `Restore`, `SystemApply`, `Initialization` synchronizes projections without Thermal invalidation.
- Upstream invalidation never marks dirty again; Climate/Construction owns that dirty action.
- Accepted Phase 3.1 publication-source contract remains unchanged. Move the sole Thermal subscriber from `ThermalViewModel` to the canonical Thermal application/state boundary; never retain both subscribers at a green boundary and never replace origin semantics with an `IsLoadProjectInProgress` guard.

#### DEC-T04A — Concrete Thermal application boundary

Create `src/Services/Project/ThermalStateCoordinator.cs` as sealed singleton `IThermalStateCoordinator`, registered once in `ServiceCollectionExtensions` and eagerly materialized by constructor injection into the singleton `ThermalViewModel` (never resolved through a service locator). It receives the reference-identical `IProjectSession.ThermalState`, `ClimateState`, `ConstructionState`, `CalculationContext`, `IMarkDirtyService`, and `IThermalCalculator` dependencies. Todo 6 creates it and gives it exactly these responsibilities: translate Thermal user commands into closed state mutations, issue one dirty intent for changed Thermal user inputs, orchestrate validation/calculation, publish the one approved context projection, and expose immutable completion data to adapters. Todo 7 adds its sole Climate/Construction completion subscriptions and removes the VM subscriptions atomically. It owns/disposes those subscriptions for the application singleton lifetime; the VM only binds/commands/refreshes and never becomes another owner or subscriber. `ProjectLoadOrchestrator`, persistence mapper, Hydraulics and Results call/read the state/coordinator contracts but do not construct another coordinator.

#### DEC-T05 — Calculation sequence and failure matrix

Observable order:

1. Validate current canonical input candidate.
2. Invalid input: calculator `0`, context events `0`, canonical result unchanged, phase unchanged, `ValidationMessage` set to validator text.
3. `BeginCalculation`: phase `Calculating`, clear recalculation and validation messages.
4. Publish calculated `CalculationContext.ThermalInputs` once.
5. Invoke calculator once.
6. Store canonical returned result (valid or invalid).
7. Publish `CalculationContext.ThermalResult` once.
8. Valid result causes exactly one Hydraulics calculation through the existing consumer; invalid/null causes zero.
9. Finish phase `Actual`; calculation creates no dirty.

Successful result clears messages. Calculator-returned invalid result is stored canonically, phase becomes `Actual`, and combined result errors become `ValidationMessage`. Exception sets result to `null`, phase `Actual`, exact `Ошибка расчёта: {ex.Message}` text; the compatibility null/invalid context publication must follow the frozen characterization result from Todo 2 rather than executor preference. Reentrant/double Calculate while already `Calculating` performs no additional calculator/context/downstream work.

#### DEC-T06 — Pipe spacing compatibility

Pipe spacing belongs to `ThermalInputsSnapshot`. `ICalculationStateService.PipeSpacing` and `PipeSpacingChanged` remain temporary compatibility read/event surfaces but lose their writable backing store.

- Changed user spacing: one canonical completion, one dirty, one `PipeSpacingChanged`, every circuit receives `spacing/10.0`, exactly one logical `CalculateAllCollectors()`, and—if a Thermal result exists—one `NeedsRecalculation` while preserving last result.
- No-op spacing: all counters zero.
- Project load spacing: non-user canonical apply, zero dirty, at most one compatibility spacing event if changed, no string-based writer authority, and no stale project-A result after complete restore.
- Existing `SetPipeSpacing(int, string)` cannot remain an independent canonical writer.

#### DEC-T07 — Compatibility services and consumers

`CalculationStateService` delegates Thermal status/spacing to `IProjectSessionThermalState`, translates canonical completions into existing `StateChanged`/`PipeSpacingChanged`, and removes `_thermalNeedsRecalculation`, `_thermalIsCalculating`, `_thermalValidationMessage`, `_pipeSpacing`. It retains Hydraulics backing state and restore-lease adapter. Any parameterless compatibility constructor creates one internally consistent isolated session and is never a second runtime singleton.

`CalculationContext` is a read/projection bus, not an owner. Approved Thermal projection publication has one writer boundary. `CircuitsViewModel` remains a consumer: ThermalInputs notification-only; valid result exactly one Hydraulics calculation; invalid/null zero; own-source events cannot recurse. `ResultsViewModel` remains a projection and may retain unrelated concrete module dependencies, but Thermal save/read paths consume state/projection only.

#### DEC-T08 — Restore and persistence matrix

Restore preserves current order and fallback behavior: Climate → Construction → Thermal inputs/spacing/pipe → saved Thermal result → Hydraulics inputs/collections → valid saved result publish or fallback calculate → saved circuit-result restore.

| Input | Required outcome |
|---|---|
| valid saved result | apply inputs/result once; calculator `0`; Phase 3.1 invalidation `0` |
| result absent | apply inputs; calculator `1`; publish successful fallback result once |
| saved result invalid | calculator `1`; invalid saved result is not final canonical result |
| persisted pipe matches standard | canonical pipe uses matching standard definition |
| persisted pipe unknown | canonical pipe uses first available standard pipe, preserving current fallback |
| persisted pipe null | pipe remains null after lifecycle reset |
| missing legacy spacing | spacing `200` |
| second load | every Thermal input/result/status from project A is replaced by project B; zero stale values |
| repeated load/reset | no subscription/event/calculation multiplication |
| restore exception | restore lease clears; preserve characterized non-transactional partial-state behavior |

Add a pure `ThermalPersistenceMapper`. Save reads only `IProjectSession.ThermalState.Snapshot`. Exact unchanged wire fields are:

- inputs: `SelectedMode`, `SupplyTemperature`, `GroundTemperature`, `SelectedPipe.{Name,OuterDiameter,InnerDiameter,WallThickness}`, `PipeSpacing`;
- result: `PowerUp`, `PowerDown`, `PowerTotal`, `SupplyTemperature`, `ReturnTemperature`, `MeanTemperature`, `DeltaT`, `IsValid`.

Never persist status, messages, origins, `Article`, `ThermalConductivity` or canonical metadata. Do not change `ProjectData.Version`, JSON naming/options or DTO definitions. Semantic round-trip is required; byte-identical JSON is not required unless an existing fixture explicitly demands it.

The runtime-only result fields excluded from `.smc` are exactly `Alpha`, `MeltingHeat`, `RadiationHeat`, `ConvectionHeat`, `ExcessTemperature`, `RFb`, `RD`, `ParameterM`, `EfficiencyEtaR`, `MassFlowRate`, `VolumeFlowRate`, and `ValidationErrors`; they restore to the existing CLR/default mapper values because the wire DTO does not contain them. The persisted result subset remains only the seven numeric fields listed above plus `IsValid`, for an exact eight-property result contract.

### Execution and recovery discipline

- One sequential central implementation lane. No two workers edit ProjectSession/Thermal state/VM/CalculationStateService/context/load/reset/Results/DI or their direct tests concurrently.
- Before every todo, compare a fresh NUL-safe status against Todo 1 baseline. A task write-set is its explicit allow-list plus its evidence files only.
- For every pre-dirty allow-listed file, preserve SHA-256 preimage and exact patch/hunk receipt. Protected set equals all baseline content except worker-owned exact hunks.
- Recovery uses a minimal inverse patch for worker-owned hunks or restores a task-created file from its recorded preimage. Never use Git reset/checkout/restore/clean in the shared working tree.
- If characterization contradicts DEC-T01..T08 in observable behavior, wire contract or central scope, stop, record evidence/options, set workflow blocked and ask owner. Never weaken tests or improvise.
- Before any execution, canonical Phase 4 plan identity and owner gates must be materialized by the repository workflow. If external Boulder mismatch blocks a hook, report and stop; never hand-edit stale Boulder state to satisfy it.

## Verification strategy

- **Strategy:** characterization-first/TDD. Todo 2 creates/extends RED ownership and multiplicity contracts before any canonical ownership edit; each production todo ships with direct tests in the same green boundary.
- **Compiler/build:** Debug and Release production builds, no new warnings/errors relative to Todo 1 baseline.
- **Focused suites:** new `ProjectSessionThermalStateTests`, `ThermalMultiplicityCharacterizationTests`, `ThermalStateLegacyStoreGuardTests`, `ThermalPersistenceMapperTests`, plus existing Thermal VM/calculator/state-service and DI tests.
- **Affected suites:** Phase 3/3.1 upstream invalidation, CalculationContext, Thermal-to-Hydraulics, spacing synchronization, double-calculation prevention, lifecycle/reset, ProjectRoundTrip, Results open/save/export.
- **Full gate:** full Release suite with TRX identity reconciliation; no new failures or `NotExecuted` identities. Baseline-known `NotExecuted` is compared by exact test name, not count alone.
- **Agent-operated user-flow QA:** run the built WPF application through a task-owned PowerShell `System.Windows.Automation` harness, exercise Thermal edit/calculate/Hydraulics/Results/save/load/second-load/new-reset, and capture screenshots/process logs/observed values. No owner action or manual fallback is an acceptance dependency; inability to resolve a selector, dialog, or screenshot is a hard QA failure.
- **Architecture:** validate state/plan identity, model-v2, runtime-v2, deterministic two-pass widget generation and all six views.

### Exact execution-root, creator and command-write contracts

All commands run from the exact repository root `D:\IA\3ace v.2` in PowerShell 7 (`pwsh`) and preserve `$LASTEXITCODE`. Before every Todo/lane, run `git rev-parse --show-toplevel`, `git rev-parse HEAD`, `git branch --show-current`, and `git status --porcelain=v1 -z --branch`; require the literal repository root, the `master` branch, and the workflow-recorded HEAD bound at execution authorization. A mismatch exits nonzero before any write. Relative source, build, test and evidence paths resolve only beneath that verified root.

Every command caller owns its task evidence directory plus generated `src/bin`, `src/obj`, `tests/SnowMeltingCalculator.Tests/bin`, and `tests/SnowMeltingCalculator.Tests/obj` outputs for the duration of that sequential task; these generated outputs are excluded from source allow-list comparison but included in the caller's dirty-output receipt. V2-V6 are parameterized templates: substitute `<OWNER>` with the literal owning row (`task-2`, `task-7`, `task-8`, `task-10`, `task-11`, or `task-12`) before invocation, and write only `docs/architecture-migration/evidence/phase-4-thermal-state/<OWNER>/TestResults`. An unsubstituted placeholder or `catalog/v*` path fails before invocation. No caller writes shared `tests/.../TestResults`, another Todo's directory, or another F lane. F1-F4 never build and therefore never write `bin/obj`.

| Script/artifact | Exists at base or creator | First legal use | Mutable output owner / consumers |
|---|---|---|---|
| `validate-state.mjs`, widget verifier/generator, schemas and existing source/tests/maps | Exists at base | As named | Read-only except Todo 14's allow-listed model/maps/widget outputs |
| `capture-baseline.ps1`, `verify-protected-baseline.ps1`, `verify-plan-structure.ps1`, `parse-trx.ps1` | Todo 1 creates before invocation | Todo 1; V10 only after Todo 1 | Todo 1 fixtures/receipts; F1 consumes scripts read-only |
| `baseline-git-status.bin`, `todo-1-completion.json` | Todo 1 creates after fresh repository-root gates | Todo 2 gate | Immutable after Todo 1; F1/F4 read-only |
| `expected-negative-test-identities.json` | Todo 2 | Todo 2 | Todo 2 immutable manifest; Todo 12/F3 consume |
| `assert-trx-identities.ps1`, `verify-frozen-release.ps1`, `verify-final-receipts.ps1`, `frozen-release-sha256.json` | Todo 12 | Todo 12, then F lanes | Todo 12 fixtures/manifest; F1-F4 consume scripts/manifest read-only and write only their lane |
| `prepare-ui-fixtures.ps1`, `run-wpf-ui-qa.ps1`, `fixture-manifest.json` | Todo 13 | Todo 13 | Todo 13 outputs; F3 invokes scripts read-only with F3-owned outputs |
| `model-v2.json`, `runtime-v2.json`, six widget screenshots | Todo 14 | Todo 14 | Todo 14 canonical evidence; F1/F4 use separately named lane outputs |

No conditional "if present" creator is allowed. The catalog is partitioned by first legal execution: V0-V8/V10 are definitions available only after their named prerequisites exist; V11 is defined and first executable inside Todo 11 after the guard suite exists and is forbidden in Todos 1-10; V9 is defined and first executable inside Todo 13 after Todos 12-13 create all inputs; V12-F1/F2/F3 and V13 are defined and first executable only in the Final wave after Todos 12-14. A later-phase definition is not an earlier Todo reference and cannot be invoked before its creator. Any earlier Todo text that names a later-only command, missing creator, unresolved path, duplicate creator, command output outside its owner, or source write caused by a verification command fails closed.

### Exact command catalog

```powershell
# V0 — authoritative state/plan gate; expect exit 0 and JSON valid=true.
node "docs/architecture-migration/workflow/validate-state.mjs" validate --check-plan

# V1 — production builds; expect exit 0, 0 errors, no warning increase vs Todo 1 baseline.
dotnet build "src/SnowMeltingCalculator.csproj" -c Debug --nologo
dotnet build "src/SnowMeltingCalculator.csproj" -c Release --nologo
# Build the Release test assembly before any V2-V6 --no-build invocation; expect exit 0.
dotnet build "tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj" -c Release --nologo

# V2 — focused canonical state/adapter/guard; expect exit 0, failed=0, every named new class executed.
dotnet test "tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj" -c Release --no-build --filter "FullyQualifiedName~ProjectSessionThermalStateTests|FullyQualifiedName~ThermalMultiplicityCharacterizationTests|FullyQualifiedName~ThermalViewModelTests|FullyQualifiedName~CalculationStateServiceTests|FullyQualifiedName~DiRegistrationTests" --logger "trx;LogFileName=phase-4-focused.trx" --results-directory "docs/architecture-migration/evidence/phase-4-thermal-state/<OWNER>/TestResults"

# V3 — upstream invalidation; expect exit 0, failed=0 and exact Phase 3/3.1 count assertions.
dotnet test "tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj" -c Release --no-build --filter "FullyQualifiedName~ClimateThermalInvalidationRegressionTests|FullyQualifiedName~ConstructionThermalInvalidationRegressionTests" --logger "trx;LogFileName=phase-4-upstream-invalidation.trx" --results-directory "docs/architecture-migration/evidence/phase-4-thermal-state/<OWNER>/TestResults"

# V4 — context/Hydraulics consumers; expect exit 0, failed=0 and exact count/order assertions.
dotnet test "tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj" -c Release --no-build --filter "FullyQualifiedName~ThermalToHydraulicsIntegrationTests|FullyQualifiedName~PipeSpacingSynchronizationTests|FullyQualifiedName~DoubleCalculationPreventionTests|FullyQualifiedName~CalculationContextInvalidationTests|FullyQualifiedName~CalculationContextWriterAuthorityTests" --logger "trx;LogFileName=phase-4-hydraulics-consumer.trx" --results-directory "docs/architecture-migration/evidence/phase-4-thermal-state/<OWNER>/TestResults"

# V5 — lifecycle/persistence/Results; expect exit 0, failed=0 and all mapper/lifecycle fixtures executed.
dotnet test "tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj" -c Release --no-build --filter "FullyQualifiedName~ProjectRoundTripTests|FullyQualifiedName~ResultsViewModelOpenProjectTests|FullyQualifiedName~ProjectLifecycleFlowCharacterizationTests|FullyQualifiedName~ThermalPersistenceMapperTests" --logger "trx;LogFileName=phase-4-persistence.trx" --results-directory "docs/architecture-migration/evidence/phase-4-thermal-state/<OWNER>/TestResults"

# V6 — full Release; expect exit 0, failed=0, no new exact NotExecuted identity vs baseline.
dotnet test "tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj" -c Release --no-build --logger "trx;LogFileName=phase-4-full-release.trx" --results-directory "docs/architecture-migration/evidence/phase-4-thermal-state/<OWNER>/TestResults"

# V7 — model/runtime; expect each exit 0 and passing JSON receipts.
node "docs/architecture-migration/widget/verify-widget.mjs" --suite model-v2 --schema "docs/architecture-migration/maps/architecture-model.widget.schema.json" --model "docs/architecture-migration/maps/architecture-model.json" --output "docs/architecture-migration/evidence/phase-4-thermal-state/model-v2.json"
node "docs/architecture-migration/widget/verify-widget.mjs" --suite runtime-v2 --schema "docs/architecture-migration/maps/architecture-model.widget.schema.json" --model "docs/architecture-migration/maps/architecture-model.json" --output "docs/architecture-migration/evidence/phase-4-thermal-state/runtime-v2.json"

# V8 — deterministic widget; expect exit 0 and before/after hashes equal.
node "docs/architecture-migration/widget/generate-widget.mjs"
$before=(Get-FileHash "docs/architecture-migration/architecture-widget.html" -Algorithm SHA256).Hash
node "docs/architecture-migration/widget/generate-widget.mjs"
$after=(Get-FileHash "docs/architecture-migration/architecture-widget.html" -Algorithm SHA256).Hash
if ($before -ne $after) { throw "nondeterministic widget: $before != $after" }
node "docs/architecture-migration/widget/generate-widget.mjs" --check

# V9 — Todo 13-only definition: prepare deterministic task-owned .smc inputs, then run the frozen WPF executable through the UI QA harness.
# Each command must exit 0; harness starts/owns/closes the process and writes JSON/screenshots/logs.
pwsh -NoProfile -File "docs/architecture-migration/evidence/phase-4-thermal-state/prepare-ui-fixtures.ps1" -Source "tests/SnowMeltingCalculator.Tests/Fixtures/v1-sample.smc" -OutputDirectory "docs/architecture-migration/evidence/phase-4-thermal-state/task-13/fixtures"
pwsh -NoProfile -File "docs/architecture-migration/evidence/phase-4-thermal-state/run-wpf-ui-qa.ps1" -Executable "src/bin/Release/net8.0-windows/win-x64/SnowMeltingCalculator.exe" -ExpectedExecutableSha256File "docs/architecture-migration/evidence/phase-4-thermal-state/frozen-release-sha256.json" -ProjectA "docs/architecture-migration/evidence/phase-4-thermal-state/task-13/fixtures/project-a.smc" -ProjectB "docs/architecture-migration/evidence/phase-4-thermal-state/task-13/fixtures/project-b.smc" -InvalidProject "docs/architecture-migration/evidence/phase-4-thermal-state/task-13/fixtures/unknown-pipe.smc" -OutputDirectory "docs/architecture-migration/evidence/phase-4-thermal-state/task-13/ui-qa"

# V10 — Todo 1-created fail-closed structural verifier; canonical plan and mirror are hash-bound by STATE.json before execution.
pwsh -NoProfile -File "docs/architecture-migration/evidence/phase-4-thermal-state/verify-plan-structure.ps1" -Plan "docs/architecture-migration/plans/phase-4-thermal-state.md" -Output "docs/architecture-migration/evidence/phase-4-thermal-state/final/f1/plan-structure.json"

# V11 — Todo 11-created repository-wide legacy-store guard suite; defined and first executable inside Todo 11 only, forbidden in Todos 1-10; Todo 11-owned output.
dotnet test "tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj" -c Release --no-build --filter "FullyQualifiedName~ThermalStateLegacyStoreGuardTests&TestCategory=NegativeFixture" --logger "trx;LogFileName=phase-4-legacy-store-guards.trx" --results-directory "docs/architecture-migration/evidence/phase-4-thermal-state/task-11/TestResults"

# V12-F2 — isolated final F2 test receipts; reads Todo 12's frozen Release binaries and writes only F2-owned TRX.
dotnet test "tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj" -c Release --no-build --filter "FullyQualifiedName~ProjectSessionThermalStateTests|FullyQualifiedName~ThermalMultiplicityCharacterizationTests|FullyQualifiedName~ThermalStateLegacyStoreGuardTests|FullyQualifiedName~ThermalViewModelTests|FullyQualifiedName~CalculationStateServiceTests|FullyQualifiedName~DiRegistrationTests" --logger "trx;LogFileName=f2-focused.trx" --results-directory "docs/architecture-migration/evidence/phase-4-thermal-state/final/f2/TestResults"
dotnet test "tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj" -c Release --no-build --filter "FullyQualifiedName~ClimateThermalInvalidationRegressionTests|FullyQualifiedName~ConstructionThermalInvalidationRegressionTests" --logger "trx;LogFileName=f2-upstream.trx" --results-directory "docs/architecture-migration/evidence/phase-4-thermal-state/final/f2/TestResults"
dotnet test "tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj" -c Release --no-build --filter "FullyQualifiedName~ThermalToHydraulicsIntegrationTests|FullyQualifiedName~PipeSpacingSynchronizationTests|FullyQualifiedName~DoubleCalculationPreventionTests|FullyQualifiedName~CalculationContextInvalidationTests|FullyQualifiedName~CalculationContextWriterAuthorityTests" --logger "trx;LogFileName=f2-hydraulics.trx" --results-directory "docs/architecture-migration/evidence/phase-4-thermal-state/final/f2/TestResults"
dotnet test "tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj" -c Release --no-build --filter "FullyQualifiedName~ThermalStateLegacyStoreGuardTests&TestCategory=NegativeFixture" --logger "trx;LogFileName=f2-negative.trx" --results-directory "docs/architecture-migration/evidence/phase-4-thermal-state/final/f2/TestResults"

# V12-F3 — isolated final F3 receipts; invoked only after F2 completes; no build/output mutation outside final/f3.
dotnet test "tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj" -c Release --no-build --logger "trx;LogFileName=f3-full-release.trx" --results-directory "docs/architecture-migration/evidence/phase-4-thermal-state/final/f3/TestResults"
dotnet test "tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj" -c Release --no-build --filter "TestCategory=CalculationFailure" --logger "trx;LogFileName=f3-calculation-failure.trx" --results-directory "docs/architecture-migration/evidence/phase-4-thermal-state/final/f3/TestResults"
dotnet test "tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj" -c Release --no-build --filter "TestCategory=PersistenceFailure" --logger "trx;LogFileName=f3-persistence-failure.trx" --results-directory "docs/architecture-migration/evidence/phase-4-thermal-state/final/f3/TestResults"
dotnet test "tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj" -c Release --no-build --filter "TestCategory=RestoreFailure" --logger "trx;LogFileName=f3-restore-failure.trx" --results-directory "docs/architecture-migration/evidence/phase-4-thermal-state/final/f3/TestResults"
pwsh -NoProfile -File "docs/architecture-migration/evidence/phase-4-thermal-state/assert-trx-identities.ps1" -InputFile "docs/architecture-migration/evidence/phase-4-thermal-state/final/f3/TestResults/f3-calculation-failure.trx" -ExpectedManifest "docs/architecture-migration/evidence/phase-4-thermal-state/expected-negative-test-identities.json" -ExpectedGroup "CalculationFailure" -Output "docs/architecture-migration/evidence/phase-4-thermal-state/final/f3/calculation-failure-identities.json"
pwsh -NoProfile -File "docs/architecture-migration/evidence/phase-4-thermal-state/assert-trx-identities.ps1" -InputFile "docs/architecture-migration/evidence/phase-4-thermal-state/final/f3/TestResults/f3-persistence-failure.trx" -ExpectedManifest "docs/architecture-migration/evidence/phase-4-thermal-state/expected-negative-test-identities.json" -ExpectedGroup "PersistenceFailure" -Output "docs/architecture-migration/evidence/phase-4-thermal-state/final/f3/persistence-failure-identities.json"
pwsh -NoProfile -File "docs/architecture-migration/evidence/phase-4-thermal-state/assert-trx-identities.ps1" -InputFile "docs/architecture-migration/evidence/phase-4-thermal-state/final/f3/TestResults/f3-restore-failure.trx" -ExpectedManifest "docs/architecture-migration/evidence/phase-4-thermal-state/expected-negative-test-identities.json" -ExpectedGroup "RestoreFailure" -Output "docs/architecture-migration/evidence/phase-4-thermal-state/final/f3/restore-failure-identities.json"
pwsh -NoProfile -File "docs/architecture-migration/evidence/phase-4-thermal-state/parse-trx.ps1" -InputDirectory "docs/architecture-migration/evidence/phase-4-thermal-state/final/f3/TestResults" -Output "docs/architecture-migration/evidence/phase-4-thermal-state/final/f3/trx-identities.json"
pwsh -NoProfile -File "docs/architecture-migration/evidence/phase-4-thermal-state/prepare-ui-fixtures.ps1" -Source "tests/SnowMeltingCalculator.Tests/Fixtures/v1-sample.smc" -OutputDirectory "docs/architecture-migration/evidence/phase-4-thermal-state/final/f3/fixtures"
pwsh -NoProfile -File "docs/architecture-migration/evidence/phase-4-thermal-state/run-wpf-ui-qa.ps1" -Executable "src/bin/Release/net8.0-windows/win-x64/SnowMeltingCalculator.exe" -ExpectedExecutableSha256File "docs/architecture-migration/evidence/phase-4-thermal-state/frozen-release-sha256.json" -ProjectA "docs/architecture-migration/evidence/phase-4-thermal-state/final/f3/fixtures/project-a.smc" -ProjectB "docs/architecture-migration/evidence/phase-4-thermal-state/final/f3/fixtures/project-b.smc" -InvalidProject "docs/architecture-migration/evidence/phase-4-thermal-state/final/f3/fixtures/unknown-pipe.smc" -OutputDirectory "docs/architecture-migration/evidence/phase-4-thermal-state/final/f3/ui-qa"

# V12-F1 — F1-owned model/runtime receipts; does not overwrite Todo 14 canonical receipts.
node "docs/architecture-migration/widget/verify-widget.mjs" --suite model-v2 --schema "docs/architecture-migration/maps/architecture-model.widget.schema.json" --model "docs/architecture-migration/maps/architecture-model.json" --output "docs/architecture-migration/evidence/phase-4-thermal-state/final/f1/model-v2.json"
node "docs/architecture-migration/widget/verify-widget.mjs" --suite runtime-v2 --schema "docs/architecture-migration/maps/architecture-model.widget.schema.json" --model "docs/architecture-migration/maps/architecture-model.json" --output "docs/architecture-migration/evidence/phase-4-thermal-state/final/f1/runtime-v2.json"

# V13 — immutable four-artifact binding; use the exact lane/moment invocation immediately before and after each F1-F4 lane.
pwsh -NoProfile -File "docs/architecture-migration/evidence/phase-4-thermal-state/verify-frozen-release.ps1" -Manifest "docs/architecture-migration/evidence/phase-4-thermal-state/frozen-release-sha256.json" -Lane F1 -Moment Before
pwsh -NoProfile -File "docs/architecture-migration/evidence/phase-4-thermal-state/verify-frozen-release.ps1" -Manifest "docs/architecture-migration/evidence/phase-4-thermal-state/frozen-release-sha256.json" -Lane F1 -Moment After
pwsh -NoProfile -File "docs/architecture-migration/evidence/phase-4-thermal-state/verify-frozen-release.ps1" -Manifest "docs/architecture-migration/evidence/phase-4-thermal-state/frozen-release-sha256.json" -Lane F2 -Moment Before
pwsh -NoProfile -File "docs/architecture-migration/evidence/phase-4-thermal-state/verify-frozen-release.ps1" -Manifest "docs/architecture-migration/evidence/phase-4-thermal-state/frozen-release-sha256.json" -Lane F2 -Moment After
pwsh -NoProfile -File "docs/architecture-migration/evidence/phase-4-thermal-state/verify-frozen-release.ps1" -Manifest "docs/architecture-migration/evidence/phase-4-thermal-state/frozen-release-sha256.json" -Lane F3 -Moment Before
pwsh -NoProfile -File "docs/architecture-migration/evidence/phase-4-thermal-state/verify-frozen-release.ps1" -Manifest "docs/architecture-migration/evidence/phase-4-thermal-state/frozen-release-sha256.json" -Lane F3 -Moment After
pwsh -NoProfile -File "docs/architecture-migration/evidence/phase-4-thermal-state/verify-frozen-release.ps1" -Manifest "docs/architecture-migration/evidence/phase-4-thermal-state/frozen-release-sha256.json" -Lane F4 -Moment Before
pwsh -NoProfile -File "docs/architecture-migration/evidence/phase-4-thermal-state/verify-frozen-release.ps1" -Manifest "docs/architecture-migration/evidence/phase-4-thermal-state/frozen-release-sha256.json" -Lane F4 -Moment After
```

**V9 fixture contract (exact generator):** Todo 13 creates QA-only `prepare-ui-fixtures.ps1` beside the harness. It uses `Get-Content -Raw | ConvertFrom-Json`, modifies only existing camel-case JSON properties, and writes UTF-8 JSON with `ConvertTo-Json -Depth 100`; it never invokes or bypasses production persistence code. It (1) copies the source unchanged to `project-a.smc`; (2) creates `project-b.smc` with `projectNumber="PHASE4-B"`, `thermalData.supplyTemperature=55.0`, `groundTemperature=5.0`, `pipeSpacing=150`, standard pipe `{name:"RAUTHERM S 17x2,0",outerDiameter:17.0,innerDiameter:13.0,wallThickness:2.0}`, and `result=null`; and (3) creates `unknown-pipe.smc` from Project B with `projectNumber="PHASE4-UNKNOWN-PIPE"` and pipe `{name:"PHASE4 UNKNOWN PIPE",outerDiameter:99.0,innerDiameter:95.0,wallThickness:2.0}`. Before writing, it asserts every mutated path already exists in the source DTO except values intentionally replaced; after writing, it reparses all three files, asserts Project A SHA equals source SHA, asserts the exact Project B/unknown-pipe values, and emits `fixture-manifest.json` with source/output SHA-256. Missing paths, parse errors, or mismatched values exit nonzero. `ThermalPersistenceMapperTests` in Todo 10 remains the production wire-contract proof; this QA generator is only deterministic fixture preparation and must preserve the mapper test's exact property-set assertion.

**V9 harness contract (exact agent-executable desktop tool):** Todo 13 creates the QA-only `run-wpf-ui-qa.ps1` under the Phase 4 evidence directory; it may use only inbox PowerShell/.NET APIs (`System.Windows.Automation`, `System.Drawing`, `Start-Process`) and must require Windows interactive desktop. Todo 6 adds the exact 17 accessibility-only, binding-neutral `AutomationProperties.AutomationId` attributes of the Todo 6 accessibility contract (the catalog list below and the Todo 6 contract are one identical single set; any divergence fails closed) to existing controls/outputs: Thermal inputs/buttons `ThermalMode` (`ComboBox`), `ThermalSupplyTemperature` (`Edit`), `ThermalGroundTemperature` (`Edit`), `ThermalPipe` (`ComboBox`), `ThermalPipeSpacing` (`ComboBox`), `ThermalCalculate` (`Button`), `ThermalReset` (`Button`); Thermal outputs `ThermalRecalcMessage`, `ThermalDeltaT`, `ThermalPowerTotal`, `ThermalResultStatus` (`Text`); Hydraulics outputs `HydraulicsPipeSpacing`, `HydraulicsSupplyTemperature`, `HydraulicsReturnTemperature` (`Text`); Results outputs `ResultsThermalPower`, `ResultsSupplyTemperature`, `ResultsReturnTemperature` (`Text`). The harness resolves each by exact AutomationId plus expected `ControlType`; sidebar items remain selected by `ControlType.ListItem` plus rendered names `Тепловой расчёт`, `Гидравлический расчёт`, and `Результаты`. Every selector must match exactly one enabled element; missing/ambiguous matches exit nonzero. Numeric output comparison parses the UIA `Name` using `ru-RU`, strips displayed units, and compares to the canonical observation rounded to the exact XAML `StringFormat`; the harness manifest records each binding and format. The exact recalculation oracle for a supply edit is `Температура подачи изменена. Требуется пересчёт.` and is compared exactly. Unexpected dialogs are identified by exact window title and `ControlType.Window`; Todo 2 characterizes the dialog buttons' actual localized accessible names and default/cancel semantics. The harness may dismiss an unexpected dialog only by selecting the unique enabled `ControlType.Button` proven by that characterization to be the cancel action, records its observed accessible name, and fails on zero/multiple matches—no hard-coded localized button label is allowed. These accessibility attributes do not change layout, style, formulas, bindings, command behavior, or public product semantics and are the only XAML changes authorized by this plan.

The harness never invokes `dotnet run` or any build. It validates the exact executable path and SHA-256 against `frozen-release-sha256.json` before and after every process run; launches that `.exe` directly with the `.smc` path as its first argument through `Start-Process -PassThru -RedirectStandardOutput <run-owned-stdout.log> -RedirectStandardError <run-owned-stderr.log>`; waits for process exit; records process ID/exit code and SHA-256 of both logs; and rejects a nonzero exit or any stderr line matching unhandled-exception/fatal-crash patterns characterized in Todo 2. Every happy, reload, second-load, reset and unknown-pipe run has distinct task-owned stdout/stderr filenames under its output directory. Todo 12 creates `frozen-release-sha256.json` after V1–V6, containing the SHA-256 of the executable, product DLL, test DLL and plan; Todo 13 and F3 consume but never rewrite it.

The harness performs these ten numbered steps without mouse coordinates: (1) verify `fixture-manifest.json` and all three input SHA-256 values before process launch; (2) start Project A as the first `.smc` command-line argument and wait for the main window; (3) navigate to Thermal and record baseline mode/supply/ground/pipe/spacing/result text; (4) select a mode different from baseline through `SelectionItemPattern`, assert exactly `Режим работы изменён. Требуется пересчёт.` and that the prior result remains; then change supply through `ValuePattern`, assert exactly `Температура подачи изменена. Требуется пересчёт.` and that the prior result remains; then change ground/pipe/spacing through their stated patterns, verify all 17 AutomationIds are unique and expose the required control types/values, and capture `01-edit.png`; (5) invoke `Рассчитать`, wait until calculating state clears, assert the recalculation message is absent and result text differs from the step-3 baseline, and capture `02-calculate.png`; (6) select the `Гидравлический расчёт` and `Результаты` sidebar items, record displayed Thermal/spacing projections through the six named downstream output AutomationIds, and capture `03-hydraulics.png` and `04-results.png`; (7) send `Ctrl+S` to loaded task-owned Project A, require its timestamp/SHA to advance and the main-window title to lose its leading `*` dirty marker, then close and require no `Закрытие приложения` dialog; relaunch that exact path and assert edited mode/inputs/result restore; if the dirty marker remains or a closing dialog appears, cancel it through the `Cancel` button (`ControlType.Button`, exact accessible name `Cancel`) and fail the harness rather than choosing Yes/No; (8) close the clean Project A instance with no closing dialog, relaunch Project B as startup argument, assert supply `55.0`, ground `5.0`, spacing `150`, pipe `RAUTHERM S 17x2,0`, and no Project A result, then capture `05-load-2.png`; (9) while Project B is still clean, send `Ctrl+N`; require no `Создать новый расчёт` dialog, navigate back to Thermal, assert canonical defaults/status from DEC-T01, require the window title has no leading `*`, and capture `06-reset.png`; an unexpected dialog is cancelled through its exact `Cancel` button and fails the harness; (10) close the clean reset process, require no `Закрытие приложения` dialog and a normal exit with stderr free of unhandled exceptions, verify all six screenshots are non-empty, and emit `observations.json` containing each step, all 17 selectors, expected/actual values, process ID/exit, fixture SHA-256 and screenshot SHA-256 plus `task-13-user-flow-qa.md`.

After the happy flow, the same V9 invocation runs a separate failure branch: launch `unknown-pipe.smc`, assert the exact fallback pipe/message/result/status frozen by Todo 9's `RestoreFailure`/unknown-pipe characterization, assert the restore guard is cleared indirectly by successfully editing supply and observing the canonical recalculation message, then send `Ctrl+S`, require the task-owned file SHA/timestamp to advance and the title dirty marker to clear before closing. Require no `Закрытие приложения` dialog and a normal exit; if a closing dialog appears, dismiss it only through the unique characterized cancel-action button described above and fail. Write `failure-observations.json`, distinct stdout/stderr logs and `07-unknown-pipe.png`. Any unhandled or unexpected dialog, selector ambiguity, timeout, process crash, missing artifact, dirty-marker persistence, or stderr exception exits nonzero; there is no manual/degraded acceptance path.

**Widget browser contract (exact agent-executable browser tool):** use the loaded `playwright` skill MCP, not a repository package or an unspecified harness. Construct the `file:///` URI from the verified repository root plus `docs/architecture-migration/architecture-widget.html`. Then for each ID in `compile-time`, `di-runtime`, `state-ownership`, `reactive`, `persistence`, `user-flow`, invoke `browser_click` with selector `[data-view="<ID>"] button`, invoke `browser_evaluate` to assert that this button has `aria-pressed="true"`, `[data-field="state-kind"]` is neither empty nor `error`, and `[data-result-rows] tr` has a positive count; invoke `browser_take_screenshot` (`scale: "css"`, `fullPage: true`) to the owner directory inside Phase 4 evidence: Todo 14 uses `task-14/browser/phase-4-widget-<ID>.png`, F1 uses `final/f1/browser/f1-phase-4-widget-<ID>.png`, and F4 uses `final/f4/browser/f4-phase-4-widget-<ID>.png`. Finally require zero console errors and close. A path outside the repository root, missing selector, zero rows, error state, console error, path outside owner evidence, or missing screenshot is a hard failure.

Todo 1 creates test-only `parse-trx.ps1`; it reads XML and emits exact `test-case-name/outcome` JSON and rejects missing input, zero tests, duplicate identities and malformed XML. Todo 12 creates `assert-trx-identities.ps1`; it accepts exactly one TRX, one immutable expected-identity manifest and one group, then rejects non-Passed outcomes, absent expected identities, unexpected identities, duplicates and an empty group. Todo 2 writes `expected-negative-test-identities.json` from the exact fully-qualified names of the characterized `CalculationFailure`, `PersistenceFailure`, and `RestoreFailure` cases, with each group non-empty and disjoint. Each script is locked by creator-owned fixtures before first use. V12-F3 reconciles each negative TRX before any UI QA, so the full-suite TRX cannot satisfy a filtered lane.

**Frozen Release verifier contract:** Todo 12 creates `verify-frozen-release.ps1` beside `frozen-release-sha256.json`. The manifest has exactly four canonical workspace-relative keys: `executable`, `productDll`, `testDll`, and `plan`, each with path and uppercase SHA-256. The verifier rejects extra/missing keys, missing/non-regular files, path escape, duplicate resolved paths, or any hash mismatch; on success it writes a lane-owned JSON receipt containing manifest SHA-256 plus the same four resolved paths/hashes. F1, F2, F3 and F4 each invoke V13 immediately before and after their own commands, writing `frozen-hashes-before.json` and `frozen-hashes-after.json` inside that lane's directory, and require byte-identical four-hash sets. Their Markdown receipts must echo all four hashes and the manifest hash. F4 uses Todo 12's `verify-final-receipts.ps1` to reject a missing receipt, differing before/after values, non-APPROVE domain verdict, wrong `SUBJECT`, altered artifact hash or any cross-lane mismatch before consolidation. No final lane rewrites the manifest or frozen binaries.

## Execution strategy

Repository-wide legacy-store guards do not exist and are not referenced by command ID in Todos 1-10. Todo 11 creates the guard suite and defines its first legal command; earlier todos use only task-local tests within their existing allow-lists.

### Dependency matrix

| Wave | Todos | Parallelism | Gate |
|---|---|---|---|
| 1 — Baseline and contracts | 1 → 2 → 3 | Sequential central lane | Fresh protected baseline; complete behavior matrix; immutable state tests green |
| 2 — Owner and adapters | 4 → 5 → 6 | Sequential central lane | One ProjectSession owner; status/spacing adapter; Thermal VM adapter green |
| 3 — Reactive/lifecycle/persistence | 7 → 8 → 9 → 10 | Sequential central lane | Phase 3.1, Hydraulics, restore and `.smc` contracts green |
| 4 — Closure | 11 → 12 → 13 → 14 | Sequential; Todo 13 agent QA only after stable code | Ownership guards, builds/tests, user flow, dossier green |
| Final | F1 → F2 → F3 → F4 | Read-only sequential after all todos | F1–F3 must APPROVE, then F4 consolidates without override authority; any correction invalidates the entire chain |

Final verification is deliberately sequential because the test and WPF suites have not proven process-level isolation: F1 owns `final/f1/`, F2 owns `final/f2/`, F3 owns `final/f3/`, and F4 reads the three immutable domain receipts and owns `final/consolidated/`. F1 is the **Conformance / Scope / Provenance** domain and includes six-view/model/widget/workflow fidelity; F2 is the **Architecture / Code Quality** domain; F3 is the **Executable QA / User Risk** domain. No lane rebuilds `bin/obj`, regenerates canonical fixtures, or writes another lane's paths. Each domain lane verifies the frozen executable/product/test DLL/plan hashes before and after its commands; mismatch rejects the entire chain. A correction reruns Todo 12 freeze and the whole F1→F4 sequence.

The exact dependency graph is `1 -> 2 -> 3 -> 4 -> 5 -> 6 -> 7 -> 8 -> 9 -> 10 -> 11 -> 12 -> 13 -> 14 -> F1 -> F2 -> F3 -> F4`. Todo 1 internally orders `identity/capture -> four builds -> two real TRX -> protected comparison -> unlock sentinel`. No node starts unless every predecessor is green. Any correction invalidates every downstream receipt; any correction after Todo 12 invalidates the frozen manifest and all final receipts. F4 cannot bypass F1/F2/F3 or transition to owner acceptance itself. The Todo 1 plan-structure verifier parses this graph, rejects missing/duplicate/cyclic/forward dependencies and resolves every Todo/F/V/generated-artifact reference before execution.

Exact parameter substitution matrix: Todo 2 invokes V3 with `<OWNER>=task-2`; Todo 7 invokes V3 with `<OWNER>=task-7`; Todo 8 invokes V4 with `task-8`; Todo 10 invokes V5 with `task-10`; Todo 11 invokes V2 with `task-11`; Todo 12 invokes V2, V3, V4, V5 and V6 with `task-12`. No other `<OWNER>` value is valid. Todo 13 alone invokes V9 with `task-13/fixtures` and `task-13/ui-qa`; F3 uses the separately literal `final/f3/*` commands. The structural verifier requires every placeholder occurrence to resolve through this matrix.

Workflow state matrix: this planning materialization plus the single terminal planning `APPROVE` sets `stage=awaiting-owner-approval`, `planning=approved`, all owner gates/final domains pending, `stop=true`, and next action `/architecture-approve phase-4-thermal-state`. `/architecture-approve` records only plan approval and prepares the separate `/architecture-start` gate; `/architecture-start` alone records execution authorization and sets `stage=executing`. Todo 14 may update technical evidence while remaining `executing`; it must not set `awaiting-owner-acceptance`. After F1-F3 APPROVE and F4 consolidated APPROVE, the canonical workflow transitions to `stage=awaiting-owner-acceptance`, with result acceptance pending and `stop=true`. Only explicit owner result acceptance sets `completed`; no phase auto-starts.

### Commit discipline

Commit lines below are guidance only for a separately authorized isolated worker. Each commit pairs behavior with tests, stages only the task allow-list, never includes unrelated baseline paths and never pushes/amends without explicit owner request.

## Todos

- [ ] 1. Capture protected Phase 4 baseline, tools, plan identity and dirty preimages
  - **Depends on:** exact reviewed Phase 4 plan approval and separate architecture execution authorization; current `STATE.json`/Boulder identity must pass the repository start gate.
  - **Allow-list:** new files under `docs/architecture-migration/evidence/phase-4-thermal-state/`; generated Debug/Release `bin/obj` and Todo 1-owned TestResults. Canonical workflow/control files remain read-only in this todo.
  - **References:** `docs/architecture-migration/AGENTS.md`; `STATE.json`; the historical 281-entry dirty audit is superseded — the tree is clean at `master` and Todo 1 captures the fresh clean baseline.
  - **Action:** create `capture-baseline.ps1`, `verify-protected-baseline.ps1`, `verify-plan-structure.ps1` and `parse-trx.ps1` before first use. Record Git root/HEAD/branch/upstream, `dotnet --info`, `node --version`, binary `git status --porcelain=v1 -z --branch`, staged/unstaged/untracked NUL-safe sets, baseline builds/tests and exact accepted `NotExecuted` identities. Store the clean NUL-safe status capture, create `todo-1-allowed-hunks.json` as an empty manifest, define protected/task-owned/generated sets, and create immutable `todo-1-completion.json` only after every Todo 1 gate passes.
  - **Acceptance:** `capture-baseline.ps1` emits deterministic NUL-safe path/hash manifests; `verify-protected-baseline.ps1` accepts `-Baseline`, `-AllowedHunks`, `-EvidenceRoot`, `-Output`, resolves every path beneath the verified root, performs a symmetric pre/post comparison, and exits nonzero on missing/malformed/duplicate/escaping paths or unexpected drift; output contains `protected_mismatch_count`, `allowed_hunk_count`, and exact changed paths. `verify-plan-structure.ps1` parses column-zero task rows and requires exact ordered unique IDs `1..14` and `F1..F4`, rejects zero matches, gaps, duplicates, out-of-order/malformed/nested IDs, any fifth final identifier, unresolved Todo/F/V references, any generated asset without one earlier creator, and any command output outside its owner. Fresh state/plan identity passes; staged/protected sets are unchanged outside authorized evidence/generated outputs and `allowed_hunk_count=0`.
  - **QA — happy:** run V0; run the capture and protected verifier twice without source edits; run `verify-plan-structure.ps1 -Plan "docs/architecture-migration/plans/phase-4-thermal-state.md" -Output "docs/architecture-migration/evidence/phase-4-thermal-state/task-1/plan-structure.json"`; run production/test Debug+Release builds and full Debug+Release tests into distinct Todo 1 evidence TestResults directories, parse both real TRX files, and require identical expected identities/outcomes. The workflow must have materialized this exact candidate byte-identically at the canonical path and mirror and bound its SHA before execution; absence or digest drift blocks Todo 1. Save raw receipts and `todo-1-completion.json` with root, branch, HEAD/base, four build results, both TRX paths/hashes, protected mismatch count, allowed hunk count (expected 0) and `todo2_unlocked=true`.
  - **QA — failure:** run isolated fixtures for spaces/non-ASCII/NUL boundaries, missing/malformed/duplicate/path-escape baseline rows, unexpected protected drift, zero/missing/stale/duplicate TRX, and malformed plans containing zero task rows, duplicate/gapped/out-of-order IDs, Todo 15, a fifth final row, an unresolved V reference, an unowned script and an output outside its owner. Every fixture must exit nonzero without touching canonical/source files. Any real HEAD/staged/protected/build/TRX drift writes `todo2_unlocked=false`, blocks Todo 2 and is recorded, never reset.
  - **Commit:** `test(architecture): capture phase 4 thermal baseline`.

### Superseded baseline correction (provenance only)

Historical provenance: original plan identity `93512` bytes / SHA-256 `C238E2876B43F8D606F24DF9682CAFD41F9B71C6C2DDDF965814C99EEC8AD451`, its amendment receipt, and the rejected retry `4311E0A9BC9CCF7B678C1A3E99EAC99B9D935E9A9199C48B3A4CA80B28C8943A`. Their prescribed constructor-argument corrections at three `ResultsViewModel` call sites (`ConstructionServiceTests.cs` twice, `DialogServiceThreadAffinityTests.cs` once) were diagnosed against provenance base `e655735dfa66c00cf9c53be93d511eda8989e8bf`. Fresh verification against the current `master` (commit `0ed4ef2`, 2026-08-22; re-verified at planning HEAD `6a5a96f` with no later `src/` or `tests/` commits) proves every required argument already present at every cited call site: `projectStateService.Session,`/`_projectStateService.Session,` and `new HydraulicSummaryBuilder()`. The corrections are therefore already applied in the execution base; this section no longer gates Todo 2, and this plan authorizes no test-source correction. Any newly discovered pre-existing compile or test defect at the Todo 1 baseline boundary blocks Todo 2, is recorded, and requires owner re-planning.

- [ ] 2. Lock Thermal writers, subscribers, calculations, lifecycle and persistence behavior before ownership edits
  - **Depends on:** Todo 1 `todo-1-completion.json` with `todo2_unlocked=true` and every build/test/TRX/identity/protected gate green; otherwise Todo 2 remains forbidden.
  - **Allow-list:** new `tests/SnowMeltingCalculator.Tests/Services/Project/ThermalMultiplicityCharacterizationTests.cs`; existing `tests/SnowMeltingCalculator.Tests/Thermal/ThermalViewModelTests.cs`, `tests/SnowMeltingCalculator.Tests/Services/Navigation/CalculationStateServiceTests.cs`, `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectLifecycleFlowCharacterizationTests.cs`, `tests/SnowMeltingCalculator.Tests/Services/Project/ClimateThermalInvalidationRegressionTests.cs`, `tests/SnowMeltingCalculator.Tests/Services/Project/ConstructionThermalInvalidationRegressionTests.cs`, `tests/SnowMeltingCalculator.Tests/ViewModels/ResultsViewModelOpenProjectTests.cs`, `tests/SnowMeltingCalculator.Tests/Project/ProjectRoundTripTests.cs`, `tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/ThermalToHydraulicsIntegrationTests.cs`, `tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/PipeSpacingSynchronizationTests.cs`, `tests/SnowMeltingCalculator.Tests/IntegrationTests/Hydraulics/DoubleCalculationPreventionTests.cs`, and Phase 4 evidence only; no production code. If characterization requires another test-source path, stop and re-plan rather than widening this list.
  - **References:** `ThermalViewModel` inputs/calculate/reset/invalidation; `CalculationStateService` Thermal/spacing fields; `CalculationContext.UpdateThermal*`; `ProjectLoadOrchestrator.RestoreModulesFromProjectAsync`; `ResultsViewModel.SaveCurrentProject`; Phase 3.1 regression tests; `docs/architecture-migration/maps/state-inventory.md` rows ST-012..ST-015 and ST-021..ST-022.
  - **Action:** create `ThermalMultiplicityCharacterizationTests`, a production-writer/subscriber inventory, and `expected-negative-test-identities.json` containing the exact non-empty, disjoint fully-qualified test-name groups `CalculationFailure`, `PersistenceFailure`, and `RestoreFailure`. Measure every DEC-T03..T08 scenario: each input changed/no-op with result present/absent; pipe structural equality; spacing changed/no-op; user/lifecycle reset; Climate/Construction user and lifecycle invalidation; valid/invalid/exception/reentrant calculation; valid/absent/invalid saved result; pipe match/fallback/null; missing spacing; second load; repeated load/reset; restore failure.
  - **Acceptance:** every scenario records final values, canonical/legacy candidate events, dirty-intent count separately from observable dirty transitions, `StateChanged`, `PipeSpacingChanged`, context input/result order, calculator count, Hydraulics calculation count, Results refresh count and subscription count. Each negative manifest group contains at least one exact test identity, all listed tests exist, and groups have no duplicate identity. Existing behavior is frozen where DEC-T01..T08 require preservation; any contradiction becomes a blocker before Todo 3.
  - **QA — happy:** run `dotnet test "tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj" -c Debug --filter "FullyQualifiedName~ThermalMultiplicityCharacterizationTests" --logger "trx;LogFileName=phase-4-characterization-debug.trx" --results-directory "docs/architecture-migration/evidence/phase-4-thermal-state/task-2/TestResults"` and V3 with V3 output rebound to `task-2/TestResults`; expect exit 0, failed 0, every matrix case executed, and emit `task-2-thermal-characterization.md`.
  - **QA — failure:** task-local cases in `ThermalMultiplicityCharacterizationTests` model one synthetic direct writer and duplicate subscriber and pass only by asserting both violations are rejected. No production file is mutated; repository-wide guard execution is unavailable before Todo 11.
  - **Commit:** `test(thermal): characterize ownership and multiplicity`.

- [ ] 3. Add immutable Thermal state contract, structural equality and direct state tests
  - **Depends on:** Todo 2 green and no unresolved contradiction.
  - **Allow-list:** `IProjectSession.cs`; new Thermal contract/implementation files under `src/Services/Project/`; direct state tests; Phase 4 evidence. Do not wire runtime consumers yet.
  - **References:** DEC-T01/T02; Climate/Construction state contract patterns; `PipeType.cs`; `ThermalCalculationResult.cs`; exact defaults from `ThermalViewModel.Reset`.
  - **Action:** implement snapshots, origins, statuses/results, closed mutation API, candidate validation, deep defensive copies and field-by-field equality. Implement own-input/status/result/upstream/lifecycle semantics without context/dirty/compatibility wiring. No general result setter and no shared mutable backing references.
  - **Acceptance:** state tests prove exact defaults; independent equal snapshots no-op; each changed field changes structurally; pipe/result ingress and egress cannot mutate owner; rejected atomicity; one completion for changed and zero for no-op/rejected; every origin is exhaustively handled.
  - **QA — happy:** run `dotnet test "tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj" -c Debug --filter "FullyQualifiedName~ProjectSessionThermalStateTests" --logger "trx;LogFileName=phase-4-state-debug.trx" --results-directory "docs/architecture-migration/evidence/phase-4-thermal-state/task-3/TestResults"`; expect exit 0/failed 0 and all mutation/origin cases executed. Evidence: `task-3-thermal-state-contract.md`.
  - **QA — failure:** the same suite includes `TestCategory=DefensiveCopy|RejectedCandidate`; run that filter and expect tests to prove original/returned mutable objects cannot change state and invalid candidates emit zero events.
  - **Commit:** `feat(project): add canonical thermal state contract`.

- [ ] 4. Attach exactly one ThermalState to ProjectSession and prove runtime DI identity
  - **Depends on:** Todo 3.
  - **Allow-list:** `IProjectSession.cs`, `ProjectSession.cs`, `ServiceCollectionExtensions.cs`, DI/ProjectSession tests, Phase 4 evidence.
  - **References:** existing `ClimateState`/`ConstructionState` creation and interface exposure; singleton registrations for `IProjectSession`, state/dirty services and module VMs.
  - **Action:** instantiate/expose one state from `ProjectSession`; prove all `IProjectSession`/legacy lifecycle interface resolutions return the same owning session and that ThermalState is not independently registered. Do not modify VM, orchestrator, Results or compatibility consumers yet; their constructor/adapter identity proofs are deferred respectively to Todos 5, 6, 9 and 10, with aggregate closure in Todo 11. Do not introduce a service locator/cycle workaround.
  - **Acceptance:** DI/ProjectSession tests prove one owning session and one reference-identical ThermalState through every session interface, no independent/transient state registration or circular construction, and unchanged Climate/Construction identities. No assertion in this todo requires an out-of-allow-list consumer edit.
  - **QA — happy:** run `dotnet test "tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj" -c Debug --filter "FullyQualifiedName~DiRegistrationTests&TestCategory=ThermalState" --logger "trx;LogFileName=phase-4-di-debug.trx" --results-directory "docs/architecture-migration/evidence/phase-4-thermal-state/task-4/TestResults"`; expect exit 0 and reference-identity/disposal cases executed. Evidence: `task-4-project-session-di.md`.
  - **QA — failure:** task-local negative cases in the Todo 4 DI/ProjectSession test allow-list model duplicate registration and a constructor cycle; they pass only if the tests reject the duplicate/cycle without service location. Repository-wide guard execution is unavailable before Todo 11.
  - **Commit:** `feat(project): expose thermal state from session`.

- [ ] 5. Convert CalculationStateService Thermal status and spacing into a compatibility adapter
  - **Depends on:** Todo 4.
  - **Allow-list:** `ICalculationStateService.cs`, `CalculationStateService.cs`, direct service/state/spacing tests, minimal test helpers, Phase 4 evidence. Hydraulics backing fields stay untouched.
  - **References:** DEC-T03/T05/T06/T07; `_thermal*` and `_pipeSpacing` current fields/events; Construction/Hydraulics spacing subscribers.
  - **Action:** delegate Thermal phase/messages/spacing to canonical state, translate canonical completions into existing `StateChanged`/`PipeSpacingChanged`, remove Thermal/spacing backing stores and string-based writer authority. Preserve Hydraulics status and restore lease. Keep any parameterless test compatibility path internally consistent and isolated.
  - **Acceptance:** compatibility API values match canonical snapshots; changed/no-op spacing and status events match Todo 2 counts; no duplicate event on adapter refresh; no writable Thermal/spacing store remains in service; existing Hydraulics state tests remain green.
  - **QA — happy:** run `dotnet test "tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj" -c Debug --filter "FullyQualifiedName~CalculationStateServiceTests|FullyQualifiedName~ThermalMultiplicityCharacterizationTests" --logger "trx;LogFileName=phase-4-status-spacing-debug.trx" --results-directory "docs/architecture-migration/evidence/phase-4-thermal-state/task-5/TestResults"`; expect exit 0/failed 0 and exact status/spacing event counts. Evidence: `task-5-calculation-state-adapter.md`.
  - **QA — failure:** task-local negative cases in the Todo 5 direct service/state/spacing test allow-list exercise noncanonical writes and duplicate completions; they pass only when direct writes are rejected/routed and event counts remain at most one. Repository-wide guard execution is unavailable before Todo 11.
  - **Commit:** `refactor(navigation): adapt thermal status to session state`.

- [ ] 6. Make ThermalViewModel a canonical-state adapter and move calculation orchestration out of writable UI fields
  - **Depends on:** Todo 5.
  - **Allow-list:** `ThermalViewModel.cs`; new `src/Services/Project/IThermalStateCoordinator.cs` and `src/Services/Project/ThermalStateCoordinator.cs`; the exact coordinator registration/composition seam in `src/Configuration/ServiceCollectionExtensions.cs`; `src/Views/Thermal/ThermalView.xaml`, `src/Views/Hydraulics/CircuitsView.xaml`, and `src/Views/Results/ResultsView.xaml` only for the exact 17 accessibility-only AutomationIds of the Todo 6 accessibility contract; direct Thermal VM/coordinator/calculation/DI and selector-contract tests; minimal constructor helpers; Phase 4 evidence.
  - **References:** DEC-T01..T05 and DEC-T04A; existing binding properties/commands; `ThermalView.xaml` controls bound to those properties/commands; `IThermalCalculator`; validators; Climate/Construction read projections; existing singleton composition in `ServiceCollectionExtensions.cs`.
  - **Action:** create/register the exact singleton coordinator contract from DEC-T04A and inject it into the singleton Thermal VM so it is eagerly materialized. Preserve public WPF bindings/commands while routing every input/reset action through the coordinator/state. Build calculator inputs from canonical Thermal inputs plus current Climate/Construction projections. Implement exact validation/calculation/failure sequence; move dirty intent and context/status ownership to the coordinator; subscribe the VM once only to canonical Thermal completion for binding refresh without recursive writes. Add only the exact 17 AutomationIds of the Todo 6 accessibility contract to already-bound input/output elements in the three named XAML files — Thermal inputs/buttons `ThermalMode` (`ComboBox`), `ThermalSupplyTemperature` (`Edit`), `ThermalGroundTemperature` (`Edit`), `ThermalPipe` (`ComboBox`), `ThermalPipeSpacing` (`ComboBox`), `ThermalCalculate` (`Button`), `ThermalReset` (`Button`); Thermal outputs `ThermalRecalcMessage`, `ThermalDeltaT`, `ThermalPowerTotal`, `ThermalResultStatus` (`Text`); Hydraulics outputs `HydraulicsPipeSpacing`, `HydraulicsSupplyTemperature`, `HydraulicsReturnTemperature` (`Text`); Results outputs `ResultsThermalPower`, `ResultsSupplyTemperature`, `ResultsReturnTemperature` (`Text`) — and do not alter layout/style/bindings/formatting. This Todo 6 accessibility contract is the single source of truth for later desktop UI QA selectors; no later command renames, extends, or diverges from it. Do not add coordinator Climate/Construction subscriptions until Todo 7 performs the atomic subscriber handoff.
  - **Acceptance:** VM has no writable canonical backing state and no direct `IMarkDirtyService`, status-store or context writer calls outside approved coordinator; all existing VM tests and new calculation matrix pass; one logical command yields one calculation/completion; every AutomationId of the Todo 6 accessibility contract occurs exactly once on the stated WPF control type, every output retains its prior binding and `StringFormat`, and no other XAML/UI behavior changes.
  - **QA — happy:** run `dotnet test "tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj" -c Debug --filter "FullyQualifiedName~ThermalViewModelTests|FullyQualifiedName~ThermalMultiplicityCharacterizationTests" --logger "trx;LogFileName=phase-4-vm-debug.trx" --results-directory "docs/architecture-migration/evidence/phase-4-thermal-state/task-6/TestResults"`; expect exit 0/failed 0 and each DEC-T05 row executed. Evidence: `task-6-thermal-viewmodel-adapter.md`.
  - **QA — failure:** run the same Todo 6 task-local suites with `TestCategory=CalculationFailure`; invalid input/result, exception, reentrancy and adapter-refresh recursion cases must execute and pass exact count/message assertions. A test-only XAML fixture within the Todo 6 selector-contract test allow-list with one missing or duplicate required AutomationId must make that task-local guard reject it while canonical XAML passes. Repository-wide guard execution is unavailable before Todo 11.
  - **Commit:** `refactor(thermal): make view model a state adapter`.

- [ ] 7. Move Climate and Construction invalidation to one canonical Thermal subscriber
  - **Depends on:** Todo 6.
  - **Allow-list:** `src/Services/Project/ThermalStateCoordinator.cs` for the exact two upstream subscriptions/disposal paths; `ThermalViewModel.cs` only to remove its legacy upstream subscriptions/handlers; coordinator/VM and directly affected Climate/Construction/Thermal regression tests; Phase 4 evidence. Do not alter accepted Climate/Construction publication-source contracts or DI lifetime.
  - **References:** DEC-T04 and DEC-T04A; `ClimateThermalInvalidationRegressionTests`; `ConstructionThermalInvalidationRegressionTests`; Phase 3.1 plan/evidence.
  - **Action:** add the sole ClimateState/ConstructionState completion subscriptions to the already-registered singleton `ThermalStateCoordinator` and remove the legacy VM subscriptions/handlers in the same green task. Coordinator disposal unsubscribes both exactly once. Map origin-aware completions to clear/result/status semantics; lifecycle/no-op/rejected paths remain silent. Never add load-guard heuristics or another subscriber type.
  - **Acceptance:** valid restored result survives load; genuine user Climate/Construction change clears/invalidate exactly once; no-op/lifecycle zero; dirty not duplicated; repeated load/reset does not increase subscriptions. At no committed boundary are both old and new subscribers active.
  - **QA — happy:** run V1 then V3 and parse its TRX; V1 recompiles all production/test changes before V3's `--no-build`; expect exit 0, failed 0 and exact Phase 3/3.1 count identities at `task-7-upstream-invalidation.md`.
  - **QA — failure:** V3 includes changed `ProjectLoadReset` zero-event cases; a task-local duplicate-subscriber case in the Todo 7 coordinator/VM or directly affected regression test allow-list passes only by proving multiplicity is detected. Repository-wide guard execution is unavailable before Todo 11.
  - **Commit:** `refactor(thermal): centralize upstream invalidation`.

- [ ] 8. Publish one Thermal projection through CalculationContext and preserve Hydraulics/spacing consumer counts
  - **Depends on:** Todo 7.
  - **Allow-list:** Thermal-only seam in `CalculationContext.cs`, Thermal/spacing consumer seam in `CircuitsViewModel.cs`, related integration/authority tests, Phase 4 evidence. No Hydraulics owner or formula changes.
  - **References:** DEC-T05..T07; `OnCalculationContextChanged`; `OnPipeSpacingChanged`; `ThermalToHydraulicsIntegrationTests`; `PipeSpacingSynchronizationTests`; `DoubleCalculationPreventionTests`; `CalculationContextWriterAuthorityTests`.
  - **Action:** route context input/result publication through the approved state/application boundary; remove legacy bypass writers. Preserve notification-only input behavior, valid-result one-calculation behavior, invalid/null zero-calculation behavior and exact spacing propagation without recursion.
  - **Acceptance:** writer guard finds one approved projection writer; changed spacing creates one compatibility event and one logical Hydraulics calculation; no-op zero; valid result one; invalid/null zero; no own-source recursion or duplicate collector calculation. Hydraulics canonical ownership remains unchanged.
  - **QA — happy:** run V1 then V4 and parse its TRX; V1 recompiles all production/test changes before V4's `--no-build`; expect exit 0, failed 0 and exact call-order/count evidence at `task-8-context-hydraulics.md`.
  - **QA — failure:** V4's invalid/null/no-op task-local cases assert calculator zero; a task-local second-writer case in the Todo 8 related integration/authority test allow-list passes only if the authority test rejects it. Repository-wide guard execution is unavailable before Todo 11.
  - **Commit:** `refactor(thermal): unify context and hydraulics projection`.

- [ ] 9. Route lifecycle reset, project restore and fallback calculation through ThermalState
  - **Depends on:** Todo 8.
  - **Allow-list:** new `src/Services/Project/ThermalPersistenceMapper.cs`; `src/Services/Project/ProjectLoadOrchestrator.cs`; `src/ViewModels/Shell/MainViewModel.cs` only for its `PerformNewCalculationReset` Thermal reset call; new `tests/SnowMeltingCalculator.Tests/Services/Project/ThermalPersistenceMapperTests.cs`; existing `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectLifecycleFlowCharacterizationTests.cs`, `tests/SnowMeltingCalculator.Tests/Services/Project/ClimateThermalInvalidationRegressionTests.cs`, and `tests/SnowMeltingCalculator.Tests/ViewModels/ResultsViewModelOpenProjectTests.cs`; Phase 4 evidence. If fresh characterization proves another production or test path is required, stop and re-plan rather than widening this list.
  - **References:** DEC-T08 matrix; `ResultsViewModel.LoadProjectDataAsync`; `ProjectLoadOrchestrator.RestoreModulesFromProjectAsync`; existing restore lease and Phase 3.1 tests.
  - **Action:** create the pure mapper's DTO-to-canonical candidate half and use it for lifecycle restore; lifecycle reset canonical defaults with non-user origin; preserve pipe match/fallback/null and spacing 200; execute fallback exactly once for absent/invalid saved result; ensure Hydraulics/circuit result restore order remains unchanged. Do not create a temporary inline mapper.
  - **Acceptance:** every DEC-T08 row passes; second load replaces all project-A Thermal state/status; repeated reset/load does not multiply subscriptions/events/calculations; successful load ends clean; restore lease clears on exception; characterized partial failure remains without new transaction semantics.
  - **QA — happy:** run V1, then `dotnet test "tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj" -c Release --no-build --filter "FullyQualifiedName~ProjectLifecycleFlowCharacterizationTests|FullyQualifiedName~ClimateThermalInvalidationRegressionTests|FullyQualifiedName~ResultsViewModelOpenProjectTests" --logger "trx;LogFileName=phase-4-lifecycle.trx" --results-directory "docs/architecture-migration/evidence/phase-4-thermal-state/task-9/TestResults"`; V1 recompiles all Todo 9 changes before `--no-build`; expect exit 0/failed 0 and all DEC-T08 rows executed. Save `task-9-lifecycle-restore.md`.
  - **QA — failure:** the same suite's `TestCategory=RestoreFailure` fixtures inject mapper/restore exceptions; expect lease false at end and exact frozen partial snapshot.
  - **Commit:** `refactor(project): restore thermal through session state`.

- [ ] 10. Complete Thermal persistence mapping and make Results save/read canonical projections
  - **Depends on:** Todo 9.
  - **Allow-list:** `ThermalPersistenceMapper` created in Todo 9; Thermal save/read seams in `ResultsViewModel.cs`; persistence/round-trip/Results tests; Phase 4 evidence. DTO definitions may be read but not changed.
  - **References:** DEC-T08 exact wire fields; `ProjectData.cs` DTOs; `ProjectRoundTripTests`; `ResultsViewModelOpenProjectTests`; `docs/architecture-migration/maps/persistence-compatibility.md` rows PP-035..PP-052.
  - **Action:** add the canonical snapshot-to-DTO half to the mapper whose restore half was created in Todo 9; save reads only state snapshot; Results Thermal projection reads state/current projection rather than writable VM/service caches. Preserve PDF/report/export behavior and version/name/options exactly.
  - **Acceptance:** v1.0/v1.1, missing spacing, pipe match/fallback/null, valid/absent/invalid result and save/reload semantic equality pass; serialized property/version set has no additions/removals; status/origin/runtime-only fields absent; Results remains derived and no calculation is triggered merely by save/export.
  - **QA — happy:** run V1 then V5 and a mapper JSON-property assertion in `ThermalPersistenceMapperTests`; V1 recompiles all Todo 10 changes before V5's `--no-build`; expect exit 0/failed 0, exact unchanged property set/version and semantic round-trip. Evidence: `task-10-persistence-results.md`.
  - **QA — failure:** V5's `TestCategory=PersistenceFailure` cases cover unknown pipe, corrupt/missing result and failed file operation; expect frozen fallback/error state, no project-A result and no schema drift.
  - **Commit:** `refactor(results): persist thermal session snapshot`.

- [ ] 11. Enforce final sole-owner, immutable projection, subscription and DI guards
  - **Depends on:** Todo 10.
  - **Allow-list:** new `tests/SnowMeltingCalculator.Tests/Services/Project/ThermalStateLegacyStoreGuardTests.cs`; `tests/SnowMeltingCalculator.Tests/Configuration/DiRegistrationTests.cs`; Phase 4 evidence. This todo is guard-only: if it detects a remaining production writer/subscriber or DI defect after Todos 3-10, stop and return the defect to the owning earlier todo (or re-plan if no earlier allow-list covers it); do not perform unnamed cleanup here.
  - **References:** ST-012..ST-015/ST-021..ST-022; all production writers inventoried in Todo 2; final target DEC-T01..T08.
  - **Action:** create `ThermalStateLegacyStoreGuardTests` and, only now, define V11 as its exact Release `dotnet test --no-build` command with `TestCategory=NegativeFixture` and Todo 11-owned results directory. Make guards fail on writable Thermal VM fields, Thermal/spacing service fields, direct orchestrator assignment, Results save from VM/service spacing, unapproved `UpdateThermal*`, mutable snapshot references, duplicate subscriber and independent DI state registration. Do not edit production code in this todo: any detected legacy seam returns to the owning earlier todo, invalidates downstream receipts, and is corrected only under that todo's exact allow-list and gates.
  - **Acceptance:** zero canonical writes outside state/application/persistence mapper boundaries; one runtime state; no shared mutable snapshots; no duplicate subscribers; no Thermal/spacing backing stores in VM/service/context; no Hydraulics/Results ownership scope creep.
  - **QA — happy:** after creating the suite, run V1, V2, then the newly defined V11 command; emit the guard suite's machine-readable writer/subscriber graph at `task-11-ownership-guards.md`; expect every command exit 0/failed 0. Run the Todo 1 structural verifier and require its `v11_first_todo=11` assertion.
  - **QA — failure:** run V11 against its in-memory/source-string violation categories; every category passes only by matching its intended rejected symbol/path. Run the plan verifier against a copied candidate containing a V11 reference in Todo 10 and require nonzero.
  - **Commit:** `test(architecture): guard thermal state ownership`.

- [ ] 12. Run Debug/Release builds, focused/affected/full suites and reconcile exact executable evidence
  - **Depends on:** Todo 11.
  - **Allow-list:** generated Debug/Release `src/bin`, `src/obj`, test `bin/obj`; Todo 12-owned TestResults; and only raw logs, TRX, new `assert-trx-identities.ps1`, `verify-frozen-release.ps1`, `verify-final-receipts.ps1`, their fixtures/manifests, `frozen-release-sha256.json`, and receipts under Phase 4 evidence. Todo 1's parser is read-only. No production, test-source, map, model, widget, or dossier edits.
  - **References:** Todo 1 baseline commands and exact known `NotExecuted`; verification strategy filters.
  - **Action:** first create and fixture-test `assert-trx-identities.ps1`, `verify-frozen-release.ps1` and `verify-final-receipts.ps1`; the last parser requires exactly F1/F2/F3 receipts with the five machine fields, identical `SUBJECT`, `VERDICT=APPROVE`, identical frozen hashes and matching artifact hashes, and rejects omissions/duplicates/wrong SHA/non-APPROVE. Then run Debug/Release builds and focused/affected/full suites into Todo 12-owned results paths. Parse every TRX by exact identity and compare warning/error/failure/NotExecuted identities with Todo 1.
  - **Acceptance:** all commands exit 0; zero new warning/error/failure/NotExecuted identity; every required new test executes; raw commands, durations, SDK, counters, hashes and parser output are recorded. After all gates pass, create immutable `frozen-release-sha256.json` with SHA-256 for `src/bin/Release/net8.0-windows/win-x64/SnowMeltingCalculator.exe`, its product DLL, the Release test DLL and the exact plan; immediately re-hash and require equality. Dossier remains untouched until this todo is green.
  - **QA — happy:** run V1, then invoke the exact V2→V6 filters/loggers but rebind every `--results-directory` to `docs/architecture-migration/evidence/phase-4-thermal-state/task-12/TestResults` and use distinct filenames `task-12-v2-focused.trx` through `task-12-v6-full-release.trx`; parse that exact directory with Todo 1's parser. Expect every exit 0 and stable non-empty identity sets. Evidence: `task-12-executable-gates.md` plus raw files; no `catalog/v*` or shared test-project TestResults output is written by Todo 12.
  - **QA — failure:** run zero/missing/stale/duplicate TRX fixtures and final-receipt fixtures for missing field, wrong subject, REJECT/BLOCKED verdict, altered artifact hash and cross-lane hash mismatch; every parser exits nonzero. Any mismatched `NotExecuted` identity blocks even when `dotnet test` exited 0.
  - **Commit:** `test(thermal): verify phase 4 executable gates`.

- [ ] 13. Execute agent-operated Thermal user flows on the stable build
  - **Depends on:** Todo 12 green.
  - **Allow-list:** QA-only `prepare-ui-fixtures.ps1`, `run-wpf-ui-qa.ps1`, screenshots/process logs/copied task-owned `.smc` fixtures under Phase 4 evidence; no production edits and no overwrite of `Тест/*.smc`.
  - **References:** user-flow map; DEC-T03..T08; built WPF application; accepted Phase 3.1 UI/load behavior.
  - **Action:** implement the exact V9 inbox-.NET UIAutomation harness contract against all 17 stable AutomationIds added in Todo 6 (7 Thermal inputs/buttons, 4 Thermal outputs, 3 Hydraulics outputs and 3 Results outputs), then use it to exercise Thermal screen mode/supply/ground/pipe/spacing edits, recalculation indicator, Calculate, Hydraulics/Results projections, save, reload, second project load and new/reset. Use task-owned copies of fixtures/output. Capture exact displayed values, event/log evidence and screenshots; do not add a production/test package or make any further XAML change.
  - **Acceptance:** fixture manifest proves deterministic inputs; own supply edit retains last result and shows exactly `Температура подачи изменена. Требуется пересчёт.`; successful Calculate refreshes Hydraulics/Results once; saved/reloaded values match; second load has no project-A state; lifecycle load remains clean; no crash, unhandled dialog, duplicate row/event or stale result. No owner intervention is required.
  - **QA — happy:** run V9 exactly; expect exit 0, `fixture-manifest.json`, `observations.json` with all ten numbered steps passing, six non-empty happy-flow screenshots, clean stderr/process exit and `task-13-user-flow-qa.md` binding every assertion to its artifact.
  - **QA — failure:** run V9's built-in `-InvalidProject` branch against the task-owned unknown-pipe fixture; expect exit 0 only because the harness asserts the exact characterized fallback/message and cleared restore guard in `failure-observations.json`. Selector ambiguity, absent interactive desktop, unhandled dialog, timeout or crash must instead exit nonzero and block Todo 14; no manual fallback or owner checklist.
  - **Commit:** `test(thermal): record phase 4 user flow qa`.

- [ ] 14. Refresh all six architecture views, shared model, widget and workflow evidence
  - **Depends on:** Todos 12 and 13 green; stable code/test write-set.
  - **Allow-list:** exactly `docs/architecture-migration/maps/compile-time.md`, `docs/architecture-migration/maps/di-runtime.md`, `docs/architecture-migration/maps/state-ownership.md`, `docs/architecture-migration/maps/reactive.md`, `docs/architecture-migration/maps/persistence.md`, `docs/architecture-migration/maps/user-flow.md`, `docs/architecture-migration/maps/state-inventory.md`, `docs/architecture-migration/maps/characterization-tests.md`, `docs/architecture-migration/maps/persistence-compatibility.md`, `docs/architecture-migration/maps/target-invariants.md`, `docs/architecture-migration/maps/architecture-model.json`, `docs/architecture-migration/architecture-widget.html`, new/updated files under `docs/architecture-migration/evidence/phase-4-thermal-state/`, and canonical control/history files `docs/architecture-migration/STATE.json` plus `docs/architecture-migration/TASK_CONTEXT.md` only through the repository workflow's permitted transition. Both schema JSON files and widget generator/verifier source remain read-only; if validation proves a schema or script change is required, stop and re-plan rather than widening this list.
  - **References:** the six existing views `docs/architecture-migration/maps/compile-time.md`, `docs/architecture-migration/maps/di-runtime.md`, `docs/architecture-migration/maps/state-ownership.md`, `docs/architecture-migration/maps/reactive.md`, `docs/architecture-migration/maps/persistence.md`, and `docs/architecture-migration/maps/user-flow.md`; `docs/architecture-migration/maps/state-inventory.md` rows ST-012..ST-015 and ST-021..ST-022; `docs/architecture-migration/maps/target-invariants.md` INV-004; widget scripts.
  - **Action:** mark ThermalState sole owner, adapters/projections/origins/completions, removed writers, persistence compatibility and executable evidence in all affected views. Update one shared model; run model-v2/runtime-v2; generate widget twice; run `--check`; keep workflow `stage=executing` and record only Todo 14 technical evidence. Transition to `awaiting-owner-acceptance` is forbidden until F4 consolidated APPROVE.
  - **Acceptance:** every changed node/edge cites current source and Phase 4 evidence; Hydraulics/Results remain consumers; `.smc` edges/version unchanged; model/runtime suites pass; generation passes are byte-identical; final NUL-safe comparison preserves protected baseline; no stale `D:\IA\ace` metrics.
  - **QA — happy:** run V0, V7 and V8; then execute the exact Playwright MCP widget browser contract for all six IDs. Expect all commands/exits/assertions green, equal generation hashes, zero browser console errors and six `docs/architecture-migration/evidence/phase-4-thermal-state/task-14/browser/phase-4-widget-<ID>.png` artifacts; record the assertion/count table in `task-14-architecture-dossier.md`.
  - **QA — failure:** run validator against task-owned copied model fixtures with a missing evidence edge and invalid ID; expect nonzero. A V8 hash mismatch or protected drift blocks final verification and is fixed only in source inputs.
  - **Commit:** `docs(architecture): record phase 4 thermal ownership`.

## Final verification wave

- [ ] F1. Verify Conformance / Scope / Provenance, dirty-worktree preservation and architecture-dossier fidelity
  - **References:** exact frozen Phase 4 plan SHA, Todo 1 baseline/preimages, Todos 1-14 receipts, final NUL-safe status, Todo 14 six views/model/widget evidence, canonical generator/validator and final workflow state.
  - **Action:** run V13 into `final/f1/frozen-hashes-before.json`; independently map every changed path/hunk to one todo allow-list, DEC-T01..T08 and scope; verify owner gates, canonical/mirror plan identity, evidence provenance, unchanged staged set and no lost unrelated user hunk. Run model-v2/runtime-v2 and inspect all six filters/shared IDs/edges without regenerating canonical artifacts; verify the canonical widget hash remains Todo 14's deterministic hash and workflow remains `executing`, not awaiting acceptance or authorizing Phase 5. Then run V13 into `final/f1/frozen-hashes-after.json`.
  - **Acceptance:** every planned requirement has evidence and every changed path is explained; Hydraulics/Results ownership, UI/formulas/wire contract stayed out; protected baseline comparison is symmetric; validators exit 0; ST-012..ST-015/ST-021..ST-022 and INV-004 reflect actual code; evidence links resolve; no manual HTML edit or premature next-phase authorization exists; before/after receipts contain identical manifest plus executable/product DLL/test DLL/plan hashes. Any unexplained path/hunk, stale view/identity, missing hash, protected drift or premature workflow transition is `REJECT`.
  - **QA — happy:** run V13-before, V0, V10, `pwsh -File "docs/architecture-migration/evidence/phase-4-thermal-state/verify-protected-baseline.ps1" -Baseline "docs/architecture-migration/evidence/phase-4-thermal-state/baseline-git-status.bin" -AllowedHunks "docs/architecture-migration/evidence/phase-4-thermal-state/todo-1-allowed-hunks.json" -EvidenceRoot "docs/architecture-migration/evidence/phase-4-thermal-state" -Output "docs/architecture-migration/evidence/phase-4-thermal-state/final/f1/protected.json"`, V12-F1, verify the canonical widget hash recorded by Todo 14, and execute the exact Playwright MCP widget browser contract without V8 regeneration using `final/f1/browser/f1-phase-4-widget-<ID>.png`; then run V13-after. Expect exits 0, `protected_mismatch_count=0`, F1-owned model/runtime receipts, six screenshots, zero console errors and workflow still `executing`. Produce `final/f1/conformance-scope-provenance.md` with exact path/hunk matrix, per-ID assertion/count table, manifest hash, all four frozen hashes and APPROVE/REJECT.
  - **QA — failure:** run the verifier against a task-owned copied ledger with one missing evidence binding and V7 against a copied model with an altered edge/evidence reference; both must reject while canonical artifacts remain untouched.
  - **Receipt:** write exactly `REVIEW_ID`, `SUBJECT: phase-4-thermal-state@<frozen-sha256>`, `RECEIPT`, `VERDICT: APPROVE|REJECT|BLOCKED`, and `REASON`; missing/malformed fields are not approval.

- [ ] F2. Audit architecture/code quality and sole Thermal ownership
  - **References:** final source, state/legacy-writer/DI tests, final writer/subscriber graph, six maps/model.
  - **Action:** run V13 into `final/f2/frozen-hashes-before.json`; independently prove immutable snapshots, exhaustive origins/status, one ProjectSession state, zero legacy writable stores/bypasses, one upstream subscriber, exact context/spacing consumer semantics and no Hydraulics/Results ownership migration; then run V13 into `final/f2/frozen-hashes-after.json`.
  - **Acceptance:** focused Release guard/state/DI suite passes; architecture inspection finds one owner and no mutable escape; all DEC-T01..T08 semantics are represented in code/tests; before/after receipts contain identical manifest plus all four frozen hashes. Any second owner, string authority, duplicate subscription, mutable snapshot, missing hash, or drift is `REJECT`.
  - **QA — happy:** run V13-before, V12-F2, reconcile every F2 TRX against its intended non-empty identity set, then V13-after; expect exits 0/failed 0 and exact identities only under `final/f2/`. Produce `final/f2/architecture-code-quality.md` with symbol-level evidence, commands, manifest hash and all four frozen hashes.
  - **QA — failure:** V12-F2's `f2-negative.trx` must execute every synthetic guard category in the F2-owned directory; missing category or grep-only claim without AST/test evidence is REJECT.
  - **Receipt:** use the same exact five-field machine format and frozen `SUBJECT` as F1.

- [ ] F3. Re-run executable lifecycle, persistence, downstream and real user-flow QA
  - **References:** Todo 12 TRX/logs, Todo 13 screenshots, task-owned fixtures, Phase 3/3.1 regressions.
  - **Action:** run V13 into `final/f3/frozen-hashes-before.json`; independently verify Todo 12's Release build log, run the full Release suite and each isolated negative category from the immutable binaries, and run an isolated agent-operated edit/calculate/save/load/second-load/reset flow; reconcile exact tests and user-visible values without rebuilding or writing canonical Todo artifacts; then run V13 into `final/f3/frozen-hashes-after.json`.
  - **Acceptance:** commands exit 0; no new warnings/failures/NotExecuted; each `CalculationFailure`, `PersistenceFailure`, and `RestoreFailure` TRX independently equals its non-empty expected manifest group with no duplicate/unexpected identity; exact multiplicity matrix and `.smc` fallback pass; Hydraulics/Results projections are current; no stale project-A state or subscription multiplication; before/after receipts contain identical manifest plus all four frozen hashes. Any self-report without raw TRX/log/screenshot or hash receipt is `REJECT`.
  - **QA — happy:** run V13-before, V12-F3 exactly, then V13-after; expect all exits 0, failed 0, three independently reconciled non-empty negative identity JSON files, fixture manifest, both observations JSON files and screenshots only under `final/f3/`. Produce `final/f3/executable-user-risk.md` with manifest hash and all four frozen hashes.
  - **QA — failure:** exercise the identity verifier with a zero-test TRX, an unexpected identity and a duplicate identity; independently corrupt one task-owned expected selector and one copied unknown-pipe expectation. Every probe must reject without touching source fixture or frozen build. Any zero-test filter, missing group, absent interactive desktop, or contract mismatch is REJECT.
  - **Receipt:** use the same exact five-field machine format and frozen `SUBJECT` as F1.

- [ ] F4. Consolidate the three immutable final-domain receipts without overriding any verdict
- [ ] F5. Bogus fifth final verification lane
  - **Depends on:** F1, F2 and F3 completed sequentially with APPROVE verdicts against the same frozen hashes.
  - **References:** `final/f1/conformance-scope-provenance.md`, `final/f2/architecture-code-quality.md`, `final/f3/executable-user-risk.md`, `frozen-release-sha256.json`, final protected baseline comparison and plan SHA.
  - **Action:** run Todo 12's `verify-final-receipts.ps1` over the exact F1/F2/F3 directories and frozen manifest; verify each says APPROVE, exact `SUBJECT` matches the frozen Phase 4 SHA, before/after and cross-lane executable/product/test DLL/plan hashes are identical, and every artifact matches its hash. Write only `final/consolidated/final-receipt.md` and `artifact-manifest.json`. This step has no authority to reinterpret, waive or override a domain rejection.
  - **Acceptance:** consolidated manifest binds the frozen write-set, exactly three domain verdicts, command/artifact hashes, reused/rerun classification and residual-risk list; any missing/mismatched artifact, non-APPROVE verdict or hash drift produces REJECT and no completion transition.
  - **QA — happy:** run `verify-final-receipts.ps1` against all three receipt directories and `frozen-release-sha256.json`; expect exit 0, exactly three APPROVE inputs, one subject/hash identity and complete artifact coverage. Re-run V13 F4-after and require its set equals F4-before and every domain pair. Produce the two consolidated artifacts with APPROVE.
  - **QA — failure:** run the verifier against a task-owned copied manifest with one altered artifact hash and one copied receipt marked REJECT; expect nonzero, consolidated REJECT, and no workflow completion/acceptance transition.
  - **Receipt:** use the exact five-field machine format; `BLOCKED` means required evidence/tool/identity is unavailable without an authorized scope change. Missing/malformed terminal planning or final receipt permits at most the repository-defined one correction retry; a second failure remains blocked.

## Commit strategy

- Planning/review creates no product commit.
- During separately authorized execution, use one green atomic commit per Todo 1-14 in the main checkout of `D:\IA\3ace v.2` on `master`, staging only that todo's allow-list. Todo 1's commit includes only its created helper/evidence files. Todos 2-11 pair their production/test boundary; Todos 12-14 pair only their owned evidence/dossier boundary. Never mix adjacent Todos.
- Pair implementation with its direct tests; inspect staged diff before every commit; stage only the todo allow-list; never amend/push or include baseline unrelated paths without explicit owner request.
- Final verification produces receipts, not code commits. Any correction invalidates all prior F1-F4 receipts and reruns the complete sequential final chain.

## Success criteria

- `ProjectSession.ThermalState` is the sole writable owner for Thermal inputs, pipe spacing, last derived result and Thermal status; immutable snapshots prevent mutation bypass.
- `ThermalViewModel`, `CalculationStateService`, `CalculationContext`, Hydraulics and Results are adapters/projections/consumers with no second writable canonical store.
- Own input edits, upstream invalidation, calculation, reset/load/second-load/fallback and failure semantics match DEC-T01..T08 and exact multiplicity tests.
- Accepted Phase 3.1 lifecycle/user Climate contract and Construction→Thermal invalidation remain green with one canonical subscriber.
- Supported `.smc` v1.0/v1.1 fields/version and semantic round-trip remain unchanged; status/origin/runtime-only metadata is not persisted.
- Debug/Release builds, focused/affected/full Release suites and agent-operated user flows pass with raw evidence and no new warning/failure/NotExecuted identity.
- All six architecture views, supporting inventories, shared model and deterministic generated widget match current source/evidence.
- Protected dirty-worktree baseline and unrelated user hunks are preserved; no forbidden Git operation or scope creep occurs.
- F1-F3 independently APPROVE the same frozen write-set in exactly the three required domains, and F4 consolidates those immutable receipts without override authority. Technical completion transitions only to `awaiting-owner-acceptance`; explicit owner result acceptance alone sets `completed`, `stop=true`. Phase 5 never starts automatically.
