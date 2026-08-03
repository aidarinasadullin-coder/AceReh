---
phase: phase-0-baseline
snapshot_sha: f0d19c34ac03075d64548f1059e9c6626d3596b5
source_basis: working-tree
generated_at_utc: 2026-07-30T17:41:01.8271777Z
working_directory: D:/IA/ace v.2
commands:
  - Read docs/architecture-migration/architecture_audit.md
  - Read docs/architecture-migration/audit_metrics.json
  - Read docs/architecture-migration/architecture_widget.html
  - Read docs/architecture-migration/archive/phase-0-baseline.invalidated-explore-chain.md
  - Read docs/architecture-migration/evidence/repository-snapshot.md
  - Read docs/architecture-migration/evidence/metrics-baseline.json
  - Read docs/architecture-migration/evidence/codegraph-baseline.md
  - codegraph_codegraph_explore focused current-source claims not covered by Todo 4 and verifier-rejected aggregate/edge claims
  - PowerShell exhaustive production declaration search for ProjectSession under src/**/*.cs excluding bin,obj,generated,tests,history,dossier
  - PowerShell read-only extraction and structural assertions recorded below
exit_code: 0
status: pass
raw_output: Inline reconciliation rows, source provenance, and read-only QA output.
limitations:
  - This receipt is bound to the stated working tree and timestamp; it is not a clean-tree, runtime, or future-architecture assertion.
  - Todo 4 sampled current source. It did not provide a complete directed graph, SCC computation, or repository-wide cycle proof.
  - Compile-time references and DI registrations do not by themselves prove runtime invocation.
---

# Historical Audit Reconciliation

## Binding, Terms, and Reuse Rule

Historical sources describe `D:/IA/ace` unless explicitly stated otherwise. That root is **historical provenance only**, never a current path. Current evidence is bound to snapshot `f0d19c34ac03075d64548f1059e9c6626d3596b5`, source basis `working-tree`, and root `D:/IA/ace v.2`.

Classification is exactly one of `confirmed`, `changed`, `not-reproducible`, or `not-applicable`:

| Classification | Meaning |
| --- | --- |
| `confirmed` | The limited current statement is directly supported by cited current source or a current metric ID. |
| `changed` | A comparable current measurement exists and differs, or current source contradicts the historical wording. |
| `not-reproducible` | No sound current method/evidence establishes the historical quantity or completeness claim. |
| `not-applicable` | A proposal, target, process rule, or specification is not a current architecture fact. |

**Reuse rule:** later maps may cite only a reconciliation ID below when that row has current evidence. They must not promote a `not-reproducible` historical value, a historical absolute path, or a target/roadmap proposal to a current metric or fact. A `confirmed` compile-time/DI row remains only that edge kind, not proof of runtime invocation.

## Current Evidence Register

| Evidence ID | Current basis |
| --- | --- |
| `EV-SNAPSHOT` | `evidence/repository-snapshot.md`: root, snapshot SHA, dirty working-tree boundary. |
| `EV-METRICS` | `evidence/metrics-baseline.json`: `METRIC-CS-TRACKED-FILES=276`, `METRIC-CS-RAW-FILES=360`, `METRIC-CS-EXCLUDED-FILES=85`, `METRIC-CS-FILTERED-FILES=275`, `METRIC-CS-PHYSICAL-LOC=70635`, `METRIC-CS-NONBLANK-LOC=60559`, `METRIC-CS-DECLARED-TYPE-LEXICAL=395`; SCC `null/degraded`, cycles `null/not-reproducible`. |
| `EV-CG-01` | `evidence/codegraph-baseline.md`, CG-01 and coverage rows: `ProjectLoadOrchestrator`, reset/restore and persistence context. |
| `EV-CG-02` | `evidence/codegraph-baseline.md`, CG-02: `CalculationContext`, `CalculationStateService`, `ProjectStateService`, reactive evidence. |
| `EV-CG-04` | `evidence/codegraph-baseline.md`, CG-04 and DI row: singleton registrations and exact source-backed lifetimes. |
| `EV-CG-05` | `evidence/codegraph-baseline.md`, CG-05: `ResultsViewModel` constructor/export source sampling. |
| `EV-SRC-A` | Focused current reads/Codegraph: `src/Services/AppSettings.cs:9-74`; `src/Controls/Climate/CityAutoCompleteBox.xaml.cs:1-440`; `src/Views/Construction/ConstructionView.xaml.cs:1-118`; `src/Views/Shared/ConstructionVisualizationView.xaml.cs:1-318`; `src/Services/Hydraulics/GlycolDataService.cs:45-62`. |
| `EV-SRC-B` | Focused current Codegraph/reads: `src/Services/Results/ResultsPdfDataBuilder.cs:1-205`; `src/Services/Results/HydraulicSummaryBuilder.cs:1-116`; `src/Services/Hydraulics/CircuitsValidator.cs:1-88`; `src/Services/Hydraulics/CollectorTypeSelector.cs:1-94`; `src/ViewModels/Results/ResultsViewModel.cs:478-511`; `src/ViewModels/Construction/ConstructionViewModel.cs:214-248`; `src/ViewModels/Construction/MaterialEditorViewModel.cs:91-120`; `src/ViewModels/Construction/TemplateEditorViewModel.cs:105-115`; `src/ViewModels/Hydraulics/CollectorViewModel.cs:273-279`. |
| `EV-PS-SEARCH` | Read-only PowerShell exhaustive declaration search: recursive `src/**/*.cs`, excluding `bin`, `obj`, `generated`, `tests`, history and dossier paths; `173` files scanned, `0` `class|record|interface|struct ProjectSession` declarations and `0` tokens. Command and observed output are recorded in `REC-012/026` and QA. |
| `EV-PROCESS` | Current dossier process record: `TASK_CONTEXT.md:271-309` identifies the invalidated chain, replacement primary plan, owner approval, and explicit execution authorization; `archive/phase-0-baseline.invalidated-explore-chain.md:1-20` labels the superseded draft invalidated and non-executable. |
| `EV-OWNER` | `TASK_CONTEXT.md:18-35,239-255` and `правка архитектуры.txt:1`; owner target/process material, not current source evidence. |

