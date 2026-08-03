---
phase: phase-0.5-model-driven-architecture-widget-v2-amendment
captured_at_utc: 2026-07-31T21:11:54.175Z
canonical_root: D:/IA/ace v.2
head: f0d19c34ac03075d64548f1059e9c6626d3596b5
branch: master
upstream: origin/master
active_plan_sha256: B6DF07E7B150F6830A3EE3A4CDBA9B441D05E5C4F475A594DC1C83208D5F3536
porcelain_nul_sha256: D249112520428DC3C6A76CC97EA3E58EA0753D61B6FFE62256744F464FC1E94E
terminal_status: BLOCKED
---

# Phase 0.5 v2 Amendment Repository Snapshot

## Capture Contract

This is the Task 1 execution-time, hash-bound pre-write boundary. It was captured before this receipt existed. Every `git` command below was read-only and prefixed with `$env:GIT_MASTER='1';`. The authoritative source is `git status --porcelain=v1 -z --untracked-files=all`; the NUL stream SHA-256 is recorded above. `-z` avoids display quoting, supports Unicode and spaces, and permits deterministic rename source/destination handling.

| Fact | Value |
| --- | --- |
| Invocation cwd / Git root | `D:/IA/ace v.2` / `D:/IA/ace v.2` |
| Capture UTC | `2026-07-31T21:11:54.175Z` |
| HEAD / branch / upstream | `f0d19c34ac03075d64548f1059e9c6626d3596b5` / `master` / `origin/master` |
| Active amendment plan SHA-256 | `B6DF07E7B150F6830A3EE3A4CDBA9B441D05E5C4F475A594DC1C83208D5F3536` (match) |
| Protected v1 schema SHA-256 | `7AFBCE3CFADC8B77443462E52D0BB15C7BDD31569DE41FADCAAE941791B07194` (match) |
| Protected v1 model SHA-256 | `EE9E0069D71F3ABFFA5FA1E9B698A97507694597FBE85B84C272C30D3F55EFBD` (match) |
| Logical porcelain rows | `70`: 10 tracked, 60 untracked; 68 present, 2 absent; 0 live rename/copy rows |
| Protected mismatches at capture | `0`; post-capture boundary drift detected before acceptance |
| Receipt write-set | exactly this absent-before path |

## Commands, Tools, and Exit Codes

```powershell
$env:GIT_MASTER='1'; git rev-parse --show-toplevel
$env:GIT_MASTER='1'; git rev-parse HEAD
$env:GIT_MASTER='1'; git branch --show-current
$env:GIT_MASTER='1'; git rev-parse --abbrev-ref '@{upstream}'
$env:GIT_MASTER='1'; git status --porcelain=v1 -z --untracked-files=all
$env:GIT_MASTER='1'; git --version
node --version
dotnet --version
node --check docs/architecture-migration/widget/verify-widget.mjs
```

All commands exited `0`. Results: Git `2.53.0.windows.1`; Node `v24.14.0`; .NET SDK `8.0.418`; `verify-widget.mjs` syntax availability check passed. No package/tool installation occurred. Existing Task 3 validator is present and syntax-valid; generic Draft 2020-12 package-validator availability remains `degraded` as documented by the accepted v1 receipt.

## Exact Parsed Porcelain Ledger

Each row is one logical porcelain record. `absent` means the path did not exist at capture; it is not a missing ledger entry. `head_blob` is `not-applicable` for untracked paths. A live rename/copy would produce two ledger identities, `destination` then `source`; none occurred.

