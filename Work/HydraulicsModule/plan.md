# План разработки модуля "Гидравлический расчёт"

**Проект:** Калькулятор снеготаяния РЕХАУ  
**Версия плана:** 1.0  
**Дата:** 2026-03-15  
**Статус:** Создано

---

## 1. Обзор модуля

### 1.1. Назначение
Модуль "Гидравлический расчёт" предназначен для:
- Расчёта гидравлических параметров контуров системы снеготаяния
- Определения режима течения (ламинарный/переходный/турбулентный)
- Расчёта потерь давления в трубах и вентилях
- Подбора коллекторов РЕХАУ
- Расчёта дросселирования для балансировки контуров

### 1.2. Интеграционные точки
| Компонент | Интерфейс | Назначение |
|-----------|-----------|------------|
| ThermalModule | `IThermalCalculationResult` | Получение расхода, температур |
| GlycolDataService | `IGlycolDataService` | Свойства теплоносителя |
| CollectorRepository | `ICollectorRepository` | Данные о коллекторах РЕХАУ |

### 1.3. Связь с юзер-кейсами
| UC | Название | Приоритет |
|----|----------|-----------|
| UC-01 | Расчёт гидравлических параметров контура | Высокий |
| UC-02 | Определение режима течения | Высокий |
| UC-03 | Расчёт потерь давления в трубе | Высокий |
| UC-04 | Расчёт потерь давления в вентилях | Высокий |
| UC-05 | Подбор коллектора РЕХАУ | Средний |
| UC-06 | Расчёт дросселирования контуров | Средний |
| UC-07 | Загрузка свойств теплоносителя | Высокий |
| UC-08 | Интеграция с ThermalModule | Высокий |

---

## 2. Структура разработки

### 2.1. Этапы разработки

```
Этап 1: Models (Модели данных)
├── Task 1.1: Enums (перечисления)
├── Task 1.2: HydraulicParameters (параметры расчёта)
├── Task 1.3: HydraulicResult (результат расчёта)
├── Task 1.4: Collector (модель коллектора)
├── Task 1.5: CircuitResult (результат контура)
└── Task 1.6: GlycolProperties (свойства гликоля)

Этап 2: Interfaces (Интерфейсы)
├── Task 2.1: IHydraulicCalculator
├── Task 2.2: IGlycolDataService
└── Task 2.3: ICollectorRepository

Этап 3: Services (Сервисы)
├── Task 3.1: HydraulicCalculator (основной калькулятор)
├── Task 3.2: FlowRegimeCalculator (режим течения)
├── Task 3.3: GlycolDataService (свойства гликолей)
├── Task 3.4: HydraulicValidator (валидация)
└── Task 3.5: CollectorRepository (репозиторий коллекторов)

Этап 4: ViewModels (Модели представления)
├── Task 4.1: HydraulicsViewModel (основная ViewModel)
├── Task 4.2: CircuitViewModel (ViewModel контура)
└── Task 4.3: CollectorViewModel (ViewModel коллектора)

Этап 5: Views (Представления)
├── Task 5.1: HydraulicsView.xaml (основное представление)
├── Task 5.2: CircuitInputView.xaml (ввод параметров)
└── Task 5.3: ResultsView.xaml (отображение результатов)

Этап 6: Integration (Интеграция)
├── Task 6.1: DI-регистрация сервисов
├── Task 6.2: Интеграция с ThermalModule
└── Task 6.3: Загрузка данных из JSON

Этап 7: Testing (Тестирование)
├── Task 7.1: Unit-тесты HydraulicCalculator
├── Task 7.2: Unit-тесты GlycolDataService
└── Task 7.3: Unit-тесты HydraulicValidator

Этап 8: Documentation (Документация)
└── Task 8.1: XML-документация
```

---

## 3. Зависимости между задачами

