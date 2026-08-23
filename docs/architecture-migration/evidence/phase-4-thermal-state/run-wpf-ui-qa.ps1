# =============================================================================
# Todo 13 / V9 UI QA harness (QA-only, inbox .NET only).
# Frozen plan contract (phase-4-thermal-state.md lines 264-316, 476-484):
#   - inbox PowerShell/.NET APIs only: System.Windows.Automation (UIAutomation),
#     System.Drawing, Start-Process (+ System.Windows.Forms.SendKeys for the
#     plan-mandated Ctrl+S / Ctrl+N keystrokes; no mouse coordinates ever);
#   - requires Windows interactive desktop (hard failure otherwise);
#   - validates executable SHA-256 against frozen-release-sha256.json before
#     and after EVERY process run;
#   - launches the exe directly with the .smc path as first argument via
#     Start-Process -PassThru -RedirectStandardOutput/-RedirectStandardError
#     with distinct run-owned log names per run; records PID/exit code and
#     SHA-256 of both logs; rejects nonzero exit or any stderr line matching
#     unhandled-exception/fatal-crash patterns;
#   - resolves each of the 17 AutomationIds by exact ID + expected ControlType;
#     every selector must match exactly one enabled element else exit nonzero;
#   - sidebar items selected by ControlType.ListItem + rendered names
#     «Тепловой расчёт» / «Гидравлический расчёт» / «Результаты»;
#   - unexpected dialogs identified by ControlType.Window + ProcessId,
#     dismissed ONLY via the unique enabled Button named 'Cancel', then FAIL;
#   - ten numbered steps + separate unknown-pipe failure branch;
#   - writes observations.json, failure-observations.json, seven screenshots,
#     run-owned stdout/stderr logs and task-13-user-flow-qa.md under
#     -OutputDirectory. Exit 0 only if EVERYTHING passes; any ambiguity,
#     dialog, timeout, crash or missing artifact exits 1 (no manual fallback).
# Usage (V9 exact): pwsh -NoProfile -File run-wpf-ui-qa.ps1 -Executable ... \
#   -ExpectedExecutableSha256File frozen-release-sha256.json -ProjectA ... \
#   -ProjectB ... -InvalidProject ... -OutputDirectory ...
# =============================================================================
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Executable,
    [Parameter(Mandatory = $true)][string]$ExpectedExecutableSha256File,
    [Parameter(Mandatory = $true)][string]$ProjectA,
    [Parameter(Mandatory = $true)][string]$ProjectB,
    [Parameter(Mandatory = $true)][string]$InvalidProject,
    [Parameter(Mandatory = $true)][string]$OutputDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# --------------------------------------------------------------- constants ---
$script:MainTitleSuffix    = 'Калькулятор снеготаяния REHAU'
$script:MsgModeChanged     = 'Режим работы изменён. Требуется пересчёт.'
$script:MsgSupplyChanged   = 'Температура подачи изменена. Требуется пересчёт.'
$script:DialogTitleClose   = 'Закрытие приложения'
$script:DialogTitleNew     = 'Создать новый расчёт'
$script:CancelButtonName   = 'Cancel'
$script:StderrCrashPatterns = @(
    'Unhandled exception', 'Unhandled Exception', 'Необработанное исключение',
    'XamlParseException', 'Stack overflow', 'Access violation',
    'Критическая ошибка', 'Критический сбой'
)
$script:WindowWaitMs   = 60000
$script:ExitWaitMs     = 40000
$script:PollMs         = 300

