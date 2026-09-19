# План: релизный пайплайн `release.yml` (порядок работ №1)

> Дата: 2026-09-20. Статус: план ожидает сигнала владельца (файлы не пишутся,
> имплементация не начинается). Основание: hardening-роадмап §11 (H5 —
> «релизный пайплайн… отдельный мини-план после волны 1»), порядок работ
> владельца, пункт 1. show-me не требуется — инфраструктура, не продукт
> (решение владельца от 2026-09-20).

## 0. Суть (снятое состояние, 2026-09-20)

| Факт | Где |
|---|---|
| Версия — единственный источник `<Version>1.7.0</Version>` | `src/SnowMeltingCalculator.csproj:11` |
| Publish-параметры уже в csproj: `SelfContained=true`, `RuntimeIdentifier=win-x64`, `PublishReadyToRun=true` | `src/SnowMeltingCalculator.csproj:17-20` |
| Инсталлятор сам читает версию из `..\publish\SnowMeltingCalculator.exe` (FileVersion), срезает хвостовой `.0`, кладёт сетап в `..\output` под именем `SnowMeltingCalculator-v{X.Y.Z}-Setup.exe`; лицензия `..\docs\license.rtf`, иконка `..\src\Assets\app_icon.ico` | `installer/SnowMeltingCalculator.iss` |
| Ручной путь publish → ISCC описан и актуален | `INSTALL.md` |
| CI — только test-gate (push master + PR, полный набор без UiSmoke) | `.github/workflows/ci.yml` |
| `publish/` и `output/` вне git (уроки №18/№19) | `.gitignore` |
| Git LFS задет только `docs/manual/**` и `docs/architecture-migration/**`; `docs/license.rtf` и `src/Assets/app_icon.ico` — обычные файлы, собранный `README.html` — обычный файл. **Сборке сетапа LFS-смудж не нужен** (CI и сейчас зелёный без `lfs: true`) | `git lfs ls-files` |
| Тегов `v*` нет (есть только старые фазовые `fase-*`, `pre-redesign`) | `git tag` |
| Inno Setup 6 предустановлен на GitHub-раннерах `windows-latest` (`C:\Program Files (x86)\Inno Setup 6\ISCC.exe`) | runner-images |

Проблема: релиз — ручной ритуал на машине владельца (publish + ISCC руками).
Нет воспроизводимости на чистой машине, нет артефактов в Actions, нет тегов
`v*`.

## 1. Решения дизайна

1. **Триггер** — `workflow_dispatch` с обязательным строковым входом `version`
   (например `1.7.0`), как заказано владельцем.
2. **Гейты до сборки** (fail-fast, до restore): запуск только с `master`;
   `version` соответствует `^[0-9]+\.[0-9]+\.[0-9]+$`; вход обязан совпасть с
   `<Version>` в csproj (источник версии един — рассинхрон входа и имени
   сетапа невозможен); тег `v{version}` свободен в `origin` (пуш без force).
3. **Тест-гейт** — полный набор `Category!=UiSmoke` перед publish:
   `workflow_dispatch` можно запустить на любом SHA, релиз не собирается из
   непроверенного дерева.
4. **Publish** — `dotnet publish src/SnowMeltingCalculator.csproj -c Release
   -r win-x64 --self-contained true -o publish`: путь к csproj явный (ловушка
   sln — иначе в publish уходит тестовый мусор, описано в INSTALL.md), флаги
   для явности, параметры дублируют csproj.
5. **ISCC** — шаг-страховка `choco install innosetup` при отсутствии
   `ISCC.exe` (на текущем образе уже стоит — шаг мгновенный), затем
   `& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
   installer\SnowMeltingCalculator.iss`. Версию, имя файла и папку выдачи
   берёт сам `.iss` — в workflow ничего не дублируем.
6. **Артефакт** — `actions/upload-artifact@v4`, `output/*.exe`, имя
   `SnowMeltingCalculator-v{version}-Setup`, `if-no-files-found: error`.
