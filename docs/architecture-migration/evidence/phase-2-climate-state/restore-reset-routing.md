# Phase 2 Task 7: Restore And Reset Routing

Date: 2026-08-11

## Scope

- `ProjectLoadOrchestrator` restores project climate state through
  `IProjectSessionClimateState.ApplyProjectSnapshot(..., ClimateMutationOrigin.Load)`.
- Orchestrator and new-calculation reset use
  `IProjectSessionClimateState.ResetToDefaults(ClimateMutationOrigin.Reset)`.
- `ClimateViewModel.SearchQuery` remains an allowed UI-only restore/reset write.
- Task 8 persistence/results snapshot reads, `.smc` schema, and wire format were not changed.

## Guard Transition

The first affected gate failed because `ClimateStateLegacyStoreGuardTests` still
expected the Task 6 legacy restore calls. The guard now forbids
`BeginLoadProject`, `EndLoadProject`, `ClimateViewModel.Reset`,
`SyncToClimateData`, `HasUserModifications = false`, and direct ClimateViewModel
project-value assignments in `ProjectLoadOrchestrator`. It requires the canonical
load and reset boundaries and continues to list `ResultsViewModel` snapshot reads
as the pending Task 8 projection boundary.

## Commands And Results

1. `dotnet build "src\SnowMeltingCalculator.csproj" -c Debug`
   - PASS: 0 warnings, 0 errors.
2. `dotnet test "tests\SnowMeltingCalculator.Tests" --filter "FullyQualifiedName~ProjectLifecycleFlowCharacterizationTests|FullyQualifiedName~ResetOrchestrationTests|FullyQualifiedName~ResultsViewModelOpenProjectTests"`
   - PASS before the guard correction: 39 passed, 1 skipped, 0 failed.
3. The prior affected guard run failed only because the source-text guard still
   positively expected the removed Task 7 bypass strings.
4. The first corrected guard run failed because the source scan found the two
   intentional `SearchQuery` assignments (restore and reset) while the assertion
   listed one; the guard was corrected to enumerate both UI-only writes.
5. `dotnet test "tests\SnowMeltingCalculator.Tests" --filter "FullyQualifiedName~ClimateStateLegacyStoreGuard|FullyQualifiedName~ClimateMultiplicity|FullyQualifiedName~ClimateViewModelTests|FullyQualifiedName~ClimateToHydraulicsIntegrationTests|FullyQualifiedName~ThermalViewModelTests.ClimateDataChanged"`
   - PASS: 54 passed, 0 failed, 0 skipped.
6. `dotnet test "tests\SnowMeltingCalculator.Tests" --filter "FullyQualifiedName~ProjectLifecycleFlowCharacterizationTests|FullyQualifiedName~ResetOrchestrationTests|FullyQualifiedName~ResultsViewModelOpenProjectTests"`
   - PASS: 39 passed, 0 failed, 1 pre-existing skipped test.
7. Final source scan in `ProjectLoadOrchestrator.cs` and `MainViewModel.cs` for
   legacy guard/sync/modification-flag/direct ClimateViewModel project-value
   assignments
   - PASS: no matches. The only remaining orchestrator ClimateViewModel writes
   are two `SearchQuery` UI assignments, explicitly allowed by the guard.

## Tooling Note

C# LSP diagnostics remain unavailable through the known workspace-root harness
issue. `dotnet build` and the targeted `dotnet test` filters are the executable
correctness gates for this task.
