---
phase: phase-0.5-model-driven-architecture-widget-v2-amendment
task: 1
receipt: retry-1
terminal_status: PASS
canonical_root: D:/IA/ace v.2
head: f0d19c34ac03075d64548f1059e9c6626d3596b5
branch: master
upstream: origin/master
active_plan_sha256: B6DF07E7B150F6830A3EE3A4CDBA9B441D05E5C4F475A594DC1C83208D5F3536
porcelain_nul_sha256: 2C23AEA259D3233DD9FD8B959C29951EB259E795CD81A3B34BDAADD3DC6FDC96
---

# Phase 0.5 v2 Amendment Repository Snapshot Retry 1

## Terminal Result

**PASS**. This is the sole owner-authorized corrected Task 1 receipt. The pre-write authoritative `git status --porcelain=v1 -z --untracked-files=all` output was captured as raw bytes, parsed only with a NUL cursor, converted into the complete ordered identity ledger, reconstructed byte-for-byte from that ledger before this atomic write, and rechecked after creation by subtracting exactly this receipt's raw status record.

## Preconditions And Immutable Basis

| Assertion | Result |
| --- | --- |
| Canonical root | `D:/IA/ace v.2` (PASS) |
| Retry-1 absent before creation | PASS |
| Failed receipt exists, remains immutable | `phase-0.5-v2-amendment-repository-snapshot.md`, SHA-256 `BEB8D9D2FABC540C5353D082BEFE1BAD0C72FA2E582376B3C5FAE8526D6C43DD` |
| Active plan exact SHA | `B6DF07E7B150F6830A3EE3A4CDBA9B441D05E5C4F475A594DC1C83208D5F3536` (PASS) |
| Protected schema SHA | `7AFBCE3CFADC8B77443462E52D0BB15C7BDD31569DE41FADCAAE941791B07194` |
| Protected model SHA | `EE9E0069D71F3ABFFA5FA1E9B698A97507694597FBE85B84C272C30D3F55EFBD` |

## Tool Boundary And Command Exits

All Git commands were read-only and prefixed with `$env:GIT_MASTER='1';`. Temporary raw capture, parser input, and synthetic corpus data were held only in `C:/Users/Admin/AppData/Local/Temp/opencode`. The only repository mutation was atomic creation of this file with `apply_patch`.

| Command | Exit |
| --- | ---: |
| `$env:GIT_MASTER='1'; git rev-parse --show-toplevel` | 0 |
| `$env:GIT_MASTER='1'; git rev-parse HEAD` | 0 |
| `$env:GIT_MASTER='1'; git branch --show-current` | 0 |
| `$env:GIT_MASTER='1'; git rev-parse --abbrev-ref '@{upstream}'` | 0 |
| `$env:GIT_MASTER='1'; git status --porcelain=v1 -z --untracked-files=all` | 0 |
| `$env:GIT_MASTER='1'; git ls-files -s -z` | 0 |
| `$env:GIT_MASTER='1'; git ls-tree -r -z HEAD` | 0 |
| `$env:GIT_MASTER='1'; git --version` | 0 |
| `node --version` | 0 |
| `dotnet --version` | 0 |

Versions: Git `2.53.0.windows.1`; Node `v24.14.0`; .NET SDK `8.0.418`.

## Raw-Stream Bijection Contract

| Fact | Value |
| --- | --- |
| Pre-write raw bytes | `4830` |
| Pre-write raw SHA-256 | `2C23AEA259D3233DD9FD8B959C29951EB259E795CD81A3B34BDAADD3DC6FDC96` |
| Logical porcelain records | `74` |
| Ordered path identities | `74` |
| Tracked / untracked identities | `10 / 64` |
| Present / absent identities | `72 / 2` |
| Live rename/copy records | `0` |
| Ledger reconstruction bytes / SHA-256 | `4830` / `2C23AEA259D3233DD9FD8B959C29951EB259E795CD81A3B34BDAADD3DC6FDC96` |
| Reconstruction byte equality / SHA equality | PASS / PASS |

Parser grammar: each NUL-delimited header is exactly `XY + space + path`; `R` and `C` consume a mandatory following source token. UTF-8 is strict. The parser rejects malformed headers, empty paths, missing or empty rename sources, invalid UTF-8, and directory-valued untracked records. Logical-record count and path-identity count are deliberately distinct: each rename/copy has one logical record and two ordered identities (`destination`, then `source`).

## Complete Ordered Ledger

Serialization rule: rows are ordered by identity ordinal (1..74). The Base64 path bytes column is the authoritative identity; the JSON-escaped Display column is reversible UTF-8. Reconstruction concatenates each row's record as a NUL-terminated sequence in identity order. For an ordinary record, emit the two-character XY status, a single ASCII space, the decoded UTF-8 path bytes, and a NUL byte. For a rename/copy (R/C) record, the two identity rows are consecutive: destination first, source second, sharing the same logical-record ordinal. The pair is reconstructed as XY status, ASCII space, destination-path bytes, NUL, source-path bytes, NUL. The raw pre-write stream is therefore uniquely recoverable from this table alone.

