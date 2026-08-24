# =============================================================================
# Todo 13 (phase-5-hydraulics-state) agent-operated Hydraulics UI QA harness.
# Adapted from evidence/phase-4-thermal-state/run-wpf-ui-qa.ps1 (V9, exit-0 run
# of 2026-08-23). Same inbox-.NET-only machinery: System.Windows.Automation
# (UIAutomation), System.Drawing, Start-Process; interactive desktop required;
# executable SHA-256 validated before and after EVERY process launch; selector
# ambiguity / unexpected dialog / crash => nonzero exit, no manual fallback.
#
# Phase-5 flows driven against the REAL app window:
#   S1 fixture manifest + input SHA verification + frozen exe SHA (pre-run)
#   S2 launch project-a -> clean loaded title
#   S3 hydraulics outputs match v1-sample.smc stored fixture math
#   S4 glycol type/concentration edits -> AUTO recalculation oracles
#      (computed from data/glycol_data.json with the service's bilinear
#      interpolation) incl. out-of-range validation message branch
#   S5 edit first circuit length (keyboard DataGrid editing) -> Рассчитать ->
#      Results summary card updates
#   S6 save via Файл->Сохранить -> close -> reload same file -> identical outputs
#   S7 second load project-b -> clean replace, no stale project-A values
#   S8 reset (Файл->Создать новый расчёт) -> defaults restored
#   F  FAILURE BRANCH (separate process run): corrupt unknown-pipe.smc ->
#      graceful validation dialog ('Ошибка' / 'Не удалось открыть проект:'),
#      process stays alive, dismissed via OK, clean close
# Every observation row records step id, action, expected selector, found
# element runtimeId, expected/observed/outcome, screenshot path and timestamp.
# Exit 0 only if ALL happy-path steps observed AND failure branch graceful.
# =============================================================================
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Executable,
    [Parameter(Mandatory = $true)][string]$ProjectA,
    [Parameter(Mandatory = $true)][string]$ProjectB,
    [Parameter(Mandatory = $true)][string]$InvalidProject,
    [Parameter(Mandatory = $true)][string]$OutputDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# --------------------------------------------------------------- constants ---
$script:MainTitleSuffix     = 'Калькулятор снеготаяния REHAU'
$script:DialogTitleError    = 'Ошибка'
$script:DialogTitleClose    = 'Закрытие приложения'
$script:DialogTitleNew      = 'Создать новый расчёт'
$script:StderrCrashPatterns = @(
    'Unhandled exception', 'Unhandled Exception', 'Необработанное исключение',
    'XamlParseException', 'Stack overflow', 'Access violation',
    'Критическая ошибка', 'Критический сбой'
)
$script:WindowWaitMs = 60000
$script:ExitWaitMs   = 40000
$script:PollMs       = 300

function Fail([string]$message) {
    throw [System.InvalidOperationException]::new("run-hydraulics-flows: $message")
}

if ($PSVersionTable.PSEdition -ne 'Core') {
    Fail "this script must run under pwsh (PowerShell Core); got edition '$($PSVersionTable.PSEdition)'"
}

# ------------------------------------------------------------ resolve paths ---
$script:RepoRoot = (Get-Location).Path
function Resolve-InputPath([string]$p) {
    if ([System.IO.Path]::IsPathRooted($p)) { return $p }
    return [System.IO.Path]::GetFullPath((Join-Path $script:RepoRoot $p))
}
$script:ExePath      = Resolve-InputPath $Executable
$script:PathA        = Resolve-InputPath $ProjectA
$script:PathB        = Resolve-InputPath $ProjectB
$script:PathInvalid  = Resolve-InputPath $InvalidProject
$script:OutDir       = Resolve-InputPath $OutputDirectory
$script:ShotDir      = Join-Path $script:OutDir 'screenshots'
$script:GlycolJson   = Join-Path $script:RepoRoot 'data\glycol_data.json'

foreach ($f in @($script:ExePath, $script:PathA, $script:PathB, $script:PathInvalid, $script:GlycolJson)) {
    if (-not (Test-Path -LiteralPath $f -PathType Leaf)) { Fail "required input not found: $f" }
}
foreach ($d in @($script:OutDir, $script:ShotDir)) {
    if (-not (Test-Path -LiteralPath $d)) { New-Item -ItemType Directory -Path $d -Force | Out-Null }
}