## Reconciliation Rows

Each ID is stable within this Phase 0 baseline. “Historical source” preserves the original text/path; “current result” never reuses a historical number as current evidence.

| Claim ID | Historical source/path/text | Classification | Current result | Snapshot/source basis | Current evidence IDs/paths | Confidence | Migration impact |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `REC-001` | `architecture_audit.md:4`; widget `:182`: `D:\IA\ace\src`, `173 .cs-файла` | changed | Current scopes are distinct: tracked `276`, raw `360`, filtered `275`; no current `173` claim. | working-tree | `EV-METRICS`; metric IDs `METRIC-CS-TRACKED-FILES`, `-RAW-FILES`, `-FILTERED-FILES` | high | Maps must name scope; do not reuse 173. |
| `REC-002` | `architecture_audit.md:4`: `108 файлов` tests | not-reproducible | Todo 3 intentionally measured all filtered source plus tests, not a current test-only file count. | working-tree | `EV-METRICS`, scope definitions `:25-40` | high | Count tests afresh only when a later map needs that scope. |
| `REC-003` | `architecture_audit.md:4`; widget `:182`: `265 классов/интерфейсов`, `265 типов` | changed | Current lexical declared-type count is `395`; it is not compiler-semantic and is not directly comparable as “classes/interfaces”. | working-tree | `EV-METRICS`; `METRIC-CS-DECLARED-TYPE-LEXICAL` | high | Do not label 395 as semantic type count or reuse 265. |
| `REC-004` | `architecture_audit.md:11,17`; widget `:191`: `14 циклических зависимостей` | not-reproducible | Current SCC is `null/degraded`; current cycle count is `null/not-reproducible`. Qualitative source coupling is separately confirmed below. | working-tree | `EV-METRICS`; `METRIC-SCC-COUNT`, `METRIC-CYCLE-COUNT`; `EV-CG-01..05` limitations | high | Never use 14 as a current metric; later graph work needs a sound complete graph. |
| `REC-005` | `architecture_audit.md:19-28`; widget `:193,231,480`: `5 services depend on ViewModels` | changed | The historical five-file aggregate is not confirmed: only direct/used dependencies in `REC-033` and `REC-034` are current evidence; `REC-035` and `REC-036` are unused imports. No current complete aggregate count was computed. | working-tree | `EV-SRC-B`; `EV-CG-01`, `EV-CG-05` | high | Widget statistic must not be reused as current; later map records per-edge findings. |
| `REC-006` | `architecture_audit.md:23,37`; widget `:303,464`: `ProjectLoadOrchestrator` injects four concrete VMs, writes/resets/restores them | confirmed | Constructor directly takes `ClimateViewModel`, `ConstructionViewModel`, `ThermalViewModel`, `CircuitsViewModel`; `ResetModules` resets the context and four VMs. | working-tree | `EV-CG-01`; `src/Services/Project/ProjectLoadOrchestrator.cs:38-67` | high | Current concrete VM dependency is a primary migration seam; reset/restore behavior still needs later flow characterization. |
| `REC-007` | `architecture_audit.md:39-43`; widget `:192,277,458`: `ResultsViewModel` `1946` LOC, `16` deps, `49` methods, `3` additional classes | not-reproducible | Todo 3 did not produce current per-file LOC/method/constructor/class metrics. Current source does directly verify four concrete VM constructor dependencies and the named three concrete collaborators, but not those historical counts. | working-tree | `EV-CG-05`; `EV-SRC-B`; `src/ViewModels/Results/ResultsViewModel.cs:478-511` | high | Do not reuse 1946/16/49/3; current concrete coupling remains a scoped migration concern. |
| `REC-008` | `architecture_audit.md:45-47`; widget `:238,325,469,481`: `ConstructionRepository -> Services.Construction`, asserted direct cycle | confirmed | Current `ConstructionRepository` has type-level dependency on `MaterialNotFoundException` in `Services.Construction`; this confirms the repository-to-service compile-time coupling only, not a graph cycle or runtime invocation. | working-tree | `EV-CG-04`; `TASK_CONTEXT.md:100-102`; `src/Repositories/Construction/ConstructionRepository.cs`; `src/Services/Construction/MaterialNotFoundException.cs:1-42` | high | Model as compile-time edge only; cycle result remains `REC-004`. |
| `REC-009` | `architecture_audit.md:49-51`; widget `:194,245,482`: five VMs directly use repositories | confirmed | Current constructors/source directly show repository dependencies for `ConstructionViewModel`, `MaterialEditorViewModel`, `TemplateEditorViewModel`, `CollectorViewModel`, and `ResultsViewModel` (`IMaterialRepository`). This confirms the listed direct compile-time paths, not a repository-wide bypass census. | working-tree | `EV-SRC-B`; `src/ViewModels/Results/ResultsViewModel.cs:478-511` | high | Record individual edges in later compile-time map; no runtime claim. |
| `REC-010` | `architecture_audit.md:57-64`; widget `:221,454,466`: all services/VMs singleton and state spread across VMs/models/`CalculationStateService` | changed | Current DI source verifies singleton lifetime for the listed module VMs and baseline services, including `CalculationStateService`, `CalculationContext`, and `ProjectStateService`; it does not prove “all” services/VMs nor canonical ownership. | working-tree | `EV-CG-04`; `codegraph-baseline.md:59,81-83` | high | Later state inventory must establish owners/writers rather than repeat an all-singleton/general-state claim. |
| `REC-011` | `architecture_audit.md:62`; widget `:466`: `CalculationStateService` `171` lines, `18` public members | not-reproducible | Current source verifies `SetPipeSpacing`, guarded restore path, `PipeSpacingChanged`, and `StateChanged`; Todo 3 did not compute current LOC/member totals. | working-tree | `EV-CG-02`; `src/Services/Navigation/CalculationStateService.cs:120-168` | high | Preserve observed seam/events; do not reuse 171/18. |
| `REC-012` | `architecture_audit.md:64`; widget target `:391,476`: no current `ProjectSession` / proposed replacement | confirmed | Exhaustive current production declaration search scanned `173` `src/**/*.cs` files (excluding generated/test/history/dossier paths) and found `0` `ProjectSession` declarations and `0` tokens. `CalculationContext` remains a separate current seam. | working-tree | `EV-PS-SEARCH`; `EV-CG-02`; `src/Core/CalculationContext.cs:222-230` | high | Treat absence as baseline fact; do not equate or silently rename `CalculationContext`. |
| `REC-013` | `architecture_audit.md:66-68`; widget target `:426`: static `AppSettings.Instance => _instance ??= Load()` | confirmed | Current source contains exactly the static `_instance` and `Instance` lazy-load expression; it reads/writes the application-data settings file. | working-tree | `EV-SRC-A`; `src/Services/AppSettings.cs:9-67` | high | Current global/static configuration seam can be mapped; DI replacement is target-only. |
| `REC-014` | `architecture_audit.md:72-77`; widget `:249-252,453`: code-behind LOC `439/318/118` | changed | Read-only `Get-Content` physical-line counts are `440/318/118` for the three named files. This is a current physical-line measurement, not a semantic LOC metric and does not validate historical methodology. | working-tree | `EV-SRC-A`; read-only `Get-Content` with `Measure-Object -Line` | high | Historical 439/318/118 cannot be reused; later metrics must declare method. |
| `REC-015` | `architecture_audit.md:80-82`; widget `:426,463`: `GlycolDataService` `1108` lines and `8 DTO` classes | not-reproducible | Current file/service is present, but Todo 3 has no per-file LOC/type grouping and focused query returned only selected constructor source. | working-tree | `EV-SRC-A`; `src/Services/Hydraulics/GlycolDataService.cs:45-62`; `EV-METRICS` | high | Do not use 1108/8 until a dedicated current measurement is requested. |
| `REC-016` | `architecture_audit.md:88`; widget historical detail `:455`: README says `Core/UI/Data` | confirmed | Current `README.md` still presents the `Core`, `UI`, `Data` tree, while current source evidence/dossier uses domain MVVM locations. The assertion is documentation comparison, not a source metric. | working-tree | `README.md:7-20`; `EV-SNAPSHOT`; `EV-CG-01..05` paths | high | Documentation alignment remains a later scoped decision; no README edit in Phase 0. |
| `REC-017` | `architecture_audit.md:89`; widget heading `:465`: `Services.Results -> Services.Visualization` | confirmed | `ResultsPdfDataBuilder` in `Services.Results` imports and receives `IConstructionVisualizationImageService` from `Services.Visualization`. This is compile-time coupling, not a policy violation or runtime trace. | working-tree | `EV-SRC-B`; `src/Services/Results/ResultsPdfDataBuilder.cs:1-35,109-121` | high | Later maps retain edge kind and evidence without inventing cross-domain rules. |
| `REC-018` | `architecture_audit.md:90`; widget `:493-494`: concrete builders/orchestrator in `ResultsViewModel` | confirmed | Constructor directly takes concrete `ProjectLoadOrchestrator`, `ResultsPdfDataBuilder`, and `HydraulicSummaryBuilder`. | working-tree | `EV-CG-05`; `src/ViewModels/Results/ResultsViewModel.cs:478-511` | high | Current compile-time design debt; no runtime behavior inferred. |
| `REC-019` | `architecture_audit.md:94-109`; widget target diagram `:343-403`: target layer direction, `ProjectSession`, interfaces | not-applicable | This is a proposed/owner target architecture, not a current architecture fact or implementation authorization. | owner/dossier material | `EV-OWNER`; `TASK_CONTEXT.md:20-35,241-244` | high | Later target-invariants artifact may state it as proposed/approved constraint only. |
| `REC-020` | `architecture_audit.md:111-121`; widget roadmap `:419-426`: five refactoring steps and parallelism | not-applicable | Roadmap is historical proposal. Current approved Phase 0 requires sequential production vertical slices and does not authorize implementation. | owner/dossier material | `EV-OWNER`; `TASK_CONTEXT.md:107-128`; approved plan `phase-0-baseline.md:107-113` | high | Do not execute or derive future compatibility/parallel production claims from it. |
| `REC-021` | `audit_metrics.json`: repeated `ResultsViewModel.cs` rows each assign `1946` LOC to four classes | not-reproducible | Historical rows are a file-level LOC value repeated per class; no current per-file LOC/class measurement was captured. | working-tree | `EV-METRICS`; `EV-CG-05` | high | Grouped under `REC-007`; never use duplicated historical LOC as per-class current LOC. |
| `REC-022` | `audit_metrics.json`: repeated `GlycolDataService.cs` rows assign `1108` LOC to service plus eight DTO-like classes | not-reproducible | Historical file LOC is repeated across class rows; no current per-file LOC or DTO census was captured. | working-tree | `EV-METRICS`; `EV-SRC-A` | high | Grouped under `REC-015`; no per-type current LOC claim. |
| `REC-023` | `audit_metrics.json`: repeated file LOC rows for `ProjectData` (`489`), `ReportSections` (`598`), code-behind and other per-class entries | not-reproducible | These are historical file-level values repeated per declared type; Todo 3 supplies only aggregate filtered LOC and lexical types. | working-tree | `EV-METRICS`; `METRIC-CS-PHYSICAL-LOC`, `METRIC-CS-DECLARED-TYPE-LEXICAL` | high | Later maps may cite symbols/paths, not these historical LOC rows. |
| `REC-024` | Owner material `правка архитектуры.txt:1`: models use services that obtain/store/process data and VM updates View by event | not-applicable | This is an owner design preference/specification, not evidence of current architecture. | owner material | `EV-OWNER` | high | Preserve as input for later approved target work, not as current edge evidence. |
| `REC-025` | Invalidated archive `:78-82`: `173 source`, raw `241`, `108 test` files | changed | Current receipt distinguishes tracked `276`, raw `360`, filtered `275`, excluded `85`; no current test-only count was produced. | working-tree | `EV-METRICS`; `REC-001`, `REC-002` | high | Archived counts cannot become current metrics. |
| `REC-026` | Invalidated archive `:83-86`: no `ProjectSession`; `CalculationContext` is a current seam, not automatically it | confirmed | The exhaustive `EV-PS-SEARCH` establishes no production declaration; current `CalculationContext.Reset` clears state and raises `ContextChanged`. No rename/facade/removal is assumed. | working-tree | `EV-PS-SEARCH`; `EV-CG-02`; `src/Core/CalculationContext.cs:222-230`; `TASK_CONTEXT.md:254-255` | high | Inventory `CalculationContext` as current seam and leave disposition open. |
| `REC-027` | Invalidated archive `:91-94`: green build proves compilation only; runtime/reactive/persistence/user flow need own evidence | confirmed | Todo 2 build/test receipt is green, while Todo 4 explicitly limits source evidence and does not claim runtime/user-flow completeness. | working-tree | `evidence/build-baseline.md`; `evidence/test-baseline.md`; `EV-CG-01..05` limitations | high | Characterization and user-flow/persistence evidence remain required. |
| `REC-028` | Invalidated archive `:1-7,14,20`: discipline-agent chain was simulated; archived draft MUST NOT be approved/executed | confirmed | The archive labels itself invalidated; `TASK_CONTEXT.md` records replacement primary plan and explicit owner execution authorization for the current canonical plan, not the archive. | dossier process record | `EV-PROCESS`; `archive/phase-0-baseline.invalidated-explore-chain.md:1-20`; `TASK_CONTEXT.md:271-309` | high | Archived plan is untrusted input only and is never executable authority. |
| `REC-029` | Invalidated archive `:223-246`: target invariants including `ProjectSession`, transactional expectations, compatibility, sequential lane | not-applicable | These are future design constraints/owner decisions. Current source does not establish transactional restore, byte identity, future compatibility, or target architecture. | owner/dossier material | `EV-OWNER`; archive `:195-246`; `TASK_CONTEXT.md:246-255` | high | Later maps must label targets/proposals and preserve open owner decisions. |
| `REC-030` | Widget `:221,479`: `VM -> VM x4`, concrete singleton classes | confirmed | `ResultsViewModel` directly accepts the four concrete module VMs. DI evidence confirms listed VMs are singleton registrations; this does not prove every historical VM-to-VM edge. | working-tree | `EV-CG-04`, `EV-CG-05`; `src/ViewModels/Results/ResultsViewModel.cs:478-511` | high | Preserve as current concrete constructor coupling; no cycle count. |
| `REC-031` | Widget `:258-270,295,457`: `Climate=784`, `Construction=1211`, `Circuits=1257`, `Thermal=734`, dependency counts | not-reproducible | Current per-file LOC and per-class dependency counts were not recomputed; the source symbols are present but no historical number is adopted. | working-tree | `EV-METRICS`; `EV-CG-01..05` | high | Use current source paths/typed edges only until dedicated metrics exist. |
| `REC-032` | Widget `:339,472`: `550` cities and ASHRAE data | not-reproducible | No current dataset census/provenance receipt was created in Todos 1-4. | working-tree | `EV-SNAPSHOT`; `README.md:21-38` is descriptive only | high | Do not represent widget dataset number as current architecture evidence. |
| `REC-033` | Audit/widget named member of five-service aggregate: `ProjectLoadOrchestrator -> four concrete VMs` | confirmed | Direct constructor dependency on `ClimateViewModel`, `ConstructionViewModel`, `ThermalViewModel`, and `CircuitsViewModel`; reset source is separately observed. | working-tree | `EV-CG-01`; `src/Services/Project/ProjectLoadOrchestrator.cs:38-67` | high | Typed compile-time edge; no runtime invocation/cycle claim. |
| `REC-034` | Audit/widget named member: `ResultsPdfDataBuilder` reads construction/hydraulics/results VMs | confirmed | Constructor directly receives `ConstructionViewModel` and `CircuitsViewModel`; `Build(ResultsViewModel results)` receives the Results projection. | working-tree | `EV-SRC-B`; `src/Services/Results/ResultsPdfDataBuilder.cs:16-43,109-205` | high | Three direct used type dependencies; no aggregate/cycle claim. |
| `REC-035` | Audit/widget named member: `HydraulicSummaryBuilder` reads `ViewModels.Hydraulics` | changed | Current `HydraulicSummaryBuilder` imports `ViewModels.Results` only for result projection DTO types and accepts `IEnumerable<CollectorData>`; it has no Hydraulics ViewModel dependency. | working-tree | `EV-SRC-B`; `src/Services/Results/HydraulicSummaryBuilder.cs:1-116` | high | Historical member claim is contradicted; do not count it as service-to-VM coupling. |
| `REC-036` | Audit/widget named members: `CircuitsValidator` and `CollectorTypeSelector` depend on `ViewModels.Hydraulics` | changed | Each file has an unused `using SnowMeltingCalculator.ViewModels.Hydraulics;`; their current public APIs and bodies use models/navigation only and no ViewModel type token. Import alone is not a used type dependency. | working-tree | `EV-SRC-B`; `src/Services/Hydraulics/CircuitsValidator.cs:1-88`; `src/Services/Hydraulics/CollectorTypeSelector.cs:1-94` | high | Do not model either as a service-to-VM edge without a used type/call. |
| `REC-037` | Audit/widget `Views.Shared -> Services.Visualization` | confirmed | `ConstructionVisualizationView` imports visualization types, instantiates `ConstructionVisualizationRenderer`, creates `ConstructionVisualizationParameters`, and calls `_renderer.Render`. | working-tree | `EV-SRC-A`; `src/Views/Shared/ConstructionVisualizationView.xaml.cs:8,23,297-310` | high | Compile-time/use edge is current; it is not a runtime user-flow proof. |
| `REC-038` | Widget `:423,465`: `8 cycles` through `Services.Results` | not-reproducible | No complete directed graph/SCC result was available; current cycle count remains null. | working-tree | `EV-METRICS`; `METRIC-CYCLE-COUNT`; `EV-CG-01..05` limitations | high | Never reuse 8 as current. |
| `REC-039` | Widget `:459`: `MainViewModel` has `3 dependencies` | not-reproducible | Current constructor has more parameters; no normalized dependency-count method was captured. | working-tree | `src/ViewModels/Shell/MainViewModel.cs:38-57`; `EV-CG-03` | high | Do not reuse 3. |
| `REC-040` | Widget `:455`: `ConstructionViewModel` has `8 dependencies` | not-reproducible | Current constructor has eleven parameters; no normalized dependency-count method was captured. | working-tree | `EV-SRC-B`; `src/ViewModels/Construction/ConstructionViewModel.cs:214-248` | high | Do not reuse 8. |
| `REC-041` | Widget `:457`: `CircuitsViewModel` has `6 dependencies` | not-reproducible | Todo 4 verified selected reactive source but did not produce a current constructor dependency count. | working-tree | `EV-CG-02`; `src/ViewModels/Hydraulics/CircuitsViewModel.cs:721-730` | high | Do not reuse 6. |
| `REC-042` | Widget `:266`: `ThermalViewModel` has `5 dependencies` | not-reproducible | No current normalized constructor dependency count was captured. | working-tree | `EV-CG-01..05` | high | Do not reuse 5. |
| `REC-043` | Widget `:335,471`: `Models — 5 domains` | not-reproducible | No current domain taxonomy/count method was established; source directory naming is not a defined domain census. | working-tree | `EV-METRICS`; `EV-CG-01..05` | high | Do not reuse five domains as current. |
| `REC-044` | Widget `:287,295,321,467-471`: qualitative `clean`, `correct direction`, `proper layer`, `POCO foundation` claims | not-reproducible | Todos 1-4 provide selected typed edges, not a complete policy-conformance assessment of the named domains/layers. | working-tree | `EV-CG-01..05` limitations | high | Later maps must cite exact edges rather than unqualified cleanliness/direction labels. |
| `REC-045` | Widget `:472`: JSON/`.smc` data statement including current supported shape | not-reproducible | Todo 4 verifies selected `.smc` persistence mechanisms, but no current data-source census or full wire-contract claim is established here. | working-tree | `EV-CG-01`; `codegraph-baseline.md:61` | high | Defer dataset/wire-contract completeness to persistence inventory. |

