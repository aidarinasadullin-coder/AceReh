# Task 4 — Attach exactly one ThermalState to ProjectSession and prove runtime DI identity

Phase: `phase-4-thermal-state` · Todo 4 (frozen plan `docs/architecture-migration/plans/phase-4-thermal-state.md`, lines 386–394; binding decision DEC-T01 lines 61–88)
Base: branch `master`, HEAD `6a5a96f1763dd952c8d772ecd1d2536eb3b804cf` · Date: 2026-08-23
Verdict: **GREEN — one session-owned ThermalState exposed on `IProjectSession`; DI identity proofs 7/7 green; full Release suite failed=0 with baseline-only NotExecuted.**

## 1. Write-set (diff summary per file)

| File | Change | Kind |
|---|---|---|
| `src/Services/Project/IProjectSession.cs` | Added `IProjectSessionThermalState ThermalState { get; }` between `ConstructionState` and `ProjectNumber`, mirroring the existing slice-property style | MODIFIED production (+7 lines) |
| `src/Services/Project/ProjectSession.cs` | Added `private readonly ProjectSessionThermalState _thermalState;` field, expression-bodied `public IProjectSessionThermalState ThermalState => _thermalState;` property (`/// <inheritdoc />`), and ctor assignment `_thermalState = new ProjectSessionThermalState();` — byte-for-byte the Climate/Construction creation pattern (ctor-created readonly field, no lazy null-coalescing) | MODIFIED production (+5 lines) |
| `src/Configuration/ServiceCollectionExtensions.cs` | **ZERO edits.** Nothing to keep compiling: no thermal-state registration exists or is permitted (DEC-T01). The state is reachable only through the owning session | UNTOUCHED |
| `tests/SnowMeltingCalculator.Tests/Configuration/DiRegistrationTests.cs` | Added 7 `[Category("ThermalState")]` tests + descriptor-guard helper + two synthetic cycle fixture classes (see §4) | MODIFIED test |
| `docs/architecture-migration/evidence/phase-4-thermal-state/task-4/` | `allowed-hunks.json`, `protected-pre.json`, `protected-post.json`, `TestResults/phase-4-di-debug.trx`, `TestResults/task-4-full-release.trx`, `trx-di-debug.json`, `trx-full-release.json`, this receipt | NEW evidence |

No VM, orchestrator, Results, CalculationStateService, CalculationContext, DI or csproj file was touched. No independent DI registration of the state was added anywhere; no service locator introduced.

## 2. DEC-T01 conformance

- `IProjectSession` exposes `IProjectSessionThermalState ThermalState { get; }` — exact contract name/type from DEC-T01 line 63.
- `ProjectSession` creates **exactly one** instance in its constructor; every access returns the reference-identical instance (proved by tests below).
- The state is **not independently registered in DI**: zero descriptors for `IProjectSessionThermalState`/`ProjectSessionThermalState` after `AddApplicationServices()`; provider self-validation confirms neither service type resolves.
- Defaults are untouched Todo-3 behavior: the class is created with its parameterless constructor exactly as `ProjectSessionThermalStateTests` does; no wiring into context/dirty/compatibility consumers (Todos 5–10 own those seams).

## 3. Interface/alias identity table (inventory from `ServiceCollectionExtensions.AddResultsModule`)

All aliases resolve to the single owning `ProjectSession` instance (singleton):

| Resolution path | Registration form | Same instance as concrete `ProjectSession` | `.ThermalState` access |
|---|---|---|---|
| `ProjectSession` | `AddSingleton<ProjectSession>()` | — (identity root) | yes |
| `IProjectSession` | factory → `GetRequiredService<ProjectSession>()` | ✓ (`ThermalState_ResolvesReferenceIdenticalThroughEverySessionAlias`) | yes |
| `IProjectInfoService` | factory → `GetRequiredService<ProjectSession>()` | ✓ (same test) | n/a (narrow legacy view) |
| `IProjectStateService` | factory → `GetRequiredService<ProjectSession>()` | ✓ (same test) | n/a (legacy view; `IProjectStateService : IProjectInfoService`) |
| `IMarkDirtyService` | factory → `GetRequiredService<ProjectSession>()` | ✓ (same test) | n/a (legacy view) |
| `IProjectSessionClimateState` | **not registered** (unchanged Phase 2 invariant) | reached only via `session.ClimateState` | — |
| `IProjectSessionConstructionState` | singleton projection of `session.ConstructionState` (unchanged Phase 3 seam) | ✓ unchanged | — |
| `IProjectSessionThermalState` / `ProjectSessionThermalState` | **NOT registered** (DEC-T01) | reached only via `session.ThermalState` | — |

Implementer inventory (blocker protocol check): `ProjectSession` is the **only** class implementing `IProjectSession` in `src/` and there are no test fakes implementing it, so adding the interface member broke nothing outside the allow-list.

## 4. Test cases added (all `[Category("ThermalState")]`, filter `FullyQualifiedName~DiRegistrationTests&TestCategory=ThermalState`)

