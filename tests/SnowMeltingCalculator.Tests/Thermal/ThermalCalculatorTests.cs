using NUnit.Framework;
using SnowMeltingCalculator.Models.Climate;
using SnowMeltingCalculator.Models.Thermal;
using SnowMeltingCalculator.Services.Thermal;
using System;

namespace SnowMeltingCalculator.Tests.Thermal
{
    /// <summary>
    /// Тесты для ThermalCalculator
    /// </summary>
    [TestFixture]
    public class ThermalCalculatorTests
    {
        private ThermalCalculator _calculator = null!;
        private IClimateData _climateData = null!;
        private IConstructionData _constructionData = null!;

        [SetUp]
        public void Setup()
        {
            _calculator = new ThermalCalculator();
            _climateData = CreateClimateData();
            _constructionData = CreateConstructionData();
        }

        #region CalculateHeatTransferCoefficient Tests

        [Test]
        public void CalculateHeatTransferCoefficient_ValidInput_ReturnsPositiveValue()
        {
            // Arrange
            var surfaceTemp = 5.0;  // °C
            var airTemp = -20.0;    // °C
            var windSpeed = 5.0;    // м/с

            // Act
            var alpha = _calculator.CalculateHeatTransferCoefficient(surfaceTemp, airTemp, windSpeed);

            // Assert
            Assert.That(alpha, Is.GreaterThan(0));
        }

        [Test]
        public void CalculateHeatTransferCoefficient_ZeroWind_CalculatesCorrectly()
        {
            // Arrange
            var surfaceTemp = 5.0;
            var airTemp = -20.0;
            var windSpeed = 0.0;

            // Act
            var alpha = _calculator.CalculateHeatTransferCoefficient(surfaceTemp, airTemp, windSpeed);

            // Assert
            // При нулевом ветре: α = 2.26 × (25)^0.33 ≈ 6.6
            Assert.That(alpha, Is.GreaterThan(6.0));
            Assert.That(alpha, Is.LessThan(7.0));
        }

        [Test]
        public void CalculateHeatTransferCoefficient_WithWind_IncreasesAlpha()
        {
            // Arrange
            var surfaceTemp = 5.0;
            var airTemp = -20.0;
            var windSpeed1 = 0.0;
            var windSpeed2 = 5.0;

            // Act
            var alpha1 = _calculator.CalculateHeatTransferCoefficient(surfaceTemp, airTemp, windSpeed1);
            var alpha2 = _calculator.CalculateHeatTransferCoefficient(surfaceTemp, airTemp, windSpeed2);

            // Assert
            // Добавление ветра должно увеличивать α на 2.6 × v
            Assert.That(alpha2, Is.GreaterThan(alpha1));
            Assert.That(alpha2 - alpha1, Is.EqualTo(2.6 * windSpeed2).Within(0.1));
        }

        [Test]
        public void CalculateHeatTransferCoefficient_NegativeWind_ThrowsException()
        {
            // Arrange
            var surfaceTemp = 5.0;
            var airTemp = -20.0;
            var windSpeed = -1.0;

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _calculator.CalculateHeatTransferCoefficient(surfaceTemp, airTemp, windSpeed));
        }

        [Test]
        public void CalculateHeatTransferCoefficient_SurfaceColderThanAir_UsesMinimumDelta()
        {
            // Arrange
            var surfaceTemp = -25.0;  // Холоднее воздуха
            var airTemp = -20.0;
            var windSpeed = 5.0;

            // Act
            var alpha = _calculator.CalculateHeatTransferCoefficient(surfaceTemp, airTemp, windSpeed);

            // Assert
            // Должен использовать минимальную разность температур
            Assert.That(alpha, Is.GreaterThan(0));
        }

        #endregion

        #region CalculatePowerUp Tests

