#!/usr/bin/env pwsh
# Todo 12 phase-5: fail-closed arithmetic reconciliation validator.
# Validates an arithmetic receipt: sums must match exactly and every
# NotExecuted identity must be a member of the accepted baseline set.
# Exit 0 = reconciled; exit 3 = mismatch (report printed to stdout).
param(
    [Parameter(Mandatory=$true)][string]$Receipt
)
$ErrorActionPreference = 'Stop'; Set-StrictMode -Version Latest
$mismatches=[Collections.Generic.List[string]]::new()
try { $a=Get-Content -LiteralPath $Receipt -Raw | ConvertFrom-Json } catch { Write-Output "RECONCILE FAIL: receipt JSON invalid: $($_.Exception.Message)"; exit 3 }
function Add-Mismatch([string]$msg){ $mismatches.Add($msg) }

if ($a.baseline_passed + $a.new_test_count -ne $a.full_passed) {
    Add-Mismatch ("sum: baseline_passed({0}) + new_test_count({1}) != full_passed({2})" -f $a.baseline_passed,$a.new_test_count,$a.full_passed)
}
if ($a.baseline_total + $a.new_test_count -ne $a.full_parser_total) {
    Add-Mismatch ("sum: baseline_total({0}) + new_test_count({1}) != full_parser_total({2})" -f $a.baseline_total,$a.new_test_count,$a.full_parser_total)
}
$skipped=0; if ($a.PSObject.Properties['skipped'] -and $null -ne $a.skipped) { $skipped=[int]$a.skipped }
if ($a.full_passed + $a.full_failed + $skipped + $a.full_notExecuted -ne $a.full_parser_total) {
    Add-Mismatch ("partition: passed({0}) + failed({1}) + skipped({2}) + notExecuted({3}) != total({4})" -f $a.full_passed,$a.full_failed,$skipped,$a.full_notExecuted,$a.full_parser_total)
}
if ($a.focused_passed + $a.focused_failed -ne $a.focused_total) {
    Add-Mismatch ("focused: passed({0}) + failed({1}) != focused_total({2})" -f $a.focused_passed,$a.focused_failed,$a.focused_total)
}
$accepted=@($a.acceptedNotExecutedBaseline); foreach($id in @($a.notExecutedIdentities)){
    if($accepted -notcontains $id){ Add-Mismatch "identity: '$id' is NOT a member of the accepted NotExecuted baseline set" }
}
if(@($a.notExecutedIdentities).Count -gt $accepted.Count){ Add-Mismatch "identity-count: more NotExecuted identities than accepted baseline set size" }

if($mismatches.Count -gt 0){
    Write-Output "RECONCILE FAIL: $($mismatches.Count) mismatch(es) in $Receipt"
    foreach($m in $mismatches){ Write-Output "  - $m" }
    exit 3
}
Write-Output "RECONCILE OK: sums match and identities subset holds for $Receipt"
exit 0