| # | XY | tracked | presence | bytes | SHA-256 | head_blob | path |
| --- | --- | --- | --- | ---: | --- | --- | --- |
| 1 | ` M` | tracked | present | 557 | D10AD9C4EC1E58BDDA653D97F5CDEF67A0DF3FE6A0C0CC3CDD0AFDD4413213B5 | 2e8d4043a9d0305de2e27c7afb0a4f5214765db5 | .gitignore |
| 2 | ` M` | tracked | present | 2958 | B31B715DFEBDA9986EAFF0D2682B465B044CCE85EF9E209EE2A905422C48A37D | c3bb4bf0722f0ec9a033aae781d001bf85da90a7 | installer/SnowMeltingCalculator.iss |
| 3 | ` M` | tracked | present | 44306 | 5138D80AE4312E891118C9FB9302F96DE472F7B8E9F4796088C82BE2D158671A | ca4dab55215d3729dcf1179a4549bdb6a718c3ba | publish/SnowMeltingCalculator.deps.json |
| 4 | ` M` | tracked | present | 354188 | 58781E6DBD3CCF946B3BC49A07C96C7CBF8D49230BB440AC7A9491ADE504C864 | a9ae7f11a1b2a191a877fb1b45bdf091c77dc048 | publish/SnowMeltingCalculator.pdb |
| 5 | ` M` | tracked | present | 1901 | 8BED932038DB57DF26C9664B3A533189D28DAC1DAF9796C717E0D8D3C20DC5D7 | e2e04a5e254f62eabbb24c9cf3b4e3ae4dfc2a10 | src/SnowMeltingCalculator.csproj |
| 6 | ` M` | tracked | present | 1022040 | B9F72C7826A13BB1D137AAE5DC59631DAB76A122B1DD39613EBCE202D9860430 | b978caeb7f7a8ad1a26152e05ce6fbbb3c52592b | Презентация/готовые/РЕХАУ Калькулятор снеготаяния — быстрый старт.pptx |
| 7 | ` D` | tracked | absent | absent | absent | 6e9fcbb502dd19dd1bc058923ab951cff2b28c7c | Презентация/готовые/РЕХАУ Калькулятор снеготаяния — дизайн-шаблон.pptx |
| 8 | ` D` | tracked | absent | absent | absent | f40c6810ee03c8b107e829d47894107f6e765db7 | Презентация/готовые/РЕХАУ Калькулятор снеготаяния — презентация плюсы программы.pptx |
| 9 | ` M` | tracked | present | 1327747 | 990F3E0873B0BDA275829AFD91948952FF3D020B07108E6BCD930EB84766E2C3 | 7e6fab1da4fa31b7b9e5bbd0a58e1d9d9f7a93ec | Презентация/готовые/РЕХАУ Калькулятор снеготаяния — техническая презентация.pptx |
| 10 | ` M` | tracked | present | 53052 | 358A14BC1A9826D0EC2FA62BD3977A40F3D3D80CCE33D7B87CCA50C32CACD119 | 3c94a3fab5c4a62b0de6828c87793db09a161ebb | Презентация/готовые/РЕХАУ Калькулятор снеготаяния — шпаргалка A4.pptx |
| 11 | `??` | untracked | present | 1315 | 7C19046A057449A84863C6EA8BCFB8C86896BA453A4FB955F645C4AA41D5DF24 | not-applicable | .opencode/commands/architecture-approve.md |
| 12 | `??` | untracked | present | 2140 | 0A9ADA6964A745A8002108633692350C164783166E8EA4081BA5867D8019A2B7 | not-applicable | .opencode/commands/architecture-draft.md |
| 13 | `??` | untracked | present | 5701 | AE9EE8E726826219A21B7B554615FD850C44AC5FEE3D4CBBA5655D12A9B28169 | not-applicable | .opencode/commands/architecture-plan.md |
| 14 | `??` | untracked | present | 2083 | 1871B83ED2407DC125E5B7E5E97712D61177880F5E28ABD929F50EFCD0781283 | not-applicable | .opencode/commands/architecture-resume.md |
| 15 | `??` | untracked | present | 2392 | F1377450F812AD0CFE9F214C1AE58672EDA8B55EBFBF7D9555884D5119CA1FAA | not-applicable | .opencode/commands/architecture-start.md |
| 16 | `??` | untracked | present | 606 | AD77BBAC1EB500786D196F52FBE52E637FA7DE7332E9AB4E23FF7C6FED8FD8FC | not-applicable | AGENTS.md |
| 17 | `??` | untracked | present | 3772 | F852D7E5736790155B4F87252E55E09756E3E5AE162272A103A35914019D89A3 | not-applicable | docs/architecture-migration/AGENTS.md |
| 18 | `??` | untracked | present | 69539 | 0CF634642A3173494C59DB2ABE4F33E92C64C8415EBEED815B4A05BC8A22F5B1 | not-applicable | docs/architecture-migration/TASK_CONTEXT.md |
| 19 | `??` | untracked | present | 9405 | E2608604D550D6EBF9C4E7A9197D425C876AD7727D5D2BAD2A43A49F4A5F2296 | not-applicable | docs/architecture-migration/architecture_audit.md |
| 20 | `??` | untracked | present | 37294 | D6F1925E188FD9E8D1485D44F040C41781580A072B5A2DA8EA7FE2B4752930CA | not-applicable | docs/architecture-migration/architecture_widget.html |
| 21 | `??` | untracked | present | 0 | E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855 | not-applicable | docs/architecture-migration/archive/.gitkeep |
| 22 | `??` | untracked | present | 37294 | D6F1925E188FD9E8D1485D44F040C41781580A072B5A2DA8EA7FE2B4752930CA | not-applicable | docs/architecture-migration/archive/architecture_widget.phase-0-historical.html |
| 23 | `??` | untracked | present | 31664 | B03F920FD04AF56E691B2C22322B9ABF61C1F8B846DAA8277ED4C84B75FB7A2C | not-applicable | docs/architecture-migration/archive/phase-0-baseline.invalidated-explore-chain.md |
| 24 | `??` | untracked | present | 83945 | 80805490E389C602F5158DEE45BDEC6B6F4C58CC8C8B7B4F9FEA447E4239C463 | not-applicable | docs/architecture-migration/audit_metrics.json |
| 25 | `??` | untracked | present | 0 | E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855 | not-applicable | docs/architecture-migration/evidence/.gitkeep |
| 26 | `??` | untracked | present | 35337 | 90C0979AD6A116F01F05B4E12D2E2564F4057EC772F2F9F64FB2A9AABF62D2AA | not-applicable | docs/architecture-migration/evidence/audit-reconciliation.md |
| 27 | `??` | untracked | present | 3124 | BC265571430D348F9323EABEC53D474DBE6BBAD3020366E9A1FEC56109DA8413 | not-applicable | docs/architecture-migration/evidence/build-baseline.md |
| 28 | `??` | untracked | present | 17136 | 53BC6B2BF14E2604BC10F9E1D5BBECABE3EF38E48C36EBAC9E7A2ABD38EC6940 | not-applicable | docs/architecture-migration/evidence/codegraph-baseline.md |
| 29 | `??` | untracked | present | 21489 | 66D3393C5436BFC2DC37C0F7C40FD393D575C6D251C362AD43903A9C5670F327 | not-applicable | docs/architecture-migration/evidence/dossier-gate.md |
| 30 | `??` | untracked | present | 3763 | 07DCC2C6CC64FD655E5D424C462EA59151B71D04080EBB6ECE30C38BDF76D8A6 | not-applicable | docs/architecture-migration/evidence/environment.md |
| 31 | `??` | untracked | present | 10220 | F70EE29B8AF6CE70A6D9E146E67F076E404264EA469ABA30D714AA61B44DFA5E | not-applicable | docs/architecture-migration/evidence/final-verification-f1-plan-compliance.md |
| 32 | `??` | untracked | present | 16780 | 67F289186B4AFC0D59EB7F99FB95818E96FEC9D9B7A0C783F7F92318447BD1B1 | not-applicable | docs/architecture-migration/evidence/final-verification-f2-dossier-quality.md |
| 33 | `??` | untracked | present | 7041 | 2CA3F1D310D36FC06421753C4D0965F9A3CCA93CF004B15E3BF42F7FD698E831 | not-applicable | docs/architecture-migration/evidence/final-verification-f3-runtime-qa.md |
| 34 | `??` | untracked | present | 18068 | 57E13C80B558F7B766FC73B46E7226625ADAF4056BC467A11A5BDC51A6188488 | not-applicable | docs/architecture-migration/evidence/final-verification-f4-scope-fidelity.md |
| 35 | `??` | untracked | present | 13562 | 69CBF258BF03EF8396E1F7275791D1F1C87066F188ED59DD8713D47659CD53CC | not-applicable | docs/architecture-migration/evidence/final-verification.md |
| 36 | `??` | untracked | present | 11485 | 3C5B918FF4219D9559F0449816C40EABE26CE5D34124D829B8576DFDC031FD8E | not-applicable | docs/architecture-migration/evidence/metrics-baseline.json |
| 37 | `??` | untracked | present | 16787 | 57D7C28ECFA6718BE4F9C55F0E092C52A7B816CF7D29A65FD3FA1B20660E68D0 | not-applicable | docs/architecture-migration/evidence/model-validation.md |
| 38 | `??` | untracked | present | 9948 | F22F37DAEA72728B6DD87D83F96B753F51B34175419A0E01EDE347E0424244C4 | not-applicable | docs/architecture-migration/evidence/persistence-fixtures.md |
| 39 | `??` | untracked | present | 16617 | 1A7EFA08B09FCB5E078DAAEECB2708B1A6D1D1AC5EE6D587D567B105AC4ED39E | not-applicable | docs/architecture-migration/evidence/phase-0.5-acceptance.json |
| 40 | `??` | untracked | present | 10322 | C41928B190895AECE2879780F0F7DC01BB8B67590E049A2A1C8B308785E2D2DF | not-applicable | docs/architecture-migration/evidence/phase-0.5-historical-widget-preservation.md |
| 41 | `??` | untracked | present | 9133 | 0DDE16C9EA219857729D14E55ACB6E1DC4217ECCD78EB3D2DD07B2294F9FCB71 | not-applicable | docs/architecture-migration/evidence/phase-0.5-model-validation.md |
| 42 | `??` | untracked | present | 24073 | 23337A310CE81D8E89C65665B4499C9666663FA3486CF6C4C5C86F24E5B67349 | not-applicable | docs/architecture-migration/evidence/phase-0.5-repository-snapshot.md |
| 43 | `??` | untracked | present | 15481 | 132CC26480A843A8AF58A8792057479D1390916FB94B8E4DCC2C0A1DAC81A3D3 | not-applicable | docs/architecture-migration/evidence/repository-snapshot.md |
| 44 | `??` | untracked | present | 6318 | AE4E7C9B576A6578B27B6FD5DC40C3A3386EC6B38EE1177BF8DB62608A81B44A | not-applicable | docs/architecture-migration/evidence/test-baseline.md |
| 45 | `??` | untracked | present | 2010737 | B82D31B2D294BC40042813A4648B2AC380322FE42FBD653BBA3C9561FC3EE164 | not-applicable | docs/architecture-migration/evidence/test-results/phase-0-f3.trx |
| 46 | `??` | untracked | present | 2010737 | FF4FD7AD83C7FDBD90A11C6FFB1A6348E487E64A03F225996E475F1C5533060D | not-applicable | docs/architecture-migration/evidence/test-results/phase-0.trx |
| 47 | `??` | untracked | present | 11214 | A8E5BD8BD2A9F9840752021895B06CC94F7DA903B212E50039D11F67BFB757DA | not-applicable | docs/architecture-migration/evidence/user-flow-baseline.md |
| 48 | `??` | untracked | present | 0 | E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855 | not-applicable | docs/architecture-migration/maps/.gitkeep |
| 49 | `??` | untracked | present | 60839 | D7CD9620F36D8E15A9D03EFC56726A3CCAFE6EB4A6BA92F4265543423AD58A8F | not-applicable | docs/architecture-migration/maps/architecture-model.baseline.json |
| 50 | `??` | untracked | present | 113053 | EE9E0069D71F3ABFFA5FA1E9B698A97507694597FBE85B84C272C30D3F55EFBD | not-applicable | docs/architecture-migration/maps/architecture-model.json |
| 51 | `??` | untracked | present | 7427 | A9A27DCCD0E6FD0DFF9582A36D3A139BECB3851AAE0DE8EF4939AF4B0973EA70 | not-applicable | docs/architecture-migration/maps/architecture-model.schema.json |
| 52 | `??` | untracked | present | 8202 | 7AFBCE3CFADC8B77443462E52D0BB15C7BDD31569DE41FADCAAE941791B07194 | not-applicable | docs/architecture-migration/maps/architecture-model.widget.schema.json |
| 53 | `??` | untracked | present | 8548 | 883B70E9E2DC7FB834A9B6EB4F0C278B75848DFE985D4571790D4065E0DDD3BF | not-applicable | docs/architecture-migration/maps/characterization-tests.md |
| 54 | `??` | untracked | present | 15517 | 03E9D59E22C40E38CC220D6098496EE7AC4BF3B43EEC2F272E7386EAF49C8142 | not-applicable | docs/architecture-migration/maps/compile-time.md |
| 55 | `??` | untracked | present | 30646 | 6C1EF70B9BE28DFA851E5CB876B6814E074EC44CF96FE6E7D5FF0D996CB3AEC7 | not-applicable | docs/architecture-migration/maps/di-runtime.md |
| 56 | `??` | untracked | present | 44521 | 09682A1BC9C019C2389110FD0181467D1C9A701A4DD500081723AC9DEBF62BBA | not-applicable | docs/architecture-migration/maps/persistence-compatibility.md |
| 57 | `??` | untracked | present | 7941 | 61D527EABFA09667B0FF9EC86E766715DEAE46764157924548DF97A6A7181A0E | not-applicable | docs/architecture-migration/maps/persistence.md |
| 58 | `??` | untracked | present | 7517 | 7F36380F1A6AF7D9DBDE1E442771BEA43F70B3C6B9135E9920B08CE9138D916F | not-applicable | docs/architecture-migration/maps/reactive.md |
| 59 | `??` | untracked | present | 12737 | 10DDB3D843482E9901FDD9CC07F0E18E64BF91DBE9B6F75551B1583756D7FCA4 | not-applicable | docs/architecture-migration/maps/state-inventory.md |
| 60 | `??` | untracked | present | 4453 | CAF6013A14204704EFC9DDD96A2EC6891A39A0F51DC50656021CA2B1B8FB3C0C | not-applicable | docs/architecture-migration/maps/state-ownership.md |
| 61 | `??` | untracked | present | 15265 | 284A4832430E3E3527798E23611B96EEE4C2C2000178D17E0CBF1C0998BA6FD8 | not-applicable | docs/architecture-migration/maps/target-invariants.md |
| 62 | `??` | untracked | present | 4968 | C74AC8677015FB04A30BFE16AA415FB2D182B5EC69F9C10E707A8BD944D6DDC7 | not-applicable | docs/architecture-migration/maps/user-flow.md |
| 63 | `??` | untracked | present | 0 | E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855 | not-applicable | docs/architecture-migration/plans/.gitkeep |
| 64 | `??` | untracked | present | 45964 | BB6F92470A4BF786FE90F8A86F2B34F3B04BEE3C5AC2654C9A45AEB75F87CC6E | not-applicable | docs/architecture-migration/plans/phase-0-baseline.md |
| 65 | `??` | untracked | present | 30476 | B6DF07E7B150F6830A3EE3A4CDBA9B441D05E5C4F475A594DC1C83208D5F3536 | not-applicable | docs/architecture-migration/plans/phase-0.5-model-driven-architecture-widget-v2-amendment.md |
| 66 | `??` | untracked | present | 84570 | 2C056AAFCE062E3E749EC9961E0B55237C4667D8CFFEF5F438CE7F108C2E452E | not-applicable | docs/architecture-migration/plans/phase-0.5-model-driven-architecture-widget.md |
| 67 | `??` | untracked | present | 20448 | 568538ACB6299001FA44C7AB2E3533ABD5C9DA10CECBADE23DA77DCE850BD490 | not-applicable | docs/architecture-migration/widget-spec.md |
| 68 | `??` | untracked | present | 9245 | 028326ADBE781AF75C504E105694C8DFE59945BE3CC6501F0F98FC6861314905 | not-applicable | docs/architecture-migration/widget/architecture-widget.mjs |
| 69 | `??` | untracked | present | 8851 | 49EE87985A1F051CA7879385F29D0DCB4CDC8592A9AE1378D469B4B5A25CD3AF | not-applicable | docs/architecture-migration/widget/model-contract.mjs |
| 70 | `??` | untracked | present | 12070 | 2C316FD7D77268DFBD30A2AD5D6A232D47DC934D0ED1569626D98F6CF6B42197 | not-applicable | docs/architecture-migration/widget/verify-widget.mjs |