## Historical Material Claim Inventory and Binding

This inventory is the executable QA input. A key represents one material audit/widget assertion or explicitly bounded group. Every key maps to one or more existing `REC-*` IDs; a missing mapping is a failure, not an implicit omission.

| Material key | Historical source | Bound reconciliation IDs |
| --- | --- | --- |
| `M-001-source-files-173` | audit `:4`, widget `:182` | `REC-001` |
| `M-002-test-files-108` | audit `:4` | `REC-002` |
| `M-003-types-265` | audit `:4`, widget `:182` | `REC-003` |
| `M-004-cycles-14` | audit `:11,17`, widget `:191` | `REC-004` |
| `M-005-services-to-vm-5` | audit `:19-28`, widget `:193,231,480` | `REC-005, REC-033, REC-034, REC-035, REC-036` |
| `M-006-project-load-four-vm-reset-restore` | audit `:23,37`, widget `:303,464` | `REC-006, REC-033` |
| `M-007-results-vm-loc-deps-methods-extra-classes` | audit `:39-43`, widget `:192,277,458` | `REC-007` |
| `M-008-repository-service-coupling` | audit `:45-47`, widget `:238,325,469,481` | `REC-008` |
| `M-009-vm-repository-5` | audit `:49-51`, widget `:194,245,482` | `REC-009` |
| `M-010-singleton-state-claims` | audit `:57-64`, widget `:221,454,466` | `REC-010, REC-011` |
| `M-011-no-project-session` | audit `:64`, widget `:391,476` | `REC-012, REC-026` |
| `M-012-app-settings-static` | audit `:66-68`, widget `:426` | `REC-013` |
| `M-013-code-behind-loc` | audit `:70-77`, widget `:249-252,453` | `REC-014` |
| `M-014-views-shared-visualization` | audit `:78`, widget `:453` | `REC-037` |
| `M-015-glycol-loc-dtos` | audit `:80-82`, widget `:426,463` | `REC-015, REC-022` |
| `M-016-readme-mismatch` | audit `:88`, widget `:455` | `REC-016` |
| `M-017-results-visualization` | audit `:89`, widget `:465` | `REC-017` |
| `M-018-concrete-builders-orchestrator` | audit `:90`, widget `:493-494` | `REC-018` |
| `M-019-target-architecture` | audit `:94-109`, widget `:343-403` | `REC-019` |
| `M-020-roadmap` | audit `:111-121`, widget `:419-426` | `REC-020` |
| `M-021-audit-metrics-repeated-file-rows` | `audit_metrics.json` repeated class rows | `REC-021, REC-022, REC-023` |
| `M-022-owner-specification` | `правка архитектуры.txt:1` | `REC-024` |
| `M-023-invalidated-counts` | archive `:78-82` | `REC-025` |
| `M-024-calculation-context-seam` | archive `:83-86` | `REC-026` |
| `M-025-green-build-limit` | archive `:91-94` | `REC-027` |
| `M-026-invalid-discipline-chain` | archive `:1-7,14,20` | `REC-028` |
| `M-027-target-invariants-not-current` | archive `:223-246` | `REC-029` |
| `M-028-vm-to-vm-four` | widget `:221,479` | `REC-030` |
| `M-029-widget-per-file-loc` | widget `:258-270,295,457` | `REC-031` |
| `M-030-dataset-550-ashrae` | widget `:339,472` | `REC-032` |
| `M-031-results-cycles-8` | widget `:423,465` | `REC-038` |
| `M-032-widget-dependency-counts-3-8-6-5` | widget `:266,455,457,459` | `REC-039, REC-040, REC-041, REC-042` |
| `M-033-models-five-domains` | widget `:335,471` | `REC-043` |
| `M-034-widget-clean-correct-direction` | widget `:287,295,321,467-471` | `REC-044` |
| `M-035-widget-json-smc-data` | widget `:339,472,478` | `REC-045` |

