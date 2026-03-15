# Task 7.3: Unit-тесты HydraulicValidator

**Этап:** 7 - Testing  
**Приоритет:** Высокий  
**Статус:** Не начато  
**Зависимости:** Task 3.4

---

## 1. Цель задачи

Создать unit-тесты для `HydraulicValidator`.

---

## 2. Создаваемые файлы

### 7.3. HydraulicValidatorTests.cs

**Путь:** `tests/Services/Hydraulics/HydraulicValidatorTests.cs`

**Тест-кейсы:**
- `Validate_WithValidParameters_ReturnsValid`
- `Validate_WithInvalidCircuitLength_ReturnsError`
- `Validate_WithInvalidSupplyLength_ReturnsError`
- `Validate_WithInvalidGlycolConcentration_ReturnsError`
- `Validate_WithInvalidTemperatures_ReturnsError`
- `ValidateResult_WithTransitionalFlow_ReturnsWarning`
- `ValidateResult_WithLowVelocity_ReturnsWarning`
- `ValidateResult_WithHighVelocity_ReturnsWarning`

---

## 3. Критерии приёмки

- [ ] Файл тестов создан
- [ ] Все тесты проходят успешно
- [ ] Покрытие кода > 80%