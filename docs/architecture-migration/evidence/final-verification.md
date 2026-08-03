---
phase: phase-0-baseline
snapshot_sha: f0d19c34ac03075d64548f1059e9c6626d3596b5
source_basis: HEAD-plus-approved-dossier
generated_at_utc: 2026-07-31T12:30:00.0000000Z
working_directory: D:/IA/ace v.2
commands:
  - PowerShell 5.1 read-only F5 aggregate validator (exact block below)
  - $env:GIT_MASTER='1'; git rev-parse HEAD
  - $env:GIT_MASTER='1'; git -c core.quotepath=false status --porcelain=v1 --untracked-files=all -- <each Todo 1 ledger path>
  - Get-FileHash -LiteralPath <each present non-dossier ledger path> -Algorithm SHA256
  - PowerShell XML parse of evidence/test-results/phase-0-f3.trx
  - PowerShell deterministic structural/reference/semantic validator extracted from evidence/model-validation.md
exit_code: 0
status: pass
raw_output: Inline section "Observed post-write output" produced by the exact validator after both F5 writes.
limitations:
  - The absent external Draft 2020-12 validator remains honestly degraded; the canonical deterministic validator passes and is re-executed here.
  - F5 aggregates the four terminal lane receipts and verifies their retained executable evidence. It does not claim untested WPF behavior or owner acceptance.
  - This receipt stops the workflow at awaiting-owner-acceptance. It neither marks Phase 0 completed nor authorizes Phase 1.
---

# F5 Final Verification Aggregate

## Binding and Decision

F5 ran sequentially after the four prescribed lane artifacts existed. It is bound
to the captured repository snapshot
`f0d19c34ac03075d64548f1059e9c6626d3596b5` and to the
`HEAD-plus-approved-dossier` basis. The live `HEAD`, canonical plan hash, exact
write allow-list, Todo 1 status/hash ledger, and every lane receipt were checked
again before this aggregate was written.

The four expected paths are distinct regular files. Each has required common
front matter, `phase-0-baseline`, the same snapshot, a recognized source basis,
working directory, exit code `0`, and a terminal `APPROVE`. F3's distinct
Markdown form, `**Terminal verdict: APPROVE**`, is explicitly parsed rather than
mistaken for the plain `verdict: APPROVE` syntax used by the other lanes.

## Lane Matrix

| Lane | Immutable receipt | Metadata / terminal form | Executable evidence independently checked | Assertions | Defects | Verdict |
| --- | --- | --- | --- | ---: | ---: | --- |
| F1 plan compliance | `final-verification-f1-plan-compliance.md` | `status: pass`; backticked `verdict: APPROVE` | Todo 12 gate, exact 34-path allow-list, 30-row ledger and protected hashes | 186/186 | 0 | APPROVE |
| F2 dossier quality | `final-verification-f2-dossier-quality.md` | `status: degraded` only for absent full schema tool; plain `verdict: APPROVE` | Canonical deterministic model validation and all model/map/inventory checks | 2506/2506 | 0 | APPROVE |
| F3 runtime QA | `final-verification-f3-runtime-qa.md` | `status: pass`; `**Terminal verdict: APPROVE**` | F3 TRX direct outcomes: `1540 = 1537 Passed + 3 NotExecuted + 0 Failed`; canonical model validator | 11/11 | 0 | APPROVE |
| F4 scope fidelity | `final-verification-f4-scope-fidelity.md` | `status: pass`; plain `verdict: APPROVE` | Scope matrix, exact dirty ledger, forbidden-path and target/current boundary checks | 445/445 | 0 | APPROVE |

Lane `status: degraded` in F2 is not a failed receipt: its retained limitation
is the unavailable external Draft 2020-12 tool, while its deterministic
structural validation and terminal verdict both pass. No lane receipt records a
blocking defect. No receipt writes a different lane's immutable artifact: F1,
F2, and F4 declare only their own receipt; F3 declares only its receipt plus the
separate allow-listed F3 TRX.

## Aggregate Assertions

