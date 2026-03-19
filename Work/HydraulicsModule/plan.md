# План разработки модуля "Контура" (Гидравлический расчёт)

**Проект:** Калькулятор снеготаяния РЕХАУ  
**Версия плана:** 2.0  
**Дата:** 2026-03-17  
**Статус:** Готов к разработке

---

## 1. Обзор модуля

### 1.1. Цель разработки

Создать модуль для гидравлического расчёта **таблицы контуров** систем снеготаяния РЕХАУ с поддержкой:
- До 48 контуров (4 коллектора × 12 контуров)
- Двух температурных режимов (рабочая и расчётная температура)
- Балансировки контуров на коллекторе
- Подбора коллекторов РЕХАУ (HKV-D, IV)

### 1.2. Ключевые требования

| Параметр | Значение |
|----------|----------|
| Максимум коллекторов | 4 |
| Максимум контуров на коллектор | 12 |
| Итого максимум контуров | 48 |
| Начальное состояние | 1 коллектор с 4 контурами |
| Температурные режимы | Рабочая и расчётная (холодный пуск) |

### 1.3. Связь с юзер-кейсами

| Юзер-кейс | Описание | Приоритет |
|-----------|----------|-----------|
| UC-01 | Ввод параметров контуров (таблица) | Высокий |
| UC-02 | Расчёт мощности контура Q_HK | Высокий |
| UC-03 | Расчёт при двух температурах | Высокий |
| UC-04 | Расчёт потерь давления | Высокий |
| UC-05 | Балансировка контуров | Средний |
| UC-06 | Подбор коллектора | Средний |
| UC-07 | Интеграция с ThermalModule и ClimateModule | Высокий |
| UC-08 | Управление контурами и коллекторами | Высокий |

---

## 2. Этапы разработки

### Этап 1: Модели данных (Task 1.1 - 1.3)

**Цель:** Создать новые модели и обновить существующие

**Задачи:**
- Task 1.1: Создать `ValveType.cs` — enum типов клапанов
- Task 1.2: Создать `HydraulicInputData.cs` — входные данные для расчёта
- Task 1.3: Обновить `CollectorSummary.cs` — добавить ValveType

**Результат:**
- Новые модели: `ValveType`, `HydraulicInputData`
- Обновлённые модели: `CollectorSummary`

**Зависимости:** Нет

---

### Этап 2: Интерфейсы сервисов (Task 2.1)

**Цель:** Создать интерфейсы для калькуляторов

**Задачи:**
- Task 2.1: Создать `ICircuitsCalculator.cs` — интерфейс калькулятора контуров

**Результат:**
- Новый интерфейс: `ICircuitsCalculator`

**Зависимости:** Этап 1 (модели)

---

### Этап 3: Сервисы расчёта (Task 3.1 - 3.3)

**Цель:** Реализовать калькуляторы для расчёта контуров

**Задачи:**
- Task 3.1: Создать `ValveTurnsCalculator.cs` — расчёт оборотов клапана
- Task 3.2: Создать `CircuitsCalculator.cs` — реализация калькулятора контуров
- Task 3.3: Обновить `FlowRegimeCalculator.cs` — добавить методы для расчёта λ

**Результат:**
- Новые сервисы: `ValveTurnsCalculator`, `CircuitsCalculator`
- Обновлённые сервисы: `FlowRegimeCalculator`

**Зависимости:** Этап 1, Этап 2

---

### Этап 4: ViewModels (Task 4.1 - 4.2)

**Цель:** Создать модели представления для UI

**Задачи:**
- Task 4.1: Создать `CircuitsViewModel.cs` — управление таблицей контуров
- Task 4.2: Адаптировать `CollectorViewModel.cs` — управление коллектором

**Результат:**
- Новые ViewModels: `CircuitsViewModel`
- Обновлённые ViewModels: `CollectorViewModel`

**Зависимости:** Этап 3

---

### Этап 5: Views (Task 5.1 - 5.2)

**Цель:** Создать представления для UI

**Задачи:**
- Task 5.1: Создать `CircuitsView.xaml` — таблица контуров с DataGrid
- Task 5.2: Создать `CircuitsView.xaml.cs` — code-behind

**Результат:**
- Новые Views: `CircuitsView.xaml`, `CircuitsView.xaml.cs`

**Зависимости:** Этап 4

