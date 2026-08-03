---
phase: phase-0-baseline
snapshot_sha: f0d19c34ac03075d64548f1059e9c6626d3596b5
source_basis: working-tree
generated_at_utc: 2026-07-30T15:54:09.8538909Z
working_directory: D:/IA/ace v.2
commands:
  - git rev-parse --show-toplevel
  - git rev-parse HEAD
  - git branch --show-current
  - git rev-parse --abbrev-ref '@{upstream}'
  - git rev-list --left-right --count origin/master...HEAD
  - git worktree list --porcelain
  - git -c core.quotepath=false status --porcelain=v1 --untracked-files=all
  - git status --porcelain=v1 -z --untracked-files=all
  - git -c core.quotepath=false diff --name-status
  - dotnet --version
  - git rev-parse "HEAD:<path>"
  - Get-FileHash -LiteralPath <path> -Algorithm SHA256
exit_code: 0
status: pass
raw_output: inline sections "Raw command output" and "Dirty path and hash ledger" in this immutable receipt
limitations:
  - Git porcelain v1 text output quotes paths containing non-ASCII characters; the NUL-delimited porcelain invocation was also captured to preserve the actual path records.
  - The receipt is a point-in-time boundary. Later writes, including this receipt itself, are intentionally not classified as pre-existing content.
  - .git, .codegraph, .omo, bin, and obj are excluded roots and were not recursively hashed.
---

# Execution-Time Repository Snapshot

## Receipt

| Field | Value |
| --- | --- |
| Canonical root | `D:/IA/ace v.2` |
| Captured UTC | `2026-07-30T15:54:09.8538909Z` |
| HEAD / snapshot SHA | `f0d19c34ac03075d64548f1059e9c6626d3596b5` |
| Branch | `master` |
| Upstream | `origin/master` |
| Ahead / behind | `0 / 0` (`git rev-list --left-right --count origin/master...HEAD` returned `0\t0`) |
| .NET SDK | `8.0.418` |
| Dirty tracked records | 10 |
| Pre-existing untracked files | 20 |
| Total pre-write status records | 30 |
| Dossier ownership at capture | Every listed `docs/architecture-migration/` file was pre-existing untracked owner content. None is a Phase 0-created output. |

## Raw Command Output

```text
$ git rev-parse --show-toplevel
D:/IA/ace v.2

$ git rev-parse HEAD
f0d19c34ac03075d64548f1059e9c6626d3596b5

$ git branch --show-current
master

$ git rev-parse --abbrev-ref '@{upstream}'
origin/master

$ git rev-list --left-right --count origin/master...HEAD
0	0

$ git worktree list --porcelain
worktree D:/IA/ace v.2
HEAD f0d19c34ac03075d64548f1059e9c6626d3596b5
branch refs/heads/master

worktree C:/Users/Admin/AppData/Local/Temp/opencode/ace-head-baseline
HEAD a6ecae1ea6f9c848c81498b1a0d69d0fadacb4c2
prunable gitdir file points to non-existent location

$ dotnet --version
8.0.418

$ git -c core.quotepath=false diff --name-status
M	.gitignore
M	installer/SnowMeltingCalculator.iss
M	publish/SnowMeltingCalculator.deps.json
M	publish/SnowMeltingCalculator.pdb
M	src/SnowMeltingCalculator.csproj
M	Презентация/готовые/РЕХАУ Калькулятор снеготаяния — быстрый старт.pptx
D	Презентация/готовые/РЕХАУ Калькулятор снеготаяния — дизайн-шаблон.pptx
D	Презентация/готовые/РЕХАУ Калькулятор снеготаяния — презентация плюсы программы.pptx
M	Презентация/готовые/РЕХАУ Калькулятор снеготаяния — техническая презентация.pptx
M	Презентация/готовые/РЕХАУ Калькулятор снеготаяния — шпаргалка A4.pptx
```

`git status --porcelain=v1 -z --untracked-files=all` exited `0`. Its records are represented losslessly as status/path rows below; this avoids ambiguity from display quoting, spaces, and Cyrillic names.

## Exact Pre-Write Dirty Status

