# Инструкция: Запуск разработки модуля гидравлического расчёта

## Статус готовности

✅ **GlycolDataService исправлен** — можно приступать к разработке модуля гидравлики

---

## Контекстные файлы

Перед запуском в новом окне изучите следующие файлы:

| Файл | Описание |
|------|----------|
| `Work/CONTEXT_GIDRAVLICS_FULL.md` | Полный контекст модуля гидравлики |
| `Work/CONTEXT_HYDRAULICS_MODULE.md` | Контекст диалога разработки |
| `Work/GlycolDataService_Fix/technical_specification.md` | ТЗ по исправлению GlycolDataService |

---

## Быстрый старт

### Шаг 1: Изучить контекст

В новом окне чата выполните:
```
Изучи файлы:
- Work/CONTEXT_GIDRAVLICS_FULL.md
- Work/CONTEXT_HYDRAULICS_MODULE.md
- Work/HydraulicsModule/technical_specification.md
- Work/HydraulicsModule/architecture.md
- Work/HydraulicsModule/plan.md
```

### Шаг 2: Проверить статус

```
Проверь статус модуля гидравлики в Work/HydraulicsModule/status.md
```

### Шаг 3: Запустить разработку

```
Приступай к разработке модуля гидравлический расчёт.
Используй агентов: analyst → reviewer → architect → planner → developer → reviewer
На каждом этапе спрашивай подтверждение перед продолжением.
```

---

## Ключевые моменты

### 1. GlycolDataService уже исправлен

- ✅ JSON парсится корректно
- ✅ Этиленгликоль и пропиленгликоль возвращают разные значения
- ✅ Максимальная температура 90°C
- ✅ Концентрация 0% = вода

### 2. Модели уже созданы

Существующие модели в `src/Models/Hydraulics/`:
- `CircuitRow.cs` — строка таблицы контура
- `CircuitTemperatureResult.cs` — результат при температуре
- `CollectorSummary.cs` — итоги коллектора
- `HydraulicMode.cs` — режим (рабочая/расчётная)
- `PipeLengthPerArea.cs` — расчёт длины трубы

### 3. Требуется разработать

- `CircuitsViewModel.cs` — ViewModel таблицы контуров
- `CircuitsView.xaml` — View с DataGrid
- Расчёт мощности Q_HK
- Расчёт при двух температурах
- Подбор коллектора
- Балансировка контуров

### 4. Формулы (исправлены)

```
Re = 1000 × v × d_inner / ν
R = 10000 × (v² × ρ × λ) / (2 × d_inner) × 100
Δp_Vent = (V_dot / 1000 / Kv)² × 100000 × ρ
```

### 5. Интеграция

- **ThermalModule:** q_up, q_down, T_supply, T_return, SelectedPipe, PipeSpacing
- **ClimateModule:** ColdFiveDayTemperature

---

## Агенты и их роли

| Агент | Роль |
|-------|------|
| `analyst` | Создание/обновление ТЗ |
| `reviewer` | Проверка ТЗ, архитектуры, кода |
| `architect` | Проектирование архитектуры |
| `planner` | Создание плана задач |
| `developer` | Реализация кода |
| `reviewer` | Финальная проверка кода |

---

## Примеры команд

### Создать ТЗ
```
Запусти аналитика для создания ТЗ модуля гидравлический расчёт.
Контекст: Work/CONTEXT_GIDRAVLICS_FULL.md
```

### Проверить ТЗ
```
Запусти ревьювера для проверки ТЗ: Work/HydraulicsModule/technical_specification.md
```

### Разработать задачу
```
Запусти разработчика для реализации задачи: Work/HydraulicsModule/tasks/task_X_Y.md
```

---

## Файловая структура

```
Work/
├── GlycolDataService_Fix/
│   └── technical_specification.md    # ТЗ исправления (выполнено ✅)
├── HydraulicsModule/
│   ├── technical_specification.md    # ТЗ модуля
│   ├── architecture.md               # Архитектура
│   ├── plan.md                       # План задач
│   ├── status.md                     # Статус
│   └── tasks/                        # Задачи
│       ├── task_1_1.md
│       ├── task_1_2.md
│       └── ...
├── CONTEXT_GIDRAVLICS_FULL.md        # Полный контекст
└── CONTEXT_HYDRAULICS_MODULE.md      # Контекст диалога
```

---

## Важно

1. **Не пиши код сам** — используй агентов через task tool
2. **На каждом этапе спрашивай подтверждение** перед продолжением
3. **Проверяй результаты** через reviewer перед следующим этапом
4. **Обновляй status.md** после каждого шага

---

*Инструкция создана: 2026-03-17*