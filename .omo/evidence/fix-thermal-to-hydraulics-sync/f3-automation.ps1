Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms

$evidenceDir = "D:\IA\ace\.omo\evidence\fix-thermal-to-hydraulics-sync"
$logLines = [System.Collections.Generic.List[string]]::new()

function Log($msg) {
    $ts = Get-Date -Format "yyyy-MM-dd HH:mm:ss.fff"
    $line = "$ts  $msg"
    $logLines.Add($line)
    Write-Host $line
}

function TakeScreenshot($path) {
    $screen = [System.Windows.Forms.Screen]::PrimaryScreen
    $bmp = New-Object System.Drawing.Bitmap($screen.Bounds.Width, $screen.Bounds.Height)
    $gfx = [System.Drawing.Graphics]::FromImage($bmp)
    $gfx.CopyFromScreen($screen.Bounds.Location, [System.Drawing.Point]::Empty, $screen.Bounds.Size)
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $gfx.Dispose()
    $bmp.Dispose()
    Log "Screenshot saved=$([System.IO.File]::Exists($path)) path=$path"
}

function Focus-Element($el) {
    try {
        $fp = $el.GetCurrentPattern([System.Windows.Automation.FocusPattern]::Pattern) -as [System.Windows.Automation.FocusPattern]
        if ($fp -ne $null) { $fp.SetFocus() }
    } catch {}
    Start-Sleep -Milliseconds 200
}

function Select-MenuItem($app, $menuText) {
    $menuItems = $app.FindAll([System.Windows.Automation.TreeScope]::Descendants,
        (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::ListItem)))
    foreach ($mi in $menuItems) {
        $texts = $mi.FindAll([System.Windows.Automation.TreeScope]::Descendants,
            (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Text)))
        foreach ($t in $texts) {
            if ($t.Current.Name -eq $menuText) {
                $sp = $mi.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern) -as [System.Windows.Automation.SelectionItemPattern]
                if ($sp -ne $null) { $sp.Select() }
                Log "Selected menu: $menuText"
                Start-Sleep -Milliseconds 800
                return
            }
        }
    }
    Log "WARNING: menu '$menuText' not found"
}

function Click-Button($app, $buttonText, $minWidth=0) {
    $buttons = $app.FindAll([System.Windows.Automation.TreeScope]::Descendants,
        (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Button)))
    foreach ($btn in $buttons) {
        $texts = $btn.FindAll([System.Windows.Automation.TreeScope]::Descendants,
            (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Text)))
        foreach ($t in $texts) {
            if ($t.Current.Name -eq $buttonText) {
                $rect = $btn.Current.BoundingRectangle
                if ($rect.Width -gt $minWidth) {
                    $ip = $btn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern) -as [System.Windows.Automation.InvokePattern]
                    if ($ip -ne $null) {
                        $ip.Invoke()
                        Log "Clicked '$buttonText' at $($rect.X),$($rect.Y)"
                        return $true
                    }
                }
            }
        }
    }
    Log "WARNING: button '$buttonText' not found"
    return $false
}

function Select-ComboByKeyboard($combo, $downPresses, $label) {
    Focus-Element $combo
    Start-Sleep -Milliseconds 200
    # Alt+Down to expand
    [System.Windows.Forms.SendKeys]::SendWait("%{DOWN}")
    Start-Sleep -Milliseconds 500
    # Press Down arrow N times
    for ($i = 0; $i -lt $downPresses; $i++) {
        [System.Windows.Forms.SendKeys]::SendWait("{DOWN}")
        Start-Sleep -Milliseconds 100
    }
    Start-Sleep -Milliseconds 200
    # Enter to select
    [System.Windows.Forms.SendKeys]::SendWait("{ENTER}")
    Start-Sleep -Milliseconds 500
    Log "Selected $label via keyboard ($downPresses down presses)"
}

function Read-TextValueAfterLabel($allTexts, $label) {
    for ($i = 0; $i -lt $allTexts.Count; $i++) {
        if ($allTexts[$i].Name -eq $label) {
            $labelRect = $allTexts[$i].Rect
            for ($j = $i + 1; $j -lt [Math]::Min($i + 5, $allTexts.Count); $j++) {
                $valRect = $allTexts[$j].Rect
                if ([Math]::Abs($valRect.Y - $labelRect.Y) -lt 10 -and $valRect.X -gt $labelRect.X) {
                    return $allTexts[$j].Name
                }
            }
        }
    }
    return "N/A"
}