---

### Этап 6: Интеграция (Task 6.1 - 6.5)

**Цель:** Интегрировать модуль с системой

**Задачи:**
- Task 6.1: Создать `ServiceCollectionExtensions.cs` — регистрация сервисов в DI
- Task 6.2: Интеграция с `ThermalModule` — получение q_up, q_down, температур
- Task 6.3: Интеграция с `ClimateModule` — получение t_cold
- Task 6.4: Обновить `MainWindow.xaml` — добавить вкладку "Контура"
- Task 6.5: Удаление устаревших моделей — миграция на новые модели

**Результат:**
- DI регистрация сервисов
- Интеграция с ThermalModule и ClimateModule
- Новая вкладка в главном окне
- Устаревшие модели удалены

**Зависимости:** Этап 5

---

### Этап 7: Тестирование (Task 7.1 - 7.4)

**Цель:** Создать тесты для формул и расчётов

**Задачи:**
- Task 7.1: Тесты для `ValveTurnsCalculator` — формулы оборотов клапана
- Task 7.2: Тесты для `CircuitsCalculator` — расчёт мощности, расхода, потерь
- Task 7.3: Тесты для `CircuitsViewModel` — управление контурами
- Task 7.4: Интеграционные тесты — полный цикл расчёта

**Результат:**
- Unit-тесты для сервисов
- Unit-тесты для ViewModels
- Интеграционные тесты

**Зависимости:** Этап 6

---

## 3. Зависимости между задачами

```
Этап 1 (Модели)
    │
    ├── Task 1.1: ValveType.cs
    ├── Task 1.2: HydraulicInputData.cs
    └── Task 1.3: Обновить CollectorSummary.cs
          │
          ▼
Этап 2 (Интерфейсы)
    │
    └── Task 2.1: ICircuitsCalculator.cs
          │
          ▼
Этап 3 (Сервисы)
    │
    ├── Task 3.1: ValveTurnsCalculator.cs
    ├── Task 3.2: CircuitsCalculator.cs
    └── Task 3.3: Обновить FlowRegimeCalculator.cs
          │
          ▼
Этап 4 (ViewModels)
    │
    ├── Task 4.1: CircuitsViewModel.cs
    └── Task 4.2: Адаптировать CollectorViewModel.cs
          │
          ▼
Этап 5 (Views)
    │
    ├── Task 5.1: CircuitsView.xaml
    └── Task 5.2: CircuitsView.xaml.cs
          │
          ▼
Этап 6 (Интеграция)
    │
    ├── Task 6.1: ServiceCollectionExtensions.cs
    ├── Task 6.2: Интеграция с ThermalModule
    ├── Task 6.3: Интеграция с ClimateModule
    ├── Task 6.4: Обновить MainWindow.xaml
    └── Task 6.5: Удаление устаревших моделей
          │
          ▼
Этап 7 (Тестирование)
    │
    ├── Task 7.1: Тесты ValveTurnsCalculator
    ├── Task 7.2: Тесты CircuitsCalculator
    ├── Task 7.3: Тесты CircuitsViewModel
    └── Task 7.4: Интеграционные тесты
```

---

## 4. Детальный план задач

### Task 1.1: Создать ValveType.cs

**Файл:** `src/Models/Hydraulics/ValveType.cs`

**Цель:** Создать enum для типов балансировочных клапанов

**Связанные юзер-кейсы:** UC-05, UC-06

**Описание:**
```csharp
namespace SnowMeltingCalculator.Models.Hydraulics
{
    public enum ValveType
    {
        HKV_D = 0,    // Бытовой коллектор
        IV_1_25 = 1,  // IV 1¼"
        IV_1_5 = 2    // IV 1½"
    }
}
```

**Критерии приёмки:**
- ✅ Enum содержит три значения: HKV_D, IV_1_25, IV_1_5
- ✅ XML-документация для каждого значения

---

### Task 1.2: Создать HydraulicInputData.cs

**Файл:** `src/Models/Hydraulics/HydraulicInputData.cs`

**Цель:** Создать класс для входных данных гидравлического расчёта

**Связанные юзер-кейсы:** UC-01, UC-07

