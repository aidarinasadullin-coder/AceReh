# ПОЛНЫЙ КОНТЕКСТ: Модуль "Гидравлический расчёт"

## СТАТУС: ГОТОВ К РАЗРАБОТКЕ

---

## 1. ЗАДАЧА

Разработать новую вкладку "Гидравлический расчёт" (вкладка "Контура") для расчёта контуров и коллекторов систем снеготаяния РЕХАУ.

**Текущий модуль HydraulicsModule считает только один контур — нужно переписать для работы с таблицей контуров (до 48 контуров = 4 коллектора × 12).**

---

## 2. ТРЕБОВАНИЯ ИЗ ПЛАН РАБОТ.TXT

### Входные данные:

| Параметр | Источник | Значение/Единица |
|----------|----------|------------------|
| q_up (мощность вверх) | ThermalModule.Result | Вт/м² |
| q_down (мощность вниз) | ThermalModule.Result | Вт/м² |
| T_supply (подача) | ThermalModule.Result | °C |
| T_return (обратка) | ThermalModel.Result | °C |
| Труба (D_ext, s) | ThermalModule.SelectedPipe | мм |
| Шаг укладки VA_hk | ThermalModule.PipeSpacing | мм (150/200/250/300) |
| t_cold (расчётная T) | ClimateModule.ColdFiveDayTemperature | °C |
| Тип гликоля | Пользователь | Ethylene/Propylene |
| Концентрация гликоля | Пользователь | % |
| Длины контуров L_hk[] | Пользователь | м |
| Длины подводок L_zul[] | Пользователь | м |
| Шаг подводки VA_zul | По умолчанию | 5 см |
| % q_zul | По умолчанию | 10% |

### НЕ выбирать во вкладке "Контура":
- ❌ Тип трубы —уже выбран в тепловом расчёте
- ❌ Шероховатость — константа 0.007 мм
- ❌ Температуры — равны рассчитанным в тепловом расчёте

### Выбирать во вкладке "Контура":
- ✅ Тип гликоля (этилен/пропилен)
- ✅ Концентрация гликоля (%)
- ✅ Длина контура L_hk (м)
- ✅ Длина подводки L_zul (м)
- ✅ Шаг подводки VA_zul (см, по умолчанию 5)
- ✅ % q_zul (по умолчанию 10%)

### Площадь и длина контура:
- Пользователь вводит **длину контура**
- Площадь вычисляется автоматически: `S = L_hk / (100 /VA_hk)`
- Таблица соответствия длины трубы на 1м²:
  - Шаг 150 мм (15 см): 6.67 м/м²
  - Шаг 200 мм (20 см): 5.00 м/м²
  - Шаг 250 мм (25 см): 4.00 м/м²
  - Шаг 300 мм (30 см): 3.33 м/м²

---

## 3. ФОРМУЛЫ (ИСПРАВЛЕНО В Formulas_Snegotayanie.md)

### Мощность контура:
```
Q_HK = [(L_hk / (100 / VA_hk)) + (L_zul / (100 / VA_zul)) × (q_zul / 100)] × (q_up + q_down)  [Вт]
```

### Расход теплоносителя:
```
V_dot = Q_HK × 3,6 / (ρ × c_p × ΔT)  [л/ч]
где ΔT = T_supply - T_return
```

### Скорость потока:
```
v = V_dot × 4 / (3600 × π × d_inner²) × 10⁶  [м/с]
где d_inner в мм
```

### Число Рейнольдса:
```
Re = 1000 × v × d_inner / ν  [безразмерный]
где d_inner в мм, ν в мм²/с
```

### Коэффициент трения λ:
- Ламинарный (Re < 2300): `λ = 64 / Re`
- Переходный (2300 ≤ Re ≤ 4000): линейная интерполяция
- Турбулентный (Re > 4000): Colebrook-White

### Удельные потери давления:
```
R = 10000 × (v² × ρ × λ) / (2 × d_inner) × 100  [Па/м]
где ρ в г/см³ (≈1.053 для 50% гликоля), d_inner в мм
```