$script:Utf8NoBom = [System.Text.UTF8Encoding]::new($false)
function Write-Utf8NoBomFile([string]$path, [string]$text) {
    [System.IO.File]::WriteAllText($path, $text, $script:Utf8NoBom)
}
function Get-Sha256File([string]$path) {
    return (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToUpperInvariant()
}
function Get-RelPath([string]$abs) {
    $norm = $abs.Replace('/', '\')
    $root = $script:RepoRoot.Replace('/', '\').TrimEnd('\') + '\'
    if ($norm.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $norm.Substring($root.Length)
    }
    return $norm
}
function Read-FileTextSafe([string]$path) {
    try { return [System.IO.File]::ReadAllText($path) } catch { return '' }
}

# ---------------------------------------------- fixture math (glycol tables) ---
# Replicates GlycolDataService interpolation (bilinear over concentration x
# temperature with NaN pass-through, GlycolDataService.cs:520-600) and
# CircuitsCalculator power/flow formulas (CircuitsCalculator.cs:20-60) so every
# recalculation oracle is computed FROM THE FIXTURE + data/glycol_data.json.
function Find-LowerIndexPs([double[]]$a, [double]$v) {
    if ($a.Count -eq 0) { return 0 }
    if ($v -le $a[0]) { return 0 }
    if ($v -ge $a[$a.Count - 1]) { return $a.Count - 2 }
    for ($i = 0; $i -lt $a.Count - 1; $i++) {
        if ($a[$i] -le $v -and $v -lt $a[$i + 1]) { return $i }
    }
    return $a.Count - 2
}
function ConvertFrom-GlycolTable([object]$src) {
    $temps = [double[]]@($src.data | ForEach-Object { [double]$_.temp_c })
    return @{ conc = [double[]]$src.concentration_vol_pct; temps = $temps; rows = @($src.data) }
}
function Get-GlycolProp([hashtable]$t, [double]$conc, [double]$temp) {
    $cs = $t.conc; $ts = $t.temps
    function valAt([hashtable]$t, [int]$c, [int]$ti) {
        $v = $t.rows[$ti].values[$c]
        if ($null -eq $v) { return [double]::NaN }
        return [double]$v
    }
    function lerpN([double]$x1, [double]$x2, [double]$y1, [double]$y2, [double]$x) {
        if ([double]::IsNaN($y1) -and [double]::IsNaN($y2)) { return [double]::NaN }
        if ([double]::IsNaN($y1)) { return $y2 }
        if ([double]::IsNaN($y2)) { return $y1 }
        if ([Math]::Abs($x2 - $x1) -lt 1e-10) { return $y1 }
        return $y1 + (($x - $x1) / ($x2 - $x1)) * ($y2 - $y1)
    }
    $cl = Find-LowerIndexPs $cs $conc
    $tl = Find-LowerIndexPs $ts $temp
    $ch = [Math]::Min($cl + 1, $cs.Count - 1)
    $th = [Math]::Min($tl + 1, $ts.Count - 1)
    if ($cl -eq $ch -and $tl -eq $th) { return valAt $t $cl $tl }
    if ($cl -eq $ch) { return lerpN $ts[$tl] $ts[$th] (valAt $t $cl $tl) (valAt $t $cl $th) $temp }
    if ($tl -eq $th) { return lerpN $cs[$cl] $cs[$ch] (valAt $t $cl $tl) (valAt $t $ch $tl) $conc }
    $v1 = lerpN $ts[$tl] $ts[$th] (valAt $t $cl $tl) (valAt $t $cl $th) $temp
    $v2 = lerpN $ts[$tl] $ts[$th] (valAt $t $ch $tl) (valAt $t $ch $th) $temp
    return lerpN $cs[$cl] $cs[$ch] $v1 $v2 $conc
}
function Get-GlycolProps([string]$type, [double]$conc, [double]$temp) {
    if ($null -eq $script:GlycolTables) {
        $j = Get-Content -LiteralPath $script:GlycolJson -Raw -Encoding UTF8 | ConvertFrom-Json
        $script:GlycolTables = @{
            ethylene  = @{
                density = (ConvertFrom-GlycolTable $j.ethylene_glycol.density_kg_m3)
                cp      = (ConvertFrom-GlycolTable $j.ethylene_glycol.specific_heat_kJ_kgK)
            }
            propylene = @{
                density = (ConvertFrom-GlycolTable $j.propylene_glycol.density_kg_m3)
                cp      = (ConvertFrom-GlycolTable $j.propylene_glycol.specific_heat_kJ_kgK)
            }
        }
    }
    $tbl = if ($type -eq 'propylene') { $script:GlycolTables.propylene } else { $script:GlycolTables.ethylene }
    return @{
        rho = (Get-GlycolProp $tbl.density $conc $temp)
        cp  = (Get-GlycolProp $tbl.cp $conc $temp)
    }
}
function Get-CircuitPower([double]$L, [double]$Lzul, [double]$spcCm, [double]$spcZulCm, [double]$heatPct, [double]$qUp, [double]$qDown) {
    # CircuitsCalculator.CalculateCircuitPower
    $lengthPerArea = $L / (100.0 / $spcCm)
    $supplyPerArea = $Lzul / (100.0 / $spcZulCm)
    return (($lengthPerArea + $supplyPerArea * ($heatPct / 100.0)) * ($qUp + $qDown))
}
function Get-FlowRateLh([double]$powerW, [double]$deltaT, [double]$rho, [double]$cp) {
    # CircuitsCalculator.CalculateFlowRate (л/ч)
    return $powerW * 3.6 / ($rho * $cp * $deltaT) * 1000.0
}
function Format-UiNumber([double]$v, [int]$decimals) {
    # WPF binding StringFormat renders with the app UI culture, observed as
    # dot-decimal (en/invariant) on this desktop: '1172.0', '88.0', '25'.
    return $v.ToString('F' + $decimals, [System.Globalization.CultureInfo]::InvariantCulture)
}

# ------------------------------------------- interactive desktop + assemblies ---
if (-not [Environment]::UserInteractive) {
    Fail 'no interactive desktop session (UserInteractive=false); the harness requires a Windows interactive desktop'
}
try {
    Add-Type -AssemblyName UIAutomationClient
    Add-Type -AssemblyName UIAutomationTypes
    Add-Type -AssemblyName System.Drawing
    Add-Type -AssemblyName System.Windows.Forms
}
catch {
    Fail "inbox UI automation assemblies unavailable: $($_.Exception.Message)"
}
try {
    Add-Type -Namespace Win32Uaq5 -Name NativeMethods -MemberDefinition (
        '[DllImport("user32.dll")] public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);' +
        '[DllImport("user32.dll")] public static extern bool SetProcessDPIAware();' +
        '[DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();' +
        '[DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);' +
        '[DllImport("user32.dll")] public static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);' +
        '[DllImport("user32.dll")] public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);')
    [void][Win32Uaq5.NativeMethods]::SetProcessDPIAware()
}
catch {
    Fail "user32 P/Invoke bootstrap failed: $($_.Exception.Message)"
}

# ------------------------------------------------------------- script state ---
$script:MainPid        = -1
$script:MainWindow     = $null
$script:MainHandle     = [IntPtr]::Zero
$script:ActiveProc     = $null
$script:ProcRuns       = [System.Collections.Generic.List[object]]::new()
$script:Steps          = [System.Collections.Generic.List[object]]::new()
$script:Shots          = [System.Collections.Generic.List[object]]::new()
$script:Deviations     = [System.Collections.Generic.List[string]]::new()
$script:FixtureInfo    = $null
$script:CurrentStep    = $null
$script:SelectorStats  = [ordered]@{}
$script:UnexpectedDialogs = [System.Collections.Generic.List[object]]::new()
$script:LastRuntimeId  = ''
$script:LastSelector   = ''
$script:ExeShaPreRun   = ''
$script:GlycolTables   = $null

# Accessibility registry for this task (existing IDs + STEP-A additions).
# optional=$true elements legitimately collapse out of the UIA tree when empty.
$script:SelectorRegistry = @(
    @{ id = 'HydraulicsGlycolType';         type = 'ComboBox'; view = 'Hydraulics'; optional = $false },
    @{ id = 'HydraulicsGlycolConcentration'; type = 'Edit';    view = 'Hydraulics'; optional = $false },
    @{ id = 'HydraulicsSupplySpacing';      type = 'Edit';     view = 'Hydraulics'; optional = $false },
    @{ id = 'HydraulicsSupplyHeatPercent';  type = 'Edit';     view = 'Hydraulics'; optional = $false },
    @{ id = 'HydraulicsCalculateButton';    type = 'Button';   view = 'Hydraulics'; optional = $false },
    @{ id = 'HydraulicsValidationMessage';  type = 'Text';     view = 'Hydraulics'; optional = $true  },
    @{ id = 'HydraulicsCircuitLengthFirst'; type = 'Edit';     view = 'Hydraulics'; optional = $true  },
    @{ id = 'HydraulicsPipeSpacing';        type = 'Text';     view = 'Hydraulics'; optional = $false },
    @{ id = 'HydraulicsSupplyTemperature';  type = 'Text';     view = 'Hydraulics'; optional = $false },
    @{ id = 'HydraulicsReturnTemperature';  type = 'Text';     view = 'Hydraulics'; optional = $false }
)
function Get-ControlTypeByName([string]$name) {
    switch ($name) {
        'ComboBox' { return [System.Windows.Automation.ControlType]::ComboBox }
        'Edit'     { return [System.Windows.Automation.ControlType]::Edit }
        'Button'   { return [System.Windows.Automation.ControlType]::Button }
        'Text'     { return [System.Windows.Automation.ControlType]::Text }
        'ListItem' { return [System.Windows.Automation.ControlType]::ListItem }
        default    { Fail "unknown control type name '$name'" }
    }
    return $null
}
foreach ($reg in $script:SelectorRegistry) {
    $script:SelectorStats[$reg.id] = [ordered]@{ controlType = $reg.type; view = $reg.view; resolvedCount = 0; lastPresent = $false }
}

# --------------------------------------------------------- step/assert machinery ---
function Start-Step([int]$n, [string]$name) {
    $script:CurrentStep = [ordered]@{
        step = $n; name = $name; status = 'RUNNING'
        assertions = [System.Collections.Generic.List[object]]::new()
        artifacts  = [System.Collections.Generic.List[object]]::new()
    }
    Write-Output ("run-hydraulics-flows: step {0}: {1}" -f $n, $name)
}
function Add-Assertion([string]$label, [string]$expected, [string]$observed, [bool]$pass,
                       [string]$selector = '', [string]$screenshot = '') {
    if ($null -ne $script:CurrentStep) {
        $script:CurrentStep.assertions.Add([ordered]@{
            assert = $label; expected = $expected; observed = $observed; outcome = $(if ($pass) { 'PASS' } else { 'FAIL' })
            selector = $selector; elementRuntimeId = $script:LastRuntimeId
            screenshot = $screenshot
            timestampUtc = [DateTime]::UtcNow.ToString('o')
        })
    }
    $script:LastRuntimeId = ''
    if (-not $pass) {
        Fail "assertion FAILED [$label]: expected <$expected>, observed <$observed>"
    }
}
function Add-ArtifactRecord([string]$path, [string]$note) {
    if ($null -ne $script:CurrentStep) {
        $script:CurrentStep.artifacts.Add([ordered]@{ path = (Get-RelPath $path); sha256 = (Get-Sha256File $path); note = $note })
    }
}
function Complete-Step {
    if ($null -eq $script:CurrentStep) { Fail 'Complete-Step called with no active step' }
    $script:CurrentStep.status = 'PASS'
    $script:Steps.Add($script:CurrentStep)
    $script:CurrentStep = $null
}
function Note-Deviation([string]$text) {
    $script:Deviations.Add($text)
}

function Get-FirstNameNumber([string]$text) {
    if ([string]::IsNullOrEmpty($text)) { return $null }
    $m = [regex]::Match($text, '-?\d+(?:[.,]\d+)?')
    if (-not $m.Success) { return $null }
    return [double]::Parse($m.Value.Replace(',', '.'), [System.Globalization.CultureInfo]::InvariantCulture)
}
function Test-Near([double]$parsed, [double]$expected, [int]$decimals) {
    $tol = 0.5 * [math]::Pow(10, -$decimals) + 0.0000001
    return ([math]::Abs($parsed - $expected) -le $tol)
}
function Assert-NumberNear([double]$parsed, [double]$expected, [int]$decimals, [string]$label,
                           [string]$selector = '', [string]$screenshot = '') {
    $ok = Test-Near $parsed $expected $decimals
    Add-Assertion $label ("{0} (+/-{1}dp)" -f $expected, $decimals) ([string]$parsed) $ok $selector $screenshot
}
function Assert-ExactText([string]$observed, [string]$expected, [string]$label,
                          [string]$selector = '', [string]$screenshot = '') {
    Add-Assertion $label $expected $observed ($observed -ceq $expected) $selector $screenshot
}
function Assert-ContainsText([string]$observed, [string]$needle, [string]$label,
                             [string]$selector = '', [string]$screenshot = '') {
    Add-Assertion $label "*$needle*" $observed (($null -ne $observed) -and $observed.Contains($needle)) $selector $screenshot
}

# ------------------------------------------------------- executable SHA gate ---
$script:ExeShaPreRun = Get-Sha256File $script:ExePath
function Test-ExeSha([string]$moment) {
    if (-not (Test-Path -LiteralPath $script:ExePath -PathType Leaf)) {
        Fail "executable disappeared at $moment"
    }
    $actual = Get-Sha256File $script:ExePath
    if ($actual -ne $script:ExeShaPreRun) {
        Fail "executable SHA-256 mismatch at ${moment}: expected $($script:ExeShaPreRun), actual $actual"
    }
    return $actual
}

# ----------------------------------------------------------- dialog machinery ---
function Get-AppWindows {
    $cond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::Window)
    $raw = [System.Windows.Automation.AutomationElement]::RootElement.FindAll(
        [System.Windows.Automation.TreeScope]::Children, $cond)
    $list = @()
    for ($i = 0; $i -lt $raw.Count; $i++) { $list += $raw.Item($i) }
    return ,$list
}
function Scan-Dialogs([string]$context) {
    if ($null -eq $script:MainWindow) { return }
    $wins = Get-AppWindows
    for ($i = 0; $i -lt $wins.Count; $i++) {
        $w = $wins[$i]
        try { $c = $w.Current } catch { continue }
        if ($c.ProcessId -ne $script:MainPid) { continue }
        if ($c.NativeWindowHandle -eq 0 -or $c.NativeWindowHandle -eq $script:MainHandle) { continue }
        if ($c.ClassName -match 'Popup') { continue }
        if ([string]::IsNullOrWhiteSpace($c.Name)) { continue }
        $title = $c.Name
        $script:UnexpectedDialogs.Add([ordered]@{ context = $context; title = $title; className = $c.ClassName })
        $dismissed = $false
        try {
            $btnCondType = New-Object System.Windows.Automation.PropertyCondition(
                [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
                [System.Windows.Automation.ControlType]::Button)
            $btns = $w.FindAll([System.Windows.Automation.TreeScope]::Descendants, $btnCondType)
            $cancelBtns = @()
            for ($b = 0; $b -lt $btns.Count; $b++) {
                $bn = $btns[$b].Current.Name
                if (($bn -ceq 'Cancel' -or $bn -ceq 'Отмена') -and $btns[$b].Current.IsEnabled) { $cancelBtns += $btns[$b] }
            }
            if ($cancelBtns.Count -eq 1) {
                ($cancelBtns[0].GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern) -as [System.Windows.Automation.InvokePattern]).Invoke()
                Start-Sleep -Milliseconds 1000
                $dismissed = $true
            }
        } catch { $dismissed = $false }
        Fail ("unexpected dialog '{0}' (context: {1}; dismissed-via-Cancel: {2})" -f $title, $context, $dismissed)
    }
}
function Assert-NoWindowByTitle([string]$title, [string]$context) {
    $wins = Get-AppWindows
    for ($i = 0; $i -lt $wins.Count; $i++) {
        $w = $wins[$i]
        try { $c = $w.Current } catch { continue }
        if ($c.ProcessId -eq $script:MainPid -and $c.Name -ceq $title) {
            Fail "forbidden window '$title' present (context: $context)"
        }
    }
}
function Find-AppDialogByTitle([string]$title) {
    # Owned modal dialogs may hang off the OWNER window instead of the desktop
    # root; probe both scopes.
    $pidCond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $script:MainPid)
    $nameCond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty, $title)
    $ctCond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::Window)
    $andPidTitle = New-Object System.Windows.Automation.AndCondition($pidCond, $nameCond)
    $andAll = New-Object System.Windows.Automation.AndCondition($andPidTitle, $ctCond)
    $hit = [System.Windows.Automation.AutomationElement]::RootElement.FindFirst(
        [System.Windows.Automation.TreeScope]::Children, $andAll)
    if ($null -ne $hit) { return $hit }
    if ($null -ne $script:MainWindow) {
        $hit = $script:MainWindow.FindFirst(
            [System.Windows.Automation.TreeScope]::Descendants, $andAll)
    }
    return $hit
}

# --------------------------------------------------------------- selectors ---
function Set-RuntimeId([System.Windows.Automation.AutomationElement]$el) {
    # .Current.RuntimeId is absent from pwsh's trimmed interop under strict
    # mode; query the RuntimeIdProperty directly, fall back to identity hash.
    try {
        $rid = $el.GetCurrentPropertyValue([System.Windows.Automation.AutomationElement]::RuntimeIdProperty)
        $script:LastRuntimeId = (($rid | ForEach-Object { [string]$_ }) -join ':')
    }
    catch {
        try { $script:LastRuntimeId = 'hash:' + $el.GetHashCode() } catch { $script:LastRuntimeId = '' }
    }
}
function Find-ByIdAndType([string]$id, [System.Windows.Automation.ControlType]$ct) {
    $idCond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty, $id)
    if ($null -eq $script:MainWindow) { Fail "no main window while resolving '$id'" }
    $raw = $null
    if ($null -ne $ct) {
        $ctCond = New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty, $ct)
        $and = New-Object System.Windows.Automation.AndCondition($idCond, $ctCond)
        $raw = $script:MainWindow.FindAll([System.Windows.Automation.TreeScope]::Descendants, $and)
    }
    else {
        $raw = $script:MainWindow.FindAll([System.Windows.Automation.TreeScope]::Descendants, $idCond)
    }
    $list = @()
    for ($i = 0; $i -lt $raw.Count; $i++) { $list += $raw.Item($i) }
    return ,$list
}
function Register-Resolution([string]$id, [bool]$present) {
    if ($script:SelectorStats.Contains($id)) {
        $script:SelectorStats[$id].resolvedCount = [int]$script:SelectorStats[$id].resolvedCount + 1
        $script:SelectorStats[$id].lastPresent = $present
    }
}
function Resolve-One([string]$id, [string]$typeName) {
    $ct = Get-ControlTypeByName $typeName
    $all = Find-ByIdAndType $id $ct
    if ($all.Count -eq 0) {
        $any = Find-ByIdAndType $id $null
        $diag = @()
        for ($i = 0; $i -lt $any.Count; $i++) { $diag += ('{0}/{1}' -f $any[$i].Current.ControlType.ProgrammaticName, $any[$i].Current.Name) }
        Register-Resolution $id $false
        Fail "selector '$id' ($typeName): 0 matches; elements with that AutomationId: [$($diag -join '; ')]"
    }
    if ($all.Count -gt 1) {
        Register-Resolution $id $false
        Fail "selector '$id' ($typeName): ambiguous, $($all.Count) matches (exactly one required)"
    }
    $el = $all[0]
    if (-not $el.Current.IsEnabled) {
        Register-Resolution $id $false
        Fail "selector '$id' ($typeName): single match found but NOT enabled"
    }
    Register-Resolution $id $true
    $script:LastSelector = $id
    Set-RuntimeId $el
    return $el
}
function Resolve-Optional([string]$id, [string]$typeName) {
    $ct = Get-ControlTypeByName $typeName
    $all = Find-ByIdAndType $id $ct
    if ($all.Count -eq 0) { Register-Resolution $id $false; $script:LastSelector = $id; return $null }
    if ($all.Count -gt 1) {
        Register-Resolution $id $false
        Fail "optional selector '$id' ($typeName): ambiguous, $($all.Count) matches"
    }
    Register-Resolution $id $true
    $script:LastSelector = $id
    Set-RuntimeId $all[0]
    return $all[0]
}
function Wait-IdResolvable([string]$id, [string]$typeName, [int]$timeoutMs) {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    while ($sw.Elapsed.TotalMilliseconds -lt $timeoutMs) {
        $ct = Get-ControlTypeByName $typeName
        $all = Find-ByIdAndType $id $ct
        if ($all.Count -ge 1) { return }
        Start-Sleep -Milliseconds $script:PollMs
    }
    Fail "timeout waiting for selector '$id' ($typeName) to become resolvable (${timeoutMs}ms)"
}
function Wait-True([scriptblock]$condition, [int]$timeoutMs, [string]$what) {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    while ($sw.Elapsed.TotalMilliseconds -lt $timeoutMs) {
        try { if ((& $condition)) { return } } catch { }
        Start-Sleep -Milliseconds $script:PollMs
    }
    Fail "timeout after ${timeoutMs}ms waiting for: $what"
}

