# Task 3.4: HydraulicValidator (Валидатор)

**Этап:** 3 - Services  
**Приоритет:** Высокий  
**Статус:** Не начато  
**Зависимости:** Task 1.2 (HydraulicParameters), Task 1.3 (HydraulicResult)

---

## 1. Цель задачи

Реализовать класс `HydraulicValidator` для валидации входных параметров и результатов расчёта.

---

## 2. Связь с юзер-кейсами

| UC | Название | Покрытие |
|----|-----------|----------|
| UC-01 | Расчёт гидравлических параметров контура | Валидация параметров |

---

## 3. Создаваемые файлы

### 3.1. ValidationResult.cs

**Путь:** `src/Models/Hydraulics/ValidationResult.cs`

```csharp
using System.Collections.Generic;

namespace SnowMeltingCalculator.Models.Hydraulics
{
    /// <summary>
    /// Результат валидации
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Признак успешной валидации
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Список ошибок
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Список предупреждений
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// Создать успешный результат
        /// </summary>
        public static ValidationResult Success()
        {
            return new ValidationResult { IsValid = true };
        }

        /// <summary>
        /// Создать результат с ошибками
        /// </summary>
        public static ValidationResult Failure(params string[] errors)
        {
            return new ValidationResult
            {
                IsValid = false,
                Errors = new List<string>(errors)
            };
        }

        /// <summary>
        /// Добавить ошибку
        /// </summary>
        public void AddError(string error)
        {
            Errors.Add(error);
            IsValid = false;
        }

        /// <summary>
        /// Добавить предупреждение
        /// </summary>
        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }
    }
}
```

### 3.2. HydraulicValidator.cs

**Путь:** `src/Services/Hydraulics/HydraulicValidator.cs`