```text
 M .gitignore
 M installer/SnowMeltingCalculator.iss
 M publish/SnowMeltingCalculator.deps.json
 M publish/SnowMeltingCalculator.pdb
 M src/SnowMeltingCalculator.csproj
 M "Презентация/готовые/РЕХАУ Калькулятор снеготаяния — быстрый старт.pptx"
 D "Презентация/готовые/РЕХАУ Калькулятор снеготаяния — дизайн-шаблон.pptx"
 D "Презентация/готовые/РЕХАУ Калькулятор снеготаяния — презентация плюсы программы.pptx"
 M "Презентация/готовые/РЕХАУ Калькулятор снеготаяния — техническая презентация.pptx"
 M "Презентация/готовые/РЕХАУ Калькулятор снеготаяния — шпаргалка A4.pptx"
?? .opencode/commands/architecture-approve.md
?? .opencode/commands/architecture-draft.md
?? .opencode/commands/architecture-plan.md
?? .opencode/commands/architecture-resume.md
?? .opencode/commands/architecture-start.md
?? AGENTS.md
?? docs/architecture-migration/AGENTS.md
?? docs/architecture-migration/TASK_CONTEXT.md
?? docs/architecture-migration/architecture_audit.md
?? docs/architecture-migration/architecture_widget.html
?? docs/architecture-migration/archive/.gitkeep
?? docs/architecture-migration/archive/phase-0-baseline.invalidated-explore-chain.md
?? docs/architecture-migration/audit_metrics.json
?? docs/architecture-migration/evidence/.gitkeep
?? docs/architecture-migration/maps/.gitkeep
?? docs/architecture-migration/plans/.gitkeep
?? docs/architecture-migration/plans/phase-0-baseline.md
?? "docs/architecture-migration/правка архитектуры.jpg"
?? "docs/architecture-migration/правка архитектуры.txt"
?? "Презентация/готовые/РЕХАУ Калькулятор снеготаяния — обзорная презентация.pptx"
```

## Dirty Path and Hash Ledger

`HEAD blob ID` is resolved only for tracked paths. `deleted` means the path did not exist at capture; `not-applicable` means the untracked path has no HEAD blob.

