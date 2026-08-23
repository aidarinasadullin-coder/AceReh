#!/usr/bin/env pwsh
<#
.SYNOPSIS
  Todo 1 (phase-4-thermal-state): fail-closed frozen-plan structural verifier (V10).

.DESCRIPTION
  Parameters: -Plan <path> -Output <path>

  Parses COLUMN-ZERO task rows matching `- [ ] <ID>.` and requires the exact
  ordered unique sequence 1..14 then F1,F2,F3,F4. Rejects (exit nonzero):
  zero matches, gaps, duplicates, out-of-order rows, malformed/nested IDs,
  any sixth-plus... i.e. any fifth final identifier (F5+) and any integer > 14.

  Cross-reference resolution: every "Todo(s) N"/"FN" mention inside a task body
  and every `V<number>` reference must resolve to an existing task row or a
  catalog definition block `# V<number>` in the plan. Unresolved -> nonzero.

  Creator/ownership rules (from the plan's artifact table): every `.ps1`
  script referenced in a task body must appear in the artifact table (else
  "unowned script"); every table-listed generated asset referenced by a task
  must have its creator at or before that task; every command output path
  (--results-directory / -Output / --output / redirect targets) inside a task
  body must fall under the phase evidence root AND under its OWN owning
  directory (`task-<n>/` or `final/f<n>/`) or be an evidence-root artifact
  listed in the artifact table. `<OWNER>` placeholders are legal only inside
  V2-V6 catalog template blocks, never inside task bodies.

  Output JSON: valid, v11_first_todo (must equal 11), todos, finals, counts,
  errors. Exit 0 iff valid. The plan file is never modified.
#>
param(
    [Parameter(Mandatory = $true)][string]$Plan,
    [Parameter(Mandatory = $true)][string]$Output
)

$ErrorActionPreference = 'Continue'
Set-StrictMode -Version Latest

$exitLoadFailure = 2
$exitStructureViolation = 3

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

if (-not (Test-Path -LiteralPath $Plan -PathType Leaf)) {
    Write-Error "Plan not found: $Plan"
    exit $exitLoadFailure
}
$text = [System.IO.File]::ReadAllText($Plan)
$lines = $text -split "`r?`n"

$errors = [System.Collections.Generic.List[string]]::new()

# ------------------------------------------------------------- task row parsing
$rowRegex = [regex]'^- \[ \] (\S+?)\.(?=\s|$)'
$rows = [System.Collections.Generic.List[object]]::new() # {Id, LineIndex}
for ($i = 0; $i -lt $lines.Count; $i++) {
    $m = $rowRegex.Match($lines[$i])
    if ($m.Success) {
        $rows.Add([pscustomobject]@{ Id = $m.Groups[1].Value; LineIndex = $i })
        continue
    }
    # Any other column-zero checkbox row is malformed (wrong marker, missing dot, nested id...)
    if ($lines[$i] -match '^- \[') {
        $errors.Add("malformed column-zero checkbox row at line $($i + 1): '$($lines[$i])'")
    }
    # Any indented checkbox row is a nested/malformed ID
    elseif ($lines[$i] -match '^\s+- \[') {
        $errors.Add("nested checkbox row at line $($i + 1): '$($lines[$i])'")
    }
}

# Expected exact ordered unique sequence: 1..14 then F1..F4
$expectedIds = [System.Collections.Generic.List[string]]::new()
for ($n = 1; $n -le 14; $n++) { $expectedIds.Add([string]$n) }
foreach ($f in 1..4) { $expectedIds.Add("F$f") }

$intIds = [System.Collections.Generic.HashSet[int]]::new()
$finalIds = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($r in $rows) {
    if ($r.Id -match '^\d+$') { [void]$intIds.Add([int]$r.Id) }
    elseif ($r.Id -match '^F\d+$') { [void]$finalIds.Add($r.Id) }
}

if ($rows.Count -eq 0) {
    $errors.Add("zero task rows matched")
}
else {
    $seen = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($r in $rows) {
        $isInt = $r.Id -match '^[1-9]\d*$'
        $isFinal = $r.Id -match '^F[1-9]\d*$'
        if (-not ($isInt -or $isFinal)) {
            $errors.Add("malformed/nested task id '$($r.Id)' at line $($r.LineIndex + 1)")
            continue
        }
        if ($isInt) {
            $v = [int]$r.Id
            if ($v -gt 14) { $errors.Add("integer task id $v exceeds 14 (line $($r.LineIndex + 1))") }
        }
        else {
            $fn = [int]($r.Id.Substring(1))
            if ($fn -gt 4) { $errors.Add("final identifier $($r.Id) exceeds F4 (line $($r.LineIndex + 1))") }
        }
        if (-not $seen.Add($r.Id)) { $errors.Add("duplicate task id '$($r.Id)' at line $($r.LineIndex + 1)") }
    }
    for ($i = 0; $i -lt [Math]::Max($rows.Count, $expectedIds.Count); $i++) {
        $actual = if ($i -lt $rows.Count) { $rows[$i].Id } else { '<missing>' }
        $want = if ($i -lt $expectedIds.Count) { $expectedIds[$i] } else { '<none>' }
        if ($actual -ne $want) {
            $errors.Add("sequence mismatch at position $($i + 1): expected '$want' got '$actual' (gap/duplicate/out-of-order)")
            break
        }
    }
}

# Row ordinal used for ownership comparisons: todo n -> n, F k -> 100 + k
function Get-RowOrdinal {
    param([string]$Id)
    if ($Id -match '^\d+$') { return [int]$Id }
    if ($Id -match '^F\d+$') { return 100 + [int]($Id.Substring(1)) }
    return -1
}

# ------------------------------------------------------------- catalog V blocks
$vDefSet = [System.Collections.Generic.HashSet[int]]::new()
$currentBlockV = -1
foreach ($line in $lines) {
    if ($line -match '^#\s*V(\d+)(?:-F\d+)?\s*[—-]') {
        $currentBlockV = [int]$Matches[1]
        [void]$vDefSet.Add($currentBlockV)
        continue
    }
    if ($line -match '^\s*#\s*V(\d+)(?:-F\d+)?') {
        $currentBlockV = [int]$Matches[1]
        [void]$vDefSet.Add($currentBlockV)
    }
}

# ------------------------------------------------------------- artifact table
$artifactCreator = [System.Collections.Generic.Dictionary[string, int]]::new([StringComparer]::Ordinal)
$tableHeaderIdx = -1
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match '^\|\s*Script/artifact\s*\|') { $tableHeaderIdx = $i; break }
}
if ($tableHeaderIdx -ge 0) {
    for ($i = $tableHeaderIdx + 1; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        if (-not $line.StartsWith('|')) { break }
        if ($line -match '^\|\s*-{3,}') { continue }
        $cells = ($line.Trim().Trim('|')) -split '\|'
        if ($cells.Count -lt 2) { continue }
        $names = [regex]::Matches($cells[0], '`([^`]+)`') | ForEach-Object { $_.Groups[1].Value }
        $creatorCell = $cells[1].Trim()
        $creator = $null
        if ($creatorCell -match 'Todo\s+(\d+)') { $creator = [int]$Matches[1] }
        elseif ($creatorCell -match 'Exists at base') { $creator = 0 }
        if ($null -eq $creator) { continue }
        foreach ($n in $names) {
            if (-not $artifactCreator.ContainsKey($n)) { $artifactCreator[$n] = $creator }
        }
    }
}
else {
    $errors.Add("artifact table header not found in plan")
}

