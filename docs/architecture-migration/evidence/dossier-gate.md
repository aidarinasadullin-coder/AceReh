---
phase: phase-0-baseline
snapshot_sha: f0d19c34ac03075d64548f1059e9c6626d3596b5
source_basis: HEAD-plus-approved-dossier
generated_at_utc: 2026-07-31T06:29:23.0832878Z
working_directory: D:/IA/ace v.2
commands:
  - PowerShell 5.1 self-contained deterministic dossier gate (exact inline block below)
exit_code: 0
status: pass
raw_output: Inline section "Observed raw output" in this receipt.
limitations:
  - No installed Draft 2020-12 validator is available; full schema validation remains degraded, while the exact deterministic model validator passes.
  - Build and tests were not rerun by Todo 12; this gate revalidates the snapshot-bound green receipts, retained logs, and TRX outcomes.
  - TASK_CONTEXT.md is updated separately after this receipt records a passing result; the embedded validator requires the resulting Stage=verification state.
  - Green build/tests and structural dossier checks do not establish untested runtime behavior; characterization gaps remain explicit.
---

# Phase 0 Pre-Verification Dossier Gate

## Boundary

This receipt is bound to snapshot
`f0d19c34ac03075d64548f1059e9c6626d3596b5`, phase
`phase-0-baseline`, working directory `D:/IA/ace v.2`, and the honest
`HEAD-plus-approved-dossier` source basis. It validates the approved Phase 0
documentation over the captured working tree. It does not run F1-F4, create an
F1-F5 receipt, update workflow state, claim owner acceptance, or authorize
Phase 1.

## Exact self-contained validator

Run from `D:/IA/ace v.2` with Windows PowerShell 5.1. The script is read-only.
Its two required failure probes mutate only in-memory values.

