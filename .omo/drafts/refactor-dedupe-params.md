---
slug: refactor-dedupe-params
status: planning
intent: unclear
review_required: true
classify: Architecture
pending-action: write .omo/plans/refactor-dedupe-params.md
approach: behavior-preserving incremental Fowler consolidation — one canonical source per physical parameter; snapshot-DTO and direct VM→VM links migrate onto the existing contract layer (IClimateData/IConstructionData/ICalculationStateService/CalculationContext); tests green at every step.
---

# Draft: refactor-dedupe-params

## Components (topology ledger)
| id | outcome (one line) | status | evidence path |
|----|--------------------|--------|---------------|
| C1 | Characterization test net locks current numeric outputs before any move | active | tests/SnowMeltingCalculator.Tests/{Thermal,ViewModels/Hydraulics,IntegrationTests/Hydraulics} |
| C2 | Dedup constants: MinPipeSpacing/MaxPipeSpacing single source in ValidationConstants | active | src/Core/Constants/{ThermalConstants.cs:189,194; ValidationConstants.cs:128,133} |
| C3 | ThermalParameters → ThermalInputs (thermal-only); calculator takes IClimateData+IConstructionData contracts | active | src/Models/Thermal/ThermalParameters.cs; src/Services/Thermal/{IThermalCalculator.cs,ThermalCalculator.cs:417,573}; src/ViewModels/Thermal/ThermalViewModel.cs:323 |
| C4 | ThermalCalculationResult: drop echo-in fields (Pipe, PipeSpacing, R1Total, R2Total); keep only true outputs | active | src/Models/Thermal/ThermalCalculationResult.cs:147-226; readers in src/ViewModels/{Hydraulics/CircuitsViewModel.cs,Results/ResultsViewModel.cs} |
| C5 | PipeSpacing single canonical owner (ThermalViewModel via ICalculationStateService); Construction/Results observe-only; mm canon; remove /10.0 scatter | active | src/ViewModels/{Thermal/ThermalViewModel.cs:103,311; Construction/ConstructionViewModel.cs:195,634; Hydraulics/CircuitsViewModel.cs:221,399,672; Results/ResultsViewModel.cs:662,1046,1732}; src/Services/Navigation/{ICalculationStateService.cs:79; CalculationStateService.cs:97,103} |
| C6 | HydraulicInputData: keep user-input fields only; thermal/climate fields read from contracts | active | src/Models/Hydraulics/HydraulicInputData.cs; src/ViewModels/Hydraulics/CircuitsViewModel.cs:365-486 |
| C7 | CalculationContext wired as the inter-module bus; CircuitsViewModel reads climate/thermal from it, not from sibling VMs | active | src/Core/CalculationContext.cs; src/ViewModels/Hydraulics/CircuitsViewModel.cs:500-530 |
| C8 | Delete dead root ViewModels/Hydraulics/ empty dir; verify no references | active | D:\IA\ace\ViewModels\Hydraulics\ (0 entries) |

## Open assumptions (announced defaults)
| # | assumption | adopted default | rationale | reversible? |
|---|-----------|-----------------|-----------|-------------|
| D1 | Scope of refactor | Behavior-preserving consolidation, NOT rewrite | Fowler; v1.0 shipping product risk control | yes |
| D2 | One canonical source per physical parameter | Delegating props elsewhere | eliminates drift | yes |
| D3 | PipeSpacing unit canon | mm | matches ThermalCalculator physics | yes |
| D4 | PipeSpacing canonical owner | ThermalViewModel; Construction/Results observe via ICalculationStateService.PipeSpacingChanged | matches existing SetPipeSpacing wiring | yes |
| D5 | Constants single home | ValidationConstants; remove dup from ThermalConstants | one purpose per constant | yes |
| D6 | ThermalParameters shape | immutable record ThermalInputs (Mode, SupplyTemperature, DeltaT, GroundTemperature, Pipe, PipeSpacing, LambdaE); climate/construction as contracts to calculator | removes cross-domain copy | yes |
| D7 | ThermalCalculationResult echo fields | drop Pipe, PipeSpacing, R1Total, R2Total; readers get them from contracts | not real outputs (calculator echoes inputs) | PARTIAL — API removal is reversible only by re-adding fields+calc echo assignments; serialization schema MUST be preserved via T19 (a JSON name change is NOT reversible for v1.0 .snowproj users) |
| D8 | HydraulicInputData | keep GlycolType, GlycolConcentration, SupplySpacing_cm, SupplyHeatPercent, ValveType; drop copied thermal/climate fields | single model | yes |
| D9 | Dead root dir | delete ViewModels/Hydraulics/ | garbage | yes |
| D10 | Test strategy | characterization tests on current flows BEFORE refactor + green at every step; dotnet build + dotnet test gates | behavior guard | yes |