## Deterministic Reproduction and Task-Temporary QA

Happy-path reproduction: execute the Node capture used for this receipt from the repository root. It reads the raw NUL stream as bytes, parses every logical record, hashes each present leaf with `SHA-256`, checks the root/HEAD/branch/upstream/plan/schema/model values, and compares the resulting 70 normalized rows and raw-stream SHA to this ledger. A later Task must treat any changed, missing, additional, or retyped protected record as a mismatch.

```powershell
$env:GIT_MASTER='1'; node -e "const cp=require('child_process'),fs=require('fs'),crypto=require('crypto');const raw=cp.execFileSync('git',['status','--porcelain=v1','-z','--untracked-files=all']);const rows=raw.toString('utf8').split('\0').filter(Boolean);if(rows.length!==71)throw Error('expected post-receipt 71 rows');console.log(crypto.createHash('sha256').update(raw).digest('hex').toUpperCase())"
```

The happy-path command would have been expected to observe 71 rows after this receipt is written: its only additional row would have been this receipt. Before writing, the exact captured input was 70 rows and `D249112520428DC3C6A76CC97EA3E58EA0753D61B6FFE62256744F464FC1E94E`.

Task-temporary failure corpus ran outside the repository with a deterministic parser: (1) rename input produced destination and source identities, (2) delete produced `absent`, (3) Unicode plus a quoted-space path retained literal UTF-8, (4) a no-upstream synthetic branch yielded `<absent>`, and (5) an altered byte hash was rejected. Missing rename source and malformed porcelain were also rejected. Result: `7/7` assertions passed, `0` failed. The live repository has an upstream; the absent-upstream case is therefore a synthetic parser contract rather than a false live claim.

## Limitations

- The receipt is a point-in-time boundary. Owner changes after `2026-07-31T21:11:54.175Z` are detected only by a later comparison, not prevented.
- Git text porcelain can quote paths; only the recorded NUL stream is authoritative. No live rename/copy row existed, but the parser explicitly handles both identities.
- Generic Draft 2020-12 validation remains honestly `degraded`; no validator was installed. This Task only confirms Node/.NET/validator file availability.
- This Task did not run product tests, build, browser automation, or Task 2.

## Final Status

**BLOCKED**. Capture assertions passed `18/18`, with `0` protected hash mismatches at capture, but the post-write deterministic comparison found three additional untracked paths not in the captured 70-row ledger: `docs/architecture-migration/правка архитектуры.jpg`, `docs/architecture-migration/правка архитектуры.txt`, and `Презентация/готовые/РЕХАУ Калькулятор снеготаяния — обзорная презентация.pptx`. The live NUL stream became 74 rows (the three external additions plus this receipt), rather than the expected 71. This violates Task 1's no-other-path-changed acceptance criterion. No further work, including Task 2, was performed. The sole path written in this lane is `docs/architecture-migration/evidence/phase-0.5-v2-amendment-repository-snapshot.md`.
