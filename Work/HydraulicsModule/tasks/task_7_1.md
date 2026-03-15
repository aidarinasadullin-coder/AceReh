# Task 7.1: Unit-тесты HydraulicCalculator

**Этап:** 7 - Testing  
**Приоритет:** Высокий  
**Статус:** Не начато  
**Зависимости:** Task 3.1

---

## 1. Цель задачи

Создать unit-тесты для `HydraulicCalculator`.

---

## 2. Создаваемые файлы

### 7.1. HydraulicCalculatorTests.cs

**Путь:** `tests/Services/Hydraulics/HydraulicCalculatorTests.cs`

**Тест-кейсы:**
- `CalculateVelocity_WithValidInput_ReturnsCorrectValue`
- `CalculateReynoldsNumber_WithValidInput_ReturnsCorrectValue`
- `DetermineFlowRegime_Laminar_ReturnsLaminar`
- `DetermineFlowRegime_Transitional_ReturnsTransitional`
- `DetermineFlowRegime_Turbulent_ReturnsTurbulent`
- `CalculateFrictionFactor_Laminar_ReturnsCorrectValue`
- `CalculateFrictionFactor_Transitional_ReturnsInterpolatedValue`
- `CalculateFrictionFactor_Turbulent_ReturnsCorrectValue`
- `CalculatePressureLossPerMeter_ReturnsCorrectValue`
- `CalculateValvePressureLoss_HKV_ReturnsCorrectValue`
- `Calculate_WithValidParameters_ReturnsValidResult`

---

## 3. Критерии приёмки

- [ ] Файл тестов создан
- [ ] Все тесты проходят успешно
- [ ] Покрытие кода > 80%