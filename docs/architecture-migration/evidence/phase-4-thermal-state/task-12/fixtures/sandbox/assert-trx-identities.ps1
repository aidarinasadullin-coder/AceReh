#!/usr/bin/env pwsh
<#
.SYNOPSIS
  Todo 12 (phase-4-thermal-state): negative-category TRX identity assert (test-only tool).

.DESCRIPTION
  Parameters (exact): -InputFile <path.trx> -ExpectedManifest <expected-negative-test-identities.json>
                      -ExpectedGroup <CalculationFailure|PersistenceFailure|RestoreFailure> -Output <json>

  Accepts EXACTLY one TRX, one immutable expected-identity manifest and one group.
  The manifest must contain exactly the three keys CalculationFailure,
  PersistenceFailure and RestoreFailure; each is a non-empty array of unique
  non-empty fully-qualified identities and the groups are pairwise disjoint.

  STRICT set equality: the TRX identity set must EQUAL manifest[ExpectedGroup].
    - every expected identity present exactly once with outcome Passed;
    - ANY extra TRX identity (including an identity from another manifest
      group) is unexpected -> reject. This proves a category-filtered lane
      contains no unmanifested identities in the category.
  Additionally rejects: any non-Passed outcome, duplicate identities inside
  the TRX, zero tests in the TRX and an empty manifest group.

  Output JSON (UTF-8 WITHOUT BOM):
    {"status":"ok","group":"...","inputTrx":"...","expected":N,"matched":N,
     "identities":["...", ...sorted Ordinal]}

  Exit codes: 0 ok; 2 usage/invalid input or invalid manifest; 3 data rejection.
#>
param(
    [Parameter(Mandatory = $true)][string]$InputFile,
    [Parameter(Mandatory = $true)][string]$ExpectedManifest,
    [Parameter(Mandatory = $true)][string]$ExpectedGroup,
    [Parameter(Mandatory = $true)][string]$Output
)

$ErrorActionPreference = 'Continue'
Set-StrictMode -Version Latest

$exitUsage = 2
$exitDataRejection = 3

function Write-Utf8NoBom {
    param([string]$Path, [string]$Text)
    [System.IO.File]::WriteAllText($Path, $Text, [System.Text.UTF8Encoding]::new($false))
}