| Status | Path | HEAD blob ID | Current SHA-256 |
| --- | --- | --- | --- |
| ` M` | `.gitignore` | `2e8d4043a9d0305de2e27c7afb0a4f5214765db5` | `D10AD9C4EC1E58BDDA653D97F5CDEF67A0DF3FE6A0C0CC3CDD0AFDD4413213B5` |
| ` M` | `installer/SnowMeltingCalculator.iss` | `c3bb4bf0722f0ec9a033aae781d001bf85da90a7` | `B31B715DFEBDA9986EAFF0D2682B465B044CCE85EF9E209EE2A905422C48A37D` |
| ` M` | `publish/SnowMeltingCalculator.deps.json` | `ca4dab55215d3729dcf1179a4549bdb6a718c3ba` | `5138D80AE4312E891118C9FB9302F96DE472F7B8E9F4796088C82BE2D158671A` |
| ` M` | `publish/SnowMeltingCalculator.pdb` | `a9ae7f11a1b2a191a877fb1b45bdf091c77dc048` | `58781E6DBD3CCF946B3BC49A07C96C7CBF8D49230BB440AC7A9491ADE504C864` |
| ` M` | `src/SnowMeltingCalculator.csproj` | `e2e04a5e254f62eabbb24c9cf3b4e3ae4dfc2a10` | `8BED932038DB57DF26C9664B3A533189D28DAC1DAF9796C717E0D8D3C20DC5D7` |
| ` M` | `Презентация/готовые/РЕХАУ Калькулятор снеготаяния — быстрый старт.pptx` | `b978caeb7f7a8ad1a26152e05ce6fbbb3c52592b` | `B9F72C7826A13BB1D137AAE5DC59631DAB76A122B1DD39613EBCE202D9860430` |
| ` D` | `Презентация/готовые/РЕХАУ Калькулятор снеготаяния — дизайн-шаблон.pptx` | `6e9fcbb502dd19dd1bc058923ab951cff2b28c7c` | `deleted` |
| ` D` | `Презентация/готовые/РЕХАУ Калькулятор снеготаяния — презентация плюсы программы.pptx` | `f40c6810ee03c8b107e829d47894107f6e765db7` | `deleted` |
| ` M` | `Презентация/готовые/РЕХАУ Калькулятор снеготаяния — техническая презентация.pptx` | `7e6fab1da4fa31b7b9e5bbd0a58e1d9d9f7a93ec` | `990F3E0873B0BDA275829AFD91948952FF3D020B07108E6BCD930EB84766E2C3` |
| ` M` | `Презентация/готовые/РЕХАУ Калькулятор снеготаяния — шпаргалка A4.pptx` | `3c94a3fab5c4a62b0de6828c87793db09a161ebb` | `358A14BC1A9826D0EC2FA62BD3977A40F3D3D80CCE33D7B87CCA50C32CACD119` |
| `??` | `.opencode/commands/architecture-approve.md` | `not-applicable` | `7C19046A057449A84863C6EA8BCFB8C86896BA453A4FB955F645C4AA41D5DF24` |
| `??` | `.opencode/commands/architecture-draft.md` | `not-applicable` | `0A9ADA6964A745A8002108633692350C164783166E8EA4081BA5867D8019A2B7` |
| `??` | `.opencode/commands/architecture-plan.md` | `not-applicable` | `AE9EE8E726826219A21B7B554615FD850C44AC5FEE3D4CBBA5655D12A9B28169` |
| `??` | `.opencode/commands/architecture-resume.md` | `not-applicable` | `1871B83ED2407DC125E5B7E5E97712D61177880F5E28ABD929F50EFCD0781283` |
| `??` | `.opencode/commands/architecture-start.md` | `not-applicable` | `F1377450F812AD0CFE9F214C1AE58672EDA8B55EBFBF7D9555884D5119CA1FAA` |
| `??` | `AGENTS.md` | `not-applicable` | `AD77BBAC1EB500786D196F52FBE52E637FA7DE7332E9AB4E23FF7C6FED8FD8FC` |
| `??` | `docs/architecture-migration/AGENTS.md` | `not-applicable` | `F852D7E5736790155B4F87252E55E09756E3E5AE162272A103A35914019D89A3` |
| `??` | `docs/architecture-migration/TASK_CONTEXT.md` | `not-applicable` | `06219B435632E52C86498D566C4EFCF04A2A88512E75ECC1269161A3A7FB55AC` |
| `??` | `docs/architecture-migration/architecture_audit.md` | `not-applicable` | `E2608604D550D6EBF9C4E7A9197D425C876AD7727D5D2BAD2A43A49F4A5F2296` |
| `??` | `docs/architecture-migration/architecture_widget.html` | `not-applicable` | `D6F1925E188FD9E8D1485D44F040C41781580A072B5A2DA8EA7FE2B4752930CA` |
| `??` | `docs/architecture-migration/archive/.gitkeep` | `not-applicable` | `E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855` |
| `??` | `docs/architecture-migration/archive/phase-0-baseline.invalidated-explore-chain.md` | `not-applicable` | `B03F920FD04AF56E691B2C22322B9ABF61C1F8B846DAA8277ED4C84B75FB7A2C` |
| `??` | `docs/architecture-migration/audit_metrics.json` | `not-applicable` | `80805490E389C602F5158DEE45BDEC6B6F4C58CC8C8B7B4F9FEA447E4239C463` |
| `??` | `docs/architecture-migration/evidence/.gitkeep` | `not-applicable` | `E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855` |
| `??` | `docs/architecture-migration/maps/.gitkeep` | `not-applicable` | `E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855` |
| `??` | `docs/architecture-migration/plans/.gitkeep` | `not-applicable` | `E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855` |
| `??` | `docs/architecture-migration/plans/phase-0-baseline.md` | `not-applicable` | `BB6F92470A4BF786FE90F8A86F2B34F3B04BEE3C5AC2654C9A45AEB75F87CC6E` |
| `??` | `docs/architecture-migration/правка архитектуры.jpg` | `not-applicable` | `473A60DF6133B3D4459F5914721471B01160B19EBCBB4BDF05E43F1FD9214F28` |
| `??` | `docs/architecture-migration/правка архитектуры.txt` | `not-applicable` | `2BAC467D565D5BAA5B603366F7CE7C10F646DFAEA74FD384263E589D518E0747` |
| `??` | `Презентация/готовые/РЕХАУ Калькулятор снеготаяния — обзорная презентация.pptx` | `not-applicable` | `9F82A062FC61EA3CEEFBC283E846A414196A580744A3350701B1BC9C6372D0A1` |

