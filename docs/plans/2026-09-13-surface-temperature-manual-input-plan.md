# План: ручной ввод температуры поверхности (+1…+7) при сохранении режимов +3/+5/+7

> Дата: 2026-09-13. Статус: на приёмке владельца; реализация — только после
> owner-signal. Дерево не коммитится.
> Основание: исследование 2026-09-13 (диалог с владельцем), вариант **B**.
> show-me: [show-me-surface-temperature.html](show-me-surface-temperature.html).

## 0. Суть

Сегодня температура поверхности t_пов — это числовое значение enum
`OperatingMode` (`AntiIcing=3`, `Melting=5`, `Intensive=7`); вся цепочка —
расчёт, Undo/Redo, `.smc`, PDF — работает через `(int)Mode` и не знает про
«3/5/7» как таковые. Ограничение «только три значения» живёт в двух местах:
список `AvailableModes` в `ThermalViewModel` и состав членов enum.

**Решение (вариант B):** дополнить `OperatingMode` членами `Manual1=1`,
`Manual2=2`, `Manual4=4`, `Manual6=6`; в UI поле «Температура поверхности»
становится редактируемым (целое 1–7) с двусторонней синхронизацией с тремя
пресетами. Каноническое состояние не меняется структурно: по-прежнему одна
величина (`Mode`), один канал мутации (`ThermalInputEdit.ForMode`),
`SurfaceTemperature` остаётся проекцией.

Плюс два решения владельца от 2026-09-13: необлокирующее предупреждение
«t_пов ≤ t_нар» и человекочитаемые подписи ручного режима во всех
поверхностях.

### Зафиксированные решения владельца

| # | Вопрос | Решение |
|---|---|---|
| V1 | Хранение произвольной температуры | **Вариант B**: расширение enum членами 1/2/4/6. Отдельное поле-переопределение (`Override ?? (int)Mode`) отклонено — два владельца одной величины, деградация инварианта «одно значение — один владелец» |
| V2 | Предупреждение при t_пов ≤ t_нар | **Сделать** — необлокирующая подсказка под полем («мощность будет отрицательной»), расчёт не запрещает |
| V3 | Подписи ручного режима | **Сделать** — человекочитаемый формат во всех поверхностях (Results, PDF «Результаты расчёта»); заодно русский текст вместо `mode.ToString()` («Melting») для пресетов |
| V4 | Диапазон и тип ввода | **Целые от +1 до +7**; ввод в поле «Температура поверхности», список `AvailableModes` расширяется до 7 значений (согласованность ComboBox при загрузке `.smc` с ручным значением) |
| V5 | Порядок в списке режимов | Три семантических пресета сверху (как сегодня), затем `Manual1, Manual2, Manual4, Manual6` — владелец может скорректировать на приёмке show-me |

---

## 1. Ф1 — Модель и валидация состояния

| Файл | Правка |
|---|---|
| `src/Models/Thermal/OperatingMode.cs` | +4 члена: `Manual1 = 1`, `Manual2 = 2`, `Manual4 = 4`, `Manual6 = 6` с `[Description("Своё значение (t_П = +N°C)")]`. Doc-comment enum: «числовое значение члена = температура поверхности, °C; члены 1/2/4/6 — пользовательский ввод; имена AntiIcing/Melting/Intensive заморожены (wire .smc)» |
| `src/Core/Constants/ValidationConstants.cs` | Секция «Температуры»: `MinSurfaceTemperature = 1`, `MaxSurfaceTemperature = 7` (int) — единый источник для координатора и UI-подсказок |
| `src/Services/Project/ProjectSessionThermalState.cs` (`ValidateInputs`) | Defensive-проверка: `(int)candidate.Mode` в `[MinSurfaceTemperature..MaxSurfaceTemperature]` (рядом с существующим `Enum.IsDefined`) |

Новый статический форматтер подписей (один источник правил — V3, по духу
урока №22 «одна производная — один источник правил»):