function ConvertTo-JsonString {
    param([string]$Value)
    $sb = [System.Text.StringBuilder]::new('"')
    foreach ($ch in $Value.ToCharArray()) {
        $code = [int]$ch
        # NOTE: if/elseif chain - `continue` inside a scalar PowerShell switch
        # falls through to post-switch code, corrupting escaped characters.
        if ($ch -eq '"') { [void]$sb.Append('\"') }
        elseif ($ch -eq '\') { [void]$sb.Append('\\') }
        elseif ($ch -eq "`b") { [void]$sb.Append('\b') }
        elseif ($ch -eq "`f") { [void]$sb.Append('\f') }
        elseif ($ch -eq "`n") { [void]$sb.Append('\n') }
        elseif ($ch -eq "`r") { [void]$sb.Append('\r') }
        elseif ($ch -eq "`t") { [void]$sb.Append('\t') }
        elseif ($code -lt 0x20) { [void]$sb.Append(('\u{0:x4}' -f $code)) }
        else { [void]$sb.Append($ch) }
    }
    [void]$sb.Append('"')
    return $sb.ToString()
}

$validGroups = @('CalculationFailure', 'PersistenceFailure', 'RestoreFailure')
if ($ExpectedGroup -notin $validGroups) {
    Write-Error "ExpectedGroup must be one of CalculationFailure|PersistenceFailure|RestoreFailure (got '$ExpectedGroup')"
    exit $exitUsage
}

if (-not (Test-Path -LiteralPath $InputFile -PathType Leaf)) {
    Write-Error "input TRX not found: $InputFile"
    exit $exitUsage
}
if (-not (Test-Path -LiteralPath $ExpectedManifest -PathType Leaf)) {
    Write-Error "expected-identity manifest not found: $ExpectedManifest"
    exit $exitUsage
}

# ------------------------------------------------------------ immutable manifest
try {
    $manifest = Get-Content -LiteralPath $ExpectedManifest -Raw | ConvertFrom-Json
}
catch {
    Write-Error "manifest is not valid JSON: $($_.Exception.Message)"
    exit $exitUsage
}

$requiredKeys = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($g in $validGroups) { [void]$requiredKeys.Add($g) }
$manifestKeys = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($p in @($manifest.PSObject.Properties.Name)) { [void]$manifestKeys.Add([string]$p) }

foreach ($k in $requiredKeys) {
    if (-not $manifestKeys.Contains($k)) {
        Write-Error "manifest is missing required group '$k'"
        exit $exitUsage
    }
}
foreach ($k in $manifestKeys) {
    if (-not $requiredKeys.Contains($k)) {
        Write-Error "manifest has unexpected key '$k' (closed set: CalculationFailure|PersistenceFailure|RestoreFailure)"
        exit $exitUsage
    }
}

$groupSets = @{}
foreach ($g in $validGroups) {
    $arr = @($manifest.$g)
    if ($arr.Count -eq 0) {
        Write-Error "empty group '$g' in manifest"
        exit $exitDataRejection
    }
    $set = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($id in $arr) {
        if ($null -eq $id -or $id -isnot [string] -or [string]::IsNullOrWhiteSpace($id)) {
            Write-Error "group '$g' contains a non-string or empty identity"
            exit $exitUsage
        }
        if (-not $set.Add($id)) {
            Write-Error "group '$g' contains duplicate identity '$id'"
            exit $exitUsage
        }
    }
    $groupSets[$g] = $set
}
for ($i = 0; $i -lt $validGroups.Count; $i++) {
    for ($j = $i + 1; $j -lt $validGroups.Count; $j++) {
        foreach ($id in $groupSets[$validGroups[$j]]) {
            if ($groupSets[$validGroups[$i]].Contains($id)) {
                Write-Error "identity '$id' appears in both '$($validGroups[$i])' and '$($validGroups[$j])' (groups must be disjoint)"
                exit $exitUsage
            }
        }
    }
}

$expected = $groupSets[$ExpectedGroup]

# ------------------------------------------------------------------- parse TRX
$doc = [System.Xml.XmlDocument]::new()
try {
    $doc.Load($InputFile)
}
catch {
    Write-Error "malformed XML in '${InputFile}': $($_.Exception.Message)"
    exit $exitDataRejection
}

# Fully-qualified identity map: UnitTest@id -> TestMethod@class+"."+@name
# (same resolution as Todo 1's parse-trx.ps1).
$fqnById = @{}
foreach ($ut in @($doc.GetElementsByTagName('UnitTest'))) {
    $tm = $null
    foreach ($child in $ut.ChildNodes) {
        if ($child.LocalName -eq 'TestMethod') { $tm = $child; break }
    }
    if ($null -ne $tm -and $null -ne $tm.Attributes['className'] -and $null -ne $tm.Attributes['name']) {
        $idAttr = $ut.Attributes['id']
        if ($null -ne $idAttr) {
            $fqnById[$idAttr.Value] = "$($tm.Attributes['className'].Value).$($tm.Attributes['name'].Value)"
        }
    }
}

$results = @($doc.GetElementsByTagName('UnitTestResult'))
if ($results.Count -eq 0) {
    Write-Error "zero tests in '${InputFile}' (empty TRX)"
    exit $exitDataRejection
}

$outcomes = @{}
$seen = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($node in $results) {
    $testIdAttr = $node.Attributes['testId']
    $nameAttr = $node.Attributes['testName']
    $outcomeAttr = $node.Attributes['outcome']

    $identity = $null
    if ($null -ne $testIdAttr -and $fqnById.ContainsKey($testIdAttr.Value)) {
        $identity = $fqnById[$testIdAttr.Value]
    }
    elseif ($null -ne $nameAttr -and -not [string]::IsNullOrEmpty($nameAttr.Value)) {
        $identity = $nameAttr.Value
    }
    else {
        Write-Error "UnitTestResult without resolvable identity in '${InputFile}'"
        exit $exitDataRejection
    }

    if ($null -eq $outcomeAttr) {
        Write-Error "UnitTestResult without outcome for '$identity' in '${InputFile}'"
        exit $exitDataRejection
    }
    $outcome = $outcomeAttr.Value
    if ($outcome -notin @('Passed', 'Failed', 'NotExecuted')) {
        Write-Error "unknown outcome '$outcome' for test '$identity' in '${InputFile}'"
        exit $exitDataRejection
    }
    if (-not $seen.Add($identity)) {
        Write-Error "duplicate test identity '$identity' in '${InputFile}'"
        exit $exitDataRejection
    }
    $outcomes[$identity] = $outcome
}

# ------------------------------------------------------- strict set reconciliation
$missing = [System.Collections.Generic.List[string]]::new()
foreach ($id in $expected) {
    if (-not $seen.Contains($id)) { $missing.Add($id) }
}
if ($missing.Count -gt 0) {
    $sortedMissing = $missing.ToArray()
    [Array]::Sort($sortedMissing, [StringComparer]::Ordinal)
    Write-Error ("absent expected identities ({0}): {1}" -f $sortedMissing.Count, ($sortedMissing -join '; '))
    exit $exitDataRejection
}

$unexpected = [System.Collections.Generic.List[string]]::new()
foreach ($id in $seen) {
    if (-not $expected.Contains($id)) { $unexpected.Add($id) }
}
if ($unexpected.Count -gt 0) {
    $sortedUnexpected = $unexpected.ToArray()
    [Array]::Sort($sortedUnexpected, [StringComparer]::Ordinal)
    Write-Error ("unexpected identities not in manifest group '{0}' ({1}): {2}" -f $ExpectedGroup, $sortedUnexpected.Count, ($sortedUnexpected -join '; '))
    exit $exitDataRejection
}

$nonPassed = [System.Collections.Generic.List[string]]::new()
foreach ($id in $seen) {
    if ($outcomes[$id] -ne 'Passed') { $nonPassed.Add("$id=$($outcomes[$id])") }
}
if ($nonPassed.Count -gt 0) {
    Write-Error ("non-Passed outcomes ({0}): {1}" -f $nonPassed.Count, (($nonPassed.ToArray() | Sort-Object { $_ }) -join '; '))
    exit $exitDataRejection
}

# NOTE: @(...) not .ToArray() - HashSet[T] has no real instance ToArray();
# PowerShell cannot invoke LINQ extension methods directly.
$identities = @($expected)
[Array]::Sort($identities, [StringComparer]::Ordinal)

$sb = [System.Text.StringBuilder]::new()
[void]$sb.AppendLine('{')
[void]$sb.AppendLine('  "status": "ok",')
[void]$sb.AppendLine(('  "group": "' + $ExpectedGroup + '",'))
[void]$sb.AppendLine(('  "inputTrx": ' + (ConvertTo-JsonString $InputFile) + ','))
[void]$sb.AppendLine(('  "expected": ' + $expected.Count + ','))
[void]$sb.AppendLine(('  "matched": ' + $identities.Count + ','))
[void]$sb.AppendLine('  "identities": [')
for ($i = 0; $i -lt $identities.Count; $i++) {
    $comma = if ($i -lt $identities.Count - 1) { ',' } else { '' }
    [void]$sb.AppendLine(('    ' + (ConvertTo-JsonString $identities[$i]) + $comma))
}
[void]$sb.AppendLine('  ]')
[void]$sb.Append('}')
Write-Utf8NoBom -Path $Output -Text $sb.ToString()

Write-Host ("assert-trx-identities: group=" + $ExpectedGroup + " expected=" + $expected.Count + " matched=" + $identities.Count + " outcome=ok")
exit 0
