# Review Receipt — R-2026-09-13-05

```text
REVIEW_ID: R-2026-09-13-05
SUBJECT: инсталлер — installer/SnowMeltingCalculator.iss + INSTALL.md
         + собранный сетап output/SnowMeltingCalculator-v1.5.0-Setup.exe
         (ревью существующего закоммиченного артефакта на HEAD, не диффа)
REVIEWER: независимый read-only subagent, чистый контекст (Explore)
DATE: 2026-09-13
RECEIPT: настоящий файл (полный отчёт ревьюера сохранён ниже)
VERDICT: REJECT
REASON: Даунгрейд-гард в [Code] — мёртвый код: UninstKey строится из
        `{#SetupSetting("AppId")}`, а ISPP возвращает сырое значение
        `{{A1B2C3D4…}` с двойной скобкой (подтверждено по исходникам
        ISPP.Funcs.pas и эмпирике реестра), поэтому ключ `{{…}_is1`
        никогда не совпадает с реальным `{…}_is1` — обещанное INSTALL.md
        предупреждение при даунгрейде не показывается никогда. Секция
        [Code] и [InstallDelete] добавлены только в 1.5.0 (6293db7) и
        реально в даунгрейд-сценарии не проверялись. Отдельно INSTALL.md
        описывает несуществующую структуру установленной папки
        (`docs\Инструкция полная\`, `media\`, `LatoFont\` вместо
        фактического `docs\manual\README.html`). R1–R6 и wire-контракт
        .smc инсталлером не задеваются. Требуется фикс .iss + пересборка
        сетапа + правка INSTALL.md до handover.
```

## Находки и статусы

| # | Приоритет | Находка | Статус |
|---|---|---|---|
| 1 | P1 | Даунгрейд-гард не работает: ключ деинсталляции строится с литеральной двойной скобкой и никогда не совпадает с реальным ключом реестра. Доказательство (цепочка замкнута с трёх сторон): installer/SnowMeltingCalculator.iss:24 — `AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}`; .iss:95-96 — `UninstKey := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\' + '{#SetupSetting("AppId")}_is1'`. ISPP-семантика: реализация `SetupSetting` (Projects/Src/ISPP.Funcs.pas, функция `SetupSetting`/`Find`) — `Result := Trim(Copy(Strings[I], J + 1, MaxInt))`: сырая строка после `=`, экранирование `{{`→`{` НЕ снимается (независимо перепроверено владельцем процесса по первоисточнику). Строковый литерал в [Code] после ISPP-расширения = `{{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}_is1` (в [Code] Pascal-строках `{{` не разворачивается — экранирование только для значений директив). Реальный ключ — с одной скобкой: эмпирика read-only `reg query` по деинсталляционному ключу самого Inno Setup 6 на этой машине (`{GUID}_is1`, DisplayVersion 6.7.3) + офиц. док topic_setup_appid. Следствие: все четыре `RegQueryStringValue` всегда False → `MsgBox` не показывается никогда. Обещание INSTALL.md:40-41 и комментарий .iss:87 не соответствуют поведению; сценарий «поставил 1.4.0 поверх 1.5.0» проходит молча, [InstallDelete] сносит более новые файлы | **требует исправления до handover** (фикс + пересборка сетапа). Варианты: (а) `#define MyAppId "{A1B2C3D4-…}"` + `AppId={#MyAppId}` (эффективный AppId не меняется — непрерывность апгрейда с v1.0 сохраняется); (б) срез первой скобки в [Code]: `Copy('{#SetupSetting("AppId")}', 2, MaxInt)`; (в) литерал ключа. В этот же фикс: гейт `WizardSilent()` — после починки ключа модальный русский MsgBox будет блокировать /SILENT и /VERYSILENT-развёртывания (skipifsilent стоит только на [Run], .iss:79); после починки — ручной smoke даунгрейда с /LOG |
| 2 | P2 | Блок «Структура установленной папки» в INSTALL.md описывает поставку, которой нет. INSTALL.md:149-153 обещает `docs\Инструкция полная\README.html` (+ `media\`) и `LatoFont\`. Факт: csproj деплоит `..\docs\manual\README.html` → `docs\manual\README.html` (src/SnowMeltingCalculator.csproj:47-54); в publish\ только `docs\manual\README.html` (скриншоты base64, `media\` не деплоится); `find publish -iname "*lato*" -o -iname "*.ttf" -o -iname "*.otf"` — пусто, шрифты Inter встроены (csproj:56-59); урок №23 (docs/agents/lessons.md:476-487) фиксирует фактический деплой. Приложение при этом работает: меню «Файл → Инструкция» открывает правильный путь (src/MainWindow.xaml.cs:161-189) | **требует исправления до handover** (правка INSTALL.md:137-157) |
| 3 | P3 | Числовые обещания INSTALL.md систематически занижены на 10–20%: сетап фактически 67 954 146 байт ≈ 68.0 MB против «~60 MB» (INSTALL.md:12,104); развёрнутая поставка ≈ 226 MB против «~200 MB» (INSTALL.md:7); потребность на диске ≈ 294 MB против «~250 MB» — при 250 MB свободного места установка может упереться в место | отложено (правится вместе с находкой 2 в INSTALL.md; решение владельца — уточнить числа или оставить «~») |
| 4 | P3 | Лицензия в мастере установки показывает «Версия 1.0.0» при установке 1.5.0: docs/license.rtf (подключён .iss:31), заголовок «…РЕХАУ / Версия 1.0.0». Файл не входит в чек-лист правила №20 (docs/agents/lessons.md:346-353), но это первое, что видит пользователь в мастере | отложено (владелец: обновить текст лицензии либо осознанно принять) |
| 5 | P3 | AppId выглядит как учебный placeholder-GUID (`A1B2C3D4/E5F6/7890/ABCDEF…`, .iss:24) — теоретический риск коллизии с чужим продуктом. Против смены сейчас: тот же AppId с самой первой фиксации (git 7d0ca2b) — смена разорвёт цепочку апгрейдов и «одну запись в Приложениях» | отложено (решение владельца; ротация — только отдельным планом миграции с деинсталляцией старых версий) |
| 6 | P3 | Свежесть publish\ относительно csproj не контролируется машинно: при запуске ISCC без свежего `dotnet publish` авто-версия молча возьмёт FileVersion протухшего exe (урок №18: publish/ протухает; sanity-скрипт уроков №17-18 не создан). VersionSyncTests (tests/SnowMeltingCalculator.Tests/Architecture/VersionSyncTests.cs:46-103) покрывает csproj↔.iss↔доки, но не собранный exe (вне git) | отложено (владелец: ручная дисциплина или make-installer.ps1) |

## Подтверждено ревьюером (выборка с путями:строками)

- **A1 (якоря файл:строка).** `LicenseFile=..\docs\license.rtf` — существует; `SetupIconFile=..\src\Assets\app_icon.ico` — существует; `OutputDir=..\output` — сетап на месте (67 954 146 байт, сверено PowerShell); Russian.isl — есть в установленном Inno Setup 6; publish\data содержит ровно 4 json из INSTALL.md:116-119 (md5 совпадают с data\ репозитория); publish\docs — только `manual\README.html` (находка 2); локализованные каталоги cs\ de\ … zh-Hant\ — 15 локалей, как обещано INSTALL.md:153; `{autopf}` per-machine = `C:\Program Files\REHAU\SnowMeltingCalculator` — верно; ярлык «Пуск → REHAU → Калькулятор снеготаяния» соответствует DefaultGroupName/[Icons] (.iss:29,64).
- **A2 (правило №20).** RULE-VER (docs/agents/rules-registry.md:36): «каждый новый инсталл — новая версия; одна каноническая строка в девяти местах». Все девять мест сверены: csproj:11; .iss:13,18,33; INSTALL.md:12,101,169; README.md:71; CHANGELOG.md:3 `## [1.5.0]`. `grep "1\.4\.0\|1\.1\.2"` в INSTALL.md/README.md — 0 вхождений. VersionSyncTests покрывает все 9 мест (CRLF-стойко).
- **A3 (ripple INSTALL.md ↔ код).** Настройки: src/Services/AppSettings.cs:11-14 — `%APPDATA%\SnowMeltingCalculator\settings.json` — совпадает с INSTALL.md:34; шаблоны: src/Repositories/Construction/ConstructionTemplateRepository.cs:39-46 — `%LOCALAPPDATA%\SnowMeltingCalculator\data\construction_templates.json` — совпадает с INSTALL.md:35. «Файл → Инструкция»: src/MainWindow.xaml.cs:161-189 открывает `BaseDirectory\docs\manual\README.html` — совпадает с фактическим деплоем (не с текстом INSTALL.md — находка 2). Ассоциация .smc: .iss:70-76 command `""{app}\…exe"" ""%1""`; приложение обрабатывает аргумент: src/App.xaml.cs:116-121 `SelectStartupProjectPath` + OnStartup:71-78 `InitialProjectPath` → `LoadProjectFromPathAsync`. «Установщик сам найдёт прежнюю установку и предложит ту же папку» — UsePreviousAppDir/UsePreviousPrivileges (Inno-дефолты); «одна запись в Приложениях» — тот же AppId (git-история).
- **A4 (excludes и мусор).** `Excludes: "*.pdb"` (.iss:61) отрезает только SnowMeltingCalculator.pdb (единственный pdb); deps.json/runtimeconfig.json на месте; тестового мусора нет (FlaUI/coverlet/test — пусто); `createdump.exe` — легитимная часть рантайма. Поставка не трогает state ownership и wire-формат .smc: инсталлер пишет только {app}, иконки и три ключа реестра ассоциации.
- **A5 (препроцессор и арифметика).** Фактическая версия publish- exe: FileVersion `1.5.0.0`, ProductVersion `1.5.0+e72a1a5…`. Ручной просчёт среза: `Pos(".0#", "1.5.0.0#")` = 6 → `Copy(1,5)` = «1.5.0» — верно; для гипотетического 4-го сегмента ≠ 0 → fallback возвращает полную строку, деградации нет. Синтаксис `AppId={{…}` (.iss:24) — корректное экранирование литеральной `{` в значении директивы (эффективный AppId — `{A1B2C3D4-…}`); но разворот `'{#SetupSetting("AppId")}_is1'` НЕ даёт реальный ключ — находка 1.
- **I1 (семантика Inno 6).** `PrivilegesRequiredOverridesAllowed=dialog`: per-machine → HKA=HKLM (HKLM64 при 64-битном режиме), per-user → HKA=HKCU: четырёхкустовый поиск (.iss:100-103) покрывает оба реальных варианта; HKLM32/HKCU32 избыточны, безвредны. Пара `ArchitecturesAllowed=x64compatible` + `ArchitecturesInstallIn64BitMode=x64compatible` — валидна для 6.3+. Директив, требующих новее 6.3, нет; устаревших нет. `[UninstallDelete] dirifempty {app}` и `{autopf}\REHAU` (.iss:81-84) достижимы в обоих режимах.
- **I2 (апгрейд).** Запущенное приложение: CloseApplications — Inno-дефолт `yes`: интерактивно промпт по файлам из [Files] и [InstallDelete] (Restart Manager), в silent — автозакрытие; INSTALL.md:26 — подстраховка, не единственная защита. Порядок подтверждён по Setup.Install.pas: RM-shutdown → ProcessInstallDeleteEntries → GenerateUninstallInfoFilename → CopyFiles: [InstallDelete] выполняется ДО сохранения нового деинсталлятора — свежий unins000.exe не сносится, консистентная запись Uninstall остаётся. Пустой OldVersion отфильтрован (.iss:105).
- **I3 (деинсталляция).** `uninsdeletevalue`/`uninsdeletekey` на .smc-ветках (.iss:70-76) согласованно; при смешении per-machine/per-user установок чистится только куст своего прогона — Inno-стандарт; иконки {group} удаляются штатно; пустая папка Start Menu\REHAU убирается, dirifempty дочищает {autopf}\REHAU.
- **I4 (локализация и сообщения).** Только Russian.isl — весь мастер и [Code]-сообщения русские на любой Windows: продуктовое решение REHAU-RU, для экспортных поставок станет дефектом (зафиксировано, без отдельного статуса). `Format(Msg, [...])`: три `%s` — три аргумента (.iss:107-111), арность верна; `MB_DEFBUTTON2` соответствует комментарию «по умолчанию — прервать»; `{#StringChange(MyAppName,'&','&&')}` в [Run] корректен. license.rtf — валидный RTF (fcharset204, кириллица `\uNNNN?`); содержимое «Версия 1.0.0» — находка 4.
- **A6 (show-me).** N/A: ревьюится артефакт поставки, а не план, передаваемый владельцу; show-me обязателен только для планов (уроки №21/№27, RULE-SHOWME).
- **A7 (гейты).** Owner-решений документация инсталлера не присваивает: обещания о сохранности данных проверяются кодом (A3); предупреждение «всё, что вручную сохранено в папке установки, будет удалено» (.iss:56 ↔ INSTALL.md:36-37) продублировано честно.
- **A8 (процесс).** docs/agents/quality-log.md — 5 записей (R-2026-09-12-01, R-2026-09-13-01…04); фактические REVIEW_ID в 5 файлах docs/reviews/ совпадают 1:1; ID R-2026-09-13-05 свободен; шаблон чека на месте; уроки №18/№19 (`publish/`, `output/` вне git — .gitignore:23-26, соответствует REPO-HYGIENE), №20, №26, №27, №29 учтены.

Источники (документация и исходники Inno Setup, использованные для вердикта):
[ISPP SetupSetting (ispp.xml)](https://raw.githubusercontent.com/jrsoftware/issrc/main/ISHelp/ispp.xml),
[ISPP.Funcs.pas](https://raw.githubusercontent.com/jrsoftware/issrc/main/Projects/Src/ISPP.Funcs.pas),
[ISPP.Preprocessor.pas](https://raw.githubusercontent.com/jrsoftware/issrc/main/Projects/Src/ISPP.Preprocessor.pas),
[Setup.Install.pas](https://raw.githubusercontent.com/jrsoftware/issrc/main/Projects/Src/Setup.Install.pas),
[isetup.xml 6.5.1](https://raw.githubusercontent.com/jrsoftware/issrc/is-6_5_1/ISHelp/isetup.xml),
[topic_setup_appid](https://jrsoftware.org/ishelp/topic_setup_appid.htm).

---

## Постскриптум (2026-09-14, после приёмки владельцем)

Владелец принял вердикт: фикс `.iss` + правка INSTALL.md + пересборка +
smoke даунгрейда, затем handover. Что сделано:

- **P1 (фикс).** installer/SnowMeltingCalculator.iss: добавлен
  `#define MyAppId "{A1B2C3D4-…}"` (одинарные скобки); в [Setup] значение
  подставляется через `{#StringChange(MyAppId, "{", "{{")}` — прямое
  `AppId={#MyAppId}` не компилируется: одиночная «{» в значении директивы
  парсится как константа, поэтому вариант «(а)» из находки 1 в исходном
  виде нерабочий; в [Code] ключ строится из `{#MyAppId}_is1`. Smoke поймал
  вторую половину P1, невидимую ревью: `ComparePackedVersion` принимает
  только Int64 (`PackVersionComponents`), на строках версий — фатальный
  Type Mismatch в InitializeSetup, причём бокс «Runtime error» показывается
  даже в /VERYSILENT и вешает тихую установку; добавлен хелпер
  `PackVersionStr` (строка «1.5» / «1.5.0» / «1.5.0.0» → Int64). Гейт
  `WizardSilent()`: в тихом режиме даунгрейд прерывается без вопроса; в
  ветку даунгрейда добавлен диагностический `Log(...)`.
- **P2 + P3-3 (фикс).** INSTALL.md: фактическая структура установленной
  папки (`docs\manual\README.html`; несуществующие `docs\Инструкция
  полная\`, `media\`, `LatoFont\` убраны) и размеры по факту (~68 MB сетап,
  ~226 MB развёрнутая, ~300 MB на диске).
- **Пересборка.** Свежий `dotnet publish` (self-contained, FileVersion
  1.5.0.0, ProductVersion 1.5.0+cff2322) → ISCC 6.7.3:
  output/SnowMeltingCalculator-v1.5.0-Setup.exe, 67 957 330 байт
  (2026-09-14 01:30). Эффективный AppId не изменился ({A1B2C3D4-…}) —
  ключ реестра существующей per-machine установки совпадает,
  непрерывность апгрейдов сохранена.
- **Smoke даунгрейда (/LOG), 3 сценария — все зелёные.** Стенд:
  синтетический установщик с тем же [Code], тестовый AppId {D1E2F3A4-…},
  PrivilegesRequired=lowest (без UAC), Uninstallable=no, фейковый
  HKCU-ключ деинсталляции. (1) DisplayVersion=9.9.9 → SMOKE-FOUND →
  SMOKE-DOWNGRADE → SMOKE-ABORT, exit 1 — гард сработал; (2)
  DisplayVersion=1.4.0 → SMOKE-FOUND без даунгрейда, exit 0 — легитимный
  апгрейд не блокируется; (3) ключа нет → SMOKE-NOTFOUND, exit 0. Стенд и
  фейковый ключ удалены; реальная per-machine установка 1.5.0 на этой
  машине не трогалась (её сетап собран со старым кодом — гарда в ней нет
  до следующей установки поверх).
- **P3-4 (license.rtf «Версия 1.0.0»)** — не входило в объём приёмки,
  осталось владельцу; **P3-5 (AppId)** — осознанно не менялся;
  **P3-6 (свежесть publish\)** — закрыто ручной дисциплиной в этом цикле
  (свежий publish), машинный контроль не сделан.
- **Тесты.** Полный прогон Release на cff2322 — 2293 пройдено / 0 не
  пройдено / 1 пропущен; VersionSyncTests повторно зелёный после правок
  .iss / INSTALL.md / CHANGELOG (правки контракт-файлов — коммитить в том
  же пуш-цикле, урок №31).
- **Урок №33** (docs/agents/lessons.md): [Code]-код инсталлера машинно не
  проверяется ничем; платформенный код инсталлера перед релизом проверяется
  smoke-стендом (3 ветки с /LOG).
