#!/usr/bin/env pwsh
<#
.SYNOPSIS
  Todo 1 (phase-4-thermal-state): TRX parser (test-only tool).

.DESCRIPTION
  Accepts EITHER -InputFile <path.trx> OR -InputDirectory <dir> (exactly one),
  plus required -Output <json>. Reads TRX via XmlDocument ([xml] semantics,
  never ConvertFrom-Json) and extracts exact FULLY-QUALIFIED test identities:
  NUnit/VSTest TRX stores a short display name in UnitTestResult@testName, so
  the exact identity is resolved through UnitTestResult@testId -> UnitTest@id
  -> TestMethod@class+"."+@name (raw testName is the fallback when no linkage
  exists). Outcomes: Passed/Failed/NotExecuted.

    {"tests":[{"name":"...","outcome":"Passed"},...],
     "counts":{"total":N,"passed":N,"failed":N,"notExecuted":N}}

   tests are sorted by name (Ordinal). Rejects with nonzero exit:
     - missing input file / directory / no .trx files in directory
     - malformed XML
     - zero tests
     - duplicate test identities within a single file
     - conflicting outcomes for one identity across files
       (benign cross-file overlap between a suite TRX and its category
        extracts is deduplicated per owner decision AMZ-4, 2026-08-23)
     - unknown outcome values or UnitTestResult without testName

  Output JSON is UTF-8 WITHOUT BOM.
#>
param(
    [string]$InputFile = "",
    [string]$InputDirectory = "",
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

if (([string]::IsNullOrWhiteSpace($InputFile) -and [string]::IsNullOrWhiteSpace($InputDirectory)) -or
    (-not [string]::IsNullOrWhiteSpace($InputFile) -and -not [string]::IsNullOrWhiteSpace($InputDirectory))) {
    Write-Error "usage: exactly one of -InputFile <path.trx> or -InputDirectory <dir> is required"
    exit $exitUsage
}

$trxPaths = [System.Collections.Generic.List[string]]::new()
if (-not [string]::IsNullOrWhiteSpace($InputFile)) {
    if (-not (Test-Path -LiteralPath $InputFile -PathType Leaf)) {
        Write-Error "input TRX not found: $InputFile"
        exit $exitUsage
    }
    $trxPaths.Add($InputFile)
}
else {
    if (-not (Test-Path -LiteralPath $InputDirectory -PathType Container)) {
        Write-Error "input directory not found: $InputDirectory"
        exit $exitUsage
    }
    $files = Get-ChildItem -LiteralPath $InputDirectory -Filter *.trx -File |
        Sort-Object -Property Name -CaseSensitive:$false
    if (@($files).Count -eq 0) {
        Write-Error "no .trx files found in directory: $InputDirectory"
        exit $exitUsage
    }
    foreach ($f in $files) { $trxPaths.Add($f.FullName) }
}

$tests = [System.Collections.Generic.List[object]]::new() # {name, outcome}
$seenOutcome = @{} # identity -> outcome (AMZ-4: cross-file overlap deduplicated when outcomes agree)
$total = 0; $passed = 0; $failed = 0; $notExecuted = 0

foreach ($trx in $trxPaths) {
    $doc = [System.Xml.XmlDocument]::new()
    try {
        $doc.Load($trx)
    }
    catch {
        Write-Error "malformed XML in '${trx}': $($_.Exception.Message)"
        exit $exitDataRejection
    }
    # Fully-qualified identity map: UnitTest@id -> TestMethod@class+"."+@name.
    # NUnit/VSTest TRX keeps only a short display name in UnitTestResult@testName;
    # short names collide across fixture classes, so the exact identity is the
    # linked TestMethod className + "." + name.
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
        Write-Error "zero tests in '${trx}'"
        exit $exitDataRejection
    }
    $fileSeen = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($node in $results) {
        $testIdAttr = $node.Attributes['testId']
        $nameAttr = $node.Attributes['testName']
        $outcomeAttr = $node.Attributes['outcome']

        $identity = $null
        if ($null -ne $testIdAttr -and $fqnById.ContainsKey($testIdAttr.Value)) {
            $identity = $fqnById[$testIdAttr.Value]
        }
        elseif ($null -ne $nameAttr -and -not [string]::IsNullOrEmpty($nameAttr.Value)) {
            # fallback for TRX without TestMethod linkage
            $identity = $nameAttr.Value
        }
        else {
            Write-Error "UnitTestResult without resolvable identity in '${trx}'"
            exit $exitDataRejection
        }

        if ($null -eq $outcomeAttr) {
            Write-Error "UnitTestResult without outcome in '${trx}'"
            exit $exitDataRejection
        }
        $outcome = $outcomeAttr.Value
        if ($outcome -notin @('Passed', 'Failed', 'NotExecuted')) {
            Write-Error "unknown outcome '$outcome' for test '$identity' in '${trx}'"
            exit $exitDataRejection
        }
        if (-not $fileSeen.Add($identity)) {
            Write-Error "duplicate test identity '$identity' in '${trx}'"
            exit $exitDataRejection
        }
        if ($seenOutcome.ContainsKey($identity)) {
            if ($seenOutcome[$identity] -ne $outcome) {
                Write-Error "conflicting outcomes for test identity '$identity' ('$($seenOutcome[$identity])' vs '$outcome') in '${trx}'"
                exit $exitDataRejection
            }
            # AMZ-4 (2026-08-23): benign cross-file overlap between a suite TRX and
            # its category extracts - same identity, same outcome - counted once.
            continue
        }
        $seenOutcome[$identity] = $outcome
        $tests.Add([pscustomobject]@{ Name = $identity; Outcome = $outcome })
        $total++
        switch ($outcome) {
            'Passed' { $passed++ }
            'Failed' { $failed++ }
            'NotExecuted' { $notExecuted++ }
        }
    }
}

$sorted = $tests.ToArray()
[Array]::Sort($sorted, [Comparison[object]] {
        param($a, $b)
        [StringComparer]::Ordinal.Compare([string]$a.Name, [string]$b.Name)
    })

$sb = [System.Text.StringBuilder]::new()
[void]$sb.AppendLine('{')
[void]$sb.AppendLine('  "tests": [')
for ($j = 0; $j -lt $sorted.Count; $j++) {
    $t = $sorted[$j]
    $comma = if ($j -lt $sorted.Count - 1) { ',' } else { '' }
    [void]$sb.AppendLine(('    {"name": ' + (ConvertTo-JsonString $t.Name) + ', "outcome": "' + $t.Outcome + '"}' + $comma))
}
[void]$sb.AppendLine('  ],')
[void]$sb.AppendLine(('  "counts": {"total": ' + $total + ', "passed": ' + $passed + ', "failed": ' + $failed + ', "notExecuted": ' + $notExecuted + '}'))
[void]$sb.Append('}')
Write-Utf8NoBom -Path $Output -Text $sb.ToString()

Write-Host ("parse-trx: files=" + $trxPaths.Count + " total=$total passed=$passed failed=$failed notExecuted=$notExecuted")
exit 0