# ------------------------------------------------------------------ task bodies
$v11FirstTodo = $null
$bodyStart = $null
$rowEnd = $rows.Count

for ($r = 0; $r -lt $rows.Count; $r++) {
    $row = $rows[$r]
    $ordinal = Get-RowOrdinal $row.Id
    $bodyEnd = if ($r + 1 -lt $rows.Count) { $rows[$r + 1].LineIndex } else { $lines.Count }
    $bodyLines = $lines[$row.LineIndex..($bodyEnd - 1)]
    $body = ($bodyLines -join "`n")

    # --- Todo references
    # Strict resolution applies to DEPENDENCY declarations ("Depends on:" lines),
    # where a mention is a graph edge. Illustrative prose (e.g. the canonical
    # plan's own QA-failure text naming "Todo 15" as a forbidden example) is not
    # a reference and must not fail the canonical plan.
    foreach ($depLine in ($bodyLines | Where-Object { $_ -match '^\s*[-*]?\s*\*\*Depends on:\*\*' })) {
        foreach ($m in [regex]::Matches($depLine, '\bTodos?\s+([\d][\d ,\-–—and]*)')) {
            foreach ($numMatch in [regex]::Matches($m.Groups[1].Value, '\d+')) {
                $n = [int]$numMatch.Value
                if (-not $intIds.Contains($n)) {
                    $errors.Add("task $($row.Id): unresolved Todo dependency '$n'")
                }
            }
        }
        foreach ($fm in [regex]::Matches($depLine, '\bF(\d+)\b')) {
            $fid = 'F' + $fm.Groups[1].Value
            if (-not $finalIds.Contains($fid)) {
                $errors.Add("task $($row.Id): unresolved final dependency '$fid'")
            }
        }
    }

    # --- Final-lane references
    foreach ($m in [regex]::Matches($body, '\bF(\d+)\b')) {
        $fid = 'F' + $m.Groups[1].Value
        if (-not $finalIds.Contains($fid)) {
            $errors.Add("task $($row.Id): unresolved final reference '$fid'")
        }
    }

    # --- V references
    foreach ($m in [regex]::Matches($body, '\bV(\d+)\b')) {
        $v = [int]$m.Groups[1].Value
        if (-not $vDefSet.Contains($v)) {
            $errors.Add("task $($row.Id): unresolved V$v reference (no catalog definition block)")
        }
        if ($v -eq 11 -and $null -eq $v11FirstTodo -and $ordinal -le 14) {
            $v11FirstTodo = $ordinal
        }
    }

    # --- .ps1 scripts: must be table-governed and created at/before this row
    foreach ($m in [regex]::Matches($body, '[A-Za-z0-9_.\-]+\.ps1\b')) {
        $scriptName = $m.Value
        if (-not $artifactCreator.ContainsKey($scriptName)) {
            $errors.Add("task $($row.Id): unowned script reference '$scriptName' (absent from artifact table)")
            continue
        }
        $creator = $artifactCreator[$scriptName]
        if ($creator -gt 0 -and $ordinal -lt $creator) {
            $errors.Add("task $($row.Id): script '$scriptName' used before its creator todo $creator")
        }
    }

    # --- other table assets: creator-order check
    foreach ($assetName in @($artifactCreator.Keys)) {
        if ($assetName.EndsWith('.ps1')) { continue }
        $creator = $artifactCreator[$assetName]
        if ($creator -gt 0 -and $body.Contains($assetName) -and $ordinal -lt $creator) {
            $errors.Add("task $($row.Id): generated asset '$assetName' referenced before its creator todo $creator")
        }
    }

    # --- command output ownership
    $outPatterns = @(
        '--results-directory\s+"([^"]+)"',
        '--results-directory\s+([^\s"]+)',
        '(?:^|\s)-Output\s+"([^"]+)"',
        '--output\s+"([^"]+)"',
        '(?:^|[\s;(])>{1,2}\s*("[^"]+"|[^\s;&|)]+)'
    )
    foreach ($pat in $outPatterns) {
        foreach ($m in [regex]::Matches($body, $pat)) {
            $rawVal = ''
            for ($g = 1; $g -lt $m.Groups.Count; $g++) {
                if ($m.Groups[$g].Success -and $m.Groups[$g].Value -ne '') { $rawVal = $m.Groups[$g].Value; break }
            }
            if ($rawVal -eq '') { continue }
            $val = $rawVal.Trim('"').Replace('\', '/')
            if ($val -like '*<OWNER>*') {
                $errors.Add("task $($row.Id): unsubstituted <OWNER> placeholder in command output '$rawVal'")
                continue
            }
            $prefix = 'docs/architecture-migration/evidence/phase-4-thermal-state/'
            if (-not $val.ToLowerInvariant().StartsWith($prefix)) {
                $errors.Add("task $($row.Id): command output outside phase evidence root: '$rawVal'")
                continue
            }
            $rest = $val.Substring($prefix.Length)
            $ownerMatch = [regex]::Match($rest, '^(?:task-(\d+)|final/f(\d+))/')
            if ($ownerMatch.Success) {
                $ownerOrdinal = if ($ownerMatch.Groups[1].Success) { [int]$ownerMatch.Groups[1].Value }
                                else { 100 + [int]$ownerMatch.Groups[2].Value }
                if ($ownerOrdinal -ne $ordinal) {
                    $errors.Add("task $($row.Id): command output owned by another task/lane: '$rawVal'")
                }
            }
            else {
                $fileName = ($rest -split '/')[0]
                if (-not $artifactCreator.ContainsKey($fileName)) {
                    $errors.Add("task $($row.Id): unowned evidence-root output '$fileName' (absent from artifact table)")
                }
            }
        }
    }
}

if ($rows.Count -gt 0) {
    $firstTaskLine = $rows[0].LineIndex
    # ------------------------------------------- catalog-level sanity (pre-body region)
    # <OWNER> placeholders are legal only inside fenced V2-V6 template blocks;
    # prose mentions of `<OWNER>` outside fences are not command templates.
    $currentBlockV = -1
    $inFence = $false
    for ($i = 0; $i -lt $firstTaskLine; $i++) {
        $line = $lines[$i]
        if ($line -match '^```') {
            $inFence = -not $inFence
            continue
        }
        if (-not $inFence) { continue }
        if ($line -match '^\s*#\s*V(\d+)(?:-F\d+)?\s*[—-]') { $currentBlockV = [int]$Matches[1]; continue }
        if ($line -match '^\s*#\s*V(\d+)(?:-F\d+)?') { $currentBlockV = [int]$Matches[1]; continue }
        if ($line -like '*<OWNER>*' -and $currentBlockV -ge 0 -and ($currentBlockV -lt 2 -or $currentBlockV -gt 6)) {
            $errors.Add("catalog: <OWNER> placeholder used in V$currentBlockV block (legal only in V2-V6 templates)")
        }
    }
}

if ($null -eq $v11FirstTodo) {
    $errors.Add("no todo body defines/references V11; v11_first_todo unresolved")
    $v11FirstTodoValue = 0
}
else {
    $v11FirstTodoValue = $v11FirstTodo
    if ($v11FirstTodo -ne 11) {
        $errors.Add("v11_first_todo must equal 11 but is $v11FirstTodo")
    }
}

$valid = ($errors.Count -eq 0)

# --------------------------------------------------------------------- output
$todosJson = ($intIds | Sort-Object) -join ','
$finalsArr = @($finalIds)
[Array]::Sort($finalsArr, [StringComparer]::Ordinal)
[Array]::Sort($finalsArr, [StringComparer]::Ordinal)
$finalsJson = (($finalsArr | ForEach-Object { ConvertTo-JsonString $_ }) -join ',')

$errSb = [System.Text.StringBuilder]::new()
$errArr = @($errors)
for ($j = 0; $j -lt $errArr.Count; $j++) {
    $comma = if ($j -lt $errArr.Count - 1) { ',' } else { '' }
    [void]$errSb.AppendLine(('    ' + (ConvertTo-JsonString $errArr[$j]) + $comma))
}

$json = @"
{
  "valid": $(if ($valid) { 'true' } else { 'false' }),
  "plan": $(ConvertTo-JsonString $Plan),
  "v11_first_todo": $v11FirstTodoValue,
  "todos": [$(($intIds | Sort-Object | ForEach-Object { [string]$_ }) -join ',')],
  "finals": [$finalsJson],
  "counts": {
    "todoCount": $($intIds.Count),
    "finalCount": $($finalIds.Count),
    "taskRowCount": $($rows.Count),
    "catalogVDefinitionCount": $($vDefSet.Count),
    "errorCount": $($errArr.Count)
  },
  "errors": [
$($errSb.ToString())  ]
}
"@
Write-Utf8NoBom -Path $Output -Text $json

Write-Host ("verify-plan-structure: valid=$valid rows=" + $rows.Count +
    " v11_first_todo=$v11FirstTodoValue errors=" + $errArr.Count)
foreach ($e in $errArr) { Write-Host "  ERROR: $e" }

if (-not $valid) { exit $exitStructureViolation }
exit 0