**Описание:**
Класс должен содержать:
- Данные из ThermalModule: PowerUp, PowerDown, SupplyTemperature, ReturnTemperature, InnerDiameter, PipeSpacing_mm
- Данные из ClimateModule: ColdFiveDayTemperature
- Данные от пользователя: GlycolType, GlycolConcentration, SupplySpacing_cm, SupplyHeatPercent
- Вычисляемые свойства: OperatingTemperature, DesignTemperature, DeltaT, PipeSpacing_cm

**Критерии приёмки:**
- ✅ Все свойства из ТЗ реализованы
- ✅ Вычисляемые свойства корректны
- ✅ XML-документация

---

### Task 1.3: Обновить CollectorSummary.cs

**Файл:** `src/Models/Hydraulics/CollectorSummary.cs`

**Цель:** Добавить свойство ValveType

**Связанные юзер-кейсы:** UC-05, UC-06

**Изменения:**
- Добавить свойство `ValveType ValveType { get; set; }`
- Обновить XML-документацию

**Критерии приёмки:**
- ✅ Свойство ValveType добавлено
- ✅ Значение по умолчанию: HKV_D

---

### Task 2.1: Создать ICircuitsCalculator.cs

**Файл:** `src/Services/Hydraulics/ICircuitsCalculator.cs`

**Цель:** Создать интерфейс калькулятора контуров

**Связанные юзер-кейсы:** UC-02, UC-03, UC-04, UC-05, UC-06

**Методы:**
```csharp
public interface ICircuitsCalculator
{
    double CalculateCircuitPower(CircuitRow circuit, double q_up, double q_down);
    double CalculateFlowRate(double power, double deltaT, double density, double specificHeat);
    CircuitTemperatureResult CalculateAtTemperature(CircuitRow circuit, double temperature, 
        GlycolProperties glycolProps, double innerDiameter, double kv);
    List<CircuitRow> CalculateAllCircuits(List<CircuitRow> circuits, HydraulicInputData inputData);
    List<CircuitRow> CalculateBalancing(List<CircuitRow> circuits, ValveType valveType);
    CollectorSummary CalculateCollectorSummary(List<CircuitRow> circuits, int collectorNumber, ValveType valveType);
}
```

**Критерии приёмки:**
- ✅ Все методы из ТЗ реализованы
- ✅ XML-документация для каждого метода

---

### Task 3.1: Создать ValveTurnsCalculator.cs

**Файл:** `src/Services/Hydraulics/ValveTurnsCalculator.cs`

**Цель:** Реализовать расчёт оборотов балансировочного клапана

**Связанные юзер-кейсы:** UC-05

**Формулы:**
- IV 1½": Обороты = 5.122 × Kv - 0.2106
- IV 1¼": Обороты = 5.1818 × Kv - 0.23
- HKV-D: Обороты = 4.2111×Kv³ - 6.7436×Kv² + 4.6613×Kv - 0.712

**Методы:**
- `CalculateTurns(double kv, ValveType valveType)` — расчёт оборотов
- `GetDefaultKv(ValveType valveType)` — получение Kv по типу клапана
- `GetValveTypeName(ValveType valveType)` — название клапана
- `IsValidKv(double kv, ValveType valveType)` — проверка диапазона Kv

**Критерии приёмки:**
- ✅ Формулы реализованы корректно
- ✅ Результат округляется до 0.1 оборота
- ✅ Unit-тесты для всех формул

---

### Task 3.2: Создать CircuitsCalculator.cs

**Файл:** `src/Services/Hydraulics/CircuitsCalculator.cs`

**Цель:** Реализовать калькулятор контуров

**Связанные юзер-кейсы:** UC-02, UC-03, UC-04, UC-05, UC-06

**Методы:**

1. **CalculateCircuitPower** — расчёт мощности Q_HK
   - Формула: Q_HK = [(L_hk/(100/VA_hk)) + (L_zul/(100/VA_zul))×(q_zul/100)] × (q_up + q_down)

2. **CalculateFlowRate** — расчёт расхода V_dot
   - Формула: V_dot = Q_HK × 3.6 / (ρ × c_p × ΔT)

3. **CalculateAtTemperature** — расчёт при температуре
   - Скорость: v = V_dot × 4 / (3600 × π × d_inner²) × 10⁶
   - Re = 1000 × v × d_inner / ν
   - λ по режиму течения
   - R = 10000 × (v² × ρ × λ) / (2 × d_inner) × 100
   - Δp_Vent = (V_dot / 1000 / Kv)² × 100000 × ρ

