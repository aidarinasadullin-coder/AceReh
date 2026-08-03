---
phase: phase-0.5-model-driven-architecture-widget
snapshot_sha: f0d19c34ac03075d64548f1059e9c6626d3596b5
source_basis: execution-time working-tree; NUL-delimited porcelain v1; each row protected before receipt write
generated_at_utc: 2026-07-31T11:35:13.5056249Z
working_directory: D:/IA/ace v.2
status: pass
---

# Phase 0.5 Execution-Time Repository Snapshot

## Mandatory Evidence Metadata

| Field | Value |
| --- | --- |
| Phase | phase-0.5-model-driven-architecture-widget |
| Capture timestamp (UTC) | 2026-07-31T11:35:13.5056249Z |
| Canonical working directory | D:/IA/ace v.2 |
| Invocation working directory | D:/IA/ace v.2 |
| Git root | D:/IA/ace v.2 (asserted equal to D:/IA/ace v.2) |
| HEAD / snapshot SHA | f0d19c34ac03075d64548f1059e9c6626d3596b5 |
| Branch / upstream | master / origin/master |
| Approved plan SHA-256 | 2C056AAFCE062E3E749EC9961E0B55237C4667D8CFFEF5F438CE7F108C2E452E |
| Phase 0 comparison snapshot SHA | f0d19c34ac03075d64548f1059e9c6626d3596b5 |
| Porcelain source input SHA-256 | 87E9AC6557F2E0E1B31FCF0E5B20B277AB824738438FC155571A156A87BF8EAA (UTF-8 serialization of exact NUL record stream) |
| Output SHA-256 | 780C5339EF661216B632C6955D25F5230DBB7D769B52AF12F2C244C02B8BE4C1 (normalized receipt bytes with this value blanked before hashing) |
| Changed-path allow-list | pass: sole repository write is this allow-listed receipt; no protected pre-existing path was written |
| Capture command exit codes | root=0, HEAD=0, branch=0, upstream=0, porcelain-text=0, porcelain-NUL=0, tracked-diff=0, git-version=0, node=0 |
| Assertion totals | 10/10 pass |

## Exact Commands and Tool Availability

All Git commands were invoked read-only with `$env:GIT_MASTER='1';` as required.

```powershell
$env:GIT_MASTER='1'; git rev-parse --show-toplevel
$env:GIT_MASTER='1'; git rev-parse HEAD
$env:GIT_MASTER='1'; git branch --show-current
$env:GIT_MASTER='1'; git rev-parse --abbrev-ref '@{upstream}'
$env:GIT_MASTER='1'; git -c core.quotepath=false status --porcelain=v1 --untracked-files=all
$env:GIT_MASTER='1'; git status --porcelain=v1 -z --untracked-files=all
$env:GIT_MASTER='1'; git -c core.quotepath=false diff --name-status
$env:GIT_MASTER='1'; git --version
node --version
node -e "require.resolve(<module>/package.json)"
Get-Command chrome, chromium, msedge, firefox
Get-FileHash -LiteralPath <every-present-path> -Algorithm SHA256
```

| Tool / probe | Actual result |
| --- | --- |
| PowerShell | 5.1.19041.6456 |
| Git | git version 2.53.0.windows.1 (exit 0) |
| Node | v24.14.0 (exit 0) |
| Node module playwright | absent: require.resolve exit 2 |
| Node module playwright-core | absent: require.resolve exit 2 |
| Node module puppeteer | absent: require.resolve exit 2 |
| Node module @playwright/test | absent: require.resolve exit 2 |
| Executable chrome / chrome.exe | absent from PATH |
| Executable chromium / chromium.exe | absent from PATH |
| Executable msedge / msedge.exe | absent from PATH |
| Executable firefox / firefox.exe | absent from PATH |

No browser automation module and no candidate browser executable was found by these non-installing probes. No browser was opened, downloaded, or installed; this Task 1 availability finding does not execute the future artifact.

## Dirty-Worktree Boundary

