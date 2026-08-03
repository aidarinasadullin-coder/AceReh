---
phase: phase-0-baseline
snapshot_sha: f0d19c34ac03075d64548f1059e9c6626d3596b5
source_basis: HEAD-plus-approved-dossier
generated_at_utc: 2026-07-31T12:04:07.5130321Z
working_directory: D:/IA/ace v.2
commands:
  - PowerShell 5.1 read-only F1 validator (exact block below)
  - git status --porcelain=v1 --untracked-files=all
  - git -c core.quotepath=false status --porcelain=v1 --untracked-files=all -- <each ledger path>
  - Get-FileHash -LiteralPath <each present ledger/protected path> -Algorithm SHA256
exit_code: 0
status: pass
raw_output: Inline section "Observed raw output".
limitations:
  - This independent plan-compliance audit does not rerun build, tests, or the full Draft 2020-12 validator; those are separate F3 and structural-validation responsibilities.
  - The receipt is bound to the stated snapshot and the working-tree state observed during this F1 run. It cannot attest to later changes.
---

# F1 Plan Compliance Audit

## Scope And Inputs

This F1 receipt independently verifies the canonical plan, its 12 column-zero
implementation Todo entries, the exact execution write allow-list, the Todo 1
dirty-worktree ledger, and persisted Todo 12 workflow state. It cites both
`AGENTS.md` files and the owner gates recorded in
`docs/architecture-migration/TASK_CONTEXT.md`.

The audit is bound to snapshot
`f0d19c34ac03075d64548f1059e9c6626d3596b5`. `git rev-parse HEAD` returned
that exact value. `TASK_CONTEXT.md` records `Current phase` as
`phase-0-baseline`, `Stage` as `verification`, approved plan and execution
authorization, and pending result acceptance. The workflow therefore has not
crossed the owner acceptance gate.

## Exact Reproducible QA

Run from `D:/IA/ace v.2` in Windows PowerShell 5.1. Every Git invocation is
prefixed with `$env:GIT_MASTER='1';`. The validator performs no mutation.

