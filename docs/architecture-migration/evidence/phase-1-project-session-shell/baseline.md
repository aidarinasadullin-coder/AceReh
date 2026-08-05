---
phase: phase-1-project-session-shell
task: 1
status: PASS
captured_at_utc: 2026-08-04T14:30:43.9478940Z
canonical_root: D:/IA/ace v.2
working_directory: D:/IA/ace v.2
head: 021d4abd159aa71c4a19c7a6536851264e5a58ca
branch: master
upstream: origin/master
ahead: 5
behind: 0
active_plan: docs/architecture-migration/plans/phase-1-project-session-shell.md
active_plan_sha256: 011594E3AB70787CCD0D49893458F70125C143EB3BD74545680712EA6AED1948
---

# Phase 1 ProjectSession Shell Baseline

## Scope and Result

This is the Task 1 pre-edit baseline. No production source, test source, project file, package/configuration file, map, widget, plan, or pre-existing dirty path was edited, staged, reverted, cleaned, stashed, or overwritten. The only created paths are this new evidence directory and its evidence artifacts.

The live repository identity is `D:/IA/ace v.2` at full HEAD `021d4abd159aa71c4a19c7a6536851264e5a58ca`, on `master` tracking `origin/master`, with `ahead=5` and `behind=0`. The approved plan hash was rechecked as `011594E3AB70787CCD0D49893458F70125C143EB3BD74545680712EA6AED1948`.

## Exact Commands and First Outcomes

All Git commands below were run with the required PowerShell prefix `$env:GIT_MASTER='1';`; no command was retried to conceal its first result.

| Command | CWD | Exit | Outcome |
| --- | --- | ---: | --- |
| `$env:GIT_MASTER='1'; git rev-parse --show-toplevel` | `D:/IA/ace v.2` | 0 | `D:/IA/ace v.2` |
| `$env:GIT_MASTER='1'; git rev-parse HEAD` | `D:/IA/ace v.2` | 0 | `021d4abd159aa71c4a19c7a6536851264e5a58ca` |
| `$env:GIT_MASTER='1'; git branch --show-current` | `D:/IA/ace v.2` | 0 | `master` |
| `$env:GIT_MASTER='1'; git rev-parse --abbrev-ref '@{upstream}'` | `D:/IA/ace v.2` | 0 | `origin/master` |
| `$env:GIT_MASTER='1'; git rev-list --left-right --count '@{upstream}...HEAD'` | `D:/IA/ace v.2` | 0 | `0 5` (`behind ahead`) |
| `$env:GIT_MASTER='1'; git status --porcelain=v1 -z --untracked-files=all > docs/architecture-migration/evidence/phase-1-project-session-shell/baseline-git-status.bin` | `D:/IA/ace v.2` | 0 | lossless pre-command dirty inventory |
| `dotnet build src/SnowMeltingCalculator.csproj -c Debug` | `D:/IA/ace v.2` | 0 | build succeeded, 0 warnings, 0 errors |
| `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Debug --filter "FullyQualifiedName~MainViewModelTests|FullyQualifiedName~ResetOrchestrationTests|FullyQualifiedName~ResultsViewModelOpenProjectTests|FullyQualifiedName~CircuitsViewModelEventLeakTests|FullyQualifiedName~DoubleCalculationPreventionTests" --logger "console;verbosity=detailed"` | `D:/IA/ace v.2` | 0 | 78 executed, 77 passed, 1 skipped |
| `dotnet test tests/SnowMeltingCalculator.Tests/SnowMeltingCalculator.Tests.csproj -c Release --logger "trx;LogFileName=docs/architecture-migration/evidence/phase-1-project-session-shell/baseline-tests-release.trx" --logger "console;verbosity=normal"` | `D:/IA/ace v.2` | 0 | 1538 executed, 1537 passed, 1 skipped; complete console log retained |
| `dotnet build src/SnowMeltingCalculator.csproj -c Release` | `D:/IA/ace v.2` | 0 | build succeeded, 0 warnings, 0 errors |
| `$env:GIT_MASTER='1'; git status --porcelain=v1 -z --untracked-files=all > docs/architecture-migration/evidence/phase-1-project-session-shell/post-baseline-final-git-status.bin` | `D:/IA/ace v.2` | 0 | post-command drift basis |

