# Исследование готовых MIT-библиотек для WPF (результаты)

> Источник: отдельная сессия исследования `sess_0bccc1a1-893c-4470-a24b-782d188a69ae`
> (промт — `docs/design/research-github-libraries-prompt.md`), 2026-09-04.
> Read-only, файлы репозитория не менялись. Этот документ — фиксация выводов
> для журнала решений плана редизайна.

## Уточнения по адресам репозиториев (важно)

- VirtualizingWrapPanel переехал: актуальный репозиторий
  **`sbaeumlisberger/VirtualizingWrapPanel`** (старый `s-baer/...` отдаёт 404).
- MicaWPF: `SIMULATAN/MicaWPF` не существует (404); реальный репозиторий —
  **`Simnico99/MicaWPF`** (подтверждено полём Source repository на nuget.org).

## Сравнительная таблица и вердикты

| Кандидат | Лицензия | NuGet / версия | Активность | .NET 8 | Риски | Вердикт |
|---|---|---|---|---|---|---|
| **VirtualizingWrapPanel** (sbaeumlisberger) | MIT | `VirtualizingWrapPanel` 2.5.4 (2026-07-19), 0 зависимостей | релиз ~1,5 мес назад; 3 открытых issues (критичных нет); 1.6M загрузок | да (ассет net6.0-windows7.0) | breakpoints не встроены; переменная высота требует `IItemSizeProvider` | **adopt** |
| **MicaWPF** (Simnico99) | MIT | `MicaWPF` 7.1.0; достаточно `MicaWPF.Core` (0 зависимостей) | живой (релиз месяц назад) | да (нативный TFM) | `MicaWindow` конфликтует с нашим WindowChrome; Win10 — молча ничего не делает; на светлых непрозрачных REHAU-поверхностях эффект невидим | **reject** |
| **XamlFlair** | MIT в репо (в nuspec поле license пустое) | `XamlFlair.WPF` 1.2.13 (2021-10-03); тащит System.Reactive | заморожен с 2022 | нет TFM (ассет netcoreapp3.1) | **утечка хэндлов при повторной навигации** (issue #109, без фикса) — ровно наш сценарий 5 модулей в ContentControl | **reject** |
| **Fluent UI System Icons** (microsoft) | MIT (нейтральные UI-иконки; товарные знаки не трогаем) | официального WPF-пакета нет; источник — SVG из npm `@fluentui/svg-icons` | n/a (ассеты) | n/a | интеграция только через генерацию | **adopt** — скрипт-генерация XAML-геометрий в словарь `Icon.*` |

## Выводы

- **Adopt:** VirtualizingWrapPanel 2.5.4 (единственный новый рантайм-пакет);
  Fluent UI System Icons через скрипт-генерацию `Icon.*` (без рантайм-зависимости).
- **Reject:** XamlFlair (заморожен + утечка хэндлов), MicaWPF (нулевая
  визуальная ценность на светлом непрозрачном бренд-UI + конфликт с WindowChrome).
- **Анимации переходов (Ф7):** собственная attached-property (~100 строк,
  fade/slide 150–200 мс) вместо XamlFlair.
- Интеграция VirtualizingWrapPanel: NuGet-пакет + xmlns + замена панели в
  `ItemsPanelTemplate`; C#-кода почти нет.

## Второй заход (тот же день, та же сессия)

### QuestPDF — лицензионный комплаенс (критично)

QuestPDF 2024.12.3 используется только в `src/Services/Results/PdfExportService.cs`
(618 строк); `ResultsPdfData.cs`/`ResultsPdfDataBuilder.cs` рендер-агностичны.

- Dual-licensing с 2022: Community («условный MIT», порог — консолидированная
  годовая выручка группы **< $1M**, элигибильность определяется организацией,
  а не типом приложения; публичные компании/госсектор — не элигибильны вовсе)
  + Professional $1 999/год, Enterprise $4 999/год (покрывает группу).
- Для REHAU порог практически наверняка не выполняется → комплаенс-проблема.
- **Решение владельца: QuestPDF не используем** → миграция на
  PDFsharp/MigraDoc 6.2.x (MIT, net8, живой): переписывается один
  `PdfExportService.cs`, билдеры данных переиспользуются (Фаза 8 плана).

### UI-автотесты

- **FlaUI — MIT** (проверено по LICENSE.txt и GitHub SPDX; прежнее
  предположение про Apache-2.0 устарело). `FlaUI.UIA3` 5.0.0 (2025-02-25),
  TFM net6.0-windows7.0, работает с уже проставленными AutomationId.
  WinAppDriver — reject (заморожен с 2021).
- **Вердикт: adopt** — smoke-набор на 5 модулей, 0,5–1 день (Фаза 1Б плана).

