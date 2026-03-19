# БЫСТРЫЙ СТАРТ: Модуль Гидравлический расчёт

## Что нужно сделать

Заменить текущий HydraulicsModule (считает один контур) на новый модуль с таблицей контуров до48 контуров.

## Уже созданные файлы

| Файл | Описание |
|------|----------|
| `src/Models/Hydraulics/CircuitRow.cs` | Строка таблицы контура |
| `src/Models/Hydraulics/CircuitTemperatureResult.cs` | Результат при температуре |
| `src/Models/Hydraulics/CollectorSummary.cs` | Итоги коллектора |
| `src/Models/Hydraulics/HydraulicMode.cs` | Режим: Operating/Design |
| `src/Models/Hydraulics/PipeLengthPerArea.cs` | Расчёт длины на м² |

## Нужно создать

1. `CircuitsViewModel.cs` — ViewModel с таблицей CircuitRow[]
2. `CircuitsView.xaml` — DataGrid с контурами
3. Обновить `HydraulicCalculator.cs` — расчёт для двух температур
4. Интегрировать с ThermalModule и ClimateModule

## Ключевые формулы

```csharp
// Мощность контура
Q_HK = [(L_hk/(100/VA_hk)) + (L_zul/(100/VA_zul))*(q_zul/100)] * (q_up + q_down)

//Число Рейнольдса
Re = 1000 * v * d_inner / nu  // d в мм, nu в мм²/с

// Удельные потери
R = 10000 * (v*v * rho * lambda) / (2 * d_inner) * 100  // rho в г/см³, d в мм
```

## Источники данных

| Данные | Источник |
|--------|----------|
| q_up, q_down, T_supply, T_return | ThermalModule.Result |
| Труба, шаг укладки | ThermalViewModel.SelectedPipe, PipeSpacing |
| t_cold (расчётная T) | ClimateModule.ColdFiveDayTemperature |
| Свойства гликоля | GlycolDataService (glycol_data.json) |

## Два режима расчёта

- **OperatingTemperature** — при T_mean = (T_supply + T_return) / 2
- **DesignTemperature** — при t_cold (холодная пятидневка)

Переключатель показывает результат для выбранного режима.

## Файлы контекста

- `CONTEXT_GIDRAVLICS_FULL.md` — полный контекст
- `CONTEXT_GIDRAVLICS.md` — краткий контекст
- `docs/Formulas_Snegotayanie.md` — исправленные формулы