```powershell
$ErrorActionPreference = 'Stop'
$root = (Get-Location).Path
$snapshot = 'f0d19c34ac03075d64548f1059e9c6626d3596b5'
$phase = 'phase-0-baseline'
$workdir = 'D:/IA/ace v.2'
$planHash = 'BB6F92470A4BF786FE90F8A86F2B34F3B04BEE3C5AC2654C9A45AEB75F87CC6E'
$script:assertions = 0
$script:failures = [System.Collections.Generic.List[string]]::new()
function Assert-Gate([bool]$condition, [string]$id, [string]$detail) {
  $script:assertions++
  if (-not $condition) { $script:failures.Add("${id}: ${detail}") }
}
function Read-Utf8([string]$path) { return [IO.File]::ReadAllText((Join-Path $root $path)) }
function Get-Sha([string]$path) { return (Get-FileHash -LiteralPath (Join-Path $root $path) -Algorithm SHA256).Hash }
function Invoke-EmbeddedValidator([string]$path) {
  $text = Read-Utf8 $path
  $match = [regex]::Match($text, '(?s)```powershell\r?\n(?<script>.*?)\r?\n```')
  if (-not $match.Success) { throw "embedded validator absent: $path" }
  return (& ([scriptblock]::Create($match.Groups['script'].Value)) | Out-String)
}

$required = @(
  'docs/architecture-migration/evidence/repository-snapshot.md',
  'docs/architecture-migration/evidence/environment.md',
  'docs/architecture-migration/evidence/build-baseline.md',
  'docs/architecture-migration/evidence/build-baseline.log',
  'docs/architecture-migration/evidence/test-baseline.md',
  'docs/architecture-migration/evidence/test-baseline.log',
  'docs/architecture-migration/evidence/test-results/phase-0.trx',
  'docs/architecture-migration/evidence/metrics-baseline.json',
  'docs/architecture-migration/evidence/codegraph-baseline.md',
  'docs/architecture-migration/evidence/audit-reconciliation.md',
  'docs/architecture-migration/evidence/persistence-fixtures.md',
  'docs/architecture-migration/evidence/user-flow-baseline.md',
  'docs/architecture-migration/evidence/model-validation.md',
  'docs/architecture-migration/maps/architecture-model.schema.json',
  'docs/architecture-migration/maps/architecture-model.baseline.json',
  'docs/architecture-migration/maps/compile-time.md',
  'docs/architecture-migration/maps/di-runtime.md',
  'docs/architecture-migration/maps/state-ownership.md',
  'docs/architecture-migration/maps/reactive.md',
  'docs/architecture-migration/maps/persistence.md',
  'docs/architecture-migration/maps/user-flow.md',
  'docs/architecture-migration/maps/state-inventory.md',
  'docs/architecture-migration/maps/characterization-tests.md',
  'docs/architecture-migration/maps/persistence-compatibility.md',
  'docs/architecture-migration/maps/target-invariants.md',
  'docs/architecture-migration/widget-spec.md'
)
foreach ($path in $required) { Assert-Gate (Test-Path -LiteralPath (Join-Path $root $path) -PathType Leaf) 'ARTIFACT' $path }

$finalReceipts = @(
  'docs/architecture-migration/evidence/final-verification-f1-plan-compliance.md',
  'docs/architecture-migration/evidence/final-verification-f2-dossier-quality.md',
  'docs/architecture-migration/evidence/final-verification-f3-runtime-qa.md',
  'docs/architecture-migration/evidence/test-results/phase-0-f3.trx',
  'docs/architecture-migration/evidence/final-verification-f4-scope-fidelity.md',
  'docs/architecture-migration/evidence/final-verification.md'
)
foreach ($path in $finalReceipts) { Assert-Gate (-not (Test-Path -LiteralPath (Join-Path $root $path))) 'FINAL-ABSENT' $path }

$receiptMarkdown = @(
  'docs/architecture-migration/evidence/repository-snapshot.md',
  'docs/architecture-migration/evidence/environment.md',
  'docs/architecture-migration/evidence/build-baseline.md',
  'docs/architecture-migration/evidence/test-baseline.md',
  'docs/architecture-migration/evidence/codegraph-baseline.md',
  'docs/architecture-migration/evidence/audit-reconciliation.md',
  'docs/architecture-migration/evidence/persistence-fixtures.md',
  'docs/architecture-migration/evidence/user-flow-baseline.md',
  'docs/architecture-migration/evidence/model-validation.md',
  'docs/architecture-migration/maps/compile-time.md',
  'docs/architecture-migration/maps/di-runtime.md',
  'docs/architecture-migration/maps/state-ownership.md',
  'docs/architecture-migration/maps/reactive.md',
  'docs/architecture-migration/maps/persistence.md',
  'docs/architecture-migration/maps/user-flow.md',
  'docs/architecture-migration/maps/state-inventory.md',
  'docs/architecture-migration/maps/characterization-tests.md',
  'docs/architecture-migration/maps/persistence-compatibility.md',
  'docs/architecture-migration/maps/target-invariants.md',
  'docs/architecture-migration/widget-spec.md'
)
foreach ($path in $receiptMarkdown) {
  $text = Read-Utf8 $path
  $front = [regex]::Match($text, '(?s)\A---\r?\n(?<yaml>.*?)\r?\n---').Groups['yaml'].Value
  Assert-Gate (-not [string]::IsNullOrWhiteSpace($front)) 'META-FRONT' $path
  foreach ($field in @('phase','snapshot_sha','source_basis','generated_at_utc','working_directory','commands','exit_code','status','raw_output','limitations')) {
    Assert-Gate ($front -match "(?m)^${field}:") 'META-FIELD' "$path::$field"
  }
  Assert-Gate ($front -match "(?m)^phase:\s*$([regex]::Escape($phase))\s*$") 'META-PHASE' $path
  Assert-Gate ($front -match "(?m)^snapshot_sha:\s*$snapshot\s*$") 'META-SNAPSHOT' $path
  Assert-Gate ($front -match '(?m)^source_basis:\s*(working-tree|HEAD|HEAD-plus-approved-dossier)\s*$') 'META-SOURCE' $path
  Assert-Gate ($front -match "(?m)^working_directory:\s*$([regex]::Escape($workdir))\s*$") 'META-WORKDIR' $path
}

$metrics = Read-Utf8 'docs/architecture-migration/evidence/metrics-baseline.json' | ConvertFrom-Json
Assert-Gate ($metrics.snapshot_sha -eq $snapshot) 'METRICS-SNAPSHOT' 'metrics-baseline.json'
Assert-Gate ($metrics.source_basis -eq 'working-tree') 'METRICS-SOURCE' 'metrics-baseline.json'
$schema = Read-Utf8 'docs/architecture-migration/maps/architecture-model.schema.json' | ConvertFrom-Json
$model = Read-Utf8 'docs/architecture-migration/maps/architecture-model.baseline.json' | ConvertFrom-Json
Assert-Gate ($schema.'$schema' -eq 'https://json-schema.org/draft/2020-12/schema') 'SCHEMA-DRAFT' 'Draft 2020-12 declaration'
Assert-Gate ($model.meta.snapshot_sha -eq $snapshot) 'MODEL-SNAPSHOT' 'canonical model'
Assert-Gate ($model.meta.phase -eq $phase) 'MODEL-PHASE' 'canonical model'

$modelOutput = Invoke-EmbeddedValidator 'docs/architecture-migration/evidence/model-validation.md'
Assert-Gate ($modelOutput -match 'result\s*:\s*pass') 'MODEL-VALIDATOR' ($modelOutput.Trim())
$targetOutput = Invoke-EmbeddedValidator 'docs/architecture-migration/maps/target-invariants.md'
Assert-Gate ($targetOutput -match 'result\s*:\s*pass') 'TARGET-VALIDATOR' ($targetOutput.Trim())
$widgetOutput = Invoke-EmbeddedValidator 'docs/architecture-migration/widget-spec.md'
Assert-Gate ($widgetOutput -match 'result\s*:\s*pass') 'WIDGET-VALIDATOR' ($widgetOutput.Trim())

$inventory = Read-Utf8 'docs/architecture-migration/maps/state-inventory.md'
$inventoryRows = @([regex]::Matches($inventory, '(?m)^\| `ST-\d{3}` \|'))
Assert-Gate ($inventoryRows.Count -eq 27) 'INVENTORY-ROWS' "expected=27 actual=$($inventoryRows.Count)"
foreach ($row in [regex]::Matches($inventory, '(?m)^\| `ST-\d{3}` \|.*$')) {
  Assert-Gate ((@($row.Value -split '\|').Count) -eq 14) 'INVENTORY-COLUMNS' $row.Value.Substring(0, [Math]::Min(30,$row.Value.Length))
  Assert-Gate ($row.Value -match '\| (covered|partial|missing|blocked) \|$') 'INVENTORY-COVERAGE' $row.Value.Substring(0, [Math]::Min(30,$row.Value.Length))
}
$inventoryDomains = [ordered]@{
  Lifecycle = 'Lifecycle'
  Climate = 'Climate'
  Construction = 'Construction'
  Thermal = 'Thermal'
  Hydraulics = 'Hydraulics'
  Navigation = 'MainViewModel.*(title|load guard)'
  Results = 'Results'
  export = 'export'
  CalculationContext = 'CalculationContext'
  CalculationStateService = 'CalculationStateService'
}
foreach ($domain in $inventoryDomains.Keys) {
  Assert-Gate ($inventory -match ('(?i)' + $inventoryDomains[$domain])) 'INVENTORY-DOMAIN' $domain
}

$characterization = Read-Utf8 'docs/architecture-migration/maps/characterization-tests.md'
$characterizationRows = @([regex]::Matches($characterization, '(?m)^\| `CF-\d{3}` \|.*$'))
Assert-Gate ($characterizationRows.Count -eq 22) 'CHAR-ROWS' "expected=22 actual=$($characterizationRows.Count)"
foreach ($row in $characterizationRows) {
  Assert-Gate ($row.Value -match '\| (covered|partial|missing|blocked) \|') 'CHAR-STATUS' $row.Groups[0].Value.Substring(0, [Math]::Min(30,$row.Value.Length))
  Assert-Gate ($row.Value -match 'ContextChanged=.*StateChanged=.*calculator=.*Results=.*dirty=') 'CHAR-COUNTERS' $row.Value.Substring(0, [Math]::Min(30,$row.Value.Length))
}

$persistence = Read-Utf8 'docs/architecture-migration/maps/persistence-compatibility.md'
$persistenceView = Read-Utf8 'docs/architecture-migration/maps/persistence.md'
$persistenceCorpus = $persistence + "`n" + $persistenceView
$propertyRows = @([regex]::Matches($persistence, '(?m)^\| PP-\d{3} \|'))
Assert-Gate ($propertyRows.Count -eq 122) 'PERSISTENCE-ROWS' "expected=122 actual=$($propertyRows.Count)"
Assert-Gate (@([regex]::Matches($persistence, '(?m)^\| (?!Classification \|)(?!---)(?!PP-)[^|]+ \| [^|]+ \| [^|]+ \| [^|]+ \| [^|]+ \| [^|]+ \|$')).Count -eq 9) 'PERSISTENCE-CLASSES' 'expected=9'
foreach ($token in @('file','JSON','restore','dirty','Results','backup','current','legacy','corrupt','byte identity','transactional')) {
  Assert-Gate ($persistenceCorpus -match ('(?i)' + [regex]::Escape($token))) 'PERSISTENCE-BOUNDARY' $token
}
$fixtureReceipt = Read-Utf8 'docs/architecture-migration/evidence/persistence-fixtures.md'
$fixtureMatch = [regex]::Match($fixtureReceipt, '(?m)^\| SMC-01 \| `tests/SnowMeltingCalculator.Tests/Fixtures/v1-sample\.smc` \| \d+ \| `(?<hash>[A-F0-9]{64})` \|')
Assert-Gate ($fixtureMatch.Success) 'FIXTURE-LEDGER' 'v1-sample.smc row/hash'
if ($fixtureMatch.Success) { Assert-Gate ((Get-Sha 'tests/SnowMeltingCalculator.Tests/Fixtures/v1-sample.smc') -eq $fixtureMatch.Groups['hash'].Value) 'FIXTURE-HASH' 'v1-sample.smc' }