7. **Тег** — аннотированный `v{version}` на `github.sha`, пуш от
   `github-actions[bot]` через `GITHUB_TOKEN` (`permissions: contents:
   write`), строго после успешной сборки артефакта.
8. **Расширение сверх ТЗ (решение владельца)** — вход `push_tag`
   (boolean, default `true`): прогон без пуша тега. Даёт безопасный первый
   прогон на текущей 1.7.0 и вечную команду «просто собрать сетап на чистой
   машине». Если владелец против — убрать вход, шаг тега выполняется
   безусловно.
9. **`concurrency: group: release`** — два одновременных ручных запуска
   выстраиваются в очередь, артефакты не перемешиваются.
10. `timeout-minutes: 60` (полный тест ~20 мин + publish ~5–8 + ISCC ~3).

## 2. Файлы

### 2.1. НОВЫЙ `.github/workflows/release.yml` (полный текст)

```yaml
# Релизный пайплайн (план docs/plans/2026-09-20-release-pipeline-plan.md,
# порядок работ №1 владельца; hardening-роадмап §11, H5).
# Ручной запуск: Actions → release → Run workflow.
# Версия — <Version> в src/SnowMeltingCalculator.csproj (единственный
# источник); сетап-имя и папку выдачи берёт installer/*.iss сам.
name: release

on:
  workflow_dispatch:
    inputs:
      version:
        description: 'Версия релиза, обязана совпадать с <Version> в src/SnowMeltingCalculator.csproj (например 1.7.0)'
        required: true
        type: string
      push_tag:
        description: 'Пушить тег vX.Y.Z (сними галочку, чтобы только собрать сетап)'
        required: false
        type: boolean
        default: true

permissions:
  contents: write   # пуш аннотированного тега vX.Y.Z

concurrency:
  group: release
  cancel-in-progress: false

jobs:
  release:
    runs-on: windows-latest
    timeout-minutes: 60
    env:
      VERSION: ${{ inputs.version }}
    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 8.0.x

      - name: Гейт: релиз только с master
        shell: bash
        run: |
          if [ "${GITHUB_REF}" != "refs/heads/master" ]; then
            echo "::error::Релиз запускается только с master (запущен на ${GITHUB_REF})."
            exit 1
          fi

      - name: Гейт: формат версии X.Y.Z
        shell: bash
        run: |
          if ! [[ "${VERSION}" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
            echo "::error::Версия '${VERSION}' не в формате X.Y.Z (например 1.7.0)."
            exit 1
          fi

      - name: Гейт: вход совпадает с <Version> в csproj
        shell: bash
        run: |
          CSPROJ_VERSION=$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' src/SnowMeltingCalculator.csproj)
          echo "csproj: <Version>${CSPROJ_VERSION}</Version>, вход: ${VERSION}"
          if [ "${CSPROJ_VERSION}" != "${VERSION}" ]; then
            echo "::error::Вход ${VERSION} не совпадает с <Version>${CSPROJ_VERSION}</Version> в src/SnowMeltingCalculator.csproj — подними версию в csproj, закоммить и перезапусти."
            exit 1
          fi

      - name: Гейт: тег v${VERSION} свободен
        if: ${{ inputs.push_tag }}
        shell: bash
        run: |
          if git ls-remote --exit-code --tags origin "refs/tags/v${VERSION}" >/dev/null 2>&1; then
            echo "::error::Тег v${VERSION} уже существует в origin."
            exit 1
          fi

      - name: Restore
        run: dotnet restore SnowMeltingCalculator.sln

      - name: Build (Release)
        run: dotnet build SnowMeltingCalculator.sln -c Release --no-restore

      - name: Full suite (without UiSmoke)
        run: dotnet test SnowMeltingCalculator.sln -c Release --no-build --nologo
             --filter "Category!=UiSmoke"

      - name: Publish (self-contained win-x64, параметры дублируют csproj)
        run: dotnet publish src/SnowMeltingCalculator.csproj -c Release
             -r win-x64 --self-contained true -o publish

      - name: Inno Setup (страховка, на образе уже предустановлен)
        shell: pwsh
        run: |
          if (-not (Test-Path 'C:\Program Files (x86)\Inno Setup 6\ISCC.exe')) {
            choco install innosetup -y --no-progress
          }

      - name: Compile installer (ISCC)
        shell: pwsh
        run: '& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" "installer\SnowMeltingCalculator.iss"'

      - name: Upload setup artifact
        uses: actions/upload-artifact@v4
        with:
          name: SnowMeltingCalculator-v${{ inputs.version }}-Setup
          path: output/*.exe
          if-no-files-found: error

      - name: Push tag v${VERSION}
        if: ${{ inputs.push_tag }}
        shell: bash
        run: |
          git -c user.name="github-actions[bot]" \
              -c user.email="41898282+github-actions[bot]@users.noreply.github.com" \
              tag -a "v${VERSION}" -m "Release v${VERSION} (${GITHUB_SHA})"
          git push origin "v${VERSION}"
```

