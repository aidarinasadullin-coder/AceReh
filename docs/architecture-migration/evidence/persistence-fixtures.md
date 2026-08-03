---
phase: phase-0-baseline
snapshot_sha: f0d19c34ac03075d64548f1059e9c6626d3596b5
source_basis: working-tree
generated_at_utc: 2026-07-30T20:19:23.9695290Z
working_directory: D:/IA/ace v.2
commands:
  - Get-ChildItem -Recurse -File -Filter *.smc; per-path git ls-files --error-unmatch; Get-FileHash -Algorithm SHA256; Get-Content -Raw | ConvertFrom-Json
  - PowerShell recursive JSON path walk of tests/SnowMeltingCalculator.Tests/Fixtures/v1-sample.smc
exit_code: 0
status: pass
raw_output: Inline tracked-file ledger and read-only QA below.
limitations:
  - No .smc file was written, regenerated, or opened for write.
  - Parsing establishes syntax and observed version, not restore semantics, byte identity, support duration, atomicity, crash safety, or transactional restore.
---

# Persistence Fixture Receipt

## Tracked `.smc` provenance and classification

Filesystem-first enumeration plus `git ls-files --error-unmatch -- <path>` for each path reports 16 tracked, non-`bin`/`obj` inputs. This avoids the Windows PowerShell 5.1 console-decoding ambiguity of one bulk Unicode `git ls-files` result. The sole path under `tests/` is a test fixture because `ProjectRoundTripTests.FixturePath` resolves it and test symbols assert its fields. The 15 files under `Тест/` are checked-in compatibility inputs, not test fixtures: no test source path references them. All 16 were enumerated, tracked, hashed, parsed, and version-read read-only. To avoid irrelevant absolute paths, paths are repository-relative.

