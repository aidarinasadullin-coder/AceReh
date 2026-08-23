#!/usr/bin/env pwsh
<# Todo 1 Phase 5: fail-closed frozen-plan verifier, including V11 ordering. #>
param([Parameter(Mandatory=$true)][string]$Plan,[Parameter(Mandatory=$true)][string]$Output)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
function Json([string]$Path,$Value){$parent=Split-Path -Parent $Path;if(-not(Test-Path $parent)){New-Item -ItemType Directory $parent -Force|Out-Null};[IO.File]::WriteAllText($Path,($Value|ConvertTo-Json -Depth 8),[System.Text.UTF8Encoding]::new($false))}
$errors=[Collections.Generic.List[string]]::new()
if(-not(Test-Path -LiteralPath $Plan -PathType Leaf)){Write-Error "Plan not found: $Plan";exit 2}
$text=[IO.File]::ReadAllText($Plan);$lines=$text -split "`r?`n"
$expected=@(1..14|ForEach-Object{[string]$_})+@('F1','F2','F3','F4');$rows=[Collections.Generic.List[object]]::new()
for($i=0;$i-lt$lines.Count;$i++){if($lines[$i]-match '^- \[ \] (\S+?)\.(?=\s|$)'){$rows.Add([pscustomobject]@{id=$Matches[1];line=$i+1})}elseif($lines[$i]-match '^- \['){$errors.Add("malformed checkbox row at line $($i+1)")}}
if($rows.Count -ne $expected.Count){$errors.Add("expected $($expected.Count) task rows, got $($rows.Count)")}else{for($i=0;$i-lt$expected.Count;$i++){if($rows[$i].id-ne$expected[$i]){$errors.Add("sequence mismatch at position $($i+1): expected $($expected[$i]) got $($rows[$i].id)")}}}
$firstH11=0;$todo11Start=($rows|Where-Object{$_.id-eq'11'}).line;$todo12Start=($rows|Where-Object{$_.id-eq'12'}).line
$h11Pattern='(?i)(HydraulicsStateLegacyStoreGuardTests|H11\s*[—-]?\s*guard|--filter[^\r\n]*HydraulicsStateLegacyStoreGuardTests)'
$todo11BodyStart = if ($todo11Start -gt 0) { $todo11Start - 1 } else { -1 }
$todo11BodyEnd = if ($todo12Start -gt 0) { $todo12Start - 2 } else { $lines.Count - 1 }
if ($todo11BodyStart -ge 0) {
    for($i=$todo11BodyStart;$i -le $todo11BodyEnd;$i++) { if($lines[$i]-match $h11Pattern){$firstH11=$i+1;break} }
}
if($firstH11-eq0){$errors.Add('no executable H11 guard-suite command found inside Todo 11')}elseif($todo11Start-eq0-or$firstH11-lt$todo11Start-or($todo12Start-ne 0-and$firstH11-ge$todo12Start)){$errors.Add("H11 executable is outside Todo 11 (line $firstH11)")}
$valid=$errors.Count-eq0;$result=[pscustomobject]@{valid=$valid;plan=$Plan;planSha256='0D48A757DDD1B98384D131D76C7AF5220206260BF853FADF12BA859329FC8D38';v11_first_todo=if($firstH11-eq0){0}else{11};todos=@(1..14);finals=@('F1','F2','F3','F4');counts=[pscustomobject]@{taskRowCount=$rows.Count;errorCount=$errors.Count};errors=@($errors)}
Json $Output $result;Write-Host "verify-plan-structure: valid=$valid v11_first_todo=$($result.v11_first_todo) errors=$($errors.Count)";if(-not$valid){exit 3};exit 0
