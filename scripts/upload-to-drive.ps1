# Заливает собранный сетап Калькулятора снеготаяния в папку выдачи на Google Drive.
#
# Разовая настройка (один раз):
#   1. winget install Rclone.Rclone
#   2. rclone config
#      n (новый remote) -> имя gdrive -> тип drive
#      client_id / client_secret — Enter (пусто)
#      scope — 1 (полный доступ к Drive)
#      root_folder_id — Enter (пусто); service_account_file — Enter (пусто)
#      Edit advanced config — n; Use auto config — y
#      -> войти в тот Google-аккаунт, где лежит папка выдачи, разрешить доступ
#
# Папка назначения адресуется по ID из ссылки Drive, поэтому переименование
# папки в веб-интерфейсе ничего не ломает.
#
# Использование:
#   .\scripts\upload-to-drive.ps1                     # самый свежий output\*-Setup.exe
#   .\scripts\upload-to-drive.ps1 -Path <файл.exe>    # конкретный файл
#
# Повторный запуск не перезаливает неизменённый файл — rclone сверяет хеши.
param(
    [string]$Path,
    [string]$Remote = 'gdrive',
    [string]$FolderId = '1PAAse-5mt51ruvZUd-NrZuBwsXLGfoFX'
)

$ErrorActionPreference = 'Stop'

function Fail([string]$Message) {
    Write-Host "ОШИБКА: $Message" -ForegroundColor Red
    exit 1
}

if (-not (Get-Command rclone -ErrorAction SilentlyContinue)) {
    Fail "rclone не найден. Разовая настройка: winget install Rclone.Rclone, затем 'rclone config' (пошагово — в шапке этого файла)."
}

# Без 2>$null: в PS 5.1 при ErrorActionPreference=Stop редирект stderr
# нативной команды оборачивает NOTICE в ErrorRecord и роняет скрипт.
# --log-level ERROR глушит NOTICE «config file not found» до настройки.
$known = @(rclone listremotes --log-level ERROR | ForEach-Object { $_.TrimEnd(':') })
if ($known -notcontains $Remote) {
    Fail "В rclone нет remote '$Remote'. Разовая настройка: 'rclone config' (пошагово — в шапке этого файла)."
}

$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $Path) {
    $outputDir = Join-Path $repoRoot 'output'
    $latest = Get-ChildItem -Path $outputDir -Filter 'SnowMeltingCalculator-v*-Setup.exe' -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (-not $latest) {
        Fail "В $outputDir нет SnowMeltingCalculator-v*-Setup.exe — собери сетап или укажи -Path."
    }
    $Path = $latest.FullName
}
if (-not (Test-Path $Path)) { Fail "Файл не найден: $Path" }

$size = '{0:N1} MB' -f ((Get-Item $Path).Length / 1MB)
$target = "${Remote}:{$FolderId}"
Write-Host "Загружаю $(Split-Path -Leaf $Path) ($size) -> $target"
rclone copy $Path $target -v --stats-one-line
if ($LASTEXITCODE -ne 0) {
    Fail "rclone вернул код $LASTEXITCODE — файл мог не долиться. Перезапусти: уже загруженное перезаливаться не будет."
}
Write-Host "Готово: $(Split-Path -Leaf $Path) в https://drive.google.com/drive/folders/$FolderId" -ForegroundColor Green