4. **CalculateAllCircuits** — расчёт всех контуров

5. **CalculateBalancing** — балансировка контуров
   - zu_drosseln = Δp_max - Δp_total

6. **CalculateCollectorSummary** — итоги коллектора

**Критерии приёмки:**
- ✅ Все формулы реализованы корректно
- ✅ Расчёт для двух температур
- ✅ Unit-тесты для всех методов

---

### Task 3.3: Обновить FlowRegimeCalculator.cs

**Файл:** `src/Services/Hydraulics/FlowRegimeCalculator.cs`

**Цель:** Добавить методы для расчёта коэффициента трения λ

**Связанные юзер-кейсы:** UC-03, UC-04

**Изменения:**
- Добавить метод `CalculateFrictionFactor(double reynolds, double innerDiameter, double roughness)`
- Реализовать формулы:
  - Ламинарный: λ = 64 / Re
  - Переходный: линейная интерполяция
  - Турбулентный: Colebrook-White

**Критерии приёмки:**
- ✅ Метод CalculateFrictionFactor добавлен
- ✅ Формулы реализованы корректно
- ✅ Unit-тесты для всех режимов

---

### Task 4.1: Создать CircuitsViewModel.cs

**Файл:** `src/ViewModels/Hydraulics/CircuitsViewModel.cs`

**Цель:** Создать ViewModel для управления таблицей контуров

**Связанные юзер-кейсы:** UC-01, UC-08

**Свойства:**
- `ObservableCollection<CollectorViewModel> Collectors` — список коллекторов
- `int SelectedCollectorIndex` — выбранный коллектор
- `HydraulicMode CurrentMode` — режим отображения
- `GlycolType GlycolType` — тип гликоля
- `double GlycolConcentration` — концентрация
- `bool CanAddCollector` — можно добавить коллектор
- `bool CanAddCircuit` — можно добавить контур

**Команды:**
- `AddCollectorCommand` — добавить коллектор
- `RemoveCollectorCommand` — удалить коллектор
- `AddCircuitCommand` — добавить контур
- `RemoveCircuitCommand` — удалить контур
- `CalculateCommand` — выполнить расчёт
- `SwitchModeCommand` — переключить режим

**Критерии приёмки:**
- ✅ Все свойства и команды реализованы
- ✅ INotifyPropertyChanged для всех свойств
- ✅ Начальное состояние: 1 коллектор с 4 контурами
- ✅ Максимум 4 коллектора, 12 контуров на коллектор

---

### Task 4.2: Адаптировать CollectorViewModel.cs

**Файл:** `src/ViewModels/Hydraulics/CollectorViewModel.cs`

**Цель:** Адаптировать существующую ViewModel для работы с CircuitRow

**Связанные юзер-кейсы:** UC-06

**Изменения:**
- Добавить свойство `ObservableCollection<CircuitRow> Circuits`
- Добавить свойство `CollectorSummary Summary`
- Добавить свойство `ValveType ValveType`
- Добавить свойство `bool CanAddCircuit`

**Критерии приёмки:**
- ✅ Свойства добавлены
- ✅ INotifyPropertyChanged реализовано

---

### Task 5.1: Создать CircuitsView.xaml

**Файл:** `src/Views/Hydraulics/CircuitsView.xaml`

**Цель:** Создать представление для таблицы контуров

**Связанные юзер-кейсы:** UC-01, UC-08

**Структура:**
- Переключатель режима (Рабочая/Расчётная температура)
- Параметры теплоносителя (тип гликоля, концентрация)
- ItemsControl для коллекторов
- DataGrid для контуров
- Кнопки управления (+ Добавить контур, + Добавить коллектор)
- Итоги коллектора

**Критерии приёмки:**
- ✅ DataGrid для таблицы контуров
- ✅ Карточки коллекторов
- ✅ Переключатель режима
- ✅ Кнопки управления
- ✅ Валидация ввода

---

### Task 5.2: Создать CircuitsView.xaml.cs

**Файл:** `src/Views/Hydraulics/CircuitsView.xaml.cs`

**Цель:** Code-behind для CircuitsView

**Связанные юзер-кейсы:** UC-01, UC-08

**Функционал:**
- Инициализация DataContext
- Обработка событий DataGrid
- Валидация ввода