The Release TRX logger interpreted the supplied path relative to `tests/SnowMeltingCalculator.Tests/TestResults` and created a nested temporary `docs/.../baseline-tests-release.trx` there. It was removed immediately after the first run. No test was rerun. `TestResults/docs` no longer exists. The raw Release console log is therefore the authoritative complete test output for this baseline.

## Protected Dirty Manifest

`baseline-git-status.bin` is the complete, NUL-safe authority for every pre-existing modified, deleted, and untracked path. It is a direct byte-for-byte `git status --porcelain=v1 -z --untracked-files=all` stream, UTF-8 filenames with NUL record delimiters, not console-decoded text.

| Manifest item | Value |
| --- | --- |
| Raw artifact | `baseline-git-status.bin` |
| Bytes | 14302 |
| SHA-256 | `16638FDC1B80AFC51E7211D9762166D1261CD1BD3EA654E9364BF4D3A9B2B27D` |
| Total non-empty status records | 247 |
| Phase-1 artifact record already present in raw stream | 1 (`?? docs/architecture-migration/evidence/phase-1-project-session-shell/baseline-git-status.bin`) |
| Protected pre-existing records | 246 |
| Protected tracked modified/deleted records | 218 records: 216 modified and 2 deleted; no Git rename/copy companion path records are present |
| Protected untracked records | 28 |
| Protected deleted tracked paths | 2 |
| Protected staged records | 0 |

Status classes are encoded per raw record exactly as Git emitted them: ` M` means pre-existing unstaged tracked modification, ` D` means pre-existing tracked deletion, and `??` means pre-existing untracked path. Thus the raw manifest distinguishes every protected path and status without filename loss, including Cyrillic filenames and whitespace-bearing names. It includes the specifically protected `.gitignore`, `src/SnowMeltingCalculator.csproj`, `docs/architecture-migration/TASK_CONTEXT.md`, extensive `src/` and `tests/` changes, installer/publish/presentation files, `.omo` state, and untracked dossier paths.

## Fixture Presence and Hashes

All accepted fixture classes are present. The v1.0 assertion-backed fixture is unchanged from the accepted Phase 0 ledger. All 15 catalogued v1.1 compatibility inputs are present but are protected pre-existing dirty inputs at this live baseline; their live hashes below are authoritative for Phase 1 comparison and intentionally differ from the historical Phase 0 ledger where applicable.

| Class | Presence | Count | Hash evidence |
| --- | --- | ---: | --- |
| v1.0 assertion-backed fixture | present | 1 | `tests/SnowMeltingCalculator.Tests/Fixtures/v1-sample.smc`, 3500 bytes, `BA05531935C25AE9A6DCA70157C0CDCC891245BB3C83C436C3F3DA2632F6D113` |
| v1.1 compatibility fixtures | present, pre-existing modified/protected | 15 | live per-path hashes below |

| v1.1 path | Bytes | Live SHA-256 |
| --- | ---: | --- |
| `Тест/1.smc` | 11362 | `96275919E5CDC738291029C2926F16B08A4648DEBF24205A8FBBEEC94DDE5060` |
| `Тест/9-1000000_20260725.smc` | 4111 | `088AA2FFB2AC130C77DA419A9E3AF75AE00904BD543C9B53FECF78A0EA977D79` |
| `Тест/_20260724.smc` | 39185 | `E5168A9CDEF3EC71BBB721E17FAF4DBED99D57E52790302B982CC4B2CD82AB2A` |
| `Тест/Екат 1.smc` | 49975 | `2BF792092F5335AF6D17D43612995ADC0262773C08EE31CDB60B79652CC13372` |
| `Тест/Екат для версии 1.1.smc` | 49433 | `B565EEF136E70387A847A7FAEE12D25BDB6ED46C146515C499C5DC0354606F0B` |
| `Тест/Екат.smc` | 50205 | `54BA24ADD8A5B56F0C091C8436D955BFC4FBC947BCBC04078C9FACEE31BA4935` |
| `Тест/Пермь площадка.smc` | 27786 | `DF0610386587662535D3F340889589EA7B464C47CB77BFCC9549B4772F8C57C0` |
| `Тест/перм.smc` | 41471 | `7A8EA56852045EA469FFD23BCDBB747EAB6F158172FC1331D59D660CFD1FBA51` |
| `Тест/тест 1.smc` | 22995 | `3538BB26043D7002612BC26DB6860AA66E202BC663C4DF164374206F989F654E` |
| `Тест/тест 10.smc` | 19556 | `C194159085BD5EC7253ED259DAEF8B9327DAFAB464B03755A2A32F3177C9E56A` |
| `Тест/тест 2.smc` | 23025 | `7B8C9D9090DBA7E0B0D00DD7840A651EB83A1DAB8C7B6D5B45A71442C8FA9BB4` |
| `Тест/тест 3.smc` | 22983 | `9AD9991CD9E0DD2A520566CDE0C77533A4E925E66DBB2723E5DC519129A853DD` |
| `Тест/тест 4.smc` | 22979 | `CD8A8818B7ACF9EC05C84E461E96B77F14D59913BB6E8E3DE03BC6F1F22BAE71` |
| `Тест/ушалы 2.smc` | 41582 | `1A89AAEE4C63B5CE15ACF5714ADA73A54926C01D500D48BDD50CE0FB3039791D` |
| `Тест/ушалы.smc` | 41658 | `79E37870767205BA4F5229365C9B4C07F82CBF25357C1781E588B0827BF01EA7` |