The exact required text command produced 62 logical status rows. The NUL form is the authoritative parser input because display quoting and terminal encoding cannot alter row identity. Every row below existed before this receipt was written and is protected owner content.

- Tracked dirty rows: 10
- Pre-existing untracked rows: 52
- Present paths hashed: 60
- Explicit deleted paths: 2
- Rename/copy rows: 0

### Separately Enumerated Tracked Diff Paths

```text
M       .gitignore
M       installer/SnowMeltingCalculator.iss
M       publish/SnowMeltingCalculator.deps.json
M       publish/SnowMeltingCalculator.pdb
M       src/SnowMeltingCalculator.csproj
M       Презентация/готовые/РЕХАУ Калькулятор снеготаяния — быстрый старт.pptx
D       Презентация/готовые/РЕХАУ Калькулятор снеготаяния — дизайн-шаблон.pptx
D       Презентация/готовые/РЕХАУ Калькулятор снеготаяния — презентация плюсы программы.pptx
M       Презентация/готовые/РЕХАУ Калькулятор снеготаяния — техническая презентация.pptx
M       Презентация/готовые/РЕХАУ Калькулятор снеготаяния — шпаргалка A4.pptx
```

### Separately Enumerated Pre-existing Untracked Paths

```text
.opencode/commands/architecture-approve.md
.opencode/commands/architecture-draft.md
.opencode/commands/architecture-plan.md
.opencode/commands/architecture-resume.md
.opencode/commands/architecture-start.md
AGENTS.md
docs/architecture-migration/AGENTS.md
docs/architecture-migration/TASK_CONTEXT.md
docs/architecture-migration/architecture_audit.md
docs/architecture-migration/architecture_widget.html
docs/architecture-migration/archive/.gitkeep
docs/architecture-migration/archive/phase-0-baseline.invalidated-explore-chain.md
docs/architecture-migration/audit_metrics.json
docs/architecture-migration/evidence/.gitkeep
docs/architecture-migration/evidence/audit-reconciliation.md
docs/architecture-migration/evidence/build-baseline.md
docs/architecture-migration/evidence/codegraph-baseline.md
docs/architecture-migration/evidence/dossier-gate.md
docs/architecture-migration/evidence/environment.md
docs/architecture-migration/evidence/final-verification-f1-plan-compliance.md
docs/architecture-migration/evidence/final-verification-f2-dossier-quality.md
docs/architecture-migration/evidence/final-verification-f3-runtime-qa.md
docs/architecture-migration/evidence/final-verification-f4-scope-fidelity.md
docs/architecture-migration/evidence/final-verification.md
docs/architecture-migration/evidence/metrics-baseline.json
docs/architecture-migration/evidence/model-validation.md
docs/architecture-migration/evidence/persistence-fixtures.md
docs/architecture-migration/evidence/repository-snapshot.md
docs/architecture-migration/evidence/test-baseline.md
docs/architecture-migration/evidence/test-results/phase-0-f3.trx
docs/architecture-migration/evidence/test-results/phase-0.trx
docs/architecture-migration/evidence/user-flow-baseline.md
docs/architecture-migration/maps/.gitkeep
docs/architecture-migration/maps/architecture-model.baseline.json
docs/architecture-migration/maps/architecture-model.schema.json
docs/architecture-migration/maps/characterization-tests.md
docs/architecture-migration/maps/compile-time.md
docs/architecture-migration/maps/di-runtime.md
docs/architecture-migration/maps/persistence-compatibility.md
docs/architecture-migration/maps/persistence.md
docs/architecture-migration/maps/reactive.md
docs/architecture-migration/maps/state-inventory.md
docs/architecture-migration/maps/state-ownership.md
docs/architecture-migration/maps/target-invariants.md
docs/architecture-migration/maps/user-flow.md
docs/architecture-migration/plans/.gitkeep
docs/architecture-migration/plans/phase-0-baseline.md
docs/architecture-migration/plans/phase-0.5-model-driven-architecture-widget.md
docs/architecture-migration/widget-spec.md
docs/architecture-migration/правка архитектуры.jpg
docs/architecture-migration/правка архитектуры.txt
Презентация/готовые/РЕХАУ Калькулятор снеготаяния — обзорная презентация.pptx
```