1. `ThermalLifecycleDescriptors_HaveNoIndependentRegistration` — descriptor enumeration: zero descriptors for interface/concrete state types; `ProjectSession`/`IProjectSession` remain Singleton.
2. `ThermalState_ResolvesReferenceIdenticalThroughEverySessionAlias` — all five aliases `SameAs` concrete session; repeated `session.ThermalState` accesses reference-identical; both resolution paths (concrete type / `IProjectSession`) expose the same instance; instance is `ProjectSessionThermalState`.
3. `ThermalState_IsNotResolvableAsIndependentService_FromBuiltProvider` — provider self-validation: `GetServices<IProjectSessionThermalState>()` empty; `GetService<ProjectSessionThermalState>()` null; state reachable only through the session.
4. `ThermalState_IsOnePerSession_SingletonAcrossScopes_DistinctAcrossSessions` — child scope resolves same singleton session/state; a second composition root yields a distinct session with a distinct state, still exactly one per session lifetime.
5. `ThermalState_DuplicateIndependentRegistration_IsFlaggedByDescriptorGuard` — NEGATIVE (synthetic): canonical composition passes the descriptor-count guard (=0); a defect model appending two independent state descriptors is flagged (>1). Guard inspects registration descriptors only — no service locator.
6. `ThermalState_ConstructorCycle_IsRejectedByContainerWithoutServiceLocator` — NEGATIVE (synthetic): an independent state registration whose implementation ctor depends on a consumer requiring the state forms a declared implementation-type cycle; the container rejects resolution of BOTH endpoints with `InvalidOperationException` ("A circular dependency was detected") before any instance exists. Rationale for the implementation-type model: MS DI detects cycles in the call-site graph; a factory-lambda cycle recurses at runtime instead of being rejected, so the factory variant cannot prove *rejection* semantics honestly. Production wiring contains no such registration at all (case 1/3 prove absence).
7. `ThermalState_Addition_PreservesClimateAndConstructionIdentities` — regression: climate has no independent registration and stable identity; construction state/projection identities unchanged; all three legacy aliases still identical to the session; thermal follows the same ownership pattern.

## 5. Gate results (commands verbatim; verifier scripts require `pwsh` 7)

| Gate | Command (abbreviated) | Exit | Result |
|---|---|---|---|
| Preflight | `git rev-parse --show-toplevel` / `HEAD` / `branch --show-current` | 0 | root=`D:/IA/3ace v.2`, HEAD=`6a5a96f…` (base), branch=`master` |
| G0 pre | `pwsh verify-protected-baseline.ps1 -Baseline …task-1/baseline-manifest.json -AllowedHunks …task-4/allowed-hunks.json -EvidenceRoot docs/architecture-migration/evidence -Output …task-4/protected-pre.json` | 0 | `drift=21 protected_mismatch_count=0 allowed_hunk_count=11` (2 inherited task-2 + 6 task-3 + 3 task-4 files) |
| G1 build | `dotnet build src/SnowMeltingCalculator.csproj -c Debug --nologo` then `-c Release` | 0 / 0 | **0 warnings, 0 errors** both configs |
| G2 happy | `dotnet build tests… -c Debug` (0w/0e) then `dotnet test … -c Debug --filter "FullyQualifiedName~DiRegistrationTests&TestCategory=ThermalState" --logger "trx;LogFileName=phase-4-di-debug.trx" --results-directory …task-4/TestResults` + `parse-trx.ps1 -InputDirectory …` | 0 / 0 | **7 total / 7 passed / failed=0 / NotExecuted=0** → `trx-di-debug.json` |
| G3 full Release | `dotnet build tests… -c Release` (0w/0e) then `dotnet test … -c Release --no-build --logger "trx;LogFileName=task-4-full-release.trx" …` + `parse-trx.ps1 -InputFile …task-4-full-release.trx` | 0 / 0 | **1860 total / 1857 passed / failed=0 / NotExecuted=3** → `trx-full-release.json` |
| G4 post | same verifier as G0 with `-Output …task-4/protected-post.json` | 0 | `drift=21 protected_mismatch_count=0 allowed_hunk_count=11` |

Reconcile arithmetic vs previous boundary (task-3): full Release 1853 total / 1850 passed + 7 new DiRegistrationTests = **1860 / 1857** ✓. NotExecuted identities are EXACTLY the Todo-1 baseline three (`RegenerateCircuitsBaseline`, `RegenerateBaseline`, `ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`) — verified by exact name, not count alone.

Note: `parse-trx.ps1` rejects duplicate identities across files in one directory, so the Release parse uses `-InputFile` on the exact TRX (the Debug TRX lives in the same task-owned directory). One earlier G2 attempt used a factory-lambda cycle model that recursed at runtime instead of being rejected; it was redesigned to the implementation-type call-site graph model above before any gate was recorded.

TRX SHA-256:

```
C5D728643A17D33AA2791A522C6B97F45510901AC0C98FC04BE893CA6B115808  TestResults/phase-4-di-debug.trx
341BC36DED90E49B8E3417B41BDD404BD5662A4D71C9508465D2D86A12452E49  TestResults/task-4-full-release.trx
```

## 6. Worktree confirmation

Final `git status --porcelain=v1` contains ONLY: the three modified allow-listed files (`IProjectSession.cs`, `ProjectSession.cs`, `DiRegistrationTests.cs`), inherited task-2 delta (`ThermalViewModelTests.cs`), untracked task-3 files, additions under `docs/architecture-migration/evidence/phase-4-thermal-state/` (Todo-1 scripts/task-1/2/3/4 evidence), and the two pre-existing dirty control files (`STATE.json`, plan) whose hashes match the Todo-1 baseline manifest (no drift, untouched by this task). **Zero out-of-allow-list diffs**; no git staging/commit/reset performed.