### Потери в вентиле:
```
Δp_Vent = (V_dot / 1000 / Kv)² × 100000 × ρ  [Па]
Kv: HKV-D =1.2; IV 1¼" = 1.45; IV 1½" = 1.5
```

### Суммарные потери:
```
Δp_total = Δp_HK + Δp_Zul + Δp_Vent  [Па]
```

---

## 4. РАСЧЁТ ДЛЯ ДВУХ ТЕМПЕРАТУР

**Важно:** Все контуры и коллектора считаются для ДВУХ температур:

| Режим | Температура | Источник |
|-------|-------------|----------|
| Рабочая | T_mean = (T_supply + T_return) / 2 | ThermalModule |
| Расчётная (холодный пуск) | t_cold (холодная пятидневка) | ClimateModule |

Переключатель режима показывает результат для выбранной температуры.

Для каждой температуры берутся свои значения:
- Плотность ρ (г/см³)
- Вязкость ν (мм²/с)
- Теплоёмкость c_p (кДж/кг·К)

---

## 5. СТРУКТУРА ТАБЛИЦЫ (из gidravlica.xls)

### Столбцы таблицы контуров:

| Столбец | Параметр | Единица | Формула/Источник |
|---------|----------|---------|------------------|
| № | Номер контура | — | Ввод пользователя |
| L_hk | Длина контура | м | Ввод пользователя |
| L_zul | Длина подводки | м | Ввод пользователя |
| L_total | Общая длина | м | L_hk + L_zul |
| S | Площадь | м² | L_hk / (100 / VA_hk) |
| Q_HK | Мощность | Вт | Формула мощности |
| V_dot | Расход | л/ч | Формула расхода |
| v | Скорость | м/с | Формула скорости |
| Re | Рейнольдс | — | Формула Re |
| λ | Коэфф. трения | — | По режиму течения |
| R | Уд. потери | Па/м | Формула R |
| Δp_Rohr | Потери в трубе | Па | L_total × R |
| Δp_Vent | Потери в вентиле | Па | Формула вентиля |
| Δp_total | Суммарные потери | Па | Δp_Rohr + Δp_Vent |
| zu_drosseln | Дросселирование | Па | MAX(Δp) - Δp_total |
| Valve_setting | Настройка вентиля | 1-8 | По таблице |

### Итоги коллектора:
- Количество контуров
- Общая длина труб
- Суммарная мощность
- Суммарный расход
- Потери при рабочей температуре (мбар)
- Потери при расчётной температуре (мбар)- Макс. потери: 320 мбар (ограничение РЕХАУ)

---

## 6. ИНТЕГРАЦИЯ С ДРУГИМИ МОДУЛЯМИ

### IThermalCalculationResult (получить):
```csharp
double PowerUp { get; }           // q_up, Вт/м²
double PowerDown { get; }         // q_down, Вт/м²
double SupplyTemperature { get; } // T_supply, °C
double ReturnTemperature { get; } // T_return, °C
double MeanTemperature { get; }   // T_mean, °C
double DeltaT { get; }            // ΔT, К
PipeType SelectedPipe { get; }    // Труба
double PipeSpacing { get; }       // VA_hk, мм
```

### IClimateData (получить):
```csharp
double ColdFiveDayTemperature { get; } // t_cold, °C
```

### ThermalViewModel (смотреть):
```csharp
PipeType SelectedPipe { get; set; }
double PipeSpacing { get; set; } // в мм!
ThermalCalculationResult Result { get; set; }
```

### Нужные значения из ThermalModule:
- `Result.PowerUp` → q_up
- `Result.PowerDown` → q_down
- `Result.SupplyTemperature` → T_supply
- `Result.ReturnTemperature` → T_return
- `SelectedPipe.OuterDiameter` → D_ext
- `SelectedPipe.WallThickness` → s
- `PipeSpacing` (в мм!) → VA_hk