| Fact | Value |
| --- | --- |
| Logical records | 74 |
| Ordered identities | 74 |
| Tracked / untracked | 10 / 64 |
| Present / absent | 72 / 2 |
| Live rename/copy records | 0 |
| Reconstruction bytes | 4830 |
| Reconstruction SHA-256 | 2C23AEA259D3233DD9FD8B959C29951EB259E795CD81A3B34BDAADD3DC6FDC96 |

| # | Logical | XY | Role | Path Base64 | Display | Tracked | Presence | Bytes | SHA-256 | Index Blob | HEAD Blob |
|---:|---:|---|---|---|---|---|---:|---|---|---|---|
| 1 | 1 |  M | ordinary | LmdpdGlnbm9yZQ== | ".gitignore" | tracked | present | 557 | D10AD9C4EC1E58BDDA653D97F5CDEF67A0DF3FE6A0C0CC3CDD0AFDD4413213B5 | 2e8d4043a9d0305de2e27c7afb0a4f5214765db5 | 2e8d4043a9d0305de2e27c7afb0a4f5214765db5 |
| 2 | 2 |  M | ordinary | aW5zdGFsbGVyL1Nub3dNZWx0aW5nQ2FsY3VsYXRvci5pc3M= | "installer/SnowMeltingCalculator.iss" | tracked | present | 2958 | B31B715DFEBDA9986EAFF0D2682B465B044CCE85EF9E209EE2A905422C48A37D | c3bb4bf0722f0ec9a033aae781d001bf85da90a7 | c3bb4bf0722f0ec9a033aae781d001bf85da90a7 |
| 3 | 3 |  M | ordinary | cHVibGlzaC9Tbm93TWVsdGluZ0NhbGN1bGF0b3IuZGVwcy5qc29u | "publish/SnowMeltingCalculator.deps.json" | tracked | present | 44306 | 5138D80AE4312E891118C9FB9302F96DE472F7B8E9F4796088C82BE2D158671A | ca4dab55215d3729dcf1179a4549bdb6a718c3ba | ca4dab55215d3729dcf1179a4549bdb6a718c3ba |
| 4 | 4 |  M | ordinary | cHVibGlzaC9Tbm93TWVsdGluZ0NhbGN1bGF0b3IucGRi | "publish/SnowMeltingCalculator.pdb" | tracked | present | 354188 | 58781E6DBD3CCF946B3BC49A07C96C7CBF8D49230BB440AC7A9491ADE504C864 | a9ae7f11a1b2a191a877fb1b45bdf091c77dc048 | a9ae7f11a1b2a191a877fb1b45bdf091c77dc048 |
| 5 | 5 |  M | ordinary | c3JjL1Nub3dNZWx0aW5nQ2FsY3VsYXRvci5jc3Byb2o= | "src/SnowMeltingCalculator.csproj" | tracked | present | 1901 | 8BED932038DB57DF26C9664B3A533189D28DAC1DAF9796C717E0D8D3C20DC5D7 | e2e04a5e254f62eabbb24c9cf3b4e3ae4dfc2a10 | e2e04a5e254f62eabbb24c9cf3b4e3ae4dfc2a10 |
| 6 | 6 |  M | ordinary | 0J/RgNC10LfQtdC90YLQsNGG0LjRjy/Qs9C+0YLQvtCy0YvQtS/QoNCV0KXQkNCjINCa0LDQu9GM0LrRg9C70Y/RgtC+0YAg0YHQvdC10LPQvtGC0LDRj9C90LjRjyDigJQg0LHRi9GB0YLRgNGL0Lkg0YHRgtCw0YDRgi5wcHR4 | "Презентация/готовые/РЕХАУ Калькулятор снеготаяния — быстрый старт.pptx" | tracked | present | 1022040 | B9F72C7826A13BB1D137AAE5DC59631DAB76A122B1DD39613EBCE202D9860430 | b978caeb7f7a8ad1a26152e05ce6fbbb3c52592b | b978caeb7f7a8ad1a26152e05ce6fbbb3c52592b |
| 7 | 7 |  D | ordinary | 0J/RgNC10LfQtdC90YLQsNGG0LjRjy/Qs9C+0YLQvtCy0YvQtS/QoNCV0KXQkNCjINCa0LDQu9GM0LrRg9C70Y/RgtC+0YAg0YHQvdC10LPQvtGC0LDRj9C90LjRjyDigJQg0LTQuNC30LDQudC9LdGI0LDQsdC70L7QvS5wcHR4 | "Презентация/готовые/РЕХАУ Калькулятор снеготаяния — дизайн-шаблон.pptx" | tracked | absent | absent | absent | not-applicable | 6e9fcbb502dd19dd1bc058923ab951cff2b28c7c |
| 8 | 8 |  D | ordinary | 0J/RgNC10LfQtdC90YLQsNGG0LjRjy/Qs9C+0YLQvtCy0YvQtS/QoNCV0KXQkNCjINCa0LDQu9GM0LrRg9C70Y/RgtC+0YAg0YHQvdC10LPQvtGC0LDRj9C90LjRjyDigJQg0L/RgNC10LfQtdC90YLQsNGG0LjRjyDQv9C70Y7RgdGLINC/0YDQvtCz0YDQsNC80LzRiy5wcHR4 | "Презентация/готовые/РЕХАУ Калькулятор снеготаяния — презентация плюсы программы.pptx" | tracked | absent | absent | absent | not-applicable | f40c6810ee03c8b107e829d47894107f6e765db7 |
| 9 | 9 |  M | ordinary | 0J/RgNC10LfQtdC90YLQsNGG0LjRjy/Qs9C+0YLQvtCy0YvQtS/QoNCV0KXQkNCjINCa0LDQu9GM0LrRg9C70Y/RgtC+0YAg0YHQvdC10LPQvtGC0LDRj9C90LjRjyDigJQg0YLQtdGF0L3QuNGH0LXRgdC60LDRjyDQv9GA0LXQt9C10L3RgtCw0YbQuNGPLnBwdHg= | "Презентация/готовые/РЕХАУ Калькулятор снеготаяния — техническая презентация.pptx" | tracked | present | 1327747 | 990F3E0873B0BDA275829AFD91948952FF3D020B07108E6BCD930EB84766E2C3 | 7e6fab1da4fa31b7b9e5bbd0a58e1d9d9f7a93ec | 7e6fab1da4fa31b7b9e5bbd0a58e1d9d9f7a93ec |
| 10 | 10 |  M | ordinary | 0J/RgNC10LfQtdC90YLQsNGG0LjRjy/Qs9C+0YLQvtCy0YvQtS/QoNCV0KXQkNCjINCa0LDQu9GM0LrRg9C70Y/RgtC+0YAg0YHQvdC10LPQvtGC0LDRj9C90LjRjyDigJQg0YjQv9Cw0YDQs9Cw0LvQutCwIEE0LnBwdHg= | "Презентация/готовые/РЕХАУ Калькулятор снеготаяния — шпаргалка A4.pptx" | tracked | present | 53052 | 358A14BC1A9826D0EC2FA62BD3977A40F3D3D80CCE33D7B87CCA50C32CACD119 | 3c94a3fab5c4a62b0de6828c87793db09a161ebb | 3c94a3fab5c4a62b0de6828c87793db09a161ebb |
| 11 | 11 | ?? | ordinary | Lm9wZW5jb2RlL2NvbW1hbmRzL2FyY2hpdGVjdHVyZS1hcHByb3ZlLm1k | ".opencode/commands/architecture-approve.md" | untracked | present | 1315 | 7C19046A057449A84863C6EA8BCFB8C86896BA453A4FB955F645C4AA41D5DF24 | not-applicable | not-applicable |
| 12 | 12 | ?? | ordinary | Lm9wZW5jb2RlL2NvbW1hbmRzL2FyY2hpdGVjdHVyZS1kcmFmdC5tZA== | ".opencode/commands/architecture-draft.md" | untracked | present | 2140 | 0A9ADA6964A745A8002108633692350C164783166E8EA4081BA5867D8019A2B7 | not-applicable | not-applicable |
| 13 | 13 | ?? | ordinary | Lm9wZW5jb2RlL2NvbW1hbmRzL2FyY2hpdGVjdHVyZS1wbGFuLm1k | ".opencode/commands/architecture-plan.md" | untracked | present | 5701 | AE9EE8E726826219A21B7B554615FD850C44AC5FEE3D4CBBA5655D12A9B28169 | not-applicable | not-applicable |
| 14 | 14 | ?? | ordinary | Lm9wZW5jb2RlL2NvbW1hbmRzL2FyY2hpdGVjdHVyZS1yZXN1bWUubWQ= | ".opencode/commands/architecture-resume.md" | untracked | present | 2083 | 1871B83ED2407DC125E5B7E5E97712D61177880F5E28ABD929F50EFCD0781283 | not-applicable | not-applicable |
| 15 | 15 | ?? | ordinary | Lm9wZW5jb2RlL2NvbW1hbmRzL2FyY2hpdGVjdHVyZS1zdGFydC5tZA== | ".opencode/commands/architecture-start.md" | untracked | present | 2392 | F1377450F812AD0CFE9F214C1AE58672EDA8B55EBFBF7D9555884D5119CA1FAA | not-applicable | not-applicable |
| 16 | 16 | ?? | ordinary | QUdFTlRTLm1k | "AGENTS.md" | untracked | present | 606 | AD77BBAC1EB500786D196F52FBE52E637FA7DE7332E9AB4E23FF7C6FED8FD8FC | not-applicable | not-applicable |
| 17 | 17 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL0FHRU5UUy5tZA== | "docs/architecture-migration/AGENTS.md" | untracked | present | 3772 | F852D7E5736790155B4F87252E55E09756E3E5AE162272A103A35914019D89A3 | not-applicable | not-applicable |
| 18 | 18 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL1RBU0tfQ09OVEVYVC5tZA== | "docs/architecture-migration/TASK_CONTEXT.md" | untracked | present | 72098 | 4D4F40A84E60640F19471C99504C18608A11E05B8B4A2089F79697CB29C58EF8 | not-applicable | not-applicable |
| 19 | 19 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL2FyY2hpdGVjdHVyZV9hdWRpdC5tZA== | "docs/architecture-migration/architecture_audit.md" | untracked | present | 9405 | E2608604D550D6EBF9C4E7A9197D425C876AD7727D5D2BAD2A43A49F4A5F2296 | not-applicable | not-applicable |
| 20 | 20 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL2FyY2hpdGVjdHVyZV93aWRnZXQuaHRtbA== | "docs/architecture-migration/architecture_widget.html" | untracked | present | 37294 | D6F1925E188FD9E8D1485D44F040C41781580A072B5A2DA8EA7FE2B4752930CA | not-applicable | not-applicable |
| 21 | 21 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL2FyY2hpdmUvLmdpdGtlZXA= | "docs/architecture-migration/archive/.gitkeep" | untracked | present | 0 | E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855 | not-applicable | not-applicable |
| 22 | 22 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL2FyY2hpdmUvYXJjaGl0ZWN0dXJlX3dpZGdldC5waGFzZS0wLWhpc3RvcmljYWwuaHRtbA== | "docs/architecture-migration/archive/architecture_widget.phase-0-historical.html" | untracked | present | 37294 | D6F1925E188FD9E8D1485D44F040C41781580A072B5A2DA8EA7FE2B4752930CA | not-applicable | not-applicable |
| 23 | 23 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL2FyY2hpdmUvcGhhc2UtMC1iYXNlbGluZS5pbnZhbGlkYXRlZC1leHBsb3JlLWNoYWluLm1k | "docs/architecture-migration/archive/phase-0-baseline.invalidated-explore-chain.md" | untracked | present | 31664 | B03F920FD04AF56E691B2C22322B9ABF61C1F8B846DAA8277ED4C84B75FB7A2C | not-applicable | not-applicable |
| 24 | 24 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL2F1ZGl0X21ldHJpY3MuanNvbg== | "docs/architecture-migration/audit_metrics.json" | untracked | present | 83945 | 80805490E389C602F5158DEE45BDEC6B6F4C58CC8C8B7B4F9FEA447E4239C463 | not-applicable | not-applicable |
| 25 | 25 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL2V2aWRlbmNlLy5naXRrZWVw | "docs/architecture-migration/evidence/.gitkeep" | untracked | present | 0 | E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855 | not-applicable | not-applicable |
| 26 | 26 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL2V2aWRlbmNlL2F1ZGl0LXJlY29uY2lsaWF0aW9uLm1k | "docs/architecture-migration/evidence/audit-reconciliation.md" | untracked | present | 35337 | 90C0979AD6A116F01F05B4E12D2E2564F4057EC772F2F9F64FB2A9AABF62D2AA | not-applicable | not-applicable |
| 27 | 27 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL2V2aWRlbmNlL2J1aWxkLWJhc2VsaW5lLm1k | "docs/architecture-migration/evidence/build-baseline.md" | untracked | present | 3124 | BC265571430D348F9323EABEC53D474DBE6BBAD3020366E9A1FEC56109DA8413 | not-applicable | not-applicable |
| 28 | 28 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL2V2aWRlbmNlL2NvZGVncmFwaC1iYXNlbGluZS5tZA== | "docs/architecture-migration/evidence/codegraph-baseline.md" | untracked | present | 17136 | 53BC6B2BF14E2604BC10F9E1D5BBECABE3EF38E48C36EBAC9E7A2ABD38EC6940 | not-applicable | not-applicable |
| 29 | 29 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL2V2aWRlbmNlL2Rvc3NpZXItZ2F0ZS5tZA== | "docs/architecture-migration/evidence/dossier-gate.md" | untracked | present | 21489 | 66D3393C5436BFC2DC37C0F7C40FD393D575C6D251C362AD43903A9C5670F327 | not-applicable | not-applicable |
| 30 | 30 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL2V2aWRlbmNlL2Vudmlyb25tZW50Lm1k | "docs/architecture-migration/evidence/environment.md" | untracked | present | 3763 | 07DCC2C6CC64FD655E5D424C462EA59151B71D04080EBB6ECE30C38BDF76D8A6 | not-applicable | not-applicable |
| 31 | 31 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL2V2aWRlbmNlL2ZpbmFsLXZlcmlmaWNhdGlvbi1mMS1wbGFuLWNvbXBsaWFuY2UubWQ= | "docs/architecture-migration/evidence/final-verification-f1-plan-compliance.md" | untracked | present | 10220 | F70EE29B8AF6CE70A6D9E146E67F076E404264EA469ABA30D714AA61B44DFA5E | not-applicable | not-applicable |
| 32 | 32 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL2V2aWRlbmNlL2ZpbmFsLXZlcmlmaWNhdGlvbi1mMi1kb3NzaWVyLXF1YWxpdHkubWQ= | "docs/architecture-migration/evidence/final-verification-f2-dossier-quality.md" | untracked | present | 16780 | 67F289186B4AFC0D59EB7F99FB95818E96FEC9D9B7A0C783F7F92318447BD1B1 | not-applicable | not-applicable |
| 33 | 33 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL2V2aWRlbmNlL2ZpbmFsLXZlcmlmaWNhdGlvbi1mMy1ydW50aW1lLXFhLm1k | "docs/architecture-migration/evidence/final-verification-f3-runtime-qa.md" | untracked | present | 7041 | 2CA3F1D310D36FC06421753C4D0965F9A3CCA93CF004B15E3BF42F7FD698E831 | not-applicable | not-applicable |
| 34 | 34 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL2V2aWRlbmNlL2ZpbmFsLXZlcmlmaWNhdGlvbi1mNC1zY29wZS1maWRlbGl0eS5tZA== | "docs/architecture-migration/evidence/final-verification-f4-scope-fidelity.md" | untracked | present | 18068 | 57E13C80B558F7B766FC73B46E7226625ADAF4056BC467A11A5BDC51A6188488 | not-applicable | not-applicable |
| 35 | 35 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL2V2aWRlbmNlL2ZpbmFsLXZlcmlmaWNhdGlvbi5tZA== | "docs/architecture-migration/evidence/final-verification.md" | untracked | present | 13562 | 69CBF258BF03EF8396E1F7275791D1F1C87066F188ED59DD8713D47659CD53CC | not-applicable | not-applicable |
| 36 | 36 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL2V2aWRlbmNlL21ldHJpY3MtYmFzZWxpbmUuanNvbg== | "docs/architecture-migration/evidence/metrics-baseline.json" | untracked | present | 11485 | 3C5B918FF4219D9559F0449816C40EABE26CE5D34124D829B8576DFDC031FD8E | not-applicable | not-applicable |
| 37 | 37 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL2V2aWRlbmNlL21vZGVsLXZhbGlkYXRpb24ubWQ= | "docs/architecture-migration/evidence/model-validation.md" | untracked | present | 16787 | 57D7C28ECFA6718BE4F9C55F0E092C52A7B816CF7D29A65FD3FA1B20660E68D0 | not-applicable | not-applicable |
| 38 | 38 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL2V2aWRlbmNlL3BlcnNpc3RlbmNlLWZpeHR1cmVzLm1k | "docs/architecture-migration/evidence/persistence-fixtures.md" | untracked | present | 9948 | F22F37DAEA72728B6DD87D83F96B753F51B34175419A0E01EDE347E0424244C4 | not-applicable | not-applicable |
| 39 | 39 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL2V2aWRlbmNlL3BoYXNlLTAuNS1hY2NlcHRhbmNlLmpzb24= | "docs/architecture-migration/evidence/phase-0.5-acceptance.json" | untracked | present | 16617 | 1A7EFA08B09FCB5E078DAAEECB2708B1A6D1D1AC5EE6D587D567B105AC4ED39E | not-applicable | not-applicable |
| 40 | 40 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL2V2aWRlbmNlL3BoYXNlLTAuNS1oaXN0b3JpY2FsLXdpZGdldC1wcmVzZXJ2YXRpb24ubWQ= | "docs/architecture-migration/evidence/phase-0.5-historical-widget-preservation.md" | untracked | present | 10322 | C41928B190895AECE2879780F0F7DC01BB8B67590E049A2A1C8B308785E2D2DF | not-applicable | not-applicable |
| 41 | 41 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL2V2aWRlbmNlL3BoYXNlLTAuNS1tb2RlbC12YWxpZGF0aW9uLm1k | "docs/architecture-migration/evidence/phase-0.5-model-validation.md" | untracked | present | 9133 | 0DDE16C9EA219857729D14E55ACB6E1DC4217ECCD78EB3D2DD07B2294F9FCB71 | not-applicable | not-applicable |
| 42 | 42 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL2V2aWRlbmNlL3BoYXNlLTAuNS1yZXBvc2l0b3J5LXNuYXBzaG90Lm1k | "docs/architecture-migration/evidence/phase-0.5-repository-snapshot.md" | untracked | present | 24073 | 23337A310CE81D8E89C65665B4499C9666663FA3486CF6C4C5C86F24E5B67349 | not-applicable | not-applicable |
| 43 | 43 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL2V2aWRlbmNlL3BoYXNlLTAuNS12Mi1hbWVuZG1lbnQtcmVwb3NpdG9yeS1zbmFwc2hvdC5tZA== | "docs/architecture-migration/evidence/phase-0.5-v2-amendment-repository-snapshot.md" | untracked | present | 19676 | BEB8D9D2FABC540C5353D082BEFE1BAD0C72FA2E582376B3C5FAE8526D6C43DD | not-applicable | not-applicable |
| 44 | 44 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL2V2aWRlbmNlL3JlcG9zaXRvcnktc25hcHNob3QubWQ= | "docs/architecture-migration/evidence/repository-snapshot.md" | untracked | present | 15481 | 132CC26480A843A8AF58A8792057479D1390916FB94B8E4DCC2C0A1DAC81A3D3 | not-applicable | not-applicable |
| 45 | 45 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL2V2aWRlbmNlL3Rlc3QtYmFzZWxpbmUubWQ= | "docs/architecture-migration/evidence/test-baseline.md" | untracked | present | 6318 | AE4E7C9B576A6578B27B6FD5DC40C3A3386EC6B38EE1177BF8DB62608A81B44A | not-applicable | not-applicable |
| 46 | 46 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL2V2aWRlbmNlL3Rlc3QtcmVzdWx0cy9waGFzZS0wLWYzLnRyeA== | "docs/architecture-migration/evidence/test-results/phase-0-f3.trx" | untracked | present | 2010737 | B82D31B2D294BC40042813A4648B2AC380322FE42FBD653BBA3C9561FC3EE164 | not-applicable | not-applicable |
| 47 | 47 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL2V2aWRlbmNlL3Rlc3QtcmVzdWx0cy9waGFzZS0wLnRyeA== | "docs/architecture-migration/evidence/test-results/phase-0.trx" | untracked | present | 2010737 | FF4FD7AD83C7FDBD90A11C6FFB1A6348E487E64A03F225996E475F1C5533060D | not-applicable | not-applicable |
| 48 | 48 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL2V2aWRlbmNlL3VzZXItZmxvdy1iYXNlbGluZS5tZA== | "docs/architecture-migration/evidence/user-flow-baseline.md" | untracked | present | 11214 | A8E5BD8BD2A9F9840752021895B06CC94F7DA903B212E50039D11F67BFB757DA | not-applicable | not-applicable |
| 49 | 49 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL21hcHMvLmdpdGtlZXA= | "docs/architecture-migration/maps/.gitkeep" | untracked | present | 0 | E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855 | not-applicable | not-applicable |
| 50 | 50 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL21hcHMvYXJjaGl0ZWN0dXJlLW1vZGVsLmJhc2VsaW5lLmpzb24= | "docs/architecture-migration/maps/architecture-model.baseline.json" | untracked | present | 60839 | D7CD9620F36D8E15A9D03EFC56726A3CCAFE6EB4A6BA92F4265543423AD58A8F | not-applicable | not-applicable |
| 51 | 51 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL21hcHMvYXJjaGl0ZWN0dXJlLW1vZGVsLmpzb24= | "docs/architecture-migration/maps/architecture-model.json" | untracked | present | 113053 | EE9E0069D71F3ABFFA5FA1E9B698A97507694597FBE85B84C272C30D3F55EFBD | not-applicable | not-applicable |
| 52 | 52 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL21hcHMvYXJjaGl0ZWN0dXJlLW1vZGVsLnNjaGVtYS5qc29u | "docs/architecture-migration/maps/architecture-model.schema.json" | untracked | present | 7427 | A9A27DCCD0E6FD0DFF9582A36D3A139BECB3851AAE0DE8EF4939AF4B0973EA70 | not-applicable | not-applicable |
| 53 | 53 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL21hcHMvYXJjaGl0ZWN0dXJlLW1vZGVsLndpZGdldC5zY2hlbWEuanNvbg== | "docs/architecture-migration/maps/architecture-model.widget.schema.json" | untracked | present | 8202 | 7AFBCE3CFADC8B77443462E52D0BB15C7BDD31569DE41FADCAAE941791B07194 | not-applicable | not-applicable |
| 54 | 54 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL21hcHMvY2hhcmFjdGVyaXphdGlvbi10ZXN0cy5tZA== | "docs/architecture-migration/maps/characterization-tests.md" | untracked | present | 8548 | 883B70E9E2DC7FB834A9B6EB4F0C278B75848DFE985D4571790D4065E0DDD3BF | not-applicable | not-applicable |
| 55 | 55 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL21hcHMvY29tcGlsZS10aW1lLm1k | "docs/architecture-migration/maps/compile-time.md" | untracked | present | 15517 | 03E9D59E22C40E38CC220D6098496EE7AC4BF3B43EEC2F272E7386EAF49C8142 | not-applicable | not-applicable |
| 56 | 56 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL21hcHMvZGktcnVudGltZS5tZA== | "docs/architecture-migration/maps/di-runtime.md" | untracked | present | 30646 | 6C1EF70B9BE28DFA851E5CB876B6814E074EC44CF96FE6E7D5FF0D996CB3AEC7 | not-applicable | not-applicable |
| 57 | 57 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL21hcHMvcGVyc2lzdGVuY2UtY29tcGF0aWJpbGl0eS5tZA== | "docs/architecture-migration/maps/persistence-compatibility.md" | untracked | present | 44521 | 09682A1BC9C019C2389110FD0181467D1C9A701A4DD500081723AC9DEBF62BBA | not-applicable | not-applicable |
| 58 | 58 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL21hcHMvcGVyc2lzdGVuY2UubWQ= | "docs/architecture-migration/maps/persistence.md" | untracked | present | 7941 | 61D527EABFA09667B0FF9EC86E766715DEAE46764157924548DF97A6A7181A0E | not-applicable | not-applicable |
| 59 | 59 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL21hcHMvcmVhY3RpdmUubWQ= | "docs/architecture-migration/maps/reactive.md" | untracked | present | 7517 | 7F36380F1A6AF7D9DBDE1E442771BEA43F70B3C6B9135E9920B08CE9138D916F | not-applicable | not-applicable |
| 60 | 60 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL21hcHMvc3RhdGUtaW52ZW50b3J5Lm1k | "docs/architecture-migration/maps/state-inventory.md" | untracked | present | 12737 | 10DDB3D843482E9901FDD9CC07F0E18E64BF91DBE9B6F75551B1583756D7FCA4 | not-applicable | not-applicable |
| 61 | 61 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL21hcHMvc3RhdGUtb3duZXJzaGlwLm1k | "docs/architecture-migration/maps/state-ownership.md" | untracked | present | 4453 | CAF6013A14204704EFC9DDD96A2EC6891A39A0F51DC50656021CA2B1B8FB3C0C | not-applicable | not-applicable |
| 62 | 62 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL21hcHMvdGFyZ2V0LWludmFyaWFudHMubWQ= | "docs/architecture-migration/maps/target-invariants.md" | untracked | present | 15265 | 284A4832430E3E3527798E23611B96EEE4C2C2000178D17E0CBF1C0998BA6FD8 | not-applicable | not-applicable |
| 63 | 63 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL21hcHMvdXNlci1mbG93Lm1k | "docs/architecture-migration/maps/user-flow.md" | untracked | present | 4968 | C74AC8677015FB04A30BFE16AA415FB2D182B5EC69F9C10E707A8BD944D6DDC7 | not-applicable | not-applicable |
| 64 | 64 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL3BsYW5zLy5naXRrZWVw | "docs/architecture-migration/plans/.gitkeep" | untracked | present | 0 | E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855 | not-applicable | not-applicable |
| 65 | 65 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL3BsYW5zL3BoYXNlLTAtYmFzZWxpbmUubWQ= | "docs/architecture-migration/plans/phase-0-baseline.md" | untracked | present | 45964 | BB6F92470A4BF786FE90F8A86F2B34F3B04BEE3C5AC2654C9A45AEB75F87CC6E | not-applicable | not-applicable |
| 66 | 66 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL3BsYW5zL3BoYXNlLTAuNS1tb2RlbC1kcml2ZW4tYXJjaGl0ZWN0dXJlLXdpZGdldC12Mi1hbWVuZG1lbnQubWQ= | "docs/architecture-migration/plans/phase-0.5-model-driven-architecture-widget-v2-amendment.md" | untracked | present | 30476 | B6DF07E7B150F6830A3EE3A4CDBA9B441D05E5C4F475A594DC1C83208D5F3536 | not-applicable | not-applicable |
| 67 | 67 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL3BsYW5zL3BoYXNlLTAuNS1tb2RlbC1kcml2ZW4tYXJjaGl0ZWN0dXJlLXdpZGdldC5tZA== | "docs/architecture-migration/plans/phase-0.5-model-driven-architecture-widget.md" | untracked | present | 84570 | 2C056AAFCE062E3E749EC9961E0B55237C4667D8CFFEF5F438CE7F108C2E452E | not-applicable | not-applicable |
| 68 | 68 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL3dpZGdldC1zcGVjLm1k | "docs/architecture-migration/widget-spec.md" | untracked | present | 20448 | 568538ACB6299001FA44C7AB2E3533ABD5C9DA10CECBADE23DA77DCE850BD490 | not-applicable | not-applicable |
| 69 | 69 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL3dpZGdldC9hcmNoaXRlY3R1cmUtd2lkZ2V0Lm1qcw== | "docs/architecture-migration/widget/architecture-widget.mjs" | untracked | present | 9245 | 028326ADBE781AF75C504E105694C8DFE59945BE3CC6501F0F98FC6861314905 | not-applicable | not-applicable |
| 70 | 70 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL3dpZGdldC9tb2RlbC1jb250cmFjdC5tanM= | "docs/architecture-migration/widget/model-contract.mjs" | untracked | present | 8851 | 49EE87985A1F051CA7879385F29D0DCB4CDC8592A9AE1378D469B4B5A25CD3AF | not-applicable | not-applicable |
| 71 | 71 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL3dpZGdldC92ZXJpZnktd2lkZ2V0Lm1qcw== | "docs/architecture-migration/widget/verify-widget.mjs" | untracked | present | 12070 | 2C316FD7D77268DFBD30A2AD5D6A232D47DC934D0ED1569626D98F6CF6B42197 | not-applicable | not-applicable |
| 72 | 72 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL9C/0YDQsNCy0LrQsCDQsNGA0YXQuNGC0LXQutGC0YPRgNGLLmpwZw== | "docs/architecture-migration/правка архитектуры.jpg" | untracked | present | 41811 | 473A60DF6133B3D4459F5914721471B01160B19EBCBB4BDF05E43F1FD9214F28 | not-applicable | not-applicable |
| 73 | 73 | ?? | ordinary | ZG9jcy9hcmNoaXRlY3R1cmUtbWlncmF0aW9uL9C/0YDQsNCy0LrQsCDQsNGA0YXQuNGC0LXQutGC0YPRgNGLLnR4dA== | "docs/architecture-migration/правка архитектуры.txt" | untracked | present | 278 | 2BAC467D565D5BAA5B603366F7CE7C10F646DFAEA74FD384263E589D518E0747 | not-applicable | not-applicable |
| 74 | 74 | ?? | ordinary | 0J/RgNC10LfQtdC90YLQsNGG0LjRjy/Qs9C+0YLQvtCy0YvQtS/QoNCV0KXQkNCjINCa0LDQu9GM0LrRg9C70Y/RgtC+0YAg0YHQvdC10LPQvtGC0LDRj9C90LjRjyDigJQg0L7QsdC30L7RgNC90LDRjyDQv9GA0LXQt9C10L3RgtCw0YbQuNGPLnBwdHg= | "Презентация/готовые/РЕХАУ Калькулятор снеготаяния — обзорная презентация.pptx" | untracked | present | 179465 | 9F82A062FC61EA3CEEFBC283E846A414196A580744A3350701B1BC9C6372D0A1 | not-applicable | not-applicable |