```powershell
$ErrorActionPreference = 'Stop'
$root = (Get-Location).Path
$planText = [IO.File]::ReadAllText((Join-Path $root 'docs/architecture-migration/plans/phase-0-baseline.md'))
$todos = [regex]::Matches($planText, '(?m)^- \[ \] (?<id>(?:[1-9]|1[0-2]))\.')
$allow = @([regex]::Matches($planText, '(?m)^- `(docs/architecture-migration/[^`]+)`$') | ForEach-Object { $_.Groups[1].Value })
$snapshotText = [IO.File]::ReadAllText((Join-Path $root 'docs/architecture-migration/evidence/repository-snapshot.md'))
$ledger = @([regex]::Matches($snapshotText, '(?m)^\| `(?<status> M| D|\?\?)` \| `(?<path>[^`]+)` \| `(?<blob>[^`]+)` \| `(?<hash>[^`]+)` \|$'))
$script:assertions = 0
$script:failures = [Collections.Generic.List[string]]::new()
function Assert-F1([bool] $ok, [string] $id, [string] $detail) {
  $script:assertions++
  if (-not $ok) { $script:failures.Add("${id}: $detail") }
}
function Get-F1Hash([string] $path) {
  (Get-FileHash -LiteralPath (Join-Path $root $path) -Algorithm SHA256).Hash
}

$requiredByTodo = @{
  1 = @('evidence/repository-snapshot.md')
  2 = @('evidence/environment.md','evidence/build-baseline.md','evidence/build-baseline.log','evidence/test-baseline.md','evidence/test-baseline.log','evidence/test-results/phase-0.trx')
  3 = @('evidence/metrics-baseline.json')
  4 = @('evidence/codegraph-baseline.md')
  5 = @('evidence/audit-reconciliation.md')
  6 = @('maps/compile-time.md','maps/di-runtime.md')
  7 = @('maps/state-ownership.md','maps/reactive.md','maps/state-inventory.md')
  8 = @('maps/characterization-tests.md','maps/user-flow.md','evidence/user-flow-baseline.md')
  9 = @('evidence/persistence-fixtures.md','maps/persistence.md','maps/persistence-compatibility.md')
  10 = @('maps/architecture-model.schema.json','maps/architecture-model.baseline.json','evidence/model-validation.md')
  11 = @('maps/target-invariants.md','widget-spec.md')
  12 = @('evidence/dossier-gate.md','TASK_CONTEXT.md')
}

Assert-F1 ($todos.Count -eq 12) 'TODO-PARSE' "expected=12 actual=$($todos.Count)"
Assert-F1 ($allow.Count -eq 34) 'ALLOWLIST-PARSE' "expected=34 actual=$($allow.Count)"
foreach ($id in 1..12) {
  $section = [regex]::Match($planText, "(?ms)^- \[ \] $id\..*?(?=^- \[ \] (?:[1-9]|1[0-2])\.|\z)").Value
  Assert-F1 ($section -match 'Acceptance criteria' -and $section -match 'Evidence') 'TODO-PLAN-EVIDENCE' "Todo $id"
  foreach ($relative in $requiredByTodo[$id]) {
    $path = "docs/architecture-migration/$relative"
    Assert-F1 (Test-Path -LiteralPath (Join-Path $root $path) -PathType Leaf) 'TODO-ARTIFACT' "Todo $id $path"
  }
}

$context = [IO.File]::ReadAllText((Join-Path $root 'docs/architecture-migration/TASK_CONTEXT.md'))
$gate = [IO.File]::ReadAllText((Join-Path $root 'docs/architecture-migration/evidence/dossier-gate.md'))
Assert-F1 ($gate -match 'assertions_total\s+: 607' -and $gate -match 'assertions_passed\s+: 607' -and $gate -match 'gate_result\s+: pass') 'TODO12-GATE' 'dossier gate 607/607 pass'
Assert-F1 ($context -match '\| Stage \| `verification` \|') 'WORKFLOW-STAGE' 'Stage=verification'
Assert-F1 ($context -match '\| Owner plan approval \| `approved; 2026-07-30` \|' -and $context -match '\| Execution authorization \| `approved; 2026-07-30') 'OWNER-GATES' 'approved plan and execution authorization'

Assert-F1 ($ledger.Count -eq 30) 'LEDGER-PARSE' "expected=30 actual=$($ledger.Count)"
$env:GIT_MASTER = '1'; & git status --porcelain=v1 --untracked-files=all | Out-Null
Assert-F1 ($LASTEXITCODE -eq 0) 'GIT-STATUS' "exit=$LASTEXITCODE"
foreach ($entry in $ledger) {
  $path = $entry.Groups['path'].Value
  $expectedStatus = $entry.Groups['status'].Value
  $env:GIT_MASTER = '1'; $line = (& git -c core.quotepath=false status --porcelain=v1 --untracked-files=all -- $path | Out-String).TrimEnd("`r", "`n")
  Assert-F1 ($LASTEXITCODE -eq 0) 'LEDGER-GIT' $path
  Assert-F1 ($line.StartsWith($expectedStatus + ' ')) 'LEDGER-STATUS' "$path expected=$expectedStatus actual=$line"
  if ($path -notlike 'docs/architecture-migration/*') {
    $expectedHash = $entry.Groups['hash'].Value
    if ($expectedHash -eq 'deleted') {
      Assert-F1 (-not (Test-Path -LiteralPath (Join-Path $root $path))) 'LEDGER-DELETED' $path
    } else {
      Assert-F1 ((Get-F1Hash $path) -eq $expectedHash) 'LEDGER-HASH' $path
    }
  }
}

$preExisting = @($ledger | ForEach-Object { $_.Groups['path'].Value })
$currentDossier = @(Get-ChildItem -LiteralPath (Join-Path $root 'docs/architecture-migration') -Recurse -File | ForEach-Object { $_.FullName.Substring($root.Length + 1).Replace('\','/') })
$phaseOutputs = @($currentDossier | Where-Object { $preExisting -notcontains $_ })
foreach ($path in $phaseOutputs) { Assert-F1 ($allow -contains $path) 'OUTPUT-ALLOWLIST' $path }
Assert-F1 ($phaseOutputs.Count -eq 28) 'OUTPUT-COUNT' "expected=28 including this F1 receipt actual=$($phaseOutputs.Count)"

$protected = @('AGENTS.md','docs/architecture-migration/AGENTS.md','docs/architecture-migration/architecture_audit.md','docs/architecture-migration/audit_metrics.json','docs/architecture-migration/architecture_widget.html','docs/architecture-migration/plans/phase-0-baseline.md')
foreach ($path in $protected) {
  $row = $ledger | Where-Object { $_.Groups['path'].Value -eq $path }
  if ($row) { Assert-F1 ((Get-F1Hash $path) -eq $row[0].Groups['hash'].Value) 'PROTECTED-HASH' $path }
}

$forbidden = '\bgit\s+(add|commit|push|stash|reset|clean|checkout|restore|rebase)\b'
$markdown = @(Get-ChildItem -LiteralPath (Join-Path $root 'docs/architecture-migration') -Recurse -File -Filter '*.md' | Where-Object { $_.FullName -notmatch '\\plans\\phase-0-baseline\.md$' })
foreach ($file in $markdown) {
  $text = [IO.File]::ReadAllText($file.FullName)
  $commands = [regex]::Match($text, '(?s)commands:\s*(?<body>.*?)(?=\r?\n\w[\w_]*:|\r?\n---)', [Text.RegularExpressions.RegexOptions]::IgnoreCase).Groups['body'].Value
  Assert-F1 ($commands -notmatch $forbidden) 'NO-GIT-MUTATION-RECORDED' $file.FullName.Substring($root.Length + 1)
}
Assert-F1 ((Get-F1Hash 'docs/architecture-migration/plans/phase-0-baseline.md') -eq 'BB6F92470A4BF786FE90F8A86F2B34F3B04BEE3C5AC2654C9A45AEB75F87CC6E') 'PLAN-HASH' 'canonical plan'

[pscustomobject]@{
  snapshot_sha = 'f0d19c34ac03075d64548f1059e9c6626d3596b5'
  todos = $todos.Count
  allow_list = $allow.Count
  todo_artifacts = 28
  ledger_rows = $ledger.Count
  phase0_outputs = $phaseOutputs.Count
  assertions_total = $script:assertions
  assertions_passed = $script:assertions - $script:failures.Count
  assertions_failed = $script:failures.Count
  defects = @($script:failures)
} | ConvertTo-Json -Depth 3
if ($script:failures.Count) { exit 1 }
```

## Observed Raw Output

```json
{
  "snapshot_sha": "f0d19c34ac03075d64548f1059e9c6626d3596b5",
  "todos": 12,
  "allow_list": 34,
  "todo_artifacts": 28,
  "ledger_rows": 30,
  "phase0_outputs": 28,
  "assertions_total": 186,
  "assertions_passed": 186,
  "assertions_failed": 0,
  "defects": []
}
```

## Assertion Results

| Assertion group | Result | Count |
| --- | --- | ---: |
| Column-zero Todo parse | pass | 12 Todos |
| Exact plan write allow-list parse | pass | 34 paths |
| Todo acceptance/evidence declarations | pass | 12 |
| Required Todo artifacts present | pass | 28 |
| Todo 12 persisted dossier gate | pass | 607/607 |
| Workflow stage entering F1 | pass | `verification` |
| Owner approval and execution gates cited | pass | 2 |
| Todo 1 status/hash ledger rows | pass | 30 |
| Pre-existing non-dossier hashes and deleted paths | pass | 13 checks |
| Phase 0 outputs restricted to allow-list | pass | 28, including this F1 receipt |
| Protected owner/forbidden path hashes | pass | 6 |
| Recorded receipt command metadata has no prohibited Git mutation | pass | 35 Markdown artifacts scanned |
| Canonical plan SHA-256 | pass | 1 |

## Mismatches And Defects

None. A pre-write execution counted 27 Phase 0 outputs; after this dedicated
F1 receipt was created, the stable reproducible count is 28. The exact re-run
above includes this allow-listed receipt and passes 186/186 assertions.

## Terminal Result

`verdict: APPROVE`

All F1 assertions passed. No implementation Todo is blocked, no pre-existing
non-dossier status or hash changed, no forbidden owner path changed, no
prohibited Git mutation is recorded, and the persisted workflow remains at
`verification` pending independent F2-F4 and later owner acceptance.