| Assertion group | Result | Count / observation |
| --- | --- | --- |
| Live HEAD equals snapshot | pass | `f0d19c34ac03075d64548f1059e9c6626d3596b5` |
| Canonical plan allow-list parsed | pass | 34 exact paths |
| Four expected receipt paths are distinct regular files and allow-listed | pass | 4 |
| Common receipt metadata and current snapshot binding | pass | 4 receipts, 10 required front-matter fields each |
| Terminal verdict parsing, including F3 Markdown syntax | pass | 4 APPROVE |
| Retained lane assertion totals and zero defects | pass | F1 186/186; F2 2506/2506; F3 11/11; F4 445/445 |
| F3 TRX direct-outcome arithmetic | pass | `1540 = 1537 + 3 + 0`, zero failed |
| Canonical deterministic model validator | pass | 79 nodes, 112 edges/semantic records, 27 states, 22 flows, 14 negative probes rejected |
| Todo 1 status/hash ledger, including Cyrillic and deleted paths | pass | 30 status records; 15 present non-dossier hashes; 2 deleted states |
| Phase outputs and cross-lane ownership | pass | 32 pre-F5 outputs, all allow-listed; no lane writes another lane artifact |

The independent pre-write F5 validator completed `175/175` assertions with zero
failures. The exact post-write validator below repeats the final persisted-state
conditions after this receipt and `TASK_CONTEXT.md` have both been written.

## Exact Post-Write Validator

Run from `D:/IA/ace v.2` in Windows PowerShell 5.1. The script is read-only.
Every Git invocation sets `$env:GIT_MASTER='1'` immediately before execution.

