# Task 2 — lifecycle-owner and compatibility contract tests in RED

## Scope

This evidence records the expected RED state for Phase 1 Task 2. Only test files
were added; no production implementation of `IProjectSession`/`ProjectSession`
was created. The failures are intentional and will turn GREEN in Task 4 when the
canonical lifecycle owner is implemented.

## Changed files

- `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectSessionTests.cs`
- `tests/SnowMeltingCalculator.Tests/Services/Project/ProjectSessionLegacyStoreGuardTests.cs`

No files under `src/` were modified.

## Command run

```bash
dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --filter "FullyQualifiedName~ProjectSession|FullyQualifiedName~ProjectStateServiceTests|FullyQualifiedName~CalculationStateServiceGuardTests"
```

## Result

Build/test exited nonzero. The build fails before any test executes because the
future production API referenced by the tests does not yet exist.

## Key failure

```text
D:\IA\ace v.2\tests\SnowMeltingCalculator.Tests\Services\Project\ProjectSessionTests.cs(16,17): error CS0246: Не удалось найти тип или имя пространства имен "IProjectSession" (возможно, отсутствует директива using или ссылка на сборку).
```

## Why this RED is expected

- `IProjectSession` and `ProjectSession` belong to Task 4 (GREEN implementation).
- Task 2 is characterization-first TDD: the tests express the decision-complete
  contract for lifecycle ownership, idempotent dirty state, identity/path
  mutation semantics, restore-guard behavior, and DI compatibility before the
  production types exist.
- `ProjectSessionLegacyStoreGuardTests` is written to fail at runtime once the
  compile succeeds while `ProjectStateService` and `CalculationStateService`
  still hold duplicate mutable lifecycle backing fields outside `ProjectSession`.

## Status

Task 2 is intentionally RED. Task 3 has not been started. Task 4 will implement
`IProjectSession`/`ProjectSession` and the forwarding-only compatibility
adapters, at which point these tests are expected to compile and pass.
