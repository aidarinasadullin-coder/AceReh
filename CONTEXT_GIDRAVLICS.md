# Контекст: Модуль "Гидравлический расчёт"

## 1. ЗАДАЧА
Разработать новую вкладку "Гидравлический расчёт" для расчёта контуров и коллекторов систем снеготаяния РЕХАУ.

## 2. ТРЕБОВАНИЯ

### Входные данные:
| Параметр | Источник | Единица |
|----------|----------|---------|
| q_up (мощность вверх) | ThermalModule | Вт/м² |
| q_down (мощность вниз) | ThermalModule | Вт/м² |
| T_supply (температура подачи) | ThermalModule | °C |
| T_return (температура обратки) | ThermalModule | °C |
| Труба (D_ext, s) | ThermalModule | мм |
| Шаг укладки VA_hk | ThermalModule | см (150/200/250/300) |
| t_cold (расчётная T) | ClimateModule | °C |
| Тип гликоля | Пользователь | Ethylene/Propylene |
| Концентрация гликоля | Пользователь | % |
| Длины контуров L_hk[] | Пользователь | м |
| Длины подводок L_zul[] | Пользователь | м |
| Шаг подводки VA_zul | По умолчанию | 5 см |
| % q_zul | По умолчанию | 10% |

### Функционал:
- Таблица контуров (до 48 = 4 коллектора × 12 контуров)
- Расчёт при рабочей температуре и при расчётной (холодный пуск)
- Подбор коллектора HKV-D / IV 1¼"/ IV1½"
- Балансировка контуров (дросселирование, настройки вентилей)

## 3. ФОРМУЛЫ

### Мощность контура:
```
Q_HK = [(L_hk / (100 / VA_hk)) + (L_zul / (100 / VA_zul)) × (q_zul / 100)] × (q_up + q_down)  [Вт]
```

### Расход:
```
V_dot = Q_HK × 3,6 / (ρ × c_p × ΔT)  [л/ч]
```

### Скорость:
```
v = V_dot × 4 / (3600 × π × d_inner²) × 10⁶  [м/с]
где d_inner в мм
```

### Число Рейнольдса:
```
Re = 1000 × v × d_inner / ν  [безразмерный]
где d_inner в мм, ν в мм²/с
```

### Удельные потери:
```
R = 10000 × (v² × ρ × λ) / (2 × d_inner) × 100  [Па/м]
где ρ в г/см³ (≈1,053), d_inner в мм
```

### Потери в вентиле:
```
Δp_Vent = (V_dot / 1000 / Kv)² × 100000 × ρ  [Па]
Kv: HKV-D = 1,2; IV 1¼" = 1,45; IV 1½" = 1,5
```

## 4. СТРУКТУРА EXCEL (gidravlica.xls)

### Столбцы таблицы контуров:
| Столбец | Параметр | Единица |
|---------|-----------|---------|
| № контура | Heizkreisnr. | — |
| L_hk | Длина контура | м |
| L_zul | Длина подводки | м |
| L_total | Общая длина | м |
| Q_HK | Мощность | Вт |
| V_dot | Расход | л/ч |
| v | Скорость | м/с |
| R | Удельные потери | Па/м |
| Δp_Rohr | Потери в трубе | Па |
| Δp_Vent | Потери в вентиле | Па |
| Δp_total | Суммарные потери | Па |
| zu_drosseln | Дросселирование | Па |
| Valve_setting | Настройка вентиля | 1-8 |

### Итоги коллектора:
- Количество контуров
- Суммарная мощность
- Суммарный расход
- Потери при рабочей T
- Потери при расчётной T

## 5. СОХРАНЯЕМЫЕ КОМПОНЕНТЫ

| Компонент | Путь | Действие |
|-----------|------|----------|
| GlycolDataService | Services/Hydraulics/GlycolDataService.cs | ✅ Сохранить |
| GlycolProperties | Models/Hydraulics/GlycolProperties.cs | ✅ Сохранить |
| GlycolType | Models/Hydraulics/GlycolType.cs | ✅ Сохранить |
| HydraulicCalculator | Services/Hydraulics/HydraulicCalculator.cs | ⚠️ Переработать |
| HydraulicsViewModel | ViewModels/Hydraulics/HydraulicsViewModel.cs | ❌ Заменить |
| HydraulicsView.xaml | Views/Hydraulics/HydraulicsView.xaml | ❌ Заменить |

## 6. ИНТЕГРАЦИЯ

### IThermalCalculationResult (уже есть):
```csharp
double PowerUp { get; }       // q_up, Вт/м²
double PowerDown { get; }     // q_down, Вт/м²
double SupplyTemperature { get; }  // °C
double ReturnTemperature { get; }  // °C
double VolumeFlowRate { get; }     // л/(ч·м²)
```

### Нужно добавить в ThermalModule:
- PipeSpacing (шаг укладки) — VA_hk, см
- SelectedPipe (труба) — D_ext, s

### Нужно из ClimateModule:
- t_5days_092 (температура холодной пятидневки) — для расчёта при "холодном пуске"

## 7. НОВЫЕ КЛАССЫ

```
src/Models/Hydraulics/
├── CircuitRow.cs           # Строка таблицы контура
├── CollectorSummary.cs     # Итоги коллектора
└── HydraulicMode.cs       # Режим: рабочая/расчётная температура

src/ViewModels/Hydraulics/
├── CircuitsViewModel.cs    # Новая ViewModel
└── CircuitRowViewModel.cs  # Строка таблицы

src/Views/Hydraulics/
├── CircuitsView.xaml       # DataGrid контуров
└── CollectorsSummary.xaml  # Сводка коллекторов
```

## 8. ФАЙЛЫ ДЛЯ РАЗРАБОТКИ

**Документация:**
- `docs/Formulas_Snegotayanie.md` — формулы (исправлено)
- `Work/HydraulicsModule/technical_specification.md` — ТЗ
- `Work/HydraulicsModule/architecture.md` — архитектура

**Данные:**
- `data/glycol_data.json` — свойства гликолей
- `data/rehau_products.json` — трубы и коллекторы

**Образец:**
- `план-исправлений/gidravlica.xls` — структура таблицы

## 9. СЛЕДУЮЩИЕ ШАГИ

1. Создать модели CircuitRow, CollectorSummary, HydraulicMode
2. Создать CircuitsViewModel с таблицей контуров
3. Создать CircuitsView.xaml с DataGrid
4. Реализовать расчёт мощности Q_HK
5. Реализовать расчёт при двух температурах
6. Добавить подбор коллектора
7. Добавить балансировку контуров