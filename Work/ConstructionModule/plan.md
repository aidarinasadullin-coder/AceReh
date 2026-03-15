# План разработки модуля "Конструктор конструкции" ("Пирог")

**Проект:** Калькулятор снеготаяния РЕХАУ  
**Версия плана:** 1.0  
**Дата:** 2026-03-15  
**ТЗ:** `Work/ConstructionModule/technical_specification.md`  
**Архитектура:** `Work/ConstructionModule/architecture.md`

---

## 1. Обзор разработки

### 1.1. Цель

Создать модуль "Конструктор конструкции" для визуального проектирования слоёв системы снеготаяния с автоматическим расчётом термических сопротивлений R1, R2 и LambdaE.

### 1.2. Подход "Сверху вниз"

**КРИТИЧЕСКИ ВАЖНО:** Система должна работать end-to-end с первой задачи!

- **Этап 1**: Создать ВСЕ классы, методы, ViewModels как заглушки
- **Этапы 2-8**: Постепенно заменять заглушки на реальную реализацию

### 1.3. Покрытие юзер-кейсов

| UC | Название | Приоритет | Задачи |
|----|----------|-----------|--------|
| UC-01 | Добавление слоя материала | Высокий | Task 3.1, 4.1, 5.1 |
| UC-02 | Выбор материала из справочника | Высокий | Task 2.1, 4.1 |
| UC-03 | Задание толщины слоя | Высокий | Task 3.1, 4.1 |
| UC-04 | Удаление слоя | Средний | Task 3.1, 4.1 |
| UC-05 | Учёт уровня грунтовых вод (УГВ) | Высокий | Task 3.2, 4.2 |
| UC-06 | Валидация минимальной стяжки | Средний | Task 3.3 |
| UC-07 | Проверка ограничений по материалам | Низкий | Task 3.3 |
| UC-08 | Визуализация конструкции ("Пирог") | Высокий | Task 5.2 |
| UC-09 | Интеграция с ThermalViewModel | Высокий | Task 6.1, 6.2 |

---

## 2. Структура задач

### Этап 1: Модели данных (Task 1.x)

**Цель:** Создать все модели данных как заглушки с базовой функциональностью.

| Задача | Название | Время | Зависимости |
|--------|----------|-------|-------------|
| Task 1.1 | Создать Material.cs | 1 ч | — |
| Task 1.2 | Создать Layer.cs и LayerPosition.cs | 1 ч | Task 1.1 |
| Task 1.3 | Создать ValidationResult.cs | 0.5 ч | — |
| Task 1.4 | Создать ConstructionTemplate.cs | 0.5 ч | Task 1.1, Task 1.2 |
| Task 1.5 | Создать Construction.cs (реализация IConstructionData) | 2 ч | Task 1.1, Task 1.2, Task 1.3 |

### Этап 2: Репозитории (Task 2.x)

**Цель:** Создать репозитории для загрузки материалов и сохранения конструкций.

| Задача | Название | Время | Зависимости |
|--------|----------|-------|-------------|
| Task 2.1 | Создать IMaterialRepository.cs и MaterialRepository.cs | 2 ч | Task 1.1 |
| Task 2.2 | Создать IConstructionRepository.cs и ConstructionRepository.cs | 2 ч | Task 1.5, Task 1.4 |
| Task 2.3 | Создать data/materials_db.json | 1 ч | — |

### Этап 3: Сервисы (Task 3.x)

**Цель:** Создать сервисы расчёта и валидации.

| Задача | Название | Время | Зависимости |
|--------|----------|-------|-------------|
| Task 3.1 | Создать IConstructionService.cs и ConstructionService.cs | 2 ч | Task 1.5, Task 2.1 |
| Task 3.2 | Реализовать расчёт λ в зависимости от УГВ | 1 ч | Task 3.1 |
| Task 3.3 | Создать ConstructionValidator.cs | 2 ч | Task 1.5 |

### Этап 4: ViewModel (Task 4.x)

**Цель:** Создать ViewModel для управления конструктором.

