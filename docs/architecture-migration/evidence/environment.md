---
phase: phase-0-baseline
snapshot_sha: f0d19c34ac03075d64548f1059e9c6626d3596b5
source_basis: working-tree
generated_at_utc: 2026-07-30T16:00:00Z
working_directory: D:/IA/ace v.2
commands:
  - git rev-parse --show-toplevel
  - git rev-parse HEAD
  - git -c core.quotepath=false status --porcelain=v1 --untracked-files=all
  - dotnet --info
  - dotnet --list-sdks
  - dotnet --list-runtimes
  - dotnet sln "SnowMeltingCalculator.sln" list
  - Test-Path -LiteralPath global.json
exit_code: 0
status: pass
raw_output: inline Environment capture section
limitations:
  - This receipt describes the local execution environment and does not establish runtime behavior.
  - Repository starts dirty; integrity is reconciled against evidence/repository-snapshot.md before and after Todo 2.
---

# Environment Baseline

## Identity

| Field | Value |
| --- | --- |
| Canonical root | `D:/IA/ace v.2` |
| HEAD / snapshot SHA | `f0d19c34ac03075d64548f1059e9c6626d3596b5` |
| `global.json` | absent |
| .NET SDK | `8.0.418` |
| MSBuild | `17.11.48+02bf66295` |
| Host | `.NET 8.0.24`, `win-x64` |
| OS | Windows `10.0.19045` |

## Installed SDKs and Runtimes

```text
$ dotnet --list-sdks
8.0.418 [C:\Program Files\dotnet\sdk]

$ dotnet --list-runtimes
Microsoft.AspNetCore.App 5.0.5 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
Microsoft.AspNetCore.App 8.0.24 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
Microsoft.NETCore.App 5.0.5 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
Microsoft.NETCore.App 8.0.24 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
Microsoft.WindowsDesktop.App 5.0.5 [C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App]
Microsoft.WindowsDesktop.App 8.0.24 [C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App]
```

## Solution Projects

```text
$ dotnet sln "SnowMeltingCalculator.sln" list
Проекты
-------
src\SnowMeltingCalculator.csproj
tests\SnowMeltingCalculator.Tests\SnowMeltingCalculator.Tests.csproj
```

## `dotnet --info` Summary

```text
SDK: 8.0.418 (commit 5854a779c1)
Workload version: 8.0.400-manifests.e5a1450a
MSBuild: 17.11.48+02bf66295
Host: 8.0.24 x64 (commit b3b35ce80e)
global.json file: Not found
Workloads: none installed
```

## Pre-Execution Boundary

The live root and HEAD match `evidence/repository-snapshot.md`. The pre-existing status set contains the ten tracked records and the dossier records captured there, including the protected pre-existing modified path `src/SnowMeltingCalculator.csproj`. No product, test, project, fixture, configuration, or Git mutation was performed to collect this receipt.

## UltraQA Probes

| Class | Applicability | Result |
| --- | --- | --- |
| `dirty_worktree` | applicable | Pre-run status was re-read and is reconciled to Todo 1's ledger. |
| `stale_state` | applicable | Live root and HEAD match the snapshot binding. |
| `hung/long commands` | applicable | Build/test use bounded command timeouts; measured durations are recorded in their receipts. |
| `flaky tests` | applicable only after test launch | Assessed from the single prescribed test run; no retry will be used. |
| `misleading_success_output` | applicable | Exit codes and parsed artifacts, rather than output wording alone, determine results. |
| `concurrency` | N/A | Todo 2 runs its dependent build then test serially. |
| `idempotency` | N/A | This is a timestamped baseline receipt, not a mutation API. |
| `resource_leak` | N/A | No persistent process is created by environment inspection. |
| `security` | N/A | Only local SDK and repository inspection is in scope. |
| `performance` | N/A | No performance claim is made. |
| `accessibility` | N/A | No UI is changed. |
| `localization` | N/A | No localized product surface is changed. |
