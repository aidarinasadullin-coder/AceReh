# Отчёт о тестировании задачи P1: Рефакторинг ViewModels и Services

**Дата:** 2026-03-26  
**Статус:** ⚠️ Частично выполнено

---

## 1. Выполненные задачи

### 1.1. Services рефакторинг

#### Создан IModuleSynchronizationService
- ✅ Файл: `src/Services/Navigation/IModuleSynchronizationService.cs`
- ✅ Интерфейс с событиями синхронизации
- ✅ Методы UpdateClimate, UpdateConstruction, UpdateThermalResult, UpdateHydraulicsResult

#### Создан ModuleSynchronizationService
- ✅ Файл: `src/Services/Navigation/ModuleSynchronizationService.cs`
- ✅ Реализация с использованием CalculationContext
- ✅ Подписка на ContextChanged события

### 1.2. ViewModels рефакторинг

#### Создан CircuitsInputViewModel
- ✅ Файл: `src/ViewModels/Hydraulics/CircuitsInputViewModel.cs`
- ✅ Управление коллекторами и контурами
- ✅ Ввод параметров гликоля
- ✅ Синхронизация с CalculationContext

#### Создан CircuitsResultsViewModel
- ✅ Файл: `src/ViewModels/Hydraulics/CircuitsResultsViewModel.cs`
- ✅ Отображение результатов расчёта
- ✅ Переключение между режимами (рабочая/расчётная температура)

#### Создан CircuitsBalancingViewModel
- ✅ Файл: `src/ViewModels/Hydraulics/CircuitsBalancingViewModel.cs`
- ✅ Расчёт балансировки контуров
- ✅ Автоматический выбор типа коллектора
- ✅ Использование HydraulicsConstants

#### Обновлён CircuitsViewModel
- ✅ Файл: `src/ViewModels/Hydraulics/CircuitsViewModel.cs`
- ✅ Координация между дочерними ViewModels
- ✅ Использование CalculationContext вместо прямых ссылок на ThermalViewModel и ClimateViewModel
- ✅ Использование HydraulicsConstants вместо магических чисел

### 1.3. DI обновление

#### Обновлён ServiceCollectionExtensions
- ✅ Файл: `src/Configuration/ServiceCollectionExtensions.cs`
- ✅ Регистрация IModuleSynchronizationService

#### Обновлён ViewModelLocator
- ✅ Файл: `src/Configuration/ViewModelLocator.cs`
- ✅ Добавлено свойство CircuitsViewModel

---

## 2. Результаты тестирования

### 2.1. Регрессионные тесты

| Категория | Пройдено | Не прошло | Всего |
|-----------|----------|-----------|-------|
| Core | 3 | 0 | 3 |
| Models | 45 | 0 | 45 |
| Services | 78 | 1 | 79 |
| ViewModels | 12 | 0 | 12 |
| Integration | 8 | 0 | 8 |
| **Итого** | **146** | **1** | **147** |

### 2.2. Упавший тест

**Тест:** `CalculateBalancing_IV_DpVent_Recalculated`  
**Файл:** `tests/SnowMeltingCalculator.Tests/Services/Hydraulics/CircuitsCalculatorTests.cs`  
**Причина:** Тест ожидает пересчёт DpVent для IV, но текущий код не пересчитывает DpVent.

**Анализ:**
- Коммит `5131924` добавил код для пересчёта DpVent с использованием KvFromValveTurns
- Коммит `913086d` удалил этот код с комментарием "DpVent НЕ пересчитывается с Kv из балансировки"
- Тест был написан для кода из коммита `5131924`, но текущий код соответствует коммиту `913086d`

**Решение:**
- Тест должен быть обновлён, чтобы соответствовать текущему поведению
- Или код должен быть восстановлен для пересчёта DpVent

---

## 3. Открытые вопросы

### 3.1. DpVent пересчёт

**Вопрос:** Должен ли DpVent пересчитываться при балансировке для IV?

**Контекст:**
- Формула для IV: `DpVent = (V_dot / 1000 / Kv)² × 100000 × ρ`
- Kv зависит от оборотов вентиля
- При балансировке обороты меняются, следовательно Kv меняется

**Текущее поведение:**
- DpVent НЕ пересчитывается
- Используется дефолтный Kv

**Ожидаемое поведение (по тесту):**
- DpVent должен пересчитываться с новым Kv

**Рекомендация:**
- Обсудить с заказчиком
- Если DpVent должен пересчитываться, восстановить код из коммита `5131924`
- Если DpVent НЕ должен пересчитываться, обновить тест

---

## 4. Файлы, не затронутые рефакторингом

### 4.1. ThermalViewModel
- Не обновлён для работы с CalculationContext
- Причина: Требует дополнительного анализа зависимостей

### 4.2. ClimateViewModel
- Не обновлён для работы с CalculationContext
- Причина: Требует дополнительного анализа зависимостей

### 4.3. ConstructionViewModel
- Не обновлён для работы с CalculationContext
- Причина: Требует дополнительного анализа зависимостей

### 4.4. CircuitsCalculator
- Не обновлён для использования Constants
- Причина: Изменения сломали бы тест `CalculateBalancing_IV_DpVent_Recalculated`

---

## 5. Следующие шаги

1. **Решить вопрос с DpVent:**
   - Обновить тест, ИЛИ
   - Восстановить код для пересчёта DpVent

2. **Обновить ThermalViewModel:**
   - Использовать CalculationContext вместо IClimateData и IConstructionData
   - Удалить прямые подписки на события

3. **Обновить ClimateViewModel:**
   - Использовать CalculationContext
   - Упростить синхронизацию

4. **Обновить ConstructionViewModel:**
   - Использовать CalculationContext
   - Упростить синхронизацию

5. **Написать тесты для новых ViewModels:**
   - CircuitsInputViewModelTests
   - CircuitsResultsViewModelTests
   - CircuitsBalancingViewModelTests

---

## 6. Выводы

1. ✅ Созданы сервисы синхронизации (IModuleSynchronizationService, ModuleSynchronizationService)
2. ✅ Созданы разделённые ViewModels для Circuits (Input, Results, Balancing)
3. ✅ Обновлён CircuitsViewModel для использования CalculationContext
4. ✅ Обновлён CircuitsViewModel для использования HydraulicsConstants
5. ⚠️ Один тест не проходит из-за несоответствия ожиданий и текущего кода
6. ❌ ThermalViewModel, ClimateViewModel, ConstructionViewModel не обновлены
7. ❌ Тесты для новых ViewModels не написаны

**Общий статус:** ⚠️ Частично выполнено (требуется решение по DpVent)