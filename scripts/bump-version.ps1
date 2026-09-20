# Поднимает версию релиза одной командой: канон — <Version> в
# src/SnowMeltingCalculator.csproj, скрипт разносит её по литеральным
# местам гейта VersionSyncTests и вставляет заготовку секции в CHANGELOG.md.
#
# Использование:
#   .\scripts\bump-version.ps1 -Version 1.9.0    # явная версия X.Y.Z
#   .\scripts\bump-version.ps1 -Minor            # 1.8.0 -> 1.9.0
#   .\scripts\bump-version.ps1 -Patch            # 1.8.0 -> 1.8.1
#   .\scripts\bump-version.ps1 -Major            # 1.8.0 -> 2.0.0
#
# Что правится (места из tests/.../Architecture/VersionSyncTests.cs):
#   1    csproj <Version> (канон)
#   2    installer/SnowMeltingCalculator.iss — шапочный комментарий
#        (литералы «v1.8.0» и «1.8.0.0»)
#   5-6  INSTALL.md — имя сетапа, ровно 2 вхождения
#   7    INSTALL.md — подвал «Версия: … | Дата: …» (дата = сегодня)
#   8    README.md — подвал (дата = сегодня)
#   9    CHANGELOG.md — заготовка «## [<новая>] - <сегодня>» перед первой
#        секцией (текст секции заполняется руками — скрипт изменениям
#        содержимого не автор)
# Места 3-4 .iss (#define MyAppVersion / OutputBaseFilename) — макро-места
# без литерала версии, не трогаются.
# В конце — гейт: dotnet test --filter VersionSyncTests.
#
# -Root и -SkipTest — для смоука на песочной копии репозитария.
param(
    [string]$Version,
    [switch]$Major,
    [switch]$Minor,
    [switch]$Patch,
    [string]$Root,
    [switch]$SkipTest
)

$ErrorActionPreference = 'Stop'

function Fail([string]$Message) {
    Write-Host "ОШИБКА: $Message" -ForegroundColor Red
    exit 1
}

if ($Root) { $repoRoot = $Root } else { $repoRoot = Split-Path -Parent $PSScriptRoot }
$today = (Get-Date).ToString('yyyy-MM-dd')

# --- выбор новой версии ---
$modeCount = [int][bool]$Major + [int][bool]$Minor + [int][bool]$Patch
if ($Version -and $modeCount -gt 0) {
    Fail "Укажи либо -Version, либо ровно один из -Major/-Minor/-Patch."
}
if (-not $Version -and $modeCount -ne 1) {
    Fail "Укажи -Version X.Y.Z либо ровно один из -Major/-Minor/-Patch."
}

# --- правка файла с сохранением байтов вне замен: кодировка UTF-8 и
# --- наличие/отсутствие BOM, EOL не меняются ---
function Update-TextFile {
    param([string]$Path, [scriptblock]$Transform)
    $bytes = [IO.File]::ReadAllBytes($Path)
    $hasBom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
    $enc = New-Object System.Text.UTF8Encoding($false)
    $skip = 0
    if ($hasBom) { $skip = 3 }
    $text = $enc.GetString($bytes, $skip, $bytes.Length - $skip)
    $newText = & $Transform $text
    if ($newText -ceq $text) { return $false }
    [IO.File]::WriteAllText($Path, $newText, (New-Object System.Text.UTF8Encoding($hasBom)))
    return $true
}

function Count-Literal([string]$Text, [string]$Literal) {
    return ([regex]::Matches($Text, [regex]::Escape($Literal))).Count
}

# --- канон: csproj ---
$csprojPath = Join-Path $repoRoot 'src\SnowMeltingCalculator.csproj'
if (-not (Test-Path $csprojPath)) { Fail "Файл не найден: $csprojPath" }
$csprojText = [IO.File]::ReadAllText($csprojPath)
$m = [regex]::Match($csprojText, '<Version>([^<]+)</Version>')
if (-not $m.Success) { Fail "В $csprojPath не найден тег <Version>." }
$old = $m.Groups[1].Value.Trim()

if ($Version) {
    $new = $Version
} else {
    $parts = $old -split '\.'
    if ($parts.Count -lt 3) { Fail "Канон версии '$old' не трёхчастный." }
    if ($Major) { $new = '' + ([int]$parts[0] + 1) + '.0.0' }
    elseif ($Minor) { $new = '' + $parts[0] + '.' + ([int]$parts[1] + 1) + '.0' }
    else { $new = '' + $parts[0] + '.' + $parts[1] + '.' + ([int]$parts[2] + 1) }
}
if ($new -notmatch '^[0-9]+\.[0-9]+\.[0-9]+$') {
    Fail "Версия '$new' не в формате X.Y.Z (например 1.9.0)."
}
if ($new -eq $old) { Fail "Версия уже '$old' — поднимать нечего." }

Write-Host "Версия: $old -> $new (канон — csproj)"