| ID | Repository-relative path | Bytes | SHA-256 | JSON parse | Version | Location/classification | Coverage |
| --- | --- | ---: | --- | --- | --- | --- | --- |
| SMC-01 | `tests/SnowMeltingCalculator.Tests/Fixtures/v1-sample.smc` | 3500 | `BA05531935C25AE9A6DCA70157C0CDCC891245BB3C83C436C3F3DA2632F6D113` | pass | `1.0` | `tests/`; assertion-backed legacy test fixture | `ProjectRoundTripTests.Load_v1_Fixture_PreservesCanonicalFields` |
| SMC-02 | `Тест/1.smc` | 11711 | `1094930F3FA328A644E996FC0F8055965881669A8A4E83DD2408F8EADF28E99C` | pass | `1.1` | checked-in compatibility input outside tests | no test-path reference observed |
| SMC-03 | `Тест/9-1000000_20260725.smc` | 4111 | `088AA2FFB2AC130C77DA419A9E3AF75AE00904BD543C9B53FECF78A0EA977D79` | pass | `1.1` | checked-in compatibility input outside tests | no test-path reference observed |
| SMC-04 | `Тест/_20260724.smc` | 40194 | `788A8E286116200305063FA642A22F015F26EC9B17607EDDCDEF4698F2FE7C91` | pass | `1.1` | checked-in compatibility input outside tests | no test-path reference observed |
| SMC-05 | `Тест/Екат 1.smc` | 51242 | `46E4FB41A299E51783C3D1B10080232D06F37B113BEE1FA88301D90ECFE89518` | pass | `1.1` | checked-in compatibility input outside tests | no test-path reference observed |
| SMC-06 | `Тест/Екат для версии 1.1.smc` | 50683 | `BA54E154EDB77A48A3D4026A508BB832B134D7F67B17D188A8A557DE86AC3010` | pass | `1.1` | checked-in compatibility input outside tests | no test-path reference observed |
| SMC-07 | `Тест/Екат.smc` | 51472 | `5890F86268EB505BC32AAE0CB3D6ACAAFB8B4F1FF0DF163895E4027B30BB9901` | pass | `1.1` | checked-in compatibility input outside tests | no test-path reference observed |
| SMC-08 | `Тест/Пермь площадка.smc` | 28519 | `2F481239E4D8CE15739570BE1434C85C20DD4A1A130A80A13CDF83CE42C1B645` | pass | `1.1` | checked-in compatibility input outside tests | no test-path reference observed |
| SMC-09 | `Тест/перм.smc` | 42538 | `A54610C3AE778C6D45C29D1486DF23169C587FA3DDA52AE2D2779ABEBE7E3DF1` | pass | `1.1` | checked-in compatibility input outside tests | no test-path reference observed |
| SMC-10 | `Тест/тест 1.smc` | 23606 | `BB3CF2E27EA114F7AC71C4B75191DC2E6EFA098FE5902F5DF4E4A62D7E849FFE` | pass | `1.1` | checked-in compatibility input outside tests | no test-path reference observed |
| SMC-11 | `Тест/тест 10.smc` | 20068 | `227723CFFECE801550B9103FBC989C808E3EA861875B0010F16B6C4C7F835061` | pass | `1.1` | checked-in compatibility input outside tests | no test-path reference observed |
| SMC-12 | `Тест/тест 2.smc` | 23636 | `D0CDDDA06804BF21802CB926009C341588FD44F5CDA9356617E7ED8081BB7EBC` | pass | `1.1` | checked-in compatibility input outside tests | no test-path reference observed |
| SMC-13 | `Тест/тест 3.smc` | 23594 | `DDD2C0AF9423C8E49A9C2DACE4BA4E8255818DD7F2E871A9ADB4CABAC350318F` | pass | `1.1` | checked-in compatibility input outside tests | no test-path reference observed |
| SMC-14 | `Тест/тест 4.smc` | 23590 | `A751208164D6A3E1E507A107819507FF69FFD21868EDB748DF9D7015A4FED313` | pass | `1.1` | checked-in compatibility input outside tests | no test-path reference observed |
| SMC-15 | `Тест/ушалы 2.smc` | 42651 | `35CEE0C93C108EF3A8E1292FB51D37ADB77CC0951FDFC3143FF7070186215C5B` | pass | `1.1` | checked-in compatibility input outside tests | no test-path reference observed |
| SMC-16 | `Тест/ушалы.smc` | 42727 | `C5D36490F4732E4A4AEBE243955953B63F09D9C885D03247724C3279828D13C9` | pass | `1.1` | checked-in compatibility input outside tests | no test-path reference observed |

The ledger stores concrete byte counts and SHA-256 values for all 16 inputs. Reproduction enumerates filesystem paths first, verifies each path is tracked, and compares every recorded byte/hash/version tuple with current content.

## Serializer, API, and test boundary facts

`ProjectFileService` uses camel-case property names, string enums, indentation, and `WhenWritingNull` ([ProjectFileService.cs](/D:/IA/ace%20v.2/src/Services/Project/ProjectFileService.cs:19)); `MaterialSnapshot` overrides names with snake_case attributes. The obsolete `SaveProjectAsync` appends `.smc`, returns `bool`, and catches all exceptions as `false`; obsolete `LoadProjectAsync` does not append an extension and returns `ProjectData?`/`null` ([ProjectFileService.cs](/D:/IA/ace%20v.2/src/Services/Project/ProjectFileService.cs:40)). Result save also appends `.smc` and returns `OperationResult<object?>.Failure(ex.Message, ex)`; Result load does not append an extension and returns `OperationResult<ProjectData>` with exact missing-file, null-deserialization, or `Ошибка десериализации: {ex.Message}` detail ([ProjectFileService.cs](/D:/IA/ace%20v.2/src/Services/Project/ProjectFileService.cs:115)). Both save methods pass cancellation only to `File.WriteAllTextAsync`; only Result load passes it to `File.ReadAllTextAsync`; broad `catch (Exception)` converts a resulting cancellation exception into failure/false/null.