function Get-AllTextValues($app) {
    $texts = $app.FindAll([System.Windows.Automation.TreeScope]::Descendants,
        (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Text)))
    $allTexts = @()
    foreach ($t in $texts) {
        $allTexts += [PSCustomObject]@{ Name = $t.Current.Name; Rect = $t.Current.BoundingRectangle }
    }
    $result = @{}
    $result["T_supply"] = Read-TextValueAfterLabel $allTexts "T_" + [char]0x043F + [char]0x043E + [char]0x0434 + [char]0x0430 + [char]0x0447 + [char]0x0438 + ":"
    $result["T_return"] = Read-TextValueAfterLabel $allTexts "T_" + [char]0x043E + [char]0x0431 + [char]0x0440 + [char]0x0430 + [char]0x0442 + [char]0x043A + [char]0x0438 + ":"
    return $result
}

# --- Launch app ---
$exePath = "D:\IA\ace\src\bin\Debug\net8.0-windows\win-x64\SnowMeltingCalculator.exe"
Log "Launching $exePath"
$proc = Start-Process -FilePath $exePath -PassThru
$proc.WaitForInputIdle(10000)
Start-Sleep -Milliseconds 2500

$rootEl = [System.Windows.Automation.AutomationElement]::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
$app = $rootEl.FindFirst([System.Windows.Automation.TreeScope]::Children, $cond)
if ($app -eq $null) { Log "ERROR: app element not found"; exit 1 }
Log "App found, handle=$($proc.MainWindowHandle)"

$winPattern = $app.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern) -as [System.Windows.Automation.WindowPattern]
if ($winPattern -ne $null) { $winPattern.SetWindowVisualState([System.Windows.Automation.WindowVisualState]::Normal) }
Start-Sleep -Milliseconds 1000

# --- Step 1: Select Climate tab and choose Moscow ---
Select-MenuItem $app ([char]0x041A + [char]0x043B + [char]0x0438 + [char]0x043C + [char]0x0430 + [char]0x0442)

# Find city search box
$citySearch = $null
$edits = $app.FindAll([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, "SearchTextBox")))
if ($edits.Count -gt 0) { $citySearch = $edits[0] }

if ($citySearch -ne $null) {
    Focus-Element $citySearch
    $vp = $citySearch.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern) -as [System.Windows.Automation.ValuePattern]
    if ($vp -ne $null) { $vp.SetValue("") }
    Start-Sleep -Milliseconds 200
    # Type Moscow using SendKeys for reliability
    [System.Windows.Forms.SendKeys]::SendWait("{BACKSPACE 20}")
    Start-Sleep -Milliseconds 100
    # Use clipboard to paste Cyrillic text
    [System.Windows.Forms.Clipboard]::SetText([string]([char]0x041C + [char]0x043E + [char]0x0441 + [char]0x043A + [char]0x0432 + [char]0x0430))
    [System.Windows.Forms.SendKeys]::SendWait("^v")
    Log "Pasted city name"
}
Start-Sleep -Milliseconds 2500

# Click the first suggestion in the dropdown
$listItems = $app.FindAll([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::ListItem)))
$clicked = $false
foreach ($li in $listItems) {
    $rect = $li.Current.BoundingRectangle
    if ($rect.Width -gt 200 -and $rect.Y -gt 500) {
        try {
            $tp = $li.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern) -as [System.Windows.Automation.InvokePattern]
            if ($tp -ne $null) { $tp.Invoke(); $clicked = $true; Log "Clicked suggestion"; break }
        } catch {}
        if (-not $clicked) {
            # Try mouse click via coordinates
            $cx = [int]($rect.X + $rect.Width / 2)
            $cy = [int]($rect.Y + $rect.Height / 2)
            Add-Type @"
using System;
using System.Runtime.InteropServices;
public class MouseHelper {
    [DllImport("user32.dll")]
    public static extern void SetCursorPos(int x, int y);
    [DllImport("user32.dll")]
    public static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);
    public const int MOUSEEVENTF_LEFTDOWN = 0x0002;
    public const int MOUSEEVENTF_LEFTUP = 0x0004;
    public static void Click(int x, int y) {
        SetCursorPos(x, y);
        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
    }
}
"@
            [MouseHelper]::Click($cx, $cy)
            $clicked = $true
            Log "Mouse-clicked suggestion at $cx,$cy"
            break
        }
    }
}
if (-not $clicked) { Log "WARNING: no suggestion clicked" }
Start-Sleep -Milliseconds 1500