| Задача | Название | Время | Зависимости |
|--------|----------|-------|-------------|
| Task 4.1 | Создать ConstructionViewModel.cs (базовая структура) | 2 ч | Task 1.5, Task 2.1, Task 3.1 |
| Task 4.2 | Реализовать команды добавления/удаления слоёв | 1 ч | Task 4.1 |
| Task 4.3 | Реализовать обработку изменения УГВ | 1 ч | Task 4.1, Task 3.2 |
| Task 4.4 | Реализовать валидацию в ViewModel | 1 ч | Task 4.1, Task 3.3 |

### Этап 5: View (Task 5.x)

**Цель:** Создать WPF UserControl для визуализации конструктора.

| Задача | Название | Время | Зависимости |
|--------|----------|-------|-------------|
| Task 5.1 | Создать ConstructionView.xaml (базовая разметка) | 2 ч | Task 4.1 |
| Task 5.2 | Реализовать визуализацию "Пирога" (Canvas) | 2 ч | Task 5.1 |
| Task 5.3 | Создать конвертеры для визуализации | 1 ч | Task 5.2 |

### Этап 6: Интеграция (Task 6.x)

**Цель:** Интегрировать модуль с существующей системой.

| Задача | Название | Время | Зависимости |
|--------|----------|-------|-------------|
| Task 6.1 | Обновить ServiceCollectionExtensions.cs (DI) | 1 ч | Task 2.1, Task 2.2, Task 3.1, Task 4.1 |
| Task 6.2 | Обновить ThermalViewModel для работы с Construction | 1 ч | Task 1.5, Task 6.1 |
| Task 6.3 | Добавить ConstructionView в MainWindow | 1 ч | Task 5.1, Task 6.1 |

### Этап 7: Тесты (Task 7.x)

**Цель:** Создать unit-тесты для критических компонентов.

| Задача | Название | Время | Зависимости |
|--------|----------|-------|-------------|
| Task 7.1 | Тесты для MaterialRepository | 1 ч | Task 2.1 |
| Task 7.2 | Тесты для ConstructionService | 2 ч | Task 3.1 |
| Task 7.3 | Тесты для ConstructionValidator | 1 ч | Task 3.3 |
| Task 7.4 | Тесты для ConstructionViewModel | 2 ч | Task 4.1 |

### Этап 8: Документация (Task 8.x)

**Цель:** Обновить документацию проекта.

| Задача | Название | Время | Зависимости |
|--------|----------|-------|-------------|
| Task 8.1 | Обновить README.md | 0.5 ч | Task 6.3 |
| Task 8.2 | Создать CHANGELOG.md | 0.5 ч | Task 6.3 |

---

## 3. Диаграмма зависимостей

```
Task 1.1 (Material)
    │
    ├──► Task 1.2 (Layer, LayerPosition)
    │        │
    │        └──► Task 1.5 (Construction)
    │                   │
    │                   ├──► Task 3.1 (ConstructionService)
    │                   │        │
    │                   │        └──► Task 4.1 (ConstructionViewModel)
    │                   │                   │
    │                   │                   ├──► Task 5.1 (ConstructionView)
    │                   │                   │        │
    │                   │                   │        └──► Task 6.3 (MainWindow)
    │                   │                   │
    │                   │                   └──► Task 7.4 (ViewModel Tests)
    │                   │
    │                   └──► Task 3.3 (Validator)
    │                            │
    │                            └──► Task 4.4 (Validation in VM)
    │
    └──► Task 2.1 (MaterialRepository)
             │
             └──► Task 7.1 (Repository Tests)

Task 1.3 (ValidationResult)
    │
    └──► Task 3.3 (Validator)

Task 1.4 (ConstructionTemplate)
    │
    └──► Task 2.2 (ConstructionRepository)

Task 2.3 (materials_db.json)
    │
    └──► Task 2.1 (MaterialRepository)

Task 6.1 (DI Registration)
    │
    ├──► Task 6.2 (ThermalViewModel)
    │
    └──► Task 6.3 (MainWindow)
```

---

## 4. Критические пути

### Путь 1: End-to-End функциональность

```
Task 1.1 → Task 1.2 → Task 1.5 → Task 3.1 → Task 4.1 → Task 5.1 → Task 6.1 → Task 6.3
```