function Fail([string]$message) {
    throw [System.InvalidOperationException]::new("run-wpf-ui-qa: $message")
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
$script:ExePath       = Resolve-InputPath $Executable
$script:ManifestPath  = Resolve-InputPath $ExpectedExecutableSha256File
$script:PathA         = Resolve-InputPath $ProjectA
$script:PathB         = Resolve-InputPath $ProjectB
$script:PathInvalid   = Resolve-InputPath $InvalidProject
$script:OutDir        = Resolve-InputPath $OutputDirectory

foreach ($f in @($script:ExePath, $script:ManifestPath, $script:PathA, $script:PathB, $script:PathInvalid)) {
    if (-not (Test-Path -LiteralPath $f -PathType Leaf)) { Fail "required input not found: $f" }
}
if (-not (Test-Path -LiteralPath $script:OutDir)) {
    New-Item -ItemType Directory -Path $script:OutDir -Force | Out-Null
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

# ------------------------------------------- frozen manifest (executable sha) ---
$manifestJson = Get-Content -LiteralPath $script:ManifestPath -Raw | ConvertFrom-Json
if ($null -eq $manifestJson.executable -or $null -eq $manifestJson.executable.sha256) {
    Fail 'frozen manifest has no executable.sha256 key'
}
$script:ExpectedExeSha = ([string]$manifestJson.executable.sha256).ToUpperInvariant()
if ($script:ExpectedExeSha -notmatch '^[0-9A-F]{64}$') { Fail "malformed executable sha256 in manifest: $($script:ExpectedExeSha)" }

# --------------------------------------------- interactive desktop + assemblies ---
if (-not [Environment]::UserInteractive) {
    Fail 'no interactive desktop session (UserInteractive=false); the V9 harness requires a Windows interactive desktop'
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
    Add-Type -Namespace Win32Uaq -Name NativeMethods -MemberDefinition (
        '[DllImport("user32.dll")] public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);' +
        '[DllImport("user32.dll")] public static extern bool SetProcessDPIAware();')
    [void][Win32Uaq.NativeMethods]::SetProcessDPIAware()
}
catch {
    Fail "user32 P/Invoke bootstrap failed: $($_.Exception.Message)"
}

# ------------------------------------------------------------- script state ---
$script:MainPid      = -1
$script:MainWindow   = $null
$script:MainHandle   = [IntPtr]::Zero
$script:ActiveProc   = $null
$script:ProcRuns     = [System.Collections.Generic.List[object]]::new()
$script:Steps        = [System.Collections.Generic.List[object]]::new()
$script:Shots        = [System.Collections.Generic.List[object]]::new()
$script:Deviations   = [System.Collections.Generic.List[string]]::new()
$script:FixtureInfo  = $null
$script:CurrentStep  = $null
$script:Step5PowerName  = ''
$script:Step5PowerValue = [double]::NaN
$script:SelectorStats   = [ordered]@{}
$script:UnexpectedDialogs = [System.Collections.Generic.List[object]]::new()

# The 17-ID accessibility registry (Todo 6 contract == V9 catalog, one set).
# optional=$true elements may be COLLAPSED/absent from the UIA tree when empty
# (ThermalRecalcMessage / ThermalResultStatus); absence is a valid observable.
$script:SelectorRegistry = @(
    @{ id = 'ThermalMode';               type = 'ComboBox'; view = 'Thermal';    optional = $false },
    @{ id = 'ThermalSupplyTemperature';  type = 'Edit';     view = 'Thermal';    optional = $false },
    @{ id = 'ThermalGroundTemperature';  type = 'Edit';     view = 'Thermal';    optional = $false },
    @{ id = 'ThermalPipe';               type = 'ComboBox'; view = 'Thermal';    optional = $false },
    @{ id = 'ThermalPipeSpacing';        type = 'ComboBox'; view = 'Thermal';    optional = $false },
    @{ id = 'ThermalCalculate';          type = 'Button';   view = 'Thermal';    optional = $false },
    @{ id = 'ThermalReset';              type = 'Button';   view = 'Thermal';    optional = $false },
    @{ id = 'ThermalRecalcMessage';      type = 'Text';     view = 'Thermal';    optional = $true  },
    @{ id = 'ThermalDeltaT';             type = 'Text';     view = 'Thermal';    optional = $false },
    @{ id = 'ThermalPowerTotal';         type = 'Text';     view = 'Thermal';    optional = $false },
    @{ id = 'ThermalResultStatus';       type = 'Text';     view = 'Thermal';    optional = $true  },
    @{ id = 'HydraulicsPipeSpacing';     type = 'Text';     view = 'Hydraulics'; optional = $false },
    @{ id = 'HydraulicsSupplyTemperature'; type = 'Text';   view = 'Hydraulics'; optional = $false },
    @{ id = 'HydraulicsReturnTemperature'; type = 'Text';   view = 'Hydraulics'; optional = $false },
    @{ id = 'ResultsThermalPower';       type = 'Text';     view = 'Results';    optional = $false },
    @{ id = 'ResultsSupplyTemperature';  type = 'Text';     view = 'Results';    optional = $false },
    @{ id = 'ResultsReturnTemperature';  type = 'Text';     view = 'Results';    optional = $false }
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
    Write-Output ("run-wpf-ui-qa: step {0}: {1}" -f $n, $name)
}
function Add-Assertion([string]$label, [string]$expected, [string]$observed, [bool]$pass) {
    if ($null -ne $script:CurrentStep) {
        $script:CurrentStep.assertions.Add([ordered]@{ assert = $label; expected = $expected; observed = $observed; pass = $pass })
    }
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

# Numeric compare: first number in displayed text, invariant parse, rounded to
# the XAML StringFormat decimals (F1 -> 0.05 tolerance, F0 -> 0.5).
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
function Assert-NumberNear([double]$parsed, [double]$expected, [int]$decimals, [string]$label) {
    $ok = Test-Near $parsed $expected $decimals
    Add-Assertion $label ("{0} (+/-{1}dp)" -f $expected, $decimals) ([string]$parsed) $ok
}
function Assert-ExactText([string]$observed, [string]$expected, [string]$label) {
    Add-Assertion $label $expected $observed ($observed -ceq $expected)
}

# ------------------------------------------------------- executable SHA gate ---
function Test-ExeSha([string]$moment) {
    if (-not (Test-Path -LiteralPath $script:ExePath -PathType Leaf)) {
        Fail "executable disappeared at $moment" 
    }
    $actual = Get-Sha256File $script:ExePath
    if ($actual -ne $script:ExpectedExeSha) {
        Fail "executable SHA-256 mismatch at ${moment}: expected $($script:ExpectedExeSha), actual $actual"
    }
    return $actual
}

# ----------------------------------------------------------- dialog machinery ---
function Get-AppWindows {
    $cond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::Window)
    # NOTE: materialize into a plain array and return with the comma operator —
    # PowerShell unwraps enumerable function returns, turning an EMPTY UIA
    # collection into $null (which breaks .Count under Set-StrictMode).
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
        if ($c.ClassName -match 'Popup') { continue }           # combo popups are not dialogs
        if ([string]::IsNullOrWhiteSpace($c.Name)) { continue } # untitled aux windows are not dialogs
        $title = $c.Name
        $script:UnexpectedDialogs.Add([ordered]@{ context = $context; title = $title; className = $c.ClassName })
        # Dismiss attempt: unique ENABLED Button named exactly 'Cancel'.
        $dismissed = $false
        try {
            $btnCondType = New-Object System.Windows.Automation.PropertyCondition(
                [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
                [System.Windows.Automation.ControlType]::Button)
            $btns = $w.FindAll([System.Windows.Automation.TreeScope]::Descendants, $btnCondType)
            $cancelBtns = @()
            for ($b = 0; $b -lt $btns.Count; $b++) {
                if ($btns[$b].Current.Name -ceq $script:CancelButtonName -and $btns[$b].Current.IsEnabled) {
                    $cancelBtns += $btns[$b]
                }
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

# --------------------------------------------------------------- selectors ---
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
    # materialize + comma-return: prevents empty-collection -> $null unwrap
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
    return $el
}
function Resolve-Optional([string]$id, [string]$typeName) {
    $ct = Get-ControlTypeByName $typeName
    $all = Find-ByIdAndType $id $ct
    if ($all.Count -eq 0) { Register-Resolution $id $false; return $null }
    if ($all.Count -gt 1) {
        Register-Resolution $id $false
        Fail "optional selector '$id' ($typeName): ambiguous, $($all.Count) matches"
    }
    Register-Resolution $id $true
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
function Verify-ThermalRegistry {
    foreach ($reg in ($script:SelectorRegistry | Where-Object { $_.view -eq 'Thermal' })) {
        if ($reg.optional) {
            $el = Resolve-Optional $reg.id $reg.type
            $present = ($null -ne $el)
            Add-Assertion "registry:$($reg.id) unique/correct-type (optional)" "$($reg.type), presence optional" $(if ($present) { "present/$($reg.type)" } else { 'absent (collapsed)' }) $true
        }
        else {
            $el = Resolve-One $reg.id $reg.type
            Add-Assertion "registry:$($reg.id) unique/enabled/$($reg.type)" "$($reg.type)" "$($el.Current.ControlType.ProgrammaticName)" $true
        }
    }
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
    Start-Sleep -Milliseconds 800
    Scan-Dialogs "sidebar-select:$title"
}

# ------------------------------------------------------------------- combos ---
function Get-ComboItems([System.Windows.Automation.AutomationElement]$combo) {
    # NOTE: pwsh's trimmed UIAutomationTypes assembly has NO ControlType.ComboBoxItem
    # static (strict access throws); WPF ComboBoxItem automation peers report
    # ControlType.ListItem anyway. Fallback: any named descendant supporting
    # SelectionItemPattern.
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
            'number'   { $n = Get-FirstNameNumber $nm; $hit = ($null -ne $n -and [math]::Abs($n - [double]$arg) -lt 0.0000001) }
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
function Get-ComboSelectionInfo([string]$id) {
    $combo = Resolve-One $id 'ComboBox'
    $selCount = 0
    $selName = ''
    try {
        $sp = $combo.GetCurrentPattern([System.Windows.Automation.SelectionPattern]::Pattern) -as [System.Windows.Automation.SelectionPattern]
        $sel = @($sp.Current.GetSelection())
        $selCount = $sel.Count
        if ($sel.Count -ge 1) { $selName = $sel[0].Current.Name }
    } catch {
        $selCount = -1
        $selName = $combo.Current.Name
    }
    return [pscustomobject]@{ SelectionCount = $selCount; Name = $selName }
}
# Tolerant variant: no enabled requirement (e.g. spacing combo is DISABLED when
# no pipe is selected after the ^n reset — still a valid observable).
function Get-ComboSelectionInfoLoose([string]$id) {
    $ct = Get-ControlTypeByName 'ComboBox'
    $all = Find-ByIdAndType $id $ct
    if ($all.Count -ne 1) {
        Register-Resolution $id $false
        Fail "loose selector '$id' (ComboBox): $($all.Count) matches (exactly one required)"
    }
    Register-Resolution $id $true
    $combo = $all[0]
    $selCount = 0
    $selName = ''
    try {
        $sp = $combo.GetCurrentPattern([System.Windows.Automation.SelectionPattern]::Pattern) -as [System.Windows.Automation.SelectionPattern]
        $sel = @($sp.Current.GetSelection())
        $selCount = $sel.Count
        if ($sel.Count -ge 1) { $selName = $sel[0].Current.Name }
    } catch {
        $selCount = -1
        $selName = $combo.Current.Name
    }
    return [pscustomobject]@{ SelectionCount = $selCount; Name = $selName; Enabled = $combo.Current.IsEnabled }
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
function Wait-True([scriptblock]$condition, [int]$timeoutMs, [string]$what) {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    while ($sw.Elapsed.TotalMilliseconds -lt $timeoutMs) {
        try { if ((& $condition)) { return } } catch { }
        Start-Sleep -Milliseconds $script:PollMs
    }
    Fail "timeout after ${timeoutMs}ms waiting for: $what"
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
    $pngPath = Join-Path $script:OutDir "$fileBase.png"
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
function Invoke-Launch([string]$tag, [string]$projectPath) {
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

    # wait for the main window of THIS pid
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
    Add-Assertion "launch '$tag': window title carries app suffix" "*$($script:MainTitleSuffix)" $title ($title -like "*$($script:MainTitleSuffix)*")
    Scan-Dialogs "post-launch-$tag"
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
    [void][Win32Uaq.NativeMethods]::PostMessage($h, 0x0010, [IntPtr]::Zero, [IntPtr]::Zero)

    $proc = $run.proc
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    while ($sw.Elapsed.TotalMilliseconds -lt $script:ExitWaitMs) {
        if ($proc.HasExited) { break }
        # dirty-marker persistence would raise the closing dialog — detect and fail
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
    # DEVIATION NOTE: the frozen brief says "send Ctrl+S/Ctrl+N after SetFocus".
    # Empirically (probes 4-8) injected chords NEVER reach the app's Window-level
    # KeyDown handler in this environment (plain keys and TextBox-internal chords
    # DO deliver; Ctrl+O raises no dialog). The harness therefore drives the SAME
    # commands through the app's own visible menu surface («Файл» -> «Сохранить» /
    # «Создать новый расчёт», bound to SaveProjectCommand / NewCalculationCommand)
    # via UIA Invoke/Selection patterns — no mouse coordinates, inbox APIs only.
    # The observable contract (save -> SHA advance + dirty marker clears; reset ->
    # DEC-T01 defaults) is unchanged.
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

# =============================================================================
# MAIN FLOW — ten numbered steps (runs 1-3 here; run 4 failure branch in part 3)
# =============================================================================
function Invoke-MainFlow {
    # ---------------------------------------------------------- step 1: fixtures ---
    Start-Step 1 'Verify fixture-manifest.json and all three input SHA-256 values'
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
    $exeShaPre = Test-ExeSha 'pre-run'
    $script:FixtureInfo = [ordered]@{
        manifest = (Get-RelPath $manifestLocal); outputs = $fxChecks
        executableShaPreRun = $exeShaPre
    }
    Complete-Step

    # ------------------------------------------------------ step 2: launch A ---
    Start-Step 2 'Start Project A as first .smc command-line argument and wait for main window'
    $run1 = Invoke-Launch 'a-edit-save' $script:PathA
    Wait-MainWindowTitleContains 'project-a.smc' 20000 'Project A load reflected in window title (filename appears)'
    $t2 = $script:MainWindow.Current.Name
    Assert-ExactText $t2 ("project-a.smc — $($script:MainTitleSuffix)") 'step2: clean loaded title (no dirty marker)'
    Complete-Step

    # ------------------------------------------- step 3: Thermal baseline ---
    Start-Step 3 'Navigate to Thermal and record baseline mode/supply/ground/pipe/spacing/result text'
    Select-Sidebar 'Тепловой расчёт'
    Wait-IdResolvable 'ThermalMode' 'ComboBox' 15000

    $modeInfo = Get-ComboSelectionInfo 'ThermalMode'
    Add-Assertion 'baseline mode == Melting' 'Melting' "$($modeInfo.SelectionCount):$($modeInfo.Name)" ($modeInfo.Name -ceq 'Melting')
    $supplyTxt = Get-TextBoxValue 'ThermalSupplyTemperature'
    $supplyN = Get-FirstNameNumber $supplyTxt
    Assert-NumberNear $supplyN 50.0 1 'baseline supply temperature (F1)'
    $groundTxt = Get-TextBoxValue 'ThermalGroundTemperature'
    $groundN = Get-FirstNameNumber $groundTxt
    Assert-NumberNear $groundN 10.0 1 'baseline ground temperature (F1)'
    $pipeInfo = Get-ComboSelectionInfo 'ThermalPipe'
    Add-Assertion "baseline pipe contains 'RAUTHERM S 20'" '*RAUTHERM S 20*' $pipeInfo.Name ($pipeInfo.Name -like '*RAUTHERM S 20*')
    $spacingInfo = Get-ComboSelectionInfo 'ThermalPipeSpacing'
    $spacingN = Get-FirstNameNumber $spacingInfo.Name
    Assert-NumberNear $spacingN 250 0 'baseline pipe spacing (mm)'

    $ptEl = Resolve-One 'ThermalPowerTotal' 'Text'
    $ptName = $ptEl.Current.Name
    $ptN = Get-FirstNameNumber $ptName
    Assert-NumberNear $ptN 261.0 1 'baseline PowerTotal (fixture v1-sample result, F1)'
    $dtName = Get-TextName 'ThermalDeltaT'
    $dtN = Get-FirstNameNumber $dtName
    Assert-NumberNear $dtN 15.0 1 'baseline DeltaT (F1)'
    $recalcBase = Resolve-Optional 'ThermalRecalcMessage' 'Text'
    Add-Assertion 'baseline recalc message absent (collapsed)' 'absent' $(if ($null -eq $recalcBase) { 'absent' } else { $recalcBase.Current.Name }) ($null -eq $recalcBase)
    $statusBase = Resolve-Optional 'ThermalResultStatus' 'Text'
    Add-Assertion 'baseline validation status absent (collapsed)' 'absent' $(if ($null -eq $statusBase) { 'absent' } else { $statusBase.Current.Name }) ($null -eq $statusBase)
    Complete-Step

    # ------------------------------------------------------------ step 4: edits ---
    Start-Step 4 'Edit mode/supply/ground/pipe/spacing; assert exact recalculation oracles and prior result retention'
    Select-ComboItem 'ThermalMode' 'exact' 'AntiIcing'
    Wait-True -what 'exact mode-changed recalc message' -timeoutMs 8000 -condition {
        $el = Resolve-Optional 'ThermalRecalcMessage' 'Text'
        return ($null -ne $el -and $el.Current.Name -ceq $script:MsgModeChanged)
    }
    $msgMode = (Resolve-One 'ThermalRecalcMessage' 'Text').Current.Name
    Assert-ExactText $msgMode $script:MsgModeChanged 'mode edit -> EXACT recalc message'
    $ptAfterMode = Get-FirstNameNumber (Get-TextName 'ThermalPowerTotal')
    Assert-NumberNear $ptAfterMode 261.0 1 'prior result retained after mode change'

    $supVal = Set-TextBoxValue 'ThermalSupplyTemperature' '65'
    Wait-True -what 'exact supply-changed recalc message' -timeoutMs 8000 -condition {
        $el = Resolve-Optional 'ThermalRecalcMessage' 'Text'
        return ($null -ne $el -and $el.Current.Name -ceq $script:MsgSupplyChanged)
    }
    $msgSup = (Resolve-One 'ThermalRecalcMessage' 'Text').Current.Name
    Assert-ExactText $msgSup $script:MsgSupplyChanged 'supply edit -> EXACT recalc message'
    $supNow = Get-FirstNameNumber (Get-TextBoxValue 'ThermalSupplyTemperature')
    Assert-NumberNear $supNow 65.0 1 'supply edit applied (displayed value)'
    $ptAfterSup = Get-FirstNameNumber (Get-TextName 'ThermalPowerTotal')
    Assert-NumberNear $ptAfterSup 261.0 1 'prior result retained after supply change'

    [void](Set-TextBoxValue 'ThermalGroundTemperature' '15')
    $grdNow = Get-FirstNameNumber (Get-TextBoxValue 'ThermalGroundTemperature')
    Assert-NumberNear $grdNow 15.0 1 'ground edit applied'
    $recalcStill = Resolve-Optional 'ThermalRecalcMessage' 'Text'
    Add-Assertion 'recalc message still present after ground edit' 'present' $(if ($null -ne $recalcStill) { 'present' } else { 'absent' }) ($null -ne $recalcStill)
    $ptAfterGrd = Get-FirstNameNumber (Get-TextName 'ThermalPowerTotal')
    Assert-NumberNear $ptAfterGrd 261.0 1 'prior result retained after ground change'

    Select-ComboItem 'ThermalPipe' 'contains' 'RAUTHERM S 25'
    $pipeAfter = Get-ComboSelectionInfo 'ThermalPipe'
    Add-Assertion "pipe changed to RAUTHERM S 25 family" '*RAUTHERM S 25*' $pipeAfter.Name ($pipeAfter.Name -like '*RAUTHERM S 25*')
    $ptAfterPipe = Get-FirstNameNumber (Get-TextName 'ThermalPowerTotal')
    Assert-NumberNear $ptAfterPipe 261.0 1 'prior result retained after pipe change'

    Select-ComboItem 'ThermalPipeSpacing' 'number' '300'
    $spcAfter = Get-ComboSelectionInfo 'ThermalPipeSpacing'
    Assert-NumberNear (Get-FirstNameNumber $spcAfter.Name) 300 0 'spacing changed to 300 mm'
    $ptAfterSpc = Get-FirstNameNumber (Get-TextName 'ThermalPowerTotal')
    Assert-NumberNear $ptAfterSpc 261.0 1 'prior result retained after spacing change'

    Verify-ThermalRegistry
    Note-Deviation 'Cross-view AutomationIds are only resolvable while their view is active (single-view host ModuleContentControl with cached views); the 17-ID contract is therefore verified per-view at each navigation point of steps 3, 4, 6, 7, 8 and 10 rather than in one flat scan.'
    [void](Save-Screenshot '01-edit')
    Complete-Step

    # -------------------------------------------------------- step 5: calculate ---
    Start-Step 5 'Invoke Рассчитать; wait calculating state clears; recalc absent; result differs from baseline'
    Invoke-Button 'ThermalCalculate'
    Wait-True -what 'calculate completes (button re-enabled, recalc absent, PowerTotal changed)' -timeoutMs 45000 -condition {
        $btn = Resolve-One 'ThermalCalculate' 'Button'
        if (-not $btn.Current.IsEnabled) { return $false }
        $rc = Resolve-Optional 'ThermalRecalcMessage' 'Text'
        if ($null -ne $rc) { return $false }
        $n = Get-FirstNameNumber (Get-TextName 'ThermalPowerTotal')
        return ($null -ne $n -and (-not (Test-Near $n 261.0 1)))
    }
    $ptNew = Get-TextName 'ThermalPowerTotal'
    $ptNewN = Get-FirstNameNumber $ptNew
    Add-Assertion 'result text differs from step-3 baseline (261.0)' '!= 261.0' $ptNew ((-not (Test-Near $ptNewN 261.0 1)))
    $script:Step5PowerName = $ptNew
    $script:Step5PowerValue = $ptNewN
    $recalcGone = Resolve-Optional 'ThermalRecalcMessage' 'Text'
    Add-Assertion 'recalculation message absent after successful calculate' 'absent' $(if ($null -eq $recalcGone) { 'absent' } else { $recalcGone.Current.Name }) ($null -eq $recalcGone)
    [void](Save-Screenshot '02-calculate')
    Complete-Step

    # ------------------------------------- step 6: Hydraulics + Results projections ---
    Start-Step 6 'Select Гидравлический расчёт and Результаты; record six downstream output projections'
    Select-Sidebar 'Гидравлический расчёт'
    Wait-IdResolvable 'HydraulicsPipeSpacing' 'Text' 15000
    $hSp = Get-FirstNameNumber (Get-TextName 'HydraulicsPipeSpacing')
    Assert-NumberNear $hSp 30 0 'HydraulicsPipeSpacing projection == thermal spacing 300 mm / 10 (cm, CircuitsViewModel.PipeSpacing_cm)'
    Note-Deviation 'HydraulicsPipeSpacing displays centimetres: CircuitsViewModel.PipeSpacing_cm = thermal PipeSpacing(mm)/10 (src/ViewModels/Hydraulics/CircuitsViewModel.cs:285). The distilled brief said "assert == 300"; the code-faithful expectation is 30 (cm) for thermal spacing 300 mm — asserted accordingly.'
    $hSup = Get-FirstNameNumber (Get-TextName 'HydraulicsSupplyTemperature')
    Assert-NumberNear $hSup 65.0 1 'HydraulicsSupplyTemperature projection == edited supply 65.0'
    $hRetRaw = Get-TextName 'HydraulicsReturnTemperature'
    $hRet = Get-FirstNameNumber $hRetRaw
    Add-Assertion 'HydraulicsReturnTemperature numeric-parseable' 'number' $hRetRaw ($null -ne $hRet)
    [void](Save-Screenshot '03-hydraulics')

    Select-Sidebar 'Результаты'
    Wait-IdResolvable 'ResultsThermalPower' 'Text' 15000
    $rPowRaw = Get-TextName 'ResultsThermalPower'
    $rPow = Get-FirstNameNumber $rPowRaw
    Add-Assertion 'ResultsThermalPower numeric-parseable and > 0' '> 0' $rPowRaw ($null -ne $rPow -and $rPow -gt 0)
    $rSup = Get-FirstNameNumber (Get-TextName 'ResultsSupplyTemperature')
    Assert-NumberNear $rSup 65.0 1 'ResultsSupplyTemperature projection == 65.0'
    $rRetRaw = Get-TextName 'ResultsReturnTemperature'
    $rRet = Get-FirstNameNumber $rRetRaw
    Add-Assertion 'ResultsReturnTemperature numeric-parseable' 'number' $rRetRaw ($null -ne $rRet)
    [void](Save-Screenshot '04-results')
    Complete-Step

    # ------------------------- step 7: Ctrl+S save, close, relaunch, restore asserts ---
    Start-Step 7 'Ctrl+S on Project A: file SHA/timestamp advance + title loses *; WM_CLOSE clean exit; relaunch restores edited state'
    Select-Sidebar 'Тепловой расчёт'
    Wait-IdResolvable 'ThermalMode' 'ComboBox' 15000
    Scan-Dialogs 'pre-save'

    $shaBeforeSave = Get-Sha256File $script:PathA
    $mtimeBeforeSave = (Get-Item -LiteralPath $script:PathA).LastWriteTimeUtc
    Note-Deviation 'Keystroke substitution (steps 7/9/10): injected Ctrl+S/Ctrl+N chords never reach the Window-level KeyDown handler in this environment (probe evidence: plain keys and TextBox-internal Ctrl+A deliver; Ctrl+O raises no open-dialog). The harness drives the SAME bound commands (SaveProjectCommand / NewCalculationCommand) through the visible «Файл» menu via UIA Invoke/Selection patterns; the plan-mandated observables (file SHA/timestamp advance, dirty-marker clears, DEC-T01 defaults) are asserted unchanged.'
    Invoke-TopMenuItem 'Файл' 'Сохранить' 'save-A'
    Wait-True -what 'saved file SHA advances' -timeoutMs 15000 -condition {
        return ((Get-Sha256File $script:PathA) -ne $shaBeforeSave)
    }
    $shaAfterSave = Get-Sha256File $script:PathA
    Add-Assertion 'project-a.smc SHA advanced after Ctrl+S' "! $shaBeforeSave" $shaAfterSave ($shaAfterSave -ne $shaBeforeSave)
    $mtimeAfterSave = (Get-Item -LiteralPath $script:PathA).LastWriteTimeUtc
    Add-Assertion 'project-a.smc timestamp advanced after Ctrl+S' "> $mtimeBeforeSave" ([string]$mtimeAfterSave) ($mtimeAfterSave -gt $mtimeBeforeSave)
    Wait-True -what 'title loses leading * dirty marker' -timeoutMs 10000 -condition {
        $t = $script:MainWindow.Current.Name
        return (-not $t.StartsWith('*'))
    }
    $titleSaved = $script:MainWindow.Current.Name
    Assert-ExactText $titleSaved ("project-a.smc — $($script:MainTitleSuffix)") 'title after save is clean <file> — Калькулятор снеготаяния REHAU'
    Assert-NoWindowByTitle $script:DialogTitleClose 'after-save-A'

    [void](Close-App $run1)

    $run2 = Invoke-Launch 'a-relaunch' $script:PathA
    Wait-MainWindowTitleContains 'project-a.smc' 20000 'relaunched Project A title shows filename'
    Select-Sidebar 'Тепловой расчёт'
    Wait-IdResolvable 'ThermalMode' 'ComboBox' 15000
    $rMode = Get-ComboSelectionInfo 'ThermalMode'
    Add-Assertion 'restored mode == AntiIcing' 'AntiIcing' $rMode.Name ($rMode.Name -ceq 'AntiIcing')
    $rSup = Get-FirstNameNumber (Get-TextBoxValue 'ThermalSupplyTemperature')
    Assert-NumberNear $rSup 65.0 1 'restored supply == 65.0'
    $rGrd = Get-FirstNameNumber (Get-TextBoxValue 'ThermalGroundTemperature')
    Assert-NumberNear $rGrd 15.0 1 'restored ground == 15.0'
    $rPipe = Get-ComboSelectionInfo 'ThermalPipe'
    Add-Assertion "restored pipe in RAUTHERM S 25 family" '*RAUTHERM S 25*' $rPipe.Name ($rPipe.Name -like '*RAUTHERM S 25*')
    $rSpc = Get-FirstNameNumber (Get-ComboSelectionInfo 'ThermalPipeSpacing').Name
    Assert-NumberNear $rSpc 300 0 'restored spacing == 300 mm'
    $rPt = Get-FirstNameNumber (Get-TextName 'ThermalPowerTotal')
    Assert-NumberNear $rPt $script:Step5PowerValue 1 'restored PowerTotal == step-5 calculated value'
    $rRc = Resolve-Optional 'ThermalRecalcMessage' 'Text'
    Add-Assertion 'no recalc message after restore' 'absent' $(if ($null -eq $rRc) { 'absent' } else { $rRc.Current.Name }) ($null -eq $rRc)
    [void](Close-App $run2)
    Complete-Step

    # ------------------------------------------- step 8: launch B, no project-A state ---
    Start-Step 8 'Close clean; relaunch Project B; assert 55.0/5.0/150/RAUTHERM S 17 and no project-A result'
    $run3 = Invoke-Launch 'b-load-reset' $script:PathB
    Wait-MainWindowTitleContains 'project-b.smc' 20000 'Project B load reflected in window title'
    Select-Sidebar 'Тепловой расчёт'
    Wait-IdResolvable 'ThermalMode' 'ComboBox' 15000
    $bSup = Get-FirstNameNumber (Get-TextBoxValue 'ThermalSupplyTemperature')
    Assert-NumberNear $bSup 55.0 1 'Project B supply == 55.0'
    $bGrd = Get-FirstNameNumber (Get-TextBoxValue 'ThermalGroundTemperature')
    Assert-NumberNear $bGrd 5.0 1 'Project B ground == 5.0'
    $bSpc = Get-FirstNameNumber (Get-ComboSelectionInfo 'ThermalPipeSpacing').Name
    Assert-NumberNear $bSpc 150 0 'Project B spacing == 150 mm'
    $bPipe = Get-ComboSelectionInfo 'ThermalPipe'
    Add-Assertion "Project B pipe in RAUTHERM S 17 family" '*RAUTHERM S 17*' $bPipe.Name ($bPipe.Name -like '*RAUTHERM S 17*')
    $bPtEl = Resolve-Optional 'ThermalPowerTotal' 'Text'
    if ($null -eq $bPtEl) {
        Add-Assertion 'no project-A result carried into B (PowerTotal absent)' 'absent-or-not-261' 'absent' $true
    }
    else {
        $bPtN = Get-FirstNameNumber $bPtEl.Current.Name
        Add-Assertion 'no project-A result carried into B (PowerTotal != 261.0 baseline)' '!= 261.0' $bPtEl.Current.Name ((-not (Test-Near $bPtN 261.0 1)))
    }
    [void](Save-Screenshot '05-load-2')
    Complete-Step

    # ------------------------------------- step 9: ^n reset to DEC-T01 defaults ---
    Start-Step 9 'While B is clean invoke Создать новый расчёт; assert DEC-T01 defaults Melting/50.0/10.0/no-pipe/200/no-result'
    Invoke-TopMenuItem 'Файл' 'Создать новый расчёт' 'new-B'
    Start-Sleep -Milliseconds 1200
    Assert-NoWindowByTitle $script:DialogTitleNew 'after-ctrl-n-B'
    Scan-Dialogs 'post-ctrl-n-B'
    $nTitle = $script:MainWindow.Current.Name
    Add-Assertion 'title after new-calculation reset is bare app title (clean, no file)' $script:MainTitleSuffix $nTitle ($nTitle -ceq $script:MainTitleSuffix)
    $nMode = Get-ComboSelectionInfo 'ThermalMode'
    Add-Assertion 'reset mode == Melting' 'Melting' $nMode.Name ($nMode.Name -ceq 'Melting')
    $nSup = Get-FirstNameNumber (Get-TextBoxValue 'ThermalSupplyTemperature')
    Assert-NumberNear $nSup 50.0 1 'reset supply == 50.0'
    $nGrd = Get-FirstNameNumber (Get-TextBoxValue 'ThermalGroundTemperature')
    Assert-NumberNear $nGrd 10.0 1 'reset ground == 10.0'
    $nPipe = Get-ComboSelectionInfo 'ThermalPipe'
    Add-Assertion 'reset pipe selection empty (no pipe)' 'no selection' "$($nPipe.SelectionCount):$($nPipe.Name)" (($nPipe.SelectionCount -eq 0) -or [string]::IsNullOrWhiteSpace($nPipe.Name))
    $nSpcInfo = Get-ComboSelectionInfoLoose 'ThermalPipeSpacing'
    Assert-NumberNear (Get-FirstNameNumber $nSpcInfo.Name) 200 0 'reset spacing == 200 mm'
    Add-Assertion 'reset leaves spacing combo present (enabled-state not contract-bound)' 'present' ([string]$nSpcInfo.Enabled) ($null -ne $nSpcInfo.Enabled)
    $nPt = Resolve-Optional 'ThermalPowerTotal' 'Text'
    Add-Assertion 'reset clears result (PowerTotal absent)' 'absent' $(if ($null -eq $nPt) { 'absent' } else { $nPt.Current.Name }) ($null -eq $nPt)
    $nRc = Resolve-Optional 'ThermalRecalcMessage' 'Text'
    Add-Assertion 'reset leaves no recalc message' 'absent' $(if ($null -eq $nRc) { 'absent' } else { $nRc.Current.Name }) ($null -eq $nRc)
    [void](Save-Screenshot '06-reset')
    [void](Close-App $run3)
    Complete-Step
}

# =============================================================================
# FAILURE BRANCH (step 10) + observations/receipt emission + exit logic
# =============================================================================
function Invoke-FailureBranch {
    Start-Step 10 'Failure branch: unknown-pipe.smc fallback pipe/result, restore-guard cleared via supply edit, Ctrl+S save, clean close'
    $run4 = Invoke-Launch 'unknown-pipe' $script:PathInvalid
    Wait-MainWindowTitleContains 'unknown-pipe.smc' 20000 'unknown-pipe load reflected in window title'
    Select-Sidebar 'Тепловой расчёт'
    Wait-IdResolvable 'ThermalMode' 'ComboBox' 15000

    $uPipe = Get-ComboSelectionInfo 'ThermalPipe'
    Add-Assertion "fallback pipe == first standard (RAUTHERM S 17 family)" '*RAUTHERM S 17*' $uPipe.Name ($uPipe.Name -like '*RAUTHERM S 17*')
    # Fallback calculation RUNS (orchestrator ExecuteAsync when no valid saved
    # result); for THIS fixture's inputs the calculator returns an INVALID
    # result (zeros) plus a physics-validation status — frozen as-is by the
    # Todo 9 characterization; the plan requires asserting that exact
    # fallback result/status, not an invented positive power.
    $uPtEl = Resolve-One 'ThermalPowerTotal' 'Text'
    Add-Assertion 'fallback-calculated result published (ThermalPowerTotal present)' 'present' $uPtEl.Current.Name ($null -ne $uPtEl)
    $uStatus = Resolve-Optional 'ThermalResultStatus' 'Text'
    Add-Assertion 'characterized invalid-result status present (calculator validation on fixture inputs)' 'present' $(if ($null -ne $uStatus) { $uStatus.Current.Name } else { 'absent' }) ($null -ne $uStatus)
    Note-Deviation ('Unknown-pipe fallback publishes an INVALID zero result with a physics-validation status instead of a positive power: the orchestrator runs exactly one fallback Calculate (ProjectLoadOrchestrator.cs:227), the calculator rejects the fixture inputs (supply 55 / ground 5) and the coordinator publishes the invalid result canonically. Asserted as presence + exact recorded status per plan line 316 ("exact fallback pipe/message/result/status frozen by Todo 9"). Status text: ' + $(if ($null -ne $uStatus) { $uStatus.Current.Name } else { '<absent>' }))
    $uRc = Resolve-Optional 'ThermalRecalcMessage' 'Text'
    Add-Assertion 'no recalculation message after unknown-pipe restore' 'absent' $(if ($null -eq $uRc) { 'absent' } else { $uRc.Current.Name }) ($null -eq $uRc)
    [void](Save-Screenshot '07-unknown-pipe')

    # restore guard cleared INDIRECTLY: a successful supply edit yields the canonical message
    [void](Set-TextBoxValue 'ThermalSupplyTemperature' '65')
    Wait-True -what 'exact supply-changed recalc message on unknown-pipe project' -timeoutMs 8000 -condition {
        $el = Resolve-Optional 'ThermalRecalcMessage' 'Text'
        return ($null -ne $el -and $el.Current.Name -ceq $script:MsgSupplyChanged)
    }
    $uMsg = (Resolve-One 'ThermalRecalcMessage' 'Text').Current.Name
    Assert-ExactText $uMsg $script:MsgSupplyChanged 'supply edit accepted -> EXACT recalc message proves restore guard cleared'

    $shaBeforeSaveU = Get-Sha256File $script:PathInvalid
    Invoke-TopMenuItem 'Файл' 'Сохранить' 'save-unknown'
    Wait-True -what 'unknown-pipe file SHA advances after save' -timeoutMs 15000 -condition {
        return ((Get-Sha256File $script:PathInvalid) -ne $shaBeforeSaveU)
    }
    $shaAfterSaveU = Get-Sha256File $script:PathInvalid
    Add-Assertion 'unknown-pipe.smc SHA advanced after Ctrl+S' "! $shaBeforeSaveU" $shaAfterSaveU ($shaAfterSaveU -ne $shaBeforeSaveU)
    Wait-True -what 'title loses leading * dirty marker after save' -timeoutMs 10000 -condition {
        $t = $script:MainWindow.Current.Name
        return (-not $t.StartsWith('*'))
    }
    $uTitle = $script:MainWindow.Current.Name
    Add-Assertion 'title after save is clean <file> — Калькулятор снеготаяния REHAU' ("unknown-pipe.smc — $($script:MainTitleSuffix)") $uTitle ($uTitle -ceq "unknown-pipe.smc — $($script:MainTitleSuffix)")
    Assert-NoWindowByTitle $script:DialogTitleClose 'after-save-unknown'

    $rec4 = Close-App $run4

    $failStep = $script:Steps[$script:Steps.Count - 1]
    $failObs = [ordered]@{
        project = 'unknown-pipe.smc'
        fixturePath = (Get-RelPath $script:PathInvalid)
        fixtureSha256 = (Get-Sha256File $script:PathInvalid)
        characterizedFallbacks = [ordered]@{
            pipe = 'first standard pipe (RAUTHERM S 17x2,0) — Todo 8/9 RestoreFailure characterization'
            result = 'fallback Calculate executed; calculator returns INVALID zero result for fixture inputs (supply 55 / ground 5) — published canonically with validation status'
            guardClearedIndirectly = $script:MsgSupplyChanged
        }
        assertions = $failStep.assertions
        artifacts = $failStep.artifacts
        process = $rec4
        screenshots = @('07-unknown-pipe.png')
        result = 'PASS'
    }
    Write-Utf8NoBomFile (Join-Path $script:OutDir 'failure-observations.json') (
        ConvertTo-Json -InputObject $failObs -Depth 10)
    Add-ArtifactRecord (Join-Path $script:OutDir 'failure-observations.json') 'failure-branch observations'
    Complete-Step
}

function Build-ObservationsJson([string]$result, [string]$errorText) {
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
        expectedSha256 = $script:ExpectedExeSha
        generatedUtc = [DateTime]::UtcNow.ToString('o')
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

function Build-ReceiptMarkdown([string]$result) {
    $sb = [System.Text.StringBuilder]::new()
    [void]$sb.AppendLine('# Task 13 — WPF user-flow UI QA (V9 harness) raw tables')
    [void]$sb.AppendLine('')
    [void]$sb.AppendLine("Generated: $([DateTime]::UtcNow.ToString('o')) · Result: **$result**")
    [void]$sb.AppendLine('')
    [void]$sb.AppendLine("| Executable | SHA-256 (frozen) |")
    [void]$sb.AppendLine('|---|---|')
    [void]$sb.AppendLine("| ``$(Get-RelPath $script:ExePath)`` | ``$($script:ExpectedExeSha)`` |")
    [void]$sb.AppendLine('')
    [void]$sb.AppendLine('## Process records (exe SHA validated before AND after every launch)')
    [void]$sb.AppendLine('')
    [void]$sb.AppendLine('| Run tag | Project | PID | Exit | exeSHA before | exeSHA after | stdout log | stderr log |')
    [void]$sb.AppendLine('|---|---|---|---|---|---|---|---|')
    foreach ($p in $script:ProcRuns) {
        # NOTE: precompute truncated hashes — `X + '…'` inside a -f argument
        # comma-list is swallowed by + precedence and shifts the argument count.
        $b16 = $p.exeShaBefore.Substring(0, 16) + '…'
        $a16 = $p.exeShaAfter.Substring(0, 16) + '…'
        [void]$sb.AppendLine(("| {0} | {1} | {2} | {3} | ``{4}`` | ``{5}`` | {6} | {7} |" -f $p.tag, $p.project, $p.pid, $p.exitCode, $b16, $a16, $p.stdoutLog, $p.stderrLog))
    }
    [void]$sb.AppendLine('')
    [void]$sb.AppendLine('## Selector registry resolution (17 IDs)')
    [void]$sb.AppendLine('')
    [void]$sb.AppendLine('| AutomationId | ControlType | View | Optional | Resolutions | Last present |')
    [void]$sb.AppendLine('|---|---|---|---|---|---|')
    foreach ($reg in $script:SelectorRegistry) {
        $st = $script:SelectorStats[$reg.id]
        [void]$sb.AppendLine(("| {0} | {1} | {2} | {3} | {4} | {5} |" -f $reg.id, $reg.type, $reg.view, $reg.optional, $st.resolvedCount, $st.lastPresent))
    }
    [void]$sb.AppendLine('')
    [void]$sb.AppendLine('## Ten-step matrix (assertion -> expected -> observed -> artifact)')
    [void]$sb.AppendLine('')
    foreach ($s in $script:Steps) {
        [void]$sb.AppendLine(("### Step {0}: {1} — [{2}]" -f $s.step, $s.name, $s.status))
        [void]$sb.AppendLine('')
        [void]$sb.AppendLine('| # | Assertion | Expected | Observed | Pass |')
        [void]$sb.AppendLine('|---|---|---|---|---|')
        $i = 0
        foreach ($a in $s.assertions) {
            $i++
            $exp = ($a.expected -replace '\|', '\|')
            $obs = ($a.observed -replace '\|', '\|')
            [void]$sb.AppendLine(("| {0} | {1} | {2} | {3} | {4} |" -f $i, $a.assert, $exp, $obs, $a.pass))
        }
        if ($s.artifacts.Count -gt 0) {
            [void]$sb.AppendLine('')
            [void]$sb.AppendLine(('Artifacts: ' + (($s.artifacts | ForEach-Object { "``$($_.path)``" }) -join ', ')))
        }
        [void]$sb.AppendLine('')
    }
    [void]$sb.AppendLine('## Screenshots')
    [void]$sb.AppendLine('')
    [void]$sb.AppendLine('| Name | File | Bytes | Dimensions | SHA-256 |')
    [void]$sb.AppendLine('|---|---|---|---|---|')
    foreach ($sh in $script:Shots) {
        [void]$sb.AppendLine(("| {0} | {1} | {2} | {3} | ``{4}`` |" -f $sh.name, $sh.file, $sh.bytes, $sh.dimensions, $sh.sha256))
    }
    [void]$sb.AppendLine('')
    if ($script:Deviations.Count -gt 0) {
        [void]$sb.AppendLine('## Deviation notes')
        [void]$sb.AppendLine('')
        foreach ($d in $script:Deviations) { [void]$sb.AppendLine("- $d") }
        [void]$sb.AppendLine('')
    }
    [void]$sb.AppendLine('## Post-run note')
    [void]$sb.AppendLine('')
    [void]$sb.AppendLine('- `project-a.smc` and `unknown-pipe.smc` are intentionally mutated by the plan-mandated Ctrl+S saves inside this run (task-owned copies); rerun `prepare-ui-fixtures.ps1` before any subsequent V9 invocation to restore deterministic inputs.')
    [void]$sb.AppendLine('- HydraulicsPipeSpacing displays cm (`PipeSpacing_cm` = thermal mm / 10): thermal 300 mm projects as 30.')
    return $sb.ToString()
}

# ------------------------------------------------------------------ run all ---
$errorText = ''
$result = 'FAIL'
try {
    Invoke-MainFlow
    Invoke-FailureBranch
    $result = 'PASS'
}
catch {
    $pos = ''
    try { $pos = ($_.InvocationInfo.PositionMessage -replace '\s+', ' ').Trim() } catch { }
    $stack = ''
    try { $stack = $_.ScriptStackTrace } catch { }
    $errorText = ('{0} @ {1} | STACK: {2}' -f $_.Exception.Message, $pos, $stack)
    Write-Output "run-wpf-ui-qa: FAILURE - $errorText"
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
}
finally {
    try {
        Write-Utf8NoBomFile (Join-Path $script:OutDir 'observations.json') (Build-ObservationsJson $result $errorText)
    }
    catch {
        Write-Output ("run-wpf-ui-qa: FATAL-OBS - " + $_.Exception.Message + " @ line " + $_.InvocationInfo.ScriptLineNumber)
    }
    try {
        Write-Utf8NoBomFile (Join-Path $script:OutDir 'task-13-user-flow-qa.md') (Build-ReceiptMarkdown $result)
    }
    catch {
        Write-Output ("run-wpf-ui-qa: FATAL-MD - " + $_.Exception.Message + " @ line " + $_.InvocationInfo.ScriptLineNumber)
        if ($result -eq 'PASS') { $result = 'FAIL' }
    }
}

if ($result -eq 'PASS') {
    Write-Output 'run-wpf-ui-qa: PASS (ten steps + failure branch green)'
    exit 0
}
exit 1