### 2.2. `INSTALL.md` — новый раздел после «Сборка установщика»

Короткий раздел «Автоматический релиз (GitHub Actions)»: поднять `<Version>`
в csproj → закоммитить и запушить → Actions → release → Run workflow →
ввести версию → забрать артефакт `SnowMeltingCalculator-vX.Y.Z-Setup` из
прогона, тег `vX.Y.Z` появляется сам; галочка «Пушить тег» собирает сетап
без релиза. Ручной путь (разделы выше) остаётся как запасной. Даты/версии в
подвале не трогаются.

### 2.3. Не меняется

`.github/workflows/ci.yml`, `src/**`, `tests/**`, `installer/*.iss` — не
трогаются; dotnet-тестов сдвиг быть не должен, полный прогон перед
хендовером выполняется по правилам.

## 3. Волна имплементации (одна, отдельный цикл с handover)

| Шаг | Действие | Гейт |
|---|---|---|
| 1 | Записать `release.yml` по §2.1 | yaml читаем |
| 2 | Дописать раздел в `INSTALL.md` по §2.2 | — |
| 3 | Локальный полный прогон `dotnet test -c Release --no-build --filter "Category!=UiSmoke"` | зелёный |
| 4 | Коммит явным pathspec: `.github/workflows/release.yml`, `INSTALL.md`, `docs/plans/2026-09-20-release-pipeline-plan.md` (untracked чужие файлы не задевать) | `git status` чистый по остальным |
| 5 | Push → дождаться CI | зелёный на последнем коммите |
| 6 | Handover: «state ownership без изменений» | — |
| 7 | Первый прогон — владелец из UI: **вариант А (рекомендую)**: `push_tag=false` на текущей 1.7.0 — чистая валидация пайплайна без релиза; **вариант Б**: сразу реальный релиз следующей версии | артефакт ~68 МБ в прогоне |

## 4. Риски и защита

| Риск | Защита |
|---|---|
| Рассинхрон входа, csproj и имени сетапа | гейт «вход == `<Version>`» (iss берёт FileVersion из exe, а тот — из csproj) |
| Перезапись существующего тега | гейт «тег свободен» + пуш без `--force` |
| `GITHUB_TOKEN` без прав записи | `permissions: contents: write` в workflow; если шаг тега упадёт — сообщение об ошибке укажет на права репозитория (Settings → Actions → Workflow permissions) |
| В publish уходит тестовый мусор | путь к csproj явный (ловушка задокументирована в INSTALL.md) |
| LFS-указатели ломают сборку | не задет: LFS только в `docs/manual` и evidence; лицензия/иконка/`README.html` — обычные файлы (проверено `git lfs ls-files`) |
| Смена образа раннера (ISCC пропал) | шаг-страховка `choco install innosetup` |
| Параллельные ручные запуски | `concurrency: group: release` |

## 5. Вне объёма

GitHub Release (страница + changelog + exe к релизу), подпись кода (D5 — не
сейчас), телеметрия (D2 — запрещена), zip портативной `publish\`,
CHANGELOG-автоматизация, автоматический бамп `<Version>` (правится
владельцем руками), миграция publish-параметров в PublishProfiles
(остаётся в волне «Хвосты», §9 роадмапа).