### Normalized Porcelain Ledger

Each logical porcelain row maps exactly once below. `not-applicable` means an untracked path has no HEAD blob. No rename/copy row occurred; if one had appeared, destination and source would have been retained as distinct fields.

| # | XY | Class | Presence | Bytes | SHA-256 | HEAD blob | Path |
| --- | --- | --- | --- | ---: | --- | --- | --- |
| 1 | ` M` | tracked | present | 557 | D10AD9C4EC1E58BDDA653D97F5CDEF67A0DF3FE6A0C0CC3CDD0AFDD4413213B5 | 2e8d4043a9d0305de2e27c7afb0a4f5214765db5 | .gitignore |
| 2 | ` M` | tracked | present | 2958 | B31B715DFEBDA9986EAFF0D2682B465B044CCE85EF9E209EE2A905422C48A37D | c3bb4bf0722f0ec9a033aae781d001bf85da90a7 | installer/SnowMeltingCalculator.iss |
| 3 | ` M` | tracked | present | 44306 | 5138D80AE4312E891118C9FB9302F96DE472F7B8E9F4796088C82BE2D158671A | ca4dab55215d3729dcf1179a4549bdb6a718c3ba | publish/SnowMeltingCalculator.deps.json |
| 4 | ` M` | tracked | present | 354188 | 58781E6DBD3CCF946B3BC49A07C96C7CBF8D49230BB440AC7A9491ADE504C864 | a9ae7f11a1b2a191a877fb1b45bdf091c77dc048 | publish/SnowMeltingCalculator.pdb |
| 5 | ` M` | tracked | present | 1901 | 8BED932038DB57DF26C9664B3A533189D28DAC1DAF9796C717E0D8D3C20DC5D7 | e2e04a5e254f62eabbb24c9cf3b4e3ae4dfc2a10 | src/SnowMeltingCalculator.csproj |
| 6 | ` M` | tracked | present | 1022040 | B9F72C7826A13BB1D137AAE5DC59631DAB76A122B1DD39613EBCE202D9860430 | b978caeb7f7a8ad1a26152e05ce6fbbb3c52592b | Презентация/готовые/РЕХАУ Калькулятор снеготаяния — быстрый старт.pptx |
| 7 | ` D` | tracked | deleted | deleted | deleted | 6e9fcbb502dd19dd1bc058923ab951cff2b28c7c | Презентация/готовые/РЕХАУ Калькулятор снеготаяния — дизайн-шаблон.pptx |
| 8 | ` D` | tracked | deleted | deleted | deleted | f40c6810ee03c8b107e829d47894107f6e765db7 | Презентация/готовые/РЕХАУ Калькулятор снеготаяния — презентация плюсы программы.pptx |
| 9 | ` M` | tracked | present | 1327747 | 990F3E0873B0BDA275829AFD91948952FF3D020B07108E6BCD930EB84766E2C3 | 7e6fab1da4fa31b7b9e5bbd0a58e1d9d9f7a93ec | Презентация/готовые/РЕХАУ Калькулятор снеготаяния — техническая презентация.pptx |
| 10 | ` M` | tracked | present | 53052 | 358A14BC1A9826D0EC2FA62BD3977A40F3D3D80CCE33D7B87CCA50C32CACD119 | 3c94a3fab5c4a62b0de6828c87793db09a161ebb | Презентация/готовые/РЕХАУ Калькулятор снеготаяния — шпаргалка A4.pptx |
| 11 | `??` | untracked | present | 1315 | 7C19046A057449A84863C6EA8BCFB8C86896BA453A4FB955F645C4AA41D5DF24 | not-applicable | .opencode/commands/architecture-approve.md |
| 12 | `??` | untracked | present | 2140 | 0A9ADA6964A745A8002108633692350C164783166E8EA4081BA5867D8019A2B7 | not-applicable | .opencode/commands/architecture-draft.md |
| 13 | `??` | untracked | present | 5701 | AE9EE8E726826219A21B7B554615FD850C44AC5FEE3D4CBBA5655D12A9B28169 | not-applicable | .opencode/commands/architecture-plan.md |
| 14 | `??` | untracked | present | 2083 | 1871B83ED2407DC125E5B7E5E97712D61177880F5E28ABD929F50EFCD0781283 | not-applicable | .opencode/commands/architecture-resume.md |
| 15 | `??` | untracked | present | 2392 | F1377450F812AD0CFE9F214C1AE58672EDA8B55EBFBF7D9555884D5119CA1FAA | not-applicable | .opencode/commands/architecture-start.md |
| 16 | `??` | untracked | present | 606 | AD77BBAC1EB500786D196F52FBE52E637FA7DE7332E9AB4E23FF7C6FED8FD8FC | not-applicable | AGENTS.md |
| 17 | `??` | untracked | present | 3772 | F852D7E5736790155B4F87252E55E09756E3E5AE162272A103A35914019D89A3 | not-applicable | docs/architecture-migration/AGENTS.md |
| 18 | `??` | untracked | present | 36465 | CAC4BC2E2E183DE06D260FB67DB71584C9264F8FB163E766D0767F44CBC07D42 | not-applicable | docs/architecture-migration/TASK_CONTEXT.md |
| 19 | `??` | untracked | present | 9405 | E2608604D550D6EBF9C4E7A9197D425C876AD7727D5D2BAD2A43A49F4A5F2296 | not-applicable | docs/architecture-migration/architecture_audit.md |
| 20 | `??` | untracked | present | 37294 | D6F1925E188FD9E8D1485D44F040C41781580A072B5A2DA8EA7FE2B4752930CA | not-applicable | docs/architecture-migration/architecture_widget.html |
| 21 | `??` | untracked | present | 0 | E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855 | not-applicable | docs/architecture-migration/archive/.gitkeep |
| 22 | `??` | untracked | present | 31664 | B03F920FD04AF56E691B2C22322B9ABF61C1F8B846DAA8277ED4C84B75FB7A2C | not-applicable | docs/architecture-migration/archive/phase-0-baseline.invalidated-explore-chain.md |
| 23 | `??` | untracked | present | 83945 | 80805490E389C602F5158DEE45BDEC6B6F4C58CC8C8B7B4F9FEA447E4239C463 | not-applicable | docs/architecture-migration/audit_metrics.json |
| 24 | `??` | untracked | present | 0 | E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855 | not-applicable | docs/architecture-migration/evidence/.gitkeep |
| 25 | `??` | untracked | present | 35337 | 90C0979AD6A116F01F05B4E12D2E2564F4057EC772F2F9F64FB2A9AABF62D2AA | not-applicable | docs/architecture-migration/evidence/audit-reconciliation.md |
| 26 | `??` | untracked | present | 3124 | BC265571430D348F9323EABEC53D474DBE6BBAD3020366E9A1FEC56109DA8413 | not-applicable | docs/architecture-migration/evidence/build-baseline.md |
| 27 | `??` | untracked | present | 17136 | 53BC6B2BF14E2604BC10F9E1D5BBECABE3EF38E48C36EBAC9E7A2ABD38EC6940 | not-applicable | docs/architecture-migration/evidence/codegraph-baseline.md |
| 28 | `??` | untracked | present | 21489 | 66D3393C5436BFC2DC37C0F7C40FD393D575C6D251C362AD43903A9C5670F327 | not-applicable | docs/architecture-migration/evidence/dossier-gate.md |
| 29 | `??` | untracked | present | 3763 | 07DCC2C6CC64FD655E5D424C462EA59151B71D04080EBB6ECE30C38BDF76D8A6 | not-applicable | docs/architecture-migration/evidence/environment.md |
| 30 | `??` | untracked | present | 10220 | F70EE29B8AF6CE70A6D9E146E67F076E404264EA469ABA30D714AA61B44DFA5E | not-applicable | docs/architecture-migration/evidence/final-verification-f1-plan-compliance.md |
| 31 | `??` | untracked | present | 16780 | 67F289186B4AFC0D59EB7F99FB95818E96FEC9D9B7A0C783F7F92318447BD1B1 | not-applicable | docs/architecture-migration/evidence/final-verification-f2-dossier-quality.md |
| 32 | `??` | untracked | present | 7041 | 2CA3F1D310D36FC06421753C4D0965F9A3CCA93CF004B15E3BF42F7FD698E831 | not-applicable | docs/architecture-migration/evidence/final-verification-f3-runtime-qa.md |
| 33 | `??` | untracked | present | 18068 | 57E13C80B558F7B766FC73B46E7226625ADAF4056BC467A11A5BDC51A6188488 | not-applicable | docs/architecture-migration/evidence/final-verification-f4-scope-fidelity.md |
| 34 | `??` | untracked | present | 13562 | 69CBF258BF03EF8396E1F7275791D1F1C87066F188ED59DD8713D47659CD53CC | not-applicable | docs/architecture-migration/evidence/final-verification.md |
| 35 | `??` | untracked | present | 11485 | 3C5B918FF4219D9559F0449816C40EABE26CE5D34124D829B8576DFDC031FD8E | not-applicable | docs/architecture-migration/evidence/metrics-baseline.json |
| 36 | `??` | untracked | present | 16787 | 57D7C28ECFA6718BE4F9C55F0E092C52A7B816CF7D29A65FD3FA1B20660E68D0 | not-applicable | docs/architecture-migration/evidence/model-validation.md |
| 37 | `??` | untracked | present | 9948 | F22F37DAEA72728B6DD87D83F96B753F51B34175419A0E01EDE347E0424244C4 | not-applicable | docs/architecture-migration/evidence/persistence-fixtures.md |
| 38 | `??` | untracked | present | 15481 | 132CC26480A843A8AF58A8792057479D1390916FB94B8E4DCC2C0A1DAC81A3D3 | not-applicable | docs/architecture-migration/evidence/repository-snapshot.md |
| 39 | `??` | untracked | present | 6318 | AE4E7C9B576A6578B27B6FD5DC40C3A3386EC6B38EE1177BF8DB62608A81B44A | not-applicable | docs/architecture-migration/evidence/test-baseline.md |
| 40 | `??` | untracked | present | 2010737 | B82D31B2D294BC40042813A4648B2AC380322FE42FBD653BBA3C9561FC3EE164 | not-applicable | docs/architecture-migration/evidence/test-results/phase-0-f3.trx |
| 41 | `??` | untracked | present | 2010737 | FF4FD7AD83C7FDBD90A11C6FFB1A6348E487E64A03F225996E475F1C5533060D | not-applicable | docs/architecture-migration/evidence/test-results/phase-0.trx |
| 42 | `??` | untracked | present | 11214 | A8E5BD8BD2A9F9840752021895B06CC94F7DA903B212E50039D11F67BFB757DA | not-applicable | docs/architecture-migration/evidence/user-flow-baseline.md |
| 43 | `??` | untracked | present | 0 | E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855 | not-applicable | docs/architecture-migration/maps/.gitkeep |
| 44 | `??` | untracked | present | 60839 | D7CD9620F36D8E15A9D03EFC56726A3CCAFE6EB4A6BA92F4265543423AD58A8F | not-applicable | docs/architecture-migration/maps/architecture-model.baseline.json |
| 45 | `??` | untracked | present | 7427 | A9A27DCCD0E6FD0DFF9582A36D3A139BECB3851AAE0DE8EF4939AF4B0973EA70 | not-applicable | docs/architecture-migration/maps/architecture-model.schema.json |
| 46 | `??` | untracked | present | 8548 | 883B70E9E2DC7FB834A9B6EB4F0C278B75848DFE985D4571790D4065E0DDD3BF | not-applicable | docs/architecture-migration/maps/characterization-tests.md |
| 47 | `??` | untracked | present | 15517 | 03E9D59E22C40E38CC220D6098496EE7AC4BF3B43EEC2F272E7386EAF49C8142 | not-applicable | docs/architecture-migration/maps/compile-time.md |
| 48 | `??` | untracked | present | 30646 | 6C1EF70B9BE28DFA851E5CB876B6814E074EC44CF96FE6E7D5FF0D996CB3AEC7 | not-applicable | docs/architecture-migration/maps/di-runtime.md |
| 49 | `??` | untracked | present | 44521 | 09682A1BC9C019C2389110FD0181467D1C9A701A4DD500081723AC9DEBF62BBA | not-applicable | docs/architecture-migration/maps/persistence-compatibility.md |
| 50 | `??` | untracked | present | 7941 | 61D527EABFA09667B0FF9EC86E766715DEAE46764157924548DF97A6A7181A0E | not-applicable | docs/architecture-migration/maps/persistence.md |
| 51 | `??` | untracked | present | 7517 | 7F36380F1A6AF7D9DBDE1E442771BEA43F70B3C6B9135E9920B08CE9138D916F | not-applicable | docs/architecture-migration/maps/reactive.md |
| 52 | `??` | untracked | present | 12737 | 10DDB3D843482E9901FDD9CC07F0E18E64BF91DBE9B6F75551B1583756D7FCA4 | not-applicable | docs/architecture-migration/maps/state-inventory.md |
| 53 | `??` | untracked | present | 4453 | CAF6013A14204704EFC9DDD96A2EC6891A39A0F51DC50656021CA2B1B8FB3C0C | not-applicable | docs/architecture-migration/maps/state-ownership.md |
| 54 | `??` | untracked | present | 15265 | 284A4832430E3E3527798E23611B96EEE4C2C2000178D17E0CBF1C0998BA6FD8 | not-applicable | docs/architecture-migration/maps/target-invariants.md |
| 55 | `??` | untracked | present | 4968 | C74AC8677015FB04A30BFE16AA415FB2D182B5EC69F9C10E707A8BD944D6DDC7 | not-applicable | docs/architecture-migration/maps/user-flow.md |
| 56 | `??` | untracked | present | 0 | E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855 | not-applicable | docs/architecture-migration/plans/.gitkeep |
| 57 | `??` | untracked | present | 45964 | BB6F92470A4BF786FE90F8A86F2B34F3B04BEE3C5AC2654C9A45AEB75F87CC6E | not-applicable | docs/architecture-migration/plans/phase-0-baseline.md |
| 58 | `??` | untracked | present | 84570 | 2C056AAFCE062E3E749EC9961E0B55237C4667D8CFFEF5F438CE7F108C2E452E | not-applicable | docs/architecture-migration/plans/phase-0.5-model-driven-architecture-widget.md |
| 59 | `??` | untracked | present | 20448 | 568538ACB6299001FA44C7AB2E3533ABD5C9DA10CECBADE23DA77DCE850BD490 | not-applicable | docs/architecture-migration/widget-spec.md |
| 60 | `??` | untracked | present | 41811 | 473A60DF6133B3D4459F5914721471B01160B19EBCBB4BDF05E43F1FD9214F28 | not-applicable | docs/architecture-migration/правка архитектуры.jpg |
| 61 | `??` | untracked | present | 278 | 2BAC467D565D5BAA5B603366F7CE7C10F646DFAEA74FD384263E589D518E0747 | not-applicable | docs/architecture-migration/правка архитектуры.txt |
| 62 | `??` | untracked | present | 179465 | 9F82A062FC61EA3CEEFBC283E846A414196A580744A3350701B1BC9C6372D0A1 | not-applicable | Презентация/готовые/РЕХАУ Калькулятор снеготаяния — обзорная презентация.pptx |

