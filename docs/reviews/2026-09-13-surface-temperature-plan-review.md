# Review Receipt — R-2026-09-13-01

```text
REVIEW_ID: R-2026-09-13-01
SUBJECT: план docs/plans/2026-09-13-surface-temperature-manual-input-plan.md
         + show-me docs/plans/show-me-surface-temperature.html
         («Ручной ввод температуры поверхности +1…+7, вариант B»)
REVIEWER: независимый read-only subagent, чистый контекст (Explore)
DATE: 2026-09-13
RECEIPT: настоящий файл (полный отчёт ревьюера сохранён ниже)
VERDICT: APPROVE-WITH-EDITS
REASON: фактическая база плана точна — все 16 групп ссылок на код
        подтвердились (enum-значения, канал мутации ForMode, JSON-политика
        camelCase, проекция SurfaceTemperature, гварды, пин-тесты, версии).
        Инварианты не нарушаются: владелец t_пов — поле Mode, писатели не
        расширяются, wire .smc совместим, ADR не требуется. Находки P2-1,
        P2-2, P3-1…P3-4 внесены в план/show-me до передачи владельцу;
        дизайн не меняется. Реализация — после owner-signal «делай».
```

## Находки и их судьба

| # | Приоритет | Находка | Судьба |
|---|---|---|---|
| P2-1 | P2 | `ThermalViewModelTests.cs:100` пин `AvailableModes.Count == 3` упадёт при расширении до 7; план упоминал только `Contains.Item` | Внесено в Ф5: пин правится на 7 |
| P2-2 | P2 | ItemTemplate ComboBox (`ThermalView.xaml:112–118`): bold-строка `{Binding}` показывает сырое `ToString()` («Melting»/«Manual4») — правки шаблона в плане не было | Внесено в Ф2: bold-строка через `OperatingModeDisplay.ToDisplayText` |
| P3-1 | P3 | `IClimateData` в VM — только параметр ctor, поля-ретейнера нет; для hint t_пов ≤ t_нар нужен ретейнер | Внесено в Ф3 |
| P3-2 | P3 | Текст предупреждения в плане и show-me расходился («Расчёт не блокируется.») | Формулировки выровнены (фраза добавлена в Ф3) |
| P3-3 | P3 | Тест-кейс 1: фактический формат `PowerSummary` — «+4,0 °C» (`+0.0`), а не «+N °C» | Внесено в Ф5 |
| P3-4 | P3 | V4/V5 в show-me не помечены в hero | Добавлены бейджи V4/V5 |

## Подтверждено ревьюером по коду (выборка)

- `OperatingMode.cs:15,22,29` — AntiIcing=3/Melting=5/Intensive=7;
  `ThermalCalculator.cs:411` — `surfaceTemp = (int)inputs.Mode`; `Validate`
  (стр. 561) Mode не проверяет.
- `ProjectSessionThermalState.cs:391` — `Enum.IsDefined`;
  `ThermalMutationResult.cs:63` — `ForMode`; `Commit` возвращает `NoChange`
  при равных снапшотах — петля «поле ↔ режим» не создаст лишних записей Undo.
- `ThermalViewModel.cs` — `AvailableModes` (273–277), проекция
  `SurfaceTemperature` (73), гварды, уведомления в `OnSelectedModeChanged`
  до гвардов (162–166) — загрузка manual-значения отобразится корректно.
- `ProjectFileService.cs:26` — `JsonStringEnumConverter(camelCase)`:
  «Manual4» → `"manual4"` корректно; старые имена не меняются.
- `ThermalView.xaml` — ComboBox `ThermalMode` (104), read-only Border
  (152–165), behaviors на месте; `ThermalAutomationIdSelectorContractTests`
  — массив Contract, `ThermalMode` закреплён.
- `PdfExportService.cs:837` (использования 274/481),
  `ResultsView.xaml:709` (`{Binding OperatingMode}` — сырое имя),
  `ClimateSectionBuilder.cs:31,79` — t_P числом, формула не меняется.
- Версии 1.3.0: `csproj:11`, `.iss:4,18` — целевая 1.4.0 по правилу №20.
- Дизайн-проверка: defensive-проверка 1..7 дублирует `Enum.IsDefined`
  после расширения (defined = {1..7}) — безвредна; расширенный список 7
  решает проблему пустого ComboBox при загрузке `.smc` с manual-значением;
  UiSmoke действительно трогает `ThermalMode` — ComboBox остаётся.