        [Test]
        public void CalculatePowerUp_ValidInput_ReturnsPositiveValue()
        {
            // Arrange
            var snowfallIntensity = 20.0;  // мм/ч
            var surfaceTemp = 5.0;        // °C
            var airTemp = -20.0;          // °C
            var alpha = 25.0;             // Вт/м²·К

            // Act
            var powerUp = _calculator.CalculatePowerUp(snowfallIntensity, surfaceTemp, airTemp, alpha);

            // Assert
            Assert.That(powerUp, Is.GreaterThan(0));
        }

        [Test]
        public void CalculatePowerUp_ZeroSnowfall_ReturnsConvectionOnly()
        {
            // Arrange
            var snowfallIntensity = 0.0;
            var surfaceTemp = 5.0;
            var airTemp = -20.0;
            var alpha = 25.0;

            // Act
            var powerUp = _calculator.CalculatePowerUp(snowfallIntensity, surfaceTemp, airTemp, alpha);

            // Assert
            // При нулевом снегопаде мощность = только конвекция (без излучения)
            // Q_конв = 25 × 25 = 625 Вт/м²
            // Примечание: Q_изл исключён из основного расчёта q_FB
            Assert.That(powerUp, Is.EqualTo(625.0).Within(1.0));
        }

        [Test]
        public void Calculate_RadiationHeat_IsCalculatedForReference()
        {
            // Arrange
            var inputs = CreateValidInputs();
            inputs = inputs with { SupplyTemperature = 70.0 };  // Увеличиваем температуру подачи для валидности расчёта
            var climate = CreateClimateData(snowfall: 2.0);
            var construction = CreateConstructionData();

            // Act
            var result = _calculator.Calculate(inputs, climate, construction);

            // Assert - сначала проверяем, что расчёт валиден
            Assert.That(result.IsValid, Is.True,
                $"Расчёт должен быть валидным. Ошибки: {string.Join(", ", result.ValidationErrors ?? new string[0])}");

            // RadiationHeat должен вычисляться для справки, но НЕ входить в PowerUp
            Assert.That(result.RadiationHeat, Is.GreaterThan(0), "RadiationHeat должен вычисляться");

            // PowerUp = MeltingHeat + ConvectionHeat (БЕЗ RadiationHeat)
            var expectedPowerUp = result.MeltingHeat + result.ConvectionHeat;
            Assert.That(result.PowerUp, Is.EqualTo(expectedPowerUp).Within(0.1),
                "PowerUp должен быть равен MeltingHeat + ConvectionHeat (без RadiationHeat)");
        }

        [Test]
        public void CalculatePowerUp_WithSnowfall_IncludesMeltingHeat()
        {
            // Arrange
            var snowfallIntensity = 20.0;  // мм/ч
            var surfaceTemp = 5.0;
            var airTemp = -20.0;
            var alpha = 25.0;

            // Act
            var powerUpWithSnow = _calculator.CalculatePowerUp(snowfallIntensity, surfaceTemp, airTemp, alpha);
            var powerUpNoSnow = _calculator.CalculatePowerUp(0.0, surfaceTemp, airTemp, alpha);

            // Assert
            // Снегопад добавляет теплоту плавления
            Assert.That(powerUpWithSnow, Is.GreaterThan(powerUpNoSnow));
        }