```csharp
using System;
using SnowMeltingCalculator.Models.Hydraulics;

namespace SnowMeltingCalculator.Services.Hydraulics
{
    /// <summary>
    /// Валидатор для гидравлических расчётов
    /// </summary>
    public class HydraulicValidator
    {
        #region Константы валидации

        // Пределы длины контура
        private const double MIN_CIRCUIT_LENGTH = 10;
        private const double MAX_CIRCUIT_LENGTH = 500;

        // Пределы длины подводки
        private const double MIN_SUPPLY_LENGTH = 1;
        private const double MAX_SUPPLY_LENGTH = 100;

        // Пределы доли гликоля
        private const double MIN_GLYCOL_CONCENTRATION = 10;
        private const double MAX_GLYCOL_CONCENTRATION = 90;

        // Пределы температуры подачи
        private const double MIN_SUPPLY_TEMPERATURE = 20;
        private const double MAX_SUPPLY_TEMPERATURE = 90;

        // Пределы температуры обратки
        private const double MIN_RETURN_TEMPERATURE = 15;
        private const double MAX_RETURN_TEMPERATURE = 80;

        // Пределы скорости потока (рекомендации)
        private const double MIN_VELOCITY = 0.2;
        private const double MAX_VELOCITY = 1.5;

        // Пределы числа Рейнольдса
        private const double LAMINAR_UPPER_LIMIT = 2300;
        private const double TURBULENT_LOWER_LIMIT = 4000;

        #endregion

        /// <summary>
        /// Валидация входных параметров гидравлического расчёта
        /// </summary>
        /// <param name="parameters">Параметры расчёта</param>
        /// <returns>Результат валидации</returns>
        public ValidationResult Validate(HydraulicParameters parameters)
        {
            var result = new ValidationResult { IsValid = true };

            if (parameters == null)
            {
                result.AddError("Параметры расчёта не указаны");
                return result;
            }

            // Валидация длины контура
            ValidateCircuitLength(parameters.CircuitLength, result);

            // Валидация длины подводки
            ValidateSupplyLength(parameters.SupplyLength, result);

            // Валидация доли гликоля
            ValidateGlycolConcentration(parameters.GlycolConcentration, result);

            // Валидация температур
            ValidateTemperatures(parameters.SupplyTemperature, parameters.ReturnTemperature, result);

            // Валидация трубы
            ValidatePipe(parameters.Pipe, result);

            // Валидация расхода
            ValidateFlowRate(parameters.VolumeFlowRate, result);

            // Валидация площади
            ValidateArea(parameters.CircuitArea, result);

            return result;
        }

        /// <summary>
        /// Валидация результата гидравлического расчёта
        /// </summary>
        /// <param name="result">Результат расчёта</param>
        /// <returns>Результат валидации</returns>
        public ValidationResult ValidateResult(HydraulicResult result)
        {
            var validationResult = new ValidationResult { IsValid = true };

            if (result == null)
            {
                validationResult.AddError("Результат расчёта не указан");
                return validationResult;
            }

            // Проверка скорости потока
            ValidateVelocity(result.Velocity, validationResult);

            // Проверка режима течения
            ValidateFlowRegime(result.ReynoldsNumber, result.FlowRegime, validationResult);

            // Проверка числа Рейнольдса
            ValidateReynoldsNumber(result.ReynoldsNumber, validationResult);

            // Проверка потерь давления
            ValidatePressureLoss(result.PressureLossPerMeter, result.TotalPressureLoss, validationResult);

            return validationResult;
        }

        #region Private Validation Methods

        private void ValidateCircuitLength(double length, ValidationResult result)
        {
            if (double.IsNaN(length) || double.IsInfinity(length))
            {
                result.AddError("Длина контура имеет недопустимое значение");
                return;
            }

            if (length < MIN_CIRCUIT_LENGTH)
            {
                result.AddError($"Длина контура должна быть не менее {MIN_CIRCUIT_LENGTH} м (текущее значение: {length:F2} м)");
            }
            else if (length > MAX_CIRCUIT_LENGTH)
            {
                result.AddError($"Длина контура должна быть не более {MAX_CIRCUIT_LENGTH} м (текущее значение: {length:F2} м)");
            }
        }

        private void ValidateSupplyLength(double length, ValidationResult result)
        {
            if (double.IsNaN(length) || double.IsInfinity(length))
            {
                result.AddError("Длина подводки имеет недопустимое значение");
                return;
            }

            if (length < MIN_SUPPLY_LENGTH)
            {
                result.AddError($"Длина подводки должна быть не менее {MIN_SUPPLY_LENGTH} м (текущее значение: {length:F2} м)");
            }
            else if (length > MAX_SUPPLY_LENGTH)
            {
                result.AddError($"Длина подводки должна быть не более {MAX_SUPPLY_LENGTH} м (текущее значение: {length:F2} м)");
            }
        }

        private void ValidateGlycolConcentration(double concentration, ValidationResult result)
        {
            if (double.IsNaN(concentration) || double.IsInfinity(concentration))
            {
                result.AddError("Доля гликоля имеет недопустимое значение");
                return;
            }

            if (concentration < MIN_GLYCOL_CONCENTRATION)
            {
                result.AddError($"Доля гликоля должна быть не менее {MIN_GLYCOL_CONCENTRATION}% (текущее значение: {concentration:F1}%)");
            }
            else if (concentration > MAX_GLYCOL_CONCENTRATION)
            {
                result.AddError($"Доля гликоля должна быть не более {MAX_GLYCOL_CONCENTRATION}% (текущее значение: {concentration:F1}%)");
            }
        }

        private void ValidateTemperatures(double supplyTemp, double returnTemp, ValidationResult result)
        {
            if (double.IsNaN(supplyTemp) || double.IsInfinity(supplyTemp))
            {
                result.AddError("Температура подачи имеет недопустимое значение");
                return;
            }

            if (double.IsNaN(returnTemp) || double.IsInfinity(returnTemp))
            {
                result.AddError("Температура обратки имеет недопустимое значение");
                return;
            }

            if (supplyTemp < MIN_SUPPLY_TEMPERATURE)
            {
                result.AddError($"Температура подачи должна быть не менее {MIN_SUPPLY_TEMPERATURE}°C (текущее значение: {supplyTemp:F1}°C)");
            }
            else if (supplyTemp > MAX_SUPPLY_TEMPERATURE)
            {
                result.AddError($"Температура подачи должна быть не более {MAX_SUPPLY_TEMPERATURE}°C (текущее значение: {supplyTemp:F1}°C)");
            }

            if (returnTemp < MIN_RETURN_TEMPERATURE)
            {
                result.AddError($"Температура обратки должна быть не менее {MIN_RETURN_TEMPERATURE}°C (текущее значение: {returnTemp:F1}°C)");
            }
            else if (returnTemp > MAX_RETURN_TEMPERATURE)
            {
                result.AddError($"Температура обратки должна быть не более {MAX_RETURN_TEMPERATURE}°C (текущее значение: {returnTemp:F1}°C)");
            }

            // Проверка логической связи температур
            if (supplyTemp <= returnTemp)
            {
                result.AddError($"Температура подачи ({supplyTemp:F1}°C) должна быть выше температуры обратки ({returnTemp:F1}°C)");
            }

            // Проверка перепада температур
            double deltaT = supplyTemp - returnTemp;
            if (deltaT < 2)
            {
                result.AddWarning($"Перепад температур очень мал ({deltaT:F1}°C). Рекомендуемый перепад: 5-15°C");
            }
            else if (deltaT > 25)
            {
                result.AddWarning($"Перепад температур очень велик ({deltaT:F1}°C). Рекомендуемый перепад: 5-15°C");
            }
        }

        private void ValidatePipe(PipeType pipe, ValidationResult result)
        {
            if (pipe == null)
            {
                result.AddError("Тип трубы не указан");
                return;
            }

            if (pipe.OuterDiameter <= 0)
            {
                result.AddError("Наружный диаметр трубы должен быть положительным числом");
            }

            if (pipe.WallThickness <= 0)
            {
                result.AddError("Толщина стенки трубы должна быть положительным числом");
            }

            if (pipe.WallThickness * 2 >= pipe.OuterDiameter)
            {
                result.AddError("Толщина стенки слишком велика для данного наружного диаметра");
            }
        }

        private void ValidateFlowRate(double flowRate, ValidationResult result)
        {
            if (double.IsNaN(flowRate) || double.IsInfinity(flowRate))
            {
                result.AddError("Расход имеет недопустимое значение");
                return;
            }

            if (flowRate <= 0)
            {
                result.AddError("Расход должен быть положительным числом");
            }
        }

        private void ValidateArea(double area, ValidationResult result)
        {
            if (double.IsNaN(area) || double.IsInfinity(area))
            {
                result.AddError("Площадь контура имеет недопустимое значение");
                return;
            }

            if (area <= 0)
            {
                result.AddError("Площадь контура должна быть положительным числом");
            }
        }

        private void ValidateVelocity(double velocity, ValidationResult result)
        {
            if (double.IsNaN(velocity) || double.IsInfinity(velocity))
            {
                result.AddError("Скорость потока имеет недопустимое значение");
                return;
            }

            if (velocity < MIN_VELOCITY)
            {
                result.AddWarning($"Скорость потока ({velocity:F3} м/с) ниже рекомендуемого минимума ({MIN_VELOCITY} м/с). " +
                    "Низкая скорость может привести к неравномерному распределению тепла.");
            }
            else if (velocity > MAX_VELOCITY)
            {
                result.AddWarning($"Скорость потока ({velocity:F3} м/с) выше рекомендуемого максимума ({MAX_VELOCITY} м/с). " +
                    "Высокая скорость увеличивает потери давления и шум.");
            }
        }

        private void ValidateFlowRegime(double reynoldsNumber, FlowRegime regime, ValidationResult result)
        {
            if (regime == FlowRegime.Transitional)
            {
                result.AddWarning($"Режим течения переходный (Re = {reynoldsNumber:F0}). " +
                    $"Рекомендуется ламинарный (Re < {LAMINAR_UPPER_LIMIT}) или турбулентный (Re > {TURBULENT_LOWER_LIMIT}) режим.");
            }
        }

        private void ValidateReynoldsNumber(double reynoldsNumber, ValidationResult result)
        {
            if (double.IsNaN(reynoldsNumber) || double.IsInfinity(reynoldsNumber))
            {
                result.AddError("Число Рейнольдса имеет недопустимое значение");
                return;
            }

            if (reynoldsNumber <= 0)
            {
                result.AddError("Число Рейнольдса должно быть положительным");
            }
        }

        private void ValidatePressureLoss(double pressureLossPerMeter, double totalPressureLoss, ValidationResult result)
        {
            if (double.IsNaN(pressureLossPerMeter) || double.IsInfinity(pressureLossPerMeter))
            {
                result.AddError("Удельные потери давления имеют недопустимое значение");
                return;
            }

            if (double.IsNaN(totalPressureLoss) || double.IsInfinity(totalPressureLoss))
            {
                result.AddError("Общие потери давления имеют недопустимое значение");
                return;
            }

            if (pressureLossPerMeter < 0)
            {
                result.AddError("Удельные потери давления не могут быть отрицательными");
            }

            if (totalPressureLoss < 0)
            {
                result.AddError("Общие потери давления не могут быть отрицательными");
            }

            // Предупреждение о высоких потерях
            if (pressureLossPerMeter > 200)
            {
                result.AddWarning($"Удельные потери давления высоки ({pressureLossPerMeter:F1} Па/м). " +
                    "Рекомендуется увеличить диаметр трубы или уменьшить расход.");
            }

            if (totalPressureLoss > 50000)
            {
                result.AddWarning($"Общие потери давления высоки ({totalPressureLoss / 1000:F1} кПа). " +
                    "Проверьте длину контура и диаметр трубы.");
            }
        }

        #endregion

        #region Static Validation Helpers

        /// <summary>
        /// Быстрая проверка валидности параметров
        /// </summary>
        public static bool IsValidParameters(HydraulicParameters parameters)
        {
            var validator = new HydraulicValidator();
            return validator.Validate(parameters).IsValid;
        }

        /// <summary>
        /// Быстрая проверка валидности результата
        /// </summary>
        public static bool IsValidResult(HydraulicResult result)
        {
            var validator = new HydraulicValidator();
            return validator.ValidateResult(result).IsValid;
        }

        #endregion
    }
}
```