Every listed path was observed as a leaf file when present. No directory-valued porcelain row occurred; a directory row is rejected instead of recursively flattened.

## Comparison with Phase 0 Snapshot

Phase 0 captured 30 rows (10 tracked, 20 untracked, 28 present, 2 deleted) at `f0d19c34ac03075d64548f1059e9c6626d3596b5`. This execution capture has 62 rows (10 tracked, 52 untracked, 60 present, 2 deleted) at the same HEAD.

- Tracked statuses and all ten tracked path hashes match the Phase 0 ledger, including both deleted presentation paths.
- The net delta is +32 untracked dossier, evidence, map, plan, and widget-spec paths produced after Phase 0. They are pre-existing owner content at this Task 1 capture, not Phase 0.5 outputs.
- The Phase 0 snapshot receipt itself has hash drift because its contents were augmented by completed Phase 0 evidence work before this capture; its current hash is recorded in the ledger rather than assumed equal to its initial receipt hash.
- No renames were reported in either execution snapshot. Current hashes are facts at the capture instant, not claims that every older Phase 0 untracked hash remains equal.

## Independent Data-Surface QA

A fresh PowerShell process reran the capture, parsed current NUL porcelain independently, normalized each row as `XY|path|source|class|presence|bytes|sha256|HEAD-blob`, and compared the sorted ledger and raw NUL input hash.

