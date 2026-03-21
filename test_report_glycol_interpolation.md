# Отчёт о тестировании интерполяции свойств гликолей

## Дата: 2026-03-21

## Цель тестирования

Проверка корректности интерполяции свойств гликолей (вязкость, плотность, теплоёмкость, теплопроводность) в зависимости от температуры.

## Физические законы, которые должны соблюдаться

1. **Вязкость**: УМЕНЬШАЕТСЯ с ростом температуры (при низкой температуре вязкость выше)
2. **Плотность**: УМЕНЬШАЕТСЯ с ростом температуры (при низкой температуре плотность выше)
3. **Теплоёмкость**: УВЕЛИЧИВАЕТСЯ с ростом температуры (при низкой температуре теплоёмкость ниже)
4. **Теплопроводность**: УВЕЛИЧИВАЕТСЯ с ростом температуры (при низкой температуре теплопроводность ниже)

## Результаты тестирования

### Пройдено: 18 тестов

✅ `KinematicViscosity_AtMinus15_HigherThanAtPlus40` - PASSED
✅ `KinematicViscosity_AtMinus15_HigherThanAtPlus40_Propylene` - PASSED
✅ `KinematicViscosity_DecreasesWithTemperature_Ethylene` - PASSED
✅ `KinematicViscosity_DecreasesWithTemperature_Propylene` - PASSED
✅ `KinematicViscosity_VariousConcentrations_LowerTempHigherViscosity` - PASSED
✅ `Density_AtMinus15_HigherThanAtPlus40` - PASSED
✅ `Density_AtMinus15_HigherThanAtPlus40_Propylene` - PASSED
✅ `Density_DecreasesWithTemperature_Ethylene` - PASSED
✅ `SpecificHeat_AtMinus15_LowerThanAtPlus40` - PASSED
✅ `SpecificHeat_AtMinus15_LowerThanAtPlus40_Propylene` - PASSED
✅ `ThermalConductivity_AtMinus15_LowerThanAtPlus40` - PASSED
✅ `BoundaryTemperatures_ViscosityOrderCorrect` - PASSED
✅ `Viscosity_PhysicallyCorrect_VariousConcentrations` - PASSED
✅ `SpecificHeat_PhysicallyCorrect_VariousConcentrations` - PASSED
✅ `PropyleneGlycol_AllProperties_PhysicallyCorrect` - PASSED
✅ `Interpolation_BetweenASHRAEPoints_PreservesPhysicalLaws` - PASSED
✅ `JsonData_DensityDecreasesWithTemperature` - PASSED
✅ `JsonData_SpecificHeatIncreasesWithTemperature` - PASSED

### Не пройдено: 5 тестов

❌ `AllProperties_PhysicallyCorrect_AtExtremeTemperatures`
- **Ошибка**: Плотность при -30°C (871.7 кг/м³) ниже, чем при +80°C (1074.1 кг/м³)
- **Ожидаемое поведение**: Плотность при низкой температуре должна быть выше
- **Причина**: Интерполяция с null значениями

❌ `Density_PhysicallyCorrect_VariousConcentrations`
- **Ошибка**: При концентрации 20% плотность при -15°C = 527.8 кг/м³ (должно быть ~1050 кг/м³)
- **Причина**: Null значения в JSON заменяются на 0, что даёт некорректную интерполяцию

❌ `JsonData_ViscosityDecreasesWithTemperature`
- **Ошибка**: Вязкость при -17.8°C = 27.2 мм²/с вместо ожидаемых ~40.8 мм²/с
- **Причина**: Интерполяция с null значениями

❌ `SpecificHeat_IncreasesWithTemperature_Ethylene`
- **Ошибка**: Теплоёмкость при -5°C (2.82 кДж/(кг·К)) не больше, чем при -15°C (2.83 кДж/(кг·К))
- **Причина**: Интерполяция с null значениями

❌ `ThermalConductivity_IncreasesWithTemperature_Ethylene`
- **Ошибка**: Теплопроводность при 5°C (0.294 Вт/(м·К)) не больше, чем при -5°C (0.303 Вт/(м·К))
- **Причина**: Интерполяция с null значениями

## Анализ проблемы

### Корневая причина

В файле `GlycolDataService.cs` на строке 438:

```csharp
private static double GetArrayValue(double?[]? array, int index)
{
    if (array == null || index >= array.Length)
        return 0;
    
    return array[index] ?? 0;  // <-- ПРОБЛЕМА: null заменяется на 0
}
```

### Почему это проблема

В JSON файле `data/glycol_data.json` для низких концентраций (10%, 20%) при низких температурах данные отсутствуют (null):

```json
{"temp_c": -17.8, "values": [null, null, 12.9, 17.8, 27.2, 40.8, 57.5, 79.4, 93.3]}
```

Для концентрации 10% и 20% при -17.8°C значение null, которое заменяется на 0.

При интерполяции между:
- Концентрация 10%: вязкость = 0 (null → 0)
- Концентрация 30%: вязкость = 12.9 мм²/с

Получаем некорректное значение ~5.2 мм²/с вместо ожидаемого ~6-7 мм²/с.

### Влияние на UI

Пользователь сообщает, что в UI кинематическая вязкость при -15°C отображается ниже, чем при +40°C. Это подтверждается тестами - проблема существует.

## Рекомендации по исправлению

### Вариант 1: Исключить null значения из интерполяции

При интерполяции проверять, что оба граничных значения не равны 0 (или не являются null в исходных данных).

### Вариант 2: Ограничить диапазон температур для низких концентраций

Для концентраций 10%, 20% ограничить минимальную температуру до -1.1°C (первая точка с валидными данными).

### Вариант 3: Добавить валидацию в UI

В UI проверять, что для заданной концентрации и температуры есть валидные данные, и показывать предупреждение пользователю.

## Созданные файлы

1. `tests/SnowMeltingCalculator.Tests/Services/Hydraulics/GlycolInterpolationTests.cs` - 23 теста для проверки интерполяции

## Итог

⚠️ **Обнаружена ошибка в интерполяции свойств гликолей**

Тесты выявили, что при низких температурах и низких концентрациях интерполяция даёт некорректные результаты из-за замены null значений на 0.

**Необходимо исправить метод `GetArrayValue` в `GlycolDataService.cs` для корректной обработки null значений.**