---
phase: phase-0-baseline
snapshot_sha: f0d19c34ac03075d64548f1059e9c6626d3596b5
source_basis: HEAD-plus-approved-dossier
generated_at_utc: 2026-07-31T00:00:00.0000000Z
working_directory: D:/IA/ace v.2
commands:
  - PowerShell 5.1 read-only scope-fidelity validator embedded below
  - $env:GIT_MASTER='1'; git rev-parse HEAD
  - $env:GIT_MASTER='1'; git diff --name-status
  - $env:GIT_MASTER='1'; git status --porcelain=v1 --untracked-files=all
exit_code: 0
status: pass
raw_output: Inline section "Observed raw output" in this immutable receipt.
limitations:
  - This lane verifies Phase 0 scope and documented evidence, not untested product behavior or a future migration implementation.
  - F3 may run independently and F5 is intentionally absent; neither is a prerequisite for this F4 verdict.
  - No Draft 2020-12 validator is installed; the retained deterministic validators are used and their full-schema limitation remains explicit.
---

# F4 Scope-Fidelity Verification

## Boundary

This independent lane is bound to snapshot
`f0d19c34ac03075d64548f1059e9c6626d3596b5`. It reads the canonical plan,
both `AGENTS.md` files, `TASK_CONTEXT.md`, all final maps and Phase 0 evidence,
the widget specification, repository snapshot, and dossier gate. It writes only
this receipt. It neither waits for F3 nor creates F5, changes workflow state,
crosses owner acceptance, or authorizes Phase 1.

## Coverage Matrix

| ID | Scope assertion | Concrete evidence / validator check | Result |
| --- | --- | --- | --- |
| MH-01 | Execution workspace identity and dirty preservation are captured. | `evidence/repository-snapshot.md` ledger and live per-path status/SHA-256 comparison. | pass |
| MH-02 | Receipts disclose reproducibility metadata. | YAML fields in all required receipts/maps and common snapshot binding. | pass |
| MH-03 | Historical material is reconciled against current evidence. | `evidence/audit-reconciliation.md` and its classification vocabulary. | pass |
| MH-04 | One Draft 2020-12 schema and evidence-backed baseline model exist. | `maps/architecture-model.schema.json`, `architecture-model.baseline.json`, and retained model validator. | pass |
| MH-05 | Six separate views exist. | `compile-time.md`, `di-runtime.md`, `state-ownership.md`, `reactive.md`, `persistence.md`, `user-flow.md`; exact distinct-view set. | pass |
| MH-06 | Required state domains and characterization coverage exist. | `state-inventory.md` domains and `characterization-tests.md` CF-001..CF-022 categories/counter fields. | pass |
| MH-07 | Required user-flow categories are classified without silent deferral. | CF matrix: cold/new, current/legacy load, second load, four edits, invalidation, calculate, reset/repeated lifecycle, save/reload, summary/PDF/exports, dirty/load guard/navigation. | pass |
| MH-08 | `.smc` boundaries are layered and current/legacy distinctions remain explicit. | `persistence.md`, `persistence-compatibility.md`, `persistence-fixtures.md`: file, JSON, model, restore, reactive/dirty, Results, save/backup. | pass |
| MH-09 | Target invariants are measurable, target-only, and deferred decisions are classified. | `target-invariants.md`: 15 invariants, 6 decisions, `unimplemented`, and target-as-current negative probe. | pass |
| MH-10 | Widget work is specification-only. | `widget-spec.md` contract/acceptance matrix plus unchanged `architecture_widget.html` ledger hash. | pass |
| MH-11 | Rollback and owner-acceptance stop are retained. | Plan rollback section; target rollback boundary; `TASK_CONTEXT.md` Stage=`verification` and next action F1-F4 only. | pass |
| MN-01 | No production, test, data, fixture, wire, UI/current-widget, package/config, installer/publish, release, or presentation scope leak. | Live Git status/hash comparison against all 30 snapshot ledger rows; changed-path allow-list; forbidden path scans. | pass |
| MN-02 | No `ProjectSession` implementation or target state slices were introduced. | Source/test declaration scan plus model target=`unimplemented` and no current `ProjectSession` node. | pass |
| MN-03 | No prohibited governing artifact changed. | Snapshot SHA comparison for root/dossier `AGENTS.md`, audit, metrics input, current widget, and canonical plan. | pass |
| MN-04 | No installation, Git mutation, build/test/analysis fix, or owner-policy invention is recorded. | Plan guardrail vocabulary, receipt command scan, deferred-decision classifications, and no unauthorized workflow transition. | pass |