```
Task 1.1 (Enums)
    ↓
Task 1.2 (HydraulicParameters) ──→ Task 1.3 (HydraulicResult)
    ↓                                    ↓
Task 1.4 (Collector)              Task 1.5 (CircuitResult)
    ↓                                    ↓
Task 1.6 (GlycolProperties)       Task 2.1 (IHydraulicCalculator)
    ↓                                    ↓
Task 2.2 (IGlycolDataService)     Task 2.3 (ICollectorRepository)
    ↓                                    ↓
    └────────────────────────────────────┘
                    ↓
            Task 3.1 (HydraulicCalculator)
                    ↓
    ┌───────────────┼───────────────┐
    ↓               ↓               ↓
Task 3.2       Task 3.3       Task 3.4
(FlowRegime)   (GlycolData)   (Validator)
    ↓               ↓               ↓
    └───────────────┼───────────────┘
                    ↓
            Task 3.5 (CollectorRepository)
                    ↓
    ┌───────────────┼───────────────┐
    ↓               ↓               ↓
Task 4.1       Task 4.2       Task 4.3
(HydraulicsVM) (CircuitVM)     (CollectorVM)
    ↓               ↓               ↓
    └───────────────┼───────────────┘
                    ↓
            Task 5.1 (HydraulicsView)
                    ↓
    ┌───────────────┼───────────────┐
    ↓               ↓               ↓
Task 5.2       Task 5.3       Task 6.1
(CircuitInput) (ResultsView)  (DI-регистрация)
    ↓               ↓               ↓
    └───────────────┼───────────────┘
                    ↓
            Task 6.2 (Интеграция с ThermalModule)
                    ↓
            Task 6.3 (Загрузка JSON)
                    ↓
            Task 7.1-7.3 (Тесты)
                    ↓
            Task 8.1 (Документация)
```

---

## 4. Детальный план задач

### Этап 1: Models (Модели данных)

#### Task 1.1: Enums (перечисления)
**Файл:** `Work/HydraulicsModule/tasks/task_1_1.md`

**Создаваемые файлы:**
- `src/Models/Hydraulics/FlowRegime.cs`
- `src/Models/Hydraulics/GlycolType.cs`
- `src/Models/Hydraulics/CollectorType.cs`

**Зависимости:** Нет

**Юзер-кейсы:** UC-01, UC-02, UC-04, UC-05

---

#### Task 1.2: HydraulicParameters (параметры расчёта)
**Файл:** `Work/HydraulicsModule/tasks/task_1_2.md`

**Создаваемые файлы:**
- `src/Models/Hydraulics/HydraulicParameters.cs`

**Зависимости:** Task 1.1

**Юзер-кейсы:** UC-01, UC-07

---

#### Task 1.3: HydraulicResult (результат расчёта)
**Файл:** `Work/HydraulicsModule/tasks/task_1_3.md`

**Создаваемые файлы:**
- `src/Models/Hydraulics/HydraulicResult.cs`

**Зависимости:** Task 1.1

**Юзер-кейсы:** UC-01, UC-02, UC-03, UC-04

---

#### Task 1.4: Collector (модель коллектора)
**Файл:** `Work/HydraulicsModule/tasks/task_1_4.md`

**Создаваемые файлы:**
- `src/Models/Hydraulics/Collector.cs`

**Зависимости:** Task 1.1

**Юзер-кейсы:** UC-04, UC-05

---

#### Task 1.5: CircuitResult (результат контура)
**Файл:** `Work/HydraulicsModule/tasks/task_1_5.md`

**Создаваемые файлы:**
- `src/Models/Hydraulics/CircuitResult.cs`

**Зависимости:** Task 1.3

**Юзер-кейсы:** UC-06

---

#### Task 1.6: GlycolProperties (свойства гликоля)
**Файл:** `Work/HydraulicsModule/tasks/task_1_6.md`

**Создаваемые файлы:**
- `src/Models/Hydraulics/GlycolProperties.cs`

**Зависимости:** Нет

**Юзер-кейсы:** UC-07

---

### Этап 2: Interfaces (Интерфейсы)

#### Task 2.1: IHydraulicCalculator
**Файл:** `Work/HydraulicsModule/tasks/task_2_1.md`

**Создаваемые файлы:**
- `src/Services/Hydraulics/IHydraulicCalculator.cs`

**Зависимости:** Task 1.1, Task 1.2, Task 1.3, Task 1.5

**Юзер-кейсы:** UC-01, UC-02, UC-03, UC-04, UC-06

---

#### Task 2.2: IGlycolDataService
**Файл:** `Work/HydraulicsModule/tasks/task_2_2.md`

**Создаваемые файлы:**
- `src/Services/Hydraulics/IGlycolDataService.cs`

**Зависимости:** Task 1.1, Task 1.6

**Юзер-кейсы:** UC-07

---

#### Task 2.3: ICollectorRepository
**Файл:** `Work/HydraulicsModule/tasks/task_2_3.md`

