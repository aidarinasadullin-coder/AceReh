# Публикует манифест обновлений latest.json в папку выдачи Google Drive
# (план 1.3 роадмапа post-1.8, решение U8; чек R-2026-09-21-02 №3).
#
# Манифест читает приложение по прямой ссылке uc?export=download&id=FILE_ID
# (константа src/Services/Updates/UpdateChannelOptions.cs → ManifestUrl).
# Скрипт печатает ссылку после публикации — впиши её в константу.
#
# Гейт стабильности: FILE_ID файла захватывается до и после copyto
# (rclone lsjson); смена ID = ссылка умерла → скрипт падает с ошибкой.
#
# Использование:
#   .\scripts\publish-latest-manifest.ps1                      # локальный rclone владельца
#   .\scripts\publish-latest-manifest.ps1 -RcloneConf <path>   # CI (конфиг из секрета)
#   .\scripts\publish-latest-manifest.ps1 -FolderUrl <url>     # + ссылка на папку выдачи
#
# Версия — из <Version> в src/SnowMeltingCalculator.csproj; строки
# whatsNew — верхняя секция CHANGELOG.md («Добавлено» + «Исправлено»,
# до 4 пунктов, обрезка 140 символов).
param(
    [string]$Version,
    [string]$FolderUrl,
    [string]$RcloneConf,
    [string]$Remote = 'gdrive',
    [string]$Folder = 'ACE 08.09.2026'
)

$ErrorActionPreference = 'Stop'

function Fail([string]$Message) {
    Write-Host "ОШИБКА: $Message" -ForegroundColor Red
    exit 1
}

if (-not (Get-Command rclone -ErrorAction SilentlyContinue)) {
    Fail "rclone не найден. Разовая настройка: winget install Rclone.Rclone, затем 'rclone config' (пошагово — в шапке scripts\upload-to-drive.ps1)."
}

$repoRoot = Split-Path -Parent $PSScriptRoot

# --- версия из csproj ---
if (-not $Version) {
    $csproj = Join-Path $repoRoot 'src\SnowMeltingCalculator.csproj'
    $match = Select-String -Path $csproj -Pattern '<Version>(.*)</Version>' | Select-Object -First 1
    if (-not $match) { Fail "Не удалось прочитать <Version> из $csproj." }
    $Version = $match.Matches[0].Groups[1].Value
}
if ($Version -notmatch '^[0-9]+\.[0-9]+\.[0-9]+$') { Fail "Версия '$Version' не в формате X.Y.Z." }

# --- выжимка CHANGELOG: буллеты верхней секции (Добавлено + Исправлено) ---
$changelogPath = Join-Path $repoRoot 'CHANGELOG.md'
$inSection = $false
$bullets = @()
foreach ($line in Get-Content $changelogPath -Encoding UTF8) {
    if ($line -match '^##\s') {
        if ($inSection) { break }           # дошли до следующей версии
        if ($line -match "\[$([regex]::Escape($Version))\]") { $inSection = $true }
        continue
    }
    if ($inSection -and $line -match '^-\s+(.+)$') {
        $text = $Matches[1] -replace '\*\*', '' -replace '`', ''
        if ($text.Length -gt 140) { $text = $text.Substring(0, 137) + '...' }
        $bullets += $text
        if ($bullets.Count -ge 4) { break }
    }
}
if (-not $inSection) { Fail "В CHANGELOG.md нет секции версии $Version — заполни CHANGELOG перед публикацией манифеста." }

# --- сборка JSON (WriteAllText = UTF-8 БЕЗ BOM; BOM ломает JsonDocument.Parse) ---
$manifest = [ordered]@{ version = $Version; publishedAt = (Get-Date -Format 'yyyy-MM-dd' ) }
if ($bullets.Count -gt 0) { $manifest.whatsNew = $bullets }
if ($FolderUrl) { $manifest.folderUrl = $FolderUrl }
$json = $manifest | ConvertTo-Json -Depth 4
$tempJson = Join-Path $env:TEMP "latest-$Version.json"
[IO.File]::WriteAllText($tempJson, $json)

# --- rclone: ID файла до/после copyto (гейт стабильности ссылки) ---
$target = "${Remote}:${Folder}/latest.json"
$confArgs = @()
if ($RcloneConf) { $confArgs = @("--config", $RcloneConf) }

function Get-FileId([string]$Path) {
    # Файл может не существовать (первый пуск) — это не ошибка, вернём ''.
    $out = & rclone @confArgs lsjson $Path --files-only --log-level ERROR 2>$null
    if ($LASTEXITCODE -ne 0) { return '' }
    try { return ([array](($out -join "`n") | ConvertFrom-Json))[0].Id } catch { return '' }
}

$known = @(rclone @confArgs listremotes --log-level ERROR | ForEach-Object { $_.TrimEnd(':') })
if ($known -notcontains $Remote) {
    Fail "В rclone нет remote '$Remote'. Настрой: 'rclone config' (шапка scripts\upload-to-drive.ps1)."
}

$idBefore = Get-FileId $target

& rclone @confArgs copyto $tempJson $target --log-level ERROR
if ($LASTEXITCODE -ne 0) { Fail "rclone copyto не смог залить манифест в $target." }

$idAfter = Get-FileId $target
if (-not $idAfter) { Fail "После copyto не удалось прочитать FILE_ID файла $target — проверь папку в Drive." }
if ($idBefore -and $idBefore -ne $idAfter) {
    Fail "FILE_ID изменился при перезаписи ($idBefore -> $idAfter): прежняя прямая ссылка в ManifestUrl умерла. Ссылку надо обновить в коде и переиздать приложение."
}

$link = "https://drive.google.com/uc?export=download&id=$idAfter"
Write-Host "Манифест $Version опубликован: $target" -ForegroundColor Green
Write-Host "Прямая ссылка для UpdateChannelOptions.ManifestUrl:" -ForegroundColor Green
Write-Host $link
if ($idBefore -eq '') {
    Write-Host "Файл создан впервые — проверь ссылку в браузере и впиши её в константу, затем закоммить (решение U8)." -ForegroundColor Yellow
} else {
    Write-Host "FILE_ID не изменился — прежняя ссылка жива." -ForegroundColor Green
}