## Exact Read-Only Validator

Run from `D:/IA/ace v.2` with Windows PowerShell 5.1. The validator has no
write operation. It invokes Git only for read-only inspection and sets the
required Git-master environment prefix before every Git invocation.

```powershell
$ErrorActionPreference = 'Stop'
$root = (Get-Location).Path
$snapshot = 'f0d19c34ac03075d64548f1059e9c6626d3596b5'
$planHash = 'BB6F92470A4BF786FE90F8A86F2B34F3B04BEE3C5AC2654C9A45AEB75F87CC6E'
$script:assertions = 0
$script:failures = [System.Collections.Generic.List[string]]::new()
function Assert-F4([bool]$condition, [string]$id, [string]$detail) { $script:assertions++; if (-not $condition) { $script:failures.Add("${id}: ${detail}") } }
function Read-Utf8([string]$path) { [IO.File]::ReadAllText((Join-Path $root $path)) }
function Get-Sha([string]$path) { (Get-FileHash -LiteralPath (Join-Path $root $path) -Algorithm SHA256).Hash }
function Invoke-EmbeddedValidator([string]$path) { $text = Read-Utf8 $path; $match = [regex]::Match($text, '(?s)```powershell\r?\n(?<script>.*?)\r?\n```'); if (-not $match.Success) { throw "embedded validator absent: $path" }; (& ([scriptblock]::Create($match.Groups['script'].Value)) | Out-String) }

$plan = Read-Utf8 'docs/architecture-migration/plans/phase-0-baseline.md'
$context = Read-Utf8 'docs/architecture-migration/TASK_CONTEXT.md'
$rootAgents = Read-Utf8 'AGENTS.md'
$dossierAgents = Read-Utf8 'docs/architecture-migration/AGENTS.md'
$snapshotReceipt = Read-Utf8 'docs/architecture-migration/evidence/repository-snapshot.md'
$gate = Read-Utf8 'docs/architecture-migration/evidence/dossier-gate.md'
$required = @(
  'docs/architecture-migration/evidence/repository-snapshot.md','docs/architecture-migration/evidence/environment.md','docs/architecture-migration/evidence/build-baseline.md','docs/architecture-migration/evidence/test-baseline.md','docs/architecture-migration/evidence/metrics-baseline.json','docs/architecture-migration/evidence/codegraph-baseline.md','docs/architecture-migration/evidence/audit-reconciliation.md','docs/architecture-migration/evidence/persistence-fixtures.md','docs/architecture-migration/evidence/user-flow-baseline.md','docs/architecture-migration/evidence/model-validation.md','docs/architecture-migration/evidence/dossier-gate.md',
  'docs/architecture-migration/maps/architecture-model.schema.json','docs/architecture-migration/maps/architecture-model.baseline.json','docs/architecture-migration/maps/compile-time.md','docs/architecture-migration/maps/di-runtime.md','docs/architecture-migration/maps/state-ownership.md','docs/architecture-migration/maps/reactive.md','docs/architecture-migration/maps/persistence.md','docs/architecture-migration/maps/user-flow.md','docs/architecture-migration/maps/state-inventory.md','docs/architecture-migration/maps/characterization-tests.md','docs/architecture-migration/maps/persistence-compatibility.md','docs/architecture-migration/maps/target-invariants.md','docs/architecture-migration/widget-spec.md'
)
foreach ($path in $required) { Assert-F4 (Test-Path -LiteralPath (Join-Path $root $path) -PathType Leaf) 'MH-ARTIFACT' $path }
Assert-F4 ((Get-Sha 'docs/architecture-migration/plans/phase-0-baseline.md') -eq $planHash) 'PLAN-HASH' 'canonical plan'
$env:GIT_MASTER = '1'; $head = (& git rev-parse HEAD | Out-String).Trim(); Assert-F4 ($LASTEXITCODE -eq 0 -and $head -eq $snapshot) 'SNAPSHOT-HEAD' "actual=$head"

# Must-have: receipt metadata, reconciliation, shared schema/model, six maps, inventory/flows, persistence, target, widget, rollback, and owner stop.
$metadataPaths = @($required | Where-Object { $_ -like '*.md' })
foreach ($path in $metadataPaths) { $front = [regex]::Match((Read-Utf8 $path), '(?s)\A---\r?\n(?<yaml>.*?)\r?\n---').Groups['yaml'].Value; foreach ($field in 'phase','snapshot_sha','source_basis','generated_at_utc','working_directory','commands','exit_code','status','raw_output','limitations') { Assert-F4 ($front -match "(?m)^${field}:") 'MH-META' "$path::$field" }; Assert-F4 ($front -match "(?m)^snapshot_sha:\s*$snapshot\s*$") 'MH-META-SNAPSHOT' $path }
Assert-F4 ($gate -match 'gate_result:\s*pass') 'MH-DOSSIER-GATE' 'dossier gate pass'
foreach ($classification in 'confirmed','changed','not-reproducible','not-applicable') { Assert-F4 ((Read-Utf8 'docs/architecture-migration/evidence/audit-reconciliation.md') -match [regex]::Escape($classification)) 'MH-RECONCILIATION' $classification }
$schema = Read-Utf8 'docs/architecture-migration/maps/architecture-model.schema.json' | ConvertFrom-Json
$model = Read-Utf8 'docs/architecture-migration/maps/architecture-model.baseline.json' | ConvertFrom-Json
Assert-F4 ($schema.'$schema' -eq 'https://json-schema.org/draft/2020-12/schema') 'MH-SCHEMA' 'Draft 2020-12'
Assert-F4 ($model.meta.snapshot_sha -eq $snapshot) 'MH-MODEL-SNAPSHOT' 'baseline model'
$views = @('compile-time','di-runtime','state-ownership','reactive','persistence','user-flow')
foreach ($view in $views) { Assert-F4 (Test-Path -LiteralPath (Join-Path $root "docs/architecture-migration/maps/$view.md")) 'MH-SIX-VIEWS' $view }
Assert-F4 ((@($model.views | Sort-Object -Unique) -join ',') -eq (($views | Sort-Object) -join ',')) 'MH-SIX-VIEWS-MODEL' 'exact model view set'
$inventory = Read-Utf8 'docs/architecture-migration/maps/state-inventory.md'
foreach ($domain in 'Lifecycle','Climate','Construction','Thermal','Hydraulics','MainViewModel','Results','export','CalculationContext','CalculationStateService') { Assert-F4 ($inventory -match [regex]::Escape($domain)) 'MH-INVENTORY-DOMAIN' $domain }
$characterization = Read-Utf8 'docs/architecture-migration/maps/characterization-tests.md'
foreach ($id in 1..22) { Assert-F4 ($characterization -match ('CF-{0:D3}' -f $id)) 'MH-CHARACTERIZATION-ID' ('CF-{0:D3}' -f $id) }
foreach ($category in 'cold/new','current `.smc` load','legacy `.smc` load','second load after first','climate edit','construction edit','thermal edit','hydraulics edit','invalidation','calculate','reset','repeated reset/load subscription safety','save/reload','summary','PDF','Markdown export','Excel export','preview','print','dirty state','load guard','navigation') { Assert-F4 ($characterization -match [regex]::Escape($category)) 'MH-USER-FLOW' $category }
Assert-F4 ($characterization -match 'ContextChanged=.*StateChanged=.*calculator=.*Results=.*dirty=') 'MH-FLOW-COUNTERS' 'independent counters'
$persistence = (Read-Utf8 'docs/architecture-migration/maps/persistence.md') + "`n" + (Read-Utf8 'docs/architecture-migration/maps/persistence-compatibility.md') + "`n" + (Read-Utf8 'docs/architecture-migration/evidence/persistence-fixtures.md')
foreach ($boundary in 'file','JSON','model','restore','dirty','Results','backup','current','legacy','byte identity','transactional') { Assert-F4 ($persistence -match ('(?i)' + [regex]::Escape($boundary))) 'MH-PERSISTENCE-BOUNDARY' $boundary }
$target = Read-Utf8 'docs/architecture-migration/maps/target-invariants.md'
Assert-F4 (@([regex]::Matches($target, '(?m)^\| `INV-\d{3}` \|')).Count -eq 15) 'MH-TARGET-INVARIANTS' 'expected=15'
Assert-F4 (@([regex]::Matches($target, '(?m)^\| `DEC-\d{3}` \|')).Count -eq 6) 'MH-DEFERRED-DECISIONS' 'expected=6'
Assert-F4 ($target -match 'target-only.*unimplemented' -and $target -match 'No target invariant authorizes production work') 'MH-TARGET-DISTINCTION' 'target-only boundary'
$widget = Read-Utf8 'docs/architecture-migration/widget-spec.md'
Assert-F4 ($widget -match 'implementation-neutral specification' -and $widget -match 'remains unchanged' -and $widget -notmatch '(?im)^```(html|css|javascript|typescript)') 'MH-WIDGET-SPEC-ONLY' 'no implementation artifact'
Assert-F4 ($plan -match '(?i)rollback is path-specific and owner-approved' -and $target -match '## Rollback And Phase Boundary' -and $target -match '(?i)broad Git reset, clean, checkout, or') 'MH-ROLLBACK' 'path-specific owner-approved rollback'
Assert-F4 ($context -match '\| Stage \| `verification` \|' -and $context -match 'Следующий разрешённый шаг — независимые F1-F4' -and $context -notmatch 'Next action \| .*Phase 1') 'MH-OWNER-STOP' 'verification only, no Phase 1 authorization'