| Файл | Правка |
|---|---|
| `src/Models/Thermal/OperatingModeDisplay.cs` (новый) | `ToDisplayText(this OperatingMode)`: `AntiIcing → «Антиобледенение (+3 °C)»`, `Melting → «Таяние (+5 °C)»`, `Intensive → «Интенсивное (+7 °C)»`, `ManualN → «Пользовательский (+N °C)»`, иначе «—». Используется Results и PDF |

Не трогается: `ThermalInputs` (поле `Mode`), `ThermalInputsSnapshot`,
`ProjectData`, мапперы персистентности, `ThermalCalculator`
(`surfaceTemp = (int)inputs.Mode` — без изменений), legacy-константы
`ThermalConstants.SurfaceTemp*` (2/0/−2; пин-тест запрещает их использование
как t_пов — не релевантны).

## 2. Ф2 — UI теплового модуля

| Файл | Правка |
|---|---|
| `src/ViewModels/Thermal/ThermalViewModel.cs` | `AvailableModes` → 7 значений в порядке V5 (пресеты сверху). Новое свойство ввода `SurfaceTemperatureEntry` (строка): парсинг целого; мутация — только `_coordinator.ApplyInputEdit(ThermalInputEdit.ForMode((OperatingMode)v))` при успешном парсе 1..7; иначе подсказка, состояние не меняется. Guard-и `_isResetting` / `IsLoadProjectInProgress` — как у остальных полей. Динамическая подпись поля: `SurfaceTemperatureCaption` — «из режима „<имя>“» для 3/5/7, «своё значение» для Manual*. Уведомления подписей — в `OnSelectedModeChanged`/`OnResultChanged` (проекции мутаций не создают) |
| `src/Views/Thermal/ThermalView.xaml` | Read-only `Border` поля «Температура поверхности» → `TextBox` (стиль `TextBox.Default`; behaviors `SelectAllOnFocus` / `RestoreOnEscape` / `NormalizeDecimalSeparator` уже есть). Подпись «из режима» → биндинг `SurfaceTemperatureCaption`. `AutomationId="ThermalSurfaceTemperature"`; ComboBox `ThermalMode` и его AutomationId — без изменений. Шапка-комментарий секции обновить |
| `src/Converters/Converters.cs` | `OperatingModeDescriptionConverter`: +4 ветки для Manual* («Своё значение: +N°C. Температура поверхности задана вручную.»); `EnumDescriptionConverter` — generic по `[Description]`, подхватит новых членов без правки |
| `src/Views/Thermal/ThermalView.xaml` (ItemTemplate ComboBox, стр. 112–118) | Первая (bold) строка шаблона — сейчас `{Binding}` → сырое `ToString()` («Melting», для новых членов было бы «Manual4»). Перевести на человекочитаемый текст через `OperatingModeDisplay.ToDisplayText` (конвертер или проекция VM) — ревью P2-2 |

Тексты UI:

- Тултип поля: «Целая температура поверхности от +1 до +7 °C. Значения +3,
  +5, +7 соответствуют режимам „Антиобледенение“, „Таяние“, „Интенсивное“.»
- Подсказка при неверном вводе: «Введите целое число от +1 до +7».
- Caption в поле: «из режима „Таяние“» / «своё значение».

## 3. Ф3 — Предупреждение t_пов ≤ t_нар (V2)

| Файл | Правка |
|---|---|
| `src/ViewModels/Thermal/ThermalViewModel.cs` | `SurfaceTemperatureHint` по образцу `SupplyTemperatureHint`: если `Result != null` и `SurfaceTemperature <= climateData.AirTemperature` → «Температура поверхности не выше температуры воздуха — расчётная мощность будет отрицательной. Расчёт не блокируется.»; иначе пусто. Форматирование — `AppCulture.Culture` (канон Ф7.0). Обновление — в `OnResultChanged` (смена климата инвалидирует тепло → пересчёт → hint свежий). Ревью P3-1: `IClimateData` сейчас в VM только параметр ctor — завести поле-ретейнер для подсказки |
| `src/Views/Thermal/ThermalView.xaml` | `TextBlock` под полем, стиль `FormCard.Hint`, видимость — `StringToVisibilityConverter`; внимание — тёплый цвет предупреждения (как подсветка валидации поля, без красной блокирующей карточки — уточнить токен по `src/Themes` при реализации) |