**Критерии приёмки:**
- ✅ DataContext установлен
- ✅ Валидация работает

---

### Task 6.1: Создать ServiceCollectionExtensions.cs

**Файл:** `src/Services/Hydraulics/ServiceCollectionExtensions.cs`

**Цель:** Регистрация сервисов в DI

**Связанные юзер-кейсы:** Все

**Регистрация:**
```csharp
services.AddSingleton<IGlycolDataService, GlycolDataService>();
services.AddSingleton<ICircuitsCalculator, CircuitsCalculator>();
```

**Критерии приёмки:**
- ✅ Сервисы зарегистрированы
- ✅ Зависимости разрешены

---

### Task 6.2: Интеграция с ThermalModule

**Файлы:** `src/ViewModels/Hydraulics/CircuitsViewModel.cs`

**Цель:** Получение данных из ThermalModule

**Связанные юзер-кейсы:** UC-07

**Данные:**
- PowerUp (q_up, Вт/м²)
- PowerDown (q_down, Вт/м²)
- SupplyTemperature (T_supply, °C)
- ReturnTemperature (T_return, °C)
- SelectedPipe (труба с d_inner)
- PipeSpacing (VA_hk, мм)

**Критерии приёмки:**
- ✅ Подписка на события ThermalViewModel
- ✅ Автоматическое обновление при изменении

---

### Task 6.3: Интеграция с ClimateModule

**Файлы:** `src/ViewModels/Hydraulics/CircuitsViewModel.cs`

**Цель:** Получение данных из ClimateModule

**Связанные юзер-кейсы:** UC-07

**Данные:**
- ColdFiveDayTemperature (t_cold, °C)

**Критерии приёмки:**
- ✅ Подписка на события ClimateViewModel
- ✅ Автоматическое обновление при изменении

---

### Task 6.4: Обновить MainWindow.xaml

**Файл:** `src/Views/MainWindow.xaml`

**Цель:** Добавить вкладку "Контура"

**Связанные юзер-кейсы:** Все

**Изменения:**
- Добавить TabItem "Контура"
- Привязать к CircuitsViewModel

**Критерии приёмки:**
- ✅ Вкладка добавлена
- ✅ DataContext привязан

---

### Task 6.5: Удаление устаревших моделей

**Файлы:** Удаление устаревших файлов

**Цель:** Удалить устаревшие модели и заменить их на новые

**Связанные юзер-кейсы:** Все

**Устаревшие модели:**
| Файл | Действие | Замена |
|------|----------|--------|
| `src/Models/Hydraulics/HydraulicParameters.cs` | Удалить | `HydraulicInputData.cs` |
| `src/Models/Hydraulics/HydraulicResult.cs` | Удалить | `CircuitTemperatureResult` |
| `src/Models/Hydraulics/CircuitResult.cs` | Удалить | `CircuitRow.cs` |

**Порядок удаления:**
1. Проверить все ссылки на старые модели
2. Заменить ссылки на новые модели
3. Удалить устаревшие файлы
4. Проверить компиляцию

**Критерии приёмки:**
- ✅ Устаревшие файлы удалены
- ✅ Новые модели используются во всём коде
- ✅ Код компилируется без ошибок
- ✅ Все тесты проходят

---

### Task 7.1: Тесты ValveTurnsCalculator

**Файл:** `tests/Services/Hydraulics/ValveTurnsCalculatorTests.cs`

**Цель:** Unit-тесты для формул оборотов клапана

**Тест-кейсы:**
- TestCalculateTurns_HKV_D — тест для HKV-D
- TestCalculateTurns_IV_1_25 — тест для IV 1¼"
- TestCalculateTurns_IV_1_5 — тест для IV 1½"
- TestGetDefaultKv — тест получения Kv
- TestIsValidKv — тест валидации Kv

**Критерии приёмки:**
- ✅ Все тесты проходят
- ✅ Покрытие > 90%

---

### Task 7.2: Тесты CircuitsCalculator

**Файл:** `tests/Services/Hydraulics/CircuitsCalculatorTests.cs`

**Цель:** Unit-тесты для расчёта контуров