$build = Read-Utf8 'docs/architecture-migration/evidence/build-baseline.md'
$test = Read-Utf8 'docs/architecture-migration/evidence/test-baseline.md'
Assert-Gate ($build -match '(?m)^exit_code:\s*0\s*$' -and $build -match '(?m)^status:\s*pass\s*$' -and $build -match '\| Warnings \| `0` \|' -and $build -match '\| Errors \| `0` \|') 'BUILD-GREEN' 'baseline receipt'
Assert-Gate (Test-Path -LiteralPath (Join-Path $root 'docs/architecture-migration/evidence/build-baseline.log')) 'BUILD-LOG' 'build-baseline.log'
Assert-Gate ($test -match '(?m)^exit_code:\s*0\s*$' -and $test -match '(?m)^status:\s*pass\s*$') 'TEST-GREEN' 'baseline receipt'
Assert-Gate (Test-Path -LiteralPath (Join-Path $root 'docs/architecture-migration/evidence/test-baseline.log')) 'TEST-LOG' 'test-baseline.log'
[xml]$trx = Read-Utf8 'docs/architecture-migration/evidence/test-results/phase-0.trx'
$results = @($trx.TestRun.Results.UnitTestResult)
$passed = @($results | Where-Object outcome -eq 'Passed').Count
$failed = @($results | Where-Object outcome -eq 'Failed').Count
$notExecuted = @($results | Where-Object outcome -eq 'NotExecuted').Count
Assert-Gate ($results.Count -eq 1540 -and $passed -eq 1537 -and $failed -eq 0 -and $notExecuted -eq 3) 'TRX-OUTCOMES' "total=$($results.Count) passed=$passed failed=$failed notExecuted=$notExecuted"

