# Иконки: карта соответствия inline Path → Fluent

Фаза 2 редизайна (план Ф2.3). Канонический словарь иконок —
`src/Themes/Icons.Fluent.xaml`, **только генерация**:
`python tools/gen-fluent-icons.py` по манифесту `tools/icons.txt`.
Источник — `@fluentui/svg-icons` (microsoft/fluentui-system-icons),
вариант `_24_regular`, лицензия MIT — проверено по первоисточникам
(LICENSE репозитория + npm metadata; товарные знаки не затрагиваются).

Использование во вьюхах (с Фазы 3, по мере правок страниц):

```xaml
<Path Data="{StaticResource Icon.Visibility}"
      Width="14" Height="14" Stretch="Uniform"
      Fill="{DynamicResource Brand.Gray.600.Brush}"/>
```

## Текущие inline Path (28 Path-элементов, инвентарь по рабочему дереву Ф2, 2026-09-05)

Замена inline-глифов в разметке вьюх — работа фаз, правящих эти страницы
(Ф3–Ф6); в объём Фазы 2 замена не входит (доставлены словарь и карта).
Номера строк соответствуют рабочему набору Ф2; после коммита сдвигаются
любой правкой выше по файлу — при замене сверяйтесь по глифу, не по номеру.

| Где | Строка | Что изображено | Заменяет | Fluent-ключ |
|---|---|---|---|---|
| ResultsView.xaml | 221 | глаз («Предпросмотр») | inline Path | `Icon.Visibility` |
| ResultsView.xaml | 235 | принтер («Печать») | inline Path | `Icon.Print` |
| ResultsView.xaml | 248 | рамка-отчёт («Сохранить») | inline Path | `Icon.Save` |
| ResultsView.xaml | 264, 280 | документ (.md отчёты) | inline Path ×2 | `Icon.Document` |
| ResultsView.xaml | 318 | пламя («Тепловая мощность») | inline Path | `Icon.Fire` |
| ResultsView.xaml | 340 | капля («Объём системы») | inline Path | `Icon.Drop` |
| ResultsView.xaml | 361 | капля-контур («Расход насоса») | inline Path | `Icon.Flow` |
| ResultsView.xaml | 382 | треугольник («Напор насоса») | inline Path | `Icon.Triangle` |
| ResultsView.xaml | 403, 426, 449 | термометр (подача/обратка/рабочая) | inline Path ×3 | `Icon.Temperature` |
| ResultsView.xaml | 471 | календарь («Расш. бак») | inline Path | `Icon.Calendar` |
| ResultsView.xaml | 654 | гребёнка («Коллекторы») | inline Path | `Icon.Manifold` |
| ResultsView.xaml | 680 | ящик («Трубы») | inline Path | `Icon.Pipe` |
| ResultsView.xaml | 695 | бак с плюсом («Расш. бак») | inline Path | `Icon.Gauge` (семантику уточнить в Ф6) |
| ResultsView.xaml | 709 | шестерня-солнце («Насос») | inline Path | `Icon.Pump` |
| ThermalView.xaml | 79 | информационный круг | inline Path | `Icon.Info` |
| ThermalView.xaml | 260 | геометка города | inline Path | `Icon.Building` |
| ThermalView.xaml | 287 | калькулятор («Рассчитать») | inline Path | `Icon.Calculator` |
| ThermalView.xaml | 302 | circular-arrow («Сброс») | inline Path | `Icon.Reset` |
| ClimateView.xaml | 65 | геометка города | inline Path | `Icon.Building` |
| ClimateView.xaml | 92 | термометр («температура воздуха») | inline Path | `Icon.Temperature` |
| ClimateView.xaml | 267 | информационный круг | inline Path | `Icon.Info` |
| CircuitsView.xaml | 211 | часы-стрелки («Пересчёт…») | inline Path | `Icon.Clock` |
| MainWindow.xaml | 151 | шеврон вниз («Отчёт PDF ▾») | inline Path | `Icon.ChevronDown` |
| MainWindow.xaml | 263 | шеврон влево/вправо (сворачивание степпера, Style-триггеры) | inline Path ×2 состояния | `Icon.ChevronLeft` / `Icon.ChevronRight` |
| MainWindow.xaml | 447 | треугольник-скос статус-бара | не иконка — фирменный приём | `SkewPlate.Tip` (Components.Brand) |

Контрольные точки вне src/Views (не в инвентаре 28):

- `src/Controls/RecalcIndicator.xaml` — 2 inline Path глифов пересчёта,
  меняются в Ф7 (оверлей расчёта, бирюзовая ветка);
- глифы состояний степпера «✓ ● ⚠ ⟳» — текстовые, Ф1; при переходе на
  иконки — `Icon.Checkmark` / `Icon.Warning` / `Icon.Clock`.

## Процедура добавления новой иконки

1. Найти имя ассета в `microsoft/fluentui-system-icons` (папка
   `assets/<Display Name>`, файл `ic_fluent_<slug>_24_regular.svg`).
2. Добавить строку `Display Name<TAB>Icon.Ключ` в `tools/icons.txt`.
3. `python tools/gen-fluent-icons.py` (потребуется сеть; кэш SVG —
   `tools/.icon-cache/`, вне git).
4. Закоммитить обновлённые `tools/icons.txt` и `src/Themes/Icons.Fluent.xaml`.