# --- 1. csproj ---
$needle = "<Version>$old</Version>"
if ((Count-Literal $csprojText $needle) -ne 1) {
    Fail "csproj: ожидался ровно один '$needle'."
}
Update-TextFile $csprojPath { param($t) $t.Replace($needle, "<Version>$new</Version>") } | Out-Null

# --- 2. .iss, шапочный комментарий ---
$issPath = Join-Path $repoRoot 'installer\SnowMeltingCalculator.iss'
if (-not (Test-Path $issPath)) { Fail "Файл не найден: $issPath" }
$issText = [IO.File]::ReadAllText($issPath)
if ((Count-Literal $issText $old) -eq 0) {
    Fail ".iss: не найден литерал '$old' в шапочном комментарии — комментарий протух?"
}
Update-TextFile $issPath { param($t) $t.Replace($old, $new) } | Out-Null

# --- INSTALL.md: 5-6 (имя сетапа, ровно 2) и 7 (подвал) ---
$footerRx = '(\*Версия: [0-9]+\.[0-9]+\.[0-9]+ \| Дата: )[0-9]{4}-[0-9]{2}-[0-9]{2}(\*)'
$installPath = Join-Path $repoRoot 'INSTALL.md'
if (-not (Test-Path $installPath)) { Fail "Файл не найден: $installPath" }
$installText = [IO.File]::ReadAllText($installPath)
$setupName = "SnowMeltingCalculator-v$old-Setup.exe"
if ((Count-Literal $installText $setupName) -ne 2) {
    Fail "INSTALL.md: имя сетапа '$setupName' встречается $(Count-Literal $installText $setupName) раз, ожидается ровно 2."
}
if (([regex]::Matches($installText, $footerRx)).Count -ne 1) {
    Fail 'INSTALL.md: подвал «*Версия: … | Дата: …*» не найден или найден многократно.'
}
Update-TextFile $installPath { param($t) $t.Replace($old, $new) } | Out-Null
Update-TextFile $installPath { param($t) [regex]::Replace($t, $footerRx, ('${1}' + $today + '${2}')) } | Out-Null

# --- README.md: 8 (подвал) ---
$readmePath = Join-Path $repoRoot 'README.md'
if (-not (Test-Path $readmePath)) { Fail "Файл не найден: $readmePath" }
$readmeText = [IO.File]::ReadAllText($readmePath)
if (([regex]::Matches($readmeText, $footerRx)).Count -ne 1) {
    Fail 'README.md: подвал «*Версия: … | Дата: …*» не найден или найден многократно.'
}
Update-TextFile $readmePath { param($t) $t.Replace($old, $new) } | Out-Null
Update-TextFile $readmePath { param($t) [regex]::Replace($t, $footerRx, ('${1}' + $today + '${2}')) } | Out-Null

# --- CHANGELOG.md: 9 (заготовка секции перед первой «## [») ---
$changelogPath = Join-Path $repoRoot 'CHANGELOG.md'
if (-not (Test-Path $changelogPath)) { Fail "Файл не найден: $changelogPath" }
$changelogText = [IO.File]::ReadAllText($changelogPath)
if ((Count-Literal $changelogText "## [$new]") -gt 0) {
    Fail "CHANGELOG.md: секция «## [$new]» уже существует."
}
if (-not ($changelogText -split "`n" | Where-Object { $_.StartsWith('## [') })) {
    Fail 'CHANGELOG.md: не найдено ни одной секции «## [ … ]».'
}
Update-TextFile $changelogPath {
    param($t)
    $nl = if ($t.Contains("`r`n")) { "`r`n" } else { "`n" }
    $lines = $t -split "`n"
    $idx = -1
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i].StartsWith('## [')) { $idx = $i; break }
    }
    $section = "## [$new] - $today" + $nl + $nl +
        "<!-- TODO: заполни изменения версии до коммита -->" + $nl + $nl
    $before = @()
    if ($idx -gt 0) { $before = $lines[0..($idx - 1)] }
    $after = @($lines[$idx..($lines.Count - 1)])
    return (@($before) + $section + $after) -join "`n"
} | Out-Null

# --- гейт ---
if ($SkipTest) {
    Write-Host 'Гейт VersionSyncTests пропущен (-SkipTest).'
} else {
    Write-Host 'Гейт: dotnet test --filter VersionSyncTests (сборка Release — несколько минут)...'
    $sln = Join-Path $repoRoot 'SnowMeltingCalculator.sln'
    dotnet test $sln -c Release --filter 'FullyQualifiedName~VersionSyncTests' --nologo --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        Fail 'VersionSyncTests упал — рассинхрон версии. Откатывай правки (git restore) и разбирайся.'
    }
}

Write-Host "Готово: $old -> $new. Осталось руками: заполнить секцию CHANGELOG.md, добавить запись для $new в src/Services/Updates/WhatsNewCatalog.cs («Что нового» при старте, план 1.3), закоммитить." -ForegroundColor Green
