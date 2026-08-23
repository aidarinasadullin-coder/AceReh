#!/usr/bin/env pwsh
# F3 QA-failure probe builder (plan line 519). Task-owned artifacts only, under final/f3/probes/.
# Builds: A zero-test TRX, B unexpected-identity TRX, C duplicate-identity TRX,
#         D corrupted expected-selector manifest, E corrupted copied unknown-pipe fixture set.
# Never touches source fixtures, frozen binaries, or canonical task artifacts.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$ev = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path   # .../phase-4-thermal-state
$trxDir = Join-Path $PSScriptRoot '..\TestResults'
$srcTrx = (Resolve-Path (Join-Path $trxDir 'f3-calculation-failure.trx')).Path

function Save-Trx([System.Xml.XmlDocument]$doc, [string]$path) {
    $settings = [System.Xml.XmlWriterSettings]::new()
    $settings.Encoding = [System.Text.UTF8Encoding]::new($false)
    $settings.Indent = $true
    $w = [System.Xml.XmlWriter]::Create($path, $settings)
    $doc.Save($w); $w.Close()
}

# --- Probe A: zero-test TRX (all UnitTestResult nodes removed) ---
$doc = [System.Xml.XmlDocument]::new(); $doc.Load($srcTrx)
$nodes = @($doc.GetElementsByTagName('UnitTestResult'))
foreach ($n in $nodes) { [void]$n.ParentNode.RemoveChild($n) }
Save-Trx $doc (Join-Path $PSScriptRoot 'trx-zero-test.trx')

# --- Probe B: unexpected identity (clone result, fresh testId + foreign testName) ---
$doc = [System.Xml.XmlDocument]::new(); $doc.Load($srcTrx)
$first = @($doc.GetElementsByTagName('UnitTestResult'))[0]
$clone = $first.CloneNode($true)
$clone.SetAttribute('testId', '11111111-2222-3333-4444-555555555555')
$clone.SetAttribute('testName', 'SnowMeltingCalculator.Tests.Probes.UnexpectedIdentityProbe')
$clone.SetAttribute('outcome', 'Passed')
[void]$first.ParentNode.AppendChild($clone)
Save-Trx $doc (Join-Path $PSScriptRoot 'trx-unexpected.trx')

# --- Probe C: duplicate identity (identical cloned UnitTestResult) ---
$doc = [System.Xml.XmlDocument]::new(); $doc.Load($srcTrx)
$first = @($doc.GetElementsByTagName('UnitTestResult'))[0]
$clone = $first.CloneNode($true)
[void]$first.ParentNode.AppendChild($clone)
Save-Trx $doc (Join-Path $PSScriptRoot 'trx-duplicate.trx')

# --- Probe D: corrupted expected selector (one CalculationFailure identity removed) ---
$manifestSrc = (Resolve-Path (Join-Path $ev 'task-2\expected-negative-test-identities.json')).Path
$m = Get-Content -LiteralPath $manifestSrc -Raw | ConvertFrom-Json
$kept = @($m.CalculationFailure | Select-Object -SkipLast 1)
$out = [ordered]@{
    CalculationFailure = $kept
    PersistenceFailure = $m.PersistenceFailure
    RestoreFailure     = $m.RestoreFailure
}
$json = ConvertTo-Json -InputObject ([pscustomobject]$out) -Depth 10
[System.IO.File]::WriteAllText((Join-Path $PSScriptRoot 'corrupted-expected-manifest.json'), $json, [System.Text.UTF8Encoding]::new($false))

# --- Probe E: corrupted copied unknown-pipe fixture set ---
$fxDir = Join-Path $PSScriptRoot 'fixture-corrupt'
New-Item -ItemType Directory -Force -Path $fxDir | Out-Null
$fixtures = Join-Path $ev 'final\f3\fixtures'
Copy-Item (Join-Path $fixtures 'project-a.smc') $fxDir -Force
Copy-Item (Join-Path $fixtures 'project-b.smc') $fxDir -Force
Copy-Item (Join-Path $fixtures 'fixture-manifest.json') $fxDir -Force
$upText = Get-Content (Join-Path $fixtures 'unknown-pipe.smc') -Raw
$corrupt = $upText.Replace('"PHASE4-UNKNOWN-PIPE"', '"PHASE4-UNKNOWN-PIPE-CORRUPTED"')
if ($corrupt -eq $upText) { throw 'corruption replacement did not apply' }
[System.IO.File]::WriteAllText((Join-Path $fxDir 'unknown-pipe.smc'), $corrupt, [System.Text.UTF8Encoding]::new($false))

Write-Output 'probes built:'
Get-ChildItem $PSScriptRoot -Recurse -File | ForEach-Object { $_.FullName }