# --- Step 2: Navigate to Thermal tab ---
Select-MenuItem $app ([char]0x0422 + [char]0x0435 + [char]0x043F + [char]0x043B + [char]0x043E + [char]0x0432 + [char]0x043E + [char]0x0439 + " " + [char]0x0440 + [char]0x0430 + [char]0x0441 + [char]0x0447 + [char]0x0451 + [char]0x0442)
Start-Sleep -Milliseconds 1000

# --- Find combos in thermal tab ---
$combos = $app.FindAll([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::ComboBox)))
Log "Combos found=$($combos.Count)"
for ($i = 0; $i -lt $combos.Count; $i++) {
    Log "  combo[$i] IsEnabled=$($combos[$i].Current.IsEnabled) Rect=$($combos[$i].Current.BoundingRectangle)"
}

# Pipe combo: combo[1] (index 1) based on previous run (combo[0] is mode, combo[1] is pipe, combo[2] is spacing)
# Pipe combo should be at Y~920 based on previous run
$pipeCombo = $null
$spacingCombo = $null
for ($i = 0; $i -lt $combos.Count; $i++) {
    $c = $combos[$i]
    $rect = $c.Current.BoundingRectangle
    if ($c.Current.IsEnabled -and $rect.Y -gt 800 -and $rect.Y -lt 1000 -and $rect.Width -gt 300) {
        $pipeCombo = $c
        Log "Pipe combo identified at index $i, Y=$($rect.Y)"
    }
}

# Select pipe via keyboard: Alt+Down, Down (to select 2nd item = RAUTHERM S 20x2,0), Enter
if ($pipeCombo -ne $null) {
    Select-ComboByKeyboard $pipeCombo 1 "pipe (2nd item = RAUTHERM S 20x2,0)"
}
Start-Sleep -Milliseconds 800

# Now find spacing combo (should be enabled after pipe selection)
$combos = $app.FindAll([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::ComboBox)))
for ($i = 0; $i -lt $combos.Count; $i++) {
    $c = $combos[$i]
    $rect = $c.Current.BoundingRectangle
    if ($c.Current.IsEnabled -and $rect.Y -gt 800 -and $rect.Width -lt 250) {
        $spacingCombo = $c
        Log "Spacing combo identified at index $i, Y=$($rect.Y), W=$($rect.Width)"
    }
}

# Select spacing = 200mm (3rd item: 150, 175, 200 -> index 2)
if ($spacingCombo -ne $null) {
    Select-ComboByKeyboard $spacingCombo 2 "spacing (200mm)"
}
Start-Sleep -Milliseconds 500

# --- Set supply temperature to 55 ---
$edits = $app.FindAll([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Edit)))
Log "Edits found=$($edits.Count)"
$supplyEdit = $null
for ($i = 0; $i -lt $edits.Count; $i++) {
    $e = $edits[$i]
    $rect = $e.Current.BoundingRectangle
    $vp = $e.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern) -as [System.Windows.Automation.ValuePattern]
    $val = if ($vp -ne $null) { $vp.Current.Value } else { "?" }
    Log "  edit[$i] Value='$val' Rect=$rect"
    if ($rect.Y -gt 700 -and $rect.Y -lt 850 -and $rect.X -gt 1050 -and $rect.X -lt 1300) {
        $supplyEdit = $e
    }
}

if ($supplyEdit -ne $null) {
    Focus-Element $supplyEdit
    Start-Sleep -Milliseconds 200
    $vp = $supplyEdit.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern) -as [System.Windows.Automation.ValuePattern]
    if ($vp -ne $null) {
        $vp.SetValue("55")
        Log "Set supply temp to 55 via ValuePattern"
    }
    Start-Sleep -Milliseconds 200
    [System.Windows.Forms.SendKeys]::SendWait("{TAB}")
    Start-Sleep -Milliseconds 300
}