**Тест-кейсы:**
- TestCalculateCircuitPower — тест мощности Q_HK
- TestCalculateFlowRate — тест расхода V_dot
- TestCalculateAtTemperature — тест расчёта при температуре
- TestCalculateAllCircuits — тест расчёта всех контуров
- TestCalculateBalancing — тест балансировки
- TestCalculateCollectorSummary — тест итогов коллектора

**Критерии приёмки:**
- ✅ Все тесты проходят
- ✅ Покрытие > 90%

---

### Task 7.3: Тесты CircuitsViewModel

**Файл:** `tests/ViewModels/Hydraulics/CircuitsViewModelTests.cs`

**Цель:** Unit-тесты для ViewModel

**Тест-кейсы:**
- TestAddCollector — тест добавления коллектора
- TestRemoveCollector — тест удаления коллектора
- TestAddCircuit — тест добавления контура
- TestRemoveCircuit — тест удаления контура
- TestCalculateCommand — тест команды расчёта
- TestSwitchModeCommand — тест переключения режима

**Критерии приёмки:**
- ✅ Все тесты проходят
- ✅ Покрытие > 80%

---

### Task 7.4: Интеграционные тесты

**Файл:** `tests/Integration/HydraulicsIntegrationTests.cs`

**Цель:** Тесты полного цикла расчёта

**Тест-кейсы:**
- TestFullCalculation — полный цикл расчёта
- TestIntegrationWithThermalModule — интеграция с ThermalModule
- TestIntegrationWithClimateModule — интеграция с ClimateModule

**Критерии приёмки:**
- ✅ Все тесты проходят
- ✅ End-to-end сценарий работает

---

## 5. Критические пути

### Критический путь 1: Расчёт мощности Q_HK

```
Task 1.2 (HydraulicInputData)
    → Task 2.1 (ICircuitsCalculator)
    → Task 3.2 (CircuitsCalculator.CalculateCircuitPower)
    → Task 7.2 (Тесты)
```

### Критический путь 2: Расчёт при двух температурах

```
Task 3.2 (CircuitsCalculator.CalculateAtTemperature)
    → Task 3.3 (FlowRegimeCalculator)
    → Task 4.1 (CircuitsViewModel)
    → Task 5.1 (CircuitsView.xaml)
```

### Критический путь 3: Балансировка контуров

```
Task 1.1 (ValveType)
    → Task 3.1 (ValveTurnsCalculator)
    → Task 3.2 (CircuitsCalculator.CalculateBalancing)
    → Task 7.1 (Тесты)
```

---

## 6. Риски и митигация

### Риск 1: Некорректные формулы расчёта

**Вероятность:** Средняя  
**Влияние:** Высокое  
**Митигация:**
- Использовать формулы из `docs/Formulas_Snegotayanie.md`
- Создать unit-тесты для каждой формулы
- Сравнить результаты с Excel-образцом `gidravlica.xls`

### Риск 2: Проблемы с интеграцией ThermalModule/ClimateModule

**Вероятность:** Средняя  
**Влияние:** Среднее  
**Митигация:**
- Определить интерфейсы IThermalCalculationResult и IClimateData
- Создать mock-объекты для тестирования
- Использовать события для обновления данных

### Риск 3: Проблемы с производительностью при 48 контурах

**Вероятность:** Низкая  
**Влияние:** Среднее  
**Митигация:**
- Оптимизировать расчёт (кэширование свойств гликоля)
- Использовать асинхронные вычисления
- Тестировать с максимальным количеством контуров

### Риск 4: Несоответствие UI требованиям

**Вероятность:** Низкая  
**Влияние:** Среднее  
**Митигация:**
- Следовать ТЗ по структуре UI
- Использовать DataGrid для таблицы контуров
- Создать прототип UI перед реализацией

---

## 7. Метрики успеха

### Функциональные метрики

| Метрика | Целевое значение |
|---------|------------------|
| Все юзер-кейсы реализованы | 8/8 (100%) |
| Формулы реализованы корректно | 100% |
| Unit-тесты проходят | 100% |
| Интеграционные тесты проходят | 100% |

### Качественные метрики

| Метрика | Целевое значение |
|---------|------------------|
| Покрытие кода тестами | > 80% |
| XML-документация | 100% методов |
| Время расчёта 48 контуров | < 1 сек |

### Приёмочные критерии