---

## 7. СОХРАНЯЕМЫЕ КОМПОНЕНТЫ

| Файл |Сохранить? |
|------|-----------|
| Models/Hydraulics/GlycolType.cs | ✅ |
| Models/Hydraulics/GlycolProperties.cs | ✅ |
| Models/Hydraulics/FlowRegime.cs | ✅ |
| Services/Hydraulics/GlycolDataService.cs | ✅ |
| Services/Hydraulics/IGlycolDataService.cs | ✅ |
| Services/Hydraulics/HydraulicCalculator.cs | ⚠️ Переработать |
| Models/Hydraulics/HydraulicParameters.cs | ⚠️ Переработать |
| Models/Hydraulics/HydraulicResult.cs | ⚠️ Переработать |
| ViewModels/Hydraulics/HydraulicsViewModel.cs | ❌ Заменить |
| Views/Hydraulics/HydraulicsView.xaml | ❌ Заменить |

---

## 8. НОВЫЕ ФАЙЛЫ (УЖЕ СОЗДАНЫ)

### Models/Hydraulics/CircuitRow.cs
```csharp
public class CircuitRow
{
    public int CircuitNumber { get; set; }
    public double CircuitLength { get; set; }      // L_hk, м
    public double SupplyLength { get; set; }        // L_zul, м
    public double TotalLength => CircuitLength + SupplyLength;
    public double CircuitArea { get; set; }         // S, м²
    public double PipeSpacing_cm { get; set; }      // VA_hk, см
    public double SupplySpacing_cm { get; set; } = 5.0; // VA_zul, см
    public double SupplyHeatPercent { get; set; } = 10.0; // q_zul, %
    public double Power { get; set; }               // Q_HK, Вт
    public double FlowRate { get; set; }            // V_dot, л/ч
    public double Velocity { get; set; }            // v, м/с
    public CircuitTemperatureResult OperatingResult { get; set; }  // При рабочей T
    public CircuitTemperatureResult DesignResult { get; set; }    // При расчётной T
    public double Throttling { get; set; }          // Дросселирование, Па
    public int RecommendedValveSetting { get; set; } // Настройка 1-8
    public bool IsReferenceCircuit { get; set; }
    public HydraulicMode DisplayMode { get; set; }
    public CircuitTemperatureResult CurrentResult => 
        DisplayMode == HydraulicMode.DesignTemperature ? DesignResult : OperatingResult;
}
```

### Models/Hydraulics/CircuitTemperatureResult.cs
```csharp
public class CircuitTemperatureResult
{
    public double Temperature { get; set; }
    public double Density { get; set; }             // г/см³
    public double KinematicViscosity { get; set; } // мм²/с
    public double ReynoldsNumber { get; set; }
    public FlowRegime FlowRegime { get; set; }
    public double FrictionFactor { get; set; }
    public double PressureLossPerMeter { get; set; } // R, Па/м
    public double CircuitPipeLoss { get; set; }   // Δp_HK, Па
    public double SupplyPipeLoss { get; set; }    // Δp_Zul, Па
    public double ValveLoss { get; set; }         // Δp_Vent, Па
    public double TotalLoss => CircuitPipeLoss + SupplyPipeLoss + ValveLoss;
    public double TotalLoss_mbar => TotalLoss / 100.0;
}
```

### Models/Hydraulics/CollectorSummary.cs
```csharp
public class CollectorSummary
{
    public int CollectorNumber { get; set; }
    public string CollectorType { get; set; } = "HKV-D";
    public double Kv { get; set; } = 1.2;
    public int CircuitCount { get; set; }
    public double TotalPipeLength { get; set; }
    public double TotalPower { get; set; }
    public double TotalFlowRate { get; set; }
    public double PressureLoss_Operating_mbar { get; set; }
    public double PressureLoss_Cold_mbar { get; set; }
    public double MaxCircuitLoss { get; set; }
    public int ReferenceCircuitNumber { get; set; }
    public bool IsPressureExceeded => PressureLoss_Cold_mbar > 320;
}
```