Не блокирует расчёт: `ThermalCalculator.Validate` остаётся без изменений
(сейчас «t_пов ≥ t_нар» там тоже не проверяется — предупреждение это
мягкое покрытие, не новая блокирующая ошибка).

## 4. Ф4 — Человекочитаемые подписи (V3)

| Файл | Правка |
|---|---|
| `src/ViewModels/Results/ResultsViewModel.cs` | Новое свойство `OperatingModeText` (через `OperatingModeDisplay.ToDisplayText`), уведомление вместе с `OperatingMode` |
| `src/Views/Results/ResultsView.xaml:709` | Биндинг `{Binding OperatingMode}` → `{Binding OperatingModeText}` (сейчас пользователь видит сырое `Melting` — переход на русский текст входит в V3) |
| `src/Services/Results/PdfExportService.cs` (`OperatingModeText`, стр. 837) | Заменить `mode.ToString()` на `OperatingModeDisplay.ToDisplayText`; format-строки «…· поверхность +N °C» сохраняются |

ПЗ (пояснительная записка): t_P выводится числом
`(int)thermal.SelectedMode` с формулой «(int)OperatingMode»
(`ClimateSectionBuilder.cs:31,79`) — остаётся технически верной, не трогается.

## 5. Ф5 — Тесты

Правимые:

- `ThermalViewModelTests` — ревью P2-1: существующий пин
  `AvailableModes.Count == 3` (строка 100) **правится на 7** (не только
  `Contains.Item`, которые не ломаются); добавить точный порядок V5.
- `ThermalAutomationIdSelectorContractTests` — + контракт
  `(@"src\Views\Thermal\ThermalView.xaml", "ThermalSurfaceTemperature", "TextBox")`;
  `ThermalMode` остаётся.
- `PdfExportServiceTests` / строковые пины Results — сверить и обновить
  тексты режима на русский формат (перечислить затронутые пины при
  реализации).
- `ResultsKpiCharacterizationTests`, `StepStatusHonestyTests`,
  `ThermalMultiplicityCharacterizationTests`, `ThermalBaselineTests` —
  прогнать; правки только если пинят строки подписей.

Новые кейсы:

1. Ввод 1..7 → `SelectedMode` = соответствующий член (`Manual*`/пресет),
   одна мутация `ForMode`, dirty-интент; `PowerSummary` показывает
   «+4,0 °C» (формат `+0.0` — ревью P3-3).
2. Синхронизация: выбор «Таяние» → поле «+5», caption «из режима „Таяние“»;
   ввод 3 → `SelectedMode = AntiIcing` (не Manual3).
3. Отклонение: «0», «8», «4,5», «abc» → мутации нет, состояние не тронуто,
   подсказка видна; Escape восстанавливает (`RestoreOnEscape`).
4. `ValidateInputs`: `Manual4` принимается; вне 1..7 / не defined —
   `Rejected`, событие `Changed` не поднимается.
5. Round-trip `.smc`: проект с `Manual4` → JSON `"manual4"` → чтение →
   `Manual4`; старый файл (`"melting"`) читается как `Melting` (wire).
6. Undo/Redo: 5 → 4 → 5, откат/повтор возвращает `Mode` бит-в-бит;
   правка t_пов склеивается в одну запись дневника (окно тишины, урок №21).
7. Пины: множество определённых значений `OperatingMode` = {1..7};
   существующие пины 3/5/7 остаются.
8. Предупреждение: t_пов=+1, воздух +10 → hint непустой; t_пов=+5, воздух
   −10 → пустой; при `Result == null` — пустой.
9. Converter: `OperatingModeDisplay.ToDisplayText` — все 7 членов + прочерк.

## 6. Ф6 — Документация

- `docs/manual/README.html` — раздел «Тепловой расчёт»: три пресета +
  ручной ввод 1..7 + предупреждение; скриншоты секции — из реальной сборки
  (self-contained base64, по процессу 1.3.0).
- `docs/Formulas_Snegotayanie.md`, `docs/calculation-parameter-inventory.md` —
  t_пов: диапазон 1..7, источник «пресет AntiIcing/Melting/Intensive или
  ручной ввод (Manual*)».