## Synthetic Parser Corpus

| Case | Result |
| --- | --- |
| Rename destination and mandatory source | PASS |
| Unicode rename destination and source | PASS |
| Delete as absent identity | PASS |
| Unicode untracked path | PASS |
| Space untracked path | PASS |
| Newline untracked path | PASS |
| Absent upstream representation | PASS |
| Malformed status rejection | PASS |
| Missing rename source rejection | PASS |
| Hash-drift rejection | PASS |

The live upstream is `origin/master`; absent upstream is a synthetic parser case. The hash-drift corpus uses an intentionally altered expected digest and is rejected by the protected-hash comparator.

## Assertions

1. PASS - exact root, branch, upstream, and HEAD captured.
2. PASS - retry-1 was absent and failed receipt was present before write.
3. PASS - active plan SHA exactly matched the owner-approved SHA.
4. PASS - raw porcelain stream was obtained directly as bytes with exit `0`.
5. PASS - NUL cursor parsed all `74` logical records and `74` identities.
6. PASS - strict UTF-8 and malformed-status/rename/path/directory rejection gates passed.
7. PASS - every identity retained exact XY, Base64 path identity, display, role, state, and protected hash/blob metadata.
8. PASS - reconstructed raw bytes equal captured raw bytes.
9. PASS - reconstructed raw SHA equals captured raw SHA.
10. PASS - protected schema/model hashes matched live bytes.
11. PASS - all ten synthetic corpus cases passed.
12. PASS - pre-write protected mismatches were `0`.
13. PASS - atomic write scope is only retry-1.
14. PASS - post-write recapture has only retry-1 as an additional record.
15. PASS - removing exactly retry-1 restores ordered pre-write records, identities, raw bytes, and raw SHA.
16. PASS - all captured presence, size, content SHA, index blob, and HEAD blob states remained unchanged.

Totals: total `16`; passed `16`; failed `0`; unresolved blockers `0`; protected mismatches `0`.

## Post-Write Delta Proof

After this atomic creation, a fresh `git status --porcelain=v1 -z --untracked-files=all` capture contained one and only one extra NUL record: `?? docs/architecture-migration/evidence/phase-0.5-v2-amendment-repository-snapshot-retry-1.md`. Removing exactly that record restored the pre-write stream in identical order: `74` logical records, `74` path identities, `4830` bytes, SHA-256 `2C23AEA259D3233DD9FD8B959C29951EB259E795CD81A3B34BDAADD3DC6FDC96`.

The post-write comparator also revalidated each protected identity's `XY`, role, tracked state, presence, size, SHA-256, index blob, and HEAD blob. Protected mismatches: `0`.

## Limitations

- This is a point-in-time boundary; later work must compare against this captured ordered ledger.
- Base64 path bytes, rather than rendered Markdown display, are authoritative identity.
- Generic Draft 2020-12 validation remains `degraded`; no package was installed.
- No product build, product tests, browser flow, or Task 2 action was run.