## Excluded Roots

| Root | Capture state | Ledger value | Reason |
| --- | --- | --- | --- |
| `.git/` | present | `unhashed-excluded` | Git administrative data, excluded by Todo 1. |
| `.codegraph/` | present | `unhashed-excluded` | Generated index data, excluded by Todo 1. |
| `.omo/` | present | `unhashed-excluded` | Orchestration data, excluded by Todo 1. |
| `bin/` | absent | `unhashed-excluded` | Generated build output root, excluded by Todo 1. |
| `obj/` | absent | `unhashed-excluded` | Generated intermediate-output root, excluded by Todo 1. |

No other generated directory was present or excluded. Every untracked status record was a file and was individually enumerated; no directory-valued untracked status row was flattened.

## Agent-Executed QA

### Happy Path: Data-Surface Recheck

PowerShell re-ran the canonical Git commands, parsed the NUL-delimited status records, and recomputed `Get-FileHash -LiteralPath <path> -Algorithm SHA256` for every present ledger path.

| Assertion | Result |
| --- | --- |
| Parsed canonical root equals `D:/IA/ace v.2` | pass |
| HEAD matches `^[0-9a-f]{40}$` | pass |
| 30 pre-write status records each map to exactly one ledger row | pass |
| 28 present ledger paths recompute to the recorded SHA-256 | pass |
| 2 deleted tracked paths remain absent and are recorded as `deleted` | pass |
| Canonical plan hash equals owner-supplied `BB6F92470A4BF786FE90F8A86F2B34F3B04BEE3C5AC2654C9A45AEB75F87CC6E` | pass |
| No allow-listed Phase 0 output that did not exist at capture appears in the pre-write ledger; pre-existing owner dossier paths that are also allow-listed, including `TASK_CONTEXT.md` and the canonical plan, are recorded as pre-existing untracked owner content | pass |

### Failure Path: In-Memory Parser Inputs

No repository files were created or altered. Parser inputs were constructed only in memory and evaluated against the same status/path rules.

| Probe input | Expected safe result | Result |
| --- | --- | --- |
| ` D deleted path.txt` | Retain ` D`, path, HEAD blob lookup, and `deleted` hash state | pass |
| `?? docs/архив/файл с пробелом.txt` | Retain Unicode and spaces as a literal path | pass |
| upstream lookup exit non-zero | Record upstream and ahead/behind as `<absent>` without fabricating counts | pass |
| `?? directory/` | Reject as non-file / mark receipt `fail`; never flatten it to a hash row | pass |

## UltraQA Probes

| Class | Applicability | Probe and result |
| --- | --- | --- |
| `dirty_worktree` | applicable | Compared every pre-write dirty status with a single path ledger and hashed every present path. Result: pass. |
| `stale_state` | applicable | Bound receipt identity to live root, HEAD, branch, upstream, SDK, timestamp, and re-ran hashes after capture. Result: pass. |
| `misleading_success_output` | applicable | Required exit code `0` plus parsed root/HEAD/status count/hash assertions; did not treat command text or warning-free output as success. Result: pass. |
| `concurrency` | N/A | No concurrent execution, locking, or shared mutable process was exercised. |
| `idempotency` | N/A | This is a point-in-time evidence capture, not a repeatable mutation operation. |
| `resource_leak` | N/A | No long-lived process, stream, or resource-owning code was run. |
| `security` | N/A | The task used only local read/hash commands and created no trust boundary. |
| `performance` | N/A | No performance-sensitive runtime behavior is in scope. |
| `accessibility` | N/A | No user interface was created or changed. |
| `localization` | N/A | Cyrillic path preservation was tested as parsing integrity, not a localized product surface. |

## Cleanup and Boundary

No cleanup action was needed or performed. No Git mutation, build, test, installation, staging, restore, or deletion command was used. The only file created by this task is this receipt; it is intentionally absent from the pre-write dirty ledger and must be classified as the Todo 1 Phase 0 output by later verification.

## Risks

- The working tree is already dirty and contains a prunable secondary Git worktree entry. Later tasks must compare against this ledger rather than infer ownership from status alone.
- The snapshot is valid only at its captured UTC instant. Any later owner change requires an explicit comparison against this ledger, not replacement of the pre-write records.