---

## 4. Тест-кейсы

### 4.1. Unit-тесты

**Файл:** `tests/Services/Hydraulics/HydraulicValidatorTests.cs`

```csharp
using SnowMeltingCalculator.Models.Hydraulics;
using SnowMeltingCalculator.Services.Hydraulics;
using NUnit.Framework;

namespace SnowMeltingCalculator.Tests.Services.Hydraulics
{
    [TestFixture]
    public class HydraulicValidatorTests
    {
        private HydraulicValidator _validator;

        [SetUp]
        public void Setup()
        {
            _validator = new HydraulicValidator();
        }

        #region Validate Parameters Tests

        [Test]
        public void Validate_ValidParameters_ReturnsValidResult()
        {
            // Arrange
            var parameters = CreateValidParameters();

            // Act
            var result = _validator.Validate(parameters);

            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors.Count, Is.EqualTo(0));
        }

        [Test]
        public void Validate_NullParameters_ReturnsInvalidResult()
        {
            // Act
            var result = _validator.Validate(null);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Count, Is.GreaterThan(0));
        }

        [Test]
        public void Validate_CircuitLengthTooSmall_ReturnsError()
        {
            // Arrange
            var parameters = CreateValidParameters();
            parameters.CircuitLength = 5; // Меньше минимума (10 м)

            // Act
            var result = _validator.Validate(parameters);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("длина контура"));
        }

        [Test]
        public void Validate_CircuitLengthTooLarge_ReturnsError()
        {
            // Arrange
            var parameters = CreateValidParameters();
            parameters.CircuitLength = 600; // Больше максимума (500 м)

            // Act
            var result = _validator.Validate(parameters);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("длина контура"));
        }

        [Test]
        public void Validate_GlycolConcentrationTooSmall_ReturnsError()
        {
            // Arrange
            var parameters = CreateValidParameters();
            parameters.GlycolConcentration = 5; // Меньше минимума (10%)

            // Act
            var result = _validator.Validate(parameters);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("доля гликоля"));
        }

        [Test]
        public void Validate_SupplyTempLowerThanReturnTemp_ReturnsError()
        {
            // Arrange
            var parameters = CreateValidParameters();
            parameters.SupplyTemperature = 30;
            parameters.ReturnTemperature = 50; // Обратка выше подачи

            // Act
            var result = _validator.Validate(parameters);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("Температура подачи"));
        }

        [Test]
        public void Validate_SmallTemperatureDelta_ReturnsWarning()
        {
            // Arrange
            var parameters = CreateValidParameters();
            parameters.SupplyTemperature = 40;
            parameters.ReturnTemperature = 39; // Перепад 1°C

            // Act
            var result = _validator.Validate(parameters);

            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Warnings.Count, Is.GreaterThan(0));
        }

        [Test]
        public void Validate_InvalidPipe_ReturnsError()
        {
            // Arrange
            var parameters = CreateValidParameters();
            parameters.Pipe = new PipeType { OuterDiameter = 16, WallThickness = 10 }; // Стенка слишком толстая

            // Act
            var result = _validator.Validate(parameters);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("толщина"));
        }

        #endregion

        #region Validate Result Tests

        [Test]
        public void ValidateResult_ValidResult_ReturnsValidResult()
        {
            // Arrange
            var result = CreateValidResult();

            // Act
            var validationResult = _validator.ValidateResult(result);

            // Assert
            Assert.That(validationResult.IsValid, Is.True);
        }

        [Test]
        public void ValidateResult_NullResult_ReturnsInvalidResult()
        {
            // Act
            var validationResult = _validator.ValidateResult(null);

            // Assert
            Assert.That(validationResult.IsValid, Is.False);
        }

        [Test]
        public void ValidateResult_LowVelocity_ReturnsWarning()
        {
            // Arrange
            var result = CreateValidResult();
            result.Velocity = 0.1; // Меньше минимума (0.2 м/с)

            // Act
            var validationResult = _validator.ValidateResult(result);

            // Assert
            Assert.That(validationResult.IsValid, Is.True);
            Assert.That(validationResult.Warnings.Count, Is.GreaterThan(0));
            Assert.That(validationResult.Warnings[0], Does.Contain("скорость"));
        }

        [Test]
        public void ValidateResult_HighVelocity_ReturnsWarning()
        {
            // Arrange
            var result = CreateValidResult();
            result.Velocity = 2.0; // Больше максимума (1.5 м/с)

            // Act
            var validationResult = _validator.ValidateResult(result);

            // Assert
            Assert.That(validationResult.IsValid, Is.True);
            Assert.That(validationResult.Warnings.Count, Is.GreaterThan(0));
        }

        [Test]
        public void ValidateResult_TransitionalFlowRegime_ReturnsWarning()
        {
            // Arrange
            var result = CreateValidResult();
            result.ReynoldsNumber = 3000; // Переходный режим
            result.FlowRegime = FlowRegime.Transitional;

            // Act
            var validationResult = _validator.ValidateResult(result);

            // Assert
            Assert.That(validationResult.IsValid, Is.True);
            Assert.That(validationResult.Warnings.Count, Is.GreaterThan(0));
            Assert.That(validationResult.Warnings[0], Does.Contain("переходный"));
        }

        [Test]
        public void ValidateResult_HighPressureLoss_ReturnsWarning()
        {
            // Arrange
            var result = CreateValidResult();
            result.PressureLossPerMeter = 300; // Высокие потери

            // Act
            var validationResult = _validator.ValidateResult(result);

            // Assert
            Assert.That(validationResult.IsValid, Is.True);
            Assert.That(validationResult.Warnings.Count, Is.GreaterThan(0));
        }

        #endregion

        #region Helper Methods

        private HydraulicParameters CreateValidParameters()
        {
            return new HydraulicParameters
            {
                CircuitLength = 100,
                SupplyLength = 10,
                GlycolConcentration = 50,
                GlycolType = GlycolType.Ethylene,
                SupplyTemperature = 50,
                ReturnTemperature = 30,
                Pipe = new PipeType { OuterDiameter = 20, WallThickness = 2 },
                Roughness = 0.007,
                VolumeFlowRate = 10,
                CircuitArea = 20,
                Density = 1053,
                KinematicViscosity = 2.16
            };
        }

        private HydraulicResult CreateValidResult()
        {
            return new HydraulicResult
            {
                Velocity = 0.5,
                ReynoldsNumber = 3700,
                FlowRegime = FlowRegime.Turbulent,
                FrictionFactor = 0.04,
                PressureLossPerMeter = 100,
                TotalPressureLoss = 10000,
                IsValid = true
            };
        }

        #endregion
    }
}
```

---

## 5. Критерии приёмки

- [ ] Файл `HydraulicValidator.cs` создан
- [ ] Файл `ValidationResult.cs` создан
- [ ] Метод `Validate(HydraulicParameters)` работает
- [ ] Метод `ValidateResult(HydraulicResult)` работает
- [ ] Все правила валидации реализованы
- [ ] Предупреждения для граничных случаев
- [ ] Unit-тесты проходят успешно
- [ ] XML-документация для всех методов

---

## 6. Примечания

- Пределы длины контура: 10-500 м
- Пределы длины подводки: 1-100 м
- Пределы доли гликоля: 10-90%
- Пределы температуры подачи: 20-90°C
- Пределы температуры обратки: 15-80°C
- Рекомендуемая скорость: 0.2-1.5 м/с
- Переходный режим (Re 2300-4000) — предупреждение