## Findings (cited - path:lines)
- ThermalParameters repeats climate/construction fields: src/Models/Thermal/ThermalParameters.cs:60-77 (AirTemperature/WindSpeed/SnowfallIntensity), :44-54 (R1Total/R2Total)
- ThermalCalculationResult echoes inputs: src/Models/Thermal/ThermalCalculationResult.cs:200-205 (Pipe, PipeSpacing); Calculator src/Services/Thermal/ThermalCalculator.cs:460-461 (R1Total/R2Total echo), :489 (SupplyTemperature echo)
- BuildThermalParameters manually copies from contracts: src/ViewModels/Thermal/ThermalViewModel.cs:323-340 + lines 267-274
- HydraulicInputData copied thermal/climate fields: src/Models/Hydraulics/HydraulicInputData.cs:22-72 (PowerUp..ColdFiveDayTemperature)
- PipeSpacing 3 competing sources: ThermalViewModel writes (ThermalViewModel.cs:103 SetPipeSpacing), ConstructionViewModel mirrors (ConstructionViewModel.cs:195,634), ResultsViewModel reads BOTH (ResultsViewModel.cs:662 vs 1046/1732)
- PipeSpacing unit scatter: /10.0 in CircuitsViewModel.cs:221,399,672; mm-cm confusion in CircuitRow.PipeSpacing_cm / CircuitData.PipeSpacingCm (serialization)
- Dup constants: ThermalConstants.cs:189,194 vs ValidationConstants.cs:128,133
- CalculationContext already central aggregation but unused as VM bus: src/Core/CalculationContext.cs:81-210 delegating props; CircuitsViewModel still injects ThermalViewModel+ClimateViewModel (CircuitsViewModel.cs:500-530)
- Dead empty dir: D:\IA\ace\ViewModels\Hydraulics\ (0 entries)

## Gate
- status: approved (user said "да" + "скажешь когда закончишь")
- pending-action: scaffold done → fill Todos → Metis → dual high-accuracy review → present
- SLUG: refactor-dedupe-params

## High-accuracy review receipts
- **Metis gap-analysis (round 1):** APPROVE_WITH_FIXES — 10 fixes cited (T6 interface gap, T3/T4/T12 red-window escape clauses, T10 tautology, T8 Trace.Assert weakness, T11/T19 QA contradiction, T18 command bug, T19 field-map, T20 runtime-judge leak, dependency-matrix reconciliation, D7 reversibility relabel). ALL 10 APPLIED.
- **Momus (Ultra, round 1):** OKAY with 3 minor gaps (T18 command bug, T6/T14 matrix contradiction, T12 temp-source ambiguity).
- **Oracle (independent, round 1):** REQUEST_CHANGES — CRITICAL T3 red window (T3 dropped 5 calculator-read fields, build red after T3) + 4 secondary findings (T8 false premise, T19 fixture not committed, T12 forward-ref, stringly-typed guard).
- **Fixes applied between rounds:** T3 → pure rename (14 fields, init-only setters, defaults); T4 → drops the 5 fields + migrates calculator to IClimateData/IConstructionData in same commit; T8 → "ADDs not replace", 2-arg SetPipeSpacing(int, string); T12 → routing via _thermalViewModel.Result.* / _climateViewModel.* (available at T12); T18 → -File flag + LEAF target; T19 → committed fixture required, BLOCKED-until-fixture-provided; T6 matrix → Blocks T9,T11 (T14 dropped).
- **Momus (Ultra, round 2):** OKAY APPROVE — all 7 round-2 fixes verified, source cross-checks pass (ThermalParameters.cs 14 fields, ThermalCalculator reads parameters.AirTemperature/.WindSpeed/.SnowfallIntensity/.R1Total/.R2Total, CalculationStateService has no Trace.Assert).
- **Oracle (independent, round 2):** OKAY APPROVE — all 5 prior findings RESOLVED, T3→T4 no-intermediate-red contract holds at every commit boundary, stringly-typed guard accepted as non-blocking architectural compromise, no new regressions.
- **Final cosmetic fix after round-2:** T6 body Parallelization "Blocks" aligned with matrix (dropped T14).
- **BOTH REVIEWERS: UNCONDITIONAL APPROVE.**

## Decisions (with rationale)

## Scope IN

## Scope OUT (Must NOT have)

## Open questions

## Approval gate
status: drafting
<!-- When exploration is exhausted and unknowns are answered, set status: awaiting-approval. -->
<!-- That durable record is the loop guard: on a later turn read it and resume at the gate instead of re-running exploration. -->