## Executable Behavior Baseline

The lifecycle filter genuinely executed nonzero tests: 78 total, 77 passed, 1 skipped. The skipped test is `ResultsViewModel_LoadsRealProject_TwoCollectorsSummaryCardsMatchFile`; it reports that the optional F5 smoke fixture `D:\IA\ace\Тест\тест 40.smc` is absent. This is recorded as an existing baseline limitation, not hidden by retry or substitution.

The full Release suite genuinely executed 1538 tests: 1537 passed and the same one optional F5 smoke test was skipped. Both builds report 0 warnings and 0 errors. These green results establish only the recorded executable baseline and do not prove future behavior preservation.

## Evidence Artifact Hashes

| Artifact | Bytes | SHA-256 |
| --- | ---: | --- |
| `baseline-git-status.bin` | 14302 | `16638FDC1B80AFC51E7211D9762166D1261CD1BD3EA654E9364BF4D3A9B2B27D` |
| `baseline-build-debug.log` | 874 | `7E0AE47206661CC4A913EEED94ABFAA0C1E5C17E311DFC389EAF442ADA91C046` |
| `baseline-build-release.log` | 878 | `389E754C9CDD4CBA4D043B20DF4A0776906F1F481327B986CB88225748300224` |
| `baseline-lifecycle-tests-debug.log` | 17732 | `39EAE81402FCFA2E8D18E023DE42C5D8C644A0F573E233FA8D23FF3B708B06E7` |
| `baseline-tests-release.log` | 230102 | `A599C79B58CFE7A494CA63CE1F7CA674DA3482825E4C570F34E6C6F9A3BB5096` |
| `post-baseline-git-status.bin` | 14401 | `01597670361ED421B1D75E4295BCD0A97E58E6AA1F315A365CB6F3E2928A9426` |
| `post-baseline-final-git-status.bin` | 14506 | `350B10D264DCD26841D5276D5F8CA6A086C2D27C5ADBF13EFBAEF4371DA47149` |
| `final-git-status.bin` | 14679 | `0400D604A99B8CFF2171D091DFCB9CA3971C3DB627C3219F689685519A17EFCF` |

## Dirty-Worktree Drift Check and Cleanup

The final NUL-safe status (`final-git-status.bin`) was compared to the baseline after symmetrically excluding only this Task 1 evidence directory. The comparison found `protected_removed=0` and `protected_added=0`: all 246 pre-existing protected status records were retained exactly. The differing raw-stream hashes are expected because the final stream includes the four new allow-listed evidence paths.

The first post-command snapshot exposed one apparent missing protected record because the initial raw stream already contained `baseline-git-status.bin`, while the comparison initially excluded all Phase 1 evidence only on the post side. The final symmetric exclusion corrected that comparison; no protected record was actually removed. The accidental nested TRX output under `tests/SnowMeltingCalculator.Tests/TestResults/docs` was cleaned, and the final check confirmed that directory does not exist.

## DoneClaim

**DoneClaim PHASE1-TASK1-BASELINE-01:** Task 1 is complete. The live Git identity, full NUL-safe dirty boundary, protected path statuses, accepted fixture presence/live hashes, first build/test outcomes, raw logs, cleanup, and final drift comparison have been captured exclusively under `docs/architecture-migration/evidence/phase-1-project-session-shell/`. The final comparison confirms zero removed and zero added pre-existing protected status records. No production or test path was changed.