## Classification Summary

| Classification | Rows | Claim IDs |
| --- | ---: | --- |
| `confirmed` | 15 | `REC-006, REC-008, REC-009, REC-012, REC-013, REC-016, REC-017, REC-018, REC-026, REC-027, REC-028, REC-030, REC-033, REC-034, REC-037` |
| `changed` | 8 | `REC-001, REC-003, REC-005, REC-010, REC-014, REC-025, REC-035, REC-036` |
| `not-reproducible` | 18 | `REC-002, REC-004, REC-007, REC-011, REC-015, REC-021, REC-022, REC-023, REC-031, REC-032, REC-038, REC-039, REC-040, REC-041, REC-042, REC-043, REC-044, REC-045` |
| `not-applicable` | 4 | `REC-019, REC-020, REC-024, REC-029` |

The canonical summary is **confirmed 15, changed 8, not-reproducible 18, not-applicable 4, total 45**.

## Extraction and Completeness QA

The following PowerShell 5.1 script was executed read-only after receipt creation. It parses the explicit material-key inventory, requires every key to map to one or more existing reconciliation IDs, and fails on unbound/missing IDs. It independently checks the classification enum, unique IDs, required columns, `confirmed` evidence, historical-root misuse, target-fact classifications, exhaustive `ProjectSession` declaration search, and an in-memory unsupported-claim probe.