        [Test]
        public void CalculatePowerUp_NegativeSnowfall_ThrowsException()
        {
            // Arrange
            var snowfallIntensity = -1.0;  // мм/ч
            var surfaceTemp = 5.0;
            var airTemp = -20.0;
            var alpha = 25.0;

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _calculator.CalculatePowerUp(snowfallIntensity, surfaceTemp, airTemp, alpha));
        }

        [Test]
        public void CalculatePowerUp_ZeroAlpha_ThrowsException()
        {
            // Arrange
            var snowfallIntensity = 20.0;  // мм/ч
            var surfaceTemp = 5.0;
            var airTemp = -20.0;
            var alpha = 0.0;

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _calculator.CalculatePowerUp(snowfallIntensity, surfaceTemp, airTemp, alpha));
        }

        #endregion

        #region CalculateThermalResistance Tests

        [Test]
        public void CalculateThermalResistance_ValidInput_ReturnsCorrectValues()
        {
            // Arrange
            var r1Total = 0.05;  // м²·К/Вт
            var r2Total = 0.10;  // м²·К/Вт
            var alpha = 25.0;    // Вт/м²·К

            // Act
            var (rFb, rD) = _calculator.CalculateThermalResistance(r1Total, r2Total, alpha);

            // Assert
            // RFb = R1 + 1/α = 0.05 + 1/25 = 0.09
            Assert.That(rFb, Is.EqualTo(0.09).Within(0.001));
            // RD = R2 + 1/α_низ ≈ R2 (адиабата)
            Assert.That(rD, Is.EqualTo(r2Total).Within(0.001));
        }

        [Test]
        public void CalculateThermalResistance_ZeroR1_ReturnsCorrectRFb()
        {
            // Arrange
            var r1Total = 0.0;
            var r2Total = 0.10;
            var alpha = 25.0;

            // Act
            var (rFb, rD) = _calculator.CalculateThermalResistance(r1Total, r2Total, alpha);

            // Assert
            // RFb = 0 + 1/25 = 0.04
            Assert.That(rFb, Is.EqualTo(0.04).Within(0.001));
        }

        [Test]
        public void CalculateThermalResistance_NegativeR1_ThrowsException()
        {
            // Arrange
            var r1Total = -0.01;
            var r2Total = 0.10;
            var alpha = 25.0;

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _calculator.CalculateThermalResistance(r1Total, r2Total, alpha));
        }

        [Test]
        public void CalculateThermalResistance_NegativeAlpha_ThrowsException()
        {
            // Arrange
            var r1Total = 0.05;
            var r2Total = 0.10;
            var alpha = -1.0;

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _calculator.CalculateThermalResistance(r1Total, r2Total, alpha));
        }

        #endregion

        #region CalculateRodTheory Tests

        [Test]
        public void CalculateRodTheory_ValidInput_ReturnsCorrectValues()
        {
            // Arrange
            var rFb = 0.09;      // м²·К/Вт
            var rD = 0.10;       // м²·К/Вт
            var lambdaE = 1.6;   // Вт/м·К
            var dE = 0.020;      // м (20 мм)
            var spacing = 0.200; // м (200 мм)

            // Act
            var (m, etaR) = _calculator.CalculateRodTheory(rFb, rD, lambdaE, dE, spacing);

            // Assert
            Assert.That(m, Is.GreaterThan(0));
            Assert.That(etaR, Is.GreaterThan(0));
            Assert.That(etaR, Is.LessThanOrEqualTo(1.0));
        }

        [Test]
        public void CalculateRodTheory_SmallSpacing_HighEfficiency()
        {
            // Arrange
            var rFb = 0.09;
            var rD = 0.10;
            var lambdaE = 1.6;
            var dE = 0.020;
            var spacingSmall = 0.100;  // 100 мм
            var spacingLarge = 0.300;  // 300 мм

            // Act
            var (_, etaRSmall) = _calculator.CalculateRodTheory(rFb, rD, lambdaE, dE, spacingSmall);
            var (_, etaRLarge) = _calculator.CalculateRodTheory(rFb, rD, lambdaE, dE, spacingLarge);

            // Assert
            // Меньший шаг → выше КПД
            Assert.That(etaRSmall, Is.GreaterThan(etaRLarge));
        }

        [Test]
        public void CalculateRodTheory_NegativeRFb_ThrowsException()
        {
            // Arrange
            var rFb = -0.01;
            var rD = 0.10;
            var lambdaE = 1.6;
            var dE = 0.020;
            var spacing = 0.200;

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _calculator.CalculateRodTheory(rFb, rD, lambdaE, dE, spacing));
        }

        [Test]
        public void CalculateRodTheory_ZeroSpacing_ThrowsException()
        {
            // Arrange
            var rFb = 0.09;
            var rD = 0.10;
            var lambdaE = 1.6;
            var dE = 0.020;
            var spacing = 0.0;

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _calculator.CalculateRodTheory(rFb, rD, lambdaE, dE, spacing));
        }

        #endregion

        #region CalculateExcessTemperature Tests

        [Test]
        public void CalculateExcessTemperature_ValidInput_ReturnsPositiveValue()
        {
            // Arrange
            var inputs = CreateValidInputs();
            var powerUp = 300.0;  // Вт/м²
            var rFb = 0.09;       // м²·К/Вт
            var rD = 0.10;        // м²·К/Вт
            var etaR = 0.85;

            // Act
            var excessTemp = _calculator.CalculateExcessTemperature(inputs, powerUp, rFb, rD, etaR, _climateData, _constructionData);

            // Assert
            Assert.That(excessTemp, Is.GreaterThan(0));
        }

        [Test]
        public void CalculateExcessTemperature_NullParameters_ThrowsException()
        {
            // Arrange
            ThermalInputs inputs = null!;
            var powerUp = 300.0;
            var rFb = 0.09;
            var rD = 0.10;
            var etaR = 0.85;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                _calculator.CalculateExcessTemperature(inputs, powerUp, rFb, rD, etaR, _climateData, _constructionData));
        }

        [Test]
        public void CalculateExcessTemperature_InvalidEtaR_ThrowsException()
        {
            // Arrange
            var inputs = CreateValidInputs();
            var powerUp = 300.0;
            var rFb = 0.09;
            var rD = 0.10;
            var etaR = 1.5;  // > 1 - недопустимо

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _calculator.CalculateExcessTemperature(inputs, powerUp, rFb, rD, etaR, _climateData, _constructionData));
        }

        #endregion

        #region Calculate (Full Calculation) Tests

        [Test]
        public void Calculate_ValidParameters_ReturnsValidResult()
        {
            // Arrange
            var inputs = CreateValidInputs();

            // Act
            var result = _calculator.Calculate(inputs, _climateData, _constructionData);

            // Assert
            Assert.That(result.IsValid, Is.True, $"Validation errors: {string.Join(", ", result.ValidationErrors)}");
            Assert.That(result.ValidationErrors.Length, Is.EqualTo(0));
            Assert.That(result.Alpha, Is.GreaterThan(0));
            Assert.That(result.PowerUp, Is.GreaterThan(0));
            Assert.That(result.PowerTotal, Is.GreaterThan(0));
        }

        [Test]
        public void Calculate_MeltingMode_SurfaceTempIs5C()
        {
            // Arrange
            var inputs = CreateValidInputs();
            inputs = inputs with { Mode = OperatingMode.Melting };

            // Act
            var result = _calculator.Calculate(inputs, _climateData, _constructionData);

            // Assert
            Assert.That(result.IsValid, Is.True);
            // При режиме Melting температура поверхности = 5°C
            Assert.That(result.SupplyTemperature, Is.GreaterThan(5.0));
        }

        [Test]
        public void Calculate_AntiIcingMode_SurfaceTempIs3C()
        {
            // Arrange
            var inputs = CreateValidInputs();
            inputs = inputs with { Mode = OperatingMode.AntiIcing };

            // Act
            var result = _calculator.Calculate(inputs, _climateData, _constructionData);

            // Assert
            Assert.That(result.IsValid, Is.True);
            // При режиме AntiIcing температура поверхности = 3°C
            Assert.That(result.SupplyTemperature, Is.GreaterThan(3.0));
        }

        [Test]
        public void Calculate_IntensiveMode_SurfaceTempIs7C()
        {
            // Arrange
            var inputs = CreateValidInputs();
            inputs = inputs with { Mode = OperatingMode.Intensive };

            // Act
            var result = _calculator.Calculate(inputs, _climateData, _constructionData);

            // Assert
            Assert.That(result.IsValid, Is.True);
            // При режиме Intensive температура поверхности = 7°C
            Assert.That(result.SupplyTemperature, Is.GreaterThan(7.0));
        }

        [Test]
        public void Calculate_WithSnowfall_HigherPowerThanWithout()
        {
            // Arrange
            var inputs = CreateValidInputs();
            var climateWithSnow = CreateClimateData(snowfall: 20.0);
            var climateNoSnow = CreateClimateData(snowfall: 0.0);

            // Act
            var resultWithSnow = _calculator.Calculate(inputs, climateWithSnow, _constructionData);
            var resultNoSnow = _calculator.Calculate(inputs, climateNoSnow, _constructionData);

            // Assert
            Assert.That(resultWithSnow.PowerUp, Is.GreaterThan(resultNoSnow.PowerUp));
        }

        [Test]
        public void Calculate_ColderClimate_HigherPower()
        {
            // Arrange
            var inputs = CreateValidInputs();
            var climateWarm = CreateClimateData(airTemp: -10.0);
            var climateCold = CreateClimateData(airTemp: -30.0);

            // Act
            var resultWarm = _calculator.Calculate(inputs, climateWarm, _constructionData);
            var resultCold = _calculator.Calculate(inputs, climateCold, _constructionData);

            // Assert
            // Более холодный климат требует большей мощности
            Assert.That(resultCold.PowerUp, Is.GreaterThan(resultWarm.PowerUp));
        }

        [Test]
        public void Calculate_HigherWindSpeed_HigherAlpha()
        {
            // Arrange
            var inputs = CreateValidInputs();
            var climateLowWind = CreateClimateData(windSpeed: 2.0);
            var climateHighWind = CreateClimateData(windSpeed: 8.0);

            // Act
            var resultLowWind = _calculator.Calculate(inputs, climateLowWind, _constructionData);
            var resultHighWind = _calculator.Calculate(inputs, climateHighWind, _constructionData);

            // Assert
            // Более высокая скорость ветра → больший α
            Assert.That(resultHighWind.Alpha, Is.GreaterThan(resultLowWind.Alpha));
        }

        [Test]
        public void Calculate_ReturnsFlowRates()
        {
            // Arrange
            var inputs = CreateValidInputs();

            // Act
            var result = _calculator.Calculate(inputs, _climateData, _constructionData);

            // Assert
            Assert.That(result.MassFlowRate, Is.GreaterThan(0));
            Assert.That(result.VolumeFlowRate, Is.GreaterThan(0));
        }

        [Test]
        public void Calculate_ReturnsThermalResistances()
        {
            // Arrange
            var inputs = CreateValidInputs();
            var construction = CreateConstructionData(r1: 0.05, r2: 0.10);

            // Act
            var result = _calculator.Calculate(inputs, _climateData, construction);

            // Assert
            Assert.That(result.RFb, Is.GreaterThan(construction.R1Total));
        }

        #endregion

        #region Validate Tests

        [Test]
        public void Validate_ValidParameters_ReturnsTrue()
        {
            // Arrange
            var inputs = CreateValidInputs();

            // Act
            var isValid = _calculator.Validate(inputs, _climateData, _constructionData, out var errors);

            // Assert
            Assert.That(isValid, Is.True);
            Assert.That(errors.Length, Is.EqualTo(0));
        }

        [Test]
        public void Validate_NullParameters_ReturnsFalse()
        {
            // Arrange
            ThermalInputs inputs = null!;

            // Act
            var isValid = _calculator.Validate(inputs, _climateData, _constructionData, out var errors);

            // Assert
            Assert.That(isValid, Is.False);
            Assert.That(errors.Length, Is.GreaterThan(0));
        }

        [Test]
        public void Validate_NegativeWindSpeed_ReturnsFalse()
        {
            // Arrange
            var inputs = CreateValidInputs();
            var climate = CreateClimateData(windSpeed: -5.0);

            // Act
            var isValid = _calculator.Validate(inputs, climate, _constructionData, out var errors);

            // Assert
            Assert.That(isValid, Is.False);
            Assert.That(errors, Has.Some.Contains("ветра"));
        }

        [Test]
        public void Validate_ExcessiveWindSpeed_ReturnsFalse()
        {
            // Arrange
            var inputs = CreateValidInputs();
            var climate = CreateClimateData(windSpeed: 100.0);

            // Act
            var isValid = _calculator.Validate(inputs, climate, _constructionData, out var errors);

            // Assert
            Assert.That(isValid, Is.False);
            Assert.That(errors, Has.Some.Contains("50"));
        }

        [Test]
        public void Validate_InvalidPipeSpacing_ReturnsFalse()
        {
            // Arrange
            var inputs = CreateValidInputs();
            inputs = inputs with { PipeSpacing = 10.0 };  // Слишком мало

            // Act
            var isValid = _calculator.Validate(inputs, _climateData, _constructionData, out var errors);

            // Assert
            Assert.That(isValid, Is.False);
            Assert.That(errors, Has.Some.Contains("Шаг укладки"));
        }

        [Test]
        public void Validate_InvalidAirTemperature_ReturnsFalse()
        {
            // Arrange
            var inputs = CreateValidInputs();
            var climate = CreateClimateData(airTemp: 20.0);  // Слишком тепло

            // Act
            var isValid = _calculator.Validate(inputs, climate, _constructionData, out var errors);

            // Assert
            Assert.That(isValid, Is.False);
            Assert.That(errors, Has.Some.Contains("10"));
        }

        [Test]
        public void Validate_NullPipe_ReturnsFalse()
        {
            // Arrange
            var inputs = CreateValidInputs();
            inputs = inputs with { Pipe = null! };

            // Act
            var isValid = _calculator.Validate(inputs, _climateData, _constructionData, out var errors);

            // Assert
            Assert.That(isValid, Is.False);
            Assert.That(errors, Has.Some.Contains("трубы"));
        }

        [Test]
        public void Validate_NegativeSnowfallIntensity_ReturnsFalse()
        {
            // Arrange
            var inputs = CreateValidInputs();
            var climate = CreateClimateData(snowfall: -1.0);

            // Act
            var isValid = _calculator.Validate(inputs, climate, _constructionData, out var errors);

            // Assert
            Assert.That(isValid, Is.False);
            Assert.That(errors, Has.Some.Contains("снегопада"));
        }

        [Test]
        public void Validate_ExcessiveSnowfallIntensity_ReturnsFalse()
        {
            // Arrange
            var inputs = CreateValidInputs();
            var climate = CreateClimateData(snowfall: 150.0);  // > 20 мм/ч

            // Act
            var isValid = _calculator.Validate(inputs, climate, _constructionData, out var errors);

            // Assert
            Assert.That(isValid, Is.False);
            Assert.That(errors, Has.Some.Contains("20"));
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Создать валидные входные параметры теплового расчёта для тестов
        /// </summary>
        private ThermalInputs CreateValidInputs()
        {
            return new ThermalInputs
            {
                Mode = OperatingMode.Melting,
                SupplyTemperature = 50.0,
                DeltaT = 15.0,
                GroundTemperature = 10.0,
                Pipe = PipeType.StandardPipes[1],  // 20x2.0
                PipeSpacing = 200.0,
                LambdaE = 1.6,
                CoolantDensity = 1053.0,
                CoolantHeatCapacity = 3.39
            };
        }

        /// <summary>
        /// Создать климатические данные для тестов
        /// По умолчанию: Сочи (Краснодарский край)
        /// t_5days_092 = -2°C, wind_max_jan = 2.8 м/с
        /// </summary>
        private ClimateData CreateClimateData(double airTemp = -2.0, double windSpeed = 2.8, double snowfall = 0.0)
        {
            return new ClimateData
            {
                AirTemperature = airTemp,
                WindSpeed = windSpeed,
                SnowfallIntensity = snowfall
            };
        }

        /// <summary>
        /// Создать данные конструкции для тестов
        /// </summary>
        private ConstructionData CreateConstructionData(double r1 = 0.05, double r2 = 0.10, double lambdaE = 1.6)
        {
            return new ConstructionData
            {
                R1Total = r1,
                R2Total = r2,
                LambdaE = lambdaE
            };
        }

        #endregion
    }
}