# ------------------------------------------------------------------ sidebar ---
function Select-Sidebar([string]$title) {
    $liCond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::ListItem)
    $items = $script:MainWindow.FindAll([System.Windows.Automation.TreeScope]::Descendants, $liCond)
    $txtCond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::Text)
    $matched = @()
    for ($i = 0; $i -lt $items.Count; $i++) {
        $texts = $items[$i].FindAll([System.Windows.Automation.TreeScope]::Descendants, $txtCond)
        for ($t = 0; $t -lt $texts.Count; $t++) {
            if ($texts[$t].Current.Name -ceq $title) { $matched += $items[$i]; break }
        }
    }
    if ($matched.Count -eq 0) { Fail "sidebar item '$title': 0 ListItems matched by descendant text" }
    if ($matched.Count -gt 1) { Fail "sidebar item '${title}': ambiguous, $($matched.Count) ListItems matched" }
    $item = $matched[0]
    if (-not $item.Current.IsEnabled) { Fail "sidebar item '$title': matched ListItem is not enabled" }
    ($item.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern) -as [System.Windows.Automation.SelectionItemPattern]).Select()
    Start-Sleep -Milliseconds 900
    Scan-Dialogs "sidebar-select:$title"
}

# ------------------------------------------------------------------- combos ---
function Get-ComboItems([System.Windows.Automation.AutomationElement]$combo) {
    $liCond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::ListItem)
    $raw = $combo.FindAll([System.Windows.Automation.TreeScope]::Descendants, $liCond)
    $list = @()
    for ($i = 0; $i -lt $raw.Count; $i++) { $list += $raw.Item($i) }
    if ($list.Count -eq 0) {
        $allRaw = $combo.FindAll([System.Windows.Automation.TreeScope]::Descendants,
            [System.Windows.Automation.Condition]::TrueCondition)
        for ($i = 0; $i -lt $allRaw.Count; $i++) {
            $el = $allRaw.Item($i)
            if ([string]::IsNullOrEmpty($el.Current.Name)) { continue }
            $supported = $el.GetCurrentPropertyValue([System.Windows.Automation.SelectionItemPattern]::PatternProperty, $false)
            if ($supported) { $list += $el }
        }
    }
    return ,$list
}
function Select-ComboItem([string]$id, [string]$mode, [string]$arg) {
    $combo = Resolve-One $id 'ComboBox'
    $ecp = $combo.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern) -as [System.Windows.Automation.ExpandCollapsePattern]
    $ecp.Expand()
    Start-Sleep -Milliseconds 500
    $items = Get-ComboItems $combo
    if ($items.Count -eq 0) { Start-Sleep -Milliseconds 700; $items = Get-ComboItems $combo }
    if ($items.Count -eq 0) {
        try { $ecp.Collapse() } catch { }
        Fail "combo '$id': expanded but 0 ComboBoxItems realized"
    }
    $matched = @()
    for ($i = 0; $i -lt $items.Count; $i++) {
        $nm = $items[$i].Current.Name
        $hit = $false
        switch ($mode) {
            'exact'    { $hit = ($nm -ceq $arg) }
            'contains' { $hit = ($nm -like "*$arg*") }
            default    { Fail "Select-ComboItem: unknown mode '$mode'" }
        }
        if ($hit) { $matched += $items[$i] }
    }
    if ($matched.Count -eq 0) {
        $names = @(); for ($i = 0; $i -lt $items.Count; $i++) { $names += $items[$i].Current.Name }
        try { $ecp.Collapse() } catch { }
        Fail "combo '$id': no item matches mode=$mode arg='$arg'; items=[$($names -join ' | ')]"
    }
    if ($matched.Count -gt 1) {
        try { $ecp.Collapse() } catch { }
        Fail "combo '$id': $($matched.Count) items match mode=$mode arg='$arg' (exactly one required)"
    }
    ($matched[0].GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern) -as [System.Windows.Automation.SelectionItemPattern]).Select()
    Start-Sleep -Milliseconds 400
    try { $ecp.Collapse() } catch { }
    Start-Sleep -Milliseconds 300
    Scan-Dialogs "combo-select:$id"
}
function Get-ComboSelectionName([string]$id) {
    $combo = Resolve-One $id 'ComboBox'
    try {
        $sp = $combo.GetCurrentPattern([System.Windows.Automation.SelectionPattern]::Pattern) -as [System.Windows.Automation.SelectionPattern]
        $sel = @($sp.Current.GetSelection())
        if ($sel.Count -ge 1) { return $sel[0].Current.Name }
        return ''
    } catch {
        return $combo.Current.Name
    }
}

# ------------------------------------------------------------------- edits ---
function Set-TextBoxValue([string]$id, [string]$text) {
    $edit = Resolve-One $id 'Edit'
    $vp = $edit.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern) -as [System.Windows.Automation.ValuePattern]
    $vp.SetValue($text)
    Start-Sleep -Milliseconds 500
    Scan-Dialogs "textbox-set:$id"
    return $vp.Current.Value
}
function Get-TextBoxValue([string]$id) {
    $edit = Resolve-One $id 'Edit'
    $vp = $edit.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern) -as [System.Windows.Automation.ValuePattern]
    return $vp.Current.Value
}
function Invoke-Button([string]$id) {
    $btn = Resolve-One $id 'Button'
    ($btn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern) -as [System.Windows.Automation.InvokePattern]).Invoke()
}
function Get-TextName([string]$id) {
    $el = Resolve-One $id 'Text'
    return $el.Current.Name
}

# ------------------------------------------------------------- grid helpers ---
function Find-SelectableRowsContaining([string]$exactText) {
    # Any element whose direct SelectionItemPattern retrieval succeeds (the
    # phase-4-proven method; GetCurrentPropertyValue(PatternProperty) probing
    # proved unreliable under this runtime) and that has a descendant Text
    # named exactly $exactText.
    $txtCond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::Text)
    $all = $script:MainWindow.FindAll([System.Windows.Automation.TreeScope]::Descendants,
        [System.Windows.Automation.Condition]::TrueCondition)
    $rows = @()
    for ($i = 0; $i -lt $all.Count; $i++) {
        $el = $all.Item($i)
        try {
            if ($el.Current.IsEnabled -eq $false) { continue }
            # a TabItem hosting the grid content also exposes SelectionItemPattern
            # and contains every cell text — it is NOT a circuit row
            if ($el.Current.ControlType -eq [System.Windows.Automation.ControlType]::TabItem) { continue }
            $sip = $el.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern) -as [System.Windows.Automation.SelectionItemPattern]
            if ($null -eq $sip) { continue }
            $texts = $el.FindAll([System.Windows.Automation.TreeScope]::Descendants, $txtCond)
            for ($t = 0; $t -lt $texts.Count; $t++) {
                if ($texts[$t].Current.Name -ceq $exactText) { $rows += $el; break }
            }
        } catch { continue }
    }
    # Keep only INNERMOST matches: a DataGridRow and one of its ancestors can
    # both expose SelectionItemPattern while containing the same cell text.
    $innermost = @()
    foreach ($c in $rows) {
        $contained = $false
        foreach ($o in $rows) {
            if ([object]::ReferenceEquals($c, $o)) { continue }
            try {
                $walker = [System.Windows.Automation.TreeWalker]::RawViewWalker
                $anc = $walker.GetParent($c)
                while ($null -ne $anc) {
                    if ($anc.GetHashCode() -eq $o.GetHashCode()) { $contained = $true; break }
                    $anc = $walker.GetParent($anc)
                }
            } catch { }
            if ($contained) { break }
        }
        if (-not $contained) { $innermost += $c }
    }
    return ,$innermost
}
function Get-RowCellTexts($row) {
    $txtCond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::Text)
    $texts = $row.FindAll([System.Windows.Automation.TreeScope]::Descendants, $txtCond)
    $names = @()
    for ($t = 0; $t -lt $texts.Count; $t++) { $names += $texts[$t].Current.Name }
    return ,$names
}
function Get-ResultsCardValue([string]$label) {
    # On Результаты view: locate the label TextBlock ('Длина труб', ...), then
    # return the NEXT Text element in document order (the sibling value cell
    # '88.0 м' / '20480 Вт' / '1.17 м³/ч').
    $nameCond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty, $label)
    $txtTypeCond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::Text)
    $and = New-Object System.Windows.Automation.AndCondition($nameCond, $txtTypeCond)
    $labels = $script:MainWindow.FindAll([System.Windows.Automation.TreeScope]::Descendants, $and)
    if ($labels.Count -eq 0) { Fail "results card label '$label': 0 matches" }
    if ($labels.Count -gt 1) { Fail "results card label '${label}': ambiguous, $($labels.Count) matches" }
    # NOTE: pwsh's trimmed UIAutomationTypes does not expose .Current.RuntimeId
    # under Set-StrictMode; GetHashCode() is identity-based for AutomationElement.
    $labelHash = $labels.Item(0).GetHashCode()
    $texts = $script:MainWindow.FindAll([System.Windows.Automation.TreeScope]::Descendants, $txtTypeCond)
    for ($t = 0; $t -lt $texts.Count - 1; $t++) {
        if ($texts.Item($t).GetHashCode() -eq $labelHash) {
            $val = $texts.Item($t + 1).Current.Name
            if ([string]::IsNullOrWhiteSpace($val)) { Fail "results card '$label': next text element is empty" }
            return $val
        }
    }
    Fail "results card '$label': no following text element found"
}