### Models/Hydraulics/HydraulicMode.cs
```csharp
public enum HydraulicMode
{
    OperatingTemperature,  // Рабочая температура
    DesignTemperature     // Расчётная (холодный пуск)
}
```

### Models/Hydraulics/PipeLengthPerArea.cs
```csharp
public static class PipeLengthPerArea
{
    public static double Calculate(double pipeSpacing_cm) => 100.0 / pipeSpacing_cm;
    public static double CalculateArea(double pipeLength_m, double pipeSpacing_cm) => 
        pipeLength_m / (100.0 / pipeSpacing_cm);
    public static double CalculateLength(double area_m2, double pipeSpacing_cm) => 
        area_m2 * (100.0 / pipeSpacing_cm);
    public static readonly double[] StandardSpacings_cm = { 15, 20, 25, 30 };
}
```

---

## 9. СЛЕДУЮЩИЕ ШАГИ РАЗРАБОТКИ

1. ✅ Модели созданы (CircuitRow, CollectorSummary, HydraulicMode)
2. 🔄 Создать CircuitsViewModel
3. ⬜ Создать CircuitsView.xaml с DataGrid
4. ⬜ Реализовать расчёт мощности Q_HK
5. ⬜ Реализовать расчёт при двух температурах
6. ⬜ Добавить подбор коллектора
7. ⬜ Добавить балансировку контуров
8. ⬜ Интегрировать с ThermalModule и ClimateModule

---

## 10. ФАЙЛЫ ПРОЕКТА

**Документация:**
- `D:\IA\ace\docs\Formulas_Snegotayanie.md` — формулы (исправлено)
- `D:\IA\ace\Work\HydraulicsModule\technical_specification.md` — ТЗ
- `D:\IA\ace\Work\HydraulicsModule\architecture.md` — архитектура

**Образец:**
- `D:\IA\ace\план-исправлений\gidravlica.xls` — Excel с таблицей

**Данные:**
- `D:\IA\ace\data\glycol_data.json` — свойства гликолей
- `D:\IA\ace\data\rehau_products.json` — трубы и коллекторы

**Код:**
- `D:\IA\ace\src\Models\Hydraulics\` — модели
- `D:\IA\ace\src\Services\Hydraulics\` — сервисы
- `D:\IA\ace\src\ViewModels\Hydraulics\` — ViewModel
- `D:\IA\ace\src\Views\Hydraulics\` — View

---

## 11. ПРИМЕР РАСЧЁТА

**Исходные данные:**
- L_hk = 100 м
- L_zul = 20 м
- VA_hk = 20 см
- VA_zul = 5 см
- q_zul = 10%
- q_up = 256 Вт/м²
- q_down = 5 Вт/м²
- Труба RAUTHERM S 20×2.0 (D_ext = 20 мм, s = 2 мм, d_inner = 16 мм)
- Гликоль 50%, T_mean = 40°C

**Расчёт:**
```
S_контур = L_hk / (100 / VA_hk) = 100 / 5 = 20 м²
S_подводка = L_zul / (100 / VA_zul) × (q_zul/100) = 20 / 20 × 0.1 = 0.1 м²
S_эфф = 20.1 м²
Q_HK = 20.1 × (256 + 5) = 5246 Вт

При T_mean = 40°C, ρ = 1.053 г/см³, ν = 2.16 мм²/с
V_dot = 5246 × 3.6 / (1053 × 3.21 × 10) = 0.56 м³/ч = 560 л/чv = 560 × 4 / (3600 × π × 16²) × 10⁶ = 0.77 м/с
Re = 1000 × 0.77 × 16 / 2.16 = 5704 (турбулентный)
λ ≈ 0.037
R = 10000 × (0.77² × 1.053 × 0.037) / (2 × 16) × 100 = 728 Па/м
```

---

*Контекст создан: 2026-03-17*