| Assertion | Result |
| --- | --- |
| root is exactly D:/IA/ace v.2 | pass |
| 62 initial rows equal 62 independent rows | pass |
| normalized row equality | pass |
| raw NUL input SHA-256 equality | pass |
| all current present paths rehashed by independent capture | pass |
| synthetic deleted retains XY and deleted state | pass |
| synthetic rename retains XY | pass |
| synthetic Cyrillic path preserves literal text | pass |
| synthetic absent upstream records `<absent>` | pass |
| synthetic untracked directory is rejected | pass |
| malformed input is rejected | pass |
| forced row-count mismatch returns blocked in copied/test state | pass |

## Adversarial Probes

| Probe | Result |
| --- | --- |
| `stale_state` | pass: identity binds live root, HEAD, branch, upstream, UTC instant, exact NUL input SHA, and independent recomputation. |
| `dirty_worktree` | pass: every pre-write porcelain row maps exactly once to a protected ledger row; every present path has byte size and SHA-256. |
| `misleading_success_output` | pass: success required exit codes plus parsed root, row count, normalization, and hash equality, not command text alone. |
| Deleted / renamed / Cyrillic / absent upstream | pass on copied synthetic parser input; no canonical repository input was mutated. |
| Untracked directory / malformed input | rejected; forced mismatch produced `blocked` in copied/test state. |

## Limitations and Cleanup

- Git text porcelain display can quote non-ASCII paths and this terminal rendered some Cyrillic text with a legacy code page. NUL-delimited parsing and `-LiteralPath` hashing supplied the authoritative identity; the ledger preserves actual Unicode paths.
- Browser availability is a detection-only fact: the probes found no resolvable Playwright/Puppeteer module and no PATH browser executable. No claim of runnable browser QA is made; Task 6 must gate it.
- This point-in-time ledger does not protect against owner edits after the capture; later tasks must compare against its rows rather than infer ownership from a later status.
- Output SHA-256 uses a reproducible normalized form: UTF-8 receipt bytes with the Output SHA-256 value blanked, avoiding an impossible self-referential fixed point. All immutable input hashes are recorded.
- Temporary scripts, copied capture output, recomputation output, and QA output were confined to `C:/Users/Admin/AppData/Local/Temp/opencode` and are removed after final status verification. No long-lived process was started.

## Final Status

`status: pass`

The protected pre-write boundary has been captured. This receipt is the only Phase 0.5-created repository output from Task 1; Task 2 was not started.