**Создаваемые файлы:**
- `src/Repositories/Hydraulics/ICollectorRepository.cs`

**Зависимости:** Task 1.1, Task 1.4

**Юзер-кейсы:** UC-05

---

### Этап 3: Services (Сервисы)

#### Task 3.1: HydraulicCalculator (основной калькулятор)
**Файл:** `Work/HydraulicsModule/tasks/task_3_1.md`

**Создаваемые файлы:**
- `src/Services/Hydraulics/HydraulicCalculator.cs`

**Зависимости:** Task 2.1, Task 2.2

**Юзер-кейсы:** UC-01, UC-02, UC-03, UC-04, UC-06

---

#### Task 3.2: FlowRegimeCalculator (режим течения)
**Файл:** `Work/HydraulicsModule/tasks/task_3_2.md`

**Создаваемые файлы:**
- `src/Services/Hydraulics/FlowRegimeCalculator.cs`

**Зависимости:** Task 1.1

**Юзер-кейсы:** UC-02

---

#### Task 3.3: GlycolDataService (свойства гликолей)
**Файл:** `Work/HydraulicsModule/tasks/task_3_3.md`

**Создаваемые файлы:**
- `src/Services/Hydraulics/GlycolDataService.cs`

**Зависимости:** Task 2.2

**Юзер-кейсы:** UC-07

---

#### Task 3.4: HydraulicValidator (валидация)
**Файл:** `Work/HydraulicsModule/tasks/task_3_4.md`

**Создаваемые файлы:**
- `src/Services/Hydraulics/HydraulicValidator.cs`
- `src/Models/Hydraulics/ValidationResult.cs`

**Зависимости:** Task 1.2, Task 1.3

**Юзер-кейсы:** UC-01

---

#### Task 3.5: CollectorRepository (репозиторий коллекторов)
**Файл:** `Work/HydraulicsModule/tasks/task_3_5.md`

**Создаваемые файлы:**
- `src/Repositories/Hydraulics/CollectorRepository.cs`

**Зависимости:** Task 2.3

**Юзер-кейсы:** UC-05

---

### Этап 4: ViewModels (Модели представления)

#### Task 4.1: HydraulicsViewModel (основная ViewModel)
**Файл:** `Work/HydraulicsModule/tasks/task_4_1.md`

**Создаваемые файлы:**
- `src/ViewModels/Hydraulics/HydraulicsViewModel.cs`

**Зависимости:** Task 3.1, Task 3.3, Task 3.4, Task 3.5

**Юзер-кейсы:** UC-01, UC-08

---

#### Task 4.2: CircuitViewModel (ViewModel контура)
**Файл:** `Work/HydraulicsModule/tasks/task_4_2.md`

**Создаваемые файлы:**
- `src/ViewModels/Hydraulics/CircuitViewModel.cs`

**Зависимости:** Task 4.1

**Юзер-кейсы:** UC-01, UC-06

---

#### Task 4.3: CollectorViewModel (ViewModel коллектора)
**Файл:** `Work/HydraulicsModule/tasks/task_4_3.md`

**Создаваемые файлы:**
- `src/ViewModels/Hydraulics/CollectorViewModel.cs`

**Зависимости:** Task 4.1

**Юзер-кейсы:** UC-05

---

### Этап 5: Views (Представления)

#### Task 5.1: HydraulicsView.xaml (основное представление)
**Файл:** `Work/HydraulicsModule/tasks/task_5_1.md`

**Создаваемые файлы:**
- `src/Views/Hydraulics/HydraulicsView.xaml`
- `src/Views/Hydraulics/HydraulicsView.xaml.cs`

**Зависимости:** Task 4.1

**Юзер-кейсы:** UC-01

---

#### Task 5.2: CircuitInputView.xaml (ввод параметров)
**Файл:** `Work/HydraulicsModule/tasks/task_5_2.md`

**Создаваемые файлы:**
- `src/Views/Hydraulics/CircuitInputView.xaml`
- `src/Views/Hydraulics/CircuitInputView.xaml.cs`

**Зависимости:** Task 4.2, Task 5.1

**Юзер-кейсы:** UC-01

---

#### Task 5.3: ResultsView.xaml (отображение результатов)
**Файл:** `Work/HydraulicsModule/tasks/task_5_3.md`

**Создаваемые файлы:**
- `src/Views/Hydraulics/ResultsView.xaml`
- `src/Views/Hydraulics/ResultsView.xaml.cs`

