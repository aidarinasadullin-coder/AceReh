# Todo 4 DI Negative Probe

Plan SHA-256: `0D48A757DDD1B98384D131D76C7AF5220206260BF853FADF12BA859329FC8D38`

The canonical composition has no independent `IProjectSessionHydraulicsState`
registration. The negative probe temporarily added this line to
`CreateApplicationServices()` in `DiRegistrationTests.cs`:

```csharp
services.AddSingleton<IProjectSessionHydraulicsState>(new ProjectSessionHydraulicsState());
```

## Temporary Registration Run

Command:

```text
dotnet test tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~DiRegistrationTests"
```

Result: `1 failed / 24 passed / 0 skipped / 25 total`.

Exact failing assertion:

```text
HydraulicsState_IsNotResolvableAsIndependentService_FromBuiltProvider
Assert.That(provider.GetServices<IProjectSessionHydraulicsState>(), Is.Empty)
Expected: <empty>
But was:  < <SnowMeltingCalculator.Services.Project.ProjectSessionHydraulicsState> >
```

The failure was at `DiRegistrationTests.cs:line 284`, with the assertion body at
line 286. Raw TRX: `task-4-di-negative.trx`.

## Reverted Registration Run

The temporary registration was removed before the green rerun. The test
assembly was rebuilt, then the same filtered command was run again.

Result: `0 failed / 25 passed / 0 skipped / 25 total`.

Raw TRX: `task-4-di-green-rerun.trx`.