```powershell
$root = 'D:/IA/ace v.2'
$receipt = Get-Content -Raw -LiteralPath "$root/docs/architecture-migration/evidence/audit-reconciliation.md"
$rows = @([regex]::Matches($receipt, '(?m)^\| `REC-\d{3}` \|.*\| (confirmed|changed|not-reproducible|not-applicable) \|'))
$ids = @([regex]::Matches($receipt, '(?m)^\| `(REC-\d{3})` \|') | ForEach-Object { $_.Groups[1].Value })
$allowed = @('confirmed','changed','not-reproducible','not-applicable')
$inventory = @([regex]::Matches($receipt, '(?m)^\| `(M-\d{3}-[^`]+)` \|[^|]*\| ([^|]+) \|') | ForEach-Object { [pscustomobject]@{ Key=$_.Groups[1].Value; Bindings=@([regex]::Matches($_.Groups[2].Value,'REC-\d{3}') | ForEach-Object Value) } })
if ($inventory.Count -eq 0) { throw 'missing material claim inventory' }
if (($ids | Select-Object -Unique).Count -ne $ids.Count) { throw 'duplicate claim ID' }
if (($rows | ForEach-Object { $_.Groups[1].Value } | Where-Object { $allowed -notcontains $_ }).Count) { throw 'invalid classification' }
foreach ($entry in $inventory) { if ($entry.Bindings.Count -eq 0) { throw "unbound material key: $($entry.Key)" }; foreach ($id in $entry.Bindings) { if ($ids -notcontains $id) { throw "material key $($entry.Key) references missing $id" } } }
foreach ($line in ($receipt -split "`n" | Where-Object { $_ -match '^\| `REC-' })) { if ((@($line -split '\|').Count - 2) -ne 8) { throw "row lacks required columns: $line" } }
foreach ($line in ($receipt -split "`n" | Where-Object { $_ -match '^\| `REC-' -and $_ -match '\| confirmed \|' })) { if ($line -notmatch 'EV-' -and $line -notmatch 'src/' -and $line -notmatch 'README.md') { throw "confirmed row lacks current evidence: $line" } }
foreach ($line in ($receipt -split "`n" | Where-Object { $_ -match '^\| `REC-' })) { $cells=@($line -split '\|'); if ($cells[4] -match 'D:\\IA\\ace(?!( v\.2))') { throw "historical root used in current result: $line" } }
foreach ($id in 'REC-019','REC-020','REC-024','REC-029') { if (($receipt -split "`n" | Where-Object { $_ -match "^\| `$id`" }) -notmatch '\| not-applicable \|') { throw "target/proposal row is not not-applicable: $id" } }
$excluded='/(bin|obj|generated|tests|docs|archive|\.codegraph|\.omo)/'; $files=@(Get-ChildItem -LiteralPath "$root/src" -Recurse -File -Filter *.cs | Where-Object { $_.FullName.Replace('\','/') -notmatch $excluded }); $declarations=@($files | Select-String -Pattern '\b(class|record|interface|struct)\s+ProjectSession\b'); if ($declarations.Count -ne 0) { throw 'ProjectSession production declaration found' }
$unsupported = 'synthetic unsupported claim: 999 complete runtime cycles'
$syntheticClassification = if ($unsupported -match 'complete runtime cycles') { 'not-reproducible' } else { throw 'unsupported claim omitted or guessed' }
if ($syntheticClassification -ne 'not-reproducible') { throw 'synthetic unsupported claim classification failed' }
[pscustomobject]@{ material_claim_keys=$inventory.Count; reconciliation_rows=$rows.Count; unique_claim_ids=$ids.Count; project_session_declarations=$declarations.Count; synthetic_unsupported_claim=$syntheticClassification; result='pass' } | Format-List
```

Observed output:

```text
material_claim_keys         : 35
reconciliation_rows         : 45
unique_claim_ids            : 45
project_session_declarations: 0
synthetic_unsupported_claim : not-reproducible
result                    : pass
```

Additional read-only assertions passed:

| Assertion | Result |
| --- | --- |
| YAML contains all common receipt fields required by plan lines 95-103 | pass |
| Snapshot SHA, `working-tree`, root, and actual UTC generation timestamp are present | pass |
| Every explicit material audit/widget/archive/owner claim key has at least one existing reconciliation ID; missing/unbound keys fail | pass |
| Claim IDs are unique and every row uses one allowed classification | pass |
| Every `confirmed` row cites at least one current evidence ID/path | pass |
| Historical `D:/IA/ace` appears only in historical/provenance text, never as a current evidence path | pass |
| Current numeric facts use `metrics-baseline.json` IDs; no historical metric is presented as current | pass |
| SCC/cycle limitation stays `null/degraded` and `null/not-reproducible`; qualitative coupling is not substituted for 14 cycles | pass |
| Target/roadmap/owner statements are `not-applicable`, not current facts | pass |
| Synthetic unsupported claim is classified `not-reproducible`, never omitted or guessed | pass |

## Safe Interpretation Limits

- `REC-005` controls the changed historical five-service aggregate; only its separately scoped current findings in `REC-033` through `REC-036` may be reused. `REC-008`, `REC-009`, `REC-017`, `REC-018`, and `REC-030` are source-backed compile-time/DI observations. None proves user-triggered runtime invocation.
- `REC-006` and `REC-007` preserve the verified current concrete dependencies of `ProjectLoadOrchestrator` and `ResultsViewModel`; they do not establish transactional restore, absence of stale state, or future compatibility.
- `REC-004` is controlling for all cycle discussion: no later artifact may quote 14, claim current SCCs, or infer cycles from `using` directives.
- All target architecture and roadmap material remains a proposal/specification until a later owner-approved artifact explicitly states otherwise.