**Зависимости:** Task 4.1, Task 5.1

**Юзер-кейсы:** UC-01, UC-03, UC-04

---

### Этап 6: Integration (Интеграция)

#### Task 6.1: DI-регистрация сервисов
**Файл:** `Work/HydraulicsModule/tasks/task_6_1.md`

**Создаваемые файлы:**
- `src/Configuration/HydraulicsServiceCollectionExtensions.cs`

**Зависимости:** Task 3.1, Task 3.3, Task 3.4, Task 3.5, Task 4.1

**Юзер-кейсы:** Все

---

#### Task 6.2: Интеграция с ThermalModule
**Файл:** `Work/HydraulicsModule/tasks/task_6_2.md`

**Создаваемые файлы:**
- Изменения в `src/ViewModels/Hydraulics/HydraulicsViewModel.cs`

**Зависимости:** Task 4.1, Task 6.1

**Юзер-кейсы:** UC-08

---

#### Task 6.3: Загрузка данных из JSON
**Файл:** `Work/HydraulicsModule/tasks/task_6_3.md`

**Создаваемые файлы:**
- `data/glycol_data.json` (если не существует)
- `data/rehau_products.json` (обновление)

**Зависимости:** Task 3.3, Task 3.5

**Юзер-кейсы:** UC-05, UC-07

---

### Этап 7: Testing (Тестирование)

#### Task 7.1: Unit-тесты HydraulicCalculator
**Файл:** `Work/HydraulicsModule/tasks/task_7_1.md`

**Создаваемые файлы:**
- `tests/Services/Hydraulics/HydraulicCalculatorTests.cs`

**Зависимости:** Task 3.1

**Юзер-кейсы:** UC-01, UC-02, UC-03, UC-04

---

#### Task 7.2: Unit-тесты GlycolDataService
**Файл:** `Work/HydraulicsModule/tasks/task_7_2.md`

**Создаваемые файлы:**
- `tests/Services/Hydraulics/GlycolDataServiceTests.cs`

**Зависимости:** Task 3.3

**Юзер-кейсы:** UC-07

---

#### Task 7.3: Unit-тесты HydraulicValidator
**Файл:** `Work/HydraulicsModule/tasks/task_7_3.md`

**Создаваемые файлы:**
- `tests/Services/Hydraulics/HydraulicValidatorTests.cs`

**Зависимости:** Task 3.4

**Юзер-кейсы:** UC-01

---

### Этап 8: Documentation (Документация)

#### Task 8.1: XML-документация
**Файл:** `Work/HydraulicsModule/tasks/task_8_1.md`

**Создаваемые файлы:**
- Обновление XML-комментариев во всех файлах

**Зависимости:** Все предыдущие задачи

**Юзер-кейсы:** Все

---

## 5. Критические пути

### Критический путь 1: Расчёт гидравлики
```
Task 1.1 → Task 1.2 → Task 1.3 → Task 2.1 → Task 3.1 → Task 4.1 → Task 5.1
```

### Критический путь 2: Свойства теплоносителя
```
Task 1.6 → Task 2.2 → Task 3.3 → Task 6.3
```

### Критический путь 3: Коллекторы
```
Task 1.4 → Task 2.3 → Task 3.5 → Task 6.3
```

---

## 6. Риски и митигация

| Риск | Вероятность | Влияние | Митигация |
|------|-------------|---------|-----------|
| Несовместимость с ThermalModule | Средняя | Высокое | Определить интерфейс IThermalCalculationResult на этапе 2 |
| Ошибки в формулах расчёта | Средняя | Высокое | Unit-тесты с эталонными значениями из Excel |
| Проблемы с интерполяцией гликолей | Низкая | Среднее | Тесты на граничных значениях температуры и концентрации |
| Производительность при 48 контурах | Низкая | Среднее | Оптимизация расчётов, кэширование |

---

## 7. Метрики успеха

| Метрика | Целевое значение |
|---------|------------------|
| Время расчёта одного контура | < 10 мс |
| Время расчёта 48 контуров | < 500 мс |
| Погрешность расчёта λ | < 0.1% |
| Покрытие тестами | > 80% |
| XML-документация | 100% публичных API |

---

## 8. История изменений

| Версия | Дата | Автор | Изменения |
|--------|------|-------|-----------|
| 1.0 | 2026-03-15 | Планировщик | Начальная версия |