# Verify supply temp was set
$edits = $app.FindAll([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Edit)))
for ($i = 0; $i -lt $edits.Count; $i++) {
    $e = $edits[$i]
    $rect = $e.Current.BoundingRectangle
    if ($rect.Y -gt 700 -and $rect.Y -lt 850 -and $rect.X -gt 1050 -and $rect.X -lt 1300) {
        $vp = $e.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern) -as [System.Windows.Automation.ValuePattern]
        if ($vp -ne $null) { Log "Supply temp confirmed: '$($vp.Current.Value)'" }
    }
}

# --- Click Calculate ---
Start-Sleep -Milliseconds 500
$calcBtn = [char]0x0420 + [char]0x0430 + [char]0x0441 + [char]0x0441 + [char]0x0447 + [char]0x0438 + [char]0x0442 + [char]0x0430 + [char]0x0442 + [char]0x044C
Click-Button $app $calcBtn 80
Start-Sleep -Milliseconds 3000

# --- Step 3: Navigate to Hydraulics and read values ---
$hydrText = [char]0x0413 + [char]0x0438 + [char]0x0434 + [char]0x0440 + [char]0x0430 + [char]0x0432 + [char]0x043B + [char]0x0438 + [char]0x0447 + [char]0x0435 + [char]0x0441 + [char]0x043A + [char]0x0438 + [char]0x0439 + " " + [char]0x0440 + [char]0x0430 + [char]0x0441 + [char]0x0447 + [char]0x0451 + [char]0x0442
Select-MenuItem $app $hydrText
Start-Sleep -Milliseconds 2000

# Read ALL text elements for debugging
Log "--- FIRST PASS: All text elements in hydraulics ---"
$texts = $app.FindAll([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Text)))
$allTexts1 = @()
foreach ($t in $texts) {
    $name = $t.Current.Name
    $rect = $t.Current.BoundingRectangle
    $allTexts1 += [PSCustomObject]@{ Name = $name; Rect = $rect }
    # Only log relevant ones (in the content area)
    if ($rect.X -gt 1050 -and $rect.Y -gt 450 -and $rect.Y -lt 800) {
        Log "  TEXT: '$name' at $($rect.X),$($rect.Y)"
    }
}

# Read specific values
$labelSupply = "T_" + [char]0x043F + [char]0x043E + [char]0x0434 + [char]0x0430 + [char]0x0447 + [char]0x0438 + ":"
$labelReturn = "T_" + [char]0x043E + [char]0x0431 + [char]0x0440 + [char]0x0430 + [char]0x0442 + [char]0x043A + [char]0x0438 + ":"
$labelPipe = [char]0x0422 + [char]0x0440 + [char]0x0443 + [char]0x0431 + [char]0x0430 + ":"
$labelDnar = "D_" + [char]0x043D + [char]0x0430 + [char]0x0440 + ":"
$labelDvn = "D_" + [char]0x0432 + [char]0x043D + ":"
$labelQup = "q_" + [char]0x0432 + [char]0x0432 + [char]0x0435 + [char]0x0440 + [char]0x0445 + ":"
$labelQdown = "q_" + [char]0x0432 + [char]0x043D + [char]0x0438 + [char]0x0437 + ":"
$labelStep = [char]0x0428 + [char]0x0430 + [char]0x0433 + ":"

$pass1 = @{}
$pass1["T_supply"] = Read-TextValueAfterLabel $allTexts1 $labelSupply
$pass1["T_return"] = Read-TextValueAfterLabel $allTexts1 $labelReturn
$pass1["Pipe"] = Read-TextValueAfterLabel $allTexts1 $labelPipe
$pass1["D_nar"] = Read-TextValueAfterLabel $allTexts1 $labelDnar
$pass1["D_vn"] = Read-TextValueAfterLabel $allTexts1 $labelDvn
$pass1["q_up"] = Read-TextValueAfterLabel $allTexts1 $labelQup
$pass1["q_down"] = Read-TextValueAfterLabel $allTexts1 $labelQdown
$pass1["Step"] = Read-TextValueAfterLabel $allTexts1 $labelStep

Log "--- FIRST PASS values ---"
foreach ($key in $pass1.Keys | Sort-Object) { Log "  $key = $($pass1[$key])" }

TakeScreenshot "$evidenceDir\f3-manual-qa-1.png"

# --- Step 4: Go back to thermal, change supply temp to 65 ---
$thermalText = [char]0x0422 + [char]0x0435 + [char]0x043F + [char]0x043B + [char]0x043E + [char]0x0432 + [char]0x043E + [char]0x0439 + " " + [char]0x0440 + [char]0x0430 + [char]0x0441 + [char]0x0447 + [char]0x0451 + [char]0x0442
Select-MenuItem $app $thermalText
Start-Sleep -Milliseconds 1000