- `CHANGELOG.md` — запись [1.4.0].

## 7. Инварианты (не меняется)

- **State ownership**: владелец t_пов — тепловое поле `Mode`
  (`ProjectSession` thermal slice); санкционированные писатели не
  расширяются — та же мутация `ThermalInputEdit.ForMode`. `SurfaceTemperature`
  и подписи — проекции (урок №22: одна величина — один владелец, производные
  — из одного источника правил). R1–R6 целы, `ArchitectureRulesTests` не
  правится, ADR не требуется.
- **Persistence**: `.smc` wire не меняется — `JsonStringEnumConverter`
  штатно пишет новые имена (`manual1…manual6`); имена `antiIcing`/`melting`/
  `intensive` заморожены, старые файлы читаются как есть.
- Физика `ThermalCalculator`, формула t_P в ПЗ, гидравлика (от t_пов не
  зависит), каскад инвалидации, Reset (`Melting`, DEC-T01).

## 8. Версия и релиз (правило №20)

1.3.0 → **1.4.0**: пользовательски заметная возможность (ручной ввод
t_пов + подписи). Синхронно: `csproj` (`<Version>` — первоисточник), `.iss`
(комментарий, `MyAppVersion`, `OutputBaseFilename`), `INSTALL.md` (имя
сетапа ×2, подвал), `README.md` (подвал), `CHANGELOG` ([1.4.0]). Проверка:
grep `1.3.0` в декларирующих файлах — 0 вхождений. Sanity-чек сетапа —
урок №18 (PDFsharp/CSharpMath/libSkiaSharp на месте, ProductVersion).

## 9. Процесс и гейты

- Независимое read-only ревью плана: **выполнено 2026-09-13, вердикт
  APPROVE-WITH-EDITS** (R-2026-09-13-01, находки P2-1, P2-2, P3-1…P3-4
  внесены в этот текст; receipt — `docs/reviews/2026-09-13-surface-temperature-plan-review.md`).
- Приёмка плана владельцем: **открыть show-me** (урок №21) → owner-signal
  «делай» → реализация Ф1 → Ф2 → Ф3 → Ф4 → Ф5 → Ф6, после каждой фазы
  зелёные тесты.
- Материальная поверхность (~20 файлов с тестами) → один независимый
  read-only ревью диффа перед коммитом; находки — в handover (и в
  `docs/architecture/README.md`, если меняют дизайн — по текущему дизайну
  не должны).
- Полный зелёный `dotnet test` → handover некоммиченным деревом → ручная
  приёмка владельцем → коммит по явной команде владельца.
- Ручная приёмка (чек-лист): «Таяние» → поле +5, caption «из режима „Таяние“»;
  ввод +4 → «своё значение», пересчёт; PDF «Результаты расчёта» →
  «Пользовательский (+4 °C) · поверхность +4 °C»; Undo/Redo 5↔4;
  сохранить .smc → `"manual4"` в JSON; открыть старый .smc (`melting`) →
  «Таяние»; t_пов +1 при воздухе +10 → предупреждение; «Сбросить» → +5;
  в списке режимов 7 пунктов в порядке V5.

## 10. Риски

- Строковые пины PDF/Results (тексты режима меняются на русский) —
  закрыть обновлением пинов в Ф4/Ф5.
- UiSmoke, кликающие `ThermalMode` — ComboBox остаётся, риск низкий;
  прогнать UiSmoke-набор.
- Ручной ввод вбок от пресетов создаёт состояния, которых не было в
  характеризационных прогонах, — закрыто defensive-валидацией координатора
  и новыми тестами 1–6.
- Инструкция (self-contained HTML) — скриншоты только из реальной сборки;
  шаг попадает в Ф6 и выполняется после сборки 1.4.0.

## 11. Вне скоупа

- Полуцелые t_пов (например +4,5) и диапазон вне 1..7.
- Слайдер/степпер вместо текстового поля; отдельная сущность
  «пользовательские профили».
- Переименование членов enum / миграция wire.
- Изменение физики расчёта, блокирующая валидация «t_пов > t_нар».
- Прочие модули (климат/гидравлика) — не затрагиваются.
