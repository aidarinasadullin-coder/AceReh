# План разработки: Модуль теплового расчёта

**Версия:** 1.0  
**Дата:** 15.03.2026  
**Статус:** Утверждено

---

## 1. Обзор плана

### 1.1. Цель
Реализовать модуль теплового расчёта по методике Chapman-Katunich.

### 1.2. Входные документы
- `Work/ThermalModule/technical_specification.md`
- `Work/ThermalModule/architecture.md`

---

## 2. Задачи

### Задача 2.1: Модели данных

**Приоритет:** Высокий  
**Оценка:** 1 час

#### Файлы
```
src/Models/Thermal/
├── OperatingMode.cs
├── PipeType.cs
├── ThermalParameters.cs
├── ThermalCalculationResult.cs
└── ThermalResultChangedEventArgs.cs
```

#### Критерии приёмки
- ✅ Все модели созданы
- ✅ IThermalCalculationResult определён
- ✅ OperatingMode enum содержит 3 режима

---

### Задача 2.2: Сервис расчёта

**Приоритет:** Высокий  
**Оценка:** 2 часа

#### Файлы
```
src/Services/Thermal/
├── IThermalCalculator.cs
└── ThermalCalculator.cs
```

#### Методы
- `CalculateHeatTransferCoefficient()` — α
- `CalculatePowerUp()` — q_FB
- `CalculateThermalResistance()` — RFb, RD
- `CalculateRodTheory()` — m, ηR
- `CalculateExcessTemperature()` — JHmü
- `Calculate()` — полный расчёт
- `Validate()` — валидация

#### Критерии приёмки
- ✅ Все формулы реализованы
- ✅ Результаты соответствуют тестовым случаям
- ✅ Валидация работает

---

### Задача 2.3: ViewModel

**Приоритет:** Высокий  
**Оценка:** 1.5 часа

#### Файлы
```
src/ViewModels/Thermal/
└── ThermalViewModel.cs
```

#### Критерии приёмки
- ✅ Привязка к UI работает
- ✅ CalculateCommand реализован
- ✅ Результат отображается

---

### Задача 2.4: View (XAML)

**Приоритет:** Высокий  
**Оценка:** 1.5 часа

#### Файлы
```
src/Views/Thermal/
├── ThermalView.xaml
└── ThermalView.xaml.cs
```

#### Элементы UI
- Выбор режима (ComboBox)
- Ввод температур (TextBox)
- Выбор трубы (ComboBox)
- Шаг укладки (TextBox)
- Результаты (DataGrid/TextBlock)

#### Критерии приёмки
- ✅ UI соответствует ТЗ
- ✅ MaterialDesign стили применены

---

### Задача 2.5: DI и интеграция

**Приоритет:** Высокий  
**Оценка:** 0.5 часа

#### Изменения
- `Configuration/ServiceCollectionExtensions.cs` — добавить AddThermalModule()
- `MainWindow.xaml` — добавить пункт меню

#### Критерии приёмки
- ✅ DI настроен
- ✅ Модуль доступен из меню

---

### Задача 2.6: Unit тесты

**Приоритет:** Средний  
**Оценка:** 1 час

#### Файлы
```
tests/SnowMeltingCalculator.Tests/Thermal/
└── ThermalCalculatorTests.cs
```

#### Тест-кейсы
- CalculateHeatTransferCoefficient_ValidInput
- CalculatePowerUp_MoscowWinter
- CalculateRodTheory_ValidInput
- Calculate_FullCalculation
- Validate_InvalidParameters

#### Критерии приёмки
- ✅ Все тесты проходят
- ✅ Покрытие ≥ 80%

---

## 3. Порядок выполнения

```
2.1 Модели данных
    │
    ▼
2.2 Сервис расчёта
    │
    ▼
2.3 ViewModel
    │
    ▼
2.4 View (XAML)
    │
    ▼
2.5 DI и интеграция
    │
    ▼
2.6 Тесты
```

---

## 4. Оценка времени

| Задача | Оценка |
|--------|--------|
| 2.1 Модели данных | 1 час |
| 2.2 Сервис расчёта | 2 часа |
| 2.3 ViewModel | 1.5 часа |
| 2.4 View (XAML) | 1.5 часа |
| 2.5 DI и интеграция | 0.5 часа |
| 2.6 Тесты | 1 час |
| **Итого** | **7.5 часов** |