# Find supply temp edit again
$edits = $app.FindAll([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Edit)))
$supplyEdit2 = $null
for ($i = 0; $i -lt $edits.Count; $i++) {
    $e = $edits[$i]
    $rect = $e.Current.BoundingRectangle
    if ($rect.Y -gt 700 -and $rect.Y -lt 850 -and $rect.X -gt 1050 -and $rect.X -lt 1300) {
        $supplyEdit2 = $e
        $vp = $e.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern) -as [System.Windows.Automation.ValuePattern]
        if ($vp -ne $null) { Log "Current supply temp before change: '$($vp.Current.Value)'" }
    }
}

if ($supplyEdit2 -ne $null) {
    Focus-Element $supplyEdit2
    Start-Sleep -Milliseconds 200
    $vp = $supplyEdit2.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern) -as [System.Windows.Automation.ValuePattern]
    if ($vp -ne $null) {
        $vp.SetValue("65")
        Log "Set supply temp to 65"
    }
    Start-Sleep -Milliseconds 200
    [System.Windows.Forms.SendKeys]::SendWait("{TAB}")
    Start-Sleep -Milliseconds 300
}

# Click Calculate again
Start-Sleep -Milliseconds 500
Click-Button $app $calcBtn 80
Start-Sleep -Milliseconds 3000

# --- Step 5: Navigate to Hydraulics and read values again ---
Select-MenuItem $app $hydrText
Start-Sleep -Milliseconds 2000

Log "--- SECOND PASS: All text elements in hydraulics ---"
$texts = $app.FindAll([System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Text)))
$allTexts2 = @()
foreach ($t in $texts) {
    $name = $t.Current.Name
    $rect = $t.Current.BoundingRectangle
    $allTexts2 += [PSCustomObject]@{ Name = $name; Rect = $rect }
    if ($rect.X -gt 1050 -and $rect.Y -gt 450 -and $rect.Y -lt 800) {
        Log "  TEXT: '$name' at $($rect.X),$($rect.Y)"
    }
}

$pass2 = @{}
$pass2["T_supply"] = Read-TextValueAfterLabel $allTexts2 $labelSupply
$pass2["T_return"] = Read-TextValueAfterLabel $allTexts2 $labelReturn
$pass2["Pipe"] = Read-TextValueAfterLabel $allTexts2 $labelPipe
$pass2["D_nar"] = Read-TextValueAfterLabel $allTexts2 $labelDnar
$pass2["D_vn"] = Read-TextValueAfterLabel $allTexts2 $labelDvn
$pass2["q_up"] = Read-TextValueAfterLabel $allTexts2 $labelQup
$pass2["q_down"] = Read-TextValueAfterLabel $allTexts2 $labelQdown
$pass2["Step"] = Read-TextValueAfterLabel $allTexts2 $labelStep

Log "--- SECOND PASS values ---"
foreach ($key in $pass2.Keys | Sort-Object) { Log "  $key = $($pass2[$key])" }

TakeScreenshot "$evidenceDir\f3-manual-qa-2.png"

# --- Comparison ---
Log "--- COMPARISON ---"
$changed = 0
$total = 0
foreach ($key in @("T_supply", "T_return", "q_up", "q_down", "Step", "Pipe", "D_nar", "D_vn")) {
    $v1 = $pass1[$key]
    $v2 = $pass2[$key]
    $total++
    if ($v1 -ne $v2) {
        Log "  CHANGED: $key '$v1' -> '$v2'"
        $changed++
    } else {
        Log "  SAME:    $key '$v1'"
    }
}
Log "Changed=$changed / $total"

$supplyChanged = ($pass1["T_supply"] -ne $pass2["T_supply"])
$powerChanged = ($pass1["q_up"] -ne $pass2["q_up"])
Log "Supply temperature changed: $supplyChanged"
Log "PowerUp changed: $powerChanged"

Log "F3 MANUAL QA COMPLETE"

# Stop app
try { $proc.Kill() } catch {}
Log "Stopped app process"

# Write log
$logPath = "$evidenceDir\f3-manual-qa.txt"
$logLines | Out-File -FilePath $logPath -Encoding UTF8
Write-Host "Log written to $logPath"