# ------------------------------------------------------------ keyboard edit ---
function Ensure-Foreground {
    for ($try = 0; $try -lt 10; $try++) {
        $fg = [Win32Uaq5.NativeMethods]::GetForegroundWindow()
        if ($fg -ne [IntPtr]::Zero) {
            $root = [Win32Uaq5.NativeMethods]::GetAncestor($fg, 2) # GA_ROOT
            if ($root -eq $script:MainHandle -or $fg -eq $script:MainHandle) { return $true }
        }
        [void][Win32Uaq5.NativeMethods]::SetForegroundWindow($script:MainHandle)
        Start-Sleep -Milliseconds 250
    }
    return $false
}
function Send-Key([byte]$vk, [int]$delayMs = 120) {
    [void][Win32Uaq5.NativeMethods]::keybd_event($vk, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 40
    [void][Win32Uaq5.NativeMethods]::keybd_event($vk, 0, 2, [UIntPtr]::Zero) # KEYEVENTF_KEYUP
    Start-Sleep -Milliseconds $delayMs
}
function Send-Chord([byte[]]$vks, [int]$delayMs = 200) {
    foreach ($vk in $vks) {
        [void][Win32Uaq5.NativeMethods]::keybd_event($vk, 0, 0, [UIntPtr]::Zero)
        Start-Sleep -Milliseconds 40
    }
    foreach ($vk in ($vks | Sort-Object -Descending)) {
        [void][Win32Uaq5.NativeMethods]::keybd_event($vk, 0, 2, [UIntPtr]::Zero) # KEYEVENTF_KEYUP
        Start-Sleep -Milliseconds 40
    }
    Start-Sleep -Milliseconds $delayMs
}
function Send-TextKeys([string]$text) {
    foreach ($ch in $text.ToCharArray()) {
        $vk = [byte][System.Text.Encoding]::ASCII.GetBytes([string]$ch)[0]
        if ($vk -lt 0x30 -or $vk -gt 0x39) { Fail "Send-TextKeys supports digits only, got '$ch'" }
        Send-Key $vk 150
    }
}

# -------------------------------------------------------------- screenshots ---
function Save-Screenshot([string]$fileBase) {
    if ($null -eq $script:MainWindow) { Fail "cannot capture '$fileBase': no main window" }
    $rect = $script:MainWindow.Current.BoundingRectangle
    $vs = [System.Windows.Forms.SystemInformation]::VirtualScreen
    $ix = [math]::Max($rect.Left, $vs.Left)
    $iy = [math]::Max($rect.Top, $vs.Top)
    $ir = [math]::Min($rect.Right, $vs.Right)
    $ib = [math]::Min($rect.Bottom, $vs.Bottom)
    $w = [int]($ir - $ix); $h = [int]($ib - $iy)
    if ($w -le 0 -or $h -le 0) { Fail "screenshot '$fileBase': window rect outside virtual screen" }
    $pngPath = Join-Path $script:ShotDir "$fileBase.png"
    $bmp = New-Object System.Drawing.Bitmap($w, $h)
    try {
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        try { $g.CopyFromScreen([int]$ix, [int]$iy, 0, 0, (New-Object System.Drawing.Size($w, $h))) }
        finally { $g.Dispose() }
        $bmp.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally { $bmp.Dispose() }
    $fi = Get-Item -LiteralPath $pngPath
    if ($fi.Length -le 1000) { Fail "screenshot '$fileBase': PNG suspiciously small ($($fi.Length) bytes)" }
    $bytes = $fi.Length
    $dims = $null
    try {
        $img = [System.Drawing.Image]::FromFile($pngPath)
        $dims = "$($img.Width)x$($img.Height)"
        $img.Dispose()
    } catch { $dims = 'unknown' }
    $script:Shots.Add([ordered]@{
        name = $fileBase; file = (Get-RelPath $pngPath); bytes = $bytes
        dimensions = $dims; sha256 = (Get-Sha256File $pngPath)
    })
    Add-ArtifactRecord $pngPath "screenshot $fileBase"
    return $pngPath
}

# ---------------------------------------------------------- process lifecycle ---
function Invoke-Launch([string]$tag, [string]$projectPath, [switch]$ExpectErrorDialog) {
    $shaBefore = Test-ExeSha "before-launch-$tag"
    $stdoutLog = Join-Path $script:OutDir "run-$tag-stdout.log"
    $stderrLog = Join-Path $script:OutDir "run-$tag-stderr.log"
    foreach ($l in @($stdoutLog, $stderrLog)) {
        if (Test-Path -LiteralPath $l) { Remove-Item -LiteralPath $l -Force }
    }
    $startedUtc = [DateTime]::UtcNow.ToString('o')
    $proc = Start-Process -FilePath $script:ExePath -ArgumentList @('"' + $projectPath + '"') `
        -PassThru -WorkingDirectory (Split-Path -Parent $script:ExePath) `
        -RedirectStandardOutput $stdoutLog -RedirectStandardError $stderrLog
    $script:ActiveProc = $proc
    if ($null -eq $proc -or $proc.HasExited) { Fail "launch '$tag': process exited immediately" }

    $win = $null
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    while ($sw.Elapsed.TotalMilliseconds -lt $script:WindowWaitMs) {
        if ($proc.HasExited) { Fail "launch '$tag': process crashed during startup (exit $($proc.ExitCode))" }
        $pidCond = New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
        $ctCond = New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            [System.Windows.Automation.ControlType]::Window)
        $and = New-Object System.Windows.Automation.AndCondition($pidCond, $ctCond)
        $w = [System.Windows.Automation.AutomationElement]::RootElement.FindFirst([System.Windows.Automation.TreeScope]::Children, $and)
        if ($null -ne $w -and -not [string]::IsNullOrWhiteSpace($w.Current.Name)) { $win = $w; break }
        Start-Sleep -Milliseconds $script:PollMs
    }
    if ($null -eq $win) { Fail "launch '$tag': main window did not appear within $($script:WindowWaitMs)ms" }
    $script:MainWindow = $win
    $script:MainPid = $proc.Id
    $script:MainHandle = [IntPtr]$win.Current.NativeWindowHandle
    $title = $win.Current.Name
    if (-not $ExpectErrorDialog) {
        Add-Assertion "launch '$tag': window title carries app suffix" "*$($script:MainTitleSuffix)" $title ($title -like "*$($script:MainTitleSuffix)*")
        Scan-Dialogs "post-launch-$tag"
    }
    return [ordered]@{
        tag = $tag; project = (Get-RelPath $projectPath); pid = $proc.Id
        exeShaBefore = $shaBefore; startedUtc = $startedUtc
        proc = $proc; window = $win
        stdoutLog = $stdoutLog; stderrLog = $stderrLog
        stdoutLogRel = (Get-RelPath $stdoutLog); stderrLogRel = (Get-RelPath $stderrLog)
    }
}
function Wait-MainWindowTitleContains([string]$fragment, [int]$timeoutMs, [string]$what) {
    Wait-True -what $what -timeoutMs $timeoutMs -condition {
        $t = $script:MainWindow.Current.Name
        return ($t -like "*$fragment*")
    }
    Scan-Dialogs "title-wait:$fragment"
}
function Close-App($run) {
    $tag = $run.tag
    Scan-Dialogs "pre-close-$tag"
    Assert-NoWindowByTitle $script:DialogTitleClose "pre-close-$tag"

    $h = [IntPtr]$run.window.Current.NativeWindowHandle
    [void][Win32Uaq5.NativeMethods]::PostMessage($h, 0x0010, [IntPtr]::Zero, [IntPtr]::Zero)

    $proc = $run.proc
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    while ($sw.Elapsed.TotalMilliseconds -lt $script:ExitWaitMs) {
        if ($proc.HasExited) { break }
        try {
            $pidCond = New-Object System.Windows.Automation.PropertyCondition(
                [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $script:MainPid)
            $ctCond = New-Object System.Windows.Automation.PropertyCondition(
                [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
                [System.Windows.Automation.ControlType]::Window)
            $and = New-Object System.Windows.Automation.AndCondition($pidCond, $ctCond)
            $ws = [System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children, $and)
            for ($i = 0; $i -lt $ws.Count; $i++) {
                if ($ws[$i].Current.Name -ceq $script:DialogTitleClose) {
                    Fail "closing dialog '$($script:DialogTitleClose)' appeared on close of '$tag' (dirty-marker persistence)"
                }
            }
        } catch { }
        Start-Sleep -Milliseconds 500
    }
    if (-not $proc.HasExited) {
        try { $proc.Kill() } catch { }
        Fail "close '$tag': process did not exit within $($script:ExitWaitMs)ms after WM_CLOSE"
    }
    Start-Sleep -Milliseconds 800
    $exitCode = $proc.ExitCode
    Add-Assertion "close '$tag': clean exit code" '0' ([string]$exitCode) ($exitCode -eq 0)

    $stderrText = Read-FileTextSafe $run.stderrLog
    $crashHit = $null
    foreach ($pat in $script:StderrCrashPatterns) {
        if ($stderrText -match [regex]::Escape($pat)) { $crashHit = $pat; break }
    }
    Add-Assertion "close '$tag': stderr free of crash patterns" 'no match' $(if ($null -ne $crashHit) { $crashHit } else { 'clean' }) ($null -eq $crashHit)

    $shaAfter = Test-ExeSha "after-exit-$tag"
    $record = [ordered]@{
        tag = $tag; project = $run.project; pid = $run.pid
        exitCode = $exitCode
        exeShaBefore = $run.exeShaBefore; exeShaAfter = $shaAfter
        stdoutLog = $run.stdoutLogRel; stdoutSha256 = (Get-Sha256File $run.stdoutLog)
        stderrLog = $run.stderrLogRel; stderrSha256 = (Get-Sha256File $run.stderrLog)
        startedUtc = $run.startedUtc; exitUtc = [DateTime]::UtcNow.ToString('o')
    }
    $script:ProcRuns.Add($record)
    $script:ActiveProc = $null
    $script:MainWindow = $null
    $script:MainPid = -1
    $script:MainHandle = [IntPtr]::Zero
    return $record
}

# ------------------------------------------------------------------- menus ---
function Find-MenuItemsByName([string]$name, [System.Windows.Automation.AutomationElement]$scope) {
    $cond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty, $name)
    $raw = $scope.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond)
    $list = @()
    for ($i = 0; $i -lt $raw.Count; $i++) {
        $el = $raw.Item($i)
        if ($el.Current.ControlType -eq [System.Windows.Automation.ControlType]::MenuItem) { $list += $el }
    }
    return ,$list
}
function Invoke-TopMenuItem([string]$topName, [string]$itemName, [string]$context) {
    # DEVIATION (carried from phase-4 V9 probes): injected Ctrl+S/Ctrl+N chords
    # never reach the app's Window-level KeyDown handler in this environment.
    # The SAME bound commands are driven through the visible «Файл» menu via
    # UIA Invoke/Selection patterns — plan observables unchanged.
    if ($null -eq $script:MainWindow) { Fail "menu '$context': no main window" }
    $tops = Find-MenuItemsByName $topName $script:MainWindow
    if ($tops.Count -eq 0) { Fail "menu '$context': top item '$topName' not found" }
    if ($tops.Count -gt 1) { Fail "menu '$context': ambiguous top item '$topName' ($($tops.Count) matches)" }
    $top = $tops[0]
    $opened = $false
    try {
        $ecp = $top.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern) -as [System.Windows.Automation.ExpandCollapsePattern]
        $ecp.Expand()
        $opened = $true
    } catch {
        try {
            ($top.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern) -as [System.Windows.Automation.SelectionItemPattern]).Select()
            $opened = $true
        } catch { }
    }
    Start-Sleep -Milliseconds 700
    if (-not $opened) { Fail "menu '$context': cannot expand '$topName'" }
    Scan-Dialogs "menu-open:$context"
    $leaves = Find-MenuItemsByName $itemName $top
    if ($leaves.Count -eq 0) { $leaves = Find-MenuItemsByName $itemName $script:MainWindow }
    if ($leaves.Count -eq 0) { Fail "menu '$context': item '$itemName' not found after expand" }
    if ($leaves.Count -gt 1) { Fail "menu '$context': ambiguous item '$itemName' ($($leaves.Count) matches)" }
    $leaf = $leaves[0]
    $invoked = $false
    try {
        ($leaf.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern) -as [System.Windows.Automation.InvokePattern]).Invoke()
        $invoked = $true
    } catch { }
    if (-not $invoked) {
        try {
            ($leaf.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern) -as [System.Windows.Automation.SelectionItemPattern]).Select()
            $invoked = $true
        } catch { }
    }
    if (-not $invoked) { Fail "menu '$context': cannot invoke '$itemName' (no Invoke/Selection pattern support)" }
    Start-Sleep -Milliseconds 1200
    Scan-Dialogs "menu-invoke:$context"
}