| API / case | Exact evidence | Established assertion |
| --- | --- | --- |
| obsolete bool/null APIs | `SaveProjectAsync`/`LoadProjectAsync`, lines 40-112 | `ProjectRoundTripTests` exercises success, legacy, defaults, and detail round trips. |
| Result APIs | `SaveProjectResultAsync`/`LoadProjectResultAsync`, lines 115-190 | `ProjectFileServiceResultTests` asserts Result missing/corrupt/save and hydraulic DTO cases. |
| obsolete backup/cleanup | save lines 53-87 | `ProjectFileServiceAtomicityTests` asserts narrow obsolete-API temp cleanup and second-save backup. This is not transferred to Result API. |
| date non-mutation | serialization path | `ProjectFileServiceMutationTests` asserts supplied `ModifiedDate` is not mutated; it does not prove CreatedDate behavior beyond source. |

## Executable read-only QA

Run from repository root. It enumerates tracked paths through Git, obtains filesystem byte/hash/parse/version data, and never mutates an input.

```powershell
$ErrorActionPreference='Stop'
$root=(Get-Location).Path
$records=@(Get-ChildItem -Recurse -File -Filter *.smc | Where-Object {$_.FullName -notmatch '[\\/](bin|obj)[\\/]'} | ForEach-Object { $p=$_.FullName.Substring($root.Length+1).Replace('\','/'); git ls-files --error-unmatch -- $p 2>$null|Out-Null; if($LASTEXITCODE-ne 0){throw "untracked $p"}; $j=Get-Content -Raw -LiteralPath $_.FullName|ConvertFrom-Json; [pscustomobject]@{Path=$p;Bytes=$_.Length;Hash=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash;Version=[string]$j.version} })
if($records.Count -ne 16){throw "tracked-smc=$($records.Count)"}
$receipt=Get-Content -Raw -Encoding UTF8 'docs/architecture-migration/evidence/persistence-fixtures.md'
foreach($r in $records){$escaped=[regex]::Escape($r.Path);if($receipt-notmatch "(?m)^\| SMC-\d{2} \| ``$escaped`` \| $($r.Bytes) \| ``$($r.Hash)`` \| pass \| ``$($r.Version)`` \|"){throw "ledger mismatch $($r.Path)"}}
if(@($records|Where-Object {$_.Path -like 'tests/*'}).Count -ne 1){throw 'test-fixture-count'}
if(@($records|Where-Object {$_.Path -notlike 'tests/*'}).Count -ne 15){throw 'compat-input-count'}
if(@($records|Where-Object {$_.Version -eq '1.0'}).Count -ne 1 -or @($records|Where-Object {$_.Version -eq '1.1'}).Count -ne 15){throw 'version-ledger'}
if(($records|Where-Object {$_.Path -eq 'tests/SnowMeltingCalculator.Tests/Fixtures/v1-sample.smc'}).Hash -ne 'BA05531935C25AE9A6DCA70157C0CDCC891245BB3C83C436C3F3DA2632F6D113'){throw 'v1-hash'}
$bad='{ invalid json'; try{$bad|ConvertFrom-Json -ErrorAction Stop;throw 'corrupt accepted'}catch [System.Management.Automation.RuntimeException]{}
'PASS tracked-smc=16; test-fixtures=1; compatibility-inputs=15; versions=1x1.0,15x1.1; parse=16; corrupt=in-memory-rejected'
```

Observed output (exit `0`): `PASS tracked-smc=16; test-fixtures=1; compatibility-inputs=15; versions=1x1.0,15x1.1; parse=16; corrupt=in-memory-rejected`.

## DoneClaim

**DoneClaim PERSIST-FIXTURES-02:** 16 tracked `.smc` inputs are completely classified: one assertion-backed v1.0 test fixture and fifteen v1.1 checked-in compatibility inputs. Provenance, hash/parse/version verification, and synthetic corrupt-JSON classification are reproducible without mutation. Byte identity, compatibility duration, whole-schema semantic round trip, atomic/crash safety, and transactional restore remain not established.
