# Phase 2 Task 4 — ClimateState API Evidence Receipt

Date: 2026-08-06
Session: continuation of current Sisyphus session
Plan: `docs/architecture-migration/plans/phase-2-climate-state.md`
Plan SHA-256: `D79D7C46CA73EBCFF161164FC41EE3EB041B9B172631944CB611FA42BA998A6B`

## Files changed

Production (modified):
- `src/Services/Project/IProjectSession.cs` — added `IProjectSessionClimateState ClimateState { get; }`
- `src/Services/Project/ProjectSession.cs` — added backing field and constructor exposing the retained state object

Production (new):
- `src/Services/Project/ClimateMutationOrigin.cs`
- `src/Services/Project/ClimateStateSnapshot.cs`
- `src/Services/Project/ClimateStateChangedEventArgs.cs`
- `src/Services/Project/ClimateMutationResult.cs`
- `src/Services/Project/ClimateEdit.cs`
- `src/Services/Project/IProjectSessionClimateState.cs`
- `src/Services/Project/ProjectSessionClimateState.cs`

Tests:
- `tests/SnowMeltingCalculator.Tests/Services/Project/ClimateStateTests.cs` (new)
- `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectSessionTests.cs` (modified: added `ClimateState_IsOwnedBySession`)

## Commands run

```powershell
dotnet build src/SnowMeltingCalculator.csproj -c Debug
dotnet test tests\SnowMeltingCalculator.Tests --filter "FullyQualifiedName~ClimateStateTests|FullyQualifiedName~ProjectSessionTests"
dotnet test tests\SnowMeltingCalculator.Tests --filter "FullyQualifiedName~ClimateStateLegacyStoreGuard|FullyQualifiedName~ClimateMultiplicity|FullyQualifiedName~ClimateViewModelTests"
```

## Results

- `dotnet build src/SnowMeltingCalculator.csproj -c Debug`: exit 0, 0 warnings, 0 errors.
- `dotnet test ...ClimateStateTests|ProjectSessionTests`: 44 passed, 0 failed, 0 skipped.
- `dotnet test ...ClimateStateLegacyStoreGuard|ClimateMultiplicity|ClimateViewModelTests`: 33 passed, 0 failed, 0 skipped.

## LSP diagnostics

C# LSP diagnostics could not be run for the changed files because the harness reports `LSP file path must be inside request cwd` for every file. This is the known pre-existing harness path issue; `dotnet build` and `dotnet test` served as the executable C# correctness gates.

## Scope confirmation

- No changes to `ClimateViewModel`, `ClimateData`, `CalculationContext`, `ProjectLoadOrchestrator`, `ResultsViewModel`, DI registrations, architecture maps, generated model/widget, Phase 1 docs, `.smc` files, packages, build/publish artifacts, or unrelated dirty paths.
- Pre-existing dirty file `src/Services/Project/ProjectFileService.cs` remains modified from before this task; this task did not edit it.
- Task 5 projection hardening was not started.

## Behavior verified

- `ProjectSession.ClimateState` returns one retained `ProjectSessionClimateState` instance owned by that session.
- Mutations return `ClimateMutationResult` carrying the origin and old/new snapshots.
- `Changed` event is raised only when the snapshot actually changes.
- `User` origin changed mutations call `ProjectSession.MarkDirty()` and set `HasUserModifications = true`.
- Non-`User` origins (`Load`, `Reset`, `Restore`, `SystemApply`, `Initialization`) do not dirty the session and clear user-modification semantics.
- Invalid scalar edits return `IsValid=false`, `IsChanged=false`, an error list, and leave the state unchanged.
- No-op mutations (same city, same scalar, same project snapshot, repeated reset) return `IsChanged=false` and emit no `Changed` event.
- Snapshot record equality covers all 11 project Climate fields.

## Task 5 not started

Projection hardening (`ClimateData` as read-only/forwarding compatibility surface) and adapter rewiring are explicitly out of scope for this receipt and were not started.