```powershell
$ErrorActionPreference = 'Stop'
$root = (Get-Location).Path
$snapshot = 'f0d19c34ac03075d64548f1059e9c6626d3596b5'
$phase = 'phase-0-baseline'
$script:assertions = 0
$script:failures = [Collections.Generic.List[string]]::new()
function Assert-F5([bool]$ok, [string]$id, [string]$detail) {
  $script:assertions++
  if (-not $ok) { $script:failures.Add("${id}: ${detail}") }
}
function Read-Utf8([string]$path) { [IO.File]::ReadAllText((Join-Path $root $path)) }
function Get-Sha([string]$path) { (Get-FileHash -LiteralPath (Join-Path $root $path) -Algorithm SHA256).Hash }

$plan = Read-Utf8 'docs/architecture-migration/plans/phase-0-baseline.md'
$allow = @([regex]::Matches($plan, '(?m)^- `(docs/architecture-migration/[^`]+)`$') | ForEach-Object { $_.Groups[1].Value })
$lanes = [ordered]@{
  F1 = 'docs/architecture-migration/evidence/final-verification-f1-plan-compliance.md'
  F2 = 'docs/architecture-migration/evidence/final-verification-f2-dossier-quality.md'
  F3 = 'docs/architecture-migration/evidence/final-verification-f3-runtime-qa.md'
  F4 = 'docs/architecture-migration/evidence/final-verification-f4-scope-fidelity.md'
}
$env:GIT_MASTER = '1'; $head = (& git rev-parse HEAD | Out-String).Trim()
Assert-F5 ($head -eq $snapshot) 'HEAD' "actual=$head"
Assert-F5 ($allow.Count -eq 34) 'ALLOWLIST-COUNT' "actual=$($allow.Count)"
Assert-F5 ((@($lanes.Values | Sort-Object -Unique).Count -eq 4)) 'LANE-DISTINCT' 'four expected unique paths'
foreach ($lane in $lanes.Keys) {
  $path = $lanes[$lane]
  Assert-F5 (Test-Path -LiteralPath (Join-Path $root $path) -PathType Leaf) 'LANE-REGULAR-FILE' "$lane $path"
  Assert-F5 ($allow -contains $path) 'LANE-ALLOWLIST' "$lane $path"
  $text = Read-Utf8 $path
  $front = [regex]::Match($text, '(?s)\A---\r?\n(?<yaml>.*?)\r?\n---').Groups['yaml'].Value
  Assert-F5 (-not [string]::IsNullOrWhiteSpace($front)) 'LANE-FRONT' $lane
  foreach ($field in 'phase','snapshot_sha','source_basis','generated_at_utc','working_directory','commands','exit_code','status','raw_output','limitations') { Assert-F5 ($front -match "(?m)^${field}:") 'LANE-META' "${lane}::$field" }
  Assert-F5 ($front -match "(?m)^phase:\s*$phase\s*$") 'LANE-PHASE' $lane
  Assert-F5 ($front -match "(?m)^snapshot_sha:\s*$snapshot\s*$") 'LANE-SNAPSHOT' $lane
  Assert-F5 ($front -match '(?m)^source_basis:\s*(HEAD|working-tree|HEAD-plus-approved-dossier)\s*$') 'LANE-BASIS' $lane
  Assert-F5 ($front -match '(?m)^working_directory:\s*D:/IA/ace v\.2\s*$') 'LANE-WORKDIR' $lane
  Assert-F5 ($front -match '(?m)^exit_code:\s*0\s*$') 'LANE-EXIT' $lane
}
$f1 = Read-Utf8 $lanes.F1; $f2 = Read-Utf8 $lanes.F2; $f3 = Read-Utf8 $lanes.F3; $f4 = Read-Utf8 $lanes.F4
Assert-F5 ($f1 -match '`verdict: APPROVE`') 'F1-VERDICT' 'backticked terminal form'
Assert-F5 ($f2 -match '(?m)^verdict:\s*APPROVE\s*$') 'F2-VERDICT' 'plain terminal form'
Assert-F5 ($f3 -match '\*\*Terminal verdict:\s*APPROVE\*\*') 'F3-VERDICT' 'bold Markdown terminal form'
Assert-F5 ($f4 -match '(?m)^verdict:\s*APPROVE\s*$') 'F4-VERDICT' 'plain terminal form'
Assert-F5 ($f1 -match 'assertions_total"\s*:\s*186' -and $f1 -match 'assertions_passed"\s*:\s*186' -and $f1 -match 'assertions_failed"\s*:\s*0') 'F1-ASSERTIONS' '186/186'
Assert-F5 ($f2 -match 'assertions_total\s+: 2506' -and $f2 -match 'assertions_passed\s+: 2506' -and $f2 -match 'assertions_failed\s+: 0') 'F2-ASSERTIONS' '2506/2506'
Assert-F5 ($f3 -match '\*\*Assertion count:\*\* 11 passed, 0 failed' -and $f3 -match '\*\*Defects:\*\* none') 'F3-ASSERTIONS' '11/11'
Assert-F5 ($f4 -match 'assertions_total\s+: 445' -and $f4 -match 'assertions_passed\s+: 445' -and $f4 -match 'assertions_failed\s+: 0') 'F4-ASSERTIONS' '445/445'
Assert-F5 ($f1 -notmatch 'final-verification-f[234]-' -and $f2 -notmatch 'final-verification-f[134]-' -and $f3 -notmatch 'final-verification-f[124]-' -and $f4 -notmatch 'final-verification-f[123]-') 'LANE-OWNERSHIP' 'each lane declares only its own immutable receipt'

[xml]$trx = Read-Utf8 'docs/architecture-migration/evidence/test-results/phase-0-f3.trx'
$results = @($trx.TestRun.Results.UnitTestResult)
$passed = @($results | Where-Object outcome -eq 'Passed').Count
$notExecuted = @($results | Where-Object outcome -eq 'NotExecuted').Count
$failed = @($results | Where-Object outcome -eq 'Failed').Count
Assert-F5 ($results.Count -eq 1540 -and $passed -eq 1537 -and $notExecuted -eq 3 -and $failed -eq 0) 'F3-TRX' "total=$($results.Count) passed=$passed notExecuted=$notExecuted failed=$failed"

$modelText = Read-Utf8 'docs/architecture-migration/evidence/model-validation.md'
$modelScript = [regex]::Match($modelText, '(?s)```powershell\r?\n(?<script>.*?)\r?\n```').Groups['script'].Value
$modelOutput = (& ([scriptblock]::Create($modelScript)) | Out-String)
Assert-F5 ($modelOutput -match 'result\s*:\s*pass') 'MODEL-VALIDATOR' $modelOutput.Trim()

$snapshotText = Read-Utf8 'docs/architecture-migration/evidence/repository-snapshot.md'
$ledger = @([regex]::Matches($snapshotText, '(?m)^\| `(?<status> M| D|\?\?)` \| `(?<path>[^`]+)` \| `(?<blob>[^`]+)` \| `(?<hash>[^`]+)` \|$'))
Assert-F5 ($ledger.Count -eq 30) 'LEDGER-COUNT' "actual=$($ledger.Count)"
foreach ($entry in $ledger) {
  $path = $entry.Groups['path'].Value; $expectedStatus = $entry.Groups['status'].Value; $expectedHash = $entry.Groups['hash'].Value
  $env:GIT_MASTER = '1'; $line = (& git -c core.quotepath=false status --porcelain=v1 --untracked-files=all -- $path | Out-String).TrimEnd("`r","`n")
  Assert-F5 ($line.StartsWith($expectedStatus + ' ')) 'LEDGER-STATUS' "$path expected=$expectedStatus actual=$line"
  if (-not $path.StartsWith('docs/architecture-migration/')) {
    if ($expectedHash -eq 'deleted') { Assert-F5 (-not (Test-Path -LiteralPath (Join-Path $root $path))) 'LEDGER-DELETED' $path }
    else { Assert-F5 ((Get-Sha $path) -eq $expectedHash) 'LEDGER-HASH' $path }
  }
}
$preExisting = @($ledger | ForEach-Object { $_.Groups['path'].Value })
$dossierFiles = @(Get-ChildItem -LiteralPath (Join-Path $root 'docs/architecture-migration') -Recurse -File | ForEach-Object { $_.FullName.Substring($root.Length + 1).Replace('\','/') })
$outputs = @($dossierFiles | Where-Object { $preExisting -notcontains $_ })
foreach ($path in $outputs) { Assert-F5 ($allow -contains $path) 'OUTPUT-ALLOWLIST' $path }
Assert-F5 ($outputs.Count -eq 33) 'OUTPUT-COUNT' "expected=33 including F1-F5 artifacts actual=$($outputs.Count)"

$context = Read-Utf8 'docs/architecture-migration/TASK_CONTEXT.md'
Assert-F5 ($context -match '\| Current phase \| `phase-0-baseline` \|') 'CONTEXT-PHASE' 'phase-0-baseline'
Assert-F5 ($context -match '\| Stage \| `awaiting-owner-acceptance` \|') 'CONTEXT-STAGE' 'awaiting-owner-acceptance'
Assert-F5 ($context -match '\| Phase result acceptance \| `pending; awaiting explicit owner acceptance` \|') 'CONTEXT-ACCEPTANCE' 'still pending owner decision'
Assert-F5 ($context -match 'Next action \| `Explicit owner acceptance of the Phase 0 result; do not start Phase 1 or mark completed` \|') 'CONTEXT-NEXT' 'owner acceptance only'
Assert-F5 ($context -notmatch '\| Stage \| `completed` \|') 'CONTEXT-NOT-COMPLETED' 'owner acceptance has not occurred'

$verdict = if ($script:failures.Count -eq 0) { 'APPROVE' } else { 'REJECT' }
[pscustomobject]@{ snapshot_sha=$snapshot; lanes=4; assertions_total=$script:assertions; assertions_passed=$script:assertions-$script:failures.Count; assertions_failed=$script:failures.Count; phase0_outputs=$outputs.Count; f3_trx="1540=1537+3+0"; model_validator='pass'; verdict=$verdict } | Format-List
if ($script:failures.Count -gt 0) { $script:failures | ForEach-Object { "DEFECT $_" }; exit 1 }
exit 0
```

## Observed Post-Write Output

```text
snapshot_sha      : f0d19c34ac03075d64548f1059e9c6626d3596b5
lanes             : 4
assertions_total  : 173
assertions_passed : 173
assertions_failed : 0
phase0_outputs    : 33
f3_trx            : 1540=1537+3+0
model_validator   : pass
verdict           : APPROVE
```

## Defects

None. All lane receipts approve, their retained assertion summaries are green,
the F3 runtime artifact reconciles, the model validator passes, and the current
non-dossier dirty worktree exactly preserves the Todo 1 ledger.

## Terminal Verdict

verdict: APPROVE

F5 authorizes only the transition to `awaiting-owner-acceptance`. Phase result
acceptance remains pending until an explicit owner decision.