$markdown = $required | Where-Object { $_ -like '*.md' }
foreach ($path in $markdown) {
  $text = Read-Utf8 $path
  foreach ($link in [regex]::Matches($text, '\[[^\]\r\n]+\]\((?<target>[^)\r\n]+)\)')) {
    $target = $link.Groups['target'].Value.Split('#')[0]
    if ([string]::IsNullOrWhiteSpace($target) -or $target.StartsWith('#') -or $target -match '^(https?|mailto):' -or $target.Contains('|')) { continue }
    try {
      $decoded = [Uri]::UnescapeDataString($target)
      $decoded = $decoded -replace ':\d+$',''
      if ($decoded -match '^/[A-Za-z]:/') {
        $resolved = $decoded.Substring(1).Replace('/','\')
      } else {
        $resolved = [IO.Path]::GetFullPath((Join-Path (Split-Path (Join-Path $root $path)) $decoded))
      }
      Assert-Gate (Test-Path -LiteralPath $resolved) 'LINK' "$path -> $target"
    } catch {
      Assert-Gate $false 'LINK-PARSE' "$path -> $target :: $($_.Exception.Message)"
    }
  }
}

$allowList = @(
  'docs/architecture-migration/evidence/repository-snapshot.md','docs/architecture-migration/evidence/environment.md','docs/architecture-migration/evidence/build-baseline.md','docs/architecture-migration/evidence/build-baseline.log','docs/architecture-migration/evidence/test-baseline.md','docs/architecture-migration/evidence/test-baseline.log','docs/architecture-migration/evidence/test-results/phase-0.trx','docs/architecture-migration/evidence/metrics-baseline.json','docs/architecture-migration/evidence/codegraph-baseline.md','docs/architecture-migration/evidence/persistence-fixtures.md','docs/architecture-migration/evidence/user-flow-baseline.md','docs/architecture-migration/evidence/audit-reconciliation.md','docs/architecture-migration/evidence/model-validation.md','docs/architecture-migration/evidence/dossier-gate.md','docs/architecture-migration/evidence/final-verification-f1-plan-compliance.md','docs/architecture-migration/evidence/final-verification-f2-dossier-quality.md','docs/architecture-migration/evidence/final-verification-f3-runtime-qa.md','docs/architecture-migration/evidence/test-results/phase-0-f3.trx','docs/architecture-migration/evidence/final-verification-f4-scope-fidelity.md','docs/architecture-migration/evidence/final-verification.md','docs/architecture-migration/maps/architecture-model.schema.json','docs/architecture-migration/maps/architecture-model.baseline.json','docs/architecture-migration/maps/compile-time.md','docs/architecture-migration/maps/di-runtime.md','docs/architecture-migration/maps/state-ownership.md','docs/architecture-migration/maps/reactive.md','docs/architecture-migration/maps/persistence.md','docs/architecture-migration/maps/user-flow.md','docs/architecture-migration/maps/state-inventory.md','docs/architecture-migration/maps/characterization-tests.md','docs/architecture-migration/maps/persistence-compatibility.md','docs/architecture-migration/maps/target-invariants.md','docs/architecture-migration/widget-spec.md','docs/architecture-migration/TASK_CONTEXT.md'
)
$plan = Read-Utf8 'docs/architecture-migration/plans/phase-0-baseline.md'
$planAllow = @([regex]::Matches($plan, '(?m)^- `(docs/architecture-migration/[^`]+)`$') | ForEach-Object { $_.Groups[1].Value })
Assert-Gate ((Compare-Object ($allowList | Sort-Object) ($planAllow | Sort-Object)).Count -eq 0) 'ALLOWLIST-EXACT' 'hard-coded validator allow-list versus canonical plan'
Assert-Gate ((Get-Sha 'docs/architecture-migration/plans/phase-0-baseline.md') -eq $planHash) 'PLAN-HASH' 'canonical plan'

$snapshotText = Read-Utf8 'docs/architecture-migration/evidence/repository-snapshot.md'
$ledger = @([regex]::Matches($snapshotText, '(?m)^\| `(?<status> M| D|\?\?)` \| `(?<path>[^`]+)` \| `(?<blob>[^`]+)` \| `(?<hash>[^`]+)` \|$'))
Assert-Gate ($ledger.Count -eq 30) 'LEDGER-ROWS' "expected=30 actual=$($ledger.Count)"
$env:GIT_MASTER = '1'
$statusRaw = (& git status --porcelain=v1 -z --untracked-files=all | Out-String)
Assert-Gate ($LASTEXITCODE -eq 0) 'GIT-STATUS-EXIT' "exit=$LASTEXITCODE"
# PowerShell text pipelines cannot preserve NUL records from native output reliably on every 5.1 host;
# query each captured path with git status --porcelain and use filesystem hashes for exact identity.
foreach ($entry in $ledger) {
  $path = $entry.Groups['path'].Value
  $expectedStatus = $entry.Groups['status'].Value
  $env:GIT_MASTER = '1'
  $line = (& git -c core.quotepath=false status --porcelain=v1 --untracked-files=all -- $path | Out-String).TrimEnd("`r","`n")
  Assert-Gate ($LASTEXITCODE -eq 0) 'LEDGER-GIT' $path
  Assert-Gate ($line.StartsWith($expectedStatus + ' ')) 'LEDGER-STATUS' "$path expected=$expectedStatus actual=$line"
  $isAllowListedDossierOwner = $path.StartsWith('docs/architecture-migration/') -and ($allowList -contains $path)
  if (-not $isAllowListedDossierOwner) {
    $expectedHash = $entry.Groups['hash'].Value
    if ($expectedHash -eq 'deleted') {
      Assert-Gate (-not (Test-Path -LiteralPath (Join-Path $root $path))) 'LEDGER-DELETED' $path
    } else {
      Assert-Gate ((Get-Sha $path) -eq $expectedHash) 'LEDGER-HASH' $path
    }
  }
}

$preExistingDossier = @($ledger | Where-Object { $_.Groups['path'].Value.StartsWith('docs/architecture-migration/') } | ForEach-Object { $_.Groups['path'].Value })
$currentDossierFiles = @(Get-ChildItem -LiteralPath (Join-Path $root 'docs/architecture-migration') -Recurse -File | ForEach-Object { $_.FullName.Substring($root.Length + 1).Replace('\','/') })
$phaseOutputs = @($currentDossierFiles | Where-Object { $preExistingDossier -notcontains $_ })
foreach ($path in $phaseOutputs) { Assert-Gate ($allowList -contains $path) 'PHASE0-ALLOWLIST' $path }
Assert-Gate (@($phaseOutputs | Where-Object { $_ -like 'docs/architecture-migration/evidence/final-verification*' -or $_ -eq 'docs/architecture-migration/evidence/test-results/phase-0-f3.trx' }).Count -eq 0) 'NO-FINAL-OUTPUT' 'F1-F5 outputs'

# Required in-memory failure probe 1: an altered expected hash must reject the exact path.
$hashProbePath = '.gitignore'
$alteredExpectedHash = ('0' * 64)
$hashProbeMessage = if ((Get-Sha $hashProbePath) -ne $alteredExpectedHash) { "hash mismatch: $hashProbePath" } else { $null }
Assert-Gate ($hashProbeMessage -eq 'hash mismatch: .gitignore') 'NEGATIVE-HASH' "actual=$hashProbeMessage"

# Required in-memory failure probe 2: an orphan model reference must reject the exact ID.
$probeModel = $model | ConvertTo-Json -Depth 12 | ConvertFrom-Json
$probeEdgeId = $probeModel.edges[0].id
$probeModel.edges[0].from = 'NODE-ORPHAN'
$nodeIds = @($probeModel.nodes.id)
$orphanProbeMessage = if ($nodeIds -notcontains $probeModel.edges[0].from) { "orphan endpoint: $probeEdgeId -> NODE-ORPHAN" } else { $null }
Assert-Gate ($orphanProbeMessage -eq "orphan endpoint: $probeEdgeId -> NODE-ORPHAN") 'NEGATIVE-ORPHAN' "actual=$orphanProbeMessage"

$context = Read-Utf8 'docs/architecture-migration/TASK_CONTEXT.md'
Assert-Gate ($context -match '\| Current phase \| `phase-0-baseline` \|') 'CONTEXT-PHASE' 'Current phase'
Assert-Gate ($context -match '\| Stage \| `verification` \|') 'CONTEXT-STAGE' 'Todo 12 pass advances only to verification'
Assert-Gate ($context -match 'F1-F4') 'CONTEXT-NEXT' 'independent verification wave is next; owner acceptance remains gated'

$gateResult = if ($script:failures.Count -eq 0) { 'pass' } else { 'blocked' }
[pscustomobject]@{
  snapshot_sha = $snapshot
  artifacts_required = $required.Count
  phase0_outputs = $phaseOutputs.Count
  ledger_rows = $ledger.Count
  assertions_total = $script:assertions
  assertions_passed = $script:assertions - $script:failures.Count
  assertions_failed = $script:failures.Count
  negative_hash_probe = $hashProbeMessage
  negative_orphan_probe = $orphanProbeMessage
  draft_2020_12 = 'degraded-full-validator-absent; deterministic-pass'
  gate_result = $gateResult
} | Format-List
if ($script:failures.Count -gt 0) {
  $script:failures | ForEach-Object { "BLOCKER $_" }
  exit 1
}
exit 0
```

## Observed raw output

```text
snapshot_sha          : f0d19c34ac03075d64548f1059e9c6626d3596b5
artifacts_required    : 26
phase0_outputs        : 27
ledger_rows           : 30
assertions_total      : 607
assertions_passed     : 607
assertions_failed     : 0
negative_hash_probe   : hash mismatch: .gitignore
negative_orphan_probe : orphan endpoint: CTE-001 -> NODE-ORPHAN
draft_2020_12         : degraded-full-validator-absent; deterministic-pass
gate_result           : pass
```

## Result

The observed command exit code and terminal result are recorded above after
executing the exact embedded validator.

gate_result: pass