**Время:** 1 + 1 + 2 + 2 + 2 + 2 + 1 + 1 = 12 часов

### Путь 2: Валидация

```
Task 1.3 → Task 3.3 → Task 4.4
```

**Время:** 0.5 + 2 + 1 = 3.5 часа

### Путь 3: Репозитории

```
Task 1.1 → Task 2.1 → Task 2.3
```

**Время:** 1 + 2 + 1 = 4 часа

---

## 5. Общая оценка времени

| Этап | Время |
|------|-------|
| Этап 1: Модели данных | 5 ч |
| Этап 2: Репозитории | 5 ч |
| Этап 3: Сервисы | 5 ч |
| Этап 4: ViewModel | 5 ч |
| Этап 5: View | 5 ч |
| Этап 6: Интеграция | 3 ч |
| Этап 7: Тесты | 6 ч |
| Этап 8: Документация | 1 ч |
| **ИТОГО** | **35 ч** |

---

## 6. Риски и митигация

| Риск | Вероятность | Влияние | Митигация |
|------|-------------|---------|-----------|
| Несовместимость с существующим IConstructionData | Средняя | Высокое | Проверить интерфейс на этапе Task 1.5 |
| Проблемы с визуализацией Canvas | Средняя | Среднее | Создать прототип визуализации на Task 5.2 |
| Ошибки в формулах расчёта R | Низкая | Высокое | Unit-тесты на Task 7.2 |
| Проблемы с DI регистрацией | Низкая | Среднее | Проверить DI на Task 6.1 |

---

## 7. Приоритеты задач

### P0 (Критические)

- Task 1.1 — Material.cs (базовая модель)
- Task 1.2 — Layer.cs (базовая модель)
- Task 1.5 — Construction.cs (интеграция с IConstructionData)
- Task 3.1 — ConstructionService (расчёт R1/R2)
- Task 4.1 — ConstructionViewModel (MVVM)
- Task 6.1 — DI регистрация

### P1 (Высокие)

- Task 2.1 — MaterialRepository
- Task 2.3 — materials_db.json
- Task 5.1 — ConstructionView.xaml
- Task 6.2 — ThermalViewModel интеграция

### P2 (Средние)

- Task 3.2 — Расчёт λ по УГВ
- Task 3.3 — ConstructionValidator
- Task 5.2 — Визуализация "Пирога"
- Task 7.x — Тесты

### P3 (Низкие)

- Task 1.4 — ConstructionTemplate
- Task 2.2 — ConstructionRepository
- Task 8.x — Документация

---

## 8. Файлы для создания/изменения

### Новые файлы

```
src/Models/Construction/
├── Material.cs
├── Layer.cs
├── LayerPosition.cs
├── Construction.cs
├── ConstructionTemplate.cs
└── ValidationResult.cs

src/Services/Construction/
├── IMaterialRepository.cs
├── MaterialRepository.cs
├── IConstructionService.cs
├── ConstructionService.cs
├── IConstructionRepository.cs
├── ConstructionRepository.cs
└── ConstructionValidator.cs

src/ViewModels/Construction/
└── ConstructionViewModel.cs

src/Views/Construction/
├── ConstructionView.xaml
└── ConstructionView.xaml.cs

src/Converters/
└── LayerToColorConverter.cs (добавить)

data/
└── materials_db.json

tests/Services/Construction/
├── MaterialRepositoryTests.cs
├── ConstructionServiceTests.cs
└── ConstructionValidatorTests.cs

tests/ViewModels/Construction/
└── ConstructionViewModelTests.cs
```

### Изменяемые файлы

```
src/Models/Thermal/IConstructionData.cs (удалить заглушку ConstructionData)
src/Configuration/ServiceCollectionExtensions.cs (добавить регистрацию)
src/ViewModels/Thermal/ThermalViewModel.cs (обновить подписку)
src/MainWindow.xaml (добавить ConstructionView)
```

---

## 9. Следующие шаги

1. Начать с **Task 1.1** — создание Material.cs
2. Параллельно можно начать **Task 2.3** — создание materials_db.json
3. После Task 1.5 проверить интеграцию с ThermalViewModel
4. После Task 6.1 проверить DI регистрацию

---

**Конец документа**