- ✅ Таблица контуров отображает до 48 контуров
- ✅ Расчёт выполняется для двух температур
- ✅ Балансировка контуров работает корректно
- ✅ Итоги коллектора отображаются
- ✅ Интеграция с ThermalModule и ClimateModule работает
- ✅ Все тесты проходят

---

## 8. Устаревшие модели (удалить/заменить)

| Файл | Действие | Замена |
|------|----------|--------|
| `src/Models/Hydraulics/HydraulicParameters.cs` | Удалить | `HydraulicInputData.cs` |
| `src/Models/Hydraulics/HydraulicResult.cs` | Удалить | `CircuitTemperatureResult` (в CircuitRow.cs) |
| `src/Models/Hydraulics/CircuitResult.cs` | Удалить | `CircuitRow.cs` |

---

## 9. Существующие модели (сохранить)

| Файл | Статус |
|------|--------|
| `src/Models/Hydraulics/CircuitRow.cs` | ✅ Сохранить |
| `src/Models/Hydraulics/CircuitTemperatureResult.cs` | ✅ Сохранить (в CircuitRow.cs) |
| `src/Models/Hydraulics/CollectorSummary.cs` | ⚠️ Обновить (добавить ValveType) |
| `src/Models/Hydraulics/HydraulicMode.cs` | ✅ Сохранить |
| `src/Models/Hydraulics/FlowRegime.cs` | ✅ Сохранить |
| `src/Models/Hydraulics/GlycolType.cs` | ✅ Сохранить |
| `src/Models/Hydraulics/GlycolProperties.cs` | ✅ Сохранить |
| `src/Models/Hydraulics/PipeLengthPerArea.cs` | ✅ Сохранить |
| `src/Services/Hydraulics/GlycolDataService.cs` | ✅ Сохранить |
| `src/Services/Hydraulics/IGlycolDataService.cs` | ✅ Сохранить |

---

## 10. Файлы задач

Все задачи детально описаны в отдельных файлах:

- `Work/HydraulicsModule/tasks/task_1_1.md` — ValveType.cs
- `Work/HydraulicsModule/tasks/task_1_2.md` — HydraulicInputData.cs
- `Work/HydraulicsModule/tasks/task_1_3.md` — Обновить CollectorSummary.cs
- `Work/HydraulicsModule/tasks/task_2_1.md` — ICircuitsCalculator.cs
- `Work/HydraulicsModule/tasks/task_3_1.md` — ValveTurnsCalculator.cs
- `Work/HydraulicsModule/tasks/task_3_2.md` — CircuitsCalculator.cs
- `Work/HydraulicsModule/tasks/task_3_3.md` — FlowRegimeCalculator.cs
- `Work/HydraulicsModule/tasks/task_4_1.md` — CircuitsViewModel.cs
- `Work/HydraulicsModule/tasks/task_4_2.md` — CollectorViewModel.cs
- `Work/HydraulicsModule/tasks/task_5_1.md` — CircuitsView.xaml
- `Work/HydraulicsModule/tasks/task_5_2.md` — CircuitsView.xaml.cs
- `Work/HydraulicsModule/tasks/task_6_1.md` — ServiceCollectionExtensions.cs
- `Work/HydraulicsModule/tasks/task_6_2.md` — Интеграция с ThermalModule
- `Work/HydraulicsModule/tasks/task_6_3.md` — Интеграция с ClimateModule
- `Work/HydraulicsModule/tasks/task_6_4.md` — MainWindow.xaml
- `Work/HydraulicsModule/tasks/task_6_5.md` — Удаление устаревших моделей
- `Work/HydraulicsModule/tasks/task_7_1.md` — Тесты ValveTurnsCalculator
- `Work/HydraulicsModule/tasks/task_7_2.md` — Тесты CircuitsCalculator
- `Work/HydraulicsModule/tasks/task_7_3.md` — Тесты CircuitsViewModel
- `Work/HydraulicsModule/tasks/task_7_4.md` — Интеграционные тесты

---

## 11. История изменений

| Версия | Дата | Автор | Изменения |
|--------|------|-------|-----------|
| 1.0 | 2026-03-15 | Планировщик | Начальная версия |
| 2.0 | 2026-03-17 | Планировщик | Обновление по ТЗ v2.0: таблица контуров, две температуры |
| 2.1 | 2026-03-17 | Планировщик | Добавлен Task 6.5: Удаление устаревших моделей |

---

*План создан: 2026-03-17*