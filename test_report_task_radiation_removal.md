# Отчёт о тестировании: Удаление Q_изл из расчёта q_FB

## Дата: 2026-03-19

## Описание задачи
Удалить параметр Q_изл (лучистый тепловой поток) из основного расчёта мощности q_FB.

## Изменения

### 1. Документация
**Файл:** `docs/Formulas_Snegotayanie.md`
- ✅ Изменена формула: `q_FB = Q_таяние + Q_конв` (без Q_изл)
- ✅ Добавлена справочная формула: `q_FB_ref = Q_таяние + Q_изл + Q_конв`

### 2. Расчёты
**Файл:** `src/Services/Thermal/ThermalCalculator.cs`
- ✅ Метод `CalculatePowerUp`: удалён Q_изл из суммы powerUp
- ✅ Обновлена документация метода
- ✅ RadiationHeat вычисляется отдельно для справки (строки 451-452)

### 3. Модель
**Файл:** `src/Models/Thermal/ThermalCalculationResult.cs`
- ✅ Свойство `RadiationHeat` сохранено как справочное

### 4. UI
**Файл:** `src/Views/Thermal/ThermalView.xaml`
- ✅ Добавлена пометка "(справочно)" для лучистого потока

### 5. Тесты
**Файл:** `tests/SnowMeltingCalculator.Tests/Thermal/ThermalCalculatorTests.cs`
- ✅ Обновлён тест `CalculatePowerUp_ZeroSnowfall_ReturnsConvectionOnly`
- ✅ Добавлен тест `Calculate_RadiationHeat_IsCalculatedForReference`

## Результаты тестирования

### Новые тесты
- ✅ `CalculatePowerUp_ZeroSnowfall_ReturnsConvectionOnly` — PASSED
- ✅ `Calculate_RadiationHeat_IsCalculatedForReference` — PASSED

### Регрессионные тесты
- Всего: 39
- Пройдено: 39
- Не пройдено: 0

## Проверка формул

### До изменений:
```
q_FB = Q_таяние + Q_изл + Q_конв
```

### После изменений:
```
q_FB = Q_таяние + Q_конв
q_FB_ref = Q_таяние + Q_изл + Q_конв (справочно)
```

## Итог
✅ Все тесты прошли успешно
✅ Q_изл удалён из основного расчёта q_FB
✅ Q_изл сохранён как справочное значение (RadiationHeat)
✅ Документация обновлена