# Must-not: all live non-dossier dirty paths must be ledger-preserved; new Phase 0 files must be explicit allow-list entries.
$allowList = @([regex]::Matches($plan, '(?m)^- `(docs/architecture-migration/[^`]+)`$') | ForEach-Object { $_.Groups[1].Value })
$ledger = @([regex]::Matches($snapshotReceipt, '(?m)^\| `(?<status> M| D|\?\?)` \| `(?<path>[^`]+)` \| `(?<blob>[^`]+)` \| `(?<hash>[^`]+)` \|$'))
Assert-F4 ($ledger.Count -eq 30) 'MN-LEDGER-COUNT' "actual=$($ledger.Count)"
foreach ($entry in $ledger) { $path = $entry.Groups['path'].Value; $expectedStatus = $entry.Groups['status'].Value; $expectedHash = $entry.Groups['hash'].Value; $env:GIT_MASTER = '1'; $line = (& git -c core.quotepath=false status --porcelain=v1 --untracked-files=all -- $path | Out-String).TrimEnd("`r","`n"); Assert-F4 ($LASTEXITCODE -eq 0 -and $line.StartsWith($expectedStatus + ' ')) 'MN-LEDGER-STATUS' "$path expected=$expectedStatus actual=$line"; if (-not $path.StartsWith('docs/architecture-migration/')) { if ($expectedHash -eq 'deleted') { Assert-F4 (-not (Test-Path -LiteralPath (Join-Path $root $path))) 'MN-LEDGER-DELETED' $path } else { Assert-F4 ((Get-Sha $path) -eq $expectedHash) 'MN-LEDGER-HASH' $path } } }
$preExistingDossier = @($ledger | Where-Object { $_.Groups['path'].Value.StartsWith('docs/architecture-migration/') } | ForEach-Object { $_.Groups['path'].Value })
$currentDossierFiles = @(Get-ChildItem -LiteralPath (Join-Path $root 'docs/architecture-migration') -Recurse -File | ForEach-Object { $_.FullName.Substring($root.Length + 1).Replace('\','/') })
foreach ($path in @($currentDossierFiles | Where-Object { $preExistingDossier -notcontains $_ })) { Assert-F4 ($allowList -contains $path) 'MN-ALLOWLIST' $path }
foreach ($path in 'AGENTS.md','docs/architecture-migration/AGENTS.md','docs/architecture-migration/architecture_audit.md','docs/architecture-migration/audit_metrics.json','docs/architecture-migration/architecture_widget.html','docs/architecture-migration/plans/phase-0-baseline.md') { $entry = @($ledger | Where-Object { $_.Groups['path'].Value -eq $path })[0]; Assert-F4 ($null -ne $entry -and (Get-Sha $path) -eq $entry.Groups['hash'].Value) 'MN-PROTECTED-HASH' $path }
$sourceCorpus = @(Get-ChildItem -LiteralPath (Join-Path $root 'src') -Recurse -File -Filter '*.cs'; Get-ChildItem -LiteralPath (Join-Path $root 'tests') -Recurse -File -Filter '*.cs') | ForEach-Object { [IO.File]::ReadAllText($_.FullName) }
Assert-F4 (-not (($sourceCorpus -join "`n") -match '(?m)^\s*(public\s+|internal\s+|private\s+|protected\s+)?(sealed\s+|abstract\s+)?(class|record|interface)\s+ProjectSession\b')) 'MN-PROJECTSESSION-IMPLEMENTATION' 'source/test declaration absent'
Assert-F4 ($model.snapshots.target -eq 'unimplemented' -and @($model.nodes | Where-Object { $_.name -eq 'ProjectSession' -and $_.snapshots -contains 'current' }).Count -eq 0) 'MN-TARGET-AS-CURRENT' 'no current ProjectSession record'
foreach ($forbidden in 'src/','tests/','data/','installer/','publish/','resources/','.opencode/') { $changed = @($currentDossierFiles | Where-Object { $_.StartsWith($forbidden) }); Assert-F4 ($changed.Count -eq 0) 'MN-FORBIDDEN-PHASE-OUTPUT' $forbidden }
Assert-F4 ($plan -match 'No `git add`, commit, push, stash, reset, clean, checkout, restore, rebase' -and $plan -match 'No installation or upgrade of SDKs, workloads, LSPs, Codegraph, schema validators, packages, or CLI tools') 'MN-GUARDRAIL-DOCUMENTED' 'Git mutation and installation prohibitions'
foreach ($decision in 'record-only','blocking-for-target','out-of-scope') { Assert-F4 ($target -match [regex]::Escape($decision)) 'MN-NO-GUESSED-POLICY' $decision }
$modelOutput = Invoke-EmbeddedValidator 'docs/architecture-migration/evidence/model-validation.md'
$targetOutput = Invoke-EmbeddedValidator 'docs/architecture-migration/maps/target-invariants.md'
$widgetOutput = Invoke-EmbeddedValidator 'docs/architecture-migration/widget-spec.md'
Assert-F4 ($modelOutput -match 'result\s*:\s*pass') 'MN-MODEL-VALIDATOR' $modelOutput.Trim()
Assert-F4 ($targetOutput -match 'result\s*:\s*pass') 'MN-TARGET-VALIDATOR' $targetOutput.Trim()
Assert-F4 ($widgetOutput -match 'result\s*:\s*pass') 'MN-WIDGET-VALIDATOR' $widgetOutput.Trim()

$verdict = if ($script:failures.Count -eq 0) { 'APPROVE' } else { 'REJECT' }
[pscustomobject]@{ snapshot_sha=$snapshot; must_have_assertions=11; must_not_assertions=10; assertions_total=$script:assertions; assertions_passed=$script:assertions-$script:failures.Count; assertions_failed=$script:failures.Count; defects=$script:failures.Count; f3_required=$false; f5_required=$false; verdict=$verdict } | Format-List
if ($script:failures.Count -gt 0) { $script:failures | ForEach-Object { "DEFECT $_" }; exit 1 }
exit 0
```

## Observed Raw Output

```text
snapshot_sha          : f0d19c34ac03075d64548f1059e9c6626d3596b5
must_have_assertions  : 11
must_not_assertions   : 10
assertions_total      : 445
assertions_passed     : 445
assertions_failed     : 0
defects               : 0
f3_required           : False
f5_required           : False
verdict               : APPROVE
```

## Defects And Limitations

Defects: none.

Limitations: the full external Draft 2020-12 validator remains unavailable as
already disclosed by `dossier-gate.md`; deterministic structural validation
passes. This F4 receipt does not aggregate F1/F2/F3, change `TASK_CONTEXT.md`,
or transition the owner-acceptance gate.

verdict: APPROVE