# ============================================================ shared oracles ===
# Fixture-derived expectations (see prepare-ui-fixtures.ps1 + v1-sample.smc).
# Display strings use the dot-decimal UI culture observed by probe.
$script:A_StoredRowLength  = '80'
$script:A_StoredRowPower   = '20480'
$script:A_StoredRowFlow    = '1172.0'
$script:A_StoredCardLength = '88.0 м'
$script:A_StoredCardPower  = '20480 Вт'
$script:A_StoredCardFlow   = '1.17 м³/ч'
# Recalculated oracles (computed below from glycol_data.json + formulas):
$script:P_A80  = Get-CircuitPower 80 8 25 5 10 256 5
$script:P_A120 = Get-CircuitPower 120 8 25 5 10 256 5
$propsE30 = Get-GlycolProps 'ethylene' 30 42.5
$propsP30 = Get-GlycolProps 'propylene' 30 42.5
$propsP50 = Get-GlycolProps 'propylene' 50 42.5
$flowE30P80  = Get-FlowRateLh $script:P_A80 15 $propsE30.rho $propsE30.cp
$flowP30P80  = Get-FlowRateLh $script:P_A80 15 $propsP30.rho $propsP30.cp
$flowP50P80  = Get-FlowRateLh $script:P_A80 15 $propsP50.rho $propsP50.cp
$flowE30P120 = Get-FlowRateLh $script:P_A120 15 $propsE30.rho $propsE30.cp
$script:P_A80_SupplySpacing12_Heat10 = Get-CircuitPower 80 8 25 12 10 256 5
$script:P_A80_SupplySpacing12_Heat15 = Get-CircuitPower 80 8 25 12 15 256 5
$script:CalcFlowEthylene30 = Format-UiNumber $flowE30P80 1    # after ANY recalc @ e30
$script:CalcFlowPropylene30 = Format-UiNumber $flowP30P80 1
$script:CalcFlowPropylene50 = Format-UiNumber $flowP50P80 1
$script:CalcPowerRecalc80  = Format-UiNumber ([math]::Round($script:P_A80, 0)) 0
$script:CalcPowerLen120    = Format-UiNumber ([math]::Round($script:P_A120, 0)) 0
$script:CalcPowerSpacing12Heat10 = Format-UiNumber ([math]::Round($script:P_A80_SupplySpacing12_Heat10, 0)) 0
$script:CalcPowerSpacing12Heat15 = Format-UiNumber ([math]::Round($script:P_A80_SupplySpacing12_Heat15, 0)) 0
$script:CalcFlowLen120     = Format-UiNumber $flowE30P120 1
$script:CardPowerLen120    = '{0} Вт' -f (Format-UiNumber ([math]::Round($script:P_A120, 0)) 0)
$script:CardFlowLen120     = '{0} м³/ч' -f (Format-UiNumber ($flowE30P120 / 1000.0) 2)

