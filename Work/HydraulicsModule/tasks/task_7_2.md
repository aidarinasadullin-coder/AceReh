# Task 7.2: Unit-тесты GlycolDataService

**Этап:** 7 - Testing  
**Приоритет:** Высокий  
**Статус:** Не начато  
**Зависимости:** Task 3.3

---

## 1. Цель задачи

Создать unit-тесты для `GlycolDataService`.

---

## 2. Создаваемые файлы

### 7.2. GlycolDataServiceTests.cs

**Путь:** `tests/Services/Hydraulics/GlycolDataServiceTests.cs`

**Тест-кейсы:**
- `GetDensity_WithValidParameters_ReturnsInterpolatedValue`
- `GetSpecificHeat_WithValidParameters_ReturnsInterpolatedValue`
- `GetKinematicViscosity_WithValidParameters_ReturnsInterpolatedValue`
- `GetProperties_ReturnsAllProperties`
- `Interpolation_BetweenTemperatures_ReturnsCorrectValue`
- `Interpolation_BetweenConcentrations_ReturnsCorrectValue`
- `Extrapolation_OutsideRange_ReturnsApproximateValue`

---

## 3. Критерии приёмки

- [ ] Файл тестов создан
- [ ] Все тесты проходят успешно
- [ ] Покрытие кода > 80%