# ================================================================ MAIN FLOW ===
function Invoke-MainFlow {
    # ---------------------------------------------------------- S1 fixtures ---
    Start-Step 1 'S1 fixtures: manifest + three .smc SHA-256 + frozen exe SHA pre-run'
    $fixturesDir = Split-Path -Parent $script:PathA
    $manifestLocal = Join-Path $fixturesDir 'fixture-manifest.json'
    if (-not (Test-Path -LiteralPath $manifestLocal -PathType Leaf)) {
        Fail "fixture manifest not found next to ProjectA: $manifestLocal"
    }
    $fx = Get-Content -LiteralPath $manifestLocal -Raw | ConvertFrom-Json
    $fxChecks = @()
    foreach ($entry in @($fx.outputs)) {
        $p = Join-Path $fixturesDir $entry.relativePath
        if (-not (Test-Path -LiteralPath $p -PathType Leaf)) { Fail "fixture output missing: $p" }
        $sha = Get-Sha256File $p
        $ok = ($sha -eq ([string]$entry.sha256).ToUpperInvariant())
        Add-Assertion "fixture $($entry.name) SHA matches manifest" ([string]$entry.sha256) $sha $ok
        $fxChecks += [ordered]@{ name = $entry.name; path = (Get-RelPath $p); sha256 = $sha; manifestMatch = $ok }
    }
    if ((Split-Path -Leaf $script:PathA) -ne 'project-a.smc') { Fail 'ProjectA must be the task-owned project-a.smc fixture copy' }
    if ((Split-Path -Leaf $script:PathB) -ne 'project-b.smc') { Fail 'ProjectB must be the task-owned project-b.smc fixture copy' }
    if ((Split-Path -Leaf $script:PathInvalid) -ne 'unknown-pipe.smc') { Fail 'InvalidProject must be the task-owned unknown-pipe.smc fixture copy' }
    $leftover = Get-Process -Name 'SnowMeltingCalculator' -ErrorAction SilentlyContinue
    if ($null -ne $leftover) {
        $leftover | Stop-Process -Force -ErrorAction SilentlyContinue
        Start-Sleep -Milliseconds 800
        Note-Deviation "killed $($leftover.Count) leftover SnowMeltingCalculator.exe process(es) before the run"
    }
    $script:FixtureInfo = [ordered]@{
        manifest = (Get-RelPath $manifestLocal); outputs = $fxChecks
        executableShaPreRun = $script:ExeShaPreRun
    }
    Complete-Step

    # ------------------------------------------------------ S2 launch A ---
    Start-Step 2 'S2 launch project-a as first .smc argument; clean loaded title'
    $run1 = Invoke-Launch 'a-load-edit-save' $script:PathA
    Wait-MainWindowTitleContains 'project-a.smc' 20000 'Project A load reflected in window title (filename appears)'
    $t2 = $script:MainWindow.Current.Name
    Assert-ExactText $t2 ("project-a.smc — $($script:MainTitleSuffix)") 'S2: clean loaded title (no dirty marker)'
    Complete-Step

    # ------------------------------------------- S3 baseline hydraulics ---
    Start-Step 3 'S3 navigate Гидравлический расчёт; outputs match v1-sample.smc stored fixture math; registry'
    Select-Sidebar 'Гидравлический расчёт'
    Wait-IdResolvable 'HydraulicsPipeSpacing' 'Text' 15000

    $glySel = Get-ComboSelectionName 'HydraulicsGlycolType'
    Assert-ExactText $glySel 'Этиленгликоль' 'S3: glycol type combo == Этиленгликоль (fixture ethylene)' 'HydraulicsGlycolType'
    $concVal = Get-TextBoxValue 'HydraulicsGlycolConcentration'
    Assert-NumberNear (Get-FirstNameNumber $concVal) 30.0 1 'S3: glycol concentration == 30 (fixture)' 'HydraulicsGlycolConcentration'
    $heatVal = Get-TextBoxValue 'HydraulicsSupplyHeatPercent'
    Assert-NumberNear (Get-FirstNameNumber $heatVal) 10.0 1 'S3: supply heat percent == 10 (fixture)' 'HydraulicsSupplyHeatPercent'
    $calcBtn = Resolve-One 'HydraulicsCalculateButton' 'Button'
    Add-Assertion 'S3: calculate button resolved/enabled' 'enabled Button' ("enabled=" + $calcBtn.Current.IsEnabled) ($calcBtn.Current.IsEnabled) 'HydraulicsCalculateButton'

    $spTxt = Get-TextName 'HydraulicsPipeSpacing'
    Assert-NumberNear (Get-FirstNameNumber $spTxt) 25.0 0 'S3: HydraulicsPipeSpacing == 25 cm (fixture pipeSpacing 250 mm / 10)' 'HydraulicsPipeSpacing'
    $supTxt = Get-TextName 'HydraulicsSupplyTemperature'
    Assert-NumberNear (Get-FirstNameNumber $supTxt) 50.0 1 'S3: HydraulicsSupplyTemperature == 50.0 (fixture thermal supply)' 'HydraulicsSupplyTemperature'
    $retTxt = Get-TextName 'HydraulicsReturnTemperature'
    Assert-NumberNear (Get-FirstNameNumber $retTxt) 35.0 1 'S3: HydraulicsReturnTemperature == 35.0 (fixture thermal result return)' 'HydraulicsReturnTemperature'

    $rows80 = Find-SelectableRowsContaining $script:A_StoredRowLength
    if ($rows80.Count -ne 1) { Fail "S3: expected exactly 1 selectable row containing length '80', got $($rows80.Count)" }
    $rowCells = Get-RowCellTexts $rows80[0]
    Add-Assertion 'S3: first circuit row length cell == 80 (stored fixture)' '80' ($rowCells -join ' | ') (($rowCells -ccontains '80')) 'DataGrid row[Длина]'
    Add-Assertion 'S3: first circuit row power cell == 20480 (stored fixture)' $script:A_StoredRowPower ($rowCells -join ' | ') (($rowCells -ccontains $script:A_StoredRowPower)) 'DataGrid row[Мощность]'
    Add-Assertion 'S3: first circuit row flow cell == 1172,0 (stored fixture)' $script:A_StoredRowFlow ($rowCells -join ' | ') (($rowCells -ccontains $script:A_StoredRowFlow)) 'DataGrid row[Расход л/ч]'

    Select-Sidebar 'Результаты'
    Wait-True -what 'results hydraulic summary card visible' -timeoutMs 15000 -condition {
        try { $null -ne $script:MainWindow.FindFirst([System.Windows.Automation.TreeScope]::Descendants,
            (New-Object System.Windows.Automation.PropertyCondition(
                [System.Windows.Automation.AutomationElement]::NameProperty, 'Длина труб'))) } catch { $false }
    }
    $cardLen = Get-ResultsCardValue 'Длина труб'
    Assert-ExactText $cardLen $script:A_StoredCardLength 'S3: summary card Длина труб == 88,0 м (stored fixture)' 'Results card[Длина труб]'
    $cardPow = Get-ResultsCardValue 'Мощность'
    Assert-ExactText $cardPow $script:A_StoredCardPower 'S3: summary card Мощность == 20480 Вт (stored fixture)' 'Results card[Мощность]'
    $cardFlow = Get-ResultsCardValue 'Расход'
    Assert-ExactText $cardFlow $script:A_StoredCardFlow 'S3: summary card Расход == 1,17 м³/ч (stored fixture)' 'Results card[Расход]'

    Note-Deviation ('Load restores the saved hydraulics snapshot verbatim (canonical Restore(ProjectLoad) -> adapter mirror); the stored fixture values (20480 W / 1172.0 л/ч) are therefore the load-time fixture math. A user-initiated recalculation recomputes from formulas: P=(L/(100/spc)+Lzul/(100/spcZul)*h%)*(qUp+qDown)=5230.44 W at fixture inputs — asserted in S4/S5.')
    [void](Save-Screenshot '01-launch-project-a-hydraulics-card')
    Complete-Step

    # --------------------------------------------------- S4 glycol edits ---
    Start-Step 4 'S4 glycol edits trigger auto-recalculation; outputs follow fixture math; validation branch'
    Select-Sidebar 'Гидравлический расчёт'
    Wait-IdResolvable 'HydraulicsGlycolType' 'ComboBox' 15000

    Select-ComboItem 'HydraulicsGlycolType' 'exact' 'Пропиленгликоль'
    Wait-True -what 'auto-recalc after glycol type change (grid shows propylene-30 flow)' -timeoutMs 20000 -condition {
        $rows = Find-SelectableRowsContaining $script:CalcFlowPropylene30
        return ($rows.Count -ge 1)
    }
    $rowsP30 = Find-SelectableRowsContaining $script:CalcFlowPropylene30
    Add-Assertion ('S4a: type→Пропиленгликоль auto-recalc: row flow cell == ' + $script:CalcFlowPropylene30 + ' (computed from glycol_data.json bilinear interp)') `
        $script:CalcFlowPropylene30 ($rowsP30.Count.ToString() + ' matching rows') ($rowsP30.Count -ge 1) 'DataGrid row[Расход л/ч]'
    $rowsOld = Find-SelectableRowsContaining $script:A_StoredRowFlow
    Add-Assertion ('S4a: stored-file flow 1172.0 no longer displayed after recalc') '0 matching rows' ([string]$rowsOld.Count) ($rowsOld.Count -eq 0) 'DataGrid row[Расход л/ч]'

    [void](Set-TextBoxValue 'HydraulicsGlycolConcentration' '50')
    Wait-True -what 'auto-recalc after concentration 50 (propylene-50 flow)' -timeoutMs 20000 -condition {
        $rows = Find-SelectableRowsContaining $script:CalcFlowPropylene50
        return ($rows.Count -ge 1)
    }
    Add-Assertion ('S4b: concentration→50 auto-recalc: row flow cell == ' + $script:CalcFlowPropylene50) `
        $script:CalcFlowPropylene50 'present' $true 'HydraulicsGlycolConcentration'

    [void](Set-TextBoxValue 'HydraulicsGlycolConcentration' '95')
    Wait-True -what 'validation message appears for concentration 95' -timeoutMs 20000 -condition {
        $el = Resolve-Optional 'HydraulicsValidationMessage' 'Text'
        return ($null -ne $el)
    }
    $vm = (Resolve-One 'HydraulicsValidationMessage' 'Text').Current.Name
    Assert-ContainsText $vm 'Концентрация должна быть 0% (вода) или в диапазоне 10-90%, получено: 95%' `
        'S4c: out-of-range concentration → EXACT validation message core' 'HydraulicsValidationMessage'
    [void](Save-Screenshot '03-validation-message')

    [void](Set-TextBoxValue 'HydraulicsGlycolConcentration' '30')
    Wait-True -what 'validation message cleared after returning to 30' -timeoutMs 20000 -condition {
        $el = Resolve-Optional 'HydraulicsValidationMessage' 'Text'
        return ($null -eq $el)
    }
    $vmAfter = Resolve-Optional 'HydraulicsValidationMessage' 'Text'
    Add-Assertion 'S4d: validation message absent again (concentration 30)' 'absent' $(if ($null -eq $vmAfter) { 'absent' } else { $vmAfter.Current.Name }) ($null -eq $vmAfter) 'HydraulicsValidationMessage'
    Wait-True -what 'flow reproducibility: propylene-30 flow reappears deterministically' -timeoutMs 20000 -condition {
        $rows = Find-SelectableRowsContaining $script:CalcFlowPropylene30
        return ($rows.Count -ge 1)
    }
    Add-Assertion ('S4d: reverting inputs reproduces identical output ' + $script:CalcFlowPropylene30 + ' (deterministic fixture math)') `
        $script:CalcFlowPropylene30 'present' $true 'DataGrid row[Расход л/ч]'

    Select-ComboItem 'HydraulicsGlycolType' 'exact' 'Этиленгликоль'
    Wait-True -what 'auto-recalc back to ethylene-30 flow' -timeoutMs 20000 -condition {
        $rows = Find-SelectableRowsContaining $script:CalcFlowEthylene30
        return ($rows.Count -ge 1)
    }
    Add-Assertion ('S4e: type→Этиленгликоль auto-recalc: row flow cell == ' + $script:CalcFlowEthylene30) `
        $script:CalcFlowEthylene30 'present' $true 'DataGrid row[Расход л/ч]'

    Wait-IdResolvable 'HydraulicsSupplySpacing' 'Edit' 15000
    $supplySpacingOriginal = Get-TextBoxValue 'HydraulicsSupplySpacing'
    Assert-NumberNear (Get-FirstNameNumber $supplySpacingOriginal) 5.0 0 'S4f: supply spacing baseline == 5 (fixture)' 'HydraulicsSupplySpacing'

    [void](Set-TextBoxValue 'HydraulicsSupplySpacing' '12')
    $supplySpacing12 = Get-TextBoxValue 'HydraulicsSupplySpacing'
    Assert-NumberNear (Get-FirstNameNumber $supplySpacing12) 12.0 0 'S4f: supply spacing edits to 12' 'HydraulicsSupplySpacing'
    Wait-True -what 'row reflects supply spacing 12' -timeoutMs 20000 -condition {
        $rows = Find-SelectableRowsContaining $script:A_StoredRowLength
        if ($rows.Count -ne 1) { return $false }
        return ((Get-RowCellTexts $rows[0]) -ccontains '12')
    }
    $rowsSpacing12 = Find-SelectableRowsContaining $script:A_StoredRowLength
    if ($rowsSpacing12.Count -ne 1) { Fail "S4f: expected exactly 1 row after spacing edit, got $($rowsSpacing12.Count)" }
    $cellsSpacing12 = Get-RowCellTexts $rowsSpacing12[0]
    Add-Assertion 'S4f: first circuit row reflects supply spacing 12' '12' ($cellsSpacing12 -join ' | ') (($cellsSpacing12 -ccontains '12')) 'DataGrid row[Подводка]'
    Invoke-Button 'HydraulicsCalculateButton'
    Wait-True -what 'recalculated power after supply spacing 12' -timeoutMs 30000 -condition {
        $rows = Find-SelectableRowsContaining $script:CalcPowerSpacing12Heat10
        return ($rows.Count -ge 1)
    }
    Add-Assertion ('S4f: supply spacing 12 recalculates power == ' + $script:CalcPowerSpacing12Heat10 + ' W') `
        $script:CalcPowerSpacing12Heat10 'present' $true 'HydraulicsCalculateButton'

    [void](Set-TextBoxValue 'HydraulicsSupplyHeatPercent' '15')
    $supplyHeat15 = Get-TextBoxValue 'HydraulicsSupplyHeatPercent'
    Assert-NumberNear (Get-FirstNameNumber $supplyHeat15) 15.0 0 'S4f: supply heat edits to 15' 'HydraulicsSupplyHeatPercent'
    Wait-True -what 'row reflects supply heat 15' -timeoutMs 20000 -condition {
        $rows = Find-SelectableRowsContaining $script:A_StoredRowLength
        if ($rows.Count -ne 1) { return $false }
        return ((Get-RowCellTexts $rows[0]) -ccontains '15')
    }
    $rowsHeat15 = Find-SelectableRowsContaining $script:A_StoredRowLength
    if ($rowsHeat15.Count -ne 1) { Fail "S4f: expected exactly 1 row after heat edit, got $($rowsHeat15.Count)" }
    $cellsHeat15 = Get-RowCellTexts $rowsHeat15[0]
    Add-Assertion 'S4f: first circuit row reflects supply heat 15' '15' ($cellsHeat15 -join ' | ') (($cellsHeat15 -ccontains '15')) 'DataGrid row[Потери]'
    Invoke-Button 'HydraulicsCalculateButton'
    Wait-True -what 'recalculated power after supply spacing 12 and heat 15' -timeoutMs 30000 -condition {
        $rows = Find-SelectableRowsContaining $script:CalcPowerSpacing12Heat15
        return ($rows.Count -ge 1)
    }
    Add-Assertion ('S4f: supply spacing 12 + heat 15 recalculates power == ' + $script:CalcPowerSpacing12Heat15 + ' W') `
        $script:CalcPowerSpacing12Heat15 'present' $true 'HydraulicsCalculateButton'

    [void](Set-TextBoxValue 'HydraulicsSupplySpacing' '5')
    [void](Set-TextBoxValue 'HydraulicsSupplyHeatPercent' '10')
    Invoke-Button 'HydraulicsCalculateButton'
    Wait-True -what 'row restored to fixture power after reverting spacing/heat' -timeoutMs 30000 -condition {
        $rows = Find-SelectableRowsContaining $script:CalcPowerRecalc80
        return ($rows.Count -ge 1)
    }
    Add-Assertion 'S4f: reverted spacing/heat restores fixture power row' $script:CalcPowerRecalc80 'present' $true 'DataGrid row[Мощность]'

    $dirtyT = $script:MainWindow.Current.Name
    Add-Assertion 'S4: edits mark project dirty (title carries *)' '*project-a.smc — …' $dirtyT ($dirtyT.StartsWith('*')) 
    Note-Deviation ('Recalculation oracles are computed by the harness from data/glycol_data.json replicating GlycolDataService bilinear interpolation (lines 520-600) and CircuitsCalculator power/flow formulas (lines 20-60): P(80m)=(80/(100/25)+8/(100/5)*0.10)*(256+5)=5230.44 W; flow(e30@42.5°C,dT15)=258.4 л/ч; flow(p30)=297,9; flow(p50)=308,9. The saved-file value 1172.0 corresponds to the legacy-characterized fixture result and is replaced on any recalculation.')
    [void](Save-Screenshot '02-glycol-edits-recalc')
    Complete-Step

    # --------------------------------------------- S5 circuit length edit ---
    Start-Step 5 'S5 keyboard-edit first circuit length 80→120; Рассчитать; summary card updates'
    # --------------------------------- focus the grid, then keyboard-edit ---
    # Deterministic route (probe-verified): select the row, UIA-Invoke the
    # Длина cell (the app's own SingleClickEdit behavior enters edit mode),
    # set the new value through the editor's UIA ValuePattern, commit ENTER.
    $rowsStart = Find-SelectableRowsContaining $script:A_StoredRowLength
    if ($rowsStart.Count -ne 1) { Fail "S5: expected exactly 1 row containing '${script:A_StoredRowLength}', got $($rowsStart.Count)" }
    $row = $rowsStart[0]
    if (-not (Ensure-Foreground)) { Fail 'S5: could not bring main window to foreground for keyboard editing' }
    ($row.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern) -as [System.Windows.Automation.SelectionItemPattern]).Select()
    Start-Sleep -Milliseconds 500

    $walker = [System.Windows.Automation.TreeWalker]::RawViewWalker
    $childEl = $walker.GetFirstChild($row)
    $invokableCell = $null
    while ($null -ne $childEl) {
        try {
            $ipTest = $childEl.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern) -as [System.Windows.Automation.InvokePattern]
            if ($null -ne $ipTest) { $invokableCell = $ipTest; break }
        } catch { }
        $childEl = $walker.GetNextSibling($childEl)
    }
    if ($null -eq $invokableCell) { Fail 'S5: no invokable Длина cell found on the circuit row' }
    $invokableCell.Invoke()
    Wait-IdResolvable 'HydraulicsCircuitLengthFirst' 'Edit' 5000

    $editor = Resolve-One 'HydraulicsCircuitLengthFirst' 'Edit'
    $edVp = $editor.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern) -as [System.Windows.Automation.ValuePattern]
    Add-Assertion 'S5: cell editor materialized with AutomationId (stored value)' '80' ("value='" + $edVp.Current.Value + "'") (($edVp.Current.Value) -ceq '80') 'HydraulicsCircuitLengthFirst'
    $edVp.SetValue('120')
    Start-Sleep -Milliseconds 400
    $edAfterSet = Resolve-One 'HydraulicsCircuitLengthFirst' 'Edit'
    $edVpAfter = $edAfterSet.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern) -as [System.Windows.Automation.ValuePattern]
    Add-Assertion 'S5: editor value set to 120 via accessibility ValuePattern' '120' ("value='" + $edVpAfter.Current.Value + "'") (($edVpAfter.Current.Value) -ceq '120') 'HydraulicsCircuitLengthFirst'
    Send-Key 0x0D 400   # ENTER commits (UpdateSourceTrigger=LostFocus)

    Wait-True -what 'committed length cell == 120' -timeoutMs 10000 -condition {
        $rows = Find-SelectableRowsContaining '120'
        return ($rows.Count -ge 1)
    }
    $rows120 = Find-SelectableRowsContaining '120'
    $cells120 = Get-RowCellTexts $rows120[0]
    Add-Assertion 'S5: committed first-circuit length cell == 120' '120' ($cells120 -join ' | ') ($cells120 -ccontains '120') 'DataGrid row[Длина]'
    $tDirty = $script:MainWindow.Current.Name
    Add-Assertion 'S5: canonical circuit-length write marks project dirty (title * )' '*project-a.smc — …' $tDirty ($tDirty.StartsWith('*'))

    Invoke-Button 'HydraulicsCalculateButton'
    Wait-True -what 'recalculated power cell appears (7840 W for L=120)' -timeoutMs 30000 -condition {
        $rows = Find-SelectableRowsContaining $script:CalcPowerLen120
        return ($rows.Count -ge 1)
    }
    Add-Assertion ('S5: Рассчитать recomputes power == ' + $script:CalcPowerLen120 + ' W (formula oracle for L=120)') `
        $script:CalcPowerLen120 'present' $true 'HydraulicsCalculateButton'
    [void](Save-Screenshot '04-circuit-length-edited')

    Select-Sidebar 'Результаты'
    Wait-True -what 'summary card reflects recalculated totals' -timeoutMs 15000 -condition {
        try {
            $v = Get-ResultsCardValue 'Длина труб'
            return ($v -ceq '128.0 м')
        } catch { return $false }
    }
    $cardLen5 = Get-ResultsCardValue 'Длина труб'
    Assert-ExactText $cardLen5 '128.0 м' 'S5: summary card Длина труб updated to 128,0 м (120+8)' 'Results card[Длина труб]'
    $cardPow5 = Get-ResultsCardValue 'Мощность'
    Assert-ExactText $cardPow5 $script:CardPowerLen120 ('S5: summary card Мощность updated to ' + $script:CardPowerLen120) 'Results card[Мощность]'
    $cardFlow5 = Get-ResultsCardValue 'Расход'
    Assert-ExactText $cardFlow5 $script:CardFlowLen120 ('S5: summary card Расход updated to ' + $script:CardFlowLen120) 'Results card[Расход]'
    [void](Save-Screenshot '05-summary-card-updated')
    Complete-Step

    # ---------------------------------------------------- S6 save/reload ---
    Start-Step 6 'S6 save (Файл→Сохранить ≙ Ctrl+S): SHA/timestamp advance, dirty clears; reload → identical outputs'
    Select-Sidebar 'Гидравлический расчёт'
    Wait-IdResolvable 'HydraulicsGlycolType' 'ComboBox' 15000
    Scan-Dialogs 'pre-save'

    $shaBeforeSave = Get-Sha256File $script:PathA
    $mtimeBeforeSave = (Get-Item -LiteralPath $script:PathA).LastWriteTimeUtc
    Invoke-TopMenuItem 'Файл' 'Сохранить' 'save-A'
    Wait-True -what 'saved file SHA advances' -timeoutMs 15000 -condition {
        return ((Get-Sha256File $script:PathA) -ne $shaBeforeSave)
    }
    $shaAfterSave = Get-Sha256File $script:PathA
    Add-Assertion 'S6: project-a.smc SHA advanced after save (Ctrl+S observable)' "! $shaBeforeSave" $shaAfterSave ($shaAfterSave -ne $shaBeforeSave)
    $mtimeAfterSave = (Get-Item -LiteralPath $script:PathA).LastWriteTimeUtc
    Add-Assertion 'S6: project-a.smc timestamp advanced after save' "> $mtimeBeforeSave" ([string]$mtimeAfterSave) ($mtimeAfterSave -gt $mtimeBeforeSave)
    Wait-True -what 'title loses leading * dirty marker' -timeoutMs 10000 -condition {
        $t = $script:MainWindow.Current.Name
        return (-not $t.StartsWith('*'))
    }
    $titleSaved = $script:MainWindow.Current.Name
    Assert-ExactText $titleSaved ("project-a.smc — $($script:MainTitleSuffix)") 'S6: title after save is clean <file> — Калькулятор снеготаяния REHAU'
    Assert-NoWindowByTitle $script:DialogTitleClose 'after-save-A'

    [void](Close-App $run1)

    $runR = Invoke-Launch 'a-reload' $script:PathA
    Wait-MainWindowTitleContains 'project-a.smc' 20000 'relaunched Project A title shows filename'
    Select-Sidebar 'Гидравлический расчёт'
    Wait-IdResolvable 'HydraulicsPipeSpacing' 'Text' 15000
    $rSp = Get-FirstNameNumber (Get-TextName 'HydraulicsPipeSpacing')
    Assert-NumberNear $rSp 25.0 0 'S6: reloaded spacing == 25 cm (identical)' 'HydraulicsPipeSpacing'
    $rSup = Get-FirstNameNumber (Get-TextName 'HydraulicsSupplyTemperature')
    Assert-NumberNear $rSup 50.0 1 'S6: reloaded supply == 50.0 (identical)' 'HydraulicsSupplyTemperature'
    $rRet = Get-FirstNameNumber (Get-TextName 'HydraulicsReturnTemperature')
    Assert-NumberNear $rRet 35.0 1 'S6: reloaded return == 35.0 (identical)' 'HydraulicsReturnTemperature'
    $glySelR = Get-ComboSelectionName 'HydraulicsGlycolType'
    Assert-ExactText $glySelR 'Этиленгликоль' 'S6: reloaded glycol type == Этиленгликоль' 'HydraulicsGlycolType'
    $concR = Get-TextBoxValue 'HydraulicsGlycolConcentration'
    Assert-NumberNear (Get-FirstNameNumber $concR) 30.0 1 'S6: reloaded concentration == 30' 'HydraulicsGlycolConcentration'
    Wait-True -what 'reloaded grid shows saved recalculated flow (387.4)' -timeoutMs 15000 -condition {
        $rows = Find-SelectableRowsContaining $script:CalcFlowLen120
        return ($rows.Count -ge 1)
    }
    $rowsR = Find-SelectableRowsContaining '120'
    if ($rowsR.Count -lt 1) { Fail 'S6: reloaded grid lost the edited length 120' }
    $cellsR = Get-RowCellTexts $rowsR[0]
    Add-Assertion 'S6: reloaded length cell == 120 (persisted edit)' '120' ($cellsR -join ' | ') ($cellsR -ccontains '120') 'DataGrid row[Длина]'
    Add-Assertion ('S6: reloaded flow cell == ' + $script:CalcFlowLen120 + ' (identical to pre-save state)') `
        $script:CalcFlowLen120 ($cellsR -join ' | ') ($cellsR -ccontains $script:CalcFlowLen120) 'DataGrid row[Расход л/ч]'
    Select-Sidebar 'Результаты'
    Wait-True -what 'reloaded summary card visible' -timeoutMs 15000 -condition {
        try { (Get-ResultsCardValue 'Длина труб') -ceq '128.0 м' } catch { return $false }
    }
    $cardLen6 = Get-ResultsCardValue 'Длина труб'
    Assert-ExactText $cardLen6 '128.0 м' 'S6: reloaded summary card Длина труб == 128,0 м (identical)' 'Results card[Длина труб]'
    $cardPow6 = Get-ResultsCardValue 'Мощность'
    Assert-ExactText $cardPow6 $script:CardPowerLen120 'S6: reloaded summary card Мощность identical' 'Results card[Мощность]'
    [void](Save-Screenshot '06-saved-reloaded-identical')
    [void](Close-App $runR)
    Complete-Step

    # ------------------------------------------------ S7 second load B ---
    Start-Step 7 'S7 second load project-b: clean replace, no stale project-A values'
    $runB = Invoke-Launch 'b-load-reset' $script:PathB
    Wait-MainWindowTitleContains 'project-b.smc' 20000 'Project B load reflected in window title'
    Select-Sidebar 'Гидравлический расчёт'
    Wait-IdResolvable 'HydraulicsPipeSpacing' 'Text' 15000
    $bSp = Get-FirstNameNumber (Get-TextName 'HydraulicsPipeSpacing')
    Assert-NumberNear $bSp 15.0 0 'S7: spacing == 15 cm (B pipeSpacing 150 mm)' 'HydraulicsPipeSpacing'
    $bSup = Get-FirstNameNumber (Get-TextName 'HydraulicsSupplyTemperature')
    Assert-NumberNear $bSup 55.0 1 'S7: supply == 55.0 (B thermal inputs)' 'HydraulicsSupplyTemperature'
    $bRet = Get-FirstNameNumber (Get-TextName 'HydraulicsReturnTemperature')
    Assert-NumberNear $bRet 30.0 1 'S7: return == 30.0 (invalid-B thermal falls back to default return)' 'HydraulicsReturnTemperature'
    $bGly = Get-ComboSelectionName 'HydraulicsGlycolType'
    Assert-ExactText $bGly 'Пропиленгликоль' 'S7: glycol type == Пропиленгликоль (B fixture)' 'HydraulicsGlycolType'
    $bConc = Get-TextBoxValue 'HydraulicsGlycolConcentration'
    Assert-NumberNear (Get-FirstNameNumber $bConc) 40.0 1 'S7: concentration == 40 (B fixture)' 'HydraulicsGlycolConcentration'
    $bHeat = Get-TextBoxValue 'HydraulicsSupplyHeatPercent'
    Assert-NumberNear (Get-FirstNameNumber $bHeat) 15.0 1 'S7: supply heat percent == 15 (B fixture)' 'HydraulicsSupplyHeatPercent'

    $rowsB1 = Find-SelectableRowsContaining '60'
    if ($rowsB1.Count -lt 1) { Fail 'S7: B row with length 60 not found' }
    $cellsB1 = Get-RowCellTexts $rowsB1[0]
    Add-Assertion 'S7: B circuit-1 row length == 60' '60' ($cellsB1 -join ' | ') ($cellsB1 -ccontains '60') 'DataGrid row[Длина]'
    Add-Assertion 'S7: B circuit-1 row power == 11111 (B sentinel, not stale A)' '11111' ($cellsB1 -join ' | ') ($cellsB1 -ccontains '11111') 'DataGrid row[Мощность]'
    $rowsB2 = Find-SelectableRowsContaining '90'
    if ($rowsB2.Count -lt 1) { Fail 'S7: B row with length 90 not found' }
    $cellsB2 = Get-RowCellTexts $rowsB2[0]
    Add-Assertion 'S7: B circuit-2 row length == 90' '90' ($cellsB2 -join ' | ') ($cellsB2 -ccontains '90') 'DataGrid row[Длина]'
    Add-Assertion 'S7: B circuit-2 row power == 33333 (B sentinel, distinct per-row)' '33333' ($cellsB2 -join ' | ') ($cellsB2 -ccontains '33333') 'DataGrid row[Мощность]'
    $stale120 = Find-SelectableRowsContaining '120'
    Add-Assertion 'S7: NO stale project-A length (120) anywhere in B grid' '0 rows' ([string]$stale120.Count) ($stale120.Count -eq 0) 'DataGrid'
    $staleFlow = Find-SelectableRowsContaining $script:CalcFlowLen120
    Add-Assertion 'S7: NO stale project-A recalculated flow carried into B' '0 rows' ([string]$staleFlow.Count) ($staleFlow.Count -eq 0) 'DataGrid'

    Select-Sidebar 'Результаты'
    Wait-True -what 'B summary card visible' -timeoutMs 15000 -condition {
        try { (Get-ResultsCardValue 'Длина труб') -ceq '165.0 м' } catch { return $false }
    }
    $cardLen7 = Get-ResultsCardValue 'Длина труб'
    Assert-ExactText $cardLen7 '165.0 м' 'S7: B summary card Длина труб == 165.0 м (60+6+90+9)' 'Results card[Длина труб]'
    $cardPow7 = Get-ResultsCardValue 'Мощность'
    Assert-ExactText $cardPow7 '44444 Вт' 'S7: B summary card Мощность == 44444 Вт (B sentinel, not stale A 7840)' 'Results card[Мощность]'
    $cardFlow7 = Get-ResultsCardValue 'Расход'
    Assert-ExactText $cardFlow7 '6.67 м³/ч' 'S7: B summary card Расход == 6.67 м³/ч (6666/1000, not stale A 0.39)' 'Results card[Расход]'
    [void](Save-Screenshot '07-second-load-project-b')
    Complete-Step

    # -------------------------------------------------------- S8 reset ---
    Start-Step 8 'S8 reset (Файл→Создать новый расчёт): defaults restored'
    Invoke-TopMenuItem 'Файл' 'Создать новый расчёт' 'new-B'
    Start-Sleep -Milliseconds 1200
    Assert-NoWindowByTitle $script:DialogTitleNew 'after-new-B'
    Scan-Dialogs 'post-new-B'
    $nTitle = $script:MainWindow.Current.Name
    Assert-ExactText $nTitle $script:MainTitleSuffix 'S8: title is bare app title (clean, no file)' 
    Select-Sidebar 'Гидравлический расчёт'
    Wait-IdResolvable 'HydraulicsGlycolType' 'ComboBox' 15000
    $nGly = Get-ComboSelectionName 'HydraulicsGlycolType'
    Assert-ExactText $nGly 'Этиленгликоль' 'S8: reset glycol type == Этиленгликоль (default)' 'HydraulicsGlycolType'
    $nConc = Get-TextBoxValue 'HydraulicsGlycolConcentration'
    Assert-NumberNear (Get-FirstNameNumber $nConc) 50.0 1 'S8: reset concentration == 50 (HydraulicInputData default)' 'HydraulicsGlycolConcentration'
    $nHeat = Get-TextBoxValue 'HydraulicsSupplyHeatPercent'
    Assert-NumberNear (Get-FirstNameNumber $nHeat) 10.0 1 'S8: reset supply heat percent == 10 (default)' 'HydraulicsSupplyHeatPercent'
    $nVm = Resolve-Optional 'HydraulicsValidationMessage' 'Text'
    Add-Assertion 'S8: reset leaves no validation message' 'absent' $(if ($null -eq $nVm) { 'absent' } else { $nVm.Current.Name }) ($null -eq $nVm) 'HydraulicsValidationMessage'
    Select-Sidebar 'Результаты'
    # After reset CircuitsViewModel.Reset() re-adds one default collector and
    # rebuilds the summary cards, so Results shows ONE ZEROED card (probe-
    # verified), not the collapsed empty state.
    Wait-True -what 'reset summary card visible (zeroed defaults)' -timeoutMs 15000 -condition {
        try { (Get-ResultsCardValue 'Длина труб') -ceq '0.0 м' } catch { return $false }
    }
    $cardLen8 = Get-ResultsCardValue 'Длина труб'
    Assert-ExactText $cardLen8 '0.0 м' 'S8: summary card Длина труб reset to 0.0 м (default collector)' 'Results card[Длина труб]'
    $cardPow8 = Get-ResultsCardValue 'Мощность'
    Assert-ExactText $cardPow8 '0 Вт' 'S8: summary card Мощность reset to 0 Вт (B sentinel 44444 gone)' 'Results card[Мощность]'
    $cardFlow8 = Get-ResultsCardValue 'Расход'
    Assert-ExactText $cardFlow8 '0.00 м³/ч' 'S8: summary card Расход reset to 0.00 м³/ч' 'Results card[Расход]'
    Note-Deviation ('Reset leaves ONE ZEROED default-collector summary card on Результаты (Длина труб 0.0 м / Мощность 0 Вт / Расход 0.00 м³/ч) instead of the collapsed empty state: CircuitsViewModel.Reset() -> AddCollector() -> RebuildHydraulicSummaryCards() adds a card per collector regardless of results (CircuitsViewModel.cs:696-711, 670-679). Asserted as the code-faithful defaults-restored observable.')
    [void](Save-Screenshot '08-reset-defaults')
    [void](Close-App $runB)
    Complete-Step
}

# =====================================================================
# FAILURE BRANCH (separate process run) + observations emission
# =====================================================================
function Invoke-FailureBranch {
    Start-Step 9 'F failure branch: corrupt unknown-pipe.smc → graceful validation dialog, NO crash, clean close'
    $runU = Invoke-Launch 'unknown-pipe' $script:PathInvalid -ExpectErrorDialog

    # wait for the modal validation dialog owned by THIS pid
    $dlg = $null
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    while ($sw.Elapsed.TotalMilliseconds -lt 20000) {
        if ($runU.proc.HasExited) { Fail 'F: process EXITED while opening corrupt file (crash, not graceful validation)' }
        $dlg = Find-AppDialogByTitle $script:DialogTitleError
        if ($null -ne $dlg) { break }
        Start-Sleep -Milliseconds $script:PollMs
    }
    if ($null -eq $dlg) { Fail "F: expected validation dialog '$($script:DialogTitleError)' did not appear within 20s" }
    Set-RuntimeId $dlg
    Add-Assertion 'F: graceful validation dialog appeared' $script:DialogTitleError $script:DialogTitleError $true "Window[name=$($script:DialogTitleError)]"

    $txtCond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::Text)
    $msgTexts = $dlg.FindAll([System.Windows.Automation.TreeScope]::Descendants, $txtCond)
    $msg = @()
    for ($i = 0; $i -lt $msgTexts.Count; $i++) { $msg += $msgTexts.Item($i).Current.Name }
    $msgJoined = $msg -join ' '
    Assert-ContainsText $msgJoined 'Не удалось открыть проект:' 'F: dialog message reports load failure' 'dialog Text'
    [void](Save-Screenshot '09-unknown-pipe-validation-dialog')

    # dismiss via the unique enabled OK button (locale-tolerant name match)
    $btnCondType = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::Button)
    $btns = $dlg.FindAll([System.Windows.Automation.TreeScope]::Descendants, $btnCondType)
    $oks = @()
    for ($b = 0; $b -lt $btns.Count; $b++) {
        $bn = $btns[$b].Current.Name
        if (($bn -ceq 'OK' -or $bn -ceq 'ОК') -and $btns[$b].Current.IsEnabled) { $oks += $btns[$b] }
    }
    if ($oks.Count -ne 1) { Fail "F: expected exactly 1 enabled OK button on dialog, got $($oks.Count)" }
    Set-RuntimeId $oks[0]
    ($oks[0].GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern) -as [System.Windows.Automation.InvokePattern]).Invoke()
    Start-Sleep -Milliseconds 1200

    Wait-True -what 'validation dialog closed after OK' -timeoutMs 8000 -condition {
        return ($null -eq (Find-AppDialogByTitle $script:DialogTitleError))
    }
    Add-Assertion 'F: dialog dismissed via OK' 'closed' 'closed' $true

    if ($runU.proc.HasExited) { Fail 'F: process exited after dismissing dialog (must stay alive)' }
    $aliveT = $script:MainWindow.Current.Name
    Add-Assertion 'F: process ALIVE after corrupt load; main window still bare app title' $script:MainTitleSuffix $aliveT ($aliveT -ceq $script:MainTitleSuffix)

    $recU = Close-App $runU

    $failStep = $script:Steps[$script:Steps.Count - 1]
    $failObs = [ordered]@{
        project = 'unknown-pipe.smc'
        fixturePath = (Get-RelPath $script:PathInvalid)
        fixtureSha256 = (Get-Sha256File $script:PathInvalid)
        characterization = [ordered]@{
            corruption = 'hydraulicsData.collectors[0].valveType = "PHASE5 UNKNOWN PIPE" — undefined ValveType enum string'
            expectedPath = 'ProjectFileService.LoadProjectResultAsync catches JsonException → OperationResult.Failure → ResultsViewModel.LoadProjectFromPathAsync shows _dialogService.ShowError("Не удалось открыть проект: …", "Ошибка") (ResultsViewModel.cs:789)'
            gracefulContract = 'process stays alive, dialog dismissed via OK, clean WM_CLOSE exit 0, stderr free of crash patterns'
        }
        assertions = $failStep.assertions
        artifacts = $failStep.artifacts
        process = $recU
        screenshots = @('09-unknown-pipe-validation-dialog.png')
        result = 'PASS'
    }
    Write-Utf8NoBomFile (Join-Path $script:OutDir 'failure-observations.json') (
        ConvertTo-Json -InputObject $failObs -Depth 10)
    Add-ArtifactRecord (Join-Path $script:OutDir 'failure-observations.json') 'failure-branch observations'
    Complete-Step
}

# ============================================================== reporting ====
function Build-ObservationsJson([string]$result, [string]$errorText, [string]$exeShaAfter) {
    $selArr = @()
    foreach ($reg in $script:SelectorRegistry) {
        $st = $script:SelectorStats[$reg.id]
        $selArr += [ordered]@{
            id = $reg.id; controlType = $reg.type; view = $reg.view; optional = $reg.optional
            resolvedCount = $st.resolvedCount; lastPresent = $st.lastPresent
        }
    }
    $stepsArr = @()
    foreach ($s in $script:Steps) {
        $stepsArr += [ordered]@{
            step = $s.step; name = $s.name; status = $s.status
            assertions = @($s.assertions); artifacts = @($s.artifacts)
        }
    }
    return (ConvertTo-Json -InputObject ([ordered]@{
        executable = $script:ExePath
        generatedUtc = [DateTime]::UtcNow.ToString('o')
        frozenExeSha256 = [ordered]@{
            beforeFirstFlow = $script:ExeShaPreRun
            afterLastFlow = $exeShaAfter
            equal = ($exeShaAfter -eq $script:ExeShaPreRun)
        }
        fixtures = $script:FixtureInfo
        processes = @($script:ProcRuns)
        selectors = $selArr
        steps = $stepsArr
        screenshots = @($script:Shots)
        deviations = @($script:Deviations)
        unexpectedDialogs = @($script:UnexpectedDialogs)
        error = $errorText
        result = $result
    }) -Depth 10)
}

# ------------------------------------------------------------------ run all ---
$errorText = ''
$result = 'FAIL'
$exeShaAfter = ''
try {
    Invoke-MainFlow
    Invoke-FailureBranch
    $exeShaAfter = Test-ExeSha 'after-last-flow'
    $result = 'PASS'
}
catch {
    $pos = ''
    try { $pos = ($_.InvocationInfo.PositionMessage -replace '\s+', ' ').Trim() } catch { }
    $stack = ''
    try { $stack = $_.ScriptStackTrace } catch { }
    $errorText = ('{0} @ {1} | STACK: {2}' -f $_.Exception.Message, $pos, $stack)
    Write-Output "run-hydraulics-flows: FAILURE - $errorText"
    if ($null -ne $script:CurrentStep) {
        $script:CurrentStep.status = 'FAILED'
        $script:CurrentStep.error = $errorText
        $script:Steps.Add($script:CurrentStep)
        $script:CurrentStep = $null
    }
    if ($null -ne $script:ActiveProc) {
        try {
            if (-not $script:ActiveProc.HasExited) { $script:ActiveProc.Kill() }
        } catch { }
    }
    try { $exeShaAfter = Get-Sha256File $script:ExePath } catch { $exeShaAfter = '' }
}
finally {
    try {
        Write-Utf8NoBomFile (Join-Path $script:OutDir 'observations.json') (Build-ObservationsJson $result $errorText $exeShaAfter)
    }
    catch {
        Write-Output ("run-hydraulics-flows: FATAL-OBS - " + $_.Exception.Message + " @ line " + $_.InvocationInfo.ScriptLineNumber)
    }
    try {
        $equal = ($exeShaAfter -eq $script:ExeShaPreRun)
        $shaTxt = "executable: $script:ExePath`nsha256BeforeFirstFlow: $script:ExeShaPreRun`nsha256AfterLastFlow: $exeShaAfter`nequal: $equal`ngeneratedUtc: $([DateTime]::UtcNow.ToString('o'))"
        Write-Utf8NoBomFile (Join-Path $script:OutDir 'exe-sha256.txt') $shaTxt
    }
    catch {
        Write-Output ("run-hydraulics-flows: FATAL-SHA - " + $_.Exception.Message)
        if ($result -eq 'PASS') { $result = 'FAIL' }
    }
}

if ($result -eq 'PASS') {
    Write-Output 'run-hydraulics-flows: PASS (happy-path steps + failure branch green)'
    exit 0
}
exit 1
